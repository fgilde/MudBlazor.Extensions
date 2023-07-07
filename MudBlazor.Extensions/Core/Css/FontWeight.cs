using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Specifies the weight or thickness of the font.
/// </summary>
public enum FontWeight
{
    /// <summary>
    /// Defines normal characters. This is default.
    /// </summary>
    [Description("normal")]
    Normal,

    /// <summary>
    /// Defines thick characters.
    /// </summary>
    [Description("bold")]
    Bold,

    /// <summary>
    /// Defines thicker characters.
    /// </summary>
    [Description("bolder")]
    Bolder,

    /// <summary>
    /// Defines lighter characters.
    /// </summary>
    [Description("lighter")]
    Lighter,

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
