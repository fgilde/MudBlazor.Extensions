using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class StylesXmlReader
{
    internal static (int[] XfStyleIds, int[] CanonicalXf) Read(Stream styles, string[] theme, StyleTable table)
    {
        var root = LoadXml(styles).Root;
        var indexed = Child(Child(root, "colors"), "indexedColors")?.Elements().Select(e => ThemeColors.Hex((string)e.Attribute("rgb"))).ToArray()
                      ?? ThemeColors.Indexed;
        var formats = Child(root, "numFmts")?.Elements()
            .Where(e => int.TryParse((string)e.Attribute("numFmtId"), out _))
            .GroupBy(e => int.Parse((string)e.Attribute("numFmtId"), CultureInfo.InvariantCulture))
            .ToDictionary(g => g.Key, g => (string)g.First().Attribute("formatCode")) ?? new Dictionary<int, string>();
        var fonts = Items(root, "fonts", e => ReadFont(e, theme, indexed));
        var fills = Items(root, "fills", e => ReadFill(e, theme, indexed));
        var borders = Items(root, "borders", e => ReadBorder(e, theme, indexed));
        var xfs = Child(root, "cellXfs")?.Elements().ToList() ?? [];
        var ids = new int[Math.Max(xfs.Count, 1)];
        CellStyle baseStyle = null;
        for (var i = 0; i < xfs.Count; i++)
        {
            var xf = xfs[i];
            var numFmt = Int(xf, "numFmtId");
            var style = new CellStyle
            {
                Font = fonts.ElementAtOrDefault(Int(xf, "fontId")) ?? new CellFont(),
                Fill = fills.ElementAtOrDefault(Int(xf, "fillId")) ?? new CellFill(),
                Border = borders.ElementAtOrDefault(Int(xf, "borderId")) ?? new CellBorder(),
                Alignment = ReadAlignment(Child(xf, "alignment")),
                NumberFormat = formats.GetValueOrDefault(numFmt) ?? BuiltInNumberFormats.Get(numFmt, CultureInfo.CurrentCulture) ?? NumberFormat.General
            };
            baseStyle ??= style;
            ids[i] = style == baseStyle ? 0 : table.GetOrAdd(style);
        }

        foreach (var dxf in Child(root, "dxfs")?.Elements().Where(e => e.Name.LocalName == "dxf") ?? [])
            table.AddDifferential(ReadDxf(dxf, formats, theme, indexed));

        var canonical = new int[table.Count];
        Array.Fill(canonical, -1);
        canonical[0] = 0;
        for (var i = xfs.Count - 1; i >= 0; i--) canonical[ids[i]] = i;
        return (ids, canonical);
    }

    private static DifferentialStyle ReadDxf(XElement dxf, Dictionary<int, string> formats, string[] theme, string[] indexed)
    {
        var pattern = Child(Child(dxf, "fill"), "patternFill");
        var fill = Child(dxf, "fill") == null ? null
            : new CellFill { Color = ThemeColors.Resolve(Child(pattern, "bgColor") ?? Child(pattern, "fgColor"), theme, indexed) };
        var numFmt = Child(dxf, "numFmt");
        return new DifferentialStyle(Child(dxf, "font") is { } font ? ReadFont(font, theme, indexed) : null, fill,
            Child(dxf, "border") is { } border ? ReadBorder(border, theme, indexed) : null,
            numFmt == null ? null : (string)numFmt.Attribute("formatCode") ?? formats.GetValueOrDefault(Int(numFmt, "numFmtId"))
                                    ?? BuiltInNumberFormats.Get(Int(numFmt, "numFmtId"), CultureInfo.CurrentCulture));
    }

    private static List<T> Items<T>(XElement root, string name, Func<XElement, T> read) =>
        Child(root, name)?.Elements().Select(read).ToList() ?? [];

    private static int Int(XElement e, string name) =>
        int.TryParse((string)e.Attribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static bool Flag(XElement e) => e != null && (string)e.Attribute("val") is null or "1" or "true";

    private static string Val(XElement e) => (string)e?.Attribute("val");

    private static CellFont ReadFont(XElement font, string[] theme, string[] indexed)
    {
        var color = Child(font, "color");
        var defaultText = color != null && (string)color.Attribute("theme") == "1" && color.Attribute("tint") == null
                          || (string)color?.Attribute("indexed") == "64";
        var underline = Child(font, "u");
        return new CellFont
        {
            Name = Val(Child(font, "name")) ?? "Calibri",
            Size = double.TryParse(Val(Child(font, "sz")), NumberStyles.Float, CultureInfo.InvariantCulture, out var size) ? size : 11,
            Bold = Flag(Child(font, "b")),
            Italic = Flag(Child(font, "i")),
            Underline = underline != null && Val(underline) != "none",
            Strikethrough = Flag(Child(font, "strike")),
            Color = defaultText ? null : ThemeColors.Resolve(color, theme, indexed)
        };
    }

    private static CellFill ReadFill(XElement fill, string[] theme, string[] indexed)
    {
        var pattern = Child(fill, "patternFill");
        if (pattern != null)
        {
            var type = (string)pattern.Attribute("patternType");
            if (type is null or "none") return new CellFill();
            var color = type == "solid" || Child(pattern, "fgColor") != null ? Child(pattern, "fgColor") : Child(pattern, "bgColor");
            return new CellFill { Color = (string)color?.Attribute("indexed") == "64" ? null : ThemeColors.Resolve(color, theme, indexed) };
        }

        var stop = Child(Child(fill, "gradientFill"), "stop");
        return new CellFill { Color = ThemeColors.Resolve(Child(stop, "color"), theme, indexed) };
    }

    private static CellBorder ReadBorder(XElement border, string[] theme, string[] indexed)
    {
        CellBorderSide Side(string name)
        {
            var side = Child(border, name) ?? Child(border, name == "left" ? "start" : name == "right" ? "end" : name);
            var style = (string)side?.Attribute("style") switch
            {
                null or "none" => CellBorderStyle.None,
                "thin" or "hair" => CellBorderStyle.Thin,
                "medium" => CellBorderStyle.Medium,
                "thick" => CellBorderStyle.Thick,
                "dotted" => CellBorderStyle.Dotted,
                "double" => CellBorderStyle.Double,
                _ => CellBorderStyle.Dashed
            };
            var color = Child(side, "color");
            return new CellBorderSide
            {
                Style = style,
                Color = style == CellBorderStyle.None || (string)color?.Attribute("indexed") == "64" ? null : ThemeColors.Resolve(color, theme, indexed)
            };
        }

        return new CellBorder { Left = Side("left"), Right = Side("right"), Top = Side("top"), Bottom = Side("bottom") };
    }

    private static CellAlignment ReadAlignment(XElement a) => a == null
        ? new CellAlignment()
        : new CellAlignment
        {
            Horizontal = (string)a.Attribute("horizontal") switch
            {
                "left" or "fill" => CellHorizontalAlignment.Left,
                "center" or "centerContinuous" => CellHorizontalAlignment.Center,
                "right" => CellHorizontalAlignment.Right,
                "justify" or "distributed" => CellHorizontalAlignment.Justify,
                _ => CellHorizontalAlignment.General
            },
            Vertical = (string)a.Attribute("vertical") switch
            {
                "top" => CellVerticalAlignment.Top,
                "center" or "justify" or "distributed" => CellVerticalAlignment.Center,
                _ => CellVerticalAlignment.Bottom
            },
            WrapText = (string)a.Attribute("wrapText") is "1" or "true",
            Indent = Int(a, "indent")
        };
}
