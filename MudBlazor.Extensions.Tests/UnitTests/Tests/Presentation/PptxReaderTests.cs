using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class PptxReaderTests
{
    private const string ChartUri = "http://schemas.openxmlformats.org/drawingml/2006/chart";
    private const string DiagramUri = "http://schemas.openxmlformats.org/drawingml/2006/diagram";

    private static string Text(SlideShape shape) =>
        string.Join("|", ((AutoShape)shape).Text.Paragraphs.Select(p => string.Concat(p.Runs.Select(r => r.Text))));

    private static string Frame(uint id, string name, string uri) =>
        $"<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id=\"{id}\" name=\"{name}\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
        "<p:xfrm><a:off x=\"100\" y=\"200\"/><a:ext cx=\"300\" cy=\"400\"/></p:xfrm>" +
        $"<a:graphic><a:graphicData uri=\"{uri}\"><x:data xmlns:x=\"urn:test\" r:id=\"rId9\"/></a:graphicData></a:graphic></p:graphicFrame>";

    [Fact]
    public void Reader_SampleDeck_ReadsAllSlides()
    {
        using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "Presentation", "sample.pptx"));
        var presentation = Presentation.Load(file);

        Assert.Equal(3, presentation.Slides.Count);
        Assert.Single(presentation.Masters);
        Assert.Equal(11, presentation.Layouts.Count);
        Assert.False(presentation.HasOpaqueContent);
        Assert.Equal(3, presentation.Slides.Select(s => s.Key).Distinct().Count());

        var title = presentation.Slides[0].Shapes[0];
        Assert.Equal("MudBlazor.Extensions", Text(title));
        Assert.Null(title.Transform);
        Assert.Equal(new ShapeTransform(685800, 2130425, 7772400, 1470025), title.ResolvedTransform);
        Assert.Equal(new PlaceholderRef("ctrTitle", null), title.Placeholder);

        var content = (AutoShape)presentation.Slides[1].Shapes[1];
        Assert.Equal([0, 0, 0, 1, 1], content.Text.Paragraphs.Select(p => p.Level));
        Assert.True(presentation.Slides[1].HasNotesPart);
        Assert.StartsWith("These notes come from ppt/notesSlides", presentation.Slides[1].Notes);
        Assert.False(presentation.Slides[0].HasNotesPart);

        var picture = Assert.IsType<PictureShape>(presentation.Slides[2].Shapes[1]);
        Assert.Equal("/ppt/media/image1.png", picture.ImagePart);
        Assert.Equal("image/png", picture.ContentType);
        Assert.Equal(new ShapeTransform(4206240, 1828800, 2926080, 2926080), picture.Transform);

        var textBox = (AutoShape)presentation.Slides[2].Shapes[2];
        Assert.True(textBox.IsTextBox);
        Assert.False(textBox.Text.Wrap);
        Assert.Equal(ShapeFillKind.None, textBox.Fill.Kind);
        Assert.Equal(new RunFormat(Italic: true, SizePt: 20), textBox.Text.Paragraphs[0].Runs[0].Format);
        Assert.All(presentation.Slides, s => Assert.Contains(s.Layout, presentation.Layouts));
    }

    [Fact]
    public void Reader_SlideSize()
    {
        var presentation = new PptxBuilder { Width = 9144000, Height = 5143500 }.Slide("").Load();

        Assert.Equal(9144000, presentation.SlideWidth);
        Assert.Equal(5143500, presentation.SlideHeight);
    }

    [Fact]
    public void Reader_PlaceholderWithoutXfrm_InheritsLayoutTransform()
    {
        var presentation = new PptxBuilder()
            .Slide(Placeholder(2, "Title 1", "type=\"title\"", "") + Placeholder(3, "Content 2", "idx=\"1\"", ""))
            .Load();

        var shapes = presentation.Slides[0].Shapes;
        Assert.Null(shapes[0].Transform);
        Assert.Equal(new ShapeTransform(1000000, 500000, 8000000, 1000000), shapes[0].ResolvedTransform);
        Assert.Equal(new ShapeTransform(838200, 1825625, 10515600, 4351338), shapes[1].ResolvedTransform);
        Assert.Equal(new PlaceholderRef("obj", 1), shapes[1].Placeholder);
    }

    [Fact]
    public void Reader_GroupChild_ResolvedTransformIsAbsolute()
    {
        var group = "<p:grpSp><p:nvGrpSpPr><p:cNvPr id=\"10\" name=\"Group 9\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr>" +
                    "<a:xfrm rot=\"5400000\"><a:off x=\"1000000\" y=\"1000000\"/><a:ext cx=\"2000000\" cy=\"1000000\"/>" +
                    "<a:chOff x=\"100000\" y=\"100000\"/><a:chExt cx=\"1000000\" cy=\"500000\"/></a:xfrm></p:grpSpPr>" +
                    Shape(11, "Child", Xfrm(100000, 100000, 500000, 250000)) + "</p:grpSp>";
        var presentation = new PptxBuilder().Slide(group).Load();

        var parent = Assert.IsType<GroupShape>(presentation.Slides[0].Shapes.Single());
        var child = parent.Children.Single();
        Assert.Same(parent, child.Parent);
        Assert.Equal(new ShapeTransform(100000, 100000, 1000000, 500000), parent.ChildTransform);
        Assert.Equal(new ShapeTransform(100000, 100000, 500000, 250000), child.Transform);
        Assert.Equal(new ShapeTransform(1750000, 750000, 1000000, 500000, 90), child.ResolvedTransform);
    }

    [Fact]
    public void Reader_MixedRuns_KeepRunBoundariesAndFormats()
    {
        var body = "<a:p><a:r><a:rPr lang=\"de-DE\"/><a:t>Normal </a:t></a:r>" +
                   "<a:r><a:rPr lang=\"de-DE\" b=\"1\" sz=\"2400\"><a:solidFill><a:srgbClr val=\"ff0000\"/></a:solidFill><a:latin typeface=\"Arial\"/></a:rPr><a:t>fett</a:t></a:r>" +
                   "<a:r><a:t> normal</a:t></a:r><a:endParaRPr sz=\"1200\"/></a:p>";
        var presentation = new PptxBuilder().Slide(Shape(2, "Text", Xfrm(0, 0, 100, 100), body)).Load();

        var paragraph = ((AutoShape)presentation.Slides[0].Shapes[0]).Text.Paragraphs.Single();
        Assert.Equal(["Normal ", "fett", " normal"], paragraph.Runs.Select(r => r.Text));
        Assert.Equal(new RunFormat(), paragraph.Runs[0].Format);
        Assert.Equal(new RunFormat(Bold: true, SizePt: 24, Color: "#FF0000", Font: "Arial"), paragraph.Runs[1].Format);
        Assert.Equal(new RunFormat(), paragraph.Runs[2].Format);
        Assert.Equal(new RunFormat(SizePt: 12), paragraph.EndFormat);
    }

    [Fact]
    public void Reader_LineBreak_IsNewlineRun()
    {
        var body = "<a:p><a:r><a:t>a</a:t></a:r><a:br><a:rPr sz=\"1000\"/></a:br><a:r><a:t>b</a:t></a:r></a:p>";
        var presentation = new PptxBuilder().Slide(Shape(2, "Text", Xfrm(0, 0, 100, 100), body)).Load();

        var runs = ((AutoShape)presentation.Slides[0].Shapes[0]).Text.Paragraphs[0].Runs;
        Assert.Equal(["a", "\n", "b"], runs.Select(r => r.Text));
        Assert.Equal(10, runs[1].Format.SizePt);
    }

    [Fact]
    public void Reader_SchemeColor_ResolvesThroughClrMapAndLumMod()
    {
        var body = "<a:p><a:r><a:rPr><a:solidFill><a:schemeClr val=\"tx1\"><a:lumMod val=\"50000\"/></a:schemeClr></a:solidFill></a:rPr><a:t>x</a:t></a:r></a:p>";
        var builder = new PptxBuilder { ClrMap = "bg1=\"lt1\" tx1=\"dk2\" bg2=\"lt2\" tx2=\"dk1\" accent1=\"accent1\"" }.ThemeColor("dk2", "FF0000");
        var spPr = Xfrm(0, 0, 100, 100) + "<a:solidFill><a:schemeClr val=\"accent1\"><a:lumMod val=\"75000\"/><a:lumOff val=\"25000\"/></a:schemeClr></a:solidFill>";
        var presentation = builder.Slide(Shape(2, "Text", spPr, body)).Load();

        var shape = (AutoShape)presentation.Slides[0].Shapes[0];
        var run = shape.Text.Paragraphs[0].Runs[0];
        var master = presentation.Masters[0];
        Assert.Equal("scheme:tx1 lumMod=50000", run.Format.Color);
        Assert.Equal("#800000", ColorResolver.Resolve(run.Format.Color, master));
        Assert.Equal("#800000", shape.EffectiveFormat(run).Color);
        Assert.Equal(new ShapeFill(ShapeFillKind.Solid, "scheme:accent1 lumMod=75000 lumOff=25000"), shape.Fill);
        Assert.Equal("#7395D3", ColorResolver.Resolve(shape.Fill.Color, master));
        Assert.Equal("#BFBFBF", ColorResolver.Resolve("#FFFFFF shade=75000", master));
        Assert.Equal("#BFBFBF", ColorResolver.Resolve("#000000 tint=25000", master));
    }

    [Fact]
    public void Reader_EffectiveFormat_InheritsThroughLayoutAndMasterStyles()
    {
        var builder = new PptxBuilder
        {
            LayoutShapes = Placeholder(2, "Title 1", "type=\"title\"", Xfrm(0, 0, 10, 10))
                .Replace("<a:lstStyle/>", "<a:lstStyle><a:lvl1pPr algn=\"ctr\"><a:defRPr sz=\"3600\" b=\"1\"/></a:lvl1pPr></a:lstStyle>")
        };
        var presentation = builder
            .Slide(Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>T</a:t></a:r></a:p>") +
                   Placeholder(3, "Body", "type=\"body\" idx=\"1\"", "", "<a:p><a:pPr lvl=\"1\"/><a:r><a:t>B</a:t></a:r></a:p>") +
                   Shape(4, "Box", Xfrm(0, 0, 10, 10), "<a:p><a:r><a:t>X</a:t></a:r></a:p>"))
            .Load();

        var shapes = presentation.Slides[0].Shapes.Cast<AutoShape>().ToList();
        var title = shapes[0].EffectiveFormat(shapes[0].Text.Paragraphs[0].Runs[0]);
        Assert.Equal(36, title.SizePt);
        Assert.True(title.Bold);
        Assert.Equal("Major Font", title.Font);
        Assert.Equal("#000000", title.Color);
        Assert.Null(shapes[0].Text.Paragraphs[0].Align);
        Assert.Equal(TextAlign.Center, shapes[0].EffectiveAlign(shapes[0].Text.Paragraphs[0]));
        Assert.Equal(24, shapes[1].EffectiveFormat(shapes[1].Text.Paragraphs[0].Runs[0]).SizePt);
        Assert.Equal(18, shapes[2].EffectiveFormat(shapes[2].Text.Paragraphs[0].Runs[0]).SizePt);
    }

    [Fact]
    public void Reader_InheritedBodyAndAlign_StayNullAndResolveThroughLayout()
    {
        var builder = new PptxBuilder
        {
            LayoutShapes = Placeholder(2, "Title 1", "type=\"title\"", Xfrm(0, 0, 10, 10))
                .Replace("<a:bodyPr/>", "<a:bodyPr anchor=\"b\" lIns=\"0\" wrap=\"none\"/>")
                .Replace("<a:lstStyle/>", "<a:lstStyle><a:lvl1pPr algn=\"ctr\"/></a:lstStyle>")
        };
        var slideTitle = Placeholder(2, "Title 1", "type=\"title\"", "", "<a:p><a:r><a:t>a</a:t></a:r></a:p><a:p><a:pPr algn=\"r\"/><a:r><a:t>b</a:t></a:r></a:p>")
            .Replace("<a:bodyPr/>", "<a:bodyPr tIns=\"1000\"/>");
        var shape = (AutoShape)builder.Slide(slideTitle).Load().Slides[0].Shapes[0];

        var own = shape.Text;
        Assert.Equal(((TextAnchor?)null, (long?)null, (long?)1000, (long?)null, (long?)null, (bool?)null),
            (own.Anchor, own.InsetLeft, own.InsetTop, own.InsetRight, own.InsetBottom, own.Wrap));
        var effective = shape.EffectiveBody();
        Assert.Equal((TextAnchor.Bottom, 0L, 1000L, 91440L, 45720L, false),
            (effective.Anchor, effective.InsetLeft, effective.InsetTop, effective.InsetRight, effective.InsetBottom, effective.Wrap));
        Assert.Equal([null, TextAlign.Right], shape.Text.Paragraphs.Select(p => p.Align));
        Assert.Equal([TextAlign.Center, TextAlign.Right], shape.Text.Paragraphs.Select(shape.EffectiveAlign));
    }

    [Fact]
    public void ColorResolver_WhitespaceOrEmpty_IsNoColor()
    {
        Assert.Null(ColorResolver.Resolve(" ", null));
        Assert.Null(ColorResolver.Resolve("", null));
    }

    [Fact]
    public void ColorResolver_Modifiers_ApplyInFileOrder()
    {
        Assert.Equal("#C0C0C0", ColorResolver.Resolve("#FFFFFF shade=50000 lumOff=25000", null));
        Assert.Equal("#808080", ColorResolver.Resolve("#FFFFFF lumOff=25000 shade=50000", null));
    }

    [Fact]
    public void Reader_NestedFlippedGroupWithMedia_ResolvesChildAndIsOpaque()
    {
        static string Group(uint id, string xfrmAttributes, string children) =>
            $"<p:grpSp><p:nvGrpSpPr><p:cNvPr id=\"{id}\" name=\"Group {id}\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm{xfrmAttributes}>" +
            "<a:off x=\"0\" y=\"0\"/><a:ext cx=\"1000\" cy=\"1000\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"1000\" cy=\"1000\"/></a:xfrm></p:grpSpPr>" +
            children + "</p:grpSp>";
        var video = "<p:pic><p:nvPicPr><p:cNvPr id=\"21\" name=\"Video\"/><p:cNvPicPr/><p:nvPr><a:videoFile r:link=\"rId7\"/></p:nvPr></p:nvPicPr>" +
                    $"<p:blipFill/><p:spPr>{Xfrm(0, 0, 10, 10)}</p:spPr></p:pic>";
        var presentation = new PptxBuilder()
            .Slide(Group(10, " flipH=\"1\"", Group(11, "", Shape(12, "Child", Xfrm(0, 0, 100, 100, " rot=\"1800000\"")) + video)))
            .Load();

        var inner = (GroupShape)((GroupShape)presentation.Slides[0].Shapes[0]).Children[0];
        Assert.Equal(new ShapeTransform(900, 0, 100, 100, 330, true), inner.Children[0].ResolvedTransform);
        Assert.Equal("media", Assert.IsType<OpaqueShape>(inner.Children[1]).Kind);
        Assert.True(presentation.HasOpaqueContent);
    }

    [Fact]
    public void Reader_DanglingSlideId_IsSkipped()
    {
        var presentation = new PptxBuilder { ExtraSlideIds = "<p:sldId id=\"300\" r:id=\"rId99\"/>" }.Slide("").Load();

        Assert.Single(presentation.Slides);
        Assert.Single(presentation.Package.Slides);
    }

    [Fact]
    public void Reader_GraphicFrameChart_IsOpaqueShape()
    {
        var presentation = new PptxBuilder().Slide(Frame(5, "Chart 4", ChartUri)).Load();

        var chart = Assert.IsType<OpaqueShape>(presentation.Slides[0].Shapes.Single());
        Assert.Equal("chart", chart.Kind);
        Assert.Equal("Chart 4", chart.Name);
        Assert.Equal(5u, chart.Id);
        Assert.Equal(new ShapeTransform(100, 200, 300, 400), chart.Transform);
        Assert.True(presentation.HasOpaqueContent);
    }

    [Fact]
    public void Reader_SmartArt_SetsHasOpaqueContent()
    {
        var presentation = new PptxBuilder().Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10))).Slide(Frame(7, "Diagram 6", DiagramUri)).Load();

        Assert.True(presentation.HasOpaqueContent);
        Assert.Equal("smartArt", ((OpaqueShape)presentation.Slides[1].Shapes[0]).Kind);
        Assert.False(new PptxBuilder().Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10))).Load().HasOpaqueContent);
    }

    [Fact]
    public void Reader_Notes_ReadsBodyPlaceholderText()
    {
        var presentation = new PptxBuilder().Slide("", notes: "First line\nSecond line").Slide("").Load();

        Assert.True(presentation.Slides[0].HasNotesPart);
        Assert.Equal("First line\nSecond line", presentation.Slides[0].Notes);
        Assert.False(presentation.Slides[1].HasNotesPart);
        Assert.Equal("", presentation.Slides[1].Notes);
    }

    [Fact]
    public void Reader_HiddenSlide()
    {
        var presentation = new PptxBuilder().Slide("", "show=\"0\"").Slide("").Load();

        Assert.True(presentation.Slides[0].Hidden);
        Assert.False(presentation.Slides[1].Hidden);
    }

    [Fact]
    public void Reader_SlideBackgroundSolid()
    {
        var presentation = new PptxBuilder
            {
                LayoutBackground = "<p:bg><p:bgPr><a:gradFill><a:gsLst><a:gs pos=\"0\"><a:srgbClr val=\"445566\"/></a:gs><a:gs pos=\"100000\"><a:srgbClr val=\"000000\"/></a:gs></a:gsLst></a:gradFill></p:bgPr></p:bg>"
            }
            .Slide("", background: "<p:bg><p:bgPr><a:solidFill><a:srgbClr val=\"112233\"/></a:solidFill><a:effectLst/></p:bgPr></p:bg>")
            .Slide("")
            .Load();

        var own = presentation.Slides[0];
        Assert.Equal(new SlideBackground(ShapeFillKind.Solid, "#112233", null), own.Background);
        Assert.Same(own.Background, SlideBackground.EffectiveBackground(own));
        var inheriting = presentation.Slides[1];
        Assert.Null(inheriting.Background);
        Assert.Equal(new SlideBackground(ShapeFillKind.Other, "#445566", null), inheriting.Layout.Background);
        Assert.Same(inheriting.Layout.Background, SlideBackground.EffectiveBackground(inheriting));
        Assert.Null(inheriting.Layout.Master.Background);
    }

    [Fact]
    public void Reader_BackgroundInheritedFromMaster_ViaBgRef()
    {
        var presentation = new PptxBuilder
            {
                MasterBackground = "<p:bg><p:bgRef idx=\"1002\"><a:schemeClr val=\"bg2\"/></p:bgRef></p:bg>",
                ThemeFormat = "<a:fmtScheme name=\"Test\"><a:fillStyleLst/><a:lnStyleLst/><a:effectStyleLst/><a:bgFillStyleLst>" +
                              "<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>" +
                              "<a:solidFill><a:schemeClr val=\"phClr\"><a:lumMod val=\"50000\"/></a:schemeClr></a:solidFill>" +
                              "</a:bgFillStyleLst></a:fmtScheme>"
            }
            .Slide("")
            .Load();
        var fallback = new PptxBuilder().ThemeColor("lt1", "FEFEFE").Slide("").Load();

        var slide = presentation.Slides[0];
        Assert.Null(slide.Background);
        Assert.Null(slide.Layout.Background);
        Assert.Equal(new SlideBackground(ShapeFillKind.Solid, "scheme:bg2 lumMod=50000", null), slide.Layout.Master.Background);
        Assert.Same(slide.Layout.Master.Background, SlideBackground.EffectiveBackground(slide));
        var white = SlideBackground.EffectiveBackground(fallback.Slides[0]);
        Assert.Equal(ShapeFillKind.Solid, white.Kind);
        Assert.Equal("#FEFEFE", ColorResolver.Resolve(white.Color, fallback.Masters[0]));
    }

    [Fact]
    public void Reader_PictureBackground_ImagePart()
    {
        var builder = new PptxBuilder();
        builder.ExtraParts["ppt/media/bg.png"] = (null, "png");
        var presentation = builder
            .Slide("", rels: "<Relationship Id=\"rId5\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"../media/bg.png\"/>",
                background: "<p:bg><p:bgPr><a:blipFill><a:blip r:embed=\"rId5\"/><a:stretch><a:fillRect/></a:stretch></a:blipFill><a:effectLst/></p:bgPr></p:bg>")
            .Load();

        Assert.Equal(new SlideBackground(ShapeFillKind.Picture, null, "/ppt/media/bg.png"), presentation.Slides[0].Background);
    }

    [Fact]
    public void Reader_LayoutAndMasterShapes_ExcludePlaceholders()
    {
        var builder = new PptxBuilder();
        builder.MasterShapes += Shape(10, "Logo", Xfrm(0, 0, 100, 100));
        builder.LayoutShapes += Shape(11, "Band", Xfrm(0, 0, 100, 100));
        var presentation = builder.Slide(Placeholder(2, "Title 1", "type=\"title\"", "")).Load();

        var layout = presentation.Layouts[0];
        Assert.Equal(["Logo"], layout.Master.Shapes.Select(s => s.Name));
        Assert.Equal(["Band"], layout.Shapes.Select(s => s.Name));
        Assert.Equal(new ShapeTransform(1000000, 500000, 8000000, 1000000), presentation.Slides[0].Shapes[0].ResolvedTransform);
    }
}
