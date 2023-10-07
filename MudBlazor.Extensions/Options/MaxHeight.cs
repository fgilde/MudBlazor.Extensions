using System.ComponentModel;

namespace MudBlazor.Extensions.Options;

public enum MaxHeight
{
    [Description("false")]
    False,
    [Description("lg")]
    Large,
    [Description("md")]
    Medium,
    [Description("sm")]
    Small,
    [Description("xl")]
    ExtraLarge,
    [Description("xxl")]
    ExtraExtraLarge,
    [Description("xs")]
    ExtraSmall,
}