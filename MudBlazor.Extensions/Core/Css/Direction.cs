using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'direction' property values.
/// </summary>
public enum Direction
{
    /// <summary>
    /// Left-to-right direction.
    /// </summary>
    [Description("ltr")]
    LTR,
    
    /// <summary>
    /// Right-to-left direction.
    /// </summary>
    [Description("rtl")]
    RTL,
    
    /// <summary>
    /// Inherit direction from parent element.
    /// </summary>
    [Description("inherit")]
    Inherit
}
