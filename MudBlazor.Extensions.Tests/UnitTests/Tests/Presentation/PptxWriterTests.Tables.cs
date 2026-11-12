using System.Xml.Linq;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.TableTests;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public partial class PptxWriterTests
{
    private const string A16Xmlns = "xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\"";

    private static bool Flag(XElement e, string name) => (string)e.Attribute(name) is "1" or "true";

    private static int Span(XElement e, string name) => (int?)e.Attribute(name) ?? 1;

    private static void AssertValidTable(XElement tbl)
    {
        var columns = tbl.Element(A + "tblGrid")!.Elements(A + "gridCol").Count();
        var rows = tbl.Elements(A + "tr").Select(tr => tr.Elements(A + "tc").ToList()).ToList();
        Assert.All(rows, row => Assert.Equal(columns, row.Count));
        var owner = rows.Select(row => new (int Row, int Column)?[row.Count]).ToArray();
        for (var r = 0; r < rows.Count; r++)
        for (var c = 0; c < columns; c++)
        {
            var tc = rows[r][c];
            if (Flag(tc, "hMerge") || Flag(tc, "vMerge"))
            {
                Assert.True(owner[r][c] != null, $"merged cell {r},{c} has no anchor");
                var (ar, ac) = owner[r][c]!.Value;
                var anchor = rows[ar][ac];
                Assert.Equal((c > ac, r > ar), (Flag(tc, "hMerge"), Flag(tc, "vMerge")));
                Assert.Equal(c == ac ? Span(anchor, "gridSpan") : 1, Span(tc, "gridSpan"));
                Assert.Equal(r == ar ? Span(anchor, "rowSpan") : 1, Span(tc, "rowSpan"));
                continue;
            }

            Assert.True(owner[r][c] == null, $"anchor {r},{c} lies under another span");
            var (rs, cs) = (Span(tc, "rowSpan"), Span(tc, "gridSpan"));
            Assert.InRange(r + rs, r + 1, rows.Count);
            Assert.InRange(c + cs, c + 1, columns);
            for (var rr = r; rr < r + rs; rr++)
            for (var cc = c; cc < c + cs; cc++)
                if (rr != r || cc != c)
                    owner[rr][cc] = (r, c);
        }
    }

    private static XElement TableXml(byte[] package, string part = SlidePart, int index = 0) => Xml(package, part).Descendants(A + "tbl").ElementAt(index);

    [Fact]
    public void Writer_Table_EveryRowHasGridColCount()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        presentation.Execute(new InsertTableRowsCommand(slide, 3, 1, 1));
        presentation.Execute(new InsertTableColumnsCommand(slide, 3, 2, 1));
        presentation.Execute(new DeleteTableRowsCommand(slide, 3, 0, 1));
        presentation.Execute(new DeleteTableColumnsCommand(slide, 3, 0, 1));
        presentation.Execute(new MergeTableCellsCommand(slide, 3, 2, 0, 2, 2));
        Assert.Equal("· · · | D[1x3] _ _ | E/H[2x2] _ F | _ _ I", Grid(Table(presentation)));

        var output = Save(presentation);

        AssertConsistent(output);
        var tbl = TableXml(output);
        Assert.Equal(3, tbl.Element(A + "tblGrid")!.Elements(A + "gridCol").Count());
        AssertValidTable(tbl);
        var cells = tbl.Elements(A + "tr").Select(tr => tr.Elements(A + "tc").ToList()).ToList();
        Assert.Equal(("3", null, null), ((string)cells[1][0].Attribute("gridSpan"), (string)cells[1][1].Attribute("gridSpan"), (string)cells[1][1].Attribute("vMerge")));
        Assert.Equal(["2", "2", null], cells[2].Select(tc => (string)tc.Attribute("rowSpan")));
        Assert.Equal(["2", null, null], cells[3].Select(tc => (string)tc.Attribute("gridSpan")));
        Assert.Equal([true, true, false], cells[3].Select(tc => Flag(tc, "vMerge")));
        Assert.Equal([false, true, false], cells[3].Select(tc => Flag(tc, "hMerge")));
        Assert.Equal(Describe(presentation), Describe(Load(output)));

        foreach (var index in new[] { 0, 1, 2 })
        {
            var deck = Deck();
            deck.Execute(new InsertTableRowsCommand(deck.Slides[0], 3, index + 1, 1));
            deck.Execute(new InsertTableColumnsCommand(deck.Slides[0], 3, index + 1, 1));
            deck.Execute(new DeleteTableRowsCommand(deck.Slides[0], 3, index, 1));
            deck.Execute(new DeleteTableColumnsCommand(deck.Slides[0], 3, index, 1));
            var saved = Save(deck);
            AssertValidTable(TableXml(saved));
            Assert.Equal(Describe(deck), Describe(Load(saved)));
        }
    }

    [Fact]
    public void Writer_Table_KeepsUnknownTcPrChildren()
    {
        var tblPr = $"<a:tblPr firstRow=\"1\" bandRow=\"1\"><a:effectLst/><a:tableStyleId>{StyleId}</a:tableStyleId></a:tblPr>";
        var table = Table(3, tblPr, [1000000, 2000000],
                Tc("A", "", $"<a:tcPr>{Borders}</a:tcPr>") + Tc("B", "", "<a:tcPr anchor=\"b\"/>"),
                Tc("C") + Tc("D", "", $"<a:tcPr>{Borders}<a:solidFill><a:srgbClr val=\"FF0000\"/></a:solidFill></a:tcPr><a:extLst><a:ext uri=\"{{1}}\"><x:y xmlns:x=\"urn:x\"/></a:ext></a:extLst>") +
                $"<a:extLst><a:ext uri=\"{{0D108BD9-81ED-4DB2-BD59-A6C34878D82A}}\"><a16:rowId {A16Xmlns} val=\"20\"/></a:ext></a:extLst>")
            .Replace("<a:gridCol w=\"1000000\"/>", $"<a:gridCol w=\"1000000\"><a:extLst><a:ext uri=\"{{9D8B030D-6E8A-4147-A177-3AD203B41FA5}}\"><a16:colId {A16Xmlns} val=\"10\"/></a:ext></a:extLst></a:gridCol>");
        var input = new PptxBuilder().Slide(table).Build();
        var presentation = Load(input);
        var slide = presentation.Slides[0];
        presentation.Execute(new SetTableCellFillCommand(slide, 3, [(0, 0)], new ShapeFill(ShapeFillKind.Solid, "#0000FF")));
        presentation.Execute(new SetTableCellFillCommand(slide, 3, [(1, 1)], null));
        presentation.Execute(new SetTableCellTextCommand(slide, 3, 1, 1, Body("New")));
        presentation.Execute(new SetTableOptionsCommand(slide, 3, false, null, null, null, true, null));
        presentation.Execute(new InsertTableColumnsCommand(slide, 3, 1, 1));

        var output = Save(presentation);

        AssertConsistent(output);
        var tbl = TableXml(output);
        var source = TableXml(input);
        var properties = tbl.Element(A + "tblPr")!;
        Assert.Equal(["effectLst", "tableStyleId"], properties.Elements().Select(e => e.Name.LocalName));
        Assert.Equal((null, "1", "1"), ((string)properties.Attribute("firstRow"), (string)properties.Attribute("bandRow"), (string)properties.Attribute("lastCol")));
        var cells = tbl.Elements(A + "tr").Select(tr => tr.Elements(A + "tc").ToList()).ToList();
        var a = cells[0][0].Element(A + "tcPr")!;
        Assert.Equal(["lnL", "lnB", "solidFill"], a.Elements().Select(e => e.Name.LocalName));
        Assert.True(XNode.DeepEquals(source.Descendants(A + "lnL").First(), a.Element(A + "lnL")));
        var d = cells[1][2];
        Assert.Equal(["txBody", "tcPr", "extLst"], d.Elements().Select(e => e.Name.LocalName));
        Assert.Equal(["lnL", "lnB"], d.Element(A + "tcPr")!.Elements().Select(e => e.Name.LocalName));
        Assert.Equal("New", d.Element(A + "txBody")!.Value);
        Assert.Equal(["txBody", "tcPr"], cells[1][1].Elements().Select(e => e.Name.LocalName));
        Assert.Equal("b", (string)cells[0][2].Element(A + "tcPr")!.Attribute("anchor"));
        var columns = tbl.Element(A + "tblGrid")!.Elements(A + "gridCol").ToList();
        Assert.Equal(["1000000", "1000000", "2000000"], columns.Select(c => (string)c.Attribute("w")));
        Assert.Single(columns[0].Descendants().Where(e => e.Name.LocalName == "colId"));
        Assert.Empty(columns[1].Elements());
        Assert.Single(tbl.Elements(A + "tr").ElementAt(1).Elements(A + "extLst"));
        Assert.Equal(Describe(presentation), Describe(Load(output)));
    }

    [Fact]
    public void Writer_NewTable_AddsTableStylesPart()
    {
        var presentation = new PptxBuilder().Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10))).Load();
        Assert.DoesNotContain("/ppt/tableStyles.xml", Parts(Save(presentation)).Keys);
        presentation.Execute(new InsertTableCommand(presentation.Slides[0], 2, 3, new ShapeTransform(100, 200, 3000000, 1000)));

        var output = Save(presentation);

        AssertConsistent(output);
        var styles = Xml(output, "/ppt/tableStyles.xml").Root!;
        Assert.Equal((A + "tblStyleLst", StyleId), (styles.Name, (string)styles.Attribute("def")));
        Assert.Single(Rels(output, "/ppt/_rels/presentation.xml.rels"), r => r.Type == RelNs + "/tableStyles" && r.Target == "tableStyles.xml");
        Assert.Equal("application/vnd.openxmlformats-officedocument.presentationml.tableStyles+xml", Overrides(output)["/ppt/tableStyles.xml"]);
        var frame = Xml(output, SlidePart).Descendants(P + "graphicFrame").Single();
        Assert.Equal(["nvGraphicFramePr", "xfrm", "graphic"], frame.Elements().Select(e => e.Name.LocalName));
        Assert.Equal(("3", "Table 2"), ((string)frame.Descendants(P + "cNvPr").Single().Attribute("id"), (string)frame.Descendants(P + "cNvPr").Single().Attribute("name")));
        Assert.Equal(TableUri, (string)frame.Descendants(A + "graphicData").Single().Attribute("uri"));
        var tbl = frame.Descendants(A + "tbl").Single();
        Assert.Equal(["tblPr", "tblGrid", "tr", "tr"], tbl.Elements().Select(e => e.Name.LocalName));
        Assert.Equal(("1", "1", StyleId), ((string)tbl.Element(A + "tblPr")!.Attribute("firstRow"), (string)tbl.Element(A + "tblPr")!.Attribute("bandRow"),
            tbl.Element(A + "tblPr")!.Element(A + "tableStyleId")!.Value));
        Assert.Equal(["1000000", "1000000", "1000000"], tbl.Descendants(A + "gridCol").Select(c => (string)c.Attribute("w")));
        Assert.All(tbl.Elements(A + "tr"), tr => Assert.Equal("370840", (string)tr.Attribute("h")));
        Assert.All(tbl.Descendants(A + "tc"), tc =>
        {
            Assert.Equal(["txBody", "tcPr"], tc.Elements().Select(e => e.Name.LocalName));
            Assert.Equal(["bodyPr", "lstStyle", "p"], tc.Element(A + "txBody")!.Elements().Select(e => e.Name.LocalName));
        });
        AssertValidTable(tbl);
        Assert.Equal(Describe(presentation), Describe(Load(output)));

        var blank = Presentation.Create();
        blank.Execute(new InsertTableCommand(blank.Slides[0], 1, 1, new ShapeTransform(0, 0, 100, 100)));
        var saved = Save(blank);
        AssertConsistent(saved);
        Assert.Single(Rels(saved, "/ppt/_rels/presentation.xml.rels"), r => r.Type == RelNs + "/tableStyles");
    }

    [Fact]
    public void Writer_TableEdits_RoundTrip()
    {
        var input = new PptxBuilder()
            .Slide(Shape(2, "Text", Xfrm(0, 0, 100, 100), "<a:p><a:r><a:t>apple</a:t></a:r></a:p>") + MergedTable)
            .Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10)))
            .Build();
        var presentation = Load(input);
        var slide = presentation.Slides[0];
        var table = Table(presentation);
        var a = table.Rows[0].Cells[0].Text;
        presentation.Execute(new SetTableCellTextCommand(slide, 3, 0, 0, a with
        {
            Paragraphs = [a.Paragraphs[0] with { Runs = [a.Paragraphs[0].Runs[0], new TextRun(" bold", new RunFormat { Bold = true, Color = "#00FF00" })] }, new TextParagraph()]
        }));
        presentation.Execute(new SetTableCellFillCommand(slide, 3, [(0, 1), (1, 1)], new ShapeFill(ShapeFillKind.None)));
        presentation.Execute(new SetTableOptionsCommand(slide, 3, null, false, true, null, null, true));
        presentation.Execute(new SetTableSizesCommand(slide, 3, [1100000, 2100000, 3100000], [300000, 300000, 300000, 500000]));
        presentation.Execute(new MergeTableCellsCommand(slide, 3, 2, 1, 2, 2));
        presentation.Execute(new SplitTableCellCommand(slide, 3, 0, 0));
        presentation.Execute(new SetTransformsCommand(slide, new Dictionary<uint, ShapeTransform> { [3] = new(10, 20, 3150000, 700000) }));
        presentation.Execute(new DuplicateShapesCommand(slide, [3], 100000));
        presentation.Execute(new InsertShapeCommand(presentation.Slides[1], ShapeEdit.Detach(presentation, slide, table)));
        presentation.Execute(new InsertTableCommand(presentation.Slides[1], 3, 2, new ShapeTransform(0, 0, 2000000, 100)));

        var output = Save(presentation);

        AssertConsistent(output);
        foreach (var part in new[] { SlidePart, "/ppt/slides/slide2.xml" })
            foreach (var tbl in Xml(output, part).Descendants(A + "tbl"))
                AssertValidTable(tbl);
        Assert.Equal(Describe(presentation), Describe(Load(output)));
        Assert.Equal(output, Save(presentation));

        while (presentation.Undo.CanUndo) presentation.Undo.Undo();
        var undone = Parts(Save(presentation));
        var original = Parts(input);
        Assert.Equal(original[SlidePart], undone[SlidePart]);
        Assert.Equal(original["/ppt/slides/slide2.xml"], undone["/ppt/slides/slide2.xml"]);
    }
}
