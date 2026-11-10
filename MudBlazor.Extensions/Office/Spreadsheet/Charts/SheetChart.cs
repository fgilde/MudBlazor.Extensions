using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>The chart types the editor can show and create.</summary>
public enum ChartKind
{
    /// <summary>Vertical bars.</summary>
    Column,

    /// <summary>Horizontal bars.</summary>
    Bar,

    /// <summary>Lines.</summary>
    Line,

    /// <summary>A pie.</summary>
    Pie,

    /// <summary>Filled areas.</summary>
    Area,

    /// <summary>X/Y points.</summary>
    Scatter,

    /// <summary>Any other chart read from a file. It is shown as a placeholder and saved unchanged, apart from its cell references.</summary>
    Other
}

/// <summary>
/// A chart placed on a worksheet. Charts are immutable; change them with <see cref="InsertChartCommand"/>, <see cref="MoveChartCommand"/>,
/// <see cref="UpdateChartCommand"/> and <see cref="DeleteChartCommand"/>. Their references follow row, column and sheet changes like formulas.
/// </summary>
public sealed class SheetChart
{
    internal SheetChart(string id, ChartKind kind)
    {
        Id = id;
        Kind = kind;
    }

    /// <summary>Identifies the chart in the commands; unique in the workbook and kept while the chart changes.</summary>
    public string Id { get; }

    /// <summary>The chart type.</summary>
    public ChartKind Kind { get; private set; }

    /// <summary>The title text, or null for no title.</summary>
    public string Title { get; private set; }

    /// <summary>True if bars, columns, lines or areas are stacked.</summary>
    public bool Stacked { get; private set; }

    /// <summary>True if the legend is shown.</summary>
    public bool ShowLegend { get; private set; }

    /// <summary>The data series in plot order; empty for <see cref="ChartKind.Other"/>.</summary>
    public IReadOnlyList<ChartSeries> Series { get; private set; } = [];

    /// <summary>The cell holding the top left corner.</summary>
    public CellRef From { get; private set; }

    /// <summary>The cell holding the bottom right corner.</summary>
    public CellRef To { get; private set; }

    /// <summary>Offset of the top left corner inside <see cref="From"/> in pixels.</summary>
    public (double X, double Y) FromOffset { get; private set; }

    /// <summary>Offset of the bottom right corner inside <see cref="To"/> in pixels.</summary>
    public (double X, double Y) ToOffset { get; private set; }

    /// <summary>
    /// True if the chart cannot be moved, changed or deleted here because it is anchored to a single cell or to the sheet, or is an Office 2016
    /// chart. Its references still follow structural changes.
    /// </summary>
    public bool ReadOnly { get; internal init; }

    internal XElement Source { get; init; }

    internal string PartName { get; init; }

    internal string RelationshipId { get; init; }

    internal IReadOnlyList<string> OtherRefs { get; private set; } = [];

    internal static SheetChart Create(ChartKind kind, string title, bool stacked, bool legend, IReadOnlyList<ChartSeries> series, IReadOnlyList<string> otherRefs,
        bool readOnly = false, XElement source = null, string partName = null, string relationshipId = null) =>
        new(Guid.NewGuid().ToString("N"), kind)
        {
            Title = title, Stacked = stacked, ShowLegend = legend, Series = series, OtherRefs = otherRefs, ReadOnly = readOnly, Source = source, PartName = partName,
            RelationshipId = relationshipId
        };

    internal SheetChart Place(CellRef from, (double X, double Y) fromOffset, CellRef to, (double X, double Y) toOffset)
    {
        var c = Copy();
        (c.From, c.FromOffset, c.To, c.ToOffset) = (from.Relative, fromOffset, to.Relative, toOffset);
        return c;
    }

    internal SheetChart With(ChartKind kind, string title, bool stacked, bool legend, IReadOnlyList<ChartSeries> series)
    {
        var c = Copy();
        (c.Kind, c.Title, c.Stacked, c.ShowLegend, c.Series) = (kind, title, stacked, legend, series);
        return c;
    }

    internal SheetChart Shift(Axis axis, int index, int count)
    {
        var (from, fromOffset) = Move(From, FromOffset, axis, index, count);
        var (to, toOffset) = Move(To, ToOffset, axis, index, count);
        return from == From && to == To && fromOffset == FromOffset && toOffset == ToOffset ? this : Place(from, fromOffset, to, toOffset);
    }

    internal SheetChart RewriteFormulas(Func<string, string> rewrite)
    {
        string F(string f) => f == null ? null : rewrite(f);
        var series = Series.Select(s => s with { NameRef = F(s.NameRef), CategoriesRef = F(s.CategoriesRef), ValuesRef = F(s.ValuesRef) }).ToList();
        var other = OtherRefs.Select(F).ToList();
        if (series.SequenceEqual(Series) && other.SequenceEqual(OtherRefs)) return this;
        var c = Copy();
        c.Series = series;
        c.OtherRefs = other;
        return c;
    }

    internal CellRange Area => CellRange.FromBounds(From.Row, From.Column, Math.Max(From.Row, To.Row), Math.Max(From.Column, To.Column));

    internal static (CellRef Cell, (double X, double Y) Offset) Move(CellRef cell, (double X, double Y) offset, Axis axis, int index, int count)
    {
        var v = axis == Axis.Row ? cell.Row : cell.Column;
        var limit = axis == Axis.Row ? CellRef.MaxRows : CellRef.MaxColumns;
        var moved = v < index ? v : count > 0 ? Math.Min(v + count, limit - 1) : v >= index - count ? v + count : index;
        var clamped = count < 0 && v >= index && v < index - count;
        if (moved == v && !clamped) return (cell, offset);
        return axis == Axis.Row
            ? (new CellRef(moved, cell.Column), clamped ? (offset.X, 0) : offset)
            : (new CellRef(cell.Row, moved), clamped ? (0, offset.Y) : offset);
    }

    private SheetChart Copy() => (SheetChart)MemberwiseClone();
}
