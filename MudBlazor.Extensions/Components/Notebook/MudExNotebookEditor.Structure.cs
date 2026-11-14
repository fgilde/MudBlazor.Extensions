using System.ComponentModel;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

public partial class MudExNotebookEditor
{
    private async Task AddSectionAsync()
    {
        AddSectionCommand command = null;
        await RunAsync(() =>
        {
            var count = _notebook.Sections.Count;
            var number = count + 1;
            while (_notebook.Sections.Any(s => s.Name == TryLocalize("New Section {0}", number)))
                number++;
            command = new AddSectionCommand(count, TryLocalize("New Section {0}", number), MudExNotebookSectionTabs.Colors[count % MudExNotebookSectionTabs.Colors.Length].Color);
            _notebook.Execute(command);
            Show(command.Section, command.Section.Pages[0]);
        });
        if (command?.Section != null)
            _tabs?.StartRename(command.Section);
    }

    /// <summary>Called by the browser when a section tab was dragged to a new position.</summary>
    /// <param name="from">The index of the dragged section.</param>
    /// <param name="target">The insertion index, from 0 to the section count.</param>
    /// <returns>A task that completes when the section moved.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnSectionDropped(int from, int target) => InvokeAsync(async () =>
    {
        var sections = _notebook?.Sections ?? [];
        if (from < 0 || from >= sections.Count || target < 0 || target > sections.Count || target == from || target == from + 1)
            return;
        await RunCommandAsync(new MoveSectionCommand(sections[from], target));
        StateHasChanged();
    });

    /// <summary>Called by the browser when a page was dragged to a new position in the page list.</summary>
    /// <param name="from">The index of the dragged page in the current section.</param>
    /// <param name="target">The insertion index, from 0 to the page count.</param>
    /// <returns>A task that completes when the page moved.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnPageDropped(int from, int target) => InvokeAsync(async () =>
    {
        var pages = _section?.Pages ?? [];
        if (from < 0 || from >= pages.Count || target < 0 || target > pages.Count || target == from || target == from + 1)
            return;
        await RunCommandAsync(new MovePagesCommand([pages[from]], _section, target));
        StateHasChanged();
    });

    /// <summary>Called by the browser when a page of the page list was dropped on a section tab; the page moves to the end of that section.</summary>
    /// <param name="from">The index of the dragged page in the current section.</param>
    /// <param name="section">The index of the target section.</param>
    /// <returns>A task that completes when the page moved.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnPageDroppedOnSection(int from, int section) => InvokeAsync(async () =>
    {
        var pages = _section?.Pages ?? [];
        var sections = _notebook?.Sections ?? [];
        if (from < 0 || from >= pages.Count || section < 0 || section >= sections.Count || ReferenceEquals(sections[section], _section))
            return;
        var target = sections[section];
        await RunCommandAsync(new MovePagesCommand([pages[from]], target, target.Pages.Count));
        StateHasChanged();
    });
}
