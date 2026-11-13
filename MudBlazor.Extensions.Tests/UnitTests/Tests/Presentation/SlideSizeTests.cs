using System.Text;
using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class SlideSizeTests
{
    private const long Inch = 914400;

    private static readonly Dictionary<string, Func<Presentation, IDocumentCommand<Presentation>>> Cases = new()
    {
        ["Standard"] = _ => new SetSlideSizeCommand(9144000, 6858000, true),
        ["StandardWithoutScaling"] = _ => new SetSlideSizeCommand(9144000, 6858000, false),
        ["Taller"] = _ => new SetSlideSizeCommand(12192000, 9144000, true)
    };

    public static TheoryData<string> CommandNames => new(Cases.Keys);

    internal static PptxBuilder Builder()
    {
        var builder = LayoutCommandTests.Builder();
        builder.Slide(Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>Title</a:t></a:r></a:p>") + Shape(4, "Box", Xfrm(Inch, Inch, Inch, Inch)) +
                      TableTests.Table(5, "", [2000000, 1000000], TableTests.Tc("a") + TableTests.Tc("b")));
        builder.Slide(Shape(2, "Other", Xfrm(0, 0, 12192000, 6858000)));
        return builder;
    }

    internal static string Snapshot(Presentation presentation)
    {
        var sb = new StringBuilder(SlideCommandTests.Snapshot(presentation));
        sb.AppendLine($"size {presentation.SlideWidth}x{presentation.SlideHeight}");
        foreach (var shape in presentation.Layouts.SelectMany(l => l.ShapeList).Concat(presentation.Masters.SelectMany(m => m.ShapeList)))
            sb.AppendLine($"inherited {shape.Id} {shape.Name} {shape.Transform}");
        return sb.ToString();
    }

    [Theory]
    [MemberData(nameof(CommandNames))]
    public void Command_ApplyThenRevert_RestoresModel(string name)
    {
        var presentation = Builder().Load();
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
    public void SlideSize_ScaleContent_FitsAndCentresSlidesLayoutsAndMasters()
    {
        var presentation = Builder().Load();
        var slide = presentation.Slides[0];
        var changed = new List<PresentationChangedEventArgs>();
        presentation.Changed += (_, e) => changed.Add(e);

        presentation.Execute(new SetSlideSizeCommand(9144000, 6858000, true));

        Assert.Equal((9144000L, 6858000L), (presentation.SlideWidth, presentation.SlideHeight));
        const long top = (6858000 - 6858000 * 3 / 4) / 2;
        Assert.Equal(new ShapeTransform(Inch * 3 / 4, Inch * 3 / 4 + top, Inch * 3 / 4, Inch * 3 / 4), slide.Shapes[1].Transform);
        var table = Assert.IsType<TableShape>(slide.Shapes[2]);
        Assert.Equal([1500000L, 750000L], table.ColumnWidths);
        Assert.Equal([300000L], table.Rows.Select(r => r.Height));
        Assert.Equal(new ShapeTransform(750000, 750000 + top, 2250000, 300000), table.Transform);
        Assert.Null(slide.Shapes[0].Transform);
        Assert.Equal(new ShapeTransform(750000, 375000 + top, 6000000, 750000), slide.Shapes[0].ResolvedTransform);
        Assert.Equal(new ShapeTransform(628650, 273844 + top, 7886700, 994172), presentation.Masters[0].ShapeList[0].Transform);
        Assert.Equal("Slide size", presentation.Undo.UndoDescription);
        Assert.Equal(presentation.Slides, Assert.Single(changed).Slides);

        presentation.Undo.Undo();
        Assert.Equal(12192000, presentation.SlideWidth);
        Assert.Equal(new ShapeTransform(Inch, Inch, Inch, Inch), slide.Shapes[1].Transform);
    }

    [Fact]
    public void SlideSize_WiderSlide_KeepsContentSizeAndCentresHorizontally()
    {
        var presentation = new PptxBuilder { Width = 9144000 }.Slide(Shape(2, "Box", Xfrm(0, 0, Inch, Inch))).Load();

        presentation.Execute(new SetSlideSizeCommand(12192000, 6858000, true));

        Assert.Equal(new ShapeTransform(1524000, 0, Inch, Inch), presentation.Slides[0].Shapes[0].Transform);
    }

    [Fact]
    public void SlideSize_WithoutScaling_MovesNothing_SameSizeRecordsNothing_InvalidThrows()
    {
        var presentation = Builder().Load();
        var before = Snapshot(presentation);

        presentation.Execute(new SetSlideSizeCommand(12192000, 6858000, true));
        Assert.False(presentation.Undo.CanUndo);
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new SetSlideSizeCommand(900000, 6858000, true)));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new SetSlideSizeCommand(9144000, 51206401, true)));
        Assert.Equal(before, Snapshot(presentation));

        presentation.Execute(new SetSlideSizeCommand(9144000, 6858000, false));
        Assert.Equal(before.Replace("size 12192000x6858000", "size 9144000x6858000"), Snapshot(presentation));
    }
}
