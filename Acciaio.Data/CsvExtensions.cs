using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Acciaio.Data;

public static class CsvExtensions
{
    private static bool TryMap<T>(ICsvRow row, ICsvTypeMapper mapper, [MaybeNullWhen(false)] out T mappedValue)
    {
        mappedValue = default;
        if (!mapper.TryMap(row, out var element)) return false;
        mappedValue = (T)element;
        return true;
    }

    public static T Map<T>(this ICsvRow row)
    {
        if (!TryMap<T>(row, ICsvTypeMapper.CreateMapper<T>(), out var value))
            throw new RowMappingException($"Unable to map row {row.Index} to type {nameof(T)}");
        return value!;
    }

    public static bool TryMap<T>(this ICsvRow row, [MaybeNullWhen(false)] out T mappedValue) 
        => TryMap(row, ICsvTypeMapper.CreateMapper<T>(), out mappedValue);
    
    public static T[] MapToType<T>(this Csv csv)
    {
        var result = new T[csv.RowsCount];
        MapToType(csv, result);
        return result;
    }

    public static int MapToType<T>(this Csv csv, T[] buffer, int startRowIndex = 0)
        => MapToType(csv, buffer.AsSpan(), startRowIndex);
    
    public static int MapToType<T>(this Csv csv, Span<T> buffer, int startRowIndex = 0)
    {
        var count = 0;
        var csvTypeMapper = ICsvTypeMapper.CreateMapper<T>();
        
        for (var i = startRowIndex; i < csv.RowsCount && count < buffer.Length; i++)
        {
            buffer[count] = TryMap<T>(csv.GetRow(i), csvTypeMapper, out var item) 
                ? item 
                : throw new RowMappingException($"Unable to map row {i} to type {typeof(T)}");
            count++;
        }
        
        return count;
    }
    
    public static string Dump(this Csv csv, 
        string separator = Csv.DefaultSeparator, 
        string lineBreak = Csv.DefaultLineBreak)
    {
        var builder = new StringBuilder();
        if (csv.HasHeaders)
        {
            DumpRow(builder, i => csv.GetColumn(i).Header);
            builder.Append(lineBreak);
        }
        
        for (var i = 0; i < csv.RowsCount; i++)
        {
            var iClosure = i;
            DumpRow(builder, j => csv[iClosure, j].StringValue);
            
            if (i < csv.RowsCount - 1) builder.Append(lineBreak);
        }

        return builder.ToString();

        void DumpRow(StringBuilder strBuilder, Func<int, string> getter)
        {
            for (var i = 0; i < csv.ColumnsCount; i++)
            {
                var value = getter(i);
                var mustBeEscaped = value.Contains(separator) || value.Contains(lineBreak);
                
                if (mustBeEscaped) strBuilder.Append('\"');
                strBuilder.Append(value);
                if (mustBeEscaped) strBuilder.Append('\"');
                
                if (i < csv.ColumnsCount - 1) strBuilder.Append(separator);
            }
        }
    }

    public static void DumpToFile(this Csv csv, string path) => File.WriteAllText(path, Dump(csv));
}