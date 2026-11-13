using System.Xml.Linq;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public partial class PptxWriterTests
{
    private const string GuideExt = "<p:ext uri=\"{EFAFB233-063F-42B5-8137-9DF3F51BA10A}\"><p15:sldGuideLst xmlns:p15=\"http://schemas.microsoft.com/office/powerpoint/2012/main\"/></p:ext>";

    private static XElement SectionExt(byte[] package) =>
        Xml(package, "/ppt/presentation.xml").Root!.Element(P + "extLst")?.Elements(P + "ext").SingleOrDefault(e => (string)e.Attribute("uri") == SectionTests.SectionsUri);

    private static void AssertSectionsListEverySlideOnce(byte[] package)
    {
        var xml = Xml(package, "/ppt/presentation.xml");
        var slideIds = xml.Root!.Element(P + "sldIdLst")?.Elements(P + "sldId").Select(e => (string)e.Attribute("id")).ToList() ?? [];
        Assert.Equal(slideIds, xml.Descendants(P14 + "section").SelectMany(s => s.Descendants(P14 + "sldId")).Select(e => (string)e.Attribute("id")));
    }

    private static string SectionLayout(Presentation presentation) =>
        string.Join(" | ", presentation.Sections.Select(s => $"{s.Id} {s.Name}:{string.Join(",", s.Slides.Select(x => x.Shapes[0].Name))}"));

    [Fact]
    public void Writer_Sections_FromModel_RoundTrip()
    {
        var source = SectionTests.Builder(6, SectionTests.SectionList(("A", [256, 257]), ("B", [258, 259]), ("C", [260, 261]))).Build();
        var presentation = Load(source);
        Assert.True(Parts(Save(presentation)).All(p => Parts(source)[p.Key].SequenceEqual(p.Value)));

        presentation.Execute(new MoveSlidesCommand([presentation.Slides[0]], 5));
        presentation.Execute(new AddSectionCommand(presentation.Slides[1], "New"));
        presentation.Execute(new MoveSectionCommand(presentation.Sections[3], 0));
        presentation.Execute(new RemoveSectionCommand(presentation.Sections[2], false));
        presentation.Execute(new RemoveSectionCommand(presentation.Sections[1], true));
        presentation.Execute(new RenameSectionCommand(presentation.Sections[0], "Renamed & <quoted>"));
        presentation.Execute(new AddSlideCommand(presentation.Slides.Count, presentation.Layouts[0]));

        var output = Save(presentation);

        AssertConsistent(output);
        AssertSectionsListEverySlideOnce(output);
        var names = Xml(output, "/ppt/presentation.xml").Descendants(P14 + "section").Select(s => (string)s.Attribute("name")).ToList();
        Assert.Equal(presentation.Sections.Select(s => s.Name), names);
        var reloaded = Load(output);
        Assert.Equal(SectionLayout(presentation), SectionLayout(reloaded));
        Assert.Equal(presentation.Sections.Sum(s => s.Slides.Count), presentation.Slides.Count);
    }

    [Fact]
    public void Writer_NoSections_RemovesOnlySectionExt()
    {
        var sections = SectionTests.SectionList(("A", [256]), ("B", [257]));
        var presentation = SectionTests.Builder(2, sections.Replace("</p:extLst>", GuideExt + "</p:extLst>")).Load();

        presentation.Execute(new RemoveAllSectionsCommand());
        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Null(SectionExt(output));
        var ext = Assert.Single(Xml(output, "/ppt/presentation.xml").Root!.Element(P + "extLst")!.Elements());
        Assert.Equal("{EFAFB233-063F-42B5-8137-9DF3F51BA10A}", (string)ext.Attribute("uri"));

        var only = SectionTests.Builder(2, sections).Load();
        only.Execute(new RemoveAllSectionsCommand());
        Assert.Null(Xml(Save(only), "/ppt/presentation.xml").Root!.Element(P + "extLst"));

        var plain = SectionTests.Builder(3).Load();
        plain.Execute(new MoveSlidesCommand([plain.Slides[0]], 3));
        plain.Execute(new DuplicateSlidesCommand([plain.Slides[1]]));
        plain.Execute(new DeleteSlidesCommand([plain.Slides[2]]));
        var plainOutput = Save(plain);
        AssertConsistent(plainOutput);
        Assert.Empty(Xml(plainOutput, "/ppt/presentation.xml").Descendants(P14 + "sectionLst"));
        Assert.Null(Xml(plainOutput, "/ppt/presentation.xml").Root!.Element(P + "extLst"));

        plain.Execute(new AddSectionCommand(plain.Slides[1], "Later"));
        var added = Save(plain);
        AssertConsistent(added);
        AssertSectionsListEverySlideOnce(added);
        Assert.Equal(["Default Section", "Later"], SectionExt(added)!.Descendants(P14 + "section").Select(s => (string)s.Attribute("name")));
        Assert.Equal(P + "extLst", Xml(added, "/ppt/presentation.xml").Root!.Elements().Last().Name);
    }

    [Fact]
    public void Writer_SectionsAfterSlideDeletesAndMoves_ListEverySlideOnce()
    {
        var presentation = SectionTests.Builder(6, SectionTests.SectionList(("A", [256, 257]), ("B", [258, 259]), ("C", [260, 261]))).Load();

        presentation.Execute(new DeleteSlidesCommand([presentation.Slides[2], presentation.Slides[3]]));
        presentation.Execute(new MoveSlidesCommand([presentation.Slides[0], presentation.Slides[3]], 2));
        presentation.Execute(new DuplicateSlidesCommand([presentation.Slides[0]]));
        presentation.Execute(new MoveSectionCommand(presentation.Sections[2], 0));
        var output = Save(presentation);

        AssertConsistent(output);
        AssertSectionsListEverySlideOnce(output);
        Assert.Equal(["C", "A", "B"], SectionExt(output)!.Descendants(P14 + "section").Select(s => (string)s.Attribute("name")));
        Assert.Empty(SectionExt(output)!.Descendants(P14 + "section").ElementAt(2).Descendants(P14 + "sldId"));
        Assert.Equal(SectionLayout(presentation), SectionLayout(Load(output)));
    }

    [Fact]
    public void Writer_LayoutChangeAndReset_KeepXfrmOfTablePlaceholder()
    {
        var table = TableTests.Table(3, "", [1000000, 1000000], TableTests.Tc("a") + TableTests.Tc("b")).Replace("<p:nvPr/>", "<p:nvPr><p:ph idx=\"1\"/></p:nvPr>");
        PptxBuilder Builder() => LayoutCommandTests.Builder().Slide(PptxBuilder.Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p/>") + table);
        var presentation = Builder().Load();
        var frame = presentation.Slides[0].Shapes[1];

        presentation.Execute(new SetSlideLayoutCommand([presentation.Slides[0]], presentation.Layouts[1]));
        var output = Save(presentation);

        Assert.Equal(new ShapeTransform(1000000, 1800000, 3900000, 4000000), frame.Transform);
        AssertConsistent(output);
        Assert.Single(Xml(output, SlidePart).Descendants(P + "graphicFrame").Elements(P + "xfrm"));

        var reset = Builder().Load();
        reset.Execute(new ResetSlideCommand([reset.Slides[0]]));
        var resetOutput = Save(reset);
        Assert.Equal(new ShapeTransform(1000000, 1800000, 8000000, 4000000), reset.Slides[0].Shapes[1].Transform);
        AssertConsistent(resetOutput);
        Assert.Single(Xml(resetOutput, SlidePart).Descendants(P + "graphicFrame").Elements(P + "xfrm"));
    }

    [Fact]
    public void Writer_EmptySectionList_Unmodified_KeepsPresentationXml()
    {
        var source = SectionTests.Builder(2, SectionTests.SectionList()).Build();

        var output = Save(Load(source));

        Assert.Equal(Parts(source)["/ppt/presentation.xml"], Parts(output)["/ppt/presentation.xml"]);
    }

    [Fact]
    public void Writer_SetLayout_ChangesLayoutRelationship()
    {
        var presentation = LayoutCommandTests.Builder()
            .Slide(PptxBuilder.Placeholder(2, "Title 1", "type=\"title\"", PptxBuilder.Xfrm(1, 2, 3, 4), "<a:p><a:r><a:t>One</a:t></a:r></a:p>") +
                   PptxBuilder.Placeholder(3, "Content Placeholder 2", "idx=\"1\"", "", "<a:p><a:r><a:t>Body</a:t></a:r></a:p>"))
            .Slide(PptxBuilder.Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>Two</a:t></a:r></a:p>"))
            .Load();

        presentation.Execute(new SetSlideLayoutCommand([presentation.Slides[0]], presentation.Layouts[3]));
        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Equal("../slideLayouts/slideLayout4.xml", Rels(output, "/ppt/slides/_rels/slide1.xml.rels").Single(r => r.Type.EndsWith("/slideLayout")).Target);
        Assert.Equal("../slideLayouts/slideLayout1.xml", Rels(output, "/ppt/slides/_rels/slide2.xml.rels").Single(r => r.Type.EndsWith("/slideLayout")).Target);
        var slide = Xml(output, SlidePart);
        Assert.Equal(["ctrTitle", "subTitle"], slide.Descendants(P + "ph").Select(ph => (string)ph.Attribute("type")));
        Assert.Equal([null, "1"], slide.Descendants(P + "ph").Select(ph => (string?)ph.Attribute("idx")));
        Assert.Empty(slide.Descendants(A + "xfrm"));
        var reloaded = Load(output);
        Assert.Equal("Title Slide", reloaded.Slides[0].Layout!.Name);
        Assert.Equal(Describe(presentation), Describe(reloaded));
    }

    [Fact]
    public void Writer_HeaderFooter_RoundTrip()
    {
        var presentation = HeaderFooterTests.Builder().Load();
        var options = HeaderFooterTests.All with { HideOnTitleSlides = true };

        presentation.Execute(new SetHeaderFooterCommand(presentation.Slides, options));
        presentation.Execute(new SetHeaderFooterCommand([presentation.Slides[3]], options with { AutomaticDate = false, FixedDate = "Q1 & <2027>" }));
        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Equal("0", (string)Xml(output, "/ppt/presentation.xml").Root!.Attribute("showSpecialPlsOnTitleSld"));
        var slide = Xml(output, SlidePart);
        var fields = slide.Descendants(A + "fld").ToList();
        Assert.Equal(["datetime1", "slidenum"], fields.Select(f => (string)f.Attribute("type")));
        Assert.All(fields, f => Assert.Matches("^\\{[0-9A-F-]{36}\\}$", (string)f.Attribute("id")));
        Assert.Equal(["title", "dt", "ftr", "sldNum"], slide.Descendants(P + "ph").Select(ph => (string)ph.Attribute("type")));
        Assert.Equal(["10", "11", "12"], slide.Descendants(P + "ph").Skip(1).Select(ph => (string)ph.Attribute("idx")));
        Assert.DoesNotContain(Xml(output, "/ppt/slides/slide3.xml").Descendants(P + "ph"), ph => (string)ph.Attribute("type") == "ftr");

        var reloaded = Load(output);
        Assert.Equal(options with { FixedDate = "" }, HeaderFooterOptions.From(reloaded.Slides[0]));
        Assert.Equal(options with { AutomaticDate = false, FixedDate = "Q1 & <2027>" }, HeaderFooterOptions.From(reloaded.Slides[3]));
        Assert.Equal(Describe(presentation), Describe(reloaded));
        Assert.Equal(SlideCommandTests.Snapshot(presentation).Split('\n').Count(l => l.Contains("slidenum")),
            SlideCommandTests.Snapshot(reloaded).Split('\n').Count(l => l.Contains("slidenum")));

        presentation.Undo.Undo();
        presentation.Undo.Undo();
        var restored = Save(presentation);
        Assert.Null(Xml(restored, "/ppt/presentation.xml").Root!.Attribute("showSpecialPlsOnTitleSld"));
        Assert.Empty(Xml(restored, SlidePart).Descendants(A + "fld"));
    }
}
