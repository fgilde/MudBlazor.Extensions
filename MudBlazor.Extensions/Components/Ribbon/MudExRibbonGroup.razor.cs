using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A titled group of commands inside a <see cref="MudExRibbonTab"/>.
/// </summary>
public partial class MudExRibbonGroup : IMudExRibbonItemHost
{
    private readonly List<MudExRibbonItemBase> _items = [];
    private object _stateKey;

    [CascadingParameter] internal MudExRibbon Ribbon { get; set; }

    [CascadingParameter] internal MudExRibbonTab Tab { get; set; }

    /// <summary>
    /// Title shown below the commands.
    /// </summary>
    [Parameter] public string Text { get; set; }

    /// <summary>
    /// Icon used when the group is collapsed to a single button.
    /// </summary>
    [Parameter] public string Icon { get; set; }

    /// <summary>
    /// Groups with a lower priority shrink first when space runs out.
    /// </summary>
    [Parameter] public int CollapsePriority { get; set; }

    /// <summary>
    /// Declaration of the items of this group.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// When set, a dialog launcher arrow is shown next to the title and raises this callback.
    /// </summary>
    [Parameter] public EventCallback OnDialogLauncherClick { get; set; }

    /// <summary>
    /// Direct items of this group in declaration order. Items inside a <see cref="MudExRibbonRow"/> belong to the row.
    /// Items declared later (e.g. via @if) are appended; use <see cref="MudExRibbonItemBase.Visible"/> to keep the declared position.
    /// </summary>
    public IReadOnlyList<MudExRibbonItemBase> Items => _items;

    void IMudExRibbonItemHost.Register(MudExRibbonItemBase item) => _items.Add(item);

    void IMudExRibbonItemHost.Unregister(MudExRibbonItemBase item)
    {
        _items.Remove(item);
        Ribbon?.Invalidate();
    }

    /// <inheritdoc />
    protected override void OnInitialized() => Tab?.Register(this);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var key = (Text, Icon, CollapsePriority, OnDialogLauncherClick.HasDelegate);
        if (Equals(key, _stateKey))
            return;
        _stateKey = key;
        Ribbon?.Invalidate();
    }

    /// <inheritdoc />
    public void Dispose() => Tab?.Unregister(this);
}
