using System.Text;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class TextMarkdownTests
{
    private const string Gruesse = "Gr\u00FC\u00DFe";

    [Fact]
    public void Txt_Read_Utf8BomAnd1252()
    {
        var utf8 = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(Gruesse)).ToArray();
        var cp1252 = System.Text.CodePagesEncodingProvider.Instance.GetEncoding(1252)!.GetBytes(Gruesse);

        Assert.Equal(Gruesse, Single(Load(utf8)).Text);
        Assert.Equal(Gruesse, Single(Load(cp1252)).Text);
        Assert.Equal(Gruesse, Single(Load(Encoding.UTF8.GetBytes(Gruesse))).Text);
    }

    [Fact]
    public void Txt_Read_LinesAndFormFeed()
    {
        var document = Load(Encoding.UTF8.GetBytes("a\r\nb\nc\rd\fe\tf\u0001\v\U0001F600\n"));

        var paragraphs = document.Blocks.Cast<Paragraph>().ToList();
        Assert.Equal(["a", "b", "c", "d\fe\tf\U0001F600", ""], paragraphs.Select(p => p.Text));
        Assert.Collection(paragraphs[3].Inlines,
            i => Assert.Equal("d", Assert.IsType<TextRun>(i).Text),
            i => Assert.Equal(BreakKind.Page, Assert.IsType<Break>(i).Kind),
            i => Assert.Equal("e", Assert.IsType<TextRun>(i).Text),
            i => Assert.IsType<Tab>(i),
            i => Assert.Equal("f\U0001F600", Assert.IsType<TextRun>(i).Text));
        Assert.Equal("\r\n", Encoding.UTF8.GetString(Save(document, DocumentFileFormat.Text))[1..3]);
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
    }

    [Fact]
    public void Txt_Read_AboveCap_Throws()
    {
        Assert.Throws<InvalidDataException>(() => TextDocument.Load(new HtmlSecurityTests.EndlessStream(), DocumentFileFormat.Text));
        Assert.Throws<InvalidDataException>(() => TextDocument.Load(new HtmlSecurityTests.EndlessStream(seekable: true), DocumentFileFormat.Text));
    }

    [Fact]
    public void Txt_Write_ListLabelsTablesCrLf()
    {
        var document = HtmlReader.Read(
            "<ol><li>one</li><li>two<ul><li>sub</li></ul></li></ol>" +
            "<table><tr><td>a</td><td><p>b1</p><p>b2</p></td></tr><tr><td colspan=\"2\">c</td></tr></table>" +
            "<p>x<br>y\u00E9<span style=\"display:none\"></span></p><div style=\"break-after:page\"></div><p>z</p>");

        var bytes = Save(document, DocumentFileFormat.Text);

        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Equal("1.\tone\r\n2.\ttwo\r\no\tsub\r\na\tb1 b2\r\nc\t\r\nx\r\ny\u00E9\r\n\fz", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Md_Read_HeadingsEmphasisListsTableLinkCode()
    {
        const string markdown = "# Title\n\n" +
                                "Some **bold**, *italic* and ~~gone~~ text with [a link](https://example.com/) and `code`.\n\n" +
                                "- one\n  - nested\n- two\n\n" +
                                "1. first\n2. second\n\n" +
                                "| H1 | H2 |\n|----|----|\n| a | **b** |\n\n" +
                                "```cs\nvar x = 1;\nreturn x;\n```\n\n" +
                                "## Sub\n";

        var document = Load(Encoding.UTF8.GetBytes(markdown), DocumentFileFormat.Markdown);

        var blocks = document.Blocks;
        var title = Assert.IsType<Paragraph>(blocks[0]);
        Assert.Equal(("Title", "Heading1"), (title.Text, title.Format.StyleId));
        var body = Assert.IsType<Paragraph>(blocks[1]);
        Assert.Equal("Some bold, italic and gone text with a link and code.", body.Text);
        var runs = body.Inlines.OfType<TextRun>().DistinctBy(r => r.Text).ToDictionary(r => r.Text);
        Assert.True(runs["bold"].Format.Bold);
        Assert.True(runs["italic"].Format.Italic);
        Assert.True(runs["gone"].Format.Strike);
        Assert.Equal("https://example.com/", runs["a link"].Link?.SafeTarget);
        Assert.Equal("Consolas", runs["code"].Format.Font);
        var items = blocks.Skip(2).Take(5).Cast<Paragraph>().ToList();
        Assert.Equal(["one", "nested", "two", "first", "second"], items.Select(p => p.Text));
        Assert.Equal([0, 1, 0, 0, 0], items.Select(p => p.Format.Numbering!.Value.Level));
        Assert.Equal(["1.", "2."], items.Skip(3).Select(document.ListLabel));
        var table = Assert.IsType<Table>(blocks[7]);
        Assert.Equal(["H1", "H2", "a", "b"], table.Rows.SelectMany(r => r.Cells).Select(c => ((Paragraph)c.Blocks[0]).Text));
        Assert.True(((Paragraph)table.Rows[1].Cells[1].Blocks[0]).Inlines[0].Format.Bold);
        var code = Assert.IsType<Paragraph>(blocks[8]);
        Assert.Equal("var x = 1;\vreturn x;", code.Text);
        Assert.All(code.Inlines.OfType<TextRun>(), r => Assert.Equal("Consolas", r.Format.Font));
        Assert.Equal(("Sub", "Heading2"), (((Paragraph)blocks[9]).Text, ((Paragraph)blocks[9]).Format.StyleId));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
    }

    [Fact]
    public void Md_RawScript_NotKept()
    {
        const string markdown = "<script>alert(1)</script>\n\n<p onclick=\"alert(2)\">t</p>\n\n<img src=\"https://example.com/x.png\">\n\n" +
                                "![remote](https://example.com/y.png) [x](javascript:alert(3)) <iframe src=\"https://example.com\"></iframe>\n";

        var document = Load(Encoding.UTF8.GetBytes(markdown), DocumentFileFormat.Markdown);

        var paragraphs = document.AllParagraphs().ToList();
        Assert.DoesNotContain(paragraphs, p => p.Text.Contains("alert", StringComparison.Ordinal));
        Assert.DoesNotContain(paragraphs.SelectMany(p => p.Inlines), i => i is InlineImage || i.Link != null);
        Assert.Contains(paragraphs, p => p.Text == "t");
    }

    [Fact]
    public void Md_Read_PathologicalInput_ReturnsPromptly()
    {
        var inputs = new[]
        {
            new string('>', 100_000) + " x",
            string.Concat(Enumerable.Repeat("- ", 50_000)) + "x",
            string.Concat(Enumerable.Range(0, 1_000).Select(i => new string(' ', i * 2) + "- x\n")),
            new string('[', 200_000) + "x" + new string(']', 200_000),
            string.Concat(Enumerable.Repeat("*a_", 100_000)) + string.Concat(Enumerable.Repeat("**", 100_000)),
            string.Concat(Enumerable.Repeat("<div>", 100_000)) + "x",
            string.Concat(Enumerable.Repeat("*a _b **c ~~d [e ", 50_000)),
            "|" + string.Concat(Enumerable.Repeat(" a |", 500)) + "\n|" + string.Concat(Enumerable.Repeat("---|", 500)) + "\n" +
            string.Concat(Enumerable.Repeat("|" + string.Concat(Enumerable.Repeat(" b |", 500)) + "\n", 2_000))
        };
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var documents = inputs.Select(input => Load(Encoding.UTF8.GetBytes(input), DocumentFileFormat.Markdown)).ToList();

        Assert.All(documents, d => Assert.NotEmpty(d.Blocks));
        Assert.Equal(new string('>', 100_000) + " x", ((Paragraph)documents[0].Blocks[0]).Text);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(30), watch.Elapsed.ToString());
    }

    [Fact]
    public void Md_Read_AboveCap_Throws()
    {
        Assert.Throws<InvalidDataException>(() => TextDocument.Load(new HtmlSecurityTests.EndlessStream(), DocumentFileFormat.Markdown));
    }

    private static TextDocument Load(byte[] bytes, DocumentFileFormat format = DocumentFileFormat.Text) => TextDocument.Load(new MemoryStream(bytes), format);

    private static byte[] Save(TextDocument document, DocumentFileFormat format)
    {
        using var stream = new MemoryStream();
        document.Save(stream, format);
        return stream.ToArray();
    }

    private static Paragraph Single(TextDocument document) => Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
}
