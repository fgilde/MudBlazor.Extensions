using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'overflow' property values.
/// </summary>
public enum Overflow
{
    /// <summary>
    /// Overflow is not clipped. It renders outside the element's box.
    /// </summary>
    [Description("visible")]
    Visible,
    
    /// <summary>
    /// Overflow is hidden, and the rest of the content is clipped.
    /// </summary>
    [Description("hidden")]
    Hidden,
    
    /// <summary>
    /// Overflow is clipped, and a scrollbar is added to see the rest of the content.
    /// </summary>
    [Description("scroll")]
    Scroll,
    
    /// <summary>
    /// Auto value is used to allow the browser to automatically manage the overflow.
    /// </summary>
    [Description("auto")]
    Auto,
    
    /// <summary>
    /// Use the parent's overflow value.
    /// </summary>
    [Description("inherit")]
    Inherit
}