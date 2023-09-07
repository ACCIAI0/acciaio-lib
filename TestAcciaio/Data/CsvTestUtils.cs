using Acciaio.Data;

namespace Test.Acciaio.Data;

public static class CsvTestUtils
{
    public sealed class Person
    {
        public string Name { get; private set; }
        public string LastName { get; private set; }
        public float Height { get; private set; }
        public DateTime DateOfBirth { get; private set; }
    }

    public sealed class AristocraticPerson
    {
        [CsvHeaderMapper("Name")]
        public string Appellative { get; private set; }
        [CsvHeaderMapper("LastName")]
        public string Surname { get; private set; }
        public float Height { get; private set; }
        public DateTime DateOfBirth { get; private set; }
    }

    [CsvTypeMapper(typeof(Mapper))]
    public sealed class MappablePerson
    {
        public sealed class Mapper : ICsvTypeMapper
        {
            public object Map(CsvRow row)
            {
                var person = new MappablePerson
                {
                    Name = row[NameHeader].StringValue,
                    LastName = row[LastNameHeader].StringValue,
                    Height = row[HeightHeader].FloatValue,
                    Age = (DateTime.Now - row[DateOfBirthHeader].DateTimeValue).Days / 365
                };

                return person;
            }
        }
        
        public string Name { get; private set; }
        public string LastName { get; private set; }
        public float Height { get; private set; }
        public int Age { get; private set; }
    }
    
   public const string NameHeader = "Name";
   public const string LastNameHeader = "LastName";
   public const string HeightHeader = "Height";
   public const string DateOfBirthHeader = "DateOfBirth";

   public static readonly string SimpleCsv = """
                                             Mario,Rossi,1.76,10/22/2000
                                             John,Doe,1.82,08/16/1996
                                             Mary,Jean,1.61,10/21/1995
                                             Luigi,Rossi,1.76,10/22/2000
                                             """;
   
   public static readonly string CsvWithHeaders = $"{NameHeader},{LastNameHeader},{HeightHeader},{DateOfBirthHeader}\n{SimpleCsv}";

   public static readonly string ItalianCultureCsv = $"""
                                                     {NameHeader};{LastNameHeader};{HeightHeader};{DateOfBirthHeader}
                                                     "Giuseppe Mario";Rossi;1,50;31/01/1981
                                                     Giuseppe Luigi;Rossi;1,50;31/01/1981
                                                     "Wario Quseppe";Bianchi;1,55;31/01/1981
                                                     "Waluigi Quseppe";Bianchi;1,80;31/01/1981
                                                     """;
}