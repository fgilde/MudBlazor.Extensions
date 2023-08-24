using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// This section can placed inside a MudExGrid component.
/// </summary>
public partial class MudExGridSection
{
    private string GetClass() =>
        new MudExCssBuilder("mud-ex-grid-section")
            .AddClass($"mud-ex-grid-section-col-start-{Column}")
            .AddClass($"mud-ex-grid-section-col-end-{Column + ColSpan}")
            .AddClass($"mud-ex-grid-section-row-start-{Row}")
            .AddClass($"mud-ex-grid-section-row-end-{Row + RowSpan}")
            .AddClass(Class)
            .Build();

    /// <summary>
    /// Gets or sets the child content of the component.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the column position for the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int Column { get; set; } = 1;

    /// <summary>
    /// Gets or sets the column span of the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int ColSpan { get; set; } = 1;

    /// <summary>
    /// Gets or sets the row position for the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int Row { get; set; } = 1;

    /// <summary>
    /// Gets or sets the row span of the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int RowSpan { get; set; } = 1;

    /// <summary>
    /// Event callback for the click event.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the click event should stop propagation. Default is false.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public bool OnClickStopPropagation { get; set; }

    /// <summary>
    /// Event callback for the context menu event.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback OnContextMenu { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the context menu event should prevent its default action. Default is false.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public bool OnContextMenuPreventDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the context menu event should stop propagation. Default is false.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public bool OnContextMenuStopPropagation { get; set; }

    /// <summary>
    /// The on click handler
    /// </summary>    
    protected async Task OnClickHandler(MouseEventArgs ev) => await OnClick.InvokeAsync(ev);
}