using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.AnimationTests;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class SlideBuildStepsTests
{
    private static Slide Load(string groups, params uint[] shapes) => new PptxBuilder().Slide(Shapes(shapes), tail: Timing(groups)).Load().Slides[0];

    [Fact]
    public void BuildSteps_GroupsWithAndAfterPrevious()
    {
        var slide = RichDeck().Slides[0];

        var steps = SlideBuildSteps.For(slide);

        Assert.Equal(3, steps.Count);
        Assert.All(steps, s => Assert.False(s.Automatic));
        Assert.Equal([(2u, 0.0), (3u, 0.25), (4u, 1.0)], steps[0].Effects.Select(e => (e.Effect.ShapeId, e.StartSeconds)));
        Assert.Equal(1.5, steps[0].DurationSeconds, 6);
        Assert.Equal([(2u, 0.0)], steps[1].Effects.Select(e => (e.Effect.ShapeId, e.StartSeconds)));
        Assert.Equal([(3u, 0.0)], steps[2].Effects.Select(e => (e.Effect.ShapeId, e.StartSeconds)));
    }

    [Fact]
    public void BuildSteps_LeadingWithPrevious_IsAutomaticStep()
    {
        var slide = Load(Group(Slice(0, Effect("entr", 10, 0, "withEffect", 2, 500)), Slice(1000, Effect("entr", 10, 0, "afterEffect", 3))) +
                         Group(Slice(0, Effect("entr", 10, 0, "clickEffect", 4))), 2, 3, 4);

        var steps = SlideBuildSteps.For(slide);

        Assert.Equal([true, false], steps.Select(s => s.Automatic));
        Assert.Equal([(2u, 0.5), (3u, 1.0)], steps[0].Effects.Select(e => (e.Effect.ShapeId, e.StartSeconds)));
    }

    [Fact]
    public void InitiallyHidden_OnlyKnownEntrances()
    {
        var slide = Load(Group(Slice(0,
            Effect("entr", 10, 0, "clickEffect", 2),
            Effect("exit", 10, 0, "withEffect", 3),
            Effect("entr", 37, 0, "withEffect", 4),
            Effect("emph", 6, 0, "withEffect", 5),
            Effect("entr", 10, 0, "withEffect", 5),
            Effect("entr", 22, 4, "withEffect", 6, paragraph: 1))), 2, 3, 4, 5, 6);

        Assert.True(SlideBuildSteps.InitiallyHidden(slide).SetEquals([(2, null), (6, 1)]));
    }

    [Fact]
    public void UnknownEffect_ShapeStaysVisible()
    {
        var slide = Load(
            Group(Slice(0, Effect("entr", 10, 0, "clickEffect", 2), Effect("exit", 37, 0, "withEffect", 3))) +
            Group(Slice(0, Effect("exit", 37, 0, "clickEffect", 2))) +
            Group(Slice(0, Effect("exit", 10, 0, "clickEffect", 4))) +
            Group(Slice(0, Effect("entr", 37, 0, "clickEffect", 4))), 2, 3, 4);
        var steps = SlideBuildSteps.For(slide).Count;

        for (var n = 0; n <= steps; n++)
        {
            var state = SlideBuildSteps.VisibleAfter(slide, n);
            Assert.True(state[(3, null)], $"shape 3 after {n} steps");
            if (n >= 1) Assert.True(state[(2, null)], $"shape 2 after {n} steps");
        }

        Assert.False(SlideBuildSteps.VisibleAfter(slide, 0)[(2, null)]);
        Assert.False(SlideBuildSteps.VisibleAfter(slide, 3)[(4, null)]);
        Assert.True(SlideBuildSteps.VisibleAfter(slide, 4)[(4, null)]);
    }

    [Fact]
    public void VisibleAfter_StepBack_RestoresState()
    {
        var slide = RichDeck().Slides[0];
        var forward = Enumerable.Range(0, 4).Select(n => Text(SlideBuildSteps.VisibleAfter(slide, n))).ToList();

        Assert.Equal("2/:False 3/:False 4/1:False", forward[0]);
        Assert.Equal("2/:True 3/:True 4/1:True", forward[1]);
        Assert.Equal(forward[1], forward[2]);
        Assert.Equal("2/:True 3/:False 4/1:True", forward[3]);
        for (var n = 3; n > 0; n--) Assert.Equal(forward[n - 1], Text(SlideBuildSteps.VisibleAfter(slide, n - 1)));
        Assert.Equal(forward[3], Text(SlideBuildSteps.VisibleAfter(slide, 10)));
        Assert.Throws<ArgumentOutOfRangeException>(() => SlideBuildSteps.VisibleAfter(slide, -1));
    }

    private static string Text(IReadOnlyDictionary<(uint Shape, int? Paragraph), bool> state) =>
        string.Join(" ", state.OrderBy(e => e.Key.Shape).ThenBy(e => e.Key.Paragraph).Select(e => $"{e.Key.Shape}/{e.Key.Paragraph}:{e.Value}"));
}
