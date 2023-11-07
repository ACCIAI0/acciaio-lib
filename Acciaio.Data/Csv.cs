namespace Acciaio.Data;

using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Text;

public sealed partial class Csv : IEnumerable<CsvColumn>
{
    public const string DefaultSeparator = ",";
    public const string DefaultLineBreak = "\n";
    public const char DefaultEscapeCharacter = '\"';
    
#region Static
    private static void ThrowHeaderException() => throw new InvalidOperationException("Can't access columns by headers");

    public static Builder UsingParsingCulture(CultureInfo parsingCulture) => 
        new ConcreteBuilder().UsingParsingCulture(parsingCulture);

    public static Builder UsingSeparator(string separator) => 
        new ConcreteBuilder().UsingSeparator(separator);

    public static Builder UsingLineBreak(string lineBreak) => 
        new ConcreteBuilder().UsingLineBreak(lineBreak);
    
    public static Builder UsingEscapeCharacter(char escapeCharacter) => 
        new ConcreteBuilder().UsingEscapeCharacter(escapeCharacter);

    public static Builder WithFirstLineAsHeaders(bool firstLineIsHeaders) => 
        new ConcreteBuilder().WithFirstLineAsHeaders(firstLineIsHeaders);

    public static Csv Empty() =>
        new ConcreteBuilder().Empty();

    public static Csv Parse(string content) => 
        new ConcreteBuilder().Parse(content);

    public static Csv FromFile(string path) =>
        new ConcreteBuilder().FromFile(path);

    public static Csv FromStream(Stream stream) =>
        new ConcreteBuilder().FromStream(stream);
#endregion
    
#region Fields
    private readonly List<Column> _columns;
#endregion

#region Properties & Indexers
    public CultureInfo ParsingCulture { get; } 
    
    public string Separator { get; }
    
    public string LineBreak { get; }
    
    public char EscapeCharacter { get; }
    
    public string[] ColumnHeaders => _columns.Select(c => c.Header).Where(h => !string.IsNullOrEmpty(h)).ToArray();
    
    public bool HasHeaders => _columns.Any(c => !string.IsNullOrEmpty(c.Header));
    
    public int ColumnsCount => _columns.Count;
    
    public int RowsCount => ColumnsCount == 0 ? 0 : _columns[0].Count;
    
    public int CellsCount => _columns.Sum(c => c.Count);

    public CsvColumn this[int index] 
    {
        get
        {
            if (index < 0 || index >= ColumnsCount)
                throw new IndexOutOfRangeException("Invalid column index");
            return _columns[index];
        }
    }
    
    public CsvColumn this[string header]
    {
        get
        {
            if (!HasHeaders) ThrowHeaderException();
            
            if (string.IsNullOrEmpty(header)) throw new ArgumentException(header, nameof(header));

            var column = _columns.Find(c => c.Header.Equals(header, StringComparison.Ordinal));
            if (column == null) throw new ArgumentException($"Unknown column with header {header}");
            
            return column;
        }
    }

    public CsvCell this[int row, int column] => this[column][row];
    
    public CsvCell this[int row, string header] => this[header][row];
#endregion

    private Csv(string csvContent, CultureInfo parsingCulture, string separator, 
        string lineBreak, char escapeCharacter, bool firstLineIsHeaders)
    {
        if (separator == lineBreak) throw new ArgumentException("Cannot set separator and endOfLine to the same value");
        if (separator.Contains(escapeCharacter)) throw new ArgumentException("separator cannot contain escaping character");
        if (lineBreak.Contains(escapeCharacter)) throw new ArgumentException("endOfLine cannot contain escaping character");

        ParsingCulture = parsingCulture;
        Separator = separator;
        LineBreak = lineBreak;
        EscapeCharacter = escapeCharacter;

        _columns = new List<Column>();

        Parse(csvContent, firstLineIsHeaders);
    }

    private void Parse(string content, bool firstLineIsHeaders)
    {
        var builder = new StringBuilder();
        var rowIndex = 0;
        var columnIndex = 0;
        var isEscaping = false;
        
        // For each character in content
        for (var i = 0; i < content.Length; i++)
        {
            var element = content[i];
            
            if (element == EscapeCharacter)
            {
                if (content[i + 1] == EscapeCharacter)
                    builder.Append(content[++i]);
                else isEscaping = !isEscaping;
                continue;
            }

            // Append the character
            builder.Append(element);

            var separatorStart = builder.Length - Separator.Length;
            var endOfLineStart = builder.Length - LineBreak.Length;
            
            var isSeparator = !isEscaping && builder.Length >= Separator.Length && 
                              builder.ToString(separatorStart, Separator.Length).Equals(Separator, StringComparison.Ordinal);
            var isEndOfLine = !isEscaping && builder.Length >= LineBreak.Length &&
                              builder.ToString(endOfLineStart, LineBreak.Length).Equals(LineBreak, StringComparison.Ordinal);

            // If the last characters added are not a separator and are not an EOL 
            // and are not the le last characters in the content continue iterating,
            // else remove them from the builder and create a new Cell element for its content
            if (!isSeparator && !isEndOfLine && i != content.Length - 1) continue;

            if (isSeparator) builder.Remove(separatorStart, Separator.Length);
            if (isEndOfLine) builder.Remove(endOfLineStart, LineBreak.Length);

            if (rowIndex == 0)
            {
                var header = firstLineIsHeaders ? builder.ToString() : string.Empty;
                var column = new Column(this, ColumnsCount, header);
                if (!firstLineIsHeaders) column.Add(builder.ToString());
                _columns.Add(column);
            } 
            else _columns[columnIndex].Add(builder.ToString());

            if (isSeparator) columnIndex++;
            if (isEndOfLine) 
            {
                columnIndex = 0;
                rowIndex++;
            }

            builder.Clear();
        }

        var rowsCountMax = ColumnsCount == 0 ? 0 : _columns.Max(c => c.Count);
        foreach (var column in _columns)
        {
            for (var i = column.Count; i < rowsCountMax; i++)
                column.Add(string.Empty);
        }
    }

