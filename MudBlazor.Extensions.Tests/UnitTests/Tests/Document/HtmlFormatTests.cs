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
    public void HtmlRead_ManyImages_DedupedByContent()
    {
        var html = new StringBuilder("<p>");
        for (var i = 0; i < 20000; i++) html.Append("<img src=\"data:image/gif;base64,").Append(Convert.ToBase64String(BitConverter.GetBytes(i % 15000))).Append("\">");

        var document = HtmlReader.Read(html.Append("</p>").ToString());

        Assert.Equal(20000, ((Paragraph)document.Blocks[0]).Inlines.Count);
        Assert.Equal(15000, document.Media.Count);
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

    [Fact]
    public void HtmlWrite_TextEncoded()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(Caret(document, id, 0), "<b>&\"x\" <script>"));
        document.Execute(new SetDocumentPropertiesCommand(document.Properties with { Title = "</title><script>alert(1)</script>" }));

        var html = HtmlWriter.Write(document);

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("<meta charset=\"utf-8\">", html);
        Assert.Contains("<p>&lt;b&gt;&amp;&quot;x&quot; &lt;script&gt;</p>", html);
        Assert.Contains("<title>&lt;/title&gt;&lt;script&gt;alert(1)&lt;/script&gt;</title>", html);
        Assert.DoesNotContain("<b>", html);
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void HtmlWrite_ImagesAsDataUri()
    {
        var png = ObjectCommandTests.Png(20, 10);
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        document.Execute(new InsertImageCommand(Caret(document, id, 0), png, "image/png"));
        document.Execute(new SetImageAltTextCommand(id, 0, "A&B \"q\""));
        document.Execute(new InsertImageCommand(Caret(document, id, 1), [1, 2, 3], "image/gif"));
        document.Execute(new ResizeImageCommand(id, 1, 9525 * 30, 9525 * 15));

        var html = HtmlWriter.Write(document);

        Assert.Contains($"<img src=\"data:image/png;base64,{Convert.ToBase64String(png)}\" alt=\"A&amp;B &quot;q&quot;\" width=\"20\" height=\"10\">", html);
        Assert.Contains("<img src=\"data:image/gif;base64,AQID\" alt=\"\" width=\"30\" height=\"15\">", html);
    }

    [Fact]
    public void HtmlWrite_ListsTablesLinksAndPageBreaks()
    {
        var document = HtmlReader.Read(
            "<ol><li>a<ul><li>b</li></ul></li><li>c</li></ol><p>x<a href=\"javascript:alert(1)\">bad</a><a href=\"mailto:a@b.c\">mail</a></p>" +
            "<table><tr><td rowspan=2>r</td><td>s</td></tr><tr><td>t</td></tr></table>");
        var id = document.Blocks[3].Id;
        document.Execute(new InsertBreakCommand(document.PositionOf(id, 1), BreakKind.Page));

        var html = HtmlWriter.Write(document);

        Assert.Contains("<ol style=\"list-style-type:decimal\"><li>a<ul><li>b</li></ul></li><li>c</li></ol>", html);
        Assert.Contains("<p>x</p><div style=\"break-after:page\"></div><p>bad<a href=\"mailto:a@b.c\">", html);
        Assert.DoesNotContain("javascript", html);
        Assert.Contains("<td rowspan=\"2\">", html);
        Assert.Equal(3, html.Split("<td").Length - 1);
    }

    [Fact]
    public void HtmlWriteThenRead_KeepsFeatures()
    {
        var png = ObjectCommandTests.Png(40, 20);
        var html =
            "<title>Features</title><h1>Head</h1><h2 style=\"text-align:right\">Second</h2>" +
            "<p style=\"text-align:justify\"><b>b</b><i>i</i><u>u</u><s>s</s><span style=\"color:#336699;font-size:14pt;font-family:Georgia\">styled</span>" +
            "<span style=\"background:yellow\">hl</span><span style=\"background:#123456\">sh</span><sup>sup</sup><sub>sub</sub>line<br>two</p>" +
            "<ul><li>one<ol><li>nested</li><li>more</li></ol></li><li>back</li></ul>" +
            "<table><tr><td colspan=2 style=\"background:#ff0000\">wide</td><td rowspan=2>tall</td></tr><tr><td bgcolor=\"#00ff00\">c</td><td><table><tr><td>inner</td></tr></table></td></tr></table>" +
            $"<p><a href=\"https://example.com/?a=1&amp;b=2\">link</a> <a href=\"#Top\">anchor</a> <img src=\"data:image/png;base64,{Convert.ToBase64String(png)}\" alt=\"Alt\" width=\"30\" height=\"15\"></p>" +
            "<pre>code  with\tspaces\nnext</pre><p></p><p><br></p>";
        var original = HtmlReader.Read(html);

        var written = HtmlWriter.Write(original);
        var reread = HtmlReader.Read(written);

        Assert.Equal(Signature(original), Signature(reread));
        Assert.Contains("p ''", Signature(original));
        Assert.Contains("img 285750x142875 Alt", Signature(original));
        Assert.Contains("tc 2 None #FF0000", Signature(original));
        Assert.Contains("tc 1 None #00FF00", Signature(original));
        Assert.Contains("<td colspan=\"2\" style=\"background-color:#FF0000\">", written);
    }

    [Fact]
    public void Save_HtmlThenLoad_RoundTrips()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        var text = "Title\nBody " + (char)0xE4 + (char)0x20AC + char.ConvertFromUtf32(0x1F600) + " text";
        document.Execute(new ReplaceTextCommand(Caret(document, id, 0), text));
        document.Execute(new ApplyParagraphStyleCommand([document.Blocks[0].Id], "Heading1"));
        var body = document.Blocks[1].Id;
        document.Execute(new SetRunFormatCommand(new DocumentRange(document.PositionOf(body, 0), document.PositionOf(body, 4)), f => f with { Bold = true }, "Bold"));
        document.Execute(new ApplyListCommand([body], ListTemplate.Decimal));
        document.Execute(new InsertTableCommand(document.PositionOf(body, ((Paragraph)document.Blocks[1]).Length), 2, 2));
        document.Execute(new SetDocumentPropertiesCommand(document.Properties with { Title = "Saved" }));
        using var stream = new MemoryStream();

        document.Save(stream, DocumentFileFormat.Html);
        var bytes = stream.ToArray();
        var loaded = TextDocument.Load(new MemoryStream(bytes), DocumentFileFormat.Html);

        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Equal(Signature(document), Signature(loaded));
        Assert.Equal("Saved", loaded.Properties.Title);
        Assert.Throws<ArgumentNullException>(() => document.Save(null!, DocumentFileFormat.Html));
        Assert.Throws<ArgumentException>(() => document.Save(new MemoryStream([], false), DocumentFileFormat.Html));
    }

    private static DocumentRange Caret(TextDocument document, string paragraphId, int offset)
    {
        var position = document.PositionOf(paragraphId, offset);
        return new DocumentRange(position, position);
    }

    private static string Signature(TextDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine("title " + document.Properties.Title);
        Blocks(document, document.Blocks, sb, "");
        return sb.ToString();
    }

    private static void Blocks(TextDocument document, IEnumerable<Block> blocks, StringBuilder sb, string indent)
    {
        foreach (var block in blocks)
            if (block is Paragraph p)
            {
                var format = document.EffectiveParagraphFormat(p);
                var list = format.Numbering is { } n ? $"{document.Numbering.Level(n)!.Format}{n.Level}" : "-";
                sb.AppendLine($"{indent}p '{p.Text}' {format.Alignment} outline={format.OutlineLevel} list={list} label={document.ListLabel(p)}");
                foreach (var inline in p.Inlines)
                {
                    var f = document.EffectiveRunFormat(p, inline) with { StyleId = null };
                    var what = inline switch
                    {
                        TextRun r => $"r '{r.Text}'",
                        InlineImage i => $"img {i.WidthEmu}x{i.HeightEmu} {i.AltText} {Convert.ToBase64String(document.GetMedia(i.MediaPart))}",
                        _ => inline.GetType().Name
                    };
                    sb.AppendLine($"{indent}  {what} {f} link={inline.Link?.SafeTarget}");
                }
            }
            else if (block is Table t)
            {
                sb.AppendLine($"{indent}table cols={t.Grid.Count}");
                foreach (var row in t.Rows)
                {
                    sb.AppendLine($"{indent}  tr after={row.GridAfter}");
                    foreach (var cell in row.Cells)
                    {
                        sb.AppendLine($"{indent}    tc {cell.GridSpan} {cell.VMerge} {cell.Shading}");
                        Blocks(document, cell.Blocks, sb, indent + "      ");
                    }
                }
            }
    }

    private static List<(int, VerticalMerge, string, int?)> Cells(TableRow row) =>
        row.Cells.Select(c => (c.GridSpan, c.VMerge, string.Concat(c.Blocks.OfType<Paragraph>().Select(p => p.Text)), c.Width)).ToList();
}
