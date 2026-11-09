using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A tab of a <see cref="MudExRibbon"/> holding <see cref="MudExRibbonGroup"/> components.
/// </summary>
public partial class MudExRibbonTab
{
    private readonly List<MudExRibbonGroup> _groups = [];
    private object _stateKey;

    [CascadingParameter] internal MudExRibbon Ribbon { get; set; }

    /// <summary>
    /// Caption of the tab.
    /// </summary>
    [Parameter] public string Text { get; set; }

    /// <summary>
    /// Key tip shown for the tab in key tip mode.
    /// </summary>
    [Parameter] public string KeyTip { get; set; }

    /// <summary>
    /// Whether the tab is shown.
    /// </summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>
    /// Whether the tab belongs to a selection, for example "Table Design". Contextual tabs are drawn tinted with
    /// <see cref="ContextualColor"/> after all normal tabs; bind <see cref="Visible"/> to the selection to show them.
    /// Becoming visible never activates the tab.
    /// </summary>
    [Parameter] public bool Contextual { get; set; }

    /// <summary>
    /// Caption shown above a contextual tab, for example "Table Tools". Consecutive contextual tabs with the same
    /// caption and color share one caption.
    /// </summary>
    [Parameter] public string ContextualTitle { get; set; }

    /// <summary>
    /// Tint of a contextual tab. Defaults to <see cref="MudBlazor.Color.Secondary"/>.
    /// </summary>
    [Parameter] public MudExColor? ContextualColor { get; set; }

    /// <summary>
    /// Declaration of the groups of this tab.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// All registered groups in declaration order.
    /// Groups declared later (e.g. via @if) are appended to the end.
    /// </summary>
    public IReadOnlyList<MudExRibbonGroup> Groups => _groups;

    internal string ContextualStyle
        => Contextual ? $"--mud-ex-ribbon-contextual-color:{MudExRibbonColors.BackgroundFor(ContextualColor ?? MudBlazor.Color.Secondary)}" : null;

    internal string ContextualClass => Contextual ? "mud-ex-ribbon-tab-contextual" : null;

    internal void Register(MudExRibbonGroup group) => _groups.Add(group);

    internal void Unregister(MudExRibbonGroup group)
    {
        _groups.Remove(group);
        Ribbon?.Invalidate();
    }

    /// <inheritdoc />
    protected override void OnInitialized() => Ribbon?.Register(this);

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        var key = (Text, KeyTip, Visible, ContextualTitle, ContextualStyle);
        if (Equals(key, _stateKey) || Ribbon == null)
            return Task.CompletedTask;
        _stateKey = key;
        Ribbon.Invalidate();
        return Visible ? Task.CompletedTask : Ribbon.OnTabHiddenAsync(this);
    }

    /// <inheritdoc />
    public void Dispose() => Ribbon?.Unregister(this);
}
