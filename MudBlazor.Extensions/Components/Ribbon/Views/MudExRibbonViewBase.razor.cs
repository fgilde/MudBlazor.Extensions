using System.ComponentModel;
using Microsoft.AspNetCore.Components;

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

    private protected MudExRibbonViewBase()
    {
    }

    /// <summary>
    /// The ribbon whose registered tabs, groups and items are drawn.
    /// </summary>
    [Parameter] public MudExRibbon Ribbon { get; set; }

    private string PanelId => $"{Ribbon.ElementId}-panel";

    private string TabId(MudExRibbonTab tab) => $"{Ribbon.ElementId}-tab-{Ribbon.IndexOf(tab)}";

    /// <inheritdoc />
    protected override void OnInitialized() => Ribbon.RegisterView(this);

    void IMudExRibbonView.Refresh() => _ = InvokeAsync(StateHasChanged);

    void IMudExRibbonView.CommandExecuted()
    {
        if (!_overlayOpen)
            return;
        _overlayOpen = false;
        _ = InvokeAsync(StateHasChanged);
    }

    private void CloseOverlay() => _overlayOpen = false;

    private Task OnTabClickAsync(MudExRibbonTab tab)
    {
        if (Ribbon.IsCollapsed)
            _overlayOpen = !_overlayOpen || tab != Ribbon.ActiveTab;
        return Ribbon.SelectTabAsync(tab);
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
