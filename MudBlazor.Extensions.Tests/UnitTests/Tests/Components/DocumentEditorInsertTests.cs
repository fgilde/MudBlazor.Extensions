using Bunit;
using MudBlazor.Extensions.Components;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentEditorInsertTests : DocumentEditorFormattingTestBase
{
    private static IReadOnlyList<Inline> Inlines(TextDocument document, int index = 0) => P(document, index).Inlines;

    [Fact]
    public async Task PageBreakAndFieldButtons_InsertAtCaret()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 5, 5);

        await Run(cut, "wd-page-break");
        await Run(cut, "wd-insert-page-number");

        Assert.Contains(Inlines(document), i => i is Break { Kind: BreakKind.Page });
        Assert.Contains(Inlines(document), i => i is OpaqueInline { Instruction: "PAGE" });
        Assert.Equal(7, P(document).Length);
    }

    [Fact]
    public async Task Table_IsInsertedWithPickedSize_AndDialogOpens()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 5, 5);

        await cut.InvokeAsync(() => cut.Instance.InsertTableAsync((2, 3)));
        var table = Assert.Single(document.Blocks.OfType<Table>());
        Assert.Equal((2, 3), (table.Rows.Count, table.Grid.Count));

        _ = cut.InvokeAsync(() => cut.Instance.OpenTableDialogAsync());
        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-doctable-dialog")));
    }

    [Fact]
    public async Task Link_AppliesToSelection_ReplacesText_RejectsUnsafeAddress_AndRemoves()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        var cut = Render(document);
        await Select(cut, 0, 0, 5);
        var range = cut.Instance.Selection;

        await cut.InvokeAsync(() => cut.Instance.ApplyLinkAsync(range, "Hello", "Hello", "javascript:alert(1)"));
        Assert.All(Inlines(document), i => Assert.Null(i.Link));

        await cut.InvokeAsync(() => cut.Instance.ApplyLinkAsync(range, "Hello", "Hi", "https://example.test/a"));
        Assert.Equal("Hi world", P(document).Text);
        Assert.Equal("https://example.test/a", Inlines(document)[0].Link.SafeTarget);
        Assert.Null(Inlines(document)[^1].Link);
        document.Undo.Undo();
        Assert.Equal("Hello world", P(document).Text);
        Assert.All(Inlines(document), i => Assert.Null(i.Link));

        await cut.InvokeAsync(() => cut.Instance.ApplyLinkAsync(range, "Hello", "Hello", "mailto:a@example.test"));
        await Select(cut, 0, 2, 2);
        await cut.InvokeAsync(() => cut.Instance.RemoveLinkAsync());
        Assert.All(Inlines(document), i => Assert.Null(i.Link));
    }

    [Fact]
    public void Surface_RunsCarryOnlySafeLinkTargets()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        var id = document.Blocks[0].Id;
        document.Execute(new SetHyperlinkCommand(new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 5)), "https://example.test"));
        document.Execute(new SetHyperlinkCommand(new DocumentRange(document.PositionOf(id, 6), document.PositionOf(id, 11)), "#mark"));

        var runs = new DocumentSurfaceRender(document).Render(document.Blocks[0]).Runs;

        Assert.Equal("https://example.test", runs[0].Link);
        Assert.All(runs.Skip(1), r => Assert.Null(r.Link));
    }

    [Fact]
    public async Task Picture_IsInserted_OversizeAndEmptyAreIgnored()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 5, 5);

        await cut.InvokeAsync(() => cut.Instance.InsertPictureAsync([], "image/png"));
        await cut.InvokeAsync(() => cut.Instance.InsertPictureAsync(new byte[MudExDocumentEditor.MaxPictureBytes + 1], "image/png"));
        Assert.DoesNotContain(Inlines(document), i => i is InlineImage);

        await cut.InvokeAsync(() => cut.Instance.InsertPictureAsync([1, 2, 3], "image/gif"));
        Assert.Contains(Inlines(document), i => i is InlineImage);
    }
}
