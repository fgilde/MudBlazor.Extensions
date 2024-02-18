using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'white-space' property values.
/// </summary>
public enum WhiteSpace
{
    /// <summary>
    /// Normal white-space behavior.
    /// </summary>
    [Description("normal")]
    Normal,
    
    /// <summary>
    /// No wrapping of text.
    /// </summary>
    [Description("nowrap")]
    Nowrap,
    
    /// <summary>
    /// Preformatted text.
    /// </summary>
    [Description("pre")]
    Pre,
    
    /// <summary>
    /// Preformatted text with preserved line breaks.
    /// </summary>
    [Description("pre-line")]
    PreLine,
    
    /// <summary>
    /// Preformatted text with preserved line breaks and white space.
    /// </summary>
    [Description("pre-wrap")]
    PreWrap,
    
    /// <summary>
    /// Inherit the 'white-space' property from the parent element.
    /// </summary>
    [Description("inherit")]
    Inherit
}