using Acciaio.Data;

namespace Test.Acciaio.Data;

public static class CsvTestUtils
{
    public sealed class Person
    {
        public string? Name { get; private set; }
        public string? LastName { get; private set; }
        public float Height { get; private set; }
        public DateTime DateOfBirth { get; private set; }
        
        [CsvIgnore] 
        public int EyesCount { get; private set; } = 2;
    }

    public sealed class AristocraticPerson
    {
        [CsvHeaderMapper("Name")]
        public string? Appellative { get; private set; }
        [CsvHeaderMapper("LastName")]
        public string? Surname { get; private set; }
        public float Height { get; private set; }
        public DateTime DateOfBirth { get; private set; }
    }

    [CsvTypeMapper(typeof(Mapper))]
    public sealed class MappablePerson
    {
        public sealed class Mapper : ICsvTypeMapper
        {
            public bool TryMap(ICsvRow row, out object obj)
            {
                obj = new MappablePerson
                {
                    Name = row[NameHeader].StringValue,
                    LastName = row[LastNameHeader].StringValue,
                    Height = row[HeightHeader].FloatValue,
                    Age = (DateTime.Now - row[DateOfBirthHeader].DateTimeValue).Days / 365
                };

                return true;
            }

            public bool TryMap(object obj, ICsvRow row)
            {
                if (obj is not MappablePerson person) return false;
                row[NameHeader].StringValue = person.Name ?? string.Empty;
                row[LastNameHeader].StringValue = person.LastName ?? string.Empty;
                row[HeightHeader].FloatValue = person.Height;
                row[DateOfBirthHeader].DateTimeValue = DateTime.Now - TimeSpan.FromDays(person.Age * 365);
                return true;
            }
        }
        
        public string? Name { get; private set; }
        public string? LastName { get; private set; }
        public float Height { get; private set; }
        public int Age { get; private set; }
    }
    
   public const string NameHeader = "Name";
   public const string LastNameHeader = "LastName";
   public const string HeightHeader = "Height";
   public const string DateOfBirthHeader = "DateOfBirth";

   public const string SimpleCsv = "\"Mario, the Dog\",Rossi,1.76,10/22/2000\nJohn,Doe,1.82,08/16/1996\nMary,Jean,1.61,10/21/1995\nLuigi,Rossi,1.76,10/22/2000";

   public const string CsvWithHeaders = $"{NameHeader},{LastNameHeader},{HeightHeader},{DateOfBirthHeader}\n{SimpleCsv}";

   public const string ItalianCsv = 
       "Giuseppe Mario;Rossi;1,50;31/01/1981\nGiuseppe Luigi;Rossi;1,50;31/01/1981\nWario Quseppe;Bianchi;1,55;31/01/1981\nWaluigi Quseppe;Bianchi;1,80;31/01/1981";
   
   public const string ItalianCsvWithHeaders = 
       $"{NameHeader};{LastNameHeader};{HeightHeader};{DateOfBirthHeader}\n{ItalianCsv}";
}