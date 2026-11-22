using System.Diagnostics;
using System.Text;
using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class HtmlSecurityTests
{
    [Theory]
    [InlineData("  JaVa\tScRiPt:alert(1)")]
    [InlineData("&#106;avascript:alert(1)")]
    [InlineData("java&#x0A;script:alert(1)")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///c:/windows/win.ini")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("//evil.example/x")]
    public void HtmlRead_UnsafeLinkTargets_AreText(string href)
    {
        var document = HtmlReader.Read($"<p><a href=\"{href}\">click</a></p>");

        var run = Assert.IsType<TextRun>(Assert.Single(((Paragraph)document.Blocks[0]).Inlines));
        Assert.Equal(("click", null, null), (run.Text, run.Link, run.Format.StyleId));
        Assert.DoesNotContain("href", HtmlWriter.Write(document));
    }

    [Fact]
    public void HtmlRead_HiddenContainers_YieldNoText()
    {
        var document = HtmlReader.Read(
            "<noscript>ns</noscript><template>tpl</template><object data=\"x\">obj</object><!-- comment --><![CDATA[c>data]]>" +
            "<script/>alert(1)</script><style/>p{}</style><iframe/>frame</iframe><script>a</scripty>b</script><svg/><p>visible</p>");

        Assert.Equal(["visible"], document.AllParagraphs().Select(p => p.Text));
    }

    [Theory]
    [InlineData("div")]
    [InlineData("span")]
    [InlineData("ul")]
    [InlineData("table")]
    public void HtmlRead_DeepNesting_NeitherThrowsNorOverflows(string tag)
    {
        const int depth = 100000;
        var open = tag == "table" ? "<table><tr><td>" : tag == "ul" ? "<ul><li>" : $"<{tag}>";
        var close = tag == "table" ? "</td></tr></table>" : tag == "ul" ? "</li></ul>" : $"</{tag}>";
        var html = new StringBuilder();
        for (var i = 0; i < depth; i++) html.Append(open);
        html.Append("deep");
        for (var i = 0; i < depth; i++) html.Append(close);

        var document = HtmlReader.Read(html.ToString());

        Assert.Contains(document.AllParagraphs(), p => p.Text == "deep");
        Assert.Contains("deep", HtmlWriter.Write(document));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
    }

    [Fact]
    public void HtmlRead_BadImagesAndHostileStyles_ReturnPromptly()
    {
        var styles = new StringBuilder("<p style=\"");
        for (var i = 0; i < 200_000; i++) styles.Append("font-family:'((((;color:rgb(((((;margin-left:1e400pt;text-indent:--5px;");
        styles.Append("\">styled</p>");
        var oversized = new StringBuilder("<p>x<img src=\"data:image/png;base64,iVBORw0KGgo").Append('A', 44_800_000).Append("\">y</p>");
        var watch = Stopwatch.StartNew();

        var images = HtmlReader.Read("<p>a<img src=\"data:image/png;base64,!!!not base64!!!\"><img src=\"data:image/png;base64,R0lGODlhAQABAAAAACw=\">" +
                                     "<img src=\"data:image/png;base64,AAAAAAAA\">b</p>");
        var styled = HtmlReader.Read(styles.ToString());
        var big = HtmlPasteReader.ReadBlocks(oversized.ToString());

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20), watch.Elapsed.ToString());
        var paragraph = (Paragraph)images.Blocks[0];
        Assert.Equal("a" + (char)0xFFFC + "b", paragraph.Text);
        Assert.Equal("image/gif", images.MediaContentType(paragraph.Inlines.OfType<InlineImage>().Single().MediaPart));
        Assert.Equal("styled", ((Paragraph)styled.Blocks[0]).Text);
        Assert.Equal("xy", string.Concat(((PastedParagraph)Assert.Single(big)).Runs.Select(r => r.Text)));
    }

    [Fact]
    public void HtmlRead_AboveImportCap_Throws()
    {
        Assert.Throws<InvalidDataException>(() => HtmlReader.Read(new string('a', HtmlReader.MaxLength + 1)));
        Assert.Throws<InvalidDataException>(() => TextDocument.Load(new EndlessStream(), DocumentFileFormat.Html));
    }

    private sealed class EndlessStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            buffer.AsSpan(offset, count).Fill((byte)'a');
            return count;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
