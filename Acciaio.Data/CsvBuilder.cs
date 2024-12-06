using System.Globalization;

namespace Acciaio.Data;

public abstract class CsvBuilder
{
    protected CultureInfo ParsingCulture = CultureInfo.InvariantCulture; 
    protected bool FirstLineIsHeaders = true;
    
    protected string Separator;
    protected string LineBreak;
    protected char EscapeCharacter;

    protected CsvBuilder(string separator, string lineBreak, char escapeCharacter)
    {
        Separator = separator;
        LineBreak = lineBreak;
        EscapeCharacter = escapeCharacter;
    }

    public CsvBuilder UsingParsingCulture(CultureInfo parsingCulture)
    {
        ParsingCulture = parsingCulture ?? throw new ArgumentNullException(nameof(parsingCulture));
        return this;
    }

    public CsvBuilder UsingSeparator(string separator)
    {
        if (string.IsNullOrEmpty(separator)) 
            throw new ArgumentException("Can't use a null or empty separator", nameof(separator));
        
        Separator = separator;
        return this;
    }

    public CsvBuilder UsingLineBreak(string lineBreak)
    {
        if (string.IsNullOrEmpty(lineBreak)) 
            throw new ArgumentException("Can't use a null or empty line break", nameof(lineBreak));
        
        LineBreak = lineBreak;
        return this;
    }

    public CsvBuilder UsingEscapeCharacter(char escapeCharacter)
    {
        EscapeCharacter = escapeCharacter;
        return this;
    }

    public CsvBuilder WithFirstLineAsHeaders(bool firstLineIsHeaders)
    {
        FirstLineIsHeaders = firstLineIsHeaders;
        return this;
    }

    public abstract Csv Empty();

    public abstract Csv Parse(string content);

    public Csv FromFile(string path) 
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (path.Length == 0) throw new ArgumentException("Invalid empty path string");
        return Parse(File.ReadAllText(path));
    }
    
    public Csv FromStream(Stream stream)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        using StreamReader reader = new(stream);
        return Parse(reader.ReadToEnd());
    }
}