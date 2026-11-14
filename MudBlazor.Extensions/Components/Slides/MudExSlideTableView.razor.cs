using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Shows a table of a slide as a grid inside its shape: columns and rows in the proportions of the file, cell fills and text colours from
/// the table style (Medium Style 2 in the accent colour of the style) unless a cell has its own fill, and white 1-pt lines between the cells.
/// </summary>
public partial class MudExSlideTableView
{
    /// <summary>The table to show.</summary>
    [Parameter] public TableShape Table { get; set; }

    /// <summary>The presentation that defines the slide size.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The master whose theme colours the table uses.</summary>
    [Parameter] public SlideMaster Master { get; set; }

    /// <summary>The number shown by slide number fields.</summary>
    [Parameter] public int SlideNumber { get; set; }

    /// <summary>A cell whose text is not shown, because it is being edited above the slide.</summary>
    [Parameter] public (int Row, int Column)? HiddenCell { get; set; }

    private string GridStyle =>
        $"grid-template-columns:{Tracks(Table.ColumnWidths)};grid-template-rows:{Tracks(Table.Rows.Select(r => r.Height))};" +
        $"gap:{MudExSlideShapeView.Num(SlideTableStyles.LineWidth / (double)Presentation.SlideWidth * 100)}cqw;" +
        (SlideTableStyles.LineColor(Table, Master) is { } line ? $"background-color:{line};" : "");

    private static string CellStyle(int row, int column, TableCell cell, SlideTableCellStyle style) =>
        $"grid-row:{row + 1} / span {cell.RowSpan};grid-column:{column + 1} / span {cell.ColumnSpan};background-color:{style.Fill ?? "transparent"};";

    private static string Tracks(IEnumerable<long> sizes) => string.Join(' ', sizes.Select(s => $"minmax(0,{Math.Max(0, s)}fr)"));
}
