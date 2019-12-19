using System;

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
            CodeGenerator generator = new CodeGenerator("JsonConverterGenerator");
            Console.WriteLine(generator.Generate(s_typesToGenerateConvertersFor));
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
