using System.Text;
using MudBlazor.Extensions.Components;

namespace MudBlazor.Extensions.Helper.Internal;

/// <summary>
/// A minimal, dependency-free, read-only parser for the SQLite file format (https://www.sqlite.org/fileformat2.html).
/// This does NOT use any native library and therefore works in every hosting model, including Blazor WebAssembly,
/// where a native SQLite engine (Microsoft.Data.Sqlite/SQLitePCLRaw) is typically not available.
/// It only supports reading (no SQL engine, no writes) and does not support "WITHOUT ROWID" tables or virtual tables.
/// </summary>
internal sealed class MudExSqlitePureReader
{
    /// <summary>
    /// Safety cap to avoid materializing an unbounded amount of rows into browser/process memory for huge tables.
    /// </summary>
    public const int MaxRowsPerTable = 200_000;

    private readonly byte[] _data;
    private int _pageSize;
    private int _usableSize;
    private Encoding _encoding = Encoding.UTF8;

    /// <summary>
    /// Schema entries found in sqlite_master, keyed by table/view name (case-insensitive)
    /// </summary>
    private readonly Dictionary<string, SchemaEntry> _schema = new(StringComparer.OrdinalIgnoreCase);

    private sealed record SchemaEntry(string Name, string Sql, long RootPage, bool IsView, bool IsWithoutRowId);

    /// <summary>
    /// Returns true if the given bytes start with the SQLite file format magic header.
    /// </summary>
    public static bool LooksLikeSqliteFile(byte[] data)
        => data is { Length: >= 100 } && Encoding.ASCII.GetString(data, 0, 16) == "SQLite format 3\0";

    public MudExSqlitePureReader(byte[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        ParseHeader();
        LoadSchema();
    }

    /// <summary>
    /// All tables and views found in the database (excluding internal sqlite_% tables)
    /// </summary>
    public IEnumerable<(string Name, bool IsView)> Tables => _schema.Values.Select(s => (s.Name, s.IsView));

    /// <summary>
    /// Returns the (best effort) column definitions for a table/view, parsed from its CREATE statement.
    /// </summary>
    public List<MudExFileDisplayDataBaseColumn> GetColumns(string tableName)
    {
        if (!_schema.TryGetValue(tableName, out var entry))
            return new List<MudExFileDisplayDataBaseColumn>();
        return ParseColumnsFromSql(entry.Sql);
    }

    /// <summary>
    /// Returns the number of rows for a table without fully materializing every row (fast path).
    /// </summary>
    public long CountRows(string tableName)
    {
        if (!_schema.TryGetValue(tableName, out var entry) || entry.RootPage <= 0 || entry.IsWithoutRowId)
            return 0;
        return CountRowsRecursive(entry.RootPage);
    }

    /// <summary>
    /// Returns true if the given table can be browsed by this reader (i.e. it's a normal rowid table with data)
    /// </summary>
    public bool CanBrowse(string tableName) => _schema.TryGetValue(tableName, out var entry) && !entry.IsView && !entry.IsWithoutRowId && entry.RootPage > 0;

    /// <summary>
    /// Loads and decodes all rows of a table (up to <see cref="MaxRowsPerTable"/>). Result rows are keyed by column name.
    /// </summary>
    public List<Dictionary<string, object>> GetRows(string tableName, out bool truncated)
    {
        truncated = false;
        var result = new List<Dictionary<string, object>>();
        if (!_schema.TryGetValue(tableName, out var entry) || entry.RootPage <= 0 || entry.IsWithoutRowId)
            return result;

        var columns = ParseColumnsFromSql(entry.Sql);
        var pkIndex = columns.FindIndex(c => c.IsPrimaryKey);

        var raw = new List<(long RowId, byte[] Payload)>();
        WalkTableBTree(entry.RootPage, raw, MaxRowsPerTable + 1);

        if (raw.Count > MaxRowsPerTable)
        {
            truncated = true;
            raw = raw.Take(MaxRowsPerTable).ToList();
        }

        foreach (var (rowId, payload) in raw)
        {
            var values = ParseRecord(payload);
            var dict = new Dictionary<string, object>(columns.Count, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < columns.Count; i++)
            {
                object value = i < values.Length ? values[i] : null;
                if (value == null && i == pkIndex)
                    value = rowId; // INTEGER PRIMARY KEY is a rowid-alias and stored as NULL in the record
                dict[columns[i].Name] = value;
            }
            result.Add(dict);
        }
        return result;
    }

    #region Header / page access

    private void ParseHeader()
    {
        if (_data.Length < 100 || Encoding.ASCII.GetString(_data, 0, 16) != "SQLite format 3\0")
            throw new InvalidDataException("This is not a valid SQLite database file");

        _pageSize = ReadUInt16BE(_data, 16);
        if (_pageSize == 1) _pageSize = 65536; // 1 is a magic value meaning 65536
        var reservedSpace = _data[20];
        _usableSize = _pageSize - reservedSpace;
        if (_usableSize <= 0)
            throw new InvalidDataException("Invalid SQLite page size header");

        var encoding = ReadInt32BE(_data, 56);
        _encoding = encoding switch
        {
            2 => Encoding.Unicode, // UTF-16LE
            3 => Encoding.BigEndianUnicode, // UTF-16BE
            _ => Encoding.UTF8
        };
    }

    private byte[] GetPage(long pageNumber)
    {
        long offset = (pageNumber - 1) * _pageSize;
        if (pageNumber <= 0 || offset + _pageSize > _data.Length)
            throw new InvalidDataException($"Invalid or corrupt SQLite page reference: {pageNumber}");
        var buf = new byte[_pageSize];
        Array.Copy(_data, offset, buf, 0, _pageSize);
        return buf;
    }

    #endregion

    #region Schema

    private void LoadSchema()
    {
        var raw = new List<(long RowId, byte[] Payload)>();
        WalkTableBTree(1, raw, int.MaxValue);

        foreach (var (_, payload) in raw)
        {
            var values = ParseRecord(payload);
            if (values.Length < 5) continue;

            var type = values[0] as string;
            var name = values[1] as string;
            var rootPage = values[3] switch { long l => l, int i => i, _ => 0L };
            var sql = values[4] as string;

            if (string.IsNullOrEmpty(name) || (type != "table" && type != "view"))
                continue;
            if (name.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase))
                continue;

            var withoutRowId = sql != null && sql.Contains("WITHOUT ROWID", StringComparison.OrdinalIgnoreCase);
            _schema[name] = new SchemaEntry(name, sql, rootPage, type == "view", withoutRowId);
        }
    }

