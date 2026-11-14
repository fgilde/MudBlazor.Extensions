using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The page list of the notebook editor: the titles of the pages of one section, subpages indented. A click shows a page, the context menu renames,
/// deletes, duplicates, indents and moves it, and pages can be dragged to reorder them or onto a section tab to move them there.
/// </summary>
public partial class MudExNotebookPageList
{
    private MudMenu _menu;
    private NotebookPage _menuPage;

    /// <summary>The section whose pages are listed.</summary>
    [Parameter] public NotebookSection Section { get; set; }

    /// <summary>The page shown in the editor.</summary>
    [Parameter] public NotebookPage Current { get; set; }

    /// <summary>If true pages can only be selected, not changed.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Raised when the user clicks a page.</summary>
    [Parameter] public EventCallback<NotebookPage> CurrentChanged { get; set; }

    /// <summary>Raised by the Add Page button.</summary>
    [Parameter] public EventCallback AddRequested { get; set; }

    /// <summary>Raised by the context menu entry Rename; the editor focuses the page title.</summary>
    [Parameter] public EventCallback<NotebookPage> RenameRequested { get; set; }

    /// <summary>Raised with a page command chosen in the context menu; the editor executes it.</summary>
    [Parameter] public EventCallback<IDocumentCommand<Notebook>> CommandRequested { get; set; }

    /// <summary>Raised for keys pressed in the list.</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

    private static string ItemClass(NotebookPage page, bool selected, bool untitled) =>
        $"mud-ex-notebook-page-item mud-ex-notebook-page-level-{page.Level}"
        + (selected ? " mud-ex-notebook-page-selected" : "")
        + (untitled ? " mud-ex-notebook-page-untitled" : "");

    private bool CanIndent(int index) => index > 0 && Section.Pages[index].Level < Math.Min(NotebookPage.MaxLevel, Section.Pages[index - 1].Level + 1);

    private Task Request(IDocumentCommand<Notebook> command) => CommandRequested.InvokeAsync(command);

    private async Task MenuAsync(NotebookPage page, MouseEventArgs e)
    {
        if (ReadOnly || _menu == null)
            return;
        _menuPage = page;
        await _menu.OpenMenuAsync(e);
    }
}
