using System.IO.Compression;
using System.Xml.Linq;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class IntegrationRoundTripTests
{
    private const string Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string DrawingNs = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace Pkg = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Types = "http://schemas.openxmlformats.org/package/2006/content-types";

    private static CellRef R(string a1) => CellRef.Parse(a1);

    [Fact]
    public void AllFeatures_InsertRowAbove_EverythingShifted_HasUnshiftedFalse()
    {
        var package = AllFeatures();
        var wb = Read(package);
        Assert.False(wb.HasUnshiftedReferences);
        var ws = wb.Sheets[0];
        Assert.Equal(Snapshot(0, "Data"), Snapshot(wb));

        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        var shifted = Write(wb);
        var back = Read(shifted);
        Assert.False(back.HasUnshiftedReferences);
        Assert.Equal(Snapshot(1, "Data"), Snapshot(back));
        Assert.Equal((20, 25), PictureRows(shifted));
        Assert.Equal(["11"], Breaks(shifted, "rowBreaks"));
        Assert.Equal("H3:H4", IgnoredErrors(shifted));

        back.Execute(new DeleteRowsCommand(back.Sheets[0], 0, 1));
        var deleted = Write(back);
        var restored = Read(deleted);
        Assert.False(restored.HasUnshiftedReferences);
        Assert.Equal(Snapshot(0, "Data"), Snapshot(restored));
        Assert.Equal((19, 24), PictureRows(deleted));
        Assert.Equal(["10"], Breaks(deleted, "rowBreaks"));
        Assert.Equal("H2:H3", IgnoredErrors(deleted));

        restored.Execute(new RenameSheetCommand(restored.Sheets[0], "My Data"));
        var renamed = Read(Write(restored));
        Assert.Equal(Snapshot(0, "'My Data'"), Snapshot(renamed));
    }

    [Fact]
    public void ControlCharacters_InFilterNotesLinksRulesAndCharts_StayWellFormedAndRoundTrip()
    {
        const string odd = "ab_x0041_";
        var wb = Workbook.Create("Data");
        var ws = wb.Sheets[0];
        object[][] rows = [["Name", "Qty"], [odd, 1], ["x", 2]];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < 2; c++)
                wb.Execute(new SetValueCommand(ws, new CellRef(r, c), rows[r][c] is string s ? CellValue.Text(s) : CellValue.Number(Convert.ToDouble(rows[r][c]))));
        wb.Execute(new SetAutoFilterCommand(ws, CellRange.Parse("A1:B3")));
        wb.Execute(new ApplyFilterCommand(ws, 0, FilterCriteria.Values(new HashSet<string> { odd })));
        wb.Execute(new SetNoteCommand(ws, new CellNote(R("A2"), odd, odd)));
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("B2"), "https://example.com/", null, odd, odd)));
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([CellRange.Parse("A2:A3")],
            [new ConditionalRule.ContainsText(ContainsTextKind.Contains, odd, new DifferentialStyle(NumberFormat: "0.0"))])));
        wb.Execute(new AddDataValidationCommand(ws, new DataValidation([CellRange.Parse("B2:B3")], ValidationType.Any, PromptTitle: "t", Prompt: odd,
            ErrorTitle: odd, Error: odd)));
        wb.Execute(new InsertChartCommand(ws, CellRange.Parse("A1:B3"), ChartKind.Column));
        var package = Write(wb);

        using (var zip = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read))
            foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".xml", StringComparison.Ordinal)))
            {
                using var reader = System.Xml.XmlReader.Create(entry.Open());
                while (reader.Read())
                {
                }
            }

        var back = Read(package).Sheets[0];
        Assert.Equal((odd, odd), (back.Notes.Single().Author, back.Notes.Single().Text));
        Assert.Equal((odd, odd), (back.Links.Single().Display, back.Links.Single().Tooltip));
        Assert.Equal([odd], back.AutoFilter!.Columns[0].Shown);
        Assert.Equal(odd, ((ConditionalRule.ContainsText)back.ConditionalFormats.Single().Rules.Single()).Text);
        var dv = back.DataValidations.Single();
        Assert.Equal(("t", odd, odd, odd), (dv.PromptTitle, dv.Prompt, dv.ErrorTitle, dv.Error));
    }

    [Fact]
    public void PivotFile_StillFlagsUnshifted()
    {
        var plain = XlsxReadTests.Package([XlsxReadTests.Sheet("")]);
        Assert.False(Read(plain).HasUnshiftedReferences);
        foreach (var (part, type) in (ReadOnlySpan<(string, string)>)
                 [
                     ("xl/pivotTables/pivotTable1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.pivotTable+xml"),
                     ("xl/pivotCache/pivotCacheDefinition1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.pivotCacheDefinition+xml"),
                     ("xl/externalLinks/externalLink1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.externalLink+xml"),
                     ("xl/queryTables/queryTable1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.queryTable+xml"),
                     ("xl/slicers/slicer1.xml", "application/vnd.ms-excel.slicer+xml"),
                     ("xl/slicerCaches/slicerCache1.xml", "application/vnd.ms-excel.slicerCache+xml"),
                     ("xl/timelines/timeline1.xml", "application/vnd.ms-excel.timeline+xml"),
                     ("xl/timelineCaches/timelineCache1.xml", "application/vnd.ms-excel.timelineCache+xml")
                 ])
            Assert.True(Read(WithPart(plain, part, type)).HasUnshiftedReferences, part);

        foreach (var child in (string[])["<protectedRanges><protectedRange sqref=\"A1:B2\" name=\"p\"/></protectedRanges>",
                     "<scenarios><scenario name=\"s\" count=\"1\"><inputCells r=\"A1\" val=\"1\"/></scenario></scenarios>",
                     "<dataConsolidate><dataRefs count=\"1\"><dataRef ref=\"A1:B2\" sheet=\"Sheet1\"/></dataRefs></dataConsolidate>",
                     "<extLst><ext uri=\"{78C0D931-6437-407d-A8EE-F0AAD7539E65}\"/></extLst>"])
            Assert.True(Read(XlsxReadTests.Package([XlsxReadTests.Sheet("", after: child)])).HasUnshiftedReferences, child);

        foreach (var child in (string[])["<rowBreaks count=\"1\" manualBreakCount=\"1\"><brk id=\"5\" max=\"16383\" man=\"1\"/></rowBreaks>",
                     "<colBreaks count=\"1\" manualBreakCount=\"1\"><brk id=\"3\" max=\"1048575\" man=\"1\"/></colBreaks>",
                     "<ignoredErrors><ignoredError sqref=\"A1:B2\" numberStoredAsText=\"1\"/></ignoredErrors>"])
            Assert.False(Read(XlsxReadTests.Package([XlsxReadTests.Sheet("", after: child)])).HasUnshiftedReferences, child);
    }

    [Fact]
    public void Breaks_AndIgnoredErrors_FollowStructureChanges()
    {
        var package = XlsxReadTests.Package([XlsxReadTests.Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row>", after:
            "<rowBreaks count=\"3\" manualBreakCount=\"3\"><brk id=\"2\" max=\"16383\" man=\"1\"/><brk id=\"5\" max=\"16383\" man=\"1\"/><brk id=\"9\" max=\"16383\" man=\"1\"/></rowBreaks>"
            + "<colBreaks count=\"1\" manualBreakCount=\"1\"><brk id=\"3\" max=\"1048575\" man=\"1\"/></colBreaks>")]);
        var wb = Read(package);
        var ws = wb.Sheets[0];

        wb.Execute(new InsertRowsCommand(ws, 3, 2));
        Assert.Equal(["2", "7", "11"], Breaks(Write(wb), "rowBreaks"));
        wb.Execute(new DeleteRowsCommand(ws, 6, 2));
        Assert.Equal(["2", "6", "9"], Breaks(Write(wb), "rowBreaks"));
        wb.Execute(new DeleteRowsCommand(ws, 1, 6));
        var xml = XDocument.Load(new MemoryStream(Entry(Write(wb), "xl/worksheets/sheet1.xml"))).Root!;
        var rowBreaks = xml.Elements().Single(e => e.Name.LocalName == "rowBreaks");
        Assert.Equal(["1", "3"], rowBreaks.Elements().Select(b => (string)b.Attribute("id")!));
        Assert.Equal(("2", "2"), ((string)rowBreaks.Attribute("count")!, (string)rowBreaks.Attribute("manualBreakCount")!));
        wb.Execute(new InsertColumnsCommand(ws, 0, 1));
        Assert.Equal(["4"], Breaks(Write(wb), "colBreaks"));

        for (var i = 0; i < 4; i++) wb.Undo.Undo();
        var undone = Write(wb);
        Assert.Equal(["2", "5", "9"], Breaks(undone, "rowBreaks"));
        Assert.Equal(["3"], Breaks(undone, "colBreaks"));
    }

    [Fact]
    public void RoundTrip_TwiceIsStable()
    {
        var first = Write(Read(AllFeatures()));
        var second = Write(Read(first));
        Assert.Equal(Entries(first).Select(e => (e.Name, e.Time)), Entries(second).Select(e => (e.Name, e.Time)));
        foreach (var (name, _) in Entries(first)) Assert.Equal(Entry(first, name), Entry(second, name));
        Assert.Equal(first, second);

        var fresh = Workbook.Create();
        fresh.Execute(new SetValueCommand(fresh.Sheets[0], R("A1"), CellValue.Number(1)));
        Assert.All(Entries(Write(fresh)), e => Assert.Equal(new DateTime(1980, 1, 1), e.Time.DateTime));
    }

    private static object Snapshot(int d, string sheet)
    {
        string A(string a1) => CellRange.Parse(a1) is var r ? CellRange.FromBounds(r.Top + d, r.Left, r.Bottom + d, r.Right).ToA1() : a1;
        string C(string a1) => CellRef.Parse(a1) is var c ? new CellRef(c.Row + d, c.Column).ToA1() : a1;
        return new
        {
            Table = A("A1:C4"),
            Totals = $"SUM(Table1[Qty])",
            Plain = $"SUM({C("B2")}:{C("B4")})",
            Cross = $"{sheet}!{C("B2")}",
            Cf = (A("B2:B4"), C("B2") + ">5"),
            Dv = (A("C2:C4"), A("E2:E4"), $"${C("A2")[..1]}${C("A2")[1..]}:${C("A4")[..1]}${C("A4")[1..]}"),
            Filter = A("E1:F4"),
            Note = C("A2"),
            Links = $"{C("B2")}>{sheet}!{C("H3")} {C("A3")}>https://example.com/",
            Chart = (C("J1"), C("Q15"), $"{sheet}!$B${2 + d}:$B${4 + d}", $"{sheet}!$A${2 + d}:$A${4 + d}")
        };
    }

    private static object Snapshot(Workbook wb)
    {
        var ws = wb.Sheets[0];
        var other = wb.Sheets[1];
        var cf = ws.ConditionalFormats.Single();
        var chart = ws.Charts.Single();
        var series = chart.Series[0];
        var dv = ws.DataValidations;
        return new
        {
            Table = ws.Tables.Single().Range.ToA1(),
            Totals = ws[TotalsCell(ws)]!.Formula,
            Plain = ws[new CellRef(ws.Tables[0].Range.Top + 2, 7)]!.Formula,
            Cross = other[R("A1")]!.Formula,
            Cf = (string.Join(" ", cf.Ranges.Select(r => r.ToA1())), ((ConditionalRule.Expression)cf.Rules.Single()).Formula),
            Dv = (dv[0].Ranges.Single().ToA1(), dv[1].Ranges.Single().ToA1(), dv[1].Formula1),
            Filter = ws.AutoFilter!.Range.ToA1(),
            Note = ws.Notes.Single().Cell.ToA1(),
            Links = string.Join(" ", ws.Links.Select(l => $"{l.Cell.ToA1()}>{l.Location ?? l.Target}")),
            Chart = (chart.From.ToA1(), chart.To.ToA1(), series.ValuesRef, series.CategoriesRef)
        };
    }

    private static CellRef TotalsCell(Worksheet ws) => new(ws.Tables[0].Range.Top + 1, 7);

    private static byte[] AllFeatures()
    {
        var wb = Workbook.Create("Data");
        wb.Execute(new AddSheetCommand("Other"));
        var ws = wb.Sheets[0];
        object[][] rows = [["Item", "Qty", "Price"], ["a", 4, 1.5], ["b", 7, 2], ["c", 9, 3]];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < 3; c++)
                wb.Execute(new SetValueCommand(ws, new CellRef(r, c), rows[r][c] is string s ? CellValue.Text(s) : CellValue.Number(Convert.ToDouble(rows[r][c]))));
        wb.Execute(new CreateTableCommand(ws, CellRange.Parse("A1:C4"), true));
        object[][] scores = [["Name", "Score"], ["x", 1], ["y", 2], ["z", 3]];
        for (var r = 0; r < scores.Length; r++)
            for (var c = 0; c < 2; c++)
                wb.Execute(new SetValueCommand(ws, new CellRef(r, 4 + c), scores[r][c] is string s ? CellValue.Text(s) : CellValue.Number(Convert.ToDouble(scores[r][c]))));
        wb.Execute(new SetAutoFilterCommand(ws, CellRange.Parse("E1:F4")));
        wb.Execute(new SetFormulaCommand(ws, R("H2"), "SUM(Table1[Qty])"));
        wb.Execute(new SetFormulaCommand(ws, R("H3"), "SUM(B2:B4)"));
        wb.Execute(new SetFormulaCommand(wb.Sheets[1], R("A1"), "Data!B2"));
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([CellRange.Parse("B2:B4")],
            [new ConditionalRule.Expression("B2>5", new DifferentialStyle(NumberFormat: "0.0"))])));
        wb.Execute(new AddDataValidationCommand(ws, new DataValidation([CellRange.Parse("C2:C4")], ValidationType.Decimal, "between", "0", "100")));
        wb.Execute(new AddDataValidationCommand(ws, new DataValidation([CellRange.Parse("E2:E4")], ValidationType.List, Formula1: "$A$2:$A$4")));
        wb.Execute(new SetNoteCommand(ws, new CellNote(R("A2"), "Ann", "first item")));
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("B2"), null, "Data!H3", null, null)));
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("A3"), "https://example.com/", null, null, null)));
        wb.Execute(new InsertChartCommand(ws, CellRange.Parse("A1:B4"), ChartKind.Column));
        wb.Execute(new MoveChartCommand(ws, ws.Charts[0].Id, R("J1"), R("Q15")));
        return WithExtras(Write(wb));
    }

    private static byte[] WithExtras(byte[] package)
    {
        var ms = new MemoryStream();
        ms.Write(package);
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, true))
        {
            var drawingEntry = zip.GetEntry("xl/drawings/drawing1.xml")!;
            var drawing = Load(drawingEntry);
            XNamespace xdr = drawing.Root!.Name.Namespace;
            XNamespace a = DrawingNs;
            XElement Marker(string name, int col, int row) => new(xdr + name, new XElement(xdr + "col", col), new XElement(xdr + "colOff", 0), new XElement(xdr + "row", row),
                new XElement(xdr + "rowOff", 0));
            drawing.Root.Add(new XElement(xdr + "twoCellAnchor", new XAttribute("editAs", "oneCell"), Marker("from", 1, 19), Marker("to", 3, 24),
                new XElement(xdr + "pic",
                    new XElement(xdr + "nvPicPr", new XElement(xdr + "cNvPr", new XAttribute("id", 9), new XAttribute("name", "Picture 1")), new XElement(xdr + "cNvPicPr")),
                    new XElement(xdr + "blipFill", new XElement(a + "blip", new XAttribute(XName.Get("embed", Rel), "rIdPic")), new XElement(a + "stretch", new XElement(a + "fillRect"))),
                    new XElement(xdr + "spPr", new XElement(a + "prstGeom", new XAttribute("prst", "rect"), new XElement(a + "avLst")))),
                new XElement(xdr + "clientData")));
            Save(zip, drawingEntry, drawing);

            var relsEntry = zip.GetEntry("xl/drawings/_rels/drawing1.xml.rels")!;
            var rels = Load(relsEntry);
            rels.Root!.Add(new XElement(Pkg + "Relationship", new XAttribute("Id", "rIdPic"), new XAttribute("Type", Rel + "/image"), new XAttribute("Target", "../media/image1.png")));
            Save(zip, relsEntry, rels);

            var typesEntry = zip.GetEntry("[Content_Types].xml")!;
            var types = Load(typesEntry);
            types.Root!.AddFirst(new XElement(Types + "Default", new XAttribute("Extension", "png"), new XAttribute("ContentType", "image/png")));
            Save(zip, typesEntry, types);

            var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml")!;
            var sheet = Load(sheetEntry);
            XNamespace m = sheet.Root!.Name.Namespace;
            sheet.Root.Elements().First(e => e.Name.LocalName is "drawing" or "legacyDrawing" or "tableParts").AddBeforeSelf(
                new XElement(m + "rowBreaks", new XAttribute("count", 1), new XAttribute("manualBreakCount", 1),
                    new XElement(m + "brk", new XAttribute("id", 10), new XAttribute("max", 16383), new XAttribute("man", 1))),
                new XElement(m + "ignoredErrors", new XElement(m + "ignoredError", new XAttribute("sqref", "H2:H3"), new XAttribute("numberStoredAsText", 1))));
            Save(zip, sheetEntry, sheet);

            using var image = zip.CreateEntry("xl/media/image1.png").Open();
            image.Write([137, 80, 78, 71, 1, 2, 3]);
        }

        return ms.ToArray();
    }

    private static XDocument Load(ZipArchiveEntry entry)
    {
        using var s = entry.Open();
        return XDocument.Load(s);
    }

    private static void Save(ZipArchive zip, ZipArchiveEntry entry, XDocument doc)
    {
        var name = entry.FullName;
        entry.Delete();
        using var s = zip.CreateEntry(name).Open();
        doc.Save(s, SaveOptions.DisableFormatting);
    }

    private static (int, int) PictureRows(byte[] package)
    {
        var anchor = Load(Entry(package, "xl/drawings/drawing1.xml")).Root!.Elements().Single(e => e.Descendants().Any(d => d.Name.LocalName == "pic"));
        int Row(string name) => int.Parse(anchor.Elements().Single(e => e.Name.LocalName == name).Elements().Single(e => e.Name.LocalName == "row").Value);
        return (Row("from"), Row("to"));
    }

    private static string IgnoredErrors(byte[] package) =>
        (string)Load(Entry(package, "xl/worksheets/sheet1.xml")).Root!.Elements().Single(e => e.Name.LocalName == "ignoredErrors").Elements().Single().Attribute("sqref")!;

    private static List<string> Breaks(byte[] package, string name) =>
        Load(Entry(package, "xl/worksheets/sheet1.xml")).Root!.Elements().Single(e => e.Name.LocalName == name).Elements().Select(b => (string)b.Attribute("id")!).ToList();

    private static byte[] WithPart(byte[] package, string part, string contentType)
    {
        var ms = new MemoryStream();
        ms.Write(package);
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, true))
        {
            var typesEntry = zip.GetEntry("[Content_Types].xml")!;
            var types = Load(typesEntry);
            types.Root!.Add(new XElement(Types + "Override", new XAttribute("PartName", "/" + part), new XAttribute("ContentType", contentType)));
            Save(zip, typesEntry, types);
            using var s = zip.CreateEntry(part).Open();
            s.Write("<x/>"u8);
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

    private static XDocument Load(byte[] xml) => XDocument.Load(new MemoryStream(xml));

    private static List<(string Name, DateTimeOffset Time)> Entries(byte[] zip)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        return archive.Entries.Select(e => (e.FullName, e.LastWriteTime)).ToList();
    }

    private static byte[] Entry(byte[] zip, string name)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        using var s = archive.GetEntry(name)!.Open();
        var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}
