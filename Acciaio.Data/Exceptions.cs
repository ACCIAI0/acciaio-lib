namespace Acciaio.Data;

public class CsvOrphanException(string message) : Exception(message);

public sealed class CsvHeaderException(string msg) : Exception(msg);

public sealed class RowMappingException(string msg) : Exception(msg);