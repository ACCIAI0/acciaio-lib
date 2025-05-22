using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Acciaio.Data;

public sealed class Csv : IEnumerable<ICsvRow>
{
#region Nested Types
    private sealed class Builder() : CsvBuilder(DefaultSeparator, DefaultLineBreak, DefaultEscapeCharacter)
    {
        // ReSharper disable MemberHidesStaticFromOuterClass
        public override Csv Parse(string content) => 
            new(content, ParsingCulture, Separator, LineBreak, EscapeCharacter, FirstLineIsHeaders);
        // ReSharper restore MemberHidesStaticFromOuterClass
    }

    private sealed class Cell : CsvCell
    {
        public bool InternalIsOrphan { private get; set; }

        private readonly CsvRow _row;

        private readonly CsvColumn _column;

        protected override CultureInfo ParsingCulture => Row.Csv.ParsingCulture;

        public override bool IsOrphan => InternalIsOrphan || _row.IsOrphan || _column.IsOrphan;
        
        public override ICsvRow Row => !IsOrphan ? _row : throw new CsvOrphanException("Orphan cell isn't part of any row");

        public override ICsvColumn Column => !IsOrphan ? _column : throw new CsvOrphanException("Orphan cell isn't part of any column");

        public Cell(CsvRow row, CsvColumn column)
        {
            _row = row;
            _column = column;
        }
    }

    private sealed class CsvRow : ICsvRow
    {
        private readonly List<Cell> _cells = [];

        public Csv Csv { get; }
        
        public bool IsOrphan { get; set; }
        
        public int Index 
        { 
            get => Csv.IndexOf(this);
            set => Csv.SetIndexOf(this, value);
        }

        public int Count => _cells.Count;

        public CsvCell this[int index] => _cells[index];

        public CsvCell this[string header]
        {
            get
            {
                if (IsOrphan) throw new CsvOrphanException("Can't access any cell of an orphan row by header");
                
                var index = Csv._columns.FindIndex(c => c.Header.Equals(header, StringComparison.Ordinal));
                if (index < 0) throw new ArgumentException($"Unknown column '{header}'");

                return this[index];
            }
        }

        public CsvRow(Csv csv)
        {
            Csv = csv;
            while (_cells.Count < Csv.ColumnsCount)
                CreateCell(_cells.Count);
        }

        public void MoveCell(int oldIndex, int newIndex)
        {
            var cell = _cells[oldIndex];
            _cells.RemoveAt(oldIndex);
            if (newIndex == _cells.Count) _cells.Add(cell);
            else _cells.Insert(newIndex, cell);
        }

        public void RemoveCell(int index)
        {
            _cells[index].InternalIsOrphan = true;
            _cells.RemoveAt(index);
        }
        
        public bool Contains(CsvCell cell) => _cells.Contains(cell);

        public void CreateCell(int index)
        {
            if (IsOrphan) throw new CsvOrphanException("Can't create any new cell in an orphan row");
            
            var cell = new Cell(this, Csv._columns[index]);
            while (index > _cells.Count)
                _cells.Add(cell);
            if (index == _cells.Count) _cells.Add(cell);
            else _cells.Insert(index, cell);
        }

        public void Clear() => _cells.ForEach(c => c.StringValue = string.Empty);
        
        public IEnumerator<CsvCell> GetEnumerator() => _cells.GetEnumerator();
    }
    
    private sealed class CsvColumn : ICsvColumn
    {
        public Csv Csv { get; }
        
        public bool IsOrphan { get; set; }
        
        public int Index
        {
            get => Csv.IndexOf(this);
            set => Csv.SetIndexOf(this, value);
        }

        public int Count => Csv.RowsCount;

        public string Header { get; set; } = string.Empty;

        public CsvCell this[int index] 
            => !IsOrphan ? Csv._rows[index][Index] : throw new CsvOrphanException("Can't access any cell of an orphan column");

        public CsvColumn(Csv csv) => Csv = csv;
        
        public bool Contains(CsvCell cell) => Csv._rows.Any(row => row[Index] == cell);
        
