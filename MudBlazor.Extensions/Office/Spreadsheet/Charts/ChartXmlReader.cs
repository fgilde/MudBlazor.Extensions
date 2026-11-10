using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class ChartXmlReader
{
    internal const double EmuPerPixel = 9525;

    private static readonly Dictionary<string, ChartKind> Kinds = new()
    {
        ["barChart"] = ChartKind.Column, ["lineChart"] = ChartKind.Line, ["pieChart"] = ChartKind.Pie, ["areaChart"] = ChartKind.Area, ["scatterChart"] = ChartKind.Scatter
    };

    internal static void Read(ZipArchive zip, XlsxSheetPart part, Worksheet ws)
    {
        var id = RelationshipId(part.Children.FirstOrDefault(c => c.Name.LocalName == "drawing"));
        if (id == null || part.PartName == null) return;
        var rel = ReadRelationships(zip, part.PartName).FirstOrDefault(r => r.Id == id && !r.External);
        if (rel == null || Find(zip, ResolveTarget(part.PartName, rel.Target)) is not { } entry) return;
        part.DrawingPart = entry.FullName;
        part.DrawingRelationshipId = id;
        part.DrawingParts.Add(entry.FullName);
        part.DrawingRoot = part.OriginalDrawing = Load(entry);
        if (part.DrawingRoot == null) return;
        part.DrawingLinked = part.DrawingRoot.Descendants().Any(e => !string.IsNullOrEmpty((string)e.Attribute("textlink")));

        var rels = ReadRelationships(zip, entry.FullName);
        var charts = new List<SheetChart>();
        foreach (var element in part.DrawingRoot.Elements())
        {
            var readOnly = element.Name.LocalName != "twoCellAnchor";
            var anchor = element.Name.LocalName == "AlternateContent" ? Child(element, "Choice")?.Elements().FirstOrDefault() : element;
            var chartId = RelationshipId(Child(Child(Child(Child(anchor, "graphicFrame"), "graphic"), "graphicData"), "chart"));
            if (chartId == null || rels.FirstOrDefault(r => r.Id == chartId && !r.External) is not { } chartRel) continue;
            var chartPart = ResolveTarget(entry.FullName, chartRel.Target);
            if (Find(zip, chartPart) is not { } chartEntry || Load(chartEntry) is not { } root)
            {
                part.DrawingLinked = true;
                continue;
            }

            Collect(zip, chartEntry.FullName, part.DrawingParts);
            var chart = Chart(root, readOnly, chartEntry.FullName, chartId);
            var (from, fromOffset, to, toOffset) = Anchor(anchor, ws);
            charts.Add(chart.Place(from, fromOffset, to, toOffset));
        }

        ws.Charts = part.OriginalCharts = charts;
    }

    internal static SheetChart Chart(XElement root, bool readOnly, string partName, string relationshipId)
    {
        var chart = Child(root, "chart");
        var plot = Child(chart, "plotArea");
        var types = plot?.Elements().Where(e => e.Name.LocalName.EndsWith("Chart", StringComparison.Ordinal)).ToList() ?? [];
        var kind = types.Count == 1 && Kinds.TryGetValue(types[0].Name.LocalName, out var k) ? k : ChartKind.Other;
        if (kind == ChartKind.Column && Val(Child(types[0], "barDir")) == "bar") kind = ChartKind.Bar;
        var series = kind == ChartKind.Other ? [] : types[0].Elements().Where(e => e.Name.LocalName == "ser").Select(Series).ToList();
        var slots = kind == ChartKind.Other ? [] : types[0].Elements().Where(e => e.Name.LocalName == "ser").SelectMany(s => Slots(s).All).ToHashSet();
        var other = root.Descendants().Where(e => e.Name.LocalName == "f" && !slots.Contains(e)).Select(e => e.Value).ToList();
        return SheetChart.Create(kind, Title(chart, series), kind != ChartKind.Other && Val(Child(types[0], "grouping")) == "stacked", Child(chart, "legend") != null,
            series, other, readOnly, root, partName, relationshipId);
    }

    internal static (XElement Name, XElement Categories, XElement Values, IEnumerable<XElement> All) Slots(XElement ser)
    {
        var name = Child(Child(Child(ser, "tx"), "strRef"), "f");
        var categories = Child((Child(ser, "cat") ?? Child(ser, "xVal"))?.Elements().FirstOrDefault(), "f");
        var values = Child((Child(ser, "val") ?? Child(ser, "yVal"))?.Elements().FirstOrDefault(), "f");
        return (name, categories, values, new[] { name, categories, values }.Where(e => e != null));
    }

    internal static string Title(XElement chart, IReadOnlyList<ChartSeries> series)
    {
        if (Child(chart, "title") is not { } title) return null;
        var tx = Child(title, "tx");
        if (Child(tx, "rich") is { } rich)
            return string.Join("\n", rich.Elements().Where(p => p.Name.LocalName == "p").Select(p => string.Concat(p.Descendants().Where(t => t.Name.LocalName == "t").Select(t => t.Value))));
        if (Child(tx, "strRef") is { } strRef) return Cache(strRef).FirstOrDefault() ?? "";
        return series.Count == 1 ? series[0].Name : "Chart Title";
    }

    internal static string Color(XElement ser)
    {
        var spPr = Child(ser, "spPr");
        var rgb = Val(Child(Child(spPr, "solidFill"), "srgbClr") ?? Child(Child(Child(spPr, "ln"), "solidFill"), "srgbClr"));
        return rgb is { Length: 6 } ? "#" + rgb.ToUpperInvariant() : null;
    }

    internal static string Val(XElement e) => (string)e?.Attribute("val");

    internal static string RelationshipId(XElement e) =>
        (string)e?.Attributes().FirstOrDefault(a => a.Name.LocalName == "id" && a.Name.Namespace != XNamespace.None);

    internal static (CellRef Cell, (double X, double Y) Offset) Marker(XElement marker)
    {
        int Int(string name) => int.TryParse(Child(marker, name)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? Math.Max(0, v) : 0;
        double Px(string name) => long.TryParse(Child(marker, name)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v / EmuPerPixel : 0;
        return (new CellRef(Math.Min(Int("row"), CellRef.MaxRows - 1), Math.Min(Int("col"), CellRef.MaxColumns - 1)), (Px("colOff"), Px("rowOff")));
    }

    private static ChartSeries Series(XElement ser)
    {
        var (name, categories, values, _) = Slots(ser);
        var tx = Child(ser, "tx");
        var label = name != null ? Cache(name.Parent).FirstOrDefault() : Child(tx, "v")?.Value;
        return new ChartSeries(name?.Value, label ?? "", categories?.Value, values?.Value, Color(ser))
        {
            CachedCategories = Cache((Child(ser, "cat") ?? Child(ser, "xVal"))?.Elements().FirstOrDefault()),
            CachedValues = Cache((Child(ser, "val") ?? Child(ser, "yVal"))?.Elements().FirstOrDefault())
                .Select(v => double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : double.NaN).ToList()
        };
    }

    private static List<string> Cache(XElement reference)
    {
        var cache = reference?.Elements().FirstOrDefault(e => e.Name.LocalName is "strCache" or "numCache" or "multiLvlStrCache");
        if (cache == null) return [];
        var points = cache.Descendants().Where(e => e.Name.LocalName == "pt").ToList();
        var count = int.TryParse(Val(Child(cache, "ptCount")), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? Math.Clamp(n, 0, 100_000) : points.Count;
        var list = Enumerable.Repeat("", count).ToList();
        foreach (var pt in points)
            if (int.TryParse((string)pt.Attribute("idx"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) && i >= 0 && i < count && list[i] == "")
                list[i] = Child(pt, "v")?.Value ?? "";
        return list;
    }

    private static (CellRef, (double, double), CellRef, (double, double)) Anchor(XElement anchor, Worksheet ws)
    {
        if (Child(anchor, "from") is { } fromMarker && Child(anchor, "to") is { } toMarker)
        {
            var (from, fromOffset) = Marker(fromMarker);
            var (to, toOffset) = Marker(toMarker);
            return (from, fromOffset, to, toOffset);
        }

        double Emu(XElement e, string name) => long.TryParse((string)e?.Attribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v / EmuPerPixel : 0;
        var ext = Child(anchor, "ext");
        var (start, offset) = Child(anchor, "from") is { } f ? Marker(f) : default;
        if (Child(anchor, "pos") is { } pos)
        {
            var (col, x) = Walk(ws, Axis.Column, 0, Emu(pos, "x"));
            var (row, y) = Walk(ws, Axis.Row, 0, Emu(pos, "y"));
            (start, offset) = (new CellRef(row, col), (x, y));
        }

        var (endCol, endX) = Walk(ws, Axis.Column, start.Column, offset.Item1 + Emu(ext, "cx"));
        var (endRow, endY) = Walk(ws, Axis.Row, start.Row, offset.Item2 + Emu(ext, "cy"));
        return (start, offset, new CellRef(endRow, endCol), (endX, endY));
    }

    private static (int Index, double Offset) Walk(Worksheet ws, Axis axis, int index, double length)
    {
        var limit = axis == Axis.Row ? CellRef.MaxRows : CellRef.MaxColumns;
        while (index < limit - 1)
        {
            var size = axis == Axis.Row ? ws.RowHeight(index) : ws.ColumnWidth(index);
            if (length < size || size <= 0 && length <= 0) break;
            length -= size;
            index++;
        }

        return (index, Math.Max(0, length));
    }

    private static void Collect(ZipArchive zip, string part, HashSet<string> parts)
    {
        if (!parts.Add(part)) return;
        foreach (var rel in ReadRelationships(zip, part).Where(r => !r.External))
            if (Find(zip, ResolveTarget(part, rel.Target)) is { } entry && !IsType(rel, "image") && !IsType(rel, "package"))
                Collect(zip, entry.FullName, parts);
    }

    private static XElement Load(ZipArchiveEntry entry)
    {
        try
        {
            using var s = entry.Open();
            return LoadXml(s).Root;
        }
        catch (XmlException)
        {
            return null;
        }
    }
}
