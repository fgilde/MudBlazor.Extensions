using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Office;

using MudBlazor.Extensions.Office.Presentation;

public class ChartSnapshotReaderTests
{
    internal const string ChartRel = "<Relationship Id=\"rIdC\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart\" Target=\"../charts/chart1.xml\"/>";
    internal const string ChartType = "application/vnd.openxmlformats-officedocument.drawingml.chart+xml";

    internal static string ChartXml(string firstSeriesFill = "", bool cache = true) =>
        "<c:chartSpace xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"><c:chart>" +
        "<c:title><c:tx><c:rich><a:bodyPr/><a:p><a:r><a:t>Sales</a:t></a:r></a:p></c:rich></c:tx></c:title>" +
        "<c:plotArea><c:barChart><c:barDir val=\"col\"/><c:grouping val=\"clustered\"/>" +
        Series(0, "Q1", firstSeriesFill, cache, "1", "2.5") + Series(1, "Q2", "", cache, "3", "") +
        "</c:barChart></c:plotArea><c:legend/></c:chart></c:chartSpace>";

    private static string Series(int index, string name, string spPr, bool cache, params string[] values) =>
        $"<c:ser><c:idx val=\"{index}\"/><c:order val=\"{index}\"/><c:tx><c:strRef><c:f>S!$B$1</c:f><c:strCache><c:ptCount val=\"1\"/><c:pt idx=\"0\"><c:v>{name}</c:v></c:pt></c:strCache></c:strRef></c:tx>{spPr}" +
        "<c:cat><c:strRef><c:f>S!$A$2:$A$3</c:f>" + (cache ? "<c:strCache><c:ptCount val=\"2\"/><c:pt idx=\"0\"><c:v>North</c:v></c:pt><c:pt idx=\"1\"><c:v>South</c:v></c:pt></c:strCache>" : "") + "</c:strRef></c:cat>" +
        "<c:val><c:numRef><c:f>S!$B$2:$B$3</c:f>" + (cache ? "<c:numCache><c:ptCount val=\"2\"/>" + string.Concat(values.Select((v, i) => v == "" ? "" : $"<c:pt idx=\"{i}\"><c:v>{v}</c:v></c:pt>")) + "</c:numCache>" : "") + "</c:numRef></c:val></c:ser>";

    private static string Hex(string color) => color;

    [Fact]
    public void ChartSnapshot_ReadsColumnChartCaches()
    {
        var fill = "<c:spPr><a:solidFill><a:srgbClr val=\"ff0000\"/></a:solidFill></c:spPr>";

        var snapshot = ChartSnapshotReader.Read(XDocument.Parse(ChartXml(fill)), Hex)!;

        Assert.Equal((ChartKind.Column, "Sales", true, false), (snapshot.Kind, snapshot.Title, snapshot.ShowLegend, snapshot.Stacked));
        Assert.Equal(["North", "South"], snapshot.Categories);
        Assert.Equal(["Q1", "Q2"], snapshot.Series.Select(s => s.Name));
        Assert.Equal([1d, 2.5], snapshot.Series[0].Values);
        Assert.Equal([3d, null], snapshot.Series[1].Values);
        Assert.Equal("#FF0000", snapshot.Series[0].Color);
        Assert.Equal("scheme:accent2", snapshot.Series[1].Color);
    }

    [Fact]
    public void ChartSnapshot_NoCache_IsNull()
    {
        Assert.Null(ChartSnapshotReader.Read(XDocument.Parse(ChartXml(cache: false)), Hex));
    }

    [Fact]
    public void ChartSnapshot_UnknownChartType_IsNull()
    {
        var xml = ChartXml().Replace("barChart", "doughnutChart");

        Assert.Null(ChartSnapshotReader.Read(XDocument.Parse(xml), Hex));
    }

    [Fact]
    public void Reader_ChartFrame_HasSnapshot()
    {
        var presentation = new PptxBuilder { ExtraParts = { ["ppt/charts/chart1.xml"] = (ChartType, ChartXml()) } }
            .Slide(Frame(7, "Sales chart", "rIdC"), rels: ChartRel).Load();

        var chart = Assert.IsType<OpaqueShape>(presentation.Slides[0].Shapes[0]);

        Assert.Equal("chart", chart.Kind);
        Assert.Equal(ChartKind.Column, chart.Chart!.Kind);
        Assert.Equal("#4472C4", chart.Chart.Series[0].Color);
    }

    [Fact]
    public void Reader_BrokenChartPart_LeavesChartNull()
    {
        var presentation = new PptxBuilder { ExtraParts = { ["ppt/charts/chart1.xml"] = (ChartType, "<c:chartSpace") } }
            .Slide(Frame(7, "Sales chart", "rIdC"), rels: ChartRel).Load();

        Assert.Null(Assert.IsType<OpaqueShape>(presentation.Slides[0].Shapes[0]).Chart);
    }

    [Fact]
    public void Reader_MissingChartPart_LeavesChartNull()
    {
        var presentation = new PptxBuilder().Slide(Frame(7, "Sales chart", "rIdC"), rels: ChartRel).Load();

        Assert.Null(Assert.IsType<OpaqueShape>(presentation.Slides[0].Shapes[0]).Chart);
    }

    internal static string Frame(uint id, string name, string rid) =>
        $"<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id=\"{id}\" name=\"{name}\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
        "<p:xfrm><a:off x=\"100\" y=\"200\"/><a:ext cx=\"3000000\" cy=\"2000000\"/></p:xfrm>" +
        "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\">" +
        $"<c:chart xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" r:id=\"{rid}\"/></a:graphicData></a:graphic></p:graphicFrame>";
}
