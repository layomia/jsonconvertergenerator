using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        private HashSet<Type> generatedTypes = new HashSet<Type>();
        private Dictionary<Type, string> _generatedCode = new Dictionary<Type, string>();

        public CodeGenerator(string outputNamespace)
        {
            if (string.IsNullOrWhiteSpace(outputNamespace))
            {
                throw new ArgumentException(string.Format("{0} is null, empty, or is whitespace", outputNamespace), "outputNamespace");
            }

            _outputNamespace = outputNamespace;
        }

        public Dictionary<Type, string> Generate(Type[] types)
        {
            if (types == null || types.Length < 1)
            {
                throw new ArgumentException(string.Format("{0} is null or empty", types), "types");
            }

            foreach (Type type in types)
            {
                WriteJsonConverterForTypeIfAbsent(type);
            }

            return _generatedCode;
        }

        private void WriteJsonConverterForTypeIfAbsent(Type type)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(type))
            {
                WriteJsonConverterWorker(type);
                _generatedCode[type] = _codeBuilder.ToString();
                _codeBuilder.Clear();
            }
        }

        private void WriteJsonConverterWorker(Type type)
        {
            WriteAutoGenerationDisclaimer();

            WriteBlankLine();

            // TODO: Dynamically generate this with input types as a factor.
            WriteLine("using System;");
            WriteLine("using System.Buffers;");
            WriteLine("using System.Collections.Generic;");
            WriteLine("using System.Runtime.InteropServices;");
            WriteLine("using System.Text.Json;");
            WriteLine("using System.Text.Json.Serialization;");

            WriteBlankLine();

            BeginNewControlBlock($"namespace {_outputNamespace}");

            WriteConverterDeclaration(type);
            WritePropertyNameConstants(type);
            WriteConverterCaches(type);
            WriteConverterReadMethod(type);
            WriteBlankLine();
            WriteConverterWriteMethod(type);
            WriteControlBlockEnd();

            WriteControlBlockEnd();
        }

        private void WriteConverterDeclaration(Type type)
        {
            // Apply indentation.
            _codeBuilder.Append(new string(' ', _indent * 4));

            _codeBuilder.Append("public class JsonConverterFor");
            _codeBuilder.Append(type.Name.Replace(".", ""));
            _codeBuilder.Append(": JsonConverter<");
            _codeBuilder.Append(type.Name);
            _codeBuilder.Append(">");
            MoveToNewLine();

            WriteControlBlockStart();
        }

        private static string GetCompilableTypeName(Type type)
        {
            string typeName = type.Name;

            if (!type.IsGenericType)
            {
                return typeName;
            }

            // TODO: Guard against open generics?
            Debug.Assert(!type.ContainsGenericParameters);

            int backTickIndex = typeName.IndexOf('`');
            string baseName = typeName.Substring(0, backTickIndex);

            return $"{baseName}<{string.Join(',', type.GetGenericArguments().Select(arg => GetCompilableTypeName(arg)))}>";
        }

        public static string GetReadableTypeName(Type type)
        {
            return GetReadableTypeName(GetCompilableTypeName(type));
        }

        private static string GetReadableTypeName(string compilableName)
        {
            return compilableName.Replace("<", "").Replace(">", "").Replace(",", "").Replace("[]", "Array");
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

                string compilableTypeName = GetCompilableTypeName(propertyType);
                string readableTypeName = GetReadableTypeName(compilableTypeName);

                string converterRetrievalSentinelFieldName = $"_checkedFor{readableTypeName}Converter";
                string converterPropertyName = $"{readableTypeName}Converter";
                string converterFieldName = $"_{char.ToLower(converterPropertyName[0])}{converterPropertyName.Substring(1)}";
                string converterReturnTypeName = $"JsonConverter<{compilableTypeName}>";

                WriteLine($"private bool {converterRetrievalSentinelFieldName};");
                WriteLine($"private {converterReturnTypeName} {converterFieldName};");

                WriteLine($"private {converterReturnTypeName} Get{converterPropertyName}(JsonSerializerOptions options)");
                WriteControlBlockStart();

                WriteLine($"if (!{converterRetrievalSentinelFieldName} && {converterFieldName} == null && options != null)");
                WriteControlBlockStart();

                WriteLine($"{converterFieldName} = ({converterReturnTypeName})options.GetConverter(typeof({compilableTypeName}));");
                WriteLine($"{converterRetrievalSentinelFieldName} = true;");

                WriteControlBlockEnd();

                WriteBlankLine();

                WriteLine($"return {converterFieldName};");

                WriteControlBlockEnd();

                WriteBlankLine();

                cachedTypes.Add(propertyType);
            }
        }

        private void WritePropertyNameConstants(Type type)
        {
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (PropertyInfo property in properties)
            {
                string objectPropertyName = property.Name;
                int objectPropertyNameLength = objectPropertyName.Length;

                StringBuilder sb = new StringBuilder();

                sb.Append($"private static ReadOnlySpan<byte> {objectPropertyName}Bytes => new byte[{objectPropertyNameLength}] {{ (byte)'{objectPropertyName[0]}'");

                for (int i = 1; i < objectPropertyNameLength; i++)
                {
                    sb.Append($", (byte)'{objectPropertyName[i]}'");
                }

                sb.Append(" };");

                WriteLine(sb.ToString());
            }

            WriteBlankLine();
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

            string typeName = type.Name;

            // Validate that the reader's cursor is at a start token.
            WriteSingleLineComment("Validate that the reader's cursor is at a start token");
            WriteLine("if (reader.TokenType != JsonTokenType.StartObject)");
            WriteControlBlockStart();
            WriteThrowJsonException();
            WriteControlBlockEnd();

            WriteBlankLine();

            // Create returned object. This assumes type has public parameterless ctor.
            WriteSingleLineComment("Create returned object. This assumes type has public parameterless ctor");
            WriteLine($"{typeName} value = new {typeName}();");

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

                    // Ignore readonly properties.
                    if (!property.CanWrite)
                    {
                        continue;
                    }

                    Type propertyType = property.PropertyType;
                    string objectPropertyName = property.Name;

                    string compilableTypeName = GetCompilableTypeName(propertyType);
                    string readableTypeName = GetReadableTypeName(compilableTypeName);

                    string elsePrefix = i > 0 ? "else " : "";

                    WriteSingleLineComment($"Determine if JSON property matches '{objectPropertyName}'");
                    WriteLine(@$"{elsePrefix}if ({objectPropertyName}Bytes.SequenceEqual(propertyName))");
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
                        WriteLine($"JsonConverter<{compilableTypeName}> converter = Get{readableTypeName}Converter(options);");

                        WriteLine("if (converter != null)");
                        WriteControlBlockStart();

                        WriteLine($"value.{objectPropertyName} = converter.Read(ref reader, typeToConvert, options);");

                        WriteControlBlockEnd();

                        WriteLine("else");
                        WriteControlBlockStart();

                        WriteLine($"value.{objectPropertyName} = JsonSerializer.Deserialize<{compilableTypeName}>(ref reader, options);");

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

            // Write null and return if value is null.
            WriteLine("if (value == null)");
            WriteControlBlockStart();
            WriteLine("writer.WriteNullValue();");
            WriteLine("return;");
            WriteControlBlockEnd();

            WriteBlankLine();

            WriteLine("writer.WriteStartObject();");

            WriteBlankLine();

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (PropertyInfo property in properties)
            {
                Type propertyType = property.PropertyType;
                string objectPropertyName = property.Name;

                string compilableTypeName = GetCompilableTypeName(propertyType);
                string readableTypeName = GetReadableTypeName(compilableTypeName);

                string jsonPropertyBytesVarName = $"{objectPropertyName}Bytes";

                string currentValueName = $"value.{objectPropertyName}";

                if (propertyType == typeof(int))
                {
                    WriteLine($@"writer.WriteNumber({jsonPropertyBytesVarName}, {currentValueName});");
                }
                else if (propertyType == typeof(char))
                {
                    WriteLine($"char charValue = {currentValueName};");
                    WriteSingleLineComment("Assume we are running NetCore app");
                    WriteLine($@"writer.WriteString({jsonPropertyBytesVarName}, MemoryMarshal.CreateSpan(ref charValue, 1));");
                }
                else if (propertyType == typeof(bool))
                {
                    WriteLine($@"writer.WriteBoolean({jsonPropertyBytesVarName}, {currentValueName});");
                }
                else if (s_simpleTypes.Contains(propertyType))
                {
                    WriteLine($@"writer.WriteString({jsonPropertyBytesVarName}, {currentValueName});");
                }
                else
                {
                    WriteLine($@"writer.WritePropertyName({jsonPropertyBytesVarName});");

                    // Confine converter name to local scope.
                    WriteControlBlockStart();

                    WriteLine($"JsonConverter<{compilableTypeName}> converter = Get{readableTypeName}Converter(options);");

                    WriteLine("if (converter != null)");
                    WriteControlBlockStart();

                    WriteLine($"converter.Write(writer, {currentValueName}, options);");

                    WriteControlBlockEnd();

                    WriteLine("else");
                    WriteControlBlockStart();

                    WriteLine($"JsonSerializer.Serialize<{compilableTypeName}>(writer, {currentValueName}, options);");

                    WriteControlBlockEnd();
                    WriteControlBlockEnd();
                }

                WriteBlankLine();
            }

            WriteLine("writer.WriteEndObject();");

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
