using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Office;

public class HtmlPasteReaderTests
{
    [Theory]
    [InlineData("<p>One</p>\n<p>Two   words </p><pre>x  y\n z</pre>", "None0:[One|||] / None0:[Two words|||] / None0:[x y z|||]")]
    [InlineData("<ul><li>a<ul><li>b</li></ul></li><li>c</li></ul><ol><li>d</li></ol>", "Bullet0:[a|||] / Bullet1:[b|||] / Bullet0:[c|||] / Number0:[d|||]")]
    [InlineData("<table>\n<tr><td>a</td> <td>b</td></tr>\n<tr><td><p>c</p></td><td>d</td></tr></table>", "None0:[a b|||] / None0:[c|||] / None0:[d|||]")]
    [InlineData("<p>line<br>next<br/></p>", "None0:[line\\nnext\\n|||]")]
    [InlineData("<p><b>bold</b> <span style=\"font-style:italic;color:#ff0000\">red</span> <u>u</u>plain<span style=\"font-size:20pt;font-family:Arial\">big</span><img src=\"data:image/png;base64,iVBORw0KGgo=\"></p>",
        "None0:[bold|b||][ |||][red|i|#FF0000|][ |||][u|u||][plainbig|||]")]
    [InlineData("<p>see <a href=\"https://x.org/a?b=1&amp;c=2\">here</a> and <a href=\"javascript:alert(1)\">there</a></p>",
        "None0:[see |||][here|||https://x.org/a?b=1&c=2][ and |||][there|||javascript:alert(1)]")]
    public void PasteReader_Read_UnchangedOutput(string html, string expected)
    {
        Assert.Equal(expected, Describe(HtmlPasteReader.Read(html)));
    }

    [Fact]
    public void ReadBlocks_TableWithSpansAndNestedList()
    {
        var blocks = HtmlPasteReader.ReadBlocks(
            "<p>before</p><table>\n<tr><td colspan=\"2\">A</td> <td rowspan=\"2\"><b>B</b></td></tr>\n" +
            "<tr><td>C<td><ul><li>x<ol><li>y</li></ol></li></ul></tr></table><p>after</p>");

        Assert.Equal(3, blocks.Count);
        Assert.Equal("None0:[before|||]", Describe([(PastedParagraph)blocks[0]]));
        var table = Assert.IsType<PastedTable>(blocks[1]);
        Assert.Equal([2, 2], table.Rows.Select(r => r.Count));
        Assert.Equal([(2, 1), (1, 2)], table.Rows[0].Select(c => (c.ColSpan, c.RowSpan)));
        Assert.Equal("None0:[A|||]", Describe(table.Rows[0][0].Blocks.Cast<PastedParagraph>()));
        Assert.Equal("None0:[B|b||]", Describe(table.Rows[0][1].Blocks.Cast<PastedParagraph>()));
        Assert.Equal("None0:[C|||]", Describe(table.Rows[1][0].Blocks.Cast<PastedParagraph>()));
        Assert.Equal("Bullet0:[x|||] / Number1:[y|||]", Describe(table.Rows[1][1].Blocks.Cast<PastedParagraph>()));
        Assert.Equal("None0:[after|||]", Describe([(PastedParagraph)blocks[2]]));
    }

    [Fact]
    public void ReadBlocks_WordClipboardList_LevelsAndLabelDropped()
    {
        const string html =
            "<html><head><meta charset=utf-8><title>Doc</title></head><body>" +
            "<p class=MsoListParagraphCxSpFirst style='margin-left:.5in;text-indent:-.25in;mso-list:l0 level1 lfo1'><![if !supportLists]><span style='font-family:Symbol'>" +
            "<span style='mso-list:Ignore'>\u00B7<span style='font:7.0pt \"Times New Roman\"'>&nbsp;&nbsp;&nbsp; </span></span></span><![endif]>First<o:p></o:p></p>\n" +
            "<p class=MsoListParagraphCxSpLast style='margin-left:1.0in;mso-list:l0 level2 lfo2'><!--[if !supportLists]--><span><span style='mso-list:Ignore'>a)" +
            "<span>&nbsp; </span></span></span><!--[endif]-->Second<o:p>&nbsp;</o:p></p>\n" +
            "<p class=MsoNormal><o:p>&nbsp;</o:p></p></body></html>";

        var blocks = HtmlPasteReader.ReadBlocks(html, out var title);

        Assert.Equal("Bullet0:[First|||] / Number1:[Second|||]", Describe(blocks.Cast<PastedParagraph>()));
        Assert.Equal("Doc", title);
        Assert.Equal("None0:[\u00B7\u00A0\u00A0\u00A0 First|||] / None0:[a)\u00A0 Second\u00A0|||] / None0:[\u00A0|||]", Describe(HtmlPasteReader.Read(html)));
    }

