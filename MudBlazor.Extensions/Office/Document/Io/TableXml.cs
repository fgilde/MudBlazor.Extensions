using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal static class TableXml
{
    internal static Table Read(XElement table, Func<IEnumerable<XElement>, List<Block>> blocks) => new()
    {
        Format = FormatXml.Table(table.Element(W + "tblPr")) ?? new TableFormat(),
        Grid = table.Element(W + "tblGrid")?.Elements(W + "gridCol").Select(c => Int(c, "w") ?? 0).ToList() ?? [],
        Rows = Children(table, "tr").Where(r => r.Element(W + "trPr")?.Element(W + "del") == null).Select(r => Row(r, blocks)).ToList(),
        Source = table
    };

    private static TableRow Row(XElement row, Func<IEnumerable<XElement>, List<Block>> blocks)
    {
        var properties = row.Element(W + "trPr");
        return new TableRow(Children(row, "tc").Select(c => Cell(c, blocks)).ToList())
        {
            Height = Int(properties?.Element(W + "trHeight"), "val"),
            IsHeader = properties != null && OnOff(properties, "tblHeader") == true,
            GridBefore = Math.Max(0, Int(properties?.Element(W + "gridBefore"), "val") ?? 0),
            GridAfter = Math.Max(0, Int(properties?.Element(W + "gridAfter"), "val") ?? 0),
            Source = row
        };
    }

    private static TableCell Cell(XElement cell, Func<IEnumerable<XElement>, List<Block>> blocks)
    {
        var properties = cell.Element(W + "tcPr");
        var merge = properties?.Element(W + "vMerge");
        var content = blocks(cell.Elements().Where(e => e.Name != W + "tcPr"));
        if (!content.OfType<Paragraph>().Any()) content.Add(new Paragraph());
        return new TableCell(content)
        {
            GridSpan = Math.Max(1, Int(properties?.Element(W + "gridSpan"), "val") ?? 1),
            VMerge = merge == null ? VerticalMerge.None : (string)merge.Attribute(W + "val") == "restart" ? VerticalMerge.Restart : VerticalMerge.Continue,
            Width = properties?.Element(W + "tcW") is { } width && (string)width.Attribute(W + "type") == "dxa" ? Int(width, "w") : null,
            Shading = Shading(properties?.Element(W + "shd")),
            Borders = Borders(properties?.Element(W + "tcBorders")),
            VerticalAlign = Val(properties, "vAlign") switch
            {
                "top" => CellVerticalAlignment.Top,
                "center" => CellVerticalAlignment.Center,
                "bottom" => CellVerticalAlignment.Bottom,
                _ => null
            },
            Source = cell
        };
    }

    private static readonly string[] TableOrder =
    [
        "tblStyle", "tblpPr", "tblOverlap", "bidiVisual", "tblStyleRowBandSize", "tblStyleColBandSize", "tblW", "jc", "tblCellSpacing", "tblInd", "tblBorders",
        "shd", "tblLayout", "tblCellMar", "tblLook", "tblCaption", "tblDescription", "tblPrChange"
    ];

    private static readonly string[] RowOrder =
        ["cnfStyle", "divId", "gridBefore", "gridAfter", "wBefore", "wAfter", "cantSplit", "trHeight", "tblHeader", "tblCellSpacing", "jc", "hidden", "ins", "del", "trPrChange"];

    private static readonly string[] CellOrder =
    [
        "cnfStyle", "tcW", "gridSpan", "hMerge", "vMerge", "tcBorders", "shd", "noWrap", "tcMar", "textDirection", "tcFitText", "vAlign", "hideMark", "headers",
        "cellIns", "cellDel", "cellMerge", "tcPrChange"
    ];

    private static readonly string[] BorderOrder = ["top", "left", "start", "bottom", "right", "end", "insideH", "insideV", "tl2br", "tr2bl"];

    internal static XElement Write(Table table, DocxWriteContext context)
    {
        var source = table.Source;
        var element = new XElement(W + "tbl", source?.Attributes(),
            Rewrite(source, "tr", table.Rows, r => r.Source, r => Row(r, context),
                e => e.Name == W + "tblPr" ? Properties(e, table.Format) : e.Name == W + "tblGrid" ? Grid(e, table.Grid) : e,
                e => e.Element(W + "trPr")?.Element(W + "del") != null));
        if (element.Element(W + "tblPr") == null) element.AddFirst(Properties(null, table.Format));
        if (element.Element(W + "tblGrid") == null) element.Element(W + "tblPr")!.AddAfterSelf(Grid(null, table.Grid));
        return element;
    }

    private static XElement Row(TableRow row, DocxWriteContext context)
    {
        if (context.IsOriginal(row)) return row.Source;
        var source = row.Source;
        var element = new XElement(W + "tr", source?.Attributes(),
            Rewrite(source, "tc", row.Cells, c => c.Source, c => Cell(c, context), e => e.Name == W + "trPr" ? Properties(e, row) : e, _ => false));
        if (source?.Element(W + "trPr") == null && Properties(null, row) is { } created)
        {
            if (element.Element(W + "tblPrEx") is { } exceptions) exceptions.AddAfterSelf(created);
            else element.AddFirst(created);
        }

        return element;
    }

    private static XElement Cell(TableCell cell, DocxWriteContext context)
    {
        if (context.IsOriginal(cell)) return cell.Source;
        var element = new XElement(W + "tc", cell.Source?.Attributes(), Properties(cell.Source?.Element(W + "tcPr"), cell),
            cell.Blocks.Select(b => context.Block(b, false)));
        if (element.Elements().LastOrDefault()?.Name != W + "p") element.Add(new XElement(W + "p"));
        return element;
    }

    private static List<object> Rewrite<T>(XElement original, string item, IReadOnlyList<T> models, Func<T, XElement> sourceOf, Func<T, XElement> write,
        Func<XElement, XElement> other, Func<XElement, bool> keepUnmapped)
    {
        var sources = models.Select(sourceOf).ToList();
        List<XElement> order = original == null ? [] : Children(original, item).ToList();
        var positions = sources.Where(s => s != null).Select(s => order.IndexOf(s)).ToList();
        if (positions.Count == 0 || positions.Any(p => p < 0) || positions.Zip(positions.Skip(1)).Any(p => p.First >= p.Second))
            return [.. (original?.Elements().Where(e => e.Name != W + item && ContentOf(e) == null) ?? []).Select(other), .. models.Select(write)];

        var emitted = new Dictionary<XElement, List<XElement>>();
        var current = new List<XElement>();
        for (var k = 0; k < models.Count; k++)
            if (sources[k] == null) current.Add(write(models[k]));
            else emitted[sources[k]] = current = [.. emitted.Count == 0 ? current : [], write(models[k])];
        return Walk(original).ToList();

        IEnumerable<object> Walk(XElement parent)
        {
            foreach (var node in parent.Nodes())
                if (node is not XElement element)
                    yield return node;
                else if (element.Name == W + item)
                {
                    if (emitted.TryGetValue(element, out var replacement))
                        foreach (var written in replacement) yield return written;
                    else if (keepUnmapped(element)) yield return element;
                }
                else if (ContentOf(element) is { } content && Children(content, item).Any())
                {
                    var inner = Walk(content).ToList();
                    if (inner.OfType<XElement>().Any(e => e.Name == W + item || ContentOf(e) != null)) yield return Rewrap(element, inner);
                }
                else
                    yield return other(element);
        }
    }

    private static XElement ContentOf(XElement element) =>
        element.Name == W + "customXml" ? element : element.Name == W + "sdt" ? element.Element(W + "sdtContent") : null;

    private static XElement Rewrap(XElement wrapper, List<object> content) =>
        wrapper.Name == W + "sdt"
            ? new XElement(wrapper.Name, wrapper.Attributes(), wrapper.Nodes().Select(n => n is XElement e && e.Name == W + "sdtContent" ? new XElement(e.Name, e.Attributes(), content) : n))
            : new XElement(wrapper.Name, wrapper.Attributes(), content);

    private static XElement Properties(XElement original, TableFormat format)
    {
        var tblPr = original != null ? new XElement(original) : new XElement(W + "tblPr");
        var current = FormatXml.Table(tblPr);
        if (current.StyleId != format.StyleId) SetVal(tblPr, "tblStyle", format.StyleId, TableOrder);
        if (current.Width != format.Width)
        {
            var width = Ensure(tblPr, "tblW", TableOrder);
            width.SetAttributeValue(W + "w", Number(format.Width ?? 0));
            width.SetAttributeValue(W + "type", format.Width == null ? "auto" : "dxa");
        }

        if (current.Alignment != format.Alignment) SetVal(tblPr, "jc", format.Alignment?.ToString().ToLowerInvariant(), TableOrder);
        if (current.Borders != format.Borders) SetBorders(tblPr, "tblBorders", format.Borders, TableOrder);
        if (current.Look != format.Look) SetLook(Ensure(tblPr, "tblLook", TableOrder), format.Look);
        return tblPr;
    }

    private static XElement Properties(XElement original, TableRow row)
    {
        var trPr = original != null ? new XElement(original) : new XElement(W + "trPr");
        if (Int(trPr.Element(W + "trHeight"), "val") != row.Height) SetVal(trPr, "trHeight", Optional(row.Height), RowOrder);
        if ((OnOff(trPr, "tblHeader") == true) != row.IsHeader) SetOnOff(trPr, "tblHeader", row.IsHeader ? true : null, RowOrder);
        if (Math.Max(0, Int(trPr.Element(W + "gridBefore"), "val") ?? 0) != row.GridBefore)
            SetVal(trPr, "gridBefore", row.GridBefore > 0 ? Number(row.GridBefore) : null, RowOrder);
        if (Math.Max(0, Int(trPr.Element(W + "gridAfter"), "val") ?? 0) != row.GridAfter)
            SetVal(trPr, "gridAfter", row.GridAfter > 0 ? Number(row.GridAfter) : null, RowOrder);
        return trPr.HasElements || trPr.HasAttributes ? trPr : null;
    }

    private static XElement Properties(XElement original, TableCell cell)
    {
        var tcPr = original != null ? new XElement(original) : new XElement(W + "tcPr");
        var current = Cell(new XElement(W + "tc", tcPr), _ => []);
        if (current.Width != cell.Width)
        {
            tcPr.Element(W + "tcW")?.Remove();
            if (cell.Width is { } value) Place(tcPr, new XElement(W + "tcW", new XAttribute(W + "w", Number(value)), new XAttribute(W + "type", "dxa")), CellOrder);
        }

        if (current.GridSpan != cell.GridSpan) SetVal(tcPr, "gridSpan", cell.GridSpan > 1 ? Number(cell.GridSpan) : null, CellOrder);
        if (current.VMerge != cell.VMerge)
        {
            tcPr.Element(W + "vMerge")?.Remove();
            if (cell.VMerge != VerticalMerge.None)
                Place(tcPr, new XElement(W + "vMerge", cell.VMerge == VerticalMerge.Restart ? new XAttribute(W + "val", "restart") : null), CellOrder);
        }

        if (current.Borders != cell.Borders) SetBorders(tcPr, "tcBorders", cell.Borders, CellOrder);
        if (current.Shading != cell.Shading) SetShading(tcPr, cell.Shading, CellOrder);
        if (current.VerticalAlign != cell.VerticalAlign) SetVal(tcPr, "vAlign", cell.VerticalAlign?.ToString().ToLowerInvariant(), CellOrder);
        return tcPr.HasElements || tcPr.HasAttributes ? tcPr : null;
    }

    private static XElement Grid(XElement original, IReadOnlyList<int> grid)
    {
        if (original != null && original.Elements(W + "gridCol").Select(c => Int(c, "w") ?? 0).SequenceEqual(grid)) return original;
        return new XElement(W + "tblGrid", original?.Attributes(), grid.Select(w => new XElement(W + "gridCol", new XAttribute(W + "w", Number(w)))));
    }

    private static void SetBorders(XElement parent, string name, TableBorders borders, string[] order)
    {
        if (borders == null)
        {
            parent.Element(W + name)?.Remove();
            return;
        }

        var element = Ensure(parent, name, order);
        var current = FormatXml.Borders(element);
        foreach (var (side, alternative, value, old) in new[]
                 {
                     ("top", "top", borders.Top, current.Top), ("left", "start", borders.Left, current.Left), ("bottom", "bottom", borders.Bottom, current.Bottom),
                     ("right", "end", borders.Right, current.Right), ("insideH", "insideH", borders.InsideH, current.InsideH),
                     ("insideV", "insideV", borders.InsideV, current.InsideV)
                 })
        {
            if (old == value) continue;
            var existing = element.Element(W + side) ?? element.Element(W + alternative);
            element.Elements().Where(e => e.Name == W + side || e.Name == W + alternative).Remove();
            if (value == null) continue;
            var border = new XElement(existing?.Name ?? W + side, new XAttribute(W + "val", value.Style ?? "single"),
                new XAttribute(W + "sz", Number((int)Math.Round(value.WidthPt * 8))), new XAttribute(W + "space", (string)existing?.Attribute(W + "space") ?? "0"));
            SetColor(border, value.Color ?? "auto", "color", "themeColor", "themeShade", "themeTint");
            Place(element, border, BorderOrder);
        }

        if (!element.HasElements) element.Remove();
    }

    private static void SetLook(XElement look, TableLook value)
    {
        var bits = 0;
        foreach (var (name, flag, bit, inverted) in new[]
                 {
                     ("firstRow", TableLook.FirstRow, 0x20, false), ("lastRow", TableLook.LastRow, 0x40, false), ("firstColumn", TableLook.FirstColumn, 0x80, false),
                     ("lastColumn", TableLook.LastColumn, 0x100, false), ("noHBand", TableLook.BandedRows, 0x200, true), ("noVBand", TableLook.BandedColumns, 0x400, true)
                 })
        {
            var on = value.HasFlag(flag) != inverted;
            if (on) bits |= bit;
            look.SetAttributeValue(W + name, on ? "1" : "0");
        }

        look.SetAttributeValue(W + "val", bits.ToString("X4", CultureInfo.InvariantCulture));
    }

    private static IEnumerable<XElement> Children(XElement parent, string name)
    {
        foreach (var child in parent.Elements())
            if (child.Name == W + name) yield return child;
            else if (child.Name == W + "customXml") foreach (var nested in Children(child, name)) yield return nested;
            else if (child.Name == W + "sdt" && child.Element(W + "sdtContent") is { } content) foreach (var nested in Children(content, name)) yield return nested;
    }
}
