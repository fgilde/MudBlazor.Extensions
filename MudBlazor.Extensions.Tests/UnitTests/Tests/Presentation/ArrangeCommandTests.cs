using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class ArrangeCommandTests
{
    internal static readonly string RotatedGroup =
        "<p:grpSp><p:nvGrpSpPr><p:cNvPr id=\"20\" name=\"Group 19\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr>" +
        "<a:xfrm rot=\"1800000\" flipH=\"1\"><a:off x=\"2000000\" y=\"1000000\"/><a:ext cx=\"3000000\" cy=\"2000000\"/>" +
        "<a:chOff x=\"100000\" y=\"200000\"/><a:chExt cx=\"1500000\" cy=\"500000\"/></a:xfrm></p:grpSpPr>" +
        Shape(21, "Child", Xfrm(100000, 200000, 600000, 200000, " rot=\"2700000\" flipV=\"1\"") +
                           "<a:effectLst><a:glow rad=\"12700\"><a:srgbClr val=\"FF0000\"/></a:glow></a:effectLst>") +
        Shape(22, "Child 2", Xfrm(900000, 400000, 300000, 300000, " rot=\"600000\" flipH=\"1\"")) +
        "<p:grpSp><p:nvGrpSpPr><p:cNvPr id=\"23\" name=\"Inner\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr>" +
        "<a:xfrm rot=\"5400000\"><a:off x=\"700000\" y=\"250000\"/><a:ext cx=\"500000\" cy=\"200000\"/>" +
        "<a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"1000000\" cy=\"1000000\"/></a:xfrm></p:grpSpPr>" +
        Shape(24, "Grandchild", Xfrm(0, 0, 500000, 1000000, " rot=\"1200000\"")) + "</p:grpSp></p:grpSp>";

    private static readonly Dictionary<string, Func<Presentation, IDocumentCommand<Presentation>>> Cases = new()
    {
        ["Group"] = p => new GroupShapesCommand(p.Slides[0], [5, 3]),
        ["GroupInsideGroup"] = p => new GroupShapesCommand(p.Slides[0], [22, 21]),
        ["Ungroup"] = p => new UngroupShapesCommand(p.Slides[0], [20]),
        ["UngroupOuterAndInner"] = p => new UngroupShapesCommand(p.Slides[0], [23, 20]),
        ["AlignLeft"] = p => new AlignShapesCommand(p.Slides[0], [3, 5, 21], ShapeAlignment.Left, false),
        ["AlignMiddleToSlide"] = p => new AlignShapesCommand(p.Slides[0], [3, 4], ShapeAlignment.Middle, true),
        ["Distribute"] = p => new DistributeShapesCommand(p.Slides[0], [3, 4, 5], true, false),
        ["DistributeVerticalToSlide"] = p => new DistributeShapesCommand(p.Slides[0], [3, 4], false, true),
        ["Rotate"] = p => new RotateShapesCommand(p.Slides[0], [3, 21], 90),
        ["Flip"] = p => new FlipShapesCommand(p.Slides[0], [5, 20], true),
        ["DeleteAllChildren"] = p => new DeleteShapesCommand(p.Slides[0], [21, 22, 24])
    };

    public static TheoryData<string> CommandNames => new(Cases.Keys);

    internal static Presentation Deck() => new PptxBuilder().Slide(
        Placeholder(2, "Title 1", "type=\"title\"", "") +
        Shape(3, "A", Xfrm(1000000, 1000000, 2000000, 1000000)) +
        Shape(4, "B", Xfrm(4000000, 500000, 1000000, 1000000)) +
        Shape(5, "C", Xfrm(6000000, 2000000, 1000000, 3000000, " rot=\"5400000\"")) + RotatedGroup).Load();

    private static List<(uint, ShapeTransform?)> Transforms(Presentation presentation) =>
        SlideShape.Flatten(presentation.Slides[0].Shapes).Select(s => (s.Id, s.Transform)).ToList();

    private static SlideShape Find(Presentation presentation, uint id) => SlideShape.Flatten(presentation.Slides[0].Shapes).Single(s => s.Id == id);

    private static (double Left, double Top, double Right, double Bottom) Bounds(ShapeTransform t)
    {
        var angle = t.Rotation * Math.PI / 180;
        var (cos, sin) = (Math.Abs(Math.Cos(angle)), Math.Abs(Math.Sin(angle)));
        var (w, h) = (t.Width * cos + t.Height * sin, t.Width * sin + t.Height * cos);
        var (cx, cy) = (t.X + t.Width / 2.0, t.Y + t.Height / 2.0);
        return (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2);
    }

    private static void AssertSameVisual(ShapeTransform expected, ShapeTransform actual)
    {
        Assert.All(new[] { actual.X - expected.X, actual.Y - expected.Y, actual.Width - expected.Width, actual.Height - expected.Height }, d => Assert.InRange(d, -2, 2));
        Assert.InRange(Math.IEEERemainder(actual.Rotation - expected.Rotation, 360), -0.001, 0.001);
        Assert.Equal((expected.FlipH, expected.FlipV), (actual.FlipH, actual.FlipV));
    }

    [Theory]
    [MemberData(nameof(CommandNames))]
    public void Command_ApplyThenRevert_RestoresModel(string name)
    {
        var presentation = Deck();
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
    public void Group_TwoShapes_FrameIsBoundingBox_ZOrderOfTopmost()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var (a, c) = (Find(presentation, 3), Find(presentation, 5));
        var before = (a.ResolvedTransform, c.ResolvedTransform);
        var command = new GroupShapesCommand(slide, [5, 3]);

        presentation.Execute(command);

        var group = command.Group;
        Assert.Equal([2u, 4u, 25u, 20u], slide.Shapes.Select(s => s.Id));
        Assert.Same(group, slide.Shapes[2]);
        Assert.Equal("Group 24", group.Name);
        Assert.Null(group.Parent);
        Assert.Equal(new ShapeTransform(1000000, 1000000, 7000000, 3000000), group.Transform);
        Assert.Equal(group.Transform, group.ChildTransform);
        Assert.Equal([a, c], group.Children);
        Assert.All(group.Children, child => Assert.Same(group, child.Parent));
        Assert.Equal(before, (a.ResolvedTransform, c.ResolvedTransform));
        Assert.Equal("Group", presentation.Undo.UndoDescription);
    }

    [Fact]
    public void Group_Placeholder_Throws()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var before = SlideCommandTests.Snapshot(presentation);

        Assert.Throws<InvalidOperationException>(() => presentation.Execute(new GroupShapesCommand(slide, [2, 3])));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new GroupShapesCommand(slide, [3, 3])));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new GroupShapesCommand(slide, [3, 21])));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new UngroupShapesCommand(slide, [3])));

        Assert.False(presentation.Undo.CanUndo);
        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
    }

    [Fact]
    public void Ungroup_RotatedFlippedScaledGroup_ChildrenKeepVisualPosition()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var descendants = SlideShape.Flatten(((GroupShape)Find(presentation, 20)).Children).ToList();
        var before = descendants.ToDictionary(s => s.Id, s => s.ResolvedTransform);
        Assert.Equal(new ShapeTransform(2000000, 1000000, 3000000, 2000000, 30, true), Find(presentation, 20).Transform);

        presentation.Execute(new UngroupShapesCommand(slide, [20]));

        Assert.Equal([2u, 3u, 4u, 5u, 21u, 22u, 23u], slide.Shapes.Select(s => s.Id));
        Assert.All(slide.Shapes, s => Assert.Null(s.Parent));
        foreach (var shape in descendants) AssertSameVisual(before[shape.Id], shape.ResolvedTransform);

        presentation.Execute(new UngroupShapesCommand(slide, [23]));

        Assert.Equal([2u, 3u, 4u, 5u, 21u, 22u, 24u], slide.Shapes.Select(s => s.Id));
        AssertSameVisual(before[24], Find(presentation, 24).ResolvedTransform);
        var grandchild = Find(presentation, 24).Transform!.Value;
        Assert.Equal((280d, true, false), (grandchild.Rotation, grandchild.FlipH, grandchild.FlipV));
    }

    [Fact]
    public void GroupUngroup_UndoRedo_Symmetric()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var initial = SlideCommandTests.Snapshot(presentation);
        var group = new GroupShapesCommand(slide, [3, 5, 20]);
        presentation.Execute(group);
        var grouped = SlideCommandTests.Snapshot(presentation);
        var visual = SlideShape.Flatten(slide.Shapes).ToDictionary(s => s, s => s.ResolvedTransform);

        presentation.Execute(new UngroupShapesCommand(slide, [20, group.Group.Id]));
        var ungrouped = SlideCommandTests.Snapshot(presentation);

        Assert.Equal([2u, 4u, 3u, 5u, 21u, 22u, 23u], slide.Shapes.Select(s => s.Id));
        foreach (var shape in SlideShape.Flatten(slide.Shapes)) AssertSameVisual(visual[shape], shape.ResolvedTransform);
        presentation.Undo.Undo();
        Assert.Equal(grouped, SlideCommandTests.Snapshot(presentation));
        presentation.Undo.Undo();
        Assert.Equal(initial, SlideCommandTests.Snapshot(presentation));
        presentation.Undo.Redo();
        Assert.Equal(grouped, SlideCommandTests.Snapshot(presentation));
        presentation.Undo.Redo();
        Assert.Equal(ungrouped, SlideCommandTests.Snapshot(presentation));
    }

    [Fact]
    public void Align_Left_RelativeToSelection_UsesRotatedBounds()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];

        presentation.Execute(new AlignShapesCommand(slide, [3, 5], ShapeAlignment.Left, false));

        Assert.Equal(new ShapeTransform(1000000, 1000000, 2000000, 1000000), Find(presentation, 3).Transform);
        Assert.Equal(new ShapeTransform(2000000, 2000000, 1000000, 3000000, 90), Find(presentation, 5).Transform);

        var top = Math.Min(Bounds(Find(presentation, 4).ResolvedTransform).Top, Bounds(Find(presentation, 21).ResolvedTransform).Top);
        presentation.Execute(new AlignShapesCommand(slide, [4, 21], ShapeAlignment.Top, false));

        Assert.InRange(Bounds(Find(presentation, 21).ResolvedTransform).Top - top, -2, 2);
        Assert.InRange(Bounds(Find(presentation, 4).ResolvedTransform).Top - top, -2, 2);
    }

    [Fact]
    public void Align_SingleShape_UsesSlide()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];

        presentation.Execute(new AlignShapesCommand(slide, [4], ShapeAlignment.Right, false));
        Assert.Equal(12192000 - 1000000, Find(presentation, 4).Transform!.Value.X);
        presentation.Execute(new AlignShapesCommand(slide, [4], ShapeAlignment.Center, false));
        Assert.Equal((12192000 - 1000000) / 2, Find(presentation, 4).Transform!.Value.X);
        presentation.Execute(new AlignShapesCommand(slide, [5], ShapeAlignment.Bottom, false));
        Assert.Equal(new ShapeTransform(6000000, 4858000, 1000000, 3000000, 90), Find(presentation, 5).Transform);
        presentation.Execute(new AlignShapesCommand(slide, [3, 4], ShapeAlignment.Top, true));
        Assert.Equal([0L, 0L], new[] { Find(presentation, 3), Find(presentation, 4) }.Select(s => s.Transform!.Value.Y));
    }

    [Fact]
    public void Distribute_Horizontal_EqualGaps()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];

        presentation.Execute(new DistributeShapesCommand(slide, [5, 3, 4], true, false));

        Assert.Equal(new ShapeTransform(1000000, 1000000, 2000000, 1000000), Find(presentation, 3).Transform);
        Assert.Equal(new ShapeTransform(3500000, 500000, 1000000, 1000000), Find(presentation, 4).Transform);
        Assert.Equal(new ShapeTransform(6000000, 2000000, 1000000, 3000000, 90), Find(presentation, 5).Transform);
        var bounds = new uint[] { 3, 4, 5 }.Select(id => Bounds(Find(presentation, id).ResolvedTransform)).ToList();
        Assert.Equal(bounds[1].Left - bounds[0].Right, bounds[2].Left - bounds[1].Right, 3);

        presentation.Execute(new DistributeShapesCommand(slide, [3, 4], true, true));

        Assert.Equal(0, Find(presentation, 3).Transform!.Value.X);
        Assert.Equal(12192000 - 1000000, Find(presentation, 4).Transform!.Value.X);
        Assert.Throws<ArgumentException>(() => presentation.Execute(new DistributeShapesCommand(slide, [3, 4], true, false)));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new DistributeShapesCommand(slide, [3], false, true)));
    }

    [Fact]
    public void Flip_Horizontal_TogglesFlipH()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var child = Find(presentation, 22);
        var before = child.ResolvedTransform;

        presentation.Execute(new FlipShapesCommand(slide, [5, 3, 22], true));

        Assert.Equal(new ShapeTransform(6000000, 2000000, 1000000, 3000000, 270, true), Find(presentation, 5).Transform);
        Assert.Equal(new ShapeTransform(1000000, 1000000, 2000000, 1000000, 0, true), Find(presentation, 3).Transform);
        AssertSameVisual(before with { Rotation = (360 - before.Rotation) % 360, FlipH = !before.FlipH }, child.ResolvedTransform);

        presentation.Execute(new FlipShapesCommand(slide, [5], false));

        Assert.Equal(new ShapeTransform(6000000, 2000000, 1000000, 3000000, 90, true, true), Find(presentation, 5).Transform);
    }

    [Fact]
    public void Rotate_90_AddsRotation()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var child = Find(presentation, 21);
        var before = child.ResolvedTransform;

        presentation.Execute(new RotateShapesCommand(slide, [3, 5, 21], 90));

        Assert.Equal(new ShapeTransform(1000000, 1000000, 2000000, 1000000, 90), Find(presentation, 3).Transform);
        Assert.Equal(180, Find(presentation, 5).Transform!.Value.Rotation);
        AssertSameVisual(before with { Rotation = (before.Rotation + 90) % 360 }, child.ResolvedTransform);

        presentation.Execute(new RotateShapesCommand(slide, [5], -270));

        Assert.Equal(270, Find(presentation, 5).Transform!.Value.Rotation);
    }

    [Fact]
    public void Delete_LastChild_RemovesEmptyGroup()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var before = SlideCommandTests.Snapshot(presentation);

        presentation.Execute(new DeleteShapesCommand(slide, [24]));
        Assert.Equal([21u, 22u], ((GroupShape)Find(presentation, 20)).Children.Select(s => s.Id));

        presentation.Execute(new DeleteShapesCommand(slide, [22, 21]));
        Assert.Equal([2u, 3u, 4u, 5u], slide.Shapes.Select(s => s.Id));

        presentation.Undo.Undo();
        presentation.Undo.Undo();
        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
    }

    [Fact]
    public void NoOpCommands_RecordNoUndoStep()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var before = SlideCommandTests.Snapshot(presentation);
        var events = 0;
        presentation.Changed += (_, _) => events++;

        foreach (var command in new IDocumentCommand<Presentation>[]
                 {
                     new SetShapeStyleCommand(slide, [20], new ShapeFill(ShapeFillKind.Solid, "#00FF00"), new ShapeLine("#000000", 12700)),
                     new SetShapeStyleCommand(slide, [3], null, null),
                     new ReorderShapesCommand(slide, [20], ZOrderChange.BringToFront),
                     new ReorderShapesCommand(slide, [3, 2], ZOrderChange.SendToBack),
                     new DeleteShapesCommand(slide, []),
                     new DuplicateShapesCommand(slide, [], 100),
                     new SetGeometryCommand(slide, 3, "rect"),
                     new SetTransformsCommand(slide, new Dictionary<uint, ShapeTransform> { [3] = new(1000000, 1000000, 2000000, 1000000) }),
                     new UngroupShapesCommand(slide, []),
                     new RotateShapesCommand(slide, [3, 5], 360),
                     new FlipShapesCommand(slide, [], true),
                     new AlignShapesCommand(slide, [], ShapeAlignment.Left, false)
                 })
            presentation.Execute(command);

        Assert.False(presentation.Undo.CanUndo);
        Assert.Equal(0, events);
        Assert.False(slide.Modified);
        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));

        presentation.Execute(new AlignShapesCommand(slide, [3, 4], ShapeAlignment.Top, false));
        presentation.Execute(new AlignShapesCommand(slide, [3, 4], ShapeAlignment.Top, false));
        presentation.Execute(new DistributeShapesCommand(slide, [3, 4, 5], true, false));
        presentation.Execute(new DistributeShapesCommand(slide, [3, 4, 5], true, false));
        presentation.Undo.Undo();
        presentation.Undo.Undo();
        Assert.False(presentation.Undo.CanUndo);
        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
    }

    [Fact]
    public void Tables_CannotBeGroupedRotatedOrFlipped()
    {
        var presentation = TableTests.Deck();
        var slide = presentation.Slides[0];
        var table = TableTests.Table(presentation);
        var before = table.Transform;

        Assert.Throws<InvalidOperationException>(() => presentation.Execute(new GroupShapesCommand(slide, [2, 3])));
        presentation.Execute(new RotateShapesCommand(slide, [3], 90));
        presentation.Execute(new FlipShapesCommand(slide, [3], true));

        Assert.Equal(before, table.Transform);
        Assert.False(presentation.Undo.CanUndo);
        presentation.Execute(new RotateShapesCommand(slide, [2, 3], 90));
        Assert.Equal(90, slide.Shapes[0].Transform!.Value.Rotation);
        Assert.Equal(before, table.Transform);
    }

    [Theory]
    [InlineData("Align")]
    [InlineData("Distribute")]
    [InlineData("Rotate")]
    [InlineData("Flip")]
    public void GroupAndOwnChild_ActsOnGroupOnly(string name)
    {
        IDocumentCommand<Presentation> Command(Presentation p, params uint[] ids) => name switch
        {
            "Align" => new AlignShapesCommand(p.Slides[0], ids, ShapeAlignment.Left, false),
            "Distribute" => new DistributeShapesCommand(p.Slides[0], ids, true, false),
            "Rotate" => new RotateShapesCommand(p.Slides[0], ids, 90),
            _ => new FlipShapesCommand(p.Slides[0], ids, true)
        };
        var (withChild, groupOnly) = (Deck(), Deck());

        withChild.Execute(Command(withChild, 3, 4, 20, 21, 24));
        groupOnly.Execute(Command(groupOnly, 3, 4, 20));

        Assert.Equal(Transforms(groupOnly), Transforms(withChild));
    }

    [Fact]
    public void ChartFrames_AreNotRotatedOrFlipped()
    {
        var presentation = new PptxBuilder().Slide(PptxReaderTests.Frame(5, "Chart 4", PptxReaderTests.ChartUri) + Shape(6, "Box", Xfrm(0, 0, 10, 10))).Load();
        var slide = presentation.Slides[0];

        presentation.Execute(new RotateShapesCommand(slide, [5, 6], 90));
        presentation.Execute(new FlipShapesCommand(slide, [5], true));

        Assert.Equal(new ShapeTransform(100, 200, 300, 400), slide.Shapes[0].Transform);
        Assert.Equal(90, slide.Shapes[1].Transform!.Value.Rotation);
    }

    [Fact]
    public void ShapeStyle_FillTheWriterCannotWrite_Throws()
    {
        var presentation = Deck();
        var gradient = new ShapeFill(ShapeFillKind.Gradient) { Stops = [new GradientStop(0, "#FF0000"), new GradientStop(1, "#0000FF")] };

        Assert.Throws<ArgumentException>(() => presentation.Execute(new SetShapeStyleCommand(presentation.Slides[0], [3], gradient, null)));
        Assert.False(presentation.Undo.CanUndo);
    }
}
