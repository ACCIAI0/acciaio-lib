namespace Acciaio.Data;
    
[AttributeUsage(AttributeTargets.Class)]
public class CsvTypeMapperAttribute : Attribute
{
    public readonly Type TypeMapperType;

    public CsvTypeMapperAttribute(Type typeMapperType)
    {
        if (!typeof(ICsvTypeMapper).IsAssignableFrom(typeMapperType))
            throw new ArgumentException("TypeMapper Type must implement the ICsvTypeMapper");

        TypeMapperType = typeMapperType;
    }

    public ICsvTypeMapper? InstantiateOrDefault(Csv csv) 
        => Activator.CreateInstance(TypeMapperType) as ICsvTypeMapper;
}

#if NET
[AttributeUsage(AttributeTargets.Class)]
public sealed class CsvTypeMapperAttribute<T> : CsvTypeMapperAttribute
{
    public CsvTypeMapperAttribute() : base(typeof(T)) { }
}
#endif