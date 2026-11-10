using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The note box next to a cell of <see cref="MudExSpreadsheetGrid"/>: shows the author and text of a note while the pointer is over the cell,
/// or edits it. Escape cancels editing, Ctrl+S raises <see cref="SaveKeyPressed"/>.
/// </summary>
public partial class MudExSpreadsheetNoteEditor
{
    private ElementReference _input;
    private string _text;
    private bool _focus;

    /// <summary>The shown note, or null for a new one.</summary>
    [Parameter] public CellNote Note { get; set; }

    /// <summary>The author shown above the text.</summary>
    [Parameter] public string Author { get; set; }

    /// <summary>True to edit the text, false to only show it.</summary>
    [Parameter] public bool Editing { get; set; }

    /// <summary>Raised with the new text when the user saves.</summary>
    [Parameter] public EventCallback<string> OnSave { get; set; }

    /// <summary>Raised when the user cancels editing.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>Raised with the current text whenever the user types.</summary>
    [Parameter] public EventCallback<string> TextChanged { get; set; }

    /// <summary>Raised when the user presses Ctrl+S while editing.</summary>
    [Parameter] public EventCallback SaveKeyPressed { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _text = Note?.Text ?? "";
        _focus = Editing;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!_focus) return;
        _focus = false;
        try
        {
            await _input.FocusAsync();
        }
        catch (Exception e) when (e is InvalidOperationException or TaskCanceledException)
        {
        }
    }

    private Task OnKeyDownAsync(KeyboardEventArgs e) =>
        !Editing ? Task.CompletedTask
        : e.Key == "Escape" ? OnCancel.InvokeAsync()
        : (e.CtrlKey || e.MetaKey) && !e.AltKey && e.Key is "s" or "S" ? SaveKeyPressed.InvokeAsync()
        : Task.CompletedTask;

    private Task OnInputAsync(ChangeEventArgs e)
    {
        _text = e.Value?.ToString() ?? "";
        return TextChanged.InvokeAsync(_text);
    }
}
