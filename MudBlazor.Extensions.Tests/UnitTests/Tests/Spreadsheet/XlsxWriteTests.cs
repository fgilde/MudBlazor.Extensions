using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit.Abstractions;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class XlsxWriteTests(ITestOutputHelper output)
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PkgRel = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string CtNs = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string X14ac = "http://schemas.microsoft.com/office/spreadsheetml/2009/9/ac";
    private const string Mc = "http://schemas.openxmlformats.org/markup-compatibility/2006";
    private static readonly XNamespace M = Main;

    private static string DataPath(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", "Spreadsheet", name);

    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static Cell At(Worksheet ws, string a1) => ws[R(a1)];

    private static string Sheet(string sheetData, string before = "", string after = "", string rootNs = "") =>
        XlsxReadTests.Sheet(sheetData, before, after, rootNs);

    private static byte[] Package(string[] sheets, string? sharedStrings = null, string? styles = null, string workbookExtra = "") =>
        XlsxReadTests.Package(sheets, sharedStrings, styles, workbookExtra: workbookExtra);

    private static Workbook Read(byte[] package) => XlsxReader.Read(new MemoryStream(package));

    private static byte[] Write(Workbook wb)
    {
        var ms = new MemoryStream();
        XlsxWriter.Write(wb, ms);
        return ms.ToArray();
    }

    private static Dictionary<string, byte[]> Entries(byte[] zip)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        return archive.Entries.ToDictionary(e => e.FullName, e =>
        {
            using var s = e.Open();
            var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        });
    }

    private static XDocument Xml(byte[] zip, string name) => XDocument.Load(new MemoryStream(Entries(zip)[name]));

    private static byte[] With(byte[] package, params (string Name, object Content)[] entries)
    {
        var ms = new MemoryStream();
        ms.Write(package);
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, true))
        {
            foreach (var (name, content) in entries)
            {
                zip.GetEntry(name)?.Delete();
                using var s = zip.CreateEntry(name).Open();
                s.Write(content as byte[] ?? Encoding.UTF8.GetBytes((string)content));
            }
        }

        return ms.ToArray();
    }

    private const string DateStyles =
        "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/><xf numFmtId=\"14\" fontId=\"0\" fillId=\"0\" borderId=\"0\" applyNumberFormat=\"1\"/></cellXfs>";

    [Fact]
    public void Date1904_ReadsAs1900SerialsAndWritesBackOriginalSerials()
    {
        var source = Package([Sheet("<row r=\"1\"><c r=\"A1\" s=\"1\"><v>44000</v></c><c r=\"B1\"><v>44000</v></c><c r=\"C1\" s=\"1\"><f>A1+1</f><v>44001</v></c></row>")],
            styles: DateStyles, workbookExtra: "<workbookPr date1904=\"1\"/>");
        var wb = Read(source);
        var ws = wb.Sheets[0];
        Assert.Equal(CellValue.Number(45462), At(ws, "A1").Value);
        Assert.Equal(CellValue.Number(44000), At(ws, "B1").Value);
        Assert.Equal(CellValue.Number(45463), At(ws, "C1").Value);
        Assert.Equal("2024-06-19", NumberFormat.Format(At(ws, "A1").Value, "yyyy-mm-dd", CultureInfo.InvariantCulture, out _));
        using var engine = new CalcEngine(wb);
        Assert.Equal(CellValue.Number(45463), At(ws, "C1").Value);

        wb.Execute(new SetValueCommand(ws, R("A2"), CellValue.Number(45462)));
        wb.Execute(new SetStyleCommand(ws, new CellRange(R("A2"), R("A2")), wb.Styles[At(ws, "A1").StyleId]));
        var sheet = Xml(Write(wb), "xl/worksheets/sheet1.xml");
        string V(string cell) => sheet.Descendants(M + "c").Single(c => (string)c.Attribute("r") == cell).Element(M + "v")!.Value;
        Assert.Equal("44000", V("A1"));
        Assert.Equal("44000", V("B1"));
        Assert.Equal("44001", V("C1"));
        Assert.Equal("44000", V("A2"));
    }

    [Fact]
    public void HasUnshiftedReferences_ReportsSheetFeaturesAndParts()
    {
        Assert.False(Read(Package([Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row>")])).HasUnshiftedReferences);
        Assert.False(Workbook.Create().HasUnshiftedReferences);
        Assert.False(Read(Package([Sheet("", after: "<conditionalFormatting sqref=\"A1:A5\"><cfRule type=\"cellIs\" priority=\"1\" operator=\"greaterThan\"><formula>1</formula></cfRule></conditionalFormatting>")]))
            .HasUnshiftedReferences);
        Assert.False(Read(Package([Sheet("", after: "<dataValidations count=\"1\"><dataValidation type=\"whole\" sqref=\"B2\"/></dataValidations>")])).HasUnshiftedReferences);
        Assert.True(Read(Package([Sheet("", after: "<hyperlinks><hyperlink ref=\"A1\" location=\"Sheet1!B5\"/></hyperlinks>")])).HasUnshiftedReferences);
        Assert.False(Read(Package([Sheet("", after: "<autoFilter ref=\"A1:C9\"/><sortState ref=\"A2:C9\"/>")])).HasUnshiftedReferences);
        var pivot = With(Package([Sheet("")]), ("xl/pivotCache/pivotCacheDefinition1.xml", "<pivotCacheDefinition/>"),
            ("[Content_Types].xml", Types(Override("/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"),
                Override("/xl/worksheets/sheet1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"),
                Override("/xl/pivotCache/pivotCacheDefinition1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.pivotCacheDefinition+xml"))));
        Assert.True(Read(pivot).HasUnshiftedReferences);
    }

    [Theory]
    [InlineData("xl/workbook.xml")]
    [InlineData("[Content_Types].xml")]
    [InlineData("xl/_rels/workbook.xml.rels")]
    public void Read_RejectsDtdInPackageParts(string part)
    {
        var source = Package([Sheet("")]);
        var xml = Encoding.UTF8.GetString(Entries(source)[part]);
        var root = xml.IndexOf('<', xml.IndexOf("?>", StringComparison.Ordinal) + 2);
        var name = xml[(root + 1)..xml.IndexOfAny([' ', '>'], root)];
        var doctype = $"<!DOCTYPE {name} [<!ENTITY a \"aaaaaaaaaa\">]>";
        Assert.Throws<InvalidDataException>(() => Read(With(source, (part, xml.Insert(root, doctype)))));
    }

    private static string Types(params string[] overrides) =>
        $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"{CtNs}\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" + string.Concat(overrides) + "</Types>";

    private static string Override(string part, string type) => $"<Override PartName=\"{part}\" ContentType=\"{type}\"/>";

    private static string Rels(params string[] rels) => $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"{PkgRel}\">{string.Concat(rels)}</Relationships>";

    private static string Relationship(string id, string type, string target) => $"<Relationship Id=\"{id}\" Type=\"{(type.Contains('/') ? type : Rel + "/" + type)}\" Target=\"{target}\"/>";

    private const string WorksheetType = "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml";
    private const string MainType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";
    private const string MacroType = "application/vnd.ms-excel.sheet.macroEnabled.main+xml";

    private static XElement Canonical(XElement e) => new(e.Name,
        e.Attributes().Where(a => a.Name.LocalName != "spans").OrderBy(a => a.Name.ToString())
            .Select(a => new XAttribute(a.Name, a.Value switch { "true" => "1", "false" => "0", var v => v })),
        e.Nodes().Select(n => n is XElement c ? Canonical(c) : n));

    private static string Normalized(byte[] zip, string sheetPart)
    {
        var entries = Entries(zip);
        var sst = entries.ContainsKey("xl/sharedStrings.xml")
            ? Xml(zip, "xl/sharedStrings.xml").Root!.Elements().Select(si => si.ToString(SaveOptions.DisableFormatting)).ToArray()
            : [];
        var root = Xml(zip, sheetPart).Root!;
        foreach (var c in root.Descendants(M + "c").Where(c => (string?)c.Attribute("t") == "s"))
        {
            var v = c.Element(M + "v")!;
            v.Value = sst[int.Parse(v.Value, CultureInfo.InvariantCulture)];
        }

        return Canonical(root).ToString();
    }

    private static string KitchenSink()
    {
        const string rows = """
            <row r="1" spans="1:5" x14ac:dyDescent="0.25"><c r="A1" t="s"><v>0</v></c><c r="B1" t="s"><v>1</v></c><c r="C1"><v>1.5</v></c><c r="D1" t="b"><v>1</v></c><c r="E1" t="e" vm="1"><v>#VALUE!</v></c></row>
            <row r="2" ht="30" customHeight="1" outlineLevel="1" x14ac:dyDescent="0.25"><c r="A2"><f>C1*2</f><v>3</v></c><c r="B2" t="str"><f>"x"&amp;A2</f><v>x3</v></c><c r="C2" cm="1"><f t="array" ref="C2:C3">_xlfn.SEQUENCE(2)</f><v>1</v></c><c r="D2" t="s" ph="1"><v>2</v></c></row>
            <row r="3" hidden="1"><c r="C3"><v>2</v></c><c r="D3" t="e"><v>#N/A</v></c></row>
            <row r="4"><c r="A4" t="s"><v>2</v></c></row>
            """;
        const string before = """
            <sheetPr><tabColor rgb="FFFF0000"/><pageSetUpPr fitToPage="1"/></sheetPr><dimension ref="A1:E4"/>
            <sheetViews><sheetView tabSelected="1" zoomScale="120" workbookViewId="0"><pane xSplit="1" ySplit="1" topLeftCell="B2" activePane="bottomRight" state="frozen"/><selection pane="bottomRight" activeCell="C3" sqref="C3"/></sheetView></sheetViews>
            <sheetFormatPr defaultRowHeight="15" x14ac:dyDescent="0.25"/>
            <cols><col min="1" max="2" width="12.5703125" customWidth="1"/><col min="4" max="4" width="9.140625" hidden="1" outlineLevel="1"/></cols>
            """;
        const string after = """
            <mergeCells count="1"><mergeCell ref="A4:B4"/></mergeCells>
            <conditionalFormatting sqref="C1:C3"><cfRule type="cellIs" dxfId="0" priority="1" operator="greaterThan"><formula>1</formula></cfRule></conditionalFormatting>
            <pageMargins left="0.7" right="0.7" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>
            <extLst><ext uri="{78C0D931-6437-407d-A8EE-F0AAD7539E65}" xmlns:x14="http://schemas.microsoft.com/office/spreadsheetml/2009/9/main"><x14:conditionalFormattings/></ext></extLst>
            """;
        return Sheet(rows.ReplaceLineEndings(""), before.ReplaceLineEndings(""), after.ReplaceLineEndings(""),
            $" xmlns:mc=\"{Mc}\" mc:Ignorable=\"x14ac\" xmlns:x14ac=\"{X14ac}\"");
    }

    private const string KitchenSinkStrings = "<si><t>alpha</t></si><si><r><rPr><b/></rPr><t>bo</t></r><r><t>ld</t></r></si><si><t xml:space=\"preserve\"> padded </t></si>";

    [Fact]
    public void Write_UnchangedWorkbook_SheetPartsSemanticallyEqual()
    {
        var sample = File.ReadAllBytes(DataPath("ExcelSheet.xlsx"));
        var written = Write(Read(sample));
        Assert.Equal(Normalized(sample, "xl/worksheets/sheet1.xml"), Normalized(written, "xl/worksheets/sheet1.xml"));
        Assert.Equal(Entries(sample)["xl/styles.xml"], Entries(written)["xl/styles.xml"]);
        Assert.Equal(Entries(sample)["xl/theme/theme1.xml"], Entries(written)["xl/theme/theme1.xml"]);

        var package = Package([KitchenSink()], KitchenSinkStrings);
        var wb = Read(package);
        using (new CalcEngine(wb)) written = Write(wb);
        Assert.Equal(Normalized(package, "xl/worksheets/sheet1.xml"), Normalized(written, "xl/worksheets/sheet1.xml"));
        var again = Read(written).Sheets[0];
        Assert.Equal("SEQUENCE(2)", At(again, "C2").Formula);
        Assert.Equal(R("C2"), At(again, "C3").SpillAnchor);
    }

    [Fact]
    public void Write_ChangedCell_OtherPartsByteIdentical()
    {
        var vba = Enumerable.Range(0, 5000).Select(i => (byte)(i * 31 % 251)).ToArray();
        var package = With(Package([Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c><c r=\"B1\" t=\"s\"><v>0</v></c></row>")], "<si><t>x</t></si>"),
            ("[Content_Types].xml", Types(
                "<Default Extension=\"bin\" ContentType=\"application/vnd.ms-office.vbaProject\"/>",
                Override("/xl/workbook.xml", MacroType), Override("/xl/worksheets/sheet1.xml", WorksheetType),
                Override("/xl/sharedStrings.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"),
                Override("/xl/pivotCache/pivotCacheDefinition1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.pivotCacheDefinition+xml"),
                Override("/xl/pivotCache/pivotCacheRecords1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.pivotCacheRecords+xml"),
                Override("/customXml/itemProps1.xml", "application/vnd.openxmlformats-officedocument.customXmlProperties+xml"))),
            ("xl/_rels/workbook.xml.rels", Rels(Relationship("rId1", "worksheet", "worksheets/sheet1.xml"), Relationship("rIdS", "sharedStrings", "sharedStrings.xml"),
                Relationship("rId7", "http://schemas.microsoft.com/office/2006/relationships/vbaProject", "vbaProject.bin"),
                Relationship("rId8", "pivotCacheDefinition", "pivotCache/pivotCacheDefinition1.xml"))),
            ("xl/vbaProject.bin", vba),
            ("xl/pivotCache/pivotCacheDefinition1.xml", $"<pivotCacheDefinition xmlns=\"{Main}\" xmlns:r=\"{Rel}\" r:id=\"rId1\" recordCount=\"1\"><cacheSource type=\"worksheet\"><worksheetSource ref=\"A1:B1\" sheet=\"Sheet1\"/></cacheSource></pivotCacheDefinition>"),
            ("xl/pivotCache/_rels/pivotCacheDefinition1.xml.rels", Rels(Relationship("rId1", "pivotCacheRecords", "pivotCacheRecords1.xml"))),
            ("xl/pivotCache/pivotCacheRecords1.xml", $"<pivotCacheRecords xmlns=\"{Main}\" count=\"1\"><r><n v=\"1\"/></r></pivotCacheRecords>"),
            ("customXml/item1.xml", "<?xml version=\"1.0\"?>\r\n<data   attr = 'kept' >  spacing  </data>"),
            ("customXml/itemProps1.xml", "<ds:datastoreItem ds:itemID=\"{1}\" xmlns:ds=\"http://schemas.openxmlformats.org/officeDocument/2006/customXml\"/>"),
            ("customXml/_rels/item1.xml.rels", Rels(Relationship("rId1", "customXmlProps", "itemProps1.xml"))),
            ("docProps/app.xml", "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\"><Application>Microsoft Excel</Application></Properties>"));

        var wb = Read(package);
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(2)));
        var written = Write(wb);

        var before = Entries(package);
        var after = Entries(written);
        string[] modelled = ["[Content_Types].xml", "xl/_rels/workbook.xml.rels", "xl/workbook.xml", "xl/worksheets/sheet1.xml", "xl/sharedStrings.xml"];
        foreach (var (name, bytes) in before.Where(e => !modelled.Contains(e.Key)))
        {
            Assert.True(after.ContainsKey(name), name);
            Assert.Equal(bytes, after[name]);
        }

        Assert.Equal(vba, after["xl/vbaProject.bin"]);
        var types = Xml(written, "[Content_Types].xml").Root!;
        Assert.Equal(MacroType, types.Elements().Single(e => (string?)e.Attribute("PartName") == "/xl/workbook.xml").Attribute("ContentType")!.Value);
        Assert.Contains(types.Elements(), e => (string?)e.Attribute("Extension") == "bin");
        var rels = Xml(written, "xl/_rels/workbook.xml.rels").Root!.Elements().ToList();
        Assert.Contains(rels, r => (string?)r.Attribute("Target") == "vbaProject.bin" && (string?)r.Attribute("Id") == "rId7");
        Assert.Contains(rels, r => (string?)r.Attribute("Target") == "pivotCache/pivotCacheDefinition1.xml");

        var again = Read(written).Sheets[0];
        Assert.Equal(2, At(again, "A1").Value.NumberValue);
        Assert.Equal("x", At(again, "B1").Value.TextValue);
    }

    [Fact]
    public void Write_UnknownChildrenStayInSchemaOrder()
    {
        const string after = """
            <sheetProtection sheet="1" objects="1"/>
            <conditionalFormatting sqref="A1"><cfRule type="expression" dxfId="0" priority="1"><formula>TRUE</formula></cfRule></conditionalFormatting>
            <hyperlinks><hyperlink ref="A1" location="Sheet1!B2" display="x"/></hyperlinks>
            <pageMargins left="0.7" right="0.7" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>
            <x:unknown xmlns:x="urn:unknown" a="1"/>
            <extLst><ext uri="{78C0D931-6437-407d-A8EE-F0AAD7539E65}"/></extLst>
            """;
        var package = Package([Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row>", "", after.ReplaceLineEndings(""))]);
        var wb = Read(package);
        var ws = wb.Sheets[0];
        wb.Execute(new MergeCommand(ws, CellRange.Parse("A1:B2")));
        wb.Execute(new FreezeCommand(ws, R("B2")));
        wb.Execute(new SetTabColorCommand(ws, "#00B050"));
        var written = Write(wb);

        var children = Xml(written, "xl/worksheets/sheet1.xml").Root!.Elements().ToList();
        Assert.Equal(["sheetPr", "dimension", "sheetViews", "sheetFormatPr", "sheetData", "sheetProtection", "mergeCells", "conditionalFormatting",
            "hyperlinks", "pageMargins", "unknown", "extLst"], children.Select(c => c.Name.LocalName));
        var original = Xml(package, "xl/worksheets/sheet1.xml").Root!;
        foreach (var name in (string[])["conditionalFormatting", "hyperlinks", "pageMargins", "sheetProtection"])
            Assert.True(XNode.DeepEquals(original.Element(M + name), children.Single(c => c.Name == M + name)), name);
        Assert.Equal("FF00B050", children[0].Element(M + "tabColor")!.Attribute("rgb")!.Value);

        var again = Read(written).Sheets[0];
        Assert.Equal([CellRange.Parse("A1:B2")], again.Merges);
        Assert.Equal(R("B2"), again.FreezeAt);
        Assert.Equal("#00B050", again.TabColor);
    }

    [Fact]
    public void Write_NewWorkbook_ReopensIdentically()
    {
        var wb = Workbook.Create("Data");
        var ws = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1.1)));
        wb.Execute(new SetValueCommand(ws, R("A2"), CellValue.Number(0.1 + 0.2)));
        wb.Execute(new SetValueCommand(ws, R("B1"), CellValue.Text(" padded ")));
        wb.Execute(new SetValueCommand(ws, R("B2"), CellValue.Text("ctrl\u0001_x0041_\r\nline")));
        wb.Execute(new SetValueCommand(ws, R("C1"), CellValue.Boolean(true)));
        wb.Execute(new SetValueCommand(ws, R("C2"), CellValue.Error(CellError.NA)));
        wb.Execute(new SetFormulaCommand(ws, R("D1"), "=A1*2"));
        wb.Execute(new SetFormulaCommand(ws, R("D2"), "=XLOOKUP(1.1,A1:A2,B1:B2)"));
        wb.Execute(new SetFormulaCommand(ws, R("D3"), "=SEQUENCE(3)"));
        wb.Execute(new SetFormulaCommand(ws, R("E1"), "=\"a\"&B2"));
        var style = new CellStyle
        {
            Font = new CellFont { Bold = true, Italic = true, Underline = true, Size = 14, Name = "Arial", Color = "#FF0000" },
            Fill = new CellFill { Color = "#00FF00" },
            Border = new CellBorder { Bottom = new CellBorderSide { Style = CellBorderStyle.Double, Color = "#0000FF" }, Left = new CellBorderSide { Style = CellBorderStyle.Thin } },
            Alignment = new CellAlignment { Horizontal = CellHorizontalAlignment.Center, Vertical = CellVerticalAlignment.Top, WrapText = true, Indent = 2 },
            NumberFormat = "0.000"
        };
        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("A1:A2"), style));
        wb.Execute(new SetValueCommand(ws, R("F1"), CellValue.Number(45000.5)));
        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("F1"), new CellStyle { NumberFormat = BuiltInNumberFormats.Get(14, CultureInfo.CurrentCulture) }));
        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("G5"), new CellStyle { Fill = new CellFill { Color = "#ABCDEF" } }));
        wb.Execute(new MergeCommand(ws, CellRange.Parse("A10:B11")));
        wb.Execute(new FreezeCommand(ws, R("B2")));
        wb.Execute(new ResizeCommand(ws, Axis.Column, 1, 2, 100));
        wb.Execute(new ResizeCommand(ws, Axis.Row, 2, 1, 40));
        wb.Execute(new SetHiddenCommand(ws, Axis.Row, 5, 1, true));
        wb.Execute(new SetHiddenCommand(ws, Axis.Column, 6, 1, true));
        wb.Execute(new SetTabColorCommand(ws, "#123456"));
        wb.Execute(new AddSheetCommand("Second"));
        wb.Execute(new SetValueCommand(wb.Sheets[1], R("A1"), CellValue.Text(" padded ")));
        wb.Execute(new DefineNameCommand("Total", "Data!$A$1:$A$2"));

        var written = Write(wb);
        var sheetXml = Encoding.UTF8.GetString(Entries(written)["xl/worksheets/sheet1.xml"]);
        Assert.Contains("_xlfn.XLOOKUP(", sheetXml);
        Assert.Contains("<f t=\"array\" ref=\"D3:D5\">_xlfn.SEQUENCE(3)</f>", sheetXml);
        Assert.Contains("ctrl_x0001__x005F_x0041__x000D_\nline", sheetXml + Encoding.UTF8.GetString(Entries(written)["xl/sharedStrings.xml"]));
        Assert.DoesNotContain("theme", string.Join(",", Entries(written).Keys));
        Assert.DoesNotContain("fullCalcOnLoad", Encoding.UTF8.GetString(Entries(written)["xl/workbook.xml"]));

        var re = Read(written);
        Assert.Equal(["Data", "Second"], re.Sheets.Select(s => s.Name));
        var rs = re.Sheets[0];
        var expected = ws.UsedCells().ToDictionary(c => c.Ref, c => c.Cell);
        var actual = rs.UsedCells().ToDictionary(c => c.Ref, c => c.Cell);
        Assert.Equal(expected.Keys.OrderBy(k => k.Row).ThenBy(k => k.Column), actual.Keys.OrderBy(k => k.Row).ThenBy(k => k.Column));
        foreach (var (at, cell) in expected)
        {
            var other = actual[at];
            Assert.True(cell.Value == other.Value, $"{at.ToA1()}: {cell.Value} vs {other.Value}");
            Assert.Equal(cell.Formula, other.Formula);
            Assert.Equal(cell.SpillAnchor, other.SpillAnchor);
            Assert.Equal(wb.Styles[cell.StyleId], re.Styles[other.StyleId]);
        }

        Assert.Equal(0.30000000000000004, At(rs, "A2").Value.NumberValue);
        Assert.Equal(ws.Merges, rs.Merges);
        Assert.Equal(ws.FreezeAt, rs.FreezeAt);
        Assert.Equal(ws.ColumnWidths, rs.ColumnWidths);
        Assert.Equal(ws.RowHeights, rs.RowHeights);
        Assert.Equal(ws.HiddenRows, rs.HiddenRows);
        Assert.Equal(ws.HiddenColumns, rs.HiddenColumns);
        Assert.Equal("#123456", rs.TabColor);
        Assert.Equal(" padded ", At(re.Sheets[1], "A1").Value.TextValue);
        Assert.Equal("Data!$A$1:$A$2", re.DefinedNames["Total"]);
    }

    [Fact]
    public void Write_RemovesCalcChain()
    {
        var package = With(Package([Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c><c r=\"B1\"><f>A1+1</f><v>2</v></c></row>")],
                workbookExtra: "<calcPr calcId=\"191029\"/>"),
            ("[Content_Types].xml", Types(Override("/xl/workbook.xml", MainType), Override("/xl/worksheets/sheet1.xml", WorksheetType),
                Override("/xl/calcChain.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.calcChain+xml"))),
            ("xl/_rels/workbook.xml.rels", Rels(Relationship("rId1", "worksheet", "worksheets/sheet1.xml"), Relationship("rId9", "calcChain", "calcChain.xml"))),
            ("xl/calcChain.xml", $"<calcChain xmlns=\"{Main}\"><c r=\"B1\" i=\"1\"/></calcChain>"));

        var written = Write(Read(package));

        var entries = Entries(written);
        Assert.DoesNotContain("xl/calcChain.xml", entries.Keys);
        Assert.DoesNotContain("calcChain", Encoding.UTF8.GetString(entries["xl/_rels/workbook.xml.rels"]));
        Assert.DoesNotContain("calcChain", Encoding.UTF8.GetString(entries["[Content_Types].xml"]));
        var workbook = Encoding.UTF8.GetString(entries["xl/workbook.xml"]);
        Assert.Contains("calcId=\"191029\"", workbook);
        Assert.DoesNotContain("fullCalcOnLoad", workbook);
    }

    [Fact]
    public void Write_SharedStringsDeduplicated()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        for (var r = 0; r < 100; r++) wb.Execute(new SetValueCommand(ws, new CellRef(r, 0), CellValue.Text(r % 2 == 0 ? "same" : "other")));
        var sst = Xml(Write(wb), "xl/sharedStrings.xml").Root!;
        Assert.Equal(["same", "other"], sst.Elements(M + "si").Select(si => si.Value));
        Assert.Equal("2", sst.Attribute("uniqueCount")!.Value);
        Assert.Equal("100", sst.Attribute("count")!.Value);

        const string strings = "<si><t>plain</t></si><si><r><rPr><b/></rPr><t>bold</t></r><r><t xml:space=\"preserve\"> tail</t></r></si><si><t>plain</t></si>";
        var package = Package([Sheet("<row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c><c r=\"B1\" t=\"s\"><v>1</v></c><c r=\"C1\" t=\"s\"><v>1</v></c><c r=\"D1\" t=\"s\"><v>2</v></c></row>")], strings);
        var read = Read(package);
        read.Execute(new SetValueCommand(read.Sheets[0], R("C1"), CellValue.Text("bold tail!")));
        var written = Write(read);
        var items = Xml(written, "xl/sharedStrings.xml").Root!.Elements(M + "si").ToList();
        Assert.Equal(4, items.Count);
        Assert.Equal(["plain", "bold tail", "plain", "bold tail!"], items.Select(si => si.Value));
        var rich = XElement.Parse($"<sst xmlns=\"{Main}\">{strings}</sst>").Elements().ElementAt(1);
        Assert.Single(items, si => XNode.DeepEquals(si, rich));
        var cells = Xml(written, "xl/worksheets/sheet1.xml").Descendants(M + "c").ToDictionary(c => (string)c.Attribute("r")!, c => int.Parse(c.Value, CultureInfo.InvariantCulture));
        Assert.Equal(cells["A1"], cells["D1"]);
        Assert.True(XNode.DeepEquals(items[cells["B1"]], rich));
        Assert.Equal("bold tail!", items[cells["C1"]].Value);
    }

    [Fact]
    public void Write_AddedAndDeletedSheets_UpdateRelsAndContentTypes()
    {
        const string names = "<definedNames><definedName name=\"G\">Sheet1!$A$1</definedName><definedName name=\"Local2\" localSheetId=\"1\">Sheet2!$A$1</definedName>" +
                             "<definedName name=\"_xlnm.Print_Area\" localSheetId=\"2\">Sheet3!$A$1:$B$2</definedName></definedNames>";
        var package = With(Package([Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row>"), Sheet("<row r=\"1\"><c r=\"A1\"><v>2</v></c></row>", after: "<legacyDrawing r:id=\"rId1\"/>"),
                Sheet("<row r=\"1\"><c r=\"A1\"><v>3</v></c></row>")], workbookExtra: names),
            ("xl/workbook.xml", $"<workbook xmlns=\"{Main}\" xmlns:r=\"{Rel}\"><bookViews><workbookView activeTab=\"2\"/></bookViews><sheets>" +
                                "<sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"Sheet2\" sheetId=\"2\" r:id=\"rId2\"/><sheet name=\"Sheet3\" sheetId=\"3\" r:id=\"rId3\"/></sheets>" + names + "</workbook>"),
            ("[Content_Types].xml", Types(Override("/xl/workbook.xml", MainType), Override("/xl/worksheets/sheet1.xml", WorksheetType),
                Override("/xl/worksheets/sheet2.xml", WorksheetType), Override("/xl/worksheets/sheet3.xml", WorksheetType),
                Override("/xl/comments1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.comments+xml"),
                "<Default Extension=\"vml\" ContentType=\"application/vnd.openxmlformats-officedocument.vmlDrawing\"/>")),
            ("xl/worksheets/_rels/sheet2.xml.rels", Rels(Relationship("rId1", "vmlDrawing", "../drawings/vmlDrawing1.vml"), Relationship("rId2", "comments", "../comments1.xml"))),
            ("xl/drawings/vmlDrawing1.vml", "<xml/>"),
            ("xl/comments1.xml", $"<comments xmlns=\"{Main}\"/>"));

        var wb = Read(package);
        wb.Execute(new DeleteSheetCommand(wb.Sheets[1]));
        var add = new AddSheetCommand("Added");
        wb.Execute(add);
        wb.Execute(new SetValueCommand(add.Sheet, R("A1"), CellValue.Number(4)));
        wb.Execute(new RenameSheetCommand(wb.Sheets[1], "Third"));
        var written = Write(wb);

        var entries = Entries(written);
        foreach (var gone in (string[])["xl/worksheets/sheet2.xml", "xl/worksheets/_rels/sheet2.xml.rels", "xl/comments1.xml", "xl/drawings/vmlDrawing1.vml"])
            Assert.DoesNotContain(gone, entries.Keys);
        Assert.Contains("xl/worksheets/sheet4.xml", entries.Keys);

        var overrides = Xml(written, "[Content_Types].xml").Root!.Elements(XName.Get("Override", CtNs))
            .ToDictionary(e => (string)e.Attribute("PartName")!, e => (string)e.Attribute("ContentType")!);
        Assert.DoesNotContain("/xl/worksheets/sheet2.xml", overrides.Keys);
        Assert.DoesNotContain("/xl/comments1.xml", overrides.Keys);
        Assert.Equal(WorksheetType, overrides["/xl/worksheets/sheet4.xml"]);

        var rels = Xml(written, "xl/_rels/workbook.xml.rels").Root!.Elements().ToDictionary(e => (string)e.Attribute("Id")!, e => (string)e.Attribute("Target")!);
        Assert.DoesNotContain("rId2", rels.Keys);
        var workbook = Xml(written, "xl/workbook.xml").Root!;
        var sheets = workbook.Element(M + "sheets")!.Elements().ToList();
        Assert.Equal(["Sheet1", "Third", "Added"], sheets.Select(s => (string)s.Attribute("name")!));
        Assert.Equal(["1", "3", "4"], sheets.Select(s => (string)s.Attribute("sheetId")!));
        Assert.Equal("worksheets/sheet4.xml", rels[(string)sheets[2].Attribute(XName.Get("id", Rel))!]);
        var definedNames = workbook.Element(M + "definedNames")!.Elements().ToList();
        Assert.Equal(["G", "_xlnm.Print_Area"], definedNames.Select(n => (string)n.Attribute("name")!));
        Assert.Equal("1", (string)definedNames[1].Attribute("localSheetId")!);
        Assert.Equal("Third!$A$1:$B$2", definedNames[1].Value);
        Assert.Equal("1", (string)workbook.Element(M + "bookViews")!.Element(M + "workbookView")!.Attribute("activeTab")!);

        var again = Read(written);
        Assert.Equal(["Sheet1", "Third", "Added"], again.Sheets.Select(s => s.Name));
        Assert.Equal([1d, 3, 4], again.Sheets.Select(s => At(s, "A1").Value.NumberValue));
    }

    [Fact]
    public void Write_KeepsCachedValues_OfUnevaluableFormulasAndArrays()
    {
        const string rows = """
            <row r="1"><c r="A1"><f>FOOBAR(1)</f><v>42</v></c><c r="B1"><f t="array" ref="B1:B2">FOOBAR(2)</f><v>1</v></c><c r="C1"><f>A1+1</f><v>43</v></c>
            <c r="D1"><f>Table1[[#This Row],[Col]]</f><v>5</v></c><c r="E1"><f t="array" ref="E1">SUM(IF(C1:C2&gt;1,1,0))</f><v>1</v></c></row>
            <row r="2"><c r="B2"><v>2</v></c></row>
            """;
        var wb = Read(Package([Sheet(rows.ReplaceLineEndings(""))]));
        var ws = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        Assert.Equal(42, At(ws, "A1").Value.NumberValue);
        Assert.Equal(2, At(ws, "B2").Value.NumberValue);
        Assert.Equal(R("B1"), At(ws, "B2").SpillAnchor);
        Assert.Equal(43, At(ws, "C1").Value.NumberValue);
        Assert.Equal(5, At(ws, "D1").Value.NumberValue);

        var written = Write(wb);
        var xml = Encoding.UTF8.GetString(Entries(written)["xl/worksheets/sheet1.xml"]);
        Assert.Contains("<c r=\"B1\"><f t=\"array\" ref=\"B1:B2\">FOOBAR(2)</f><v>1</v></c>", xml);
        Assert.Contains("<c r=\"B2\"><v>2</v></c>", xml);
        Assert.Contains("<c r=\"E1\"><f t=\"array\" ref=\"E1\">SUM(IF(C1:C2&gt;1,1,0))</f>", xml);
        Assert.Equal(42, At(Read(written).Sheets[0], "A1").Value.NumberValue);

        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=FOOBAR(3)"));
        Assert.Equal(CellValue.Error(CellError.Name), At(ws, "A1").Value);
        Assert.Equal(CellValue.Error(CellError.Name), At(ws, "C1").Value);
    }

    [Fact]
    public void Write_CircularReference_WritesZeroAndKeepsFormula()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=A1+1"));
        Assert.Equal(CellValue.Error(CellError.Circ), At(ws, "A1").Value);

        var xml = Encoding.UTF8.GetString(Entries(Write(wb))["xl/worksheets/sheet1.xml"]);
        Assert.Contains("<c r=\"A1\"><f>A1+1</f><v>0</v></c>", xml);
        Assert.DoesNotContain("#CIRC!", xml);
    }

    [Fact]
    public void Write_KeepsDate1904ChartSheetsAndUnmodelledXf()
    {
        const string styles = """
            <fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts>
            <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
            <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
            <cellXfs count="4"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyProtection="1"><protection locked="0"/></xf>
            <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/>
            <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1" applyProtection="1"><protection locked="0"/></xf></cellXfs>
            """;
        var sheet = Sheet("<row r=\"1\" s=\"3\" customFormat=\"1\"><c r=\"A1\" s=\"1\"/><c r=\"B1\" s=\"3\"><v>1</v></c><c r=\"C1\" s=\"2\"><v>2</v></c></row>",
            "<cols><col min=\"1\" max=\"1\" width=\"9.140625\" style=\"1\"/><col min=\"2\" max=\"3\" width=\"9.140625\" style=\"3\"/></cols>");
        var package = With(Package([sheet], styles: styles.ReplaceLineEndings("")),
            ("xl/workbook.xml", $"<workbook xmlns=\"{Main}\" xmlns:r=\"{Rel}\"><workbookPr date1904=\"1\"/><sheets><sheet name=\"Chart1\" sheetId=\"5\" r:id=\"rIdC\"/>" +
                                "<sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>"),
            ("xl/_rels/workbook.xml.rels", Rels(Relationship("rId1", "worksheet", "worksheets/sheet1.xml"), Relationship("rIdY", "styles", "/xl/styles.xml"),
                Relationship("rIdC", "chartsheet", "chartsheets/sheet1.xml"))),
            ("xl/chartsheets/sheet1.xml", $"<chartsheet xmlns=\"{Main}\"><sheetViews><sheetView workbookViewId=\"0\"/></sheetViews></chartsheet>"));

        var wb = Read(package);
        wb.Execute(new SetValueCommand(wb.Sheets[0], R("C1"), CellValue.Number(3)));
        var written = Write(wb);

        var workbook = Xml(written, "xl/workbook.xml").Root!;
        Assert.Equal("1", (string)workbook.Element(M + "workbookPr")!.Attribute("date1904")!);
        Assert.Equal(["Chart1", "Sheet1"], workbook.Element(M + "sheets")!.Elements().Select(s => (string)s.Attribute("name")!));
        Assert.Equal("rIdC", (string)workbook.Element(M + "sheets")!.Elements().First().Attribute(XName.Get("id", Rel))!);
        Assert.Equal(Entries(package)["xl/chartsheets/sheet1.xml"], Entries(written)["xl/chartsheets/sheet1.xml"]);
        Assert.Equal(Entries(package)["xl/styles.xml"], Entries(written)["xl/styles.xml"]);

        var root = Xml(written, "xl/worksheets/sheet1.xml").Root!;
        Assert.Equal(["1:1:1", "2:3:3"], root.Element(M + "cols")!.Elements().Select(c => $"{c.Attribute("min")!.Value}:{c.Attribute("max")!.Value}:{c.Attribute("style")!.Value}"));
        var row = root.Descendants(M + "row").Single();
        Assert.Equal("3", (string)row.Attribute("s")!);
        Assert.Equal(["A1:1", "B1:3", "C1:2"], row.Elements().Select(c => $"{c.Attribute("r")!.Value}:{c.Attribute("s")!.Value}"));
    }

    [Fact]
    public void Write_BuiltInDateFormats_MapToBuiltInIdsInCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            var wb = Workbook.Create();
            var ws = wb.Sheets[0];
            wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(45000)));
            wb.Execute(new SetStyleCommand(ws, CellRange.Parse("A1"), new CellStyle { NumberFormat = BuiltInNumberFormats.Get(14, CultureInfo.CurrentCulture) }));
            wb.Execute(new SetValueCommand(ws, R("A2"), CellValue.Number(45000.5)));
            wb.Execute(new SetStyleCommand(ws, CellRange.Parse("A2"), new CellStyle { NumberFormat = BuiltInNumberFormats.Get(22, CultureInfo.CurrentCulture) }));

            var styles = Xml(Write(wb), "xl/styles.xml").Root!;
            Assert.Equal(["0", "14", "22"], styles.Element(M + "cellXfs")!.Elements().Select(x => (string)x.Attribute("numFmtId")!));
            Assert.Null(styles.Element(M + "numFmts")?.Elements().FirstOrDefault());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Write_Large_100kx30()
    {
        const int rowCount = 100_000, cols = 30;
        var sb = new StringBuilder(rowCount * cols * 30);
        for (var r = 1; r <= rowCount; r++)
        {
            sb.Append("<row r=\"").Append(r).Append("\" spans=\"1:30\" x14ac:dyDescent=\"0.25\">");
            for (var c = 0; c < cols; c++)
            {
                var a1 = CellRef.ColumnName(c) + r;
                if (c % 3 == 0) sb.Append("<c r=\"").Append(a1).Append("\" t=\"s\"><v>").Append(r % 1000).Append("</v></c>");
                else sb.Append("<c r=\"").Append(a1).Append("\"><v>").Append((r * c * 0.5).ToString(CultureInfo.InvariantCulture)).Append("</v></c>");
            }

            sb.Append("</row>");
        }

        var sst = new StringBuilder();
        for (var i = 0; i < 1000; i++) sst.Append("<si><t>text ").Append(i).Append("</t></si>");
        var package = Package([Sheet(sb.ToString(), $"<dimension ref=\"A1:AD{rowCount}\"/>", rootNs: $" xmlns:mc=\"{Mc}\" mc:Ignorable=\"x14ac\" xmlns:x14ac=\"{X14ac}\"")],
            sst.ToString());
        sb.Clear();

        GC.Collect();
        var before = GC.GetTotalMemory(true);
        var wb = Read(package);
        var retained = GC.GetTotalMemory(true) - before;
        var watch = Stopwatch.StartNew();
        XlsxWriter.Write(wb, new MemoryStream());
        var copied = watch.ElapsedMilliseconds;
        wb.Execute(new SetValueCommand(wb.Sheets[0], new CellRef(0, 1), CellValue.Number(-1)));
        var target = new MemoryStream();
        watch.Restart();
        XlsxWriter.Write(wb, target);
        watch.Stop();
        output.WriteLine($"write {rowCount}x{cols}: untouched {copied} ms, edited {watch.ElapsedMilliseconds} ms, {target.Length / 1024} KB; " +
                         $"read retained {retained / (1024 * 1024)} MB, row attribute sets {wb.Package.Sheets[0].RowAttributes.Values.Distinct().Count()}");

        var again = Read(target.ToArray()).Sheets[0];
        Assert.Equal(CellRange.FromBounds(0, 0, rowCount - 1, cols - 1), again.UsedRange);
        Assert.Equal("text 0", again[new CellRef(rowCount - 1, 0)].Value.TextValue);
        Assert.Equal(rowCount * 29 * 0.5, again[new CellRef(rowCount - 1, 29)].Value.NumberValue);
        Assert.Contains("x14ac:dyDescent=\"0.25\"", Encoding.UTF8.GetString(Entries(target.ToArray())["xl/worksheets/sheet1.xml"], 0, 4000));
    }

    private static string SheetXml(byte[] zip, int n = 1) => Encoding.UTF8.GetString(Entries(zip)[$"xl/worksheets/sheet{n}.xml"]);

    [Fact]
    public void Write_UntouchedSheet_ByteIdenticalAndStringsAppendOnly()
    {
        const string rows2 = """
            <row r="1" spans="1:4"><c r="A1"><v>1</v></c><c r="B1"><f t="dataTable" ref="B1:B2" dt2D="0" dtr="0" r1="A1"/><v>5</v></c>
            <c r="C1" t="inlineStr"><is><r><rPr><b/></rPr><t>bo</t></r><r><t>ld</t></r></is></c><c r="D1"><f t="shared" ref="D1:D2" si="0">A1*2</f><v>2</v></c></row>
            <row r="2"><c r="B2"><v>6</v></c><c r="D2"><f t="shared" si="0"/><v>0</v></c><c r="E2" t="s"><v>1</v></c></row>
            """;
        const string strings = "<si><t>x</t></si><si><t>y</t></si><si><t>x</t></si>";
        var package = Package([Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c><c r=\"B1\" t=\"s\"><v>0</v></c></row>"), Sheet(rows2.ReplaceLineEndings(""))], strings);
        var wb = Read(package);
        using var engine = new CalcEngine(wb);
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Text("y")));
        wb.Execute(new SetValueCommand(ws, R("A2"), CellValue.Text("new")));
        var written = Write(wb);

        Assert.Equal(Entries(package)["xl/worksheets/sheet2.xml"], Entries(written)["xl/worksheets/sheet2.xml"]);
        Assert.Equal(["x", "y", "x", "new"], Xml(written, "xl/sharedStrings.xml").Root!.Elements(M + "si").Select(si => si.Value));
        var cells = Xml(written, "xl/worksheets/sheet1.xml").Descendants(M + "c").ToDictionary(c => (string)c.Attribute("r")!, c => c.Value);
        Assert.Equal("1", cells["A1"]);
        Assert.Equal("0", cells["B1"]);
        Assert.Equal("3", cells["A2"]);
        Assert.Equal("y", At(Read(written).Sheets[0], "A1").Value.TextValue);
    }

    [Fact]
    public void Write_StructureChange_ShiftsPackageData()
    {
        const string styles = """
            <fonts count="1"><font><sz val="11"/><name val="Calibri"/></font></fonts>
            <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
            <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
            <cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyProtection="1"><protection locked="0"/></xf></cellXfs>
            """;
        const string rows = """
            <row r="1"><c r="A1"><v>1</v></c></row><row r="2"><c r="A2" s="1"/></row>
            <row r="3" outlineLevel="1"><c r="B3" cm="1"><f t="array" ref="B3:B4">FOOBAR(1)</f><v>7</v></c></row><row r="4"><c r="B4"><v>8</v></c></row>
            """;
        var package = Package([Sheet(rows.ReplaceLineEndings(""), "<cols><col min=\"2\" max=\"2\" width=\"9.140625\" style=\"1\"/></cols>")], styles: styles.ReplaceLineEndings(""),
            workbookExtra: "<definedNames><definedName name=\"_xlnm.Print_Area\" localSheetId=\"0\">Sheet1!$A$1:$B$4</definedName></definedNames>");
        var wb = Read(package);
        var ws = wb.Sheets[0];
        using var engine = new CalcEngine(wb);

        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        var xml = SheetXml(Write(wb));
        Assert.Contains("<c r=\"A3\" s=\"1\" />", xml);
        Assert.DoesNotContain("<c r=\"A2\" s=\"1\" />", xml);
        Assert.Contains("<row r=\"4\" outlineLevel=\"1\">", xml);
        Assert.Contains("<c r=\"B4\" cm=\"1\"><f t=\"array\" ref=\"B4:B5\">FOOBAR(1)</f><v>7</v></c>", xml);
        Assert.Contains("<c r=\"B5\"><v>8</v></c>", xml);
        Assert.Contains("<col min=\"2\" max=\"2\" width=\"9.140625\" style=\"1\" />", xml);
        Assert.Equal("Sheet1!$A$2:$B$5", Xml(Write(wb), "xl/workbook.xml").Descendants(M + "definedName").Single().Value);

        wb.Execute(new InsertColumnsCommand(ws, 0, 1));
        xml = SheetXml(Write(wb));
        Assert.Contains("<col min=\"3\" max=\"3\" width=\"9.140625\" style=\"1\" />", xml);
        Assert.Contains("<c r=\"B3\" s=\"1\" />", xml);

        wb.Execute(new DeleteRowsCommand(ws, 2, 1));
        xml = SheetXml(Write(wb));
        Assert.DoesNotContain(" s=\"1\" />", xml);
        Assert.Contains("<row r=\"3\" outlineLevel=\"1\">", xml);

        wb.Undo.Undo();
        xml = SheetXml(Write(wb));
        Assert.Contains("<c r=\"B3\" s=\"1\" />", xml);
        Assert.Contains("<row r=\"4\" outlineLevel=\"1\">", xml);
        Assert.Equal("Sheet1!$B$2:$C$5", Xml(Write(wb), "xl/workbook.xml").Descendants(M + "definedName").Single().Value);
    }

    [Theory]
    [InlineData("SUM(A1#)+Sheet2!$B$2#", "SUM(_xlfn.ANCHORARRAY(A1))+_xlfn.ANCHORARRAY(Sheet2!$B$2)")]
    [InlineData("LET(x,1,y,x+1,x*y)", "_xlfn.LET(_xlpm.x,1,_xlpm.y,_xlpm.x+1,_xlpm.x*_xlpm.y)")]
    [InlineData("LET(x,LET(z,2,z),x&\"x\")", "_xlfn.LET(_xlpm.x,_xlfn.LET(_xlpm.z,2,_xlpm.z),_xlpm.x&\"x\")")]
    [InlineData("ISO.CEILING(1.5)+IFS(TRUE,1)", "_xlfn.ISO.CEILING(1.5)+_xlfn.IFS(TRUE,1)")]
    [InlineData("FILTER(A1:A3,A1:A3>1)", "_xlfn._xlws.FILTER(A1:A3,A1:A3>1)")]
    [InlineData("\"A1# LET(x\"&x", "\"A1# LET(x\"&x")]
    public void FormulaText_RoundTripsSpillRefsLetAndPrefixes(string model, string file)
    {
        Assert.Equal(file, SheetXmlWriter.FormulaText(model));
        Assert.Equal(model, SheetXmlWriter.FromFile(file));
    }

    [Fact]
    public void FormulaText_WritesCommaSeparators_AndLetFromFileEvaluates()
    {
        Assert.Equal("SUM(A1,A2)+SUM({1,2;3,4})", SheetXmlWriter.FormulaText("SUM(A1;A2)+SUM({1,2;3,4})"));
        var wb = Read(Package([Sheet("<row r=\"1\"><c r=\"A1\"><f>_xlfn.LET(_xlpm.x,1,_xlpm.x+1)</f><v>2</v></c><c r=\"B1\"><f>_xlfn.SEQUENCE(2)</f><v>1</v></c><c r=\"C1\"><f>SUM(_xlfn.ANCHORARRAY(B1))</f><v>3</v></c></row>")]));
        var ws = wb.Sheets[0];
        Assert.Equal("LET(x,1,x+1)", At(ws, "A1").Formula);
        Assert.Equal("SUM(B1#)", At(ws, "C1").Formula);
        using var engine = new CalcEngine(wb);
        Assert.Equal(2, At(ws, "A1").Value.NumberValue);
        Assert.Equal(3, At(ws, "C1").Value.NumberValue);
    }

    [Fact]
    public void Undo_OfEditedKeptFormula_RestoresCachedValue()
    {
        var wb = Read(Package([Sheet("<row r=\"1\"><c r=\"A1\"><f>FOOBAR(1)</f><v>42</v></c><c r=\"B1\"><f>A1+1</f><v>43</v></c></row>")]));
        var ws = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=FOOBAR(3)"));
        Assert.Equal(CellValue.Error(CellError.Name), At(ws, "A1").Value);
        wb.Undo.Undo();
        Assert.Equal(42, At(ws, "A1").Value.NumberValue);
        Assert.Equal(43, At(ws, "B1").Value.NumberValue);
    }

    [Fact]
    public void EditingKeptFormulaAwayAndBack_ShowsNameErrorAndDoesNotPersistStaleValue()
    {
        var wb = Read(Package([Sheet("<row r=\"1\"><c r=\"A1\"><f>FOOBAR(1)</f><v>42</v></c></row>")]));
        var ws = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=1+1"));
        Assert.Equal(2, At(ws, "A1").Value.NumberValue);
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=FOOBAR(1)"));
        Assert.Equal(CellValue.Error(CellError.Name), At(ws, "A1").Value);
        Assert.DoesNotContain("<v>2</v>", SheetXml(Write(wb)));
        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(42, At(ws, "A1").Value.NumberValue);
    }

    [Fact]
    public void Write_ArrayMarks_KeepLegacyRangeUntilEdited_AndReuseDynamicArrayMetadata()
    {
        const string rows = """
            <row r="1"><c r="A1"><v>1</v></c><c r="B1"><f t="array" ref="B1:B3">A1:A2*2</f><v>2</v></c><c r="C1" cm="1"><f t="array" ref="C1:C2">_xlfn.SEQUENCE(2)</f><v>1</v></c></row>
            <row r="2"><c r="A2"><v>2</v></c><c r="B2"><v>4</v></c><c r="C2"><v>2</v></c></row><row r="3"><c r="B3" t="e"><v>#N/A</v></c></row>
            """;
        var wb = Read(Package([Sheet(rows.ReplaceLineEndings(""))]));
        var ws = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("D1"), "=SEQUENCE(3)"));
        var xml = SheetXml(Write(wb));
        Assert.Contains("<c r=\"B1\"><f t=\"array\" ref=\"B1:B3\">A1:A2*2</f>", xml);
        Assert.Contains("<c r=\"D1\" cm=\"1\"><f t=\"array\" ref=\"D1:D3\">_xlfn.SEQUENCE(3)</f>", xml);

        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=A1*3"));
        Assert.Contains("<c r=\"B1\"><f>A1*3</f><v>3</v></c>", SheetXml(Write(wb)));
    }

    [Fact]
    public void Write_SheetRenameSwap_RewritesLocalNamesOnce()
    {
        const string names = "<definedNames><definedName name=\"_xlnm.Print_Area\" localSheetId=\"0\">Sheet1!$A$1</definedName>" +
                             "<definedName name=\"_xlnm.Print_Area\" localSheetId=\"1\">Sheet2!$B$2</definedName></definedNames>";
        var wb = Read(Package([Sheet("<row r=\"1\"><c r=\"A1\"><f>Sheet2!B2</f><v>0</v></c></row>"), Sheet("")], workbookExtra: names));
        var (first, second) = (wb.Sheets[0], wb.Sheets[1]);
        wb.Execute(new RenameSheetCommand(first, "T"));
        wb.Execute(new RenameSheetCommand(second, "Sheet1"));
        wb.Execute(new RenameSheetCommand(first, "Sheet2"));
        var written = Write(wb);

        var defined = Xml(written, "xl/workbook.xml").Descendants(M + "definedName").ToList();
        Assert.Equal(["Sheet2!$A$1", "Sheet1!$B$2"], defined.Select(d => d.Value));
        Assert.Equal("Sheet1!B2", At(Read(written).Sheets[0], "A1").Formula);
    }
}
