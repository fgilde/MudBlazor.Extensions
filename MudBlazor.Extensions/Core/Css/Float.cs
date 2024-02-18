using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'float' property values.
/// </summary>
public enum Float
{
    /// <summary>
    /// No float value.
    /// </summary>
    [Description("none")]
    None,
    
    /// <summary>
    /// Float to the left.
    /// </summary>
    [Description("left")]
    Left,
    
    /// <summary>
    /// Float to the right.
    /// </summary>
    [Description("right")]
    Right,
    
    /// <summary>
    /// Use the parent's float value.
    /// </summary>
    [Description("inherit")]
    Inherit
}