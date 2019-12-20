using System;

namespace JsonConverterGenerator
{
    public class BasicPerson
    {
        public int Age { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public char MiddleInitial { get; set; }
        public DateTimeOffset BirthDate { get; set; }
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
