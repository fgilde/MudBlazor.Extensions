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

    internal static string Shading(XElement shading) => Color(shading, "fill", "themeFill", "themeFillShade", "themeFillTint");

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

    internal static TableBorders Borders(XElement borders) => borders == null
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

    internal static readonly string[] RunOrder =
    [
        "ins", "del", "moveFrom", "moveTo", "rStyle", "rFonts", "b", "bCs", "i", "iCs", "caps", "smallCaps", "strike", "dstrike", "outline", "shadow", "emboss",
        "imprint", "noProof", "snapToGrid", "vanish", "webHidden", "color", "spacing", "w", "kern", "position", "sz", "szCs", "highlight", "u", "effect", "bdr",
        "shd", "fitText", "vertAlign", "rtl", "cs", "em", "lang", "eastAsianLayout", "specVanish", "oMath", "rPrChange"
    ];

    internal static readonly string[] ParagraphOrder =
    [
        "pStyle", "keepNext", "keepLines", "pageBreakBefore", "framePr", "widowControl", "numPr", "suppressLineNumbers", "pBdr", "shd", "tabs",
        "suppressAutoHyphens", "kinsoku", "wordWrap", "overflowPunct", "topLinePunct", "autoSpaceDE", "autoSpaceDN", "bidi", "adjustRightInd", "snapToGrid",
        "spacing", "ind", "contextualSpacing", "mirrorIndents", "suppressOverlap", "jc", "textDirection", "textAlignment", "textboxTightWrap", "outlineLvl",
        "divId", "cnfStyle", "rPr", "sectPr", "pPrChange"
    ];

    internal static void PatchRun(XElement rPr, RunFormat format) => PatchRun(rPr, Run(rPr), format);

    internal static void PatchRun(XElement rPr, RunFormat current, RunFormat format)
    {
        if (current.StyleId != format.StyleId) SetVal(rPr, "rStyle", format.StyleId, RunOrder);
        if (current.Font != format.Font) SetFont(rPr, format.Font);
        if (current.Bold != format.Bold) SetOnOff(rPr, "b", format.Bold, RunOrder);
        if (current.Italic != format.Italic) SetOnOff(rPr, "i", format.Italic, RunOrder);
        if (current.Caps != format.Caps) SetOnOff(rPr, "caps", format.Caps, RunOrder);
        if (current.SmallCaps != format.SmallCaps) SetOnOff(rPr, "smallCaps", format.SmallCaps, RunOrder);
        if (current.Strike != format.Strike) SetOnOff(rPr, "strike", format.Strike, RunOrder);
        if (current.DoubleStrike != format.DoubleStrike) SetOnOff(rPr, "dstrike", format.DoubleStrike, RunOrder);
        if (current.Hidden != format.Hidden) SetOnOff(rPr, "vanish", format.Hidden, RunOrder);
        if (current.Color != format.Color) SetColor(rPr, "color", format.Color, RunOrder, "val", "themeColor", "themeShade", "themeTint");
        if (current.SizePt != format.SizePt) SetVal(rPr, "sz", format.SizePt is { } size ? Number((int)Math.Round(size * 2)) : null, RunOrder);
        if (current.Highlight != format.Highlight) SetVal(rPr, "highlight", format.Highlight, RunOrder);
        if (current.Underline != format.Underline) SetVal(rPr, "u", format.Underline, RunOrder);
        if (current.Shading != format.Shading) SetShading(rPr, format.Shading, RunOrder);
        if (current.VerticalAlign != format.VerticalAlign) SetVal(rPr, "vertAlign", format.VerticalAlign?.ToString().ToLowerInvariant(), RunOrder);
    }

    internal static void PatchParagraph(XElement pPr, ParagraphFormat format)
    {
        var current = Paragraph(pPr);
        if (current.StyleId != format.StyleId) SetVal(pPr, "pStyle", format.StyleId, ParagraphOrder);
        if (current.KeepWithNext != format.KeepWithNext) SetOnOff(pPr, "keepNext", format.KeepWithNext, ParagraphOrder);
        if (current.KeepLines != format.KeepLines) SetOnOff(pPr, "keepLines", format.KeepLines, ParagraphOrder);
        if (current.PageBreakBefore != format.PageBreakBefore) SetOnOff(pPr, "pageBreakBefore", format.PageBreakBefore, ParagraphOrder);
        if (current.WidowControl != format.WidowControl) SetOnOff(pPr, "widowControl", format.WidowControl, ParagraphOrder);
        if (current.Numbering != format.Numbering) SetNumbering(pPr, format.Numbering);
        if (current.Shading != format.Shading) SetShading(pPr, format.Shading, ParagraphOrder);
        if (!Equals(current.Tabs, format.Tabs)) SetTabs(pPr, format.Tabs);
        if (current.SpacingBefore != format.SpacingBefore || current.SpacingAfter != format.SpacingAfter || current.Line != format.Line) SetSpacing(pPr, current, format);
        if (current.IndentLeft != format.IndentLeft || current.IndentRight != format.IndentRight || current.IndentFirstLine != format.IndentFirstLine)
            SetIndent(pPr, current, format);
        if (current.Alignment != format.Alignment)
            SetVal(pPr, "jc", format.Alignment switch
            {
                ParagraphAlignment.Left => "left",
                ParagraphAlignment.Center => "center",
                ParagraphAlignment.Right => "right",
                ParagraphAlignment.Justify => "both",
                _ => null
            }, ParagraphOrder);
        if (current.OutlineLevel != format.OutlineLevel) SetVal(pPr, "outlineLvl", Optional(format.OutlineLevel), ParagraphOrder);
    }

    internal static XElement Place(XElement parent, XElement child, string[] order)
    {
        var rank = Rank(child.Name, order);
        if (parent.Elements().FirstOrDefault(e => Rank(e.Name, order) > rank) is { } next) next.AddBeforeSelf(child);
        else parent.Add(child);
        return child;
    }

    internal static XElement Ensure(XElement parent, string name, string[] order) =>
        parent.Element(W + name) ?? Place(parent, new XElement(W + name), order);

    internal static void SetVal(XElement parent, string name, string value, string[] order)
    {
        if (value == null) parent.Element(W + name)?.Remove();
        else Ensure(parent, name, order).SetAttributeValue(W + "val", value);
    }

    internal static void SetOnOff(XElement parent, string name, bool? value, string[] order)
    {
        if (value == null) parent.Element(W + name)?.Remove();
        else Ensure(parent, name, order).SetAttributeValue(W + "val", value.Value ? null : "0");
    }

    internal static void SetColor(XElement element, string color, string value, string theme, string shade, string tint)
    {
        if (color.StartsWith("theme:", StringComparison.Ordinal))
        {
            var parts = color[6..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            element.SetAttributeValue(W + theme, parts.FirstOrDefault());
            element.SetAttributeValue(W + shade, parts.FirstOrDefault(p => p.StartsWith("shade=", StringComparison.Ordinal))?[6..]);
            element.SetAttributeValue(W + tint, parts.FirstOrDefault(p => p.StartsWith("tint=", StringComparison.Ordinal))?[5..]);
            if (value != "fill" && element.Attribute(W + value) == null) element.SetAttributeValue(W + value, "auto");
            return;
        }

        element.SetAttributeValue(W + value, color == "auto" ? "auto" : ThemeColors.Hex(color.TrimStart('#')) ?? "auto");
        element.SetAttributeValue(W + theme, null);
        element.SetAttributeValue(W + shade, null);
        element.SetAttributeValue(W + tint, null);
    }

    internal static void SetShading(XElement parent, string fill, string[] order)
    {
        if (fill == null)
        {
            parent.Element(W + "shd")?.Remove();
            return;
        }

        var shading = parent.Element(W + "shd") ?? Place(parent, new XElement(W + "shd", new XAttribute(W + "val", "clear"), new XAttribute(W + "color", "auto")), order);
        SetColor(shading, fill, "fill", "themeFill", "themeFillShade", "themeFillTint");
    }

    internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    internal static string Optional(int? value) => value is { } v ? Number(v) : null;

    private static int Rank(XName name, string[] order) => name.Namespace == W ? Array.IndexOf(order, name.LocalName) : int.MaxValue;

    private static void SetColor(XElement parent, string name, string color, string[] order, string value, string theme, string shade, string tint)
    {
        if (color == null) parent.Element(W + name)?.Remove();
        else SetColor(Ensure(parent, name, order), color, value, theme, shade, tint);
    }

    private static void SetFont(XElement rPr, string font)
    {
        var fonts = Ensure(rPr, "rFonts", RunOrder);
        var theme = font?.StartsWith("theme:", StringComparison.Ordinal) == true;
        foreach (var (name, isTheme) in new[] { ("ascii", false), ("hAnsi", false), ("asciiTheme", true), ("hAnsiTheme", true) })
            fonts.SetAttributeValue(W + name, font == null || isTheme != theme ? null : theme ? font[6..] : font);
        if (!fonts.HasAttributes && !fonts.HasElements) fonts.Remove();
    }

    private static void SetNumbering(XElement pPr, NumberingRef? numbering)
    {
        if (numbering is not { } value)
        {
            pPr.Element(W + "numPr")?.Remove();
            return;
        }

        var numPr = Ensure(pPr, "numPr", ParagraphOrder);
        string[] order = ["ilvl", "numId", "numberingChange", "ins"];
        SetVal(numPr, "ilvl", Number(value.Level), order);
        SetVal(numPr, "numId", Number(value.NumId), order);
    }

    private static void SetTabs(XElement pPr, IReadOnlyList<TabStop> tabs)
    {
        pPr.Element(W + "tabs")?.Remove();
        if (tabs is not { Count: > 0 }) return;
        Place(pPr, new XElement(W + "tabs", tabs.Select(t => new XElement(W + "tab",
            new XAttribute(W + "val", t.Alignment.ToString().ToLowerInvariant()),
            t.Leader == null ? null : new XAttribute(W + "leader", t.Leader),
            new XAttribute(W + "pos", t.Position)))), ParagraphOrder);
    }

    private static void SetSpacing(XElement pPr, ParagraphFormat current, ParagraphFormat format)
    {
        var spacing = Ensure(pPr, "spacing", ParagraphOrder);
        if (current.SpacingBefore != format.SpacingBefore) SetAttributes(spacing, ("before", Optional(format.SpacingBefore)), ("beforeLines", null), ("beforeAutospacing", null));
        if (current.SpacingAfter != format.SpacingAfter) SetAttributes(spacing, ("after", Optional(format.SpacingAfter)), ("afterLines", null), ("afterAutospacing", null));
        if (current.Line != format.Line)
            SetAttributes(spacing, ("line", Optional(format.Line?.Value)),
                ("lineRule", format.Line?.Rule switch { LineRule.Exact => "exact", LineRule.AtLeast => "atLeast", null => null, _ => "auto" }));
        if (!spacing.HasAttributes) spacing.Remove();
    }

    private static void SetIndent(XElement pPr, ParagraphFormat current, ParagraphFormat format)
    {
        var indent = Ensure(pPr, "ind", ParagraphOrder);
        if (current.IndentLeft != format.IndentLeft)
            SetAttributes(indent, ("left", null), ("start", null), ("leftChars", null), ("startChars", null),
                (indent.Attribute(W + "start") != null ? "start" : "left", Optional(format.IndentLeft)));
        if (current.IndentRight != format.IndentRight)
            SetAttributes(indent, ("right", null), ("end", null), ("rightChars", null), ("endChars", null),
                (indent.Attribute(W + "end") != null ? "end" : "right", Optional(format.IndentRight)));
        if (current.IndentFirstLine != format.IndentFirstLine)
            SetAttributes(indent, ("firstLine", format.IndentFirstLine is >= 0 ? Optional(format.IndentFirstLine) : null),
                ("hanging", format.IndentFirstLine is < 0 ? Number(-format.IndentFirstLine.Value) : null), ("firstLineChars", null), ("hangingChars", null));
        if (!indent.HasAttributes) indent.Remove();
    }

    private static void SetAttributes(XElement element, params (string Name, string Value)[] attributes)
    {
        foreach (var (name, value) in attributes) element.SetAttributeValue(W + name, value);
    }
}
