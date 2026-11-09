using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Classic Office layout of a <see cref="MudExRibbon"/>: colored header with tabs and the titled groups of the active tab.
/// Rendered by the ribbon, not meant to be used directly.
/// </summary>
public partial class MudExRibbonClassicView : IMudExRibbonView, IDisposable
{
    /// <summary>
    /// The ribbon whose registered tabs, groups and items are drawn.
    /// </summary>
    [Parameter] public MudExRibbon Ribbon { get; set; }

    private string PanelId => $"{Ribbon.ElementId}-panel";

    private string TabId(MudExRibbonTab tab) => $"{Ribbon.ElementId}-tab-{Ribbon.IndexOf(tab)}";

    /// <inheritdoc />
    protected override void OnInitialized() => Ribbon.RegisterView(this);

    void IMudExRibbonView.Refresh() => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose() => Ribbon.UnregisterView(this);
}
