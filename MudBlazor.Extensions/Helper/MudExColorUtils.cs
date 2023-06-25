using System.Reflection;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// MudExColor is a static utility class that provides a set of extension methods for working with Color and MudColor instances.
/// </summary>
[HasDocumentation("MudExColorUtils.md")]
public static class MudExColorUtils
{

    /// <summary>
    /// Returns array with all colors in given palette.
    /// </summary>
    public static MudColor[] AllColors(this Palette palette) => palette.AllOf<MudColor>();
    
    /// <summary>
    /// Returns array with all colors in given palette.
    /// </summary>
    public static MudColor[] AllColors(this PaletteLight palette) => palette.AllOf<MudColor>();
    
    /// <summary>
    /// Returns array with all colors in given palette.
    /// </summary>
    public static MudColor[] AllColors(this PaletteDark palette) => palette.AllOf<MudColor>();

    /// <summary>
    /// Converts a MudColor to an int
    /// </summary>
    public static int ToInt(this MudColor color) 
        => (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

    /// <summary>
    /// Converts a MudColor to an unsigned int
    /// </summary>    
    public static uint ToUInt(this MudColor color) => (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);

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

    /// <summary>
    /// Converts a sytsem drawing color to a MudColor
    /// </summary>
    public static MudColor ToMudColor(this System.Drawing.Color color)
        => new MudColor(color.R, color.G, color.B, color.A);

    /// <summary>
    /// Converts a MudColor to a string in the format css rgba format
    /// </summary>
    public static string ToCssRgba(this MudColor c) => $"rgba({c.R},{c.G},{c.B},{c.A})";

    /// <summary>
    /// returns true is given color is black
    /// </summary>
    public static bool IsBlack(this MudColor color) 
        => color.R == 0 && color.G == 0 && color.B == 0 && color.A == 255;

    /// <summary>
    /// returns true is given color is white
    /// </summary>    
    public static bool IsWhite(this MudColor color) 
        => color.R == 255 && color.G == 255 && color.B == 255 && color.A == 255;
    
    /// <summary>
    /// Creates a drawing color from html known color name
    /// </summary>
    public static System.Drawing.Color? FromHtmlColorName(string htmlColorName) => System.Drawing.Color.FromName(htmlColorName);

    /// <summary>
    /// Converts a system drawing color to hey
    /// </summary>
    public static string ToHex(this System.Drawing.Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>
    /// returns true is given MudColor is not null and valid filled
    /// </summary>
    public static bool IsValid(this MudColor? color) =>  color is not null && color != default && color is not { A: 0, R: 0, G: 0, B: 0 };

    /// <summary>
    /// Try's to parse string to system drawing color
    /// </summary>
    public static bool TryParseFromHtmlColorName(string s, out System.Drawing.Color color)
    {
        color = default;
        try
        {
            var value = FromHtmlColorName(s);
            color = value ?? default;
            return color is not {A: 0, R: 0, G: 0, B: 0} && value.HasValue;
        }
        catch
        {}
        return false;
    }
}