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
    private const long MaxBulkCells = 100_000;
    private static readonly Regex NumberSuffix = new(@"^(.*?)(\d+)$", RegexOptions.CultureInvariant);

    private bool _editing;
    private string _editText = "";
    private string _editOriginal = "";
    private CellRef _editCell;
    private bool _editMode;
    private bool _editFromBar;
    private FormulaAssistResult _assist = FormulaAssistResult.None;
    private int _assistIndex;
    private bool _assistDismissed;
    private ClipboardSource _clipboard;

    internal bool EditFromBar => _editing && _editFromBar;

    internal FormulaAssistResult Assist => _editing && !_assistDismissed ? _assist : FormulaAssistResult.None;

    internal int AssistIndex => _assistIndex;

    /// <summary>Starts editing the active cell.</summary>
    /// <param name="initialText">Text that replaces the content (as when typing), or null to edit the current content (as with F2).</param>
    /// <returns>A task that completes when the editor is shown.</returns>
    public Task BeginEditAsync(string initialText = null) => BeginEditCoreAsync(initialText, false);

    /// <summary>Commits the edit to the active cell without moving the selection.</summary>
    /// <returns>A task that completes when the value is written.</returns>
    public Task CommitEditAsync() => CommitAsync(0, 0, false);

    /// <summary>Discards the edit.</summary>
    /// <returns>A task that completes when the editor is hidden.</returns>
    public async Task CancelEditAsync()
    {
        if (!_editing) return;
        EndEditState();
        await Js("endEdit", true);
        StateHasChanged();
    }

    /// <summary>Copies the current range as tab separated text to the clipboard.</summary>
    /// <returns>A task that completes when the text is handed to the browser.</returns>
    public Task CopyAsync() => CopyCoreAsync(false);

    /// <summary>Copies the current range and moves it on the next paste.</summary>
    /// <returns>A task that completes when the text is handed to the browser.</returns>
    public Task CutAsync() => CopyCoreAsync(CanEdit);

    /// <summary>
    /// Pastes text at the top-left cell of the selection. Tab separated text fills a range; each value is parsed like typed input.
    /// Text that was copied from this grid pastes the original cells with formulas, adjusting relative references.
    /// </summary>
    /// <param name="text">The clipboard text.</param>
    /// <returns>A task that completes when the cells are written.</returns>
    [JSInvokable]
    public async Task PasteTextAsync(string text)
    {
        if (!CanEdit || string.IsNullOrEmpty(text)) return;
        var origin = _selection.Range.Normalized;
        CellRange target;
        if (_clipboard is { } clip && clip.Text == text && _workbook.Sheets.Contains(clip.Sheet))
        {
            target = CellRange.FromBounds(origin.Top, origin.Left, Math.Min(CellRef.MaxRows - 1, origin.Top + clip.Range.RowCount - 1),
                Math.Min(CellRef.MaxColumns - 1, origin.Left + clip.Range.ColumnCount - 1));
            using (_workbook.Undo.BeginTransaction(clip.Cut ? "Move" : "Paste"))
            {
                foreach (var s in clip.Range.Cells())
                {
                    var t = new CellRef(s.Row - clip.Range.Top + target.Top, s.Column - clip.Range.Left + target.Left);
                    if (target.Contains(t)) CopyCell(clip.Sheet, s, t);
                }

                if (clip.Cut)
                    foreach (var s in clip.Range.Cells())
                        if (clip.Sheet != _sheet || !target.Contains(s))
                            ClearCell(clip.Sheet, s, true);
            }

            if (clip.Cut) _clipboard = null;
        }
        else
        {
            var rows = ParseTabular(text);
            if (rows.Count == 0) return;
            var width = rows.Max(r => r.Count);
            target = CellRange.FromBounds(origin.Top, origin.Left, Math.Min(CellRef.MaxRows - 1, origin.Top + rows.Count - 1),
                Math.Min(CellRef.MaxColumns - 1, origin.Left + width - 1));
            using (_workbook.Undo.BeginTransaction("Paste"))
                for (var i = 0; i < rows.Count; i++)
                    for (var j = 0; j < rows[i].Count; j++)
                    {
                        var t = new CellRef(origin.Top + i, origin.Left + j);
                        if (target.Contains(t)) WriteInput(t, rows[i][j]);
                    }
        }

        await SelectAsync(new SpreadsheetSelection(target.Start, [target]), target.Start);
        StateHasChanged();
    }

    /// <summary>
    /// Extends the current range to <paramref name="target"/> like dragging the fill handle: numbers, dates and text ending in a
    /// number continue as a linear series, everything else is copied with relative references adjusted.
    /// </summary>
    /// <param name="target">The range to fill; it must contain the current range and extend it in one direction.</param>
    /// <returns>A task that completes when the cells are written.</returns>
    public async Task FillToAsync(CellRange target)
    {
        var src = _selection.Range.Normalized;
        target = target.Normalized;
        if (!CanEdit || target == src || !target.Contains(src.Start) || !target.Contains(src.End)) return;
        var vertical = target.Left == src.Left && target.Right == src.Right;
        if (!vertical && (target.Top != src.Top || target.Bottom != src.Bottom)) return;
        if (target.Area > MaxBulkCells) return;
        using (_workbook.Undo.BeginTransaction("Fill"))
        {
            var lines = vertical ? src.ColumnCount : src.RowCount;
            for (var line = 0; line < lines; line++)
            {
                var seq = vertical
                    ? Enumerable.Range(src.Top, src.RowCount).Select(r => new CellRef(r, src.Left + line)).ToList()
                    : Enumerable.Range(src.Left, src.ColumnCount).Select(c => new CellRef(src.Top + line, c)).ToList();
                var series = Series(seq);
                var from = vertical ? target.Top : target.Left;
                var to = vertical ? target.Bottom : target.Right;
                var start = vertical ? src.Top : src.Left;
                for (var i = from; i <= to; i++)
                {
                    var k = i - start;
                    if (k >= 0 && k < seq.Count) continue;
                    var t = vertical ? new CellRef(i, src.Left + line) : new CellRef(src.Top + line, i);
                    var s = seq[((k % seq.Count) + seq.Count) % seq.Count];
                    if (series != null)
                    {
                        var (prefix, first, step, digits) = series.Value;
                        var n = first + step * k;
                        _workbook.Execute(new SetValueCommand(_sheet, t, prefix == null
                            ? CellValue.Number(n)
                            : CellValue.Text(prefix + Math.Abs(n).ToString(new string('0', digits), Culture))));
                        CopyStyle(_sheet, s, t);
                    }
                    else
                    {
                        CopyCell(_sheet, s, t);
                    }
                }
            }
        }

        await SelectAsync(new SpreadsheetSelection(_selection.Active, [target]), _selection.Active);
        StateHasChanged();
    }

    /// <summary>Fills the selection down from its top row (or the cell above a single selected row), like Ctrl+D.</summary>
    public void FillDown() => FillFromEdge(true);

    /// <summary>Fills the selection right from its left column (or the cell left of a single selected column), like Ctrl+R.</summary>
    public void FillRight() => FillFromEdge(false);

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

    /// <summary>
    /// Starts editing the active cell with a function over the adjacent numbers above (or else to the left), like AutoSum (Alt+=).
    /// </summary>
    /// <param name="function">The function name, for example SUM, AVERAGE, COUNT, MAX or MIN.</param>
    /// <returns>A task that completes when the editor is shown.</returns>
    public Task AutoSumAsync(string function = "SUM")
    {
        if (!CanEdit) return Task.CompletedTask;
        var a = _selection.Active;
        bool IsNumber(int r, int c) => _sheet[new CellRef(r, c)]?.Value.Kind == CellValueKind.Number;
        var r = a.Row - 1;
        while (r >= 0 && IsNumber(r, a.Column)) r--;
        CellRange? range = r < a.Row - 1 ? CellRange.FromBounds(r + 1, a.Column, a.Row - 1, a.Column) : null;
        if (range == null)
        {
            var c = a.Column - 1;
            while (c >= 0 && IsNumber(a.Row, c)) c--;
            if (c < a.Column - 1) range = CellRange.FromBounds(a.Row, c + 1, a.Row, a.Column - 1);
        }

        return BeginEditAsync($"={function}({range?.ToA1()})");
    }

    /// <summary>Jumps to a cell, a range or a defined name. An unknown valid name is defined for the current selection.</summary>
    /// <param name="reference">For example <c>B7</c>, <c>A1:C3</c>, <c>Sheet2!A1</c> or a defined name.</param>
    /// <returns>A task that completes when the selection changed.</returns>
    internal async Task GoToAsync(string reference)
    {
        if (_sheet == null || string.IsNullOrWhiteSpace(reference)) return;
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

            return;
        }

        if (!CanEdit || bang > 0 || !IsValidName(text)) return;
        var r = _selection.Range.Normalized;
        var target = new CellRange(r.Start with { RowAbsolute = true, ColumnAbsolute = true }, r.End with { RowAbsolute = true, ColumnAbsolute = true });
        _workbook.Execute(new DefineNameCommand(text, Workbook.QuoteSheetName(_sheet.Name) + "!" + target.ToA1()));
        NotifyEdit();
    }

    internal string NameBoxText
    {
        get
        {
            if (_sheet == null) return "";
            var r = _selection.Range.Normalized;
            foreach (var (name, refersTo) in _workbook.DefinedNames)
            {
                var text = refersTo.TrimStart('=');
                var bang = text.LastIndexOf('!');
                if (bang > 0 && _workbook.FindSheet(text[..bang].Trim('\'').Replace("''", "'")) == _sheet
                             && CellRange.TryParse(text[(bang + 1)..], out var range) && range.Normalized == r && !r.IsSingleCell)
                    return name;
            }

            return _selection.Active.ToA1();
        }
    }

    internal (string Text, bool Ghost) BarText
    {
        get
        {
            if (_sheet == null) return ("", false);
            if (_editing) return (_editText, false);
            var cell = _sheet[_selection.Active];
            if (cell?.Formula == null && cell?.SpillAnchor is { } anchor && _sheet[anchor]?.Formula is { } f) return ("=" + f, true);
            return (CellEditText(_selection.Active), false);
        }
    }

    internal async Task BeginEditFromBarAsync()
    {
        if (!CanEdit) return;
        if (_editing)
        {
            _editFromBar = true;
            NotifyEdit();
            return;
        }

        _editFromBar = true;
        await BeginEditCoreAsync(null, true);
    }

    internal async Task SetEditTextFromBarAsync(string text)
    {
        if (!_editing) return;
        _editText = text ?? "";
        _assistIndex = 0;
        _assistDismissed = false;
        NotifyEdit();
        await Js("setEditText", _editText, false);
    }

    internal async Task CommitFromBarAsync(int dRow, int dCol)
    {
        await CommitAsync(dRow, dCol, false);
        await FocusAsync();
    }

    internal async Task CancelFromBarAsync()
    {
        await CancelEditAsync();
        await FocusAsync();
    }

    internal async Task AcceptSuggestionAsync(int index)
    {
        var assist = Assist;
        if (index < 0 || index >= assist.Suggestions.Count) return;
        _editText = assist.Accept(_editText, index);
        _assistIndex = 0;
        NotifyEdit();
        await Js("setEditText", _editText, !_editFromBar);
    }

    internal void BuildAssist(RenderTreeBuilder b, object receiver)
    {
        var assist = Assist;
        if (assist.Suggestions.Count > 0)
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", "mud-ex-ssg-assist");
            b.AddAttribute(2, "role", "listbox");
            for (var i = 0; i < assist.Suggestions.Count; i++)
            {
                var index = i;
                var s = assist.Suggestions[i];
                b.OpenElement(3, "div");
                b.SetKey(s.Name);
                b.AddAttribute(4, "class", i == _assistIndex ? "mud-ex-ssg-assist-item mud-ex-ssg-assist-active" : "mud-ex-ssg-assist-item");
                b.AddAttribute(5, "role", "option");
                b.AddAttribute(6, "aria-selected", i == _assistIndex ? "true" : "false");
                b.AddAttribute(7, "title", s.Description);
                b.AddAttribute(8, "onmousedown", EventCallback.Factory.Create<MouseEventArgs>(receiver, () => AcceptSuggestionAsync(index)));
                b.AddEventPreventDefaultAttribute(9, "onmousedown", true);
                b.OpenElement(10, "span");
                b.AddAttribute(11, "class", s.IsName ? "mud-ex-ssg-assist-name" : "mud-ex-ssg-assist-fn");
                b.AddContent(12, s.Name);
                b.CloseElement();
                b.CloseElement();
            }

            b.CloseElement();
            return;
        }

        if (assist.Function == null) return;
        var (before, current, after) = assist.SignatureParts();
        b.OpenElement(13, "div");
        b.AddAttribute(14, "class", "mud-ex-ssg-signature");
        b.AddContent(15, before);
        b.OpenElement(16, "b");
        b.AddContent(17, current);
        b.CloseElement();
        b.AddContent(18, after);
        b.CloseElement();
    }

    internal void BuildReferences(RenderTreeBuilder b)
    {
        if (!_editing || _editFromBar || _sheet == null || _editText.Length < 2 || _editText[0] != '=') return;
        List<FormulaToken> tokens;
        try
        {
            tokens = FormulaLexer.Tokenize(_editText, true);
        }
        catch (FormatException)
        {
            return;
        }

        var n = 0;
        var window = CellRange.FromBounds(0, 0, _lastRow, _lastCol);
        foreach (var t in tokens)
        {
            if (t.Kind != FormulaTokenKind.Reference) continue;
            var text = t.Text.TrimEnd('#');
            var bang = text.LastIndexOf('!');
            if (bang > 0 && _workbook.FindSheet(text[..bang].Trim('\'').Replace("''", "'")) != _sheet) continue;
            if (!CellRange.TryParse(text[(bang + 1)..], out var range)) continue;
            var r = range.Normalized;
            var color = n++ % 7;
            if (!r.Intersects(window)) continue;
            int top = r.Top, left = r.Left, bottom = Math.Min(r.Bottom, _lastRow), right = Math.Min(r.Right, _lastCol);
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", $"mud-ex-ssg-ref mud-ex-ssg-ref-{color}");
            b.AddAttribute(2, "style", $"left:{Px(X(left) - 1)};top:{Px(Y(top) - 1)};width:{Px(X(right + 1) - X(left) + 1)};height:{Px(Y(bottom + 1) - Y(top) + 1)}");
            b.CloseElement();
        }
    }

    internal string AssistHost => AssistHostStyle;

    /// <summary>Called by the browser after a pointer selection of cells.</summary>
    /// <param name="anchorRow">Row where the drag started.</param>
    /// <param name="anchorCol">Column where the drag started.</param>
    /// <param name="row">Row where the drag ended.</param>
    /// <param name="col">Column where the drag ended.</param>
    /// <param name="extend">True if Shift was held: extends the current range from the active cell.</param>
    /// <param name="add">True if Ctrl was held: adds a range.</param>
    /// <returns>A task that completes when the selection is updated.</returns>
    [JSInvokable]
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
    [JSInvokable]
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
    [JSInvokable]
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
    [JSInvokable]
    public async Task OnCellDoubleClick(int row, int col)
    {
        if (_sheet == null) return;
        if (!_selection.Contains(new CellRef(row, col)) || _selection.Ranges.Count > 1) await SelectCellAsync(Clamp(row, col));
        await BeginEditAsync();
    }

    /// <summary>Called by the browser after a column was resized by dragging its heading border.</summary>
    /// <param name="col">The column.</param>
    /// <param name="px">The new width in screen pixels.</param>
    /// <returns>A task that completes when the width is set.</returns>
    [JSInvokable]
    public Task ResizeColumn(int col, double px) => ResizeAsync(Axis.Column, col, px);

    /// <summary>Called by the browser after a row was resized by dragging its heading border.</summary>
    /// <param name="row">The row.</param>
    /// <param name="px">The new height in screen pixels.</param>
    /// <returns>A task that completes when the height is set.</returns>
    [JSInvokable]
    public Task ResizeRow(int row, double px) => ResizeAsync(Axis.Row, row, px);

    /// <summary>Called by the browser after a double click on a row heading border: restores the default height.</summary>
    /// <param name="row">The row.</param>
    /// <returns>A task that completes when the height is reset.</returns>
    [JSInvokable]
    public Task AutoFitRow(int row) => ResizeAsync(Axis.Row, row, -1);

    /// <summary>Returns the longest texts of a column with their CSS font so the browser can measure the auto-fit width.</summary>
    /// <param name="col">The column.</param>
    /// <returns>Pairs of text and font.</returns>
    [JSInvokable]
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

    /// <summary>Called by the browser after the fill handle was dragged.</summary>
    /// <param name="top">Top row of the filled range.</param>
    /// <param name="left">Left column of the filled range.</param>
    /// <param name="bottom">Bottom row of the filled range.</param>
    /// <param name="right">Right column of the filled range.</param>
    /// <returns>A task that completes when the cells are written.</returns>
    [JSInvokable]
    public Task FillTo(int top, int left, int bottom, int right) => FillToAsync(CellRange.FromBounds(top, left, bottom, right));

    /// <summary>Called by the browser after a double click on the fill handle: fills down along the data in the adjacent column.</summary>
    /// <returns>A task that completes when the cells are written.</returns>
    [JSInvokable]
    public Task FillHandleDoubleClick()
    {
        var src = _selection.Range.Normalized;
        foreach (var col in new[] { src.Left - 1, src.Right + 1 })
        {
            if (col < 0 || col >= CellRef.MaxColumns) continue;
            var r = src.Bottom;
            while (r + 1 < CellRef.MaxRows && _sheet[new CellRef(r + 1, col)]?.HasContent == true) r++;
            if (r > src.Bottom) return FillToAsync(CellRange.FromBounds(src.Top, src.Left, r, src.Right));
        }

        return Task.CompletedTask;
    }

    private async Task ResizeAsync(Axis axis, int index, double px)
    {
        if (!CanEdit || index < 0) return;
        double? size = px < 0 ? null : Math.Round(Math.Max(0, px / _zoom), 2);
        var ranges = _selection.Ranges.Where(r => axis == Axis.Column ? r.IsWholeColumns && index >= r.Left && index <= r.Right : r.IsWholeRows && index >= r.Top && index <= r.Bottom).ToList();
        using (_workbook.Undo.BeginTransaction(axis == Axis.Column ? "Column width" : "Row height"))
        {
            if (ranges.Count == 0) _workbook.Execute(new ResizeCommand(_sheet, axis, index, 1, size));
            foreach (var r in ranges)
                _workbook.Execute(axis == Axis.Column
                    ? new ResizeCommand(_sheet, axis, r.Left, r.ColumnCount, size)
                    : new ResizeCommand(_sheet, axis, r.Top, r.RowCount, size));
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (_sheet == null) return;
        if (_editing)
        {
            await OnEditKeyAsync(e);
            return;
        }

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
            case "5":
                ToggleFont(f => f.Strikethrough, (f, on) => f with { Strikethrough = on }, "Strikethrough");
                break;
            case "1":
                await FormatCellsRequested.InvokeAsync();
                break;
            case ";":
                await BeginEditAsync(DateTime.Today.ToString(Culture.DateTimeFormat.ShortDatePattern, Culture));
                break;
            case ":":
                await BeginEditAsync(DateTime.Now.ToString(Culture.DateTimeFormat.ShortTimePattern, Culture));
                break;
        }
    }

    private async Task OnEditKeyAsync(KeyboardEventArgs e)
    {
        var ctrl = e.CtrlKey || e.MetaKey;
        var assist = !_editFromBar ? Assist : FormulaAssistResult.None;
        var open = assist.Suggestions.Count > 0;
        switch (e.Key)
        {
            case "Enter" when e.AltKey:
                return;
            case "Enter":
                await CommitAsync(ctrl ? 0 : e.ShiftKey ? -1 : 1, 0, ctrl);
                return;
            case "Tab" when open:
                await AcceptSuggestionAsync(_assistIndex);
                _suppressRender = true;
                return;
            case "Tab":
                await CommitAsync(0, e.ShiftKey ? -1 : 1, false);
                return;
            case "Escape" when open:
                _assistDismissed = true;
                NotifyEdit();
                _suppressRender = true;
                return;
            case "Escape":
                await CancelEditAsync();
                return;
            case "ArrowDown" or "ArrowUp" when open:
                _assistIndex = (_assistIndex + (e.Key == "ArrowDown" ? 1 : -1) + assist.Suggestions.Count) % assist.Suggestions.Count;
                NotifyEdit();
                _suppressRender = true;
                return;
            case "F2":
                _editMode = !_editMode;
                return;
            case ";" when ctrl:
                await Js("insertText", DateTime.Today.ToString(Culture.DateTimeFormat.ShortDatePattern, Culture));
                return;
            case ":" when ctrl:
                await Js("insertText", DateTime.Now.ToString(Culture.DateTimeFormat.ShortTimePattern, Culture));
                return;
        }

        if (_editMode || _editFromBar) return;
        switch (e.Key)
        {
            case "ArrowUp":
                await CommitAsync(-1, 0, false);
                break;
            case "ArrowDown":
                await CommitAsync(1, 0, false);
                break;
            case "ArrowLeft":
                await CommitAsync(0, -1, false);
                break;
            case "ArrowRight":
                await CommitAsync(0, 1, false);
                break;
        }
    }

    private void OnEditorInput(ChangeEventArgs e)
    {
        var text = e.Value?.ToString() ?? "";
        if (!_editing)
        {
            if (!CanEdit)
            {
                _ = Js("endEdit", true);
                return;
            }

            StartEdit(text, false);
            NotifyEdit();
            return;
        }

        _editText = text;
        _assistIndex = 0;
        _assistDismissed = false;
        NotifyEdit();
        _suppressRender = true;
    }

    private async Task BeginEditCoreAsync(string initialText, bool fromBar)
    {
        if (!CanEdit) return;
        if (_editing) EndEditState();
        _editFromBar = fromBar;
        StartEdit(initialText ?? CellEditText(_selection.Active), initialText == null);
        Reveal(_selection.Active);
        NotifyEdit();
        StateHasChanged();
        await Js("beginEdit", _editText, _editMode, !fromBar);
    }

    private void StartEdit(string text, bool editMode)
    {
        _editing = true;
        _editCell = MergeAt(_selection.Active)?.Start ?? _selection.Active;
        _editOriginal = CellEditText(_editCell);
        _editText = text;
        _editMode = editMode;
        _assistIndex = 0;
        _assistDismissed = false;
    }

    private void EndEditState()
    {
        _editing = false;
        _editFromBar = false;
        _editText = "";
        _assist = FormulaAssistResult.None;
        NotifyEdit();
    }

    private async Task CommitAsync(int dRow, int dCol, bool allSelected)
    {
        if (!_editing) return;
        var text = _editText;
        var cell = _editCell;
        var changed = text != _editOriginal;
        EndEditState();
        if (CanEdit && (changed || allSelected))
        {
            _clipboard = null;
            using (_workbook.Undo.BeginTransaction("Typing"))
            {
                if (allSelected && _selection.Ranges.Sum(r => r.Area) <= MaxBulkCells)
                    foreach (var t in _selection.Ranges.SelectMany(r => r.Normalized.Cells()).Distinct())
                        WriteInput(t, text.Length > 1 && text[0] == '='
                            ? "=" + ReferenceShifter.Translate(text[1..], t.Row - cell.Row, t.Column - cell.Column)
                            : text);
                else
                    WriteInput(cell, text);
            }
        }

        await Js("endEdit", true);
        if (dRow != 0 || dCol != 0) await StepWithinSelectionAsync(dRow, dCol);
        StateHasChanged();
    }

    private void WriteInput(CellRef cell, string text)
    {
        if (text.Length > 1 && text[0] == '=')
        {
            _workbook.Execute(new SetFormulaCommand(_sheet, cell, text));
            return;
        }

        if (text.Length == 0)
        {
            if (_sheet[cell]?.HasContent == true) _workbook.Execute(new ClearCommand(_sheet, new CellRange(cell, cell)));
            return;
        }

        var format = _workbook.Styles[_sheet.EffectiveStyleId(cell)].NumberFormat;
        if (format == "@")
        {
            _workbook.Execute(new SetValueCommand(_sheet, cell, CellValue.Text(text)));
            return;
        }

        var (value, suggested) = CellInputParser.Parse(text, Culture);
        _workbook.Execute(new SetValueCommand(_sheet, cell, value));
        if (suggested != null && (string.IsNullOrEmpty(format) || format == NumberFormat.General))
            _workbook.Execute(new SetStyleCommand(_sheet, new CellRange(cell, cell), s => s with { NumberFormat = suggested }));
    }

    internal string CellEditText(CellRef at)
    {
        var cell = _sheet?[at];
        if (cell == null) return "";
        if (cell.Formula != null) return "=" + cell.Formula;
        var v = cell.Value;
        var format = _workbook.Styles[_sheet.EffectiveStyleId(at)].NumberFormat;
        switch (v.Kind)
        {
            case CellValueKind.Number:
                var n = v.NumberValue;
                if (n >= 0 && n < 2958466 && NumberFormat.IsDateFormat(format))
                {
                    var dt = ExcelDate.ToDateTime(n);
                    var date = n >= 1 ? dt.ToString(Culture.DateTimeFormat.ShortDatePattern, Culture) : "";
                    var time = n % 1 != 0 ? dt.ToString(Culture.DateTimeFormat.LongTimePattern, Culture) : "";
                    return (date + " " + time).Trim();
                }

                if (format != null && format.Replace("\"%\"", "").Contains('%')) return (n * 100).ToString("G15", Culture) + "%";
                return n.ToString("G15", Culture);
            case CellValueKind.Boolean:
                return NumberFormat.BooleanText(v.BooleanValue, Culture);
            default:
                return v.ToString();
        }
    }

    private void NotifyEdit()
    {
        _assist = _editing && _editText.StartsWith('=') && _engine != null
            ? FormulaAssist.Analyze(_editText, _editText.Length, _engine.Functions, _workbook.DefinedNames.Keys)
            : FormulaAssistResult.None;
        if (_assistIndex >= _assist.Suggestions.Count) _assistIndex = 0;
        EditChanged?.Invoke();
    }

    private async Task SelectAsync(SpreadsheetSelection sel, CellRef? reveal = null)
    {
        var changed = !sel.Equals(_selection);
        _selection = sel;
        Selection = sel;
        Reveal(reveal ?? sel.Active);
        NotifyEdit();
        if (changed) await SelectionChanged.InvokeAsync(sel);
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

    private CellRange CurrentRegion(CellRef cell)
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

    private void ClearContents()
    {
        if (!CanEdit) return;
        using (_workbook.Undo.BeginTransaction("Clear contents"))
            foreach (var r in _selection.Ranges)
                _workbook.Execute(new ClearCommand(_sheet, r.Normalized));
    }

    private void ToggleFont(Func<CellFont, bool> get, Func<CellFont, bool, CellFont> set, string description)
    {
        if (!CanEdit) return;
        var on = !get(_workbook.Styles[_sheet.EffectiveStyleId(_selection.Active)].Font);
        ApplyStyle(s => s with { Font = set(s.Font, on) }, description);
    }

    private void FillFromEdge(bool down)
    {
        if (!CanEdit) return;
        var r = _selection.Range.Normalized;
        if (r.Area > MaxBulkCells) r = CellRange.FromBounds(r.Top, r.Left, Math.Min(r.Bottom, _sheet.UsedRange.Bottom), Math.Min(r.Right, _sheet.UsedRange.Right));
        var single = down ? r.RowCount == 1 : r.ColumnCount == 1;
        if (single && (down ? r.Top == 0 : r.Left == 0)) return;
        using (_workbook.Undo.BeginTransaction(down ? "Fill down" : "Fill right"))
            foreach (var t in r.Cells())
            {
                var s = down
                    ? new CellRef(single ? r.Top - 1 : r.Top, t.Column)
                    : new CellRef(t.Row, single ? r.Left - 1 : r.Left);
                if (s != t) CopyCell(_sheet, s, t);
            }
    }

    private void CopyCell(Worksheet sheet, CellRef from, CellRef to)
    {
        var src = sheet[from];
        if (src?.Formula != null)
            _workbook.Execute(new SetFormulaCommand(_sheet, to, ReferenceShifter.Translate(src.Formula, to.Row - from.Row, to.Column - from.Column)));
        else if (src?.HasContent == true)
            _workbook.Execute(new SetValueCommand(_sheet, to, src.Value));
        else if (_sheet[to]?.HasContent == true)
            _workbook.Execute(new ClearCommand(_sheet, new CellRange(to, to)));
        CopyStyle(sheet, from, to);
    }

    private void CopyStyle(Worksheet sheet, CellRef from, CellRef to)
    {
        var id = sheet.EffectiveStyleId(from);
        if (id == _sheet.EffectiveStyleId(to)) return;
        var style = _workbook.Styles[id];
        _workbook.Execute(new SetStyleCommand(_sheet, new CellRange(to, to), style));
    }

    private void ClearCell(Worksheet sheet, CellRef cell, bool style)
    {
        if (sheet[cell] != null) _workbook.Execute(new ClearCommand(sheet, new CellRange(cell, cell), style));
    }

    private (string Prefix, double First, double Step, int Digits)? Series(List<CellRef> seq)
    {
        var cells = seq.Select(c => _sheet[c]).ToList();
        if (cells.Any(c => c?.Formula != null || c?.HasContent != true)) return null;
        if (cells.All(c => c.Value.Kind == CellValueKind.Number))
        {
            var first = cells[0].Value.NumberValue;
            if (cells.Count == 1)
                return NumberFormat.IsDateFormat(_workbook.Styles[_sheet.EffectiveStyleId(seq[0])].NumberFormat) ? (null, first, 1, 0) : null;
            return (null, first, (cells[^1].Value.NumberValue - first) / (cells.Count - 1), 0);
        }

        if (!cells.All(c => c.Value.Kind == CellValueKind.Text)) return null;
        var parts = cells.Select(c => NumberSuffix.Match(c.Value.TextValue)).ToList();
        if (parts.Any(p => !p.Success) || parts.Any(p => p.Groups[1].Value != parts[0].Groups[1].Value)) return null;
        var numbers = parts.Select(p => double.Parse(p.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        var step = numbers.Count == 1 ? 1 : (numbers[^1] - numbers[0]) / (numbers.Count - 1);
        return (parts[0].Groups[1].Value, numbers[0], step, parts[0].Groups[2].Value.StartsWith('0') ? parts[0].Groups[2].Value.Length : 1);
    }

    private async Task CopyCoreAsync(bool cut)
    {
        if (_sheet == null) return;
        var range = _selection.Range.Normalized;
        var used = _sheet.UsedRange;
        if (range.Area > MaxBulkCells)
            range = CellRange.FromBounds(range.Top, range.Left, Math.Max(range.Top, Math.Min(range.Bottom, used.Bottom)), Math.Max(range.Left, Math.Min(range.Right, used.Right)));
        var sb = new StringBuilder();
        for (var r = range.Top; r <= range.Bottom; r++)
        {
            for (var c = range.Left; c <= range.Right; c++)
            {
                if (c > range.Left) sb.Append('\t');
                var text = DisplayText(new CellRef(r, c));
                sb.Append(text.IndexOfAny(['\t', '\n', '\r', '"']) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text);
            }

            sb.Append("\r\n");
        }

        var result = sb.ToString();
        _clipboard = new ClipboardSource(_sheet, range, cut, result);
        StateHasChanged();
        await Js("writeClipboard", result);
    }

    internal static List<List<string>> ParseTabular(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var atStart = true;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (ch == '"') quoted = false;
                else field.Append(ch);
                continue;
            }

            switch (ch)
            {
                case '"' when atStart:
                    quoted = true;
                    atStart = false;
                    continue;
                case '\t':
                    row.Add(field.ToString());
                    field.Clear();
                    atStart = true;
                    continue;
                case '\r' or '\n':
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = [];
                    atStart = true;
                    continue;
            }

            field.Append(ch);
            atStart = false;
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }

    private static bool IsValidName(string name) =>
        name.Length is > 0 and <= 255 && (char.IsLetter(name[0]) || name[0] is '_' or '\\') && name.All(c => char.IsLetterOrDigit(c) || c is '_' or '.' or '\\')
        && !CellRange.TryParse(name, out _) && !name.Equals("TRUE", StringComparison.OrdinalIgnoreCase) && !name.Equals("FALSE", StringComparison.OrdinalIgnoreCase)
        && !(name.Length == 1 && char.ToUpperInvariant(name[0]) is 'R' or 'C');

    private sealed record ClipboardSource(Worksheet Sheet, CellRange Range, bool Cut, string Text);
}
