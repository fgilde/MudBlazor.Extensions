using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

internal static class TableXml
{
    internal const string Uri = "http://schemas.openxmlformats.org/drawingml/2006/table";
    private static readonly string[] AfterFill = ["headers", "extLst"];

    internal static XElement Of(XElement frame) => frame.Element(A + "graphic")?.Element(A + "graphicData")?.Element(A + "tbl");

    internal static XElement Create() => new(A + "graphic", new XElement(A + "graphicData", new XAttribute("uri", Uri), new XElement(A + "tbl")));

    internal static bool Readable(XElement tbl)
    {
        var columns = tbl.Element(A + "tblGrid")?.Elements(A + "gridCol").Count() ?? 0;
        return columns > 0 && tbl.Elements(A + "tr").All(tr => tr.Elements(A + "tc").Count() <= columns);
    }

    internal static TableShape Read(XElement tbl)
    {
        var table = new TableShape();
        ReadProperties(table, tbl.Element(A + "tblPr"));
        table.ColumnWidths = tbl.Element(A + "tblGrid")?.Elements(A + "gridCol").Select(g => Long((string)g.Attribute("w")) ?? 0).ToList() ?? [];
        var columns = table.ColumnWidths.Count;
        var rows = tbl.Elements(A + "tr").Select(tr => new TableRow
        {
            Source = tr, Height = Long((string)tr.Attribute("h")) ?? 0,
            Cells = tr.Elements(A + "tc").Select(ReadCell).Concat(Enumerable.Range(0, columns).Select(_ => new TableCell())).Take(columns).ToList()
        }).ToList();
        table.Rows = TableEdit.Rows(rows, TableEdit.Normalize(TableEdit.Grid(rows)));
        return table;
    }

    internal static void Write(XElement frame, TableShape table)
    {
        if (Of(frame) is not { } tbl) return;
        WriteProperties(tbl, table);
        var grid = tbl.Element(A + "tblGrid") ?? SlideXml.Place(tbl, new XElement(A + "tblGrid"), "tr");
        var columns = grid.Elements(A + "gridCol").ToList();
        for (var i = 0; i < table.ColumnWidths.Count; i++)
        {
            var column = i < columns.Count ? columns[i] : SlideXml.Place(grid, new XElement(A + "gridCol"), "extLst");
            if (Long((string)column.Attribute("w")) != table.ColumnWidths[i]) column.SetAttributeValue("w", table.ColumnWidths[i]);
        }

        columns.Skip(table.ColumnWidths.Count).Remove();
        var layout = TableEdit.Layout(table.Rows.Select(r => r.Cells).ToList());
        tbl.Elements(A + "tr").Remove();
        tbl.Add(table.Rows.Select((row, r) =>
        {
            var tr = row.Source != null ? new XElement(row.Source) : new XElement(A + "tr");
            if (Long((string)tr.Attribute("h")) != row.Height) tr.SetAttributeValue("h", row.Height);
            tr.Elements(A + "tc").Remove();
            var cells = row.Cells.Select((cell, c) => Cell(cell, r, c, layout[r][c])).ToList();
            if (tr.Element(A + "extLst") is { } extension) extension.AddBeforeSelf(cells);
            else tr.Add(cells);
            return tr;
        }));
    }

    private static void ReadProperties(TableShape table, XElement tblPr)
    {
        table.StyleId = (string)tblPr?.Element(A + "tableStyleId");
        table.FirstRow = Flag(tblPr, "firstRow");
        table.FirstColumn = Flag(tblPr, "firstCol");
        table.LastRow = Flag(tblPr, "lastRow");
        table.LastColumn = Flag(tblPr, "lastCol");
        table.BandedRows = Flag(tblPr, "bandRow");
        table.BandedColumns = Flag(tblPr, "bandCol");
    }

    private static void WriteProperties(XElement tbl, TableShape table)
    {
        var before = new TableShape();
        ReadProperties(before, tbl.Element(A + "tblPr"));
        if ((before.StyleId, before.FirstRow, before.FirstColumn, before.LastRow, before.LastColumn, before.BandedRows, before.BandedColumns) ==
            (table.StyleId, table.FirstRow, table.FirstColumn, table.LastRow, table.LastColumn, table.BandedRows, table.BandedColumns)) return;
        var tblPr = tbl.Element(A + "tblPr") ?? SlideXml.AddFirst(tbl, new XElement(A + "tblPr"));
        SetFlag(tblPr, "firstRow", table.FirstRow);
        SetFlag(tblPr, "firstCol", table.FirstColumn);
        SetFlag(tblPr, "lastRow", table.LastRow);
        SetFlag(tblPr, "lastCol", table.LastColumn);
        SetFlag(tblPr, "bandRow", table.BandedRows);
        SetFlag(tblPr, "bandCol", table.BandedColumns);
        if (before.StyleId == table.StyleId) return;
        tblPr.Elements().Where(e => e.Name == A + "tableStyleId" || e.Name == A + "tableStyle").Remove();
        if (table.StyleId != null) SlideXml.Place(tblPr, new XElement(A + "tableStyleId", table.StyleId), "extLst");
    }