    [Fact]
    public void ReadBlocks_PreKeepsWhitespace()
    {
        var blocks = HtmlPasteReader.ReadBlocks("<pre>\nint  x;\r\n\tif (y)  <b>z</b>;\n</pre><p>a   b</p>");

        var pre = Assert.IsType<PastedParagraph>(blocks[0]);
        Assert.True(pre.Preformatted);
        Assert.Equal("None0:[int  x;\\n\tif (y)  |||][z|b||][;|||]", Describe([pre]));
        var plain = Assert.IsType<PastedParagraph>(blocks[1]);
        Assert.False(plain.Preformatted);
        Assert.Equal("None0:[a b|||]", Describe([plain]));
    }

    [Fact]
    public void ReadBlocks_FormatsHeadingsAlignmentAndImages()
    {
        var blocks = HtmlPasteReader.ReadBlocks(
            "<h2 style=\"text-align:center\">Title</h2><div align=right><p><s>gone</s><sup>2</sup><span style=\"font-size:16px;font-family:'Times New Roman', serif;background:#ff0\">big</span></p></div>" +
            "<p>a<img src=\"data:image/png;base64,iVBORw0KGgo=\" alt=\"Logo\" width=\"20px\" height=10><img src=\"https://example.com/x.png\"><img src=\"data:image/svg+xml;base64,PHN2Zy8+\">b</p>");

        var heading = (PastedParagraph)blocks[0];
        Assert.Equal((2, "center"), (heading.Heading, heading.Align));
        var formatted = (PastedParagraph)blocks[1];
        Assert.Equal("right", formatted.Align);
        Assert.True(formatted.Runs[0].Format.Strike);
        Assert.Equal("super", formatted.Runs[1].Format.VerticalAlign);
        Assert.Equal((12.0, "Times New Roman", "#FFFF00"), (formatted.Runs[2].Format.SizePt!.Value, formatted.Runs[2].Format.Font, formatted.Runs[2].Format.Highlight));
        var images = (PastedParagraph)blocks[2];
        Assert.Equal(3, images.Runs.Count);
        var image = images.Runs[1];
        Assert.Equal((8, "image/png", "Logo", 20, 10), (image.ImageData.Length, image.ImageType, image.Alt, image.Width!.Value, image.Height!.Value));
        Assert.Equal("ab", string.Concat(images.Runs.Select(r => r.Text)));
    }

    [Fact]
    public void ReadBlocks_InvalidCharactersAndHostileNestingAreBounded()
    {
        var blocks = HtmlPasteReader.ReadBlocks("<p>a&#0;&#xD800;&#xFFFE;&#xFFFC;\u0001b&#x1F600;</p>");
        Assert.Equal("None0:[a&#xD800;b\U0001F600|||]",Describe(blocks.Cast<PastedParagraph>()));

        var nested = string.Concat(Enumerable.Repeat("<table><tr><td>", 200)) + "deep" + string.Concat(Enumerable.Repeat("</td></tr></table>", 200));
        var depth = 0;
        for (var block = HtmlPasteReader.ReadBlocks(nested)[0]; block is PastedTable table; block = table.Rows[0][0].Blocks[0]) depth++;
        Assert.Equal(HtmlPasteReader.MaxTableDepth, depth);
    }

    [Theory]
    [InlineData("<p>a&#xFFFE;\uFFFCb<a href=\"x\u0001y\">l</a></p>", "None0:[ab|||][l|||xy]")]
    [InlineData("<script/>alert(1)</script><style/>p{}</style><iframe/>f</iframe><noscript/>n</noscript><script>x</scripty>y</script><![CDATA[c>d]]><p>z</p>", "None0:[z|||]")]
    public void PasteReader_Read_NewFilteringPinned(string html, string expected)
    {
        Assert.Equal(expected, Describe(HtmlPasteReader.Read(html)));
    }

