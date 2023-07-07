using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Specifies the font style for a text.
/// </summary>
public enum FontStyle
{
    /// <summary>
    /// The text is shown in a normal font style. This is default.
    /// </summary>
    [Description("normal")]
    Normal,

    /// <summary>
    /// The text is shown in an italic font style.
    /// </summary>
    [Description("italic")]
    Italic,

    /// <summary>
    /// The text is shown in an oblique font style.
    /// </summary>
    [Description("oblique")]
    Oblique,

    /// <summary>
    /// Sets this property to its default value (i.e., 'normal').
    /// </summary>
    [Description("initial")]
    Initial,

    /// <summary>
    /// Inherits this property from its parent element.
    /// </summary>
    [Description("inherit")]
    Inherit
}
