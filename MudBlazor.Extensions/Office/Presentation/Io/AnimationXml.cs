using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

internal static class AnimationXml
{
    private static readonly (AnimationPreset Preset, int Id)[] Presets =
        [(AnimationPreset.Appear, 1), (AnimationPreset.FlyIn, 2), (AnimationPreset.Fade, 10), (AnimationPreset.Wipe, 22), (AnimationPreset.Zoom, 53), (AnimationPreset.Zoom, 23)];

    private static readonly (AnimationDirection Direction, int Subtype, string Wipe, string X, string Y)[] Sides =
    [
        (AnimationDirection.FromTop, 1, "down", "#ppt_x", "0-#ppt_h/2"),
        (AnimationDirection.FromRight, 2, "left", "1+#ppt_w/2", "#ppt_y"),
        (AnimationDirection.FromBottom, 4, "up", "#ppt_x", "1+#ppt_h/2"),
        (AnimationDirection.FromLeft, 8, "right", "0-#ppt_w/2", "#ppt_y")
    ];

    private static readonly (AnimationTrigger Trigger, string NodeType)[] NodeTypes =
        [(AnimationTrigger.OnClick, "clickEffect"), (AnimationTrigger.WithPrevious, "withEffect"), (AnimationTrigger.AfterPrevious, "afterEffect")];

    internal static List<AnimationEffect> Read(XElement slide) =>
        Children(MainSequence(slide)).SelectMany(step => Children(step).SelectMany((slice, s) => Children(slice).Select((effect, e) => Effect(effect.Parent, s == 0 && e == 0)))).ToList();

    internal static void Write(XElement slide, IReadOnlyList<AnimationEffect> effects, XElement loaded)
    {
        var shapes = (slide.Element(P + "cSld")?.Element(P + "spTree")?.Descendants(P + "cNvPr") ?? []).ToLookup(c => (string)c.Attribute("id"), c => c.Parent!.Parent!);
        var live = effects.Where(e => shapes.Contains(e.ShapeId.ToString(CultureInfo.InvariantCulture))).ToList();
        if (live.Count == effects.Count && AnimationEdit.Same(effects, loaded == null ? [] : Read(loaded))) return;
        var timing = slide.Element(P + "timing");
        if (timing != null) SlideTiming.Renumber(timing);
        var before = timing == null ? [] : Builds(timing);
        var main = MainSequence(slide) ?? CreateMainSequence(slide);
        timing = slide.Element(P + "timing")!;
        var list = main.Element(P + "childTnLst") ?? Add(main, new XElement(P + "childTnLst"));
        list.RemoveNodes();
        var used = Builds(timing).Concat(before).Concat(timing.Element(P + "bldLst")?.Elements().Select(Build) ?? []).Concat(live.Where(e => e.Source != null).Select(e => Build(e.Source))).ToList();
        var created = new List<(string Shape, string Group)>();
        XElement step = null, slice = null;
        foreach (var (effect, newStep, newSlice, sliceStart, _) in SlideBuildSteps.Schedule(live))
        {
            if (newStep) list.Add(step = TimeNode(Conditions(new XElement(P + "cond", new XAttribute("delay", "indefinite")),
                effect.Trigger == AnimationTrigger.OnClick ? null : new XElement(P + "cond", new XAttribute("evt", "onBegin"), new XAttribute("delay", 0), new XElement(P + "tn", new XAttribute("val", (string)main.Attribute("id")))))));
            if (newSlice) step!.Element(P + "cTn")!.Element(P + "childTnLst")!.Add(slice = TimeNode(Conditions(Delay(sliceStart))));
            XElement node;
            if (effect.Source != null && (Reusable(effect) || !AnimationEdit.IsCreatable(effect))) node = new XElement(effect.Source);
            else
            {
                var key = (effect.ShapeId.ToString(CultureInfo.InvariantCulture), NextGroup(used, effect.ShapeId));
                used.Add(key);
                created.Add(key);
                node = Create(effect, key.Item2);
            }

            var tn = node.Element(P + "cTn")!;
            tn.SetAttributeValue("nodeType", NodeTypes.First(n => n.Trigger == effect.Trigger).NodeType);
            var conditions = tn.Element(P + "stCondLst") ?? SlideXml.AddFirst(tn, new XElement(P + "stCondLst"));
            (conditions.Element(P + "cond") ?? SlideXml.AddFirst(conditions, new XElement(P + "cond"))).SetAttributeValue("delay", Milliseconds(effect.DelaySeconds));
            slice!.Element(P + "cTn")!.Element(P + "childTnLst")!.Add(node);
        }

        SlideTiming.Renumber(timing);
        var after = Builds(timing);
        timing.Element(P + "bldLst")?.Elements().Where(b => before.Contains(Build(b)) && !after.Contains(Build(b))).Remove();
        foreach (var (shape, group) in created)
        {
            if (shapes[shape].FirstOrDefault() is not { } element || element.Name != P + "sp") continue;
            var builds = timing.Element(P + "bldLst") ?? SlideXml.Place(timing, new XElement(P + "bldLst"), "extLst");
            builds.Add(new XElement(P + "bldP", new XAttribute("spid", shape), new XAttribute("grpId", group),
                (string)element.Element(P + "nvSpPr")?.Element(P + "cNvSpPr")?.Attribute("txBox") is "1" or "true" || element.Descendants(P + "ph").Any()
                    ? null
                    : new XAttribute("animBg", "1")));
        }
    }

