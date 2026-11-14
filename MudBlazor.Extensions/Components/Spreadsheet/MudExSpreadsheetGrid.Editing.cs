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
    private const long MaxBulkCells = 100_000;

    private bool _editing;
    private string _editText = "";
    private string _editOriginal = "";
    private CellRef _editCell;
    private bool _editMode;
    private bool _editFromBar;
    private FormulaAssistResult _assist = FormulaAssistResult.None;
    private int _assistIndex;
    private bool _assistDismissed;

    internal bool EditFromBar => _editing && _editFromBar;

    internal bool HasPendingEdit => _editing && _editText != _editOriginal || HasNoteDraft;

    internal FormulaAssistResult Assist => _editing && !_assistDismissed ? _assist : FormulaAssistResult.None;

    internal int AssistIndex => _assistIndex;

    /// <summary>Starts editing the active cell.</summary>
    /// <param name="initialText">Text that replaces the content (as when typing), or null to edit the current content (as with F2).</param>
    /// <returns>A task that completes when the editor is shown.</returns>
    public Task BeginEditAsync(string initialText = null) => BeginEditCoreAsync(initialText, false);

    /// <summary>Commits the edit to the active cell without moving the selection, and a changed note that is being written.</summary>
    /// <returns>A task that completes when the value is written.</returns>
    public async Task CommitEditAsync()
    {
        if (HasNoteDraft) await SaveNoteAsync(_noteText);
        await CommitAsync(0, 0, false);
    }

    /// <summary>Discards the edit.</summary>
    /// <returns>A task that completes when the editor is hidden.</returns>
    public async Task CancelEditAsync()
    {
        if (!_editing) return;
        EndEditState();
        await Js("endEdit", true);
        StateHasChanged();
    }

    private async Task<bool> TryWriteAsync(Action write)
    {
        try
        {
            write();
            return true;
        }
        catch (Exception e)
        {
            await ReportErrorAsync(e);
            return false;
        }
    }

    private async Task ReportErrorAsync(Exception e)
    {
        StateHasChanged();
        if (OnError.HasDelegate)
            await OnError.InvokeAsync(e);
        else if (DialogService != null)
            await DialogService.ShowInformationAsync(TryLocalize("Error"), e.Message, Icons.Material.Filled.Error);
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
            case "s" or "S" when ctrl && !e.AltKey:
                await SaveKeyPressed.InvokeAsync();
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
        if (CanEdit && (changed || allSelected))
        {
            switch (await CheckEditAsync(text, cell))
            {
                case EditCheck.Retry:
                    await Js("beginEdit", _editText, _editMode, !_editFromBar);
                    return;
                case EditCheck.Cancel:
                    await CancelEditAsync();
                    return;
            }

            if (!await TryWriteAsync(() => WriteEdit(text, cell, allSelected)))
            {
                await Js("beginEdit", _editText, _editMode, !_editFromBar);
                return;
            }

            _clipboard = null;
        }

        EndEditState();
        await Js("endEdit", true);
        if (dRow != 0 || dCol != 0) await StepWithinSelectionAsync(dRow, dCol);
        StateHasChanged();
    }

    private void WriteEdit(string text, CellRef cell, bool allSelected)
    {
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

        var (value, suggested) = ParseInput(cell, text);
        _workbook.Execute(new SetValueCommand(_sheet, cell, value));
        if (suggested != null)
            _workbook.Execute(new SetStyleCommand(_sheet, new CellRange(cell, cell), s => s with { NumberFormat = suggested }));
    }

    private (CellValue Value, string SuggestedFormat) ParseInput(CellRef cell, string text)
    {
        var format = _workbook.Styles[_sheet.EffectiveStyleId(cell)].NumberFormat;
        if (format == "@") return (CellValue.Text(text), null);
        var (value, suggested) = CellInputParser.Parse(text, Culture);
        return (value, string.IsNullOrEmpty(format) || format == NumberFormat.General ? suggested : null);
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

    private void ClearCell(Worksheet sheet, CellRef cell, bool style)
    {
        if (sheet[cell] != null) _workbook.Execute(new ClearCommand(sheet, new CellRange(cell, cell), style));
    }

    internal static bool IsValidName(string name) => Workbook.IsValidName(name);
}
