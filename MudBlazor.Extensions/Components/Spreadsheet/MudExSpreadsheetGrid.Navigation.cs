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
    internal async Task<bool> GoToAsync(string reference, bool define = false)
    {
        if (_sheet == null || string.IsNullOrWhiteSpace(reference)) return false;
        var text = reference.Trim();
        if (_workbook.DefinedNames.TryGetValue(text, out var refersTo)) text = refersTo.TrimStart('=');
        var sheet = _sheet;
        var bang = text.LastIndexOf('!');
        if (bang > 0)
        {
            sheet = _workbook.FindSheet(text[..bang].Trim('\'').Replace("''", "'"));
            text = text[(bang + 1)..];
        }

        if (sheet != null && CellRange.TryParse(text, out var range))
        {
            var sel = new SpreadsheetSelection(range.Start.Relative, [new CellRange(range.Start.Relative, range.End.Relative)]);
            if (sheet == _sheet)
            {
                await SelectAsync(sel);
                StateHasChanged();
            }
            else
            {
                _sheetSelections[sheet] = sel;
                await SheetIndexChanged.InvokeAsync(_workbook.Sheets.ToList().IndexOf(sheet));
            }

            return true;
        }

        if (!define || !CanEdit || bang > 0 || !IsValidName(text)) return false;
        var r = _selection.Range.Normalized;
        var target = new CellRange(r.Start with { RowAbsolute = true, ColumnAbsolute = true }, r.End with { RowAbsolute = true, ColumnAbsolute = true });
        _workbook.Execute(new DefineNameCommand(text, Workbook.QuoteSheetName(_sheet.Name) + "!" + target.ToA1()));
        NotifyEdit();
        return true;
    }

    /// <summary>Called by the browser after a pointer selection of cells.</summary>
    /// <param name="anchorRow">Row where the drag started.</param>
    /// <param name="anchorCol">Column where the drag started.</param>
    /// <param name="row">Row where the drag ended.</param>
    /// <param name="col">Column where the drag ended.</param>
    /// <param name="extend">True if Shift was held: extends the current range from the active cell.</param>
    /// <param name="add">True if Ctrl was held: adds a range.</param>
    /// <returns>A task that completes when the selection is updated.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnPointerSelect(int anchorRow, int anchorCol, int row, int col, bool extend, bool add)
    {
        if (_sheet == null) return;
        if (_editing) await CommitAsync(0, 0, false);
        var end = Clamp(row, col);
        if (extend)
        {
            var range = Oriented(ExpandMerges(new CellRange(_selection.Active, end)), _selection.Active, end);
            await SelectAsync(new SpreadsheetSelection(_selection.Active, [.. _selection.Ranges.Take(_selection.Ranges.Count - 1), range]), end);
        }
        else
        {
            var anchor = Clamp(anchorRow, anchorCol);
            var merge = MergeAt(anchor);
            var active = merge?.Start ?? anchor;
            var range = Oriented(ExpandMerges(new CellRange(anchor, end)), anchor, end);
            await SelectAsync(add
                ? new SpreadsheetSelection(active, [.. _selection.Ranges, range])
                : new SpreadsheetSelection(active, [range]), end);
        }

        StateHasChanged();
    }

    /// <summary>Called by the browser after a click or drag on column or row headings.</summary>
    /// <param name="columns">True for column headings, false for row headings.</param>
    /// <param name="from">First heading index.</param>
    /// <param name="to">Last heading index.</param>
    /// <param name="extend">True if Shift was held.</param>
    /// <param name="add">True if Ctrl was held.</param>
    /// <returns>A task that completes when the selection is updated.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnHeaderSelect(bool columns, int from, int to, bool extend, bool add)
    {
        if (_sheet == null) return;
        if (_editing) await CommitAsync(0, 0, false);
        var top = TopVisibleRow();
        var left = LeftVisibleColumn();
        if (extend) from = columns ? _selection.Active.Column : _selection.Active.Row;
        var range = columns
            ? new CellRange(new CellRef(0, from), new CellRef(CellRef.MaxRows - 1, to))
            : new CellRange(new CellRef(from, 0), new CellRef(to, CellRef.MaxColumns - 1));
        var active = extend ? _selection.Active : columns ? new CellRef(top, from) : new CellRef(from, left);
        var ranges = add ? [.. _selection.Ranges, range] : extend ? [.. _selection.Ranges.Take(_selection.Ranges.Count - 1), range] : new List<CellRange> { range };
        await SelectAsync(new SpreadsheetSelection(active, ranges), columns ? new CellRef(top, to) : new CellRef(to, left));
        StateHasChanged();
    }

    /// <summary>Called by the browser when the select-all corner is clicked.</summary>
    /// <returns>A task that completes when the selection is updated.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnSelectAll()
    {
        if (_sheet == null) return;
        if (_editing) await CommitAsync(0, 0, false);
        await SelectAsync(new SpreadsheetSelection(_selection.Active, [CellRange.FromBounds(0, 0, CellRef.MaxRows - 1, CellRef.MaxColumns - 1)]), _selection.Active);
        StateHasChanged();
    }

    /// <summary>Called by the browser after a double click on a cell: starts editing it.</summary>
    /// <param name="row">The row.</param>
    /// <param name="col">The column.</param>
    /// <returns>A task that completes when the editor is shown.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnCellDoubleClick(int row, int col)
    {
        if (_sheet == null) return;
        if (!_selection.Contains(new CellRef(row, col)) || _selection.Ranges.Count > 1) await SelectCellAsync(Clamp(row, col));
        await BeginEditAsync();
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        try
        {
            await OnKeyDownCoreAsync(e);
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex);
        }
    }

    private async Task OnKeyDownCoreAsync(KeyboardEventArgs e)
    {
        if (_sheet == null) return;
        if (_editing)
        {
            await OnEditKeyAsync(e);
            return;
        }

        if (_selectedChart != null && await ChartKeyAsync(e)) return;
        var ctrl = e.CtrlKey || e.MetaKey;
        var key = e.Key ?? "";
        switch (key)
        {
            case "ArrowUp":
                await MoveAsync(-1, 0, ctrl, e.ShiftKey);
                return;
            case "ArrowDown":
                await MoveAsync(1, 0, ctrl, e.ShiftKey);
                return;
            case "ArrowLeft":
                await MoveAsync(0, -1, ctrl, e.ShiftKey);
                return;
            case "ArrowRight":
                await MoveAsync(0, 1, ctrl, e.ShiftKey);
                return;
            case "Home":
                await MoveToAsync(ctrl ? new CellRef(FrozenRows, FrozenColumns) : new CellRef(_selection.Active.Row, 0), e.ShiftKey);
                return;
            case "End" when ctrl:
                await MoveToAsync(new CellRef(_sheet.UsedRange.Bottom, _sheet.UsedRange.Right), e.ShiftKey);
                return;
            case "PageDown" or "PageUp":
                await PageAsync(key == "PageDown" ? 1 : -1, e.AltKey, e.ShiftKey);
                return;
            case "Tab":
                await StepWithinSelectionAsync(0, e.ShiftKey ? -1 : 1);
                return;
            case "Enter":
                await StepWithinSelectionAsync(e.ShiftKey ? -1 : 1, 0);
                return;
            case "F2":
                await BeginEditAsync();
                return;
            case "F9":
                _engine?.RecalculateAll();
                return;
            case "F5":
                await DialogRequested.InvokeAsync(SpreadsheetDialog.GoTo);
                return;
            case "F3" when ctrl || e.ShiftKey:
                await DialogRequested.InvokeAsync(ctrl ? SpreadsheetDialog.NameManager : SpreadsheetDialog.InsertFunction);
                return;
            case "Delete":
                ClearContents();
                return;
            case "Backspace":
                await BeginEditAsync("");
                return;
            case "Escape":
                _clipboard = null;
                return;
            case " " when ctrl || e.ShiftKey:
                await SelectEntireAsync(ctrl, e.ShiftKey);
                return;
            case "=" when e.AltKey && !ctrl:
                await AutoSumAsync();
                return;
        }

        if (ctrl && !e.AltKey)
        {
            await CommandKeyAsync(key, e.ShiftKey);
            return;
        }

        if (key.Length == 1 && !(e.AltKey && !ctrl)) await BeginEditAsync(key);
    }

    private async Task CommandKeyAsync(string key, bool shift)
    {
        switch (key.ToLowerInvariant())
        {
            case "a":
                await SelectAllAsync();
                break;
            case "z" when shift:
            case "y":
                if (CanEdit && _workbook.Undo.CanRedo) _workbook.Undo.Redo();
                break;
            case "z":
                if (CanEdit && _workbook.Undo.CanUndo) _workbook.Undo.Undo();
                break;
            case "c":
                await CopyAsync();
                break;
            case "x":
                await CutAsync();
                break;
            case "d":
                FillDown();
                break;
            case "r":
                FillRight();
                break;
            case "b":
                ToggleFont(f => f.Bold, (f, on) => f with { Bold = on }, "Bold");
                break;
            case "i":
                ToggleFont(f => f.Italic, (f, on) => f with { Italic = on }, "Italic");
                break;
            case "u":
                ToggleFont(f => f.Underline, (f, on) => f with { Underline = on }, "Underline");
                break;
            case "5" when !shift:
                ToggleFont(f => f.Strikethrough, (f, on) => f with { Strikethrough = on }, "Strikethrough");
                break;
            case "%" or "5":
                ApplyStyle(s => s with { NumberFormat = "0%" }, "Percent Style");
                break;
            case "s":
                await SaveKeyPressed.InvokeAsync();
                break;
            case "1":
                await FormatCellsRequested.InvokeAsync();
                break;
            case "f":
                await DialogRequested.InvokeAsync(SpreadsheetDialog.Find);
                break;
            case "h":
                await DialogRequested.InvokeAsync(SpreadsheetDialog.Replace);
                break;
            case "g":
                await DialogRequested.InvokeAsync(SpreadsheetDialog.GoTo);
                break;
            case "k":
                await DialogRequested.InvokeAsync(SpreadsheetDialog.Hyperlink);
                break;
            case ";":
                await BeginEditAsync(DateTime.Today.ToString(Culture.DateTimeFormat.ShortDatePattern, Culture));
                break;
            case ":":
                await BeginEditAsync(DateTime.Now.ToString(Culture.DateTimeFormat.ShortTimePattern, Culture));
                break;
        }
    }

    private async Task SelectAsync(SpreadsheetSelection sel, CellRef? reveal = null)
    {
        var changed = !sel.Equals(_selection);
        var before = _selection;
        _selectedChart = null;
        _selection = sel;
        Selection = sel;
        Reveal(reveal ?? sel.Active);
        if (changed) BumpSelection(before);
        NotifyEdit();
        if (changed) await SelectionChanged.InvokeAsync(sel);
    }

    private void Reveal(CellRef cell)
    {
        if (_sheet == null) return;
        if (cell.Row >= _rowExtent - RowBuffer || cell.Column >= _colExtent - ColumnBuffer)
        {
            _maxRow = Math.Max(_maxRow, cell.Row);
            _maxCol = Math.Max(_maxCol, cell.Column);
            Relayout();
        }

        var stickyTop = Y(FrozenRows);
        var stickyLeft = X(FrozenColumns);
        var range = MergeAt(cell) ?? new CellRange(cell, cell);
        double x = -1, y = -1;
        if (range.Top >= FrozenRows)
        {
            y = Y(range.Top);
            var bottom = Y(range.Bottom + 1);
            if (y - _scrollTop < stickyTop) _scrollTop = Math.Max(0, y - stickyTop);
            else if (bottom - _scrollTop > _viewHeight) _scrollTop = Math.Min(y - stickyTop, bottom - _viewHeight);
        }

        if (range.Left >= FrozenColumns)
        {
            x = X(range.Left);
            var right = X(range.Right + 1);
            if (x - _scrollLeft < stickyLeft) _scrollLeft = Math.Max(0, x - stickyLeft);
            else if (right - _scrollLeft > _viewWidth) _scrollLeft = Math.Min(x - stickyLeft, right - _viewWidth);
        }

        UpdateWindow();
        _reveal = [x, y, X(range.Right + 1) - X(range.Left), Y(range.Bottom + 1) - Y(range.Top), stickyLeft, stickyTop];
    }

    private void BumpSelection(SpreadsheetSelection before)
    {
        foreach (var sel in new[] { before, _selection })
        {
            Bump(sel.Active.Row);
            foreach (var r in sel.Ranges) BumpRows(r);
        }
    }

    private Task SelectCellAsync(CellRef cell)
    {
        var merge = MergeAt(cell);
        return SelectAsync(merge is { } m ? new SpreadsheetSelection(m.Start, [m]) : SpreadsheetSelection.Single(cell), cell);
    }

    private async Task MoveAsync(int dRow, int dCol, bool jump, bool extend)
    {
        if (extend)
        {
            var end = _selection.Range.End;
            var next = jump ? Jump(end, dRow, dCol) : Clamp(end.Row + dRow, end.Column + dCol);
            var range = Oriented(ExpandMerges(new CellRange(_selection.Active, next)), _selection.Active, next);
            await SelectAsync(new SpreadsheetSelection(_selection.Active, [.. _selection.Ranges.Take(_selection.Ranges.Count - 1), range]), next);
            return;
        }

        await SelectCellAsync(jump ? Jump(_selection.Active, dRow, dCol) : Step(_selection.Active, dRow, dCol));
    }

    private async Task MoveToAsync(CellRef target, bool extend)
    {
        if (!extend)
        {
            await SelectCellAsync(target);
            return;
        }

        var range = Oriented(ExpandMerges(new CellRange(_selection.Active, target)), _selection.Active, target);
        await SelectAsync(new SpreadsheetSelection(_selection.Active, [.. _selection.Ranges.Take(_selection.Ranges.Count - 1), range]), target);
    }

    private Task PageAsync(int direction, bool horizontal, bool extend)
    {
        var a = extend ? _selection.Range.End : _selection.Active;
        if (horizontal)
        {
            var cols = Math.Max(1, ColumnAt(_scrollLeft + _viewWidth) - ColumnAt(_scrollLeft + X(FrozenColumns)));
            _scrollLeft = Math.Max(0, _scrollLeft + direction * (X(Math.Max(0, a.Column + cols)) - X(a.Column)));
            return MoveToAsync(Clamp(a.Row, a.Column + direction * cols), extend);
        }

        var rows = Math.Max(1, RowAt(_scrollTop + _viewHeight) - RowAt(_scrollTop + Y(FrozenRows)));
        _scrollTop = Math.Max(0, _scrollTop + direction * (Y(Math.Max(0, a.Row + rows)) - Y(a.Row)));
        _scrollRequest = true;
        return MoveToAsync(Clamp(a.Row + direction * rows, a.Column), extend);
    }

    private Task StepWithinSelectionAsync(int dRow, int dCol)
    {
        var sel = _selection;
        var range = sel.Range.Normalized;
        if (sel.Ranges.Count == 1 && !range.IsSingleCell && MergeAt(sel.Active) != range && range.Area <= CellRef.MaxRows)
        {
            int r = sel.Active.Row, c = sel.Active.Column;
            if (dRow != 0)
            {
                r += dRow;
                if (r > range.Bottom) { r = range.Top; c = c + 1 > range.Right ? range.Left : c + 1; }
                else if (r < range.Top) { r = range.Bottom; c = c - 1 < range.Left ? range.Right : c - 1; }
            }
            else
            {
                c += dCol;
                if (c > range.Right) { c = range.Left; r = r + 1 > range.Bottom ? range.Top : r + 1; }
                else if (c < range.Left) { c = range.Right; r = r - 1 < range.Top ? range.Bottom : r - 1; }
            }

            return SelectAsync(sel with { Active = new CellRef(r, c) });
        }

        return SelectCellAsync(Step(sel.Active, dRow, dCol));
    }

    private Task SelectEntireAsync(bool columns, bool rows)
    {
        var r = _selection.Range.Normalized;
        var range = columns && rows
            ? CellRange.FromBounds(0, 0, CellRef.MaxRows - 1, CellRef.MaxColumns - 1)
            : columns
                ? CellRange.FromBounds(0, r.Left, CellRef.MaxRows - 1, r.Right)
                : CellRange.FromBounds(r.Top, 0, r.Bottom, CellRef.MaxColumns - 1);
        return SelectAsync(new SpreadsheetSelection(_selection.Active, [range]), _selection.Active);
    }

    private Task SelectAllAsync()
    {
        var region = CurrentRegion(_selection.Active);
        var all = CellRange.FromBounds(0, 0, CellRef.MaxRows - 1, CellRef.MaxColumns - 1);
        var target = !region.IsSingleCell && _selection.Range.Normalized != region ? region : all;
        return SelectAsync(new SpreadsheetSelection(_selection.Active, [target]), _selection.Active);
    }

    internal CellRange CurrentRegion(CellRef cell)
    {
        int top = cell.Row, left = cell.Column, bottom = cell.Row, right = cell.Column;
        var used = _sheet.UsedRange;
        bool Any(int r0, int c0, int r1, int c1)
        {
            r0 = Math.Max(r0, used.Top);
            c0 = Math.Max(c0, used.Left);
            r1 = Math.Min(r1, used.Bottom);
            c1 = Math.Min(c1, used.Right);
            for (var r = r0; r <= r1; r++)
                for (var c = c0; c <= c1; c++)
                    if (_sheet[new CellRef(r, c)]?.HasContent == true) return true;
            return false;
        }

        var grew = true;
        while (grew)
        {
            grew = false;
            if (top > 0 && Any(top - 1, left - 1, top - 1, right + 1)) { top--; grew = true; }
            if (bottom < CellRef.MaxRows - 1 && Any(bottom + 1, left - 1, bottom + 1, right + 1)) { bottom++; grew = true; }
            if (left > 0 && Any(top - 1, left - 1, bottom + 1, left - 1)) { left--; grew = true; }
            if (right < CellRef.MaxColumns - 1 && Any(top - 1, right + 1, bottom + 1, right + 1)) { right++; grew = true; }
        }

        return CellRange.FromBounds(top, left, bottom, right);
    }

    private CellRef Step(CellRef from, int dRow, int dCol)
    {
        if (MergeAt(from) is { } m)
            from = new CellRef(dRow > 0 ? m.Bottom : dRow < 0 ? m.Top : from.Row, dCol > 0 ? m.Right : dCol < 0 ? m.Left : from.Column);
        int r = from.Row, c = from.Column;
        do
        {
            r += dRow;
            c += dCol;
        } while (r >= 0 && r < CellRef.MaxRows && c >= 0 && c < CellRef.MaxColumns
                 && ((dRow != 0 && _sheet.IsRowHidden(r)) || (dCol != 0 && _sheet.IsColumnHidden(c))));

        return r < 0 || r >= CellRef.MaxRows || c < 0 || c >= CellRef.MaxColumns ? from : new CellRef(r, c);
    }

    private CellRef Jump(CellRef from, int dRow, int dCol)
    {
        bool In(int r, int c) => r >= 0 && r < CellRef.MaxRows && c >= 0 && c < CellRef.MaxColumns;
        bool Has(int r, int c) => _sheet[new CellRef(r, c)]?.HasContent == true;
        int row = from.Row, col = from.Column;
        if (!In(row + dRow, col + dCol)) return from;
        if (Has(row, col) && Has(row + dRow, col + dCol))
        {
            while (In(row + dRow, col + dCol) && Has(row + dRow, col + dCol))
            {
                row += dRow;
                col += dCol;
            }

            return new CellRef(row, col);
        }

        var used = _sheet.UsedRange;
        row += dRow;
        col += dCol;
        while (!Has(row, col))
        {
            if (dRow > 0 && row > used.Bottom) return new CellRef(CellRef.MaxRows - 1, col);
            if (dRow < 0 && row < used.Top) return new CellRef(0, col);
            if (dCol > 0 && col > used.Right) return new CellRef(row, CellRef.MaxColumns - 1);
            if (dCol < 0 && col < used.Left) return new CellRef(row, 0);
            if (!In(row + dRow, col + dCol)) return new CellRef(row, col);
            row += dRow;
            col += dCol;
        }

        return new CellRef(row, col);
    }

    private static CellRef Clamp(int row, int col) =>
        new(Math.Clamp(row, 0, CellRef.MaxRows - 1), Math.Clamp(col, 0, CellRef.MaxColumns - 1));

    private CellRange ExpandMerges(CellRange range)
    {
        var r = range.Normalized;
        var grew = true;
        while (grew)
        {
            grew = false;
            foreach (var m in _sheet.Merges)
            {
                if (!m.Intersects(r)) continue;
                var u = CellRange.FromBounds(Math.Min(r.Top, m.Top), Math.Min(r.Left, m.Left), Math.Max(r.Bottom, m.Bottom), Math.Max(r.Right, m.Right));
                if (u == r) continue;
                r = u;
                grew = true;
            }
        }

        return r;
    }

    private static CellRange Oriented(CellRange bounds, CellRef anchor, CellRef end)
    {
        var down = anchor.Row <= end.Row;
        var right = anchor.Column <= end.Column;
        return new CellRange(new CellRef(down ? bounds.Top : bounds.Bottom, right ? bounds.Left : bounds.Right),
            new CellRef(down ? bounds.Bottom : bounds.Top, right ? bounds.Right : bounds.Left));
    }

    private int TopVisibleRow() => Math.Max(FrozenRows, RowAt(_scrollTop + Y(FrozenRows)));

    private int LeftVisibleColumn() => Math.Max(FrozenColumns, ColumnAt(_scrollLeft + X(FrozenColumns)));
}
