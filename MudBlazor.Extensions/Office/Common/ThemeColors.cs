using System.Globalization;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office;

internal static class ThemeColors
{
    internal static readonly string[] SchemeOrder = ["lt1", "dk1", "lt2", "dk2", "accent1", "accent2", "accent3", "accent4", "accent5", "accent6", "hlink", "folHlink"];

    internal static readonly string[] Office = ["FFFFFF", "000000", "E7E6E6", "44546A", "4472C4", "ED7D31", "A5A5A5", "FFC000", "5B9BD5", "70AD47", "0563C1", "954F72"];

    internal static readonly string[] Indexed =
    [
        "000000", "FFFFFF", "FF0000", "00FF00", "0000FF", "FFFF00", "FF00FF", "00FFFF",
        "000000", "FFFFFF", "FF0000", "00FF00", "0000FF", "FFFF00", "FF00FF", "00FFFF",
        "800000", "008000", "000080", "808000", "800080", "008080", "C0C0C0", "808080",
        "9999FF", "993366", "FFFFCC", "CCFFFF", "660066", "FF8080", "0066CC", "CCCCFF",
        "000080", "FF00FF", "FFFF00", "00FFFF", "800080", "800000", "008080", "0000FF",
        "00CCFF", "CCFFFF", "CCFFCC", "FFFF99", "99CCFF", "FF99CC", "CC99FF", "FFCC99",
        "3366FF", "33CCCC", "99CC00", "FFCC00", "FF9900", "FF6600", "666699", "969696",
        "003366", "339966", "003300", "333300", "993300", "993366", "333399", "333333"
    ];

    internal static string[] Read(XDocument theme)
    {
        var colors = (string[])Office.Clone();
        var scheme = theme.Descendants().FirstOrDefault(e => e.Name.LocalName == "clrScheme");
        if (scheme == null) return colors;
        for (var i = 0; i < SchemeOrder.Length; i++)
        {
            var color = scheme.Elements().FirstOrDefault(e => e.Name.LocalName == SchemeOrder[i])?.Elements().FirstOrDefault();
            var rgb = color?.Name.LocalName switch
            {
                "srgbClr" => (string)color.Attribute("val"),
                "sysClr" => (string)color.Attribute("lastClr"),
                _ => null
            };
            if (Hex(rgb) is { } valid) colors[i] = valid;
        }

        return colors;
    }

    internal static string Resolve(XElement color, string[] theme, string[] indexed)
    {
        if (color == null || (string)color.Attribute("auto") is "1" or "true") return null;
        string rgb = null;
        if ((string)color.Attribute("rgb") is { } argb) rgb = Hex(argb);
        else if (int.TryParse((string)color.Attribute("theme"), out var t)) rgb = t >= 0 && t < theme.Length ? theme[t] : null;
        else if (int.TryParse((string)color.Attribute("indexed"), out var x)) rgb = x >= 0 && x < indexed.Length ? indexed[x] : null;
        if (rgb == null) return null;
        var tint = double.TryParse((string)color.Attribute("tint"), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && double.IsFinite(v) ? Math.Clamp(v, -1, 1) : 0;
        return "#" + (tint == 0 ? rgb : ApplyTint(rgb, tint));
    }

    internal static string Hex(string rgb) =>
        rgb is { Length: 6 or 8 } && rgb.All(char.IsAsciiHexDigit) ? rgb[^6..].ToUpperInvariant() : null;

    internal static string ApplyTint(string rgb, double tint) => MapLightness(rgb, l => tint < 0 ? l * (1 + tint) : l * (1 - tint) + tint);

    internal static string MapLightness(string rgb, Func<double, double> map)
    {
        var r = Convert.ToInt32(rgb[..2], 16) / 255.0;
        var g = Convert.ToInt32(rgb[2..4], 16) / 255.0;
        var b = Convert.ToInt32(rgb[4..6], 16) / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2;
        double h = 0, s = 0;
        if (max > min)
        {
            var d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            h = max == r ? (g - b) / d + (g < b ? 6 : 0) : max == g ? (b - r) / d + 2 : (r - g) / d + 4;
            h /= 6;
        }

        l = Math.Clamp(map(l), 0, 1);
        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        return Hex(Hue(p, q, h + 1 / 3.0)) + Hex(Hue(p, q, h)) + Hex(Hue(p, q, h - 1 / 3.0));
    }

    private static double Hue(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        return t < 1 / 6.0 ? p + (q - p) * 6 * t : t < 0.5 ? q : t < 2 / 3.0 ? p + (q - p) * (2 / 3.0 - t) * 6 : p;
    }

    private static string Hex(double v) => ((int)Math.Round(Math.Clamp(v, 0, 1) * 255)).ToString("X2", CultureInfo.InvariantCulture);
}
