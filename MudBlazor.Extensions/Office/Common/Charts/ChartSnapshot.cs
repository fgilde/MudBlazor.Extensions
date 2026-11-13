namespace MudBlazor.Extensions.Office;

/// <summary>A chart as the values cached in its file: what a viewer draws without any workbook behind it.</summary>
/// <param name="Kind">The chart type.</param>
/// <param name="Title">The title text; empty for no title.</param>
/// <param name="Categories">The category labels, shared by all series.</param>
/// <param name="Series">The data series in plot order.</param>
/// <param name="ShowLegend">True if the legend is shown.</param>
public sealed record ChartSnapshot(ChartKind Kind, string Title, IReadOnlyList<string> Categories, IReadOnlyList<ChartSnapshotSeries> Series, bool ShowLegend)
{
    /// <summary>True if bars, columns, lines or areas are stacked.</summary>
    public bool Stacked { get; init; }
}

/// <summary>One series of a <see cref="ChartSnapshot"/>.</summary>
/// <param name="Name">The series name.</param>
/// <param name="Values">The values; a point without a number is null.</param>
/// <param name="Color">The fill or line color as #RRGGBB.</param>
public sealed record ChartSnapshotSeries(string Name, IReadOnlyList<double?> Values, string Color);
