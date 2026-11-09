using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A line of items inside a <see cref="MudExRibbonGroup"/>, drawn side by side in small or medium layout.
/// </summary>
public partial class MudExRibbonRow : IMudExRibbonItemHost
{
    private readonly List<MudExRibbonItemBase> _items = [];

    /// <summary>
    /// Declaration of the items of this row.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Items of this row in declaration order.
    /// Items declared later (e.g. via @if) are appended; use <see cref="MudExRibbonItemBase.Visible"/> to keep the declared position.
    /// </summary>
    public IReadOnlyList<MudExRibbonItemBase> Items => _items;

    internal override IReadOnlyList<MudExRibbonItemBase> Children => _items;

    /// <inheritdoc />
    public override bool IsCommand => false;

    void IMudExRibbonItemHost.Register(MudExRibbonItemBase item) => _items.Add(item);

    void IMudExRibbonItemHost.Unregister(MudExRibbonItemBase item)
    {
        _items.Remove(item);
        Ribbon?.Invalidate();
    }

    private static MudExRibbonItemLayout ChildLayout(MudExRibbonItemBase item, MudExRibbonItemLayout layout) => layout switch
    {
        MudExRibbonItemLayout.Large or MudExRibbonItemLayout.Medium => item.Size == MudExRibbonItemSize.Small ? MudExRibbonItemLayout.Small : MudExRibbonItemLayout.Medium,
        _ => layout
    };
}
