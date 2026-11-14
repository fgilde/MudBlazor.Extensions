using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The section tabs of the notebook editor, one colored tab per section. A click shows a section, a double click renames it, the context menu renames,
/// colors, deletes and moves it, and the tabs can be dragged to reorder the sections.
/// </summary>
public partial class MudExNotebookSectionTabs
{
    internal static readonly (string Name, string Color)[] Colors =
    [
        ("Purple", NotebookSection.DefaultColor), ("Blue", "#2E75B6"), ("Teal", "#00A2AD"), ("Green", "#548235"), ("Yellow", "#BF9000"),
        ("Orange", "#C55A11"), ("Red", "#C00000"), ("Magenta", "#B4009E"), ("Gray", "#595959")
    ];

    private MudMenu _menu;
    private NotebookSection _menuSection;
    private NotebookSection _renaming;
    private bool _focusRename;
    private ElementReference _renameInput;
    private EventCallback<KeyboardEventArgs>? _renameKeyDown;

    /// <summary>The notebook whose sections are shown.</summary>
    [Parameter] public Notebook Notebook { get; set; }

    /// <summary>The section shown in the editor.</summary>
    [Parameter] public NotebookSection Current { get; set; }

    /// <summary>If true sections can only be selected, not changed.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Raised when the user clicks a section tab.</summary>
    [Parameter] public EventCallback<NotebookSection> CurrentChanged { get; set; }

    /// <summary>Raised by the add button.</summary>
    [Parameter] public EventCallback AddRequested { get; set; }

    /// <summary>Raised with a section command chosen by the user; the editor executes it.</summary>
    [Parameter] public EventCallback<IDocumentCommand<Notebook>> CommandRequested { get; set; }

    /// <summary>Raised for keys pressed on the tabs.</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

    private EventCallback<KeyboardEventArgs> RenameKeyDown =>
        _renameKeyDown ??= new EventCallback<KeyboardEventArgs>(WithoutRender.Instance, (Action<KeyboardEventArgs>)(e =>
        {
            if (e.Key != "Escape" || _renaming == null) return;
            _renaming = null;
            StateHasChanged();
        }));

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!_focusRename || _renaming == null)
            return;
        _focusRename = false;
        try { await _renameInput.FocusAsync(); }
        catch (Exception e) when (e is InvalidOperationException or JSException or JSDisconnectedException or TaskCanceledException) { }
    }

    internal void StartRename(NotebookSection section)
    {
        if (ReadOnly || section == null || Notebook?.Sections.Contains(section) != true)
            return;
        _renaming = section;
        _focusRename = true;
        StateHasChanged();
    }

    private async Task MenuAsync(NotebookSection section, MouseEventArgs e)
    {
        if (ReadOnly || _menu == null)
            return;
        _menuSection = section;
        await _menu.OpenMenuAsync(e);
    }

    private async Task RenameAsync(NotebookSection section, string name)
    {
        var cancelled = !ReferenceEquals(_renaming, section);
        _renaming = null;
        if (!cancelled && !string.IsNullOrWhiteSpace(name) && name.Trim() != section.Name)
            await CommandRequested.InvokeAsync(new RenameSectionCommand(section, name.Trim()));
    }

    private void EndRename(NotebookSection section)
    {
        if (ReferenceEquals(_renaming, section))
            _renaming = null;
    }
}
