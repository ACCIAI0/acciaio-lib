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
        if (string.IsNullOrEmpty(separator)) 
            throw new ArgumentException("Can't use a null or empty separator", nameof(separator));
        if (string.IsNullOrEmpty(lineBreak)) 
            throw new ArgumentException("Can't use a null or empty line break", nameof(lineBreak));
        
        if (separator.Contains(lineBreak) || lineBreak.Contains(separator)) 
            throw new ArgumentException("separator and lineBreak cannot share characters");
        if (separator.Contains(escapeCharacter)) 
            throw new ArgumentException("separator cannot contain the escaping character");
        if (lineBreak.Contains(escapeCharacter)) 
            throw new ArgumentException("lineBreak cannot contain the escaping character");
        
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
        if (separator.Contains(LineBreak) || LineBreak.Contains(separator)) 
            throw new ArgumentException("separator and endOfLine cannot share characters");
        if (separator.Contains(EscapeCharacter)) 
            throw new ArgumentException("separator cannot contain the escaping character");
        
        Separator = separator;
        return this;
    }

    public CsvBuilder UsingLineBreak(string lineBreak)
    {
        if (string.IsNullOrEmpty(lineBreak)) 
            throw new ArgumentException("Can't use a null or empty line break", nameof(lineBreak));
        if (Separator.Contains(lineBreak) || lineBreak.Contains(Separator)) 
            throw new ArgumentException("separator and endOfLine cannot share characters");
        if (lineBreak.Contains(EscapeCharacter)) 
            throw new ArgumentException("laneBreak cannot contain the escaping character");
        
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

    public abstract Csv Parse(string content);

    public Csv ParseFile(string path) 
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        if (path.Length == 0) throw new ArgumentException("Invalid empty path string");
        return Parse(File.ReadAllText(path));
    }
    
    public Csv ParseStream(Stream stream)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        using StreamReader reader = new(stream);
        return Parse(reader.ReadToEnd());
    }
}