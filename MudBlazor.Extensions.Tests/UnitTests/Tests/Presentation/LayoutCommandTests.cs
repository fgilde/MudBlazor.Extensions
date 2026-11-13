using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class LayoutCommandTests
{
    private const string Footers =
        "<p:sp><p:nvSpPr><p:cNvPr id=\"10\" name=\"Date Placeholder 9\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"dt\" sz=\"half\" idx=\"10\"/></p:nvPr></p:nvSpPr>" +
        "<p:spPr><a:xfrm><a:off x=\"100\" y=\"6000000\"/><a:ext cx=\"2000000\" cy=\"300000\"/></a:xfrm></p:spPr><p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody></p:sp>" +
        "<p:sp><p:nvSpPr><p:cNvPr id=\"11\" name=\"Footer Placeholder 10\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"ftr\" sz=\"quarter\" idx=\"11\"/></p:nvPr></p:nvSpPr>" +
        "<p:spPr><a:xfrm><a:off x=\"4000000\" y=\"6000000\"/><a:ext cx=\"4000000\" cy=\"300000\"/></a:xfrm></p:spPr><p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody></p:sp>" +
        "<p:sp><p:nvSpPr><p:cNvPr id=\"12\" name=\"Slide Number Placeholder 11\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"sldNum\" sz=\"quarter\" idx=\"12\"/></p:nvPr></p:nvSpPr>" +
        "<p:spPr><a:xfrm><a:off x=\"9000000\" y=\"6000000\"/><a:ext cx=\"2000000\" cy=\"300000\"/></a:xfrm></p:spPr><p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody></p:sp>";

    private const string Title = "<a:p><a:r><a:t>Hello</a:t></a:r></a:p>";

    private static readonly Dictionary<string, Func<Presentation, IDocumentCommand<Presentation>>> Cases = new()
    {
        ["SetSlideLayout"] = p => new SetSlideLayoutCommand([p.Slides[0], p.Slides[1]], p.Layouts[2]),
        ["SetSlideLayoutTwoContent"] = p => new SetSlideLayoutCommand([p.Slides[0]], p.Layouts[1]),
        ["ResetSlide"] = p => new ResetSlideCommand([p.Slides[0], p.Slides[2]])
    };

    public static TheoryData<string> CommandNames => new(Cases.Keys);

    internal static PptxBuilder Builder()
    {
        var builder = new PptxBuilder
        {
            LayoutShapes = Placeholder(2, "Title 1", "type=\"title\"", Xfrm(1000000, 500000, 8000000, 1000000)) +
                           Placeholder(3, "Content Placeholder 2", "idx=\"1\"", Xfrm(1000000, 1800000, 8000000, 4000000)) + Footers
        };
        builder.ExtraLayouts.Add(("twoObj", "Two Content",
            Placeholder(2, "Title 1", "type=\"title\"", Xfrm(1000000, 500000, 8000000, 1000000)) +
            Placeholder(3, "Content Placeholder 2", "sz=\"half\" idx=\"1\"", Xfrm(1000000, 1800000, 3900000, 4000000)) +
            Placeholder(4, "Content Placeholder 3", "sz=\"half\" idx=\"2\"", Xfrm(5100000, 1800000, 3900000, 4000000)) + Footers));
        builder.ExtraLayouts.Add(("titleOnly", "Title Only", Placeholder(2, "Title 1", "type=\"title\"", Xfrm(1000000, 300000, 8000000, 800000))));
        builder.ExtraLayouts.Add(("title", "Title Slide",
            Placeholder(2, "Title 1", "type=\"ctrTitle\"", Xfrm(1500000, 1100000, 9000000, 2300000)) +
            Placeholder(3, "Subtitle 2", "type=\"subTitle\" idx=\"1\"", Xfrm(1500000, 3600000, 9000000, 1600000)) + Footers));
        return builder;
    }

    private static Presentation Deck() => Builder()
        .Slide(Placeholder(2, "Title 1", "type=\"title\"", Xfrm(10, 20, 30, 40), Title) +
               Placeholder(3, "Content Placeholder 2", "idx=\"1\"", "", "<a:p><a:r><a:t>Body</a:t></a:r></a:p>") + Shape(4, "Free", Xfrm(5, 6, 7, 8)))
        .Slide(Placeholder(2, "Title 1", "type=\"title\"", "", Title) + Placeholder(3, "Content Placeholder 2", "idx=\"1\"", "", "<a:p><a:r><a:t> </a:t></a:r></a:p>"))
        .Slide(Placeholder(2, "Title 1", "type=\"ctrTitle\"", Xfrm(1, 2, 3, 4), Title) + Placeholder(3, "Subtitle 2", "type=\"subTitle\" idx=\"1\"", "", "<a:p/>")
               .Replace("<a:bodyPr/>", "<a:bodyPr anchor=\"ctr\" lIns=\"0\" wrap=\"none\"/>"), layout: 4)
        .Load();

    private static T Ph<T>(Slide slide, string type) where T : SlideShape => slide.Shapes.OfType<T>().Single(s => s.Placeholder?.Type == type);

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
    public void SetLayout_MapsPlaceholdersByType_RemovesEmptyUnmatched_KeepsFilledUnmatched()
    {
        var presentation = Deck();
        var titleOnly = presentation.Layouts[2];
        var first = presentation.Slides[0];
        var body = Ph<AutoShape>(first, "obj");
        var bodyPosition = body.ResolvedTransform;
        var title = Ph<AutoShape>(first, "title");

        presentation.Execute(new SetSlideLayoutCommand([first, presentation.Slides[1]], titleOnly));

        Assert.Same(titleOnly, first.Layout);
        Assert.Null(title.Transform);
        Assert.Same(titleOnly.ShapeList.Single(), title.InheritsFrom);
        Assert.Equal(new ShapeTransform(1000000, 300000, 8000000, 800000), title.ResolvedTransform);
        Assert.Equal("Hello", title.Text!.Paragraphs[0].Runs[0].Text);
        Assert.Equal(bodyPosition, body.Transform);
        Assert.Contains(body, first.Shapes);
        Assert.Equal(new ShapeTransform(5, 6, 7, 8), first.Shapes.Single(s => s.Name == "Free").Transform);
        Assert.Equal(["Title 1"], presentation.Slides[1].Shapes.Select(s => s.Name));

        var twoContent = presentation.Layouts[1];
        presentation.Execute(new SetSlideLayoutCommand([presentation.Slides[1]], twoContent));
        var slide = presentation.Slides[1];
        Assert.Equal(["Title 1", "Content Placeholder 2", "Content Placeholder 3"], slide.Shapes.Select(s => s.Name));
        var added = slide.Shapes.OfType<AutoShape>().Where(s => s.Placeholder?.Type == "obj").ToList();
        Assert.Equal([1u, 2u], added.Select(s => s.Placeholder.Index!.Value));
        Assert.All(added, s => Assert.Null(s.Transform));
        Assert.All(added, s => Assert.Equal("", string.Concat(s.Text!.Paragraphs.SelectMany(p => p.Runs).Select(r => r.Text))));
        Assert.Equal(3, slide.Shapes.Select(s => s.Id).Distinct().Count());

        var titleSlide = presentation.Slides[2];
        presentation.Execute(new SetSlideLayoutCommand([titleSlide], presentation.Layouts[0]));
        var mapped = titleSlide.Shapes.OfType<AutoShape>().ToList();
        Assert.Equal(new PlaceholderRef("title", null), mapped[0].Placeholder);
        Assert.Equal(new PlaceholderRef("obj", 1), mapped[1].Placeholder);
        Assert.Equal(2, mapped.Count);
    }

    [Fact]
    public void SetLayout_SameLayout_RecordsNoUndoStep()
    {
        var presentation = Deck();

        presentation.Execute(new SetSlideLayoutCommand([presentation.Slides[0]], presentation.Layouts[0]));

        Assert.False(presentation.Undo.CanUndo);
        Assert.Throws<ArgumentNullException>(() => presentation.Execute(new SetSlideLayoutCommand([presentation.Slides[0]], null!)));
    }

    [Fact]
    public void ResetSlide_RemovesOwnTransforms()
    {
        var presentation = Deck();
        var slide = presentation.Slides[2];
        var subtitle = Ph<AutoShape>(slide, "subTitle");
        Assert.Equal(TextAnchor.Middle, subtitle.Text!.Anchor);

        presentation.Execute(new ResetSlideCommand([slide, presentation.Slides[0]]));

        Assert.All(slide.Shapes, s => Assert.Null(s.Transform));
        Assert.Null(subtitle.Text!.Anchor);
        Assert.Null(subtitle.Text.InsetLeft);
        Assert.Null(subtitle.Text.Wrap);
        Assert.Null(Ph<AutoShape>(presentation.Slides[0], "title").Transform);
        Assert.Equal(new ShapeTransform(5, 6, 7, 8), presentation.Slides[0].Shapes.Single(s => s.Name == "Free").Transform);

        presentation.Undo.Undo();
        Assert.Equal(TextAnchor.Middle, subtitle.Text!.Anchor);
        Assert.Equal(new ShapeTransform(1, 2, 3, 4), slide.Shapes[0].Transform);

        presentation.Execute(new ResetSlideCommand([presentation.Slides[1]]));
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public void SetLayout_TableInContentPlaceholder_NeverMovesIntoATextPlaceholder()
    {
        var table = TableTests.Table(5, "", [1000000], TableTests.Tc("a")).Replace("<p:nvPr/>", "<p:nvPr><p:ph idx=\"1\"/></p:nvPr>");
        var presentation = Builder().Slide(Placeholder(2, "Title 1", "type=\"title\"", "", Title) + table).Load();
        var slide = presentation.Slides[0];
        var before = slide.Shapes.OfType<TableShape>().Single().ResolvedTransform;

        presentation.Execute(new SetSlideLayoutCommand([slide], presentation.Layouts[3]));

        var moved = slide.Shapes.OfType<TableShape>().Single();
        Assert.Equal("obj", moved.Placeholder!.Type);
        Assert.Equal(before, moved.Transform);
        Assert.Contains(slide.Shapes, s => s is AutoShape { Placeholder.Type: "subTitle" });
    }
}
