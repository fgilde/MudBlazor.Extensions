using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

public partial class MudExSpreadsheetGrid
{
    private IReadOnlyList<CellNote> _notesSource;
    private Dictionary<CellRef, CellNote> _notesByCell = new();
    private CellRef? _noteHover;
    private CellRef? _noteEdit;

    /// <summary>The author written into new notes.</summary>
    [Parameter] public string Author { get; set; }

    /// <summary>Called by the browser on a Ctrl+click on a hyperlink cell. Opens safe web and mail targets in a new tab and jumps to places in the workbook.</summary>
    /// <param name="row">The row.</param>
    /// <param name="col">The column.</param>
    /// <returns>A task that completes when the link was followed.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnLinkClick(int row, int col)
    {
        if (_sheet?.LinkAt(new CellRef(row, col)) is not { IsSafe: true } link) return;
        if (string.IsNullOrEmpty(link.Target)) await GoToAsync(link.Location);
        else await Js("openLink", string.IsNullOrEmpty(link.Location) ? link.Target : link.Target + "#" + link.Location);
    }

    /// <summary>Called by the browser when the pointer enters or leaves a cell with a note.</summary>
    /// <param name="row">The row, or -1 when the pointer left.</param>
    /// <param name="col">The column, or -1 when the pointer left.</param>
    /// <returns>A task that completes when the note is shown or hidden.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnNoteHover(int row, int col)
    {
        CellRef? cell = row < 0 || col < 0 ? null : new CellRef(row, col);
        if (cell == _noteHover) return Task.CompletedTask;
        _noteHover = cell;
        return InvokeAsync(StateHasChanged);
    }

    internal bool CanEditNotes => CanEdit && !_sheet.NotesReadOnly;

    internal CellRef ActiveCell => MergeAt(_selection.Active)?.Start ?? _selection.Active;

    internal CellNote NoteAt(CellRef cell)
    {
        if (_sheet == null || _sheet.Notes.Count == 0) return null;
        if (!ReferenceEquals(_notesSource, _sheet.Notes))
        {
            _notesSource = _sheet.Notes;
            _notesByCell = new Dictionary<CellRef, CellNote>();
            foreach (var n in _sheet.Notes) _notesByCell[n.Cell.Relative] = n;
        }

        return _notesByCell.GetValueOrDefault(cell.Relative);
    }

    internal async Task EditNoteAsync()
    {
        if (!CanEditNotes) return;
        if (_editing) await CommitAsync(0, 0, false);
        _noteEdit = ActiveCell;
        _noteHover = null;
        StateHasChanged();
    }

    internal async Task DeleteNoteAsync()
    {
        var cell = ActiveCell;
        if (!CanEditNotes || NoteAt(cell) == null) return;
        _noteEdit = null;
        await TryWriteAsync(() => _workbook.Execute(new RemoveNoteCommand(_sheet, cell)));
        StateHasChanged();
    }

    private CellLink LinkFor(CellRef cell) => _sheet.Links.Count == 0 ? null : _sheet.LinkAt(cell);

    private (CellRef Cell, CellNote Note, bool Editing)? NoteBox =>
        _noteEdit is { } edit ? (edit, NoteAt(edit), true)
        : _noteHover is { } hover && NoteAt(hover) is { } note ? (hover, note, false)
        : null;

    private string NoteBoxStyle(CellRef cell)
    {
        var r = MergeAt(cell) ?? new CellRange(cell, cell);
        return $"left:{Px(X(r.Right + 1) + 8)};top:{Px(Y(r.Top))}";
    }

    private async Task SaveNoteAsync(string text)
    {
        if (_noteEdit is not { } cell) return;
        _noteEdit = null;
        var author = NoteAt(cell)?.Author ?? Author ?? "";
        await TryWriteAsync(() => _workbook.Execute(new SetNoteCommand(_sheet, new CellNote(cell, author, text ?? ""))));
        StateHasChanged();
        await FocusAsync();
    }

    private async Task CancelNoteAsync()
    {
        _noteEdit = null;
        StateHasChanged();
        await FocusAsync();
    }
}
