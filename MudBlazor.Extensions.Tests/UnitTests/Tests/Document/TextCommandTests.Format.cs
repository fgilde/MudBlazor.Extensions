using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public partial class TextCommandTests
{
    [Fact]
    public void SetRunFormat_BoldMiddleWord_ThreeRuns_NeighboursKeepSource()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>one two three</w:t></w:r></w:p>").Load();
        var id = document.Blocks[0].Id;
        var source = ((Paragraph)document.Blocks[0]).Inlines[0].Source;
        Assert.NotNull(source);

        document.Execute(new SetRunFormatCommand(Range(document, id, 4, id, 7), f => f with { Bold = true }, "Bold"));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal(["one ", "two", " three"], paragraph.Inlines.Select(i => ((TextRun)i).Text));
        Assert.Equal([null, true, null], paragraph.Inlines.Select(i => i.Format.Bold));
        Assert.All(paragraph.Inlines, i => Assert.Same(source, i.Source));
        Assert.Equal("Bold", document.Undo.UndoDescription);
    }

    [Fact]
    public void SetRunFormat_BoldOnThenOff_SingleRunAgain()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>one two three</w:t></w:r></w:p>").Load();
        var id = document.Blocks[0].Id;
        var original = ((Paragraph)document.Blocks[0]).Inlines[0];

        document.Execute(new SetRunFormatCommand(Range(document, id, 4, id, 7), f => f with { Bold = true }, "Bold"));
        document.Execute(new SetRunFormatCommand(Range(document, id, 4, id, 7), f => f with { Bold = null }, "Not bold"));

        var run = Assert.Single(((Paragraph)document.Blocks[0]).Inlines);
        Assert.Equal(original, run);
        Assert.Same(original.Source, run.Source);
    }

    [Fact]
    public void SetHyperlink_OnThenOff_NeverMergesRunsWithDifferentSource()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>ab</w:t></w:r><w:r><w:t>cd</w:t></w:r></w:p>").Load();
        var id = document.Blocks[0].Id;

        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 4), "https://example.com"));
        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 4), null));

        Assert.Equal(["ab", "cd"], ((Paragraph)document.Blocks[0]).Inlines.Select(i => ((TextRun)i).Text));
    }

    [Fact]
    public void SetRunFormat_NothingChanges_NoUndoStep()
    {
        var document = TextDocument.FromBlocks([P("abc")]);
        var id = document.Blocks[0].Id;

        document.Execute(new SetRunFormatCommand(Range(document, id, 1, id, 2), f => f, "Same"));

        Assert.False(document.Undo.CanUndo);
    }

    [Fact]
    public void SetParagraphFormat_ChangesOnlyListedParagraphs()
    {
        var document = Standard();
        var cell = document.AllParagraphs().Single(p => p.Text == "inner a");
        var untouched = document.Blocks[1];

        document.Execute(new SetParagraphFormatCommand([document.Blocks[0].Id, cell.Id], f => f with { Alignment = ParagraphAlignment.Center }, "Center"));

        Assert.Equal(ParagraphAlignment.Center, ((Paragraph)document.Blocks[0]).Format.Alignment);
        Assert.Equal(ParagraphAlignment.Center, ((Paragraph)document.FindBlock(cell.Id)).Format.Alignment);
        Assert.Same(untouched, document.Blocks[1]);
        Assert.Throws<ArgumentException>(() => document.Execute(new SetParagraphFormatCommand(["missing"], f => f, "x")));
    }


    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///c:/windows")]
    [InlineData("data:text/html,x")]
    [InlineData("relative/path")]
    public void SetHyperlink_JavascriptScheme_Throws(string target)
    {
        var document = TextDocument.FromBlocks([P("abc")]);
        var id = document.Blocks[0].Id;

        Assert.Throws<ArgumentException>(() => new SetHyperlinkCommand(Range(document, id, 0, id, 3), target));
    }

    [Fact]
    public void SetHyperlink_SetsSharedLink_AnchorAndRemove()
    {
        var document = TextDocument.FromBlocks([P("abcdef")]);
        var id = document.Blocks[0].Id;

        document.Execute(new SetHyperlinkCommand(Range(document, id, 1, id, 4), "https://example.com"));
        var linked = (Paragraph)document.Blocks[0];
        Assert.Equal(["a", "bcd", "ef"], linked.Inlines.Select(i => ((TextRun)i).Text));
        Assert.Equal("https://example.com", linked.Inlines[1].Link.SafeTarget);

        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 2), "#Mark"));
        Assert.Equal("Mark", ((Paragraph)document.Blocks[0]).Inlines[0].Link.Anchor);

        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 6), null));
        Assert.All(((Paragraph)document.Blocks[0]).Inlines, i => Assert.Null(i.Link));
    }
}
