using MainSample.WebAssembly.Examples.DocumentEditor;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Samples;

using MudBlazor.Extensions.Office.Document;

public class DocumentSamplesTests
{
    [Fact]
    public void Showcase_HasTitleHeadingsFormattingListsAndATable()
    {
        var document = DocumentSamples.Showcase();
        var paragraphs = document.Blocks.OfType<Paragraph>().ToList();

        Assert.Equal("Title", paragraphs[0].Format.StyleId);
        Assert.Equal(3, paragraphs.Count(p => p.Format.StyleId == "Heading1"));
        Assert.Contains(paragraphs, p => p.Inlines.Any(i => i.Format.Bold == true));
        Assert.Equal(6, paragraphs.Count(p => document.ListLabel(p) != null));
        Assert.Equal("Q2", ((Paragraph)Assert.Single(document.Blocks.OfType<Table>()).Rows[2].Cells[0].Blocks[0]).Text);
        Assert.False(document.Undo.CanUndo);
    }

    [Fact]
    public void Large_HasTheRequestedParagraphs()
    {
        var document = DocumentSamples.Large(50);

        Assert.True(document.Blocks.Count >= 50);
        Assert.StartsWith("Paragraph 50:", ((Paragraph)document.Blocks[49]).Text);
    }
}
