using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class ChartXmlWriter
{
    internal const string ChartContentType = "application/vnd.openxmlformats-officedocument.drawingml.chart+xml";
    internal const string DrawingContentType = "application/vnd.openxmlformats-officedocument.drawing+xml";
    private const string ChartNs = "http://schemas.openxmlformats.org/drawingml/2006/chart";
    private const string MainNs = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private const string SheetDrawingNs = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
    private const string StrictPrefix = "http://purl.oclc.org/ooxml/drawingml/";

    private static readonly string[] ChartOrder =
        ["title", "autoTitleDeleted", "pivotFmts", "view3D", "floor", "sideWall", "backWall", "plotArea", "legend", "plotVisOnly", "dispBlanksAs", "showDLblsOverMax", "extLst"];

    private static readonly string[] SeriesOrder =
    [
        "idx", "order", "tx", "spPr", "invertIfNegative", "pictureOptions", "marker", "explosion", "dPt", "dLbls", "trendline", "errBars", "cat", "xVal", "val", "yVal",
        "shape", "smooth", "bubbleSize", "bubble3D", "extLst"
    ];

    private static readonly HashSet<string> AfterSeries =
        ["dLbls", "gapWidth", "overlap", "serLines", "dropLines", "hiLowLines", "upDownBars", "marker", "smooth", "firstSliceAng", "holeSize", "axId", "extLst"];

    internal static XElement NewDrawing() =>
        new(XName.Get("wsDr", SheetDrawingNs), new XAttribute(XNamespace.Xmlns + "xdr", SheetDrawingNs), new XAttribute(XNamespace.Xmlns + "a", MainNs));

    internal static XElement ShiftDrawing(XElement root, Axis axis, int index, int count)
    {
        var copy = new XElement(root);
        var changed = false;
        foreach (var anchor in copy.Descendants().Where(e => e.Name.LocalName is "twoCellAnchor" or "oneCellAnchor"))
        {
            if (Child(anchor, "from") is not { } fromMarker) continue;
            var toMarker = Child(anchor, "to");
            var (from, fromOffset) = ChartXmlReader.Marker(fromMarker);
            var (to, toOffset) = toMarker != null ? ChartXmlReader.Marker(toMarker) : (from, fromOffset);
            var moved = SheetChart.MoveAnchor(ChartXmlReader.EditAs(anchor), from, fromOffset, to, toOffset, axis, index, count);
            if (moved == (from, fromOffset, to, toOffset)) continue;
            changed = true;
            SetMarker(fromMarker, moved.From, moved.FromOffset);
            SetMarker(toMarker, moved.To, moved.ToOffset);
        }

        return changed ? copy : root;
    }

    internal static void SetAnchor(XElement anchor, SheetChart chart)
    {
        SetMarker(Child(anchor, "from"), chart.From, chart.FromOffset);
        SetMarker(Child(anchor, "to"), chart.To, chart.ToOffset);
    }

    internal static XElement NewAnchor(XElement root, SheetChart chart, XNamespace r, string relationshipId, int shapeId)
    {
        var xdr = root.Name.Namespace;
        var strict = xdr.NamespaceName.StartsWith(StrictPrefix, StringComparison.Ordinal);
        XNamespace a = strict ? StrictPrefix + "main" : MainNs;
        XNamespace c = strict ? StrictPrefix + "chart" : ChartNs;
        XElement Marker(string name) => new(xdr + name, new XElement(xdr + "col", 0), new XElement(xdr + "colOff", 0), new XElement(xdr + "row", 0), new XElement(xdr + "rowOff", 0));
        var anchor = new XElement(xdr + "twoCellAnchor", Marker("from"), Marker("to"),
            new XElement(xdr + "graphicFrame", new XAttribute("macro", ""),
                new XElement(xdr + "nvGraphicFramePr",
                    new XElement(xdr + "cNvPr", new XAttribute("id", shapeId), new XAttribute("name", "Chart " + (shapeId - 1))), new XElement(xdr + "cNvGraphicFramePr")),
                new XElement(xdr + "xfrm", new XElement(a + "off", new XAttribute("x", 0), new XAttribute("y", 0)),
                    new XElement(a + "ext", new XAttribute("cx", 0), new XAttribute("cy", 0))),
                new XElement(a + "graphic", new XElement(a + "graphicData", new XAttribute("uri", c.NamespaceName),
                    new XElement(c + "chart", new XAttribute(XNamespace.Xmlns + "c", c), new XAttribute(XNamespace.Xmlns + "r", r), new XAttribute(r + "id", relationshipId))))),
            new XElement(xdr + "clientData"));
        SetAnchor(anchor, chart);
        return anchor;
    }

    internal static XElement Create(SheetChart chart, Workbook wb, Worksheet host)
    {
        XNamespace c = ChartNs;
        XNamespace a = MainNs;
        return new XElement(c + "chartSpace", new XAttribute(XNamespace.Xmlns + "c", c), new XAttribute(XNamespace.Xmlns + "a", a),
            new XAttribute(XNamespace.Xmlns + "r", RelationshipsNs), Val(c, "roundedCorners", "0"),
            new XElement(c + "chart", chart.Title == null ? null : Title(c, a, chart.Title, null), Val(c, "autoTitleDeleted", chart.Title == null ? "1" : "0"),
                new XElement(c + "plotArea", new XElement(c + "layout"), Plot(c, a, chart, wb, host, "1", "2"), Axes(c, chart.Kind, "1", "2")),
                chart.ShowLegend ? Legend(c) : null, Val(c, "plotVisOnly", "1"), Val(c, "dispBlanksAs", "gap")));
    }

    internal static XElement Patch(SheetChart chart, Workbook wb, Worksheet host)
    {
        var before = ChartXmlReader.Chart(chart.Source, false, null, null);
        var root = new XElement(chart.Source);
        var types = Child(Child(root, "chart"), "plotArea")?.Elements().Where(e => e.Name.LocalName.EndsWith("Chart", StringComparison.Ordinal)).ToList() ?? [];
        var slots = before.Kind == ChartKind.Other ? [] : types[0].Elements().Where(e => e.Name.LocalName == "ser").SelectMany(s => ChartXmlReader.Slots(s).All).ToHashSet();
        foreach (var (f, text) in root.Descendants().Where(e => e.Name.LocalName == "f" && !slots.Contains(e)).Zip(chart.OtherRefs))
            if (f.Value != text) f.Value = text;
        if (chart.Kind == ChartKind.Other || before.Kind == ChartKind.Other) return root;

        var c = root.Name.Namespace;
        var a = root.GetNamespaceOfPrefix("a") ?? MainNs;
        var type = types[0];
        if (chart.Kind != before.Kind)
        {
            var axes = type.Parent!.Elements().Where(e => e.Name.LocalName.EndsWith("Ax", StringComparison.Ordinal)).ToList();
            var keep = before.Kind is ChartKind.Column or ChartKind.Line or ChartKind.Area && chart.Kind is ChartKind.Column or ChartKind.Line or ChartKind.Area && axes.Count == 2;
            var ids = keep ? type.Elements().Where(e => e.Name.LocalName == "axId").Select(ChartXmlReader.Val).ToList() : ["1", "2"];
            type.ReplaceWith(Plot(c, a, chart, wb, host, ids[0], ids[^1]));
            if (!keep)
            {
                axes.ForEach(x => x.Remove());
                Child(Child(root, "chart"), "plotArea")!.Elements().Last(e => e.Name.LocalName.EndsWith("Chart", StringComparison.Ordinal)).AddAfterSelf(Axes(c, chart.Kind, "1", "2"));
            }
        }
        else
        {
            PatchSeries(type, before, chart, wb, host, c, a);
            if (chart.Stacked != before.Stacked && Child(type, "grouping") is { } grouping)
            {
                grouping.SetAttributeValue("val", chart.Stacked ? "stacked" : chart.Kind is ChartKind.Column or ChartKind.Bar ? "clustered" : "standard");
                if (chart.Kind is ChartKind.Column or ChartKind.Bar)
                {
                    Child(type, "overlap")?.Remove();
                    if (chart.Stacked) (Child(type, "gapWidth") ?? type.Elements().Last(e => e.Name.LocalName is "ser" or "dLbls" or "varyColors")).AddAfterSelf(Val(c, "overlap", "100"));
                }
            }
        }

        var space = Child(root, "chart");
        if (chart.Title != before.Title) SetTitle(space, c, a, chart.Title);
        if (chart.ShowLegend != before.ShowLegend)
        {
            if (chart.ShowLegend) Insert(space, Legend(c), ChartOrder);
            else Child(space, "legend")?.Remove();
        }

        return root;
    }

    private static void PatchSeries(XElement type, SheetChart before, SheetChart chart, Workbook wb, Worksheet host, XNamespace c, XNamespace a)
    {
        var sers = type.Elements().Where(e => e.Name.LocalName == "ser").ToList();
        var next = sers.Select(s => int.TryParse(ChartXmlReader.Val(Child(s, "idx")), out var i) ? i : 0).DefaultIfEmpty(-1).Max() + 1;
        for (var i = 0; i < Math.Max(sers.Count, chart.Series.Count); i++)
        {
            if (i >= chart.Series.Count)
            {
                sers[i].Remove();
                continue;
            }

            if (i >= sers.Count)
            {
                var ser = Series(c, a, chart.Kind, next++, chart.Series[i], wb, host);
                if (type.Elements().LastOrDefault(e => e.Name.LocalName == "ser") is { } last) last.AddAfterSelf(ser);
                else if (type.Elements().FirstOrDefault(e => AfterSeries.Contains(e.Name.LocalName)) is { } after) after.AddBeforeSelf(ser);
                else type.Add(ser);
                continue;
            }

            var (old, now, element) = (before.Series[i], chart.Series[i], sers[i]);
            var (name, categories, values, _) = ChartXmlReader.Slots(element);
            if (now.NameRef != old.NameRef || now.NameRef == null && now.Name != old.Name)
            {
                if (name != null && now.NameRef != null) name.Value = now.NameRef;
                else
                {
                    Child(element, "tx")?.Remove();
                    if (now.NameRef != null || !string.IsNullOrEmpty(now.Name)) Insert(element, SeriesName(c, now, wb, host), SeriesOrder);
                }
            }

            var scatter = chart.Kind == ChartKind.Scatter;
            if (now.CategoriesRef != old.CategoriesRef)
            {
                if (categories != null && now.CategoriesRef != null) categories.Value = now.CategoriesRef;
                else
                {
                    Child(element, scatter ? "xVal" : "cat")?.Remove();
                    if (now.CategoriesRef != null) Insert(element, Data(c, scatter ? "xVal" : "cat", now.CategoriesRef, !scatter, wb, host), SeriesOrder);
                }
            }

            if (now.ValuesRef != old.ValuesRef)
            {
                if (values != null && now.ValuesRef != null) values.Value = now.ValuesRef;
                else
                {
                    Child(element, scatter ? "yVal" : "val")?.Remove();
                    if (now.ValuesRef != null) Insert(element, Data(c, scatter ? "yVal" : "val", now.ValuesRef, false, wb, host), SeriesOrder);
                }
            }

            if (now.Color != old.Color) SetColor(element, c, a, chart.Kind, now.Color);
        }
    }

    private static XElement Plot(XNamespace c, XNamespace a, SheetChart chart, Workbook wb, Worksheet host, string axis1, string axis2)
    {
        var series = chart.Series.Select((s, i) => Series(c, a, chart.Kind, i, s, wb, host));
        XElement[] ids = [Val(c, "axId", axis1), Val(c, "axId", axis2)];
        var grouping = Val(c, "grouping", chart.Stacked ? "stacked" : chart.Kind is ChartKind.Column or ChartKind.Bar ? "clustered" : "standard");
        return chart.Kind switch
        {
            ChartKind.Column or ChartKind.Bar => new XElement(c + "barChart", Val(c, "barDir", chart.Kind == ChartKind.Bar ? "bar" : "col"), grouping, Val(c, "varyColors", "0"),
                series, Val(c, "gapWidth", "150"), chart.Stacked ? Val(c, "overlap", "100") : null, ids),
            ChartKind.Line => new XElement(c + "lineChart", grouping, Val(c, "varyColors", "0"), series, Val(c, "marker", "1"), ids),
            ChartKind.Area => new XElement(c + "areaChart", grouping, Val(c, "varyColors", "0"), series, ids),
            ChartKind.Pie => new XElement(c + "pieChart", Val(c, "varyColors", "1"), series, Val(c, "firstSliceAng", "0")),
            _ => new XElement(c + "scatterChart", Val(c, "scatterStyle", "lineMarker"), Val(c, "varyColors", "0"), series, ids)
        };
    }

    private static XElement Series(XNamespace c, XNamespace a, ChartKind kind, int index, ChartSeries s, Workbook wb, Worksheet host)
    {
        var scatter = kind == ChartKind.Scatter;
        var ser = new XElement(c + "ser", Val(c, "idx", index.ToString(CultureInfo.InvariantCulture)), Val(c, "order", index.ToString(CultureInfo.InvariantCulture)),
            s.NameRef != null || !string.IsNullOrEmpty(s.Name) ? SeriesName(c, s, wb, host) : null,
            kind is ChartKind.Column or ChartKind.Bar ? Val(c, "invertIfNegative", "0") : null,
            kind == ChartKind.Line ? new XElement(c + "marker", Val(c, "symbol", "none")) : null,
            s.CategoriesRef == null ? null : Data(c, scatter ? "xVal" : "cat", s.CategoriesRef, !scatter, wb, host),
            Data(c, scatter ? "yVal" : "val", s.ValuesRef, false, wb, host),
            kind is ChartKind.Line or ChartKind.Scatter ? Val(c, "smooth", "0") : null);
        if (s.Color != null) SetColor(ser, c, a, kind, s.Color);
        return ser;
    }

    private static XElement SeriesName(XNamespace c, ChartSeries s, Workbook wb, Worksheet host) =>
        new(c + "tx", s.NameRef == null
            ? new XElement(c + "v", SharedStringsWriter.Escape(s.Name, false))
            : new XElement(c + "strRef", new XElement(c + "f", s.NameRef), Cache(c, "strCache", [s.Name ?? ""])));

    private static XElement Data(XNamespace c, string name, string reference, bool text, Workbook wb, Worksheet host)
    {
        var cells = ChartDataResolver.Cells(wb, host, reference) ?? [];
        text |= name == "xVal" && cells.Any(x => x.Value.Kind is not (CellValueKind.Empty or CellValueKind.Number));
        var values = cells.Select(x => text ? x.Value.Kind == CellValueKind.Empty ? null : x.Value.ToString()
                : x.Value.Kind == CellValueKind.Number ? SheetXmlWriter.Number(x.Value.NumberValue) : null).ToList();
        return new XElement(c + name, new XElement(c + (text ? "strRef" : "numRef"), new XElement(c + "f", reference), Cache(c, text ? "strCache" : "numCache", values)));
    }

    private static XElement Cache(XNamespace c, string name, IReadOnlyList<string> values) =>
        new(c + name, name == "numCache" ? new XElement(c + "formatCode", "General") : null, Val(c, "ptCount", values.Count.ToString(CultureInfo.InvariantCulture)),
            values.Select((v, i) => v == null ? null : new XElement(c + "pt", new XAttribute("idx", i), new XElement(c + "v", SharedStringsWriter.Escape(v, false)))));

    private static IEnumerable<XElement> Axes(XNamespace c, ChartKind kind, string axis1, string axis2)
    {
        XElement Axis(string type, string id, string cross, string position, bool grid, params XElement[] tail) =>
            new(c + type, Val(c, "axId", id), new XElement(c + "scaling", Val(c, "orientation", "minMax")), Val(c, "delete", "0"), Val(c, "axPos", position),
                grid ? new XElement(c + "majorGridlines") : null, new XElement(c + "numFmt", new XAttribute("formatCode", "General"), new XAttribute("sourceLinked", "1")),
                Val(c, "majorTickMark", "out"), Val(c, "minorTickMark", "none"), Val(c, "tickLblPos", "nextTo"), Val(c, "crossAx", cross), Val(c, "crosses", "autoZero"), tail);
        if (kind == ChartKind.Pie) return [];
        var bar = kind == ChartKind.Bar;
        if (kind == ChartKind.Scatter)
            return [Axis("valAx", axis1, axis2, "b", false, Val(c, "crossBetween", "midCat")), Axis("valAx", axis2, axis1, "l", true, Val(c, "crossBetween", "midCat"))];
        return
        [
            Axis("catAx", axis1, axis2, bar ? "l" : "b", false, Val(c, "auto", "1"), Val(c, "lblAlgn", "ctr"), Val(c, "lblOffset", "100"), Val(c, "noMultiLvlLbl", "0")),
            Axis("valAx", axis2, axis1, bar ? "b" : "l", true, Val(c, "crossBetween", "between"))
        ];
    }

    private static XElement Legend(XNamespace c) => new(c + "legend", Val(c, "legendPos", "r"), Val(c, "overlay", "0"));

    private static XElement Title(XNamespace c, XNamespace a, string text, XElement template)
    {
        var p = Child(Child(Child(template, "tx"), "rich"), "p");
        var pPr = Child(p, "pPr");
        var rPr = Child(Child(p, "r"), "rPr");
        var rich = new XElement(c + "rich", new XElement(a + "bodyPr"), new XElement(a + "lstStyle"),
            text.Split('\n').Select(line => new XElement(a + "p", pPr == null ? null : new XElement(pPr), new XElement(a + "r", rPr == null ? null : new XElement(rPr), new XElement(a + "t", SharedStringsWriter.Escape(line, false))))));
        if (Child(Child(template, "tx"), "rich") is { } old)
        {
            rich.Element(a + "bodyPr")!.ReplaceWith(Child(old, "bodyPr") ?? new XElement(a + "bodyPr"));
            rich.Element(a + "lstStyle")!.ReplaceWith(Child(old, "lstStyle") ?? new XElement(a + "lstStyle"));
        }

        return new XElement(c + "title", new XElement(c + "tx", rich), Val(c, "overlay", "0"));
    }

    private static void SetTitle(XElement chart, XNamespace c, XNamespace a, string text)
    {
        var title = Child(chart, "title");
        var deleted = Child(chart, "autoTitleDeleted");
        if (deleted == null) Insert(chart, deleted = Val(c, "autoTitleDeleted", "0"), ChartOrder);
        deleted.SetAttributeValue("val", text == null ? "1" : "0");
        if (text == null)
        {
            title?.Remove();
            return;
        }

        var fresh = Title(c, a, text, title);
        if (title == null) chart.AddFirst(fresh);
        else
        {
            Child(title, "tx")?.Remove();
            title.AddFirst(fresh.Element(c + "tx"));
        }
    }

    private static void SetColor(XElement ser, XNamespace c, XNamespace a, ChartKind kind, string color)
    {
        var spPr = Child(ser, "spPr");
        if (spPr == null)
        {
            if (color == null) return;
            Insert(ser, spPr = new XElement(c + "spPr"), SeriesOrder);
        }

        var line = kind is ChartKind.Line or ChartKind.Scatter;
        var target = spPr;
        if (line)
        {
            target = Child(spPr, "ln");
            if (target == null) spPr.Add(target = new XElement(a + "ln"));
        }

        foreach (var fill in target.Elements().Where(e => e.Name.LocalName is "noFill" or "solidFill" or "gradFill" or "pattFill" or "blipFill" or "grpFill").ToList())
            fill.Remove();
        if (color == null) return;
        var solid = new XElement(a + "solidFill", new XElement(a + "srgbClr", new XAttribute("val", color.TrimStart('#').ToUpperInvariant())));
        if (target.Elements().LastOrDefault(e => e.Name.LocalName is "xfrm" or "custGeom" or "prstGeom") is { } geometry) geometry.AddAfterSelf(solid);
        else target.AddFirst(solid);
    }

    private static void Insert(XElement parent, XElement child, string[] order)
    {
        var rank = Array.IndexOf(order, child.Name.LocalName);
        if (parent.Elements().FirstOrDefault(e => Array.IndexOf(order, e.Name.LocalName) > rank) is { } after) after.AddBeforeSelf(child);
        else parent.Add(child);
    }

    private static void SetMarker(XElement marker, CellRef cell, (double X, double Y) offset)
    {
        if (marker == null) return;
        var (current, currentOffset) = ChartXmlReader.Marker(marker);
        void Set(string name, long value) => (Child(marker, name) ?? Add(marker, name)).Value = value.ToString(CultureInfo.InvariantCulture);
        if (current.Column != cell.Column) Set("col", cell.Column);
        if (current.Row != cell.Row) Set("row", cell.Row);
        if (Math.Abs(currentOffset.X - offset.X) > 1e-6) Set("colOff", (long)Math.Round(offset.X * ChartXmlReader.EmuPerPixel));
        if (Math.Abs(currentOffset.Y - offset.Y) > 1e-6) Set("rowOff", (long)Math.Round(offset.Y * ChartXmlReader.EmuPerPixel));
    }

    private static XElement Add(XElement marker, string name)
    {
        var e = new XElement(marker.Name.Namespace + name);
        marker.Add(e);
        return e;
    }

    private static XElement Val(XNamespace c, string name, string value) => new(c + name, new XAttribute("val", value));
}
