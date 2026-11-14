using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlidePanel
{
    private readonly HashSet<string> _collapsed = [];
    private Slide _shown;
    private MudMenu _sectionMenu;
    private PresentationSection _menuSection;
    private string _renaming;
    private bool _focusRename;
    private ElementReference _renameInput;
    private EventCallback<KeyboardEventArgs>? _renameKeyDown;

    /// <summary>Raised by the context menu entry Add Section; the new section starts at the current slide.</summary>
    [Parameter] public EventCallback AddSectionRequested { get; set; }

    /// <summary>Raised with a section command chosen in the context menu of a section header; the editor executes it.</summary>
    [Parameter] public EventCallback<IDocumentCommand<Presentation>> CommandRequested { get; set; }

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
        catch (Exception e) when (e is InvalidOperationException or JSException or JSDisconnectedException) { }
    }

    internal void StartRename(PresentationSection section)
    {
        if (ReadOnly || section == null || Presentation?.Sections.Contains(section) != true)
            return;
        _renaming = section.Id;
        _focusRename = true;
        StateHasChanged();
    }

    internal void SetCollapsed(bool collapsed)
    {
        _collapsed.Clear();
        if (collapsed)
            _collapsed.UnionWith(Presentation?.Sections.Select(s => s.Id) ?? []);
        StateHasChanged();
    }

    private bool IsCollapsed(PresentationSection section) => _collapsed.Contains(section.Id);

    private void Toggle(PresentationSection section)
    {
        if (!_collapsed.Remove(section.Id))
            _collapsed.Add(section.Id);
    }

    private async Task SectionMenuAsync(PresentationSection section, MouseEventArgs e)
    {
        if (ReadOnly || _sectionMenu == null)
            return;
        _menuSection = section;
        await _sectionMenu.OpenMenuAsync(e);
    }

    private Task RequestAsync(IDocumentCommand<Presentation> command) => CommandRequested.InvokeAsync(command);

    private async Task RenameAsync(PresentationSection section, string name)
    {
        var cancelled = _renaming != section.Id;
        _renaming = null;
        if (!cancelled && !string.IsNullOrWhiteSpace(name) && name.Trim() != section.Name)
            await RequestAsync(new RenameSectionCommand(section, name.Trim()));
    }

    private void EndRename(PresentationSection section)
    {
        if (_renaming == section.Id)
            _renaming = null;
    }
}
