using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'overflow' property values.
/// </summary>
public enum Overflow
{
    [Description("visible")]
    Visible,
    [Description("hidden")]
    Hidden,
    [Description("scroll")]
    Scroll,
    [Description("auto")]
    Auto,
    [Description("inherit")]
    Inherit
}