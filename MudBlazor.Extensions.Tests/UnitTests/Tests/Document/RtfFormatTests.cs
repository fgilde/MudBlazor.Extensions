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

    private static Paragraph Single(TextDocument document) => Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
}
