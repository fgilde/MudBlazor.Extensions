using System.Text;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Rewrites references in formula text when rows or columns are inserted or deleted, or a sheet is renamed. Only the
/// affected references change; everything else, including whitespace and parts the parser does not understand (structured
/// references, sheet-scoped names, 3D references), is kept as written.
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

    /// <summary>Replaces the sheet name in sheet-qualified references and sheet-scoped names, including either end of a 3D reference.</summary>
    /// <param name="formula">Formula text without leading equals sign.</param>
    /// <param name="oldName">The previous sheet name (case-insensitive).</param>
    /// <param name="newName">The new sheet name.</param>
    /// <returns>The rewritten formula, quoting the new name where needed.</returns>
    public static string RenameSheet(string formula, string oldName, string newName) =>
        !CanReference(formula, oldName) ? formula : Rewrite(formula, (_, sheet, body, _) =>
        {
            var ends = sheet?.Split(':');
            if (ends == null || !ends.Any(e => Same(e, oldName))) return null;
            for (var i = 0; i < ends.Length; i++)
                if (Same(ends[i], oldName))
                    ends[i] = newName;
            var joined = string.Join(':', ends);
            return (ends.All(e => Workbook.QuoteSheetName(e) == e) ? joined : "'" + joined.Replace("'", "''") + "'") + "!" + body;
        });

    internal static string QualifySheet(string formula, string sheet) =>
        Rewrite(formula, (_, s, body, node) => s == null && node != null ? Workbook.QuoteSheetName(sheet) + "!" + body : null);

    internal static string RemoveSheet(string formula, string sheet) =>
        !CanReference(formula, sheet) ? formula : Rewrite(formula, (_, s, _, _) =>
            s != null && s.Split(':').Any(e => Same(e, sheet)) ? "#REF!" : null);

    internal static string Shift(string formula, Axis axis, string sheet, int at, int count, string host) =>
        !Same(host, sheet) && !CanReference(formula, sheet) ? formula : Rewrite(formula, (prefix, s, _, node) =>
        {
            if (!Same(s ?? host, sheet)) return null;
            var shifted = node switch
            {
                RefNode r => ShiftCell(r, axis, at, count),
                RangeNode r => ShiftRange(r, axis, at, count),
                _ => node
            };
            return shifted switch
            {
                _ when ReferenceEquals(shifted, node) => null,
                RefNode r => prefix + r.Cell.ToA1() + (r.Spill ? "#" : ""),
                RangeNode r => prefix + r.Range.ToA1(),
                _ => "#REF!"
            };
        });

    internal static string Translate(string formula, int rows, int columns) =>
        rows == 0 && columns == 0 ? formula : Rewrite(formula, (prefix, _, _, node) => node switch
        {
            RefNode r => Move(r.Cell, rows, columns, false, false) is { } c ? prefix + c.ToA1() + (r.Spill ? "#" : "") : prefix + "#REF!",
            RangeNode { Range: var g } => (Move(g.Start, rows, columns, g.IsWholeColumns, g.IsWholeRows), Move(g.End, rows, columns, g.IsWholeColumns, g.IsWholeRows)) is ({ } s, { } e)
                ? prefix + new CellRange(s, e).ToA1()
                : prefix + "#REF!",
            _ => null
        });

    internal static FormulaNode Translate(FormulaNode node, int rows, int columns) =>
        rows == 0 && columns == 0 ? node : node.Rewrite(n => n switch
        {
            RefNode r => Move(r.Cell, rows, columns, false, false) is { } c ? r with { Cell = c } : new ErrorNode(CellError.Ref),
            RangeNode { Range: var g } r => (Move(g.Start, rows, columns, g.IsWholeColumns, g.IsWholeRows), Move(g.End, rows, columns, g.IsWholeColumns, g.IsWholeRows)) is ({ } s, { } e)
                ? r with { Range = new CellRange(s, e) }
                : new ErrorNode(CellError.Ref),
            _ => n
        });

    private static CellRef? Move(CellRef c, int rows, int columns, bool keepRow, bool keepColumn)
    {
        var row = c.RowAbsolute || keepRow ? c.Row : c.Row + rows;
        var col = c.ColumnAbsolute || keepColumn ? c.Column : c.Column + columns;
        return row >= 0 && row < CellRef.MaxRows && col >= 0 && col < CellRef.MaxColumns ? c with { Row = row, Column = col } : null;
    }

    internal static string RenameName(string formula, string oldName, string newName)
    {
        if (string.IsNullOrEmpty(formula) || formula.IndexOf(oldName, StringComparison.OrdinalIgnoreCase) < 0) return formula;
        StringBuilder sb = null;
        var last = 0;
        foreach (var t in FormulaLexer.Tokenize(formula, true))
        {
            if (t.Kind != FormulaTokenKind.Name || !Same(t.Text, oldName)) continue;
            sb ??= new StringBuilder(formula.Length + newName.Length);
            sb.Append(formula, last, t.Start - last).Append(newName);
            last = t.Start + t.Text.Length;
        }

        return sb == null ? formula : sb.Append(formula, last, formula.Length - last).ToString();
    }

    internal static bool CanReference(string formula, string sheet) =>
        formula != null && formula.Contains('!') && (formula.Contains(sheet, StringComparison.OrdinalIgnoreCase)
                                                     || formula.Contains(sheet.Replace("'", "''"), StringComparison.OrdinalIgnoreCase));

    private static bool Same(string a, string b) => a != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string Rewrite(string formula, Func<string, string, string, FormulaNode, string> rewrite)
    {
        if (string.IsNullOrEmpty(formula)) return formula;
        StringBuilder sb = null;
        var last = 0;
        foreach (var t in FormulaLexer.Tokenize(formula, true))
        {
            var bang = t.Text.LastIndexOf('!');
            if (t.Kind != FormulaTokenKind.Reference && (t.Kind != FormulaTokenKind.Name || bang < 0)) continue;
            var prefix = t.Text[..(bang + 1)];
            var sheet = bang < 0 ? null : FormulaParser.UnquoteSheet(t.Text[..bang]);
            var node = t.Kind == FormulaTokenKind.Reference ? FormulaParser.ParseReference(t.Text) : null;
            var text = rewrite(prefix, sheet, t.Text[(bang + 1)..], node);
            if (text == null) continue;
            sb ??= new StringBuilder(formula.Length + 8);
            sb.Append(formula, last, t.Start - last).Append(text);
            last = t.Start + t.Text.Length;
        }

        return sb == null ? formula : sb.Append(formula, last, formula.Length - last).ToString();
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
