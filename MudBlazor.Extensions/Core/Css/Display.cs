using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'display' property values.
/// </summary>
public enum Display
{
    [Description("block")]
    Block,
    [Description("inline")]
    Inline,
    [Description("flex")]
    Flex,
    [Description("grid")]
    Grid,
    [Description("none")]
    None,
    [Description("inherit")]
    Inherit
}