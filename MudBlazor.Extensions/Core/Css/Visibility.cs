using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'visibility' property values.
/// </summary>
public enum Visibility
{
    /// <summary>
    /// Visible.
    /// </summary>
    [Description("visible")]
    Visible,
    
    /// <summary>
    /// Hidden.
    /// </summary>
    [Description("hidden")]
    Hidden,
    
    /// <summary>
    /// Collapse.
    /// </summary>
    [Description("collapse")]
    Collapse,
    
    /// <summary>
    /// Inherit.
    /// </summary>
    [Description("inherit")]
    Inherit
}