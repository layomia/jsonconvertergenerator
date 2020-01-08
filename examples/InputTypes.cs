using System;
using System.Collections.Generic;

namespace JsonConverterGenerator
{
    public class BasicPerson
    {
        public int Age { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public char MiddleInitial { get; set; }
        public DateTimeOffset BirthDate { get; set; }
    }

    public class BasicPersonWithCollections
    {
        public int Age { get; set; }
        public string First { get; set; }
        public ISet<List<Dictionary<string,int>>> RandomData { get; set; }
        public string Last { get; set; }
        public char MiddleInitial { get; set; }
        public DateTimeOffset BirthDate { get; set; }
        public List<string> OtherName { get; set; }
        public Dictionary<string, int> PhoneNumbers { get; set; }
    }

    public class BasicPersonWithComplexTypes
    {
        public int Age { get; set; }
        public string First { get; set; }
        public BasicJsonAddress HomeAddress { get; set; }
        public List<BasicJsonAddress> OtherAddresses { get; set; }
    }

    public class BasicJsonAddress
    {
        public string Street { get; set; }
        public string City { get; set; }
        public int Zip { get; set; }
    }
}
