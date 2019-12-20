using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace JsonConverterGenerator
{
    public class CodeGenerator
    {
        private readonly StringBuilder _codeBuilder = new StringBuilder();

        private readonly string _outputNamespace;
        private int _indent;

        private static readonly HashSet<Type> s_simpleTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(int),
            typeof(string),
            typeof(char),
            typeof(DateTime),
            typeof(DateTimeOffset),
        };

        public CodeGenerator(string outputNamespace)
        {
            if (string.IsNullOrWhiteSpace(outputNamespace))
            {
                throw new ArgumentException(string.Format("{0} is null, empty, or is whitespace", outputNamespace), "outputNamespace");
            }

            _outputNamespace = outputNamespace;
        }

        public string Generate(Type[] types)
        {
            if (types == null || types.Length < 1)
            {
                throw new ArgumentException(string.Format("{0} is null or empty", types), "types");
            }

            WriteAutoGenerationDisclaimer();

            WriteBlankLine();

            WriteLine("using System;");
            WriteLine("using System.Buffers;");
            WriteLine("using System.Text.Json;");
            WriteLine("using System.Text.Json.Serialization;");

            WriteBlankLine();

            BeginNewControlBlock($"namespace {_outputNamespace}");
            
            for (int i = 1; i < types.Length + 1; i++)
            {
                if (WriteJsonConverterForType(types[i - 1]) && i < types.Length - 1)
                {
                    WriteBlankLine();
                }
            }

            WriteControlBlockEnd();

            return _codeBuilder.ToString();
        }

        private bool WriteJsonConverterForType(Type type)
        {
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return false;
            }

            WriteJsonConverterForObject(type);
            return true;
        }

        private void WriteJsonConverterForObject(Type type)
        {
            WriteConverterDeclaration(type);
            WriteConverterCaches(type);
            WriteConverterReadMethod(type);
            WriteBlankLine();
            WriteConverterWriteMethod(type);
            WriteControlBlockEnd();
        }

        private void WriteConverterDeclaration(Type type)
        {
            // Apply indentation.
            _codeBuilder.Append(new string(' ', _indent * 4));

            _codeBuilder.Append("public class JsonConverterFor");
            _codeBuilder.Append(type.FullName.Replace(".", ""));
            _codeBuilder.Append(": JsonConverter<");
            _codeBuilder.Append(type.Name);
            _codeBuilder.Append(">");
            MoveToNewLine();

            WriteControlBlockStart();
        }

        private void WriteConverterCaches(Type type)
        {
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            HashSet<Type> cachedTypes = new HashSet<Type>();

            foreach (PropertyInfo property in properties)
            {
                Type propertyType = property.PropertyType;

                if (cachedTypes.Contains(propertyType) || s_simpleTypes.Contains(propertyType))
                {
                    continue;
                }

                string propertyTypeName = propertyType.Name;
                
                string converterRetrievalSentinelFieldName = $"_checkedFor{propertyTypeName}Converter";
                string converterPropertyName = $"{propertyTypeName}Converter";
                string converterFieldName = $"_{char.ToLower(converterPropertyName[0])}{converterPropertyName.Substring(1)}";
                string converterReturnTypeName = $"JsonConverter<{propertyTypeName}>";

                WriteLine($"private bool {converterRetrievalSentinelFieldName};");
                WriteLine($"private {converterReturnTypeName} {converterFieldName};");

                WriteLine($"private {converterReturnTypeName} Get{converterPropertyName}(JsonSerializerOptions options)");
                WriteControlBlockStart();

                WriteLine($"if (!{converterRetrievalSentinelFieldName} && {converterFieldName} == null && options != null)");
                WriteControlBlockStart();

                WriteLine($"{converterFieldName} = ({converterReturnTypeName})options.GetConverter(typeof({propertyTypeName}));");
                WriteLine($"{converterRetrievalSentinelFieldName} = true;");

                WriteControlBlockEnd();

                WriteBlankLine();

                WriteLine($"return {converterFieldName};");

                WriteControlBlockEnd();
                
                WriteBlankLine();

                cachedTypes.Add(propertyType);
            }
        }

        private void WriteThrowJsonException()
        {
            WriteLine("throw new JsonException();");
        }

        private void WriteConverterReadMethod(Type type)
        {
            WriteMethodStart(
                level: AccessibilityLevel.Public,
                isOverride: true,
                returnTypeName: $"{type.Name}",
                methodName: "Read",
                parameterListValue: "ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options");

            string typeFullName = type.FullName;

            // Validate that the reader's cursor is at a start token.
            WriteSingleLineComment("Validate that the reader's cursor is at a start token");
            WriteLine("if (reader.TokenType != JsonTokenType.StartObject)");
            WriteControlBlockStart();
            WriteThrowJsonException();
            WriteControlBlockEnd();

            WriteBlankLine();

            // Create returned object. This assumes type has public parameterless ctor.
            WriteSingleLineComment("Create returned object. This assumes type has public parameterless ctor");
            WriteLine($"{typeFullName} value = new {typeFullName}();");

            WriteBlankLine();

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (properties.Length > 0)
            {
                // Read all properties.
                WriteSingleLineComment("Read all properties");
                WriteLine("while (true)");
                WriteControlBlockStart();

                WriteLine(@$"reader.Read();");
                WriteBlankLine();

                WriteLine("if (reader.TokenType == JsonTokenType.EndObject)");
                WriteControlBlockStart();
                WriteLine("break;");
                WriteControlBlockEnd();

                WriteBlankLine();

                // Note that we don't check for escaping: only unescaped property names are accounted for.
                WriteSingleLineComment("Only unescaped property names are allowed");
                WriteLine("ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;");

                WriteBlankLine();

                WriteSingleLineComment("Move reader cursor to property value");
                WriteLine(@$"reader.Read();");

                WriteBlankLine();

                // Try to match property name with object properties (case sensitive).
                WriteSingleLineComment("Try to match property name with object properties (case sensitive)");
                WriteBlankLine();

                for (int i = 0; i < properties.Length; i++)
                {
                    PropertyInfo property = properties[i];
                    Type propertyType = property.PropertyType;
                    string objectPropertyName = property.Name;
                    string propertyTypeName = propertyType.Name;

                    string elsePrefix = i > 0 ? "else " : "";

                    WriteSingleLineComment($"Determine if JSON property matches '{objectPropertyName}'");
                    WriteLine(@$"{elsePrefix}if ({GeneratePropertyNameMatchCondition(objectPropertyName)})");
                    WriteControlBlockStart();

                    if (propertyType == typeof(char))
                    {
                        WriteLine("string tmp = reader.GetString();");
                        WriteLine("if (string.IsNullOrEmpty(tmp))");
                        WriteControlBlockStart();
                        WriteThrowJsonException();
                        WriteControlBlockEnd();

                        WriteBlankLine();

                        WriteLine($"value.{objectPropertyName} = tmp[0];");
                    }
                    else if (s_simpleTypes.Contains(propertyType))
                    {
                        WriteLine($"value.{objectPropertyName} = reader.Get{propertyType.Name}();");
                    }
                    else
                    {
                        WriteLine($"JsonConverter<{propertyTypeName}> converter = Get{propertyTypeName}Converter(options);");

                        WriteLine("if (converter != null)");
                        WriteControlBlockStart();

                        WriteLine($"value.{objectPropertyName} = converter.Read(ref reader, typeToConvert, options);");

                        WriteControlBlockEnd();

                        WriteLine("else");
                        WriteControlBlockStart();

                        WriteLine($"value.{objectPropertyName} = JsonSerializer.Deserialize<{propertyTypeName}>(ref reader, options);");

                        WriteControlBlockEnd();
                    }

                    WriteControlBlockEnd();
                }
            };

            WriteControlBlockEnd();

            WriteBlankLine();

            WriteLine($"return value;");

            WriteControlBlockEnd();
        }

        private string GeneratePropertyNameMatchCondition(string objectPropertyName)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"propertyName.Length == {objectPropertyName.Length}");

            for (int i = 0; i < objectPropertyName.Length; i++)
            {
                sb.Append($" && propertyName[{i}] == (byte)'{objectPropertyName[i]}'");
            }

            return sb.ToString();
        }

        private void WriteSingleLineComment(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                WriteLine($"// {value}.");
            }
        }

        private void WriteConverterWriteMethod(Type type)
        {
            WriteMethodStart(
                level: AccessibilityLevel.Public,
                isOverride: true,
                returnTypeName: "void",
                methodName: "Write",
                parameterListValue: $"Utf8JsonWriter writer, {type.Name} value, JsonSerializerOptions options");

            WriteLine($"writer.WriteNullValue();");

            WriteControlBlockEnd();
        }

        private void WriteAutoGenerationDisclaimer()
        {
            WriteLine($@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:{Environment.Version}
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------");
        }

        private void WriteBlankLine()
        {
            WriteLine("");
        }

        private void MoveToNewLine()
        {
            _codeBuilder.Append("\n");
        }

        private void WriteLine(string value)
        {
            if (_indent > 0)
            {
                _codeBuilder.Append(new string(' ', _indent * 4));
            }
            _codeBuilder.AppendLine(value);
        }

        private void WriteMethodStart(AccessibilityLevel level, bool isOverride, string returnTypeName, string methodName, string parameterListValue)
        {
            // Apply indentation.
            _codeBuilder.Append(new string(' ', _indent * 4));

            if (level == AccessibilityLevel.Public)
            {
                _codeBuilder.Append("public");
            }

            _codeBuilder.Append(" ");

            if (isOverride)
            {
                _codeBuilder.Append("override");
                _codeBuilder.Append(" ");
            }

            _codeBuilder.Append(returnTypeName);
            
            _codeBuilder.Append(" ");

            _codeBuilder.Append(methodName);
            _codeBuilder.Append("(");
            _codeBuilder.Append(parameterListValue);
            _codeBuilder.Append(")");
            MoveToNewLine();

            WriteControlBlockStart();
        }

        private void BeginNewControlBlock(string value)
        {
            if (_indent > 0)
            {
                _codeBuilder.Append(new string(' ', _indent * 4));
            }
            _codeBuilder.AppendLine(value);

            WriteControlBlockStart();
        }

        private void WriteControlBlockStart()
        {
            WriteLine("{");
            Indent();
        }

        private void WriteControlBlockEnd()
        {
            Unindent();
            WriteLine("}");
        }

        private void Indent()
        {
            _indent++;
        }

        private void Unindent()
        {
            _indent--;
        }
    }

    internal enum AccessibilityLevel
    {
        Public,
    }
}
