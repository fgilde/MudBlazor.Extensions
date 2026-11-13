using System.Text;
using System.Xml.Linq;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideEditorFeaturesTests : IAsyncLifetime
{
    private const long Inch = 914400;
    private const long Nudge = Inch / 8;
    private const string Solid = "<a:solidFill><a:srgbClr val=\"FF0000\"/></a:solidFill>";

    private readonly BunitContext _context = new();
    private IRenderedComponent<MudPopoverProvider> _popovers;
    private IRenderedComponent<MudDialogProvider> _dialogs;

    public Task InitializeAsync()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _popovers = _context.Render<MudPopoverProvider>();
        _dialogs = _context.Render<MudDialogProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private static Presentation Deck() => new PptxBuilder { NotesMaster = true }
        .Slide(Shape(2, "Title", Xfrm(Inch, Inch, 4 * Inch, Inch), "<a:p><a:r><a:rPr lang=\"en-US\"/><a:t>app</a:t></a:r><a:r><a:rPr lang=\"en-US\" b=\"1\"/><a:t>le pie, Apple</a:t></a:r></a:p>") +
               Shape(3, "Box", Xfrm(6 * Inch, Inch, Inch, Inch) + Solid, "<a:p><a:r><a:t>box text</a:t></a:r></a:p>"), notes: "an apple a day")
        .Slide(Shape(2, "Other", Xfrm(Inch, 2 * Inch, 2 * Inch, Inch), "<a:p><a:r><a:t>pineapple and second apple</a:t></a:r></a:p>"))
        .Slide(Shape(2, "Third", Xfrm(Inch, Inch, Inch, Inch), "<a:p><a:r><a:t>third</a:t></a:r></a:p>"))
        .Load();

    private IRenderedComponent<MudExSlideEditor> RenderEditor(Presentation presentation)
    {
        var cut = _context.Render<MudExSlideEditor>(p => p.Add(x => x.Presentation, presentation));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-canvas .mud-ex-slide")));
        return cut;
    }

    private static MudExSlideCanvas Canvas(IRenderedComponent<MudExSlideEditor> cut) => cut.FindComponent<MudExSlideCanvas>().Instance;

    private static Task Key(IRenderedComponent<MudExSlideEditor> cut, string key, bool ctrl = false, string target = ".mud-ex-slide-canvas") =>
        cut.Find(target).TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = key, CtrlKey = ctrl });

    private static string Text(SlideShape shape) => string.Join("\n", ((AutoShape)shape).Text.Paragraphs.Select(p => string.Concat(p.Runs.Select(r => r.Text))));

    [Fact]
    public async Task Gallery_InsertChevron_UsesAccent1Fill()
    {
        var presentation = Deck();
        var cut = RenderEditor(presentation);
        var slide = presentation.Slides[0];

        cut.FindAll("button.mud-ex-ribbon-menu-face").First(b => b.GetAttribute("aria-label") == "Shapes").Click();
        _popovers.WaitForAssertion(() => Assert.NotEmpty(_popovers.FindAll(".mud-ex-slide-gallery [data-preset]")));
        Assert.Equal(["Lines", "Rectangles", "Basic shapes", "Arrows", "Callouts", "Stars"],
            _popovers.FindAll(".mud-ex-slide-gallery-header").Select(h => h.TextContent.Trim()));
        var presets = _popovers.FindAll(".mud-ex-slide-gallery [data-preset]").Select(b => b.GetAttribute("data-preset")).ToList();
        Assert.Equal(PresetGeometry.Supported.Order(), presets.Order());
        _popovers.Find("[data-preset='chevron']").Click();
        cut.WaitForAssertion(() => Assert.Equal("chevron", cut.Find(".mud-ex-slide-canvas").GetAttribute("data-insert")));

        await cut.InvokeAsync(() => Canvas(cut).OnInsertAsync(2 * Inch, Inch, 2 * Inch, -Inch));

        var chevron = Assert.IsType<AutoShape>(slide.Shapes[^1]);
        Assert.Equal("chevron", chevron.Geometry);
        Assert.Equal(new ShapeFill(ShapeFillKind.Inherited, "scheme:accent1"), chevron.Fill);
        Assert.Null(chevron.Line);
        Assert.Equal(new ShapeTransform(2 * Inch, 0, 2 * Inch, Inch), chevron.Transform);
        Assert.Equal([chevron.Id], cut.Instance.SelectedShapes);
        Assert.Equal("Insert shape", presentation.Undo.UndoDescription);
        Assert.Null(cut.Find(".mud-ex-slide-canvas").GetAttribute("data-insert"));
        cut.WaitForAssertion(() => Assert.Equal("#4472C4", cut.Find($".mud-ex-slide-canvas [data-shape-id='{chevron.Id}'] path").GetAttribute("fill"), ignoreCase: true));

        await cut.InvokeAsync(() => Canvas(cut).InsertShapeAsync("line"));
        await cut.InvokeAsync(() => Canvas(cut).OnInsertAsync(3 * Inch, 3 * Inch, -Inch, Inch));
        var line = Assert.IsType<AutoShape>(slide.Shapes[^1]);
        Assert.Equal(new ShapeTransform(2 * Inch, 3 * Inch, Inch, Inch, 0, true), line.Transform);
        Assert.Equal(ShapeFillKind.None, line.Fill.Kind);
        Assert.Equal("scheme:accent1", line.Line?.Color);

        await cut.InvokeAsync(() => Canvas(cut).InsertShapeAsync("ellipse"));
        await cut.InvokeAsync(() => Canvas(cut).OnInsertAsync(Inch, Inch, 0, 0));
        Assert.Equal(new ShapeTransform(Inch, Inch, Inch, Inch), slide.Shapes[^1].Transform);
    }

    [Fact]
    public async Task Gallery_NewShape_HasStyleAndCenteredBody()
    {
        var presentation = Deck();
        var cut = RenderEditor(presentation);
        var slide = presentation.Slides[0];

        await cut.InvokeAsync(() => Canvas(cut).InsertShapeAsync("ellipse"));
        await cut.InvokeAsync(() => Canvas(cut).OnInsertAsync(Inch, Inch, 2 * Inch, Inch));

        var shape = Assert.IsType<AutoShape>(slide.Shapes[^1]);
        Assert.Equal("#4472C4", ShapeFill.EffectiveFill(shape).Color is { } fill ? ColorResolver.Resolve(fill, shape.Master) : null, ignoreCase: true);
        Assert.StartsWith("scheme:accent1", ShapeLine.EffectiveLine(shape)?.Color);
        var paragraph = Assert.Single(shape.Text.Paragraphs);
        Assert.Empty(paragraph.Runs);
        Assert.Equal(TextAlign.Center, paragraph.Align);
        Assert.Equal(TextAnchor.Middle, shape.EffectiveBody().Anchor);

        presentation.Execute(new SetTextCommand(slide, shape.Id, shape.Text with { Paragraphs = [paragraph with { Runs = [new TextRun("typed")] }] }));
        var run = shape.Text.Paragraphs[0].Runs[0];
        Assert.Equal("#FFFFFF", shape.EffectiveFormat(run).Color, ignoreCase: true);
        Assert.Equal(TextAlign.Center, shape.EffectiveParagraph(shape.Text.Paragraphs[0]).Align);

        var output = PptxWriterTests.Save(presentation);
        PptxWriterTests.AssertConsistent(output);
        XNamespace p = "http://schemas.openxmlformats.org/presentationml/2006/main", a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        var sp = PptxWriterTests.Xml(output, "/ppt/slides/slide1.xml").Descendants(p + "sp").Last();
        var style = sp.Element(p + "style")!;
        Assert.Equal(["lnRef:2:accent1", "fillRef:1:accent1", "effectRef:0:accent1", "fontRef:minor:lt1"],
            style.Elements().Select(r => $"{r.Name.LocalName}:{(string)r.Attribute("idx")}:{(string)r.Elements().Single().Attribute("val")}"));
        Assert.Equal(["nvSpPr", "spPr", "style", "txBody"], sp.Elements().Select(e => e.Name.LocalName));
        Assert.Null(sp.Element(p + "spPr")!.Element(a + "solidFill"));
        Assert.Equal("ctr", (string)sp.Element(p + "txBody")!.Element(a + "bodyPr")!.Attribute("anchor"));
        Assert.Equal("ctr", (string)sp.Element(p + "txBody")!.Element(a + "p")!.Element(a + "pPr")!.Attribute("algn"));
        Assert.Equal("typed", sp.Descendants(a + "t").Single().Value);
        var reloaded = (AutoShape)Presentation.Load(new MemoryStream(output)).Slides[0].Shapes[^1];
        Assert.Equal("ellipse", reloaded.Geometry);
        Assert.Equal("#FFFFFF", reloaded.EffectiveFormat(reloaded.Text.Paragraphs[0].Runs[0]).Color, ignoreCase: true);
    }

    [Fact]
    public async Task InsertPicture_FitsIntoSlideKeepingAspect()
    {
        var presentation = Deck();
        var cut = RenderEditor(presentation);
        var slide = presentation.Slides[0];
        var wide = PngWriter.Write(new byte[2000 * 500], 2000, 500, 1);
        var small = PngWriter.Write(new byte[200 * 100], 200, 100, 1);

        await cut.InvokeAsync(() => cut.Instance.InsertPictureAsync(wide, "image/png"));
        var picture = Assert.IsType<PictureShape>(slide.Shapes[^1]);
        Assert.Equal(new ShapeTransform(1219200, 2209800, 9753600, 2438400), picture.Transform);
        Assert.Equal([picture.Id], cut.Instance.SelectedShapes);
        Assert.Equal("Insert picture", presentation.Undo.UndoDescription);
        Assert.Contains(_context.JSInterop.Invocations, i => i.Identifier == "register" && ReferenceEquals(i.Arguments[0], wide));

        await cut.InvokeAsync(() => cut.Instance.InsertPictureAsync(small, "image/png"));
        Assert.Equal(new ShapeTransform((12192000 - 1905000) / 2, (6858000 - 952500) / 2, 1905000, 952500), slide.Shapes[^1].Transform);

        var tall = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1000 4000\"><rect width=\"10\" height=\"40\"/></svg>");
        await cut.InvokeAsync(() => cut.Instance.InsertPictureAsync(tall, "image/svg+xml"));
        Assert.Equal(new ShapeTransform((12192000 - 1371600) / 2, 685800, 1371600, 5486400), slide.Shapes[^1].Transform);

        byte[] gif = [.. "GIF89a"u8, 0x20, 0x03, 0xC8, 0x00];
        await cut.InvokeAsync(() => cut.Instance.InsertPictureAsync(gif, "image/gif"));
        Assert.Equal(new ShapeTransform((12192000 - 7620000) / 2, (6858000 - 1905000) / 2, 7620000, 1905000), slide.Shapes[^1].Transform);
    }

    [Fact]
    public async Task CopyPasteShape_OtherSlide_KeepsTransform_SameSlide_Offsets()
    {
        var presentation = Deck();
        var cut = RenderEditor(presentation);
        var box = presentation.Slides[0].Shapes.Single(s => s.Id == 3);
        var at = box.ResolvedTransform;

        await cut.InvokeAsync(() => Canvas(cut).SelectAsync([3]));
        await Key(cut, "c", ctrl: true);
        Assert.Contains(_context.JSInterop.Invocations, i => i.Identifier == "writeClipboard" && (string)i.Arguments[0] == "box text");

        cut.FindAll(".mud-ex-slide-panel-item")[1].Click();
        await cut.InvokeAsync(() => cut.Instance.PasteAsync(null, null, null));
        var pasted = presentation.Slides[1].Shapes[^1];
        Assert.NotSame(box, pasted);
        Assert.Equal(3u, pasted.Id);
        Assert.Equal(at, pasted.ResolvedTransform);
        Assert.Equal("box text", Text(pasted));
        Assert.Equal([pasted.Id], cut.Instance.SelectedShapes);
        Assert.Equal("Paste", presentation.Undo.UndoDescription);

        cut.FindAll(".mud-ex-slide-panel-item")[0].Click();
        await cut.InvokeAsync(() => cut.Instance.PasteAsync("box text", null, null));
        Assert.Equal(at with { X = at.X + Nudge, Y = at.Y + Nudge }, presentation.Slides[0].Shapes[^1].ResolvedTransform);
        await cut.InvokeAsync(() => cut.Instance.PasteAsync("box text\r\n", null, null));
        Assert.Equal(at with { X = at.X + 2 * Nudge, Y = at.Y + 2 * Nudge }, presentation.Slides[0].Shapes[^1].ResolvedTransform);
        Assert.Equal(4, presentation.Slides[0].Shapes.Count);

        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.Equal(3, presentation.Slides[0].Shapes.Count);

        await cut.InvokeAsync(() => Canvas(cut).SelectAsync([3]));
        await Key(cut, "x", ctrl: true);
        Assert.DoesNotContain(presentation.Slides[0].Shapes, s => s.Id == 3);
        await cut.InvokeAsync(() => cut.Instance.PasteAsync(null, null, null));
        Assert.Equal(at, presentation.Slides[0].Shapes[^1].ResolvedTransform);
    }

    [Fact]
    public async Task Paste_ExternalText_IsTextBox_PanelSlides_AreDuplicatedAtTarget()
    {
        var presentation = Deck();
        var cut = RenderEditor(presentation);
        var slides = presentation.Slides.ToList();

        await cut.InvokeAsync(() => cut.Instance.PasteAsync("hello\r\nworld", null, null));
        var box = Assert.IsType<AutoShape>(presentation.Slides[0].Shapes[^1]);
        Assert.True(box.IsTextBox);
        Assert.Equal("hello\nworld", Text(box));
        Assert.Equal([box.Id], cut.Instance.SelectedShapes);

        await Key(cut, "c", ctrl: true, target: ".mud-ex-slide-panel");
        cut.FindAll(".mud-ex-slide-panel-item")[2].Click();
        await cut.InvokeAsync(() => cut.Instance.PasteAsync(null, null, null));
        Assert.Equal(4, presentation.Slides.Count);
        Assert.Equal(slides, presentation.Slides.Take(3));
        Assert.Same(presentation.Slides[3], cut.Instance.CurrentSlide);
        Assert.Equal(Text(slides[0].Shapes[0]), Text(presentation.Slides[3].Shapes[0]));
        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.Equal(slides, presentation.Slides);
    }

    [Fact]
    public async Task ReplaceAll_AcrossSlidesAndNotes_IsOneUndoStep()
    {
        var presentation = Deck();
        var cut = RenderEditor(presentation);
        var title = (AutoShape)presentation.Slides[0].Shapes[0];
        var before = title.Text;

        await Key(cut, "h", ctrl: true);
        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-slidefind-with input")));
        _dialogs.Find(".mud-ex-slidefind-what input").Input("apple");
        _dialogs.Find(".mud-ex-slidefind-with input").Input("pear");
        _dialogs.Find(".mud-ex-slidefind-whole input").Change(true);
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-slidefind-replace-all").Click());

        Assert.Equal("pear pie, pear", Text(title));
        Assert.Equal(["pear", " pie, pear"], title.Text.Paragraphs[0].Runs.Select(r => r.Text));
        Assert.Same(before.Paragraphs[0].Runs[1].Source, title.Text.Paragraphs[0].Runs[1].Source);
        Assert.True(title.Text.Paragraphs[0].Runs[1].Format.Bold);
        Assert.Equal("pineapple and second pear", Text(presentation.Slides[1].Shapes[0]));
        Assert.Equal("an pear a day", presentation.Slides[0].Notes);
        _dialogs.WaitForAssertion(() => Assert.Contains("4", _dialogs.Find(".mud-ex-slidefind-status").TextContent));
        Assert.Equal("Replace All", presentation.Undo.UndoDescription);

        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.Same(before, title.Text);
        Assert.Equal("pineapple and second apple", Text(presentation.Slides[1].Shapes[0]));
        Assert.Equal("an apple a day", presentation.Slides[0].Notes);
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public async Task FindReplace_ReplacesInTableCells()
    {
        var presentation = new PptxBuilder()
            .Slide(Shape(2, "Title", Xfrm(Inch, Inch, Inch, Inch), "<a:p><a:r><a:t>apple</a:t></a:r></a:p>") +
                   TableTests.Table(3, "", [Inch, Inch], TableTests.Tc("apple pie") + TableTests.Tc("x"), TableTests.Tc("an apple") + TableTests.Tc("APPLE")))
            .Load();
        var cut = RenderEditor(presentation);
        var table = TableTests.Table(presentation);
        var rows = table.Rows;

        await Key(cut, "h", ctrl: true);
        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-slidefind-with input")));
        _dialogs.Find(".mud-ex-slidefind-what input").Input("apple");
        _dialogs.Find(".mud-ex-slidefind-with input").Input("pear");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-slidefind-replace-all").Click());

        Assert.Equal("pear", Text(presentation.Slides[0].Shapes[0]));
        Assert.Equal("pear pie x | an pear pear", TableTests.Grid(table));
        _dialogs.WaitForAssertion(() => Assert.Contains("4", _dialogs.Find(".mud-ex-slidefind-status").TextContent));
        Assert.Equal("Replace All", presentation.Undo.UndoDescription);

        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.Same(rows, table.Rows);
        Assert.False(presentation.Undo.CanUndo);

        _dialogs.Find(".mud-ex-slidefind-what input").Input("an apple");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-slidefind-next").Click());
        Assert.Equal([3u], cut.Instance.SelectedShapes);
    }

    [Fact]
    public async Task FindNext_JumpsToSlideAndSelectsShape()
    {
        var presentation = Deck();
        var cut = RenderEditor(presentation);

        await Key(cut, "f", ctrl: true);
        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-slidefind-what input")));
        Assert.Empty(_dialogs.FindAll(".mud-ex-slidefind-with input"));
        _dialogs.Find(".mud-ex-slidefind-what input").Input("second");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-slidefind-next").Click());
        Assert.Same(presentation.Slides[1], cut.Instance.CurrentSlide);
        Assert.Equal([2u], cut.Instance.SelectedShapes);

        _dialogs.Find(".mud-ex-slidefind-what input").Input("APPLE");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-slidefind-next").Click());
        Assert.Same(presentation.Slides[1], cut.Instance.CurrentSlide);
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-slidefind-next").Click());
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-slidefind-next").Click());
        Assert.Same(presentation.Slides[0], cut.Instance.CurrentSlide);
        Assert.Equal([2u], cut.Instance.SelectedShapes);
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-slidefind-next").Click());
        Assert.Same(presentation.Slides[0], cut.Instance.CurrentSlide);
        Assert.Empty(cut.Instance.SelectedShapes);

        _dialogs.Find(".mud-ex-slidefind-what input").Input("missing");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-slidefind-next").Click());
        _dialogs.WaitForAssertion(() => Assert.Contains("missing", _dialogs.Find(".mud-ex-slidefind-status").TextContent));
        Assert.DoesNotMatch(@">\s+<", cut.Find(".mud-ex-slide-canvas .mud-ex-slide-paragraph").InnerHtml);
    }

    [Fact]
    public async Task CtrlM_AddsSlide()
    {
        var presentation = Deck();
        var cut = RenderEditor(presentation);

        await Key(cut, "m", ctrl: true);
        Assert.Equal(4, presentation.Slides.Count);
        Assert.Same(presentation.Slides[1], cut.Instance.CurrentSlide);
        Assert.Equal("New slide", presentation.Undo.UndoDescription);

        await Key(cut, "z", ctrl: true);
        Assert.Equal(3, presentation.Slides.Count);
        await Key(cut, "y", ctrl: true);
        Assert.Equal(4, presentation.Slides.Count);
        await Key(cut, "m", ctrl: true, target: ".mud-ex-slide-panel");
        Assert.Equal(5, presentation.Slides.Count);

        cut.FindAll(".mud-ex-slide-panel-item")[0].Click();
        await cut.InvokeAsync(() => Canvas(cut).SelectAsync([3]));
        await Key(cut, "PageDown");
        Assert.Same(presentation.Slides[0], cut.Instance.CurrentSlide);
        await cut.InvokeAsync(() => Canvas(cut).SelectAsync([]));
        await Key(cut, "PageDown");
        Assert.Same(presentation.Slides[1], cut.Instance.CurrentSlide);
    }
}
