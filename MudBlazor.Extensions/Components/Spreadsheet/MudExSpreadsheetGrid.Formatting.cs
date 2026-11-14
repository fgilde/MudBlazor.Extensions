using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

public partial class MudExSpreadsheetGrid
{
    /// <summary>Sets the width of each selected column to fit its content, like AutoFit Column Width.</summary>
    /// <returns>A task that completes when the widths are set.</returns>
    public async Task AutoFitColumnsAsync()
    {
        if (!CanEdit || JsReference is not { } js) return;
        var used = _sheet.UsedRange;
        var widths = new List<(int Column, double Width)>();
        foreach (var col in _selection.Ranges.SelectMany(r => Enumerable.Range(r.Left, Math.Max(0, Math.Min(r.Right, used.Right) - r.Left + 1))).Distinct())
        {
            var candidates = AutoFitColumnCandidates(col);
            if (candidates.Count > 0) widths.Add((col, await js.InvokeAsync<double>("measure", candidates)));
        }

        if (widths.Count == 0) return;
        using (_workbook.Undo.BeginTransaction("Column width"))
            foreach (var (col, width) in widths)
                _workbook.Execute(new ResizeCommand(_sheet, Axis.Column, col, 1, Math.Ceiling(width + 10)));
        StateHasChanged();
    }

    /// <summary>Changes the style of every selected range in one undo step.</summary>
    /// <param name="change">Maps the current style of a cell to its new style.</param>
    /// <param name="description">Description of the undo step.</param>
    public void ApplyStyle(Func<CellStyle, CellStyle> change, string description = "Format cells")
    {
        if (!CanEdit) return;
        using (_workbook.Undo.BeginTransaction(description))
            foreach (var r in _selection.Ranges)
                _workbook.Execute(new SetStyleCommand(_sheet, r.Normalized, change));
    }

    /// <summary>Called by the browser after a column was resized by dragging its heading border.</summary>
    /// <param name="col">The column.</param>
    /// <param name="px">The new width in screen pixels.</param>
    /// <returns>A task that completes when the width is set.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task ResizeColumn(int col, double px) => ResizeAsync(Axis.Column, col, px);

    /// <summary>Called by the browser after a row was resized by dragging its heading border.</summary>
    /// <param name="row">The row.</param>
    /// <param name="px">The new height in screen pixels.</param>
    /// <returns>A task that completes when the height is set.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task ResizeRow(int row, double px) => ResizeAsync(Axis.Row, row, px);

    /// <summary>Called by the browser after a double click on a row heading border: restores the default height.</summary>
    /// <param name="row">The row.</param>
    /// <returns>A task that completes when the height is reset.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task AutoFitRow(int row) => ResizeAsync(Axis.Row, row, -1);

    /// <summary>Returns the longest texts of a column with their CSS font so the browser can measure the auto-fit width.</summary>
    /// <param name="col">The column.</param>
    /// <returns>Pairs of text and font.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public List<string[]> AutoFitColumnCandidates(int col)
    {
        if (_sheet == null || !CanEdit) return [];
        return _sheet.UsedCells()
            .Where(u => u.Ref.Column == col && u.Cell.HasContent && MergeAt(u.Ref) == null)
            .Select(u => (Text: DisplayText(u.Ref), u.Ref))
            .OrderByDescending(u => u.Text.Length)
            .Take(50)
            .Select(u => new[] { u.Text, FontShorthand(_sheet.EffectiveStyleId(u.Ref)) })
            .ToList();
    }

    private async Task ResizeAsync(Axis axis, int index, double px)
    {
        if (!CanEdit || index < 0) return;
        double? size = px < 0 ? null : Math.Round(Math.Max(0, px / _zoom), 2);
        var ranges = _selection.Ranges.Where(r => axis == Axis.Column ? r.IsWholeColumns && index >= r.Left && index <= r.Right : r.IsWholeRows && index >= r.Top && index <= r.Bottom).ToList();
        await TryWriteAsync(() =>
        {
            using (_workbook.Undo.BeginTransaction(axis == Axis.Column ? "Column width" : "Row height"))
            {
                if (ranges.Count == 0) _workbook.Execute(new ResizeCommand(_sheet, axis, index, 1, size));
                foreach (var r in ranges)
                    _workbook.Execute(axis == Axis.Column
                        ? new ResizeCommand(_sheet, axis, r.Left, r.ColumnCount, size)
                        : new ResizeCommand(_sheet, axis, r.Top, r.RowCount, size));
            }
        });

        await InvokeAsync(StateHasChanged);
    }

    private void ClearContents()
    {
        if (!CanEdit) return;
        using (_workbook.Undo.BeginTransaction("Clear contents"))
            foreach (var r in _selection.Ranges.SelectMany(VisibleParts))
                _workbook.Execute(new ClearCommand(_sheet, r));
    }

    private void ToggleFont(Func<CellFont, bool> get, Func<CellFont, bool, CellFont> set, string description)
    {
        if (!CanEdit) return;
        var on = !get(_workbook.Styles[_sheet.EffectiveStyleId(_selection.Active)].Font);
        ApplyStyle(s => s with { Font = set(s.Font, on) }, description);
    }
}
