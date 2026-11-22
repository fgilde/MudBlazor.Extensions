using System.Text;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class RtfFormatTests
{
    [Fact]
    public void Rtf_Read_BoldItalicColorAlignmentTable()
    {
        var document = Load(
            @"{\rtf1\ansi\ansicpg1252\deff0{\fonttbl{\f0\fswiss Arial;}{\f1\fmodern Courier New;}}{\colortbl;\red255\green0\blue0;}" +
            @"\pard\qc{\b bold} {\i italic} {\cf1 red} {\f1\fs32 mono}\par" +
            @"\trowd\trgaph108\cellx2000\cellx4000\pard\intbl A\cell {\b B}\cell\row" +
            @"\pard\qr after\par}");

        var first = Assert.IsType<Paragraph>(document.Blocks[0]);
        Assert.Equal("bold italic red mono", first.Text);
        Assert.Equal(ParagraphAlignment.Center, first.Format.Alignment);
        Assert.True(Run(first, "bold").Format.Bold);
        Assert.True(Run(first, "italic").Format.Italic);
        Assert.Null(Run(first, "italic").Format.Bold);
        Assert.Equal("#FF0000", Run(first, "red").Format.Color);
        Assert.Equal(("Courier New", 16.0), (Run(first, "mono").Format.Font, Run(first, "mono").Format.SizePt));
        Assert.Equal("Arial", Run(first, "bold").Format.Font);

        var table = Assert.IsType<Table>(document.Blocks[1]);
        var cells = Assert.Single(table.Rows).Cells;
        Assert.Equal(["A", "B"], cells.Select(c => ((Paragraph)Assert.Single(c.Blocks)).Text));
        Assert.True(((Paragraph)cells[1].Blocks[0]).Inlines[0].Format.Bold);

        var last = Assert.IsType<Paragraph>(document.Blocks[2]);
        Assert.Equal(("after", ParagraphAlignment.Right), (last.Text, last.Format.Alignment));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
    }

    [Fact]
    public void Rtf_Read_UnicodeAndCodepageEscapes()
    {
        Assert.Equal("\u00FC\u00FC \U0001F600 \u20AC", Single(Load(@"{\rtf1\ansi\ansicpg1252\deff0 \u252?\'fc \u-10179?\u-8704? \'80}")).Text);
        Assert.Equal("\u0410", Single(Load(@"{\rtf1\ansi\ansicpg1251\deff0 \'c0}")).Text);
    }

    [Fact]
    public void Rtf_Read_TabsLineBreaksSpacesIndentsAndEmptyCells()
    {
        var document = Load(
            @"{\rtf1\ansi\deff0\pard\li720\fi-360 \tab a\tab\tab b  c\line d\par" +
            @"\trowd\cellx1000\cellx2000\pard\intbl\cell x\cell\row}");

        var paragraph = Assert.IsType<Paragraph>(document.Blocks[0]);
        Assert.Equal("\ta\t\tb  c\vd", paragraph.Text);
        Assert.Equal(3, paragraph.Inlines.Count(i => i is Tab));
        Assert.Equal(BreakKind.Line, Assert.Single(paragraph.Inlines.OfType<Break>()).Kind);
        Assert.Equal((720, -360), (paragraph.Format.IndentLeft, paragraph.Format.IndentFirstLine));
        var table = Assert.IsType<Table>(document.Blocks[1]);
        Assert.Equal(["", "x"], table.Rows[0].Cells.Select(c => ((Paragraph)c.Blocks[0]).Text));
    }

    [Fact]
    public void Rtf_Write_EscapesBracesBackslashUnicode()
    {
        var text = "{a}\\\u00FC \U0001F600\uFB01";
        var bytes = Save(TextFormat.FromText(text));

        Assert.All(bytes, b => Assert.True(b is >= 0x20 and < 0x7F or (byte)'\r' or (byte)'\n', $"byte {b}"));
        var rtf = Encoding.ASCII.GetString(bytes);
        Assert.StartsWith(@"{\rtf1\ansi\ansicpg1252\deff0", rtf);
        Assert.Contains(@"\{a\}\\\u252?", rtf);
        Assert.Contains(@"\u-10179?\u-8704?\u-1279?", rtf);
        Assert.Equal(text, Single(Load(bytes)).Text);
    }

    [Fact]
    public void Rtf_WriteThenRead_KeepsFormatsTableImageLink()
    {
        var png = ObjectCommandTests.Png(40, 20);
        var source = HtmlReader.Read(
            "<h1>Title</h1>" +
            "<p style=\"text-align:center\"><b>bold</b> <i>italic</i> <u>under</u> <s>strike</s> <span style=\"color:#FF0000\">red</span> " +
            "<span style=\"font-family:'Courier New';font-size:16pt\">mono</span> <sup>up</sup> <span style=\"background-color:yellow\">mark</span></p>" +
            "<table><tr><td colspan=\"2\">A</td></tr><tr><td>B</td><td><b>C</b></td></tr></table>" +
            $"<p><a href=\"https://example.com/?a=1&amp;b={{x}}\">link</a> <img src=\"data:image/png;base64,{Convert.ToBase64String(png)}\" width=\"40\" height=\"20\"></p>");

        var bytes = Save(source);
        var document = Load(bytes);

        var heading = Assert.IsType<Paragraph>(document.Blocks[0]);
        Assert.Equal(("Title", "Heading1"), (heading.Text, heading.Format.StyleId));
        var formatted = Assert.IsType<Paragraph>(document.Blocks[1]);
        Assert.Equal("bold italic under strike red mono up mark", formatted.Text);
        Assert.Equal(ParagraphAlignment.Center, formatted.Format.Alignment);
        Assert.True(Run(formatted, "bold").Format.Bold);
        Assert.True(Run(formatted, "italic").Format.Italic);
        Assert.Equal("single", Run(formatted, "under").Format.Underline);
        Assert.True(Run(formatted, "strike").Format.Strike);
        Assert.Equal("#FF0000", Run(formatted, "red").Format.Color);
        Assert.Equal(("Courier New", 16.0), (Run(formatted, "mono").Format.Font, Run(formatted, "mono").Format.SizePt));
        Assert.Equal(VerticalAlign.Superscript, Run(formatted, "up").Format.VerticalAlign);
        Assert.Equal("yellow", Run(formatted, "mark").Format.Highlight);

        var table = Assert.IsType<Table>(document.Blocks[2]);
        Assert.Equal(2, table.Rows.Count);
        var merged = Assert.Single(table.Rows[0].Cells);
        Assert.Equal((2, "A"), (merged.GridSpan, ((Paragraph)merged.Blocks[0]).Text));
        Assert.Equal(["B", "C"], table.Rows[1].Cells.Select(c => ((Paragraph)c.Blocks[0]).Text));
        Assert.True(((Paragraph)table.Rows[1].Cells[1].Blocks[0]).Inlines[0].Format.Bold);

        var last = Assert.IsType<Paragraph>(document.Blocks[3]);
        var link = Run(last, "link");
        Assert.Equal("https://example.com/?a=1&b={x}", link.Link?.SafeTarget);
        var image = Assert.Single(last.Inlines.OfType<InlineImage>());
        Assert.Equal(png, document.GetMedia(image.MediaPart));
        Assert.Equal((40 * 9525L, 20 * 9525L), (image.WidthEmu, image.HeightEmu));
        Assert.Contains(@"{\pict\pngblip\picw40\pich20\picwgoal600\pichgoal300 ", Encoding.ASCII.GetString(bytes));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
    }

    [Fact]
    public void Rtf_Write_ListAsTextLabel()
    {
        var bytes = Save(HtmlReader.Read("<ol><li>one</li><li>two</li></ol><p>after</p>"));

        var rtf = Encoding.ASCII.GetString(bytes);
        Assert.Contains(@"\li720\fi-360", rtf);
        Assert.DoesNotContain(@"\ls", rtf);
        var paragraphs = Load(bytes).Blocks.Cast<Paragraph>().ToList();
        Assert.Equal(["1.\tone", "2.\ttwo", "after"], paragraphs.Select(p => p.Text));
        Assert.All(paragraphs, p => Assert.Null(p.Format.Numbering));
        Assert.Equal((720, -360), (paragraphs[0].Format.IndentLeft, paragraphs[0].Format.IndentFirstLine));
    }

    [Fact]
    public void Rtf_NotRtf_ThrowsInvalidData()
    {
        Assert.Throws<InvalidDataException>(() => Load("hello"));
        Assert.Throws<InvalidDataException>(() => Load(""));
        Assert.Throws<InvalidDataException>(() => Load("{\\rt"));
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 \'zz}"));
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 x} trailing"));
        Assert.Throws<InvalidDataException>(() => TextDocument.Load(new HtmlSecurityTests.EndlessStream("{\\rtf1 "u8.ToArray()), DocumentFileFormat.Rtf));
        Assert.Throws<InvalidDataException>(() => TextDocument.Load(new HtmlSecurityTests.EndlessStream("{\\rtf1 "u8.ToArray(), seekable: true), DocumentFileFormat.Rtf));
    }

    [Fact]
    public void Rtf_Read_HostileNestingAndQuadraticInput_ThrowsInvalidData()
    {
        Assert.Equal("x", Single(Load(@"{\rtf1 " + new string('{', 200) + "x" + new string('}', 200) + "}")).Text);
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 " + new string('{', 300) + "x" + new string('}', 300) + "}"));
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 " + Repeat("{x}", 200_000) + "}"));
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 " + Repeat(@"\trowd\cellx1000\pard\intbl a\cell\row\pard\par ", 30_000) + "}"));
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 " + Repeat(@"\b", 5_000_000) + "}"));
    }

    private static string Repeat(string text, int count) => new StringBuilder(text.Length * count).Insert(0, text, count).ToString();

    private static TextRun Run(Paragraph paragraph, string text) => paragraph.Inlines.OfType<TextRun>().Single(r => r.Text == text);

    private static TextDocument Load(string rtf) => Load(Encoding.Latin1.GetBytes(rtf));

    private static TextDocument Load(byte[] bytes) => TextDocument.Load(new MemoryStream(bytes), DocumentFileFormat.Rtf);

    private static byte[] Save(TextDocument document)
    {
        using var stream = new MemoryStream();
        document.Save(stream, DocumentFileFormat.Rtf);
        return stream.ToArray();
    }

    private static Paragraph Single(TextDocument document) => Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
}
