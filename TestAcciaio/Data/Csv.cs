using System.Globalization;
using Acciaio.Data;
using Xunit.Abstractions;

namespace Test.Acciaio.Data;

public sealed class CsvTests
{
    private readonly ITestOutputHelper _output;

    public CsvTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void CanCreateEmpty()
    {
        var csv = Csv.Empty();
        Assert.Equal(0, csv.ColumnsCount);
        Assert.Equal(0, csv.RowsCount);
        Assert.Empty(csv);
        Assert.Empty(csv.ColumnHeaders);
        Assert.False(csv.HasHeaders);
    }

    [Fact]
    public void CanAddHeaderlessColumns()
    {
        var csv = Csv.Empty();
        var column0 = csv.CreateColumn();
        var column2 = csv.CreateColumn();
        var column1 = csv.CreateColumn(1);
        
        Assert.Equal(3, csv.ColumnsCount);
        
        Assert.False(csv.HasHeaders);
        Assert.Empty(csv.ColumnHeaders);
        
        Assert.Equal(0, column0.Index);
        Assert.Equal(1, column1.Index);
        Assert.Equal(2, column2.Index);
    }
    
    [Fact]
    public void CanAddHeaderColumns()
    {
        var csv = Csv.Empty();
        var column0 = csv.CreateColumn(CsvTestUtils.NameHeader);
        var column2 = csv.CreateColumn(CsvTestUtils.HeightHeader);
        var column1 = csv.CreateColumn(1);
        
        Assert.Equal(3, csv.ColumnsCount);
        
        Assert.Equal(0, column0.Index);
        Assert.Equal(1, column1.Index);
        Assert.Equal(2, column2.Index);
        
        Assert.True(csv.HasHeaders);
        Assert.Equal(2, csv.ColumnHeaders.Length);
        
        Assert.Equal(CsvTestUtils.NameHeader, column0.Header);
        Assert.Equal(CsvTestUtils.HeightHeader, column2.Header);
    }

    [Fact]
    public void WontAddRowsWithNoColumns()
    {
        var csv = Csv.Empty();
        var row = csv.CreateRow();
        
        Assert.Null(row);
        Assert.Equal(0, csv.RowsCount);
    }

    [Fact]
    public void CanAddRow()
    {
        var csv = Csv.Empty();
        var column0 = csv.CreateColumn(CsvTestUtils.NameHeader);
        var column2 = csv.CreateColumn(CsvTestUtils.HeightHeader);
        var column1 = csv.CreateColumn(1, CsvTestUtils.LastNameHeader);
        var row = csv.CreateRow();
        
        Assert.NotNull(row);

        row[CsvTestUtils.NameHeader].StringValue = "John";
        row[1].StringValue = "Smith";
        row[CsvTestUtils.HeightHeader].FloatValue = 1.82f;
        
        Assert.Equal(CsvTestUtils.NameHeader, column0.Header);
        Assert.Equal(CsvTestUtils.LastNameHeader, column1.Header);
        Assert.Equal(CsvTestUtils.HeightHeader, column2.Header);
        
        Assert.Equal("John", csv[0, CsvTestUtils.NameHeader].StringValue);
        Assert.Equal("Smith", csv[0, 1].StringValue);
        Assert.Equal(1.82f, csv[0, CsvTestUtils.HeightHeader].FloatValue);
    }

    [Fact]
    public void CanParseSimple()
    {
        var csv = Csv.WithFirstLineAsHeaders(false).Parse(CsvTestUtils.SimpleCsv);
        
        Assert.Equal(4, csv.ColumnsCount);
        Assert.False(csv.HasHeaders);
        
        Assert.Equal("Mario", csv[0, 0].StringValue);
        Assert.Equal("Doe", csv[1, 1].StringValue);
        
        Assert.True(csv[2, 2].TryGetFloatValue(out var height));
        Assert.Equal(1.61f, height);
        
        Assert.True(csv[3, 3].TryGetDateTimeValue(out var dateOfBirth));
        Assert.Equal(new DateTime(2000, 10, 22), dateOfBirth);
    }
    
    [Fact]
    public void CanParseWithHeaders()
    {
        var csv = Csv.Parse(CsvTestUtils.CsvWithHeaders);
        
        Assert.Equal(4, csv.ColumnsCount);
        Assert.True(csv.HasHeaders);
        
        Assert.Equal("Mario", csv[0, CsvTestUtils.NameHeader].StringValue);
        Assert.Equal("Doe", csv[1, CsvTestUtils.LastNameHeader].StringValue);
        
        Assert.True(csv[2, CsvTestUtils.HeightHeader].TryGetFloatValue(out var height));
        Assert.Equal(1.61f, height);
        
        Assert.True(csv[3, CsvTestUtils.DateOfBirthHeader].TryGetDateTimeValue(out var dateOfBirth));
        Assert.Equal(new DateTime(2000, 10, 22), dateOfBirth);
    }

