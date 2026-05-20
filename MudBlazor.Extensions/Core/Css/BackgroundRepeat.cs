using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Values for the CSS <c>background-repeat</c> property.
/// </summary>
public enum BackgroundRepeat
{
    /// <summary>The background image is repeated both vertically and horizontally.</summary>
    [Description("repeat")] Repeat,
    /// <summary>The background image is repeated only horizontally.</summary>
    [Description("repeat-x")] RepeatX,
    /// <summary>The background image is repeated only vertically.</summary>
    [Description("repeat-y")] RepeatY,
    /// <summary>The background image is not repeated.</summary>
    [Description("no-repeat")] NoRepeat,
    /// <summary>The image is rescaled to fit and is repeated as often as it can without being clipped.</summary>
    [Description("round")] Round,
    /// <summary>The image is repeated as many times as possible without clipping; spaces are inserted between images.</summary>
    [Description("space")] Space
}
