using System.Drawing;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// MudExColor is a static utility class that provides a set of extension methods for working with Color and MudColor instances.
/// </summary>
[HasDocumentation("MudExColor.md")]
public static class MudExColor
{
    /// <summary>
    /// Returns the CSS variable name for the given color.
    /// </summary>
    public static string CssVarName(this Color color)
        => $"--mud-palette-{color.ToDescriptionString()}";

    /// <summary>
    /// Returns the CSS variable declaration for the given color. For example var(--mud-color-primary) for Color.Primary
    /// </summary>
    public static string CssVarDeclaration(this Color color)
        => color == Color.Transparent ? "transparent" : $"var({color.CssVarName()})";

    /// <summary>
    /// Converts a MudColor to a system drawing color
    /// </summary>
    public static System.Drawing.Color ToDrawingColor(this MudColor color)
        => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

    /// <summary>
    /// Converts a Color enum value of Color (like Color.Primary) to a MudColor with the correct values filled
    /// </summary>
    public static async Task<MudColor> ToMudColorAsync(this Color color) => (await MudExCss.GetCssColorVariablesAsync()).FirstOrDefault(k => k.Key == color.CssVarName()).Value;

    public static MudColor ToMudColor(this System.Drawing.Color color)
        => new MudColor(color.R, color.G, color.B, color.A);

    public static string ToCssRgba(this MudColor c) => $"rgba({c.R},{c.G},{c.B},{c.A})";
    
    public static bool IsBlack(this MudColor color) 
        => color.R == 0 && color.G == 0 && color.B == 0 && color.A == 255;

    public static bool IsWhite(this MudColor color) 
        => color.R == 255 && color.G == 255 && color.B == 255 && color.A == 255;
}