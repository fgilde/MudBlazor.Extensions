using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class SortFilterTests
{
    private static readonly XNamespace M = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static CellRange G(string a1) => CellRange.Parse(a1);

    private static CellValue V(object o) => o switch
    {
        null => CellValue.Empty,
        double d => CellValue.Number(d),
        int i => CellValue.Number(i),
        string s => CellValue.Text(s),
        bool b => CellValue.Boolean(b),
        CellError e => CellValue.Error(e),
        _ => throw new ArgumentException()
    };

    private static (Workbook Wb, Worksheet Ws) Sheet(params object[][] rows)
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var block = new CellValue[rows.Length, rows.Max(r => r.Length)];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                block[r, c] = V(rows[r][c]);
        wb.Execute(new SetValuesCommand(ws, R("A1"), block));
        return (wb, ws);
    }

    private static List<CellValue> Column(Worksheet ws, int column, int from, int to) =>
        Enumerable.Range(from, to - from + 1).Select(r => ws[new CellRef(r, column)]?.Value ?? default).ToList();

    [Fact]
    public void Sort_ByTwoKeys_StableAndExcelOrder()
    {
        var (wb, ws) = Sheet(
            ["Key", "Second", "Id"],
            [3, 2, 1],
            ["b", 0, 2],
            [true, 0, 3],
            [CellError.NA, 0, 4],
            [null, 0, 5],
            [1, 0, 6],
            ["A", 0, 7],
            [3, 1, 8],
            [1, 0, 9]);
        wb.Execute(new SortCommand(ws, G("A1:C10"), [new SortKey(0), new SortKey(1, Descending: true)], hasHeader: true));

        Assert.Equal("Key", ws[R("A1")].Value.TextValue);
        Assert.Equal([6, 9, 1, 8, 7, 2, 3, 4, 5], Column(ws, 2, 1, 9).Select(v => (int)v.NumberValue));

        wb.Execute(new SortCommand(ws, G("A2:C10"), [new SortKey(0, Descending: true)], hasHeader: false));
        Assert.Equal([4, 3, 2, 7, 1, 8, 6, 9, 5], Column(ws, 2, 1, 9).Select(v => (int)v.NumberValue));
    }

    [Fact]
    public void Sort_MovesFormulasRelatively()
    {
        var (wb, ws) = Sheet(["Key", "Value"], [40, 4], [30, 3], [20, 2], [10, 1]);
        wb.Execute(new SetFormulaCommand(ws, R("C2"), "=B2*2"));
        wb.Execute(new SetFormulaCommand(ws, R("C3"), "=$B$3+B3"));
        wb.Execute(new ResizeCommand(ws, Axis.Row, 1, 1, 50));

        wb.Execute(new SortCommand(ws, G("A1:C5"), [new SortKey(0)], hasHeader: true));

        Assert.Equal("B5*2", ws[R("C5")].Formula);
        Assert.Equal(4, ws[R("B5")].Value.NumberValue);
        Assert.Equal("$B$3+B4", ws[R("C4")].Formula);
        Assert.Null(ws[R("C2")]);
        Assert.Equal(50, ws.RowHeight(4));
        Assert.Equal(ws.DefaultRowHeight, ws.RowHeight(1));
    }

    [Fact]
    public void Sort_IsOneUndoStep()
    {
        var (wb, ws) = Sheet([3, "c"], [1, "a"], [2, "b"]);
        wb.Execute(new ResizeCommand(ws, Axis.Row, 0, 1, 40));
        var changes = 0;
        wb.Changed += (_, _) => changes++;

        wb.Execute(new SortCommand(ws, G("A1:B3"), [new SortKey(0)], hasHeader: false));
        Assert.Equal(1, changes);
        Assert.Equal(["a", "b", "c"], Column(ws, 1, 0, 2).Select(v => v.TextValue));
        Assert.Equal(40, ws.RowHeight(2));

        wb.Undo.Undo();
        Assert.Equal(["c", "a", "b"], Column(ws, 1, 0, 2).Select(v => v.TextValue));
        Assert.Equal(40, ws.RowHeight(0));
        Assert.Equal("Resize rows", wb.Undo.UndoDescription);

        wb.Undo.Redo();
        Assert.Equal(["a", "b", "c"], Column(ws, 1, 0, 2).Select(v => v.TextValue));
    }

    [Fact]
    public void Filter_Values_HidesOtherRows_UndoRestores()
    {
        var (wb, ws) = Sheet(["Name", "Code"], ["x", 7], ["y", 8], ["X", 7], [null, 9], ["z", 7]);
        var style = wb.Styles.GetOrAdd(new CellStyle { NumberFormat = "000" });
        wb.Execute(new SetStyleCommand(ws, G("B2:B6"), wb.Styles[style]));
        wb.Execute(new SetAutoFilterCommand(ws, G("A1:B6")));

        wb.Execute(new ApplyFilterCommand(ws, 0, FilterCriteria.Values(new HashSet<string> { "x" }, blanks: true)));
        Assert.Equal([2, 5], ws.FilteredRows.Order());
        Assert.True(ws.IsRowHidden(2));
        Assert.False(ws.IsRowHidden(4));

        wb.Execute(new ApplyFilterCommand(ws, 1, FilterCriteria.Values(new HashSet<string> { "007" })));
        Assert.Equal([2, 4, 5], ws.FilteredRows.Order());
        Assert.Equal(2, ws.AutoFilter.Columns.Count);

        wb.Undo.Undo();
        Assert.Equal([2, 5], ws.FilteredRows.Order());
        wb.Undo.Undo();
        Assert.Empty(ws.FilteredRows);
        Assert.Empty(ws.AutoFilter.Columns);
        wb.Undo.Undo();
        Assert.Null(ws.AutoFilter);
    }

    [Fact]
    public void Filter_CustomGreaterThanAndContains()
    {
        var (wb, ws) = Sheet(["Name", "Amount"], ["Banana", 12], ["Apple", 30], ["Mango", 5], ["Cherry", 50], ["Orange", 11]);
        wb.Execute(new SetAutoFilterCommand(ws, G("A1:B6")));

        wb.Execute(new ApplyFilterCommand(ws, 1, FilterCriteria.Custom(CustomFilterOperator.GreaterThan, "10")));
        Assert.Equal([3], ws.FilteredRows.Order());

        wb.Execute(new ApplyFilterCommand(ws, 0, FilterCriteria.Custom(CustomFilterOperator.Equal, "*an*")));
        Assert.Equal([2, 3, 4], ws.FilteredRows.Order());

        wb.Execute(new ApplyFilterCommand(ws, 1, FilterCriteria.Custom(CustomFilterOperator.LessThan, "6", and: false, CustomFilterOperator.GreaterThanOrEqual, "50")));
        Assert.Equal([1, 2, 4, 5], ws.FilteredRows.Order());

        wb.Execute(new ApplyFilterCommand(ws, 0, FilterCriteria.Custom(CustomFilterOperator.NotEqual, "m?ngo")));
        Assert.Equal([1, 2, 3, 5], ws.FilteredRows.Order());
    }

    [Fact]
    public void Filter_Top10Percent()
    {
        var (wb, ws) = Sheet([.. new object[][] { ["Value"] }, .. Enumerable.Range(1, 10).Select(i => new object[] { i * 10 })]);
        wb.Execute(new SetAutoFilterCommand(ws, G("A1:A11")));

        wb.Execute(new ApplyFilterCommand(ws, 0, FilterCriteria.Top10(20, percent: true)));
        Assert.Equal(Enumerable.Range(1, 8), ws.FilteredRows.Order());

        wb.Execute(new ApplyFilterCommand(ws, 0, FilterCriteria.Top10(3, bottom: true)));
        Assert.Equal(Enumerable.Range(4, 7), ws.FilteredRows.Order());
    }

    [Fact]
    public void FilteredRows_SeparateFromManualHidden()
    {
        var (wb, ws) = Sheet(["Name"], ["a"], ["b"], ["c"], ["d"]);
        wb.Execute(new SetHiddenCommand(ws, Axis.Row, 2, 1, true));
        wb.Execute(new SetAutoFilterCommand(ws, G("A1:A5")));
        wb.Execute(new ApplyFilterCommand(ws, 0, FilterCriteria.Values(new HashSet<string> { "a", "b" })));

        Assert.Equal([3, 4], ws.FilteredRows.Order());
        Assert.Equal([2, 3, 4], ws.HiddenRows.Order());
        Assert.False(ws.IsRowFiltered(2));

        wb.Execute(new ApplyFilterCommand(ws, 0, null));
        Assert.Empty(ws.FilteredRows);
        Assert.True(ws.IsRowHidden(2));
        Assert.False(ws.IsRowHidden(3));
        Assert.Equal([2], ws.HiddenRows);

        wb.Execute(new ApplyFilterCommand(ws, 0, FilterCriteria.Values(new HashSet<string> { "a" })));
        wb.Execute(new SetAutoFilterCommand(ws, null));
        Assert.Null(ws.AutoFilter);
        Assert.Empty(ws.FilteredRows);
        Assert.Equal([2], ws.HiddenRows);
    }

    [Fact]
    public void InsertRow_ShiftsAutoFilterRange()
    {
        var (wb, ws) = Sheet(["A", "B", "C"], [1, "x", 1], [2, "y", 2], [3, "x", 3]);
        wb.Execute(new SetAutoFilterCommand(ws, G("A1:C4")));
        wb.Execute(new ApplyFilterCommand(ws, 1, FilterCriteria.Values(new HashSet<string> { "x" })));
        Assert.Equal([2], ws.FilteredRows);

        wb.Execute(new InsertRowsCommand(ws, 2, 1));
        Assert.Equal(G("A1:C5"), ws.AutoFilter.Range);
        Assert.Equal([3], ws.FilteredRows);

        wb.Execute(new InsertColumnsCommand(ws, 0, 1));
        Assert.Equal(G("B1:D5"), ws.AutoFilter.Range);
        Assert.Equal([2], ws.AutoFilter.Columns.Keys);

        wb.Execute(new DeleteColumnsCommand(ws, 2, 1));
        Assert.Equal(G("B1:C5"), ws.AutoFilter.Range);
        Assert.Empty(ws.AutoFilter.Columns);
        Assert.Empty(ws.FilteredRows);

        wb.Undo.Undo();
        Assert.Equal(G("B1:D5"), ws.AutoFilter.Range);
        Assert.Equal([2], ws.AutoFilter.Columns.Keys);
        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(G("A1:C4"), ws.AutoFilter.Range);
        Assert.Equal([2], ws.FilteredRows);

        wb.Execute(new DeleteRowsCommand(ws, 0, 4));
        Assert.Null(ws.AutoFilter);
        Assert.Empty(ws.FilteredRows);
        wb.Undo.Undo();
        Assert.Equal(G("A1:C4"), ws.AutoFilter.Range);
        Assert.Equal([2], ws.FilteredRows);
    }

    [Fact]
    public void Xlsx_AutoFilterAndSortState_RoundTrip()
    {
        var rows = "<row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>N</t></is></c><c r=\"B1\" t=\"inlineStr\"><is><t>T</t></is></c></row>"
                   + "<row r=\"2\"><c r=\"A2\"><v>2</v></c><c r=\"B2\" t=\"inlineStr\"><is><t>x</t></is></c></row>"
                   + "<row r=\"3\" hidden=\"1\"><c r=\"A3\"><v>1</v></c><c r=\"B3\" t=\"inlineStr\"><is><t>y</t></is></c></row>"
                   + "<row r=\"4\"><c r=\"A4\"><v>3</v></c></row>";
        const string filter = "<autoFilter ref=\"A1:B4\"><filterColumn colId=\"1\" showButton=\"0\"><filters blank=\"1\"><filter val=\"x\"/></filters></filterColumn>"
                              + "<sortState ref=\"A2:B4\" caseSensitive=\"1\"><sortCondition descending=\"1\" ref=\"A2:A4\"/></sortState>"
                              + "<extLst><ext uri=\"{KEEP}\"/></extLst></autoFilter>";
        var package = XlsxReadTests.Package([XlsxReadTests.Sheet(rows, after: filter + "<sortState ref=\"D2:D9\"><sortCondition ref=\"D2:D9\"/></sortState>")]);
        var wb = XlsxReader.Read(new MemoryStream(package));
        var ws = wb.Sheets[0];

        Assert.False(wb.HasUnshiftedReferences);
        Assert.Equal(G("A1:B4"), ws.AutoFilter.Range);
        var values = ws.AutoFilter.Columns[1];
        Assert.Equal(FilterKind.Values, values.Kind);
        Assert.Equal(["x"], values.Shown);
        Assert.True(values.Blanks);
        Assert.Equal(new SortKey(0, true, true), ws.AutoFilter.Sort);
        Assert.Equal([2], ws.FilteredRows);
        Assert.Empty(ws.HiddenRows.Except(ws.FilteredRows));

        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        var xml = Xml(Write(wb));
        var af = xml.Root!.Element(M + "autoFilter")!;
        Assert.Equal("A2:B5", (string)af.Attribute("ref"));
        var column = af.Element(M + "filterColumn")!;
        Assert.Equal("1", (string)column.Attribute("colId"));
        Assert.Equal("0", (string)column.Attribute("showButton"));
        Assert.Equal("1", (string)column.Element(M + "filters")!.Attribute("blank"));
        Assert.Equal("x", (string)column.Element(M + "filters")!.Element(M + "filter")!.Attribute("val"));
        var sort = af.Element(M + "sortState")!;
        Assert.Equal("A3:B5", (string)sort.Attribute("ref"));
        Assert.Equal("A3:A5", (string)sort.Element(M + "sortCondition")!.Attribute("ref"));
        Assert.Equal("{KEEP}", (string)af.Element(M + "extLst")!.Element(M + "ext")!.Attribute("uri"));
        Assert.Equal("D3:D10", (string)xml.Root.Element(M + "sortState")!.Attribute("ref"));
        Assert.Equal("1", (string)xml.Root.Descendants(M + "row").Single(r => (string)r.Attribute("r") == "4").Attribute("hidden"));

        var created = Workbook.Create();
        var sheet = created.Sheets[0];
        created.Execute(new SetAutoFilterCommand(sheet, G("B2:D20")));
        created.Execute(new ApplyFilterCommand(sheet, 1, FilterCriteria.Custom(CustomFilterOperator.GreaterThan, "5", and: true, CustomFilterOperator.NotEqual, "*z*")));
        created.Execute(new ApplyFilterCommand(sheet, 2, FilterCriteria.Top10(5, percent: true, bottom: true)));
        created.Execute(new ApplyFilterCommand(sheet, 3, FilterCriteria.Color(2, cellColor: false)));
        var back = XlsxReader.Read(new MemoryStream(Write(created))).Sheets[0].AutoFilter;
        Assert.Equal(G("B2:D20"), back.Range);
        var custom = back.Columns[1];
        Assert.Equal((FilterKind.Custom, CustomFilterOperator.GreaterThan, "5", true, CustomFilterOperator.NotEqual, "*z*"),
            (custom.Kind, custom.Operator1, custom.Value1, custom.And, custom.Operator2, custom.Value2));
        var top = back.Columns[2];
        Assert.Equal((FilterKind.Top10, 5d, true, true), (top.Kind, top.Count, top.Percent, top.Bottom));
        var color = back.Columns[3];
        Assert.Equal((FilterKind.Color, 2, false), (color.Kind, color.DxfId, color.CellColor));
    }

    [Fact]
    public void VisibleRowsIn_SkipsFiltered()
    {
        var (wb, ws) = Sheet(["Name"], ["a"], ["b"], ["a"], ["b"], ["a"]);
        wb.Execute(new SetHiddenCommand(ws, Axis.Row, 1, 1, true));
        wb.Execute(new SetAutoFilterCommand(ws, G("A1:A6")));
        wb.Execute(new ApplyFilterCommand(ws, 0, FilterCriteria.Values(new HashSet<string> { "a" })));

        Assert.Equal([0, 3, 5, 6], ws.VisibleRowsIn(G("A1:B7")));
    }

    private static byte[] Write(Workbook wb)
    {
        var ms = new MemoryStream();
        XlsxWriter.Write(wb, ms);
        return ms.ToArray();
    }

    private static XDocument Xml(byte[] zip)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        using var s = archive.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        return XDocument.Load(s);
    }
}
