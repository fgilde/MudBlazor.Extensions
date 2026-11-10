using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>How a threshold of a color scale, data bar or icon set is computed.</summary>
public enum ConditionalValueType
{
    /// <summary>The smallest number of the ranges.</summary>
    Min,

    /// <summary>The largest number of the ranges.</summary>
    Max,

    /// <summary>A fixed number (or formula) in <see cref="ConditionalValue.Value"/>.</summary>
    Number,

    /// <summary>A percentage of the span between the smallest and the largest number.</summary>
    Percent,

    /// <summary>A percentile of the numbers of the ranges.</summary>
    Percentile,

    /// <summary>The result of the formula in <see cref="ConditionalValue.Value"/>.</summary>
    Formula
}

/// <summary>A threshold of a color scale, data bar or icon set (<c>cfvo</c>).</summary>
/// <param name="Type">How the threshold is computed.</param>
/// <param name="Value">Number, percentage or formula text; null for <see cref="ConditionalValueType.Min"/> and <see cref="ConditionalValueType.Max"/>.</param>
/// <param name="GreaterOrEqual">For icon sets: true if values equal to the threshold reach it, false if they must exceed it.</param>
public sealed record ConditionalValue(ConditionalValueType Type, string Value = null, bool GreaterOrEqual = true)
{
    internal ConditionalValue Rewrite(Func<string, string> map) => this with { Value = map(Value) };
}

/// <summary>One color of a color scale.</summary>
/// <param name="Value">Where the color applies.</param>
/// <param name="Color">The color as #RRGGBB.</param>
public sealed record ColorScaleStop(ConditionalValue Value, string Color);

/// <summary>What a <see cref="ConditionalRule.ContainsText"/> rule tests.</summary>
public enum ContainsTextKind
{
    /// <summary>The display text contains the text, ignoring case.</summary>
    Contains,

    /// <summary>The display text does not contain the text.</summary>
    NotContains,

    /// <summary>The display text begins with the text.</summary>
    BeginsWith,

    /// <summary>The display text ends with the text.</summary>
    EndsWith,

    /// <summary>The cell is blank or holds only spaces.</summary>
    Blanks,

    /// <summary>The cell is not blank.</summary>
    NotBlanks,

    /// <summary>The cell holds an error.</summary>
    Errors,

    /// <summary>The cell holds no error.</summary>
    NotErrors
}

/// <summary>
/// A conditional formatting rule. Rules with a lower <see cref="Priority"/> win; with <see cref="StopIfTrue"/> no rule of lower priority applies
/// once this one matched. Formulas are written for the top-left cell of the first range and move relatively for the other cells.
/// </summary>
/// <param name="Priority">Priority, 1 is the highest.</param>
/// <param name="StopIfTrue">True to skip all rules of lower priority when this rule matches.</param>
public abstract record ConditionalRule(int Priority, bool StopIfTrue)
{
    internal XElement Source { get; init; }

    internal int? DxfId { get; init; }

    internal virtual DifferentialStyle Format => null;

    internal abstract ConditionalRule With(Func<string, string> map);

    internal ConditionalRule Rewrite(Func<string, string> map)
    {
        var changed = false;
        var next = With(f =>
        {
            var text = f == null ? null : map(f);
            changed |= text != f;
            return text;
        });
        return changed ? next : this;
    }

    /// <summary>Formats cells whose value compares to one or two values.</summary>
    /// <param name="Operator">between, notBetween, equal, notEqual, greaterThan, lessThan, greaterThanOrEqual or lessThanOrEqual.</param>
    /// <param name="Formula1">The first value as formula, for example <c>10</c>, <c>"abc"</c> or <c>$D$1</c>.</param>
    /// <param name="Formula2">The second value for between and notBetween, else null.</param>
    /// <param name="Style">The format applied to matching cells.</param>
    /// <param name="Priority">Priority, 1 is the highest.</param>
    /// <param name="StopIfTrue">True to skip rules of lower priority when this rule matches.</param>
    public sealed record CellIs(string Operator, string Formula1, string Formula2, DifferentialStyle Style, int Priority = 1, bool StopIfTrue = false)
        : ConditionalRule(Priority, StopIfTrue)
    {
        internal override DifferentialStyle Format => Style;

        internal override ConditionalRule With(Func<string, string> map) => this with { Formula1 = map(Formula1), Formula2 = map(Formula2) };
    }

