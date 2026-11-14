namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Inserts a chart of a range next to it. Like Excel, the series run along the longer side of the range; a first row and a first column holding text
/// become the series names and the categories, and an empty top left cell makes both of them labels. A scatter chart takes its X values from the
/// first column.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="range">The data range.</param>
/// <param name="kind">The chart type; not <see cref="ChartKind.Other"/>.</param>
public sealed class InsertChartCommand(Worksheet sheet, CellRange range, ChartKind kind) : IWorkbookCommand
{
    private const int Columns = 8;
    private const int Rows = 15;
    private IReadOnlyList<SheetChart> _before;

    /// <summary>The created chart, available after the command was applied.</summary>
    public SheetChart Chart { get; private set; }

    /// <inheritdoc />
    public string Description => "Insert chart";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        if (kind == ChartKind.Other) throw new ArgumentException("Charts of this kind cannot be created.", nameof(kind));
        Worksheet.Check(range);
        if (sheet.DrawingOpaque) throw new InvalidOperationException("The drawing of this sheet cannot be read, so no chart can be added.");
        _before = sheet.Charts;
        Chart ??= Build(sheet, range.Normalized, kind);
        ChartEdit.Set(wb, sheet, [.. sheet.Charts, Chart]);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => ChartEdit.Set(wb, sheet, _before);

    private static SheetChart Build(Worksheet sheet, CellRange r, ChartKind kind)
    {
        sheet.Workbook.Calculate();
        bool Text(int row, int col) => sheet[new CellRef(row, col)]?.Value.Kind == CellValueKind.Text;
        bool Empty(int row, int col) => sheet[new CellRef(row, col)] is not { HasContent: true };
        var firstRow = Enumerable.Range(r.Left + 1, r.ColumnCount - 1).Any(c => Text(r.Top, c));
        var firstColumn = Enumerable.Range(r.Top + 1, r.RowCount - 1).Any(row => Text(row, r.Left));
        var corner = Empty(r.Top, r.Left);
        var header = r.RowCount > 1 && (firstRow || corner && firstColumn);
        var labels = r.ColumnCount > 1 && (firstColumn || corner && firstRow || kind == ChartKind.Scatter);
        var byColumns = r.RowCount >= r.ColumnCount || kind == ChartKind.Scatter;
        if (!byColumns) (header, labels) = (labels, header);

        var sheetName = Workbook.QuoteSheetName(sheet.Name) + "!";
        string Ref(int top, int left, int bottom, int right) =>
            sheetName + new CellRange(new CellRef(top, left, true, true), new CellRef(bottom, right, true, true)).ToA1();
        var (lineStart, lineEnd) = byColumns ? (r.Left, r.Right) : (r.Top, r.Bottom);
        var (dataStart, dataEnd) = byColumns ? (r.Top, r.Bottom) : (r.Left, r.Right);
        if (labels) lineStart++;
        if (header) dataStart++;
        string Line(int line, int from, int to) => byColumns ? Ref(from, line, to, line) : Ref(line, from, line, to);
        var categories = labels ? Line(lineStart - 1, dataStart, dataEnd) : null;
        var series = new List<ChartSeries>();
        for (var line = lineStart; line <= lineEnd; line++)
        {
            var nameCell = byColumns ? new CellRef(r.Top, line) : new CellRef(line, r.Left);
            var name = header ? sheet[nameCell]?.Value.ToString() ?? "" : "Series" + (series.Count + 1);
            series.Add(new ChartSeries(header ? Ref(nameCell.Row, nameCell.Column, nameCell.Row, nameCell.Column) : null, name, categories, Line(line, dataStart, dataEnd), null));
        }

        var from = new CellRef(r.Top, Math.Min(r.Right + 2, CellRef.MaxColumns - 1));
        var to = new CellRef(Math.Min(from.Row + Rows, CellRef.MaxRows - 1), Math.Min(from.Column + Columns, CellRef.MaxColumns - 1));
        return SheetChart.Create(kind, null, false, true, series, []).Place(from, default, to, default);
    }
}

