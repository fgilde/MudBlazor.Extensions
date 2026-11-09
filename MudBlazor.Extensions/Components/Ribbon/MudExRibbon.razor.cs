using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Office style ribbon. Tabs, groups and items are declared as child components and register themselves,
/// the selected <see cref="Mode"/> decides how they are presented.
/// </summary>
public partial class MudExRibbon
{
    private readonly List<MudExRibbonTab> _tabs = [];
    private bool _invalidatePending;
    private IMudExRibbonView _view;

    /// <summary>
    /// Declaration of the ribbon: <see cref="MudExRibbonTab"/> components.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Presentation of the ribbon.
    /// </summary>
    [Parameter] public MudExRibbonMode Mode { get; set; }

    /// <summary>
    /// Raised when the presentation changes.
    /// </summary>
    [Parameter] public EventCallback<MudExRibbonMode> ModeChanged { get; set; }

    /// <summary>
    /// Background of the header. The text color is chosen for contrast.
    /// </summary>
    [Parameter] public MudExColor Color { get; set; } = MudBlazor.Color.Primary;

    /// <summary>
    /// Content shown at the start of the header, for example a file name.
    /// </summary>
    [Parameter] public RenderFragment TitleContent { get; set; }

    /// <summary>
    /// Content shown at the end of the header.
    /// </summary>
    [Parameter] public RenderFragment HeaderEndContent { get; set; }

    /// <summary>
    /// Index of the active tab in <see cref="Tabs"/>.
    /// </summary>
    [Parameter] public int ActiveTabIndex { get; set; }

    /// <summary>
    /// Raised when the user selects another tab.
    /// </summary>
    [Parameter] public EventCallback<int> ActiveTabIndexChanged { get; set; }

    /// <summary>
    /// All registered tabs in declaration order, including invisible ones.
    /// Tabs declared later (e.g. via @if) are appended; use <see cref="MudExRibbonTab.Visible"/> to keep the declared position.
    /// </summary>
    public IReadOnlyList<MudExRibbonTab> Tabs => _tabs;

    internal IEnumerable<MudExRibbonTab> VisibleTabs => _tabs.Where(t => t.Visible);

    internal MudExRibbonTab ActiveTab
        => _tabs.ElementAtOrDefault(ActiveTabIndex) is { Visible: true } tab ? tab : VisibleTabs.FirstOrDefault();

    internal string HeaderStyle
        => $"background-color:{MudExRibbonColors.BackgroundFor(Color)};color:{MudExRibbonColors.ForegroundFor(Color)};";

    internal int IndexOf(MudExRibbonTab tab) => _tabs.IndexOf(tab);

    internal void Register(MudExRibbonTab tab) => _tabs.Add(tab);

    internal void Unregister(MudExRibbonTab tab)
    {
        _tabs.Remove(tab);
        Invalidate();
    }

    internal void RegisterView(IMudExRibbonView view) => _view = view;

    internal void UnregisterView(IMudExRibbonView view)
    {
        if (_view == view)
            _view = null;
    }

    internal void BeginViewRender() => _invalidatePending = false;

    internal void Invalidate()
    {
        if (_invalidatePending || IsDisposed || _view == null)
            return;
        _invalidatePending = true;
        _view.Refresh();
    }

    internal Task SelectTabAsync(MudExRibbonTab tab)
    {
        var index = _tabs.IndexOf(tab);
        if (index < 0 || tab == ActiveTab)
            return Task.CompletedTask;
        ActiveTabIndex = index;
        Invalidate();
        return ActiveTabIndexChanged.InvokeAsync(index);
    }
}
