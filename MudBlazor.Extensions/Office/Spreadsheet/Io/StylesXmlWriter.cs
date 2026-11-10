using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal sealed class StylesXmlWriter
{
    private static readonly string[] Order = ["numFmts", "fonts", "fills", "borders", "cellStyleXfs", "cellXfs", "cellStyles", "dxfs", "tableStyles", "colors", "extLst"];

    private readonly XDocument _doc;
    private readonly XNamespace _ns;
    private readonly StyleTable _table;
    private readonly int[] _canonical;
    private readonly CultureInfo _culture;
    private readonly Dictionary<int, int> _added = new();
    private readonly Dictionary<string, int> _parts = new();
    private readonly Dictionary<DifferentialStyle, int> _addedDxfs = new();

    internal StylesXmlWriter(XDocument original, StyleTable table, int[] canonical, CultureInfo culture)
    {
        _doc = original ?? Skeleton();
        _ns = _doc.Root!.Name.Namespace;
        _table = table;
        _canonical = original == null ? [0] : canonical;
        _culture = culture;
        Changed = original == null;
    }

    internal bool Changed { get; private set; }

    internal int Xf(int styleId)
    {
        if (styleId < _canonical.Length && _canonical[styleId] >= 0) return _canonical[styleId];
        if (_added.TryGetValue(styleId, out var xf)) return xf;
        var style = _table[styleId];
        var font = Font(style.Font);
        var fill = style.Fill.Color == null ? null : new XElement(_ns + "fill", new XElement(_ns + "patternFill", new XAttribute("patternType", "solid"),
            new XElement(_ns + "fgColor", Rgb(style.Fill.Color)), new XElement(_ns + "bgColor", new XAttribute("indexed", "64"))));
        var border = style.Border == new CellBorder() ? null : Border(style.Border);
        var alignment = Alignment(style.Alignment);
        var numFmt = NumFmt(style.NumberFormat);
        var element = new XElement(_ns + "xf",
            new XAttribute("numFmtId", numFmt), new XAttribute("fontId", Add("fonts", font)),
            new XAttribute("fillId", fill == null ? 0 : Add("fills", fill)), new XAttribute("borderId", border == null ? 0 : Add("borders", border)),
            new XAttribute("xfId", 0),
            numFmt != 0 ? new XAttribute("applyNumberFormat", 1) : null, new XAttribute("applyFont", 1),
            fill != null ? new XAttribute("applyFill", 1) : null, border != null ? new XAttribute("applyBorder", 1) : null,
            alignment != null ? new XAttribute("applyAlignment", 1) : null, alignment);
        var xfs = Section("cellXfs");
        xf = xfs.Elements().Count();
        xfs.Add(element);
        _added[styleId] = xf;
        Changed = true;
        return xf;
    }

    internal int Dxf(DifferentialStyle style, int? original)
    {
        var known = _table.DifferentialStyles;
        if (original is { } id && id >= 0 && id < known.Count && known[id] == style) return id;
        for (var i = 0; i < known.Count; i++)
            if (known[i] == style)
                return i;
        if (_addedDxfs.TryGetValue(style, out id)) return id;
        var dxfs = Section("dxfs");
        id = dxfs.Elements().Count();
        var font = style.Font;
        XElement Flag(string name, bool? on, string off = "0") => on == null ? null : new XElement(_ns + name, on.Value ? null : new XAttribute("val", off));
        var border = style.Border;
        dxfs.Add(new XElement(_ns + "dxf",
            font == null ? null : new XElement(_ns + "font", Flag("b", font.Bold), Flag("i", font.Italic), Flag("strike", font.Strikethrough),
                Flag("u", font.Underline, "none"),
                font.Size == null ? null : new XElement(_ns + "sz", new XAttribute("val", font.Size.Value.ToString("R", CultureInfo.InvariantCulture))),
                font.Color == null ? null : new XElement(_ns + "color", Rgb(font.Color)),
                font.Name == null ? null : new XElement(_ns + "name", new XAttribute("val", font.Name))),
            style.NumberFormat == null ? null
                : new XElement(_ns + "numFmt", new XAttribute("numFmtId", NumFmt(style.NumberFormat)), new XAttribute("formatCode", style.NumberFormat)),
            style.Fill == null ? null : new XElement(_ns + "fill", new XElement(_ns + "patternFill",
                style.Fill.Color == null ? new XAttribute("patternType", "none") : new XElement(_ns + "bgColor", Rgb(style.Fill.Color)))),
            border == null ? null : new XElement(_ns + "border", Side("left", border.Left, true), Side("right", border.Right, true), Side("top", border.Top, true),
                Side("bottom", border.Bottom, true))));
        _addedDxfs[style] = id;
        Changed = true;
        return id;
    }

    internal void Save(Stream stream)
    {
        foreach (var name in (ReadOnlySpan<string>)["numFmts", "fonts", "fills", "borders", "cellXfs", "dxfs"])
            if (Child(_doc.Root, name) is { } section)
                section.SetAttributeValue("count", section.Elements().Count());
        SheetXmlWriter.Save(_doc, stream);
    }

    private XDocument Skeleton()
    {
        XNamespace ns = SheetXmlWriter.MainNs;
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement(ns + "styleSheet",
            new XElement(ns + "fonts", new XElement(ns + "font", new XElement(ns + "sz", new XAttribute("val", 11)), new XElement(ns + "name", new XAttribute("val", "Calibri")),
                new XElement(ns + "family", new XAttribute("val", 2)))),
            new XElement(ns + "fills", new XElement(ns + "fill", new XElement(ns + "patternFill", new XAttribute("patternType", "none"))),
                new XElement(ns + "fill", new XElement(ns + "patternFill", new XAttribute("patternType", "gray125")))),
            new XElement(ns + "borders", new XElement(ns + "border", new XElement(ns + "left"), new XElement(ns + "right"), new XElement(ns + "top"),
                new XElement(ns + "bottom"), new XElement(ns + "diagonal"))),
            new XElement(ns + "cellStyleXfs", new XAttribute("count", 1), Plain(ns, false)),
            new XElement(ns + "cellXfs", Plain(ns, true)),
            new XElement(ns + "cellStyles", new XAttribute("count", 1),
                new XElement(ns + "cellStyle", new XAttribute("name", "Normal"), new XAttribute("xfId", 0), new XAttribute("builtinId", 0)))));
    }

    private static XElement Plain(XNamespace ns, bool withXfId) => new(ns + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 0),
        new XAttribute("fillId", 0), new XAttribute("borderId", 0), withXfId ? new XAttribute("xfId", 0) : null);

    private XElement Section(string name)
    {
        if (Child(_doc.Root, name) is { } existing) return existing;
        var section = new XElement(_ns + name);
        var rank = Array.IndexOf(Order, name);
        var before = _doc.Root!.Elements().FirstOrDefault(e => e.Name.Namespace == _ns && Array.IndexOf(Order, e.Name.LocalName) > rank);
        if (before != null) before.AddBeforeSelf(section);
        else _doc.Root.Add(section);
        return section;
    }

    private int Add(string section, XElement item)
    {
        var key = section + item.ToString(SaveOptions.DisableFormatting);
        if (_parts.TryGetValue(key, out var index)) return index;
        var target = Section(section);
        index = target.Elements().Count();
        target.Add(item);
        _parts[key] = index;
        return index;
    }

    private int NumFmt(string code)
    {
        if (string.IsNullOrEmpty(code) || code == NumberFormat.General) return 0;
        if (BuiltInNumberFormats.TryGetId(code, out var id, _culture)) return id;
        var numFmts = Section("numFmts");
        var existing = numFmts.Elements().FirstOrDefault(e => (string)e.Attribute("formatCode") == code);
        if (existing != null && int.TryParse((string)existing.Attribute("numFmtId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out id)) return id;
        id = Math.Max(163, numFmts.Elements().Select(e => int.TryParse((string)e.Attribute("numFmtId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0)
            .DefaultIfEmpty(0).Max()) + 1;
        numFmts.Add(new XElement(_ns + "numFmt", new XAttribute("numFmtId", id), new XAttribute("formatCode", code)));
        return id;
    }

    private XAttribute Rgb(string color) => new("rgb", "FF" + color.TrimStart('#').ToUpperInvariant());

    private XElement Font(CellFont f) => new(_ns + "font",
        f.Bold ? new XElement(_ns + "b") : null,
        f.Italic ? new XElement(_ns + "i") : null,
        f.Strikethrough ? new XElement(_ns + "strike") : null,
        f.Underline ? new XElement(_ns + "u") : null,
        new XElement(_ns + "sz", new XAttribute("val", f.Size.ToString("R", CultureInfo.InvariantCulture))),
        f.Color == null ? null : new XElement(_ns + "color", Rgb(f.Color)),
        new XElement(_ns + "name", new XAttribute("val", f.Name ?? "Calibri")));

    private XElement Side(string name, CellBorderSide side, bool explicitNone = false) => side == null ? null : new(_ns + name,
        side.Style == CellBorderStyle.None && !explicitNone ? null : new XAttribute("style", side.Style.ToString().ToLowerInvariant()),
        side.Style == CellBorderStyle.None || side.Color == null ? null : new XElement(_ns + "color", Rgb(side.Color)));

    private XElement Border(CellBorder b) =>
        new(_ns + "border", Side("left", b.Left), Side("right", b.Right), Side("top", b.Top), Side("bottom", b.Bottom), new XElement(_ns + "diagonal"));

    private XElement Alignment(CellAlignment a) => a == new CellAlignment()
        ? null
        : new XElement(_ns + "alignment",
            a.Horizontal == CellHorizontalAlignment.General ? null : new XAttribute("horizontal", a.Horizontal.ToString().ToLowerInvariant()),
            a.Vertical == CellVerticalAlignment.Bottom ? null : new XAttribute("vertical", a.Vertical.ToString().ToLowerInvariant()),
            a.WrapText ? new XAttribute("wrapText", 1) : null,
            a.Indent > 0 ? new XAttribute("indent", a.Indent) : null);
}
