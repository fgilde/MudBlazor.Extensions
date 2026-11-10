using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit.Abstractions;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class XlsxReadTests(ITestOutputHelper output)
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static string DataPath(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", "Spreadsheet", name);

    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static Cell At(Worksheet ws, string a1) => ws[R(a1)];

    internal static string Sheet(string sheetData, string before = "", string after = "", string rootNs = "") =>
        $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"{Main}\" xmlns:r=\"{Rel}\"{rootNs}>{before}<sheetData>{sheetData}</sheetData>{after}</worksheet>";

    internal static byte[] Package(string[] sheets, string? sharedStrings = null, string? styles = null, string? theme = null,
        string workbookExtra = "", string[]? sheetAttributes = null)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            void Add(string name, string xml)
            {
                using var s = zip.CreateEntry(name).Open();
                var bytes = Encoding.UTF8.GetBytes(xml);
                s.Write(bytes);
            }

            var types = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">")
                .Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>")
                .Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>")
                .Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            var rels = new StringBuilder($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            var list = new StringBuilder();
            for (var i = 0; i < sheets.Length; i++)
            {
                types.Append($"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
                rels.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"{Rel}/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
                list.Append($"<sheet name=\"Sheet{i + 1}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\" {sheetAttributes?.ElementAtOrDefault(i)}/>");
                Add($"xl/worksheets/sheet{i + 1}.xml", sheets[i]);
            }

            if (sharedStrings != null)
            {
                rels.Append($"<Relationship Id=\"rIdS\" Type=\"{Rel}/sharedStrings\" Target=\"sharedStrings.xml\"/>");
                Add("xl/sharedStrings.xml", $"<sst xmlns=\"{Main}\">{sharedStrings}</sst>");
            }

            if (styles != null)
            {
                rels.Append($"<Relationship Id=\"rIdY\" Type=\"{Rel}/styles\" Target=\"/xl/styles.xml\"/>");
                Add("xl/styles.xml", $"<styleSheet xmlns=\"{Main}\">{styles}</styleSheet>");
            }

            if (theme != null)
            {
                rels.Append($"<Relationship Id=\"rIdT\" Type=\"{Rel}/theme\" Target=\"theme/theme1.xml\"/>");
                Add("xl/theme/theme1.xml", theme);
            }

            Add("[Content_Types].xml", types.Append("</Types>").ToString());
            Add("_rels/.rels", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"{Rel}/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Add("xl/_rels/workbook.xml.rels", rels.Append("</Relationships>").ToString());
            Add("xl/workbook.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"{Main}\" xmlns:r=\"{Rel}\"><sheets>{list}</sheets>{workbookExtra}</workbook>");
        }

        return ms.ToArray();
    }

    private static Workbook Read(byte[] package) => XlsxReader.Read(new MemoryStream(package));

    [Fact]
    public void Reads_ValuesFormulasStyles_FromSample()
    {
        using var file = File.OpenRead(DataPath("ExcelSheet.xlsx"));
        var wb = XlsxReader.Read(file);

        var ws = Assert.Single(wb.Sheets);
        Assert.Equal("Report", ws.Name);
        Assert.Equal(100, At(ws, "A2").Value.NumberValue);
        Assert.Equal("John Doe", At(ws, "B2").Value.TextValue);
        Assert.Equal(3600, At(ws, "C2").Value.NumberValue);
        Assert.Equal("\\$\\ #,##0", wb.Styles[At(ws, "C2").StyleId].NumberFormat);
        Assert.True(wb.Styles[At(ws, "A1").StyleId].Font.Bold);
        Assert.Equal(CellHorizontalAlignment.Center, wb.Styles[At(ws, "C1").StyleId].Alignment.Horizontal);
        Assert.Equal(17, ws.DefaultRowHeight);
        Assert.Equal(40, ws.ColumnWidth(0));
        Assert.Equal(100, ws.ColumnWidth(1));
        Assert.NotEqual(0, ws.ColumnStyle(2));

        var formulas = Read(Package([Sheet("<row r=\"1\"><c r=\"A1\"><v>2</v></c><c r=\"B1\"><f>A1*3</f><v>6</v></c><c r=\"C1\" t=\"str\"><f>\"x\"&amp;A1</f><v>x2</v></c></row>")]));
        var fs = formulas.Sheets[0];
        Assert.Equal("A1*3", At(fs, "B1").Formula);
        Assert.Equal(6, At(fs, "B1").Value.NumberValue);
        Assert.Equal("\"x\"&A1", At(fs, "C1").Formula);
        Assert.Equal("x2", At(fs, "C1").Value.TextValue);
    }

    [Fact]
    public void Reads_SharedFormulas_Expanded()
    {
        var rows = new StringBuilder("<row r=\"1\"><c r=\"A1\"><v>10</v></c></row>");
        rows.Append("<row r=\"2\"><c r=\"A2\"><v>1</v></c><c r=\"B2\"><f t=\"shared\" ref=\"B2:C10\" si=\"0\">A2*2+$A$1</f><v>12</v></c><c r=\"C2\"><f t=\"shared\" si=\"0\"/><v>0</v></c></row>");
        for (var r = 3; r <= 10; r++)
            rows.Append($"<row r=\"{r}\"><c r=\"A{r}\"><v>{r - 1}</v></c><c r=\"B{r}\"><f t=\"shared\" si=\"0\"/><v>{(r - 1) * 2 + 10}</v></c></row>");
        var ws = Read(Package([Sheet(rows.ToString())])).Sheets[0];

        Assert.Equal("A2*2+$A$1", At(ws, "B2").Formula);
        Assert.Equal("B2*2+$A$1", At(ws, "C2").Formula);
        Assert.Equal("A3*2+$A$1", At(ws, "B3").Formula);
        Assert.Equal("A10*2+$A$1", At(ws, "B10").Formula);
        Assert.Equal(28, At(ws, "B10").Value.NumberValue);
    }

    [Fact]
    public void Reads_ThemeColorWithTint()
    {
        const string styles = """
            <fonts count="2"><font><sz val="11"/><color theme="1"/><name val="Calibri"/></font><font><b/><sz val="14"/><color rgb="FFFF0000"/><name val="Arial"/></font></fonts>
            <fills count="4"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill>
            <fill><patternFill patternType="solid"><fgColor theme="4" tint="0.39997558519241921"/><bgColor indexed="64"/></patternFill></fill>
            <fill><patternFill patternType="solid"><fgColor theme="4" tint="-0.249977111117893"/></patternFill></fill></fills>
            <borders count="2"><border/><border><left style="thin"><color indexed="10"/></left><bottom style="double"><color theme="5"/></bottom></border></borders>
            <cellXfs count="3"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/><xf numFmtId="0" fontId="1" fillId="2" borderId="1"><alignment horizontal="right" vertical="top" wrapText="1"/></xf><xf numFmtId="0" fontId="0" fillId="3" borderId="0"/></cellXfs>
            """;
        var wb = Read(Package([Sheet("<row r=\"1\"><c r=\"A1\" s=\"1\"><v>1</v></c><c r=\"B1\" s=\"2\"><v>1</v></c></row>")], styles: styles,
            theme: ThemeXml("4472C4", "ED7D31")));
        var ws = wb.Sheets[0];

        var light = wb.Styles[At(ws, "A1").StyleId];
        AssertColorNear("#8EA9DB", light.Fill.Color);
        Assert.Equal("#FF0000", light.Font.Color);
        Assert.Equal("Arial", light.Font.Name);
        Assert.Equal(14, light.Font.Size);
        Assert.Equal(CellBorderStyle.Thin, light.Border.Left.Style);
        Assert.Equal("#FF0000", light.Border.Left.Color);
        Assert.Equal(CellBorderStyle.Double, light.Border.Bottom.Style);
        Assert.Equal(new CellAlignment { Horizontal = CellHorizontalAlignment.Right, Vertical = CellVerticalAlignment.Top, WrapText = true }, light.Alignment);
        Assert.Equal("#ED7D31", light.Border.Bottom.Color);
        AssertColorNear("#2F5597", wb.Styles[At(ws, "B1").StyleId].Fill.Color);
        Assert.Null(wb.Styles[0].Font.Color);
    }

    private static void AssertColorNear(string expected, string actual)
    {
        Assert.NotNull(actual);
        for (var i = 1; i < 7; i += 2)
        {
            var e = Convert.ToInt32(expected.Substring(i, 2), 16);
            var a = Convert.ToInt32(actual.Substring(i, 2), 16);
            Assert.True(Math.Abs(e - a) <= 1, $"expected {expected}, got {actual}");
        }
    }

    private static string ThemeXml(string accent1, string accent2) => $"""
        <a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Office"><a:themeElements><a:clrScheme name="Office">
        <a:dk1><a:sysClr val="windowText" lastClr="000000"/></a:dk1><a:lt1><a:sysClr val="window" lastClr="FFFFFF"/></a:lt1>
        <a:dk2><a:srgbClr val="44546A"/></a:dk2><a:lt2><a:srgbClr val="E7E6E6"/></a:lt2>
        <a:accent1><a:srgbClr val="{accent1}"/></a:accent1><a:accent2><a:srgbClr val="{accent2}"/></a:accent2>
        <a:accent3><a:srgbClr val="A5A5A5"/></a:accent3><a:accent4><a:srgbClr val="FFC000"/></a:accent4>
        <a:accent5><a:srgbClr val="5B9BD5"/></a:accent5><a:accent6><a:srgbClr val="70AD47"/></a:accent6>
        <a:hlink><a:srgbClr val="0563C1"/></a:hlink><a:folHlink><a:srgbClr val="954F72"/></a:folHlink>
        </a:clrScheme></a:themeElements></a:theme>
        """;

    [Fact]
    public void Reads_MergesFreezeHiddenRowsCols()
    {
        const string before = """
            <sheetPr><tabColor rgb="FF00B050"/></sheetPr><dimension ref="A1:D5"/>
            <sheetViews><sheetView workbookViewId="0"><pane xSplit="1" ySplit="2" topLeftCell="B3" activePane="bottomRight" state="frozen"/></sheetView></sheetViews>
            <sheetFormatPr defaultRowHeight="15" defaultColWidth="10.7109375"/>
            <cols><col min="2" max="3" width="20.7109375" customWidth="1"/><col min="4" max="4" width="9.140625" hidden="1"/></cols>
            """;
        const string rows = "<row r=\"1\"><c r=\"A1\"><v>1</v></c></row><row r=\"2\" ht=\"30\" customHeight=\"1\"/><row r=\"3\" hidden=\"1\"><c r=\"A3\"><v>3</v></c></row>";
        var wb = Read(Package([Sheet(rows, before, "<mergeCells count=\"2\"><mergeCell ref=\"A1:B2\"/><mergeCell ref=\"C4:D5\"/></mergeCells>"), Sheet("")],
            sheetAttributes: ["", "state=\"hidden\""],
            workbookExtra: "<definedNames><definedName name=\"Total\">Sheet1!$A$1:$A$3</definedName><definedName name=\"_xlnm.Print_Area\" localSheetId=\"0\">Sheet1!$A$1:$D$5</definedName></definedNames>"));
        var ws = wb.Sheets[0];

        Assert.Equal([CellRange.Parse("A1:B2"), CellRange.Parse("C4:D5")], ws.Merges);
        Assert.Equal(new CellRef(2, 1), ws.FreezeAt);
        Assert.Equal(40, ws.RowHeight(1));
        Assert.True(ws.IsRowHidden(2));
        Assert.False(ws.IsRowHidden(1));
        Assert.Equal(145, ws.ColumnWidth(1));
        Assert.Equal(145, ws.ColumnWidth(2));
        Assert.True(ws.IsColumnHidden(3));
        Assert.Equal(75, ws.DefaultColumnWidth);
        Assert.Equal("#00B050", ws.TabColor);
        Assert.False(ws.Hidden);
        Assert.True(wb.Sheets[1].Hidden);
        Assert.Equal("Sheet1!$A$1:$A$3", wb.DefinedNames["Total"]);
        Assert.False(wb.DefinedNames.ContainsKey("_xlnm.Print_Area"));
    }

    [Fact]
    public void Reads_UnknownWorksheetChildren_Preserved()
    {
        const string cf = "<conditionalFormatting sqref=\"A1:A10\"><cfRule type=\"cellIs\" dxfId=\"0\" priority=\"1\" operator=\"greaterThan\"><formula>5</formula></cfRule></conditionalFormatting>";
        var xml = Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row>", "<dimension ref=\"A1\"/>",
            "<mergeCells count=\"1\"><mergeCell ref=\"B1:C1\"/></mergeCells>" + cf +
            "<pageMargins left=\"0.7\" right=\"0.7\" top=\"0.75\" bottom=\"0.75\" header=\"0.3\" footer=\"0.3\"/><x14ac:foo xmlns:x14ac=\"urn:x\"/>",
            " xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" mc:Ignorable=\"x14ac\"");
        var wb = Read(Package([xml]));

        var part = wb.Package.Sheets.Single(p => p.Worksheet == wb.Sheets[0]);
        Assert.Equal(["dimension", "sheetData", "mergeCells", "conditionalFormatting", "pageMargins", "foo"],
            part.Children.Select(c => c.Name.LocalName));
        Assert.Equal(["pageMargins", "foo"], part.Unmodelled.Select(c => c.Name.LocalName));
        var kept = part.Children[3];
        Assert.Equal(System.Xml.Linq.XNamespace.Get(Main) + "conditionalFormatting", kept.Name);
        Assert.Equal("5", kept.Descendants().Single(d => d.Name.LocalName == "formula").Value);
        Assert.Equal("x14ac", part.Root.Attribute(System.Xml.Linq.XName.Get("Ignorable", "http://schemas.openxmlformats.org/markup-compatibility/2006"))?.Value);
        Assert.Equal("xl/worksheets/sheet1.xml", part.PartName);
        Assert.NotEmpty(wb.Package.Source);
    }

    [Fact]
    public void Reads_CellTypesAndRichSharedStrings()
    {
        const string sst = "<si><t>plain</t></si><si><r><rPr><b/></rPr><t>bold</t></r><r><t xml:space=\"preserve\"> tail</t></r></si><si><t>a_x000D_b</t><rPh sb=\"0\" eb=\"1\"><t>ignored</t></rPh></si>";
        const string rows = """
            <row r="1"><c r="A1" t="s"><v>0</v></c><c r="B1" t="s"><v>1</v></c><c r="C1" t="s"><v>2</v></c>
            <c r="D1" t="b"><v>1</v></c><c r="E1" t="e"><v>#DIV/0!</v></c><c r="F1" t="inlineStr"><is><t>inline</t></is></c>
            <c r="G1" t="d"><v>2024-01-01T12:00:00</v></c><c r="H1" s="1"/><c><v>7</v></c></row>
            <row><c r="A2"><f t="array" ref="A2:A4">SEQUENCE(3)</f><v>1</v></c></row><row r="3"><c r="A3"><v>2</v></c></row>
            """;
        var styles = "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/><xf numFmtId=\"10\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellXfs>";
        var wb = Read(Package([Sheet(rows)], sst, styles));
        var ws = wb.Sheets[0];

        Assert.Equal("plain", At(ws, "A1").Value.TextValue);
        Assert.Equal("bold tail", At(ws, "B1").Value.TextValue);
        Assert.Equal("a\rb", At(ws, "C1").Value.TextValue);
        Assert.Equal(CellValue.Boolean(true), At(ws, "D1").Value);
        Assert.Equal(CellValue.Error(CellError.Div0), At(ws, "E1").Value);
        Assert.Equal("inline", At(ws, "F1").Value.TextValue);
        Assert.Equal(45292.5, At(ws, "G1").Value.NumberValue);
        Assert.Equal("0.00%", wb.Styles[At(ws, "H1").StyleId].NumberFormat);
        Assert.Equal(CellValueKind.Empty, At(ws, "H1").Value.Kind);
        Assert.Equal(7, At(ws, "I1").Value.NumberValue);
        Assert.Equal("SEQUENCE(3)", At(ws, "A2").Formula);
        Assert.Equal(R("A2"), At(ws, "A3").SpillAnchor);

        var part = wb.Package.Sheets[0];
        Assert.Equal(1, part.RichStringCells[R("B1").Key]);
        Assert.Equal(2, part.RichStringCells[R("C1").Key]);
        Assert.False(part.RichStringCells.ContainsKey(R("A1").Key));
        Assert.Contains("<b", wb.Package.RichStrings[1].ToString());

        using var engine = new CalcEngine(wb);
        Assert.Equal(3, At(ws, "A4").Value.NumberValue);
    }

    [Fact]
    public void Reads_Large_100kx30()
    {
        const int rowCount = 100_000, cols = 30;
        var sb = new StringBuilder(rowCount * cols * 30);
        for (var r = 1; r <= rowCount; r++)
        {
            sb.Append("<row r=\"").Append(r).Append("\">");
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
        var package = Package([Sheet(sb.ToString(), $"<dimension ref=\"A1:AD{rowCount}\"/>")], sst.ToString());
        sb.Clear();

        GC.Collect();
        var before = GC.GetTotalMemory(true);
        var watch = Stopwatch.StartNew();
        var wb = Read(package);
        watch.Stop();
        var after = GC.GetTotalMemory(true);
        output.WriteLine($"read {rowCount}x{cols} ({package.Length / 1024} KB zip): {watch.ElapsedMilliseconds} ms, retained {(after - before) / (1024 * 1024)} MB");

        var ws = wb.Sheets[0];
        Assert.Equal(CellRange.FromBounds(0, 0, rowCount - 1, cols - 1), ws.UsedRange);
        Assert.Equal("text 0", ws[new CellRef(rowCount - 1, 0)].Value.TextValue);
        Assert.Equal(rowCount * 29 * 0.5, ws[new CellRef(rowCount - 1, 29)].Value.NumberValue);
    }

    [Fact]
    public void Csv_DetectsSemicolonAndQuotes()
    {
        var csv = "name;note;amount\r\nAlpha;\"a;b\";12\r\n\"Beta \"\"B\"\"\";;3\r\n";
        var ws = CsvFormat.Read(new MemoryStream(Encoding.UTF8.GetBytes(csv))).Sheets[0];

        Assert.Equal("name", At(ws, "A1").Value.TextValue);
        Assert.Equal("a;b", At(ws, "B2").Value.TextValue);
        Assert.Equal(12, At(ws, "C2").Value.NumberValue);
        Assert.Equal("Beta \"B\"", At(ws, "A3").Value.TextValue);
        Assert.Null(At(ws, "B3"));
        Assert.Equal(3, At(ws, "C3").Value.NumberValue);

        using var sample = File.OpenRead(DataPath("sample.csv"));
        var wb = CsvFormat.Read(sample);
        var s = wb.Sheets[0];
        Assert.Equal("customer", At(s, "B1").Value.TextValue);
        Assert.Equal(115, At(s, "E2").Value.NumberValue);
        Assert.Equal(ExcelDate.FromDate(2026, 1, 4), At(s, "A2").Value.NumberValue);
        Assert.Equal(CellRange.Parse("A1:F6"), s.UsedRange);
    }

    [Fact]
    public void Csv_NumberScan_MatchesPlainNumberGrammar()
    {
        var plain = new System.Text.RegularExpressions.Regex(@"^[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?$");
        var thousands = new System.Text.RegularExpressions.Regex(@"^[+-]?[1-9]\d{0,2}(?:\.\d{3})+$");
        string[] tokens =
        [
            "0", "12", "-3", "+4", "1.5", "-1.5", ".5", "-.5", "5.", "1e5", "1E-5", "2.5e+10", "1e", "1e+", "e5", ".", "+", "-", "1.500", "0.500",
            "-1.500", "12.345", "123.456", "1234.567", "1.5000", "1.50", "1.500.000", "1,5", " 1", "1 ", "1.2.3", "0x10", "1e5.5", "00.500", "999.999e1"
        ];
        foreach (var token in tokens)
            foreach (var dotGroups in new[] { false, true })
                Assert.True(CsvFormat.IsPlainNumber(token, dotGroups) == (plain.IsMatch(token) && !(dotGroups && thousands.IsMatch(token))), $"{token} {dotGroups}");
        Assert.Equal(CellValueKind.Text, CsvFormat.Read(new MemoryStream(Encoding.UTF8.GetBytes("١٢"))).Sheets[0][R("A1")].Value.Kind);
    }

    [Fact]
    public void Csv_RoundTripsEmbeddedNewlines()
    {
        var csv = "﻿text,n\r\n\"line1\nline2\",1.5\n\"x,\"\"y\"\"\",2\n";
        var ws = CsvFormat.Read(new MemoryStream(Encoding.UTF8.GetBytes(csv)), ',').Sheets[0];
        Assert.Equal("text", At(ws, "A1").Value.TextValue);
        Assert.Equal("line1\nline2", At(ws, "A2").Value.TextValue);
        Assert.Equal("x,\"y\"", At(ws, "A3").Value.TextValue);

        var target = new MemoryStream();
        CsvFormat.Write(ws, target);
        var again = CsvFormat.Read(new MemoryStream(target.ToArray())).Sheets[0];
        Assert.Equal("line1\nline2", At(again, "A2").Value.TextValue);
        Assert.Equal("x,\"y\"", At(again, "A3").Value.TextValue);
        Assert.Equal(1.5, At(again, "B2").Value.NumberValue);
        Assert.Equal(CellRange.Parse("A1:B3"), again.UsedRange);

        var semi = new MemoryStream();
        CsvFormat.Write(ws, semi, ';');
        var text = Encoding.UTF8.GetString(semi.ToArray()).TrimStart('﻿');
        Assert.StartsWith("text;n\r\n\"line1\nline2\";", text);
    }

    [Fact]
    public void Xls_ReadsValues()
    {
        using var file = File.OpenRead(DataPath("sample.xls"));
        var wb = XlsReader.Read(file);

        Assert.Equal(["Data", "Second"], wb.Sheets.Select(s => s.Name));
        var ws = wb.Sheets[0];
        Assert.Equal("Name", At(ws, "A1").Value.TextValue);
        Assert.Equal(42.5, At(ws, "B2").Value.NumberValue);
        Assert.Equal(85, At(ws, "B3").Value.NumberValue);
        Assert.Equal(CellValue.Boolean(true), At(ws, "C2").Value);
        Assert.Equal("hello", At(wb.Sheets[1], "A1").Value.TextValue);
    }

    private static byte[] Replace(byte[] package, string entry, string xml)
    {
        var ms = new MemoryStream();
        ms.Write(package);
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, true))
        {
            zip.GetEntry(entry)!.Delete();
            using var s = zip.CreateEntry(entry).Open();
            s.Write(Encoding.UTF8.GetBytes(xml));
        }

        return ms.ToArray();
    }

    private static T WithCulture<T>(string name, Func<T> action)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Reads_MalformedColsAndNonFiniteValues()
    {
        const string cols = "<cols><col min=\"1\" max=\"1e10\" hidden=\"1\"/><col min=\"2\" max=\"NaN\" hidden=\"1\"/><col min=\"3\" max=\"3\" width=\"NaN\"/></cols>";
        const string rows = "<row r=\"1\"><c r=\"A1\"><v>NaN</v></c><c r=\"B1\"><v>Infinity</v></c></row><row r=\"2\" ht=\"NaN\"/>";
        var ws = Read(Package([Sheet(rows, cols)])).Sheets[0];

        Assert.True(ws.IsColumnHidden(0));
        Assert.True(ws.IsColumnHidden(1));
        Assert.False(ws.IsColumnHidden(5));
        Assert.Equal(ws.DefaultColumnWidth, ws.ColumnWidth(2));
        Assert.Equal(ws.DefaultRowHeight, ws.RowHeight(1));
        Assert.Equal(CellValue.Error(CellError.Num), At(ws, "A1").Value);
        Assert.Equal(CellValue.Error(CellError.Num), At(ws, "B1").Value);
    }

    [Fact]
    public void Reads_DimensionReservation_CappedByPartSize()
    {
        var ws = Read(Package([Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row>", "<dimension ref=\"A1:XFD1048576\"/>")])).Sheets[0];
        Assert.True(ws.CellCapacity < 1000, $"capacity {ws.CellCapacity}");
    }

    [Fact]
    public void Reads_DimensionReservation_CappedForLargeParts()
    {
        var padding = new string(' ', 4_000_000);
        var ws = Read(Package([Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row>", "<dimension ref=\"A1:XFD1048576\"/>", padding)])).Sheets[0];
        Assert.True(ws.CellCapacity < 100_000, $"capacity {ws.CellCapacity}");
    }

    [Fact]
    public void Reads_OriginalXfKept_ForUnmodelledXfAttributes()
    {
        const string styles = """
            <cellXfs count="4"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" applyProtection="1"><protection locked="0"/></xf>
            <xf numFmtId="10" fontId="0" fillId="0" borderId="0"/>
            <xf numFmtId="10" fontId="0" fillId="0" borderId="0" applyProtection="1"><protection locked="0"/></xf></cellXfs>
            """;
        const string rows = "<row r=\"1\"><c r=\"A1\" s=\"1\"/><c r=\"B1\" s=\"1\"><v>5</v></c><c r=\"C1\" s=\"2\"><v>1</v></c><c r=\"D1\" s=\"3\"><v>1</v></c><c r=\"E1\" s=\"0\"/></row><row r=\"2\" s=\"3\" customFormat=\"1\"/>";
        var wb = Read(Package([Sheet(rows)], styles: styles));
        var ws = wb.Sheets[0];
        var part = wb.Package.Sheets[0];

        Assert.Null(At(ws, "A1"));
        Assert.Equal(0, At(ws, "B1").StyleId);
        Assert.Equal(At(ws, "C1").StyleId, At(ws, "D1").StyleId);
        Assert.Equal([0, 2], wb.Package.CanonicalXf);
        Assert.Equal(1, part.OriginalXf[R("A1").Key]);
        Assert.Equal(1, part.OriginalXf[R("B1").Key]);
        Assert.Equal(3, part.OriginalXf[R("D1").Key]);
        Assert.False(part.OriginalXf.ContainsKey(R("C1").Key));
        Assert.False(part.OriginalXf.ContainsKey(R("E1").Key));
        Assert.Equal(3, part.OriginalRowXf[1]);
    }

    [Fact]
    public void Reads_MalformedInput_ThrowsInvalidDataOrIgnoresBadColors()
    {
        var broken = Replace(Package([Sheet("")]), "xl/_rels/workbook.xml.rels",
            $"<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"{Rel}/worksheet\"/></Relationships>");
        Assert.Throws<InvalidDataException>(() => Read(broken));
        var disposed = new MemoryStream(Package([Sheet("")]));
        disposed.Dispose();
        Assert.Throws<ObjectDisposedException>(() => XlsxReader.Read(disposed));

        const string styles = """
            <colors><indexedColors><rgbColor rgb="FF"/></indexedColors></colors>
            <fonts count="1"><font><color indexed="0"/></font></fonts>
            <fills count="3"><fill><patternFill/></fill><fill><patternFill patternType="gray125"/></fill>
            <fill><patternFill patternType="solid"><fgColor theme="4" tint="0.39997558519241921"/></patternFill></fill></fills>
            <cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/><xf numFmtId="0" fontId="0" fillId="2" borderId="0"/></cellXfs>
            """;
        var wb = Read(Package([Sheet("<row r=\"1\"><c r=\"A1\" s=\"1\"><v>1</v></c></row>", "<sheetPr><tabColor rgb=\"FFZZZZZZ\"/></sheetPr>")],
            styles: styles, theme: ThemeXml("ZZZZZZ", "ED7D31")));
        AssertColorNear("#8EA9DB", wb.Styles[At(wb.Sheets[0], "A1").StyleId].Fill.Color);
        Assert.Null(wb.Sheets[0].TabColor);
    }

    [Fact]
    public void Translate_MovesRelativePartsOnly()
    {
        Assert.Equal("SUM(B2:C3)+$C$1+B:B+2:2+Sheet2!C4+B$1", ReferenceShifter.Translate("SUM(A1:B2)+$C$1+A:A+1:1+Sheet2!B3+A$1", 1, 1));
        Assert.Equal("#REF!+Sheet2!#REF!+$A$1", ReferenceShifter.Translate("A1+Sheet2!A1+$A$1", -1, 0));
    }

    [Fact]
    public void Csv_DeDe_ThousandsDotsAndListSeparator()
    {
        var ws = WithCulture("de-DE", () => CsvFormat.Read(new MemoryStream(Encoding.UTF8.GetBytes("Menge;Preis\r\nA;1.500\r\nB;115.00\r\nC;-12.345.678\r\nD;0.500\r\nE;0.125\r\n")))).Sheets[0];
        Assert.Equal(1500, At(ws, "B2").Value.NumberValue);
        Assert.Equal(115, At(ws, "B3").Value.NumberValue);
        Assert.Equal(-12345678, At(ws, "B4").Value.NumberValue);
        Assert.Equal(0.5, At(ws, "B5").Value.NumberValue);
        Assert.Equal(0.125, At(ws, "B6").Value.NumberValue);

        var headerless = WithCulture("de-DE", () => CsvFormat.Read(new MemoryStream(Encoding.UTF8.GetBytes("1,5;2,5;3,5")))).Sheets[0];
        Assert.Equal(1.5, At(headerless, "A1").Value.NumberValue);
        Assert.Equal(3.5, At(headerless, "C1").Value.NumberValue);

        var ragged = WithCulture("en-US", () => CsvFormat.Read(new MemoryStream(Encoding.UTF8.GetBytes("a;b;c,x\r\nd;e;f;g,y\r\nh;i;j,z\r\n")))).Sheets[0];
        Assert.Equal("c,x", At(ragged, "C1").Value.TextValue);
        Assert.Equal("g,y", At(ragged, "D2").Value.TextValue);
    }
}
