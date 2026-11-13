using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideDesignTests : IAsyncLifetime
{
    private readonly BunitContext _context = new();
    private IRenderedComponent<MudPopoverProvider> _popovers = null!;
    private IRenderedComponent<MudDialogProvider> _dialogs = null!;

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

    private static Presentation Sample() =>
        Presentation.Load(new MemoryStream(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Presentation", "sample.pptx"))));

    private static Presentation LayoutDeck() => LayoutCommandTests.Builder()
        .Slide(Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>Hello</a:t></a:r></a:p>") +
               Placeholder(3, "Content Placeholder 2", "idx=\"1\"", "", "<a:p><a:r><a:t>Body</a:t></a:r></a:p>"))
        .Slide(Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>Second</a:t></a:r></a:p>"))
        .Load();

    private IRenderedComponent<MudExSlideEditor> RenderEditor(Presentation presentation)
    {
        var cut = _context.Render<MudExSlideEditor>(p => p.Add(x => x.Presentation, presentation));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-panel-item .mud-ex-slide")));
        return cut;
    }

    private static Task Run(IRenderedComponent<MudExSlideEditor> cut, string id)
        => cut.InvokeAsync(() => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).ExecuteAsync());

    private static void Tab(IRenderedComponent<MudExSlideEditor> cut, string tab) => cut.FindAll("[role='tab']").First(t => t.TextContent.Trim() == tab).Click();

    private static void Open(IRenderedComponent<MudExSlideEditor> cut, string label, string css = "button") =>
        cut.WaitForAssertion(() => cut.FindAll(css).First(b => b.GetAttribute("aria-label") == label).Click());

    private void Choose(string css, string title) =>
        _popovers.WaitForAssertion(() => _popovers.FindAll(css).Single(i => i.GetAttribute("title") == title).Click());

    [Fact]
    public void Design_ApplyFromGallery_RerendersAllThumbnails()
    {
        var presentation = Sample();
        var cut = RenderEditor(presentation);
        var thumbnails = cut.FindComponent<MudExSlidePanel>().FindComponents<MudExSlideView>();
        var before = thumbnails.Select(t => t.RenderCount).ToArray();
        var ocean = SlideDesigns.BuiltIn.Single(d => d.Name == "Ocean");

        Tab(cut, "Design");
        Assert.Empty(_popovers.FindAll(".mud-ex-slide-design-item"));
        Open(cut, "Themes");
        _popovers.WaitForAssertion(() => Assert.Equal(6, _popovers.FindAll(".mud-ex-slide-design-item .mud-ex-slide").Count));
        Assert.Empty(_popovers.FindAll(".mud-ex-slide-design-item[aria-selected='true']"));
        Choose(".mud-ex-slide-design-item", "Ocean");

        var master = presentation.Masters[0];
        Assert.Equal(ocean.Colors, master.Theme.Colors);
        Assert.Equal((ocean.MajorFont, ocean.MinorFont), (master.Theme.MajorFont, master.Theme.MinorFont));
        Assert.Equal("Apply design", presentation.Undo.UndoDescription);
        cut.WaitForAssertion(() => Assert.All(thumbnails.Select((t, i) => (t, i)), x => Assert.True(x.t.RenderCount > before[x.i], $"thumbnail {x.i}")));

        Open(cut, "Themes");
        _popovers.WaitForAssertion(() => Assert.Equal("Ocean", _popovers.Find(".mud-ex-slide-design-item[aria-selected='true']").GetAttribute("title")));
    }

    [Fact]
    public void Design_ColorsOnly_KeepsFonts()
    {
        var presentation = Sample();
        var master = presentation.Masters[0];
        var (major, minor, background) = (master.Theme.MajorFont, master.Theme.MinorFont, master.Background);
        var slate = SlideDesigns.BuiltIn.Single(d => d.Name == "Slate");
        var cut = RenderEditor(presentation);

        Tab(cut, "Design");
        Open(cut, "Colors");
        _popovers.WaitForAssertion(() => Assert.Equal(12 * 6, _popovers.FindAll(".mud-ex-slide-design-swatch").Count));
        Choose(".mud-ex-ribbon-gallery-item", "Slate");

        Assert.Equal(slate.Colors, master.Theme.Colors);
        Assert.Equal((major, minor), (master.Theme.MajorFont, master.Theme.MinorFont));
        Assert.Equal(background, master.Background);
        Assert.Equal("Theme colors", presentation.Undo.UndoDescription);

        Open(cut, "Fonts");
        _popovers.WaitForAssertion(() => Assert.Contains("monospace", _popovers.FindAll(".mud-ex-slide-design-font").First(f => f.TextContent.Contains("Consolas")).GetAttribute("style")));
        Choose(".mud-ex-ribbon-gallery-item", "Mono");

        Assert.Equal(("Consolas", "Segoe UI"), (master.Theme.MajorFont, master.Theme.MinorFont));
        Assert.Equal(slate.Colors, master.Theme.Colors);
        Assert.Equal("Theme fonts", presentation.Undo.UndoDescription);
    }

    [Fact]
    public async Task Design_SlideSize_4to3_ScalesContent()
    {
        var presentation = SlideSizeTests.Builder().Load();
        var box = presentation.Slides[0].Shapes[1];
        var cut = RenderEditor(presentation);
        bool? Checked(string id) => ((MudExRibbonMenuItem)cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id)).Checked;
        Assert.Equal(true, Checked("pp-slide-size-widescreen"));

        await Run(cut, "pp-slide-size-standard");

        Assert.Equal((9144000L, 6858000L), (presentation.SlideWidth, presentation.SlideHeight));
        Assert.Equal(new ShapeTransform(685800, 685800 + 857250, 685800, 685800), box.Transform);
        Assert.Equal("Slide size", presentation.Undo.UndoDescription);
        cut.WaitForAssertion(() => Assert.Contains("aspect-ratio:9144000/6858000", cut.Find(".mud-ex-slide-panel-thumb").GetAttribute("style")));
        Assert.Contains("aspect-ratio:9144000/6858000", cut.Find(".mud-ex-slide-canvas .mud-ex-slide").GetAttribute("style"));
        Assert.Equal(true, Checked("pp-slide-size-standard"));

        await Run(cut, "pp-slide-size-standard");
        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.Equal(12192000, presentation.SlideWidth);
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public async Task Layout_ChangeFromGallery_KeepsTitleText()
    {
        var presentation = LayoutDeck();
        var slide = presentation.Slides[0];
        var titleOnly = presentation.Layouts.Single(l => l.Name == "Title Only");
        var cut = RenderEditor(presentation);

        Open(cut, "Layout");
        _popovers.WaitForAssertion(() => Assert.Equal(4, _popovers.FindAll(".mud-ex-slide-layout-item").Count));
        Assert.Equal("Title and Content", _popovers.Find(".mud-ex-slide-layout-item[aria-selected='true']").GetAttribute("title"));
        Assert.Equal(4, _popovers.FindAll(".mud-ex-slide-layout-item .mud-ex-slide").Count);
        Assert.Equal(2, _popovers.FindAll(".mud-ex-slide-layout-item")[0].QuerySelectorAll(".mud-ex-slide-layout-placeholder").Length);
        Choose(".mud-ex-slide-layout-item", "Title Only");

        Assert.Same(titleOnly, slide.Layout);
        Assert.Equal("Hello", ((AutoShape)slide.Shapes.Single(s => s.Placeholder?.Type == "title")).Text!.Paragraphs[0].Runs[0].Text);
        Assert.Equal("Change layout", presentation.Undo.UndoDescription);
        cut.WaitForAssertion(() => Assert.Contains("Hello", cut.Find(".mud-ex-slide-canvas").TextContent));

        var title = slide.Shapes.Single(s => s.Placeholder?.Type == "title");
        await cut.InvokeAsync(() => presentation.Execute(new SetTransformsCommand(slide, new Dictionary<uint, ShapeTransform> { [title.Id] = new ShapeTransform(1, 2, 3, 4) })));
        await Run(cut, "pp-reset-slide");
        Assert.Null(title.Transform);
        Assert.Equal("Reset slide", presentation.Undo.UndoDescription);

        Open(cut, "More options: New Slide", ".mud-ex-ribbon-split-arrow");
        Choose(".mud-ex-slide-layout-item", "Two Content");
        Assert.Equal(3, presentation.Slides.Count);
        Assert.Equal("Two Content", presentation.Slides[1].Layout!.Name);
        Assert.Same(presentation.Slides[1], cut.Instance.CurrentSlide);
    }

    [Fact]
    public void HeaderFooter_ApplyToAll_AddsSlideNumbers()
    {
        var presentation = LayoutDeck();
        var cut = RenderEditor(presentation);

        Tab(cut, "Insert");
        Open(cut, "Header & Footer");
        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-slide-hf-number input")));
        _dialogs.Find(".mud-ex-slide-hf-number input").Change(true);
        _dialogs.Find(".mud-ex-slide-hf-footer input").Change(true);
        _dialogs.Find(".mud-ex-slide-hf-footer-text input").Change("Confidential");
        _dialogs.Find(".mud-ex-slide-hf-apply-all").Click();

        cut.WaitForAssertion(() => Assert.All(presentation.Slides, s => Assert.Contains(s.Shapes, x => x.Placeholder?.Type == "sldNum")));
        Assert.All(presentation.Slides, s => Assert.Equal("Confidential", HeaderFooterOptions.From(s).FooterText));
        Assert.Equal("Header and footer", presentation.Undo.UndoDescription);
        cut.WaitForAssertion(() => Assert.Empty(_dialogs.FindAll(".mud-ex-slide-hf-number")));

        Open(cut, "Header & Footer");
        _dialogs.WaitForAssertion(() => Assert.Equal("Confidential", _dialogs.Find(".mud-ex-slide-hf-footer-text input").GetAttribute("value")));
        _dialogs.Find(".mud-ex-slide-hf-number input").Change(false);
        _dialogs.Find(".mud-ex-slide-hf-apply").Click();
        cut.WaitForAssertion(() => Assert.DoesNotContain(presentation.Slides[0].Shapes, x => x.Placeholder?.Type == "sldNum"));
        Assert.Contains(presentation.Slides[1].Shapes, x => x.Placeholder?.Type == "sldNum");
    }
}
