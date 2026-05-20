using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Values for the CSS <c>mix-blend-mode</c> property.
/// </summary>
public enum MixBlendMode
{
    /// <summary>The final color is the top color.</summary>
    [Description("normal")] Normal,
    /// <summary>Multiplies the top and bottom colors.</summary>
    [Description("multiply")] Multiply,
    /// <summary>Multiplies the complements of the colors.</summary>
    [Description("screen")] Screen,
    /// <summary>Multiplies or screens, depending on the bottom color.</summary>
    [Description("overlay")] Overlay,
    /// <summary>Replaces with the darker color.</summary>
    [Description("darken")] Darken,
    /// <summary>Replaces with the lighter color.</summary>
    [Description("lighten")] Lighten,
    /// <summary>Brightens the bottom color to reflect the top.</summary>
    [Description("color-dodge")] ColorDodge,
    /// <summary>Darkens the bottom color to reflect the top.</summary>
    [Description("color-burn")] ColorBurn,
    /// <summary>Multiplies or screens, depending on the top color.</summary>
    [Description("hard-light")] HardLight,
    /// <summary>Darkens or lightens, depending on the top color.</summary>
    [Description("soft-light")] SoftLight,
    /// <summary>Subtracts the darker color from the lighter.</summary>
    [Description("difference")] Difference,
    /// <summary>Like difference, but with lower contrast.</summary>
    [Description("exclusion")] Exclusion,
    /// <summary>Hue of top, saturation and luminosity of bottom.</summary>
    [Description("hue")] Hue,
    /// <summary>Saturation of top, hue and luminosity of bottom.</summary>
    [Description("saturation")] Saturation,
    /// <summary>Hue and saturation of top, luminosity of bottom.</summary>
    [Description("color")] Color,
    /// <summary>Luminosity of top, hue and saturation of bottom.</summary>
    [Description("luminosity")] Luminosity
}