    #endregion

    #region B-Tree traversal (table b-trees only - rowid tables)

    private (byte PageType, int NumCells, int CellArrayStart, uint RightMostPointer) ParseBTreeHeader(byte[] page, int headerOffset)
    {
        byte pageType = page[headerOffset];
        int numCells = ReadUInt16BE(page, headerOffset + 3);
        bool isInterior = pageType is 2 or 5;
        int cellArrayStart = headerOffset + (isInterior ? 12 : 8);
        uint rightMost = isInterior ? ReadUInt32BE(page, headerOffset + 8) : 0;
        return (pageType, numCells, cellArrayStart, rightMost);
    }

    private void WalkTableBTree(long pageNumber, List<(long RowId, byte[] Payload)> results, int maxResults)
    {
        if (results.Count >= maxResults) return;

        var page = GetPage(pageNumber);
        int headerOffset = pageNumber == 1 ? 100 : 0;
        var (pageType, numCells, cellArrayStart, rightMost) = ParseBTreeHeader(page, headerOffset);

        switch (pageType)
        {
            case 5: // interior table b-tree page
                for (int i = 0; i < numCells && results.Count < maxResults; i++)
                {
                    int cellPtr = ReadUInt16BE(page, cellArrayStart + i * 2);
                    uint childPage = ReadUInt32BE(page, cellPtr);
                    WalkTableBTree(childPage, results, maxResults);
                }
                if (rightMost > 0 && results.Count < maxResults)
                    WalkTableBTree(rightMost, results, maxResults);
                break;

            case 13: // leaf table b-tree page
                for (int i = 0; i < numCells && results.Count < maxResults; i++)
                {
                    int cellPtr = ReadUInt16BE(page, cellArrayStart + i * 2);
                    int pos = cellPtr;
                    long payloadLen = ReadVarint(page, ref pos);
                    long rowId = ReadVarint(page, ref pos);
                    byte[] payload = ReadPayload(page, pos, payloadLen);
                    results.Add((rowId, payload));
                }
                break;

            default:
                throw new InvalidDataException($"Unsupported SQLite b-tree page type {pageType} (index pages / WITHOUT ROWID tables are not supported by the built-in client-side reader)");
        }
    }

