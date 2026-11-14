using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public partial class PptxWriterTests
{
    [Fact]
    public void Writer_SlideSize_RoundTrip_WritesSizeSlidesLayoutsAndMasters()
    {
        var source = SlideSizeTests.Builder().Build();
        var presentation = Load(source);
        presentation.Execute(new SetSlideSizeCommand(9144000, 6858000, true));
        var expected = SlideSizeTests.Snapshot(presentation);

        var output = Save(presentation);

        AssertConsistent(output);
        var size = Xml(output, "/ppt/presentation.xml").Root!.Element(P + "sldSz")!;
        Assert.Equal(("9144000", "6858000", "screen4x3"), ((string)size.Attribute("cx"), (string)size.Attribute("cy"), (string)size.Attribute("type")));
        var reloaded = Load(output);
        Assert.Equal(Describe(presentation), Describe(reloaded));
        Assert.Equal(Inherited(expected), Inherited(SlideSizeTests.Snapshot(reloaded)));
        Assert.Equal([1500000L, 750000L], reloaded.Slides[0].Shapes.OfType<TableShape>().Single().ColumnWidths);
        var layout = Xml(output, "/ppt/slideLayouts/slideLayout1.xml");
        Assert.Equal(Xml(source, "/ppt/slideLayouts/slideLayout1.xml").Descendants(A + "xfrm").Count(), layout.Descendants(A + "xfrm").Count());
        Assert.Equal(Parts(source)["/ppt/theme/theme1.xml"], Parts(output)["/ppt/theme/theme1.xml"]);

        presentation.Execute(new SetSlideSizeCommand(12192000, 6858000, true));
        Assert.Null(Xml(Save(presentation), "/ppt/presentation.xml").Root!.Element(P + "sldSz")!.Attribute("type"));

        presentation.Undo.Undo();
        presentation.Undo.Undo();
        var undone = Save(presentation);
        AssertConsistent(undone);
        foreach (var (name, bytes) in Parts(source))
            Assert.True(bytes.SequenceEqual(Parts(undone)[name]), name);
    }

    private static string Inherited(string snapshot) =>
        string.Join("\n", snapshot.Split('\n').Where(l => l.StartsWith("size", StringComparison.Ordinal) || l.StartsWith("inherited", StringComparison.Ordinal)));
}
