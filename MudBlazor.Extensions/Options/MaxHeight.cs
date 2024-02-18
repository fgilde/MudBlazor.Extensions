using System.ComponentModel;

namespace MudBlazor.Extensions.Options;

/// <summary>
/// Max height of the Dialog
/// </summary>
public enum MaxHeight
{
    /// <summary>
    /// No max height
    /// </summary>
    [Description("false")]
    False,
    
    /// <summary>
    /// Large max height
    /// </summary>
    [Description("lg")]
    Large,
    
    /// <summary>
    /// Medium max height
    /// </summary>
    [Description("md")]
    Medium,
    
    /// <summary>
    /// Small max height
    /// </summary>
    [Description("sm")]
    Small,
    
    /// <summary>
    /// Extra large max height
    /// </summary>
    [Description("xl")]
    ExtraLarge,
    
    /// <summary>
    /// Extreme extra large max height
    /// </summary>
    [Description("xxl")]
    ExtraExtraLarge,
    
    /// <summary>
    /// Much smaller max height
    /// </summary>
    [Description("xs")]
    ExtraSmall,
}