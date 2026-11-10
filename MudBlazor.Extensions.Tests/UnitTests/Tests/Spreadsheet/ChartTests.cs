using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class ChartTests
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string ChartNs = "http://schemas.openxmlformats.org/drawingml/2006/chart";
    private const string DrawingNs = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private const string XdrNs = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
    private const string CxNs = "http://schemas.microsoft.com/office/drawing/2014/chartex";
    private const string McNs = "http://schemas.openxmlformats.org/markup-compatibility/2006";
    private const string DrawingType = "application/vnd.openxmlformats-officedocument.drawing+xml";
    private const string ChartType = "application/vnd.openxmlformats-officedocument.drawingml.chart+xml";
    private static readonly XNamespace M = Main;
    private static readonly XNamespace C = ChartNs;
    private static readonly XNamespace Xdr = XdrNs;
    private static readonly XNamespace Pkg = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Types = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly XName RelId = XName.Get("id", Rel);

    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static (string?, string?, string?, string?, string?) S(ChartSeries s) => (s.NameRef, s.Name, s.CategoriesRef, s.ValuesRef, s.Color);

    [Fact]
    public void ReadChart_KindsSeriesAndAnchor()
    {
        var wb = Read(ChartPackage());
        var ws = wb.Sheets[0];
        Assert.False(wb.HasUnshiftedReferences);
        Assert.Equal(2, ws.Charts.Count);

        var column = ws.Charts[0];
        Assert.Equal((ChartKind.Column, "Sales", false, true), (column.Kind, column.Title, column.Stacked, column.ShowLegend));
        Assert.Equal([("Sheet1!$B$1", "North", "Sheet1!$A$2:$A$4", "Sheet1!$B$2:$B$4", "#4472C4"), ("Sheet1!$C$1", "South", "Sheet1!$A$2:$A$4", "Sheet1!$C$2:$C$4", null)],
            column.Series.Select(S));
        Assert.Equal((R("E2"), R("L17")), (column.From, column.To));
        Assert.Equal(((1.0, 2.0), (0.0, 0.0)), (column.FromOffset, column.ToOffset));

        var pie = ws.Charts[1];
        Assert.Equal((ChartKind.Pie, (string?)null, false), (pie.Kind, pie.Title, pie.ShowLegend));
        Assert.Equal([(null, "Share", "Sheet1!$A$2:$A$4", "Missing!$B$2:$B$4", null)], pie.Series.Select(S));
        Assert.Equal((R("N2"), R("S12")), (pie.From, pie.To));
        Assert.NotEqual(column.Id, pie.Id);

        var bar = Read(ChartPackage(chart2: BarChart(stacked: true))).Sheets[0].Charts[1];
        Assert.Equal((ChartKind.Bar, true, "Bars"), (bar.Kind, bar.Stacked, bar.Title));
    }

    [Fact]
    public void InsertRow_ShiftsChartFormulas_RestOfChartXmlIdentical()
    {
        var package = ChartPackage();
        var wb = Read(package);
        var ws = wb.Sheets[0];
        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        Assert.Equal(("Sheet1!$B$2", "Sheet1!$A$3:$A$5", "Sheet1!$B$3:$B$5"), (ws.Charts[0].Series[0].NameRef, ws.Charts[0].Series[0].CategoriesRef, ws.Charts[0].Series[0].ValuesRef));
        Assert.Equal((R("E3"), R("L18")), (ws.Charts[0].From, ws.Charts[0].To));
        Assert.False(wb.HasUnshiftedReferences);

        var written = Write(wb);
        Assert.True(XNode.DeepEquals(XDocument.Parse(ColumnChart(1)).Root, Xml(written, "xl/charts/chart1.xml").Root));
        Assert.True(XNode.DeepEquals(XDocument.Parse(PieChart(1)).Root, Xml(written, "xl/charts/chart2.xml").Root));
        Assert.True(XNode.DeepEquals(XDocument.Parse(Drawing(1)).Root, Xml(written, "xl/drawings/drawing1.xml").Root));
        foreach (var same in (string[])["xl/drawings/_rels/drawing1.xml.rels", "xl/charts/_rels/chart1.xml.rels", "xl/charts/style1.xml", "xl/media/image1.png", "xl/worksheets/_rels/sheet1.xml.rels"])
            Assert.Equal(Entry(package, same), Entry(written, same));

        wb.Execute(new DeleteColumnsCommand(ws, 0, 1));
        Assert.Equal(("Sheet1!$A$2", "Sheet1!$A$3:$A$5"), (ws.Charts[0].Series[0].NameRef, ws.Charts[0].Series[0].ValuesRef));
        Assert.Contains("#REF!", ws.Charts[0].Series[0].CategoriesRef);
        Assert.Equal(R("D3"), ws.Charts[0].From);
        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal((R("E2"), "Sheet1!$B$2:$B$4"), (ws.Charts[0].From, ws.Charts[0].Series[0].ValuesRef));
        var undone = Write(wb);
        foreach (var same in (string[])["xl/charts/chart1.xml", "xl/charts/chart2.xml", "xl/drawings/drawing1.xml"])
            Assert.Equal(Entry(package, same), Entry(undone, same));
    }

    [Fact]
    public void RenameSheet_UpdatesChartFormulas()
    {
        var package = ChartPackage();
        var wb = Read(package);
        var ws = wb.Sheets[0];
        wb.Execute(new RenameSheetCommand(ws, "My Data"));
        Assert.Equal("'My Data'!$B$2:$B$4", ws.Charts[0].Series[0].ValuesRef);
        Assert.Equal("Missing!$B$2:$B$4", ws.Charts[1].Series[0].ValuesRef);

        var written = Write(wb);
        Assert.True(XNode.DeepEquals(XDocument.Parse(ColumnChart(0, "'My Data'")).Root, Xml(written, "xl/charts/chart1.xml").Root));
        Assert.True(XNode.DeepEquals(XDocument.Parse(PieChart(0, "'My Data'")).Root, Xml(written, "xl/charts/chart2.xml").Root));
        Assert.Equal(Entry(package, "xl/drawings/drawing1.xml"), Entry(written, "xl/drawings/drawing1.xml"));

        wb.Undo.Undo();
        Assert.Equal("Sheet1!$B$2:$B$4", ws.Charts[0].Series[0].ValuesRef);
        Assert.Equal(Entry(package, "xl/charts/chart1.xml"), Entry(Write(wb), "xl/charts/chart1.xml"));
    }

    [Fact]
    public void InsertChart_FromRange_SeriesLikeExcel()
    {
        var wb = Workbook.Create("Data");
        var ws = wb.Sheets[0];
        Fill(wb, ws, "A1", ["Month", "North", "South"], ["Jan", 10, 20], ["Feb", 11, 21], ["Mar", 12, 22]);
        var insert = new InsertChartCommand(ws, CellRange.Parse("A1:C4"), ChartKind.Column);
        wb.Execute(insert);
        var chart = Assert.Single(ws.Charts);
        Assert.Same(chart, insert.Chart);
        Assert.Equal((ChartKind.Column, true), (chart.Kind, chart.ShowLegend));
        Assert.Equal([("Data!$B$1", "North", "Data!$A$2:$A$4", "Data!$B$2:$B$4", null), ("Data!$C$1", "South", "Data!$A$2:$A$4", "Data!$C$2:$C$4", null)],
            chart.Series.Select(S));
        Assert.Equal(R("E1"), chart.From);
        Assert.True(chart.To.Row > chart.From.Row && chart.To.Column > chart.From.Column);

        var empty = Workbook.Create();
        var corner = empty.Sheets[0];
        Fill(empty, corner, "B2", [null, "Q1", "Q2", "Q3", "Q4"], ["East", 1, 2, 3, 4], ["West", 5, 6, 7, 8]);
        empty.Execute(new InsertChartCommand(corner, CellRange.Parse("B2:F4"), ChartKind.Line));
        Assert.Equal([("Sheet1!$B$3", "East", "Sheet1!$C$2:$F$2", "Sheet1!$C$3:$F$3", null), ("Sheet1!$B$4", "West", "Sheet1!$C$2:$F$2", "Sheet1!$C$4:$F$4", null)],
            corner.Charts[0].Series.Select(S));

        var numbers = Workbook.Create();
        var plain = numbers.Sheets[0];
        Fill(numbers, plain, "A1", [1, 2], [3, 4], [5, 6]);
        numbers.Execute(new InsertChartCommand(plain, CellRange.Parse("A1:B3"), ChartKind.Area));
        Assert.Equal([(null, "Series1", null, "Sheet1!$A$1:$A$3", null), (null, "Series2", null, "Sheet1!$B$1:$B$3", null)], plain.Charts[0].Series.Select(S));

        var scatter = Workbook.Create();
        var xy = scatter.Sheets[0];
        Fill(scatter, xy, "A1", ["X", "Y"], [1, 2], [3, 4]);
        scatter.Execute(new InsertChartCommand(xy, CellRange.Parse("A1:B3"), ChartKind.Scatter));
        Assert.Equal([("Sheet1!$B$1", "Y", "Sheet1!$A$2:$A$3", "Sheet1!$B$2:$B$3", null)], xy.Charts[0].Series.Select(S));

        Assert.Throws<ArgumentException>(() => wb.Execute(new InsertChartCommand(ws, CellRange.Parse("A1:C4"), ChartKind.Other)));
        wb.Undo.Undo();
        Assert.Empty(ws.Charts);
        wb.Undo.Redo();
        Assert.Same(chart, Assert.Single(ws.Charts));

        wb.Execute(new UpdateChartCommand(ws, chart.Id, ChartKind.Bar, "Regions", false, stacked: true));
        Assert.Equal((ChartKind.Bar, "Regions", false, true), (ws.Charts[0].Kind, ws.Charts[0].Title, ws.Charts[0].ShowLegend, ws.Charts[0].Stacked));
        wb.Execute(new MoveChartCommand(ws, chart.Id, R("H2"), R("N20"), (3, 4)));
        Assert.Equal((R("H2"), R("N20"), (3.0, 4.0)), (ws.Charts[0].From, ws.Charts[0].To, ws.Charts[0].FromOffset));
        wb.Execute(new DeleteChartCommand(ws, chart.Id));
        Assert.Empty(ws.Charts);
        wb.Undo.Undo();
        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Same(chart, Assert.Single(ws.Charts));
        Assert.Throws<ArgumentException>(() => wb.Execute(new DeleteChartCommand(ws, "nope")));
    }

    [Fact]
    public void InsertChart_WritesDrawingChartRelsContentTypes()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        Fill(wb, ws, "A1", ["Month", "North", "South"], ["Jan", 10, 20], ["Feb", 11, 21]);
        foreach (var kind in (ChartKind[])[ChartKind.Column, ChartKind.Bar, ChartKind.Line, ChartKind.Pie, ChartKind.Area, ChartKind.Scatter])
            wb.Execute(new InsertChartCommand(ws, CellRange.Parse("A1:C3"), kind));
        wb.Execute(new UpdateChartCommand(ws, ws.Charts[0].Id, ChartKind.Column, "Totals", true, stacked: true));
        var written = Write(wb);

        var sheet = Xml(written, "xl/worksheets/sheet1.xml").Root!;
        var drawingId = (string)sheet.Element(M + "drawing")!.Attribute(RelId)!;
        Assert.True(sheet.Elements().ToList().IndexOf(sheet.Element(M + "drawing")!) > sheet.Elements().ToList().IndexOf(sheet.Element(M + "sheetData")!));
        var sheetRel = Xml(written, "xl/worksheets/_rels/sheet1.xml.rels").Root!.Elements(Pkg + "Relationship").Single(r => (string)r.Attribute("Id")! == drawingId);
        Assert.Equal((Rel + "/drawing", "../drawings/drawing1.xml"), ((string)sheetRel.Attribute("Type")!, (string)sheetRel.Attribute("Target")!));
        var drawingRels = Xml(written, "xl/drawings/_rels/drawing1.xml.rels").Root!.Elements(Pkg + "Relationship").ToList();
        Assert.Equal(6, drawingRels.Count);
        Assert.All(drawingRels, r => Assert.Equal(Rel + "/chart", (string)r.Attribute("Type")!));
        var types = Xml(written, "[Content_Types].xml").Root!.Elements(Types + "Override").ToDictionary(e => (string)e.Attribute("PartName")!, e => (string)e.Attribute("ContentType")!);
        Assert.Equal(DrawingType, types["/xl/drawings/drawing1.xml"]);
        for (var i = 1; i <= 6; i++) Assert.Equal(ChartType, types[$"/xl/charts/chart{i}.xml"]);

        var drawing = Xml(written, "xl/drawings/drawing1.xml").Root!;
        var anchors = drawing.Elements(Xdr + "twoCellAnchor").ToList();
        Assert.Equal(6, anchors.Count);
        Assert.Equal(6, anchors.Select(a => (string)a.Descendants(Xdr + "cNvPr").Single().Attribute("id")!).Distinct().Count());
        foreach (var anchor in anchors)
        {
            Assert.Equal(["from", "to", "graphicFrame", "clientData"], anchor.Elements().Select(e => e.Name.LocalName));
            Assert.All(anchor.Elements().Take(2).SelectMany(m => m.Elements()), v => Assert.Matches("^[0-9]+$", v.Value));
            var id = (string)anchor.Descendants(C + "chart").Single().Attribute(RelId)!;
            var target = (string)drawingRels.Single(r => (string)r.Attribute("Id")! == id).Attribute("Target")!;
            Assert.StartsWith("../charts/chart", target);
            var chart = Xml(written, "xl/" + target[3..]).Root!;
            Assert.Equal(["roundedCorners", "chart"], chart.Elements().Select(e => e.Name.LocalName));
            Assert.Equal("0", (string)chart.Element(C + "roundedCorners")!.Attribute("val")!);
            var plot = chart.Element(C + "chart")!.Element(C + "plotArea")!;
            var axes = plot.Elements().Where(e => e.Name.LocalName.EndsWith("Ax")).ToList();
            var type = plot.Elements().Single(e => e.Name.LocalName.EndsWith("Chart"));
            var axIds = type.Elements(C + "axId").Select(a => (string)a.Attribute("val")!).ToList();
            Assert.Equal(axIds, axes.Select(a => (string)a.Element(C + "axId")!.Attribute("val")!));
            if (axes.Count == 2)
            {
                Assert.Equal(axIds[1], (string)axes[0].Element(C + "crossAx")!.Attribute("val")!);
                Assert.Equal(axIds[0], (string)axes[1].Element(C + "crossAx")!.Attribute("val")!);
            }
        }

        var first = Xml(written, "xl/charts/chart1.xml").Root!.Element(C + "chart")!;
        Assert.Equal(["title", "autoTitleDeleted", "plotArea", "legend", "plotVisOnly", "dispBlanksAs"], first.Elements().Select(e => e.Name.LocalName));
        var bar = first.Element(C + "plotArea")!.Element(C + "barChart")!;
        Assert.Equal(["barDir", "grouping", "varyColors", "ser", "ser", "gapWidth", "overlap", "axId", "axId"], bar.Elements().Select(e => e.Name.LocalName));
        Assert.Equal(("col", "stacked", "100"), ((string)bar.Element(C + "barDir")!.Attribute("val")!, (string)bar.Element(C + "grouping")!.Attribute("val")!,
            (string)bar.Element(C + "overlap")!.Attribute("val")!));
        Assert.Equal(["idx", "order", "tx", "invertIfNegative", "cat", "val"], bar.Elements(C + "ser").First().Elements().Select(e => e.Name.LocalName));
        Assert.Equal(["Sheet1!$B$1", "Sheet1!$A$2:$A$3", "Sheet1!$B$2:$B$3"], bar.Elements(C + "ser").First().Descendants(C + "f").Select(f => f.Value));
        Assert.Equal(["10", "11"], bar.Elements(C + "ser").First().Element(C + "val")!.Descendants(C + "v").Select(v => v.Value));
        var scatter = Xml(written, "xl/charts/chart6.xml").Root!.Descendants(C + "scatterChart").Single();
        Assert.Equal(["xVal", "yVal"], scatter.Element(C + "ser")!.Elements().Where(e => e.Name.LocalName.EndsWith("Val")).Select(e => e.Name.LocalName));
        Assert.Equal(2, scatter.Parent!.Elements(C + "valAx").Count());

        var back = Read(written);
        Assert.False(back.HasUnshiftedReferences);
        Assert.Equal([ChartKind.Column, ChartKind.Bar, ChartKind.Line, ChartKind.Pie, ChartKind.Area, ChartKind.Scatter], back.Sheets[0].Charts.Select(c => c.Kind));
        Assert.Equal(ws.Charts.Select(c => (c.Title, c.Stacked, c.From, c.To)), back.Sheets[0].Charts.Select(c => (c.Title, c.Stacked, c.From, c.To)));
        Assert.Equal(ws.Charts[0].Series.Select(S), back.Sheets[0].Charts[0].Series.Select(S));

        var package = ChartPackage();
        var file = Read(package);
        file.Execute(new InsertChartCommand(file.Sheets[0], CellRange.Parse("A1:B4"), ChartKind.Line));
        var added = Write(file);
        Assert.Contains("xl/charts/chart3.xml", Entries(added));
        Assert.Equal(4, Xml(added, "xl/drawings/drawing1.xml").Root!.Elements().Count());
        Assert.Equal(Entry(package, "xl/worksheets/_rels/sheet1.xml.rels"), Entry(added, "xl/worksheets/_rels/sheet1.xml.rels"));
        Assert.Equal(Entry(package, "xl/charts/chart1.xml"), Entry(added, "xl/charts/chart1.xml"));
        Assert.Equal(3, Xml(added, "xl/drawings/_rels/drawing1.xml.rels").Root!.Elements().Count(r => ((string)r.Attribute("Type")!).EndsWith("/chart")));
        Assert.Equal(3, Read(added).Sheets[0].Charts.Count);
    }

    [Fact]
    public void ChartDataResolver_UsesCachedValuesOfFormulas()
    {
        var wb = Read(ChartPackage());
        var ws = wb.Sheets[0];
        var data = ChartDataResolver.Resolve(wb, ws.Charts[0]);
        Assert.Equal(["Jan", "Feb", "Mar"], data.Categories);
        Assert.Equal(["North", "South"], data.Series.Select(s => s.Name));
        Assert.Equal([10.0, 20, 30], data.Series[0].Values);
        Assert.Equal([20.0, 40, 60], data.Series[1].Values);

        var pie = ChartDataResolver.Resolve(wb, ws.Charts[1]);
        Assert.Equal("Share", pie.Series[0].Name);
        Assert.Equal([5.0, 6, 7], pie.Series[0].Values);

        wb.Execute(new SetValueCommand(ws, R("B1"), CellValue.Text("Nord")));
        wb.Execute(new SetValueCommand(ws, R("B3"), CellValue.Number(25)));
        var edited = ChartDataResolver.Resolve(wb, ws.Charts[0]);
        Assert.Equal(("Nord", 25.0), (edited.Series[0].Name, edited.Series[0].Values[1]));
    }

    [Fact]
    public void UnsupportedChart_IsOther_PreservedUnchanged()
    {
        var package = ChartPackage(chart2: RadarChart(0), chartEx: true);
        var wb = Read(package);
        var ws = wb.Sheets[0];
        Assert.False(wb.HasUnshiftedReferences);
        Assert.Equal([ChartKind.Column, ChartKind.Other, ChartKind.Other], ws.Charts.Select(c => c.Kind));
        Assert.Empty(ws.Charts[1].Series);
        Assert.Equal(R("B20"), ws.Charts[2].From);
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new UpdateChartCommand(ws, ws.Charts[1].Id, ChartKind.Column, "x", true)));
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new MoveChartCommand(ws, ws.Charts[2].Id, R("A1"), R("C3"))));
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new DeleteChartCommand(ws, ws.Charts[2].Id)));

        wb.Execute(new SetValueCommand(ws, R("Z9"), CellValue.Number(1)));
        var edited = Write(wb);
        foreach (var same in (string[])["xl/charts/chart1.xml", "xl/charts/chart2.xml", "xl/charts/chartEx1.xml", "xl/drawings/drawing1.xml"])
            Assert.Equal(Entry(package, same), Entry(edited, same));

        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        Assert.False(wb.HasUnshiftedReferences);
        var shifted = Write(wb);
        Assert.True(XNode.DeepEquals(XDocument.Parse(RadarChart(1)).Root, Xml(shifted, "xl/charts/chart2.xml").Root));
        Assert.True(XNode.DeepEquals(XDocument.Parse(ChartEx(1)).Root, Xml(shifted, "xl/charts/chartEx1.xml").Root));
        Assert.True(XNode.DeepEquals(XDocument.Parse(Drawing(1, true)).Root, Xml(shifted, "xl/drawings/drawing1.xml").Root));

        wb.Execute(new MoveChartCommand(ws, ws.Charts[1].Id, R("A30"), R("F40")));
        wb.Execute(new DeleteChartCommand(ws, ws.Charts[1].Id));
        var deleted = Write(wb);
        Assert.DoesNotContain("xl/charts/chart2.xml", Entries(deleted));
        Assert.Contains("xl/charts/chartEx1.xml", Entries(deleted));
        Assert.Equal([ChartKind.Column, ChartKind.Other], ws.Charts.Select(c => c.Kind));
    }

    [Fact]
    public void Pictures_InDrawing_Preserved()
    {
        var package = ChartPackage();
        var wb = Read(package);
        var ws = wb.Sheets[0];
        var picture = Xml(package, "xl/drawings/drawing1.xml").Root!.Elements().ElementAt(1);

        wb.Execute(new MoveChartCommand(ws, ws.Charts[0].Id, R("B30"), R("H45"), (0, 0), (5, 6)));
        var moved = Write(wb);
        var drawing = Xml(moved, "xl/drawings/drawing1.xml").Root!;
        Assert.True(XNode.DeepEquals(picture, drawing.Elements().ElementAt(1)));
        var from = drawing.Elements().First().Element(Xdr + "from")!;
        Assert.Equal(("1", "0", "29", "0"), (from.Element(Xdr + "col")!.Value, from.Element(Xdr + "colOff")!.Value, from.Element(Xdr + "row")!.Value, from.Element(Xdr + "rowOff")!.Value));
        Assert.Equal("47625", drawing.Elements().First().Element(Xdr + "to")!.Element(Xdr + "colOff")!.Value);
        Assert.Equal(Entry(package, "xl/media/image1.png"), Entry(moved, "xl/media/image1.png"));
        Assert.Equal(Entry(package, "xl/charts/chart1.xml"), Entry(moved, "xl/charts/chart1.xml"));
        Assert.Equal(Entry(package, "xl/drawings/_rels/drawing1.xml.rels"), Entry(moved, "xl/drawings/_rels/drawing1.xml.rels"));

        wb.Execute(new InsertRowsCommand(ws, 0, 2));
        var shiftedPicture = Xml(Write(wb), "xl/drawings/drawing1.xml").Root!.Elements().ElementAt(1);
        Assert.Equal(("3", "12"), (shiftedPicture.Element(Xdr + "from")!.Element(Xdr + "row")!.Value, shiftedPicture.Element(Xdr + "to")!.Element(Xdr + "row")!.Value));

        wb.Execute(new DeleteChartCommand(ws, ws.Charts[0].Id));
        wb.Execute(new DeleteChartCommand(ws, ws.Charts[0].Id));
        Assert.Empty(ws.Charts);
        var deleted = Write(wb);
        var names = Entries(deleted);
        Assert.DoesNotContain("xl/charts/chart1.xml", names);
        Assert.DoesNotContain("xl/charts/chart2.xml", names);
        Assert.DoesNotContain("xl/charts/style1.xml", names);
        Assert.Contains("xl/media/image1.png", names);
        var left = Xml(deleted, "xl/drawings/drawing1.xml").Root!.Elements().ToList();
        Assert.Equal("pic", Assert.Single(left).Elements().ElementAt(2).Name.LocalName);
        var rels = Xml(deleted, "xl/drawings/_rels/drawing1.xml.rels").Root!.Elements().ToList();
        Assert.Equal(Rel + "/image", (string)Assert.Single(rels).Attribute("Type")!);
        var types = Xml(deleted, "[Content_Types].xml").Root!.Elements(Types + "Override").Select(e => (string)e.Attribute("PartName")!).ToList();
        Assert.DoesNotContain("/xl/charts/chart1.xml", types);
        Assert.Contains("/xl/drawings/drawing1.xml", types);
        Assert.NotNull(Xml(deleted, "xl/worksheets/sheet1.xml").Root!.Element(M + "drawing"));

        var back = Read(deleted);
        Assert.Empty(back.Sheets[0].Charts);
        back.Execute(new InsertChartCommand(back.Sheets[0], CellRange.Parse("A2:C5"), ChartKind.Pie));
        var again = Xml(Write(back), "xl/drawings/drawing1.xml").Root!.Elements().ToList();
        Assert.Equal(2, again.Count);
        Assert.Equal("pic", again[0].Elements().ElementAt(2).Name.LocalName);
    }

    [Fact]
    public void UpdateChart_PatchesExistingChartInPlace()
    {
        var package = ChartPackage();
        var wb = Read(package);
        var ws = wb.Sheets[0];
        var chart = ws.Charts[0];
        var series = new[] { chart.Series[0] with { Color = "#ff0000" }, chart.Series[1] with { ValuesRef = "Sheet1!$C$2:$C$3" }, new ChartSeries(null, "Extra", null, "Sheet1!$B$2:$B$4", null) };
        wb.Execute(new UpdateChartCommand(ws, chart.Id, ChartKind.Column, "Revenue", false, series, true));
        var written = Write(wb);
        Assert.Equal(Entry(package, "xl/drawings/drawing1.xml"), Entry(written, "xl/drawings/drawing1.xml"));
        var root = Xml(written, "xl/charts/chart1.xml").Root!;
        var space = root.Element(C + "chart")!;
        Assert.Equal(["title", "autoTitleDeleted", "plotArea", "plotVisOnly", "dispBlanksAs"], space.Elements().Select(e => e.Name.LocalName));
        var run = space.Element(C + "title")!.Descendants(XName.Get("r", DrawingNs)).Single();
        Assert.Equal(("Revenue", "1"), (run.Element(XName.Get("t", DrawingNs))!.Value, (string)run.Element(XName.Get("rPr", DrawingNs))!.Attribute("b")!));
        var bar = space.Element(C + "plotArea")!.Element(C + "barChart")!;
        Assert.Equal(["barDir", "grouping", "varyColors", "ser", "ser", "ser", "dLbls", "gapWidth", "overlap", "axId", "axId"], bar.Elements().Select(e => e.Name.LocalName));
        Assert.Equal(("stacked", "100"), ((string)bar.Element(C + "grouping")!.Attribute("val")!, (string)bar.Element(C + "overlap")!.Attribute("val")!));
        var sers = bar.Elements(C + "ser").ToList();
        Assert.Equal("FF0000", (string)sers[0].Descendants(XName.Get("srgbClr", DrawingNs)).Single().Attribute("val")!);
        Assert.NotNull(sers[0].Element(C + "extLst"));
        Assert.Equal("Sheet1!$C$2:$C$3", sers[1].Element(C + "val")!.Descendants(C + "f").Single().Value);
        Assert.Equal(("2", "Extra"), ((string)sers[2].Element(C + "idx")!.Attribute("val")!, sers[2].Element(C + "tx")!.Value));
        Assert.True(XNode.DeepEquals(XDocument.Parse(ColumnChart(0)).Root!.Element(C + "txPr"), root.Element(C + "txPr")));
        Assert.True(XNode.DeepEquals(XDocument.Parse(ColumnChart(0)).Root!.Descendants(C + "catAx").Single(), root.Descendants(C + "catAx").Single()));

        var back = Read(written).Sheets[0].Charts[0];
        Assert.Equal((ChartKind.Column, "Revenue", true, false, 3), (back.Kind, back.Title, back.Stacked, back.ShowLegend, back.Series.Count));
        Assert.Equal("#FF0000", back.Series[0].Color);

        wb.Execute(new UpdateChartCommand(ws, chart.Id, ChartKind.Line, null, true));
        var line = Xml(Write(wb), "xl/charts/chart1.xml").Root!;
        Assert.Equal(["autoTitleDeleted", "plotArea", "legend", "plotVisOnly", "dispBlanksAs"], line.Element(C + "chart")!.Elements().Select(e => e.Name.LocalName));
        Assert.Equal("1", (string)line.Descendants(C + "autoTitleDeleted").Single().Attribute("val")!);
        Assert.Equal(["111", "222"], line.Descendants(C + "lineChart").Single().Elements(C + "axId").Select(a => (string)a.Attribute("val")!));
        Assert.True(XNode.DeepEquals(XDocument.Parse(ColumnChart(0)).Root!.Descendants(C + "valAx").Single(), line.Descendants(C + "valAx").Single()));

        wb.Execute(new UpdateChartCommand(ws, chart.Id, ChartKind.Pie, "Pie", true));
        var pie = Xml(Write(wb), "xl/charts/chart1.xml").Root!.Descendants(C + "plotArea").Single();
        Assert.Equal(["layout", "pieChart"], pie.Elements().Select(e => e.Name.LocalName));
        Assert.Equal([ChartKind.Pie], Read(Write(wb)).Sheets[0].Charts.Take(1).Select(c => c.Kind));

        wb.Undo.Undo();
        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(Entry(package, "xl/charts/chart1.xml"), Entry(Write(wb), "xl/charts/chart1.xml"));
    }

    private static void Fill(Workbook wb, Worksheet ws, string start, params object?[][] rows)
    {
        var at = R(start);
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                if (rows[r][c] is { } v)
                    wb.Execute(new SetValueCommand(ws, new CellRef(at.Row + r, at.Column + c), v is string s ? CellValue.Text(s) : CellValue.Number(Convert.ToDouble(v))));
    }

    private static string Marker(string name, int col, long colOff, int row, long rowOff) =>
        $"<xdr:{name}><xdr:col>{col}</xdr:col><xdr:colOff>{colOff}</xdr:colOff><xdr:row>{row}</xdr:row><xdr:rowOff>{rowOff}</xdr:rowOff></xdr:{name}>";

    private static string Frame(int id, string name, string chart) =>
        $"<xdr:graphicFrame macro=\"\"><xdr:nvGraphicFramePr><xdr:cNvPr id=\"{id}\" name=\"{name}\"/><xdr:cNvGraphicFramePr/></xdr:nvGraphicFramePr>"
        + "<xdr:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/></xdr:xfrm><a:graphic>" + chart + "</a:graphic></xdr:graphicFrame>";

    private static string Drawing(int rows, bool chartEx = false) =>
        $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><xdr:wsDr xmlns:xdr=\"{XdrNs}\" xmlns:a=\"{DrawingNs}\" xmlns:r=\"{Rel}\" xmlns:mc=\"{McNs}\">"
        + "<xdr:twoCellAnchor>" + Marker("from", 4, 9525, 1 + rows, 19050) + Marker("to", 11, 0, 16 + rows, 0)
        + Frame(2, "Chart 1", $"<a:graphicData uri=\"{ChartNs}\"><c:chart xmlns:c=\"{ChartNs}\" r:id=\"rId1\"/></a:graphicData>") + "<xdr:clientData/></xdr:twoCellAnchor>"
        + "<xdr:twoCellAnchor editAs=\"oneCell\">" + Marker("from", 1, 0, 1 + rows, 0) + Marker("to", 3, 0, 10 + rows, 0)
        + "<xdr:pic><xdr:nvPicPr><xdr:cNvPr id=\"3\" name=\"Picture 2\" descr=\"logo\"/><xdr:cNvPicPr><a:picLocks noChangeAspect=\"1\"/></xdr:cNvPicPr></xdr:nvPicPr>"
        + "<xdr:blipFill><a:blip r:embed=\"rId3\"/><a:stretch><a:fillRect/></a:stretch></xdr:blipFill><xdr:spPr><a:xfrm><a:off x=\"1\" y=\"2\"/><a:ext cx=\"3\" cy=\"4\"/></a:xfrm>"
        + "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></xdr:spPr></xdr:pic><xdr:clientData/></xdr:twoCellAnchor>"
        + "<xdr:twoCellAnchor>" + Marker("from", 13, 0, 1 + rows, 0) + Marker("to", 18, 0, 11 + rows, 0)
        + Frame(4, "Chart 2", $"<a:graphicData uri=\"{ChartNs}\"><c:chart xmlns:c=\"{ChartNs}\" r:id=\"rId2\"/></a:graphicData>") + "<xdr:clientData/></xdr:twoCellAnchor>"
        + (chartEx
            ? "<mc:AlternateContent><mc:Choice xmlns:cx1=\"http://schemas.microsoft.com/office/drawing/2015/9/8/chartex\" Requires=\"cx1\"><xdr:twoCellAnchor>"
              + Marker("from", 1, 0, 19 + rows, 0) + Marker("to", 6, 0, 30 + rows, 0)
              + Frame(5, "Chart 3", $"<a:graphicData uri=\"{CxNs}\"><cx:chart xmlns:cx=\"{CxNs}\" r:id=\"rId4\"/></a:graphicData>")
              + "<xdr:clientData/></xdr:twoCellAnchor></mc:Choice><mc:Fallback><xdr:twoCellAnchor>" + Marker("from", 1, 0, 19 + rows, 0) + Marker("to", 6, 0, 30 + rows, 0)
              + "<xdr:sp macro=\"\" textlink=\"\"><xdr:nvSpPr><xdr:cNvPr id=\"0\" name=\"\"/><xdr:cNvSpPr><a:spLocks noTextEdit=\"1\"/></xdr:cNvSpPr></xdr:nvSpPr>"
              + "<xdr:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"1\" cy=\"1\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></xdr:spPr></xdr:sp>"
              + "<xdr:clientData/></xdr:twoCellAnchor></mc:Fallback></mc:AlternateContent>"
            : "")
        + "</xdr:wsDr>";

    private static string Space(string chart, string extra = "") =>
        $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><c:chartSpace xmlns:c=\"{ChartNs}\" xmlns:a=\"{DrawingNs}\" xmlns:r=\"{Rel}\" "
        + "xmlns:c16r2=\"http://schemas.microsoft.com/office/drawing/2015/06/chart\"><c:date1904 val=\"0\"/><c:lang val=\"en-US\"/><c:roundedCorners val=\"0\"/>"
        + $"<c:chart>{chart}</c:chart><c:spPr><a:solidFill><a:schemeClr val=\"bg1\"/></a:solidFill></c:spPr>{extra}<c:printSettings><c:headerFooter/>"
        + "<c:pageMargins b=\"0.75\" l=\"0.7\" r=\"0.7\" t=\"0.75\" header=\"0.3\" footer=\"0.3\"/><c:pageSetup/></c:printSettings></c:chartSpace>";

    private static string StrRef(string f, params string[] values) =>
        $"<c:strRef><c:f>{f}</c:f><c:strCache><c:ptCount val=\"{values.Length}\"/>{string.Concat(values.Select((v, i) => $"<c:pt idx=\"{i}\"><c:v>{v}</c:v></c:pt>"))}</c:strCache></c:strRef>";

    private static string NumRef(string f, params int[] values) =>
        $"<c:numRef><c:f>{f}</c:f><c:numCache><c:formatCode>General</c:formatCode><c:ptCount val=\"{values.Length}\"/>"
        + string.Concat(values.Select((v, i) => $"<c:pt idx=\"{i}\"><c:v>{v}</c:v></c:pt>")) + "</c:numCache></c:numRef>";

    private static string Axes =>
        "<c:catAx><c:axId val=\"111\"/><c:scaling><c:orientation val=\"minMax\"/></c:scaling><c:delete val=\"0\"/><c:axPos val=\"b\"/><c:numFmt formatCode=\"General\" sourceLinked=\"1\"/>"
        + "<c:majorTickMark val=\"none\"/><c:minorTickMark val=\"none\"/><c:tickLblPos val=\"nextTo\"/><c:crossAx val=\"222\"/><c:crosses val=\"autoZero\"/><c:auto val=\"1\"/>"
        + "<c:lblAlgn val=\"ctr\"/><c:lblOffset val=\"100\"/><c:noMultiLvlLbl val=\"0\"/></c:catAx><c:valAx><c:axId val=\"222\"/><c:scaling><c:orientation val=\"minMax\"/></c:scaling>"
        + "<c:delete val=\"0\"/><c:axPos val=\"l\"/><c:majorGridlines/><c:numFmt formatCode=\"General\" sourceLinked=\"1\"/><c:majorTickMark val=\"none\"/><c:minorTickMark val=\"none\"/>"
        + "<c:tickLblPos val=\"nextTo\"/><c:crossAx val=\"111\"/><c:crosses val=\"autoZero\"/><c:crossBetween val=\"between\"/></c:valAx>";

    private static string Title(string text) =>
        $"<c:title><c:tx><c:rich><a:bodyPr rot=\"0\"/><a:lstStyle/><a:p><a:pPr><a:defRPr sz=\"1400\"/></a:pPr><a:r><a:rPr lang=\"en-US\" b=\"1\"/><a:t>{text}</a:t></a:r></a:p></c:rich></c:tx>"
        + "<c:overlay val=\"0\"/></c:title><c:autoTitleDeleted val=\"0\"/>";

    private static string ColumnChart(int rows, string sheet = "Sheet1") =>
        Space(Title("Sales") + "<c:plotArea><c:layout/><c:barChart><c:barDir val=\"col\"/><c:grouping val=\"clustered\"/><c:varyColors val=\"0\"/>"
            + $"<c:ser><c:idx val=\"0\"/><c:order val=\"0\"/><c:tx>{StrRef($"{sheet}!$B${1 + rows}", "North")}</c:tx><c:spPr><a:solidFill><a:srgbClr val=\"4472C4\"/></a:solidFill></c:spPr>"
            + $"<c:invertIfNegative val=\"0\"/><c:cat>{StrRef($"{sheet}!$A${2 + rows}:$A${4 + rows}", "Jan", "Feb", "Mar")}</c:cat><c:val>{NumRef($"{sheet}!$B${2 + rows}:$B${4 + rows}", 10, 20, 30)}</c:val>"
            + "<c:extLst><c:ext uri=\"{C3380CC4-5D6E-409C-BE32-E72D297353CC}\"><c16:uniqueId xmlns:c16=\"http://schemas.microsoft.com/office/drawing/2014/chart\" val=\"{0001}\"/></c:ext></c:extLst></c:ser>"
            + $"<c:ser><c:idx val=\"1\"/><c:order val=\"1\"/><c:tx>{StrRef($"{sheet}!$C${1 + rows}", "South")}</c:tx><c:spPr><a:solidFill><a:schemeClr val=\"accent2\"/></a:solidFill></c:spPr>"
            + $"<c:invertIfNegative val=\"0\"/><c:cat>{StrRef($"{sheet}!$A${2 + rows}:$A${4 + rows}", "Jan", "Feb", "Mar")}</c:cat><c:val>{NumRef($"{sheet}!$C${2 + rows}:$C${4 + rows}", 20, 40, 60)}</c:val></c:ser>"
            + "<c:dLbls><c:showLegendKey val=\"0\"/><c:showVal val=\"0\"/><c:showCatName val=\"0\"/><c:showSerName val=\"0\"/><c:showPercent val=\"0\"/><c:showBubbleSize val=\"0\"/></c:dLbls>"
            + "<c:gapWidth val=\"219\"/><c:overlap val=\"-27\"/><c:axId val=\"111\"/><c:axId val=\"222\"/></c:barChart>" + Axes + "</c:plotArea>"
            + "<c:legend><c:legendPos val=\"b\"/><c:overlay val=\"0\"/></c:legend><c:plotVisOnly val=\"1\"/><c:dispBlanksAs val=\"gap\"/>",
            "<c:txPr><a:bodyPr/><a:lstStyle/><a:p><a:pPr><a:defRPr/></a:pPr><a:endParaRPr lang=\"en-US\"/></a:p></c:txPr>");

    private static string PieChart(int rows, string sheet = "Sheet1") =>
        Space("<c:autoTitleDeleted val=\"1\"/><c:plotArea><c:layout/><c:pieChart><c:varyColors val=\"1\"/><c:ser><c:idx val=\"0\"/><c:order val=\"0\"/><c:tx><c:v>Share</c:v></c:tx>"
              + $"<c:cat>{StrRef($"{sheet}!$A${2 + rows}:$A${4 + rows}", "Jan", "Feb", "Mar")}</c:cat><c:val>{NumRef("Missing!$B$2:$B$4", 5, 6, 7)}</c:val></c:ser>"
              + "<c:firstSliceAng val=\"0\"/></c:pieChart></c:plotArea><c:plotVisOnly val=\"1\"/>");

    private static string BarChart(bool stacked) =>
        Space(Title("Bars") + $"<c:plotArea><c:layout/><c:barChart><c:barDir val=\"bar\"/><c:grouping val=\"{(stacked ? "stacked" : "clustered")}\"/><c:varyColors val=\"0\"/>"
              + $"<c:ser><c:idx val=\"0\"/><c:order val=\"0\"/><c:val>{NumRef("Sheet1!$B$2:$B$4", 10, 20, 30)}</c:val></c:ser><c:gapWidth val=\"150\"/><c:overlap val=\"100\"/>"
              + "<c:axId val=\"111\"/><c:axId val=\"222\"/></c:barChart>" + Axes + "</c:plotArea><c:plotVisOnly val=\"1\"/>");

    private static string RadarChart(int rows) =>
        Space("<c:autoTitleDeleted val=\"1\"/><c:plotArea><c:layout/><c:radarChart><c:radarStyle val=\"marker\"/><c:varyColors val=\"0\"/>"
              + $"<c:ser><c:idx val=\"0\"/><c:order val=\"0\"/><c:tx>{StrRef($"Sheet1!$B${1 + rows}", "North")}</c:tx><c:cat>{StrRef($"Sheet1!$A${2 + rows}:$A${4 + rows}", "Jan", "Feb", "Mar")}</c:cat>"
              + $"<c:val>{NumRef($"Sheet1!$B${2 + rows}:$B${4 + rows}", 10, 20, 30)}</c:val></c:ser><c:axId val=\"111\"/><c:axId val=\"222\"/></c:radarChart>" + Axes + "</c:plotArea>"
              + "<c:legend><c:legendPos val=\"r\"/><c:overlay val=\"0\"/></c:legend><c:plotVisOnly val=\"1\"/>");

    private static string ChartEx(int rows) =>
        $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><cx:chartSpace xmlns:a=\"{DrawingNs}\" xmlns:r=\"{Rel}\" xmlns:cx=\"{CxNs}\"><cx:chartData><cx:data id=\"0\">"
        + $"<cx:strDim type=\"cat\"><cx:f>Sheet1!$A${2 + rows}:$A${4 + rows}</cx:f><cx:lvl ptCount=\"3\"><cx:pt idx=\"0\">Jan</cx:pt><cx:pt idx=\"1\">Feb</cx:pt><cx:pt idx=\"2\">Mar</cx:pt></cx:lvl></cx:strDim>"
        + $"<cx:numDim type=\"val\"><cx:f>Sheet1!$B${2 + rows}:$B${4 + rows}</cx:f><cx:lvl ptCount=\"3\" formatCode=\"General\"><cx:pt idx=\"0\">10</cx:pt><cx:pt idx=\"1\">20</cx:pt><cx:pt idx=\"2\">30</cx:pt></cx:lvl></cx:numDim>"
        + "</cx:data></cx:chartData><cx:chart><cx:plotArea><cx:plotAreaRegion><cx:series layoutId=\"waterfall\" uniqueId=\"{1}\"><cx:dataId val=\"0\"/></cx:series></cx:plotAreaRegion></cx:plotArea></cx:chart></cx:chartSpace>";

    private static byte[] ChartPackage(string? chart2 = null, bool chartEx = false)
    {
        string Cell(string r, string body, string type = "") => $"<c r=\"{r}\"{type}>{body}</c>";
        var rows = "<row r=\"1\">" + Cell("A1", "<v>0</v>", " t=\"s\"") + Cell("B1", "<v>1</v>", " t=\"s\"") + Cell("C1", "<v>2</v>", " t=\"s\"") + "</row>";
        for (var i = 0; i < 3; i++)
            rows += $"<row r=\"{i + 2}\">" + Cell($"A{i + 2}", $"<v>{3 + i}</v>", " t=\"s\"") + Cell($"B{i + 2}", $"<v>{10 * (i + 1)}</v>")
                    + Cell($"C{i + 2}", $"<f>B{i + 2}*2</f><v>{20 * (i + 1)}</v>") + "</row>";
        var sheet = $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"{Main}\" xmlns:r=\"{Rel}\"><sheetData>{rows}</sheetData>"
                    + "<pageMargins left=\"0.7\" right=\"0.7\" top=\"0.75\" bottom=\"0.75\" header=\"0.3\" footer=\"0.3\"/><drawing r:id=\"rId1\"/></worksheet>";
        string Rels(params (string Id, string Type, string Target)[] rels) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
            + string.Concat(rels.Select(r => $"<Relationship Id=\"{r.Id}\" Type=\"{(r.Type.Contains("://") ? r.Type : Rel + "/" + r.Type)}\" Target=\"{r.Target}\"/>")) + "</Relationships>";
        var drawingRels = new List<(string, string, string)> { ("rId1", "chart", "../charts/chart1.xml"), ("rId2", "chart", "../charts/chart2.xml"), ("rId3", "image", "../media/image1.png") };
        if (chartEx) drawingRels.Add(("rId4", "http://schemas.microsoft.com/office/2014/relationships/chartEx", "../charts/chartEx1.xml"));
        var types = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
                    + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/>"
                    + "<Default Extension=\"png\" ContentType=\"image/png\"/>"
                    + "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>"
                    + "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"
                    + "<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>"
                    + $"<Override PartName=\"/xl/drawings/drawing1.xml\" ContentType=\"{DrawingType}\"/><Override PartName=\"/xl/charts/chart1.xml\" ContentType=\"{ChartType}\"/>"
                    + $"<Override PartName=\"/xl/charts/chart2.xml\" ContentType=\"{ChartType}\"/><Override PartName=\"/xl/charts/style1.xml\" ContentType=\"application/vnd.ms-office.chartstyle+xml\"/>"
                    + (chartEx ? "<Override PartName=\"/xl/charts/chartEx1.xml\" ContentType=\"application/vnd.ms-office.chartex+xml\"/>" : "")
                    + "</Types>";
        var entries = new List<(string, byte[])>
        {
            ("[Content_Types].xml", Encoding.UTF8.GetBytes(types)),
            ("_rels/.rels", Encoding.UTF8.GetBytes(Rels(("rId1", "officeDocument", "xl/workbook.xml")))),
            ("xl/workbook.xml", Encoding.UTF8.GetBytes($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"{Main}\" xmlns:r=\"{Rel}\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>")),
            ("xl/_rels/workbook.xml.rels", Encoding.UTF8.GetBytes(Rels(("rId1", "worksheet", "worksheets/sheet1.xml"), ("rId2", "sharedStrings", "sharedStrings.xml")))),
            ("xl/sharedStrings.xml", Encoding.UTF8.GetBytes($"<sst xmlns=\"{Main}\">{string.Concat(new[] { "Month", "North", "South", "Jan", "Feb", "Mar" }.Select(s => $"<si><t>{s}</t></si>"))}</sst>")),
            ("xl/worksheets/sheet1.xml", Encoding.UTF8.GetBytes(sheet)),
            ("xl/worksheets/_rels/sheet1.xml.rels", Encoding.UTF8.GetBytes(Rels(("rId1", "drawing", "../drawings/drawing1.xml")))),
            ("xl/drawings/drawing1.xml", Encoding.UTF8.GetBytes(Drawing(0, chartEx))),
            ("xl/drawings/_rels/drawing1.xml.rels", Encoding.UTF8.GetBytes(Rels([.. drawingRels]))),
            ("xl/charts/chart1.xml", Encoding.UTF8.GetBytes(ColumnChart(0))),
            ("xl/charts/_rels/chart1.xml.rels", Encoding.UTF8.GetBytes(Rels(("rId1", "http://schemas.microsoft.com/office/2011/relationships/chartStyle", "style1.xml")))),
            ("xl/charts/style1.xml", Encoding.UTF8.GetBytes("<cs:chartStyle xmlns:cs=\"http://schemas.microsoft.com/office/drawing/2012/chartStyle\" id=\"201\"/>")),
            ("xl/charts/chart2.xml", Encoding.UTF8.GetBytes(chart2 ?? PieChart(0))),
            ("xl/media/image1.png", [137, 80, 78, 71, 1, 2, 3])
        };
        if (chartEx) entries.Add(("xl/charts/chartEx1.xml", Encoding.UTF8.GetBytes(ChartEx(0))));
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            foreach (var (name, content) in entries)
            {
                using var s = zip.CreateEntry(name).Open();
                s.Write(content);
            }

        return ms.ToArray();
    }

    private static Workbook Read(byte[] package) => XlsxReader.Read(new MemoryStream(package));

    private static byte[] Write(Workbook wb)
    {
        var ms = new MemoryStream();
        XlsxWriter.Write(wb, ms);
        return ms.ToArray();
    }

    private static List<string> Entries(byte[] zip)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        return archive.Entries.Select(e => e.FullName).ToList();
    }

    private static byte[] Entry(byte[] zip, string name)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        using var s = archive.GetEntry(name)!.Open();
        var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    private static XDocument Xml(byte[] zip, string name) => XDocument.Load(new MemoryStream(Entry(zip, name)));
}
