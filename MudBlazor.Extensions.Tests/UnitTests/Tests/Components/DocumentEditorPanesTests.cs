using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Office.Document;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class DocumentEditorPanesTests : DocumentEditorFormattingTestBase
{
    private static Paragraph Heading(string text, int level) => new() { Format = new ParagraphFormat { OutlineLevel = level }, Inlines = [new TextRun(text)] };

    private static Paragraph Body(string text) => new() { Inlines = [new TextRun(text)] };

    [Fact]
    public async Task Ruler_ShowsMarkersOfTheCaretParagraph_AndEveryEditIsOneUndoStep()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 0, 0);
        cut.Render();
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".mud-ex-doc-ruler-marker").Count));
        var ruler = cut.FindComponent<MudExDocumentRuler>().Instance;

        await cut.InvokeAsync(() => ruler.OnEdit("left", 0, 720, false));
        Assert.Equal(720, P(document).Format.IndentLeft);
        await cut.InvokeAsync(() => ruler.OnEdit("first", 0, 1000, false));
        Assert.Equal(280, P(document).Format.IndentFirstLine);
        await cut.InvokeAsync(() => ruler.OnEdit("add", 0, 2000, false));
        Assert.Equal(2000, Assert.Single(P(document).Format.Tabs!).Position);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-doc-ruler-tab")));
        await cut.InvokeAsync(() => ruler.OnEdit("tab", 0, 0, true));
        Assert.Empty(P(document).Format.Tabs ?? []);

        document.Undo.Undo();
        Assert.Equal(2000, Assert.Single(P(document).Format.Tabs!).Position);
        document.Undo.Undo();
        Assert.Empty(P(document).Format.Tabs ?? []);
        Assert.Equal(280, P(document).Format.IndentFirstLine);
    }

    [Fact]
    public async Task NavigationPane_ListsHeadings_HighlightsTheCurrentOne_AndClickSelectsIt()
    {
        var document = TextDocument.FromBlocks([Heading("Intro", 0), Body("a"), Heading("Details", 1), Body("b")]);
        var cut = Render(document);
        await Run(cut, "wd-view-nav");

        cut.WaitForAssertion(() => Assert.Equal(["Intro", "Details"], cut.FindAll(".mud-ex-doc-nav-item").Select(i => i.TextContent.Trim())));
        await Select(cut, 3, 0, 0);
        cut.WaitForAssertion(() => Assert.Equal("Details", cut.Find(".mud-ex-doc-nav-current").TextContent.Trim()));

        await cut.InvokeAsync(() => cut.FindAll(".mud-ex-doc-nav-item")[0].Click());
        Assert.Equal(document.Blocks[0].Id, cut.Instance.Selection.Start.BlockId);
        cut.WaitForAssertion(() => Assert.Equal("Intro", cut.Find(".mud-ex-doc-nav-current").TextContent.Trim()));
    }

    [Fact]
    public async Task CommentsPane_ListsTheComments_AndClickGoesToTheAnchor()
    {
        var document = TextDocument.FromBlocks([Body("one"), Body("two")]);
        document.CommentList.Add(new Comment("1", "Ann", "A", new DateTimeOffset(2026, 1, 2, 3, 4, 0, TimeSpan.Zero), "Check this", document.Blocks[1].Id));
        var cut = Render(document);
        Assert.Empty(cut.FindAll(".mud-ex-doc-comment"));
        await Run(cut, "wd-view-comments");

        cut.WaitForAssertion(() => Assert.Contains("Ann", cut.Find(".mud-ex-doc-comment").TextContent));
        Assert.Contains("Check this", cut.Find(".mud-ex-doc-comment").TextContent);
        await cut.InvokeAsync(() => cut.Find(".mud-ex-doc-comment").Click());
        Assert.Equal(document.Blocks[1].Id, cut.Instance.Selection.Start.BlockId);
    }
}
