using Bunit;
using MudBlazor.Extensions.Components;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentSurfaceHeaderFooterTests : DocumentSurfaceTestBase
{
    private static SurfaceHeaderFooter Sent(IEnumerable<Bunit.JSRuntimeInvocation> invocations) =>
        (SurfaceHeaderFooter)invocations.Last(i => i.Identifier == "setHeaderFooter").Arguments[0]!;

    [Fact]
    public void Payload_CarriesHeaderAndFooterBlocksAndPageFields_AndIsSentAgainOnChange()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var section = document.Sections[0].Id;
        document.Execute(new SetHeaderFooterTextCommand(section, HeaderFooterKind.Header, HeaderFooterType.Default, ["Top"], ParagraphAlignment.Center));
        document.Execute(new SetHeaderFooterTextCommand(section, HeaderFooterKind.Footer, HeaderFooterType.Default, ["Page {PAGE} of {NUMPAGES}"], ParagraphAlignment.Right));

        var cut = Surface(document);
        cut.WaitForAssertion(() => Assert.Contains(_context.JSInterop.Invocations, i => i.Identifier == "setHeaderFooter" && i.Arguments[0] != null));
        var sent = Sent(_context.JSInterop.Invocations);

        Assert.Equal("Top", sent.Header.Default.Single().Text);
        Assert.Contains("text-align:center", sent.Header.Default.Single().Css);
        Assert.Null(sent.Header.First);
        Assert.Equal(["PAGE", "NUMPAGES"], sent.Footer.Default.Single().Runs.Where(r => r.Field != null).Select(r => r.Field));

        var before = _context.JSInterop.Invocations.Count(i => i.Identifier == "setHeaderFooter");
        cut.InvokeAsync(() => document.Execute(new SetHeaderFooterTextCommand(section, HeaderFooterKind.Header, HeaderFooterType.Default, ["Changed"], ParagraphAlignment.Left)));
        cut.WaitForAssertion(() => Assert.True(_context.JSInterop.Invocations.Count(i => i.Identifier == "setHeaderFooter") > before));
        Assert.Equal("Changed", Sent(_context.JSInterop.Invocations).Header.Default.Single().Text);
    }
}

public class DocumentEditorHeaderFooterTests : DocumentEditorFormattingTestBase
{
    private async Task Open(IRenderedComponent<MudExDocumentEditor> cut, HeaderFooterKind kind)
    {
        _ = cut.InvokeAsync(() => cut.Instance.OpenHeaderFooterAsync(kind));
        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-dochf-dialog")));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task HeaderDialog_CreatesTheHeaderInOneUndoStep_AndRemovesIt()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 0, 0);

        await Open(cut, HeaderFooterKind.Header);
        _dialogs.Find(".mud-ex-dochf-text textarea").Input("Title {PAGE}");
        _dialogs.Find(".mud-ex-dochf-ok").Click();
        _dialogs.WaitForAssertion(() => Assert.Empty(_dialogs.FindAll(".mud-ex-dochf-dialog")));

        var part = Assert.Single(document.HeadersFooters);
        var paragraph = Assert.IsType<Paragraph>(Assert.Single(part.Blocks));
        Assert.Equal("Title \uFFFC", paragraph.Text);
        Assert.Contains(paragraph.Inlines, i => i is OpaqueInline { Instruction: "PAGE" });

        document.Undo.Undo();
        Assert.Empty(document.HeadersFooters);
        Assert.Empty(document.Sections[0].Headers);
        document.Undo.Redo();
        Assert.Single(document.HeadersFooters);

        await Open(cut, HeaderFooterKind.Header);
        Assert.Equal("Title {PAGE}", _dialogs.Find(".mud-ex-dochf-text textarea").GetAttribute("value") ?? _dialogs.Find(".mud-ex-dochf-text textarea").TextContent.Trim());
        _dialogs.Find(".mud-ex-dochf-text textarea").Input("Second");
        _dialogs.Find(".mud-ex-dochf-ok").Click();
        _dialogs.WaitForAssertion(() => Assert.Empty(_dialogs.FindAll(".mud-ex-dochf-dialog")));
        Assert.Equal("Second", ((Paragraph)document.HeadersFooters.Single().Blocks.Single()).Text);
        document.Undo.Undo();
        Assert.Equal("Title \uFFFC", ((Paragraph)document.HeadersFooters.Single().Blocks.Single()).Text);
        document.Undo.Redo();

        await Open(cut, HeaderFooterKind.Header);
        _dialogs.Find(".mud-ex-dochf-remove").Click();
        _dialogs.WaitForAssertion(() => Assert.Empty(_dialogs.FindAll(".mud-ex-dochf-dialog")));
        Assert.Empty(document.HeadersFooters);
    }

    [Fact]
    public async Task FormattingMarksToggle_TellsTheSurface_AndInsertTabHasHeaderFooterButtons()
    {
        var cut = Render(DocumentSurfaceRenderTests.Created("Hello"));
        Assert.NotNull(Command(cut, "wd-insert-header"));
        Assert.NotNull(Command(cut, "wd-insert-footer"));

        await Run(cut, "wd-view-marks");

        cut.WaitForAssertion(() => Assert.Contains(_context.JSInterop.Invocations["setMarks"], i => i.Arguments[0] is true));
    }
}
