using System.Globalization;
using System.Text;

namespace MudBlazor.Extensions.Office.Document;

internal static class DocumentCss
{
    private static readonly Dictionary<string, string> Fallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Calibri"] = "'Carlito',sans-serif",
        ["Cambria"] = "'Caladea',serif",
        ["Arial"] = "'Liberation Sans',sans-serif",
        ["Times New Roman"] = "'Liberation Serif',serif",
        ["Courier New"] = "'Liberation Mono',monospace"
    };

    internal static string Run(RunFormat format, RunFormat basis, bool fallbacks = false)
    {
        var css = new StringBuilder();
        if ((format.Bold ?? false) != (basis.Bold ?? false)) css.Append("font-weight:").Append(format.Bold == true ? "bold" : "normal").Append(';');
        if ((format.Italic ?? false) != (basis.Italic ?? false)) css.Append("font-style:").Append(format.Italic == true ? "italic" : "normal").Append(';');
        var decoration = Decoration(format);
        if (decoration != Decoration(basis)) css.Append("text-decoration:").Append(decoration).Append(';');
        if (format.Caps == true && basis.Caps != true) css.Append("text-transform:uppercase;");
        if (format.SmallCaps == true && basis.SmallCaps != true) css.Append("font-variant:small-caps;");
        if (format.Color != basis.Color && Color(format.Color) is { } color) css.Append("color:").Append(color).Append(';');
        if (format.SizePt is { } size && size != basis.SizePt) css.Append("font-size:").Append(Number(size)).Append("pt;");
        if (format.Font != basis.Font && (fallbacks ? FontStack(format.Font) : FontName(format.Font) is { } name ? "'" + name + "'" : null) is { } font)
            css.Append("font-family:").Append(font).Append(';');
        var background = format.Highlight != null && HtmlReader.Highlights.TryGetValue(format.Highlight, out var highlight) ? highlight : Color(format.Shading);
        if (background != null) css.Append("background-color:").Append(background).Append(';');
        if (format.VerticalAlign is { } align and not VerticalAlign.Baseline) css.Append("vertical-align:").Append(align == VerticalAlign.Superscript ? "super" : "sub").Append(';');
        return css.ToString();
    }

    internal static string Paragraph(ParagraphFormat format, ParagraphFormat basis, bool listItem, bool preWrap)
    {
        var css = new StringBuilder(preWrap ? "white-space:pre-wrap;" : "");
        if (format.Alignment is { } alignment and not ParagraphAlignment.Left && alignment != basis.Alignment)
            css.Append("text-align:").Append(alignment switch { ParagraphAlignment.Center => "center", ParagraphAlignment.Right => "right", _ => "justify" }).Append(';');
        if (!listItem && (format.IndentLeft ?? 0) != (basis.IndentLeft ?? 0)) css.Append("margin-left:").Append(Number((format.IndentLeft ?? 0) / 20.0)).Append("pt;");
        if (!listItem && (format.IndentFirstLine ?? 0) != (basis.IndentFirstLine ?? 0))
            css.Append("text-indent:").Append(Number((format.IndentFirstLine ?? 0) / 20.0)).Append("pt;");
        if (Color(format.Shading) is { } shading) css.Append("background-color:").Append(shading).Append(';');
        return css.ToString();
    }

    internal static string Spacing(ParagraphFormat format)
    {
        var css = new StringBuilder();
        css.Append("margin-top:").Append(Number((format.SpacingBefore ?? 0) / 20.0)).Append("pt;");
        css.Append("margin-bottom:").Append(Number((format.SpacingAfter ?? 0) / 20.0)).Append("pt;");
        if ((format.IndentRight ?? 0) != 0) css.Append("margin-right:").Append(Number(format.IndentRight.Value / 20.0)).Append("pt;");
        if (format.Line is { } line)
            css.Append("line-height:").Append(line.Rule == LineRule.Auto ? Number(line.Value / 240.0 * 1.15) : Number(line.Value / 20.0) + "pt").Append(';');
        return css.ToString();
    }

    internal static string Color(string color) =>
        color is { Length: 7 } && color[0] == '#' && color.Skip(1).All(char.IsAsciiHexDigit) ? color.ToUpperInvariant() : null;

    internal static string FontName(string font)
    {
        var name = new string((font ?? "").Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-').ToArray()).Trim();
        return name.Length > 0 ? name : null;
    }

    internal static string FontStack(string font) =>
        FontName(font) is { } name ? "'" + name + "'," + (Fallbacks.TryGetValue(name, out var fallback) ? fallback : "sans-serif") : null;

    internal static string Number(double value) => double.IsFinite(value) ? value.ToString("0.##", CultureInfo.InvariantCulture) : "0";

    private static string Decoration(RunFormat format)
    {
        var underline = format.Underline is { } u && u != "none";
        var strike = format.Strike == true || format.DoubleStrike == true;
        return underline && strike ? "underline line-through" : underline ? "underline" : strike ? "line-through" : "none";
    }
}
