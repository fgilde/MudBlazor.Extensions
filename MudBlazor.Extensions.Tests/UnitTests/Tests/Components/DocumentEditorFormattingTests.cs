using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentEditorFormattingTests : DocumentEditorTestBase
{
    private const int Seen = 1_000_000;
    private static readonly string Bullet = ((char)0x2022).ToString();

    private static MudExRibbonItemBase Command(IRenderedComponent<MudExDocumentEditor> cut, string id) =>
        cut.FindComponent<MudExRibbon>().Instance.Commands.SingleOrDefault(c => c.Id == id) ?? cut.FindComponents<MudExRibbonButton>().Select(b => b.Instance).Single(b => b.Id == id);

    private static Task Run(IRenderedComponent<MudExDocumentEditor> cut, string id) => cut.InvokeAsync(() => Command(cut, id).ExecuteAsync());

    private static bool Checked(IRenderedComponent<MudExDocumentEditor> cut, string id) => Command(cut, id) switch
    {
        MudExRibbonToggleButton toggle => toggle.Checked,
        MudExRibbonMenuItem item => item.Checked == true,
        _ => throw new InvalidOperationException(id)
    };

    private static MudExDocumentSurface Surface<T>(IRenderedComponent<T> cut) where T : MudExDocumentEditor => cut.FindComponent<MudExDocumentSurface>().Instance;

    private static Task Select<T>(IRenderedComponent<T> cut, string anchorId, int anchor, string focusId, int focus) where T : MudExDocumentEditor =>
        cut.InvokeAsync(() => Surface(cut).OnSyncAsync(new SurfaceSync(Seen, [], new SurfaceSelection(new SurfacePoint(anchorId, anchor), new SurfacePoint(focusId, focus)), false), null));

    private static Task Select<T>(IRenderedComponent<T> cut, int paragraph, int from, int to) where T : MudExDocumentEditor
    {
        var id = cut.Instance.CurrentDocument.Blocks[paragraph].Id;
        return Select(cut, id, from, id, to);
    }

    private static Paragraph P(TextDocument document, int index = 0) => (Paragraph)document.Blocks[index];

    private static RunFormat Run(TextDocument document, int paragraph, int offset) =>
        document.EffectiveRunFormat(P(document, paragraph), InlineEdit.At(P(document, paragraph), offset));

    private static void Bold(TextDocument document, int paragraph, int from, int to)
    {
        var id = document.Blocks[paragraph].Id;
        document.Execute(new SetRunFormatCommand(new DocumentRange(document.PositionOf(id, from), document.PositionOf(id, to)), f => f with { Bold = true }, "Bold"));
    }

    [Fact]
    public async Task Bold_OnSelection_TogglesOnThenOff()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        var cut = Render(document);
        await Select(cut, 0, 0, 5);
        var selection = cut.Instance.Selection;
        var step = document.Undo.UndoDescription;

        await Run(cut, "wd-bold");
        Assert.True(Run(document, 0, 0).Bold);
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-bold")));
        await Run(cut, "wd-bold");
        Assert.False(Run(document, 0, 0).Bold);
        cut.WaitForAssertion(() => Assert.False(Checked(cut, "wd-bold")));

        await Run(cut, "wd-undo");
        Assert.True(Run(document, 0, 0).Bold);
        await Run(cut, "wd-undo");
        Assert.NotEqual(true, Run(document, 0, 0).Bold);
        Assert.Equal(selection, cut.Instance.Selection);
        Assert.Equal(step, document.Undo.UndoDescription);
    }

    [Fact]
    public async Task Bold_OnCaret_SetsCaretFormat_NextTypingIsBold()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        var id = document.Blocks[0].Id;
        await Select(cut, 0, 5, 5);
        var version = document.Version;

        await Run(cut, "wd-bold");

        Assert.Equal(version, document.Version);
        Assert.True(Surface(cut).CaretFormat?.Bold);
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-bold")));
        var result = (SurfaceResult)_context.JSInterop.Invocations["resume"].Last().Arguments[0]!;
        var token = Assert.IsType<SurfaceCaretFormat>(result.CaretFormat).Token;
        Assert.Contains("font-weight:bold", result.CaretFormat.Css);
        await cut.InvokeAsync(() => Surface(cut).OnSyncAsync(new SurfaceSync(Seen, [new SurfaceTyped(id, "Hello!", 6, token)], new SurfaceSelection(new SurfacePoint(id, 6), new SurfacePoint(id, 6)), false), null));

        Assert.Equal("Hello!", P(document).Text);
        Assert.True(Run(document, 0, 5).Bold);
        Assert.NotEqual(true, Run(document, 0, 0).Bold);
    }

    [Fact]
    public async Task Bold_MixedSelection_TurnsAllBold()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        Bold(document, 0, 0, 3);
        var cut = Render(document);
        await Select(cut, 0, 0, 11);
        Assert.False(Checked(cut, "wd-bold"));

        await Run(cut, "wd-bold");

        Assert.All(Enumerable.Range(0, 11), i => Assert.True(Run(document, 0, i).Bold));
    }

    [Fact]
    public async Task FontSize_GrowFrom11_Is12_ShrinkFrom12_Is11()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 0, 5);
        Assert.Equal(11, Run(document, 0, 0).SizePt);

        await Run(cut, "wd-grow-font");
        Assert.Equal(12, Run(document, 0, 0).SizePt);
        cut.WaitForAssertion(() => Assert.Equal(12, cut.FindComponent<MudExRibbonNumberBox>().Instance.Value));
        await Run(cut, "wd-shrink-font");
        Assert.Equal(11, Run(document, 0, 0).SizePt);
        await cut.InvokeAsync(() => cut.Instance.SetSizeAsync(72));
        await Run(cut, "wd-grow-font");
        Assert.Equal(80, Run(document, 0, 0).SizePt);
    }

    [Fact]
    public async Task Align_Center_SetsAlignment_ButtonChecked()
    {
        var document = DocumentSurfaceRenderTests.Created("one", "two");
        var cut = Render(document);
        await Select(cut, document.Blocks[0].Id, 1, document.Blocks[1].Id, 0);

        await Run(cut, "wd-align-center");

        Assert.Equal(ParagraphAlignment.Center, document.EffectiveParagraphFormat(P(document)).Alignment);
        Assert.NotEqual(ParagraphAlignment.Center, document.EffectiveParagraphFormat(P(document, 1)).Alignment);
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-align-center")));
        Assert.False(Checked(cut, "wd-align-left"));
    }

    [Fact]
    public async Task Bullets_ToggleOnThenOff()
    {
        var document = DocumentSurfaceRenderTests.Created("one", "two", "three");
        var cut = Render(document);
        await Select(cut, document.Blocks[0].Id, 0, document.Blocks[2].Id, 5);

        await Run(cut, "wd-bullets");
        Assert.All(document.Blocks, b => Assert.Equal(Bullet, document.ListLabel((Paragraph)b)));
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-bullets")));
        await Run(cut, "wd-bullets");

        Assert.All(document.Blocks, b => Assert.Null(document.ListLabel((Paragraph)b)));
        cut.WaitForAssertion(() => Assert.False(Checked(cut, "wd-bullets")));
    }

    [Fact]
    public async Task Indent_ListItem_ChangesLevel_Plain_ChangesIndentLeft()
    {
        var document = DocumentSurfaceRenderTests.Created("item", "plain");
        document.Execute(new ApplyListCommand([document.Blocks[0].Id], ListTemplate.Decimal));
        var cut = Render(document);

        await Select(cut, 0, 0, 0);
        await Run(cut, "wd-indent");
        await Select(cut, 1, 0, 0);
        await Run(cut, "wd-indent");

        Assert.Equal(1, P(document).Format.Numbering?.Level);
        Assert.Equal(720, P(document, 1).Format.IndentLeft);
        await cut.InvokeAsync(() => cut.Instance.IndentAsync(-1));
        Assert.Equal(0, P(document, 1).Format.IndentLeft);
    }

    [Fact]
    public async Task LineSpacing_15_SetsAuto360()
    {
        var document = DocumentSurfaceRenderTests.Created("one");
        var cut = Render(document);
        await Select(cut, 0, 1, 1);

        await Run(cut, "wd-line-spacing-150");

        Assert.Equal(new LineSpacing(360, LineRule.Auto), P(document).Format.Line);
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-line-spacing-150")));
        Assert.False(Checked(cut, "wd-line-spacing-100"));
    }

    [Fact]
    public async Task Highlight_YellowColour_StoresName()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 0, 5);
        var picker = cut.FindComponents<MudExRibbonColorPicker>().Single(c => c.Instance.Id == "wd-highlight-color").Instance;

        await cut.InvokeAsync(() => picker.ValueChanged.InvokeAsync("#FFFF00"));
        Assert.Equal("yellow", Run(document, 0, 0).Highlight);
        await cut.InvokeAsync(() => picker.ValueChanged.InvokeAsync("#123456"));
        Assert.Equal("yellow", Run(document, 0, 0).Highlight);
        await Run(cut, "wd-highlight-none");

        Assert.Equal("none", Run(document, 0, 0).Highlight);
    }

    [Fact]
    public async Task FontAndColour_OnASelection()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 0, 5);
        var color = cut.FindComponents<MudExRibbonColorPicker>().Single(c => c.Instance.Id == "wd-font-color").Instance;

        await cut.InvokeAsync(() => color.ValueChanged.InvokeAsync("#C00000"));
        await cut.InvokeAsync(() => cut.FindComponent<MudExRibbonSelect<string>>().Instance.ValueChanged.InvokeAsync("Arial"));

        Assert.Equal(("#C00000", "Arial"), (Run(document, 0, 0).Color, Run(document, 0, 4).Font));
        cut.WaitForAssertion(() => Assert.Equal("Arial", cut.FindComponent<MudExRibbonSelect<string>>().Instance.Value));
    }

    [Fact]
    public async Task ClearAll_IsOneUndoStep()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var id = document.Blocks[0].Id;
        Bold(document, 0, 0, 5);
        document.Execute(new SetParagraphFormatCommand([id], p => p with { Alignment = ParagraphAlignment.Center, SpacingBefore = 240 }, "Center"));
        document.Execute(new ApplyParagraphStyleCommand([id], "Heading1"));
        var cut = Render(document);
        await Select(cut, 0, 0, 5);

        await Run(cut, "wd-clear-formatting");

        Assert.Equal(new RunFormat(), Assert.IsType<TextRun>(Assert.Single(P(document).Inlines)).Format);
        Assert.Equal(new ParagraphFormat(), P(document).Format);
        await Run(cut, "wd-undo");
        Assert.True(Run(document, 0, 0).Bold);
        Assert.Equal(("Heading1", ParagraphAlignment.Center, 240), (P(document).Format.StyleId, P(document).Format.Alignment, P(document).Format.SpacingBefore));
    }

    [Fact]
    public async Task Shortcut_CtrlB_ViaSurfaceCommand_Bolds()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var id = document.Blocks[0].Id;
        var cut = Render(document);

        await cut.InvokeAsync(() => Surface(cut).OnCommandAsync(new SurfaceSync(Seen, [], new SurfaceSelection(new SurfacePoint(id, 0), new SurfacePoint(id, 5)), false), null, "shortcut", "Ctrl+B"));

        cut.WaitForAssertion(() => Assert.True(Run(document, 0, 2).Bold));
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-bold")));
    }

    [Fact]
    public void Shortcuts_MapContainsTheWordSet()
    {
        string[] keys =
        [
            "Ctrl+B", "Ctrl+I", "Ctrl+U", "Ctrl+Shift+D", "Ctrl+=", "Ctrl+Shift+=", "Ctrl+Space", "Ctrl+]", "Ctrl+[", "Ctrl+Shift+.", "Ctrl+Shift+,", "Ctrl+L", "Ctrl+E",
            "Ctrl+R", "Ctrl+J", "Ctrl+M", "Ctrl+Shift+M", "Ctrl+Shift+L", "Ctrl+1", "Ctrl+5", "Ctrl+2", "Ctrl+0", "Ctrl+Alt+1", "Ctrl+Alt+2", "Ctrl+Alt+3", "Ctrl+Shift+N",
            "Ctrl+D", "Ctrl+S", "Ctrl+F", "Ctrl+H"
        ];

        Assert.All(keys, k => Assert.Contains(k, DocumentShortcuts.Map.Keys));
        Assert.Equal(keys.Length, DocumentShortcuts.Map.Count);
        Assert.Equal("superscript", DocumentShortcuts.ActionOf("Ctrl+Shift+="));
        Assert.Null(DocumentShortcuts.ActionOf("Ctrl+Shift++"));
        var surface = Surface(Render(TextDocument.Create()));
        Assert.Contains("Ctrl+B", surface.Shortcuts!);
        Assert.Contains("Ctrl+S", surface.Shortcuts!);
        Assert.DoesNotContain("Ctrl+F", surface.Shortcuts!);
    }

    [Fact]
    public async Task Shortcuts_ParagraphActions()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 1, 1);
        Task Press(string key) => cut.InvokeAsync(() => Surface(cut).ShortcutPressed!(key));

        await Press("Ctrl+E");
        Assert.Equal(ParagraphAlignment.Center, P(document).Format.Alignment);
        await Press("Ctrl+E");
        Assert.Equal(ParagraphAlignment.Left, P(document).Format.Alignment);
        await Press("Ctrl+2");
        Assert.Equal(new LineSpacing(480, LineRule.Auto), P(document).Format.Line);
        await Press("Ctrl+0");
        Assert.Equal(240, P(document).Format.SpacingBefore);
        await Press("Ctrl+0");
        Assert.Equal(0, P(document).Format.SpacingBefore);
        await Press("Ctrl+Alt+2");
        Assert.Equal("Heading2", P(document).Format.StyleId);
        await Press("Ctrl+Shift+N");
        Assert.Null(P(document).Format.StyleId);
    }

    [Fact]
    public async Task RibbonState_FollowsSelection()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        Bold(document, 0, 0, 5);
        var cut = Render(document);

        await Select(cut, 0, 3, 3);
        cut.WaitForAssertion(() => Assert.True(Checked(cut, "wd-bold")));
        await Select(cut, 0, 8, 8);
        cut.WaitForAssertion(() => Assert.False(Checked(cut, "wd-bold")));
    }

    [Fact]
    public async Task RibbonState_RendersOnlyWhenTheFormatChanges()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        var cut = RenderEditor<CountingEditor>(p => p.Add(x => x.TextDocument, document));
        await Select(cut, 0, 1, 1);
        await cut.InvokeAsync(() => Task.Delay(50));
        var renders = cut.Instance.Renders;

        await Select(cut, 0, 2, 2);
        await Select(cut, 0, 3, 3);
        await cut.InvokeAsync(() => Task.Delay(50));

        Assert.Equal(renders, cut.Instance.Renders);
    }

    [Fact]
    public async Task ReadOnly_FormattingDoesNothing()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document, readOnly: true);
        await Select(cut, 0, 0, 5);
        var version = document.Version;

        await Run(cut, "wd-bold");
        await Run(cut, "wd-align-center");
        await cut.InvokeAsync(() => Surface(cut).ShortcutPressed!("Ctrl+B"));
        await cut.InvokeAsync(() => cut.Instance.ToggleRunAsync("italic"));
        await cut.InvokeAsync(() => cut.Instance.ClearFormattingAsync(true));

        Assert.Equal(version, document.Version);
        Assert.Null(Surface(cut).CaretFormat);
        Assert.True(Command(cut, "wd-bold").Disabled);
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
