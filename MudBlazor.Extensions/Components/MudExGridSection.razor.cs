using BlazorJS.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;

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
    private SectionLayoutDto? _layout;
    private bool _layoutSuspend;

    [Inject] private IJSRuntime JS { get; set; }
    private IJSObjectReference? _module;
    private ElementReference HandleSE, HandleE, HandleS;

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
    /// Id of the section. Should be unique. If not set, an id will be generated.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    [JSInvokable("MudEx_CommitLayout")]
    public Task CommitLayoutJs(List<SectionLayoutDto> changes)
    {
        return Grid.CommitLayout(changes);
    }

    /// <summary>
    /// Set this to true to wrap the content in a MudExGroupBox.
    /// </summary>
    [Parameter] public bool WrapInGroupBox { get; set; }

    /// <summary>
    /// The title for the MudExGroupBox if WrapInGroupBox is true.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the child content of the component.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the column position for the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int Column { get => _layout?.Column ?? _column; set => Set(ref _column, value, v => ColumnChanged.InvokeAsync(v)); }


    /// <summary>
    /// Gets or sets the column span of the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int ColSpan { get => _layout?.ColSpan ?? _colspan; set => Set(ref _colspan, value, v => ColSpanChanged.InvokeAsync(v)); }

    /// <summary>
    /// Gets or sets the row position for the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int Row { get => _layout?.Row ?? _row; set => Set(ref _row, value, v => RowChanged.InvokeAsync(v)); }

    /// <summary>
    /// Gets or sets the row span of the component. Default is 1.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int RowSpan { get => _layout?.RowSpan ?? _rowspan; set => Set(ref _rowspan, value, v => RowSpanChanged.InvokeAsync(v)); }

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
    /// Specify if the section should be movable.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")] 
    public bool Movable { get; set; }

    /// <summary>
    /// Specify if the section should be resizeable.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public bool Resizable { get; set; }

    /// <summary>
    /// Gets or sets the minimum column value. Default is 1.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public int MinCol { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the maximum column value. Default is <see cref="int.MaxValue"/>.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public int MaxCol { get; set; } = int.MaxValue;
    
    /// <summary>
    /// Gets or sets the minimum row value. Default is 1.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public int MinRow { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the maximum row value. Default is <see cref="int.MaxValue"/>.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public int MaxRow { get; set; } = int.MaxValue;
    
    /// <summary>
    /// Gets or sets the minimum column span value. Default is 1.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public int MinColSpan { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the maximum column span value. Default is 12.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public int MaxColSpan { get; set; } = 12;
    
    /// <summary>
    /// Gets or sets the minimum row span value. Default is 1.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public int MinRowSpan { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the maximum row span value. Default is 12.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public int MaxRowSpan { get; set; } = 12;



    /// <summary>
    /// When true and <see cref="Movable"/> is true element can only be moved on Y axis.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")] 
    public bool LockX { get; set; }

    /// <summary>
    /// When true and <see cref="Movable"/> is true element can only be moved on X axis.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public bool LockY { get; set; }


    /// <summary>
    /// When true the movement and resize will be animated.
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public bool Animate { get; set; } = true;

    /// <summary>
    /// Animation duration for move and resize
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public TimeSpan AnimationMs { get; set; } = TimeSpan.FromMilliseconds(200);


    /// <summary>
    /// Event callback that is invoked when the manual position of the section changes.
    /// </summary>
    [Parameter] public EventCallback<SectionLayoutDto> OnManualPositionChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the context menu event should stop propagation. Default is false.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public bool OnContextMenuStopPropagation { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Id ??= BuildPathId();
        Grid?.RegisterSection(this);
        base.OnInitialized();
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



    private string GetStyle() => new MudExStyleBuilder()
        .With("transition", $"transform {AnimationMs.TotalMilliseconds}ms, box-shadow {AnimationMs.TotalMilliseconds}ms", Animate)
        .AddRaw(Style)
        .Build();

    private string BuildPathId() => Grid.GetNewSectionId();


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.ImportModuleAsync<MudExGrid>();
           
            await _module.InvokeVoidAsync("mudexGrid.bind", Grid.GridElement, DotNetObjectReference.Create(this));
            if(Movable)
                await _module.InvokeVoidAsync("mudexGrid.wireDrag", Grid.GridElement, ElementReference);
            if(Resizable)
                await _module.InvokeVoidAsync("mudexGrid.wireResize", Grid.GridElement, ElementReference, new object?[] { HandleE, HandleS, HandleSE });
        }
        await base.OnAfterRenderAsync(firstRender);
    }



    public void SetLayout(SectionLayoutDto ch)
    {
        if (_layoutSuspend)
            return;
        _layoutSuspend = true;
        ColSpan = ch.ColSpan;
        RowSpan = ch.RowSpan;
        Column = ch.Column;
        Row = ch.Row;
        _layout = ch;
        OnManualPositionChanged.InvokeAsync(ch);
        Task.Delay(200).ContinueWith(_ => _layoutSuspend = false);
    }

    public SectionLayoutDto GetLayout()
    {
        return new SectionLayoutDto(Id, Row, Column, RowSpan, ColSpan);
    }
}