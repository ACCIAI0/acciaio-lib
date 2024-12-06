using System.Reflection;

namespace Acciaio.Data;

public interface ICsvTypeMapper
{
    public bool TryMap(CsvRow row, out object? obj);
}

internal sealed class DefaultCsvTypeMapper : ICsvTypeMapper
{
#region Static
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

    private static void PopulateField(CsvCell cell, object obj, FieldInfo info) 
        => info.SetValue(obj, ExtractValue(cell, info.FieldType));

    private static void PopulateProperty(CsvCell cell, object obj, PropertyInfo info)
        => info.SetValue(obj, ExtractValue(cell, info.PropertyType));
#endregion
    
    private readonly Csv _csv;
    private readonly Type _type;

    internal DefaultCsvTypeMapper(Csv csv, Type type)
    {
        _csv = csv;
        _type = type;
    }

    private Dictionary<int, MemberInfo> BuildMembersMapping()
    {
        Dictionary<int, MemberInfo> memberInfos = new();

        var members = _type
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(MemberIsEditablePropertyOrField)
            .ToList();

        // First pass: NAMES
        if (_csv.HasHeaders)
        {
            var headers = _csv.ColumnHeaders;
            foreach (var header in headers)
            {
                if (string.IsNullOrEmpty(header)) continue;

                for (var i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    if (!member.Name.Equals(header, StringComparison.Ordinal) && !MemberHasHeaderAttribute(member, header)) 
                        continue;
                    
                    memberInfos.Add(_csv[header].Index, member);
                    members.RemoveAt(i--);
                    break;
                }
            }
        }
        
        // Second pass: ORDER
        for (var i = 0; i < _csv.ColumnsCount && members.Count > 0; i++)
        {
            if (memberInfos.ContainsKey(i)) continue;
            memberInfos.Add(i, members[0]);
            members.RemoveAt(0);
        }
        
        return memberInfos;

        static bool MemberIsEditablePropertyOrField(MemberInfo info)
        {
            if ((info.MemberType & (MemberTypes.Field | MemberTypes.Property)) == 0) return false;
            return info is not FieldInfo { IsInitOnly: true } and not PropertyInfo { CanWrite: false };
        }

        static bool MemberHasHeaderAttribute(MemberInfo info, string header)
            => info.GetCustomAttribute<CsvHeaderMapperAttribute>()?.Header.Equals(header, StringComparison.Ordinal) ?? false;
    }
    
    public bool TryMap(CsvRow row, out object? obj)
    {
        var membersMapping = BuildMembersMapping();
        obj = Activator.CreateInstance(_type);

        if (obj is null) return false;
        
        foreach (var index in membersMapping.Keys)
        {
            var cell = row[index];
            var member = membersMapping[index];
            
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    PopulateField(cell, obj, (FieldInfo)member);
                    break;
                case MemberTypes.Property:
                    PopulateProperty(cell, obj, (PropertyInfo)member);
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