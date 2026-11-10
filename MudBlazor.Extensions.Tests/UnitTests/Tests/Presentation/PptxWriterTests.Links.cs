using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public partial class PptxWriterTests
{
    private const string Ms2007 = "http://schemas.microsoft.com/office/2007/relationships";
    private const string Ms2011 = "http://schemas.microsoft.com/office/2011/relationships";
    private static readonly XNamespace Mc = "http://schemas.openxmlformats.org/markup-compatibility/2006";
    private static readonly XNamespace A16 = "http://schemas.microsoft.com/office/drawing/2014/main";

    private const string Chart =
        "<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id=\"3\" name=\"Chart 2\"><a:extLst><a:ext uri=\"{FF2B5EF4-FFF2-40B4-BE49-F238E27FC236}\">" +
        "<a16:creationId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" id=\"{11111111-2222-3333-4444-555555555555}\"/></a:ext></a:extLst></p:cNvPr>" +
        "<p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr><p:xfrm><a:off x=\"1\" y=\"2\"/><a:ext cx=\"3\" cy=\"4\"/></p:xfrm><a:graphic>" +
        "<a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" r:id=\"rId3\"/>" +
        "</a:graphicData></a:graphic></p:graphicFrame>";

    private const string SmartArt =
        "<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id=\"4\" name=\"Diagram 3\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
        "<p:xfrm><a:off x=\"5\" y=\"6\"/><a:ext cx=\"7\" cy=\"8\"/></p:xfrm><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\">" +
        "<dgm:relIds xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\" r:dm=\"rId4\" r:lo=\"rId5\" r:qs=\"rId6\" r:cs=\"rId7\"/></a:graphicData></a:graphic></p:graphicFrame>";

    private const string Picture =
        "<p:pic><p:nvPicPr><p:cNvPr id=\"5\" name=\"Picture 4\"/><p:cNvPicPr/><p:nvPr/></p:nvPicPr><p:blipFill><a:blip r:embed=\"rId9\"/><a:stretch><a:fillRect/></a:stretch>" +
        "</p:blipFill><p:spPr><a:xfrm><a:off x=\"9\" y=\"9\"/><a:ext cx=\"9\" cy=\"9\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr></p:pic>";

    private const string SlideZoom =
        "<mc:AlternateContent xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\">" +
        "<mc:Choice xmlns:pslz=\"http://schemas.microsoft.com/office/powerpoint/2016/slidezoom\" Requires=\"pslz\"><p:graphicFrame><p:nvGraphicFramePr>" +
        "<p:cNvPr id=\"3\" name=\"Slide Zoom 2\"><a:hlinkClick r:id=\"rId5\" action=\"ppaction://hlinksldjump\"/></p:cNvPr><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
        "<p:xfrm><a:off x=\"1\" y=\"1\"/><a:ext cx=\"2\" cy=\"2\"/></p:xfrm><a:graphic><a:graphicData uri=\"http://schemas.microsoft.com/office/powerpoint/2016/slidezoom\">" +
        "<pslz:sldZm><pslz:sldZmObj sldId=\"257\" cId=\"0\"><pslz:zmPr><p166:blipFill xmlns:p166=\"http://schemas.microsoft.com/office/powerpoint/2016/6/main\">" +
        "<a:blip r:embed=\"rId6\"/><a:stretch><a:fillRect/></a:stretch></p166:blipFill></pslz:zmPr></pslz:sldZmObj></pslz:sldZm></a:graphicData></a:graphic>" +
        "</p:graphicFrame></mc:Choice><mc:Fallback><p:pic><p:nvPicPr><p:cNvPr id=\"3\" name=\"Slide Zoom 2\"><a:hlinkClick r:id=\"rId5\" action=\"ppaction://hlinksldjump\"/>" +
        "</p:cNvPr><p:cNvPicPr/><p:nvPr/></p:nvPicPr><p:blipFill><a:blip r:embed=\"rId6\"/><a:stretch><a:fillRect/></a:stretch></p:blipFill><p:spPr>" +
        "<a:xfrm><a:off x=\"1\" y=\"1\"/><a:ext cx=\"2\" cy=\"2\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr></p:pic></mc:Fallback></mc:AlternateContent>";

    [Fact]
    public void Writer_DeleteSlide_RemovesJumpLinksOnOtherSlides()
    {
        var source = LinkedDeck().Build();
        var presentation = Load(source);
        presentation.Execute(new DeleteSlidesCommand([presentation.Slides[1]]));

        var output = Save(presentation);

        AssertConsistent(output);
        var rels = Rels(output, "/ppt/slides/_rels/slide1.xml.rels");
        Assert.DoesNotContain(rels, r => r.Id == "rId5");
        Assert.Contains(rels, r => r.Id == "rId7" && r.Target == "slide3.xml");
        var slide = Xml(output, "/ppt/slides/slide1.xml");
        Assert.Empty(slide.Descendants(Mc + "AlternateContent"));
        Assert.Equal(["Go to two", "Go to three"], slide.Descendants(A + "t").Select(t => t.Value));
        Assert.Equal(["rId7"], slide.Descendants(A + "hlinkClick").Select(h => (string)h.Attribute(R + "id")));
        Assert.Equal("de-DE", (string)slide.Descendants(A + "rPr").First().Attribute("lang"));
        Assert.Equal(["Agenda", "Keep"], slide.Descendants(P + "cNvPr").Skip(1).Select(c => (string)c.Attribute("name")));
        var notes = Xml(output, "/ppt/notesSlides/notesSlide3.xml");
        Assert.Empty(notes.Descendants(A + "hlinkClick"));
        Assert.Contains("to two", notes.Descendants(A + "t").Select(t => t.Value));
        Assert.DoesNotContain(Rels(output, "/ppt/notesSlides/_rels/notesSlide3.xml.rels"), r => r.Target.Contains("slide2", StringComparison.Ordinal));
        Assert.Equal(["Agenda", "Three"], Load(output).Slides.Select(s => s.Shapes[0].Name));
    }

    [Fact]
    public void Writer_DeleteSlide_RemovesViewPropsOutlineEntry()
    {
        var builder = new PptxBuilder { PresentationRels = Rel("rId90", "viewProps", "viewProps.xml") };
        builder.ExtraParts["ppt/viewProps.xml"] = ("officedocument.presentationml.viewProps",
            $"<p:viewPr {Ns}><p:outlineViewPr><p:cViewPr><p:scale><a:sx n=\"33\" d=\"100\"/><a:sy n=\"33\" d=\"100\"/></p:scale><p:origin x=\"0\" y=\"0\"/></p:cViewPr>" +
            "<p:sldLst><p:sld r:id=\"rId1\" collapse=\"1\"/><p:sld r:id=\"rId2\"/></p:sldLst></p:outlineViewPr></p:viewPr>");
        builder.ExtraParts["ppt/_rels/viewProps.xml.rels"] = (null, RelsXml(Rel("rId1", "slide", "slides/slide1.xml") + Rel("rId2", "slide", "slides/slide2.xml")));
        var presentation = builder.Slide(Shape(2, "One", Xfrm(0, 0, 10, 10))).Slide(Shape(2, "Two", Xfrm(0, 0, 10, 10))).Load();
        presentation.Execute(new DeleteSlidesCommand([presentation.Slides[1]]));

        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Equal(["rId1"], Xml(output, "/ppt/viewProps.xml").Descendants(P + "sld").Select(s => (string)s.Attribute(R + "id")));
        Assert.Equal(["rId1"], Rels(output, "/ppt/_rels/viewProps.xml.rels").Select(r => r.Id));
        Assert.Equal("1", (string)Xml(output, "/ppt/viewProps.xml").Descendants(P + "sld").Single().Attribute("collapse"));
    }

    [Fact]
    public void Writer_EditedDeck_NoRelationshipTargetsMissingPart()
    {
        var builder = LinkedDeck();
        builder.Slide(Chart + SmartArt + Picture, rels: ChartRels);
        AddChartParts(builder);
        var presentation = Load(builder.Build());
        var agenda = presentation.Slides[0];
        var text = ((AutoShape)agenda.Shapes[0]).Text;
        presentation.Execute(new SetTextCommand(agenda, 2, text with { Paragraphs = [.. text.Paragraphs, new TextParagraph { Runs = [new TextRun("More")] }] }));
        presentation.Execute(new DeleteSlidesCommand([presentation.Slides[1]]));
        presentation.Execute(new DuplicateShapesCommand(presentation.Slides[2], [3, 4], 1000));
        presentation.Execute(new DuplicateSlidesCommand([presentation.Slides[0], presentation.Slides[2]]));

        var output = Save(presentation);

        AssertNoDanglingRelationships(output);
        AssertConsistent(output);
        Assert.Empty(Xml(output, "/ppt/slides/slide1.xml").Descendants(Mc + "AlternateContent"));
    }

    [Fact]
    public void Writer_DuplicateSlide_DeepCopiesChartAndSmartArtParts()
    {
        var builder = new PptxBuilder().Slide(Chart + SmartArt + Picture, rels: ChartRels);
        AddChartParts(builder);
        var source = builder.Build();
        var presentation = Load(source);
        presentation.Execute(new DuplicateSlidesCommand([presentation.Slides[0]]));

        var output = Save(presentation);

        AssertConsistent(output);
        var before = Parts(source);
        var after = Parts(output);
        Assert.True(before["/ppt/slides/_rels/slide1.xml.rels"].SequenceEqual(after["/ppt/slides/_rels/slide1.xml.rels"]));
        var opc = OpcPackage.Open(output);
        var copy = opc.Relationships("/ppt/slides/slide2.xml");
        string Target(string part, string id) => opc.ResolveTarget(part, opc.Relationships(part).Single(r => r.Id == id).Target);
        var chart = Target("/ppt/slides/slide2.xml", "rId3");
        Assert.NotEqual("/ppt/charts/chart1.xml", chart);
        Assert.Equal(before["/ppt/charts/chart1.xml"], after[chart]);
        Assert.Equal("application/vnd.openxmlformats-officedocument.drawingml.chart+xml", opc.ContentType(chart));
        foreach (var (id, original) in new[] { ("rId1", "/ppt/embeddings/Microsoft_Excel_Worksheet.xlsx"), ("rId2", "/ppt/charts/style1.xml"), ("rId3", "/ppt/charts/colors1.xml") })
        {
            var copied = Target(chart, id);
            Assert.NotEqual(original, copied);
            Assert.Equal(before[original], after[copied]);
            Assert.Equal(OpcPackage.Open(source).ContentType(original), opc.ContentType(copied));
        }

        foreach (var (id, original) in new[]
                 {
                     ("rId4", "/ppt/diagrams/data1.xml"), ("rId5", "/ppt/diagrams/layout1.xml"), ("rId6", "/ppt/diagrams/quickStyle1.xml"),
                     ("rId7", "/ppt/diagrams/colors1.xml"), ("rId8", "/ppt/diagrams/drawing1.xml")
                 })
        {
            var copied = Target("/ppt/slides/slide2.xml", id);
            Assert.NotEqual(original, copied);
            Assert.Equal(before[original], after[copied]);
        }

        Assert.Equal("/ppt/media/image9.png", Target("/ppt/slides/slide2.xml", "rId9"));
        Assert.Equal(copy.Count, Rels(source, "/ppt/slides/_rels/slide1.xml.rels").Count);
    }

    [Fact]
    public void Writer_DuplicateSlide_NewCreationId_NoComments()
    {
        const string tail = "<p:extLst><p:ext uri=\"{BB962C8B-B14F-4D97-AF65-F5344CB8AC3E}\"><p14:creationId xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\" val=\"123\"/>" +
                            "</p:ext><p:ext uri=\"{6950BFC3-D8DA-4A85-94F7-54DA5524770B}\"><p188:commentRel xmlns:p188=\"http://schemas.microsoft.com/office/powerpoint/2018/8/main\" r:id=\"rId20\"/>" +
                            "</p:ext></p:extLst>";
        var builder = new PptxBuilder().Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10)), tail: tail,
            rels: FullRel("rId20", "http://schemas.microsoft.com/office/2018/10/relationships/comments", "../comments/modernComment_1.xml") +
                  Rel("rId21", "comments", "../comments/comment1.xml"));
        builder.ExtraParts["ppt/comments/modernComment_1.xml"] = ("application/vnd.ms-powerpoint.comments+xml", "<p188:cmLst xmlns:p188=\"http://schemas.microsoft.com/office/powerpoint/2018/8/main\"/>");
        builder.ExtraParts["ppt/comments/comment1.xml"] = ("officedocument.presentationml.comments", $"<p:cmLst {Ns}/>");
        var source = builder.Build();
        var presentation = Load(source);
        presentation.Execute(new DuplicateSlidesCommand([presentation.Slides[0]]));
        presentation.Execute(new DuplicateSlidesCommand([presentation.Slides[0]]));

        var output = Save(presentation);

        AssertConsistent(output);
        var ids = new[] { "/ppt/slides/slide1.xml", "/ppt/slides/slide2.xml", "/ppt/slides/slide3.xml" }
            .Select(part => (string)Xml(output, part).Descendants(P14 + "creationId").Single().Attribute("val")).ToList();
        Assert.Equal("123", ids[0]);
        Assert.Equal(3, ids.Distinct().Count());
        Assert.All(ids, id => Assert.True(uint.TryParse(id, out _)));
        foreach (var part in new[] { "/ppt/slides/slide2.xml", "/ppt/slides/slide3.xml" })
        {
            Assert.DoesNotContain(Rels(output, OpcPackage.RelsPartOf(part)), r => r.Type.EndsWith("/comments", StringComparison.Ordinal));
            var extensions = Xml(output, part).Root!.Element(P + "extLst")!.Elements(P + "ext").ToList();
            Assert.Equal(["{BB962C8B-B14F-4D97-AF65-F5344CB8AC3E}"], extensions.Select(e => (string)e.Attribute("uri")));
        }

        Assert.True(Parts(source)["/ppt/slides/slide1.xml"].SequenceEqual(Parts(output)["/ppt/slides/slide1.xml"]));
    }

    [Fact]
    public void Writer_DuplicateChartShape_GetsOwnChartPart()
    {
        var builder = new PptxBuilder().Slide(Chart + SmartArt + Picture, rels: ChartRels);
        AddChartParts(builder);
        var source = builder.Build();
        var presentation = Load(source);
        presentation.Execute(new DuplicateShapesCommand(presentation.Slides[0], [3, 4, 5], 1000));

        var output = Save(presentation);

        AssertConsistent(output);
        var opc = OpcPackage.Open(output);
        const string slide = "/ppt/slides/slide1.xml";
        string Target(string part, string id) => opc.ResolveTarget(part, opc.Relationships(part).Single(r => r.Id == id).Target);
        var xml = Xml(output, slide);
        var charts = xml.Descendants().Where(e => e.Name.LocalName == "chart").Select(c => (string)c.Attribute(R + "id")).ToList();
        Assert.Equal("rId3", charts[0]);
        Assert.NotEqual(charts[0], charts[1]);
        var copiedChart = Target(slide, charts[1]);
        Assert.NotEqual("/ppt/charts/chart1.xml", copiedChart);
        Assert.NotEqual("/ppt/embeddings/Microsoft_Excel_Worksheet.xlsx", Target(copiedChart, "rId1"));
        var diagrams = xml.Descendants().Where(e => e.Name.LocalName == "relIds").ToList();
        Assert.Equal("rId4", (string)diagrams[0].Attribute(R + "dm"));
        var copiedData = Target(slide, (string)diagrams[1].Attribute(R + "dm"));
        Assert.NotEqual("/ppt/diagrams/data1.xml", copiedData);
        var drawingId = (string)opc.ReadXml(copiedData).Descendants().Single(e => e.Name.LocalName == "dataModelExt").Attribute("relId");
        Assert.NotEqual("rId8", drawingId);
        Assert.NotEqual("/ppt/diagrams/drawing1.xml", Target(slide, drawingId));
        Assert.Equal("rId8", (string)opc.ReadXml("/ppt/diagrams/data1.xml").Descendants().Single(e => e.Name.LocalName == "dataModelExt").Attribute("relId"));
        var pictures = xml.Descendants(A + "blip").Select(b => (string)b.Attribute(R + "embed")).ToList();
        Assert.Equal(["rId9", "rId9"], pictures);
        var creationIds = xml.Descendants(A16 + "creationId").Select(c => (string)c.Attribute("id")).ToList();
        Assert.Equal(2, creationIds.Distinct().Count());
    }

    [Fact]
    public void Writer_EditedField_BecomesPlainRun_InvalidColorKeepsFill()
    {
        var presentation = new PptxBuilder()
            .Slide(Shape(2, "Text", Xfrm(0, 0, 100, 100) + "<a:solidFill><a:srgbClr val=\"FF0000\"/></a:solidFill>",
                "<a:p><a:fld id=\"{B6F15528-21DE-4FAA-801E-634DDDAF4B2B}\" type=\"slidenum\"><a:rPr lang=\"de-DE\"/><a:t>7</a:t></a:fld></a:p>"))
            .Load();
        var slide = presentation.Slides[0];
        var body = ((AutoShape)slide.Shapes[0]).Text;
        presentation.Execute(new SetTextCommand(slide, 2, body with
        {
            Paragraphs = [body.Paragraphs[0] with { Runs = [body.Paragraphs[0].Runs[0] with { Text = "Seven" }] }]
        }));
        presentation.Execute(new SetShapeStyleCommand(slide, [2], new ShapeFill(ShapeFillKind.Solid, "not a colour"), null));

        var xml = Xml(Save(presentation), "/ppt/slides/slide1.xml");

        Assert.Empty(xml.Descendants(A + "fld"));
        var run = xml.Descendants(A + "r").Single();
        Assert.Empty(run.Attributes());
        Assert.Equal("de-DE", (string)run.Element(A + "rPr")!.Attribute("lang"));
        Assert.Equal("Seven", run.Element(A + "t")!.Value);
        Assert.Equal("FF0000", (string)xml.Descendants(P + "spPr").Single().Element(A + "solidFill")!.Element(A + "srgbClr")!.Attribute("val"));
    }

    private static string ChartRels =>
        Rel("rId3", "chart", "../charts/chart1.xml") + Rel("rId4", "diagramData", "../diagrams/data1.xml") + Rel("rId5", "diagramLayout", "../diagrams/layout1.xml") +
        Rel("rId6", "diagramQuickStyle", "../diagrams/quickStyle1.xml") + Rel("rId7", "diagramColors", "../diagrams/colors1.xml") +
        FullRel("rId8", Ms2007 + "/diagramDrawing", "../diagrams/drawing1.xml") + Rel("rId9", "image", "../media/image9.png");

    private static void AddChartParts(PptxBuilder builder)
    {
        const string dgm = "xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\"";
        builder.ExtraParts["ppt/charts/chart1.xml"] = ("officedocument.drawingml.chart",
            "<c:chartSpace xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<c:externalData r:id=\"rId1\"/></c:chartSpace>");
        builder.ExtraParts["ppt/charts/_rels/chart1.xml.rels"] = (null, RelsXml(Rel("rId1", "package", "../embeddings/Microsoft_Excel_Worksheet.xlsx") +
                                                                            FullRel("rId2", Ms2011 + "/chartStyle", "style1.xml") +
                                                                            FullRel("rId3", Ms2011 + "/chartColorStyle", "colors1.xml")));
        builder.ExtraParts["ppt/charts/style1.xml"] = ("application/vnd.ms-office.chartstyle+xml", "<cs:chartStyle xmlns:cs=\"http://schemas.microsoft.com/office/drawing/2012/chartStyle\"/>");
        builder.ExtraParts["ppt/charts/colors1.xml"] = ("application/vnd.ms-office.chartcolorstyle+xml", "<cs:colorStyle xmlns:cs=\"http://schemas.microsoft.com/office/drawing/2012/chartStyle\"/>");
        builder.ExtraParts["ppt/embeddings/Microsoft_Excel_Worksheet.xlsx"] = ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "workbook bytes");
        builder.ExtraParts["ppt/diagrams/data1.xml"] = ("officedocument.drawingml.diagramData",
            $"<dgm:dataModel {dgm} xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"><dgm:extLst><a:ext uri=\"http://schemas.microsoft.com/office/drawing/2008/diagram\">" +
            "<dsp:dataModelExt xmlns:dsp=\"http://schemas.microsoft.com/office/drawing/2008/diagram\" relId=\"rId8\" minVer=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\"/>" +
            "</a:ext></dgm:extLst></dgm:dataModel>");
        builder.ExtraParts["ppt/diagrams/layout1.xml"] = ("officedocument.drawingml.diagramLayout", $"<dgm:layoutDef {dgm}/>");
        builder.ExtraParts["ppt/diagrams/quickStyle1.xml"] = ("officedocument.drawingml.diagramStyle", $"<dgm:styleDef {dgm}/>");
        builder.ExtraParts["ppt/diagrams/colors1.xml"] = ("officedocument.drawingml.diagramColors", $"<dgm:colorsDef {dgm}/>");
        builder.ExtraParts["ppt/diagrams/drawing1.xml"] = ("application/vnd.ms-office.drawingml.diagramDrawing+xml",
            "<dsp:drawing xmlns:dsp=\"http://schemas.microsoft.com/office/drawing/2008/diagram\"/>");
        builder.ExtraParts["ppt/media/image9.png"] = (null, "not really an image");
    }

    private static PptxBuilder LinkedDeck()
    {
        var text = Shape(2, "Agenda", Xfrm(0, 0, 100, 100),
            "<a:p><a:r><a:rPr lang=\"de-DE\"><a:hlinkClick r:id=\"rId5\" action=\"ppaction://hlinksldjump\"/></a:rPr><a:t>Go to two</a:t></a:r>" +
            "<a:r><a:rPr><a:hlinkClick r:id=\"rId7\" action=\"ppaction://hlinksldjump\"/></a:rPr><a:t>Go to three</a:t></a:r></a:p>");
        var builder = new PptxBuilder()
            .Slide(text + SlideZoom + Shape(4, "Keep", Xfrm(0, 0, 10, 10)),
                rels: Rel("rId5", "slide", "slide2.xml") + Rel("rId6", "image", "../media/image8.png") + Rel("rId7", "slide", "slide3.xml"))
            .Slide(Shape(2, "Two", Xfrm(0, 0, 10, 10)))
            .Slide(Shape(2, "Three", Xfrm(0, 0, 10, 10)),
                notes: "Back</a:t></a:r><a:r><a:rPr><a:hlinkClick r:id=\"rId9\" action=\"ppaction://hlinksldjump\"/></a:rPr><a:t>to two");
        builder.NotesRels[3] = Rel("rId9", "slide", "../slides/slide2.xml");
        builder.ExtraParts["ppt/media/image8.png"] = (null, "zoom preview");
        return builder;
    }

    private static string FullRel(string id, string type, string target) => $"<Relationship Id=\"{id}\" Type=\"{type}\" Target=\"{target}\"/>";

    private static string RelsXml(string rels) => $"<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">{rels}</Relationships>";
}
