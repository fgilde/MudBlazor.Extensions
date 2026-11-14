using System.Xml.Linq;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.AnimationTests;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public partial class PptxWriterTests
{
    private static XElement SlideXml(byte[] package, int n = 1) => Xml(package, $"/ppt/slides/slide{n}.xml").Root!;

    private static IEnumerable<XElement> EffectNodes(XElement slide) =>
        slide.Descendants(P + "cTn").Where(c => c.Attribute("presetClass") != null && c.Ancestors(P + "cTn").Any(a => (string)a.Attribute("nodeType") == "mainSeq")).Select(c => c.Parent!);

    private static XElement Stripped(XElement effect)
    {
        var copy = new XElement(effect);
        foreach (var tn in copy.DescendantsAndSelf(P + "cTn")) tn.SetAttributeValue("id", null);
        copy.Element(P + "cTn")!.SetAttributeValue("nodeType", null);
        copy.Element(P + "cTn")!.Element(P + "stCondLst")?.Remove();
        return copy;
    }

    [Fact]
    public void Writer_SetTransition_ReplacesAlternateContent_AtSchemaPosition()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2, 3), tail: AnimationTests.Timing(Group(Slice(0, AnimationTests.Effect("entr", 10, 0, "clickEffect", 2)))).Replace("</p:clrMapOvr>", "</p:clrMapOvr>" + TransitionTests.Alternate)).Load();
        var source = Xml(new PptxBuilder().Slide(Shapes(2, 3), tail: AnimationTests.Timing(Group(Slice(0, AnimationTests.Effect("entr", 10, 0, "clickEffect", 2))))).Build(), "/ppt/slides/slide1.xml").Root!;
        var fade = new SlideTransition(TransitionKind.Fade, TransitionDirection.None, 0.75, false, 3);

        presentation.Execute(new SetTransitionCommand([presentation.Slides[0]], fade));
        var output = Save(presentation);

        AssertConsistent(output);
        var slide = SlideXml(output);
        Assert.Equal(["cSld", "clrMapOvr", "transition", "timing"], slide.Elements().Select(e => e.Name.LocalName));
        Assert.Empty(slide.Descendants(Mc + "AlternateContent"));
        var transition = slide.Element(P + "transition")!;
        Assert.Equal("med", (string)transition.Attribute("spd"));
        Assert.Equal("0", (string)transition.Attribute("advClick"));
        Assert.Equal("3000", (string)transition.Attribute("advTm"));
        Assert.Equal([P + "fade"], transition.Elements().Select(e => e.Name));
        Assert.True(XNode.DeepEquals(source.Element(P + "timing"), slide.Element(P + "timing")));
        Assert.Equal(fade, Load(output).Slides[0].Transition);
    }

    [Fact]
    public void Writer_SetTransitionTiming_KeepsAlternateContent()
    {
        var presentation = TransitionTests.Deck(TransitionTests.Alternate);
        var slide = presentation.Slides[0];
        var changed = slide.Transition with { AdvanceAfterSeconds = 5 };

        presentation.Execute(new SetTransitionCommand([slide], changed));
        var output = Save(presentation);

        AssertConsistent(output);
        var alternate = Assert.Single(SlideXml(output).Elements(Mc + "AlternateContent"));
        var choice = alternate.Element(Mc + "Choice")!.Element(P + "transition")!;
        var fallback = alternate.Element(Mc + "Fallback")!.Element(P + "transition")!;
        Assert.NotNull(choice.Element(P14 + "vortex"));
        Assert.Equal("2000", (string)choice.Attribute(P14 + "dur"));
        Assert.Equal(["5000", "5000"], new[] { choice, fallback }.Select(t => (string)t.Attribute("advTm")));
        Assert.Equal(changed, Load(output).Slides[0].Transition);

        presentation.Execute(new SetTransitionCommand([slide], changed with { DurationSeconds = 0.5 }));
        choice = SlideXml(Save(presentation)).Descendants(Mc + "Choice").Single().Element(P + "transition")!;
        Assert.Null(choice.Attribute(P14 + "dur"));
        Assert.Null(choice.Attribute("spd"));
        Assert.NotNull(choice.Element(P14 + "vortex"));
    }

    [Fact]
    public void Reader_Transition_EmptyDirection_DefaultsToLeft()
    {
        Assert.Equal(TransitionDirection.Left, TransitionTests.Deck("<p:transition><p:push dir=\"\"/></p:transition>").Slides[0].Transition.Direction);
    }

    [Fact]
    public void Writer_TransitionNone_RemovesElement()
    {
        var presentation = TransitionTests.Deck("<p:transition spd=\"med\"><p:fade/></p:transition>");
        presentation.Execute(new SetTransitionCommand([presentation.Slides[0]], SlideTransition.None));
        presentation.Execute(new SetTransitionCommand([presentation.Slides[1]], new SlideTransition(TransitionKind.Cover, TransitionDirection.Down, 0.5, true, null)));

        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Empty(SlideXml(output).Elements(P + "transition"));
        Assert.Equal("<p:transition xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"><p:cover dir=\"d\" /></p:transition>",
            SlideXml(output, 2).Element(P + "transition")!.ToString(SaveOptions.DisableFormatting));
        var reloaded = Load(output);
        Assert.Equal(SlideTransition.None, reloaded.Slides[0].Transition);
        Assert.Equal(TransitionKind.Cover, reloaded.Slides[1].Transition.Kind);
    }

    [Fact]
    public void Writer_UnchangedTransitionKind_KeepsEffectAttributes()
    {
        var presentation = TransitionTests.Deck("<p:transition spd=\"slow\"><p:split orient=\"vert\" dir=\"in\"/><p:extLst><p:ext uri=\"{X}\"/></p:extLst></p:transition>");
        var slide = presentation.Slides[0];
        presentation.Execute(new SetTransitionCommand([slide], slide.Transition with { DurationSeconds = 0.5 }));

        var transition = SlideXml(Save(presentation)).Element(P + "transition")!;

        Assert.Null(transition.Attribute("spd"));
        Assert.Equal("in", (string)transition.Element(P + "split")!.Attribute("dir"));
        Assert.Equal(["split", "extLst"], transition.Elements().Select(e => e.Name.LocalName));
    }

    [Fact]
    public void Writer_AnimationsUnchanged_TimingIdentical()
    {
        var presentation = RichDeck();
        var slide = presentation.Slides[0];
        presentation.Execute(new SetTransformsCommand(slide, new Dictionary<uint, ShapeTransform> { [5] = new(1, 2, 3, 4) }));
        presentation.Execute(new AddAnimationCommand(slide, new AnimationEffect(2, AnimationClass.Exit, AnimationPreset.Fade, AnimationTrigger.OnClick, 0, 1, AnimationDirection.None, null)));
        presentation.Undo.Undo();

        var output = Save(presentation);

        var original = Xml(new PptxBuilder().Slide(Shapes(2, 3, 4, 5), tail: RichTiming).Build(), "/ppt/slides/slide1.xml").Root!;
        Assert.NotEqual(original.Element(P + "cSld")!.ToString(), SlideXml(output).Element(P + "cSld")!.ToString());
        Assert.True(XNode.DeepEquals(original.Element(P + "timing"), SlideXml(output).Element(P + "timing")));
    }

    [Fact]
    public void Writer_AddFadeEntrance_ToSlideWithoutTiming_RoundTrips()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2, 3) + Shape(4, "TextBox 3", Xfrm(0, 0, 10, 10), "<a:p/>").Replace("<p:cNvSpPr/>", "<p:cNvSpPr txBox=\"1\"/>")).Load();
        var slide = presentation.Slides[0];
        AnimationEffect[] effects =
        [
            new(2, AnimationClass.Entrance, AnimationPreset.Fade, AnimationTrigger.OnClick, 0, 0.5, AnimationDirection.None, null),
            new(3, AnimationClass.Exit, AnimationPreset.FlyIn, AnimationTrigger.WithPrevious, 0.25, 1, AnimationDirection.FromLeft, null),
            new(3, AnimationClass.Entrance, AnimationPreset.Zoom, AnimationTrigger.AfterPrevious, 0, 0.5, AnimationDirection.None, null),
            new(4, AnimationClass.Entrance, AnimationPreset.Wipe, AnimationTrigger.OnClick, 0.5, 0.75, AnimationDirection.FromTop, null),
            new(2, AnimationClass.Exit, AnimationPreset.Appear, AnimationTrigger.AfterPrevious, 0, 0.5, AnimationDirection.None, null),
            new(4, AnimationClass.Entrance, AnimationPreset.FlyIn, AnimationTrigger.OnClick, 0, 0.5, AnimationDirection.FromBottom, null),
            new(4, AnimationClass.Exit, AnimationPreset.Wipe, AnimationTrigger.WithPrevious, 0, 0.5, AnimationDirection.FromRight, null),
            new(2, AnimationClass.Entrance, AnimationPreset.Appear, AnimationTrigger.OnClick, 0, 0.5, AnimationDirection.None, null),
            new(3, AnimationClass.Exit, AnimationPreset.Zoom, AnimationTrigger.OnClick, 0, 0.5, AnimationDirection.None, null)
        ];
        foreach (var effect in effects) presentation.Execute(new AddAnimationCommand(slide, effect));

        var output = Save(presentation);

        AssertConsistent(output);
        var xml = SlideXml(output);
        var timing = xml.Element(P + "timing")!;
        Assert.Equal(["cSld", "timing"], xml.Elements().Select(e => e.Name.LocalName));
        var ids = timing.Descendants(P + "cTn").Select(c => (int)c.Attribute("id")!).ToList();
        Assert.Equal(Enumerable.Range(1, ids.Count), ids);
        var main = timing.Descendants(P + "cTn").Single(c => (string)c.Attribute("nodeType") == "mainSeq");
        Assert.Equal(5, main.Element(P + "childTnLst")!.Elements(P + "par").Count());
        Assert.Equal(["onPrev", "onNext"], main.Parent!.Descendants(P + "cond").Where(c => c.Attribute("evt") != null).Select(c => (string)c.Attribute("evt")));
        Assert.Equal(["2:0", "3:0", "3:1", "4:0", "2:1", "4:1", "4:2", "2:2", "3:2"], EffectNodes(xml).Select(e => $"{e.Descendants(P + "spTgt").First().Attribute("spid")!.Value}:{e.Element(P + "cTn")!.Attribute("grpId")!.Value}"));
        Assert.Equal(["2:0:1", "3:0:1", "3:1:1", "4:0:", "2:1:1", "4:1:", "4:2:", "2:2:1", "3:2:1"],
            timing.Element(P + "bldLst")!.Elements(P + "bldP").Select(b => $"{b.Attribute("spid")!.Value}:{b.Attribute("grpId")!.Value}:{(string)b.Attribute("animBg")}"));
        Assert.Equal(effects.Select(e => e.ToString()), Load(output).Slides[0].Animations.Select(e => e.ToString()));
    }

    [Fact]
    public void Writer_ReorderAnimations_KeepsOriginalEffectNodes()
    {
        var presentation = RichDeck();
        var slide = presentation.Slides[0];
        var original = EffectNodes(Xml(new PptxBuilder().Slide(Shapes(2, 3, 4, 5), tail: RichTiming).Build(), "/ppt/slides/slide1.xml").Root!).ToList();

        presentation.Execute(new MoveAnimationCommand(slide, 0, 4));
        var output = Save(presentation);

        AssertConsistent(output);
        var written = EffectNodes(SlideXml(output)).ToList();
        Assert.Equal(5, written.Count);
        foreach (var (node, index) in new[] { 1, 2, 3, 4, 0 }.Select((o, i) => (original[o], i)))
            Assert.True(XNode.DeepEquals(Stripped(node), Stripped(written[index])), $"effect {index}");
        Assert.Equal(["withEffect", "afterEffect", "clickEffect", "clickEffect", "clickEffect"], written.Select(e => (string)e.Element(P + "cTn")!.Attribute("nodeType")));
        Assert.Single(SlideXml(output).Descendants(P + "cTn"), c => (string)c.Attribute("nodeType") == "interactiveSeq");
        var group = written[0].Parent!.Parent!.Parent!.Parent!.Parent!;
        Assert.Equal("2", group.Element(P + "stCondLst")!.Elements(P + "cond").Last().Element(P + "tn")!.Attribute("val")!.Value);
        Assert.Equal(slide.Animations.Select(a => a.ToString()), Load(output).Slides[0].Animations.Select(a => a.ToString()));
    }

    [Fact]
    public void Writer_RemoveAnimation_CleansBldLst()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2, 3, 4), tail: AnimationTests.Timing(Group(Slice(0, AnimationTests.Effect("entr", 10, 0, "clickEffect", 2), AnimationTests.Effect("entr", 10, 0, "withEffect", 3))), "",
            "<p:bldP spid=\"2\" grpId=\"0\"/><p:bldP spid=\"3\" grpId=\"0\"/><p:bldP spid=\"4\" grpId=\"5\"/>")).Load();
        var slide = presentation.Slides[0];

        presentation.Execute(new RemoveAnimationCommand(slide, 0));
        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Equal(["3", "4"], SlideXml(output).Descendants(P + "bldP").Select(b => (string)b.Attribute("spid")));
        Assert.Equal([AnimationTrigger.WithPrevious], Load(output).Slides[0].Animations.Select(a => a.Trigger));

        presentation.Execute(new RemoveAnimationCommand(slide, 0));
        var empty = SlideXml(Save(presentation));
        Assert.Empty(empty.Descendants(P + "timing"));
    }

    [Fact]
    public void Writer_DeleteShape_WithParagraphBuilds_RemovesBldPAndTargets()
    {
        const string ink = "<p:par><p:cTn presetID=\"1\" presetClass=\"entr\" presetSubtype=\"0\" fill=\"hold\" nodeType=\"withEffect\"><p:stCondLst><p:cond delay=\"0\"/></p:stCondLst>" +
                           "<p:childTnLst><p:set><p:cBhvr><p:cTn dur=\"1\" fill=\"hold\"/><p:tgtEl><p:inkTgt spid=\"3\"/></p:tgtEl><p:attrNameLst><p:attrName>style.visibility</p:attrName></p:attrNameLst></p:cBhvr>" +
                           "<p:to><p:strVal val=\"visible\"/></p:to></p:set></p:childTnLst></p:cTn></p:par>";
        var presentation = new PptxBuilder().Slide(Shapes(2, 3), tail: AnimationTests.Timing(
            Group(Slice(0, AnimationTests.Effect("entr", 10, 0, "clickEffect", 3, paragraph: 0), AnimationTests.Effect("entr", 10, 0, "withEffect", 2), ink)) +
            Group(Slice(0, AnimationTests.Effect("entr", 10, 0, "clickEffect", 3, paragraph: 1))), "",
            "<p:bldP spid=\"3\" grpId=\"0\" build=\"p\"/><p:bldP spid=\"2\" grpId=\"0\"/>")).Load();

        presentation.Execute(new DeleteShapesCommand(presentation.Slides[0], [3]));
        var output = Save(presentation);

        AssertConsistent(output);
        var xml = SlideXml(output);
        Assert.Equal(["2"], xml.Descendants().Where(e => e.Name == P + "spTgt" || e.Name == P + "inkTgt").Select(t => (string)t.Attribute("spid")));
        Assert.Equal(["2"], xml.Descendants(P + "bldP").Select(b => (string)b.Attribute("spid")));
        Assert.Single(EffectNodes(xml));
        Assert.Equal([2u], Load(output).Slides[0].Animations.Select(a => a.ShapeId));
    }

    [Fact]
    public void Writer_DeleteShapeLeadingAClickGroup_RegroupsLikeTheModel()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2, 3), tail: AnimationTests.Timing(
            Group(Slice(0, AnimationTests.Effect("entr", 10, 0, "clickEffect", 2))) +
            Group(Slice(0, AnimationTests.Effect("entr", 10, 0, "clickEffect", 3), AnimationTests.Effect("emph", 6, 0, "withEffect", 2, grpId: 1))), "",
            "<p:bldP spid=\"2\" grpId=\"0\"/><p:bldP spid=\"3\" grpId=\"0\"/><p:bldP spid=\"2\" grpId=\"1\"/>")).Load();
        var slide = presentation.Slides[0];

        presentation.Execute(new DeleteShapesCommand(slide, [3]));
        var output = Save(presentation);

        AssertConsistent(output);
        var main = SlideXml(output).Descendants(P + "cTn").Single(c => (string)c.Attribute("nodeType") == "mainSeq");
        Assert.Single(main.Element(P + "childTnLst")!.Elements(P + "par"));
        Assert.Equal(["2:0", "2:1"], SlideXml(output).Descendants(P + "bldP").Select(b => $"{b.Attribute("spid")!.Value}:{b.Attribute("grpId")!.Value}"));
        var steps = SlideBuildSteps.For(slide);
        var reloaded = SlideBuildSteps.For(Load(output).Slides[0]);
        Assert.Equal(steps.Select(s => string.Join(",", s.Effects.Select(e => $"{e.Effect.ShapeId}@{e.StartSeconds}"))),
            reloaded.Select(s => string.Join(",", s.Effects.Select(e => $"{e.Effect.ShapeId}@{e.StartSeconds}"))));
    }

    [Fact]
    public void Writer_TriggerChangedWithFreshRecord_KeepsOriginalEffectNode()
    {
        var presentation = RichDeck();
        var slide = presentation.Slides[0];
        var original = EffectNodes(Xml(new PptxBuilder().Slide(Shapes(2, 3, 4, 5), tail: RichTiming).Build(), "/ppt/slides/slide1.xml").Root!).ToList();
        var exit = slide.Animations[4];

        presentation.Execute(new SetAnimationCommand(slide, 4, new AnimationEffect(exit.ShapeId, exit.Class, exit.Preset, AnimationTrigger.AfterPrevious, 0.5, exit.DurationSeconds, exit.Direction, null)));
        var output = Save(presentation);

        AssertConsistent(output);
        var written = EffectNodes(SlideXml(output)).ToList();
        Assert.True(XNode.DeepEquals(Stripped(original[4]), Stripped(written[4])));
        Assert.Equal("afterEffect", (string)written[4].Element(P + "cTn")!.Attribute("nodeType"));
    }

    [Fact]
    public void Writer_MainSequenceWithoutIds_AutomaticFirstStep_Saves()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2, 3), tail: AnimationTests.Timing(Group(Slice(0, AnimationTests.Effect("entr", 10, 0, "withEffect", 2))))
            .Replace(" id=\"1\"", "").Replace(" id=\"2\"", "")).Load();
        var slide = presentation.Slides[0];
        presentation.Execute(new AddAnimationCommand(slide, new AnimationEffect(3, AnimationClass.Entrance, AnimationPreset.Fade, AnimationTrigger.OnClick, 0, 0.5, AnimationDirection.None, null)));

        var output = Save(presentation);

        AssertConsistent(output);
        var xml = SlideXml(output);
        var main = xml.Descendants(P + "cTn").Single(c => (string)c.Attribute("nodeType") == "mainSeq");
        Assert.Equal((string)main.Attribute("id"), (string)xml.Descendants(P + "tn").Single().Attribute("val"));
    }

    [Fact]
    public void Writer_NewEffect_AvoidsGroupIdsOfOrphanBuilds()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2, 4), tail: AnimationTests.Timing(Group(Slice(0, AnimationTests.Effect("entr", 10, 0, "clickEffect", 2))), "",
            "<p:bldP spid=\"2\" grpId=\"0\"/><p:bldP spid=\"4\" grpId=\"0\"/>")).Load();
        presentation.Execute(new AddAnimationCommand(presentation.Slides[0], new AnimationEffect(4, AnimationClass.Entrance, AnimationPreset.Fade, AnimationTrigger.OnClick, 0, 0.5, AnimationDirection.None, null)));

        var xml = SlideXml(Save(presentation));

        Assert.Equal(["2:0", "4:0", "4:1"], xml.Descendants(P + "bldP").Select(b => $"{b.Attribute("spid")!.Value}:{b.Attribute("grpId")!.Value}"));
        Assert.Equal("1", (string)EffectNodes(xml).Last().Element(P + "cTn")!.Attribute("grpId"));
    }

    [Fact]
    public void Writer_NewEffectOnPlaceholder_BuildWithoutAnimBg()
    {
        var presentation = new PptxBuilder().Slide(Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>T</a:t></a:r></a:p>")).Load();
        presentation.Execute(new AddAnimationCommand(presentation.Slides[0], new AnimationEffect(2, AnimationClass.Entrance, AnimationPreset.Fade, AnimationTrigger.OnClick, 0, 0.5, AnimationDirection.None, null)));

        var build = SlideXml(Save(presentation)).Descendants(P + "bldP").Single();

        Assert.Null(build.Attribute("animBg"));
    }

    [Fact]
    public void Writer_ZoomTemplate_WithoutAdditive_FlyInWithAdditiveBase()
    {
        var presentation = new PptxBuilder().Slide(Shapes(2)).Load();
        var slide = presentation.Slides[0];
        presentation.Execute(new AddAnimationCommand(slide, new AnimationEffect(2, AnimationClass.Entrance, AnimationPreset.Zoom, AnimationTrigger.OnClick, 0, 0.5, AnimationDirection.None, null)));
        presentation.Execute(new AddAnimationCommand(slide, new AnimationEffect(2, AnimationClass.Exit, AnimationPreset.FlyIn, AnimationTrigger.OnClick, 0, 0.5, AnimationDirection.FromLeft, null)));

        var anims = SlideXml(Save(presentation)).Descendants(P + "anim").Select(a => $"{a.Descendants(P + "attrName").Single().Value}:{(string)a.Element(P + "cBhvr")!.Attribute("additive")}");

        Assert.Equal(["ppt_w:", "ppt_h:", "ppt_x:base", "ppt_y:base"], anims);
    }

    [Fact]
    public void Writer_DeleteShape_RemovesInkTargetInInteractiveSequence()
    {
        const string ink = "<p:par><p:cTn presetID=\"1\" presetClass=\"entr\" presetSubtype=\"0\" fill=\"hold\" nodeType=\"clickEffect\"><p:stCondLst><p:cond delay=\"0\"/></p:stCondLst>" +
                           "<p:childTnLst><p:set><p:cBhvr><p:cTn dur=\"1\" fill=\"hold\"/><p:tgtEl><p:inkTgt spid=\"3\"/></p:tgtEl><p:attrNameLst><p:attrName>style.visibility</p:attrName></p:attrNameLst></p:cBhvr>" +
                           "<p:to><p:strVal val=\"visible\"/></p:to></p:set></p:childTnLst></p:cTn></p:par>";
        var presentation = new PptxBuilder().Slide(Shapes(2, 3, 4), tail: AnimationTests.Timing(Group(Slice(0, AnimationTests.Effect("entr", 10, 0, "clickEffect", 2))),
            Interactive(4, Group(Slice(0, ink, AnimationTests.Effect("entr", 10, 0, "withEffect", 2)))))).Load();

        presentation.Execute(new DeleteShapesCommand(presentation.Slides[0], [3]));
        var output = Save(presentation);

        AssertConsistent(output);
        var xml = SlideXml(output);
        Assert.Empty(xml.Descendants(P + "inkTgt"));
        Assert.Single(xml.Descendants(P + "cTn"), c => (string)c.Attribute("nodeType") == "interactiveSeq");
        Assert.Equal(["2", "2", "4"], xml.Descendants(P + "spTgt").Select(t => (string)t.Attribute("spid")).Order());
    }
}
