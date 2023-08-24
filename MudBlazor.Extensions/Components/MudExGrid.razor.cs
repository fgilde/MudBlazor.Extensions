using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;


/// <summary>
/// A simple grid component that supports Columns, and Rows and ColSpan and RowSpan for containing sections.
/// </summary>
public partial class MudExGrid 
{
    private string GetClass() =>
      new MudExCssBuilder("mud-ex-grid")
          .AddClass($"mud-ex-grid-column-{Column}")
          .AddClass($"mud-ex-grid-row-{Row}")
          .AddClass(Class)
      .Build();

    private string GetStyle() =>
        new MudExStyleBuilder()
            .WithHeight($"{Height}", Height is not null)
            .AddRaw(Style)
        .Build();

    /// <summary>
    /// Gets or sets the child content of the component.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the column size for the component. Default is 4.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int Column { get; set; } = 4;

    /// <summary>
    /// Gets or sets the row size for the component. Default is 4.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int Row { get; set; } = 4;

    /// <summary>
    /// Gets or sets the height of the component.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public MudExSize<double>? Height { get; set; }

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