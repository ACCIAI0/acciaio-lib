using System.Reflection;
using System.Text;

namespace Acciaio.Data;

public static class CsvExtensions
{
    private static ICsvTypeMapper GetMapper<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<CsvTypeMapperAttribute>(true);
        return attribute?.InstantiateOrDefault() ?? new DefaultCsvTypeMapper(typeof(T));    
    }
    
    private static bool TryMap<T>(CsvRow row, ICsvTypeMapper mapper, out T mappedValue)
    {
        mappedValue = default!;
        if (!mapper.TryMap(row, out var element) || element is null) return false;
        mappedValue = (T)element;
        return true;
    }

    public static T Map<T>(this CsvRow row)
    {
        if (!TryMap<T>(row, GetMapper<T>(), out var value))
            throw new RowMappingException($"Unable to map row {row.Index} to type {nameof(T)}");
        return value!;
    }

    public static bool TryMap<T>(this CsvRow row, out T mappedValue) => TryMap(row, GetMapper<T>(), out mappedValue);
    
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
        var csvTypeMapper = GetMapper<T>();
        
        for (var i = startRowIndex; i < csv.RowsCount && count < buffer.Length; i++)
        {
            buffer[count] = TryMap<T>(csv.GetRow(i), csvTypeMapper, out var item) 
                ? item 
                : throw new RowMappingException($"Unable to map row {i} to type {typeof(T)}");;
            count++;
        }
        
        return count;
    }
    
    public static string Dump(this Csv csv)
    {
        var builder = new StringBuilder();
        if (csv.HasHeaders)
        {
            DumpRow(builder, i => csv[i].Header);
            builder.Append(csv.LineBreak);
        }
        
        for (var i = 0; i < csv.RowsCount; i++)
        {
            var iClosure = i;
            DumpRow(builder, j => csv[iClosure, j].StringValue);
            
            if (i < csv.RowsCount - 1) builder.Append(csv.LineBreak);
        }

        return builder.ToString();

        void DumpRow(StringBuilder strBuilder, Func<int, string> getter)
        {
            for (var i = 0; i < csv.ColumnsCount; i++)
            {
                var value = getter(i);
                var mustBeEscaped = value.Contains(csv.Separator) || value.Contains(csv.LineBreak);
                
                if (mustBeEscaped) strBuilder.Append('\"');
                strBuilder.Append(value);
                if (mustBeEscaped) strBuilder.Append('\"');
                
                if (i < csv.ColumnsCount - 1) strBuilder.Append(csv.Separator);
            }
        }
    }

    public static void DumpToFile(this Csv csv, string path) => File.WriteAllText(path, Dump(csv));
}