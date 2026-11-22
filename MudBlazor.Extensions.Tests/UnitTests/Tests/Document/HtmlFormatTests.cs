using System.Text;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class HtmlFormatTests
{
    [Fact]
    public void HtmlRead_HeadingsListsTableImageLink()
    {
        var png = ObjectCommandTests.Png(200, 100);
        var html =
            "<html><head><title>My &amp; Doc</title><style>p { color: red }</style></head><body>" +
            "<h1>Main</h1><h3 style=\"text-align:center\">Sub</h3>" +
            "<ul><li>one<ul><li>two</li></ul></li></ul><ol><li>first</li><li>second</li></ol>" +
            "<table><tr><td colspan=2>A</td><td rowspan=2>B</td></tr><tr><td>C</td><td><b>D</b></td></tr></table>" +
            $"<p>see <a href=\"https://example.com/\">site</a> <img src=\"data:image/png;base64,{Convert.ToBase64String(png)}\" alt=\"Pic\"></p>" +
            "</body></html>";

        var document = HtmlReader.Read(html);

        Assert.Equal("My & Doc", document.Properties.Title);
        var blocks = document.Blocks;
        Assert.Equal(["Heading1", "Heading3"], blocks.Take(2).Cast<Paragraph>().Select(p => p.Format.StyleId));
        Assert.Equal(["Main", "Sub"], blocks.Take(2).Cast<Paragraph>().Select(p => p.Text));
        Assert.Equal(ParagraphAlignment.Center, ((Paragraph)blocks[1]).Format.Alignment);
        var items = blocks.Skip(2).Take(4).Cast<Paragraph>().ToList();
        Assert.Equal(["one", "two", "first", "second"], items.Select(p => p.Text));
        Assert.Equal([0, 1, 0, 0], items.Select(p => p.Format.Numbering!.Value.Level));
        Assert.Equal(items[0].Format.Numbering!.Value.NumId, items[1].Format.Numbering!.Value.NumId);
        Assert.NotEqual(items[0].Format.Numbering!.Value.NumId, items[2].Format.Numbering!.Value.NumId);
        Assert.Equal(NumberFormat.Bullet, document.Numbering.Level(items[0].Format.Numbering!.Value)!.Format);
        Assert.Equal("o", document.ListLabel(items[1]));
        Assert.Equal(["1.", "2."], items.Skip(2).Select(document.ListLabel));

        var table = Assert.IsType<Table>(blocks[6]);
        Assert.Equal(Tables.GridStyle, table.Format.StyleId);
        Assert.Equal([3008, 3008, 3008], table.Grid);
        Assert.Equal([(2, VerticalMerge.None, "A", 6016), (1, VerticalMerge.Restart, "B", 3008)], Cells(table.Rows[0]));
        Assert.Equal([(1, VerticalMerge.None, "C", 3008), (1, VerticalMerge.None, "D", 3008), (1, VerticalMerge.Continue, "", 3008)], Cells(table.Rows[1]));
        Assert.True(((Paragraph)table.Rows[1].Cells[1].Blocks[0]).Inlines[0].Format.Bold);

        var last = Assert.IsType<Paragraph>(blocks[7]);
        Assert.Equal(8, blocks.Count);
        var link = Assert.IsType<TextRun>(last.Inlines[1]);
        Assert.Equal(("site", "https://example.com/", "Hyperlink"), (link.Text, link.Link?.SafeTarget, link.Format.StyleId));
        Assert.Null(last.Inlines[0].Link);
        var image = Assert.IsType<InlineImage>(last.Inlines[3]);
        Assert.Equal((1905000L, 952500L, "Pic"), (image.WidthEmu, image.HeightEmu, image.AltText));
        Assert.Equal(png, document.GetMedia(image.MediaPart));
        Assert.Equal("image/png", document.MediaContentType(image.MediaPart));
        Assert.NotNull(document.Styles.Find("Heading3"));
        Assert.NotNull(document.Styles.Find("Hyperlink"));
        Assert.NotNull(document.Styles.Find(Tables.GridStyle));

        var output = DocxWriterTests.Save(document);
        DocxWriterTests.AssertConsistent(output);
        var reloaded = TextDocument.Load(new MemoryStream(output));
        Assert.Equal("My & Doc", reloaded.Properties.Title);
        Assert.Equal(["1.", "2."], reloaded.AllParagraphs().Skip(4).Take(2).Select(reloaded.ListLabel));
    }

    [Fact]
    public void HtmlRead_ScriptAndEventAttributesIgnored()
    {
        var document = HtmlReader.Read("<script>alert(1)</script><p onclick=\"x\">t</p><style>p{}</style><iframe src=\"https://example.com\">frame</iframe>");

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
        Assert.Equal("t", paragraph.Text);
    }

    [Fact]
    public void HtmlRead_JavascriptLinkIsPlainText()
    {
        var document = HtmlReader.Read("<p><a href=\"javascript:alert(1)\">click</a> <a href=\"#top\">up</a></p>");

        var inlines = ((Paragraph)document.Blocks[0]).Inlines;
        var click = Assert.IsType<TextRun>(inlines[0]);
        Assert.Equal(("click ", null, null), (click.Text, click.Link, click.Format.StyleId));
        Assert.Equal(("up", "#top"), (((TextRun)inlines[1]).Text, inlines[1].Link?.SafeTarget));
    }

    [Fact]
    public void HtmlRead_RemoteImageDropped()
    {
        var document = HtmlReader.Read("<p>a<img src=\"https://example.com/x.png\" alt=\"remote\">b<img src=\"//example.com/y.png\"></p>");

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("ab", paragraph.Text);
        Assert.DoesNotContain(paragraph.Inlines, i => i is InlineImage);
        Assert.Empty(document.Media);
    }

    [Fact]
    public void HtmlRead_PreformattedRowspanBeyondTableAndEmptyInput()
    {
        var document = HtmlReader.Read("<pre>a\tb\nc</pre><table><tr><td rowspan=5>x</td><td>y</td></tr><tr><td>z</td></tr></table>");

        var pre = (Paragraph)document.Blocks[0];
        Assert.Equal("a\tb\vc", pre.Text);
        Assert.All(pre.Inlines, i => Assert.Equal("Consolas", i.Format.Font));
        var table = (Table)document.Blocks[1];
        Assert.Equal([(1, VerticalMerge.Continue, "", 4513), (1, VerticalMerge.None, "z", 4513)], Cells(table.Rows[1]));
        Assert.IsType<Paragraph>(document.Blocks[2]);
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));

        var empty = HtmlReader.Read("");
        Assert.Equal("", Assert.IsType<Paragraph>(Assert.Single(empty.Blocks)).Text);
    }

    private static List<(int, VerticalMerge, string, int?)> Cells(TableRow row) =>
        row.Cells.Select(c => (c.GridSpan, c.VMerge, string.Concat(c.Blocks.OfType<Paragraph>().Select(p => p.Text)), c.Width)).ToList();
}
