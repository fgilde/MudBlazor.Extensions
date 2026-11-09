namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Rewrites references in formula text when rows or columns are inserted or deleted, or a sheet is renamed. Formulas
/// without affected references are returned unchanged (including their whitespace); unparsable formulas are returned as is.
/// </summary>
public static class ReferenceShifter
{
    /// <summary>Shifts row references of a formula that lives on the changed sheet.</summary>
    /// <param name="formula">Formula text without leading equals sign.</param>
    /// <param name="sheet">Name of the sheet whose rows change.</param>
    /// <param name="at">Zero-based first inserted or deleted row.</param>
    /// <param name="count">Positive to insert rows at <paramref name="at"/>, negative to delete <c>-count</c> rows from it.</param>
    /// <returns>The rewritten formula; references to deleted cells become <c>#REF!</c>.</returns>
    public static string ShiftRows(string formula, string sheet, int at, int count) => Shift(formula, Axis.Row, sheet, at, count, sheet);

    /// <summary>Shifts column references of a formula that lives on the changed sheet.</summary>
    /// <param name="formula">Formula text without leading equals sign.</param>
    /// <param name="sheet">Name of the sheet whose columns change.</param>
    /// <param name="at">Zero-based first inserted or deleted column.</param>
    /// <param name="count">Positive to insert columns at <paramref name="at"/>, negative to delete <c>-count</c> columns from it.</param>
    /// <returns>The rewritten formula; references to deleted cells become <c>#REF!</c>.</returns>
    public static string ShiftColumns(string formula, string sheet, int at, int count) => Shift(formula, Axis.Column, sheet, at, count, sheet);

    /// <summary>Replaces the sheet name in sheet-qualified references.</summary>
    /// <param name="formula">Formula text without leading equals sign.</param>
    /// <param name="oldName">The previous sheet name (case-insensitive).</param>
    /// <param name="newName">The new sheet name.</param>
    /// <returns>The rewritten formula, quoting the new name where needed.</returns>
    public static string RenameSheet(string formula, string oldName, string newName) => Rewrite(formula, n => n switch
    {
        RefNode r when Same(r.Sheet, oldName) => r with { Sheet = newName },
        RangeNode r when Same(r.Sheet, oldName) => r with { Sheet = newName },
        _ => n
    });

    internal static string RemoveSheet(string formula, string sheet) => Rewrite(formula, n => n switch
    {
        RefNode r when Same(r.Sheet, sheet) => new ErrorNode(CellError.Ref),
        RangeNode r when Same(r.Sheet, sheet) => new ErrorNode(CellError.Ref),
        _ => n
    });

    internal static string Shift(string formula, Axis axis, string sheet, int at, int count, string host) => Rewrite(formula, n => n switch
    {
        RefNode r when Same(r.Sheet ?? host, sheet) => ShiftCell(r, axis, at, count),
        RangeNode r when Same(r.Sheet ?? host, sheet) => ShiftRange(r, axis, at, count),
        _ => n
    });

    private static bool Same(string a, string b) => a != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string Rewrite(string formula, Func<FormulaNode, FormulaNode> leaf)
    {
        if (formula == null || !FormulaParser.TryParse(formula, out var node, out _)) return formula;
        var result = node.Rewrite(leaf);
        return ReferenceEquals(result, node) ? formula : FormulaParser.Format(result);
    }

    private static int Limit(Axis axis) => axis == Axis.Row ? CellRef.MaxRows : CellRef.MaxColumns;

    private static FormulaNode ShiftCell(RefNode r, Axis axis, int at, int count)
    {
        var v = axis == Axis.Row ? r.Cell.Row : r.Cell.Column;
        if (v < at) return r;
        int nv;
        if (count > 0) nv = v + count;
        else if (v < at - count) return new ErrorNode(CellError.Ref);
        else nv = v + count;
        if (nv >= Limit(axis)) return new ErrorNode(CellError.Ref);
        return r with { Cell = axis == Axis.Row ? r.Cell with { Row = nv } : r.Cell with { Column = nv } };
    }

    private static FormulaNode ShiftRange(RangeNode r, Axis axis, int at, int count)
    {
        var range = r.Range;
        if (axis == Axis.Row ? range.IsWholeColumns : range.IsWholeRows) return r;
        var s = axis == Axis.Row ? range.Top : range.Left;
        var e = axis == Axis.Row ? range.Bottom : range.Right;
        int ns, ne;
        if (count > 0)
        {
            ns = s >= at ? s + count : s;
            ne = e >= at ? Math.Min(e + count, Limit(axis) - 1) : e;
            if (ns >= Limit(axis)) return new ErrorNode(CellError.Ref);
        }
        else
        {
            var end = at - count;
            ns = s < at ? s : s >= end ? s + count : at;
            ne = e < at ? e : e >= end ? e + count : at - 1;
            if (ne < ns) return new ErrorNode(CellError.Ref);
        }

        if (ns == s && ne == e) return r;
        var start = new CellRef(axis == Axis.Row ? ns : range.Top, axis == Axis.Row ? range.Left : ns,
            range.Start.RowAbsolute, range.Start.ColumnAbsolute);
        var stop = new CellRef(axis == Axis.Row ? ne : range.Bottom, axis == Axis.Row ? range.Right : ne,
            range.End.RowAbsolute, range.End.ColumnAbsolute);
        return r with { Range = new CellRange(start, stop) };
    }
}
