using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Values for the CSS <c>text-align</c> property.
/// </summary>
public enum TextAlign
{
    /// <summary>Text is aligned to the left edge.</summary>
    [Description("left")] Left,
    /// <summary>Text is aligned to the right edge.</summary>
    [Description("right")] Right,
    /// <summary>Text is centered.</summary>
    [Description("center")] Center,
    /// <summary>Text is justified — both edges align with the container.</summary>
    [Description("justify")] Justify,
    /// <summary>Same as left if direction is ltr, right if rtl.</summary>
    [Description("start")] Start,
    /// <summary>Same as right if direction is ltr, left if rtl.</summary>
    [Description("end")] End
}
