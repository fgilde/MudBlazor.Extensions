using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Enum representing CSS 'justify-content' property values.
/// </summary>
public enum JustifyContent
{
    /// <summary>
    /// Justify content to flex-start.
    /// </summary>
    [Description("flex-start")]
    FlexStart,

    /// <summary>
    /// Justify content to flex-end.
    /// </summary>
    [Description("flex-end")]
    FlexEnd,

    /// <summary>
    /// Justify content to the center.
    /// </summary>
    [Description("center")]
    Center,

    /// <summary>
    /// Justify content to space-between.
    /// </summary>
    [Description("space-between")]
    SpaceBetween,

    /// <summary>
    /// Justify content to space-around.
    /// </summary>
    [Description("space-around")]
    SpaceAround,

    /// <summary>
    /// Justify content to space-evenly.
    /// </summary>
    [Description("space-evenly")]
    SpaceEvenly,

    /// <summary>
    /// Justify content to the start.
    /// </summary>
    [Description("start")]
    Start,

    /// <summary>
    /// Justify content to the end.
    /// </summary>
    [Description("end")]
    End,

    /// <summary>
    /// Inherit the value from the parent element.
    /// </summary>
    [Description("inherit")]
    Inherit
}