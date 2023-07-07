using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Specifies the font variant for a text.
/// </summary>
public enum FontVariant
{
    /// <summary>
    /// The text is shown in a normal font variant. This is default.
    /// </summary>
    [Description("normal")]
    Normal,

    /// <summary>
    /// The text is transformed to use small capitals for lower case letters.
    /// </summary>
    [Description("small-caps")]
    SmallCaps,

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