    private long CountRowsRecursive(long pageNumber)
    {
        var page = GetPage(pageNumber);
        int headerOffset = pageNumber == 1 ? 100 : 0;
        var (pageType, numCells, cellArrayStart, rightMost) = ParseBTreeHeader(page, headerOffset);

        if (pageType == 13)
            return numCells;

        if (pageType != 5)
            throw new InvalidDataException($"Unsupported SQLite b-tree page type {pageType}");

        long count = 0;
        for (int i = 0; i < numCells; i++)
        {
            int cellPtr = ReadUInt16BE(page, cellArrayStart + i * 2);
            uint childPage = ReadUInt32BE(page, cellPtr);
            count += CountRowsRecursive(childPage);
        }
        if (rightMost > 0)
            count += CountRowsRecursive(rightMost);
        return count;
    }

    private byte[] ReadPayload(byte[] page, int pos, long payloadLen)
    {
        int u = _usableSize;
        int x = u - 35; // max bytes stored locally on a table leaf page before overflow is used

        if (payloadLen <= x)
        {
            var buf = new byte[payloadLen];
            Array.Copy(page, pos, buf, 0, payloadLen);
            return buf;
        }

        int m = ((u - 12) * 32 / 255) - 23;
        long k = m + (payloadLen - m) % (u - 4);
        int localSize = k <= x ? (int)k : m;

        var result = new byte[payloadLen];
        Array.Copy(page, pos, result, 0, localSize);
        long written = localSize;
        uint overflowPage = ReadUInt32BE(page, pos + localSize);

        while (overflowPage != 0 && written < payloadLen)
        {
            var overflow = GetPage(overflowPage);
            uint nextPage = ReadUInt32BE(overflow, 0);
            long remaining = payloadLen - written;
            int chunk = (int)Math.Min(remaining, u - 4);
            Array.Copy(overflow, 4, result, written, chunk);
            written += chunk;
            overflowPage = nextPage;
        }
        return result;
    }

    #endregion

    #region Record format (column values)

    private object[] ParseRecord(byte[] payload)
    {
        int pos = 0;
        long headerLen = ReadVarint(payload, ref pos);
        int headerEnd = (int)headerLen;

        var serialTypes = new List<long>();
        while (pos < headerEnd)
            serialTypes.Add(ReadVarint(payload, ref pos));

        int bodyPos = headerEnd;
        var values = new object[serialTypes.Count];
        for (int i = 0; i < serialTypes.Count; i++)
        {
            var (value, size) = ReadValue(payload, bodyPos, serialTypes[i]);
            values[i] = value;
            bodyPos += size;
        }
        return values;
    }

    private (object Value, int Size) ReadValue(byte[] buf, int pos, long serialType)
    {
        switch (serialType)
        {
            case 0: return (null, 0); // NULL
            case 1: return (ReadIntBE(buf, pos, 1), 1);
            case 2: return (ReadIntBE(buf, pos, 2), 2);
            case 3: return (ReadIntBE(buf, pos, 3), 3);
            case 4: return (ReadIntBE(buf, pos, 4), 4);
            case 5: return (ReadIntBE(buf, pos, 6), 6);
            case 6: return (ReadIntBE(buf, pos, 8), 8);
            case 7:
                var bytes = new byte[8];
                Array.Copy(buf, pos, bytes, 0, 8);
                if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
                return (BitConverter.ToDouble(bytes, 0), 8);
            case 8: return (0L, 0);
            case 9: return (1L, 0);
            case 10:
            case 11: return (null, 0); // reserved for internal use, should not appear in well-formed files
            default:
                if (serialType >= 12 && serialType % 2 == 0)
                {
                    int len = (int)((serialType - 12) / 2);
                    var blob = new byte[len];
                    Array.Copy(buf, pos, blob, 0, len);
                    return (blob, len);
                }
                else
                {
                    int len = (int)((serialType - 13) / 2);
                    var str = len == 0 ? string.Empty : _encoding.GetString(buf, pos, len);
                    return (str, len);
                }
        }
    }

