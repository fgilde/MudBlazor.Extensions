using System.Globalization;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

internal static class FormatXml
{
    internal static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    internal static RunFormat Run(XElement rPr) => rPr == null
        ? null
        : new RunFormat
        {
            Bold = OnOff(rPr, "b"),
            Italic = OnOff(rPr, "i"),
            Strike = OnOff(rPr, "strike"),
            DoubleStrike = OnOff(rPr, "dstrike"),
            Caps = OnOff(rPr, "caps"),
            SmallCaps = OnOff(rPr, "smallCaps"),
            Hidden = OnOff(rPr, "vanish"),
            Underline = Val(rPr, "u"),
            SizePt = Int(rPr.Element(W + "sz"), "val") / 2.0,
            Font = Font(rPr.Element(W + "rFonts")),
            Color = Color(rPr.Element(W + "color"), "val", "themeColor", "themeShade", "themeTint"),
            Highlight = Val(rPr, "highlight"),
            Shading = Shading(rPr.Element(W + "shd")),
            VerticalAlign = Val(rPr, "vertAlign") switch
            {
                "superscript" => VerticalAlign.Superscript,
                "subscript" => VerticalAlign.Subscript,
                "baseline" => VerticalAlign.Baseline,
                _ => null
            },
            StyleId = Val(rPr, "rStyle")
        };

    internal static ParagraphFormat Paragraph(XElement pPr)
    {
        if (pPr == null) return null;
        var indent = pPr.Element(W + "ind");
        var spacing = pPr.Element(W + "spacing");
        var numbering = pPr.Element(W + "numPr");
        return new ParagraphFormat
        {
            StyleId = Val(pPr, "pStyle"),
            Alignment = Val(pPr, "jc") switch
            {
                "left" or "start" => ParagraphAlignment.Left,
                "center" => ParagraphAlignment.Center,
                "right" or "end" => ParagraphAlignment.Right,
                "both" or "distribute" => ParagraphAlignment.Justify,
                _ => null
            },
            IndentLeft = Int(indent, "left") ?? Int(indent, "start"),
            IndentRight = Int(indent, "right") ?? Int(indent, "end"),
            IndentFirstLine = -Int(indent, "hanging") ?? Int(indent, "firstLine"),
            SpacingBefore = Int(spacing, "before"),
            SpacingAfter = Int(spacing, "after"),
            Line = Int(spacing, "line") is { } line
                ? new LineSpacing(line, (string)spacing.Attribute(W + "lineRule") switch { "exact" => LineRule.Exact, "atLeast" => LineRule.AtLeast, _ => LineRule.Auto })
                : null,
            KeepWithNext = OnOff(pPr, "keepNext"),
            KeepLines = OnOff(pPr, "keepLines"),
            PageBreakBefore = OnOff(pPr, "pageBreakBefore"),
            WidowControl = OnOff(pPr, "widowControl"),
            OutlineLevel = Int(pPr.Element(W + "outlineLvl"), "val"),
            Numbering = Int(numbering?.Element(W + "numId"), "val") is { } numId ? new NumberingRef(numId, Int(numbering.Element(W + "ilvl"), "val") ?? 0) : null,
            Tabs = pPr.Element(W + "tabs")?.Elements(W + "tab").Select(Tab).Where(t => t != null).ToList(),
            Shading = Shading(pPr.Element(W + "shd"))
        };
    }

    internal static TableFormat Table(XElement tblPr) => tblPr == null
        ? null
        : new TableFormat
        {
            StyleId = Val(tblPr, "tblStyle"),
            Width = tblPr.Element(W + "tblW") is { } width && (string)width.Attribute(W + "type") == "dxa" ? Int(width, "w") : null,
            Alignment = Val(tblPr, "jc") switch
            {
                "left" or "start" => TableAlignment.Left,
                "center" => TableAlignment.Center,
                "right" or "end" => TableAlignment.Right,
                _ => null
            },
            Borders = Borders(tblPr.Element(W + "tblBorders")),
            Look = Look(tblPr.Element(W + "tblLook"))
        };

    internal static string Val(XElement parent, string name) => (string)parent?.Element(W + name)?.Attribute(W + "val");

    internal static int? Int(XElement element, string attribute) =>
        int.TryParse((string)element?.Attribute(W + attribute), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    internal static bool? OnOff(XElement parent, string name) =>
        parent.Element(W + name) is not { } element ? null : (string)element.Attribute(W + "val") is not ("0" or "false" or "off");

    private static string Font(XElement fonts) =>
        fonts == null ? null : (string)fonts.Attribute(W + "asciiTheme") is { } theme ? "theme:" + theme : (string)fonts.Attribute(W + "ascii") ?? (string)fonts.Attribute(W + "hAnsi");

    private static string Shading(XElement shading) => Color(shading, "fill", "themeFill", "themeFillShade", "themeFillTint");

    private static string Color(XElement element, string value, string theme, string shade, string tint)
    {
        if (element == null) return null;
        if ((string)element.Attribute(W + theme) is { } name) return "theme:" + name + Modifier(element, shade, "shade") + Modifier(element, tint, "tint");
        var raw = (string)element.Attribute(W + value);
        return raw == "auto" ? "auto" : ThemeColors.Hex(raw) is { } hex ? "#" + hex : null;
    }

    private static string Modifier(XElement element, string attribute, string kind) => (string)element.Attribute(W + attribute) is { } hex ? $" {kind}={hex}" : "";

    private static TabStop Tab(XElement tab)
    {
        if (Int(tab, "pos") is not { } position) return null;
        var alignment = (string)tab.Attribute(W + "val") switch
        {
            "center" => TabAlignment.Center,
            "right" or "end" => TabAlignment.Right,
            "decimal" => TabAlignment.Decimal,
            "bar" => TabAlignment.Bar,
            "clear" => TabAlignment.Clear,
            _ => TabAlignment.Left
        };
        return new TabStop(position, alignment, (string)tab.Attribute(W + "leader") is { } leader and not "none" ? leader : null);
    }

    private static TableBorders Borders(XElement borders) => borders == null
        ? null
        : new TableBorders(
            Border(borders.Element(W + "top")),
            Border(borders.Element(W + "bottom")),
            Border(borders.Element(W + "left") ?? borders.Element(W + "start")),
            Border(borders.Element(W + "right") ?? borders.Element(W + "end")),
            Border(borders.Element(W + "insideH")),
            Border(borders.Element(W + "insideV")));

    private static Border Border(XElement border) => border == null
        ? null
        : new Border((string)border.Attribute(W + "val"), (Int(border, "sz") ?? 0) / 8.0, Color(border, "color", "themeColor", "themeShade", "themeTint") ?? "auto");

    private static TableLook Look(XElement look)
    {
        if (look == null) return TableLook.None;
        var legacy = int.TryParse((string)look.Attribute(W + "val"), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bits) ? bits : 0;
        bool Flag(string name, int bit) => (string)look.Attribute(W + name) is { } value ? value is "1" or "true" or "on" : (legacy & bit) != 0;
        var result = TableLook.None;
        if (Flag("firstRow", 0x20)) result |= TableLook.FirstRow;
        if (Flag("lastRow", 0x40)) result |= TableLook.LastRow;
        if (Flag("firstColumn", 0x80)) result |= TableLook.FirstColumn;
        if (Flag("lastColumn", 0x100)) result |= TableLook.LastColumn;
        if (!Flag("noHBand", 0x200)) result |= TableLook.BandedRows;
        if (!Flag("noVBand", 0x400)) result |= TableLook.BandedColumns;
        return result;
    }
}
