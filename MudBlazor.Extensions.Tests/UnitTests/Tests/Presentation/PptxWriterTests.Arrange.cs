using System.Xml.Linq;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public partial class PptxWriterTests
{
    private const string SlidePart = "/ppt/slides/slide1.xml";

    private static XElement Tree(byte[] package) => Xml(package, SlidePart).Descendants(P + "spTree").Single();

    private static XElement ShapeElement(XElement tree, string id) => tree.Descendants().Single(e => (string)e.Element(P + "nvSpPr")?.Element(P + "cNvPr")?.Attribute("id") == id);

    private static XElement WithoutXfrm(XElement e)
    {
        var copy = new XElement(e);
        copy.Descendants(A + "xfrm").Remove();
        return copy;
    }

    private static IEnumerable<string> TopLevelIds(XElement tree) =>
        tree.Elements().Where(e => e.Name != P + "nvGrpSpPr" && e.Name != P + "grpSpPr").Select(e => (string)e.Descendants(P + "cNvPr").First().Attribute("id"));

    [Fact]
    public void Writer_NewGroup_WritesGrpSpWithChOffChExt_RoundTrips()
    {
        var presentation = ArrangeCommandTests.Deck();
        var source = Tree(Save(presentation));
        var command = new GroupShapesCommand(presentation.Slides[0], [3, 5]);
        presentation.Execute(command);

        var output = Save(presentation);

        AssertConsistent(output);
        var tree = Tree(output);
        Assert.Equal(["2", "4", "25", "20"], TopLevelIds(tree));
        var group = tree.Elements(P + "grpSp").First();
        Assert.Equal(["nvGrpSpPr", "grpSpPr", "sp", "sp"], group.Elements().Select(e => e.Name.LocalName));
        var properties = group.Element(P + "nvGrpSpPr")!.Element(P + "cNvPr")!;
        Assert.Equal(("25", "Group 24"), ((string)properties.Attribute("id"), (string)properties.Attribute("name")));
        var xfrm = group.Element(P + "grpSpPr")!.Element(A + "xfrm")!;
        Assert.Equal(["off", "ext", "chOff", "chExt"], xfrm.Elements().Select(e => e.Name.LocalName));
        Assert.Equal(["1000000", "1000000", "7000000", "3000000", "1000000", "1000000", "7000000", "3000000"], xfrm.Elements().Attributes().Select(a => a.Value));
        Assert.True(XNode.DeepEquals(ShapeElement(source, "3"), group.Elements(P + "sp").First()));
        Assert.True(XNode.DeepEquals(ShapeElement(source, "5"), group.Elements(P + "sp").Last()));
        var reloaded = Load(output);
        Assert.Equal(Describe(presentation), Describe(reloaded));
        Assert.Equal(command.Group.ChildTransform, ((GroupShape)reloaded.Slides[0].Shapes[2]).ChildTransform);
    }

    [Fact]
    public void Writer_Ungroup_RoundTrips()
    {
        var builder = new PptxBuilder().Slide(Shape(3, "A", Xfrm(0, 0, 10, 10)) + ArrangeCommandTests.RotatedGroup, tail: Timing(20, 21));
        var input = builder.Build();
        var presentation = Load(input);
        var slide = presentation.Slides[0];
        var source = Tree(input);

        presentation.Execute(new UngroupShapesCommand(slide, [23, 20]));
        var output = Save(presentation);

        AssertConsistent(output);
        var tree = Tree(output);
        Assert.Equal(["3", "21", "22", "24"], TopLevelIds(tree));
        Assert.Empty(tree.Descendants(P + "grpSp"));
        foreach (var id in new[] { "21", "22", "24" }) Assert.True(XNode.DeepEquals(WithoutXfrm(ShapeElement(source, id)), WithoutXfrm(ShapeElement(tree, id))), id);
        Assert.Single(ShapeElement(tree, "21").Descendants(A + "glow"));
        var xml = Xml(output, SlidePart);
        Assert.Equal(["21"], xml.Descendants(P + "spTgt").Select(t => (string)t.Attribute("spid")));
        Assert.Equal(["21"], xml.Descendants(P + "bldP").Select(t => (string)t.Attribute("spid")));
        var reloaded = Load(output);
        Assert.Equal(Describe(presentation), Describe(reloaded));
        foreach (var (shape, read) in SlideShape.Flatten(slide.Shapes).Zip(SlideShape.Flatten(reloaded.Slides[0].Shapes)))
            Assert.Equal(shape.ResolvedTransform, read.ResolvedTransform);

        presentation.Undo.Undo();
        Assert.Equal(Parts(input)[SlidePart], Parts(Save(presentation))[SlidePart]);
        presentation.Undo.Redo();
        Assert.Equal(output, Save(presentation));
    }
}