    private static TableCell ReadCell(XElement tc)
    {
        var tcPr = tc.Element(A + "tcPr");
        var fill = ShapeReader.FillOf(tcPr);
        var merged = Flag(tc, "hMerge") || Flag(tc, "vMerge");
        return new TableCell
        {
            Source = tc,
            Text = tc.Element(A + "txBody") is { } body ? ShapeReader.ReadText(body) : null,
            Fill = fill == null ? null : ShapeReader.ReadFill(fill),
            IsMergedAway = merged,
            ColumnSpan = merged ? 1 : Span(tc, "gridSpan"),
            RowSpan = merged ? 1 : Span(tc, "rowSpan"),
            MarginLeft = Long((string)tcPr?.Attribute("marL")),
            MarginTop = Long((string)tcPr?.Attribute("marT")),
            MarginRight = Long((string)tcPr?.Attribute("marR")),
            MarginBottom = Long((string)tcPr?.Attribute("marB")),
            Anchor = TextStyles.ParseAnchor((string)tcPr?.Attribute("anchor"))
        };
    }

    private static XElement Cell(TableCell cell, int row, int column, (int Row, int Column, int RowSpan, int ColumnSpan) owner)
    {
        var tc = cell.Source != null ? new XElement(cell.Source) : new XElement(A + "tc", new XElement(A + "tcPr"));
        SetSpan(tc, "rowSpan", row == owner.Row ? owner.RowSpan : 1);
        SetSpan(tc, "gridSpan", column == owner.Column ? owner.ColumnSpan : 1);
        SetFlag(tc, "hMerge", column > owner.Column);
        SetFlag(tc, "vMerge", row > owner.Row);
        TextBodyXml.WriteCell(tc, cell.Text);
        var before = ReadCell(tc);
        if ((before.MarginLeft, before.MarginTop, before.MarginRight, before.MarginBottom, before.Anchor, before.Fill) ==
            (cell.MarginLeft, cell.MarginTop, cell.MarginRight, cell.MarginBottom, cell.Anchor, cell.Fill)) return tc;
        var tcPr = tc.Element(A + "tcPr") ?? SlideXml.Place(tc, new XElement(A + "tcPr"), "extLst");
        TextBodyXml.Set(tcPr, "marL", before.MarginLeft, cell.MarginLeft, TextBodyXml.Format);
        TextBodyXml.Set(tcPr, "marR", before.MarginRight, cell.MarginRight, TextBodyXml.Format);
        TextBodyXml.Set(tcPr, "marT", before.MarginTop, cell.MarginTop, TextBodyXml.Format);
        TextBodyXml.Set(tcPr, "marB", before.MarginBottom, cell.MarginBottom, TextBodyXml.Format);
        TextBodyXml.Set(tcPr, "anchor", before.Anchor, cell.Anchor, TextBodyXml.AnchorValue);
        var fill = cell.Fill?.Kind switch
        {
            ShapeFillKind.None => new XElement(A + "noFill"),
            ShapeFillKind.Solid => SlideXml.SolidFill(cell.Fill.Color),
            _ => null
        };
        if (before.Fill == cell.Fill || fill == null && cell.Fill != null) return tc;
        tcPr.Elements().Where(f => SlideXml.Fills.Contains(f.Name.LocalName)).Remove();
        SlideXml.Place(tcPr, fill, AfterFill);
        return tc;
    }

    private static bool Flag(XElement element, string name) => (string)element?.Attribute(name) is "1" or "true";

    private static int Span(XElement tc, string name) => (int)Math.Clamp(Long((string)tc.Attribute(name)) ?? 1, 1, int.MaxValue);

    private static void SetFlag(XElement element, string name, bool value)
    {
        if (Flag(element, name) != value) element.SetAttributeValue(name, value ? "1" : null);
    }

    private static void SetSpan(XElement tc, string name, int value)
    {
        if (Span(tc, name) != value) tc.SetAttributeValue(name, value == 1 ? null : value);
    }
}
