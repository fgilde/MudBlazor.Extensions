using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'white-space' property values.
/// </summary>
public enum WhiteSpace
{
    [Description("normal")]
    Normal,
    [Description("nowrap")]
    Nowrap,
    [Description("pre")]
    Pre,
    [Description("pre-line")]
    PreLine,
    [Description("pre-wrap")]
    PreWrap,
    [Description("inherit")]
    Inherit
}