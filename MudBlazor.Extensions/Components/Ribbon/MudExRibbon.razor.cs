using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Office style ribbon. Tabs, groups and items are declared as child components and register themselves,
/// the selected <see cref="Mode"/> decides how they are presented.
/// </summary>
/// <remarks>
/// The <see cref="FileMenu"/> backstage covers its nearest ancestor with <c>position: relative</c> and leaves the header visible.
/// The ribbon sets no position itself, so wrap the ribbon and the content it controls in a relative container
/// with the ribbon at its top.
/// </remarks>
public partial class MudExRibbon
{
    private readonly List<MudExRibbonTab> _tabs = [];
    private readonly QuickAccessHost _quickAccess;
    private readonly Dictionary<object, string> _keyTips = [];
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
    /// Content of the backstage opened by the file tab, for example New, Open and Save. The file tab is shown only when this is set.
    /// The backstage covers the nearest ancestor with <c>position: relative</c> below the header.
    /// Escape anywhere inside the backstage closes it, including Escape typed into inputs of this content.
    /// </summary>
    [Parameter] public RenderFragment FileMenu { get; set; }

    /// <summary>
    /// Caption of the file tab.
    /// </summary>
    [Parameter] public string FileMenuText { get; set; } = "File";

    /// <summary>
    /// Whether the <see cref="FileMenu"/> backstage is open.
    /// </summary>
    [Parameter] public bool FileMenuOpen { get; set; }

    /// <summary>
    /// Raised when the user opens or closes the <see cref="FileMenu"/> backstage.
    /// </summary>
    [Parameter] public EventCallback<bool> FileMenuOpenChanged { get; set; }

    /// <summary>
    /// Items always shown as small icons at the start of the header, for example Save and Undo.
    /// The commands of <see cref="PinnedCommandIds"/> follow them.
    /// </summary>
    [Parameter] public RenderFragment QuickAccess { get; set; }

    /// <summary>
    /// Ids (<see cref="MudExRibbonItemBase.Id"/>) of the commands the user pinned to the quick access toolbar with a right click.
    /// Ids without a matching command are ignored, and commands of a hidden tab are not shown.
    /// </summary>
    [Parameter] public IList<string> PinnedCommandIds { get; set; }

    /// <summary>
    /// Raised when the user pins or unpins a command.
    /// </summary>
    [Parameter] public EventCallback<IList<string>> PinnedCommandIdsChanged { get; set; }

    /// <summary>
    /// Whether the header offers a field to search and run commands.
    /// </summary>
    [Parameter] public bool ShowCommandSearch { get; set; } = true;

    /// <summary>
    /// Whether pressing and releasing Alt shows key tips: letters over the tabs and, after a tab is chosen, over its commands.
    /// Tips come from <see cref="MudExRibbonTab.KeyTip"/> and <see cref="MudExRibbonItemBase.KeyTip"/> or are assigned from the text,
    /// the file tab uses "F".
    /// Alt is handled while the focus is inside the ribbon. While the focus is elsewhere, only the first ribbon on the page reacts, and only when
    /// it is the only ribbon or nothing is focused; focus inside a modal dialog that does not contain the ribbon is always ignored.
    /// </summary>
    [Parameter] public bool EnableKeyTips { get; set; } = true;

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

    /// <summary>
    /// All registered items of all tabs in declaration order, depth first: rows, menu buttons and menu items are followed by their entries.
    /// </summary>
    public IEnumerable<MudExRibbonItemBase> Commands => _tabs.SelectMany(t => t.Groups).SelectMany(g => Flatten(g.Items));

    /// <summary>
    /// Creates the ribbon.
    /// </summary>
    public MudExRibbon() => _quickAccess = new QuickAccessHost(this);

    internal IEnumerable<MudExRibbonTab> VisibleTabs => _tabs.Where(t => t.Visible && !t.Contextual).Concat(_tabs.Where(t => t.Visible && t.Contextual));

    internal IEnumerable<MudExRibbonItemBase> QuickAccessItems
    {
        get
        {
            if (PinnedCommandIds is not { Count: > 0 })
                return _quickAccess.Items.ToList();
            var commands = Commands.Where(c => c.IsCommand && c.Tab is { Visible: true }).ToLookup(c => c.Id);
            return _quickAccess.Items.Concat((PinnedCommandIds ?? []).SelectMany(id => commands[id].Take(1)).Distinct()).ToList();
        }
    }

    internal string FileTabStyle
        => $"background-color:{MudExRibbonColors.ForegroundFor(Color)};color:{MudExRibbonColors.BackgroundFor(Color)};";

