using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>One sort level.</summary>
/// <param name="Column">Zero-based sheet column to sort by.</param>
/// <param name="Descending">True to sort from largest to smallest; blank cells stay last either way.</param>
/// <param name="CaseSensitive">True to order text case-sensitively.</param>
public sealed record SortKey(int Column, bool Descending = false, bool CaseSensitive = false);

/// <summary>Kinds of auto filter criteria.</summary>
public enum FilterKind
{
    /// <summary>Shows rows whose display text is in a list.</summary>
    Values,

    /// <summary>Shows rows matching one or two comparisons.</summary>
    Custom,

    /// <summary>Shows the largest or smallest values.</summary>
    Top10,

    /// <summary>Shows rows by cell or font color (a differential format of the file).</summary>
    Color,

    /// <summary>A filter read from a file that the model does not represent (date group, dynamic or icon filter); it is written back unchanged, and the rows it hid in the file stay hidden when other columns are re-applied.</summary>
    Other
}

/// <summary>Comparison of a custom filter.</summary>
public enum CustomFilterOperator
{
    /// <summary>Equal; text criteria may use the wildcards <c>*</c> and <c>?</c> (<c>~</c> escapes), so <c>*abc*</c> means "contains".</summary>
    Equal,

    /// <summary>Not equal; wildcards as for <see cref="Equal"/>.</summary>
    NotEqual,

    /// <summary>Greater than.</summary>
    GreaterThan,

    /// <summary>Greater than or equal.</summary>
    GreaterThanOrEqual,

    /// <summary>Less than.</summary>
    LessThan,

    /// <summary>Less than or equal.</summary>
    LessThanOrEqual
}

/// <summary>The criteria of one auto filter column. Create it with <see cref="Values"/>, <see cref="Custom"/>, <see cref="Top10"/> or <see cref="Color"/>.</summary>
public sealed record FilterCriteria
{
    private FilterCriteria()
    {
    }

    /// <summary>The kind of criteria.</summary>
    public FilterKind Kind { get; init; }

    /// <summary>For <see cref="FilterKind.Values"/>: display texts of the rows to show (case-insensitive).</summary>
    public IReadOnlySet<string> Shown { get; init; }

    /// <summary>For <see cref="FilterKind.Values"/>: true to show blank cells as well.</summary>
    public bool Blanks { get; init; }

    /// <summary>For <see cref="FilterKind.Custom"/>: the first comparison.</summary>
    public CustomFilterOperator Operator1 { get; init; }

    /// <summary>For <see cref="FilterKind.Custom"/>: the first value; numbers in invariant format, text with optional wildcards.</summary>
    public string Value1 { get; init; }

    /// <summary>For <see cref="FilterKind.Custom"/>: true if both comparisons must match, false if either may.</summary>
    public bool And { get; init; }

    /// <summary>For <see cref="FilterKind.Custom"/>: the second comparison, or null.</summary>
    public CustomFilterOperator? Operator2 { get; init; }

    /// <summary>For <see cref="FilterKind.Custom"/>: the second value.</summary>
    public string Value2 { get; init; }

    /// <summary>For <see cref="FilterKind.Top10"/>: number of items, or percent of items with <see cref="Percent"/>.</summary>
    public double Count { get; init; }

    /// <summary>For <see cref="FilterKind.Top10"/>: true if <see cref="Count"/> is a percentage.</summary>
    public bool Percent { get; init; }

    /// <summary>For <see cref="FilterKind.Top10"/>: true for the smallest values instead of the largest.</summary>
    public bool Bottom { get; init; }

    /// <summary>For <see cref="FilterKind.Color"/>: index of the differential format holding the color.</summary>
    public int DxfId { get; init; }

    /// <summary>For <see cref="FilterKind.Color"/>: true to filter by fill color, false by font color.</summary>
    public bool CellColor { get; init; }

    /// <summary>Shows rows whose display text is one of the given texts.</summary>
    /// <param name="shown">Display texts to show.</param>
    /// <param name="blanks">True to show blank cells too.</param>
    /// <returns>The criteria.</returns>
    public static FilterCriteria Values(IReadOnlySet<string> shown, bool blanks = false) =>
        new() { Kind = FilterKind.Values, Shown = shown ?? new HashSet<string>(), Blanks = blanks };

    /// <summary>Shows rows matching one comparison, or two combined with and/or.</summary>
    /// <param name="operator1">The first comparison.</param>
    /// <param name="value1">The first value.</param>
    /// <param name="and">True if both comparisons must match.</param>
    /// <param name="operator2">The second comparison, or null.</param>
    /// <param name="value2">The second value.</param>
    /// <returns>The criteria.</returns>
    public static FilterCriteria Custom(CustomFilterOperator operator1, string value1, bool and = false, CustomFilterOperator? operator2 = null, string value2 = null) =>
        new() { Kind = FilterKind.Custom, Operator1 = operator1, Value1 = value1 ?? "", And = and, Operator2 = operator2, Value2 = value2 ?? "" };