    private static AnimationEffect Effect(XElement node, bool first)
    {
        var tn = node.Element(P + "cTn")!;
        var target = tn.Descendants().FirstOrDefault(t => t.Name == P + "spTgt" || t.Name == P + "inkTgt");
        var text = target?.Element(P + "txEl");
        var range = text?.Element(P + "pRg");
        var paragraph = range != null && (string)range.Attribute("st") == (string)range.Attribute("end") ? (int?)Long((string)range.Attribute("st")) : null;
        var type = (string)tn.Attribute("presetClass") switch
        {
            "entr" => AnimationClass.Entrance,
            "exit" => AnimationClass.Exit,
            "emph" => AnimationClass.Emphasis,
            "path" => AnimationClass.Path,
            _ => AnimationClass.Other
        };
        var id = Long((string)tn.Attribute("presetID"));
        var preset = type is AnimationClass.Entrance or AnimationClass.Exit && (text == null || paragraph != null) ? Presets.Where(p => p.Id == id).Select(p => p.Preset).DefaultIfEmpty(AnimationPreset.Other).First() : AnimationPreset.Other;
        var subtype = Long((string)tn.Attribute("presetSubtype"));
        var trigger = NodeTypes.FirstOrDefault(n => n.NodeType == (string)tn.Attribute("nodeType"), (first ? AnimationTrigger.OnClick : AnimationTrigger.WithPrevious, null)).Trigger;
        var duration = tn.Element(P + "childTnLst")?.Descendants(P + "cTn").Select(c => Long((string)c.Attribute("dur")) is { } dur ? Start(c) + dur : 0).DefaultIfEmpty().Max() ?? 0;
        return new AnimationEffect((uint)(Long((string)target?.Attribute("spid")) ?? 0), type, preset, trigger, Start(tn) / 1000.0, duration / 1000.0,
            preset is AnimationPreset.FlyIn or AnimationPreset.Wipe ? Sides.FirstOrDefault(s => s.Subtype == subtype).Direction : AnimationDirection.None,
            paragraph) { Source = node };
    }

    private static bool Reusable(AnimationEffect effect) =>
        Effect(effect.Source, effect.Trigger == AnimationTrigger.OnClick) with { Trigger = effect.Trigger, DelaySeconds = effect.DelaySeconds } == effect;

