using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A ribbon command that opens a dropdown menu of <see cref="MudExRibbonMenuItem"/> components.
/// As split button the face runs <see cref="OnClick"/> and only the arrow opens the menu.
/// </summary>
public partial class MudExRibbonMenuButton : IMudExRibbonItemHost
{
    private readonly List<MudExRibbonItemBase> _items = [];

    /// <summary>
    /// Whether the button is split into a face that runs <see cref="OnClick"/> and an arrow that opens the menu.
    /// </summary>
    [Parameter] public bool Split { get; set; }

    /// <summary>
    /// Raised when the face of a split button is clicked.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Declaration of the menu entries.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Menu entries in declaration order.
    /// Entries declared later (e.g. via @if) are appended; use <see cref="MudExRibbonItemBase.Visible"/> to keep the declared position.
    /// </summary>
    public IReadOnlyList<MudExRibbonItemBase> Items => _items;

    /// <inheritdoc />
    protected override object StateKey => (base.StateKey, Split);

    /// <inheritdoc />
    public override Task ExecuteAsync() => Split && !Disabled ? OnClick.InvokeAsync() : Task.CompletedTask;

    void IMudExRibbonItemHost.Register(MudExRibbonItemBase item) => _items.Add(item);

    void IMudExRibbonItemHost.Unregister(MudExRibbonItemBase item)
    {
        _items.Remove(item);
        Ribbon?.Invalidate();
    }
}
