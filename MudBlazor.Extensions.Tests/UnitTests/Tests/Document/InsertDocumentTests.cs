using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class InsertDocumentTests
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace W14 = "http://schemas.microsoft.com/office/word/2010/wordml";

    [Fact]
    public void InsertDocument_MiddleOfParagraph_MergesFirstAndLast()
    {
        var document = Text("Hello World");
        var id = document.Blocks[0].Id;
        var source = HtmlReader.Read("<p><b>one</b></p><h2>two</h2><table><tr><td>cell</td></tr></table><p>three <a href=\"https://example.com\">link</a></p>");
        var command = new InsertDocumentCommand(Caret(document, id, 6), source);

        document.Execute(command);

        var blocks = document.Blocks;
        Assert.Equal(4, blocks.Count);
        var first = Assert.IsType<Paragraph>(blocks[0]);
        Assert.Equal(id, first.Id);
        Assert.Equal("Hello one", first.Text);
        Assert.Null(first.Format.StyleId);
        Assert.True(first.Inlines[^1].Format.Bold);
        Assert.Equal(("two", "Heading2"), (((Paragraph)blocks[1]).Text, ((Paragraph)blocks[1]).Format.StyleId));
        Assert.Equal("cell", ((Paragraph)((Table)blocks[2]).Rows[0].Cells[0].Blocks[0]).Text);
        var last = Assert.IsType<Paragraph>(blocks[3]);
        Assert.Equal("three linkWorld", last.Text);
        Assert.Equal(("https://example.com", "Hyperlink"), (last.Inlines[1].Link?.SafeTarget, last.Inlines[1].Format.StyleId));
        Assert.Null(last.Inlines[^1].Link);
        Assert.Equal(document.PositionOf(last.Id, 10), command.After.Start);
        Assert.NotNull(document.Styles.Find("Heading2"));
        Assert.NotNull(document.Styles.Find("Hyperlink"));
        var ids = document.AllParagraphs().Select(p => p.Id).Concat(blocks.OfType<Table>().Select(b => b.Id)).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(ids, i => Assert.NotNull(document.FindBlock(i)));
        Assert.DoesNotContain(blocks, b => source.Blocks.Contains(b));

        var empty = TextDocument.Create();
        empty.Execute(new InsertDocumentCommand(Caret(empty, empty.Blocks[0].Id, 0), HtmlReader.Read("<h1>T</h1><ul><li>x</li></ul>")));
        Assert.Equal([("T", "Heading1", false), ("x", null, true)], empty.Blocks.Cast<Paragraph>().Select(p => (p.Text, p.Format.StyleId, p.Format.Numbering != null)));

        var range = Text("Hello World");
        var rangeId = range.Blocks[0].Id;
        range.Execute(new InsertDocumentCommand(new DocumentRange(range.PositionOf(rangeId, 2), range.PositionOf(rangeId, 8)), HtmlReader.Read("<p>A</p><p>B</p>")));
        Assert.Equal(["HeA", "Brld"], range.Blocks.Cast<Paragraph>().Select(p => p.Text));

        var output = DocxWriterTests.Save(document);
        DocxWriterTests.AssertConsistent(output);
    }

    [Fact]
    public void InsertDocument_RemapsNumberingCopiesMissingStyles()
    {
        var document = new DocxBuilder()
            .Styles("<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>" +
                    "<w:style w:type=\"paragraph\" w:styleId=\"Heading1\"><w:name w:val=\"heading 1\"/><w:basedOn w:val=\"Normal\"/><w:rPr><w:sz w:val=\"60\"/></w:rPr></w:style>")
            .Numbering("<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\"/><w:numFmt w:val=\"upperRoman\"/><w:lvlText w:val=\"%1.\"/></w:lvl></w:abstractNum>" +
                       "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\"/></w:num>")
            .Body("<w:p><w:pPr><w:numPr><w:ilvl w:val=\"0\"/><w:numId w:val=\"1\"/></w:numPr></w:pPr><w:r><w:t>existing</w:t></w:r></w:p><w:p/>")
            .Load();
        var heading1 = document.Styles.Find("Heading1");
        var source = HtmlReader.Read("<h1>A</h1><ol><li>x</li><li>y</li></ol><h4>B</h4>");
        Assert.Equal(1, source.Numbering.Instances.Single().Id);

        document.Execute(new InsertDocumentCommand(Caret(document, document.Blocks[1].Id, 0), source));

        var paragraphs = document.Blocks.Cast<Paragraph>().ToList();
        Assert.Equal(["existing", "A", "x", "y", "B"], paragraphs.Select(p => p.Text));
        Assert.Same(heading1, document.Styles.Find("Heading1"));
        Assert.Equal(30, document.EffectiveRunFormat(paragraphs[1], paragraphs[1].Inlines[0]).SizePt);
        Assert.Equal(source.Styles.Find("Heading4")!.RunFormat, document.Styles.Find("Heading4")!.RunFormat);
        Assert.Equal(source.Styles.Find("Heading4")!.BasedOn, document.Styles.Find("Heading4")!.BasedOn);
        var numId = paragraphs[2].Format.Numbering!.Value.NumId;
        Assert.NotEqual(1, numId);
        Assert.Equal(numId, paragraphs[3].Format.Numbering!.Value.NumId);
        Assert.Equal(2, document.Numbering.Instances.Count);
        Assert.Equal(2, document.Numbering.Abstracts.Select(a => a.Id).Distinct().Count());
        Assert.Equal(["I.", "1.", "2."], new[] { paragraphs[0], paragraphs[2], paragraphs[3] }.Select(document.ListLabel));

        var png = ObjectCommandTests.Png(10, 10);
        var image = HtmlReader.Read($"<p><img src=\"data:image/png;base64,{Convert.ToBase64String(png)}\"></p>");
        var end = document.Blocks[^1].Id;
        document.Execute(new InsertDocumentCommand(Caret(document, end, ((Paragraph)document.Blocks[^1]).Length), image));
        document.Execute(new InsertDocumentCommand(Caret(document, end, ((Paragraph)document.Blocks[^1]).Length), image));
        var images = document.AllParagraphs().SelectMany(p => p.Inlines).OfType<InlineImage>().ToList();
        Assert.Equal(2, images.Count);
        Assert.Single(images.Select(i => i.MediaPart).Distinct());
        Assert.Equal(png, document.GetMedia(images[0].MediaPart));

        var output = DocxWriterTests.Save(document);
        DocxWriterTests.AssertConsistent(output);
        Assert.Single(OpcPackage.Open(output).PartNames, p => p.StartsWith("/word/media/", StringComparison.Ordinal));
    }

    [Fact]
    public void InsertDocument_OneUndoStep()
    {
        var document = Text("Hello World");
        var before = document.Blocks.ToList();
        var styles = document.Styles;
        var numbering = document.Numbering;
        var events = 0;
        document.Changed += (_, _) => events++;
        var source = HtmlReader.Read("<h3>a</h3><ol><li>b</li></ol><table><tr><td>c</td></tr></table><p>d</p>");

        document.Execute(new InsertDocumentCommand(Caret(document, before[0].Id, 5), source));
        var after = document.Blocks.ToList();

        Assert.Equal(1, events);
        Assert.Equal(4, after.Count);
        document.Undo.Undo();
        Assert.False(document.Undo.CanUndo);
        Assert.Equal(before.Count, document.Blocks.Count);
        Assert.All(before.Zip(document.Blocks), pair => Assert.Same(pair.First, pair.Second));
        Assert.Same(styles, document.Styles);
        Assert.Same(numbering, document.Numbering);
        document.Undo.Redo();
        Assert.All(after.Zip(document.Blocks), pair => Assert.Same(pair.First, pair.Second));
        Assert.Equal(3, events);

        var unchanged = document.Version;
        document.Execute(new InsertDocumentCommand(Caret(document, after[0].Id, 0), HtmlReader.Read("")));
        Assert.Equal(unchanged, document.Version);
    }

    [Fact]
    public void InsertDocument_FromDocx_StripsIdsAndForeignParts()
    {
        var png = ObjectCommandTests.Png(4, 4);
        var source = new DocxBuilder()
            .Styles("<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>" +
                    "<w:style w:type=\"paragraph\" w:styleId=\"Fancy\"><w:name w:val=\"Fancy\"/><w:basedOn w:val=\"Base\"/><w:pPr><w:jc w:val=\"center\"/></w:pPr></w:style>" +
                    "<w:style w:type=\"paragraph\" w:styleId=\"Base\"><w:name w:val=\"Base\"/><w:rPr><w:i/></w:rPr></w:style>")
            .Media("rId9", "image1.png", png)
            .Body("<w:p><w:pPr><w:pStyle w:val=\"Fancy\"/></w:pPr>" +
                  "<w:bookmarkStart w:id=\"0\" w:name=\"Mark\"/><w:r><w:t xml:space=\"preserve\">first </w:t></w:r><w:bookmarkEnd w:id=\"0\"/>" +
                  "<w:fldSimple w:instr=\" PAGE \"><w:r><w:t>7</w:t></w:r></w:fldSimple></w:p>" +
                  "<w:p><w:pPr><w:pBdr><w:top w:val=\"single\" w:sz=\"4\" w:space=\"1\" w:color=\"auto\"/></w:pBdr></w:pPr><w:r><w:drawing><wp:inline><wp:extent cx=\"38100\" cy=\"38100\"/><wp:docPr id=\"1\" name=\"P\"/><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">" +
                  "<pic:pic><pic:nvPicPr><pic:cNvPr id=\"1\" name=\"P\"/><pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed=\"rId9\"/></pic:blipFill><pic:spPr/></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>" +
                  "<w:p><w:pPr><w:pStyle w:val=\"Fancy\"/></w:pPr><w:r><w:t>last</w:t></w:r></w:p>")
            .Load();
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;

        document.Execute(new InsertDocumentCommand(Caret(document, id, 0), source));

        Assert.Equal(["first 7","\uFFFC", "last"], document.Blocks.Cast<Paragraph>().Select(p => p.Text));
        Assert.DoesNotContain(document.AllParagraphs().SelectMany(p => p.Inlines), i => i is Marker or OpaqueInline);
        Assert.Equal(["Base", "Fancy"], document.Styles.All.Select(s => s.Id).Where(s => s is "Base" or "Fancy").Order());
        var output = DocxWriterTests.Save(document);
        DocxWriterTests.AssertConsistent(output);
        var body = OpcAssert.Xml(output, "/word/document.xml").Root!.Element(W + "body")!;
        Assert.NotNull(source.Blocks[0].Source!.Attribute(W14 + "paraId"));
        Assert.Empty(body.Descendants().Attributes(W14 + "paraId"));
        Assert.Empty(body.Descendants().Attributes(W14 + "textId"));
        Assert.Empty(body.Descendants(W + "bookmarkStart"));
        Assert.Empty(body.Descendants(W + "fldSimple"));
        Assert.Equal(png, OpcPackage.Open(output).ReadBytes("/word/media/image1.png"));
        var reloaded = TextDocument.Load(new MemoryStream(output));
        Assert.All(new[] { 0, 2 }, k =>
        {
            var paragraph = (Paragraph)reloaded.Blocks[k];
            Assert.Equal(("Fancy", ParagraphAlignment.Center), (paragraph.Format.StyleId, reloaded.EffectiveParagraphFormat(paragraph).Alignment));
            Assert.True(reloaded.EffectiveRunFormat(paragraph, paragraph.Inlines[0]).Italic);
        });
        Assert.Single(body.Descendants(W + "pBdr"));
    }

    [Fact]
    public void InsertDocument_IntoTableCell_NestsTable()
    {
        var document = WithTable();
        var cell = CellParagraph(document, 0);

        var after = Apply(document, new DocumentRange(document.PositionOf(cell.Id, 4), document.PositionOf(cell.Id, 4)),
            "<p>x</p><table><tr><td>inner</td></tr></table><p>y</p>");

        var blocks = ((Table)after[1]).Rows[0].Cells[0].Blocks;
        Assert.Equal(3, blocks.Count);
        Assert.Equal(("cellx", "y one"), (((Paragraph)blocks[0]).Text, ((Paragraph)blocks[2]).Text));
        Assert.Equal("inner", ((Paragraph)((Table)blocks[1]).Rows[0].Cells[0].Blocks[0]).Text);
    }

    [Fact]
    public void InsertDocument_ReplacesSelectionAcrossBlocks()
    {
        var document = HtmlReader.Read("<p>alpha</p><p>beta</p><p>gamma</p>");
        var (first, last) = (document.Blocks[0].Id, document.Blocks[2].Id);

        var after = Apply(document, new DocumentRange(document.PositionOf(first, 2), document.PositionOf(last, 2)), "<p>X</p><p>Y</p>");

        Assert.Equal(["alX", "Ymma"], after.Cast<Paragraph>().Select(p => p.Text));
    }

    [Fact]
    public void InsertDocument_ReplacesSelectionAcrossCells()
    {
        var document = WithTable();
        var (one, two) = (CellParagraph(document, 0), CellParagraph(document, 1));

        var after = Apply(document, new DocumentRange(document.PositionOf(one.Id, 2), document.PositionOf(two.Id, 2)), "<p>X</p><p>Y</p>");

        var cells = ((Table)after[1]).Rows[0].Cells;
        Assert.Equal(["ceX", "Y"], cells[0].Blocks.Cast<Paragraph>().Select(p => p.Text));
        Assert.Equal(["ll two"], cells[1].Blocks.Cast<Paragraph>().Select(p => p.Text));
    }

    [Fact]
    public void InsertDocument_TableFirstAtStart_TableLastAtEnd()
    {
        var document = HtmlReader.Read("<p>text</p>");
        var id = document.Blocks[0].Id;

        var atStart = Apply(document, Caret(document, id, 0), "<table><tr><td>t1</td></tr></table><p>p1</p>");

        Assert.Equal(["", "table", "p1text"], atStart.Select(b => b is Paragraph p ? p.Text : "table"));
        var end = ((Paragraph)document.Blocks[^1]).Id;
        var atEnd = Apply(document, Caret(document, end, ((Paragraph)document.Blocks[^1]).Length), "<p>p2</p><table><tr><td>t2</td></tr></table>");
        Assert.Equal(["", "table", "p1textp2", "table", ""], atEnd.Select(b => b is Paragraph p ? p.Text : "table"));
    }

    [Fact]
    public void InsertDocument_TableLastAtStart_TableFirstAtEnd()
    {
        var document = HtmlReader.Read("<p>text</p>");
        var id = document.Blocks[0].Id;

        var atStart = Apply(document, Caret(document, id, 0), "<p>p1</p><table><tr><td>t1</td></tr></table>");

        Assert.Equal(["p1", "table", "text"], atStart.Select(b => b is Paragraph p ? p.Text : "table"));
        var last = (Paragraph)document.Blocks[^1];
        var atEnd = Apply(document, Caret(document, last.Id, last.Length), "<table><tr><td>t2</td></tr></table><p>p2</p>");
        Assert.Equal(["p1", "table", "text", "table", "p2"], atEnd.Select(b => b is Paragraph p ? p.Text : "table"));
    }

    [Fact]
    public void InsertDocument_SingleParagraphMidParagraph_KeepsTargetFormat()
    {
        var document = HtmlReader.Read("<h1>Heading</h1>");
        var id = document.Blocks[0].Id;

        var after = Apply(document, Caret(document, id, 4), "<p style=\"text-align:center\"><b>bold</b></p>");

        var paragraph = (Paragraph)Assert.Single(after);
        Assert.Equal(("Headbolding", "Heading1", (ParagraphAlignment?)null), (paragraph.Text, paragraph.Format.StyleId, paragraph.Format.Alignment));
        Assert.True(paragraph.Inlines[1].Format.Bold);
    }

    [Fact]
    public void InsertDocument_Undo_LeavesNoMediaInTheFile()
    {
        var document = TextDocument.Create();
        var png = ObjectCommandTests.Png(8, 8);

        document.Execute(new InsertDocumentCommand(Caret(document, document.Blocks[0].Id, 0), HtmlReader.Read($"<p><img src=\"data:image/png;base64,{Convert.ToBase64String(png)}\"></p>")));
        document.Undo.Undo();
        var output = DocxWriterTests.Save(document);

        DocxWriterTests.AssertConsistent(output);
        var opc = OpcPackage.Open(output);
        Assert.DoesNotContain(opc.PartNames, p => p.StartsWith("/word/media/", StringComparison.Ordinal));
        Assert.DoesNotContain(opc.Relationships("/word/document.xml"), r => r.Type.EndsWith("/image", StringComparison.Ordinal));
    }

    private static IReadOnlyList<Block> Apply(TextDocument document, DocumentRange range, string html)
    {
        var before = document.Blocks.ToList();
        var styles = document.Styles;
        var numbering = document.Numbering;
        document.Execute(new InsertDocumentCommand(range, HtmlReader.Read(html)));
        var after = document.Blocks.ToList();
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));

        document.Undo.Undo();
        Assert.Equal(before.Count, document.Blocks.Count);
        Assert.All(before.Zip(document.Blocks), pair => Assert.Same(pair.First, pair.Second));
        Assert.Same(styles, document.Styles);
        Assert.Same(numbering, document.Numbering);
        document.Undo.Redo();
        Assert.All(after.Zip(document.Blocks), pair => Assert.Same(pair.First, pair.Second));
        return after;
    }

    private static TextDocument WithTable() => HtmlReader.Read("<p>before</p><table><tr><td>cell one</td><td>cell two</td></tr></table><p>after</p>");

    private static Paragraph CellParagraph(TextDocument document, int cell) => (Paragraph)((Table)document.Blocks[1]).Rows[0].Cells[cell].Blocks[0];

    private static TextDocument Text(string text)
    {
        var document = TextDocument.Create();
        document.Execute(new ReplaceTextCommand(Caret(document, document.Blocks[0].Id, 0), text));
        document.Undo.Clear();
        return document;
    }

    private static DocumentRange Caret(TextDocument document, string paragraphId, int offset)
    {
        var position = document.PositionOf(paragraphId, offset);
        return new DocumentRange(position, position);
    }
}