    private static XElement Create(AnimationEffect effect, string group)
    {
        var ms = Math.Max(1, (long)Math.Round(effect.DurationSeconds * 1000));
        var entrance = effect.Class == AnimationClass.Entrance;
        var side = Sides.FirstOrDefault(s => s.Direction == effect.Direction);
        var instant = effect.Preset == AnimationPreset.Appear;
        var behaviours = new List<XElement>();
        if (entrance) behaviours.Add(Visibility(effect.ShapeId, "visible", 0, instant ? ms : 1));
        switch (effect.Preset)
        {
            case AnimationPreset.Fade:
                behaviours.Add(Filter(effect.ShapeId, entrance, "fade", ms));
                break;
            case AnimationPreset.Wipe:
                behaviours.Add(Filter(effect.ShapeId, entrance, $"wipe({side.Wipe})", ms));
                break;
            case AnimationPreset.FlyIn:
                behaviours.Add(Move(effect.ShapeId, "ppt_x", side.X, "#ppt_x", entrance, ms));
                behaviours.Add(Move(effect.ShapeId, "ppt_y", side.Y, "#ppt_y", entrance, ms));
                break;
            case AnimationPreset.Zoom:
                behaviours.Add(Move(effect.ShapeId, "ppt_w", "0", "#ppt_w", entrance, ms, false));
                behaviours.Add(Move(effect.ShapeId, "ppt_h", "0", "#ppt_h", entrance, ms, false));
                behaviours.Add(Filter(effect.ShapeId, entrance, "fade", ms));
                break;
        }

        if (!entrance) behaviours.Add(Visibility(effect.ShapeId, "hidden", instant ? 0 : ms - 1, instant ? ms : 1));
        return new XElement(P + "par", new XElement(P + "cTn",
            new XAttribute("presetID", Presets.First(p => p.Preset == effect.Preset).Id),
            new XAttribute("presetClass", entrance ? "entr" : "exit"),
            new XAttribute("presetSubtype", effect.Preset == AnimationPreset.Zoom ? 16 : side.Subtype),
            new XAttribute("fill", "hold"), new XAttribute("grpId", group),
            new XElement(P + "stCondLst", new XElement(P + "cond")), new XElement(P + "childTnLst", behaviours)));
    }

    private static XElement Visibility(uint shape, string value, long delay, long duration) =>
        new(P + "set", Behaviour(shape, new XElement(P + "cTn", new XAttribute("dur", duration), new XAttribute("fill", "hold"), Conditions(Delay(delay / 1000.0))), "style.visibility"),
            new XElement(P + "to", new XElement(P + "strVal", new XAttribute("val", value))));

    private static XElement Filter(uint shape, bool entrance, string filter, long duration) =>
        new(P + "animEffect", new XAttribute("transition", entrance ? "in" : "out"), new XAttribute("filter", filter),
            Behaviour(shape, new XElement(P + "cTn", new XAttribute("dur", duration)), null));

    private static XElement Move(uint shape, string attribute, string outside, string inside, bool entrance, long duration, bool additive = true) =>
        new(P + "anim", new XAttribute("calcmode", "lin"), new XAttribute("valueType", "num"),
            Behaviour(shape, new XElement(P + "cTn", new XAttribute("dur", duration), new XAttribute("fill", "hold")), attribute, additive),
            new XElement(P + "tavLst", Value(0, entrance ? outside : inside), Value(100000, entrance ? inside : outside)));

    private static XElement Value(int time, string value) =>
        new(P + "tav", new XAttribute("tm", time), new XElement(P + "val", new XElement(P + (value == "0" ? "fltVal" : "strVal"), new XAttribute("val", value))));

    private static XElement Behaviour(uint shape, XElement timeNode, string attribute, bool additive = false) =>
        new(P + "cBhvr", additive ? new XAttribute("additive", "base") : null, timeNode,
            new XElement(P + "tgtEl", new XElement(P + "spTgt", new XAttribute("spid", shape))),
            attribute == null ? null : new XElement(P + "attrNameLst", new XElement(P + "attrName", attribute)));

