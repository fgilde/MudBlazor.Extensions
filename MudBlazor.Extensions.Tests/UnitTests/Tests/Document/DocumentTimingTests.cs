using System.Diagnostics;
using System.Text;
using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

[CollectionDefinition(nameof(DocumentTimingTests), DisableParallelization = true)]
public class DocumentTimingCollection;

[Collection(nameof(DocumentTimingTests))]
public class DocumentTimingTests
{
    [Fact]
    public void Reader_3000Paragraphs_UnderTwoSeconds()
    {
        var body = string.Concat(Enumerable.Range(0, 3000).Select(i =>
            $"<w:p><w:pPr><w:spacing w:after=\"120\"/></w:pPr><w:r><w:t xml:space=\"preserve\">Paragraph {i} with </w:t></w:r><w:r><w:rPr><w:b/></w:rPr><w:t>bold</w:t></w:r>" +
            "<w:proofErr w:type=\"spellStart\"/><w:r><w:t xml:space=\"preserve\"> text.</w:t></w:r></w:p>"));
        var bytes = new DocxBuilder().Body(body).Build();

        var watch = Stopwatch.StartNew();
        var document = TextDocument.Load(new MemoryStream(bytes));
        watch.Stop();

        Assert.Equal(3000, document.Blocks.Count);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"Reading took {watch.Elapsed}.");
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
        Assert.All(((PastedParagraph)big[0]).Runs, r => Assert.Null(r.ImageData));
    }
}
