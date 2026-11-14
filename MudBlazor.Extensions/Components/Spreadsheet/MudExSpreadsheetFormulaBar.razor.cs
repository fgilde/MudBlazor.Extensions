using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Excel style formula bar for a <see cref="MudExSpreadsheetGrid"/>: a name box that shows the active cell and jumps to a cell,
/// range or defined name (typing a new name defines it for the selection), and an input that shows and edits the content of
/// the active cell with function autocomplete and signature tips.
/// </summary>
public partial class MudExSpreadsheetFormulaBar : IDisposable
{
    private MudExSpreadsheetGrid _grid;
    private bool _nameEditing;
    private string _nameText = "";

    /// <summary>The grid whose active cell the bar shows and edits.</summary>
    [Parameter] public MudExSpreadsheetGrid Grid { get; set; }

    private (string Text, bool Ghost) Text => Grid?.BarText ?? ("", false);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (ReferenceEquals(_grid, Grid)) return;
        if (_grid != null) _grid.EditChanged -= OnGridChanged;
        _grid = Grid;
        if (_grid != null) _grid.EditChanged += OnGridChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_grid != null) _grid.EditChanged -= OnGridChanged;
    }

    private void OnGridChanged() => _ = InvokeAsync(StateHasChanged);

    private void NameFocus()
    {
        _nameEditing = true;
        _nameText = Grid?.NameBoxText ?? "";
    }

    private void NameBlur() => _nameEditing = false;

    private void NameInput(ChangeEventArgs e)
    {
        _nameEditing = true;
        _nameText = e.Value?.ToString() ?? "";
    }

    private async Task NameKeyDown(KeyboardEventArgs e)
    {
        if (Grid == null) return;
        if (e.Key == "Escape")
        {
            _nameEditing = false;
            await Grid.FocusAsync();
            return;
        }

        if (e.Key != "Enter") return;
        _nameEditing = false;
        await Grid.GoToAsync(_nameText, true);
        await Grid.FocusAsync();
    }

    private Task FocusAsync() => Grid?.BeginEditFromBarAsync() ?? Task.CompletedTask;

    private async Task InputAsync(ChangeEventArgs e)
    {
        if (Grid == null) return;
        if (!Grid.IsEditing) await Grid.BeginEditFromBarAsync();
        await Grid.SetEditTextFromBarAsync(e.Value?.ToString());
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (Grid is not { IsEditing: true }) return;
        var assist = Grid.Assist;
        switch (e.Key)
        {
            case "Enter" when !e.AltKey:
                await Grid.CommitFromBarAsync(e.CtrlKey ? 0 : e.ShiftKey ? -1 : 1, 0);
                break;
            case "Tab" when assist.Suggestions.Count > 0:
                await Grid.AcceptSuggestionAsync(Grid.AssistIndex);
                break;
            case "Tab":
                await Grid.CommitFromBarAsync(0, e.ShiftKey ? -1 : 1);
                break;
            case "Escape":
                await Grid.CancelFromBarAsync();
                break;
        }
    }

    private Task EnterAsync() => Grid?.CommitFromBarAsync(0, 0) ?? Task.CompletedTask;

    private Task CancelAsync() => Grid?.CancelFromBarAsync() ?? Task.CompletedTask;

    private async Task InsertFunctionAsync()
    {
        if (Grid?.CanEdit != true) return;
        if (!Grid.IsEditing) await Grid.BeginEditFromBarAsync();
        var text = Grid.EditText ?? "";
        await Grid.SetEditTextFromBarAsync(text.StartsWith('=') ? text : "=" + text);
    }
}
