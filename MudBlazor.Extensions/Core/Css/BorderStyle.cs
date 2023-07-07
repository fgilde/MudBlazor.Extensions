using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Specifies various styles for the border of an element in CSS.
/// </summary>
public enum BorderStyle
{
    /// <summary>
    /// No border; the computed border width is zero.
    /// </summary>
    [Description("none")]
    None,

    /// <summary>
    /// Same as `None`, except in border conflict resolution for table elements.
    /// </summary>
    [Description("hidden")]
    Hidden,

    /// <summary>
    /// The border is a series of dots.
    /// </summary>
    [Description("dotted")]
    Dotted,

    /// <summary>
    /// The border is a series of short lines.
    /// </summary>
    [Description("dashed")]
    Dashed,

    /// <summary>
    /// The border is a single, solid line.
    /// </summary>
    [Description("solid")]
    Solid,

    /// <summary>
    /// The border is two solid lines. The sum of the two lines and the space between them equals the value of 'border-width'.
    /// </summary>
    [Description("double")]
    Double,

    /// <summary>
    /// The border looks as though it were carved into the canvas.
    /// </summary>
    [Description("groove")]
    Groove,

    /// <summary>
    /// The opposite of 'groove': the border looks as though it were coming out of the canvas.
    /// </summary>
    [Description("ridge")]
    Ridge,

    /// <summary>
    /// The border makes the box look as though it were embedded in the canvas.
    /// </summary>
    [Description("inset")]
    Inset,

    /// <summary>
    /// The opposite of 'inset': the border makes the box look as though it were coming out of the canvas.
    /// </summary>
    [Description("outset")]
    Outset,

    /// <summary>
    /// Sets this property to its default value.
    /// </summary>
    [Description("initial")]
    Initial,

    /// <summary>
    /// Inherits this property from its parent element.
    /// </summary>
    [Description("inherit")]
    Inherit,

    /// <summary>
    /// Resets the property to its inherited value if it inherits from its parent, and to its initial value if not.
    /// </summary>
    [Description("revert")]
    Revert,

    /// <summary>
    /// Can be used on any CSS property, including 'border-style', and takes the same values as inherit. It acts like inherit if the property is inherited and like initial if not.
    /// </summary>
    [Description("unset")]
    Unset
}
