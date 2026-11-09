namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class ConditionalFunctions
{
    private const string Cat = "Conditional";

    internal static void Register(FunctionRegistry r)
    {
        r.Add(Cat, "SUMIF(range, criteria, [sum_range])", "Adds the cells that meet a criterion.",
            (ctx, a) => Fn.Number(Single(ctx, a).Values.Sum()));
        r.Add(Cat, "SUMIFS(sum_range, criteria_range1, criteria1, ...)", "Adds the cells that meet all criteria.",
            (ctx, a) => Fn.Number(Multiple(ctx, a).Values.Sum()));
        r.Add(Cat, "COUNTIF(range, criteria)", "Counts the cells that meet a criterion.",
            (ctx, a) => CalcValue.Number(ScanArgs(ctx, null, [(a[0], a[1])]).Count));
        r.Add(Cat, "COUNTIFS(criteria_range1, criteria1, ...)", "Counts the cells that meet all criteria.", (ctx, a) =>
        {
            if (a.Count % 2 != 0) return CalcValue.Error(CellError.Value);
            return CalcValue.Number(ScanArgs(ctx, null, Pairs(a, 0)).Count);
        });
        r.Add(Cat, "AVERAGEIF(range, criteria, [average_range])", "Returns the average of the cells that meet a criterion.",
            (ctx, a) => Fn.Number(StatisticalFunctions.Average(Single(ctx, a).Values)));
        r.Add(Cat, "AVERAGEIFS(average_range, criteria_range1, criteria1, ...)", "Returns the average of the cells that meet all criteria.",
            (ctx, a) => Fn.Number(StatisticalFunctions.Average(Multiple(ctx, a).Values)));
        r.Add(Cat, "MAXIFS(max_range, criteria_range1, criteria1, ...)", "Returns the largest value among the cells that meet all criteria.", (ctx, a) =>
        {
            var values = Multiple(ctx, a).Values;
            return CalcValue.Number(values.Count == 0 ? 0 : values.Max());
        });
        r.Add(Cat, "MINIFS(min_range, criteria_range1, criteria1, ...)", "Returns the smallest value among the cells that meet all criteria.", (ctx, a) =>
        {
            var values = Multiple(ctx, a).Values;
            return CalcValue.Number(values.Count == 0 ? 0 : values.Min());
        });
    }

    private static List<(CalcArg Range, CalcArg Criteria)> Pairs(IReadOnlyList<CalcArg> a, int from)
    {
        var pairs = new List<(CalcArg, CalcArg)>();
        for (var i = from; i + 1 < a.Count; i += 2) pairs.Add((a[i], a[i + 1]));
        return pairs;
    }

    private static (List<double> Values, long Count) Single(FunctionContext ctx, IReadOnlyList<CalcArg> a)
    {
        var range = ToSource(ctx, a[0]);
        var target = a.Count > 2 && !a[2].IsMissing ? ToSource(ctx, a[2]) : range;
        if (target.Grid == null) target = target.Resize(range.Rows, range.Columns);
        return Scan(ctx, target, [(range, Criterion.Parse(Fn.Scalar(ctx, a[1])))]);
    }

    private static (List<double> Values, long Count) Multiple(FunctionContext ctx, IReadOnlyList<CalcArg> a)
    {
        if (a.Count % 2 == 0) throw Fn.Error(CellError.Value);
        return ScanArgs(ctx, a[0], Pairs(a, 1));
    }

    private static (List<double> Values, long Count) ScanArgs(FunctionContext ctx, CalcArg target, List<(CalcArg Range, CalcArg Criteria)> pairs) =>
        Scan(ctx, target == null ? (Source?)null : ToSource(ctx, target), pairs.Select(p => (ToSource(ctx, p.Range), Criterion.Parse(Fn.Scalar(ctx, p.Criteria)))).ToList());

    private static Source ToSource(FunctionContext ctx, CalcArg arg)
    {
        var value = Fn.Value(arg);
        if (value.IsError) throw Fn.Error(value.Scalar.ErrorValue);
        if (value.Kind == CalcValueKind.Reference && value.Areas.Count != 1) throw Fn.Error(CellError.Value);
        return value.Kind == CalcValueKind.Reference
            ? new Source(value.Areas[0].Sheet, value.Areas[0].Range, null)
            : new Source(null, default, Fn.Array(ctx, arg));
    }

    private static (List<double> Values, long Count) Scan(FunctionContext ctx, Source? target, List<(Source Range, Criterion Criterion)> criteria)
    {
        var (rows, cols) = (criteria[0].Range.Rows, criteria[0].Range.Columns);
        if (criteria.Any(c => c.Range.Rows != rows || c.Range.Columns != cols)
            || (target is { } t && (t.Rows != rows || t.Columns != cols)))
            throw Fn.Error(CellError.Value);

        var sources = criteria.Select(c => c.Range).ToList();
        if (target is { } tt) sources.Add(tt);
        IEnumerable<(int Row, int Column)> candidates;
        var sparse = sources.All(s => s.Grid == null);
        if (sparse)
        {
            var offsets = new HashSet<(int, int)>();
            foreach (var s in sources)
                foreach (var c in s.Sheet.ExistingIn(s.Range))
                    offsets.Add((c.Row - s.Range.Top, c.Column - s.Range.Left));
            candidates = offsets.OrderBy(o => o.Item1).ThenBy(o => o.Item2);
        }
        else
        {
            candidates = Enumerable.Range(0, rows).SelectMany(r => Enumerable.Range(0, cols).Select(c => (r, c)));
        }

        var values = new List<double>();
        long count = 0, seen = 0;
        foreach (var (r, c) in candidates)
        {
            seen++;
            if (!criteria.All(x => x.Criterion.Matches(x.Range[r, c]))) continue;
            count++;
            if (target is not { } tv) continue;
            var v = tv[r, c];
            if (v.Kind == CellValueKind.Error) throw Fn.Error(v.ErrorValue);
            if (v.Kind == CellValueKind.Number) values.Add(v.NumberValue);
        }

        if (sparse && criteria.All(x => x.Criterion.Matches(default))) count += (long)rows * cols - seen;
        return (values, count);
    }
}

