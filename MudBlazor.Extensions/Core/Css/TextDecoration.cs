using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Specifies the type of text decoration to use.
/// </summary>
public enum TextDecoration
{
    /// <summary>
    /// Default value. Specifies that text should not have a decoration.
    /// </summary>
    [Description("none")]
    None,

    /// <summary>
    /// Specifies that text should be underlined.
    /// </summary>
    [Description("underline")]
    Underline,

    /// <summary>
    /// Specifies that text should be overlined.
    /// </summary>
    [Description("overline")]
    Overline,

    /// <summary>
    /// Specifies that text should have a line through the middle.
    /// </summary>
    [Description("line-through")]
    LineThrough,

    /// <summary>
    /// Sets this property to its default value (i.e., 'none').
    /// </summary>
    [Description("initial")]
    Initial,

    /// <summary>
    /// Inherits this property from its parent element.
    /// </summary>
    [Description("inherit")]
    Inherit
}