    private static XElement TimeNode(XElement conditions) =>
        new(P + "par", new XElement(P + "cTn", new XAttribute("fill", "hold"), conditions, new XElement(P + "childTnLst")));

    private static XElement Conditions(params XElement[] conditions) => new(P + "stCondLst", conditions);

    private static XElement Delay(double seconds) => new(P + "cond", new XAttribute("delay", Milliseconds(seconds)));

    private static string Milliseconds(double seconds) => ((long)Math.Round(seconds * 1000)).ToString(CultureInfo.InvariantCulture);

    private static long Start(XElement timeNode) => Long((string)timeNode.Element(P + "stCondLst")?.Element(P + "cond")?.Attribute("delay")) ?? 0;

    private static XElement MainSequence(XElement slide) =>
        slide.Element(P + "timing")?.Element(P + "tnLst")?.Element(P + "par")?.Element(P + "cTn")?.Element(P + "childTnLst")?.Elements(P + "seq")
            .Select(s => s.Element(P + "cTn")).FirstOrDefault(c => (string)c?.Attribute("nodeType") == "mainSeq");

    private static IEnumerable<XElement> Children(XElement timeNode) =>
        timeNode?.Element(P + "childTnLst")?.Elements(P + "par").Select(p => p.Element(P + "cTn")).Where(c => c != null) ?? [];

    private static XElement CreateMainSequence(XElement slide)
    {
        var timing = slide.Element(P + "timing") ?? SlideXml.Place(slide, new XElement(P + "timing"), "extLst");
        var next = timing.Descendants(P + "cTn").Select(c => Long((string)c.Attribute("id")) ?? 0).DefaultIfEmpty().Max() + 1;
        var tnLst = timing.Element(P + "tnLst") ?? SlideXml.AddFirst(timing, new XElement(P + "tnLst"));
        var root = tnLst.Element(P + "par")?.Element(P + "cTn") ?? Add(tnLst, new XElement(P + "par",
            new XElement(P + "cTn", new XAttribute("id", next++), new XAttribute("dur", "indefinite"), new XAttribute("restart", "never"), new XAttribute("nodeType", "tmRoot")))).Element(P + "cTn")!;
        var children = root.Element(P + "childTnLst") ?? SlideXml.Place(root, new XElement(P + "childTnLst"), "subTnLst");
        var main = new XElement(P + "cTn", new XAttribute("id", next), new XAttribute("dur", "indefinite"), new XAttribute("nodeType", "mainSeq"), new XElement(P + "childTnLst"));
        children.AddFirst(new XElement(P + "seq", new XAttribute("concurrent", 1), new XAttribute("nextAc", "seek"), main,
            new XElement(P + "prevCondLst", SlideCondition("onPrev")), new XElement(P + "nextCondLst", SlideCondition("onNext"))));
        return main;
    }

    private static XElement SlideCondition(string trigger) =>
        new(P + "cond", new XAttribute("evt", trigger), new XAttribute("delay", 0), new XElement(P + "tgtEl", new XElement(P + "sldTgt")));

    private static HashSet<(string Shape, string Group)> Builds(XElement timing) =>
        timing.Descendants(P + "cTn").Where(c => c.Attribute("presetClass") != null && c.Attribute("grpId") != null).Select(c => Build(c.Parent)).ToHashSet();

    private static (string Shape, string Group) Build(XElement element) =>
        element.Name == P + "par"
            ? ((string)element.Descendants(P + "spTgt").FirstOrDefault()?.Attribute("spid"), (string)element.Element(P + "cTn")?.Attribute("grpId"))
            : ((string)element.Attribute("spid"), (string)element.Attribute("grpId"));

    private static string NextGroup(IEnumerable<(string Shape, string Group)> used, uint shape)
    {
        var id = shape.ToString(CultureInfo.InvariantCulture);
        return (used.Where(u => u.Shape == id).Select(u => Long(u.Group) ?? -1).DefaultIfEmpty(-1).Max() + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static XElement Add(XElement parent, XElement child)
    {
        parent.Add(child);
        return child;
    }
}
