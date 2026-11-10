using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class TableTests
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string TableType = "application/vnd.openxmlformats-officedocument.spreadsheetml.table+xml";
    private static readonly XNamespace M = Main;
    private static readonly XNamespace Pkg = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Types = "http://schemas.openxmlformats.org/package/2006/content-types";

    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static CellRange G(string a1) => CellRange.Parse(a1);

    private static CellValue V(object o) => o switch
    {
        null => CellValue.Empty,
        int i => CellValue.Number(i),
        string s => CellValue.Text(s),
        _ => throw new ArgumentException()
    };

    private static double N(Worksheet ws, string a1) => ws[R(a1)].Value.NumberValue;

    private static (Workbook Wb, Worksheet Ws) Sales()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        object[][] rows = [["Item", "Qty", "Price", "Total"], ["a", 2, 10, null], ["b", 3, 20, null], ["c", 4, 30, null]];
        var block = new CellValue[rows.Length, 4];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < 4; c++)
                block[r, c] = V(rows[r][c]);
        wb.Execute(new SetValuesCommand(ws, R("A1"), block));
        wb.Execute(new CreateTableCommand(ws, G("A1:D4"), true, "TableStyleMedium2"));
        wb.Execute(new RenameTableCommand(ws, "Table1", "Sales"));
        return (wb, ws);
    }

    [Fact]
    public void StructuredRef_ColumnAndThisRow()
    {
        var (wb, ws) = Sales();
        var table = Assert.Single(ws.Tables);
        Assert.Equal(("Sales", "Sales", G("A1:D4"), true, false, "TableStyleMedium2", true, true),
            (table.Name, table.DisplayName, table.Range, table.HeaderRow, table.TotalsRow, table.StyleName, table.BandedRows, table.HasAutoFilter));
        Assert.Equal(["Item", "Qty", "Price", "Total"], table.Columns.Select(c => c.Name));
        Assert.Same(table, wb.FindTable("SALES"));
        Assert.Same(table, ws.TableAt(R("C3")));
        Assert.Null(ws.TableAt(R("E3")));
        wb.Execute(new AddSheetCommand("Other"));
        using var engine = new CalcEngine(wb);

        wb.Execute(new SetFormulaCommand(ws, R("D2"), "=[@Qty]*[@Price]"));
        Assert.Equal(["[@Qty]*[@Price]", "[@Qty]*[@Price]", "[@Qty]*[@Price]"], new[] { "D2", "D3", "D4" }.Select(a => ws[R(a)].Formula));
        Assert.Equal("[@Qty]*[@Price]", ws.Tables[0].Columns[3].CalculatedFormula);
        Assert.Equal([20d, 60, 120], new[] { "D2", "D3", "D4" }.Select(a => N(ws, a)));

        wb.Execute(new SetFormulaCommand(ws, R("F1"), "=SUM(Sales[Total])"));
        wb.Execute(new SetFormulaCommand(ws, R("F2"), "=sum(sales[qty])"));
        var o = wb.Sheets[1];
        wb.Execute(new SetFormulaCommand(o, R("A1"), "=SUM(Sales[[Qty]:[Price]])"));
        wb.Execute(new SetFormulaCommand(o, R("A2"), "=Sales[@Qty]"));
        wb.Execute(new SetFormulaCommand(o, R("A3"), "=Sales[@Qty]"));
        Assert.Equal(200, N(ws, "F1"));
        Assert.Equal(9, N(ws, "F2"));
        Assert.Equal(69, N(o, "A1"));
        Assert.Equal(2, N(o, "A2"));
        Assert.Equal(3, N(o, "A3"));

        wb.Execute(new SetValueCommand(ws, R("B3"), V(5)));
        Assert.Equal(100, N(ws, "D3"));
        Assert.Equal(240, N(ws, "F1"));
        Assert.Equal(5, N(o, "A3"));
        wb.Undo.Undo();
        Assert.Equal(60, N(ws, "D3"));
        Assert.Equal(200, N(ws, "F1"));

        wb.Execute(new SetFormulaCommand(ws, R("F5"), "=[@Qty]"));
        Assert.Equal(CellValue.Error(CellError.Ref), ws[R("F5")].Value);
        wb.Execute(new SetFormulaCommand(o, R("A5"), "=Sales[@Qty]"));
        Assert.Equal(CellValue.Error(CellError.Value), o[R("A5")].Value);
        wb.Execute(new SetFormulaCommand(o, R("A6"), "=SUM(Sales[Nope])"));
        Assert.Equal(CellValue.Error(CellError.Ref), o[R("A6")].Value);
    }

    [Fact]
    public void StructuredRef_SpecialItems()
    {
        Assert.Equal(new StructuredRefNode("Sales", "Qty", null, StructuredItem.ThisRow), FormulaParser.Parse("Sales[[#This Row],[Qty]]"));
        Assert.Equal("Sales[@Qty]", FormulaParser.Format(FormulaParser.Parse("Sales[[#This Row],[Qty]]")));
        Assert.Equal(new StructuredRefNode(null, "Unit Price", null, StructuredItem.ThisRow), FormulaParser.Parse("[@[Unit Price]]"));
        Assert.Equal("[@[Unit Price]]", FormulaParser.Format(new StructuredRefNode(null, "Unit Price", null, StructuredItem.ThisRow)));
        Assert.Equal(new StructuredRefNode("T", "A", "B", StructuredItem.Headers | StructuredItem.Data), FormulaParser.Parse("T[[#Headers],[#Data],[A]:[B]]"));
        Assert.Equal("T[[#Headers],[#Data],[A]:[B]]", FormulaParser.Format(FormulaParser.Parse("T[[#Headers],[#Data],[A]:[B]]")));
        Assert.Equal(new StructuredRefNode("T", null, null, StructuredItem.All), FormulaParser.Parse("T[#All]"));
        Assert.Equal(new StructuredRefNode("T", null, null, StructuredItem.Data), FormulaParser.Parse("T[]"));
        Assert.Equal(new StructuredRefNode("T", "[x]", null, StructuredItem.Data), FormulaParser.Parse("T['[x']]"));
        Assert.Equal("T[['[x']]]", FormulaParser.Format(new StructuredRefNode("T", "[x]", null, StructuredItem.Data)));
        Assert.Equal("T[[#Totals],[A]]", FormulaParser.Format(FormulaParser.Parse("T[[#Totals],[A]]")));
        var token = Assert.Single(FormulaLexer.Tokenize("=Sales[[#Headers],[Qty]]"));
        Assert.Equal(FormulaTokenKind.StructuredReference, token.Kind);
        Assert.False(FormulaParser.TryParse("Sales[[#Headers]", out _, out _));

        var (wb, ws) = Sales();
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetTotalsRowCommand(ws, "Sales", true));
        var table = ws.Tables[0];
        Assert.Equal((G("A1:D5"), true), (table.Range, table.TotalsRow));
        Assert.Equal("Total", ws[R("A5")].Value.TextValue);
        Assert.Equal("sum", table.Columns[3].TotalsFunction);
        Assert.Equal("SUBTOTAL(109,Sales[Total])", ws[R("D5")].Formula);
        wb.Execute(new SetTotalsFunctionCommand(ws, "Sales", "Qty", "average"));
        Assert.Equal("SUBTOTAL(101,Sales[Qty])", ws[R("B5")].Formula);
        Assert.Equal(3, N(ws, "B5"));

        wb.Execute(new SetFormulaCommand(ws, R("F1"), "=ROWS(Sales[#All])"));
        wb.Execute(new SetFormulaCommand(ws, R("F2"), "=ROWS(Sales[#Data])"));
        wb.Execute(new SetFormulaCommand(ws, R("F3"), "=Sales[[#Headers],[Qty]]"));
        wb.Execute(new SetFormulaCommand(ws, R("F4"), "=Sales[[#Totals],[Qty]]"));
        wb.Execute(new SetFormulaCommand(ws, R("F5"), "=COUNTA(Sales[[#Headers],[#Data],[Item]])"));
        wb.Execute(new SetFormulaCommand(ws, R("F6"), "=ROWS(Sales)"));
        wb.Execute(new SetFormulaCommand(ws, R("F7"), "=COLUMNS(Sales[#Totals])"));
        wb.Execute(new SetFormulaCommand(ws, R("F8"), "=Sales[[#This Row],[Price]]"));
        Assert.Equal(5, N(ws, "F1"));
        Assert.Equal(3, N(ws, "F2"));
        Assert.Equal("Qty", ws[R("F3")].Value.TextValue);
        Assert.Equal(3, N(ws, "F4"));
        Assert.Equal(4, N(ws, "F5"));
        Assert.Equal(3, N(ws, "F6"));
        Assert.Equal(4, N(ws, "F7"));
        Assert.Equal(CellValue.Error(CellError.Value), ws[R("F8")].Value);

        wb.Execute(new SetTotalsRowCommand(ws, "Sales", false));
        Assert.Equal((G("A1:D4"), false), (ws.Tables[0].Range, ws.Tables[0].TotalsRow));
        Assert.Null(ws[R("D5")]);
        Assert.Equal(CellValue.Error(CellError.Ref), ws[R("F4")].Value);
        Assert.Equal(4, N(ws, "F1"));
        wb.Undo.Undo();
        Assert.Equal("SUBTOTAL(109,Sales[Total])", ws[R("D5")].Formula);
        Assert.Equal(3, N(ws, "F4"));

        var (blocked, sheet) = Sales();
        blocked.Execute(new SetValueCommand(sheet, R("B5"), V(1)));
        Assert.Throws<InvalidOperationException>(() => blocked.Execute(new SetTotalsRowCommand(sheet, "Sales", true)));
        Assert.False(sheet.Tables[0].TotalsRow);
    }

    [Fact]
    public void RenameColumnHeader_UpdatesFormulas()
    {
        var (wb, ws) = Sales();
        wb.Execute(new AddSheetCommand("Other"));
        var o = wb.Sheets[1];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("D2"), "=[@Qty]*[@Price]"));
        wb.Execute(new SetFormulaCommand(ws, R("F1"), "=SUM(Sales[Qty])"));
        wb.Execute(new SetFormulaCommand(o, R("A1"), "=SUM(Sales[[Qty]:[Price]])+Sales[[#Headers],[Qty]]&\"\""));
        wb.Execute(new SetFormulaCommand(o, R("A2"), "=SUM(Other[Qty])"));
        var undoDepth = wb.Undo.UndoDescription;

        wb.Execute(new SetValueCommand(ws, R("B1"), V("Quantity")));
        Assert.Equal("Quantity", ws.Tables[0].Columns[1].Name);
        Assert.Equal("[@Quantity]*[@Price]", ws[R("D3")].Formula);
        Assert.Equal("[@Quantity]*[@Price]", ws.Tables[0].Columns[3].CalculatedFormula);
        Assert.Equal("SUM(Sales[Quantity])", ws[R("F1")].Formula);
        Assert.Equal("SUM(Sales[[Quantity]:[Price]])+Sales[[#Headers],[Quantity]]&\"\"", o[R("A1")].Formula);
        Assert.Equal("SUM(Other[Qty])", o[R("A2")].Formula);
        Assert.Equal(60, N(ws, "D3"));
        Assert.Equal(9, N(ws, "F1"));

        wb.Undo.Undo();
        Assert.Equal(undoDepth, wb.Undo.UndoDescription);
        Assert.Equal("Qty", ws.Tables[0].Columns[1].Name);
        Assert.Equal("Qty", ws[R("B1")].Value.TextValue);
        Assert.Equal("[@Qty]*[@Price]", ws[R("D3")].Formula);
        Assert.Equal("SUM(Sales[Qty])", ws[R("F1")].Formula);
        Assert.Equal(9, N(ws, "F1"));

        wb.Execute(new SetValueCommand(ws, R("C1"), V("qty")));
        Assert.Equal("qty2", ws.Tables[0].Columns[2].Name);
        Assert.Equal("qty2", ws[R("C1")].Value.TextValue);
        Assert.Equal("[@Qty]*[@qty2]", ws[R("D2")].Formula);
        wb.Execute(new SetValueCommand(ws, R("A1"), V(null)));
        Assert.Equal("Column1", ws.Tables[0].Columns[0].Name);
        Assert.Equal("Column1", ws[R("A1")].Value.TextValue);
    }

    [Fact]
    public void RenameTable_UpdatesFormulas()
    {
        var (wb, ws) = Sales();
        wb.Execute(new AddSheetCommand("Other"));
        var o = wb.Sheets[1];
        wb.Execute(new DefineNameCommand("Quantities", "Sales[Qty]"));
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("D2"), "=[@Qty]*[@Price]"));
        wb.Execute(new SetFormulaCommand(o, R("A2"), "=SUM(Sales[Total])+ROWS(Sales)+SUM(Quantities)+SUM(sales[[#This Row],[Qty]])"));
        Assert.Equal(200 + 3 + 9 + 2, N(o, "A2"));

        wb.Execute(new RenameTableCommand(ws, "sales", "Revenue"));
        Assert.Equal(("Revenue", "Revenue"), (ws.Tables[0].Name, ws.Tables[0].DisplayName));
        Assert.Equal("SUM(Revenue[Total])+ROWS(Revenue)+SUM(Quantities)+SUM(Revenue[[#This Row],[Qty]])", o[R("A2")].Formula);
        Assert.Equal("[@Qty]*[@Price]", ws[R("D2")].Formula);
        Assert.Equal("Revenue[Qty]", wb.DefinedNames["Quantities"]);
        Assert.Equal(214, N(o, "A2"));
        Assert.Null(wb.FindTable("Sales"));

        wb.Undo.Undo();
        Assert.Equal("Sales", ws.Tables[0].Name);
        Assert.Equal("SUM(Sales[Total])+ROWS(Sales)+SUM(Quantities)+SUM(sales[[#This Row],[Qty]])", o[R("A2")].Formula);
        Assert.Equal("Sales[Qty]", wb.DefinedNames["Quantities"]);
        Assert.Equal(214, N(o, "A2"));
    }

    [Fact]
    public void InsertRowInside_GrowsTable_FillsCalculatedColumn()
    {
        var (wb, ws) = Sales();
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("D2"), "=[@Qty]*[@Price]+$F$1-F$1"));
        wb.Execute(new SetFormulaCommand(ws, R("G1"), "=SUM(Sales[Total])"));
        Assert.Equal(200, N(ws, "G1"));

        wb.Execute(new InsertRowsCommand(ws, 2, 1));
        Assert.Equal(G("A1:D5"), ws.Tables[0].Range);
        Assert.Equal("[@Qty]*[@Price]+$F$1-F$1", ws[R("D3")].Formula);
        Assert.Equal("[@Qty]*[@Price]+$F$1-F$1", ws[R("D4")].Formula);
        Assert.Equal(0, N(ws, "D3"));
        wb.Execute(new SetValueCommand(ws, R("B3"), V(1)));
        wb.Execute(new SetValueCommand(ws, R("C3"), V(7)));
        Assert.Equal(7, N(ws, "D3"));
        Assert.Equal(207, N(ws, "G1"));

        wb.Undo.Undo();
        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(G("A1:D4"), ws.Tables[0].Range);
        Assert.Equal(200, N(ws, "G1"));

        wb.Execute(new InsertRowsCommand(ws, 4, 1));
        Assert.Equal(G("A1:D4"), ws.Tables[0].Range);
        Assert.Null(ws[R("D5")]);
        wb.Undo.Undo();
        wb.Execute(new InsertRowsCommand(ws, 0, 2));
        Assert.Equal(G("A3:D6"), ws.Tables[0].Range);
        Assert.Equal(200, N(ws, "G3"));
        wb.Undo.Undo();

        wb.Execute(new InsertColumnsCommand(ws, 2, 1));
        Assert.Equal(G("A1:E4"), ws.Tables[0].Range);
        Assert.Equal(["Item", "Qty", "Column1", "Price", "Total"], ws.Tables[0].Columns.Select(c => c.Name));
        Assert.Equal("Column1", ws[R("C1")].Value.TextValue);
        Assert.Equal(200, N(ws, "H1"));
        wb.Undo.Undo();
        Assert.Equal(["Item", "Qty", "Price", "Total"], ws.Tables[0].Columns.Select(c => c.Name));

        wb.Execute(new DeleteRowsCommand(ws, 1, 1));
        Assert.Equal(G("A1:D3"), ws.Tables[0].Range);
        Assert.Equal(180, N(ws, "G1"));
        wb.Execute(new DeleteColumnsCommand(ws, 1, 1));
        Assert.Equal(["Item", "Price", "Total"], ws.Tables[0].Columns.Select(c => c.Name));
        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(G("A1:D4"), ws.Tables[0].Range);
        Assert.Equal(["Item", "Qty", "Price", "Total"], ws.Tables[0].Columns.Select(c => c.Name));
        wb.Execute(new DeleteRowsCommand(ws, 0, 1));
        Assert.Empty(ws.Tables);
        wb.Undo.Undo();
        Assert.Single(ws.Tables);
    }

    [Fact]
    public void CreateTable_NewPart_ContentTypeAndRels()
    {
        var (wb, ws) = Sales();
        wb.Execute(new SetFormulaCommand(ws, R("D2"), "=[@Qty]*[@Price]"));
        wb.Execute(new SetTableOptionsCommand(ws, "Sales", bandedColumns: true, firstColumn: true));
        var written = Write(wb);

        var types = Xml(written, "[Content_Types].xml").Root!.Elements(Types + "Override").ToList();
        Assert.Contains(types, t => (string)t.Attribute("PartName") == "/xl/tables/table1.xml" && (string)t.Attribute("ContentType") == TableType);
        var rel = Assert.Single(Xml(written, "xl/worksheets/_rels/sheet1.xml.rels").Root!.Elements(Pkg + "Relationship"));
        Assert.Equal((Rel + "/table", "../tables/table1.xml"), ((string)rel.Attribute("Type"), (string)rel.Attribute("Target")));
        var sheet = Xml(written, "xl/worksheets/sheet1.xml").Root!;
        var parts = sheet.Element(M + "tableParts")!;
        Assert.Equal("1", (string)parts.Attribute("count"));
        Assert.Equal((string)rel.Attribute("Id"), (string)parts.Element(M + "tablePart")!.Attribute(XName.Get("id", Rel)));
        Assert.Equal("tableParts", sheet.Elements().Last().Name.LocalName);
        Assert.Equal("Sales[[#This Row],[Qty]]*Sales[[#This Row],[Price]]",
            sheet.Descendants(M + "c").First(c => (string)c.Attribute("r") == "D3").Element(M + "f")!.Value);

        var table = Xml(written, "xl/tables/table1.xml").Root!;
        Assert.Equal(M + "table", table.Name);
        Assert.Equal(("1", "Sales", "Sales", "A1:D4"), ((string)table.Attribute("id"), (string)table.Attribute("name"), (string)table.Attribute("displayName"), (string)table.Attribute("ref")));
        Assert.Equal(["autoFilter", "tableColumns", "tableStyleInfo"], table.Elements().Select(e => e.Name.LocalName));
        Assert.Equal("A1:D4", (string)table.Element(M + "autoFilter")!.Attribute("ref"));
        var columns = table.Element(M + "tableColumns")!;
        Assert.Equal("4", (string)columns.Attribute("count"));
        Assert.Equal(["1", "2", "3", "4"], columns.Elements().Select(c => (string)c.Attribute("id")));
        Assert.Equal(["Item", "Qty", "Price", "Total"], columns.Elements().Select(c => (string)c.Attribute("name")));
        Assert.Equal("Sales[[#This Row],[Qty]]*Sales[[#This Row],[Price]]", columns.Elements().Last().Element(M + "calculatedColumnFormula")!.Value);
        var style = table.Element(M + "tableStyleInfo")!;
        Assert.Equal(("TableStyleMedium2", "1", "0", "1", "1"), ((string)style.Attribute("name"), (string)style.Attribute("showFirstColumn"),
            (string)style.Attribute("showLastColumn"), (string)style.Attribute("showRowStripes"), (string)style.Attribute("showColumnStripes")));

        var again = Read(written);
        var back = Assert.Single(again.Sheets[0].Tables);
        Assert.Equal(("Sales", G("A1:D4"), "TableStyleMedium2", true, true, false), (back.Name, back.Range, back.StyleName, back.BandedColumns, back.FirstColumn, back.LastColumn));
        Assert.Equal("Sales[@Qty]*Sales[@Price]", back.Columns[3].CalculatedFormula);
        Assert.Equal("Sales[@Qty]*Sales[@Price]", again.Sheets[0][R("D2")].Formula);

        again.Execute(new AddSheetCommand("Second"));
        var second = again.Sheets[1];
        again.Execute(new SetValuesCommand(second, R("B2"), new[,] { { V("X"), V("Y") }, { V(1), V(2) } }));
        again.Execute(new CreateTableCommand(second, G("B2:C3"), true, "TableStyleLight1"));
        Assert.Equal("Table1", second.Tables[0].Name);
        var next = Write(again);
        Assert.Equal(Entry(written, "xl/tables/table1.xml"), Entry(next, "xl/tables/table1.xml"));
        Assert.Equal(Entry(written, "xl/worksheets/_rels/sheet1.xml.rels"), Entry(next, "xl/worksheets/_rels/sheet1.xml.rels"));
        var t2 = Xml(next, "xl/tables/table2.xml").Root!;
        Assert.Equal(("2", "Table1", "B2:C3"), ((string)t2.Attribute("id"), (string)t2.Attribute("name"), (string)t2.Attribute("ref")));
        Assert.Contains(Xml(next, "[Content_Types].xml").Root!.Elements(Types + "Override"), t => (string)t.Attribute("PartName") == "/xl/tables/table2.xml");
        var secondRel = Assert.Single(Xml(next, "xl/worksheets/_rels/sheet2.xml.rels").Root!.Elements(Pkg + "Relationship"));
        Assert.Equal("../tables/table2.xml", (string)secondRel.Attribute("Target"));

        var noHeader = Workbook.Create();
        noHeader.Execute(new SetValuesCommand(noHeader.Sheets[0], R("A1"), new[,] { { V(1), V(2) } }));
        noHeader.Execute(new CreateTableCommand(noHeader.Sheets[0], G("A1:B1"), false, "TableStyleLight1"));
        var plain = noHeader.Sheets[0].Tables[0];
        Assert.Equal((false, false), (plain.HeaderRow, plain.HasAutoFilter));
        Assert.Equal(["Column1", "Column2"], plain.Columns.Select(c => c.Name));
        var plainXml = Xml(Write(noHeader), "xl/tables/table1.xml").Root!;
        Assert.Equal("0", (string)plainXml.Attribute("headerRowCount"));
        Assert.Null(plainXml.Element(M + "autoFilter"));

        wb.Execute(new ConvertToRangeCommand(ws, "Sales"));
        var removed = Write(wb);
        Assert.DoesNotContain("xl/tables/table1.xml", Names(removed));
        Assert.Null(Xml(removed, "xl/worksheets/sheet1.xml").Root!.Element(M + "tableParts"));
        Assert.DoesNotContain(Xml(removed, "[Content_Types].xml").Root!.Elements(Types + "Override"), t => ((string)t.Attribute("PartName")!).Contains("table"));
    }

    private const string TablePart = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <table xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" mc:Ignorable="xr xr3" xmlns:xr="http://schemas.microsoft.com/office/spreadsheetml/2014/revision" xmlns:xr3="http://schemas.microsoft.com/office/spreadsheetml/2016/revision3" id="3" xr:uid="{AAA}" name="Table1" displayName="Table1" ref="A1:D4" totalsRowShown="0" headerRowDxfId="5"><autoFilter ref="A1:D4" xr:uid="{BBB}"/><tableColumns count="4"><tableColumn id="1" xr3:uid="{C1}" name="Item"/><tableColumn id="2" xr3:uid="{C2}" name="Qty" dataDxfId="4"/><tableColumn id="3" xr3:uid="{C3}" name="Price"/><tableColumn id="4" xr3:uid="{C4}" name="Total"><calculatedColumnFormula>Table1[[#This Row],[Qty]]*Table1[[#This Row],[Price]]</calculatedColumnFormula></tableColumn></tableColumns><tableStyleInfo name="TableStyleLight9" showFirstColumn="0" showLastColumn="0" showRowStripes="1" showColumnStripes="0"/><extLst><ext uri="{504A1905-F514-4f6f-8877-14C23A59335A}" xmlns:x14="http://schemas.microsoft.com/office/spreadsheetml/2009/9/main"><x14:table altText="Sales"/></ext></extLst></table>
        """;

    private static string Str(string cell, string text) => $"<c r=\"{cell}\" t=\"inlineStr\"><is><t>{text}</t></is></c>";

    private static string Product(int row) => $"<c r=\"D{row}\"><f>Table1[[#This Row],[Qty]]*Table1[[#This Row],[Price]]</f><v>{row * 10}</v></c>";

    private static byte[] TablePackage()
    {
        var data = $"<row r=\"1\">{Str("A1", "Item")}{Str("B1", "Qty")}{Str("C1", "Price")}{Str("D1", "Total")}<c r=\"F1\"><f>SUM(Table1[Total])</f><v>0</v></c></row>"
                   + string.Concat(Enumerable.Range(2, 3).Select(r => $"<row r=\"{r}\">{Str($"A{r}", "x")}<c r=\"B{r}\"><v>{r}</v></c><c r=\"C{r}\"><v>10</v></c>{Product(r)}</row>"));
        var package = XlsxReadTests.Package([XlsxReadTests.Sheet(data, "", "<pageMargins left=\"0.7\" right=\"0.7\" top=\"0.75\" bottom=\"0.75\" header=\"0.3\" footer=\"0.3\"/><tableParts count=\"1\"><tablePart r:id=\"rId1\"/></tableParts>")]);
        using var ms = new MemoryStream();
        ms.Write(package);
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, true))
        {
            void Put(string name, string content)
            {
                zip.GetEntry(name)?.Delete();
                using var s = zip.CreateEntry(name).Open();
                s.Write(Encoding.UTF8.GetBytes(content));
            }

            string types;
            using (var reader = new StreamReader(zip.GetEntry("[Content_Types].xml")!.Open())) types = reader.ReadToEnd();
            Put("[Content_Types].xml", types.Replace("</Types>", $"<Override PartName=\"/xl/tables/table1.xml\" ContentType=\"{TableType}\"/></Types>"));
            Put("xl/worksheets/_rels/sheet1.xml.rels",
                $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"{Pkg.NamespaceName}\"><Relationship Id=\"rId1\" Type=\"{Rel}/table\" Target=\"../tables/table1.xml\"/></Relationships>");
            Put("xl/tables/table1.xml", TablePart.ReplaceLineEndings(""));
        }

        return ms.ToArray();
    }

    [Fact]
    public void Xlsx_Table_RoundTrip_UnknownAttributesKept()
    {
        var package = TablePackage();
        var wb = Read(package);
        var ws = wb.Sheets[0];
        Assert.False(wb.HasUnshiftedReferences);
        var table = Assert.Single(ws.Tables);
        Assert.Equal(("Table1", G("A1:D4"), "TableStyleLight9", true, false, true), (table.Name, table.Range, table.StyleName, table.BandedRows, table.BandedColumns, table.HasAutoFilter));
        Assert.Equal(["Item", "Qty", "Price", "Total"], table.Columns.Select(c => c.Name));
        Assert.Equal("Table1[@Qty]*Table1[@Price]", table.Columns[3].CalculatedFormula);
        Assert.Equal("Table1[@Qty]*Table1[@Price]", ws[R("D2")].Formula);
        using var engine = new CalcEngine(wb);
        Assert.Equal(30, N(ws, "D3"));
        Assert.Equal(90, N(ws, "F1"));

        var same = Write(wb);
        Assert.Equal(Entry(package, "xl/tables/table1.xml"), Entry(same, "xl/tables/table1.xml"));
        Assert.Equal(Entry(package, "xl/worksheets/_rels/sheet1.xml.rels"), Entry(same, "xl/worksheets/_rels/sheet1.xml.rels"));

        wb.Execute(new InsertRowsCommand(ws, 2, 1));
        var written = Write(wb);
        Assert.Equal(Entry(package, "xl/worksheets/_rels/sheet1.xml.rels"), Entry(written, "xl/worksheets/_rels/sheet1.xml.rels"));
        Assert.Equal(["/xl/tables/table1.xml"], Xml(written, "[Content_Types].xml").Root!.Elements(Types + "Override")
            .Where(t => (string)t.Attribute("ContentType") == TableType).Select(t => (string)t.Attribute("PartName")));
        var original = XDocument.Parse(TablePart.ReplaceLineEndings("")).Root!;
        var root = Xml(written, "xl/tables/table1.xml").Root!;
        XNamespace xr = "http://schemas.microsoft.com/office/spreadsheetml/2014/revision";
        XNamespace xr3 = "http://schemas.microsoft.com/office/spreadsheetml/2016/revision3";
        Assert.Equal(("3", "{AAA}", "A1:D5", "5", "0"), ((string)root.Attribute("id"), (string)root.Attribute(xr + "uid"), (string)root.Attribute("ref"),
            (string)root.Attribute("headerRowDxfId"), (string)root.Attribute("totalsRowShown")));
        Assert.Equal(("A1:D5", "{BBB}"), ((string)root.Element(M + "autoFilter")!.Attribute("ref"), (string)root.Element(M + "autoFilter")!.Attribute(xr + "uid")));
        var columns = root.Element(M + "tableColumns")!.Elements().ToList();
        Assert.Equal(["{C1}", "{C2}", "{C3}", "{C4}"], columns.Select(c => (string)c.Attribute(xr3 + "uid")));
        Assert.Equal("4", (string)columns[1].Attribute("dataDxfId"));
        Assert.Equal("Table1[[#This Row],[Qty]]*Table1[[#This Row],[Price]]", columns[3].Element(M + "calculatedColumnFormula")!.Value);
        Assert.True(XNode.DeepEquals(original.Element(M + "extLst"), root.Element(M + "extLst")));
        Assert.True(XNode.DeepEquals(original.Element(M + "tableStyleInfo"), root.Element(M + "tableStyleInfo")));
        Assert.Equal(original.Elements().Select(e => e.Name), root.Elements().Select(e => e.Name));

        var sheet = Xml(written, "xl/worksheets/sheet1.xml").Root!;
        Assert.Equal("rId1", (string)sheet.Element(M + "tableParts")!.Element(M + "tablePart")!.Attribute(XName.Get("id", Rel)));
        Assert.Equal(["pageMargins", "tableParts"], sheet.Elements().Select(e => e.Name.LocalName).SkipWhile(n => n != "pageMargins"));
        var formulas = sheet.Descendants(M + "f").Select(f => f.Value).ToList();
        Assert.Contains("SUM(Table1[Total])", formulas);
        Assert.Equal(4, formulas.Count(f => f == "Table1[[#This Row],[Qty]]*Table1[[#This Row],[Price]]"));

        var again = Read(written).Sheets[0].Tables[0];
        Assert.Equal(G("A1:D5"), again.Range);
    }

    [Fact]
    public void ConvertToRange_FormulasBecomeA1()
    {
        var (wb, ws) = Sales();
        wb.Execute(new AddSheetCommand("Other"));
        var o = wb.Sheets[1];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("D2"), "=[@Qty]*[@Price]"));
        wb.Execute(new SetFormulaCommand(ws, R("F1"), "=SUM(Sales[Total])&Sales[[#Headers],[Qty]]"));
        wb.Execute(new SetFormulaCommand(o, R("A1"), "=SUM(Sales[[Qty]:[Price]])+ROWS(Sales)+COUNTA(Sales[#All])"));
        Assert.Equal(("200Qty", 88d), (ws[R("F1")].Value.TextValue, N(o, "A1")));

        wb.Execute(new ConvertToRangeCommand(ws, "Sales"));
        Assert.Empty(ws.Tables);
        Assert.Equal("$B3*$C3", ws[R("D3")].Formula);
        Assert.Equal("SUM($D$2:$D$4)&$B$1", ws[R("F1")].Formula);
        Assert.Equal("SUM(Sheet1!$B$2:$C$4)+ROWS(Sheet1!$A$2:$D$4)+COUNTA(Sheet1!$A$1:$D$4)", o[R("A1")].Formula);
        Assert.Equal(("200Qty", 88d), (ws[R("F1")].Value.TextValue, N(o, "A1")));
        Assert.Equal(60, N(ws, "D3"));

        wb.Undo.Undo();
        Assert.Equal("Sales", Assert.Single(ws.Tables).Name);
        Assert.Equal("[@Qty]*[@Price]", ws[R("D3")].Formula);
        Assert.Equal("SUM(Sales[[Qty]:[Price]])+ROWS(Sales)+COUNTA(Sales[#All])", o[R("A1")].Formula);
        Assert.Equal(("200Qty", 88d), (ws[R("F1")].Value.TextValue, N(o, "A1")));
    }

    [Fact]
    public void TableName_Validation()
    {
        var (wb, ws) = Sales();
        wb.Execute(new SetValuesCommand(ws, R("F1"), new[,] { { V("X") }, { V(1) } }));
        wb.Execute(new CreateTableCommand(ws, G("F1:F2"), true, "TableStyleLight1"));
        wb.Execute(new DefineNameCommand("Rate", "Sheet1!$A$1"));
        foreach (var bad in new[] { "My Table", "", "SALES", "A1", "AB12", "R1C1", "r2c3", "R", "c", "1abc", "Rate", "a-b" })
            Assert.Throws<ArgumentException>(() => wb.Execute(new RenameTableCommand(ws, "Table1", bad)));
        Assert.Equal(["Sales", "Table1"], ws.Tables.Select(t => t.Name));
        wb.Execute(new RenameTableCommand(ws, "Table1", "_Rates.2024"));
        wb.Execute(new RenameTableCommand(ws, "_Rates.2024", "_rates.2024"));
        Assert.Equal("_rates.2024", ws.Tables[1].Name);
        Assert.Throws<ArgumentException>(() => wb.Execute(new RenameTableCommand(ws, "Missing", "Other")));

        Assert.Throws<InvalidOperationException>(() => wb.Execute(new CreateTableCommand(ws, G("D4:E6"), true, "TableStyleLight1")));
        wb.Execute(new MergeCommand(ws, G("H1:I1")));
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new CreateTableCommand(ws, G("H1:I3"), true, "TableStyleLight1")));
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new ResizeTableCommand(ws, "Sales", G("A1:F4"))));
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new ResizeTableCommand(ws, "Sales", G("A2:D4"))));
        wb.Execute(new ResizeTableCommand(ws, "Sales", G("A1:E6")));
        Assert.Equal((G("A1:E6"), "Column1"), (ws.Tables[0].Range, ws.Tables[0].Columns[4].Name));
        Assert.Equal("Column1", ws[R("E1")].Value.TextValue);
        wb.Undo.Undo();
        Assert.Equal(G("A1:D4"), ws.Tables[0].Range);
        Assert.Null(ws[R("E1")]);
        Assert.Equal(2, wb.Tables.Count);
    }

    [Fact]
    public void HeaderEdits_AnyCommand_KeepColumnNamesInSync()
    {
        var (wb, ws) = Sales();
        wb.Execute(new AddSheetCommand("Other"));
        var o = wb.Sheets[1];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("D2"), "=[@Qty]*[@Price]"));
        wb.Execute(new SetFormulaCommand(ws, R("F1"), "=SUM(Sales[Qty])"));
        wb.Execute(new SetFormulaCommand(o, R("A1"), "=SUM(Sales[Price])"));

        wb.Execute(new SetValuesCommand(ws, R("B1"), new[,] { { V("Units"), V("Cost") } }));
        Assert.Equal(["Item", "Units", "Cost", "Total"], ws.Tables[0].Columns.Select(c => c.Name));
        Assert.Equal("[@Units]*[@Cost]", ws[R("D3")].Formula);
        Assert.Equal("SUM(Sales[Units])", ws[R("F1")].Formula);
        Assert.Equal("SUM(Sales[Cost])", o[R("A1")].Formula);
        Assert.Equal(9, N(ws, "F1"));
        Assert.Equal(60, N(o, "A1"));
        wb.Undo.Undo();
        Assert.Equal(["Item", "Qty", "Price", "Total"], ws.Tables[0].Columns.Select(c => c.Name));
        Assert.Equal(("Qty", "[@Qty]*[@Price]", "SUM(Sales[Price])"), (ws[R("B1")].Value.TextValue, ws[R("D3")].Formula, o[R("A1")].Formula));
        wb.Undo.Redo();
        Assert.Equal(("Units", "SUM(Sales[Units])"), (ws.Tables[0].Columns[1].Name, ws[R("F1")].Formula));
        wb.Undo.Undo();

        wb.Execute(new ClearCommand(ws, G("A1")));
        Assert.Equal(("Column1", "Column1"), (ws.Tables[0].Columns[0].Name, ws[R("A1")].Value.TextValue));

        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=\"Co\"&\"st\""));
        Assert.Equal(("Cost", null, "Cost"), (ws[R("C1")].Value.TextValue, ws[R("C1")].Formula, ws.Tables[0].Columns[2].Name));
        Assert.Equal("SUM(Sales[Cost])", o[R("A1")].Formula);

        wb.Execute(new SetValueCommand(ws, R("A1"), V(2024)));
        Assert.Equal((CellValue.Text("2024"), "2024"), (ws[R("A1")].Value, ws.Tables[0].Columns[0].Name));

        wb.Execute(new SetValuesCommand(ws, R("A1"), new[,] { { V("Total"), V("Total") } }));
        Assert.Equal(["Total2", "Total3", "Cost", "Total"], ws.Tables[0].Columns.Select(c => c.Name));
        Assert.Equal(["Total2", "Total3"], new[] { "A1", "B1" }.Select(a => ws[R(a)].Value.TextValue));
        wb.Undo.Undo();

        wb.Execute(new SortCommand(ws, G("A1:A4"), [new SortKey(0, Descending: true)], hasHeader: false));
        Assert.Equal(("c", "c"), (ws[R("A1")].Value.TextValue, ws.Tables[0].Columns[0].Name));
        Assert.Equal("2024", ws[R("A4")].Value.TextValue);
    }

    [Fact]
    public void HeaderSync_RunsOnceAtTransactionCommit()
    {
        var (wb, ws) = Sales();
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("F1"), "=SUM(Sales[Qty])"));
        var events = 0;
        wb.Changed += (_, _) => events++;
        using (wb.Undo.BeginTransaction("Edit"))
        {
            wb.Execute(new SetValueCommand(ws, R("B1"), V("Units")));
            Assert.Equal("Qty", ws.Tables[0].Columns[1].Name);
            wb.Execute(new SetValueCommand(ws, R("B2"), V(5)));
            Assert.Equal("Qty", ws.Tables[0].Columns[1].Name);
        }

        Assert.Equal(1, events);
        Assert.Equal(("Units", "SUM(Sales[Units])", 12d), (ws.Tables[0].Columns[1].Name, ws[R("F1")].Formula, N(ws, "F1")));
        wb.Undo.Undo();
        Assert.Equal(("Qty", "Qty", "SUM(Sales[Qty])", 9d), (ws[R("B1")].Value.TextValue, ws.Tables[0].Columns[1].Name, ws[R("F1")].Formula, N(ws, "F1")));
        wb.Undo.Redo();
        Assert.Equal(("Units", "SUM(Sales[Units])", 12d), (ws.Tables[0].Columns[1].Name, ws[R("F1")].Formula, N(ws, "F1")));
    }

    [Fact]
    public void Writer_SafetyNet_HeaderCellsBecomeColumnNames()
    {
        var (wb, ws) = Sales();
        ws[R("B1")].Value = CellValue.Number(7);
        ws[R("C1")].Formula = "\"Q\"&\"ty\"";
        ws[R("C1")].Value = CellValue.Text("Qty");
        ws.Modified = true;

        var written = Write(wb);
        var columns = Xml(written, "xl/tables/table1.xml").Root!.Element(M + "tableColumns")!.Elements().Select(c => (string)c.Attribute("name"));
        Assert.Equal(["Item", "7", "Qty", "Total"], columns);
        var cells = Xml(written, "xl/worksheets/sheet1.xml").Root!.Descendants(M + "c").ToDictionary(c => (string)c.Attribute("r")!);
        Assert.Equal("s", (string)cells["B1"].Attribute("t"));
        Assert.Null(cells["C1"].Element(M + "f"));
        var again = Read(written).Sheets[0];
        Assert.Equal(["Item", "7", "Qty", "Total"], again.Tables[0].Columns.Select(c => c.Name));
        Assert.Equal(["Item", "7", "Qty", "Total"], new[] { "A1", "B1", "C1", "D1" }.Select(a => again[R(a)].Value.TextValue));
    }

    [Fact]
    public void DeleteHeaderRowOrColumn_StructuredRefsBecomeRefErrors()
    {
        var (wb, ws) = Sales();
        wb.Execute(new AddSheetCommand("Other"));
        var o = wb.Sheets[1];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("D2"), "=[@Qty]*[@Price]"));
        wb.Execute(new SetFormulaCommand(ws, R("F1"), "=SUM(Sales[Price])"));
        wb.Execute(new SetFormulaCommand(o, R("A1"), "=SUM(Sales[Qty])"));
        wb.Execute(new SetFormulaCommand(o, R("A2"), "=ROWS(Sales)+SUM(Sales[[Qty]:[Price]])"));
        wb.Execute(new SetFormulaCommand(o, R("A3"), "=SUM(Sales[Total])"));

        wb.Execute(new DeleteColumnsCommand(ws, 1, 1));
        Assert.Equal("#REF!*[@Price]", ws[R("C3")].Formula);
        Assert.Equal("SUM(Sales[Price])", ws[R("E1")].Formula);
        Assert.Equal(("SUM(#REF!)", "ROWS(Sales)+SUM(#REF!)", "SUM(Sales[Total])"), (o[R("A1")].Formula, o[R("A2")].Formula, o[R("A3")].Formula));
        Assert.Equal(CellValue.Error(CellError.Ref), o[R("A1")].Value);
        wb.Undo.Undo();
        Assert.Equal(("SUM(Sales[Qty])", "[@Qty]*[@Price]"), (o[R("A1")].Formula, ws[R("D3")].Formula));
        Assert.Equal(9, N(o, "A1"));

        wb.Execute(new DeleteRowsCommand(ws, 0, 1));
        Assert.Empty(ws.Tables);
        Assert.Equal("#REF!*#REF!", ws[R("D2")].Formula);
        Assert.Equal(("SUM(#REF!)", "ROWS(#REF!)+SUM(#REF!)", "SUM(#REF!)"), (o[R("A1")].Formula, o[R("A2")].Formula, o[R("A3")].Formula));
        Assert.Equal(CellValue.Error(CellError.Ref), o[R("A3")].Value);
        wb.Undo.Undo();
        Assert.Equal(("SUM(Sales[Total])", "[@Qty]*[@Price]"), (o[R("A3")].Formula, ws[R("D3")].Formula));
        Assert.Equal(200, N(o, "A3"));
    }

    [Fact]
    public void ExternalWorkbookPrefix_IsNotAStructuredReference()
    {
        Assert.Throws<FormatException>(() => FormulaLexer.Tokenize("[1]Sheet1!A1"));
        Assert.Throws<FormatException>(() => FormulaLexer.Tokenize("Sales[Qty]x"));
        var (wb, ws) = Sales();
        wb.Execute(new SetFormulaCommand(ws, R("D2"), "=[1]Data!A1+[@Qty]"));
        wb.Execute(new SetFormulaCommand(ws, R("F2"), "=[1]Sheet1!A1+[2]!Name+Sales[@Qty]"));
        var written = Write(wb);
        var f = Xml(written, "xl/worksheets/sheet1.xml").Root!.Descendants(M + "c").ToDictionary(c => (string)c.Attribute("r")!, c => c.Element(M + "f")?.Value);
        Assert.Equal("[1]Data!A1+Sales[[#This Row],[Qty]]", f["D2"]);
        Assert.Equal("[1]Sheet1!A1+[2]!Name+Sales[[#This Row],[Qty]]", f["F2"]);
        Assert.Equal("[1]Data!A1+Sales[@Qty]", Read(written).Sheets[0][R("D2")].Formula);
    }

    [Fact]
    public void AutoFilter_NeverOverlapsTable_NamesNeverClash()
    {
        var (wb, ws) = Sales();
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new SetAutoFilterCommand(ws, G("C1:F4"))));
        wb.Execute(new SetAutoFilterCommand(ws, G("F1:G4")));
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new ResizeTableCommand(ws, "Sales", G("A1:F4"))));
        Assert.Equal(G("A1:D4"), ws.Tables[0].Range);

        Assert.Throws<ArgumentException>(() => wb.Execute(new DefineNameCommand("SALES", "Sheet1!$A$1")));
        wb.Execute(new DefineNameCommand("Rate", "Sheet1!$A$1"));
        Assert.Throws<ArgumentException>(() => wb.Execute(new RenameTableCommand(ws, "Sales", "RATE")));
        wb.Execute(new DefineNameCommand("Table1", "Sheet1!$A$1"));
        wb.Execute(new SetValuesCommand(ws, R("H1"), new[,] { { V("X") }, { V(1) } }));
        wb.Execute(new CreateTableCommand(ws, G("H1:H2"), true, "TableStyleLight1"));
        Assert.Equal("Table2", ws.Tables[1].Name);
    }

    [Fact]
    public void FileForms_EscapingLabelsAndLineBreaks()
    {
        var (wb, ws) = Sales();
        wb.Execute(new SetValueCommand(ws, R("C1"), V("Unit-Price")));
        wb.Execute(new SetValueCommand(ws, R("A1"), V("It'em\nName")));
        wb.Execute(new DefineNameCommand("Firsts", "Sales[@Qty]"));
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, R("F1"), "=SUM(Sales[Unit-Price])"));
        wb.Execute(new SetFormulaCommand(ws, R("F2"), "=ROWS(Sales[[#Headers],[#Totals]])"));
        wb.Execute(new SetFormulaCommand(ws, R("F7"), "=[@Qty]"));
        Assert.Equal(60, N(ws, "F1"));
        Assert.Equal(CellValue.Error(CellError.Ref), ws[R("F2")].Value);
        wb.Execute(new SetTotalsRowCommand(ws, "Sales", true));

        var written = Write(wb);
        var f = Xml(written, "xl/worksheets/sheet1.xml").Root!.Descendants(M + "c").ToDictionary(c => (string)c.Attribute("r")!, c => c.Element(M + "f")?.Value);
        Assert.Equal("SUM(Sales[[Unit-Price]])", f["F1"]);
        Assert.Equal("#REF!", f["F7"]);
        Assert.Equal("Sales[[#This Row],[Qty]]", Xml(written, "xl/workbook.xml").Root!.Descendants(M + "definedName").Single().Value);
        var table = Xml(written, "xl/tables/table1.xml").Root!;
        var columns = table.Element(M + "tableColumns")!.Elements().ToList();
        Assert.Equal(("Total", "sum"), ((string)columns[0].Attribute("totalsRowLabel"), (string)columns[3].Attribute("totalsRowFunction")));
        Assert.Contains("It'em_x000a_Name", Encoding.UTF8.GetString(Entry(written, "xl/tables/table1.xml")));
        var again = Read(written).Sheets[0];
        Assert.Equal("It'em\nName", again.Tables[0].Columns[0].Name);
        Assert.Equal("SUM(Sales[[Unit-Price]])", again[R("F1")].Formula);
        Assert.Equal(new StructuredRefNode("T", "a'b", null, StructuredItem.Data), FormulaParser.Parse("T[a''b]"));
        Assert.Equal("T[[a''b]]", FormulaParser.Format(new StructuredRefNode("T", "a'b", null, StructuredItem.Data)));
    }

    [Fact]
    public void UnparsableTablePart_KeptWithItsRelationship()
    {
        var package = TablePackage();
        using (var ms = new MemoryStream())
        {
            ms.Write(package);
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, true))
            {
                zip.GetEntry("xl/tables/table1.xml")!.Delete();
                using var s = zip.CreateEntry("xl/tables/table1.xml").Open();
                s.Write(Encoding.UTF8.GetBytes(TablePart.ReplaceLineEndings("").Replace("ref=\"A1:D4\" totalsRowShown", "ref=\"bad\" totalsRowShown")));
            }

            package = ms.ToArray();
        }

        var wb = Read(package);
        var ws = wb.Sheets[0];
        Assert.Empty(ws.Tables);
        wb.Execute(new SetValueCommand(ws, R("H1"), V(1)));
        var written = Write(wb);
        Assert.Equal(Entry(package, "xl/tables/table1.xml"), Entry(written, "xl/tables/table1.xml"));
        Assert.Equal(Entry(package, "xl/worksheets/_rels/sheet1.xml.rels"), Entry(written, "xl/worksheets/_rels/sheet1.xml.rels"));
        Assert.Equal("rId1", (string)Xml(written, "xl/worksheets/sheet1.xml").Root!.Element(M + "tableParts")!.Element(M + "tablePart")!.Attribute(XName.Get("id", Rel)));
    }

    private static Workbook Read(byte[] package) => XlsxReader.Read(new MemoryStream(package));

    private static byte[] Write(Workbook wb)
    {
        var ms = new MemoryStream();
        XlsxWriter.Write(wb, ms);
        return ms.ToArray();
    }

    private static List<string> Names(byte[] zip)
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
