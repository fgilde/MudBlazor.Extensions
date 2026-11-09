namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// A rectangular cell range between two corners. Corners may be given in any order; whole columns and rows
/// are represented by spanning all rows or columns.
/// </summary>
/// <param name="Start">First corner as written.</param>
/// <param name="End">Second corner as written.</param>
public readonly record struct CellRange(CellRef Start, CellRef End)
{
    /// <summary>Top row (zero-based).</summary>
    public int Top => Math.Min(Start.Row, End.Row);

    /// <summary>Bottom row (zero-based).</summary>
    public int Bottom => Math.Max(Start.Row, End.Row);

    /// <summary>Left column (zero-based).</summary>
    public int Left => Math.Min(Start.Column, End.Column);

    /// <summary>Right column (zero-based).</summary>
    public int Right => Math.Max(Start.Column, End.Column);

    /// <summary>Number of rows covered.</summary>
    public int RowCount => Bottom - Top + 1;

    /// <summary>Number of columns covered.</summary>
    public int ColumnCount => Right - Left + 1;

    /// <summary>Number of cells covered.</summary>
    public long Area => (long)RowCount * ColumnCount;

    /// <summary>True if the range covers exactly one cell.</summary>
    public bool IsSingleCell => Top == Bottom && Left == Right;

    /// <summary>True if the range spans all rows (a whole-column range such as A:A).</summary>
    public bool IsWholeColumns => Top == 0 && Bottom == CellRef.MaxRows - 1;

    /// <summary>True if the range spans all columns (a whole-row range such as 3:3).</summary>
    public bool IsWholeRows => Left == 0 && Right == CellRef.MaxColumns - 1;

    /// <summary>Creates a range from explicit bounds.</summary>
    /// <param name="top">Top row.</param>
    /// <param name="left">Left column.</param>
    /// <param name="bottom">Bottom row.</param>
    /// <param name="right">Right column.</param>
    /// <returns>The range.</returns>
    public static CellRange FromBounds(int top, int left, int bottom, int right) =>
        new(new CellRef(top, left), new CellRef(bottom, right));

    /// <summary>Parses A1 notation: <c>A1</c>, <c>A1:B2</c>, <c>A:A</c> or <c>3:3</c>.</summary>
    /// <param name="a1">The text to parse.</param>
    /// <returns>The parsed range.</returns>
    /// <exception cref="FormatException">The text is not a valid range.</exception>
    public static CellRange Parse(string a1) =>
        TryParse(a1, out var r) ? r : throw new FormatException($"'{a1}' is not a valid range.");

    /// <summary>Tries to parse A1 notation for ranges.</summary>
    /// <param name="a1">The text to parse.</param>
    /// <param name="range">The parsed range when successful.</param>
    /// <returns>True if the text is a valid range.</returns>
    public static bool TryParse(string a1, out CellRange range)
    {
        range = default;
        if (string.IsNullOrEmpty(a1)) return false;
        var span = a1.AsSpan();
        var colon = span.IndexOf(':');
        if (colon < 0)
        {
            if (!CellRef.TryParse(a1, out var single)) return false;
            range = new CellRange(single, single);
            return true;
        }

        var second = span[(colon + 1)..];
        if (second.IndexOf(':') >= 0
            || !CellRef.TryParseParts(span[..colon], out var c1, out var ca1, out var r1, out var ra1)
            || !CellRef.TryParseParts(second, out var c2, out var ca2, out var r2, out var ra2))
            return false;

        if (c1 >= 0 && r1 >= 0 && c2 >= 0 && r2 >= 0)
            range = new CellRange(new CellRef(r1, c1, ra1, ca1), new CellRef(r2, c2, ra2, ca2));
        else if (c1 >= 0 && r1 < 0 && c2 >= 0 && r2 < 0)
            range = new CellRange(new CellRef(0, c1, false, ca1), new CellRef(CellRef.MaxRows - 1, c2, false, ca2));
        else if (c1 < 0 && r1 >= 0 && c2 < 0 && r2 >= 0)
            range = new CellRange(new CellRef(r1, 0, ra1), new CellRef(r2, CellRef.MaxColumns - 1, ra2));
        else
            return false;
        return true;
    }

    /// <summary>Tests whether a cell lies inside the range.</summary>
    /// <param name="c">The cell to test.</param>
    /// <returns>True if the cell is inside.</returns>
    public bool Contains(CellRef c) => c.Row >= Top && c.Row <= Bottom && c.Column >= Left && c.Column <= Right;

    /// <summary>Tests whether two ranges share at least one cell.</summary>
    /// <param name="other">The other range.</param>
    /// <returns>True if they overlap.</returns>
    public bool Intersects(CellRange other) =>
        Top <= other.Bottom && Bottom >= other.Top && Left <= other.Right && Right >= other.Left;

    /// <summary>Enumerates all cells row by row, normalized to relative references.</summary>
    /// <returns>The cells of the range.</returns>
    public IEnumerable<CellRef> Cells()
    {
        for (var r = Top; r <= Bottom; r++)
            for (var c = Left; c <= Right; c++)
                yield return new CellRef(r, c);
    }

    /// <summary>Formats the range in A1 notation.</summary>
    /// <returns>For example <c>A1:B2</c>, <c>A:A</c>, <c>3:3</c> or <c>C3</c>.</returns>
    public string ToA1()
    {
        if (IsWholeColumns)
            return (Start.ColumnAbsolute ? "$" : "") + CellRef.ColumnName(Start.Column) + ":" +
                   (End.ColumnAbsolute ? "$" : "") + CellRef.ColumnName(End.Column);
        if (IsWholeRows)
            return (Start.RowAbsolute ? "$" : "") + (Start.Row + 1) + ":" + (End.RowAbsolute ? "$" : "") + (End.Row + 1);
        return IsSingleCell && Start == End ? Start.ToA1() : Start.ToA1() + ":" + End.ToA1();
    }

    internal CellRange Normalized => FromBounds(Top, Left, Bottom, Right);
}
