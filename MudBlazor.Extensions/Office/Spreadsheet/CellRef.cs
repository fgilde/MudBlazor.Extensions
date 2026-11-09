namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Zero-based address of a single cell with optional absolute markers ($) as used in A1 notation.
/// </summary>
/// <param name="Row">Zero-based row index (0 to <see cref="MaxRows"/> - 1).</param>
/// <param name="Column">Zero-based column index (0 to <see cref="MaxColumns"/> - 1).</param>
/// <param name="RowAbsolute">True if the row is written with a leading $.</param>
/// <param name="ColumnAbsolute">True if the column is written with a leading $.</param>
public readonly record struct CellRef(int Row, int Column, bool RowAbsolute = false, bool ColumnAbsolute = false)
{
    /// <summary>Maximum number of rows in a worksheet (1,048,576).</summary>
    public const int MaxRows = 1_048_576;

    /// <summary>Maximum number of columns in a worksheet (16,384, column XFD).</summary>
    public const int MaxColumns = 16_384;

    internal long Key => ((long)Row << 20) | (uint)Column;

    internal static CellRef FromKey(long key) => new((int)(key >> 20), (int)(key & 0xFFFFF));

    internal CellRef Relative => new(Row, Column);

    /// <summary>Parses an A1 style cell address such as <c>A1</c> or <c>$B$2</c>.</summary>
    /// <param name="a1">The address to parse.</param>
    /// <returns>The parsed reference.</returns>
    /// <exception cref="FormatException">The text is not a valid cell address within the worksheet limits.</exception>
    public static CellRef Parse(string a1) =>
        TryParse(a1, out var r) ? r : throw new FormatException($"'{a1}' is not a valid cell reference.");

    /// <summary>Tries to parse an A1 style cell address.</summary>
    /// <param name="a1">The address to parse.</param>
    /// <param name="r">The parsed reference when successful.</param>
    /// <returns>True if the text is a valid cell address within the worksheet limits.</returns>
    public static bool TryParse(string a1, out CellRef r)
    {
        r = default;
        if (a1 == null || !TryParseParts(a1, out var col, out var colAbs, out var row, out var rowAbs) || col < 0 || row < 0)
            return false;
        r = new CellRef(row, col, rowAbs, colAbs);
        return true;
    }

    /// <summary>Formats the reference in A1 notation including absolute markers.</summary>
    /// <returns>The A1 text, for example <c>$B$2</c>.</returns>
    public string ToA1() => (ColumnAbsolute ? "$" : "") + ColumnName(Column) + (RowAbsolute ? "$" : "") + (Row + 1);

    /// <summary>Converts a zero-based column index to its letters (0 = A, 26 = AA).</summary>
    /// <param name="col">Zero-based column index.</param>
    /// <returns>The column letters.</returns>
    public static string ColumnName(int col)
    {
        Span<char> buffer = stackalloc char[3];
        var i = buffer.Length;
        for (var n = col + 1; n > 0; n = (n - 1) / 26)
            buffer[--i] = (char)('A' + (n - 1) % 26);
        return new string(buffer[i..]);
    }

    internal static bool TryParseParts(ReadOnlySpan<char> s, out int col, out bool colAbs, out int row, out bool rowAbs)
    {
        col = -1;
        row = -1;
        colAbs = rowAbs = false;
        var i = 0;
        var dollar = false;
        if (i < s.Length && s[i] == '$')
        {
            dollar = true;
            i++;
        }

        var start = i;
        long c = 0;
        while (i < s.Length && char.IsAsciiLetter(s[i]))
        {
            c = c * 26 + (char.ToUpperInvariant(s[i]) - 'A' + 1);
            if (c > MaxColumns) return false;
            i++;
        }

        if (i > start)
        {
            col = (int)c - 1;
            colAbs = dollar;
            dollar = false;
            if (i < s.Length && s[i] == '$')
            {
                dollar = true;
                i++;
            }
        }

        start = i;
        long r = 0;
        while (i < s.Length && char.IsAsciiDigit(s[i]))
        {
            r = r * 10 + (s[i] - '0');
            if (r > MaxRows) return false;
            i++;
        }

        if (i > start)
        {
            if (r == 0) return false;
            row = (int)r - 1;
            rowAbs = dollar;
            dollar = false;
        }

        return i == s.Length && !dollar && (col >= 0 || row >= 0);
    }
}
