using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Base of the views that draw a <see cref="MudExRibbon"/> in one <see cref="MudExRibbonMode"/>.
/// The active view registers with the ribbon and redraws itself when tabs, groups or items change.
/// Rendered by the ribbon, not meant to be used directly.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract partial class MudExRibbonViewBase : IMudExRibbonView, IDisposable
{
    private static readonly (MudExRibbonMode Mode, string Text)[] Modes =
    [
        (MudExRibbonMode.Classic, "Classic ribbon"),
        (MudExRibbonMode.Simplified, "Simplified ribbon"),
        (MudExRibbonMode.Menu, "Menu bar"),
        (MudExRibbonMode.Toolbar, "Toolbar")
    ];

    private bool _overlayOpen;
    private bool _backstageFocused;
    private ElementReference _backstageBack;
    private MudMenu _contextMenu;
    private MudExRibbonItemBase _contextItem;

    private protected MudExRibbonGroup OpenGroup { get; set; }

    private protected MudExRibbonViewBase()
    {
    }

    /// <summary>
    /// The ribbon whose registered tabs, groups and items are drawn.
    /// </summary>
    [Parameter] public MudExRibbon Ribbon { get; set; }

    private string PanelId => Ribbon.PanelId;

    private string TabId(MudExRibbonTab tab) => $"{Ribbon.ElementId}-tab-{Ribbon.IndexOf(tab)}";

    /// <inheritdoc />
    protected override void OnInitialized() => Ribbon.RegisterView(this);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await Ribbon.SyncReductionAsync(_overlayOpen);
        var backstageOpen = Ribbon.FileMenu != null && Ribbon.FileMenuOpen;
        if (backstageOpen)
            await Ribbon.PlaceBackstageAsync();
        if (backstageOpen && !_backstageFocused)
            await FocusBackstageAsync();
        _backstageFocused = backstageOpen;
    }

    private async Task FocusBackstageAsync()
    {
        try
        {
            await _backstageBack.FocusAsync();
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
        }
    }

    void IMudExRibbonView.Refresh() => _ = InvokeAsync(StateHasChanged);

    void IMudExRibbonView.CommandExecuted()
    {
        if (!_overlayOpen && OpenGroup == null)
            return;
        _overlayOpen = false;
        OpenGroup = null;
        _ = InvokeAsync(StateHasChanged);
    }

    async Task IMudExRibbonView.OpenContextMenuAsync(MudExRibbonItemBase item, MouseEventArgs args)
    {
        _contextItem = item;
        StateHasChanged();
        if (_contextMenu != null)
            await _contextMenu.OpenMenuAsync(args);
    }

    private static List<List<MudExRibbonTab>> TabSets(IEnumerable<MudExRibbonTab> tabs)
    {
        var sets = new List<List<MudExRibbonTab>>();
        foreach (var tab in tabs)
        {
            if (sets.LastOrDefault() is { } last && last[0].Contextual == tab.Contextual
                && (!tab.Contextual || (last[0].ContextualTitle == tab.ContextualTitle && last[0].ContextualStyle == tab.ContextualStyle)))
                last.Add(tab);
            else
                sets.Add([tab]);
        }
        return sets;
    }

    private Task OnBackstageKeyDown(KeyboardEventArgs args)
        => args.Key == "Escape" ? Ribbon.SetFileMenuOpenAsync(false) : Task.CompletedTask;

    private void CloseOverlay() => _overlayOpen = false;

    private void OnOverlayKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
            CloseOverlay();
    }

    private async Task OnTabClickAsync(MudExRibbonTab tab)
    {
        await Ribbon.SetFileMenuOpenAsync(false);
        if (Ribbon.IsCollapsed)
            _overlayOpen = !_overlayOpen || tab != Ribbon.ActiveTab;
        await Ribbon.SelectTabAsync(tab);
    }

    private Task ToggleCollapsedAsync()
    {
        if (!Ribbon.AllowCollapse)
            return Task.CompletedTask;
        _overlayOpen = false;
        return Ribbon.SetCollapsedAsync(!Ribbon.Collapsed);
    }

    /// <inheritdoc />
    public void Dispose() => Ribbon.UnregisterView(this);
}
