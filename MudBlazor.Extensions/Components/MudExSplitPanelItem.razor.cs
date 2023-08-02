using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// SplitPanelItem for MudExSplitPanel component
/// </summary>
public partial class MudExSplitPanelItem
{
    /// <summary>
    /// The parent MudExSplitPanel component
    /// </summary>
    [CascadingParameter]
    public MudExSplitPanel Panel { get; set; }

    /// <summary>
    /// The child content of MudExSplitPanelItem component
    /// </summary>
    [Parameter, SafeCategory("Content")]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// The min size of MudExSplitPanelItem component
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public int MinSize { get; set; }

    /// <summary>
    /// The CSS unit of MudExSplitPanelItem component's size
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public CssUnit SizeUnit { get; set; } = CssUnit.Pixels;


    /// <summary>
    /// Get the CSS class of MudExSplitPanelItem component
    /// </summary>
    protected virtual string GetClass()
    {
        return $"{(Panel?.ColumnLayout == true ? "mud-width-full" : "mud-height-full")}";
    }

    /// <summary>
    /// Get the CSS style of MudExSplitPanelItem component
    /// </summary>
    protected virtual string GetStyle()
    {
        return MudExStyleBuilder.GenerateStyleString(new
        {
            //Flex = 1,
            Overflow = "auto",
            MinWidth = Panel?.ColumnLayout == true ? MinSize.ToString() : "unset",
            MinHeight = !Panel?.ColumnLayout == true ? MinSize.ToString() : "unset",
        }, SizeUnit, Style);
    }
}