    [Fact]
    public void WontParseWithWrongSettings()
    {
        Assert.Throws<ArgumentException>(() => Csv.UsingSeparator(null!).Parse(CsvTestUtils.SimpleCsv));
        Assert.Throws<ArgumentException>(() => Csv.UsingLineBreak(null!).Parse(CsvTestUtils.SimpleCsv));
        Assert.Throws<ArgumentException>(() => Csv.UsingSeparator("").Parse(CsvTestUtils.SimpleCsv));
        Assert.Throws<ArgumentException>(() => Csv.UsingLineBreak("").Parse(CsvTestUtils.SimpleCsv));
        Assert.Throws<ArgumentException>(() => Csv.UsingLineBreak("\n").UsingSeparator("\n").Parse(CsvTestUtils.SimpleCsv));
    }

    [Fact]
    public void CanParseWithSpecificCulture()
    {
        var isWinLineBreak = CsvTestUtils.ItalianCultureCsv.Contains('\r');
        var csv = Csv.UsingParsingCulture(CultureInfo.GetCultureInfo("IT-it"))
                .UsingSeparator(";")
                .UsingLineBreak(isWinLineBreak ? "\r\n" : Csv.DefaultLineBreak)
                .Parse(CsvTestUtils.ItalianCultureCsv);
        
        Assert.Equal(4, csv.ColumnsCount);
        Assert.True(csv.HasHeaders);
        
        _output.WriteLine(CsvTestUtils.ItalianCultureCsv);
        
        Assert.Equal("Giuseppe Mario", csv[0, CsvTestUtils.NameHeader].StringValue);
        Assert.Equal("Rossi", csv[1, CsvTestUtils.LastNameHeader].StringValue);
        
        Assert.True(csv[2, CsvTestUtils.HeightHeader].TryGetFloatValue(out var height));
        Assert.Equal(1.55f, height);
        
        Assert.True(csv[3, CsvTestUtils.DateOfBirthHeader].TryGetDateTimeValue(out var dateOfBirth));
        Assert.Equal(new DateTime(1981, 01, 31), dateOfBirth);
    }

    [Fact]
    public void CanUseDefaultMappingToType()
    {
        var csv = Csv.Parse(CsvTestUtils.CsvWithHeaders);

        var people0 = csv.MapToType<CsvTestUtils.Person>();
        var people1 = csv.MapToType<CsvTestUtils.AristocraticPerson>();
        
        Assert.Equal(4, people0.Length);

        for (var i = 0; i < people0.Length; i++)
        {
            var person = people0[i];
            Assert.Equal(csv[i, CsvTestUtils.NameHeader].StringValue, person.Name);
            Assert.Equal(csv[i, CsvTestUtils.LastNameHeader].StringValue, person.LastName);
            Assert.Equal(csv[i, CsvTestUtils.HeightHeader].FloatValue, person.Height);
            Assert.Equal(csv[i, CsvTestUtils.DateOfBirthHeader].DateTimeValue, person.DateOfBirth);
        }
        
        for (var i = 0; i < people0.Length; i++)
        {
            var person = people1[i];
            Assert.Equal(csv[i, CsvTestUtils.NameHeader].StringValue, person.Appellative);
            Assert.Equal(csv[i, CsvTestUtils.LastNameHeader].StringValue, person.Surname);
            Assert.Equal(csv[i, CsvTestUtils.HeightHeader].FloatValue, person.Height);
            Assert.Equal(csv[i, CsvTestUtils.DateOfBirthHeader].DateTimeValue, person.DateOfBirth);
        }
    }
    
    [Fact]
    public void CanUseCustomMappingToType()
    {
        var csv = Csv.Parse(CsvTestUtils.CsvWithHeaders);
        var people = csv.MapToType<CsvTestUtils.MappablePerson>();
        
        for (var i = 0; i < people.Length; i++)
        {
            var person = people[i];
            Assert.Equal(csv[i, CsvTestUtils.NameHeader].StringValue, person.Name);
            Assert.Equal(csv[i, CsvTestUtils.LastNameHeader].StringValue, person.LastName);
            Assert.Equal(csv[i, CsvTestUtils.HeightHeader].FloatValue, person.Height);
            Assert.Equal((DateTime.Now - csv[i, CsvTestUtils.DateOfBirthHeader].DateTimeValue).Days / 365, person.Age);
        }
    }
}