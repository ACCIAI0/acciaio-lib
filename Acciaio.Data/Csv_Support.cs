using System.Collections;
using System.Globalization;

namespace Acciaio.Data;
    
public abstract class CsvCell
{
    private readonly IFormatProvider _formatProvider;
    
    private string _value;

    public CsvColumn CsvColumn { get; }
    
    public int ColumnIndex => CsvColumn.Index;
    
    public abstract int RowIndex { get; }

    public bool IsEmpty => string.IsNullOrEmpty(_value);
    
    public string StringValue 
    {
        get => _value;
        set => _value = value ?? string.Empty;
    }
    
    public int IntValue 
    {
        get => int.Parse(_value, _formatProvider);
        set => _value = value.ToString(_formatProvider);
    }
    
    public long LongValue 
    {
        get => long.Parse(_value, _formatProvider);
        set => _value = value.ToString(_formatProvider);
    }
    
    public float FloatValue 
    {
        get => float.Parse(_value, _formatProvider);
        set => _value = value.ToString(_formatProvider);
    }
    
    public double DoubleValue 
    {
        get => double.Parse(_value, _formatProvider);
        set => _value = value.ToString(_formatProvider);
    }

    public DateTime DateTimeValue
    {
        get => DateTime.Parse(StringValue, _formatProvider);
        set => _value = value.ToString(_formatProvider);
    }

    protected CsvCell(CsvColumn csvColumn, IFormatProvider formatProvider, string value) 
    {
        CsvColumn = csvColumn;
        _formatProvider = formatProvider;
        _value = value;
    }

    public bool TryGetIntValue(out int value) 
        => int.TryParse(StringValue, NumberStyles.Integer, _formatProvider, out value);
    
    public bool TryGetLongValue(out long value) 
        => long.TryParse(StringValue, NumberStyles.Integer, _formatProvider, out value);
    
    public bool TryGetFloatValue(out float value) 
        => float.TryParse(StringValue, NumberStyles.Float, _formatProvider, out value);
    
    public bool TryGetDoubleValue(out double value) 
        => double.TryParse(StringValue, NumberStyles.Float, _formatProvider, out value);

    public bool TryGetDateTimeValue(out DateTime value)
        => DateTime.TryParse(StringValue, _formatProvider, DateTimeStyles.None, out value);
    
    public bool TryGetEnumValue(Type type, out object? value, bool ignoreCase = true)
    {
        var result = Enum.TryParse(type, StringValue, ignoreCase, out var objValue);
        value = result ? objValue : Enum.GetValues(type).GetValue(0);
        return result;
    }

    public bool TryGetEnumValue<T>(out T value, bool ignoreCase = true) where T : Enum
    {
        var result = TryGetEnumValue(typeof(T), out var obj, ignoreCase);
        value = (T)obj!;
        return result;
    }

    public void Clear() => _value = string.Empty;

    public override string ToString() => $"Cell('{StringValue}', {RowIndex}, {ColumnIndex})";
}

public abstract class IndexedCellsCollection : IEnumerable<CsvCell> 
{
    public CsvCell this[int index]
    {
        get
        {
            if (index < 0 || index >= Count) 
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be non negative and less than Count");

            return GetCellAt(index);
        }
    }
        
    public abstract int Index { get; }
        
    public abstract int Count { get; }

    protected abstract CsvCell GetCellAt(int safeIndex);

    public abstract bool Contains(CsvCell csvCell);

    public override string ToString() => $"CellsCollection({Index}, {Count})";

    public abstract IEnumerator<CsvCell> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public abstract class CsvRow : IndexedCellsCollection
{
    private readonly Csv _csv;

    public override int Index { get; }

    public override int Count => _csv.ColumnsCount;

    public CsvCell this[string header] => _csv[Index, header];

    protected CsvRow(Csv csv, int index)
    {
        _csv = csv;
        Index = index;
    }

    protected override CsvCell GetCellAt(int safeIndex) => _csv[Index, safeIndex];

    public bool HasHeader(string header) => _csv.HasHeader(header);

    public override IEnumerator<CsvCell> GetEnumerator()
    {
        foreach (var column in _csv)
            yield return column[Index];
    }

    public override bool Contains(CsvCell csvCell) => csvCell.RowIndex == Index;

    public override string ToString() => $"Row({Index}, {Count})";
}

public abstract class CsvColumn : IndexedCellsCollection
{
    private string _header = string.Empty;

