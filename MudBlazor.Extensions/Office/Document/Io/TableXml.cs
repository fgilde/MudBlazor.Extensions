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

    private static IEnumerable<XElement> Children(XElement parent, string name)
    {
        foreach (var child in parent.Elements())
            if (child.Name == W + name) yield return child;
            else if (child.Name == W + "customXml") foreach (var nested in Children(child, name)) yield return nested;
            else if (child.Name == W + "sdt" && child.Element(W + "sdtContent") is { } content) foreach (var nested in Children(content, name)) yield return nested;
    }
}
