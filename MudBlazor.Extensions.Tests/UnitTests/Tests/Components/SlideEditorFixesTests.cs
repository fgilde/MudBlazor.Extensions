using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideEditorFixesTests : IAsyncLifetime
{
    private const long Inch = 914400;
    private const string PptxType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
    private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private readonly BunitContext _context = new();

    public Task InitializeAsync()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _context.Render<MudPopoverProvider>();
        _context.Render<MudDialogProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private sealed record Infos(string FileName, string ContentType, byte[] Bytes) : IMudExFileDisplayInfos
    {
        public string Url => null;
        public Stream ContentStream { get; } = new MemoryStream(Bytes);
    }

    private static string Picture(uint id, string rel) =>
        $"<p:pic><p:nvPicPr><p:cNvPr id=\"{id}\" name=\"Picture {id}\"/><p:cNvPicPr/><p:nvPr/></p:nvPicPr><p:blipFill><a:blip r:embed=\"{rel}\"/><a:stretch><a:fillRect/></a:stretch>" +
        $"</p:blipFill><p:spPr>{Xfrm(0, 0, Inch, Inch)}<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr></p:pic>";

    private static string ImageRel(string id, string target) => $"<Relationship Id=\"{id}\" Type=\"{RelNs}/image\" Target=\"{target}\"/>";

    private static Presentation TextDeck() => new PptxBuilder { NotesMaster = true }
        .Slide(Shape(2, "Text", Xfrm(Inch, Inch, 4 * Inch, Inch), "<a:p><a:r><a:t>Old</a:t></a:r></a:p>"), notes: "notes")
        .Slide(Shape(2, "Other", Xfrm(Inch, Inch, Inch, Inch), "<a:p><a:r><a:t>second</a:t></a:r></a:p>"))
        .Load();

    private IRenderedComponent<MudExSlideEditor> RenderEditor(Action<ComponentParameterCollectionBuilder<MudExSlideEditor>> configure)
    {
        var cut = _context.Render(configure);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-canvas .mud-ex-slide")));
        return cut;
    }

    private static string ShapeText(AutoShape shape) => string.Concat(shape.Text.Paragraphs.SelectMany(p => p.Runs).Select(r => r.Text));

    private async Task<MudExSlideTextEditor> StartTypingAsync(IRenderedComponent<MudExSlideEditor> cut, uint id, string draft)
    {
        var canvas = cut.FindComponent<MudExSlideCanvas>();
        await cut.InvokeAsync(() => canvas.Instance.OnSelectionAsync([id]));
        cut.Find(".mud-ex-slide-canvas").KeyDown(new KeyboardEventArgs { Key = draft[..1] });
        var editor = cut.FindComponent<MudExSlideTextEditor>().Instance;
        await cut.InvokeAsync(() => editor.OnTextInputAsync([new SlideTextParagraphDto { Runs = [new SlideTextRunDto { Text = draft }] }], null));
        return editor;
    }

    [Fact]
    public void Images_OnlyRasterTypesBecomeBlobs_DataUrisNeverCarryAForeignType()
    {
        var builder = new PptxBuilder().Slide(Picture(2, "rId5") + Picture(3, "rId6") + Picture(4, "rId7"),
            rels: ImageRel("rId5", "../media/image1.png") + ImageRel("rId6", "../media/image2.svg") + ImageRel("rId7", "../media/image3.png"));
        builder.ExtraParts["ppt/media/image1.png"] = (null, "png bytes");
        builder.ExtraParts["ppt/media/image2.svg"] = ("image/svg+xml", "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");
        builder.ExtraParts["ppt/media/image3.png"] = ("text/html", "<script>alert(1)</script>");
        var presentation = builder.Load();

        RenderEditor(p => p.Add(x => x.Presentation, presentation));

        var registered = _context.JSInterop.Invocations.Where(i => i.Identifier == "register").Select(i => (string)i.Arguments[1]).ToList();
        Assert.Equal(["image/png"], registered.Distinct());
        Assert.StartsWith("data:application/octet-stream;base64,", new SlideImageUrls(presentation).UrlFor("/ppt/media/image3.png"));
        Assert.StartsWith("data:image/svg+xml;base64,", new SlideImageUrls(presentation).UrlFor("/ppt/media/image2.svg"));
    }

    [Fact]
    public async Task Images_AreSentForTheShownSlidesOnly_ThumbnailsWhenVisible()
    {
        var builder = new PptxBuilder();
        for (var i = 1; i <= 12; i++)
        {
            builder.Slide(Picture(2, "rId5"), rels: ImageRel("rId5", $"../media/image{i}.png"));
            builder.ExtraParts[$"ppt/media/image{i}.png"] = (null, $"image {i}");
        }

        var presentation = builder.Load();
        _context.JSInterop.Setup<string>("register", _ => true).SetResult("blob:test");
        var cut = RenderEditor(p => p.Add(x => x.Presentation, presentation));
        int Registered() => _context.JSInterop.Invocations.Count(i => i.Identifier == "register");

        Assert.Equal(MudExSlidePanel.EagerThumbnails, Registered());

        await cut.InvokeAsync(() => cut.Instance.OnThumbnailsVisible([presentation.Slides[10].Key]));

        cut.WaitForAssertion(() => Assert.Equal(MudExSlidePanel.EagerThumbnails + 1, Registered()));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-panel-item")[10].QuerySelectorAll("img")));
    }

    [Fact]
    public async Task SlideShow_SendsTheImagesOfTheShownAndTheNextSlideOnly()
    {
        var builder = new PptxBuilder();
        for (var i = 1; i <= 12; i++)
        {
            builder.Slide(Picture(2, "rId5"), rels: ImageRel("rId5", $"../media/image{i}.png"));
            builder.ExtraParts[$"ppt/media/image{i}.png"] = (null, $"image {i}");
        }

        var presentation = builder.Load();
        _context.JSInterop.Setup<string>("register", _ => true).SetResult("blob:test");
        var cut = RenderEditor(p => p.Add(x => x.Presentation, presentation));
        int Registered() => _context.JSInterop.Invocations.Count(i => i.Identifier == "register");
        cut.FindAll(".mud-ex-slide-panel-item")[9].Click();
        cut.WaitForAssertion(() => Assert.Equal(MudExSlidePanel.EagerThumbnails + 1, Registered()));

        await cut.Find(".mud-ex-slide-editor").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "F5", ShiftKey = true });
        await cut.Find(".mud-ex-slide-editor").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "F5", ShiftKey = true });

        var show = cut.FindComponent<MudExSlideShow>();
        Assert.Equal(MudExSlidePanel.EagerThumbnails + 2, Registered());
        await show.Find(".mud-ex-slide-show").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "ArrowRight" });
        cut.WaitForAssertion(() => Assert.Equal("10", show.Find(".mud-ex-slide-show-in").GetAttribute("data-slide-index")));
        Assert.Equal(MudExSlidePanel.EagerThumbnails + 3, Registered());
        Assert.Same(presentation.Slides[9], cut.Instance.CurrentSlide);
        Assert.Single(cut.FindComponents<MudExSlideShow>());
    }

    [Fact]
    public void FontName_FromTheFile_CannotInjectCss()
    {
        var presentation = new PptxBuilder()
            .Slide(Shape(2, "Text", Xfrm(0, 0, Inch, Inch),
                "<a:p><a:r><a:rPr><a:latin typeface=\"x&#10;;background-image:url(https://tracker.test/x)\"/></a:rPr><a:t>T</a:t></a:r></a:p>"))
            .Load();

        var cut = _context.Render<MudExSlideView>(p => p.Add(c => c.Presentation, presentation).Add(c => c.Slide, presentation.Slides[0]));

        var style = cut.Find(".mud-ex-slide-text span").GetAttribute("style");
        Assert.DoesNotContain("url(", style);
        Assert.DoesNotContain("\n", style);
        Assert.Contains("font-family:\"xbackground-imageurlhttpstracker.testx\",sans-serif;", style);
    }

    [Fact]
    public async Task Save_WhileTyping_SavesTheLiveText_AndEndsClean()
    {
        var presentation = TextDeck();
        byte[] saved = null;
        _context.JSInterop.Setup<IJSStreamReference>("readData", _ => true)
            .SetResult(new JsonStreamReference(new[] { new SlideTextParagraphDto { Runs = [new SlideTextRunDto { Text = "Quarterly" }] } }));
        var cut = RenderEditor(p => p.Add(x => x.Presentation, presentation).Add(x => x.SaveRequested, (MudExFileSaveRequest r) =>
        {
            using var buffer = new MemoryStream();
            r.Content.Stream.CopyTo(buffer);
            saved = buffer.ToArray();
            r.Handled = true;
        }));
        await StartTypingAsync(cut, 2, "Q");
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));

        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));

        var reloaded = Presentation.Load(new MemoryStream(saved));
        Assert.Equal("Quarterly", ShapeText((AutoShape)reloaded.Slides[0].Shapes[0]));
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task Notes_FirstInput_MarksDirtyBeforeBlur_SaveEndsClean()
    {
        var presentation = TextDeck();
        var cut = RenderEditor(p => p.Add(x => x.Presentation, presentation).Add(x => x.SaveRequested, (MudExFileSaveRequest r) => r.Handled = true));

        cut.Find(".mud-ex-slide-notes textarea").Input("typed notes");

        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));
        Assert.False(presentation.Undo.CanUndo);
        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));
        Assert.Equal("typed notes", presentation.Slides[0].Notes);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task HostSwapsPresentation_ForgetsFileNameAndDirtyState()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Presentation", "sample.pptx"));
        var cut = RenderEditor(p => p.Add(x => x.FileDisplayInfos, new Infos("budget.pptx", PptxType, bytes)));
        cut.WaitForAssertion(() => Assert.Equal("budget.pptx", cut.Instance.FileName));
        await cut.InvokeAsync(() => cut.Instance.CurrentPresentation.Execute(new SetSlideHiddenCommand([cut.Instance.CurrentPresentation.Slides[0]], true)));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));
        var other = Presentation.Create();

        cut.Render(p => p.Add(x => x.Presentation, other));

        cut.WaitForAssertion(() => Assert.Same(other, cut.Instance.CurrentPresentation));
        Assert.Null(cut.Instance.FileName);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task RibbonCommand_WhileTyping_CommitsTheTextFirst()
    {
        var presentation = TextDeck();
        var cut = RenderEditor(p => p.Add(x => x.Presentation, presentation));
        await StartTypingAsync(cut, 2, "New text");

        await cut.InvokeAsync(() => (cut.FindComponent<MudExRibbon>().Instance.Commands.SingleOrDefault(c => c.Id == "pp-new-slide")
                                     ?? cut.FindComponents<MudExRibbonButton>().Select(b => b.Instance).Single(b => b.Id == "pp-new-slide")).ExecuteAsync());
        cut.WaitForAssertion(() => Assert.Equal(3, presentation.Slides.Count));
        Assert.Equal("New text", ShapeText((AutoShape)presentation.Slides[0].Shapes[0]));

        await cut.InvokeAsync(() => presentation.Undo.Undo());

        Assert.Equal(2, presentation.Slides.Count);
        Assert.Equal("New text", ShapeText((AutoShape)presentation.Slides[0].Shapes[0]));
    }

    [Fact]
    public async Task DisposeWhileTypingInNewTextBox_RecordsTheTextBox()
    {
        var presentation = TextDeck();
        var slide = presentation.Slides[0];
        var cut = _context.Render<MudExSlideCanvas>(p => p.Add(c => c.Presentation, presentation).Add(c => c.Slide, slide).Add(c => c.Images, new SlideImageUrls(presentation)));
        await cut.InvokeAsync(() => cut.Instance.InsertTextBoxAsync());
        await cut.InvokeAsync(() => cut.Instance.OnInsertAsync(Inch, Inch, 0, 0));
        var editor = cut.FindComponent<MudExSlideTextEditor>().Instance;
        await cut.InvokeAsync(() => editor.OnTextInputAsync([new SlideTextParagraphDto { Runs = [new SlideTextRunDto { Text = "Hi" }] }], null));
        Assert.Equal(2, slide.Shapes.Count);

        await _context.DisposeComponentsAsync();

        Assert.Equal("Hi", ShapeText((AutoShape)slide.Shapes[1]));
        Assert.True(presentation.Undo.CanUndo);
        presentation.Undo.Undo();
        Assert.Single(slide.Shapes);
    }

    [Fact]
    public async Task Connector_DoubleClick_DoesNotStartTextEditing()
    {
        var presentation = new PptxBuilder()
            .Slide("<p:cxnSp><p:nvCxnSpPr><p:cNvPr id=\"4\" name=\"Connector 3\"/><p:cNvCxnSpPr/><p:nvPr/></p:nvCxnSpPr><p:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/>" +
                   "<a:ext cx=\"100\" cy=\"0\"/></a:xfrm><a:prstGeom prst=\"line\"><a:avLst/></a:prstGeom></p:spPr></p:cxnSp>")
            .Load();
        var cut = _context.Render<MudExSlideCanvas>(p => p.Add(c => c.Presentation, presentation).Add(c => c.Slide, presentation.Slides[0]));

        await cut.InvokeAsync(() => cut.Instance.OnShapeDoubleClickAsync(4, 0, 0));

        Assert.False(cut.Instance.IsEditingText);
        Assert.Empty(cut.FindComponents<MudExSlideTextEditor>());
    }

    [Fact]
    public void EmptyPlaceholders_ShowPromptAndOutline_OnlyWhenInteractive()
    {
        var builder = new PptxBuilder
        {
            LayoutShapes = Placeholder(2, "Title 1", "type=\"title\" hasCustomPrompt=\"1\"", Xfrm(Inch, Inch, 8 * Inch, Inch), "<a:p><a:r><a:t>Type the headline</a:t></a:r></a:p>") +
                           Placeholder(3, "Content Placeholder 2", "idx=\"1\"", Xfrm(Inch, 3 * Inch, 8 * Inch, 3 * Inch))
        };
        var presentation = builder.Slide(Placeholder(2, "Title 1", "type=\"title\"", "") + Placeholder(3, "Content 2", "idx=\"1\"", "") +
                                         Placeholder(4, "Filled 3", "idx=\"1\"", "", "<a:p><a:r><a:t>filled</a:t></a:r></a:p>")).Load();

        IRenderedComponent<MudExSlideView> Render(bool interactive) => _context.Render<MudExSlideView>(p => p
            .Add(c => c.Presentation, presentation).Add(c => c.Slide, presentation.Slides[0]).Add(c => c.Interactive, interactive));

        var editing = Render(true);
        Assert.Equal(["2", "3"], editing.FindAll(".mud-ex-slide-placeholder-empty").Select(e => e.GetAttribute("data-shape-id")));
        Assert.Equal(["Type the headline", "Click to add text"], editing.FindAll(".mud-ex-slide-prompt").Select(e => e.TextContent.Trim()));
        var thumbnail = Render(false);
        Assert.Empty(thumbnail.FindAll(".mud-ex-slide-placeholder-empty"));
        Assert.Empty(thumbnail.FindAll(".mud-ex-slide-prompt"));
    }
}