    /// <summary>Formats cells for which a formula returns true.</summary>
    /// <param name="Formula">The formula, for example <c>$B2&gt;100</c>.</param>
    /// <param name="Style">The format applied to matching cells.</param>
    /// <param name="Priority">Priority, 1 is the highest.</param>
    /// <param name="StopIfTrue">True to skip rules of lower priority when this rule matches.</param>
    public sealed record Expression(string Formula, DifferentialStyle Style, int Priority = 1, bool StopIfTrue = false) : ConditionalRule(Priority, StopIfTrue)
    {
        internal override DifferentialStyle Format => Style;

        internal override ConditionalRule With(Func<string, string> map) => this with { Formula = map(Formula) };
    }

    /// <summary>Colors numbers on a gradient of two or three colors.</summary>
    /// <param name="Stops">Two or three stops from the lowest to the highest.</param>
    /// <param name="Priority">Priority, 1 is the highest.</param>
    /// <param name="StopIfTrue">True to skip rules of lower priority when this rule matches.</param>
    public sealed record ColorScale(IReadOnlyList<ColorScaleStop> Stops, int Priority = 1, bool StopIfTrue = false) : ConditionalRule(Priority, StopIfTrue)
    {
        internal override ConditionalRule With(Func<string, string> map) => this with { Stops = Stops.Select(s => s with { Value = s.Value.Rewrite(map) }).ToList() };
    }

    /// <summary>Draws a bar whose length follows the number.</summary>
    /// <param name="Min">Where the bar is empty.</param>
    /// <param name="Max">Where the bar is full.</param>
    /// <param name="Color">Bar color as #RRGGBB.</param>
    /// <param name="Priority">Priority, 1 is the highest.</param>
    /// <param name="StopIfTrue">True to skip rules of lower priority when this rule matches.</param>
    public sealed record DataBar(ConditionalValue Min, ConditionalValue Max, string Color, int Priority = 1, bool StopIfTrue = false) : ConditionalRule(Priority, StopIfTrue)
    {
        internal override ConditionalRule With(Func<string, string> map) => this with { Min = Min.Rewrite(map), Max = Max.Rewrite(map) };
    }

    /// <summary>Shows an icon by the band the number falls into.</summary>
    /// <param name="Set">Excel icon set name, for example <c>3Arrows</c> or <c>5Rating</c>.</param>
    /// <param name="Thresholds">Lower bound of each icon, one per icon from the lowest band up; the first is usually 0 percent.</param>
    /// <param name="Reverse">True to reverse the icon order.</param>
    /// <param name="ShowValue">False to show only the icon.</param>
    /// <param name="Priority">Priority, 1 is the highest.</param>
    /// <param name="StopIfTrue">True to skip rules of lower priority when this rule matches.</param>
    public sealed record IconSet(string Set, IReadOnlyList<ConditionalValue> Thresholds, bool Reverse = false, bool ShowValue = true, int Priority = 1,
        bool StopIfTrue = false) : ConditionalRule(Priority, StopIfTrue)
    {
        internal override ConditionalRule With(Func<string, string> map) => this with { Thresholds = Thresholds.Select(t => t.Rewrite(map)).ToList() };
    }

    /// <summary>Formats the largest or smallest numbers.</summary>
    /// <param name="Rank">Number of items, or percent of items with <paramref name="Percent"/>.</param>
    /// <param name="Percent">True if <paramref name="Rank"/> is a percentage.</param>
    /// <param name="Bottom">True for the smallest numbers.</param>
    /// <param name="Style">The format applied to matching cells.</param>
    /// <param name="Priority">Priority, 1 is the highest.</param>
    /// <param name="StopIfTrue">True to skip rules of lower priority when this rule matches.</param>
    public sealed record Top10(int Rank, bool Percent, bool Bottom, DifferentialStyle Style, int Priority = 1, bool StopIfTrue = false)
        : ConditionalRule(Priority, StopIfTrue)
    {
        internal override DifferentialStyle Format => Style;

        internal override ConditionalRule With(Func<string, string> map) => this;
    }

