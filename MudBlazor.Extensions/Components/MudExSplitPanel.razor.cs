using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A SplitPanel Component 
/// </summary>
public partial class MudExSplitPanel
{
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
    public bool ColumnLayout { get; set; } = true;

    /// <summary>
    /// Sets whether the SplitPanel component should reverse its flex direction
    /// </summary>
    [Parameter, SafeCategory("Layout")]
    public bool Reverse { get; set; } = false;

    /// <summary>
    /// Sets the CSS class(es) for the SplitPanel component
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string Class { get; set; }

    /// <summary>
    /// Sets the CSS style for the SplitPanel component
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string Style { get; set; }

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


    // Its not a bug. if columnLayout is true flex-direction is row
    /// <summary>
    /// Gets the style string for the SplitPanel component
    /// </summary>
    /// <returns>The style string containing the flex-direction property value</returns>
    public string GetStyle()
    {
        return $"flex-direction:{(ColumnLayout ? "row" : "column")}{(Reverse ? "-reverse" : "")};";
    }
}