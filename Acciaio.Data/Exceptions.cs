namespace Acciaio.Data;

public sealed class CsvHeaderException(string msg) : Exception(msg);

public sealed class RowMappingException(string msg) : Exception(msg);