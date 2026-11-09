namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class DynamicArrayFunctions
{
    private const string Cat = "Dynamic Array";
    private const long MaxCells = 1_000_000;

    internal static void Register(FunctionRegistry r)
    {
        r.Add(Cat, "FILTER(array, include, [if_empty])", "Filters the rows or columns of an array by a Boolean array.", (ctx, a) =>
        {
            var source = Fn.Array(ctx, a[0]);
            var include = Fn.Array(ctx, a[1]);
            bool byRows;
            if (include.Columns == 1 && include.Rows == source.Rows) byRows = true;
            else if (include.Rows == 1 && include.Columns == source.Columns) byRows = false;
            else return CalcValue.Error(CellError.Value);
            var keep = new List<int>();
            for (var i = 0; i < include.Rows * include.Columns; i++)
            {
                var v = byRows ? include[i, 0] : include[0, i];
                if (v.Kind == CellValueKind.Error) return v;
                if (Fn.Bool(v)) keep.Add(i);
            }

            if (keep.Count == 0) return a.Count > 2 && !a[2].IsMissing ? a[2].Value : CalcValue.Error(CellError.Calc);
            return byRows
                ? Make(keep.Count, source.Columns, (row, col) => source[keep[row], col])
                : Make(source.Rows, keep.Count, (row, col) => source[row, keep[col]]);
        });
        r.Add(Cat, "SORT(array, [sort_index], [sort_order], [by_col])", "Sorts the rows (or columns) of an array.", (ctx, a) =>
        {
            var source = Fn.Array(ctx, a[0]);
            var index = Fn.OptionalInt(ctx, a, 1, 1);
            var order = Fn.OptionalInt(ctx, a, 2, 1);
            var byCol = a.Count > 3 && !a[3].IsMissing && Fn.Bool(Fn.Scalar(ctx, a[3]));
            if (order is not (1 or -1) || index < 1 || index > (byCol ? source.Rows : source.Columns)) return CalcValue.Error(CellError.Value);
            var keys = Enumerable.Range(0, byCol ? source.Columns : source.Rows)
                .Select(i => (Index: i, Keys: new[] { byCol ? source[index - 1, i] : source[i, index - 1] })).ToList();
            return Reorder(source, Sorted(keys, [order]), byCol);
        });
        r.Add(Cat, "SORTBY(array, by_array1, [sort_order1], ...)", "Sorts an array by the values of other arrays.", (ctx, a) =>
        {
            var source = Fn.Array(ctx, a[0]);
            var by = new List<CalcArray>();
            var orders = new List<int>();
            for (var i = 1; i < a.Count; i += 2)
            {
                by.Add(Fn.Array(ctx, a[i]));
                orders.Add(Fn.OptionalInt(ctx, a, i + 1, 1));
            }

            var byCol = by[0].Rows == 1 && by[0].Columns > 1;
            var count = byCol ? source.Columns : source.Rows;
            if (orders.Any(o => o is not (1 or -1)) || by.Any(b => byCol ? b.Rows != 1 || b.Columns != count : b.Columns != 1 || b.Rows != count))
                return CalcValue.Error(CellError.Value);
            var keys = Enumerable.Range(0, count).Select(i => (Index: i, Keys: by.Select(b => byCol ? b[0, i] : b[i, 0]).ToArray())).ToList();
            return Reorder(source, Sorted(keys, orders), byCol);
        });
        r.Add(Cat, "UNIQUE(array, [by_col], [exactly_once])", "Returns the unique rows (or columns) of an array.", (ctx, a) =>
        {
            var source = Fn.Array(ctx, a[0]);
            var byCol = a.Count > 1 && !a[1].IsMissing && Fn.Bool(Fn.Scalar(ctx, a[1]));
            var once = a.Count > 2 && !a[2].IsMissing && Fn.Bool(Fn.Scalar(ctx, a[2]));
            var count = byCol ? source.Columns : source.Rows;
            var length = byCol ? source.Rows : source.Columns;
            var groups = Enumerable.Range(0, count)
                .GroupBy(i => string.Join("\u0001", Enumerable.Range(0, length).Select(j => Key(byCol ? source[j, i] : source[i, j]))))
                .Where(g => !once || g.Count() == 1).Select(g => g.First()).ToList();
            if (groups.Count == 0) return CalcValue.Error(CellError.Calc);
            return Reorder(source, groups, byCol);
        });
        r.Add(Cat, "SEQUENCE(rows, [columns], [start], [step])", "Returns an array of sequential numbers.", (ctx, a) =>
        {
            var (rows, cols) = (Fn.OptionalInt(ctx, a, 0, 1), Fn.OptionalInt(ctx, a, 1, 1));
            var start = a.Count > 2 && !a[2].IsMissing ? Fn.Num(Fn.Scalar(ctx, a[2])) : 1;
            var step = a.Count > 3 && !a[3].IsMissing ? Fn.Num(Fn.Scalar(ctx, a[3])) : 1;
            return Sized(rows, cols, () => Make(rows, cols, (row, col) => Fn.Number(start + (row * (double)cols + col) * step)));
        });
        r.Add(Cat, "RANDARRAY([rows], [columns], [min], [max], [integer])", "Returns an array of random numbers.", (ctx, a) =>
        {
            var (rows, cols) = (Fn.OptionalInt(ctx, a, 0, 1), Fn.OptionalInt(ctx, a, 1, 1));
            var min = a.Count > 2 && !a[2].IsMissing ? Fn.Num(Fn.Scalar(ctx, a[2])) : 0;
            var max = a.Count > 3 && !a[3].IsMissing ? Fn.Num(Fn.Scalar(ctx, a[3])) : 1;
            var integer = a.Count > 4 && !a[4].IsMissing && Fn.Bool(Fn.Scalar(ctx, a[4]));
            if (min > max || (integer && (min != Math.Floor(min) || max != Math.Floor(max)))) return CalcValue.Error(CellError.Value);
            return Sized(rows, cols, () => Make(rows, cols, (_, _) => CellValue.Number(integer
                ? Math.Floor(min + Random.Shared.NextDouble() * (max - min + 1))
                : min + Random.Shared.NextDouble() * (max - min))));
        }, true);
        r.Add(Cat, "VSTACK(array1, [array2], ...)", "Stacks arrays vertically.", (ctx, a) =>
        {
            var arrays = a.Select(x => Fn.Array(ctx, x)).ToList();
            var width = arrays.Max(x => x.Columns);
            var rows = arrays.SelectMany(x => Enumerable.Range(0, x.Rows).Select(i => (Array: x, Row: i))).ToList();
            return Make(rows.Count, width, (row, col) => col < rows[row].Array.Columns ? rows[row].Array[rows[row].Row, col] : CellValue.Error(CellError.NA));
        });
        r.Add(Cat, "HSTACK(array1, [array2], ...)", "Stacks arrays horizontally.", (ctx, a) =>
        {
            var arrays = a.Select(x => Fn.Array(ctx, x)).ToList();
            var height = arrays.Max(x => x.Rows);
            var cols = arrays.SelectMany(x => Enumerable.Range(0, x.Columns).Select(i => (Array: x, Column: i))).ToList();
            return Make(height, cols.Count, (row, col) => row < cols[col].Array.Rows ? cols[col].Array[row, cols[col].Column] : CellValue.Error(CellError.NA));
        });
        r.Add(Cat, "TAKE(array, rows, [columns])", "Returns rows or columns from the start (positive) or end (negative) of an array.", (ctx, a) =>
        {
            var source = Fn.Array(ctx, a[0]);
            var rows = Range(ctx, a, 1, source.Rows, true);
            var cols = Range(ctx, a, 2, source.Columns, true);
            if (rows.Count == 0 || cols.Count == 0) return CalcValue.Error(CellError.Calc);
            return Make(rows.Count, cols.Count, (row, col) => source[rows[row], cols[col]]);
        });
        r.Add(Cat, "DROP(array, rows, [columns])", "Removes rows or columns from the start (positive) or end (negative) of an array.", (ctx, a) =>
        {
            var source = Fn.Array(ctx, a[0]);
            var rows = Range(ctx, a, 1, source.Rows, false);
            var cols = Range(ctx, a, 2, source.Columns, false);
            if (rows.Count == 0 || cols.Count == 0) return CalcValue.Error(CellError.Calc);
            return Make(rows.Count, cols.Count, (row, col) => source[rows[row], cols[col]]);
        });
        r.Add(Cat, "TOCOL(array, [ignore], [scan_by_column])", "Returns an array as one column.", (ctx, a) => Flatten(ctx, a, true));
        r.Add(Cat, "TOROW(array, [ignore], [scan_by_column])", "Returns an array as one row.", (ctx, a) => Flatten(ctx, a, false));
        r.Add(Cat, "WRAPROWS(vector, wrap_count, [pad_with])", "Wraps a row or column into rows of a given length.", (ctx, a) => Wrap(ctx, a, true));
        r.Add(Cat, "WRAPCOLS(vector, wrap_count, [pad_with])", "Wraps a row or column into columns of a given length.", (ctx, a) => Wrap(ctx, a, false));
        r.Add(Cat, "CHOOSEROWS(array, row_num1, [row_num2], ...)", "Returns the given rows of an array (negative numbers count from the end).",
            (ctx, a) => Choose(ctx, a, true));
        r.Add(Cat, "CHOOSECOLS(array, col_num1, [col_num2], ...)", "Returns the given columns of an array (negative numbers count from the end).",
            (ctx, a) => Choose(ctx, a, false));
        r.Add(Cat, "EXPAND(array, rows, [columns], [pad_with])", "Expands an array to the given size.", (ctx, a) =>
        {
            var source = Fn.Array(ctx, a[0]);
            var rows = Fn.OptionalInt(ctx, a, 1, source.Rows);
            var cols = Fn.OptionalInt(ctx, a, 2, source.Columns);
            var pad = a.Count > 3 && !a[3].IsMissing ? Fn.Scalar(ctx, a[3]) : CellValue.Error(CellError.NA);
            if (rows < source.Rows || cols < source.Columns) return CalcValue.Error(CellError.Value);
            return Sized(rows, cols, () => Make(rows, cols, (row, col) => row < source.Rows && col < source.Columns ? source[row, col] : pad));
        });
    }

    private static CalcValue Make(int rows, int cols, Func<int, int, CellValue> value)
    {
        var array = new CalcArray(rows, cols);
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                array[r, c] = value(r, c);
        return CalcValue.FromArray(array);
    }

    private static CalcValue Sized(int rows, int cols, Func<CalcValue> make)
    {
        if (rows < 0 || cols < 0) return CalcValue.Error(CellError.Value);
        if (rows == 0 || cols == 0) return CalcValue.Error(CellError.Calc);
        return (long)rows * cols > MaxCells ? CalcValue.Error(CellError.Num) : make();
    }

    private static string Key(CellValue v) => (int)v.Kind + ":" + (v.Kind == CellValueKind.Text ? v.TextValue.ToUpperInvariant() : v.ToString());

    private static int Rank(CellValue v) => v.Kind switch
    {
        CellValueKind.Number => 0,
        CellValueKind.Text => 1,
        CellValueKind.Boolean => 2,
        CellValueKind.Error => 3,
        _ => 4
    };

    private static int CompareForSort(CellValue a, CellValue b)
    {
        var rank = Rank(a).CompareTo(Rank(b));
        if (rank != 0) return rank;
        return a.Kind switch
        {
            CellValueKind.Error => a.ErrorValue.CompareTo(b.ErrorValue),
            CellValueKind.Empty => 0,
            _ => Coercion.Compare(a, b)
        };
    }

    private static List<int> Sorted(List<(int Index, CellValue[] Keys)> keys, IReadOnlyList<int> orders)
    {
        var comparer = Comparer<CellValue[]>.Create((x, y) =>
        {
            for (var i = 0; i < x.Length; i++)
            {
                var c = CompareForSort(x[i], y[i]);
                if (c != 0) return x[i].Kind == CellValueKind.Empty || y[i].Kind == CellValueKind.Empty ? c : c * orders[i];
            }

            return 0;
        });
        return keys.OrderBy(k => k.Keys, comparer).Select(k => k.Index).ToList();
    }

    private static CalcValue Reorder(CalcArray source, List<int> order, bool byCol) => byCol
        ? Make(source.Rows, order.Count, (row, col) => source[row, order[col]])
        : Make(order.Count, source.Columns, (row, col) => source[order[row], col]);

    private static List<int> Range(FunctionContext ctx, IReadOnlyList<CalcArg> a, int i, int length, bool take)
    {
        if (a.Count <= i || a[i].IsMissing) return Enumerable.Range(0, length).ToList();
        var n = Fn.Int(Fn.Scalar(ctx, a[i]));
        if (take)
        {
            var count = Math.Min(Math.Abs(n), length);
            return Enumerable.Range(n >= 0 ? 0 : length - count, count).ToList();
        }

        var keep = Math.Max(0, length - Math.Abs(n));
        return Enumerable.Range(n >= 0 ? length - keep : 0, keep).ToList();
    }

    private static CalcValue Flatten(FunctionContext ctx, IReadOnlyList<CalcArg> a, bool column)
    {
        var source = Fn.Array(ctx, a[0]);
        var ignore = Fn.OptionalInt(ctx, a, 1, 0);
        var byColumn = a.Count > 2 && !a[2].IsMissing && Fn.Bool(Fn.Scalar(ctx, a[2]));
        if (ignore is < 0 or > 3) return CalcValue.Error(CellError.Value);
        var items = new List<CellValue>();
        for (var i = 0; i < source.Rows * source.Columns; i++)
        {
            var v = byColumn ? source[i % source.Rows, i / source.Rows] : source[i / source.Columns, i % source.Columns];
            if ((ignore & 1) != 0 && v.Kind == CellValueKind.Empty) continue;
            if ((ignore & 2) != 0 && v.Kind == CellValueKind.Error) continue;
            items.Add(v);
        }

        if (items.Count == 0) return CalcValue.Error(CellError.Calc);
        return column ? Make(items.Count, 1, (row, _) => items[row]) : Make(1, items.Count, (_, col) => items[col]);
    }

    private static CalcValue Wrap(FunctionContext ctx, IReadOnlyList<CalcArg> a, bool rows)
    {
        var source = Fn.Array(ctx, a[0]);
        if (source.Rows > 1 && source.Columns > 1) return CalcValue.Error(CellError.Value);
        var count = Fn.Int(Fn.Scalar(ctx, a[1]));
        if (count < 1) return CalcValue.Error(CellError.Num);
        var pad = a.Count > 2 && !a[2].IsMissing ? Fn.Scalar(ctx, a[2]) : CellValue.Error(CellError.NA);
        var length = source.Rows * source.Columns;
        var lines = (length + count - 1) / count;
        CellValue At(int i) => i < length ? source[source.Rows == 1 ? 0 : i, source.Rows == 1 ? i : 0] : pad;
        return rows ? Make(lines, count, (row, col) => At(row * count + col)) : Make(count, lines, (row, col) => At(col * count + row));
    }

    private static CalcValue Choose(FunctionContext ctx, IReadOnlyList<CalcArg> a, bool rows)
    {
        var source = Fn.Array(ctx, a[0]);
        var length = rows ? source.Rows : source.Columns;
        var picks = new List<int>();
        foreach (var n in Fn.Numbers(ctx, a, 1))
        {
            var i = (int)Math.Truncate(n);
            if (i == 0 || Math.Abs(i) > length) return CalcValue.Error(CellError.Value);
            picks.Add(i > 0 ? i - 1 : length + i);
        }

        return rows
            ? Make(picks.Count, source.Columns, (row, col) => source[picks[row], col])
            : Make(source.Rows, picks.Count, (row, col) => source[row, picks[col]]);
    }
}
