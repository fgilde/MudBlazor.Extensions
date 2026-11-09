using Microsoft.AspNetCore.Components;

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
    /// Declaration of the groups of this tab.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// All registered groups in declaration order.
    /// </summary>
    public IReadOnlyList<MudExRibbonGroup> Groups => _groups;

    internal void Register(MudExRibbonGroup group) => _groups.Add(group);

    internal void Unregister(MudExRibbonGroup group)
    {
        _groups.Remove(group);
        Ribbon?.Invalidate();
    }

    /// <inheritdoc />
    protected override void OnInitialized() => Ribbon?.Register(this);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var key = (Text, KeyTip, Visible);
        if (Equals(key, _stateKey))
            return;
        _stateKey = key;
        Ribbon?.Invalidate();
    }

    /// <inheritdoc />
    public void Dispose() => Ribbon?.Unregister(this);
}
