using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'float' property values.
/// </summary>
public enum Float
{
    [Description("none")]
    None,
    [Description("left")]
    Left,
    [Description("right")]
    Right,
    [Description("inherit")]
    Inherit
}