    internal string BackstageId => $"{ElementId}-backstage";

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
        if (FileMenu != null && FileMenuOpen)
            await PlaceBackstageAsync();
    }

    internal async Task PlaceBackstageAsync()
    {
        if (JsReference == null || IsDisposed)
            return;
        try
        {
            await JsReference.InvokeVoidAsync("placeBackstage", BackstageId);
        }
        catch (Exception e) when (e is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
        {
        }
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

    internal IReadOnlyList<MudExRibbonItemBase> SearchCommands(string text)
        => string.IsNullOrWhiteSpace(text)
            ? []
            : Commands.Where(c => c.IsCommand && c.IsReachable && (c.Children.Count == 0 || c is MudExRibbonMenuButton { Split: true }) && c.Matches(text.Trim())).ToList();

    internal bool IsPinned(MudExRibbonItemBase item) => PinnedCommandIds?.Contains(item.Id) == true;

    internal Task TogglePinnedAsync(MudExRibbonItemBase item)
    {
        var ids = PinnedCommandIds?.ToList() ?? [];
        if (!ids.Remove(item.Id))
            ids.Add(item.Id);
        PinnedCommandIds = ids;
        Invalidate();
        return PinnedCommandIdsChanged.InvokeAsync(ids);
    }

    internal Task OpenContextMenuAsync(MudExRibbonItemBase item, MouseEventArgs args)
        => _view?.OpenContextMenuAsync(item, args) ?? Task.CompletedTask;

    internal Task SetFileMenuOpenAsync(bool open)
    {
        if (open == FileMenuOpen)
            return Task.CompletedTask;
        FileMenuOpen = open;
        Invalidate();
        return FileMenuOpenChanged.InvokeAsync(open);
    }

    internal Task OnTabHiddenAsync(MudExRibbonTab tab)
    {
        if (_tabs.IndexOf(tab) != ActiveTabIndex || VisibleTabs.FirstOrDefault() is not { } fallback)
            return Task.CompletedTask;
        ActiveTabIndex = _tabs.IndexOf(fallback);
        Invalidate();
        return ActiveTabIndexChanged.InvokeAsync(ActiveTabIndex);
    }

    internal void AssignKeyTips()
    {
        _keyTips.Clear();
        if (!EnableKeyTips)
            return;
        var file = FileMenu == null ? [] : new (object Owner, string Text, string KeyTip)[] { (this, FileMenuText, "F") };
        AddKeyTips(file.Concat(VisibleTabs.Select(t => ((object)t, t.Text, t.KeyTip))).ToList());
        if (ActiveTab is { } tab)
            AddKeyTips(tab.Groups.SelectMany(g => KeyTipItems(g.Items)).Select(i => ((object)i, i.Text ?? i.Tooltip, i.KeyTip))
                .Concat(tab.Groups.Select(g => ((object)g, g.Text, (string)null))).ToList());
    }

    internal Dictionary<string, object> TabKeyTipAttributes(object owner) => KeyTipAttributes(owner, "tab");

    internal Dictionary<string, object> ItemKeyTipAttributes(object owner) => KeyTipAttributes(owner, "item");

    private Dictionary<string, object> KeyTipAttributes(object owner, string level)
        => _keyTips.TryGetValue(owner, out var tip) ? new() { ["data-mudex-keytip"] = tip, ["data-mudex-keytip-level"] = level } : null;

    private void AddKeyTips(IReadOnlyList<(object Owner, string Text, string KeyTip)> entries)
    {
        var tips = MudExRibbonKeyTips.Assign(entries.Select(e => (e.Text, e.KeyTip)).ToList());
        for (var i = 0; i < tips.Length; i++)
        {
            if (tips[i] != null)
                _keyTips[entries[i].Owner] = tips[i];
        }
    }

    private static IEnumerable<MudExRibbonItemBase> KeyTipItems(IEnumerable<MudExRibbonItemBase> items)
        => items.Where(i => i.Visible).SelectMany(i => i is MudExRibbonRow row ? KeyTipItems(row.Items) : i.IsCommand ? [i] : Enumerable.Empty<MudExRibbonItemBase>());

    private static IEnumerable<MudExRibbonItemBase> Flatten(IEnumerable<MudExRibbonItemBase> items)
        => items.SelectMany(item => Flatten(item.Children).Prepend(item));

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

    private sealed class QuickAccessHost(MudExRibbon ribbon) : IMudExRibbonItemHost
    {
        internal List<MudExRibbonItemBase> Items { get; } = [];

        public void Register(MudExRibbonItemBase item) => Items.Add(item);

        public void Unregister(MudExRibbonItemBase item)
        {
            Items.Remove(item);
            ribbon.Invalidate();
        }
    }
}
