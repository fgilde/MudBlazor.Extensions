using System.Globalization;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

internal sealed record DocumentTheme(IReadOnlyList<string> Palette, string MajorFont, string MinorFont)
{
    internal static readonly DocumentTheme Default = new(ThemeColors.Office, "Calibri Light", "Calibri");

    internal static DocumentTheme Read(XDocument theme) => theme == null
        ? Default
        : new DocumentTheme(ThemeColors.Read(theme), ThemeColors.Typeface(theme, "majorFont") ?? Default.MajorFont, ThemeColors.Typeface(theme, "minorFont") ?? Default.MinorFont);

    internal string Font(string font) => font?.StartsWith("theme:", StringComparison.Ordinal) == true
        ? font.Contains("major", StringComparison.OrdinalIgnoreCase) ? MajorFont : MinorFont
        : font;

    internal string Color(string color)
    {
        if (color?.StartsWith("theme:", StringComparison.Ordinal) != true) return color;
        var parts = color[6..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var index = parts.Length == 0 ? -1 : Array.IndexOf(ThemeColors.SchemeOrder, Slot(parts[0]));
        if (index < 0) return null;
        var rgb = Palette[index];
        foreach (var part in parts.Skip(1))
        {
            if (part.Split('=') is not [var kind, var hex] || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;
            var t = value / 255.0;
            if (kind == "shade") rgb = ThemeColors.MapLightness(rgb, l => l * t);
            else if (kind == "tint") rgb = ThemeColors.MapLightness(rgb, l => l * t + (1 - t));
        }

        return "#" + rgb;
    }

    private static string Slot(string name) => name switch
    {
        "dark1" or "text1" => "dk1",
        "light1" or "background1" => "lt1",
        "dark2" or "text2" => "dk2",
        "light2" or "background2" => "lt2",
        "hyperlink" => "hlink",
        "followedHyperlink" => "folHlink",
        _ => name
    };
}
