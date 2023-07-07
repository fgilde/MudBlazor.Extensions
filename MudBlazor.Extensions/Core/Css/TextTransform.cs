using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'text-transform' property values.
/// </summary>
public enum TextTransform
{
    [Description("none")]
    None,
    [Description("uppercase")]
    Uppercase,
    [Description("lowercase")]
    Lowercase,
    [Description("capitalize")]
    Capitalize,
    [Description("inherit")]
    Inherit
}