    protected int InternalIndex;
        
    protected readonly Csv Csv;

    public override int Index => InternalIndex;

    public string Header 
    { 
        get => _header; 
        set 
        {
            if (!string.IsNullOrEmpty(value) && Csv.Any(c => c != this && c.Header == value))
                throw new ArgumentException($"Another column called {value} already exists");
                
            _header = value ?? string.Empty;
        }
    }

    protected CsvColumn(Csv csv, int internalIndex, string header)
    {
        Csv = csv;
        InternalIndex = internalIndex;
        Header = header;
    }

    public override bool Contains(CsvCell csvCell) => csvCell.ColumnIndex == InternalIndex;

    public override string ToString() => $"Column({Index}, '{Header}', {Count})";
}

public sealed partial class Csv
{
#region Builder
    public abstract class Builder
    {
        private CultureInfo _parsingCulture = CultureInfo.InvariantCulture; 
        private string _separator = DefaultSeparator;
        private string _lineBreak = DefaultLineBreak;
        private char _escapeCharacter = DefaultEscapeCharacter;
        private bool _firstLineIsHeaders = true;

        public Builder UsingParsingCulture(CultureInfo parsingCulture)
        {
            _parsingCulture = parsingCulture ?? throw new ArgumentNullException(nameof(parsingCulture));
            return this;
        }

        public Builder UsingSeparator(string separator)
        {
            if (string.IsNullOrEmpty(separator)) 
                throw new ArgumentException("Can't use a null or empty separator", nameof(separator));
            
            _separator = separator;
            return this;
        }

        public Builder UsingLineBreak(string lineBreak)
        {
            if (string.IsNullOrEmpty(lineBreak)) 
                throw new ArgumentException("Can't use a null or empty line break", nameof(lineBreak));
            
            _lineBreak = lineBreak;
            return this;
        }

        public Builder UsingEscapeCharacter(char escapeCharacter)
        {
            _escapeCharacter = escapeCharacter;
            return this;
        }

        public Builder WithFirstLineAsHeaders(bool firstLineIsHeaders)
        {
            _firstLineIsHeaders = firstLineIsHeaders;
            return this;
        }

        public Csv Empty() =>
            new(string.Empty, _parsingCulture, _separator, _lineBreak, _escapeCharacter, false);

        public Csv Parse(string content) => 
            new(content, _parsingCulture, _separator, _lineBreak, _escapeCharacter, _firstLineIsHeaders);

        public Csv FromFile(string path) 
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path.Length == 0) throw new ArgumentException("Invalid empty path string");
            return Parse(File.ReadAllText(path));
        }
        
        public Csv FromStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using StreamReader reader = new(stream);
            return Parse(reader.ReadToEnd());
        }
    }

    private sealed class ConcreteBuilder : Builder { }
#endregion

#region Cell
    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
    private sealed class Cell : CsvCell
    { 
        private int _row;

        public override int RowIndex => _row;

        public Cell(CsvColumn csvColumn, IFormatProvider formatProvider, string value, int row) : 
            base(csvColumn, formatProvider, value) => _row = row;

        public void SetRow(int row) => _row = row;
    }
#endregion

#region Row & Column

    private sealed class Row : CsvRow
    {
        public Row(Csv csv, int index) : base(csv, index) { }
    }

    private sealed class Column : CsvColumn
    {
        private readonly List<Cell> _cells = new();

        public override int Count => _cells.Count;

        public Column(Csv csv, int internalIndex, string header) : 
            base(csv, internalIndex, header) { }

        protected override CsvCell GetCellAt(int safeIndex) => _cells[safeIndex];

        public void SetIndex(int index) => InternalIndex = index;

        public void Add(string value) => _cells.Add(new Cell(this, Csv.ParsingCulture, value, Count));

        public void Insert(int index, string value)
        {
            _cells.Insert(index, new Cell(this, Csv.ParsingCulture, value, index));
            
            for (var i = index + 1; i < Count; i++)
                _cells[i].SetRow(_cells[i].RowIndex + 1);
        }

        public void RemoveAtRow(int rowIndex)
        {
            _cells.RemoveAt(rowIndex);
            
            for (var i = rowIndex; i < Count; i++)
                _cells[i].SetRow(_cells[i].RowIndex - 1);
        }

        public void Clear() => _cells.Clear();

        public override IEnumerator<CsvCell> GetEnumerator() => _cells.GetEnumerator();
    }
#endregion
}