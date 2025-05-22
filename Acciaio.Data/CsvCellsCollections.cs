using System.Collections;

namespace Acciaio.Data;

public interface IIndexedCsvCellsCollection : IEnumerable<CsvCell>
{
    public Csv Csv { get; }
    
    public bool IsOrphan { get; }
    
    public int Index { get; set; }
    
    public int Count { get; }
    
    public CsvCell this[int index] { get; }
    
    public bool Contains(CsvCell cell);
    
    public void Clear();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public interface ICsvRow : IIndexedCsvCellsCollection { public CsvCell this[string header] { get; } }

public interface ICsvColumn : IIndexedCsvCellsCollection { public string Header { get; set; } }