using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// An entry of a <see cref="MudExRibbonMenuButton"/>. Nested entries form a submenu.
/// </summary>
public partial class MudExRibbonMenuItem : IMudExRibbonItemHost
{
    private readonly List<MudExRibbonItemBase> _items = [];

    /// <summary>
    /// Raised when the entry is executed. The menu closes afterwards.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Check state drawn as a tick, null for an entry that cannot be checked.
    /// </summary>
    [Parameter] public bool? Checked { get; set; }

    /// <summary>
    /// Declaration of the submenu entries.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Submenu entries in declaration order.
    /// Entries declared later (e.g. via @if) are appended; use <see cref="MudExRibbonItemBase.Visible"/> to keep the declared position.
    /// </summary>
    public IReadOnlyList<MudExRibbonItemBase> Items => _items;

    /// <inheritdoc />
    protected override object StateKey => (base.StateKey, Checked);

    /// <inheritdoc />
    public override Task ExecuteAsync() => Disabled ? Task.CompletedTask : OnClick.InvokeAsync();

    void IMudExRibbonItemHost.Register(MudExRibbonItemBase item) => _items.Add(item);

    void IMudExRibbonItemHost.Unregister(MudExRibbonItemBase item)
    {
        _items.Remove(item);
        Ribbon?.Invalidate();
    }
}
