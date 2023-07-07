using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'position' property values.
/// </summary>
public enum Position
{
    [Description("static")]
    Static,
    [Description("relative")]
    Relative,
    [Description("fixed")]
    Fixed,
    [Description("absolute")]
    Absolute,
    [Description("sticky")]
    Sticky,
    [Description("inherit")]
    Inherit
}
