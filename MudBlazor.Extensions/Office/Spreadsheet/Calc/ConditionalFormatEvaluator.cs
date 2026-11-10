using System.Globalization;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Evaluates the conditional formatting of a sheet cell by cell, so a grid only pays for the cells it shows. Aggregates (minimum, maximum,
/// percentiles, ranks, averages, duplicate counts) are computed once per rule and kept until <see cref="Workbook.Changed"/> fires or
/// <see cref="Invalidate"/> is called. Evaluation never changes the workbook or the calculation engine. Dispose the evaluator to stop
/// following the workbook.
/// </summary>
public sealed class ConditionalFormatEvaluator : IDisposable
{
    private readonly Workbook _wb;
    private readonly Worksheet _sheet;
    private readonly FormulaEvaluator _evaluator = new(FunctionRegistry.Default);
    private readonly Dictionary<ConditionalRule, RuleState> _states = new(ReferenceEqualityComparer.Instance);

    /// <summary>Creates an evaluator for one sheet and follows the workbook's changes.</summary>
    /// <param name="wb">The workbook.</param>
    /// <param name="sheet">The sheet whose rules are evaluated.</param>
    public ConditionalFormatEvaluator(Workbook wb, Worksheet sheet)
    {
        _wb = wb;
        _sheet = sheet;
        wb.Changed += OnChanged;
    }

    internal int AggregateComputations { get; private set; }

    /// <summary>Applies all rules covering a cell in priority order.</summary>
    /// <param name="cell">The cell.</param>
    /// <returns>The combined result, or null if no rule matches.</returns>
    public ConditionalResult Evaluate(CellRef cell)
    {
        cell = cell.Relative;
        List<(ConditionalFormatting Formatting, ConditionalRule Rule)> matches = null;
        foreach (var cf in _sheet.ConditionalFormats)
            if (cf.Ranges.Any(r => r.Contains(cell)))
                foreach (var rule in cf.Rules)
                    (matches ??= []).Add((cf, rule));
        if (matches == null) return null;

        var value = _sheet[cell]?.Value ?? default;
        double? number = value.Kind == CellValueKind.Number ? value.NumberValue : null;
        DifferentialStyle style = null;
        string scale = null, barColor = null, icon = null;
        double? bar = null;
        bool hide = false, any = false;
        foreach (var (cf, rule) in matches.OrderBy(m => m.Rule.Priority))
        {
            var state = State(cf, rule);
            var applied = false;
            switch (rule)
            {
                case ConditionalRule.ColorScale c when number is { } v && scale == null:
                    scale = Scale(state, c, v);
                    applied = scale != null;
                    break;
                case ConditionalRule.DataBar d when number is { } v && bar == null:
                    var bounds = Resolve(state, [d.Min, d.Max]);
                    if (bounds is [{ } lo, { } hi])
                    {
                        bar = hi > lo ? Math.Clamp((v - lo) / (hi - lo), 0, 1) : 1;
                        barColor = d.Color;
                        applied = true;
                    }

                    break;
                case ConditionalRule.IconSet s when number is { } v && icon == null:
                    if (IconIndex(state, s, v) is { } index)
                    {
                        icon = s.Set + ":" + index.ToString(CultureInfo.InvariantCulture);
                        hide = !s.ShowValue;
                        applied = true;
                    }

                    break;
                case ConditionalRule.ColorScale or ConditionalRule.DataBar or ConditionalRule.IconSet:
                    break;
                default:
                    applied = Matches(state, rule, cell, value, number);
                    if (applied) style = Merge(style, rule.Format);
                    break;
            }

            any |= applied;
            if (applied && rule.StopIfTrue) break;
        }

        return any ? new ConditionalResult(style, scale, bar, barColor, icon, hide) : null;
    }

    /// <summary>Drops all cached aggregates; the next <see cref="Evaluate"/> computes them again.</summary>
    public void Invalidate() => _states.Clear();

    /// <summary>Stops following <see cref="Workbook.Changed"/>.</summary>
    public void Dispose() => _wb.Changed -= OnChanged;

    private void OnChanged(object sender, WorkbookChangedEventArgs e) => Invalidate();

    private RuleState State(ConditionalFormatting cf, ConditionalRule rule)
    {
        if (!_states.TryGetValue(rule, out var state)) _states[rule] = state = new RuleState(cf);
        return state;
    }

    private bool Matches(RuleState state, ConditionalRule rule, CellRef cell, CellValue value, double? number)
    {
        switch (rule)
        {
            case ConditionalRule.CellIs c:
                state.Node ??= RuleFormulas.Comparison(c.Operator, new RefNode(state.Formatting.Anchor, null), c.Formula1, c.Formula2);
                return Formula(state, cell);
            case ConditionalRule.Expression x:
                state.Node ??= RuleFormulas.Parse(x.Formula);
                return Formula(state, cell);
            case ConditionalRule.Top10 t when number is { } v:
                var sorted = Numbers(state);
                if (sorted.Length == 0) return false;
                var take = Math.Clamp(t.Percent ? (int)Math.Floor(sorted.Length * (double)t.Rank / 100) : t.Rank, 1, sorted.Length);
                return t.Bottom ? v <= sorted[take - 1] : v >= sorted[^take];
            case ConditionalRule.AboveAverage a when number is { } v:
                var (mean, sd) = Average(state);
                var bound = mean + (a.Below ? -1 : 1) * a.StdDev * sd;
                return a.Below ? a.Equal ? v <= bound : v < bound : a.Equal ? v >= bound : v > bound;
            case ConditionalRule.Duplicate d:
                return Key(value) is { } key && Counts(state).GetValueOrDefault(key) > 1 != d.Unique;
            case ConditionalRule.ContainsText t:
                return Contains(t, cell, value);
            case ConditionalRule.TimePeriod p when number is { } v:
                return InPeriod(p.Period, ExcelDate.ToDateTime(Math.Floor(v)).Date, DateTime.Today);
            default:
                return false;
        }
    }

    private bool Formula(RuleState state, CellRef cell) =>
        state.Node != null && RuleFormulas.IsTrue(RuleFormulas.Evaluate(_evaluator, _wb, _sheet, state.Node, state.Formatting.Anchor, cell));

    private bool Contains(ConditionalRule.ContainsText t, CellRef cell, CellValue value)
    {
        var error = value.Kind == CellValueKind.Error;
        var text = NumberFormat.Format(value, _wb.Styles[_sheet.EffectiveStyleId(cell)].NumberFormat, CultureInfo.CurrentCulture, out _);
        var find = t.Text ?? "";
        return t.Kind switch
        {
            ContainsTextKind.Contains => text.Contains(find, StringComparison.CurrentCultureIgnoreCase),
            ContainsTextKind.NotContains => !text.Contains(find, StringComparison.CurrentCultureIgnoreCase),
            ContainsTextKind.BeginsWith => text.StartsWith(find, StringComparison.CurrentCultureIgnoreCase),
            ContainsTextKind.EndsWith => text.EndsWith(find, StringComparison.CurrentCultureIgnoreCase),
            ContainsTextKind.Blanks => !error && string.IsNullOrWhiteSpace(value.ToString()),
            ContainsTextKind.NotBlanks => !error && !string.IsNullOrWhiteSpace(value.ToString()),
            ContainsTextKind.Errors => error,
            _ => !error
        };
    }

    private static bool InPeriod(string period, DateTime d, DateTime today)
    {
        var week = today.AddDays(-(int)today.DayOfWeek);
        bool Month(DateTime m) => d.Year == m.Year && d.Month == m.Month;
        return period switch
        {
            "today" => d == today,
            "yesterday" => d == today.AddDays(-1),
            "tomorrow" => d == today.AddDays(1),
            "last7Days" => d > today.AddDays(-7) && d <= today,
            "thisWeek" => d >= week && d < week.AddDays(7),
            "lastWeek" => d >= week.AddDays(-7) && d < week,
            "nextWeek" => d >= week.AddDays(7) && d < week.AddDays(14),
            "thisMonth" => Month(today),
            "lastMonth" => Month(today.AddMonths(-1)),
            "nextMonth" => Month(today.AddMonths(1)),
            _ => false
        };
    }

    private string Scale(RuleState state, ConditionalRule.ColorScale c, double v)
    {
        var at = Resolve(state, c.Stops.Select(s => s.Value).ToList());
        if (at.Count == 0 || at.Any(a => a == null)) return null;
        if (v <= at[0]) return Mix(c.Stops[0].Color, c.Stops[0].Color, 0);
        for (var i = 0; i + 1 < at.Count; i++)
        {
            if (v > at[i + 1]) continue;
            var span = at[i + 1].Value - at[i].Value;
            return Mix(c.Stops[i].Color, c.Stops[i + 1].Color, span > 0 ? (v - at[i].Value) / span : 1);
        }

        return Mix(c.Stops[^1].Color, c.Stops[^1].Color, 0);
    }

    private static string Mix(string from, string to, double t)
    {
        static int Channel(string color, int i) => Convert.ToInt32(color.TrimStart('#').Substring(i * 2, 2), 16);
        return "#" + string.Concat(Enumerable.Range(0, 3).Select(i =>
            ((int)Math.Round(Channel(from, i) + (Channel(to, i) - Channel(from, i)) * t, MidpointRounding.AwayFromZero)).ToString("X2", CultureInfo.InvariantCulture)));
    }

    private int? IconIndex(RuleState state, ConditionalRule.IconSet s, double v)
    {
        var at = Resolve(state, s.Thresholds);
        if (at.Count == 0 || at.Any(a => a == null)) return null;
        var index = 0;
        for (var i = 1; i < at.Count; i++)
            if (s.Thresholds[i].GreaterOrEqual ? v >= at[i] : v > at[i])
                index = i;
        return s.Reverse ? at.Count - 1 - index : index;
    }

    private IReadOnlyList<double?> Resolve(RuleState state, IReadOnlyList<ConditionalValue> values)
    {
        if (state.Resolved != null) return state.Resolved;
        var sorted = Numbers(state);
        return state.Resolved = sorted.Length == 0 ? [] : values.Select(v => Threshold(v, sorted, state.Formatting)).ToList();
    }

    private double? Threshold(ConditionalValue v, double[] sorted, ConditionalFormatting cf)
    {
        double min = sorted[0], max = sorted[^1];
        double? Scalar() => RuleFormulas.Parse(v.Value) is { } node
                            && RuleFormulas.Evaluate(_evaluator, _wb, _sheet, node, cf.Anchor, cf.Anchor) is { Kind: CellValueKind.Number } r
            ? r.NumberValue
            : null;
        return v.Type switch
        {
            ConditionalValueType.Min => min,
            ConditionalValueType.Max => max,
            ConditionalValueType.Percent => Scalar() is { } p ? min + (max - min) * p / 100 : null,
            ConditionalValueType.Percentile => Scalar() is { } q ? Percentile(sorted, Math.Clamp(q / 100, 0, 1)) : null,
            _ => Scalar()
        };
    }

    private static double Percentile(double[] sorted, double q)
    {
        var rank = q * (sorted.Length - 1);
        var low = (int)Math.Floor(rank);
        return low + 1 < sorted.Length ? sorted[low] + (rank - low) * (sorted[low + 1] - sorted[low]) : sorted[low];
    }

    private double[] Numbers(RuleState state)
    {
        if (state.Numbers != null) return state.Numbers;
        AggregateComputations++;
        var list = new List<double>();
        foreach (var range in state.Formatting.Ranges)
            foreach (var c in _sheet.ExistingIn(range))
                if (_sheet[c].Value is { Kind: CellValueKind.Number } v)
                    list.Add(v.NumberValue);
        list.Sort();
        return state.Numbers = [.. list];
    }

    private (double Mean, double Sd) Average(RuleState state)
    {
        var numbers = Numbers(state);
        if (numbers.Length == 0) return (0, 0);
        var mean = numbers.Average();
        return (mean, numbers.Length < 2 ? 0 : Math.Sqrt(numbers.Sum(n => (n - mean) * (n - mean)) / (numbers.Length - 1)));
    }

    private Dictionary<string, int> Counts(RuleState state)
    {
        if (state.Counts != null) return state.Counts;
        AggregateComputations++;
        var counts = new Dictionary<string, int>();
        foreach (var range in state.Formatting.Ranges)
            foreach (var c in _sheet.ExistingIn(range))
                if (Key(_sheet[c].Value) is { } key)
                    counts[key] = counts.GetValueOrDefault(key) + 1;
        return state.Counts = counts;
    }

    private static string Key(CellValue v) => v.Kind switch
    {
        CellValueKind.Empty => null,
        CellValueKind.Text => "t" + v.TextValue.ToUpperInvariant(),
        _ => (int)v.Kind + v.ToString()
    };

    private static DifferentialStyle Merge(DifferentialStyle current, DifferentialStyle next) =>
        current == null ? next
        : next == null ? current
        : new DifferentialStyle(current.Font ?? next.Font, current.Fill ?? next.Fill, current.Border ?? next.Border, current.NumberFormat ?? next.NumberFormat);

    private sealed class RuleState(ConditionalFormatting formatting)
    {
        public ConditionalFormatting Formatting { get; } = formatting;

        public FormulaNode Node { get; set; }

        public double[] Numbers { get; set; }

        public IReadOnlyList<double?> Resolved { get; set; }

        public Dictionary<string, int> Counts { get; set; }
    }
}

internal static class RuleFormulas
{
    internal static string Bare(string formula) => formula is ['=', ..] ? formula[1..] : formula;

    internal static FormulaNode Parse(string formula) =>
        !string.IsNullOrWhiteSpace(formula) && FormulaParser.TryParse(formula, out var node, out _) ? node : null;

    internal static CalcValue Calculate(FormulaEvaluator evaluator, FormulaNode node, Workbook wb, Worksheet sheet, CellRef cell)
    {
        var previous = Coercion.EvaluationCulture;
        Coercion.EvaluationCulture = CultureInfo.CurrentCulture;
        try
        {
            return evaluator.EvaluateFormula(node, new EvalScope(wb, sheet, cell));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return CalcValue.Error(CellError.Value);
        }
        finally
        {
            Coercion.EvaluationCulture = previous;
        }
    }

    internal static CellValue Evaluate(FormulaEvaluator evaluator, Workbook wb, Worksheet sheet, FormulaNode node, CellRef anchor, CellRef cell)
    {
        var moved = ReferenceShifter.Translate(node, cell.Row - anchor.Row, cell.Column - anchor.Column);
        return evaluator.ToScalar(Calculate(evaluator, moved, wb, sheet, cell), new EvalScope(wb, sheet, cell));
    }

    internal static CellValue Evaluate(Workbook wb, Worksheet sheet, FormulaNode node, CellRef anchor, CellRef cell) =>
        Evaluate(new FormulaEvaluator(FunctionRegistry.Default), wb, sheet, node, anchor, cell);

    internal static bool IsTrue(CellValue v) => v.Kind switch
    {
        CellValueKind.Number => v.NumberValue != 0,
        CellValueKind.Boolean => v.BooleanValue,
        _ => false
    };

    internal static FormulaNode Literal(CellValue v) => v.Kind switch
    {
        CellValueKind.Number => new NumberNode(v.NumberValue),
        CellValueKind.Boolean => new BoolNode(v.BooleanValue),
        CellValueKind.Error => new ErrorNode(v.ErrorValue),
        _ => new TextNode(v.TextValue ?? "")
    };

    internal static FormulaNode Comparison(string op, FormulaNode value, string formula1, string formula2)
    {
        if (Parse(formula1) is not { } a) return null;
        var b = Parse(formula2);
        BinaryNode Compare(BinaryOp o, FormulaNode x) => new(o, value, x);
        FormulaNode Inside() => new CallNode("OR", [
            new CallNode("AND", [Compare(BinaryOp.GreaterOrEqual, a), Compare(BinaryOp.LessOrEqual, b)]),
            new CallNode("AND", [Compare(BinaryOp.GreaterOrEqual, b), Compare(BinaryOp.LessOrEqual, a)])
        ]);
        return op switch
        {
            null or "between" => b == null ? null : Inside(),
            "notBetween" => b == null ? null : new CallNode("NOT", [Inside()]),
            "equal" => Compare(BinaryOp.Equal, a),
            "notEqual" => Compare(BinaryOp.NotEqual, a),
            "greaterThan" => Compare(BinaryOp.Greater, a),
            "lessThan" => Compare(BinaryOp.Less, a),
            "greaterThanOrEqual" => Compare(BinaryOp.GreaterOrEqual, a),
            "lessThanOrEqual" => Compare(BinaryOp.LessOrEqual, a),
            _ => null
        };
    }
}
