using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Acciaio.Data;

public abstract class CsvCell
{
    protected abstract CultureInfo ParsingCulture { get; }
    
    public abstract bool IsOrphan { get; }
    
    public abstract ICsvRow Row { get; }
    
    public abstract ICsvColumn Column { get; }
    
    public (int Row, int Column) Indices
    {
        get
        {
            if (IsOrphan)
                throw new CsvOrphanException("Can't get indices of an orphan cell");
            return (Row.Index, Column.Index);
        }
    }

    public string StringValue { get; set; } = string.Empty;

    public int IntValue 
    {
        get => int.Parse(StringValue, ParsingCulture);
        set => StringValue = value.ToString(ParsingCulture);
    }
        
    public long LongValue 
    {
        get => long.Parse(StringValue, ParsingCulture);
        set => StringValue = value.ToString(ParsingCulture);
    }
        
    public float FloatValue 
    {
        get => float.Parse(StringValue, ParsingCulture);
        set => StringValue = value.ToString(ParsingCulture);
    }
        
    public double DoubleValue 
    {
        get => double.Parse(StringValue, ParsingCulture);
        set => StringValue = value.ToString(ParsingCulture);
    }

    public DateTime DateTimeValue
    {
        get => DateTime.Parse(StringValue, ParsingCulture);
        set => StringValue = value.ToString(ParsingCulture);
    }
    
    public bool IsEmpty => string.IsNullOrEmpty(StringValue);

    public bool TryGetIntValue(out int value) 
        => int.TryParse(StringValue, NumberStyles.Integer, ParsingCulture, out value);
        
    public bool TryGetLongValue(out long value) 
        => long.TryParse(StringValue, NumberStyles.Integer, ParsingCulture, out value);
        
    public bool TryGetFloatValue(out float value) 
        => float.TryParse(StringValue, NumberStyles.Float, ParsingCulture, out value);
        
    public bool TryGetDoubleValue(out double value) 
        => double.TryParse(StringValue, NumberStyles.Float, ParsingCulture, out value);
        
    public bool TryGetDateTimeValue(DateTimeStyles style, out DateTime value)
        => DateTime.TryParse(StringValue, ParsingCulture, style, out value);

    public bool TryGetDateTimeValue(out DateTime value) => TryGetDateTimeValue(DateTimeStyles.None, out value);
    
    public bool TryGetEnumValue(Type type, [MaybeNullWhen(false)] out object value, bool ignoreCase = true) 
        => Enum.TryParse(type, StringValue, ignoreCase, out value);

    public bool TryGetEnumValue<T>(out T value, bool ignoreCase = true) where T : struct, Enum 
        => Enum.TryParse(StringValue, ignoreCase, out value);

    public void Clear() => StringValue = string.Empty;

    public void CopyInto(CsvCell cell) => cell.StringValue = StringValue;

    public override string ToString() => $"Cell['{StringValue}', ({Row.Index}, {Column.Index}))";
}