    [Fact]
    public void ReadBlocks_ListRuns_SeparateListsAndWordListIds()
    {
        var blocks = HtmlPasteReader.ReadBlocks(
            "<ol><li>a<ul><li>a1</li></ul></li></ol><ol><li>b</li></ol>" +
            "<p style='mso-list:l0 level1 lfo1'><span style='mso-list:Ignore'>1.</span>w1</p><p>between</p>" +
            "<p style='mso-list:l0 level1 lfo1'><span style='mso-list:Ignore'>2.</span>w2</p><p style='mso-list:l1 level1 lfo2'><span style='mso-list:Ignore'>1.</span>v1</p>");

        var runs = blocks.Cast<PastedParagraph>().Select(p => (string.Concat(p.Runs.Select(r => r.Text)), p.ListRun)).ToList();
        Assert.Equal(runs[0].ListRun, runs[1].ListRun);
        Assert.NotEqual(runs[0].ListRun, runs[2].ListRun);
        Assert.NotEqual(runs[2].ListRun, runs[3].ListRun);
        Assert.Equal(("w2", runs[3].ListRun), runs[5]);
        Assert.NotEqual(runs[5].ListRun, runs[6].ListRun);
    }

    [Fact]
    public void ReadBlocks_LegacyTagsTablesCaptionsHeadAndEmptyParagraphs()
    {
        var blocks = HtmlPasteReader.ReadBlocks(
            "<html><head><title>T</title><meta charset=utf-8>" +
            "<p>first</p><font color=red face=Georgia size=5>f</font><center>c</center><mark>m</mark><code>k</code><big>B</big><small>s</small>" +
            "<p></p><ul><li>x</li><li></li><li>y</li></ul>" +
            "<table align=center style=\"background:#ff0000\"><caption>Cap</caption><tr bgcolor=\"#00ff00\"><td>cell</td></tr></table><p></p>");

        var paragraphs = blocks.OfType<PastedParagraph>().ToList();
        Assert.Equal("first", paragraphs[0].Runs[0].Text);
        var font = paragraphs[1].Runs[0].Format;
        Assert.Equal(("#FF0000", "Georgia", 18.0), (font.Color, font.Font, font.SizePt!.Value));
        Assert.Equal(("c", "center"), (paragraphs[2].Runs[0].Text, paragraphs[2].Align));
        var inline = paragraphs[3].Runs.ToDictionary(r => r.Text, r => r.Format);
        Assert.Equal("#FFFF00", inline["m"].Highlight);
        Assert.Equal("Consolas", inline["k"].Font);
        Assert.Equal(13.0, inline["B"].SizePt);
        Assert.Equal(9.0, inline["s"].SizePt);
        Assert.Empty(paragraphs[4].Runs);
        Assert.Equal(["x", "", "y"], paragraphs.Skip(5).Take(3).Select(p => string.Concat(p.Runs.Select(r => r.Text))));
        Assert.All(paragraphs.Skip(5).Take(3), p => Assert.Equal(PastedList.Bullet, p.List));
        Assert.Equal(("Cap", "center"), (paragraphs[8].Runs[0].Text, paragraphs[8].Align));
        var table = Assert.IsType<PastedTable>(blocks[^1]);
        var cell = (PastedParagraph)Assert.Single(Assert.Single(table.Rows)).Blocks.Single();
        Assert.Equal(("cell", null, null), (cell.Runs[0].Text, cell.Align, cell.Runs[0].Format.Highlight));
    }

    [Fact]
    public void ReadBlocks_IndentsPageBreaksWhiteSpaceAndSniffedImages()
    {
        var blocks = HtmlPasteReader.ReadBlocks(
            "<p style=\"margin-left:36pt;text-indent:-18pt\">a</p><p style=\"margin-left:.5in;padding-left:20px\">b</p><p style=\"margin-left:1cm\">c</p>" +
            "<div style=\"break-after:page\"></div><p>d</p><p style=\"page-break-before:always\">e</p>" +
            "<p style=\"white-space:pre-wrap\">  f  g\t</p>" +
            "<p><img src=\"data:image/png;base64,R0lGODlhAQABAAAAACw=\"><img src=\"data:image/png;base64,AAAAAAAA\"></p>");

        var p = blocks.Cast<PastedParagraph>().ToList();
        Assert.Equal([(720, -360), (1020, null), (567, null)], p.Take(3).Select(x => (x.IndentLeft, x.IndentFirstLine)).Select(t => (t.IndentLeft!.Value, t.IndentFirstLine)));
        Assert.Equal([false, false, false, true, true, false], p.Take(6).Select(x => x.PageBreakBefore));
        Assert.Equal("  f  g\t", p[5].Runs[0].Text);
        Assert.True(p[5].KeepSpace);
        Assert.Equal("image/gif", Assert.Single(p[6].Runs).ImageType);
    }