    /// <summary>Formats numbers above or below the average of the ranges.</summary>
    /// <param name="Below">True for numbers below the average.</param>
    /// <param name="Equal">True to include numbers equal to the bound.</param>
    /// <param name="StdDev">Number of standard deviations the bound lies from the average, 0 for the average itself.</param>
    /// <param name="Style">The format applied to matching cells.</param>
    /// <param name="Priority">Priority, 1 is the highest.</param>
    /// <param name="StopIfTrue">True to skip rules of lower priority when this rule matches.</param>
    public sealed record AboveAverage(bool Below, bool Equal, int StdDev, DifferentialStyle Style, int Priority = 1, bool StopIfTrue = false)
        : ConditionalRule(Priority, StopIfTrue)
    {
        internal override DifferentialStyle Format => Style;

        internal override ConditionalRule With(Func<string, string> map) => this;
    }

    /// <summary>Formats cells by their display text, blanks or errors.</summary>
    /// <param name="Kind">What is tested.</param>
    /// <param name="Text">The text for the text kinds, else null.</param>
    /// <param name="Style">The format applied to matching cells.</param>
    /// <param name="Priority">Priority, 1 is the highest.</param>
    /// <param name="StopIfTrue">True to skip rules of lower priority when this rule matches.</param>
    public sealed record ContainsText(ContainsTextKind Kind, string Text, DifferentialStyle Style, int Priority = 1, bool StopIfTrue = false)
        : ConditionalRule(Priority, StopIfTrue)
    {
        internal override DifferentialStyle Format => Style;

        internal override ConditionalRule With(Func<string, string> map) => this;
    }

    /// <summary>Formats values that occur more than once in the ranges, or exactly once with <paramref name="Unique"/>. Text compares ignoring case.</summary>
    /// <param name="Unique">True for unique values instead of duplicates.</param>
    /// <param name="Style">The format applied to matching cells.</param>
    /// <param name="Priority">Priority, 1 is the highest.</param>
    /// <param name="StopIfTrue">True to skip rules of lower priority when this rule matches.</param>
    public sealed record Duplicate(bool Unique, DifferentialStyle Style, int Priority = 1, bool StopIfTrue = false) : ConditionalRule(Priority, StopIfTrue)
    {
        internal override DifferentialStyle Format => Style;

        internal override ConditionalRule With(Func<string, string> map) => this;
    }

    /// <summary>Formats dates in a period relative to today.</summary>
    /// <param name="Period">today, yesterday, tomorrow, last7Days, thisWeek, lastWeek, nextWeek, thisMonth, lastMonth or nextMonth.</param>
    /// <param name="Style">The format applied to matching cells.</param>
    /// <param name="Priority">Priority, 1 is the highest.</param>
    /// <param name="StopIfTrue">True to skip rules of lower priority when this rule matches.</param>
    public sealed record TimePeriod(string Period, DifferentialStyle Style, int Priority = 1, bool StopIfTrue = false) : ConditionalRule(Priority, StopIfTrue)
    {
        internal override DifferentialStyle Format => Style;

        internal override ConditionalRule With(Func<string, string> map) => this;
    }
}

/// <summary>Conditional formatting: rules applied to one or more ranges of a sheet.</summary>
/// <param name="Ranges">The ranges; formulas of the rules are relative to the top-left cell of the first range.</param>
/// <param name="Rules">The rules.</param>
public sealed record ConditionalFormatting(IReadOnlyList<CellRange> Ranges, IReadOnlyList<ConditionalRule> Rules)
{
    internal XElement Source { get; init; }

    internal CellRef Anchor => RuleRanges.Anchor(Ranges);

