using System.Globalization;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Office;

internal readonly record struct PastedStyle(
    PastedFormat Format, string Align, int? Level, string ListId, bool Label, bool? KeepSpace, int? Indent, int? FirstLine, bool BreakBefore, bool BreakAfter);

internal static class HtmlPasteStyle
{
    private const int MaxIndent = 31680;

    private static readonly (string Unit, double Twips)[] Units = [("pt", 20), ("px", 15), ("in", 1440), ("cm", 1440 / 2.54), ("mm", 144 / 2.54)];

    private static readonly double[] FontSizes = [7.5, 10, 12, 13.5, 18, 24, 36];

    internal static PastedStyle Css(PastedFormat format, string css)
    {
        string align = null, listId = null;
        int? level = null, margin = null, padding = null, firstLine = null;
        bool? keep = null;
        bool label = false, before = false, after = false;
        foreach (var declaration in HtmlTableFormat.Split(css, ';'))
        {
            var colon = declaration.IndexOf(':');
            if (colon <= 0) continue;
            var raw = declaration[(colon + 1)..].Replace("!important", "", StringComparison.OrdinalIgnoreCase).Trim();
            var value = raw.ToLowerInvariant();
            switch (declaration[..colon].Trim().ToLowerInvariant())
            {
                case "font-weight":
                    format = format with { Bold = value is "bold" or "bolder" || int.TryParse(value, out var weight) && weight >= 600 };
                    break;
                case "font-style":
                    format = format with { Italic = value is "italic" or "oblique" };
                    break;
                case "text-decoration" or "text-decoration-line":
                    format = format with { Underline = value.Contains("underline"), Strike = value.Contains("line-through") };
                    break;
                case "color":
                    format = format with { Color = HtmlTableFormat.Color(value) ?? format.Color };
                    break;
                case "font-size":
                    format = format with { SizePt = FontSize(value) ?? format.SizePt };
                    break;
                case "font-family":
                    format = format with { Font = FontName(raw) ?? format.Font };
                    break;
                case "background" or "background-color":
                    format = format with { Highlight = HtmlTableFormat.Split(value, ' ').Select(HtmlTableFormat.Color).FirstOrDefault(c => c != null) ?? format.Highlight };
                    break;
                case "vertical-align":
                    format = format with { VerticalAlign = value switch { "super" => "super", "sub" => "sub", "baseline" => null, _ => format.VerticalAlign } };
                    break;
                case "text-align":
                    align = Align(value) ?? align;
                    break;
                case "white-space":
                    keep = value is "pre" or "pre-wrap" or "pre-line" or "break-spaces" ? true : value is "normal" or "nowrap" ? false : keep;
                    break;
                case "margin-left":
                    margin = Twips(value) ?? margin;
                    break;
                case "padding-left":
                    padding = Twips(value) ?? padding;
                    break;
                case "text-indent":
                    firstLine = Twips(value) ?? firstLine;
                    break;
                case "break-before" or "page-break-before":
                    before |= value is "page" or "always" or "left" or "right";
                    break;
                case "break-after" or "page-break-after":
                    after |= value is "page" or "always" or "left" or "right";
                    break;
                case "mso-list":
                    label |= value == "ignore";
                    foreach (var token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        if (token.StartsWith("level", StringComparison.Ordinal) && int.TryParse(token[5..], NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n >= 1)
                            level = Math.Min(n - 1, 8);
                        else if (token.Length > 1 && token[0] == 'l' && token.Skip(1).All(char.IsAsciiDigit))
                            listId = token;
                    break;
            }
        }

        int? indent = margin == null && padding == null ? null : Math.Clamp((margin ?? 0) + (padding ?? 0), -MaxIndent, MaxIndent);
        return new PastedStyle(format, align, level, listId, label, keep, indent, firstLine, before, after);
    }

    internal static string Align(string value) => value switch
    {
        "left" or "start" => "left",
        "center" => "center",
        "right" or "end" => "right",
        "justify" => "justify",
        _ => null
    };

    internal static double? FontSize(string value)
    {
        var factor = value.EndsWith("pt", StringComparison.Ordinal) ? 1 : value.EndsWith("px", StringComparison.Ordinal) ? 0.75 : 0;
        if (factor == 0 || !double.TryParse(value[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var size) || !(size > 0)) return null;
        return Math.Clamp(Math.Round(size * factor * 2) / 2, 1, 1638);
    }

    internal static double? FontTagSize(string value)
    {
        value = value.Trim();
        if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var size)) return null;
        if (value[0] is '+' or '-') size += 3;
        return FontSizes[Math.Clamp(size, 1, 7) - 1];
    }

    internal static string FontName(string value)
    {
        var first = HtmlTableFormat.Split(value, ',').FirstOrDefault()?.Trim().Trim('"', '\'') ?? "";
        var name = new string(first.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-').ToArray()).Trim();
        return name.Length is > 0 and <= 31 && !(name.ToLowerInvariant() is "serif" or "sans-serif" or "monospace" or "cursive" or "fantasy" or "inherit" or "initial")
            ? name
            : null;
    }

    private static int? Twips(string value)
    {
        if (value == "0") return 0;
        foreach (var (unit, twips) in Units)
            if (value.EndsWith(unit, StringComparison.Ordinal) &&
                double.TryParse(value[..^unit.Length], NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
                return (int)Math.Clamp(Math.Round(number * twips), -MaxIndent, MaxIndent);
        return null;
    }
}
