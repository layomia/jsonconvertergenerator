using System;
using System.Collections.Generic;
using System.IO;

namespace JsonConverterGenerator
{
    class Program
    {
        private static readonly Type[] s_typesToGenerateConvertersFor = new Type[]
        {
            typeof(LoginViewModel),
            typeof(Location),
            typeof(IndexViewModel),
            typeof(MyEventsListerViewModel),
            typeof(CollectionsOfPrimitives),
        };

        static void Main(string[] args)
        {
            CodeGenerator generator = new CodeGenerator(outputNamespace: "JsonConverterGenerator");
            Dictionary<Type, string> generatedCode = generator.Generate(s_typesToGenerateConvertersFor);

            string examplesDirPath = Path.Join(Directory.GetCurrentDirectory(), "examples");
            Directory.CreateDirectory(examplesDirPath);

            foreach (KeyValuePair<Type, string> pair in generatedCode)
            {
                File.WriteAllText(Path.Join(
                    examplesDirPath,
                    $"JsonConverterFor{CodeGenerator.GetReadableTypeName(pair.Key)}.cs"), pair.Value);
                Console.WriteLine(generatedCode);
            }
        }
    }
}
