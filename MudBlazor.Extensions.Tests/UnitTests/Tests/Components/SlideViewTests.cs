using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
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
}