    #endregion

    #region Low level readers

    private static ushort ReadUInt16BE(byte[] buf, int pos) => (ushort)((buf[pos] << 8) | buf[pos + 1]);

    private static uint ReadUInt32BE(byte[] buf, int pos) =>
        (uint)((buf[pos] << 24) | (buf[pos + 1] << 16) | (buf[pos + 2] << 8) | buf[pos + 3]);

    private static int ReadInt32BE(byte[] buf, int pos) => (int)ReadUInt32BE(buf, pos);

    private static long ReadIntBE(byte[] buf, int pos, int numBytes)
    {
        long value = 0;
        for (int i = 0; i < numBytes; i++)
            value = (value << 8) | buf[pos + i];

        if (numBytes < 8)
        {
            long signBit = 1L << (numBytes * 8 - 1);
            if ((value & signBit) != 0)
                value -= 1L << (numBytes * 8);
        }
        return value;
    }

    /// <summary>
    /// Reads a SQLite varint (big-endian, 1-9 bytes, last byte uses all 8 bits) starting at pos and advances pos.
    /// </summary>
    private static long ReadVarint(byte[] buf, ref int pos)
    {
        long result = 0;
        for (int i = 0; i < 8; i++)
        {
            byte b = buf[pos++];
            result = (result << 7) | (uint)(b & 0x7F);
            if ((b & 0x80) == 0) return result;
        }
        result = (result << 8) | buf[pos++];
        return result;
    }

    #endregion

    #region CREATE TABLE parsing (best effort, does not require a real SQL parser)

    private static List<MudExFileDisplayDataBaseColumn> ParseColumnsFromSql(string sql)
    {
        var result = new List<MudExFileDisplayDataBaseColumn>();
        if (string.IsNullOrWhiteSpace(sql)) return result;

        int open = sql.IndexOf('(');
        int close = sql.LastIndexOf(')');
        if (open < 0 || close < 0 || close <= open) return result;

        var inner = sql.Substring(open + 1, close - open - 1);
        foreach (var part in SplitTopLevel(inner))
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0) continue;

            var upper = trimmed.ToUpperInvariant();
            if (upper.StartsWith("PRIMARY KEY") || upper.StartsWith("UNIQUE") || upper.StartsWith("CHECK") ||
                upper.StartsWith("FOREIGN KEY") || upper.StartsWith("CONSTRAINT"))
                continue; // table level constraint, not a column definition

            var name = ExtractIdentifier(trimmed);
            if (string.IsNullOrEmpty(name)) continue;

            bool isPk = upper.Contains("PRIMARY KEY");
            result.Add(new MudExFileDisplayDataBaseColumn(name, string.Empty, isPk));
        }
        return result;
    }

    private static IEnumerable<string> SplitTopLevel(string s)
    {
        var parts = new List<string>();
        int depth = 0, start = 0;
        bool inQuote = false;
        char quoteChar = '\0';

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (inQuote)
            {
                if (c == quoteChar) inQuote = false;
                continue;
            }
            switch (c)
            {
                case '"' or '\'' or '`':
                    inQuote = true;
                    quoteChar = c;
                    break;
                case '[':
                    inQuote = true;
                    quoteChar = ']';
                    break;
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    parts.Add(s.Substring(start, i - start));
                    start = i + 1;
                    break;
            }
        }
        parts.Add(s.Substring(start));
        return parts;
    }

    private static string ExtractIdentifier(string columnDef)
    {
        columnDef = columnDef.TrimStart();
        if (columnDef.Length == 0) return null;

        char first = columnDef[0];
        if (first is '"' or '`' or '[' or '\'')
        {
            char closeCh = first switch { '[' => ']', _ => first };
            int end = columnDef.IndexOf(closeCh, 1);
            if (end > 0) return columnDef.Substring(1, end - 1);
        }

        int idx = 0;
        while (idx < columnDef.Length && !char.IsWhiteSpace(columnDef[idx]) && columnDef[idx] != '(')
            idx++;
        return idx == 0 ? null : columnDef.Substring(0, idx);
    }

    #endregion
}
