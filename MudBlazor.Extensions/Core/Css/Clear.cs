using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'clear' property values.
/// </summary>
public enum Clear
{
    /// <summary>
    /// No clearing is applied.
    /// </summary>
    [Description("none")]
    None,
    
    /// <summary>
    /// Left side is cleared.
    /// </summary>
    [Description("left")]
    Left,
    
    /// <summary>
    /// Right side is cleared.
    /// </summary>
    [Description("right")]
    Right,
    
    /// <summary>
    /// Clearing is applied to both sides.
    /// </summary>
    [Description("both")]
    Both,
    
    /// <summary>
    /// Inherit the value from the parent element.
    /// </summary>
    [Description("inherit")]
    Inherit
}