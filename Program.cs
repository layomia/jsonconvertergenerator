using System;
using System.IO;

namespace JsonConverterGenerator
{
    class Program
    {
        private static readonly Type[] s_typesToGenerateConvertersFor = new Type[]
        {
            typeof(BasicPerson),
            typeof(int[]),
            typeof(BasicJsonAddress),
        };

        static void Main(string[] args)
        {
            CodeGenerator generator = new CodeGenerator(outputNamespace: "JsonConverterGenerator");
            string generatedCode = generator.Generate(s_typesToGenerateConvertersFor);

            string examplesDirPath = Path.Join(Directory.GetCurrentDirectory(), "examples");
            Directory.CreateDirectory(examplesDirPath);

            File.WriteAllText(Path.Join(examplesDirPath, "Converters.cs"), generatedCode); ;
            Console.WriteLine(generatedCode);
        }
    }

    public class BasicPerson
    {
        public int Age { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        //public List<string> PhoneNumbers { get; set; }
        //public BasicJsonAddress Address { get; set; }
    }

    public class BasicJsonAddress
    {
        public string Street { get; set; }
        public string City { get; set; }
        public int Zip { get; set; }
    }
}
