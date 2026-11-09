using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class LookupFunctions
{
    private const string Cat = "Lookup & Reference";

    private static readonly Regex R1C1 = new(@"^(?:(?<sheet>.+)!)?R(?<r1>\d+)C(?<c1>\d+)(?::R(?<r2>\d+)C(?<c2>\d+))?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    internal static void Register(FunctionRegistry r)
    {
        r.Add(Cat, "VLOOKUP(lookup_value, table_array, col_index_num, [range_lookup])",
            "Looks up a value in the first column of a table and returns a value from another column of the same row.", (ctx, a) => Lookup(ctx, a, true));
        r.Add(Cat, "HLOOKUP(lookup_value, table_array, row_index_num, [range_lookup])",
            "Looks up a value in the first row of a table and returns a value from another row of the same column.", (ctx, a) => Lookup(ctx, a, false));
        r.Add(Cat, "XLOOKUP(lookup_value, lookup_array, return_array, [if_not_found], [match_mode], [search_mode])",
            "Searches a range for a match and returns the corresponding item of a second range.", (ctx, a) =>
            {
                var lookup = Fn.Scalar(ctx, a[0]);
                var keys = Fn.ToGrid(ctx, a[1], true);
                var values = Fn.ToGrid(ctx, a[2]);
                if (!keys.IsVector) return CalcValue.Error(CellError.Value);
                var vertical = keys.Columns == 1 && keys.Rows > 1 || (keys.Length == 1 && values.Columns == 1);
                if (vertical ? values.Rows < keys.Rows : values.Columns < keys.Columns) return CalcValue.Error(CellError.Value);
                var i = Find(keys, lookup, Mode(ctx, a, 4), Mode(ctx, a, 5, 1));
                if (i < 0) return a.Count > 3 && !a[3].IsMissing ? a[3].Value : CalcValue.Error(CellError.NA);
                return vertical ? values.Slice(i, 0, 1, values.Columns) : values.Slice(0, i, values.Rows, 1);
            });
        r.Add(Cat, "XMATCH(lookup_value, lookup_array, [match_mode], [search_mode])", "Returns the relative position of an item in an array.", (ctx, a) =>
        {
            var keys = Fn.ToGrid(ctx, a[1], true);
            if (!keys.IsVector) return CalcValue.Error(CellError.Value);
            var i = Find(keys, Fn.Scalar(ctx, a[0]), Mode(ctx, a, 2), Mode(ctx, a, 3, 1));
            return i < 0 ? CalcValue.Error(CellError.NA) : CalcValue.Number(i + 1);
        });
        r.Add(Cat, "MATCH(lookup_value, lookup_array, [match_type])", "Returns the relative position of an item in a range.", (ctx, a) =>
        {
            var keys = Fn.ToGrid(ctx, a[1], true);
            if (!keys.IsVector) return CalcValue.Error(CellError.NA);
            var type = a.Count > 2 ? Math.Sign(Fn.Num(Fn.Scalar(ctx, a[2]))) : 1;
            var i = Find(keys, Fn.Scalar(ctx, a[0]), type == 0 ? 2 : -type, type == 0 ? 1 : 2 * type);
            return i < 0 ? CalcValue.Error(CellError.NA) : CalcValue.Number(i + 1);
        });
        r.Add(Cat, "INDEX(array, row_num, [column_num], [area_num])", "Returns the value or reference at a row and column of a range or array.", Index);
        r.Add(Cat, "CHOOSE(index_num, value1, [value2], ...)", "Returns the value at a position in the list of arguments.", (ctx, a) =>
        {
            var i = Fn.Int(Fn.Scalar(ctx, a[0]));
            return i < 1 || i >= a.Count ? CalcValue.Error(CellError.Value) : a[i].IsMissing ? CalcValue.Number(0) : a[i].Value;
        });
        r.Add(Cat, "LOOKUP(lookup_value, lookup_vector, [result_vector])", "Looks up a value in a sorted vector and returns the value at the same position of another vector.", (ctx, a) =>
        {
            var keys = Fn.ToGrid(ctx, a[1], true);
            var lookup = Fn.Scalar(ctx, a[0]);
            if (a.Count > 2)
            {
                var results = Fn.ToGrid(ctx, a[2]);
                var i = Find(keys.IsVector ? keys : Vector(keys, keys.Rows > keys.Columns), lookup, -1, 2);
                return i < 0 ? CalcValue.Error(CellError.NA) : i < results.Length ? results.At(i) : CalcValue.Error(CellError.NA);
            }

            var byRow = keys.Columns > keys.Rows;
            var j = Find(Vector(keys, !byRow), lookup, -1, 2);
            if (j < 0) return CalcValue.Error(CellError.NA);
            return byRow ? keys.Slice(keys.Rows - 1, j, 1, 1) : keys.Slice(j, keys.Columns - 1, 1, 1);
        });
        r.Add(Cat, "OFFSET(reference, rows, cols, [height], [width])", "Returns a reference offset from a reference by rows and columns.", (ctx, a) =>
        {
            var reference = a[0].Value;
            if (reference.Kind != CalcValueKind.Reference) return reference.IsError ? reference : CalcValue.Error(CellError.Value);
            if (reference.Areas.Count != 1) return CalcValue.Error(CellError.Value);
            var (sheet, range) = reference.Areas[0];
            var top = range.Top + Fn.Int(Fn.Scalar(ctx, a[1]));
            var left = range.Left + Fn.Int(Fn.Scalar(ctx, a[2]));
            var height = a.Count > 3 && !a[3].IsMissing ? Fn.Int(Fn.Scalar(ctx, a[3])) : range.RowCount;
            var width = a.Count > 4 && !a[4].IsMissing ? Fn.Int(Fn.Scalar(ctx, a[4])) : range.ColumnCount;
            if (height == 0 || width == 0) return CalcValue.Error(CellError.Ref);
            var bottom = top + height - Math.Sign(height);
            var right = left + width - Math.Sign(width);
            if (Math.Min(top, bottom) < 0 || Math.Min(left, right) < 0 || Math.Max(top, bottom) >= CellRef.MaxRows || Math.Max(left, right) >= CellRef.MaxColumns)
                return CalcValue.Error(CellError.Ref);
            return CalcValue.FromReference([new RefArea(sheet, CellRange.FromBounds(Math.Min(top, bottom), Math.Min(left, right), Math.Max(top, bottom), Math.Max(left, right)))]);
        }, true);
        r.Add(Cat, "INDIRECT(ref_text, [a1])", "Returns the reference given by a text such as \"Sheet1!A1\" or, with a1 FALSE, \"R1C1\".", (ctx, a) =>
        {
            var text = Fn.Str(Fn.Scalar(ctx, a[0])).Trim();
            if (a.Count > 1 && !a[1].IsMissing && !Fn.Bool(Fn.Scalar(ctx, a[1])))
            {
                var m = R1C1.Match(text);
                if (!m.Success) return CalcValue.Error(CellError.Ref);
                string Cell(string row, string col) => int.TryParse(col, out var c) && c is >= 1 and <= CellRef.MaxColumns && int.TryParse(row, out var rr) && rr >= 1
                    ? CellRef.ColumnName(c - 1) + rr
                    : throw Fn.Error(CellError.Ref);
                text = (m.Groups["sheet"].Success ? m.Groups["sheet"].Value + "!" : "") + Cell(m.Groups["r1"].Value, m.Groups["c1"].Value)
                       + (m.Groups["r2"].Success ? ":" + Cell(m.Groups["r2"].Value, m.Groups["c2"].Value) : "");
            }

            if (!FormulaParser.TryParse(text, out var node, out _) || !IsReferenceExpression(node)) return CalcValue.Error(CellError.Ref);
            var result = ctx.Evaluate(node);
            return result.Kind == CalcValueKind.Reference ? result : CalcValue.Error(CellError.Ref);
        }, true);
        r.Add(Cat, "ROW([reference])", "Returns the row number of a reference.", (ctx, a) => Position(ctx, a, true));
        r.Add(Cat, "COLUMN([reference])", "Returns the column number of a reference.", (ctx, a) => Position(ctx, a, false));
        r.Add(Cat, "ROWS(array)", "Returns the number of rows in a reference or array.", (ctx, a) => Size(ctx, a, true));
        r.Add(Cat, "COLUMNS(array)", "Returns the number of columns in a reference or array.", (ctx, a) => Size(ctx, a, false));
        r.Add(Cat, "AREAS(reference)", "Returns the number of areas in a reference.",
            (_, a) => a[0].Value.Kind == CalcValueKind.Reference ? CalcValue.Number(a[0].Value.Areas.Count) : CalcValue.Error(CellError.Value));
        r.Add(Cat, "ADDRESS(row_num, column_num, [abs_num], [a1], [sheet_text])", "Returns a cell address as text.", Fn.Lift(a =>
        {
            var (row, col, abs) = (Fn.Int(a[0]), Fn.Int(a[1]), Fn.Int(a, 2, 1));
            if (row is < 1 or > CellRef.MaxRows || col is < 1 or > CellRef.MaxColumns || abs is < 1 or > 4) throw Fn.Error(CellError.Value);
            var (rowAbs, colAbs) = (abs is 1 or 2, abs is 1 or 3);
            var address = Fn.Bool(a, 3, true)
                ? new CellRef(row - 1, col - 1, rowAbs, colAbs).ToA1()
                : "R" + (rowAbs ? row : $"[{row}]") + "C" + (colAbs ? col : $"[{col}]");
            var sheet = a.Length > 4 ? Fn.Str(a[4]) : "";
            return CellValue.Text(sheet.Length == 0 ? address : Workbook.QuoteSheetName(sheet) + "!" + address);
        }));
        r.Add(Cat, "TRANSPOSE(array)", "Swaps the rows and columns of an array or range.", (ctx, a) =>
        {
            var source = Fn.Array(ctx, a[0]);
            var result = new CalcArray(source.Columns, source.Rows);
            for (var i = 0; i < source.Rows; i++)
                for (var j = 0; j < source.Columns; j++)
                    result[j, i] = source[i, j];
            return CalcValue.FromArray(result);
        });
        r.Add(Cat, "FORMULATEXT(reference)", "Returns the formula of a cell as text.", (_, a) =>
        {
            var value = a[0].Value;
            if (value.Kind != CalcValueKind.Reference) return CalcValue.Error(CellError.NA);
            var (sheet, range) = value.Areas[0];
            return sheet[range.Start.Relative]?.Formula is { } f ? CalcValue.Text("=" + f) : CalcValue.Error(CellError.NA);
        });
        r.Add(Cat, "HYPERLINK(link_location, [friendly_name])", "Returns the friendly name of a link, or the link itself.",
            Fn.Lift(a => a.Length > 1 ? a[1] : CellValue.Text(Fn.Str(a[0]))));
    }

    private static int Mode(FunctionContext ctx, IReadOnlyList<CalcArg> a, int i, int fallback = 0) =>
        a.Count > i && !a[i].IsMissing ? Fn.Int(Fn.Scalar(ctx, a[i])) : fallback;

    private static Grid Vector(Grid grid, bool column)
    {
        var array = new CalcArray(column ? grid.Rows : 1, column ? 1 : grid.Columns);
        for (var i = 0; i < (column ? grid.Rows : grid.Columns); i++)
            if (column) array[i, 0] = grid[i, 0];
            else array[0, i] = grid[0, i];
        return new Grid(array);
    }

    internal static int Find(Grid keys, CellValue lookup, int matchMode, int searchMode)
    {
        if (lookup.Kind == CellValueKind.Error) throw Fn.Error(lookup.ErrorValue);
        if (matchMode is < -1 or > 2 || searchMode is not (1 or -1 or 2 or -2)) throw Fn.Error(CellError.Value);
        var n = keys.Length;
        var reverse = searchMode == -1;
        var wildcards = matchMode == 2 && lookup.Kind == CellValueKind.Text;
        if (Math.Abs(searchMode) == 2 && matchMode != 2)
        {
            var beforeMode = searchMode == 2 ? -1 : 1;
            var best = -1;
            for (var i = 0; i < n; i++)
            {
                var v = keys[i];
                if (v.Kind != lookup.Kind) continue;
                var cmp = Coercion.Compare(v, lookup);
                if (cmp == 0) best = i;
                else if (searchMode == 2 ? cmp < 0 : cmp > 0)
                {
                    if (matchMode == beforeMode) best = i;
                }
                else
                {
                    if (matchMode == -beforeMode && (best < 0 || Coercion.Compare(keys[best], lookup) != 0)) best = i;
                    break;
                }
            }

            return matchMode == 0 && best >= 0 && Coercion.Compare(keys[best], lookup) != 0 ? -1 : best;
        }

        var candidate = -1;
        for (var k = 0; k < n; k++)
        {
            var i = reverse ? n - 1 - k : k;
            var v = keys[i];
            if (Fn.SameValue(lookup, v, wildcards)) return i;
            if (matchMode is 0 or 2 || v.Kind != lookup.Kind) continue;
            var cmp = Coercion.Compare(v, lookup);
            if (matchMode == -1 ? cmp < 0 && (candidate < 0 || Coercion.Compare(v, keys[candidate]) > 0)
                    : cmp > 0 && (candidate < 0 || Coercion.Compare(v, keys[candidate]) < 0))
                candidate = i;
        }

        return candidate;
    }

    private static CalcValue Lookup(FunctionContext ctx, IReadOnlyList<CalcArg> a, bool vertical)
    {
        var lookup = Fn.Scalar(ctx, a[0]);
        var table = Fn.ToGrid(ctx, a[1], true);
        var index = Fn.Int(Fn.Scalar(ctx, a[2]));
        var approximate = a.Count <= 3 || a[3].IsMissing || Fn.Bool(Fn.Scalar(ctx, a[3]));
        if (index < 1) return CalcValue.Error(CellError.Value);
        if (index > (vertical ? table.Columns : table.Rows)) return CalcValue.Error(CellError.Ref);
        var keys = Vector(table, vertical);
        var i = Find(keys, lookup, approximate ? -1 : 2, approximate ? 2 : 1);
        if (i < 0) return CalcValue.Error(CellError.NA);
        return vertical ? table[i, index - 1] : table[index - 1, i];
    }

    private static CalcValue Index(FunctionContext ctx, IReadOnlyList<CalcArg> a)
    {
        var source = a[0].Value;
        var areaIndex = a.Count > 3 && !a[3].IsMissing ? Fn.Int(Fn.Scalar(ctx, a[3])) : 1;
        Grid grid;
        if (source.Kind == CalcValueKind.Reference)
        {
            if (areaIndex < 1 || areaIndex > source.Areas.Count) return CalcValue.Error(CellError.Ref);
            grid = new Grid(source.Areas[areaIndex - 1].Sheet, source.Areas[areaIndex - 1].Range);
        }
        else
        {
            if (areaIndex != 1) return CalcValue.Error(CellError.Ref);
            grid = Fn.ToGrid(ctx, source);
        }

        var row = a.Count > 1 && !a[1].IsMissing ? Fn.Int(Fn.Scalar(ctx, a[1])) : 0;
        var hasColumn = a.Count > 2 && !a[2].IsMissing;
        var col = hasColumn ? Fn.Int(Fn.Scalar(ctx, a[2])) : 0;
        if (!hasColumn && grid.Rows == 1 && grid.Columns > 1) (row, col) = (1, row);
        if (!hasColumn && grid.Columns == 1) col = 1;
        if (row < 0 || col < 0 || row > grid.Rows || col > grid.Columns) return CalcValue.Error(CellError.Ref);
        return grid.Slice(row == 0 ? 0 : row - 1, col == 0 ? 0 : col - 1, row == 0 ? grid.Rows : 1, col == 0 ? grid.Columns : 1);
    }

    private static bool IsReferenceExpression(FormulaNode node) => node switch
    {
        RefNode or RangeNode or NameNode => true,
        BinaryNode { Op: BinaryOp.Range } b => IsReferenceExpression(b.Left) && IsReferenceExpression(b.Right),
        _ => false
    };

    private static CalcValue Position(FunctionContext ctx, IReadOnlyList<CalcArg> a, bool row)
    {
        if (a.Count == 0 || a[0].IsMissing) return CalcValue.Number((row ? ctx.Cell.Row : ctx.Cell.Column) + 1);
        var value = a[0].Value;
        if (value.Kind != CalcValueKind.Reference) return value.IsError ? value : CalcValue.Error(CellError.Value);
        if (value.Areas.Count != 1) return CalcValue.Error(CellError.Value);
        var range = value.Areas[0].Range;
        var count = row ? range.RowCount : range.ColumnCount;
        var first = row ? range.Top : range.Left;
        if (count == 1 || (row ? range.IsWholeColumns : range.IsWholeRows)) return CalcValue.Number(first + 1);
        var array = new CalcArray(row ? count : 1, row ? 1 : count);
        for (var i = 0; i < count; i++) array[row ? i : 0, row ? 0 : i] = CellValue.Number(first + i + 1);
        return CalcValue.FromArray(array);
    }

    private static CalcValue Size(FunctionContext ctx, IReadOnlyList<CalcArg> a, bool rows)
    {
        var value = a[0].Value;
        switch (value.Kind)
        {
            case CalcValueKind.Reference:
                if (value.Areas.Count != 1) return CalcValue.Error(CellError.Ref);
                return CalcValue.Number(rows ? value.Areas[0].Range.RowCount : value.Areas[0].Range.ColumnCount);
            case CalcValueKind.Array:
                return CalcValue.Number(rows ? value.Array.Rows : value.Array.Columns);
            default:
                return value.IsError ? value : CalcValue.Number(1);
        }
    }
}
