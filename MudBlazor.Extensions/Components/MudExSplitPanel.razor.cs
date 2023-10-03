using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A SplitPanel Component 
/// </summary>
public partial class MudExSplitPanel
{
    private MudExSplitter _splitter;
    private string _rightSize;
    private bool AnythingCollapsed() => _isCollapsed;
    private bool _isCollapsed;
    private bool _columnLayout = true;

    /// <summary>
    /// The content that will be displayed on the left or top hand side of the SplitPanel
    /// </summary>
    [Parameter, SafeCategory("Content")]
    public RenderFragment Left { get; set; }

    /// <summary>
    /// The content that will be displayed on the right or bottom hand side of the SplitPanel
    /// </summary>
    [Parameter, SafeCategory("Content")]
    public RenderFragment Right { get; set; }

    /// <summary>
    /// Sets whether the SplitPanel component should lay out horizontally
    /// </summary>
    [Parameter, SafeCategory("Layout")]
    public bool ColumnLayout
    {
        get => _columnLayout;
        set
        {
            if (_columnLayout != value)
            {
                _columnLayout = value;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Sets whether the SplitPanel component should reverse its flex direction
    /// </summary>
    [Parameter, SafeCategory("Layout")]
    public bool Reverse { get; set; } = false;

    /// <summary>
    /// Sets whether the sizes of the panes should be updated in percentage values rather than pixels
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool UpdateSizesInPercentage { get; set; } = true;

    /// <summary>
    /// Sets whether the SplitPanel component should be splittable
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool Splittable { get; set; } = true;

    /// <summary>
    /// Size of splitter
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExSize<double> SplitterSize { get; set; } = 5;


    /// <summary>
    /// Color of splitter
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExColor SplitterColor { get; set; } = Color.Primary;

    /// <summary>
    /// Specifies the icon for the right direction.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string IconRight { get; set; } = Icons.Material.Filled.ArrowForward;

    /// <summary>
    /// Specifies the icon for the left direction.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string IconLeft { get; set; } = Icons.Material.Filled.ArrowBack;

    /// <summary>
    /// Specifies the icon for the upward direction.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string IconUp { get; set; } = Icons.Material.Filled.ArrowUpward;

    /// <summary>
    /// Specifies the icon for the downward direction.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string IconDown { get; set; } = Icons.Material.Filled.ArrowDownward;

    /// <summary>
    /// Indicates if the component is collapsed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool IsInitiallyCollapsed { get; set; }

    /// <summary>
    /// Indicates if the component can be collapsed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool IsCollapsible { get; set; }
        
    // Its not a bug. if columnLayout is true flex-direction is row
    /// <summary>
    /// Gets the style string for the SplitPanel component
    /// </summary>
    /// <returns>The style string containing the flex-direction property value</returns>
    public string GetStyle()
    {
        return $"flex-direction:{(ColumnLayout ? "row" : "column")}{(Reverse ? "-reverse" : "")};";
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            _isCollapsed = IsInitiallyCollapsed;
        base.OnAfterRender(firstRender);
    }

    private async Task ToggleCollapsed(bool isCollapsed)
    {
        if (isCollapsed)
        {
            var sizes = await _splitter.GetElementSizes();
            _rightSize = ColumnLayout ? sizes?.Next?.Width : sizes?.Next?.Height ?? string.Empty;
            await _splitter.Reset();
        }

        _isCollapsed = isCollapsed;
        StateHasChanged();
    }

}