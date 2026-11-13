using System.Globalization;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

internal static class ColorResolver
{
    private static readonly Dictionary<string, string> DefaultMap = new() { ["bg1"] = "lt1", ["tx1"] = "dk1", ["bg2"] = "lt2", ["tx2"] = "dk2" };
    private static readonly HashSet<string> SchemeNames = [.. ThemeColors.SchemeOrder, .. DefaultMap.Keys, "phClr"];

    internal static string Resolve(string color, SlideMaster master, IReadOnlyDictionary<string, string> colorMap = null)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        var parts = color.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var rgb = parts[0].StartsWith("scheme:", StringComparison.Ordinal) ? Scheme(parts[0][7..], master, colorMap) : ThemeColors.Hex(parts[0].TrimStart('#'));
        if (rgb == null) return null;
        double lumMod = 1, lumOff = 0;
        foreach (var modifier in parts.Skip(1))
        {
            var eq = modifier.IndexOf('=');
            if (eq < 0 || !int.TryParse(modifier[(eq + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) continue;
            var f = value / 100000.0;
            switch (modifier[..eq])
            {
                case "lumMod":
                    lumMod = f;
                    break;
                case "lumOff":
                    lumOff = f;
                    break;
                case "tint":
                    rgb = Channels(Lightness(rgb, ref lumMod, ref lumOff), c => c * f + 1 - f);
                    break;
                case "shade":
                    rgb = Channels(Lightness(rgb, ref lumMod, ref lumOff), c => c * f);
                    break;
            }
        }

        return "#" + Lightness(rgb, ref lumMod, ref lumOff);
    }

    internal static string FromXml(XElement color)
    {
        var value = (string)color?.Attribute("val");
        var basis = color?.Name.LocalName switch
        {
            "srgbClr" => ThemeColors.Hex(value) is { } hex ? "#" + hex : null,
            "schemeClr" => value is null or "phClr" ? null : "scheme:" + value,
            "sysClr" => ThemeColors.Hex((string)color.Attribute("lastClr")) is { } sys ? "#" + sys : null,
            "prstClr" => value switch { "black" => "#000000", "white" => "#FFFFFF", _ => null },
            _ => null
        };
        if (basis == null) return null;
        return basis + string.Concat(color.Elements().Where(m => m.Name.LocalName is "lumMod" or "lumOff" or "tint" or "shade")
            .Select(m => $" {m.Name.LocalName}={(string)m.Attribute("val")}"));
    }

    internal static XElement ToXml(string color)
    {
        var parts = color?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts is not { Length: > 0 }) return null;
        var element = parts[0].StartsWith("scheme:", StringComparison.Ordinal)
            ? SchemeNames.Contains(parts[0][7..]) ? new XElement(PptxReader.A + "schemeClr", new XAttribute("val", parts[0][7..])) : null
            : ThemeColors.Hex(parts[0].TrimStart('#')) is { } hex ? new XElement(PptxReader.A + "srgbClr", new XAttribute("val", hex)) : null;
        element?.Add(parts.Skip(1).Select(m => m.Split('='))
            .Where(m => m.Length == 2 && m[0] is "lumMod" or "lumOff" or "tint" or "shade" && int.TryParse(m[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            .Select(m => new XElement(PptxReader.A + m[0], new XAttribute("val", m[1]))));
        return element;
    }

    private static string Lightness(string rgb, ref double lumMod, ref double lumOff)
    {
        if (lumMod == 1 && lumOff == 0) return rgb;
        double mod = lumMod, off = lumOff;
        lumMod = 1;
        lumOff = 0;
        return ThemeColors.MapLightness(rgb, l => l * mod + off);
    }

    private static string Scheme(string name, SlideMaster master, IReadOnlyDictionary<string, string> colorMap)
    {
        var mapped = (colorMap ?? master?.ColorMap)?.GetValueOrDefault(name) ?? DefaultMap.GetValueOrDefault(name, name);
        var index = Array.IndexOf(ThemeColors.SchemeOrder, mapped);
        return index < 0 ? null : (master?.Theme.Palette ?? ThemeColors.Office)[index];
    }

    private static string Channels(string rgb, Func<double, double> map) =>
        string.Concat(Enumerable.Range(0, 3).Select(i =>
            ((int)Math.Round(Math.Clamp(map(Convert.ToInt32(rgb.Substring(i * 2, 2), 16) / 255.0), 0, 1) * 255)).ToString("X2", CultureInfo.InvariantCulture)));
}