    [Theory]
    [InlineData("<ul><li><p>a</p></li><li><p>b</p></li></ul>", "Bullet0:[a|||] / Bullet0:[b|||]")]
    [InlineData("<ol><li><h2>x</h2></li><li><h2>y</h2></li></ol>", "Number0:[x|||] / Number0:[y|||]")]
    public void ReadBlocks_BlocksInsideListItems_StayItems(string html, string expected)
    {
        Assert.Equal(expected, Describe(HtmlPasteReader.ReadBlocks(html).Cast<PastedParagraph>()));
    }

    [Fact]
    public void ReadBlocks_SecondParagraphOfItem_ContinuesItWithoutLabel()
    {
        var blocks = HtmlPasteReader.ReadBlocks("<ol><li><p>a</p><p>b</p></li><li>c</li></ol>").Cast<PastedParagraph>().ToList();

        Assert.Equal("Number0:[a|||] / Number0:[b|||] / Number0:[c|||]", Describe(blocks));
        Assert.Equal([false, true, false], blocks.Select(b => b.Continuation));
        Assert.Single(blocks.Select(b => b.ListRun).Distinct());
    }

    [Fact]
    public void ReadBlocks_PreLine_CollapsesSpacesKeepsNewlines()
    {
        var paragraph = (PastedParagraph)Assert.Single(HtmlPasteReader.ReadBlocks("<p style=\"white-space:pre-line\">  a   b  \n  c </p>"));

        Assert.Equal("a b\nc", string.Concat(paragraph.Runs.Select(r => r.Text)));
        Assert.False(paragraph.KeepSpace);
    }

    [Fact]
    public void ReadBlocks_WordListAfterHtmlList_StartsNewRun()
    {
        var blocks = HtmlPasteReader.ReadBlocks(
            "<p style=\"mso-list:l0 level1 lfo1\">a</p><ul><li>b</li></ul><p style=\"mso-list:l0 level1 lfo1\">c</p>").Cast<PastedParagraph>().ToList();

        Assert.Equal(3, blocks.Select(b => b.ListRun).Distinct().Count());
    }

    [Fact]
    public void ReadBlocks_IndentStopsAtTableBoundary()
    {
        var blocks = HtmlPasteReader.ReadBlocks(
            "<div style=\"margin-left:36pt\"><table><tr><td style=\"padding-left:10pt\"><p>x</p></td></tr></table><p>y</p></div>");

        var cell = (PastedParagraph)Assert.IsType<PastedTable>(blocks[0]).Rows[0][0].Blocks.Single();
        Assert.Null(cell.IndentLeft);
        Assert.Equal(720, ((PastedParagraph)blocks[1]).IndentLeft);
    }

    [Fact]
    public void ReadBlocks_CellAlignmentKeptOuterIndentAndAlignmentNot()
    {
        var blocks = HtmlPasteReader.ReadBlocks(
            "<div style=\"margin-left:36pt;text-align:justify\"><table><tr><td align=\"right\">r</td>" +
            "<td style=\"text-align:center;padding-left:10pt\"><p>c</p></td><td>n</td></tr></table></div>");

        var cells = Assert.IsType<PastedTable>(blocks[0]).Rows[0].Select(c => (PastedParagraph)c.Blocks.Single()).ToList();
        Assert.Equal(["right", "center", null], cells.Select(c => c.Align));
        Assert.All(cells, c => Assert.Null(c.IndentLeft));
    }

    private static string Describe(IEnumerable<PastedParagraph> paragraphs) =>
        string.Join(" / ", paragraphs.Select(p => $"{p.List}{p.Depth}:" + string.Concat(p.Runs.Select(r =>
            $"[{r.Text.Replace("\n", "\\n")}|{(r.Format.Bold ? "b" : "")}{(r.Format.Italic ? "i" : "")}{(r.Format.Underline ? "u" : "")}|{r.Format.Color}|{r.Format.Link}]"))));
}
