using System;
using System.Buffers;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonConverterGenerator
{
    class Program
    {
        private static readonly string s_benchmarkCommandSample = "e.g. dotnet run Benchmarks Serialize Default LoginViewModel";
        private static readonly string s_firstArgPrompt = "First arg should be Generator or Benchmarks";

        private static readonly Type[] s_typesToGenerateConvertersFor = new Type[]
        {
            typeof(LoginViewModel),
            typeof(Location),
            typeof(IndexViewModel),
            typeof(MyEventsListerViewModel),
            typeof(CollectionsOfPrimitives),
        };

        public static void Main(string[] args)
        {
            switch (args[0])
            {
                case "Generator":
                    GenerateConverters();
                    break;
                case "Benchmarks":
                    RunBenchmarks(args);
                    break;
                default:
                    throw new ArgumentException(s_firstArgPrompt);
            }
        }

        private static void GenerateConverters()
        {
            CodeGenerator generator = new CodeGenerator(outputNamespace: "JsonConverterGenerator");
            Dictionary<Type, string> generatedCode = generator.Generate(s_typesToGenerateConvertersFor, out string helperSource);

            string examplesDirPath = Path.Join(Directory.GetCurrentDirectory(), "examples");
            Directory.CreateDirectory(examplesDirPath);

            foreach (KeyValuePair<Type, string> pair in generatedCode)
            {
                File.WriteAllText(Path.Join(examplesDirPath, $"JsonConverterFor{CodeGenerator.GetReadableTypeName(pair.Key)}.cs"),
                    pair.Value);
            }

            File.WriteAllText(Path.Join(examplesDirPath, $"JsonConverterHelpers.cs"), helperSource);
        }

        private static void RunBenchmarks(string[] args)
        {
            if (args.Length < 4)
            {
                throw new ArgumentException(s_benchmarkCommandSample);
            }

            string process = args[1];
            string mechanism = args[2];
            string type = args[3];

            if (process == "Serialize")
            {
                RunSerializationBenchmark(mechanism, type);   
            }
            else if (process == "Deserialize")
            {
                RunDeserializationBenchmark(mechanism, type);
            }
            else
            {
                throw new ArgumentException(s_benchmarkCommandSample);
            }
        }

        private static void WriteElapsedTime(long ticks)
        {
            Console.Write(ticks / (TimeSpan.TicksPerMillisecond / 1000));
        }

        private static void RunDeserializationBenchmark_Default<T>()
        {
            T value = DataGenerator.Generate<T>();

            var options = new JsonSerializerOptions();
            string serialized = JsonSerializer.Serialize(value, options);

            var sw = new Stopwatch();
            sw.Start();
            JsonSerializer.Deserialize<T>(serialized);
            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunDeserializationBenchmark_AOT_LoadConverters<T>()
        {
            T value = DataGenerator.Generate<T>();

            var options = new JsonSerializerOptions();
            string serialized = JsonSerializer.Serialize(value, options);

            // Load converters
            options = new JsonSerializerOptions();            
            foreach (JsonConverter converter in GetAotConverters())
            {
                options.Converters.Add(converter);
            }

            var sw = new Stopwatch();
            sw.Start();
            JsonSerializer.Deserialize<T>(serialized, options);
            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunDeserializationBenchmark_AOT_Raw<T>()
        {
            T value = DataGenerator.Generate<T>();

            var options = new JsonSerializerOptions();
            string serialized = JsonSerializer.Serialize(value, options);

            JsonConverter<T> aotConverter = GetAotConverter<T>();
            
            var sw = new Stopwatch();
            sw.Start();

            const long ArrayPoolMaxSizeBeforeUsingNormalAlloc = 1024 * 1024;

            // In the worst case, a single UTF-16 character could be expanded to 3 UTF-8 bytes.
            // Only surrogate pairs expand to 4 UTF-8 bytes but that is a transformation of 2 UTF-16 characters goign to 4 UTF-8 bytes (factor of 2).
            // All other UTF-16 characters can be represented by either 1 or 2 UTF-8 bytes.
            const int MaxExpansionFactorWhileTranscoding = 3;

            byte[] tempArray = null;

            // For performance, avoid obtaining actual byte count unless memory usage is higher than the threshold.
            Span<byte> utf8 = serialized.Length <= (ArrayPoolMaxSizeBeforeUsingNormalAlloc / MaxExpansionFactorWhileTranscoding) ?
            // Use a pooled alloc.
                tempArray = ArrayPool<byte>.Shared.Rent(serialized.Length * MaxExpansionFactorWhileTranscoding) :
                // Use a normal alloc since the pool would create a normal alloc anyway based on the threshold (per current implementation)
                // and by using a normal alloc we can avoid the Clear().
                new byte[Encoding.UTF8.GetByteCount(serialized.AsSpan())];

            int actualByteCount = Encoding.UTF8.GetBytes(serialized.AsSpan(), utf8);
            utf8 = utf8.Slice(0, actualByteCount);

            var reader = new Utf8JsonReader(utf8);

            if (reader.Read())
            {
                aotConverter.Read(ref reader, typeof(T), null);

                if (tempArray != null)
                {
                    utf8.Clear();
                    ArrayPool<byte>.Shared.Return(tempArray);
                }
            }

            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunDeserializationBenchmark_Jil<T>()
        {
            T value = DataGenerator.Generate<T>();

            string serialized = Jil.JSON.Serialize<T>(value, Jil.Options.ISO8601);

            var sw = new Stopwatch();
            sw.Start();
            Jil.JSON.Deserialize<T>(serialized, Jil.Options.ISO8601);
            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunDeserializationBenchmark_JsonNet<T>()
        {
            T value = DataGenerator.Generate<T>();

            string serialized = Newtonsoft.Json.JsonConvert.SerializeObject(value);

            var sw = new Stopwatch();
            sw.Start();
            Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serialized);
            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunDeserializationBenchmark_Utf8Json<T>()
        {
            T value = DataGenerator.Generate<T>();

            string serialized = Utf8Json.JsonSerializer.ToJsonString(value);

            var sw = new Stopwatch();
            sw.Start();
            Utf8Json.JsonSerializer.Deserialize<T>(serialized);
            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunSerializationBenchmark_Default<T>()
        {
            T value = DataGenerator.Generate<T>();
            
            // System.Text.Json
            var options = new JsonSerializerOptions();

            var sw = new Stopwatch();
            sw.Start();
            JsonSerializer.Serialize(value, options);
            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunSerializationBenchmark_AOT_LoadConverters<T>()
        {
            T value = DataGenerator.Generate<T>();

            // System.Text.Json
            var options = new JsonSerializerOptions();
            foreach (JsonConverter converter in GetAotConverters())
            {
                options.Converters.Add(converter);
            }


            var sw = new Stopwatch();
            sw.Start();
            JsonSerializer.Serialize(value, options);
            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunSerializationBenchmark_AOT_Raw<T>()
        {
            T value = DataGenerator.Generate<T>();

            JsonConverter<T> aotConverter = GetAotConverter<T>();

            var sw = new Stopwatch();
            sw.Start();

            const int DefaultBufferSize = 16384;

            using (var output = new PooledByteBufferWriter(DefaultBufferSize))
            {
                using (var writer = new Utf8JsonWriter(output))
                {
                    aotConverter.Write(writer, value, null);
                }

                Encoding.UTF8.GetString(output.WrittenMemory.Span);
            }

            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunSerializationBenchmark_Jil<T>()
        {
            T value = DataGenerator.Generate<T>();

            var sw = new Stopwatch();
            sw.Start();
            Jil.JSON.Serialize<T>(value, Jil.Options.ISO8601);
            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunSerializationBenchmark_JsonNet<T>()
        {
            T value = DataGenerator.Generate<T>();

            var sw = new Stopwatch();
            sw.Start();
            Newtonsoft.Json.JsonConvert.SerializeObject(value);
            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunSerializationBenchmark_Utf8Json<T>()
        {
            T value = DataGenerator.Generate<T>();

            var sw = new Stopwatch();
            sw.Start();
            Utf8Json.JsonSerializer.ToJsonString(value);
            sw.Stop();
            WriteElapsedTime(sw.ElapsedTicks);
        }

        private static void RunDeserializationBenchmark(string mechanism, string type)
        {
            switch (mechanism)
            {
                case "Default":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunDeserializationBenchmark_Default<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunDeserializationBenchmark_Default<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunDeserializationBenchmark_Default<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunDeserializationBenchmark_Default<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunDeserializationBenchmark_Default<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                case "AOT_LoadConverters":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunDeserializationBenchmark_AOT_LoadConverters<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunDeserializationBenchmark_AOT_LoadConverters<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunDeserializationBenchmark_AOT_LoadConverters<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunDeserializationBenchmark_AOT_LoadConverters<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunDeserializationBenchmark_AOT_LoadConverters<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                case "AOT_Raw":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunDeserializationBenchmark_AOT_Raw<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunDeserializationBenchmark_AOT_Raw<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunDeserializationBenchmark_AOT_Raw<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunDeserializationBenchmark_AOT_Raw<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunDeserializationBenchmark_AOT_Raw<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                case "Jil":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunDeserializationBenchmark_Jil<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunDeserializationBenchmark_Jil<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunDeserializationBenchmark_Jil<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunDeserializationBenchmark_Jil<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunDeserializationBenchmark_Jil<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                case "Json.NET":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunDeserializationBenchmark_JsonNet<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunDeserializationBenchmark_JsonNet<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunDeserializationBenchmark_JsonNet<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunDeserializationBenchmark_JsonNet<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunDeserializationBenchmark_JsonNet<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                case "Utf8Json":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunDeserializationBenchmark_Utf8Json<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunDeserializationBenchmark_Utf8Json<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunDeserializationBenchmark_Utf8Json<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunDeserializationBenchmark_Utf8Json<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunDeserializationBenchmark_Utf8Json<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                default:
                    throw new ArgumentException(s_benchmarkCommandSample);
            }
        }

        private static void RunSerializationBenchmark(string mechanism, string type)
        {
            switch (mechanism)
            {
                case "Default":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunSerializationBenchmark_Default<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunSerializationBenchmark_Default<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunSerializationBenchmark_Default<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunSerializationBenchmark_Default<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunSerializationBenchmark_Default<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                case "AOT_LoadConverters":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunSerializationBenchmark_AOT_LoadConverters<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunSerializationBenchmark_AOT_LoadConverters<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunSerializationBenchmark_AOT_LoadConverters<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunSerializationBenchmark_AOT_LoadConverters<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunSerializationBenchmark_AOT_LoadConverters<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                case "AOT_Raw":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunSerializationBenchmark_AOT_Raw<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunSerializationBenchmark_AOT_Raw<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunSerializationBenchmark_AOT_Raw<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunSerializationBenchmark_AOT_Raw<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunSerializationBenchmark_AOT_Raw<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                case "Jil":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunSerializationBenchmark_Jil<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunSerializationBenchmark_Jil<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunSerializationBenchmark_Jil<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunSerializationBenchmark_Jil<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunSerializationBenchmark_Jil<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                case "Json.NET":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunSerializationBenchmark_JsonNet<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunSerializationBenchmark_JsonNet<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunSerializationBenchmark_JsonNet<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunSerializationBenchmark_JsonNet<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunSerializationBenchmark_JsonNet<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                case "Utf8Json":
                    {
                        switch (type)
                        {
                            case "LoginViewModel":
                                {
                                    RunSerializationBenchmark_Utf8Json<LoginViewModel>();
                                }
                                break;
                            case "Location":
                                {
                                    RunSerializationBenchmark_Utf8Json<Location>();
                                }
                                break;
                            case "IndexViewModel":
                                {
                                    RunSerializationBenchmark_Utf8Json<IndexViewModel>();
                                }
                                break;
                            case "MyEventsListerViewModel":
                                {
                                    RunSerializationBenchmark_Utf8Json<MyEventsListerViewModel>();
                                }
                                break;
                            case "CollectionsOfPrimitives":
                                {
                                    RunSerializationBenchmark_Utf8Json<CollectionsOfPrimitives>();
                                }
                                break;
                            default:
                                throw new ArgumentException(s_benchmarkCommandSample);
                        }
                    }
                    break;
                default:
                    throw new ArgumentException(s_benchmarkCommandSample);
            }
        }

        static JsonConverter<T> GetAotConverter<T>()
        {
            JsonConverter<T> aotConverter;

            if (typeof(T) == typeof(LoginViewModel))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForLoginViewModel.Instance;
            }
            else if (typeof(T) == typeof(Location))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForLocation.Instance;
            }
            else if (typeof(T) == typeof(IndexViewModel))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForIndexViewModel.Instance;
            }
            else if (typeof(T) == typeof(MyEventsListerViewModel))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForMyEventsListerViewModel.Instance;
            }
            else if (typeof(T) == typeof(CollectionsOfPrimitives))
            {
                aotConverter = (JsonConverter<T>)(object)JsonConverterForCollectionsOfPrimitives.Instance;
            }
            else
            {
                throw new NotSupportedException();
            }

            return aotConverter;
        }

        static IEnumerable<JsonConverter> GetAotConverters()
        {
            yield return JsonConverterForLoginViewModel.Instance;
            yield return JsonConverterForLocation.Instance;
            yield return JsonConverterForIndexViewModel.Instance;
            yield return JsonConverterForMyEventsListerViewModel.Instance;
            yield return JsonConverterForCollectionsOfPrimitives.Instance;
        }
    }
}
