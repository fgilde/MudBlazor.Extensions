using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

public partial class MudExNotebookEditor
{
    private enum KeyArea { Surface, Pages, Sections }

    private async Task RunAsync(Action action)
    {
        if (IsReadOnly)
            return;
        await CommitTextAsync();
        await CommitTitleAsync();
        try
        {
            action();
        }
        catch (Exception e)
        {
            await ShowErrorAsync(e.Message);
        }
    }

    private Task RunCommandAsync(IDocumentCommand<Notebook> command) => RunAsync(() => _notebook.Execute(command));

    private Task UndoAsync(bool redo) => RunAsync(() =>
    {
        if (redo && _notebook.Undo.CanRedo)
            _notebook.Undo.Redo();
        else if (!redo && _notebook.Undo.CanUndo)
            _notebook.Undo.Undo();
    });

    private Task AddPageAsync() => RunAsync(() =>
    {
        if (_section == null)
            return;
        var command = new AddPageCommand(_section, PageIndex + 1);
        _notebook.Execute(command);
        Show(_section, command.Page);
        _focusTitle = true;
    });

    private Task RenamePageAsync(NotebookPage page)
    {
        Show(_section, page);
        _focusTitle = true;
        return Task.CompletedTask;
    }

    private void Show(NotebookSection section, NotebookPage page)
    {
        _section = section;
        _page = page;
        _sectionIndex = Math.Max(0, _notebook?.SectionList.IndexOf(section) ?? 0);
        _pageIndex = Math.Max(0, PageIndex);
        if (section != null && page != null)
            _lastPage[section] = page;
    }

    private void Reselect()
    {
        var sections = _notebook.Sections;
        if (_page != null && sections.FirstOrDefault(s => s.Pages.Contains(_page)) is { } owner)
        {
            Show(owner, _page);
            return;
        }

        var section = sections.Contains(_section) ? _section : sections[Math.Clamp(_sectionIndex, 0, sections.Count - 1)];
        Show(section, section.Pages[Math.Clamp(_pageIndex, 0, section.Pages.Count - 1)]);
    }

    private NotebookPage PageFor(NotebookSection section)
        => _lastPage.TryGetValue(section, out var page) && section.Pages.Contains(page) ? page : section.Pages.FirstOrDefault();

    private async Task<bool> GoAsync(NotebookSection section, NotebookPage page)
    {
        if (section == null || page == null || (ReferenceEquals(section, _section) && ReferenceEquals(page, _page)))
            return false;
        await CommitTextAsync();
        await CommitTitleAsync();
        if (!section.Pages.Contains(page))
            return false;
        Show(section, page);
        return true;
    }

    private Task ShowPageAsync(NotebookPage page) => GoAsync(_section, page);

    private Task ShowSectionAsync(NotebookSection section) => GoAsync(section, section == null ? null : PageFor(section));

    private Task<bool> GoToPageAsync(int index) => GoAsync(_section, _section?.Pages.ElementAtOrDefault(index));

    private Task<bool> GoToSectionAsync(int index)
    {
        var sections = _notebook.Sections;
        var section = sections[(index % sections.Count + sections.Count) % sections.Count];
        return GoAsync(section, PageFor(section));
    }

    private async Task<bool> ShortcutAsync(KeyboardEventArgs e, KeyArea area)
    {
        if (_notebook == null || _section == null || e.AltKey)
            return false;
        var index = PageIndex;
        if (e.CtrlKey || e.MetaKey)
        {
            switch (e.Key.ToLowerInvariant())
            {
                case "z" or "y" or "n" when IsReadOnly:
                    return false;
                case "z":
                    await UndoAsync(e.ShiftKey);
                    return true;
                case "y":
                    await UndoAsync(true);
                    return true;
                case "n":
                    await AddPageAsync();
                    return true;
                case "pageup":
                    return index > 0 && await GoToPageAsync(index - 1);
                case "pagedown":
                    return await GoToPageAsync(index + 1);
                case "tab":
                    return await GoToSectionAsync(_sectionIndex + (e.ShiftKey ? -1 : 1));
                default:
                    return false;
            }
        }

        var sectionIndex = _sectionIndex;
        return (area, e.Key) switch
        {
            (KeyArea.Pages, "ArrowUp") => index > 0 && await GoToPageAsync(index - 1),
            (KeyArea.Pages, "ArrowDown") => await GoToPageAsync(index + 1),
            (KeyArea.Pages, "Home") => await GoToPageAsync(0),
            (KeyArea.Pages, "End") => await GoToPageAsync(_section.Pages.Count - 1),
            (KeyArea.Sections, "ArrowLeft") => sectionIndex > 0 && await GoToSectionAsync(sectionIndex - 1),
            (KeyArea.Sections, "ArrowRight") => sectionIndex < _notebook.Sections.Count - 1 && await GoToSectionAsync(sectionIndex + 1),
            (KeyArea.Sections, "Home") => await GoToSectionAsync(0),
            (KeyArea.Sections, "End") => await GoToSectionAsync(_notebook.Sections.Count - 1),
            _ => false
        };
    }

    private void SetZoom(double zoom) => _zoom = double.IsFinite(zoom) ? Math.Clamp(Math.Round(zoom, 2), 0.25, 4) : 1;
}
