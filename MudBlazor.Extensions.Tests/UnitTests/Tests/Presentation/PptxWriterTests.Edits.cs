using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public partial class PptxWriterTests
{
    private const string Connector =
        "<p:cxnSp><p:nvCxnSpPr><p:cNvPr id=\"4\" name=\"Connector 3\"/><p:cNvCxnSpPr/><p:nvPr/></p:nvCxnSpPr><p:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"0\"/></a:xfrm>" +
        "<a:prstGeom prst=\"line\"><a:avLst/></a:prstGeom></p:spPr></p:cxnSp>";

    private static string Effect(int id, uint spid) =>
        $"<p:par><p:cTn id=\"{id}\" fill=\"hold\"><p:stCondLst><p:cond delay=\"indefinite\"/></p:stCondLst><p:childTnLst>" +
        $"<p:par><p:cTn id=\"{id + 1}\" fill=\"hold\"><p:stCondLst><p:cond delay=\"0\"/></p:stCondLst><p:childTnLst>" +
        $"<p:par><p:cTn id=\"{id + 2}\" presetID=\"1\" presetClass=\"entr\" presetSubtype=\"0\" fill=\"hold\" grpId=\"0\" nodeType=\"clickEffect\"><p:stCondLst><p:cond delay=\"0\"/></p:stCondLst>" +
        $"<p:childTnLst><p:set><p:cBhvr><p:cTn id=\"{id + 3}\" dur=\"1\" fill=\"hold\"><p:stCondLst><p:cond delay=\"0\"/></p:stCondLst></p:cTn><p:tgtEl><p:spTgt spid=\"{spid}\"/></p:tgtEl>" +
        "<p:attrNameLst><p:attrName>style.visibility</p:attrName></p:attrNameLst></p:cBhvr><p:to><p:strVal val=\"visible\"/></p:to></p:set></p:childTnLst></p:cTn></p:par>" +
        "</p:childTnLst></p:cTn></p:par></p:childTnLst></p:cTn></p:par>";

    private static string Timing(params uint[] animated) =>
        "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr><p:timing><p:tnLst><p:par><p:cTn id=\"1\" dur=\"indefinite\" restart=\"never\" nodeType=\"tmRoot\"><p:childTnLst>" +
        "<p:seq concurrent=\"1\" nextAc=\"seek\"><p:cTn id=\"2\" dur=\"indefinite\" nodeType=\"mainSeq\"><p:childTnLst>" +
        string.Concat(animated.Select((spid, i) => Effect(3 + i * 4, spid))) +
        "</p:childTnLst></p:cTn><p:prevCondLst><p:cond evt=\"onPrev\" delay=\"0\"><p:tgtEl><p:sldTgt/></p:tgtEl></p:cond></p:prevCondLst>" +
        "<p:nextCondLst><p:cond evt=\"onNext\" delay=\"0\"><p:tgtEl><p:sldTgt/></p:tgtEl></p:cond></p:nextCondLst></p:seq></p:childTnLst></p:cTn></p:par></p:tnLst>" +
        "<p:bldLst>" + string.Concat(animated.Select(spid => $"<p:bldP spid=\"{spid}\" grpId=\"0\"/>")) + "</p:bldLst></p:timing>";

    private static Presentation AnimatedDeck() => new PptxBuilder()
        .Slide(Shape(2, "Kept", Xfrm(0, 0, 10, 10), "<a:p><a:r><a:t>A</a:t></a:r></a:p>") + Shape(3, "Animated", Xfrm(0, 0, 10, 10), "<a:p><a:r><a:t>B</a:t></a:r></a:p>"),
            tail: Timing(2, 3))
        .Load();

    [Fact]
    public void Writer_PastedInsertedPicture_TargetsItsOwnMedia()
    {
        var presentation = new PptxBuilder().Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10)), notes: "Notes").Slide(Shape(2, "Other", Xfrm(0, 0, 10, 10))).Load();
        var (from, to) = (presentation.Slides[0], presentation.Slides[1]);
        var insert = new InsertPictureCommand(from, [1, 2, 3], "image/png", new ShapeTransform(0, 0, 5, 5));
        presentation.Execute(insert);
        presentation.Execute(new InsertShapeCommand(to, ShapeEdit.Detach(presentation, from, insert.Picture)));

        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Equal(insert.Picture.ImagePart, BlipTarget(output, "/ppt/slides/slide2.xml"));
        Assert.Equal(insert.Picture.ImagePart, BlipTarget(output, "/ppt/slides/slide1.xml"));
    }

    [Fact]
    public void Writer_UndoneInsertedPicture_PastedCopyKeepsMedia()
    {
        var presentation = new PptxBuilder().Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10))).Load();
        var slide = presentation.Slides[0];
        var insert = new InsertPictureCommand(slide, [1, 2, 3], "image/png", new ShapeTransform(0, 0, 5, 5));
        presentation.Execute(insert);
        var clip = ShapeEdit.Detach(presentation, slide, insert.Picture);
        presentation.Undo.Undo();
        var other = new InsertPictureCommand(slide, [7, 8, 9], "image/png", new ShapeTransform(0, 0, 5, 5));
        presentation.Execute(other);
        presentation.Execute(new InsertShapeCommand(slide, clip));

        var output = Save(presentation);

        AssertConsistent(output);
        Assert.NotEqual(insert.Picture.ImagePart, other.Picture.ImagePart);
        Assert.Equal(new byte[] { 1, 2, 3 }, Parts(output)[insert.Picture.ImagePart]);
        Assert.Equal(new byte[] { 7, 8, 9 }, Parts(output)[other.Picture.ImagePart]);
    }

    [Fact]
    public void Writer_DeletedAnimatedShape_RemovesItsAnimationAndBuild()
    {
        var presentation = AnimatedDeck();
        var slide = presentation.Slides[0];
        presentation.Execute(new DeleteShapesCommand(slide, [3]));

        var xml = Xml(Save(presentation), "/ppt/slides/slide1.xml");

        Assert.Equal(["2"], xml.Descendants(P + "spTgt").Select(t => (string)t.Attribute("spid")));
        Assert.Equal(["2"], xml.Descendants(P + "bldP").Select(t => (string)t.Attribute("spid")));
        Assert.DoesNotContain(xml.Descendants(), e => e.Name.LocalName is "childTnLst" or "tnLst" or "bldLst" && !e.HasElements);
    }

    [Fact]
    public void Writer_AllAnimatedShapesDeleted_RemovesTiming()
    {
        var presentation = AnimatedDeck();
        presentation.Execute(new DeleteShapesCommand(presentation.Slides[0], [2, 3]));

        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Empty(Xml(output, "/ppt/slides/slide1.xml").Descendants(P + "timing"));
    }

    [Fact]
    public void InsertShape_AfterDeletingAnimatedShape_DoesNotReuseItsId()
    {
        var presentation = AnimatedDeck();
        var slide = presentation.Slides[0];
        presentation.Execute(new DeleteShapesCommand(slide, [3]));
        var shape = new AutoShape { Transform = new ShapeTransform(0, 0, 10, 10) };

        presentation.Execute(new InsertShapeCommand(slide, shape));

        Assert.Equal(4u, shape.Id);
        Assert.DoesNotContain(Xml(Save(presentation), "/ppt/slides/slide1.xml").Descendants(P + "spTgt"), t => (string)t.Attribute("spid") == "4");
    }

    [Fact]
    public void Writer_LinkRemovedAndPictureAdded_RewritesSlideRels()
    {
        var presentation = LinkedDeck().Load();
        presentation.Execute(new DeleteSlidesCommand([presentation.Slides[1]]));
        presentation.Execute(new InsertPictureCommand(presentation.Slides[0], [1, 2, 3], "image/png", new ShapeTransform(0, 0, 5, 5)));

        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Equal("/ppt/media/image1.png", BlipTarget(output, "/ppt/slides/slide1.xml", true));
    }

    [Fact]
    public void Connector_IsNotTextEditable()
    {
        var presentation = new PptxBuilder().Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10)) + Connector).Load();
        var slide = presentation.Slides[0];
        var connector = Assert.IsType<AutoShape>(slide.Shapes[1]);
        Assert.True(connector.IsConnector);
        Assert.False(((AutoShape)slide.Shapes[0]).IsConnector);

        Assert.Throws<ArgumentException>(() => presentation.Execute(new SetTextCommand(slide, 4, new TextBody { Paragraphs = [new TextParagraph { Runs = [new TextRun("x")] }] })));

        Assert.Null(connector.Text);
        Assert.False(presentation.Undo.CanUndo);
    }

    private static string BlipTarget(byte[] package, string slide, bool last = false)
    {
        var opc = OpcPackage.Open(package);
        var blips = Xml(package, slide).Descendants(A + "blip").ToList();
        var embed = (string)(last ? blips[^1] : blips[0]).Attribute(R + "embed");
        return opc.Relationships(slide).SingleOrDefault(r => r.Id == embed) is { } rel ? opc.ResolveTarget(slide, rel.Target) : null;
    }
}
