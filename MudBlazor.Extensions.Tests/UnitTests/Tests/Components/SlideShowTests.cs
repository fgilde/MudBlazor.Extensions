using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.AnimationTests;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideShowTests : IAsyncLifetime
{
    private readonly BunitContext _context = new();

    public Task InitializeAsync()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _context.Render<MudPopoverProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    internal static Presentation Deck(params bool[] hidden) =>
        hidden.Aggregate(new PptxBuilder(), (b, h) => b.Slide(Shapes(2, 3), h ? "show=\"0\"" : "", notes: "Notes")).Load();

    private static Presentation Animated(string groups) => new PptxBuilder().Slide(Shapes(2, 3), notes: "First notes", tail: Timing(groups)).Slide(Shapes(2), notes: "Second notes").Load();

    private IRenderedComponent<MudExSlideShow> RenderShow(Presentation presentation, int start = 0, bool presenter = false, Action closed = null, Action<int> changed = null) =>
        _context.Render<MudExSlideShow>(p => p.Add(x => x.Presentation, presentation).Add(x => x.StartIndex, start).Add(x => x.PresenterView, presenter)
            .Add(x => x.Closed, () => closed?.Invoke()).Add(x => x.SlideChanged, i => changed?.Invoke(i)));

    private static string Current(IRenderedComponent<MudExSlideShow> cut) => cut.Find(".mud-ex-slide-show-in").GetAttribute("data-slide-index");

    private static string ShapeStyle(IRenderedComponent<MudExSlideShow> cut, uint id) => cut.Find($".mud-ex-slide-show-in [data-shape-id='{id}']").GetAttribute("style");

    private static string ShapeClass(IRenderedComponent<MudExSlideShow> cut, uint id) => cut.Find($".mud-ex-slide-show-in [data-shape-id='{id}']").GetAttribute("class");

    private static Task Command(IRenderedComponent<MudExSlideShow> cut, string command, int number = 0) => cut.InvokeAsync(() => cut.Instance.OnShowCommand(command, number));

    [Fact]
    public void ShowState_SkipsHiddenSlides()
    {
        var slides = Deck(false, true, false, true).Slides;
        var state = new SlideShowState(slides, 0);

        Assert.Equal(0, state.SlideIndex);
        state.Next();
        Assert.Equal(2, state.SlideIndex);
        state.Next();
        Assert.True(state.IsEnd);
        state.Previous();
        Assert.Equal(2, state.SlideIndex);
        state.Previous();
        Assert.Equal(0, state.SlideIndex);
        state.Last();
        Assert.Equal(2, state.SlideIndex);
        state.First();
        Assert.Equal(0, state.SlideIndex);

        Assert.Equal(2, new SlideShowState(slides, 1).SlideIndex);
        Assert.Equal(2, new SlideShowState(slides, 3).SlideIndex);
        Assert.True(new SlideShowState(Deck(true).Slides, 0).IsEnd);
    }

    [Fact]
    public void ShowState_PreviousAtSlideStart_GoesToPreviousSlideFullyBuilt()
    {
        var slides = Animated(Group(Slice(0, Effect("entr", 10, 0, "clickEffect", 2))) + Group(Slice(0, Effect("entr", 10, 0, "clickEffect", 3)))).Slides;
        var state = new SlideShowState(slides, 0);

        state.Next();
        state.Next();
        Assert.Equal((0, 2), (state.SlideIndex, state.Step));
        state.Next();
        Assert.Equal((1, 0), (state.SlideIndex, state.Step));

        state.Previous();
        Assert.Equal((0, 2), (state.SlideIndex, state.Step));
        Assert.True(state.FullyBuilt);
        state.Previous();
        Assert.Equal((0, 1), (state.SlideIndex, state.Step));
        Assert.Equal([true, false], new[] { 2u, 3u }.Select(id => SlideBuildSteps.VisibleAfter(slides[0], state.Step)[(id, null)]));
    }

    [Fact]
    public async Task ShowState_NumberEnter_Jumps()
    {
        var presentation = Deck(false, false, true, false);
        var state = new SlideShowState(presentation.Slides, 0);
        state.GoTo(2);
        Assert.Equal(2, state.SlideIndex);
        state.GoTo(9);
        Assert.Equal(2, state.SlideIndex);

        var changed = new List<int>();
        var cut = RenderShow(presentation, changed: changed.Add);
        await Command(cut, "goto", 3);
        Assert.Equal("2", Current(cut));
        await Command(cut, "goto", 0);
        await Command(cut, "goto", 5);
        Assert.Equal("2", Current(cut));
        Assert.Equal([2], changed);
    }

    [Fact]
    public async Task Show_Escape_RaisesClosed()
    {
        var closed = 0;
        var cut = RenderShow(Deck(false, false), closed: () => closed++);

        await cut.Find(".mud-ex-slide-show").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal(0, closed);
        await cut.Find(".mud-ex-slide-show").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, closed);

        var closedFromEnd = 0;
        var end = RenderShow(Deck(false), closed: () => closedFromEnd++);
        await Command(end, "next");
        await Command(end, "exit");
        Assert.Equal(1, closedFromEnd);
    }

    [Fact]
    public async Task Show_UnknownEffectShape_IsVisible()
    {
        var cut = RenderShow(Animated(Group(Slice(0, Effect("entr", 37, 0, "clickEffect", 2), Effect("entr", 10, 0, "withEffect", 3)))));

        Assert.DoesNotContain("visibility:hidden", ShapeStyle(cut, 2));
        Assert.Contains("visibility:hidden", ShapeStyle(cut, 3));

        await Command(cut, "next");
        Assert.DoesNotContain("visibility:hidden", ShapeStyle(cut, 2));
        Assert.DoesNotContain("visibility:hidden", ShapeStyle(cut, 3));
        Assert.Contains("mud-ex-show-entr-fade", ShapeClass(cut, 3));

        await Command(cut, "previous");
        Assert.DoesNotContain("visibility:hidden", ShapeStyle(cut, 2));
    }

    [Fact]
    public async Task Show_EntranceShape_HiddenUntilStep()
    {
        var presentation = Animated(Group(Slice(0, Effect("entr", 10, 0, "clickEffect", 2, delay: 250, duration: 750))) +
                                    Group(Slice(0, Effect("exit", 10, 0, "clickEffect", 2))));
        var cut = RenderShow(presentation);

        Assert.Contains("visibility:hidden", ShapeStyle(cut, 2));
        Assert.DoesNotContain("mud-ex-show-anim", ShapeClass(cut, 2));
        Assert.DoesNotContain("visibility:hidden", ShapeStyle(cut, 3));

        await Command(cut, "next");
        Assert.DoesNotContain("visibility:hidden", ShapeStyle(cut, 2));
        Assert.Contains("mud-ex-show-entr-fade", ShapeClass(cut, 2));
        Assert.Contains("animation-delay:0.25s", ShapeStyle(cut, 2));
        Assert.Contains("animation-duration:0.75s", ShapeStyle(cut, 2));

        await Command(cut, "next");
        Assert.Contains("visibility:hidden", ShapeStyle(cut, 2));
        Assert.Contains("mud-ex-show-exit-fade", ShapeClass(cut, 2));

        await Command(cut, "previous");
        Assert.DoesNotContain("visibility:hidden", ShapeStyle(cut, 2));
        Assert.DoesNotContain("mud-ex-show-anim", ShapeClass(cut, 2));
        await Command(cut, "previous");
        Assert.Contains("visibility:hidden", ShapeStyle(cut, 2));
        Assert.Equal("0", Current(cut));
    }

    [Fact]
    public void Show_AutoAdvance_AfterSeconds()
    {
        var presentation = Deck(false, false);
        presentation.Slides[0].Transition = SlideTransition.None with { AdvanceAfterSeconds = 0.05 };
        var changed = new List<int>();

        var cut = RenderShow(presentation, changed: changed.Add);

        cut.WaitForAssertion(() => Assert.Equal("1", Current(cut)), TimeSpan.FromSeconds(5));
        Assert.Equal([1], changed);
    }

    [Fact]
    public async Task Show_EndScreen_AfterLastSlide()
    {
        var presentation = Deck(false, false);
        presentation.Slides[1].Transition = new SlideTransition(TransitionKind.Push, TransitionDirection.Left, 0.5, true, null);
        var closed = 0;
        var cut = RenderShow(presentation, closed: () => closed++);

        await Command(cut, "click");
        Assert.Contains("mud-ex-show-in-push-left", cut.Find(".mud-ex-slide-show-in").GetAttribute("class"));
        Assert.Contains("mud-ex-show-out-push-left", cut.Find(".mud-ex-slide-show-out").GetAttribute("class"));
        Assert.Contains("animation-duration:0.5s", cut.Find(".mud-ex-slide-show-in").GetAttribute("style"));

        await Command(cut, "next");
        Assert.Equal("End of slide show, click to exit.", cut.Find(".mud-ex-slide-show-end").TextContent.Trim());
        Assert.Empty(cut.FindAll(".mud-ex-slide-show-in"));

        await Command(cut, "previous");
        Assert.Equal("1", Current(cut));
        Assert.Empty(cut.FindAll(".mud-ex-slide-show-out"));
        await Command(cut, "next");
        Assert.Equal(0, closed);
        await Command(cut, "click");
        Assert.Equal(1, closed);
    }

    [Fact]
    public async Task Presenter_ShowsNextSlideAndNotes()
    {
        _context.JSInterop.Setup<bool>("openAudience", _ => true).SetResult(true);
        var presentation = Animated(Group(Slice(0, Effect("entr", 10, 0, "clickEffect", 2))));
        var cut = RenderShow(presentation, presenter: true);

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-show-presenter")));
        Assert.Empty(cut.FindAll(".mud-alert"));
        Assert.Equal("First notes", cut.Find(".mud-ex-slide-show-presenter-notes").TextContent.Trim());
        Assert.Contains("Slide 1 of 2", cut.Find(".mud-ex-slide-show-presenter-nav").TextContent);
        Assert.Equal("0", cut.Find(".mud-ex-slide-show-presenter-next").GetAttribute("data-slide-index"));
        Assert.DoesNotContain("visibility:hidden", cut.Find(".mud-ex-slide-show-presenter-next [data-shape-id='2']").GetAttribute("style"));
        Assert.Contains("visibility:hidden", ShapeStyle(cut, 2));
        Assert.NotEmpty(cut.FindAll(".mud-ex-slide-show-presenter-clock"));
        Assert.Equal("00:00:00", cut.Find(".mud-ex-slide-show-presenter-stopwatch").TextContent.Trim());

        await Command(cut, "next");
        Assert.Equal("1", cut.Find(".mud-ex-slide-show-presenter-next").GetAttribute("data-slide-index"));

        cut.Find(".mud-ex-slide-show-presenter-nav button[aria-label='Next']").Click();
        Assert.Equal("Second notes", cut.Find(".mud-ex-slide-show-presenter-notes").TextContent.Trim());
        Assert.Contains("Slide 2 of 2", cut.Find(".mud-ex-slide-show-presenter-nav").TextContent);
        Assert.Equal("End of slide show", cut.Find(".mud-ex-slide-show-presenter-next").TextContent.Trim());

        var size = cut.Find(".mud-ex-slide-show-presenter-notes").GetAttribute("style");
        cut.Find("button[aria-label='Larger notes']").Click();
        Assert.NotEqual(size, cut.Find(".mud-ex-slide-show-presenter-notes").GetAttribute("style"));
    }

    [Fact]
    public async Task Presenter_PopupBlocked_ShowsAlertAndContinues()
    {
        var cut = RenderShow(Deck(false, false), presenter: true);

        cut.WaitForAssertion(() => Assert.Contains("blocked", cut.Find(".mud-alert").TextContent));
        Assert.Empty(cut.FindAll(".mud-ex-slide-show-presenter"));
        Assert.Equal("0", Current(cut));

        await Command(cut, "next");
        Assert.Equal("1", Current(cut));
    }

    [Fact]
    public async Task Editor_F5_StartsShow_AlsoReadOnly()
    {
        var presentation = Deck(false, false, false);
        var cut = _context.Render<MudExSlideEditor>(p => p.Add(x => x.Presentation, presentation).Add(x => x.ReadOnly, true));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-canvas .mud-ex-slide")));
        cut.FindAll(".mud-ex-slide-panel-item")[2].Click();

        await cut.Find(".mud-ex-slide-editor").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "F5" });
        cut.WaitForAssertion(() => Assert.Equal(0, cut.FindComponent<MudExSlideShow>().Instance.StartIndex));
        await cut.Find(".mud-ex-slide-show").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindComponents<MudExSlideShow>());

        await cut.Find(".mud-ex-slide-editor").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "F5", ShiftKey = true });
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindComponent<MudExSlideShow>().Instance.StartIndex));
        await Command(cut.FindComponent<MudExSlideShow>(), "previous");
        await Command(cut.FindComponent<MudExSlideShow>(), "exit");
        Assert.Empty(cut.FindComponents<MudExSlideShow>());
        Assert.Same(presentation.Slides[1], cut.Instance.CurrentSlide);

        cut.Find(".mud-ex-slide-status-bar button[aria-label='Slide Show']").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, cut.FindComponent<MudExSlideShow>().Instance.StartIndex));
        await Command(cut.FindComponent<MudExSlideShow>(), "exit");

        await cut.InvokeAsync(() => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == "pp-show-from-beginning").ExecuteAsync());
        cut.WaitForAssertion(() => Assert.Equal(0, cut.FindComponent<MudExSlideShow>().Instance.StartIndex));
    }
}