        public void Clear() => Csv._rows.ForEach(row => row[Index].StringValue = string.Empty);

        public IEnumerator<CsvCell> GetEnumerator()
        {
            foreach (var row in Csv._rows)
                yield return row[Index];
        }
    }

    private sealed class HeadersCollection : IReadOnlyList<string>
    {
        private readonly Csv _csv;

        public int Count => _csv._columns.Count(c => !string.IsNullOrWhiteSpace(c.Header));

        public string this[int index]
        {
            get
            {
                if (index < 0) throw new IndexOutOfRangeException("index was outside the collection bounds");
                
                for (int i = 0, current = 0; i < _csv._columns.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(_csv._columns[i].Header)) continue;
                    if (++current == index) return _csv._columns[i].Header;
                }
                
                throw new IndexOutOfRangeException("index was outside the collection bounds");
            }
        }
        
        public HeadersCollection(Csv csv) => _csv = csv;
        
        public IEnumerator<string> GetEnumerator()
        {
            for (var i = 0; i < _csv._columns.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(_csv._columns[i].Header)) continue;
                yield return _csv._columns[i].Header;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
#endregion

#region Constants
    public const string DefaultSeparator = ",";
    
    public const string DefaultLineBreak = "\n";
    
    public const char DefaultEscapeCharacter = '"';
#endregion

#region Static
    public static CsvBuilder UsingParsingCulture(CultureInfo parsingCulture) 
        => new Builder().UsingParsingCulture(parsingCulture);

    public static CsvBuilder UsingSeparator(string separator) 
        => new Builder().UsingSeparator(separator);

    public static CsvBuilder UsingLineBreak(string lineBreak) 
        => new Builder().UsingLineBreak(lineBreak);
        
    public static CsvBuilder UsingEscapeCharacter(char escapeCharacter) 
        => new Builder().UsingEscapeCharacter(escapeCharacter);

    public static CsvBuilder WithFirstLineAsHeaders(bool firstLineIsHeaders) 
        => new Builder().WithFirstLineAsHeaders(firstLineIsHeaders);

    public static Csv Empty() 
        => new(string.Empty, CultureInfo.InvariantCulture, DefaultSeparator, DefaultLineBreak, DefaultEscapeCharacter, false);

    public static Csv Parse(string csvContent) => new Builder().Parse(csvContent);

    public static Csv FromFile(string path) => new Builder().ParseFile(path);

    public static Csv FromStream(Stream stream) => new Builder().ParseStream(stream);
#endregion

    private readonly List<CsvColumn> _columns = [];
    private readonly List<CsvRow> _rows = [];

    private CultureInfo ParsingCulture { get; set; }

    public bool HasHeaders => _columns.Any(c => !string.IsNullOrWhiteSpace(c.Header));

    public int RowsCount => _rows.Count;

    public int ColumnsCount => _columns.Count;

    public IReadOnlyList<string> Headers => new HeadersCollection(this);
    
    public CsvCell this[int row, int column] => _rows[row][column];
    
    public CsvCell this[int row, string header] => _rows[row][header];
    
    private Csv(
        string csvContent, 
        CultureInfo parsingCulture, 
        string separator, 
        string lineBreak, 
        char escapeCharacter, 
        bool firstRowIsHeaders)
    {
        ParsingCulture = parsingCulture;

        Parse(csvContent, separator, lineBreak, escapeCharacter, firstRowIsHeaders);
    }

    private void Parse(string csvContent, string separator, string lineBreak, char escapeChar, bool firstRowIsHeaders)
    {
        var builder = new StringBuilder();
        var isEscaping = false;
        
        // For each character in content
        for (int row = 0, column = 0, i = 0; i < csvContent.Length; i++)
        {
            var element = csvContent[i];

            if (element == escapeChar)
            {
                if (csvContent[i + 1] == escapeChar)
                    builder.Append(csvContent[++i]);
                else isEscaping = !isEscaping;
                continue;
            }

            // Append the character
            builder.Append(element);

            var separatorStart = builder.Length - separator.Length;
            var endOfLineStart = builder.Length - lineBreak.Length;

            var isSeparator = !isEscaping && builder.EndsWith(separator);
            var isEndOfLine = !isEscaping && builder.EndsWith(lineBreak);

            if (!isSeparator && !isEndOfLine && i != csvContent.Length - 1) continue;

            if (isSeparator) builder.Remove(separatorStart, separator.Length);
            if (isEndOfLine) builder.Remove(endOfLineStart, lineBreak.Length);
            
            if (row == 0)
            {
                AppendColumn(new CsvColumn(this) { Header = firstRowIsHeaders ? builder.ToString() : string.Empty });
                if (firstRowIsHeaders)
                {
                    if (isEndOfLine) row++;
                    builder.Clear();
                    continue;
                }
            }
            
            if (column == 0) AppendRow(new CsvRow(this));
            
            this[firstRowIsHeaders ? row - 1 : row, column].StringValue = builder.ToString();

            if (isSeparator) column++;

            if (isEndOfLine)
            {
                column = 0;
                row++;
            }

            builder.Clear();
        }
    }
    
    private int IndexOf(CsvRow row) => _rows.IndexOf(row);

    private int IndexOf(CsvColumn column) => _columns.IndexOf(column);
    
    private void AppendRow(CsvRow row) => _rows.Add(row);

    private void AppendColumn(CsvColumn column)
    {
        _columns.Add(column);
        _rows.ForEach(r => r.CreateCell(column.Index));
    }
    
    private void SetIndexOf(CsvRow row, int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be non negative");
        _rows.Remove(row);
        
        while (index > _rows.Count)
            AppendRow(new CsvRow(this));

        if (index == _rows.Count) _rows.Add(row);
        else _rows.Insert(index, row);
    }
    
    private void SetIndexOf(CsvColumn column, int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be non negative");
        
        var oldIndex = column.Index;
        
        while (index > _columns.Count - 1)
            AppendColumn(new CsvColumn(this) { Header = string.Empty });

        _columns.Remove(column);
        if (index == _columns.Count) _columns.Add(column);
        else _columns.Insert(index, column);
        _rows.ForEach(r => r.MoveCell(oldIndex, column.Index));
    }
    
    public ICsvRow GetRow(int index) => _rows[index];

    public ICsvColumn GetColumn(int index) => _columns[index];
    
    public ICsvColumn GetColumn(string header) 
        => TryGetColumn(header, out var column) ? column : throw new ArgumentException($"Unknown column '{header}'");

    public bool TryGetColumn(string header, [MaybeNullWhen(false)] out ICsvColumn column)
    {
        column = _columns.Find(c => c.Header.Equals(header, StringComparison.Ordinal));
        return column is not null;
    }
    
    public ICsvRow CreateRow(int index = -1)
    {
        var row = new CsvRow(this);
        
        _rows.Add(row);
        if (index >= 0) SetIndexOf(row, index);

        return row;
    }

    public bool RemoveRow(ICsvRow row)
    {
        if (row is not CsvRow r || !_rows.Remove(r)) return false;
        r.IsOrphan = true;
        return true;
    }

    public bool RemoveRowAt(int index)
    {
        if (index < 0 || index >= _rows.Count) return false;
        return RemoveRow(_rows[index]);
    }

    public ICsvColumn CreateColumn(string header = "", int index = -1)
    {
        var column = new CsvColumn(this) { Header = header };
        
        _columns.Add(column);
        if (index >= 0) SetIndexOf(column, index);

        return column;
    }
    
    public bool RemoveColumn(ICsvColumn column)
    {
        var index = column.Index;
        if (column is not CsvColumn c || !_columns.Remove(c)) return false;
        c.IsOrphan = true;
        _rows.ForEach(r => r.RemoveCell(index));
        return true;
    }

    public bool RemoveColumn(string header) => TryGetColumn(header, out var column) && RemoveColumn(column);

    public bool RemoveColumnAt(int index)
    {
        if (index < 0 || index >= _columns.Count) return false;
        return RemoveColumn(_columns[index]);
    }
    
    public IEnumerator<ICsvRow> GetEnumerator() => _rows.GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}