    /// <summary>Shows the largest or smallest numbers of the column.</summary>
    /// <param name="count">Number of items, or percent of items.</param>
    /// <param name="percent">True if <paramref name="count"/> is a percentage.</param>
    /// <param name="bottom">True for the smallest values.</param>
    /// <returns>The criteria.</returns>
    public static FilterCriteria Top10(double count, bool percent = false, bool bottom = false) =>
        new() { Kind = FilterKind.Top10, Count = count, Percent = percent, Bottom = bottom };

    /// <summary>Shows rows by color. Kept and written, not evaluated: rows it hid in the file stay hidden, and it hides no further rows.</summary>
    /// <param name="dxfId">Index of the differential format holding the color.</param>
    /// <param name="cellColor">True for fill color, false for font color.</param>
    /// <returns>The criteria.</returns>
    public static FilterCriteria Color(int dxfId, bool cellColor = true) => new() { Kind = FilterKind.Color, DxfId = dxfId, CellColor = cellColor };

    internal static FilterCriteria Other() => new() { Kind = FilterKind.Other };

    internal Func<int, bool> Matcher(Worksheet sheet, int column, int top, int bottom, CultureInfo culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        CellValue Value(int row) => sheet[new CellRef(row, column)]?.Value ?? default;
        string Text(int row) => NumberFormat.Format(Value(row), sheet.Workbook.Styles[sheet.EffectiveStyleId(new CellRef(row, column))].NumberFormat,
            culture, out _);
        switch (Kind)
        {
            case FilterKind.Values:
                var set = new HashSet<string>(Shown, StringComparer.Create(culture, true));
                return r => Text(r) is { Length: > 0 } t ? set.Contains(t) : Blanks;
            case FilterKind.Custom:
                var first = Comparison(Operator1, Value1, culture);
                var second = Operator2 is { } op ? Comparison(op, Value2, culture) : null;
                return r =>
                {
                    var (value, text) = (Value(r), Text(r));
                    var a = first(value, text);
                    if (second == null) return a;
                    var b = second(value, text);
                    return And ? a && b : a || b;
                };
            case FilterKind.Top10:
                var numbers = new List<double>();
                for (var r = top; r <= bottom; r++)
                    if (Value(r) is { Kind: CellValueKind.Number } v)
                        numbers.Add(v.NumberValue);
                if (numbers.Count == 0) return _ => false;
                numbers.Sort();
                if (!Bottom) numbers.Reverse();
                var take = (int)Math.Clamp(Percent ? Math.Floor(numbers.Count * Count / 100) : Count, 1, numbers.Count);
                var limit = numbers[take - 1];
                return r => Value(r) is { Kind: CellValueKind.Number } v && (Bottom ? v.NumberValue <= limit : v.NumberValue >= limit);
            default:
                return _ => true;
        }
    }

    private static Func<CellValue, string, bool> Comparison(CustomFilterOperator op, string criterion, CultureInfo culture)
    {
        var isNumber = double.TryParse(criterion, NumberStyles.Float, CultureInfo.InvariantCulture, out var number);
        var pattern = op is CustomFilterOperator.Equal or CustomFilterOperator.NotEqual ? Wildcard(criterion) : null;
        return (value, text) =>
        {
            if (isNumber && value.Kind == CellValueKind.Number) return Test(value.NumberValue.CompareTo(number), op);
            if (pattern != null) return pattern.IsMatch(text) == (op == CustomFilterOperator.Equal);
            return value.Kind == CellValueKind.Text && !isNumber
                                                   && Test(string.Compare(text, criterion, culture, CompareOptions.IgnoreCase), op);
        };
    }

    private static bool Test(int order, CustomFilterOperator op) => op switch
    {
        CustomFilterOperator.Equal => order == 0,
        CustomFilterOperator.NotEqual => order != 0,
        CustomFilterOperator.GreaterThan => order > 0,
        CustomFilterOperator.GreaterThanOrEqual => order >= 0,
        CustomFilterOperator.LessThan => order < 0,
        _ => order <= 0
    };