    internal ConditionalFormatting Rewrite(Func<string, string> map)
    {
        var rules = Rules.Select(r => r.Rewrite(map)).ToList();
        return rules.SequenceEqual(Rules, ReferenceEqualityComparer.Instance) ? this : this with { Rules = rules };
    }

    internal ConditionalFormatting Shift(Axis axis, int index, int count) =>
        RuleRanges.Shift(Ranges, axis, index, count) is { } moved ? WithRanges(moved.Ranges, moved.Rows, moved.Columns) : null;

    internal ConditionalFormatting WithRanges(List<CellRange> ranges, int rows, int columns)
    {
        var next = this with { Ranges = ranges };
        return rows == 0 && columns == 0 ? next : next.Rewrite(f => ReferenceShifter.Translate(f, rows, columns));
    }
}

/// <summary>The result of all conditional formatting rules for one cell.</summary>
/// <param name="Style">The merged differential format of the matching rules, or null.</param>
/// <param name="ScaleColor">Fill color from a color scale as #RRGGBB, or null.</param>
/// <param name="BarFraction">Length of a data bar from 0 to 1, or null.</param>
/// <param name="BarColor">Color of the data bar, or null.</param>
/// <param name="Icon">Stable icon id <c>set:index</c> such as <c>3Arrows:0</c> (index 0 is the lowest band's icon), or null.</param>
/// <param name="HideValue">True if an icon set or data bar hides the cell value.</param>
public sealed record ConditionalResult(DifferentialStyle Style, string ScaleColor, double? BarFraction, string BarColor, string Icon, bool HideValue);

internal static class RuleRanges
{
    internal static CellRef Anchor(IReadOnlyList<CellRange> ranges) => ranges.Count == 0 ? default : new CellRef(ranges[0].Top, ranges[0].Left);

    internal static (List<CellRange> Ranges, int Rows, int Columns)? Shift(IReadOnlyList<CellRange> ranges, Axis axis, int index, int count)
    {
        var moved = ranges.Select(r => Worksheet.MoveRange(r, axis, index, count)).OfType<CellRange>().ToList();
        if (moved.Count == 0) return null;
        var before = Anchor(ranges);
        var after = Anchor(moved);
        int Origin(int v) => count > 0 ? v >= index + count ? v - count : v : v < index ? v : v - count;
        var origin = axis == Axis.Row ? new CellRef(Origin(after.Row), after.Column) : new CellRef(after.Row, Origin(after.Column));
        return (moved, origin.Row - before.Row, origin.Column - before.Column);
    }

    internal static (List<CellRange> Ranges, int Rows, int Columns)? Clear(IReadOnlyList<CellRange> ranges, CellRange cut)
    {
        if (!ranges.Any(r => r.Intersects(cut))) return (ranges.ToList(), 0, 0);
        var left = ranges.SelectMany(r => r.Intersects(cut) ? Subtract(r.Normalized, cut.Normalized) : [r]).ToList();
        if (left.Count == 0) return null;
        var before = Anchor(ranges);
        var after = Anchor(left);
        return (left, after.Row - before.Row, after.Column - before.Column);
    }

    internal static IEnumerable<CellRange> Subtract(CellRange r, CellRange cut)
    {
        if (cut.Top > r.Top) yield return CellRange.FromBounds(r.Top, r.Left, cut.Top - 1, r.Right);
        var top = Math.Max(r.Top, cut.Top);
        var bottom = Math.Min(r.Bottom, cut.Bottom);
        if (cut.Left > r.Left) yield return CellRange.FromBounds(top, r.Left, bottom, cut.Left - 1);
        if (cut.Right < r.Right) yield return CellRange.FromBounds(top, cut.Right + 1, bottom, r.Right);
        if (cut.Bottom < r.Bottom) yield return CellRange.FromBounds(cut.Bottom + 1, r.Left, r.Bottom, r.Right);
    }

    internal static void Check(IReadOnlyList<CellRange> ranges)
    {
        if (ranges == null || ranges.Count == 0) throw new ArgumentException("At least one range is required.", nameof(ranges));
        foreach (var r in ranges) Worksheet.Check(r);
    }
}
