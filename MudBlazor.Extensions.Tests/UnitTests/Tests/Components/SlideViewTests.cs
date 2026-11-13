using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideViewTests
{
    private const string Solid = "<a:solidFill><a:srgbClr val=\"FF0000\"/></a:solidFill>";

    private static IRenderedComponent<MudExSlideView> Render(TestContext context, Presentation presentation, Slide slide = null)
    {
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context.Render<MudExSlideView>(p => p
            .Add(c => c.Presentation, presentation)
            .Add(c => c.Slide, slide ?? presentation.Slides[0])
            .Add(c => c.Images, new SlideImageUrls(presentation)));
    }

    private static string Text(string text, string runProperties = "") =>
        $"<a:p><a:r><a:rPr lang=\"en-US\"{runProperties}/><a:t>{text}</a:t></a:r></a:p>";

    [Fact]
    public void SlideView_ShapePositionedInPercent()
    {
        var presentation = new PptxBuilder().Slide(Shape(2, "Box", Xfrm(1219200, 685800, 6096000, 3429000) + "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>" + Solid)).Load();
        using var context = new TestContext();

        var cut = Render(context, presentation);

        var style = cut.Find("[data-shape-id='2']").GetAttribute("style");
        Assert.Contains("left:10%", style);
        Assert.Contains("top:10%", style);
        Assert.Contains("width:50%", style);
        Assert.Contains("height:50%", style);
        Assert.Contains("aspect-ratio:12192000/6858000", cut.Find(".mud-ex-slide").GetAttribute("style"));
        Assert.Equal("#FF0000", cut.Find("[data-shape-id='2'] path").GetAttribute("fill"));
    }

    [Fact]
    public void SlideView_FontSizeInCqw()
    {
        var body = Text("Sized", " sz=\"2400\"");
        var presentation = new PptxBuilder().Slide(Shape(2, "Text", Xfrm(0, 0, 3000000, 1000000), body)).Load();
        using var context = new TestContext();

        var cut = Render(context, presentation);

        Assert.Contains("font-size:2.5cqw", cut.Find(".mud-ex-slide-text span").GetAttribute("style"));
    }

    [Fact]
    public void SlideView_ScriptTextRendersAsText()
    {
        var body = Text("&lt;script&gt;alert(1)&lt;/script&gt;&lt;b&gt;x&lt;/b&gt;");
        var presentation = new PptxBuilder().Slide(Shape(2, "Text", Xfrm(0, 0, 3000000, 1000000), body)).Load();
        using var context = new TestContext();

        var cut = Render(context, presentation);

        Assert.Empty(cut.FindAll(".mud-ex-slide script"));
        Assert.Empty(cut.FindAll(".mud-ex-slide b"));
        Assert.Equal("<script>alert(1)</script><b>x</b>", cut.Find(".mud-ex-slide-text span").TextContent);
    }

    [Fact]
    public void SlideView_OpaqueShape_RendersFrameWithName()
    {
        const string frame = "<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id=\"7\" name=\"Sales chart\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
                             "<p:xfrm><a:off x=\"100\" y=\"200\"/><a:ext cx=\"3000000\" cy=\"2000000\"/></p:xfrm>" +
                             "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"/></a:graphic></p:graphicFrame>";
        var presentation = new PptxBuilder().Slide(frame).Load();
        using var context = new TestContext();

        var cut = Render(context, presentation);

        var opaque = cut.Find("[data-shape-id='7']");
        Assert.Contains("mud-ex-slide-opaque", opaque.ClassName);
        Assert.Contains("Sales chart", opaque.TextContent);
        Assert.Contains("chart", cut.Find("[data-shape-id='7'] small").TextContent);
    }

    [Fact]
    public void SlideView_ChartFrame_RendersChart()
    {
        var presentation = new PptxBuilder { ExtraParts = { ["ppt/charts/chart1.xml"] = (ChartSnapshotReaderTests.ChartType, ChartSnapshotReaderTests.ChartXml()) } }
            .Slide(ChartSnapshotReaderTests.Frame(7, "Sales chart", "rIdC"), rels: ChartSnapshotReaderTests.ChartRel).Load();
        using var context = new TestContext();

        var cut = Render(context, presentation);

        var frame = cut.Find("[data-shape-id='7']");
        Assert.Equal("Sales", frame.QuerySelector(".mud-ex-sschart-title")!.TextContent);
        Assert.NotNull(frame.QuerySelector(".mud-ex-sschart-body"));
        Assert.Empty(frame.QuerySelectorAll(".mud-ex-slide-opaque-label"));
    }

    [Fact]
    public void SlideView_UnreadableChart_RendersFrame()
    {
        var presentation = new PptxBuilder { ExtraParts = { ["ppt/charts/chart1.xml"] = (ChartSnapshotReaderTests.ChartType, ChartSnapshotReaderTests.ChartXml(cache: false)) } }
            .Slide(ChartSnapshotReaderTests.Frame(7, "Sales chart", "rIdC"), rels: ChartSnapshotReaderTests.ChartRel).Load();
        using var context = new TestContext();

        var cut = Render(context, presentation);

        var frame = cut.Find("[data-shape-id='7']");
        Assert.Contains("Sales chart", frame.TextContent);
        Assert.Empty(frame.QuerySelectorAll(".mud-ex-sschart"));
    }

    [Fact]
    public void SlideView_UnchangedVersion_DoesNotRerender()
    {
        var presentation = new PptxBuilder().Slide(Shape(2, "Box", Xfrm(0, 0, 100, 100) + Solid)).Load();
        var slide = presentation.Slides[0];
        using var context = new TestContext();
        var cut = Render(context, presentation);
        var renders = cut.RenderCount;

        cut.Render(p => p.Add(c => c.Slide, slide));
        Assert.Equal(renders, cut.RenderCount);

        slide.Version++;
        cut.Render(p => p.Add(c => c.Slide, slide));
        Assert.True(cut.RenderCount > renders);
        var after = cut.RenderCount;

        cut.Render(p => p.Add(c => c.Slide, slide));
        Assert.Equal(after, cut.RenderCount);
    }

    [Fact]
    public void SlideView_GroupChildAtResolvedPosition()
    {
        var group = "<p:grpSp><p:nvGrpSpPr><p:cNvPr id=\"10\" name=\"Group\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr>" +
                    "<a:xfrm><a:off x=\"1219200\" y=\"685800\"/><a:ext cx=\"2438400\" cy=\"1371600\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"1219200\" cy=\"685800\"/></a:xfrm>" +
                    "</p:grpSpPr>" + Shape(11, "Child", Xfrm(609600, 342900, 609600, 342900) + Solid) + "</p:grpSp>";
        var presentation = new PptxBuilder().Slide(group).Load();
        using var context = new TestContext();

        var cut = Render(context, presentation);

        var child = presentation.Slides[0].Shapes.OfType<GroupShape>().Single().Children.Single().ResolvedTransform;
        Assert.Equal(new ShapeTransform(2438400, 1371600, 1219200, 685800), child);
        var style = cut.Find("[data-shape-id='11']").GetAttribute("style");
        Assert.Contains("left:20%", style);
        Assert.Contains("top:20%", style);
        Assert.Contains("width:10%", style);
        Assert.Contains("height:10%", style);
    }

    [Fact]
    public void SlideView_RendersBackgroundColor()
    {
        var builder = new PptxBuilder();
        builder.ExtraParts["ppt/media/bg.png"] = (null, "png");
        var presentation = builder
            .Slide("", background: "<p:bg><p:bgPr><a:solidFill><a:srgbClr val=\"112233\"/></a:solidFill></p:bgPr></p:bg>")
            .Slide("", rels: "<Relationship Id=\"rId5\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"../media/bg.png\"/>",
                background: "<p:bg><p:bgPr><a:blipFill><a:blip r:embed=\"rId5\"/></a:blipFill></p:bgPr></p:bg>")
            .Load();
        using var context = new TestContext();
        using var second = new TestContext();

        var solid = Render(context, presentation).Find(".mud-ex-slide").GetAttribute("style");
        var picture = Render(second,presentation, presentation.Slides[1]).Find(".mud-ex-slide").GetAttribute("style");

        Assert.Contains("background-color:#112233", solid);
        Assert.Contains("background-image:url(\"data:image/png;base64,", picture);
        Assert.Contains("background-size:cover", picture);
    }

    [Fact]
    public void SlideView_RendersMasterShapesBelowSlideShapes()
    {
        var builder = new PptxBuilder();
        builder.MasterShapes += Shape(10, "Logo", Xfrm(0, 0, 100, 100) + "<a:solidFill><a:srgbClr val=\"00FF00\"/></a:solidFill>");
        builder.LayoutShapes += Shape(11, "Band", Xfrm(0, 0, 100, 100) + "<a:solidFill><a:srgbClr val=\"0000FF\"/></a:solidFill>");
        var presentation = builder.Slide(Shape(2, "Box", Xfrm(0, 0, 100, 100) + Solid)).Load();
        using var context = new TestContext();

        var cut = Render(context, presentation);

        Assert.Equal(["#00FF00", "#0000FF", "#FF0000"], cut.FindAll(".mud-ex-slide-shape path").Select(p => p.GetAttribute("fill")));
        Assert.Equal(["2"], cut.FindAll("[data-shape-id]").Select(e => e.GetAttribute("data-shape-id")));
        Assert.Equal(2, cut.FindAll(".mud-ex-slide-inherited").Count);
    }

    [Fact]
    public void SlideView_ShowMasterSpFalse_HidesMasterShapes()
    {
        var builder = new PptxBuilder { LayoutAttributes = " showMasterSp=\"0\"" };
        builder.MasterShapes += Shape(10, "Logo", Xfrm(0, 0, 100, 100) + "<a:solidFill><a:srgbClr val=\"00FF00\"/></a:solidFill>");
        builder.LayoutShapes += Shape(11, "Band", Xfrm(0, 0, 100, 100) + "<a:solidFill><a:srgbClr val=\"0000FF\"/></a:solidFill>");
        var presentation = builder.Slide("").Slide("", "showMasterSp=\"0\"").Load();
        using var context = new TestContext();
        using var second = new TestContext();

        var layoutOnly = Render(context, presentation).FindAll(".mud-ex-slide-shape path").Select(p => p.GetAttribute("fill"));
        var none = Render(second,presentation, presentation.Slides[1]).FindAll(".mud-ex-slide-shape");

        Assert.Equal(["#0000FF"], layoutOnly);
        Assert.Empty(none);
    }

    [Fact]
    public void SlideView_RendersBulletAndHangingIndent()
    {
        var builder = new PptxBuilder
        {
            TxStyles = "<p:titleStyle/><p:bodyStyle><a:lvl1pPr marL=\"228600\" indent=\"-228600\"><a:buFont typeface=\"Arial\"/><a:buChar char=\"•\"/><a:defRPr sz=\"2800\"/></a:lvl1pPr>" +
                       "</p:bodyStyle><p:otherStyle/>"
        };
        var body = Text("First") + Text("Second") + "<a:p><a:pPr><a:buNone/></a:pPr><a:r><a:t>Plain</a:t></a:r></a:p>";
        var presentation = builder.Slide(Placeholder(3, "Body", "type=\"body\" idx=\"1\"", "", body)).Load();
        using var context = new TestContext();

        var cut = Render(context, presentation);

        var paragraphs = cut.FindAll(".mud-ex-slide-paragraph");
        Assert.Equal(["•", "•"], cut.FindAll(".mud-ex-slide-bullet").Select(b => b.TextContent.Trim(' ')));
        Assert.Equal("First", paragraphs[0].QuerySelector(".mud-ex-slide-bullet + span")!.TextContent);
        Assert.Contains("padding-left:1.875cqw", paragraphs[0].GetAttribute("style"));
        Assert.Contains("text-indent:-1.875cqw", paragraphs[0].GetAttribute("style"));
        Assert.Contains("--mud-ex-slide-hang:1.875cqw", paragraphs[0].GetAttribute("style"));
        Assert.Null(paragraphs[2].QuerySelector(".mud-ex-slide-bullet"));
        Assert.Equal(["•", "•", null], SlideTextDom.ToDom((AutoShape)presentation.Slides[0].Shapes[0], presentation).Select(p => p.Marker));
    }

    [Fact]
    public void SlideView_SlideNumberField_ShowsNumber()
    {
        const string field = "<a:p><a:fld id=\"{B6F15528-21DE-4FAA-801E-634DDDAF4B2B}\" type=\"slidenum\"><a:rPr lang=\"en-US\"/><a:t>‹#›</a:t></a:fld>" +
                             "<a:r><a:t> / </a:t></a:r><a:fld id=\"{1D8BD707-D9CF-40AE-B4C6-C98DA3205C09}\" type=\"datetimeFigureOut\"><a:t>1/1/2000</a:t></a:fld></a:p>";
        var shape = Shape(2, "Number", Xfrm(0, 0, 3000000, 1000000), field);
        var presentation = new PptxBuilder { PresentationAttributes = " firstSlideNum=\"5\"" }.Slide(shape).Slide(shape).Load();
        using var context = new TestContext();
        using var second = new TestContext();

        var first = Render(context, presentation).Find(".mud-ex-slide-text").TextContent;
        var next = Render(second, presentation, presentation.Slides[1]).Find(".mud-ex-slide-text").TextContent;

        Assert.Equal("slidenum", ((AutoShape)presentation.Slides[0].Shapes[0]).Text.Paragraphs[0].Runs[0].FieldType);
        Assert.Null(((AutoShape)presentation.Slides[0].Shapes[0]).Text.Paragraphs[0].Runs[1].FieldType);
        var today = DateTime.Today.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        Assert.Equal($"5 / {today}", first);
        Assert.Equal($"6 / {today}", next);
    }

    [Fact]
    public void SlideView_GradientFill_RendersLinearGradient()
    {
        const string gradient = "<a:gradFill><a:gsLst><a:gs pos=\"0\"><a:srgbClr val=\"FF0000\"/></a:gs><a:gs pos=\"100000\"><a:srgbClr val=\"0000FF\"/></a:gs></a:gsLst>" +
                                "<a:lin ang=\"5400000\"/></a:gradFill>";
        var presentation = new PptxBuilder()
            .Slide(Shape(2, "Box", Xfrm(0, 0, 100, 100) + "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>" + gradient),
                background: "<p:bg><p:bgPr>" + gradient + "</p:bgPr></p:bg>")
            .Load();
        using var context = new TestContext();

        var cut = Render(context, presentation);

        var fill = cut.Find("[data-shape-id='2'] path").GetAttribute("fill");
        var id = cut.Find("[data-shape-id='2'] defs > *").GetAttribute("id");
        Assert.Equal($"url(#{id})", fill);
        Assert.Equal(("0.5", "0", "0.5", "1"), (Attr("x1"), Attr("y1"), Attr("x2"), Attr("y2")));
        Assert.Equal(["0:#FF0000", "1:#0000FF"], cut.FindAll("[data-shape-id='2'] stop").Select(s => $"{s.GetAttribute("offset")}:{s.GetAttribute("stop-color")}"));
        Assert.Contains("background-image:linear-gradient(180deg, #FF0000 0%, #0000FF 100%)", cut.Find(".mud-ex-slide").GetAttribute("style"));
        return;

        string Attr(string name) => cut.Find("[data-shape-id='2'] defs > *").GetAttribute(name);
    }
}