    private static Regex Wildcard(string criterion)
    {
        var sb = new StringBuilder("^");
        for (var i = 0; i < criterion.Length; i++)
        {
            var c = criterion[i];
            if (c == '~' && i + 1 < criterion.Length) sb.Append(Regex.Escape(criterion[++i].ToString()));
            else if (c == '*') sb.Append(".*");
            else if (c == '?') sb.Append('.');
            else sb.Append(Regex.Escape(c.ToString()));
        }

        return new Regex(sb.Append('$').ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }
}

/// <summary>The auto filter of a worksheet: the filtered range (header row first), criteria per column and the recorded sort.</summary>
public sealed class AutoFilter
{
    internal AutoFilter(CellRange range, IReadOnlyDictionary<int, FilterCriteria> columns = null, SortKey sort = null, XElement source = null,
        IReadOnlyDictionary<int, XElement> columnSources = null, IReadOnlySet<int> opaqueRows = null)
    {
        Range = range.Normalized;
        Columns = columns ?? new Dictionary<int, FilterCriteria>();
        Sort = sort;
        Source = source;
        ColumnSources = columnSources ?? new Dictionary<int, XElement>();
        OpaqueRows = opaqueRows ?? new HashSet<int>();
    }

    /// <summary>The filtered range; its first row holds the filter buttons.</summary>
    public CellRange Range { get; }

    /// <summary>Criteria by zero-based sheet column.</summary>
    public IReadOnlyDictionary<int, FilterCriteria> Columns { get; }

    /// <summary>The sort recorded with the filter, or null.</summary>
    public SortKey Sort { get; }

    internal XElement Source { get; }

    internal IReadOnlyDictionary<int, XElement> ColumnSources { get; }

    internal IReadOnlySet<int> OpaqueRows { get; }

    internal bool HasOpaqueColumns => Columns.Values.Any(c => !IsEvaluable(c));

    internal static bool IsEvaluable(FilterCriteria c) => c.Kind is FilterKind.Values or FilterKind.Custom or FilterKind.Top10;

    internal AutoFilter WithColumn(int column, FilterCriteria criteria)
    {
        var columns = Columns.ToDictionary();
        var sources = ColumnSources.ToDictionary();
        if (criteria == null)
        {
            columns.Remove(column);
            sources.Remove(column);
        }
        else columns[column] = criteria;

        var next = new AutoFilter(Range, columns, Sort, Source, sources, OpaqueRows);
        return next.HasOpaqueColumns ? next : next.WithOpaqueRows(new HashSet<int>());
    }

    internal AutoFilter WithOpaqueRows(IReadOnlySet<int> rows) => new(Range, Columns, Sort, Source, ColumnSources, rows);

    internal AutoFilter WithSort(SortKey sort) => new(Range, Columns, sort, Source, ColumnSources, OpaqueRows);

    internal AutoFilter WithRange(CellRange range)
    {
        if (range == Range) return this;
        bool Inside(int column) => column >= range.Left && column <= range.Right;
        return new AutoFilter(range, Columns.Where(p => Inside(p.Key)).ToDictionary(), Sort, Source, ColumnSources.Where(p => Inside(p.Key)).ToDictionary(), OpaqueRows);
    }

    internal AutoFilter Shift(Axis axis, int index, int count)
    {
        if (Worksheet.MoveRange(Range, axis, index, count) is not { } range) return null;
        var limit = axis == Axis.Row ? CellRef.MaxRows : CellRef.MaxColumns;
        int? Map(int c) => count > 0 ? c >= index ? c + count < limit ? c + count : null : c
            : c < index ? c : c < index - count ? null : c + count;
        if (axis == Axis.Row)
            return new AutoFilter(range, Columns, Sort, Source, ColumnSources, OpaqueRows.Select(Map).OfType<int>().ToHashSet());
        Dictionary<int, T> Remap<T>(IReadOnlyDictionary<int, T> map) =>
            map.Select(p => (Key: Map(p.Key), p.Value)).Where(p => p.Key != null).ToDictionary(p => p.Key.Value, p => p.Value);
        var sort = Sort != null && Map(Sort.Column) is { } c ? Sort with { Column = c } : null;
        return new AutoFilter(range, Remap(Columns), sort, Source, Remap(ColumnSources), OpaqueRows);
    }

    internal HashSet<int> Evaluate(Worksheet sheet)
    {
        var hidden = EvaluateKnown(sheet);
        if (HasOpaqueColumns) hidden.UnionWith(OpaqueRows);
        return hidden;
    }

    internal HashSet<int> EvaluateKnown(Worksheet sheet, CultureInfo culture = null)
    {
        var hidden = new HashSet<int>();
        var (top, bottom) = (Range.Top + 1, Math.Min(Range.Bottom, sheet.UsedRange.Bottom));
        foreach (var (column, criteria) in Columns.Where(p => IsEvaluable(p.Value)))
        {
            var matches = criteria.Matcher(sheet, column, top, bottom, culture);
            for (var r = top; r <= bottom; r++)
                if (!hidden.Contains(r) && !matches(r))
                    hidden.Add(r);
        }

        return hidden;
    }
}
