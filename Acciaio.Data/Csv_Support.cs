using System.Collections;
using System.Globalization;

namespace Acciaio.Data;
    
public abstract class CsvCell
{
    private readonly IFormatProvider _formatProvider;

    public CsvColumn CsvColumn { get; }
    
    public int ColumnIndex => CsvColumn.Index;
    
    public abstract int RowIndex { get; }

    public bool IsEmpty => string.IsNullOrEmpty(StringValue);
    
    public string StringValue { get; set; }

    public int IntValue 
    {
        get => int.Parse(StringValue, _formatProvider);
        set => StringValue = value.ToString(_formatProvider);
    }
    
    public long LongValue 
    {
        get => long.Parse(StringValue, _formatProvider);
        set => StringValue = value.ToString(_formatProvider);
    }
    
    public float FloatValue 
    {
        get => float.Parse(StringValue, _formatProvider);
        set => StringValue = value.ToString(_formatProvider);
    }
    
    public double DoubleValue 
    {
        get => double.Parse(StringValue, _formatProvider);
        set => StringValue = value.ToString(_formatProvider);
    }

    public DateTime DateTimeValue
    {
        get => DateTime.Parse(StringValue, _formatProvider);
        set => StringValue = value.ToString(_formatProvider);
    }

    protected CsvCell(CsvColumn csvColumn, IFormatProvider formatProvider, string value) 
    {
        CsvColumn = csvColumn;
        _formatProvider = formatProvider;
        StringValue = value;
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

    public void Clear() => StringValue = string.Empty;

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
                
            _header = value;
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