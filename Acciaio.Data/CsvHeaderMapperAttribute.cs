namespace Acciaio.Data;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class CsvHeaderMapperAttribute : Attribute
{
    public readonly string Header;

    public CsvHeaderMapperAttribute(string header) => Header = header;
}