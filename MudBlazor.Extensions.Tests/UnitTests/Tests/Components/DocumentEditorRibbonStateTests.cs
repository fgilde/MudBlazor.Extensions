using Bunit;
using MudBlazor.Extensions.Components;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentEditorRibbonStateTests : DocumentEditorFormattingTestBase
{
    private static Task Press(IRenderedComponent<MudExDocumentEditor> cut, string key) => cut.InvokeAsync(() => Surface(cut).ShortcutPressed!(key));

    private static RunFormat Own(TextDocument document, int paragraph = 0, int inline = 0) => P(document, paragraph).Inlines[inline].Format;

    [Fact]
    public async Task ClearAtTheCaret_InBoldText_TurnsTheBoldToggleOff_OneClickTurnsItOn()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        Bold(document, 0, 0, 5);
        var cut = Render(document);
        await Select(cut, 0, 3, 3);
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-bold")));

        await Press(cut, "Ctrl+Space");
        cut.WaitForAssertion(() => Assert.False(Checked(cut, "wd-bold")));
        await Run(cut, "wd-bold");

        Assert.True(Surface(cut).CaretFormat?.Bold);
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-bold")));
    }

    [Fact]
    public async Task CtrlB_AtACaretInBoldText_TogglesTheCaretOff()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        var id = document.Blocks[0].Id;
        Bold(document, 0, 0, 5);
        var cut = Render(document);
        await Select(cut, 0, 5, 5);

        await Press(cut, "Ctrl+B");

        Assert.False(Surface(cut).CaretFormat?.Bold);
        cut.WaitForAssertion(() => Assert.False(Checked(cut, "wd-bold")));
        var token = ((SurfaceResult)_context.JSInterop.Invocations["resume"].Last().Arguments[0]!).CaretFormat!.Token;
        await cut.InvokeAsync(() => Surface(cut).OnSyncAsync(new SurfaceSync(Seen, [new SurfaceTyped(id, "HelloX world", 6, token)], new SurfaceSelection(new SurfacePoint(id, 6), new SurfacePoint(id, 6)), false), null));
        Assert.True(Run(document, 0, 4).Bold);
        Assert.NotEqual(true, Run(document, 0, 5).Bold);
    }

    [Fact]
    public async Task SetColor_AcceptsOnlyTheFormsOfTheModel()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 0, 5);

        foreach (var bad in new[] { "red", "#12AB3", "#12AB3G", "theme:accent1;color:red", "url(x)", "", null })
            await cut.InvokeAsync(() => cut.Instance.SetColorAsync(bad!));
        Assert.Null(Own(document).Color);
        await cut.InvokeAsync(() => cut.Instance.SetColorAsync("#12ab34"));
        Assert.Equal("#12AB34", Own(document).Color);
        await cut.InvokeAsync(() => cut.Instance.SetColorAsync("theme:accent1 shade=BF"));
        Assert.Equal("theme:accent1 shade=BF", Own(document).Color);
        await cut.InvokeAsync(() => cut.Instance.SetColorAsync("auto"));
        Assert.Equal("auto", Own(document).Color);
    }

    [Fact]
    public async Task StaleSelection_GrowAndOtherCommands_AreSilentNoOps()
    {
        var document = DocumentSurfaceRenderTests.Created("one", "two");
        var second = document.Blocks[1].Id;
        var cut = Render(document);
        await Select(cut, 1, 0, 3);
        await cut.InvokeAsync(() => document.Execute(new MergeParagraphsCommand(document.Blocks[0].Id)));
        Assert.Null(document.FindBlock(second));
        var version = document.Version;

        await cut.InvokeAsync(() => cut.Instance.GrowFontAsync(1));
        await cut.InvokeAsync(() => cut.Instance.GrowFontAsync(-1));
        await cut.InvokeAsync(() => cut.Instance.ToggleRunAsync("bold"));
        await cut.InvokeAsync(() => cut.Instance.IndentAsync(1));
        await cut.InvokeAsync(() => cut.Instance.ClearFormattingAsync(true));
        await cut.InvokeAsync(() => Task.Delay(50));

        Assert.Equal(version, document.Version);
        Assert.Empty(_dialogs.FindAll(".mud-dialog"));
    }

    [Fact]
    public async Task AlignLeft_IsCheckedForTheDefaultAlignment()
    {
        var document = DocumentSurfaceRenderTests.Created("one");
        var cut = Render(document);
        await Select(cut, 0, 1, 1);

        Assert.Null(document.EffectiveParagraphFormat(P(document)).Alignment);
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-align-left")));
        Assert.False(Checked(cut, "wd-align-center"));
    }

    [Theory]
    [InlineData("hidden")]
    [InlineData("collapsed")]
    [InlineData("readonly")]
    public async Task RibbonState_IsNotComputed_WhenTheRibbonIsHiddenCollapsedOrReadOnly(string mode)
    {
        var document = DocumentSurfaceRenderTests.Created("one", "two");
        var cut = Render(p => p.Add(x => x.TextDocument, document).Add(x => x.ShowRibbon, mode != "hidden").Add(x => x.RibbonCollapsed, mode == "collapsed")
            .Add(x => x.ReadOnly, mode == "readonly"));
        var visits = document.ParagraphVisits;

        await Select(cut, 0, 1, 1);
        await Select(cut, 1, 0, 2);
        await cut.InvokeAsync(() => Task.Delay(20));

        Assert.Equal(visits, document.ParagraphVisits);
        cut.Render(p => p.Add(x => x.ShowRibbon, true).Add(x => x.RibbonCollapsed, false).Add(x => x.ReadOnly, false));
        Assert.True(document.ParagraphVisits > visits);
    }

    [Fact]
    public async Task RibbonState_IgnoresFormatsTheRibbonDoesNotShow()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        var id = document.Blocks[0].Id;
        document.Execute(new SetRunFormatCommand(new DocumentRange(document.PositionOf(id, 6), document.PositionOf(id, 11)), f => f with { Caps = true }, "Caps"));
        var cut = RenderEditor<CountingEditor>(p => p.Add(x => x.TextDocument, document));
        await Select(cut, 0, 2, 2);
        await cut.InvokeAsync(() => Task.Delay(50));
        var renders = cut.Instance.Renders;

        await Select(cut, 0, 9, 9);
        await cut.InvokeAsync(() => Task.Delay(50));

        Assert.Equal(renders, cut.Instance.Renders);
    }

    [Fact]
    public async Task ClearAll_ClearsTheMarkFormatOfEmptyParagraphs()
    {
        var document = DocumentSurfaceRenderTests.Created("one", "", "three");
        var empty = document.Blocks[1].Id;
        document.Execute(new SetMarkFormatCommand([empty], f => f with { Bold = true }, "Bold"));
        var cut = Render(document);
        await Select(cut, document.Blocks[0].Id, 0, document.Blocks[2].Id, 5);

        await Run(cut, "wd-clear-formatting");

        Assert.NotEqual(true, P(document, 1).MarkFormat?.Bold);
        await Run(cut, "wd-undo");
        Assert.True(P(document, 1).MarkFormat?.Bold);
    }

    [Fact]
    public async Task Outdent_AtZero_ViaTheRibbon_ChangesNothing()
    {
        var document = DocumentSurfaceRenderTests.Created("one");
        var cut = Render(document);
        await Select(cut, 0, 1, 1);
        var (version, step) = (document.Version, document.Undo.UndoDescription);

        await Run(cut, "wd-outdent");

        Assert.Null(P(document).Format.IndentLeft);
        Assert.Equal((version, step), (document.Version, document.Undo.UndoDescription));
    }

    [Fact]
    public async Task Bullets_ThenNumbering_SwitchesTheListKind()
    {
        var document = DocumentSurfaceRenderTests.Created("a", "b");
        var cut = Render(document);
        await Select(cut, document.Blocks[0].Id, 0, document.Blocks[1].Id, 1);

        await Run(cut, "wd-bullets");
        await Run(cut, "wd-numbering");

        Assert.Equal(["1.", "2."], document.Blocks.Select(b => document.ListLabel((Paragraph)b)));
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-numbering")));
        Assert.False(Checked(cut, "wd-bullets"));
    }

    [Fact]
    public async Task MixedListSelection_IsNotChecked_AndBulletsAll()
    {
        var document = DocumentSurfaceRenderTests.Created("a", "b");
        document.Execute(new ApplyListCommand([document.Blocks[0].Id], ListTemplate.Bullet));
        var cut = Render(document);
        await Select(cut, document.Blocks[0].Id, 0, document.Blocks[1].Id, 1);
        Assert.False(Checked(cut, "wd-bullets"));

        await Run(cut, "wd-bullets");

        Assert.All(document.Blocks, b => Assert.Equal(Bullet, document.ListLabel((Paragraph)b)));
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-bullets")));
    }

    [Fact]
    public async Task ReadOnly_CollapsedCaret_GetsNoCaretFormat()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document, readOnly: true);
        await Select(cut, 0, 2, 2);
        var version = document.Version;

        await Press(cut, "Ctrl+B");
        await cut.InvokeAsync(() => cut.Instance.ToggleRunAsync("italic"));
        await cut.InvokeAsync(() => cut.Instance.ClearFormattingAsync(false));
        await Run(cut, "wd-bold");

        Assert.Null(Surface(cut).CaretFormat);
        Assert.Equal(version, document.Version);
        Assert.Empty(_context.JSInterop.Invocations["resume"]);
    }

    private sealed class CountingEditor : MudExDocumentEditor
    {
        public int Renders { get; private set; }

        protected override void OnAfterRender(bool firstRender)
        {
            Renders++;
            base.OnAfterRender(firstRender);
        }
    }
}
