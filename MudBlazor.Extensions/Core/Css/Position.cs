using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'position' property values.
/// </summary>
public enum Position
{
    /// <summary>
    /// Static positioning.
    /// </summary>
    [Description("static")]
    Static,
    
    /// <summary>
    /// Relative positioning.
    /// </summary>
    [Description("relative")]
    Relative,
    
    /// <summary>
    /// fixed positioning.
    /// </summary>
    [Description("fixed")]
    Fixed,
    
    /// <summary>
    /// Absolute positioning.
    /// </summary>
    [Description("absolute")]
    Absolute,
    
    /// <summary>
    /// Sticky positioning.
    /// </summary>
    [Description("sticky")]
    Sticky,
    
    /// <summary>
    /// Use the value of the 'position' property from the parent element.
    /// </summary>
    [Description("inherit")]
    Inherit
}
