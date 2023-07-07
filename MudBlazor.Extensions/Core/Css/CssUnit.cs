using System.ComponentModel;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// Represents units of measure in CSS.
/// </summary>
public enum CssUnit
{
    /// <summary>
    /// Represents a percentage of a base value in CSS.
    /// </summary>
    [Description("%")]
    Percentage,

    /// <summary>
    /// Represents pixels in CSS.
    /// </summary>
    [Description("px")]
    Pixels,

    /// <summary>
    /// Represents the font size of the parent element in CSS.
    /// </summary>
    [Description("em")]
    Em,

    /// <summary>
    /// Represents the font size of the root element in CSS.
    /// </summary>
    [Description("rem")]
    Rem,

    /// <summary>
    /// Represents a percentage of the viewport's width in CSS.
    /// </summary>
    [Description("vw")]
    ViewportWidth,

    /// <summary>
    /// Represents a percentage of the viewport's height in CSS.
    /// </summary>
    [Description("vh")]
    ViewportHeight,

    /// <summary>
    /// Represents the smaller dimension of the viewport in CSS.
    /// </summary>
    [Description("vmin")]
    ViewportMinimum,

    /// <summary>
    /// Represents the larger dimension of the viewport in CSS.
    /// </summary>
    [Description("vmax")]
    ViewportMaximum,

    /// <summary>
    /// Represents centimeters in CSS.
    /// </summary>
    [Description("cm")]
    Centimeters,

    /// <summary>
    /// Represents millimeters in CSS.
    /// </summary>
    [Description("mm")]
    Millimeters,

    /// <summary>
    /// Represents inches in CSS.
    /// </summary>
    [Description("in")]
    Inches,

    /// <summary>
    /// Represents points in CSS. One point is equal to 1/72 of an inch.
    /// </summary>
    [Description("pt")]
    Points,

    /// <summary>
    /// Represents picas in CSS. One pica is equal to 12 points.
    /// </summary>
    [Description("pc")]
    Picas
}
