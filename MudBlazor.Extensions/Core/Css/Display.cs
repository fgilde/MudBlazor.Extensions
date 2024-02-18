using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'display' property values.
/// </summary>
public enum Display
{
    
    /// <summary>
    /// Display as a block element.
    /// </summary>
    [Description("block")]
    Block,
    
    /// <summary>
    /// Display as an inline element.
    /// </summary>
    [Description("inline")]
    Inline,
    
    /// <summary>
    /// Display as a flex container.
    /// </summary>
    [Description("flex")]
    Flex,
    
    /// <summary>
    /// Display as a grid container.
    /// </summary>
    [Description("grid")]
    Grid,
    
    /// <summary>
    /// No specified display value. element will not be rendered.
    /// </summary>
    [Description("none")]
    None,
    
    /// <summary>
    /// Inherit display value from parent element.
    /// </summary>
    [Description("inherit")]
    Inherit
}