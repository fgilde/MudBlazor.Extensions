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

    private static TextDocument Load(byte[] bytes, DocumentFileFormat format = DocumentFileFormat.Text) => TextDocument.Load(new MemoryStream(bytes), format);

    private static byte[] Save(TextDocument document, DocumentFileFormat format)
    {
        using var stream = new MemoryStream();
        document.Save(stream, format);
        return stream.ToArray();
    }

    private static Paragraph Single(TextDocument document) => Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
}
