using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Acciaio.Data;

public interface ICsvTypeMapper
{
    internal static ICsvTypeMapper CreateMapper<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<CsvTypeMapperAttribute>(true);
        return attribute?.InstantiateOrDefault() ?? new DefaultCsvTypeMapper(typeof(T));    
    }
    
    public bool TryMap(ICsvRow row, [MaybeNullWhen(false)] out object obj);
}

internal sealed class DefaultCsvTypeMapper : ICsvTypeMapper
{
    private static object? ExtractValue(CsvCell cell, Type type)
    {
        object? value = null;
        if (type == typeof(string)) value = cell.StringValue;
        else if (type == typeof(int)) value = cell.IntValue;
        else if (type == typeof(long)) value = cell.LongValue; 
        else if (type == typeof(float)) value = cell.FloatValue; 
        else if (type == typeof(double)) value = cell.DoubleValue; 
        else if (type == typeof(DateTime)) value = cell.DateTimeValue; 
        else if (typeof(Enum).IsAssignableFrom(type)) value = cell.TryGetEnumValue(type, out value);

        return value;
    }
    
    private readonly Type _type;
    private readonly List<MemberInfo> _filteredMembers;
    private readonly Dictionary<string, MemberInfo> _filteredMembersByAttribute;
    
    internal DefaultCsvTypeMapper(Type type)
    {
        _type = type;
        _filteredMembers = _type
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(MemberIsEditableFieldOrProperty)
            .Where(MemberIsNotIgnored)
            .ToList();
        _filteredMembersByAttribute = _filteredMembers
            .Select(m => (Member: m, Attribute: m.GetCustomAttribute<CsvHeaderMapperAttribute>()))
            .Where(t => t.Attribute is not null && !string.IsNullOrWhiteSpace(t.Attribute.Header))
            .ToDictionary(t => t.Attribute!.Header, t => t.Member);
        return;
        
        static bool MemberIsEditableFieldOrProperty(MemberInfo info) 
            => info is FieldInfo { IsInitOnly: false } or PropertyInfo { CanWrite: true };

        static bool MemberIsNotIgnored(MemberInfo info) => info.GetCustomAttribute<CsvIgnore>() is null;
    }

    private Dictionary<int, MemberInfo> BuildMembersMapping(Csv csv)
    {
        Dictionary<int, MemberInfo> memberInfos = new();

        // FIRST PASS - Names/Headers
        for (var i = 0; i < csv.ColumnsCount; i++)
        {
            var header = csv.GetColumn(i).Header;

            if (string.IsNullOrEmpty(header)) continue;

            if (!_filteredMembersByAttribute.TryGetValue(header, out var info) && !TryGetMemberWithName(header, out info)) 
                continue;
            
            memberInfos.Add(i, info);
        }
        
        // SECOND PASS - Definition Order
        for (var i = 0; i < csv.ColumnsCount; i++)
        {
            if (memberInfos.ContainsKey(i)) continue;

            var info = _filteredMembers.Find(m => !memberInfos.ContainsValue(m));

            if (info is null) break;
            
            memberInfos.Add(i, info);
        }

        return memberInfos;
        
        bool TryGetMemberWithName(string name, [MaybeNullWhen(false)] out MemberInfo info)
        {
            info = _filteredMembers.Find(m => m.Name.Equals(name, StringComparison.Ordinal));
            return info is not null;
        }
    }
    
    public bool TryMap(ICsvRow row, [MaybeNullWhen(false)] out object obj)
    {
        var membersMapping = BuildMembersMapping(row.Csv);
        obj = Activator.CreateInstance(_type);

        if (obj is null) return false;
        
        foreach (var (index, member) in membersMapping)
        {
            var cell = row[index];

            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    var field = (FieldInfo)member;
                    field.SetValue(obj, ExtractValue(cell, field.FieldType));
                    break;
                case MemberTypes.Property:
                    var property = (PropertyInfo)member;
                    property.SetValue(obj, ExtractValue(cell, property.PropertyType));
                    break;
                case MemberTypes.Constructor:
                case MemberTypes.Event:
                case MemberTypes.Method:
                case MemberTypes.TypeInfo:
                case MemberTypes.Custom:
                case MemberTypes.NestedType:
                case MemberTypes.All:
                default:
                    break;
            }
        }

        return true;
    }
}