    public bool HasHeader(string header) => HasHeaders && _columns.Any(c => c.Header.Equals(header, StringComparison.Ordinal));

    public CsvColumn CreateColumn() => CreateColumn(ColumnsCount, string.Empty);
    
    public CsvColumn CreateColumn(int index) => CreateColumn(index, string.Empty);
    
    public CsvColumn CreateColumn(string header) => CreateColumn(ColumnsCount, header);
    
    public CsvColumn CreateColumn(int index, string? header)
    {
        if (index < 0 || index > ColumnsCount) 
            throw new IndexOutOfRangeException("index must be positive and lower than or equal to ColumnsCount");

        var column = new Column(this, index, header ?? string.Empty);
        
        for (var i = 0; i < RowsCount; i++)
            column.Add(string.Empty);
        
        _columns.Insert(index, column);
        
        if (index < ColumnsCount - 1)
        {
            for(var i = index + 1; i < ColumnsCount; i++)
                _columns[i].SetIndex(_columns[i].Index + 1);
        }
        
        return column;
    }

    public void RemoveColumn(int index)
    {
        if (index < 0 || index >= _columns.Count)
            throw new IndexOutOfRangeException("Invalid column index");
        
        _columns.RemoveAt(index);
        for (var i = index; i < ColumnsCount; i++)
            _columns[i].SetIndex(_columns[i].Index - 1);
    }

    public void RemoveColumn(string header) => RemoveColumn(this[header].Index);

    public CsvRow? CreateRow() => CreateRow(RowsCount);

    public CsvRow? CreateRow(int index)
    {
        if (index < 0 || index > RowsCount) 
            throw new IndexOutOfRangeException("index must be positive and lower than or equal to RowsCount");

        if (ColumnsCount == 0) return null;
        
        foreach(var column in _columns) column.Insert(index, string.Empty);
        return new Row(this, index);
    }

    public CsvRow GetRow(int index)
    {
        if (index < 0 || index >= RowsCount) 
            throw new IndexOutOfRangeException("index must be positive and lower than RowsCount");
        
        return new Row(this, index);
    }

    public List<CsvRow> GetRows()
    {
        List<CsvRow> list = new();
        
        for (var i = 0; i < RowsCount; i++)
            list.Add(new Row(this, i));
        
        return list;
    }

    public void RemoveRow(int index) => _columns.ForEach(c => c.RemoveAtRow(index));

    public void Clear(bool keepHeaders)
    {
        if (!keepHeaders) _columns.Clear();
        else _columns.ForEach(c => c.Clear());
    }

    public string Dump()
    {
        var builder = new StringBuilder();
        if (HasHeaders) DumpRow(builder, i => _columns[i].Header);
        for (var i = 0; i < RowsCount; i++)
        {
            var capturedI = i;
            DumpRow(builder, j => this[j, capturedI].StringValue);
        }

        return builder.ToString();

        void DumpRow(StringBuilder strBuilder, Func<int, string> getter)
        {
            for (var i = 0; i < ColumnsCount; i++)
            {
                var value = getter(i);
                var mustBeEscaped = value.Contains(Separator) || value.Contains(LineBreak);
                
                if (mustBeEscaped) strBuilder.Append('\"');
                strBuilder.Append(value);
                if (mustBeEscaped) strBuilder.Append('\"');
                
                if (i < ColumnsCount - 1) strBuilder.Append(Separator);
            }
            strBuilder.Append(LineBreak);
        }
    }

    public void DumpToFile(string path) => File.WriteAllText(path, Dump());

    public T[] MapToType<T>()
    {
        var result = new T[RowsCount];
        MapToType(result);
        return result;
    }

    public int MapToType<T>(T[] buffer, int startRowIndex = 0)
    {
        var count = 0;
        var attribute = typeof(T).GetCustomAttribute(typeof(CsvTypeMapperAttribute), true);
        
        ICsvTypeMapper? csvTypeMapper = null;
        if (attribute is CsvTypeMapperAttribute mapperAttribute)
            csvTypeMapper = mapperAttribute.InstantiateOrDefault(this);
        csvTypeMapper ??= new DefaultCsvTypeMapper(this, typeof(T));
        
        for (var i = startRowIndex; i < RowsCount && count < buffer.Length; i++)
        {
            if (!csvTypeMapper.TryMap(GetRow(i), out var element) || element == null) continue;
            
            buffer[count] = (T)element;
            count++;
        }
        
        return count;
    }
    
    public override string ToString() => $"CSV({RowsCount}x{ColumnsCount})";

    public IEnumerator<CsvColumn> GetEnumerator() => _columns.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}