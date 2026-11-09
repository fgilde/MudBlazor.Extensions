using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Office style ribbon. Tabs, groups and items are declared as child components and register themselves,
/// the selected <see cref="Mode"/> decides how they are presented.
/// </summary>
public partial class MudExRibbon
{
    private readonly List<MudExRibbonTab> _tabs = [];
    private IMudExRibbonView _view;
    private (MudExRibbonMode Mode, MudExRibbonTab Tab) _reductionKey;
    private int _reductionStep;
    private ((MudExRibbonMode Mode, MudExRibbonTab Tab, bool Collapsed, bool OverlayOpen) Key, int Step, int Max)? _syncedReduction;

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
    /// Whether only the header is shown and a tab opens its commands as an overlay.
    /// Applies to <see cref="MudExRibbonMode.Classic"/> and <see cref="MudExRibbonMode.Simplified"/>.
    /// </summary>
    [Parameter] public bool Collapsed { get; set; }

    /// <summary>
    /// Raised when the user collapses or expands the ribbon.
    /// </summary>
    [Parameter] public EventCallback<bool> CollapsedChanged { get; set; }

    /// <summary>
    /// Whether the header offers a button to switch the <see cref="Mode"/>.
    /// </summary>
    [Parameter] public bool ShowModeSwitch { get; set; } = true;

    /// <summary>
    /// Whether the user may collapse the ribbon with the chevron button or a double click on a tab.
    /// </summary>
    [Parameter] public bool AllowCollapse { get; set; } = true;

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

    internal bool IsCollapsed => Collapsed && Mode is MudExRibbonMode.Classic or MudExRibbonMode.Simplified;

    internal string HeaderStyle
        => $"background-color:{MudExRibbonColors.BackgroundFor(Color)};color:{MudExRibbonColors.ForegroundFor(Color)};";

    internal string PanelId => $"{ElementId}-panel";

    private (MudExRibbonMode Mode, MudExRibbonTab Tab) ReductionKey => (Mode, ActiveTab);

    internal int MaxReductionStep => ActiveTab is not { } tab ? 0 : Mode switch
    {
        MudExRibbonMode.Classic => MudExRibbonReduction.MaxStep(tab.Groups),
        MudExRibbonMode.Simplified => tab.Groups.Sum(g => g.Items.Count(i => i.Visible)),
        _ => 0
    };

    internal int ReductionStep
    {
        get
        {
            if (!Equals(_reductionKey, ReductionKey))
                (_reductionKey, _reductionStep) = (ReductionKey, 0);
            return Math.Min(_reductionStep, MaxReductionStep);
        }
    }

    /// <summary>
    /// Applies a reduction step measured by the ribbon script: in <see cref="MudExRibbonMode.Classic"/> groups shrink one level per step,
    /// in <see cref="MudExRibbonMode.Simplified"/> one more item moves into the overflow menu. The step is clamped to what the active tab supports
    /// and starts at 0 again when the tab or the mode changes.
    /// </summary>
    /// <param name="step">Number of reduction steps to apply.</param>
    [JSInvokable]
    public Task SetReductionStep(int step)
    {
        step = Math.Clamp(step, 0, MaxReductionStep);
        if (step == ReductionStep)
            return Task.CompletedTask;
        _reductionStep = step;
        Invalidate();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override object[] GetJsArguments() => [ElementReference, CreateDotNetObjectReference(), PanelId];

    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        await base.ImportModuleAndCreateJsAsync();
        await SyncReductionAsync(false);
    }

    internal bool IsFullyReduced => MaxReductionStep > 0 && ReductionStep == MaxReductionStep;

    internal async Task SyncReductionAsync(bool overlayOpen)
    {
        var state = (Key: (Mode, ActiveTab, IsCollapsed, overlayOpen), Step: ReductionStep, Max: MaxReductionStep);
        if (JsReference == null || IsDisposed || Equals(state, _syncedReduction))
            return;
        var reset = !Equals(_syncedReduction?.Key, state.Key);
        _syncedReduction = state;
        try
        {
            await JsReference.InvokeVoidAsync("update", state.Step, state.Max, reset);
        }
        catch (Exception e) when (e is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
        {
        }
    }

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

    internal void Invalidate()
    {
        if (!IsDisposed)
            _view?.Refresh();
    }

    internal void NotifyCommandExecuted() => _view?.CommandExecuted();

    internal Task SelectTabAsync(MudExRibbonTab tab)
    {
        var index = _tabs.IndexOf(tab);
        if (index < 0 || tab == ActiveTab)
            return Task.CompletedTask;
        ActiveTabIndex = index;
        Invalidate();
        return ActiveTabIndexChanged.InvokeAsync(index);
    }

    internal Task SetModeAsync(MudExRibbonMode mode)
    {
        if (mode == Mode)
            return Task.CompletedTask;
        Mode = mode;
        StateHasChanged();
        return ModeChanged.InvokeAsync(mode);
    }

    internal Task SetCollapsedAsync(bool collapsed)
    {
        if (collapsed == Collapsed)
            return Task.CompletedTask;
        Collapsed = collapsed;
        Invalidate();
        return CollapsedChanged.InvokeAsync(collapsed);
    }
}
