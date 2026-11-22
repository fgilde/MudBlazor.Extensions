using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class TableCommandTests
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace W14 = "http://schemas.microsoft.com/office/word/2010/wordml";

    [Fact]
    public void InsertTable_MidParagraph_SplitsAndInsertsBetween()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 0), "HelloWorld"));
        var before = document.Blocks.ToList();
        var styles = document.Styles;

        var command = new InsertTableCommand(document.PositionOf(id, 5), 2, 3);
        document.Execute(command);

        Assert.Equal(3, document.Blocks.Count);
        Assert.Equal("Hello", ((Paragraph)document.Blocks[0]).Text);
        Assert.Equal("World", ((Paragraph)document.Blocks[2]).Text);
        var table = Assert.IsType<Table>(document.Blocks[1]);
        Assert.Equal([3008, 3008, 3008], table.Grid);
        Assert.Equal("TableGrid", table.Format.StyleId);
        Assert.NotNull(document.Styles.Find("TableGrid"));
        Assert.All(table.Rows, r => Assert.Equal(3, r.Cells.Count));
        Assert.Equal(((Paragraph)table.Rows[0].Cells[0].Blocks[0]).Id, document.Resolve(command.After.Start).Paragraph.Id);
        Assert.Null(document.Sections[^1].LastBlockId);
        var output = DocxWriterTests.Save(document);
        DocxWriterTests.AssertConsistent(output);
        Assert.Equal(["p", "tbl", "p", "sectPr"], Body(output).Elements().Select(e => e.Name.LocalName));

        document.Undo.Undo();

        Assert.True(before.SequenceEqual(document.Blocks, ReferenceEqualityComparer.Instance));
        Assert.Same(styles, document.Styles);
    }

    [Fact]
    public void InsertTable_AtEndOfLastParagraph_AddsParagraphAfter_SectionEndMovesOnSplit()
    {
        var document = new DocxBuilder().Body(
            "<w:p><w:pPr><w:sectPr><w:pgSz w:w=\"12240\" w:h=\"15840\"/><w:pgMar w:top=\"1440\" w:right=\"1440\" w:bottom=\"1440\" w:left=\"1440\" w:header=\"720\" w:footer=\"720\" w:gutter=\"0\"/></w:sectPr></w:pPr><w:r><w:t>first</w:t></w:r></w:p>" +
            "<w:p><w:r><w:t>last</w:t></w:r></w:p>" +
            "<w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/><w:pgMar w:top=\"1417\" w:right=\"1417\" w:bottom=\"1134\" w:left=\"1417\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/></w:sectPr>").Load();
        var first = document.Blocks[0].Id;

        document.Execute(new InsertTableCommand(document.PositionOf(document.Blocks[1].Id, 4), 1, 2));
        document.Execute(new InsertTableCommand(document.PositionOf(first, 2), 1, 1));

        Assert.Equal(["p", "t", "p", "p", "t", "p"], document.Blocks.Select(b => b is Table ? "t" : "p"));
        Assert.Equal(document.Blocks[2].Id, document.Sections[0].LastBlockId);
        Assert.Equal([9360], ((Table)document.Blocks[1]).Grid);
        Assert.Equal([4536, 4536], ((Table)document.Blocks[4]).Grid);
        Assert.Empty(((Paragraph)document.Blocks[5]).Inlines);
        var output = DocxWriterTests.Save(document);
        DocxWriterTests.AssertConsistent(output);
        Assert.NotNull(Body(output).Elements(W + "p").ElementAt(1).Element(W + "pPr")?.Element(W + "sectPr"));
    }

    [Fact]
    public void InsertRows_InOuterTable_InnerNestedTableXmlIdentical()
    {
        var source = Nested();
        var document = Load(source);
        var outer = (Table)document.Blocks[0];

        document.Execute(new InsertTableRowsCommand(outer.Id, 1, 1));
        document.Execute(new InsertTableRowsCommand(outer.Id, 0, 1));

        var table = (Table)document.Blocks[0];
        Assert.Equal(4, table.Rows.Count);
        Assert.All(table.Rows[0].Cells.Concat(table.Rows[2].Cells), c => Assert.Equal("", Assert.IsType<Paragraph>(Assert.Single(c.Blocks)).Text));
        var output = DocxWriterTests.Save(document);
        DocxWriterTests.AssertConsistent(output);
        var inner = Body(source).Descendants(W + "tbl").Single(t => t.Ancestors(W + "tbl").Any());
        Assert.True(XNode.DeepEquals(inner, Body(output).Descendants(W + "tbl").Single(t => t.Ancestors(W + "tbl").Any())));

        document.Undo.Undo();
        document.Undo.Undo();

        Assert.Same(outer, document.Blocks[0]);
    }

    [Fact]
    public void InsertRows_CopiesFormatsWithoutParaIds_InheritsUnmodelledRowAndCellProperties()
    {
        var source = new DocxBuilder().Body(
            "<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"3000\"/><w:gridCol w:w=\"3000\"/></w:tblGrid>" +
            "<w:tr w14:paraId=\"0AAAAAAA\" w14:textId=\"77777777\"><w:trPr><w:cantSplit/><w:trHeight w:val=\"500\"/></w:trPr>" +
            "<w:tc><w:tcPr><w:tcW w:w=\"3000\" w:type=\"dxa\"/><w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"FF0000\"/><w:noWrap/></w:tcPr>" +
            "<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:t>a</w:t></w:r></w:p></w:tc>" +
            "<w:tc><w:tcPr><w:tcW w:w=\"3000\" w:type=\"dxa\"/></w:tcPr><w:p><w:r><w:t>b</w:t></w:r></w:p></w:tc></w:tr></w:tbl><w:p/>").Build();
        var document = Load(source);
        var table = (Table)document.Blocks[0];

        document.Execute(new InsertTableRowsCommand(table.Id, 0, 2));

        var rows = ((Table)document.Blocks[0]).Rows;
        Assert.Equal(500, rows[0].Height);
        Assert.Equal("#FF0000", rows[0].Cells[0].Shading);
        Assert.Equal(ParagraphAlignment.Center, ((Paragraph)rows[1].Cells[0].Blocks[0]).Format.Alignment);
        var output = DocxWriterTests.Save(document);
        DocxWriterTests.AssertConsistent(output);
        var written = Body(output).Element(W + "tbl")!.Elements(W + "tr").ToList();
        Assert.True(XNode.DeepEquals(Body(source).Element(W + "tbl")!.Element(W + "tr"), written[2]));
        Assert.All(written.Take(2), r =>
        {
            Assert.Null(r.Attribute(W14 + "paraId"));
            Assert.NotNull(r.Element(W + "trPr")!.Element(W + "cantSplit"));
            Assert.NotNull(r.Element(W + "tc")!.Element(W + "tcPr")!.Element(W + "noWrap"));
            Assert.Empty(r.Descendants(W + "t"));
        });
        var paraIds = Body(output).Descendants().Attributes(W14 + "paraId").Select(a => a.Value).ToList();
        Assert.Equal(paraIds.Count, paraIds.Distinct().Count());
    }

    [Fact]
    public void Writer_RowsInsertedDeletedReordered_KeepWrappersAndTrackedDeletedRows()
    {
        static string Cell(string text) => $"<w:tc><w:p><w:r><w:t>{text}</w:t></w:r></w:p></w:tc>";
        var source = new DocxBuilder().Body(
            "<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"3000\"/><w:gridCol w:w=\"3000\"/></w:tblGrid>" +
            $"<w:customXml w:element=\"row\"><w:tr>{Cell("r1a")}{Cell("r1b")}</w:tr></w:customXml>" +
            $"<w:tr><w:sdt><w:sdtPr><w:id w:val=\"3\"/></w:sdtPr><w:sdtContent>{Cell("r2a")}</w:sdtContent></w:sdt>{Cell("r2b")}</w:tr>" +
            $"<w:tr><w:trPr><w:del w:id=\"5\" w:author=\"A\"/></w:trPr>{Cell("gone")}{Cell("gone")}</w:tr>" +
            $"<w:sdt><w:sdtPr><w:id w:val=\"4\"/></w:sdtPr><w:sdtContent><w:tr>{Cell("r3a")}{Cell("r3b")}</w:tr><w:tr>{Cell("r4a")}{Cell("r4b")}</w:tr></w:sdtContent></w:sdt>" +
            $"<w:tr>{Cell("r5a")}{Cell("r5b")}</w:tr>" +
            "</w:tbl><w:p/>").Build();
        var document = Load(source);
        var id = document.Blocks[0].Id;

        document.Execute(new InsertTableRowsCommand(id, 1, 1));
        document.Execute(new DeleteTableRowsCommand(id, 5, 1));
        document.Execute(new InsertTableRowsCommand(id, 4, 1));
        document.Execute(new InsertTableColumnsCommand(id, 1, 1));
        var output = DocxWriterTests.Save(document);

        var table = Body(output).Element(W + "tbl")!;
        Assert.Equal(["tblPr", "tblGrid", "customXml", "tr", "tr", "tr", "sdt"], table.Elements().Select(e => e.Name.LocalName));
        Assert.Equal("row", (string)table.Element(W + "customXml")!.Attribute(W + "element"));
        Assert.Equal("r1ar1b", table.Element(W + "customXml")!.Value);
        Assert.Equal("", table.Elements(W + "tr").First().Value);
        Assert.Equal("r2ar2b", table.Elements(W + "tr").ElementAt(1).Value);
        Assert.Equal("3", (string)table.Elements(W + "tr").ElementAt(1).Element(W + "sdt")!.Element(W + "sdtPr")!.Element(W + "id")!.Attribute(W + "val"));
        Assert.NotNull(table.Elements(W + "tr").ElementAt(2).Element(W + "trPr")!.Element(W + "del"));
        var sdt = table.Element(W + "sdt")!;
        Assert.Equal("4", (string)sdt.Element(W + "sdtPr")!.Element(W + "id")!.Attribute(W + "val"));
        Assert.Equal(["r3ar3b", "", "r4ar4b"], sdt.Element(W + "sdtContent")!.Elements(W + "tr").Select(r => r.Value));
        Assert.All(table.Descendants(W + "tr").Where(r => r.Element(W + "trPr")?.Element(W + "del") == null), r => Assert.Equal(3, r.Descendants(W + "tc").Count()));
        DocxWriterTests.AssertConsistent(output);
    }

    [Fact]
    public void InsertColumn_WidensSpanningCell()
    {
        var document = Load(Doc(Table([2000, 2000, 2000], Row(Cell("ab", span: 2), Cell("c")), Row(Cell("d"), Cell("e"), Cell("f")))));
        var id = document.Blocks[0].Id;

        document.Execute(new InsertTableColumnsCommand(id, 1, 1));

        var table = (Table)document.Blocks[0];
        Assert.Equal([2000, 2000, 2000, 2000], table.Grid);
        Assert.Equal([3, 1], table.Rows[0].Cells.Select(c => c.GridSpan));
        Assert.Equal(["d", "", "e", "f"], table.Rows[1].Cells.Select(c => ((Paragraph)c.Blocks[0]).Text));
        Assert.Equal(6000, table.Rows[0].Cells[0].Width);
        Assert.Equal(2000, table.Rows[1].Cells[1].Width);
        var output = DocxWriterTests.Save(document);
        DocxWriterTests.AssertConsistent(output);
        Assert.Equal(4, Body(output).Element(W + "tbl")!.Element(W + "tblGrid")!.Elements().Count());

        document.Execute(new DeleteTableColumnsCommand(id, 0, 2));

        table = (Table)document.Blocks[0];
        Assert.Equal([2000, 2000], table.Grid);
        Assert.Equal([1, 1], table.Rows[0].Cells.Select(c => c.GridSpan));
        Assert.Equal(["e", "f"], table.Rows[1].Cells.Select(c => ((Paragraph)c.Blocks[0]).Text));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
    }

    [Fact]
    public void MergeCells2x2_GridSpanVMerge_ContentJoined()
    {
        var document = Load(Doc(Table([2000, 2000, 2000], Row(Cell("a"), Cell("b"), Cell("c")), Row(Cell("d"), Cell(""), Cell("f")), Row(Cell("g"), Cell("h"), Cell("i")))));
        var original = document.Blocks[0];

        document.Execute(new MergeTableCellsCommand(original.Id, 0, 0, 1, 1));

        var table = (Table)document.Blocks[0];
        var merged = table.Rows[0].Cells[0];
        Assert.Equal((2, VerticalMerge.Restart), (merged.GridSpan, merged.VMerge));
        Assert.Equal(["a", "b", "d"], merged.Blocks.Select(b => ((Paragraph)b).Text));
        Assert.Equal((2, VerticalMerge.Continue), (table.Rows[1].Cells[0].GridSpan, table.Rows[1].Cells[0].VMerge));
        Assert.Equal("", Assert.IsType<Paragraph>(Assert.Single(table.Rows[1].Cells[0].Blocks)).Text);
        Assert.Equal(4000, merged.Width);
        var output = DocxWriterTests.Save(document);
        DocxWriterTests.AssertConsistent(output);
        var rows = Body(output).Element(W + "tbl")!.Elements(W + "tr").ToList();
        Assert.Equal("2", (string)rows[0].Element(W + "tc")!.Element(W + "tcPr")!.Element(W + "gridSpan")!.Attribute(W + "val"));
        Assert.Equal("restart", (string)rows[0].Element(W + "tc")!.Element(W + "tcPr")!.Element(W + "vMerge")!.Attribute(W + "val"));
        Assert.Null(rows[1].Element(W + "tc")!.Element(W + "tcPr")!.Element(W + "vMerge")!.Attribute(W + "val"));
        Assert.Equal(2, rows[1].Elements(W + "tc").Count());

        document.Undo.Undo();

        Assert.Same(original, document.Blocks[0]);
        document.Execute(new MergeTableCellsCommand(original.Id, 1, 1, 1, 1));
        Assert.Same(original, document.Blocks[0]);
    }

    [Fact]
    public void Merge_RangeCuttingSpannedCell_Throws()
    {
        var document = Load(Doc(Table([2000, 2000, 2000], Row(Cell("ab", span: 2), Cell("c")), Row(Cell("d"), Cell("e"), Cell("f")))));

        Assert.Throws<ArgumentException>(() => document.Execute(new MergeTableCellsCommand(document.Blocks[0].Id, 0, 1, 1, 2)));
        Assert.False(document.Undo.CanUndo);
    }

    [Fact]
    public void SplitCell_RestoresGrid()
    {
        var document = Load(Doc(Table([2000, 2000, 2000], Row(Cell("a"), Cell("b"), Cell("c")), Row(Cell("d"), Cell("e"), Cell("f")))));
        var id = document.Blocks[0].Id;
        document.Execute(new MergeTableCellsCommand(id, 0, 0, 1, 1));

        document.Execute(new SplitTableCellCommand(id, 1, 1));

        var table = (Table)document.Blocks[0];
        Assert.All(table.Rows, r => Assert.Equal([1, 1, 1], r.Cells.Select(c => c.GridSpan)));
        Assert.All(table.Rows.SelectMany(r => r.Cells), c => Assert.Equal(VerticalMerge.None, c.VMerge));
        Assert.Equal(["a", "b", "d", "e"], table.Rows[0].Cells[0].Blocks.Select(b => ((Paragraph)b).Text));
        Assert.All(table.Rows.SelectMany(r => r.Cells), c => Assert.IsType<Paragraph>(c.Blocks[^1]));
        Assert.Equal([2000, 2000, 2000], table.Rows[1].Cells.Select(c => c.Width));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
        var unchanged = document.Blocks[0];
        document.Execute(new SplitTableCellCommand(id, 0, 2));
        Assert.Same(unchanged, document.Blocks[0]);
    }

    [Fact]
    public void DeleteAllRows_RemovesTable()
    {
        var document = Load(Doc(Para("x") + Table([3000], Row(Cell("a")), Row(Cell("b"))) + Para("y")));
        var table = document.Blocks[1];

        document.Execute(new DeleteTableRowsCommand(table.Id, 0, 2));

        Assert.Equal(["x", "y"], document.Blocks.Select(b => ((Paragraph)b).Text));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));

        document.Undo.Undo();

        Assert.Same(table, document.Blocks[1]);
    }

    [Fact]
    public void DeleteRows_MiddleOfVerticalMerge_RestartsBelow()
    {
        var document = Load(Doc(Table([2000, 2000], Row(Cell("a", merge: "restart"), Cell("b")), Row(Cell("", merge: "continue"), Cell("c")), Row(Cell("", merge: "continue"), Cell("d")))));

        document.Execute(new DeleteTableRowsCommand(document.Blocks[0].Id, 0, 1));

        var table = (Table)document.Blocks[0];
        Assert.Equal([VerticalMerge.Restart, VerticalMerge.Continue], table.Rows.Select(r => r.Cells[0].VMerge));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
    }

    [Fact]
    public void DeleteNestedTable_CellKeepsParagraph_OnlyTableInBody_GetsParagraph()
    {
        var document = Load(Doc(Table([4000], Row("<w:tc>" + Table([2000], Row(Cell("inner"))) + "<w:p/></w:tc>"))));
        var outer = (Table)document.Blocks[0];
        var inner = (Table)outer.Rows[0].Cells[0].Blocks[0];

        document.Execute(new DeleteTableCommand(inner.Id));

        var cell = ((Table)document.Blocks[0]).Rows[0].Cells[0];
        Assert.IsType<Paragraph>(Assert.Single(cell.Blocks));

        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
        document.Undo.Undo();
        Assert.Same(outer, document.Blocks[0]);

        var alone = new DocxBuilder().Body(Table([2000], Row(Cell("only")))).Load();
        alone.Execute(new DeleteTableCommand(alone.Blocks[0].Id));

        Assert.IsType<Paragraph>(Assert.Single(alone.Blocks));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(alone));
    }

    [Fact]
    public void Writer_TableEdits_GridAndCellsConsistent()
    {
        var source = Doc(Table([2000, 2000, 2000], Row(Cell("a"), Cell("b"), Cell("c")), Row(Cell("d"), Cell("e"), Cell("f")), Row(Cell("g"), Cell("h"), Cell("i"))));
        var document = Load(source);
        var id = document.Blocks[0].Id;

        document.Execute(new SetTableColumnWidthsCommand(id, [1000, 2500, 2500]));
        document.Execute(new SetTableHeaderRowsCommand(id, 1));
        document.Execute(new SetTableFormatCommand(id, f => f with { Alignment = TableAlignment.Center }, "Center"));
        document.Execute(new SetTableCellFormatCommand(id, [(1, 1), (2, 2)], c => c with { Shading = "#00FF00", VerticalAlign = CellVerticalAlignment.Bottom, Blocks = [] }, "Shade"));
        document.Execute(new MergeTableCellsCommand(id, 1, 1, 2, 2));
        document.Execute(new InsertTableColumnsCommand(id, 3, 1));
        document.Execute(new InsertTableRowsCommand(id, 2, 1));
        var output = DocxWriterTests.Save(document);

        DocxWriterTests.AssertConsistent(output);
        var table = Body(output).Element(W + "tbl")!;
        Assert.Equal(["1000", "2500", "2500", "2500"], table.Element(W + "tblGrid")!.Elements().Select(c => (string)c.Attribute(W + "w")));
        Assert.Equal("center", (string)table.Element(W + "tblPr")!.Element(W + "jc")!.Attribute(W + "val"));
        Assert.NotNull(table.Element(W + "tr")!.Element(W + "trPr")!.Element(W + "tblHeader"));
        var rows = table.Elements(W + "tr").ToList();
        Assert.Equal(4, rows.Count);
        Assert.All(rows, r => Assert.Equal(4, r.Elements(W + "tc").Sum(c => int.Parse((string)c.Element(W + "tcPr")?.Element(W + "gridSpan")?.Attribute(W + "val") ?? "1"))));
        Assert.Equal("continue", Describe(rows[2].Elements(W + "tc").ElementAt(1)));
        Assert.Equal("continue", Describe(rows[3].Elements(W + "tc").ElementAt(1)));
        Assert.Equal("00FF00", (string)rows[1].Elements(W + "tc").ElementAt(1).Element(W + "tcPr")!.Element(W + "shd")!.Attribute(W + "fill"));
        Assert.Equal(Describe(document), Describe(Load(output)));
    }

    private static string Describe(XElement cell) => cell.Element(W + "tcPr")?.Element(W + "vMerge") is { } merge ? (string)merge.Attribute(W + "val") ?? "continue" : "none";

    private static string Describe(TextDocument document) => DocumentDescriber.Describe(document);

    internal static byte[] Nested() => new DocxBuilder()
        .Styles("<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>")
        .Body("<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"4000\"/><w:gridCol w:w=\"4000\"/></w:tblGrid>" +
              "<w:tr><w:tc><w:tcPr><w:tcW w:w=\"4000\" w:type=\"dxa\"/></w:tcPr><w:p><w:r><w:t>a</w:t></w:r></w:p></w:tc>" +
              "<w:tc><w:tcPr><w:tcW w:w=\"4000\" w:type=\"dxa\"/></w:tcPr><w:p><w:r><w:t>b</w:t></w:r></w:p></w:tc></w:tr>" +
              "<w:tr><w:tc><w:p><w:r><w:t>c</w:t></w:r></w:p>" +
              "<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"2000\"/></w:tblGrid><w:tr><w:tc><w:p><w:r><w:t>inner</w:t></w:r></w:p></w:tc></w:tr></w:tbl>" +
              "<w:p/></w:tc><w:tc><w:p><w:r><w:t>d</w:t></w:r></w:p></w:tc></w:tr></w:tbl><w:p/>")
        .Build();

    internal static string Table(int[] grid, params string[] rows) =>
        $"<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr><w:tblGrid>{string.Concat(grid.Select(w => $"<w:gridCol w:w=\"{w}\"/>"))}</w:tblGrid>{string.Concat(rows)}</w:tbl>";

    internal static string Row(params string[] cells) => $"<w:tr>{string.Concat(cells)}</w:tr>";

    internal static string Cell(string text, int span = 1, string merge = null) =>
        $"<w:tc><w:tcPr><w:tcW w:w=\"{2000 * span}\" w:type=\"dxa\"/>{(span > 1 ? $"<w:gridSpan w:val=\"{span}\"/>" : "")}" +
        $"{(merge == null ? "" : merge == "restart" ? "<w:vMerge w:val=\"restart\"/>" : "<w:vMerge/>")}</w:tcPr>{Para(text)}</w:tc>";

    private static string Para(string text) => text.Length > 0 ? $"<w:p><w:r><w:t>{text}</w:t></w:r></w:p>" : "<w:p/>";

    private static byte[] Doc(string body) => new DocxBuilder()
        .Styles("<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>")
        .Body(body.EndsWith("</w:p>", StringComparison.Ordinal) || body.EndsWith("<w:p/>", StringComparison.Ordinal) ? body : body + "<w:p/>")
        .Build();

    private static TextDocument Load(byte[] bytes) => TextDocument.Load(new MemoryStream(bytes));

    private static XElement Body(byte[] package) => OpcAssert.Xml(package, "/word/document.xml").Root!.Element(W + "body")!;

    private static DocumentRange Range(TextDocument document, string startId, int start, string endId, int end) =>
        new(document.PositionOf(startId, start), document.PositionOf(endId, end));
}
