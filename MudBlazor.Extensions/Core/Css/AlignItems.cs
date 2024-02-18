using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'align-items' property values.
/// </summary>
public enum AlignItems
{
    /// <summary>
    /// Align items to stretch.
    /// </summary>
    [Description("stretch")]
    Stretch,
    
    /// <summary>
    /// Align items to the center.
    /// </summary>
    [Description("center")]
    Center,
    
    /// <summary>
    /// Align items to the start.
    /// </summary>
    [Description("start")]
    Start,
    
    /// <summary>
    /// Align items to the end.
    /// </summary>
    [Description("end")]
    End,
    
    /// <summary>
    /// Align items to the flex-start.
    /// </summary>
    [Description("flex-start")]
    FlexStart,
    
    /// <summary>
    /// Align items to the flex-end.
    /// </summary>
    [Description("flex-end")]
    FlexEnd,
    
    /// <summary>
    /// Baseline alignment.
    /// </summary>
    [Description("baseline")]
    Baseline,
    
    /// <summary>
    /// Inherit the value from the parent element.
    /// </summary>
    [Description("inherit")]
    Inherit
}