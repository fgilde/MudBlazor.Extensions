using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class AnimationTests
{
    internal static string Target(uint spid, int? paragraph = null) =>
        $"<p:tgtEl><p:spTgt spid=\"{spid}\">{(paragraph is { } p ? $"<p:txEl><p:pRg st=\"{p}\" end=\"{p}\"/></p:txEl>" : "")}</p:spTgt></p:tgtEl>";

    internal static string Effect(string presetClass, int presetId, int subtype, string nodeType, uint spid, int delay = 0, int duration = 500, int? paragraph = null, int grpId = 0) =>
        $"<p:par><p:cTn presetID=\"{presetId}\" presetClass=\"{presetClass}\" presetSubtype=\"{subtype}\" fill=\"hold\" grpId=\"{grpId}\" nodeType=\"{nodeType}\">" +
        $"<p:stCondLst><p:cond delay=\"{delay}\"/></p:stCondLst><p:childTnLst>" +
        $"<p:animEffect transition=\"in\" filter=\"fade\"><p:cBhvr><p:cTn dur=\"{duration}\"/>{Target(spid, paragraph)}</p:cBhvr></p:animEffect>" +
        "</p:childTnLst></p:cTn></p:par>";

    internal static string Slice(int delay, params string[] effects) =>
        $"<p:par><p:cTn fill=\"hold\"><p:stCondLst><p:cond delay=\"{delay}\"/></p:stCondLst><p:childTnLst>{string.Concat(effects)}</p:childTnLst></p:cTn></p:par>";

    internal static string Group(params string[] slices) =>
        $"<p:par><p:cTn fill=\"hold\"><p:stCondLst><p:cond delay=\"indefinite\"/></p:stCondLst><p:childTnLst>{string.Concat(slices)}</p:childTnLst></p:cTn></p:par>";

    internal static string Interactive(uint trigger, string group) =>
        $"<p:seq concurrent=\"1\" nextAc=\"seek\"><p:cTn dur=\"indefinite\" nodeType=\"interactiveSeq\"><p:childTnLst>{group}</p:childTnLst></p:cTn>" +
        $"<p:nextCondLst><p:cond evt=\"onClick\" delay=\"0\"><p:tgtEl><p:spTgt spid=\"{trigger}\"/></p:tgtEl></p:cond></p:nextCondLst></p:seq>";

    internal static string Timing(string groups, string interactive = "", string builds = "") =>
        "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr><p:timing><p:tnLst><p:par><p:cTn id=\"1\" dur=\"indefinite\" restart=\"never\" nodeType=\"tmRoot\"><p:childTnLst>" +
        $"<p:seq concurrent=\"1\" nextAc=\"seek\"><p:cTn id=\"2\" dur=\"indefinite\" nodeType=\"mainSeq\"><p:childTnLst>{groups}</p:childTnLst></p:cTn>" +
        "<p:prevCondLst><p:cond evt=\"onPrev\" delay=\"0\"><p:tgtEl><p:sldTgt/></p:tgtEl></p:cond></p:prevCondLst>" +
        "<p:nextCondLst><p:cond evt=\"onNext\" delay=\"0\"><p:tgtEl><p:sldTgt/></p:tgtEl></p:cond></p:nextCondLst></p:seq>" +
        $"{interactive}</p:childTnLst></p:cTn></p:par></p:tnLst>{(builds.Length == 0 ? "" : $"<p:bldLst>{builds}</p:bldLst>")}</p:timing>";

    internal static string Shapes(params uint[] ids) =>
        string.Concat(ids.Select(id => Shape(id, $"Shape {id}", Xfrm(id * 100000, 0, 50000, 50000), "<a:p><a:r><a:t>One</a:t></a:r></a:p><a:p><a:r><a:t>Two</a:t></a:r></a:p>")));

    internal static readonly string RichTiming = Timing(
        Group(
            Slice(0, Effect("entr", 10, 0, "clickEffect", 2), Effect("entr", 2, 8, "withEffect", 3, 250, 750)),
            Slice(1000, Effect("entr", 22, 4, "afterEffect", 4, paragraph: 1))) +
        Group(Slice(0, Effect("emph", 6, 0, "clickEffect", 2, duration: 2000))) +
        Group(Slice(0, Effect("exit", 10, 0, "clickEffect", 3, grpId: 1))),
        Interactive(4, Group(Slice(0, Effect("entr", 1, 0, "clickEffect", 5, duration: 1)))),
        "<p:bldP spid=\"2\" grpId=\"0\" animBg=\"1\"/><p:bldP spid=\"3\" grpId=\"0\"/><p:bldP spid=\"3\" grpId=\"1\"/><p:bldP spid=\"4\" grpId=\"0\" build=\"p\"/>");

    internal static Presentation RichDeck() => new PptxBuilder().Slide(Shapes(2, 3, 4, 5), tail: RichTiming).Slide(Shapes(2, 3)).Load();

    private static AnimationEffect Fade(uint shape, AnimationTrigger trigger = AnimationTrigger.OnClick) =>
        new(shape, AnimationClass.Entrance, AnimationPreset.Fade, trigger, 0, 0.5, AnimationDirection.None, null);

    private static readonly Dictionary<string, Func<Presentation, IDocumentCommand<Presentation>>> Cases = new()
    {
        ["Add"] = p => new AddAnimationCommand(p.Slides[0], Fade(5)),
        ["AddAtIndex"] = p => new AddAnimationCommand(p.Slides[0], Fade(5, AnimationTrigger.WithPrevious), 1),
        ["AddToSlideWithoutTiming"] = p => new AddAnimationCommand(p.Slides[1], Fade(3)),
        ["Remove"] = p => new RemoveAnimationCommand(p.Slides[0], 1),
        ["MoveDown"] = p => new MoveAnimationCommand(p.Slides[0], 0, 3),
        ["MoveUp"] = p => new MoveAnimationCommand(p.Slides[0], 4, 0),
        ["SetTiming"] = p => new SetAnimationCommand(p.Slides[0], 3, p.Slides[0].Animations[3] with { Trigger = AnimationTrigger.AfterPrevious, DelaySeconds = 1 }),
        ["SetPreset"] = p => new SetAnimationCommand(p.Slides[0], 0, Fade(2) with { Preset = AnimationPreset.FlyIn, Direction = AnimationDirection.FromTop }),
        ["SetTransition"] = p => new SetTransitionCommand([p.Slides[0], p.Slides[1]], new SlideTransition(TransitionKind.Push, TransitionDirection.Up, 1, false, 2))
    };

    public static TheoryData<string> CommandNames => new(Cases.Keys);

    [Theory]
    [MemberData(nameof(CommandNames))]
    public void Command_ApplyThenRevert_RestoresModel(string name)
    {
        var presentation = RichDeck();
        var before = SlideCommandTests.Snapshot(presentation);
        var command = Cases[name](presentation);

        command.Apply(presentation);
        var applied = SlideCommandTests.Snapshot(presentation);
        Assert.NotEqual(before, applied);
        command.Revert(presentation);
        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
        command.Apply(presentation);
        Assert.Equal(applied, SlideCommandTests.Snapshot(presentation));
    }

    [Fact]
    public void Reader_Animations_MainSequenceOrderTriggersAndParagraphs()
    {
        var animations = RichDeck().Slides[0].Animations;

        Assert.Equal(
        [
            new AnimationEffect(2, AnimationClass.Entrance, AnimationPreset.Fade, AnimationTrigger.OnClick, 0, 0.5, AnimationDirection.None, null).ToString(),
            new AnimationEffect(3, AnimationClass.Entrance, AnimationPreset.FlyIn, AnimationTrigger.WithPrevious, 0.25, 0.75, AnimationDirection.FromLeft, null).ToString(),
            new AnimationEffect(4, AnimationClass.Entrance, AnimationPreset.Wipe, AnimationTrigger.AfterPrevious, 0, 0.5, AnimationDirection.FromBottom, 1).ToString(),
            new AnimationEffect(2, AnimationClass.Emphasis, AnimationPreset.Other, AnimationTrigger.OnClick, 0, 2, AnimationDirection.None, null).ToString(),
            new AnimationEffect(3, AnimationClass.Exit, AnimationPreset.Fade, AnimationTrigger.OnClick, 0, 0.5, AnimationDirection.None, null).ToString()
        ], animations.Select(a => a.ToString()));
        Assert.Empty(RichDeck().Slides[1].Animations);
    }

    [Fact]
    public void Reader_InteractiveSequence_NotModelled()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2, 5), tail: Timing("", Interactive(2, Group(Slice(0, Effect("entr", 1, 0, "clickEffect", 5)))))).Load();

        Assert.Empty(presentation.Slides[0].Animations);
        Assert.DoesNotContain(RichDeck().Slides[0].Animations, a => a.ShapeId == 5);
    }

    [Fact]
    public void Reader_ZoomPresets_AndUnknownEntrance()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2),
            tail: Timing(Group(Slice(0, Effect("entr", 23, 16, "clickEffect", 2), Effect("exit", 53, 16, "withEffect", 2), Effect("entr", 37, 0, "withEffect", 2),
                Effect("entr", 10, 0, "withEffect", 2, paragraph: 0).Replace("end=\"0\"", "end=\"1\""))))).Load();

        Assert.Equal([AnimationPreset.Zoom, AnimationPreset.Zoom, AnimationPreset.Other, AnimationPreset.Other], presentation.Slides[0].Animations.Select(a => a.Preset));
        Assert.Null(presentation.Slides[0].Animations[3].ParagraphIndex);
    }

    [Fact]
    public void AddAnimation_UnsupportedPreset_Throws()
    {
        var presentation = RichDeck();
        var slide = presentation.Slides[0];
        var before = SlideCommandTests.Snapshot(presentation);

        foreach (var effect in new[]
                 {
                     Fade(2) with { Class = AnimationClass.Emphasis },
                     Fade(2) with { Class = AnimationClass.Path },
                     Fade(2) with { Preset = AnimationPreset.Other },
                     Fade(2) with { ParagraphIndex = 0 },
                     Fade(2) with { Preset = AnimationPreset.FlyIn },
                     Fade(2) with { Direction = AnimationDirection.FromLeft },
                     Fade(2) with { DurationSeconds = double.NaN },
                     Fade(2) with { DelaySeconds = -1 },
                     Fade(99)
                 })
            Assert.ThrowsAny<ArgumentException>(() => presentation.Execute(new AddAnimationCommand(slide, effect)));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new AddAnimationCommand(slide, Fade(2), 6)));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new SetAnimationCommand(slide, 3, slide.Animations[3] with { DurationSeconds = 1 })));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new RemoveAnimationCommand(slide, 5)));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new MoveAnimationCommand(slide, 0, 5)));

        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public void AnimationCommands_NoChange_RecordNoUndoStep()
    {
        var presentation = RichDeck();
        var slide = presentation.Slides[0];

        presentation.Execute(new MoveAnimationCommand(slide, 2, 2));
        presentation.Execute(new SetAnimationCommand(slide, 1, slide.Animations[1]));
        presentation.Execute(new SetTransitionCommand([slide], slide.Transition));

        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public void SetAnimation_ValueEqualRecordWithoutSource_IsNoOp()
    {
        var presentation = RichDeck();
        var slide = presentation.Slides[0];
        var before = SlideCommandTests.Snapshot(presentation);

        foreach (var index in new[] { 0, 2, 3 })
        {
            var e = slide.Animations[index];
            presentation.Execute(new SetAnimationCommand(slide, index, new AnimationEffect(e.ShapeId, e.Class, e.Preset, e.Trigger, e.DelaySeconds, e.DurationSeconds, e.Direction, e.ParagraphIndex)));
        }

        Assert.False(presentation.Undo.CanUndo);
        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
        Assert.Equal(slide.Animations[0], slide.Animations[0] with { Source = null });
    }

    [Fact]
    public void SetAnimation_FreshRecordChangingTiming_KeepsSource()
    {
        var presentation = RichDeck();
        var slide = presentation.Slides[0];
        var other = slide.Animations[3];

        presentation.Execute(new SetAnimationCommand(slide, 3, new AnimationEffect(other.ShapeId, other.Class, other.Preset, AnimationTrigger.WithPrevious, 1, other.DurationSeconds, other.Direction, null)));

        Assert.True(presentation.Undo.CanUndo);
        Assert.Same(other.Source, slide.Animations[3].Source);
        Assert.Equal(AnimationTrigger.WithPrevious, slide.Animations[3].Trigger);
    }

    [Fact]
    public void Animations_OfDeletedShape_AreLeftOut_UndoBringsThemBack()
    {
        var presentation = RichDeck();
        var slide = presentation.Slides[0];

        presentation.Execute(new DeleteShapesCommand(slide, [3]));
        Assert.Equal([2u, 4u, 2u], slide.Animations.Select(a => a.ShapeId));
        presentation.Execute(new RemoveAnimationCommand(slide, 1));
        Assert.Equal([2u, 2u], slide.Animations.Select(a => a.ShapeId));
        presentation.Undo.Undo();
        presentation.Undo.Undo();

        Assert.Equal([2u, 3u, 4u, 2u, 3u], slide.Animations.Select(a => a.ShapeId));
    }

    [Fact]
    public void InsertShape_AfterDeletingNewlyAnimatedShape_DoesNotReuseItsId()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2)).Load();
        var slide = presentation.Slides[0];
        var first = AutoShape.CreateTextBox(new ShapeTransform(0, 0, 10, 10), null);
        presentation.Execute(new InsertShapeCommand(slide, first));
        presentation.Execute(new AddAnimationCommand(slide, Fade(first.Id)));
        presentation.Execute(new DeleteShapesCommand(slide, [first.Id]));

        var second = AutoShape.CreateTextBox(new ShapeTransform(0, 0, 10, 10), null);
        presentation.Execute(new InsertShapeCommand(slide, second));

        Assert.NotEqual(first.Id, second.Id);
        Assert.Empty(slide.Animations);
    }

    [Fact]
    public void DuplicateSlide_CopiesTransitionAndAnimations()
    {
        var presentation = RichDeck();
        var slide = presentation.Slides[0];
        presentation.Execute(new SetTransitionCommand([slide], new SlideTransition(TransitionKind.Fade, TransitionDirection.None, 1, true, null)));
        var command = new DuplicateSlidesCommand([slide]);

        presentation.Execute(command);

        Assert.Equal(slide.Transition, command.Copies[0].Transition);
        Assert.Equal(slide.Animations, command.Copies[0].Animations);
    }
}
