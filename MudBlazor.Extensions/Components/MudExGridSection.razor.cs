using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using System.Data.Common;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// This section can be placed inside a MudExGrid component.
/// </summary>
public partial class MudExGridSection
{
    private int _column = 1;
    private int _colspan = 1;
    private int _row = 1;
    private int _rowspan = 1;
    private string GetClass() =>
        new MudExCssBuilder("mud-ex-grid-section")
            .AddClass($"mud-ex-grid-section-col-start-{Column}")
            .AddClass($"mud-ex-grid-section-col-end-{Column + ColSpan}")
            .AddClass($"mud-ex-grid-section-row-start-{Row}")
            .AddClass($"mud-ex-grid-section-row-end-{Row + RowSpan}")
            .AddClass(Class)
            .Build();

    /// <summary>
    /// Reference to rendered element
    /// </summary>
    public ElementReference ElementReference { get; set; }

    /// <summary>
    /// Reference to the parent MudExGrid component.
    /// </summary>
    [CascadingParameter] public MudExGrid Grid { get; set; }
    
    /// <summary>
    /// Gets or sets the child content of the component.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the column position for the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int Column { get => _column; set => Set(ref _column, value, v => ColumnChanged.InvokeAsync(v)); }


    /// <summary>
    /// Gets or sets the column span of the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int ColSpan { get => _colspan; set => Set(ref _colspan, value, v => ColSpanChanged.InvokeAsync(v)); }

    /// <summary>
    /// Gets or sets the row position for the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int Row { get => _row; set => Set(ref _row, value, v => RowChanged.InvokeAsync(v)); }

    /// <summary>
    /// Gets or sets the row span of the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int RowSpan { get =>_rowspan; set => Set(ref _rowspan, value, v => RowSpanChanged.InvokeAsync(v)); }

    /// <summary>
    /// Callback that is invoked when the column position changes.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback<int> ColumnChanged { get; set; }
    
    /// <summary>
    /// Callback that is invoked when the column span changes.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback<int> ColSpanChanged { get; set; }
    
    /// <summary>
    /// Callback that is invoked when the row position changes.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback<int> RowChanged { get; set; }
    
    /// <summary>
    /// Callback that is invoked when the row span changes.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback<int> RowSpanChanged { get; set; }

    /// <summary>
    /// Event callback for the click event.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback<MouseEventArgs> OnClick { get; set; }   
    
    /// <summary>
    /// Event callback for the double click event.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback<MouseEventArgs> OnDblClick { get; set; }

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

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Grid?.RegisterSection(this);
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        Grid?.UnregisterSection(this);
        return base.DisposeAsync();
    }

    /// <summary>
    /// The on click handler
    /// </summary>    
    protected virtual Task OnClickHandler(MouseEventArgs ev) => OnClick.InvokeAsync(ev);

    /// <summary>
    /// The on double click handler
    /// </summary>    
    protected virtual Task OnDblClickHandler(MouseEventArgs ev) => OnDblClick.InvokeAsync(ev);

}