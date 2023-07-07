using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'direction' property values.
/// </summary>
public enum Direction
{
    [Description("ltr")]
    LTR,
    [Description("rtl")]
    RTL,
    [Description("inherit")]
    Inherit
}
