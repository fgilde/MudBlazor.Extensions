using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'text-transform' property values.
/// </summary>
public enum TextTransform
{
    /// <summary>
    /// Text will not be transformed.
    /// </summary>
    [Description("none")]
    None,
    
    /// <summary>
    /// Text will be transformed to uppercase.
    /// </summary>
    [Description("uppercase")]
    Uppercase,
    
    /// <summary>
    /// Text will be transformed to lowercase.
    /// </summary>
    [Description("lowercase")]
    Lowercase,
    
    /// <summary>
    /// Text will be transformed to capitalize.
    /// </summary>
    [Description("capitalize")]
    Capitalize,
    
    /// <summary>
    /// Text will inherit the text-transform value from the parent element.
    /// </summary>
    [Description("inherit")]
    Inherit
}