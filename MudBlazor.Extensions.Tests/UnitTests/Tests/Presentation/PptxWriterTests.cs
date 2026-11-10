using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public partial class PptxWriterTests
{
    private static readonly XNamespace P = "http://schemas.openxmlformats.org/presentationml/2006/main";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace P14 = "http://schemas.microsoft.com/office/powerpoint/2010/main";
    private static readonly XNamespace PackageRels = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static string SamplePath => Path.Combine(AppContext.BaseDirectory, "TestData", "Presentation", "sample.pptx");

    [Fact]
    public void Writer_Unmodified_EveryPartContentIdentical()
    {
        var source = File.ReadAllBytes(SamplePath);
        var output = Save(Presentation.Load(new MemoryStream(source)));

        var before = Parts(source);
        var after = Parts(output);
        Assert.Equal(before.Keys.Order(), after.Keys.Order());
        foreach (var (name, bytes) in before) Assert.True(bytes.SequenceEqual(after[name]), name);
    }

    [Fact]
    public void Writer_EditedSlide_KeepsUnknownElementsAndParts()
    {
        const string chart = "<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id=\"3\" name=\"Chart 2\"/><p:cNvGraphicFramePr/><p:nvPr><p:custDataLst><p:tags r:id=\"rId8\"/></p:custDataLst></p:nvPr>" +
                             "</p:nvGraphicFramePr><p:xfrm><a:off x=\"1\" y=\"2\"/><a:ext cx=\"3\" cy=\"4\"/></p:xfrm><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\">" +
                             "<c:chart xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" r:id=\"rId3\"/></a:graphicData></a:graphic></p:graphicFrame>";
        const string smartArt = "<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id=\"4\" name=\"Diagram 3\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
                                "<p:xfrm><a:off x=\"5\" y=\"6\"/><a:ext cx=\"7\" cy=\"8\"/></p:xfrm><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\">" +
                                "<dgm:relIds xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\" r:dm=\"rId4\" r:lo=\"rId5\" r:qs=\"rId6\" r:cs=\"rId7\"/></a:graphicData></a:graphic></p:graphicFrame>";
        const string video = "<p:pic><p:nvPicPr><p:cNvPr id=\"5\" name=\"Video 4\"/><p:cNvPicPr/><p:nvPr><a:videoFile r:link=\"rId9\"/><p:extLst><p:ext uri=\"{DAA4B4D4-6D71-4841-9C94-3DE7FCFB9230}\">" +
                             "<p14:media xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\" r:embed=\"rId10\"/></p:ext></p:extLst></p:nvPr></p:nvPicPr>" +
                             "<p:blipFill><a:blip r:embed=\"rId11\"/><a:stretch><a:fillRect/></a:stretch></p:blipFill><p:spPr><a:xfrm><a:off x=\"9\" y=\"9\"/><a:ext cx=\"9\" cy=\"9\"/></a:xfrm>" +
                             "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr></p:pic>";
        const string treeExt = "<p:extLst><p:ext uri=\"{11111111-2222-3333-4444-555555555555}\"><x:any xmlns:x=\"urn:test\" v=\"1\"/></p:ext></p:extLst>";
        const string tail = "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr><p:extLst><p:ext uri=\"{BB962C8B-B14F-4D97-AF65-F5344CB8AC3E}\">" +
                            "<p14:creationId xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\" val=\"123\"/></p:ext></p:extLst>";
        var rels = Rel("rId3", "chart", "../charts/chart1.xml") + Rel("rId4", "diagramData", "../diagrams/data1.xml") +
                   Rel("rId5", "diagramLayout", "../diagrams/layout1.xml") + Rel("rId6", "diagramQuickStyle", "../diagrams/quickStyle1.xml") +
                   Rel("rId7", "diagramColors", "../diagrams/colors1.xml") + Rel("rId8", "tags", "../tags/tag1.xml") + Rel("rId9", "video", "../media/media1.mp4") +
                   $"<Relationship Id=\"rId10\" Type=\"http://schemas.microsoft.com/office/2007/relationships/media\" Target=\"../media/media1.mp4\"/>" +
                   Rel("rId11", "image", "../media/image9.png");
        var builder = new PptxBuilder().Slide(Shape(2, "Text", Xfrm(0, 0, 100, 100), "<a:p><a:r><a:rPr lang=\"de-DE\"/><a:t>Old</a:t></a:r></a:p>") + chart + smartArt + video + treeExt,
            rels: rels, tail: tail);
        builder.ExtraParts["ppt/charts/chart1.xml"] = ("officedocument.drawingml.chart", "<c:chartSpace xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"/>");
        builder.ExtraParts["ppt/diagrams/data1.xml"] = ("officedocument.drawingml.diagramData", "<dgm:dataModel xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\"/>");
        builder.ExtraParts["ppt/diagrams/layout1.xml"] = ("officedocument.drawingml.diagramLayout", "<dgm:layoutDef xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\"/>");
        builder.ExtraParts["ppt/diagrams/quickStyle1.xml"] = ("officedocument.drawingml.diagramStyle", "<dgm:styleDef xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\"/>");
        builder.ExtraParts["ppt/diagrams/colors1.xml"] = ("officedocument.drawingml.diagramColors", "<dgm:colorsDef xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\"/>");
        builder.ExtraParts["ppt/tags/tag1.xml"] = ("officedocument.presentationml.tags", "<p:tagLst " + Ns + "><p:tag name=\"X\" val=\"Y\"/></p:tagLst>");
        builder.ExtraParts["ppt/media/media1.mp4"] = ("video/mp4", "not really a video");
        builder.ExtraParts["ppt/media/image9.png"] = (null, "not really an image");
        var source = builder.Build();
        var presentation = Presentation.Load(new MemoryStream(source));
        var slide = presentation.Slides[0];
        var text = ((AutoShape)slide.Shapes[0]).Text;
        presentation.Execute(new SetTextCommand(slide, 2, text with
        {
            Paragraphs = [text.Paragraphs[0] with { Runs = [text.Paragraphs[0].Runs[0] with { Text = "New" }] }]
        }));

        var output = Save(presentation);

        AssertConsistent(output);
        var before = Parts(source);
        var after = Parts(output);
        foreach (var part in builder.ExtraParts.Keys) Assert.True(before["/" + part].SequenceEqual(after["/" + part]), part);
        Assert.True(before["/ppt/slides/_rels/slide1.xml.rels"].SequenceEqual(after["/ppt/slides/_rels/slide1.xml.rels"]));
        Assert.Equal(Xml(source, "/[Content_Types].xml").ToString(), Xml(output, "/[Content_Types].xml").ToString());
        var original = Xml(source, "/ppt/slides/slide1.xml").Root!;
        var written = Xml(output, "/ppt/slides/slide1.xml").Root!;
        var originalTree = original.Element(P + "cSld")!.Element(P + "spTree")!.Elements().ToList();
        var writtenTree = written.Element(P + "cSld")!.Element(P + "spTree")!.Elements().ToList();
        Assert.Equal(originalTree.Count, writtenTree.Count);
        for (var i = 3; i < originalTree.Count; i++) Assert.True(XNode.DeepEquals(originalTree[i], writtenTree[i]), originalTree[i].Name.LocalName);
        Assert.Equal("New", writtenTree[2].Descendants(A + "t").Single().Value);
        Assert.Equal("de-DE", (string)writtenTree[2].Descendants(A + "rPr").Single().Attribute("lang"));
        Assert.True(XNode.DeepEquals(original.Element(P + "extLst"), written.Element(P + "extLst")));
        Assert.True(XNode.DeepEquals(original.Element(P + "clrMapOvr"), written.Element(P + "clrMapOvr")));
    }

    [Fact]
    public void Writer_MovedInheritedPlaceholder_WritesXfrm_UnmovedWritesNone()
    {
        var presentation = new PptxBuilder()
            .Slide(Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>Title</a:t></a:r></a:p>") + Placeholder(3, "Content 2", "idx=\"1\"", ""))
            .Load();
        var slide = presentation.Slides[0];
        presentation.Execute(new SetTransformsCommand(slide, new Dictionary<uint, ShapeTransform> { [2] = new(10, 20, 30, 40, 90) }));

        var output = Save(presentation);

        var shapes = Xml(output, "/ppt/slides/slide1.xml").Descendants(P + "sp").ToList();
        var xfrm = shapes[0].Element(P + "spPr")!.Element(A + "xfrm")!;
        Assert.Equal("5400000", (string)xfrm.Attribute("rot"));
        Assert.Equal("10", (string)xfrm.Element(A + "off")!.Attribute("x"));
        Assert.Equal("40", (string)xfrm.Element(A + "ext")!.Attribute("cy"));
        Assert.Null(shapes[1].Element(P + "spPr")!.Element(A + "xfrm"));
        var reloaded = Load(output).Slides[0];
        Assert.Equal(new ShapeTransform(10, 20, 30, 40, 90), reloaded.Shapes[0].Transform);
        Assert.Null(reloaded.Shapes[1].Transform);
    }

    [Fact]
    public void Writer_RunEdit_KeepsUnknownRPrAttributes()
    {
        const string runs = "<a:p><a:r><a:rPr lang=\"de-DE\" kumimoji=\"1\"/><a:t>Normal </a:t></a:r>" +
                            "<a:r><a:rPr lang=\"de-DE\" b=\"1\" kern=\"1200\" dirty=\"0\"><a:highlight><a:srgbClr val=\"FFFF00\"/></a:highlight></a:rPr><a:t>fett</a:t></a:r>" +
                            "<a:r><a:rPr i=\"1\" spc=\"50\"/><a:t> normal</a:t></a:r></a:p>";
        var source = new PptxBuilder().Slide(Shape(2, "Text", Xfrm(0, 0, 100, 100), runs)).Build();
        var presentation = Load(source);
        var slide = presentation.Slides[0];
        var body = ((AutoShape)slide.Shapes[0]).Text;
        var paragraph = body.Paragraphs[0];
        presentation.Execute(new SetTextCommand(slide, 2, body with
        {
            Paragraphs =
            [
                paragraph with
                {
                    Runs =
                    [
                        paragraph.Runs[0], paragraph.Runs[1] with { Text = "kursiv", Format = paragraph.Runs[1].Format with { Bold = null, Italic = true, SizePt = 20 } },
                        paragraph.Runs[2], new TextRun("!", paragraph.Runs[2].Format)
                    ]
                }
            ]
        }));

        var output = Save(presentation);

        var originalRuns = Xml(source, "/ppt/slides/slide1.xml").Descendants(A + "r").ToList();
        var writtenRuns = Xml(output, "/ppt/slides/slide1.xml").Descendants(A + "r").ToList();
        Assert.Equal(4, writtenRuns.Count);
        Assert.True(XNode.DeepEquals(originalRuns[0], writtenRuns[0]));
        Assert.True(XNode.DeepEquals(originalRuns[2], writtenRuns[2]));
        var edited = writtenRuns[1].Element(A + "rPr")!;
        Assert.Equal("de-DE", (string)edited.Attribute("lang"));
        Assert.Equal("1200", (string)edited.Attribute("kern"));
        Assert.Equal("0", (string)edited.Attribute("dirty"));
        Assert.Null(edited.Attribute("b"));
        Assert.Equal("1", (string)edited.Attribute("i"));
        Assert.Equal("2000", (string)edited.Attribute("sz"));
        Assert.NotNull(edited.Element(A + "highlight"));
        Assert.Equal("kursiv", writtenRuns[1].Element(A + "t")!.Value);
        Assert.Equal("50", (string)writtenRuns[3].Element(A + "rPr")!.Attribute("spc"));
        Assert.Equal("!", writtenRuns[3].Element(A + "t")!.Value);
    }

    [Fact]
    public void Writer_EmptyParagraph_KeepsEndParaRPr()
    {
        var presentation = new PptxBuilder()
            .Slide(Shape(2, "Text", Xfrm(0, 0, 100, 100), "<a:p><a:pPr algn=\"ctr\"/><a:r><a:t>Gone</a:t></a:r><a:endParaRPr lang=\"de-DE\" sz=\"2000\" dirty=\"0\"/></a:p>"))
            .Load();
        var slide = presentation.Slides[0];
        var body = ((AutoShape)slide.Shapes[0]).Text;
        presentation.Execute(new SetTextCommand(slide, 2, body with { Paragraphs = [body.Paragraphs[0] with { Runs = [] }] }));

        var paragraph = Xml(Save(presentation), "/ppt/slides/slide1.xml").Descendants(A + "p").Single();

        Assert.Empty(paragraph.Elements(A + "r"));
        Assert.Equal("ctr", (string)paragraph.Element(A + "pPr")!.Attribute("algn"));
        var end = paragraph.Element(A + "endParaRPr")!;
        Assert.Equal("de-DE", (string)end.Attribute("lang"));
        Assert.Equal("2000", (string)end.Attribute("sz"));
        Assert.Equal("0", (string)end.Attribute("dirty"));
    }

    [Fact]
    public void Writer_AddSlide_RegistersPartRelsContentTypeAndSldId()
    {
        var source = new PptxBuilder { AppXml = true }.Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10))).Build();
        var presentation = Load(source);
        presentation.Execute(new AddSlideCommand(1, presentation.Layouts[0]));

        var output = Save(presentation);

        AssertConsistent(output);
        var rels = Rels(output, "/ppt/slides/_rels/slide2.xml.rels");
        Assert.Contains(rels, r => r.Type == RelNs + "/slideLayout" && r.Target == "../slideLayouts/slideLayout1.xml");
        Assert.Equal("application/vnd.openxmlformats-officedocument.presentationml.slide+xml", Overrides(output)["/ppt/slides/slide2.xml"]);
        var ids = Xml(output, "/ppt/presentation.xml").Descendants(P + "sldId").ToList();
        Assert.Equal(["256", "257"], ids.Select(i => (string)i.Attribute("id")));
        var newRel = Rels(output, "/ppt/_rels/presentation.xml.rels").Single(r => r.Id == (string)ids[1].Attribute(R + "id"));
        Assert.Equal("slides/slide2.xml", newRel.Target);
        Assert.Equal(RelNs + "/slide", newRel.Type);
        Assert.Equal("2", Xml(output, "/docProps/app.xml").Root!.Elements().Single(e => e.Name.LocalName == "Slides").Value);
        var reloaded = Load(output);
        Assert.Equal(2, reloaded.Slides.Count);
        Assert.Equal(["Title 1", "Content Placeholder 2"], reloaded.Slides[1].Shapes.Select(s => s.Name));
        Assert.Same(reloaded.Layouts[0], reloaded.Slides[1].Layout);
    }

    [Fact]
    public void Writer_DeleteSlide_RemovesSectionAndCustomShowReferences()
    {
        var builder = new PptxBuilder
        {
            AppXml = true, NotesMaster = true,
            CustomShows = "<p:custShowLst><p:custShow name=\"Short\" id=\"0\"><p:sldLst><p:sld r:id=\"rId3\"/><p:sld r:id=\"rId4\"/></p:sldLst></p:custShow></p:custShowLst>",
            PresentationExt = "<p:extLst><p:ext uri=\"{521415D9-36F7-43E2-AB2F-B90AF26B5E84}\"><p14:sectionLst xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\">" +
                              "<p14:section name=\"A\" id=\"{00000000-0000-0000-0000-00000000000A}\"><p14:sldIdLst><p14:sldId id=\"256\"/><p14:sldId id=\"257\"/></p14:sldIdLst></p14:section>" +
                              "<p14:section name=\"B\" id=\"{00000000-0000-0000-0000-00000000000B}\"><p14:sldIdLst><p14:sldId id=\"258\"/></p14:sldIdLst></p14:section>" +
                              "</p14:sectionLst></p:ext></p:extLst>"
        };
        var source = builder.Slide(Shape(2, "One", Xfrm(0, 0, 10, 10))).Slide(Shape(2, "Two", Xfrm(0, 0, 10, 10)), notes: "Notes of two")
            .Slide(Shape(2, "Three", Xfrm(0, 0, 10, 10))).Build();
        var presentation = Load(source);
        presentation.Execute(new DeleteSlidesCommand([presentation.Slides[1]]));

        var output = Save(presentation);

        AssertConsistent(output);
        var parts = Parts(output);
        Assert.DoesNotContain("/ppt/slides/slide2.xml", parts.Keys);
        Assert.DoesNotContain("/ppt/slides/_rels/slide2.xml.rels", parts.Keys);
        Assert.DoesNotContain("/ppt/notesSlides/notesSlide2.xml", parts.Keys);
        Assert.DoesNotContain("/ppt/notesSlides/_rels/notesSlide2.xml.rels", parts.Keys);
        Assert.DoesNotContain("/ppt/slides/slide2.xml", Overrides(output).Keys);
        Assert.DoesNotContain("/ppt/notesSlides/notesSlide2.xml", Overrides(output).Keys);
        Assert.DoesNotContain(Rels(output, "/ppt/_rels/presentation.xml.rels"), r => r.Id == "rId3");
        var xml = Xml(output, "/ppt/presentation.xml");
        Assert.Equal(["256", "258"], xml.Descendants(P + "sldId").Select(i => (string)i.Attribute("id")));
        Assert.Equal(["256"], xml.Descendants(P14 + "section").First().Descendants(P14 + "sldId").Select(i => (string)i.Attribute("id")));
        Assert.Equal(["258"], xml.Descendants(P14 + "section").Last().Descendants(P14 + "sldId").Select(i => (string)i.Attribute("id")));
        Assert.Equal(["rId4"], xml.Descendants(P + "sld").Select(s => (string)s.Attribute(R + "id")));
        var app = Xml(output, "/docProps/app.xml").Root!.Elements().ToDictionary(e => e.Name.LocalName, e => e.Value);
        Assert.Equal("2", app["Slides"]);
        Assert.Equal("0", app["Notes"]);
        Assert.Equal(["One", "Three"], Load(output).Slides.Select(s => s.Shapes[0].Name));
    }

    [Fact]
    public void Writer_MoveSlides_ReordersSldIdListOnly()
    {
        var source = new PptxBuilder().Slide(Shape(2, "One", Xfrm(0, 0, 10, 10))).Slide(Shape(2, "Two", Xfrm(0, 0, 10, 10)))
            .Slide(Shape(2, "Three", Xfrm(0, 0, 10, 10))).Build();
        var presentation = Load(source);
        presentation.Execute(new MoveSlidesCommand([presentation.Slides[2]], 0));

        var output = Save(presentation);

        AssertConsistent(output);
        var ids = Xml(output, "/ppt/presentation.xml").Descendants(P + "sldId").Select(i => ((string)i.Attribute("id"), (string)i.Attribute(R + "id")));
        Assert.Equal([("258", "rId4"), ("256", "rId2"), ("257", "rId3")], ids);
        var before = Parts(source);
        var after = Parts(output);
        foreach (var part in before.Keys.Where(k => k.StartsWith("/ppt/slides/", StringComparison.Ordinal)))
            Assert.True(before[part].SequenceEqual(after[part]), part);
        Assert.True(before["/ppt/_rels/presentation.xml.rels"].SequenceEqual(after["/ppt/_rels/presentation.xml.rels"]));
        Assert.Equal(["Three", "One", "Two"], Load(output).Slides.Select(s => s.Shapes[0].Name));
    }

    [Fact]
    public void Writer_InsertPicture_AddsMediaPartDefaultTypeAndRel()
    {
        var presentation = new PptxBuilder().Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10))).Load();
        var slide = presentation.Slides[0];
        var kept = new InsertPictureCommand(slide, [1, 2, 3], "image/jpeg", new ShapeTransform(10, 20, 30, 40));
        presentation.Execute(kept);
        var removed = new InsertPictureCommand(slide, [4, 5, 6], "image/gif", new ShapeTransform(0, 0, 5, 5));
        presentation.Execute(removed);
        presentation.Execute(new DeleteShapesCommand(slide, [removed.Picture.Id]));

        var output = Save(presentation);

        AssertConsistent(output);
        var parts = Parts(output);
        Assert.Equal(new byte[] { 1, 2, 3 }, parts["/ppt/media/image1.jpg"]);
        Assert.DoesNotContain(parts.Keys, k => k.EndsWith(".gif", StringComparison.Ordinal));
        var defaults = Xml(output, "/[Content_Types].xml").Root!.Elements(ContentTypes + "Default").ToDictionary(d => (string)d.Attribute("Extension"), d => (string)d.Attribute("ContentType"));
        Assert.Equal("image/jpeg", defaults["jpg"]);
        Assert.DoesNotContain("gif", defaults.Keys);
        var rel = Rels(output, "/ppt/slides/_rels/slide1.xml.rels").Single(r => r.Type == RelNs + "/image");
        Assert.Equal("../media/image1.jpg", rel.Target);
        var picture = Xml(output, "/ppt/slides/slide1.xml").Descendants(P + "pic").Single();
        Assert.Equal(rel.Id, (string)picture.Descendants(A + "blip").Single().Attribute(R + "embed"));
        var reloaded = Assert.IsType<PictureShape>(Load(output).Slides[0].Shapes[1]);
        Assert.Equal("/ppt/media/image1.jpg", reloaded.ImagePart);
        Assert.Equal(new ShapeTransform(10, 20, 30, 40), reloaded.Transform);
        Assert.Equal(kept.Picture.Id, reloaded.Id);
    }

    [Fact]
    public void Writer_ControlCharacterInText_IsEscaped()
    {
        var presentation = new PptxBuilder().Slide(Shape(2, "Text", Xfrm(0, 0, 100, 100), "<a:p><a:r><a:t>x</a:t></a:r></a:p>")).Load();
        var slide = presentation.Slides[0];
        var body = ((AutoShape)slide.Shapes[0]).Text;
        presentation.Execute(new SetTextCommand(slide, 2, body with
        {
            Paragraphs = [body.Paragraphs[0] with { Runs = [body.Paragraphs[0].Runs[0] with { Text = "A\u0001B _x0041_ <script>" }] }]
        }));

        var output = Save(presentation);

        Assert.Equal("A_x0001_B _x005F_x0041_ <script>", Xml(output, "/ppt/slides/slide1.xml").Descendants(A + "t").Single().Value);
        Assert.Equal("A\u0001B _x0041_ <script>", ((AutoShape)Load(output).Slides[0].Shapes[0]).Text.Paragraphs[0].Runs[0].Text);
    }

    [Fact]
    public void Writer_RoundTrip_ReadBackEqualsModel()
    {
        var presentation = Presentation.Load(File.OpenRead(SamplePath));
        var title = (AutoShape)presentation.Slides[0].Shapes[0];
        var p0 = title.Text.Paragraphs[0];
        presentation.Execute(new SetTextCommand(presentation.Slides[0], title.Id, title.Text with
        {
            Paragraphs = [p0 with { Align = TextAlign.Center, Runs = [p0.Runs[0] with { Text = "Edited" }, new TextRun(" more", new RunFormat(Bold: true, Color: "scheme:accent2 lumMod=75000"))] }],
            Anchor = TextAnchor.Bottom, InsetLeft = 0
        }));
        presentation.Execute(new InsertShapeCommand(presentation.Slides[0], new AutoShape
        {
            Name = "Ellipse", Geometry = "ellipse", Transform = new ShapeTransform(100, 200, 300, 400, 15, true), Fill = new ShapeFill(ShapeFillKind.Solid, "#00FF00"),
            Line = new ShapeLine(null, 12700),
            Text = new TextBody { Paragraphs = [new TextParagraph { Bullet = false, Level = 1, Runs = [new TextRun("Inside", new RunFormat(Italic: true, SizePt: 12.5, Font: "Arial"))] }] }
        }));
        presentation.Execute(new InsertPictureCommand(presentation.Slides[0], [9, 8, 7], "image/png", new ShapeTransform(1, 2, 3, 4)));
        var pictures = presentation.Slides[2];
        presentation.Execute(new SetTransformsCommand(pictures, new Dictionary<uint, ShapeTransform> { [3] = new(1000, 1000, 2000, 2000) }));
        presentation.Execute(new SetShapeStyleCommand(pictures, [4], new ShapeFill(ShapeFillKind.Solid, "#FF0000"), new ShapeLine("scheme:accent1", 25400, "dash")));
        presentation.Execute(new SetGeometryCommand(pictures, 4, "roundRect"));
        presentation.Execute(new DuplicateShapesCommand(pictures, [3, 4], 50000));
        presentation.Execute(new SetNotesCommand(presentation.Slides[1], "Changed notes\nline 2"));
        presentation.Execute(new AddSlideCommand(3, presentation.Layouts[1]));
        presentation.Execute(new DuplicateSlidesCommand([presentation.Slides[1], presentation.Slides[2]]));
        presentation.Execute(new SetSlideHiddenCommand([presentation.Slides[0]], true));
        presentation.Execute(new MoveSlidesCommand([presentation.Slides[^1]], 0));

        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Equal(output, Save(presentation));
        var reloaded = Load(output);
        Assert.Equal(Describe(presentation), Describe(reloaded));
        Assert.Equal(6, reloaded.Slides.Count);
        Assert.All(reloaded.Slides, s => Assert.Equal(SlideShape.Flatten(s.Shapes).Count(), SlideShape.Flatten(s.Shapes).Select(x => x.Id).Distinct().Count()));
    }

    [Fact]
    public void Writer_GroupEdits_RoundTrip()
    {
        const string group =
            "<p:grpSp><p:nvGrpSpPr><p:cNvPr id=\"10\" name=\"Group 9\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr>" +
            "<a:xfrm rot=\"5400000\"><a:off x=\"1000000\" y=\"1000000\"/><a:ext cx=\"2000000\" cy=\"1000000\"/>" +
            "<a:chOff x=\"100000\" y=\"100000\"/><a:chExt cx=\"1000000\" cy=\"500000\"/></a:xfrm></p:grpSpPr>" +
            "<p:sp><p:nvSpPr><p:cNvPr id=\"11\" name=\"Child\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x=\"100000\" y=\"100000\"/>" +
            "<a:ext cx=\"500000\" cy=\"250000\"/></a:xfrm></p:spPr></p:sp>" +
            "<p:sp><p:nvSpPr><p:cNvPr id=\"12\" name=\"Child 2\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr><a:xfrm><a:off x=\"600000\" y=\"300000\"/>" +
            "<a:ext cx=\"100000\" cy=\"100000\"/></a:xfrm></p:spPr></p:sp></p:grpSp>";
        var presentation = new PptxBuilder().Slide(group).Load();
        var slide = presentation.Slides[0];
        presentation.Execute(new SetTransformsCommand(slide, new Dictionary<uint, ShapeTransform> { [11] = new(1800000, 800000, 900000, 400000, 45) }));
        presentation.Execute(new DuplicateShapesCommand(slide, [10], 100000));
        presentation.Execute(new DeleteShapesCommand(slide, [12]));

        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Equal(Describe(presentation), Describe(Load(output)));
        var groups = Xml(output, "/ppt/slides/slide1.xml").Descendants(P + "grpSp").ToList();
        Assert.Equal(2, groups.Count);
        Assert.Equal(["10", "11"], groups[0].Descendants(P + "cNvPr").Select(c => (string)c.Attribute("id")));
        Assert.Equal(["13", "14", "15"], groups[1].Descendants(P + "cNvPr").Select(c => (string)c.Attribute("id")));
        Assert.NotNull(groups[1].Element(P + "grpSpPr")!.Element(A + "xfrm")!.Element(A + "chOff"));
    }

    [Fact]
    public void Create_BlankDeck_RoundTripsAndHasNotesMaster()
    {
        var presentation = Presentation.Create();

        Assert.Equal(12192000, presentation.SlideWidth);
        Assert.Equal(6858000, presentation.SlideHeight);
        Assert.Single(presentation.Masters);
        Assert.Equal(["title", "obj", "titleOnly", "blank"], presentation.Layouts.Select(l => l.Type));
        Assert.Equal("title", Assert.Single(presentation.Slides).Layout.Type);
        Assert.NotNull(presentation.Package.NotesMasterPart);
        presentation.Execute(new SetNotesCommand(presentation.Slides[0], "Speaker notes"));

        var output = Save(presentation);

        AssertConsistent(output);
        var theme = Xml(output, "/ppt/theme/theme1.xml").Root!;
        Assert.Equal("MudEx", (string)theme.Attribute("name"));
        var reloaded = Load(output);
        Assert.Equal(Describe(presentation), Describe(reloaded));
        Assert.True(reloaded.Slides[0].HasNotesPart);
        Assert.Equal("Speaker notes", reloaded.Slides[0].Notes);
        AssertConsistent(Save(Presentation.Create()));
    }

    private static string Rel(string id, string type, string target) => $"<Relationship Id=\"{id}\" Type=\"{RelNs}/{type}\" Target=\"{target}\"/>";

    private static byte[] Save(Presentation presentation)
    {
        using var stream = new MemoryStream();
        presentation.Save(stream);
        return stream.ToArray();
    }

    private static Presentation Load(byte[] bytes) => Presentation.Load(new MemoryStream(bytes));

    private static Dictionary<string, byte[]> Parts(byte[] package)
    {
        using var zip = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read);
        return zip.Entries.ToDictionary(e => "/" + e.FullName, e =>
        {
            using var stream = e.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        });
    }

    private static XDocument Xml(byte[] package, string part) => XDocument.Parse(Encoding.UTF8.GetString(Parts(package)[part]).TrimStart('﻿'));

    private static List<OpcRelationship> Rels(byte[] package, string relsPart) =>
        Xml(package, relsPart).Root!.Elements(PackageRels + "Relationship")
            .Select(r => new OpcRelationship((string)r.Attribute("Id"), (string)r.Attribute("Type"), (string)r.Attribute("Target"), (string)r.Attribute("TargetMode"))).ToList();

    private static Dictionary<string, string> Overrides(byte[] package) =>
        Xml(package, "/[Content_Types].xml").Root!.Elements(ContentTypes + "Override").ToDictionary(o => (string)o.Attribute("PartName"), o => (string)o.Attribute("ContentType"));

    private static void AssertNoDanglingRelationships(byte[] package)
    {
        var opc = OpcPackage.Open(package);
        var parts = opc.PartNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var rels in parts.Where(p => p.EndsWith(".rels", StringComparison.Ordinal)))
        {
            var source = rels.Replace("/_rels/", "/", StringComparison.Ordinal)[..^5];
            Assert.True(source == "/" || parts.Contains(source), $"Rels without source {rels}");
            foreach (var rel in opc.Relationships(source).Where(r => r.TargetMode == null))
                Assert.True(parts.Contains(opc.ResolveTarget(source, rel.Target)), $"{rels}: {rel.Id} -> {rel.Target}");
        }

        foreach (var part in parts.Where(p => p.EndsWith(".xml", StringComparison.Ordinal) && p != "/[Content_Types].xml"))
        {
            var ids = opc.Relationships(part).Select(r => r.Id).ToHashSet();
            foreach (var reference in opc.ReadXml(part).Descendants().Attributes().Where(a => a.Name.Namespace == R && a.Value.Length > 0))
                Assert.True(ids.Contains(reference.Value), $"{part}: {reference.Name.LocalName}={reference.Value} has no relationship");
        }
    }

    private static void AssertConsistent(byte[] package)
    {
        var opc = OpcPackage.Open(package);
        var parts = opc.PartNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts.Where(p => !p.EndsWith(".rels", StringComparison.Ordinal) && p != "/[Content_Types].xml"))
            Assert.True(opc.ContentType(part) != null, $"No content type for {part}");
        foreach (var name in opc.OverrideContentTypes.Keys) Assert.True(parts.Contains(name), $"Override without part {name}");
        AssertNoDanglingRelationships(package);

        var main = opc.ReadXml("/ppt/presentation.xml");
        var mainRels = opc.Relationships("/ppt/presentation.xml").Select(r => r.Id).ToHashSet();
        foreach (var id in main.Descendants().Attributes(R + "id")) Assert.Contains(id.Value, mainRels);
        var slideIds = main.Descendants(P + "sldId").Select(i => (string)i.Attribute("id")).ToList();
        Assert.Equal(slideIds.Count, slideIds.Distinct().Count());
        foreach (var id in main.Descendants(P14 + "sldId")) Assert.Contains((string)id.Attribute("id"), slideIds);
        foreach (var slide in opc.Relationships("/ppt/presentation.xml").Where(r => r.Type.EndsWith("/slide", StringComparison.Ordinal)))
        {
            var part = opc.ResolveTarget("/ppt/presentation.xml", slide.Target);
            var ids = opc.Relationships(part).Select(r => r.Id).ToHashSet();
            foreach (var reference in opc.ReadXml(part).Descendants().Attributes().Where(a => a.Name.Namespace == R)) Assert.Contains(reference.Value, ids);
            var shapeIds = opc.ReadXml(part).Descendants(P + "cNvPr").Select(c => (string)c.Attribute("id")).ToList();
            Assert.Equal(shapeIds.Count, shapeIds.Distinct().Count());
        }
    }

    private static string Describe(Presentation presentation)
    {
        var sb = new StringBuilder();
        foreach (var slide in presentation.Slides)
        {
            sb.AppendLine($"slide {slide.Layout?.Name} hidden={slide.Hidden} notes={slide.Notes}");
            foreach (var shape in SlideShape.Flatten(slide.Shapes))
            {
                sb.AppendLine($"  {shape.GetType().Name} {shape.Id} {shape.Name} {shape.Transform} {shape.Placeholder} parent={shape.Parent?.Id}");
                switch (shape)
                {
                    case AutoShape auto:
                        sb.AppendLine($"    {auto.Geometry} {string.Join(",", auto.Adjustments)} {auto.Fill} {auto.Line} box={auto.IsTextBox}");
                        if (auto.Text is { } text)
                        {
                            sb.AppendLine($"    body {text.Anchor} {text.InsetLeft} {text.InsetTop} {text.InsetRight} {text.InsetBottom} {text.Wrap}");
                            foreach (var p in text.Paragraphs)
                            {
                                sb.AppendLine($"    p {p.Align} {p.Level} {p.Bullet} end={p.EndFormat}");
                                foreach (var r in p.Runs) sb.AppendLine($"      r '{r.Text}' {r.Format}");
                            }
                        }

                        break;
                    case PictureShape picture:
                        sb.AppendLine($"    {picture.ImagePart} {picture.ContentType}");
                        break;
                    case OpaqueShape opaque:
                        sb.AppendLine($"    {opaque.Kind}");
                        break;
                }
            }
        }

        return sb.ToString();
    }
}
