using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class ExtractTests
{
    private static readonly RunFormat Bold = new() { Bold = true };

    private static Paragraph P(params Inline[] inlines) => new() { Inlines = inlines };

    private static TextRun R(string text, RunFormat? format = null) => new(text) { Format = format ?? new RunFormat() };

    private static DocumentRange Range(TextDocument d, Paragraph a, int o1, Paragraph b, int o2) => new(d.PositionOf(a.Id, o1), d.PositionOf(b.Id, o2));

    private static Paragraph Para(TextDocument d, string text) => d.AllParagraphs(true).First(p => p.Text == text);

    private static List<string> Texts(TextDocument d) => d.Blocks.Select(b => b is Paragraph p ? p.Text : "table").ToList();

    private static TextDocument WithTable() =>
        HtmlReader.Read("<p>before</p><table><tr><td><p>a1</p><p>a2</p><p>a3</p></td><td>cell two</td></tr></table><p>after</p>");

    [Fact]
    public void Extract_PartialParagraphs_KeepsFormatsAndText()
    {
        var d = TextDocument.FromBlocks([
            new Paragraph { Inlines = [R("Hello "), R("bold", Bold), R(" world")], Format = new ParagraphFormat { Alignment = ParagraphAlignment.Center } },
            new Paragraph { Inlines = [R("Second")], Format = new ParagraphFormat { Alignment = ParagraphAlignment.Right } }]);

        var extract = d.Extract(Range(d, (Paragraph)d.Blocks[0], 7, (Paragraph)d.Blocks[1], 3));

        Assert.Equal(["old world", "Sec"], Texts(extract));
        var paragraphs = extract.Blocks.Cast<Paragraph>().ToList();
        Assert.True(paragraphs[0].Inlines[0].Format.Bold);
        Assert.Equal([ParagraphAlignment.Center, ParagraphAlignment.Right], paragraphs.Select(p => p.Format.Alignment!.Value));
        Assert.False(extract.EndsWithParagraphMark);
        Assert.Same(d.Styles, extract.Styles);
        Assert.Same(d.Numbering, extract.Numbering);
        Assert.Same(d.Theme, extract.Theme);
        Assert.All(extract.Blocks, b => Assert.Same(b, extract.FindBlock(b.Id)));
    }

    [Fact]
    public void Extract_ThenInsertDocument_ReproducesTheContent()
    {
        var d = HtmlReader.Read("<h1>Title</h1><ol><li>item</li></ol><p>tail</p>");
        var item = Para(d, "item");

        var extract = d.Extract(Range(d, Para(d, "Title"), 0, Para(d, "tail"), 4));
        var target = TextDocument.Create();
        target.Execute(new InsertDocumentCommand(new DocumentRange(target.PositionOf(target.Blocks[0].Id, 0), target.PositionOf(target.Blocks[0].Id, 0)), extract));

        Assert.Equal(item.Format.Numbering, ((Paragraph)extract.Blocks[1]).Format.Numbering);
        Assert.Equal(["Title", "item", "tail"], Texts(target));
        var pasted = target.Blocks.Cast<Paragraph>().ToList();
        Assert.Equal("Heading1", pasted[0].Format.StyleId);
        Assert.Equal("1.", target.ListLabel(pasted[1]));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(target));
    }

    [Fact]
    public void Extract_InsideOneCell_CopiesOnlyThoseParagraphs()
    {
        var d = WithTable();

        var extract = d.Extract(Range(d, Para(d, "a1"), 1, Para(d, "a2"), 1));

        Assert.Equal(["1", "a"], Texts(extract));
    }

    [Fact]
    public void Extract_AcrossATable_CopiesTheWholeTable()
    {
        var d = WithTable();

        var into = d.Extract(Range(d, Para(d, "before"), 2, Para(d, "a2"), 1));
        var outOf = d.Extract(Range(d, Para(d, "cell two"), 2, Para(d, "after"), 2));
        var cells = d.Extract(Range(d, Para(d, "a1"), 1, Para(d, "cell two"), 1));

        Assert.Equal(["fore", "table"], Texts(into));
        Assert.Equal(["table", "af"], Texts(outOf));
        Assert.Equal(["table"], Texts(cells));
        var table = (Table)into.Blocks[1];
        Assert.Equal(["a1", "a2", "a3", "cell two"], table.Rows[0].Cells.SelectMany(c => c.Blocks).Cast<Paragraph>().Select(p => p.Text));
        Assert.All(table.Rows[0].Cells.SelectMany(c => c.Blocks), b => Assert.Same(b, into.FindBlock(b.Id)));
    }

    [Fact]
    public void Extract_Image_CopiesItsMedia()
    {
        var png = ObjectCommandTests.Png(6, 6);
        var d = HtmlReader.Read($"<p>x<img src=\"data:image/png;base64,{Convert.ToBase64String(png)}\">y</p>");
        var paragraph = (Paragraph)d.Blocks[0];
        var part = paragraph.Inlines.OfType<InlineImage>().Single().MediaPart;

        var extract = d.Extract(Range(d, paragraph, 1, paragraph, 2));

        Assert.Equal(part, ((Paragraph)extract.Blocks[0]).Inlines.OfType<InlineImage>().Single().MediaPart);
        Assert.Equal(png, extract.GetMedia(part));
        Assert.NotSame(d.GetMedia(part), extract.GetMedia(part));
        Assert.Equal(d.MediaContentType(part), extract.MediaContentType(part));
    }

    [Fact]
    public void Extract_BodyAndHeader_Throws()
    {
        var d = new DocxBuilder().Header("<w:p><w:r><w:t>head line</w:t></w:r></w:p>").Body("<w:p><w:r><w:t>body</w:t></w:r></w:p>").Load();
        var header = Para(d, "head line");

        Assert.Throws<ArgumentException>(() => d.Extract(Range(d, Para(d, "body"), 1, header, 2)));
        Assert.Equal(["ead"], Texts(d.Extract(Range(d, header, 1, header, 4))));
    }

    [Fact]
    public void Extract_EmptyRange_HasNoBlocks()
    {
        var d = TextDocument.FromBlocks([P(R("abc"))]);

        Assert.Empty(d.Extract(Range(d, (Paragraph)d.Blocks[0], 1, (Paragraph)d.Blocks[0], 1)).Blocks);
    }

    [Fact]
    public void Extract_Reversed_SameAsForward()
    {
        var d = TextDocument.FromBlocks([P(R("abc")), P(R("def"))]);
        var (a, b) = ((Paragraph)d.Blocks[0], (Paragraph)d.Blocks[1]);

        Assert.Equal(Texts(d.Extract(Range(d, a, 1, b, 2))), Texts(d.Extract(Range(d, b, 2, a, 1))));
    }

    [Fact]
    public void Extract_InsideOneParagraph_KeepsOnlyTheCutInlinesAndTheFormat()
    {
        var d = TextDocument.FromBlocks([new Paragraph { Inlines = [R("ab"), R("cd", Bold), R("ef")], Format = new ParagraphFormat { Alignment = ParagraphAlignment.Center } }]);

        var extract = d.Extract(Range(d, (Paragraph)d.Blocks[0], 1, (Paragraph)d.Blocks[0], 3));

        var paragraph = (Paragraph)Assert.Single(extract.Blocks);
        Assert.Equal(("bc", ParagraphAlignment.Center), (paragraph.Text, paragraph.Format.Alignment!.Value));
        Assert.Equal([false, true], paragraph.Inlines.Select(i => i.Format.Bold == true));
        Assert.False(extract.EndsWithParagraphMark);
    }

    [Fact]
    public void Extract_EndingAtStartOfLaterParagraph_EndsWithParagraphMark()
    {
        var d = TextDocument.FromBlocks([P(R("abc")), P(R("def")), P(R("ghi"))]);

        var extract = d.Extract(Range(d, (Paragraph)d.Blocks[0], 1, (Paragraph)d.Blocks[2], 0));

        Assert.Equal(["bc", "def"], Texts(extract));
        Assert.True(extract.EndsWithParagraphMark);
    }

    [Fact]
    public void Extract_Markers_KeepsCompletePairsOnly()
    {
        var d = new DocxBuilder().Body(
            "<w:p><w:bookmarkStart w:id=\"1\" w:name=\"Outer\"/><w:r><w:t>ab</w:t></w:r>" +
            "<w:bookmarkStart w:id=\"2\" w:name=\"Inner\"/><w:r><w:t>cd</w:t></w:r><w:bookmarkEnd w:id=\"2\"/><w:r><w:t>ef</w:t></w:r></w:p>" +
            "<w:p><w:r><w:t>gh</w:t></w:r><w:bookmarkEnd w:id=\"1\"/><w:r><w:t>ij</w:t></w:r></w:p>").Load();

        var extract = d.Extract(Range(d, Para(d, "abcdef"), 1, Para(d, "ghij"), 3));

        Assert.Equal(["bcdef", "ghi"], Texts(extract));
        var markers = extract.AllParagraphs().SelectMany(p => p.Inlines).OfType<Marker>().ToList();
        Assert.Equal(["bookmarkStart", "bookmarkEnd"], markers.Select(m => m.Kind));
        Assert.Equal("Inner", markers[0].Name);
    }

    [Fact]
    public void Extract_SourceEditedAfterwards_ExtractUnchanged()
    {
        var d = TextDocument.FromBlocks([P(R("Hello")), P(R("World"))]);
        var extract = d.Extract(Range(d, (Paragraph)d.Blocks[0], 0, (Paragraph)d.Blocks[1], 5));

        d.Execute(new ReplaceTextCommand(Range(d, (Paragraph)d.Blocks[0], 1, (Paragraph)d.Blocks[0], 4), "XYZ"));

        Assert.Equal("HXYZo", ((Paragraph)d.Blocks[0]).Text);
        Assert.Equal(["Hello", "World"], Texts(extract));
    }
}
