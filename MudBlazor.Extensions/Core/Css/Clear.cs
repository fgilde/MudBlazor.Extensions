using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'clear' property values.
/// </summary>
public enum Clear
{
    [Description("none")]
    None,
    [Description("left")]
    Left,
    [Description("right")]
    Right,
    [Description("both")]
    Both,
    [Description("inherit")]
    Inherit
}