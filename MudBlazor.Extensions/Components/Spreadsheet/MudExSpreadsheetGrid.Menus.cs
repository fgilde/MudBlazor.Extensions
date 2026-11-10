using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>A dialog the grid asks its host to open from a keyboard shortcut.</summary>
public enum SpreadsheetDialog
{
    /// <summary>Find (Ctrl+F).</summary>
    Find,

    /// <summary>Replace (Ctrl+H).</summary>
    Replace,

    /// <summary>Go To (Ctrl+G, F5).</summary>
    GoTo,

    /// <summary>Insert Function (Shift+F3).</summary>
    InsertFunction,

    /// <summary>Name Manager (Ctrl+F3).</summary>
    NameManager,

    /// <summary>Insert Hyperlink (Ctrl+K).</summary>
    Hyperlink
}

/// <summary>The part of the grid a context menu was opened on.</summary>
public enum SpreadsheetContextMenuKind
{
    /// <summary>A cell.</summary>
    Cell,

    /// <summary>A row heading.</summary>
    RowHeader,

    /// <summary>A column heading.</summary>
    ColumnHeader
}

/// <summary>Arguments of <see cref="MudExSpreadsheetGrid.ContextMenuRequested"/>.</summary>
/// <param name="Kind">Where the menu was opened.</param>
/// <param name="ClientX">Horizontal pointer position in CSS pixels of the viewport.</param>
/// <param name="ClientY">Vertical pointer position in CSS pixels of the viewport.</param>
public sealed record SpreadsheetContextMenuEventArgs(SpreadsheetContextMenuKind Kind, double ClientX, double ClientY);

public partial class MudExSpreadsheetGrid
{
    /// <summary>Raised on the shortcuts of Find, Replace, Go To, Insert Function and Name Manager so the host can open its dialog.</summary>
    [Parameter] public EventCallback<SpreadsheetDialog> DialogRequested { get; set; }

    /// <summary>Raised on a right click on a cell or heading, after the clicked cell, row or column was selected if it was not.</summary>
    [Parameter] public EventCallback<SpreadsheetContextMenuEventArgs> ContextMenuRequested { get; set; }

    /// <summary>Called by the browser on a right click in the grid.</summary>
    /// <param name="kind"><c>cell</c>, <c>row</c> or <c>column</c>.</param>
    /// <param name="row">The row, or -1 for a column heading.</param>
    /// <param name="col">The column, or -1 for a row heading.</param>
    /// <param name="clientX">Horizontal pointer position.</param>
    /// <param name="clientY">Vertical pointer position.</param>
    /// <returns>A task that completes when the host was told.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnContextMenu(string kind, int row, int col, double clientX, double clientY)
    {
        if (_sheet == null) return;
        if (_editing) await CommitAsync(0, 0, false);
        var menu = kind switch
        {
            "row" => SpreadsheetContextMenuKind.RowHeader,
            "column" => SpreadsheetContextMenuKind.ColumnHeader,
            _ => SpreadsheetContextMenuKind.Cell
        };
        var selected = menu switch
        {
            SpreadsheetContextMenuKind.RowHeader => _selection.Ranges.Any(r => r.IsWholeRows && row >= r.Normalized.Top && row <= r.Normalized.Bottom),
            SpreadsheetContextMenuKind.ColumnHeader => _selection.Ranges.Any(r => r.IsWholeColumns && col >= r.Normalized.Left && col <= r.Normalized.Right),
            _ => _selection.Contains(Clamp(row, col))
        };
        if (!selected)
        {
            if (menu == SpreadsheetContextMenuKind.Cell) await SelectCellAsync(Clamp(row, col));
            else await OnHeaderSelect(menu == SpreadsheetContextMenuKind.ColumnHeader, Math.Max(0, menu == SpreadsheetContextMenuKind.ColumnHeader ? col : row),
                Math.Max(0, menu == SpreadsheetContextMenuKind.ColumnHeader ? col : row), false, false);
            StateHasChanged();
        }

        await ContextMenuRequested.InvokeAsync(new SpreadsheetContextMenuEventArgs(menu, clientX, clientY));
    }

    /// <summary>
    /// Splits a range into the bands of rows that edits hit. While the auto filter hides rows, only visible rows are hit, like Fill,
    /// Delete and Replace All in Excel; otherwise the range is returned unchanged.
    /// </summary>
    internal IEnumerable<CellRange> VisibleParts(CellRange range)
    {
        var r = range.Normalized;
        if (_sheet == null || !_sheet.FilteredRows.Any(x => x >= r.Top && x <= r.Bottom))
        {
            yield return r;
            yield break;
        }

        var last = Math.Min(r.Bottom, _sheet.FilteredRows.Max());
        var start = -1;
        var previous = -2;
        foreach (var row in _sheet.VisibleRowsIn(CellRange.FromBounds(r.Top, r.Left, last, r.Right)))
        {
            if (row != previous + 1)
            {
                if (start >= 0) yield return CellRange.FromBounds(start, r.Left, previous, r.Right);
                start = row;
            }

            previous = row;
        }

        if (start >= 0) yield return CellRange.FromBounds(start, r.Left, previous, r.Right);
        if (last < r.Bottom) yield return CellRange.FromBounds(last + 1, r.Left, r.Bottom, r.Right);
    }

    internal Task PrintAsync(ElementReference element) => Js("printNode", element);

    private bool HidesRow(int row) => _sheet.FilteredRows.Count > 0 && _sheet.IsRowHidden(row);
}
