using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class HeaderFooterTests
{
    internal static readonly HeaderFooterOptions All = new(true, true, "", true, true, "Confidential", false);

    private static readonly Dictionary<string, Func<Presentation, IDocumentCommand<Presentation>>> Cases = new()
    {
        ["ShowAll"] = p => new SetHeaderFooterCommand(p.Slides, All),
        ["HideOnTitle"] = p => new SetHeaderFooterCommand(p.Slides, All with { HideOnTitleSlides = true }),
        ["RemoveAll"] = p => new SetHeaderFooterCommand([p.Slides[3]], new HeaderFooterOptions(false, true, "", false, false, "", false)),
        ["FixedDate"] = p => new SetHeaderFooterCommand([p.Slides[3]], All with { AutomaticDate = false, FixedDate = "1.1.2027" })
    };

    public static TheoryData<string> CommandNames => new(Cases.Keys);

    private static string Footer(uint id, string type, uint idx, string body) =>
        Placeholder(id, $"{type} Placeholder {id - 1}", $"type=\"{type}\" idx=\"{idx}\"", "", body);

    internal static PptxBuilder Builder(string attributes = "")
    {
        var builder = LayoutCommandTests.Builder();
        builder.PresentationAttributes = attributes;
        return builder
            .Slide(Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>One</a:t></a:r></a:p>"))
            .Slide(Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>Two</a:t></a:r></a:p>"), layout: 3)
            .Slide(Placeholder(2, "Title 1", "type=\"ctrTitle\"", "", "<a:p><a:r><a:t>Three</a:t></a:r></a:p>") +
                   Footer(5, "ftr", 11, "<a:p><a:r><a:t>Old</a:t></a:r></a:p>"), layout: 4)
            .Slide(Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>Four</a:t></a:r></a:p>") +
                   Footer(5, "dt", 10, "<a:p><a:r><a:rPr lang=\"de-DE\"/><a:t>Jan 2027</a:t></a:r></a:p>") +
                   Footer(6, "ftr", 11, "<a:p><a:r><a:t>Footer</a:t></a:r></a:p>"));
    }

    private static AutoShape Part(Slide slide, string type) => slide.Shapes.OfType<AutoShape>().SingleOrDefault(s => s.Placeholder?.Type == type);

    private static string Text(AutoShape shape) => string.Concat(shape.Text!.Paragraphs.SelectMany(p => p.Runs).Select(r => r.Text));

    [Theory]
    [MemberData(nameof(CommandNames))]
    public void Command_ApplyThenRevert_RestoresModel(string name)
    {
        var presentation = Builder().Load();
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
    public void HeaderFooter_AddsAndRemovesPlaceholders()
    {
        var presentation = Builder().Load();
        var slide = presentation.Slides[0];
        var titleOnly = SlideCommandTests.Snapshot(presentation).Split('\n').Where(l => l.Contains("Two")).ToList();

        presentation.Execute(new SetHeaderFooterCommand([slide, presentation.Slides[1]], All));

        var date = Part(slide, "dt")!;
        var number = Part(slide, "sldNum")!;
        var footer = Part(slide, "ftr")!;
        Assert.Equal(["Title 1", "Date Placeholder 9", "Footer Placeholder 10", "Slide Number Placeholder 11"], slide.Shapes.Select(s => s.Name));
        Assert.Equal(new PlaceholderRef("dt", 10), date.Placeholder);
        Assert.Null(date.Transform);
        Assert.Equal(new ShapeTransform(100, 6000000, 2000000, 300000), date.ResolvedTransform);
        Assert.Equal("datetime1", date.Text!.Paragraphs[0].Runs[0].FieldType);
        Assert.Equal("slidenum", number.Text!.Paragraphs[0].Runs[0].FieldType);
        Assert.Equal("1", Text(number));
        Assert.Equal("Confidential", Text(footer));
        Assert.Equal(slide.Shapes.Count, slide.Shapes.Select(s => s.Id).Distinct().Count());
        Assert.Equal(titleOnly, SlideCommandTests.Snapshot(presentation).Split('\n').Where(l => l.Contains("Two")).ToList());
        Assert.Single(presentation.Slides[1].Shapes);

        presentation.Execute(new SetHeaderFooterCommand([slide], new HeaderFooterOptions(true, false, "1.1.2027", false, false, "", false)));

        Assert.Equal(["Title 1", "Date Placeholder 9"], slide.Shapes.Select(s => s.Name));
        Assert.Same(date, Part(slide, "dt"));
        Assert.Equal("1.1.2027", Text(date));
        Assert.Null(date.Text!.Paragraphs[0].Runs[0].FieldType);

        presentation.Execute(new SetHeaderFooterCommand([slide], new HeaderFooterOptions(true, false, "1.1.2027", false, false, "", false)));
        presentation.Undo.Undo();
        Assert.Equal(4, slide.Shapes.Count);
        Assert.Equal("datetime1", Part(slide, "dt")!.Text!.Paragraphs[0].Runs[0].FieldType);
        presentation.Undo.Undo();
        Assert.Single(slide.Shapes);
    }

    [Fact]
    public void HeaderFooter_HideOnTitle_SkipsTitleSlidesAndSetsFlag()
    {
        var presentation = Builder().Load();
        var titleSlide = presentation.Slides[2];
        Assert.True(presentation.ShowSpecialPlaceholdersOnTitleSlide);

        presentation.Execute(new SetHeaderFooterCommand(presentation.Slides, All with { HideOnTitleSlides = true }));

        Assert.False(presentation.ShowSpecialPlaceholdersOnTitleSlide);
        Assert.Equal(["Title 1"], titleSlide.Shapes.Select(s => s.Name));
        Assert.NotNull(Part(presentation.Slides[0], "ftr"));
        Assert.True(HeaderFooterOptions.From(presentation.Slides[0]).HideOnTitleSlides);

        presentation.Undo.Undo();
        Assert.True(presentation.ShowSpecialPlaceholdersOnTitleSlide);
        Assert.Equal("Old", Text(Part(titleSlide, "ftr")!));

        presentation.Execute(new SetHeaderFooterCommand([presentation.Slides[1]], All with { HideOnTitleSlides = true }));
        Assert.False(presentation.ShowSpecialPlaceholdersOnTitleSlide);
        Assert.Single(presentation.Slides[1].Shapes);
        Assert.True(presentation.Undo.CanUndo);
    }

    [Fact]
    public void HeaderFooter_FromSlide_ReadsState()
    {
        var presentation = Builder(" showSpecialPlsOnTitleSld=\"0\"").Load();

        Assert.Equal(new HeaderFooterOptions(true, false, "Jan 2027", false, true, "Footer", true), HeaderFooterOptions.From(presentation.Slides[3]));
        Assert.Equal(new HeaderFooterOptions(false, true, "", false, false, "", true), HeaderFooterOptions.From(presentation.Slides[0]));

        presentation.Execute(new SetHeaderFooterCommand([presentation.Slides[0]], All));
        Assert.Equal(All with { HideOnTitleSlides = false }, HeaderFooterOptions.From(presentation.Slides[0]));
    }

    [Fact]
    public void HeaderFooter_SameOptions_RecordsNoUndoStep()
    {
        var presentation = Builder().Load();

        presentation.Execute(new SetHeaderFooterCommand([presentation.Slides[3]], HeaderFooterOptions.From(presentation.Slides[3])));
        presentation.Execute(new SetHeaderFooterCommand([presentation.Slides[1]], All));

        Assert.False(presentation.Undo.CanUndo);
    }
}
