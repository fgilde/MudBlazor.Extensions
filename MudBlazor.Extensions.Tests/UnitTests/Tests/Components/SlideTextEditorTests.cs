using System.Text.Json;
using System.Xml.Linq;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideTextEditorTests : IAsyncLifetime
{
    private const long Inch = 914400;
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private const string Text =
        "<a:p><a:r><a:rPr lang=\"de-DE\" kumimoji=\"1\"/><a:t>Normal </a:t></a:r><a:r><a:rPr lang=\"de-DE\" b=\"1\" spc=\"50\"/><a:t>fett</a:t></a:r>" +
        "<a:r><a:rPr i=\"1\" kumimoji=\"1\"/><a:t> normal</a:t></a:r></a:p>" +
        "<a:p><a:endParaRPr lang=\"de-DE\" sz=\"2400\" dirty=\"0\"/></a:p>" +
        "<a:p><a:r><a:rPr lang=\"en-US\" kumimoji=\"1\" spc=\"20\"/><a:t>one two three</a:t></a:r><a:br><a:rPr lang=\"en-US\"/></a:br>" +
        "<a:r><a:rPr lang=\"en-US\"/><a:t>last</a:t></a:r></a:p>";

    private readonly List<BunitContext> _contexts = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();
    }

    private static Presentation Deck() => new PptxBuilder()
        .Slide(Shape(2, "Text", Xfrm(Inch, Inch, 4 * Inch, 2 * Inch), Text) + Shape(3, "Box", Xfrm(6 * Inch, Inch, Inch, Inch)))
        .Slide(Shape(2, "Other", Xfrm(Inch, Inch, Inch, Inch), "<a:p><a:r><a:t>second</a:t></a:r></a:p>"))
        .Load();

    private static AutoShape TextShape(Presentation presentation, int slide = 0) =>
        presentation.Slides[slide].Shapes.OfType<AutoShape>().Single(s => s.Name == "Text");

    private BunitContext NewContext()
    {
        var context = new BunitContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        context.Render<MudPopoverProvider>();
        context.Render<MudDialogProvider>();
        return context;
    }

    private IRenderedComponent<MudExSlideEditor> RenderEditor(Presentation presentation)
    {
        var cut = NewContext().Render<MudExSlideEditor>(p => p.Add(x => x.Presentation, presentation));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-canvas .mud-ex-slide")));
        return cut;
    }

    private IRenderedComponent<MudExSlideCanvas> RenderCanvas(Presentation presentation) =>
        NewContext().Render<MudExSlideCanvas>(p => p
            .Add(c => c.Presentation, presentation)
            .Add(c => c.Slide, presentation.Slides[0])
            .Add(c => c.Images, new SlideImageUrls(presentation)));

    private static SlideTextParagraphDto[] Dom(AutoShape shape, Presentation presentation) =>
        JsonSerializer.Deserialize<SlideTextParagraphDto[]>(JsonSerializer.Serialize(SlideTextDom.ToDom(shape, presentation), Web), Web)!;

    private static SlideTextRunDto Copy(SlideTextRunDto run, string text, string? bold = null) =>
        new() { Source = run.Source, Text = text, B = bold ?? run.B, I = run.I, U = run.U, S = run.S, Size = run.Size, Color = run.Color, Font = run.Font };

    private static Task Run(IRenderedComponent<MudExSlideEditor> cut, string id)
        => cut.InvokeAsync(() => (cut.FindComponent<MudExRibbon>().Instance.Commands.SingleOrDefault(c => c.Id == id)
                                  ?? cut.FindComponents<MudExRibbonButton>().Select(b => b.Instance).Single(b => b.Id == id)).ExecuteAsync());

    private static Presentation SaveAndLoad(Presentation presentation)
    {
        var stream = new MemoryStream();
        presentation.Save(stream);
        stream.Position = 0;
        return Presentation.Load(stream);
    }

    private static XElement RunProperties(TextRun run) => run.Source!.Element(A + "rPr")!;

    [Fact]
    public void TextDom_RoundTrip_KeepsRunsAndFormats()
    {
        var presentation = Deck();
        var shape = TextShape(presentation);

        var dom = Dom(shape, presentation);

        Assert.Equal(3, dom.Length);
        Assert.Equal(["Normal ", "fett", " normal"], dom[0].Runs.Select(r => r.Text));
        Assert.Equal([0, 1, 2], dom[0].Runs.Select(r => r.Source!.Value));
        Assert.Equal([null, "1", null], dom[0].Runs.Select(r => r.B));
        Assert.Equal("1", dom[0].Runs[2].I);
        Assert.Contains("font-weight:bold", dom[0].Runs[1].Style);
        Assert.Contains("text-align:left", dom[0].Style);
        Assert.Equal([0, 1, 2], dom.Select(p => p.Source!.Value));
        var empty = Assert.Single(dom[1].Runs);
        Assert.Equal("", empty.Text);
        Assert.Null(empty.Source);
        Assert.Equal("24", empty.Size);
        Assert.Equal(["one two three", "\n", "last"], dom[2].Runs.Select(r => r.Text));

        Assert.Same(shape.Text, SlideTextDom.FromDom(shape.Text, dom));

        var original = shape.Text.Paragraphs[0].Runs;
        dom[0].Runs = [Copy(dom[0].Runs[0], "Normal "), Copy(dom[0].Runs[1], "dick"), Copy(dom[0].Runs[2], " normal")];
        dom[1].Runs = [new SlideTextRunDto { Text = "\u200Bneu" }];
        var edited = SlideTextDom.FromDom(shape.Text, dom);

        Assert.Same(original[0], edited.Paragraphs[0].Runs[0]);
        Assert.Same(original[2], edited.Paragraphs[0].Runs[2]);
        Assert.Same(original[1].Source, edited.Paragraphs[0].Runs[1].Source);
        Assert.Equal(new RunFormat(Bold: true), edited.Paragraphs[0].Runs[1].Format);
        Assert.Equal("neu", Assert.Single(edited.Paragraphs[1].Runs).Text);
        Assert.Same(shape.Text.Paragraphs[1].Source, edited.Paragraphs[1].Source);
        Assert.Same(shape.Text.Paragraphs[2], edited.Paragraphs[2]);
    }

    [Fact]
    public async Task TextEdit_BoldMiddleWord_IsSeparateRun()
    {
        var presentation = Deck();
        var shape = TextShape(presentation);
        var source = shape.Text.Paragraphs[2].Runs[0].Source;
        var cut = RenderCanvas(presentation);

        await cut.InvokeAsync(() => cut.Instance.OnShapeDoubleClickAsync(shape.Id, 10, 20));
        var editor = cut.FindComponent<MudExSlideTextEditor>();
        Assert.Equal([shape.Id], cut.Instance.Selection);
        Assert.Empty(cut.Find(".mud-ex-slide-text-editable").InnerHtml);

        var dom = Dom(shape, presentation);
        var run = dom[2].Runs[0];
        dom[2].Runs = [Copy(run, "one "), Copy(run, "two", "1"), Copy(run, " three"), .. dom[2].Runs.Skip(1)];
        await cut.InvokeAsync(() => editor.Instance.OnTextCommitAsync(dom, null));

        Assert.Empty(cut.FindComponents<MudExSlideTextEditor>());
        var runs = shape.Text.Paragraphs[2].Runs;
        Assert.Equal(["one ", "two", " three", "\n", "last"], runs.Select(r => r.Text));
        Assert.Equal([null, true, null], runs.Take(3).Select(r => r.Format.Bold));
        Assert.All(runs.Take(3), r => Assert.Same(source, r.Source));

        var paragraph = SaveAndLoad(presentation).Slides[0].Shapes.OfType<AutoShape>().Single(s => s.Name == "Text").Text.Paragraphs[2];
        Assert.Equal(["one ", "two", " three", "\n", "last"], paragraph.Runs.Select(r => r.Text));
        Assert.All(paragraph.Runs.Take(3), r => Assert.Equal("1", (string?)RunProperties(r).Attribute("kumimoji")));
        Assert.All(paragraph.Runs.Take(3), r => Assert.Equal("20", (string?)RunProperties(r).Attribute("spc")));
        Assert.Equal([null, "1", null], paragraph.Runs.Take(3).Select(r => (string?)RunProperties(r).Attribute("b")));

        presentation.Undo.Undo();
        Assert.Equal(["one two three", "\n", "last"], shape.Text.Paragraphs[2].Runs.Select(r => r.Text));
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public async Task TextEdit_Session_IsOneUndoStep()
    {
        var presentation = Deck();
        var shape = TextShape(presentation);
        var before = shape.Text;
        var cut = RenderEditor(presentation);
        var canvas = cut.FindComponent<MudExSlideCanvas>();

        await cut.InvokeAsync(() => canvas.Instance.OnSelectionAsync([shape.Id]));
        cut.Find(".mud-ex-slide-canvas").KeyDown(new KeyboardEventArgs { Key = "x" });
        var editor = cut.FindComponent<MudExSlideTextEditor>();
        Assert.Equal("x", editor.Instance.Text);
        Assert.False(cut.Instance.IsDirty);

        var dom = Dom(shape, presentation);
        dom[0].Runs[1].Text = "f";
        await cut.InvokeAsync(() => editor.Instance.OnTextInputAsync(dom, null));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));
        cut.Find(".mud-ex-slide-text-editable").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("Slide 1 of 2", cut.Find(".mud-ex-slide-status-position").TextContent.Trim());
        dom[0].Runs[1].Text = "fe";
        await cut.InvokeAsync(() => editor.Instance.OnTextInputAsync(dom, null));
        dom[0].Runs[1].Text = "fest";
        await cut.InvokeAsync(() => editor.Instance.OnTextInputAsync(dom, null));
        Assert.Same(before, shape.Text);
        Assert.False(presentation.Undo.CanUndo);

        cut.FindAll(".mud-ex-slide-panel-item")[1].Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindComponents<MudExSlideTextEditor>()));
        Assert.Equal(["Normal ", "fest", " normal"], shape.Text.Paragraphs[0].Runs.Select(r => r.Text));
        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.Same(before, shape.Text);
        Assert.False(presentation.Undo.CanUndo);

        await cut.InvokeAsync(() => canvas.Instance.OnSelectionAsync([presentation.Slides[1].Shapes[0].Id]));
        cut.Find(".mud-ex-slide-canvas").KeyDown(new KeyboardEventArgs { Key = "F2" });
        editor = cut.FindComponent<MudExSlideTextEditor>();
        Assert.Null(editor.Instance.Text);
        await cut.InvokeAsync(() => editor.Instance.OnTextCommitAsync(Dom((AutoShape)presentation.Slides[1].Shapes[0], presentation), null));
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public async Task TextEdit_PasteHtml_KeepsBoldDropsScript()
    {
        var presentation = Deck();
        var shape = TextShape(presentation);
        var cut = RenderCanvas(presentation);
        await cut.InvokeAsync(() => cut.Instance.OnShapeDoubleClickAsync(shape.Id, 0, 0));
        var editor = cut.FindComponent<MudExSlideTextEditor>();

        var pasted = await editor.Instance.OnPasteHtml(
            "<html><head><style>b{color:red}</style><title>t</title></head><body><!--StartFragment--><b onclick=\"alert(1)\">neu</b>" +
            "<script>alert('x')</script><span style=\"color:#ff0000;font-size:40px;font-family:Evil\" class=\"x\">&lt;script&gt;rot</span>" +
            "<img src=x onerror=alert(1)><p><i>zwei</i> <u>drei</u></p><!--EndFragment--></body></html>", null);

        Assert.Equal(2, pasted.Length);
        Assert.Equal(["neu", "<script>rot"], pasted[0].Runs.Select(r => r.Text));
        Assert.Equal(["1", null], pasted[0].Runs.Select(r => r.B));
        Assert.Equal([null, "#FF0000"], pasted[0].Runs.Select(r => r.Color));
        Assert.All(pasted.SelectMany(p => p.Runs), r => Assert.Null(r.Size));
        Assert.All(pasted.SelectMany(p => p.Runs), r => Assert.Null(r.Font));
        Assert.Equal(["zwei", " ", "drei"], pasted[1].Runs.Select(r => r.Text));
        Assert.Equal(["1", null, null], pasted[1].Runs.Select(r => r.I));
        Assert.Equal([null, null, "1"], pasted[1].Runs.Select(r => r.U));
        Assert.DoesNotContain(pasted.SelectMany(p => p.Runs), r => r.Text.Contains("alert"));
        Assert.Empty(await editor.Instance.OnPasteHtml("<script>alert(1)</script>", null));

        var dom = Dom(shape, presentation);
        var bold = dom[0].Runs[1];
        var inserted = pasted[0].Runs.Select(r => new SlideTextRunDto { Source = bold.Source, Text = r.Text, B = r.B ?? bold.B, Color = r.Color ?? bold.Color }).ToArray();
        dom[0].Runs = [dom[0].Runs[0], .. inserted, dom[0].Runs[2]];
        await cut.InvokeAsync(() => editor.Instance.OnTextCommitAsync(dom, null));

        var reloaded = SaveAndLoad(presentation).Slides[0].Shapes.OfType<AutoShape>().Single(s => s.Name == "Text").Text;
        var runs = reloaded.Paragraphs[0].Runs;
        Assert.Equal(["Normal ", "neu", "<script>rot", " normal"], runs.Select(r => r.Text));
        Assert.Equal("1", (string?)RunProperties(runs[0]).Attribute("kumimoji"));
        Assert.Equal("1", (string?)RunProperties(runs[3]).Attribute("kumimoji"));
        Assert.Equal("1", (string?)RunProperties(runs[3]).Attribute("i"));
        Assert.Equal("50", (string?)RunProperties(runs[1]).Attribute("spc"));
        Assert.Equal("#FF0000", runs[2].Format.Color);
        Assert.Equal(true, runs[2].Format.Bold);
        Assert.Equal("<script>rot", runs[2].Source!.Element(A + "t")!.Value);
        var end = reloaded.Paragraphs[1].Source!.Element(A + "endParaRPr")!;
        Assert.Equal("0", (string?)end.Attribute("dirty"));
        Assert.Equal("2400", (string?)end.Attribute("sz"));
    }

    [Fact]
    public async Task TextEdit_EmptyNewTextBox_IsRemovedWithoutUndoStep()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var cut = RenderEditor(presentation);
        var canvas = cut.FindComponent<MudExSlideCanvas>();

        await Run(cut, "pp-insert-text-box");
        cut.WaitForAssertion(() => Assert.Equal("text-box", cut.Find(".mud-ex-slide-canvas").GetAttribute("data-insert")));
        await cut.InvokeAsync(() => canvas.Instance.OnInsertAsync(2 * Inch, 3 * Inch, 0, 0));

        var box = Assert.IsType<AutoShape>(slide.Shapes[^1]);
        Assert.True(box.IsTextBox);
        Assert.Equal(2 * Inch, box.ResolvedTransform.X);
        Assert.Equal([box.Id], cut.Instance.SelectedShapes);
        Assert.Null(cut.Find(".mud-ex-slide-canvas").GetAttribute("data-insert"));
        var editor = cut.FindComponent<MudExSlideTextEditor>();

        await cut.InvokeAsync(() => editor.Instance.OnTextCommitAsync(Dom(box, presentation), null));

        Assert.DoesNotContain(box, slide.Shapes);
        Assert.Empty(cut.Instance.SelectedShapes);
        Assert.False(presentation.Undo.CanUndo);
        Assert.False(cut.Instance.IsDirty);

        await Run(cut, "pp-insert-text-box");
        await cut.InvokeAsync(() => canvas.Instance.OnInsertAsync(Inch, Inch, 0, 0));
        box = (AutoShape)slide.Shapes[^1];
        editor = cut.FindComponent<MudExSlideTextEditor>();
        var dom = Dom(box, presentation);
        dom[0].Runs[0].Text = "Hallo";
        await cut.InvokeAsync(() => editor.Instance.OnTextInputAsync(dom, null));
        await cut.InvokeAsync(() => editor.Instance.OnTextCommitAsync(dom, null));

        Assert.Same(box, slide.Shapes[^1]);
        Assert.Equal("Hallo", box.Text.Paragraphs[0].Runs.Single().Text);
        Assert.Equal($"TextBox {box.Id - 1}", box.Name);
        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.DoesNotContain(box, slide.Shapes);
        Assert.False(presentation.Undo.CanUndo);
        await cut.InvokeAsync(() => presentation.Undo.Redo());
        Assert.Equal("Hallo", ((AutoShape)slide.Shapes[^1]).Text.Paragraphs[0].Runs.Single().Text);
    }

    [Fact]
    public async Task RibbonBold_WithoutEdit_AppliesToWholeShape()
    {
        var presentation = Deck();
        var shape = TextShape(presentation);
        var original = shape.Text.Paragraphs[0].Runs;
        var cut = RenderEditor(presentation);
        var canvas = cut.FindComponent<MudExSlideCanvas>();

        await cut.InvokeAsync(() => canvas.Instance.OnSelectionAsync([shape.Id, 3]));
        await Run(cut, "pp-bold");

        var runs = shape.Text.Paragraphs.SelectMany(p => p.Runs).Where(r => r.Text != "\n").ToList();
        Assert.All(runs, r => Assert.True(r.Format.Bold));
        Assert.Equal(true, shape.Text.Paragraphs[1].EndFormat.Bold);
        Assert.Same(original[1], shape.Text.Paragraphs[0].Runs[1]);
        Assert.Same(original[0].Source, shape.Text.Paragraphs[0].Runs[0].Source);
        Assert.Equal(true, shape.Text.Paragraphs[0].Runs[2].Format.Italic);
        Assert.Empty(cut.FindComponents<MudExSlideTextEditor>());

        await Run(cut, "pp-bold");
        Assert.All(shape.Text.Paragraphs.SelectMany(p => p.Runs).Where(r => r.Text != "\n"), r => Assert.False(r.Format.Bold));

        await Run(cut, "pp-align-center");
        Assert.All(shape.Text.Paragraphs, p => Assert.Equal(TextAlign.Center, p.Align));

        cut.Find(".mud-ex-slide-canvas").KeyDown(new KeyboardEventArgs { Key = "i", CtrlKey = true });
        Assert.All(shape.Text.Paragraphs.SelectMany(p => p.Runs).Where(r => r.Text != "\n"), r => Assert.True(r.Format.Italic));

        await cut.InvokeAsync(() =>
        {
            presentation.Undo.Undo();
            presentation.Undo.Undo();
            presentation.Undo.Undo();
        });
        Assert.All(shape.Text.Paragraphs.SelectMany(p => p.Runs).Where(r => r.Text != "\n"), r => Assert.True(r.Format.Bold));
        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.Same(original[0], shape.Text.Paragraphs[0].Runs[0]);
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public async Task RibbonState_WhileEditing_FollowsTheTextSelection()
    {
        var presentation = Deck();
        var shape = TextShape(presentation);
        var cut = RenderEditor(presentation);
        var canvas = cut.FindComponent<MudExSlideCanvas>();
        bool? Checked(string id) => ((MudExRibbonToggleButton)cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id)).Checked;
        double Size() => cut.FindComponents<MudExRibbonNumberBox>().Single(b => b.Instance.Text == "Font Size").Instance.Value;
        Task Report(params SlideTextParagraphDto[] selection) =>
            cut.InvokeAsync(() => cut.FindComponent<MudExSlideTextEditor>().Instance.OnSelectionFormatAsync(selection));

        await cut.InvokeAsync(() => canvas.Instance.OnShapeDoubleClickAsync(shape.Id, 0, 0));
        Assert.False(Checked("pp-bold"));
        Assert.Equal(18, Size());

        await Report(new SlideTextParagraphDto { Source = 0, Runs = [new SlideTextRunDto { Source = 1, Text = "fett", B = "1" }] });
        cut.WaitForAssertion(() => Assert.True(Checked("pp-bold")));
        Assert.False(Checked("pp-italic"));
        Assert.True(Checked("pp-align-left"));

        await Report(new SlideTextParagraphDto { Source = 0, Align = "center", Runs = [new SlideTextRunDto { Source = 1, Text = "fett", B = "1" }, new SlideTextRunDto { Source = 2, Text = " n", I = "1" }] });
        cut.WaitForAssertion(() => Assert.False(Checked("pp-bold")));
        Assert.True(Checked("pp-align-center"));
        Assert.False(Checked("pp-align-left"));

        await Report(new SlideTextParagraphDto { Source = 1, Runs = [new SlideTextRunDto { Text = "​" }] });
        cut.WaitForAssertion(() => Assert.Equal(24, Size()));

        await Report(new SlideTextParagraphDto { Runs = [new SlideTextRunDto { Text = "new", I = "1", Size = "40", Font = "Georgia" }] });
        cut.WaitForAssertion(() => Assert.True(Checked("pp-italic")));
        Assert.Equal(40, Size());
        Assert.Equal("Georgia", cut.FindComponents<MudExRibbonSelect<string>>().Single(s => s.Instance.Text == "Font").Instance.Value);

        await cut.InvokeAsync(() => canvas.Instance.CommitTextAsync());
        cut.WaitForAssertion(() => Assert.False(Checked("pp-italic")));
        Assert.Equal(18, Size());
        Assert.False(presentation.Undo.CanUndo);
    }
}
