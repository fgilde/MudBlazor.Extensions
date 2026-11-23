using Bunit;
using MudBlazor.Extensions.Components;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentFindPanelTests : DocumentEditorFormattingTestBase
{
    private static async Task Open(IRenderedComponent<MudExDocumentEditor> cut, bool replace = false, int caret = 0)
    {
        var id = cut.Instance.CurrentDocument.Blocks[0].Id;
        var sync = new SurfaceSync(Seen, [], new SurfaceSelection(new SurfacePoint(id, caret), new SurfacePoint(id, caret)), false);
        await cut.InvokeAsync(() => Surface(cut).OnCommandAsync(sync, null, "shortcut", replace ? "Ctrl+H" : "Ctrl+F"));
        cut.WaitForAssertion(() => Assert.NotNull(cut.FindComponent<MudExDocumentFindPanel>()));
    }

    private static Task Type(IRenderedComponent<MudExDocumentEditor> cut, string css, string text) => cut.InvokeAsync(() => cut.Find(css).Input(text));

    private static Task Click(IRenderedComponent<MudExDocumentEditor> cut, string css) => cut.InvokeAsync(() => cut.Find(css).Click());

    private static string Status(IRenderedComponent<MudExDocumentEditor> cut) => cut.Find(".mud-ex-doc-find-status").TextContent.Trim();

    private static (string Id, int From, int To) Selected(IRenderedComponent<MudExDocumentEditor> cut)
    {
        var range = cut.Instance.Selection.Normalize(cut.Instance.CurrentDocument);
        return (range.Start.BlockId, range.Start.Offset, range.End.Offset);
    }

    private static string[] Texts(TextDocument document) => document.Blocks.OfType<Paragraph>().Select(p => p.Text).ToArray();

    [Fact]
    public async Task Find_CountsAndSelectsFirstAfterCaret()
    {
        var document = DocumentSurfaceRenderTests.Created("one two one", "three one");
        var cut = Render(document);
        
        await Open(cut, caret: 4);

        await Type(cut, ".mud-ex-doc-find-query", "one");

        cut.WaitForAssertion(() => Assert.Equal("2 of 3", Status(cut)));
        Assert.Equal((document.Blocks[0].Id, 8, 11), Selected(cut));
    }

    [Fact]
    public async Task Next_WrapsAround()
    {
        var document = DocumentSurfaceRenderTests.Created("one two one");
        var cut = Render(document);
        await Open(cut);
        await Type(cut, ".mud-ex-doc-find-query", "one");

        await Click(cut, ".mud-ex-doc-find-next");
        cut.WaitForAssertion(() => Assert.Equal("2 of 2", Status(cut)));
        await Click(cut, ".mud-ex-doc-find-next");

        cut.WaitForAssertion(() => Assert.Equal("1 of 2", Status(cut)));
        Assert.Equal((document.Blocks[0].Id, 0, 3), Selected(cut));
    }

    [Fact]
    public async Task Previous_WrapsBack()
    {
        var document = DocumentSurfaceRenderTests.Created("one two one");
        var cut = Render(document);
        await Open(cut);
        await Type(cut, ".mud-ex-doc-find-query", "one");

        await Click(cut, ".mud-ex-doc-find-previous");

        cut.WaitForAssertion(() => Assert.Equal("2 of 2", Status(cut)));
        Assert.Equal((document.Blocks[0].Id, 8, 11), Selected(cut));
    }

    [Fact]
    public async Task Find_InvalidRegex_ShowsMessage()
    {
        var cut = Render(DocumentSurfaceRenderTests.Created("one"));
        await Open(cut);
        await Click(cut, ".mud-ex-doc-find-regex");

        await Type(cut, ".mud-ex-doc-find-query", "(");

        cut.WaitForAssertion(() => Assert.Equal("Invalid regular expression.", Status(cut)));
    }

    [Fact]
    public async Task Replace_CurrentMatch_RegexGroups()
    {
        var document = DocumentSurfaceRenderTests.Created("12-34");
        var cut = Render(document);
        await Open(cut, true);
        await Click(cut, ".mud-ex-doc-find-regex");
        await Type(cut, ".mud-ex-doc-find-query", @"(\d+)-(\d+)");
        await Type(cut, ".mud-ex-doc-find-with", "$2-$1");
        cut.WaitForAssertion(() => Assert.Equal("1 of 1", Status(cut)));

        await Click(cut, ".mud-ex-doc-find-replace");

        Assert.Equal(["34-12"], Texts(document));
        await Run(cut, "wd-undo");
        Assert.Equal(["12-34"], Texts(document));
    }

    [Fact]
    public async Task Replace_NotOnAMatch_JustSelectsNext()
    {
        var document = DocumentSurfaceRenderTests.Created("one two one");
        var cut = Render(document);
        await Open(cut, true);
        await Type(cut, ".mud-ex-doc-find-query", "two");
        await Type(cut, ".mud-ex-doc-find-with", "x");
        await Select(cut, 0, 0, 0);

        await Click(cut, ".mud-ex-doc-find-replace");

        Assert.Equal(["one two one"], Texts(document));
        Assert.Equal((document.Blocks[0].Id, 4, 7), Selected(cut));
    }

    [Fact]
    public async Task ReplaceAll_OneUndoStep_ReportsCount()
    {
        var document = DocumentSurfaceRenderTests.Created("one two one", "one");
        var cut = Render(document);
        await Open(cut, true);
        await Type(cut, ".mud-ex-doc-find-query", "one");
        await Type(cut, ".mud-ex-doc-find-with", "1");

        await Click(cut, ".mud-ex-doc-find-replace-all");

        Assert.Equal(["1 two 1", "1"], Texts(document));
        cut.WaitForAssertion(() => Assert.Equal("3 replacements made", Status(cut)));
        await Run(cut, "wd-undo");
        Assert.Equal(["one two one", "one"], Texts(document));
    }

    [Fact]
    public async Task ReplaceAll_Timeout_ShowsMessage_NothingChanged()
    {
        var text = new string('a', 40) + "!";
        var document = DocumentSurfaceRenderTests.Created(text);
        var cut = Render(document);
        await Open(cut, true);
        await Click(cut, ".mud-ex-doc-find-regex");
        await Type(cut, ".mud-ex-doc-find-query", "(a+)+$");
        await Type(cut, ".mud-ex-doc-find-with", "x");

        await Click(cut, ".mud-ex-doc-find-replace-all");

        Assert.Equal([text], Texts(document));
        cut.WaitForAssertion(() => Assert.Equal("The search took too long; results are incomplete.", Status(cut)));
    }

    [Fact]
    public async Task Find_ReadOnly_AllowedReplaceHidden()
    {
        var document = DocumentSurfaceRenderTests.Created("one two one");
        var cut = Render(document, true);
        await Open(cut, true);

        await Type(cut, ".mud-ex-doc-find-query", "one");

        cut.WaitForAssertion(() => Assert.Equal("1 of 2", Status(cut)));
        Assert.Empty(cut.FindAll(".mud-ex-doc-find-replace"));
        Assert.Empty(cut.FindAll(".mud-ex-doc-find-with"));
    }

    [Fact]
    public async Task Find_HighlightsAllMatches()
    {
        var cut = Render(DocumentSurfaceRenderTests.Created("one two one"));
        await Open(cut);

        await Type(cut, ".mud-ex-doc-find-query", "one");

        cut.WaitForAssertion(() => _context.JSInterop.VerifyInvoke("highlight"));
        var last = _context.JSInterop.Invocations["highlight"].Last();
        Assert.Equal(2, ((System.Collections.IEnumerable)last.Arguments[0]!).Cast<object>().Count());
    }

    [Fact]
    public void ReplacementFor_StaleRange_IsNull()
    {
        var document = DocumentSurfaceRenderTests.Created("one two");
        var id = document.Blocks[0].Id;
        var range = new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 3));
        var options = new FindOptions();

        Assert.Equal("1", DocumentSearch.ReplacementFor(document, range, "one", "1", options));
        Assert.Null(DocumentSearch.ReplacementFor(document, range, "two", "1", options));
    }
}
