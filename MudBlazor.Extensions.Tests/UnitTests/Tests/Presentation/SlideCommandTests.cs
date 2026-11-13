using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class SlideCommandTests
{
    private const string MixedRuns = "<a:p><a:r><a:rPr lang=\"de-DE\" kumimoji=\"1\"/><a:t>Normal </a:t></a:r>" +
                                     "<a:r><a:rPr lang=\"de-DE\" b=\"1\"/><a:t>fett</a:t></a:r><a:r><a:rPr i=\"1\"/><a:t> normal</a:t></a:r></a:p>";

    private const string ScaledGroup =
        "<p:grpSp><p:nvGrpSpPr><p:cNvPr id=\"10\" name=\"Group 9\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr>" +
        "<a:xfrm rot=\"5400000\"><a:off x=\"1000000\" y=\"1000000\"/><a:ext cx=\"2000000\" cy=\"1000000\"/>" +
        "<a:chOff x=\"100000\" y=\"100000\"/><a:chExt cx=\"1000000\" cy=\"500000\"/></a:xfrm></p:grpSpPr>" +
        "<p:sp><p:nvSpPr><p:cNvPr id=\"11\" name=\"Child\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x=\"100000\" y=\"100000\"/>" +
        "<a:ext cx=\"500000\" cy=\"250000\"/></a:xfrm></p:spPr></p:sp>" +
        "<p:sp><p:nvSpPr><p:cNvPr id=\"12\" name=\"Child 2\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x=\"600000\" y=\"300000\"/>" +
        "<a:ext cx=\"100000\" cy=\"100000\"/></a:xfrm></p:spPr></p:sp></p:grpSp>";

    private static readonly Dictionary<string, Func<Presentation, IDocumentCommand<Presentation>>> Cases = new()
    {
        ["SetTransforms"] = p => new SetTransformsCommand(p.Slides[0], new Dictionary<uint, ShapeTransform>
        {
            [11] = new(1800000, 800000, 900000, 400000, 45), [2] = new(1, 2, 3, 4), [10] = new(900000, 900000, 2000000, 1000000, 90)
        }),
        ["SetShapeStyle"] = p => new SetShapeStyleCommand(p.Slides[0], [3, 4, 10], new ShapeFill(ShapeFillKind.Solid, "#00FF00"), new ShapeLine("#000000", 12700)),
        ["SetShapeStyleLineOnly"] = p => new SetShapeStyleCommand(p.Slides[0], [4], null, new ShapeLine(null, 0)),
        ["SetGeometry"] = p => new SetGeometryCommand(p.Slides[0], 4, "ellipse"),
        ["SetText"] = p => new SetTextCommand(p.Slides[0], 3, new TextBody { Paragraphs = [new TextParagraph { Runs = [new TextRun("<script>x</script>")] }] }),
        ["InsertShape"] = p => new InsertShapeCommand(p.Slides[0], new AutoShape { Name = "New", Transform = new ShapeTransform(5, 6, 7, 8) }, 1),
        ["InsertPicture"] = p => new InsertPictureCommand(p.Slides[0], [1, 2, 3], "image/png", new ShapeTransform(10, 20, 30, 40)),
        ["DeleteShapes"] = p => new DeleteShapesCommand(p.Slides[0], [2, 11, 4]),
        ["DeleteGroupAndChild"] = p => new DeleteShapesCommand(p.Slides[0], [11, 10]),
        ["DuplicateShapes"] = p => new DuplicateShapesCommand(p.Slides[0], [10, 2, 4], 100000),
        ["ReorderToFront"] = p => new ReorderShapesCommand(p.Slides[0], [2, 3], ZOrderChange.BringToFront),
        ["ReorderBackward"] = p => new ReorderShapesCommand(p.Slides[0], [4, 12], ZOrderChange.SendBackward),
        ["AddSlide"] = p => new AddSlideCommand(1, p.Layouts[0]),
        ["DuplicateSlides"] = p => new DuplicateSlidesCommand([p.Slides[2], p.Slides[0]]),
        ["DeleteSlides"] = p => new DeleteSlidesCommand([p.Slides[0], p.Slides[2]]),
        ["DeleteAllSlides"] = p => new DeleteSlidesCommand(p.Slides.ToList()),
        ["MoveSlides"] = p => new MoveSlidesCommand([p.Slides[2], p.Slides[0]], 2),
        ["SetSlideHidden"] = p => new SetSlideHiddenCommand([p.Slides[0], p.Slides[2]], true),
        ["SetNotes"] = p => new SetNotesCommand(p.Slides[1], "New\r\nnotes")
    };

    public static TheoryData<string> CommandNames => new(Cases.Keys);

    private static Presentation Deck() => new PptxBuilder()
        .Slide(Placeholder(2, "Title 1", "type=\"title\"", "") + Shape(3, "Text", Xfrm(0, 0, 100, 100), MixedRuns) +
               Shape(4, "Rect", Xfrm(1000, 1000, 2000, 2000) + "<a:prstGeom prst=\"roundRect\"><a:avLst><a:gd name=\"adj\" fmla=\"val 20000\"/></a:avLst></a:prstGeom>" +
                                "<a:solidFill><a:srgbClr val=\"FF0000\"/></a:solidFill>") + ScaledGroup)
        .Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10)), notes: "Hello")
        .Slide(Shape(2, "Hidden box", Xfrm(0, 0, 10, 10)), "show=\"0\"")
        .Load();

    private static Presentation Slides(int count)
    {
        var builder = new PptxBuilder();
        for (var i = 0; i < count; i++) builder.Slide("");
        return builder.Load();
    }

    internal static string Snapshot(Presentation presentation)
    {
        var sb = new StringBuilder();
        var used = presentation.Slides.SelectMany(s => SlideShape.Flatten(s.Shapes)).OfType<PictureShape>().Select(p => p.ImagePart).ToHashSet();
        foreach (var (part, media) in presentation.PendingMedia.Where(m => used.Contains(m.Key)).OrderBy(m => m.Key))
            sb.AppendLine($"media {part} {media.ContentType} {Convert.ToBase64String(media.Data)}");
        foreach (var slide in presentation.Slides)
        {
            sb.AppendLine($"slide {slide.Key} {Ref(slide.Layout)} hidden={slide.Hidden} notes={slide.Notes} part={slide.HasNotesPart} copyOf={slide.CopyOf?.Key}");
            foreach (var shape in SlideShape.Flatten(slide.Shapes)) sb.AppendLine(Describe(shape));
        }

        foreach (var section in presentation.Sections) sb.AppendLine($"section {Ref(section)} {section.Id} {section.Name} {string.Join(",", section.Slides.Select(s => s.Key))}");
        sb.AppendLine($"titlePlaceholders={presentation.ShowSpecialPlaceholdersOnTitleSlide}");
        return sb.ToString();
    }

    private static string Describe(SlideShape s) =>
        $"{Ref(s)} {s.GetType().Name} {s.Id} {s.Name} {s.Transform} {s.Placeholder} parent={Ref(s.Parent)} inherits={Ref(s.InheritsFrom)} source={Ref(s.Source)} " +
        $"master={Ref(s.Master)} " + s switch
        {
            AutoShape a => $"{a.Geometry} [{string.Join(",", a.Adjustments.OrderBy(k => k.Key))}] {a.Fill} {a.Line} box={a.IsTextBox} {Text(a.Text)}",
            PictureShape p => $"{p.ImagePart} {p.ContentType}",
            GroupShape g => $"{g.ChildTransform} children={string.Join(",", g.Children.Select(Ref))}",
            OpaqueShape o => o.Kind,
            TableShape t => $"{t.StyleId} {t.FirstRow}{t.FirstColumn}{t.LastRow}{t.LastColumn}{t.BandedRows}{t.BandedColumns} [{string.Join(",", t.ColumnWidths)}] " +
                            string.Join(" / ", t.Rows.Select(r => $"{r.Height} {Ref(r.Source)} " + string.Concat(r.Cells.Select(c =>
                                $"<{Text(c.Text)} {c.Fill} {c.RowSpan}x{c.ColumnSpan} {c.IsMergedAway} {c.MarginLeft} {c.MarginTop} {c.MarginRight} {c.MarginBottom} {c.Anchor} {Ref(c.Source)}>")))),
            _ => ""
        };

    private static string Text(TextBody b) => b == null
        ? "no text"
        : $"{b.Anchor} {b.InsetLeft} {b.InsetTop} {b.InsetRight} {b.InsetBottom} {b.Wrap} source={Ref(b.Source)} " + string.Join(" | ", b.Paragraphs.Select(p =>
            $"{p.Align} {p.Level} {p.Bullet} {p.EndFormat} source={Ref(p.Source)} " + string.Concat(p.Runs.Select(r => $"<{r.Text} {r.Format} {r.FieldType} {Ref(r.Source)}>"))));

    private static int Ref(object o) => o == null ? 0 : RuntimeHelpers.GetHashCode(o);

    private static IEnumerable<uint> Ids(IEnumerable<SlideShape> shapes) => shapes.Select(s => s.Id);

    [Theory]
    [MemberData(nameof(CommandNames))]
    public void Command_ApplyThenRevert_RestoresModel(string name)
    {
        var presentation = Deck();
        var before = Snapshot(presentation);
        var command = Cases[name](presentation);

        command.Apply(presentation);
        var applied = Snapshot(presentation);
        Assert.NotEqual(before, applied);
        command.Revert(presentation);
        Assert.Equal(before, Snapshot(presentation));
        command.Apply(presentation);
        Assert.Equal(applied, Snapshot(presentation));
    }

    [Fact]
    public void SetTransforms_ChildOfScaledGroup_WritesChildCoordinates()
    {
        var presentation = Deck();
        var child = SlideShape.Flatten(presentation.Slides[0].Shapes).Single(s => s.Id == 11);
        Assert.Equal(new ShapeTransform(1750000, 750000, 1000000, 500000, 90), child.ResolvedTransform);

        var target = new ShapeTransform(1850000, 850000, 1000000, 500000, 90);
        presentation.Execute(new SetTransformsCommand(presentation.Slides[0], new Dictionary<uint, ShapeTransform> { [11] = target }));

        Assert.Equal(new ShapeTransform(150000, 50000, 500000, 250000), child.Transform);
        Assert.Equal(target, child.ResolvedTransform);
    }

    [Fact]
    public void SetTransforms_GroupAndChildTogether_ChildEndsAtTarget()
    {
        var presentation = Deck();
        var shapes = SlideShape.Flatten(presentation.Slides[0].Shapes).ToList();
        var target = new ShapeTransform(400000, 300000, 600000, 300000, 30, true);

        presentation.Execute(new SetTransformsCommand(presentation.Slides[0], new Dictionary<uint, ShapeTransform>
        {
            [11] = target, [10] = new(500000, 500000, 2000000, 1500000, 45, false, true)
        }));

        var resolved = shapes.Single(s => s.Id == 11).ResolvedTransform;
        Assert.Equal((target.FlipH, target.FlipV), (resolved.FlipH, resolved.FlipV));
        Assert.All(new[] { resolved.X - target.X, resolved.Y - target.Y, resolved.Width - target.Width, resolved.Height - target.Height }, d => Assert.InRange(d, -2, 2));
        Assert.Equal(30, resolved.Rotation, 6);
    }

    [Fact]
    public void SetTransforms_InheritedPlaceholder_GetsExplicitTransform_UndoRestoresNull()
    {
        var presentation = Deck();
        var title = presentation.Slides[0].Shapes[0];
        Assert.Null(title.Transform);

        presentation.Execute(new SetTransformsCommand(presentation.Slides[0], new Dictionary<uint, ShapeTransform> { [2] = new(1, 2, 3, 4) }));
        Assert.Equal(new ShapeTransform(1, 2, 3, 4), title.Transform);
        presentation.Undo.Undo();
        Assert.Null(title.Transform);
    }

    [Fact]
    public void SetText_ReplacingMiddleRun_KeepsNeighbourFormats()
    {
        var presentation = Deck();
        var shape = (AutoShape)presentation.Slides[0].Shapes[1];
        var paragraph = shape.Text.Paragraphs[0];
        var runs = paragraph.Runs;
        var body = shape.Text with { Paragraphs = [paragraph with { Runs = [runs[0], runs[1] with { Text = "<script>x</script>" }, runs[2]] }] };

        presentation.Execute(new SetTextCommand(presentation.Slides[0], 3, body));

        var edited = shape.Text.Paragraphs[0].Runs;
        Assert.Equal(["Normal ", "<script>x</script>", " normal"], edited.Select(r => r.Text));
        Assert.Same(runs[0], edited[0]);
        Assert.Same(runs[2], edited[2]);
        Assert.Equal(new RunFormat(Bold: true), edited[1].Format);
        Assert.Same(runs[1].Source, edited[1].Source);
        Assert.Equal("1", (string)edited[0].Source.Element(PptxReader.A + "rPr")!.Attribute("kumimoji"));
        Assert.Equal(new RunFormat(Italic: true), edited[2].Format);
        Assert.Same(paragraph.Source, shape.Text.Paragraphs[0].Source);

        presentation.Undo.Undo();
        Assert.Equal(["Normal ", "fett", " normal"], shape.Text.Paragraphs[0].Runs.Select(r => r.Text));
    }

    [Fact]
    public void InsertShape_IdIsUniquePerSlide()
    {
        var presentation = Deck();
        var first = new AutoShape();
        var second = new AutoShape();
        var other = new AutoShape();

        presentation.Execute(new InsertShapeCommand(presentation.Slides[0], first));
        presentation.Execute(new InsertShapeCommand(presentation.Slides[0], second, 0));
        presentation.Execute(new InsertShapeCommand(presentation.Slides[1], other));

        Assert.Equal(13u, first.Id);
        Assert.Equal(14u, second.Id);
        Assert.Equal(3u, other.Id);
        Assert.Same(second, presentation.Slides[0].Shapes[0]);
        Assert.Same(first, presentation.Slides[0].Shapes[^1]);
        var ids = Ids(SlideShape.Flatten(presentation.Slides[0].Shapes)).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void InsertPicture_RegistersPendingMediaPart()
    {
        var presentation = Deck();
        var command = new InsertPictureCommand(presentation.Slides[0], [1, 2, 3], "image/jpeg", new ShapeTransform(1, 2, 3, 4));

        presentation.Execute(command);

        Assert.Equal("/ppt/media/image1.jpg", command.Picture.ImagePart);
        Assert.Equal("image/jpeg", command.Picture.ContentType);
        Assert.Equal(13u, command.Picture.Id);
        Assert.Equal(new byte[] { 1, 2, 3 }, presentation.PendingMedia["/ppt/media/image1.jpg"].Data);
        Assert.Same(command.Picture, presentation.Slides[0].Shapes[^1]);

        var second = new InsertPictureCommand(presentation.Slides[0], [4], "image/jpeg", default);
        presentation.Execute(second);
        Assert.Equal("/ppt/media/image2.jpg", second.Picture.ImagePart);
        Assert.Throws<ArgumentException>(() => presentation.Execute(new InsertPictureCommand(presentation.Slides[0], [1], "text/plain", default)));
    }

    [Fact]
    public void InsertPicture_AllowsOnlyKnownImageTypes_CopiesTheBuffer()
    {
        var presentation = Deck();
        var data = new byte[] { 1, 2, 3 };
        var command = new InsertPictureCommand(presentation.Slides[0], data, "image/jpg", default);

        presentation.Execute(command);
        data[0] = 9;

        Assert.Equal("/ppt/media/image1.jpg", command.Picture.ImagePart);
        Assert.Equal("image/jpeg", command.Picture.ContentType);
        Assert.Equal(new byte[] { 1, 2, 3 }, presentation.PendingMedia[command.Picture.ImagePart].Data);
        Assert.Throws<ArgumentException>(() => presentation.Execute(new InsertPictureCommand(presentation.Slides[0], [1], "image/png/../x", default)));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new InsertPictureCommand(presentation.Slides[0], [1], "image/x-citrix-jpeg", default)));
    }

    [Fact]
    public void InsertShape_ShapeAlreadyPlaced_Throws_CreatedShapeIsInserted()
    {
        var presentation = Deck();
        var child = SlideShape.Flatten(presentation.Slides[0].Shapes).First(s => s.Parent != null);

        Assert.Throws<ArgumentException>(() => presentation.Execute(new InsertShapeCommand(presentation.Slides[1], presentation.Slides[0].Shapes[1])));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new InsertShapeCommand(presentation.Slides[1], child)));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new InsertShapeCommand(presentation.Slides[1], presentation.Layouts[0].ShapeList[0])));
        Assert.Throws<ArgumentException>(() => AutoShape.Create("nope", default));
        Assert.False(presentation.Undo.CanUndo);

        var created = AutoShape.Create("ellipse", new ShapeTransform(1, 2, 3, 4), new ShapeFill(ShapeFillKind.Solid, "#FF0000"));
        var box = AutoShape.CreateTextBox(new ShapeTransform(5, 6, 7, 8));
        presentation.Execute(new InsertShapeCommand(presentation.Slides[1], created));
        presentation.Execute(new InsertShapeCommand(presentation.Slides[1], box));

        Assert.Equal([created, box], presentation.Slides[1].Shapes.Skip(1));
        Assert.Equal(["Shape 2", "TextBox 3"], [created.Name, box.Name]);
        Assert.True(box.IsTextBox);
        Assert.Single(box.Text.Paragraphs);
    }

    [Fact]
    public void ShapeCommands_InvalidTransformOrLine_ThrowWithoutChange()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];

        Assert.Throws<ArgumentException>(() => presentation.Execute(new SetTransformsCommand(slide, new Dictionary<uint, ShapeTransform> { [3] = new(0, 0, -1, 5) })));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new SetShapeStyleCommand(slide, [3], null, new ShapeLine("#000000", -1))));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new SetShapeStyleCommand(slide, [3], null, new ShapeLine("#000000", 12700, "wavy"))));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new InsertShapeCommand(slide, AutoShape.Create("rect", new ShapeTransform(0, 0, 1, -1)))));

        Assert.False(presentation.Undo.CanUndo);
        Assert.Equal(new ShapeTransform(0, 0, 100, 100), slide.Shapes[1].Transform);
    }

    [Fact]
    public void DuplicateShapes_CopiesGetNewIdsAndOffset()
    {
        var presentation = Deck();
        var command = new DuplicateShapesCommand(presentation.Slides[0], [11, 4], 100000);

        presentation.Execute(command);

        var shapes = SlideShape.Flatten(presentation.Slides[0].Shapes).ToList();
        Assert.Equal(["Rect", "Child"], command.Copies.Select(c => c.Name));
        Assert.Equal([13u, 14u], Ids(command.Copies));
        var original = shapes.Single(s => s.Id == 11);
        var copy = command.Copies[1];
        Assert.Same(original.Parent, copy.Parent);
        Assert.Same(copy, ((GroupShape)presentation.Slides[0].Shapes[3]).Children[^1]);
        Assert.Equal(original.ResolvedTransform with { X = original.ResolvedTransform.X + 100000, Y = original.ResolvedTransform.Y + 100000 }, copy.ResolvedTransform);
        Assert.NotSame(original.Source, copy.Source);
        Assert.True(XNode.DeepEquals(original.Source, copy.Source));
    }

    [Theory]
    [InlineData(ZOrderChange.BringToFront, new uint[] { 1, 4, 5, 2, 3 })]
    [InlineData(ZOrderChange.BringForward, new uint[] { 1, 4, 2, 3, 5 })]
    [InlineData(ZOrderChange.SendBackward, new uint[] { 2, 3, 1, 4, 5 })]
    [InlineData(ZOrderChange.SendToBack, new uint[] { 2, 3, 1, 4, 5 })]
    public void ReorderShapes_MovesSelectionAsBlock(ZOrderChange change, uint[] expected)
    {
        var presentation = new PptxBuilder().Slide(string.Concat(Enumerable.Range(1, 5).Select(i => Shape((uint)i, $"S{i}", Xfrm(0, 0, 1, 1))))).Load();

        presentation.Execute(new ReorderShapesCommand(presentation.Slides[0], [3, 2], change));

        Assert.Equal(expected, Ids(presentation.Slides[0].Shapes));
    }

    [Fact]
    public void AddSlide_GetsLayoutPlaceholdersAsEmptyShapes()
    {
        var presentation = Deck();
        var layout = presentation.Layouts[0];
        var command = new AddSlideCommand(1, layout);

        presentation.Execute(command);

        var slide = presentation.Slides[1];
        Assert.Same(command.Slide, slide);
        Assert.Same(layout, slide.Layout);
        Assert.Equal(layout.ShapeList.Select(s => s.Placeholder), slide.Shapes.Select(s => s.Placeholder));
        Assert.All(slide.Shapes, s => Assert.Null(s.Transform));
        Assert.Equal(layout.ShapeList.Select(s => s.ResolvedTransform), slide.Shapes.Select(s => s.ResolvedTransform));
        Assert.All(slide.Shapes.Cast<AutoShape>(), s => Assert.Equal("", string.Concat(s.Text.Paragraphs.SelectMany(p => p.Runs).Select(r => r.Text))));
        Assert.Equal(2, Ids(slide.Shapes).Distinct().Count());
    }

    [Fact]
    public void DuplicateSlides_InsertsCopiesAfterLastSelected()
    {
        var presentation = Deck();
        var (a, b, c) = (presentation.Slides[0], presentation.Slides[1], presentation.Slides[2]);
        var command = new DuplicateSlidesCommand([b, a]);

        presentation.Execute(command);

        Assert.Equal([a, b, command.Copies[0], command.Copies[1], c], presentation.Slides);
        Assert.Same(a, command.Copies[0].CopyOf);
        Assert.Equal("Hello", command.Copies[1].Notes);
        Assert.Equal(Ids(SlideShape.Flatten(a.Shapes)), Ids(SlideShape.Flatten(command.Copies[0].Shapes)));
        Assert.Empty(SlideShape.Flatten(a.Shapes).Intersect(SlideShape.Flatten(command.Copies[0].Shapes)));
    }

    [Fact]
    public void DeleteSlides_LastSlide_LeavesEmptyDeck()
    {
        var presentation = Slides(1);

        presentation.Execute(new DeleteSlidesCommand(presentation.Slides.ToList()));

        Assert.Empty(presentation.Slides);
        presentation.Undo.Undo();
        Assert.Single(presentation.Slides);
    }

    [Fact]
    public void MoveSlides_MultipleNonContiguous_KeepsRelativeOrder()
    {
        var presentation = Slides(5);
        var s = presentation.Slides.ToArray();

        presentation.Execute(new MoveSlidesCommand([s[3], s[1]], 5));
        Assert.Equal([s[0], s[2], s[4], s[1], s[3]], presentation.Slides);

        presentation.Undo.Undo();
        presentation.Execute(new MoveSlidesCommand([s[3], s[1]], 0));
        Assert.Equal([s[1], s[3], s[0], s[2], s[4]], presentation.Slides);

        presentation.Undo.Undo();
        presentation.Execute(new MoveSlidesCommand([s[2], s[0]], 4));
        Assert.Equal([s[1], s[3], s[0], s[2], s[4]], presentation.Slides);
    }

    [Fact]
    public void Transaction_DragOfThreeShapes_IsOneUndoStep()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var before = Snapshot(presentation);

        using (presentation.Undo.BeginTransaction("Move"))
            foreach (var step in new long[] { 10, 20, 30 })
                presentation.Execute(new SetTransformsCommand(slide, new Dictionary<uint, ShapeTransform>
                {
                    [2] = new(step, step, 100, 100), [4] = new(step, 0, 100, 100), [11] = new(0, step, 100, 100)
                }));

        Assert.Equal("Move", presentation.Undo.UndoDescription);
        presentation.Undo.Undo();
        Assert.Equal(before, Snapshot(presentation));
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public void Changed_RaisedOncePerTransaction_WithAffectedSlides()
    {
        var presentation = Deck();
        var events = new List<PresentationChangedEventArgs>();
        presentation.Changed += (_, e) => events.Add(e);

        using (presentation.Undo.BeginTransaction("Edit"))
        {
            presentation.Execute(new SetGeometryCommand(presentation.Slides[0], 4, "ellipse"));
            presentation.Execute(new SetSlideHiddenCommand([presentation.Slides[1]], true));
            Assert.Empty(events);
        }

        var e = Assert.Single(events);
        Assert.True(new HashSet<Slide> { presentation.Slides[0], presentation.Slides[1] }.SetEquals(e.Slides));
        Assert.Equal(PresentationChangeKind.Content | PresentationChangeKind.SlideOrder, e.Kind);
        Assert.True(presentation.Slides[0].Modified);
    }

    [Fact]
    public void UndoRedo_RaiseChanged_WithAffectedSlides()
    {
        var presentation = Deck();
        var (a, c) = (presentation.Slides[0], presentation.Slides[2]);
        presentation.Execute(new DeleteSlidesCommand([a, c]));
        var events = new List<PresentationChangedEventArgs>();
        presentation.Changed += (_, e) => events.Add(e);
        var version = a.Version;

        presentation.Undo.Undo();
        var undo = Assert.Single(events);
        Assert.True(new HashSet<Slide> { a, c }.SetEquals(undo.Slides));
        Assert.Equal(PresentationChangeKind.SlidesAdded, undo.Kind);
        Assert.True(a.Version > version);

        presentation.Undo.Redo();
        Assert.Equal(2, events.Count);
        Assert.True(new HashSet<Slide> { a, c }.SetEquals(events[1].Slides));
        Assert.Equal(PresentationChangeKind.SlidesRemoved, events[1].Kind);
    }

    [Fact]
    public void SetNotes_WithoutNotesMaster_Throws()
    {
        var presentation = Deck();
        Assert.Null(presentation.Package.NotesMasterPart);

        Assert.Throws<InvalidOperationException>(() => presentation.Execute(new SetNotesCommand(presentation.Slides[0], "x")));
        Assert.False(presentation.Undo.CanUndo);
        Assert.False(presentation.Slides[0].Modified);

        presentation.Execute(new SetNotesCommand(presentation.Slides[1], "a\r\nb"));
        Assert.Equal("a\nb", presentation.Slides[1].Notes);
    }

    [Fact]
    public void ShapeCommand_UnknownId_ThrowsWithoutChange()
    {
        var presentation = Deck();
        var before = Snapshot(presentation);

        Assert.Throws<ArgumentException>(() => presentation.Execute(new DeleteShapesCommand(presentation.Slides[0], [2, 99])));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new SetGeometryCommand(presentation.Slides[0], 4, "noSuchShape")));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new SetTextCommand(presentation.Slides[0], 10, new TextBody())));
        Assert.Equal(before, Snapshot(presentation));
        Assert.False(presentation.Undo.CanUndo);
    }
}
