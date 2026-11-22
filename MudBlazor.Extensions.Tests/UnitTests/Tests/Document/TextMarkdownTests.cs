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
    public void Md_Read_TableColumnAlignment()
    {
        var document = Load(Encoding.UTF8.GetBytes("| a | b | c |\n| :-: | --: | --- |\n| x | y | z |\n"), DocumentFileFormat.Markdown);

        var table = Assert.IsType<Table>(document.Blocks[0]);
        Assert.All(table.Rows, r => Assert.Equal(new ParagraphAlignment?[] { ParagraphAlignment.Center, ParagraphAlignment.Right, null },
            r.Cells.Select(c => ((Paragraph)c.Blocks[0]).Format.Alignment)));
    }

    [Fact]
    public void Txt_Read_EmptyAndUtf16Boms()
    {
        Assert.Equal("", Single(Load([])).Text);
        Assert.Equal(Gruesse, Single(Load(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(Gruesse)).ToArray())).Text);
        Assert.Equal(Gruesse, Single(Load(Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes(Gruesse)).ToArray())).Text);
    }

    [Fact]
    public void Txt_And_Html_Read_CharCapCheckedBeforeDecoding()
    {
        var cp1252 = System.Text.CodePagesEncodingProvider.Instance.GetEncoding(1252)!;

        Assert.Equal(1000, TextFormat.Decode(new MemoryStream(Encoding.ASCII.GetBytes(new string('a', 1000))), 1000).Length);
        Assert.Throws<InvalidDataException>(() => TextFormat.Decode(new MemoryStream(Encoding.ASCII.GetBytes(new string('a', 1001))), 1000));
        Assert.Equal(1000, TextFormat.Decode(new MemoryStream(Encoding.UTF8.GetBytes(new string('\u00FC', 1000))), 1000).Length);
        Assert.Throws<InvalidDataException>(() => TextFormat.Decode(new MemoryStream(Encoding.UTF8.GetBytes(new string('\u00FC', 1001))), 1000));
        Assert.Equal(1000, TextFormat.Decode(new MemoryStream(cp1252.GetBytes(new string('\u00FC', 1000))), 1000).Length);
        Assert.Throws<InvalidDataException>(() => TextFormat.Decode(new MemoryStream(cp1252.GetBytes(new string('\u00FC', 1001))), 1000));
        Assert.Equal("a", ((Paragraph)HtmlReader.Read(new MemoryStream(Encoding.ASCII.GetBytes("<p>a</p>" + new string(' ', 992))), 1000).Blocks[0]).Text);
        Assert.Throws<InvalidDataException>(() => HtmlReader.Read(new MemoryStream(Encoding.ASCII.GetBytes("<p>a</p>" + new string(' ', 993))), 1000));
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

    [Fact]
    public void Md_Write_Escapes()
    {
        const string text = "a*b_c\n# x\n- y\n1. z\n+ w\n  2) v\n<b>&amp;</b> [l](u) `c` ~~s~~ |p| ===\\";
        var document = Load(Encoding.UTF8.GetBytes(text));

        var markdown = Encoding.UTF8.GetString(Save(document, DocumentFileFormat.Markdown));

        Assert.Equal("a\\*b\\_c\n\n\\# x\n\n\\- y\n\n1\\. z\n\n\\+ w\n\n2\\) v\n\n\\<b\\>\\&amp;\\</b\\> \\[l\\](u) \\`c\\` \\~\\~s\\~\\~ \\|p\\| ===\\\\\n", markdown);
        var reread = Load(Encoding.UTF8.GetBytes(markdown), DocumentFileFormat.Markdown);
        Assert.Equal(text.Replace("  2)", "2)").Split('\n'), reread.Blocks.Cast<Paragraph>().Select(p => p.Text));
    }

    [Fact]
    public void Md_Write_ListsTablesCodeAndBreaks()
    {
        var document = HtmlReader.Read(
            "<h2>Head <b>bold</b></h2><ul><li>a<ol><li>b<ul><li>c</li></ul></li></ol></li><li>d</li></ul><p>after</p>" +
            "<table><tr><td colspan=\"2\">m</td><td rowspan=\"2\">r</td></tr><tr><td>x</td><td><p>y1</p><p>y2 <i>it</i></p></td></tr></table>" +
            "<pre>code ```\n  *raw*</pre><p>one<br>two <a href=\"https://example.com/a(b) c\">link</a> <a href=\"javascript:x\">bad</a></p>");

        var markdown = Encoding.UTF8.GetString(Save(document, DocumentFileFormat.Markdown));

        Assert.Equal(
            "## Head **bold**\n\n" +
            "- a\n  1. b\n     - c\n- d\n\n" +
            "after\n\n" +
            "| m |  | r |\n| --- | --- | --- |\n| x | y1 y2 *it* |  |\n\n" +
            "````\ncode ```\n  *raw*\n````\n\n" +
            "one\\\ntwo [link](https://example.com/a\\(b\\)%20c) bad\n", markdown);
    }

    [Fact]
    public void Md_RoundTrip_Features()
    {
        var png = Convert.ToBase64String(ObjectCommandTests.Png(20, 10));
        var source = "# Title\n\n" +
                     $"Some **bold**, *italic*, ***both*** and ~~gone~~ with [a link](https://example.com/a_(b)) and ![pic](data:image/png;base64,{png}).\n\n" +
                     "Line one\\\nline two\n\n" +
                     "- one\n  - nested\n    1. deep\n- two\n\n" +
                     "1. first\n2. second\n\n" +
                     "| H1 | H2 | H3 |\n| --- | --- | --- |\n| a | **b** | c |\n\n" +
                     "```\ncode *x*\n  indented\n```\n\n" +
                     "### Sub *it*\n";
        var original = Load(Encoding.UTF8.GetBytes(source), DocumentFileFormat.Markdown);

        var markdown = Save(original, DocumentFileFormat.Markdown);
        var reread = Load(markdown, DocumentFileFormat.Markdown);

        Assert.Equal(Signature(original), Signature(reread));
        Assert.Equal(Encoding.UTF8.GetString(markdown), Encoding.UTF8.GetString(Save(reread, DocumentFileFormat.Markdown)));
        Assert.Contains("[img pic ", Signature(reread));
        Assert.Contains("Heading3", Signature(reread));
        Assert.Contains("table ", Signature(reread));
    }

    private static string Signature(TextDocument document) =>
        string.Join("\n", document.Blocks.Select(b => b switch
        {
            Paragraph p => Signature(document, p),
            Table t => "table " + string.Join(" / ", t.Rows.Select(r => string.Join(" | ", r.Cells.Select(c =>
                $"{c.GridSpan}{c.VMerge}:" + string.Join(" ", c.Blocks.OfType<Paragraph>().Select(p => Signature(document, p))))))),
            _ => b.GetType().Name
        }));

    private static string Signature(TextDocument document, Paragraph paragraph) =>
        $"{paragraph.Format.StyleId}|{document.ListLabel(paragraph)}|{paragraph.Format.Numbering?.Level}|" + string.Concat(paragraph.Inlines.Select(i => i switch
        {
            TextRun r => $"[{r.Text}|{(r.Format.Bold == true ? "b" : "")}{(r.Format.Italic == true ? "i" : "")}{(r.Format.Strike == true ? "s" : "")}|{r.Format.Font}|{r.Link?.SafeTarget}]",
            InlineImage image => $"[img {image.AltText} {Convert.ToBase64String(document.GetMedia(image.MediaPart)!)}]",
            Break b => $"[{b.Kind}]",
            _ => $"[{i.GetType().Name}]"
        }));

    private static TextDocument Load(byte[] bytes, DocumentFileFormat format = DocumentFileFormat.Text) => TextDocument.Load(new MemoryStream(bytes), format);

    private static byte[] Save(TextDocument document, DocumentFileFormat format)
    {
        using var stream = new MemoryStream();
        document.Save(stream, format);
        return stream.ToArray();
    }

    private static Paragraph Single(TextDocument document) => Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
}
