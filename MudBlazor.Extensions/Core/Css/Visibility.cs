using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'visibility' property values.
/// </summary>
public enum Visibility
{
    [Description("visible")]
    Visible,
    [Description("hidden")]
    Hidden,
    [Description("collapse")]
    Collapse,
    [Description("inherit")]
    Inherit
}