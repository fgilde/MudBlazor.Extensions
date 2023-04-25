using System.Drawing;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Helper;

public static class MudExColor
{
    public static string CssVarName(this Color color)
        => $"--mud-palette-{color.ToDescriptionString()}";

    public static string CssVarDeclaration(this Color color)
        => color == Color.Transparent ? "transparent" : $"var({color.CssVarName()})";

    public static System.Drawing.Color ToDrawingColor(this MudColor color)
        => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

    public static MudColor ToMudColor(this System.Drawing.Color color)
        => new MudColor(color.R, color.G, color.B, color.A);

    public static string ToCssRgba(this MudColor c) => $"rgba({c.R},{c.G},{c.B},{c.A})";
    
    public static bool IsBlack(this MudColor color) 
        => color.R == 0 && color.G == 0 && color.B == 0 && color.A == 255;

    public static bool IsWhite(this MudColor color) 
        => color.R == 255 && color.G == 255 && color.B == 255 && color.A == 255;
}