/// <summary>Moves or resizes a chart by placing its corners in other cells.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="id">The chart's <see cref="SheetChart.Id"/>.</param>
/// <param name="from">The cell for the top left corner.</param>
/// <param name="to">The cell for the bottom right corner.</param>
/// <param name="fromOffset">Offset of the top left corner inside <paramref name="from"/> in pixels.</param>
/// <param name="toOffset">Offset of the bottom right corner inside <paramref name="to"/> in pixels.</param>
public sealed class MoveChartCommand(Worksheet sheet, string id, CellRef from, CellRef to, (double X, double Y) fromOffset = default, (double X, double Y) toOffset = default)
    : IWorkbookCommand
{
    private IReadOnlyList<SheetChart> _before;

    /// <inheritdoc />
    public string Description => "Move chart";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var chart = ChartEdit.Writable(sheet, id);
        Worksheet.Check(from);
        Worksheet.Check(to);
        if (to.Row < from.Row || to.Column < from.Column || fromOffset.X < 0 || fromOffset.Y < 0 || toOffset.X < 0 || toOffset.Y < 0)
            throw new ArgumentException("The bottom right corner must not lie above or left of the top left corner.", nameof(to));
        _before = sheet.Charts;
        ChartEdit.Set(wb, sheet, ChartEdit.Replace(sheet, chart, chart.Place(from, fromOffset, to, toOffset)));
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => ChartEdit.Set(wb, sheet, _before);
}

/// <summary>Changes the type, title, legend, stacking or series of a chart. Formatting from the file stays where it still applies.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="id">The chart's <see cref="SheetChart.Id"/>.</param>
/// <param name="kind">The chart type; not <see cref="ChartKind.Other"/>.</param>
/// <param name="title">The title, or null or empty for none.</param>
/// <param name="showLegend">True to show the legend.</param>
/// <param name="series">The series, each with a <see cref="ChartSeries.ValuesRef"/>; null keeps them.</param>
/// <param name="stacked">True to stack bars, lines or areas; null keeps the setting. Ignored for pie and scatter charts.</param>
public sealed class UpdateChartCommand(Worksheet sheet, string id, ChartKind kind, string title, bool showLegend, IReadOnlyList<ChartSeries> series = null,
    bool? stacked = null) : IWorkbookCommand
{
    private IReadOnlyList<SheetChart> _before;

    /// <inheritdoc />
    public string Description => "Change chart";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var chart = ChartEdit.Writable(sheet, id);
        if (chart.Kind == ChartKind.Other) throw new InvalidOperationException("Charts of this kind cannot be changed.");
        if (kind == ChartKind.Other) throw new ArgumentException("Charts cannot be changed to this kind.", nameof(kind));
        if (series != null && series.Any(s => s == null || string.IsNullOrEmpty(s.ValuesRef))) throw new ArgumentException("Every series needs values.", nameof(series));
        _before = sheet.Charts;
        var stack = kind is not (ChartKind.Pie or ChartKind.Scatter) && (stacked ?? chart.Stacked);
        var next = chart.With(kind, string.IsNullOrEmpty(title) ? null : title, stack, showLegend, series?.ToList() ?? chart.Series);
        ChartEdit.Set(wb, sheet, ChartEdit.Replace(sheet, chart, next));
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => ChartEdit.Set(wb, sheet, _before);
}

/// <summary>Deletes a chart.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="id">The chart's <see cref="SheetChart.Id"/>.</param>
public sealed class DeleteChartCommand(Worksheet sheet, string id) : IWorkbookCommand
{
    private IReadOnlyList<SheetChart> _before;

    /// <inheritdoc />
    public string Description => "Delete chart";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var chart = ChartEdit.Writable(sheet, id);
        _before = sheet.Charts;
        ChartEdit.Set(wb, sheet, sheet.Charts.Where(c => c != chart).ToList());
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => ChartEdit.Set(wb, sheet, _before);
}

internal static class ChartEdit
{
    public static SheetChart Writable(Worksheet sheet, string id)
    {
        var chart = sheet.Charts.FirstOrDefault(c => c.Id == id) ?? throw new ArgumentException("The sheet has no chart with this id.", nameof(id));
        return chart.ReadOnly ? throw new InvalidOperationException("This chart cannot be changed.") : chart;
    }

    public static List<SheetChart> Replace(Worksheet sheet, SheetChart old, SheetChart next) => sheet.Charts.Select(c => c == old ? next : c).ToList();

    public static void Set(Workbook wb, Worksheet sheet, IReadOnlyList<SheetChart> charts)
    {
        var touched = sheet.Charts.Except(charts).Concat(charts.Except(sheet.Charts)).Select(c => c.Area).ToList();
        sheet.Charts = charts;
        sheet.Modified = true;
        foreach (var area in touched) wb.NotifyFormatted(sheet, area);
    }
}
