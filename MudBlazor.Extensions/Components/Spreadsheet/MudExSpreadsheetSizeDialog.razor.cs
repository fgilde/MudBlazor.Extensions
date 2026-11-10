using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;
using SheetAxis = MudBlazor.Extensions.Office.Spreadsheet.Axis;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Row Height and Column Width dialog of <see cref="MudExSpreadsheetEditor"/>: row heights in points, column widths in characters like
/// Excel. Closes with a <see cref="ResizeCommand"/> as result data.
/// </summary>
public partial class MudExSpreadsheetSizeDialog
{
    private double _value;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The sheet.</summary>
    [Parameter] public Worksheet Sheet { get; set; }

    /// <summary>Rows or columns.</summary>
    [Parameter] public SheetAxis Axis { get; set; }

    /// <summary>The first row or column to resize.</summary>
    [Parameter] public int Index { get; set; }

    /// <summary>The number of rows or columns to resize.</summary>
    [Parameter] public int Count { get; set; } = 1;

    private bool Rows => Axis == SheetAxis.Row;

    private string Label => Rows ? "Row height:" : "Column width:";

    private double Max => Rows ? 409 : 255;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _value = Math.Round(Rows ? Sheet.RowHeight(Index) * 72 / 96 : Math.Max(0, (Sheet.ColumnWidth(Index) - 5) / 7), 2);
    }

    private void Ok()
    {
        var size = Rows ? Math.Round(_value * 96 / 72, 2) : _value == 0 ? 0 : Math.Round(_value * 7 + 5);
        MudDialog.Close(DialogResult.Ok<IWorkbookCommand>(new ResizeCommand(Sheet, Axis, Index, Math.Max(1, Count), size)));
    }

    private void Cancel() => MudDialog.Cancel();
}