internal readonly record struct Source(Worksheet Sheet, CellRange Range, CalcArray Grid)
{
    public int Rows => Grid?.Rows ?? Range.RowCount;

    public int Columns => Grid?.Columns ?? Range.ColumnCount;

    public CellValue this[int r, int c] => Grid != null ? Grid[r, c] : FormulaEvaluator.GetValue(Sheet, new CellRef(Range.Top + r, Range.Left + c));

    public Source Resize(int rows, int columns) => this with
    {
        Range = CellRange.FromBounds(Range.Top, Range.Left, Math.Min(Range.Top + rows, CellRef.MaxRows) - 1,
            Math.Min(Range.Left + columns, CellRef.MaxColumns) - 1)
    };
}

internal sealed class Criterion
{
    private readonly string _op;
    private readonly CellValue _value;

    private Criterion(string op, CellValue value)
    {
        _op = op;
        _value = value;
    }

    public static Criterion Parse(CellValue criteria)
    {
        switch (criteria.Kind)
        {
            case CellValueKind.Empty:
                return new Criterion("=", CellValue.Number(0));
            case CellValueKind.Text:
                break;
            default:
                return new Criterion("=", criteria);
        }

        var s = criteria.TextValue;
        var op = s.StartsWith(">=") || s.StartsWith("<=") || s.StartsWith("<>") ? s[..2] : s.StartsWith('=') || s.StartsWith('<') || s.StartsWith('>') ? s[..1] : "";
        var rest = s[op.Length..];
        if (op == "") op = "=";
        if (rest.Length == 0) return new Criterion(op, default);
        if (Coercion.TryToNumber(CellValue.Text(rest), out var n, out _)) return new Criterion(op, CellValue.Number(n));
        if (rest.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || rest.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            return new Criterion(op, CellValue.Boolean(rest.Equals("TRUE", StringComparison.OrdinalIgnoreCase)));
        var error = FormulaLexer.ErrorCodes.FirstOrDefault(e => e.Code.Equals(rest, StringComparison.OrdinalIgnoreCase));
        return new Criterion(op, error.Code != null ? CellValue.Error(error.Error) : CellValue.Text(rest));
    }

    public bool Matches(CellValue v)
    {
        if (_value.Kind == CellValueKind.Empty)
        {
            var blank = v.Kind == CellValueKind.Empty || v is { Kind: CellValueKind.Text, TextValue: "" };
            return _op switch { "=" => blank, "<>" => !blank, _ => false };
        }

        var equal = _value.Kind == CellValueKind.Text ? v.Kind == CellValueKind.Text && Fn.Like(v.TextValue, _value.TextValue) : Fn.SameValue(_value, v, false);
        if (_op == "=") return equal;
        if (_op == "<>") return !equal;
        if (v.Kind != _value.Kind) return false;
        var cmp = Coercion.Compare(v, _value);
        return _op switch
        {
            "<" => cmp < 0,
            "<=" => cmp <= 0,
            ">" => cmp > 0,
            _ => cmp >= 0
        };
    }
}
