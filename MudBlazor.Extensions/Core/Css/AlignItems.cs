using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'align-items' property values.
/// </summary>
public enum AlignItems
{
    [Description("stretch")]
    Stretch,
    [Description("center")]
    Center,
    [Description("start")]
    Start,
    [Description("end")]
    End,
    [Description("flex-start")]
    FlexStart,
    [Description("flex-end")]
    FlexEnd,
    [Description("baseline")]
    Baseline,
    [Description("inherit")]
    Inherit
}