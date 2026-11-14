using System.Globalization;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class HtmlTableFormatTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private const string ExcelClipboard = """
        <html xmlns:v="urn:schemas-microsoft-com:vml"
        xmlns:o="urn:schemas-microsoft-com:office:office"
        xmlns:x="urn:schemas-microsoft-com:office:excel"
        xmlns="http://www.w3.org/TR/REC-html40">

        <head>
        <meta http-equiv=Content-Type content="text/html; charset=utf-8">
        <meta name=ProgId content=Excel.Sheet>
        <meta name=Generator content="Microsoft Excel 15">
        <link id=Main-File rel=Main-File
        href="file:///C:/Users/user/AppData/Local/Temp/msohtmlclip1/01/clip.htm">
        <link rel=File-List
        href="file:///C:/Users/user/AppData/Local/Temp/msohtmlclip1/01/clip_filelist.xml">
        <style>
        <!--table
        	{mso-displayed-decimal-separator:"\,";
        	mso-displayed-thousand-separator:"\.";}
        @page
        	{margin:.78in .7in .78in .7in;
        	mso-header-margin:.3in;
        	mso-footer-margin:.3in;}
        tr
        	{mso-height-source:auto;}
        col
        	{mso-width-source:auto;}
        br
        	{mso-data-placement:same-cell;}
        td
        	{padding-top:1px;
        	padding-right:1px;
        	padding-left:1px;
        	mso-ignore:padding;
        	color:black;
        	font-size:11.0pt;
        	font-weight:400;
        	font-style:normal;
        	text-decoration:none;
        	font-family:Aptos, sans-serif;
        	mso-generic-font-family:auto;
        	mso-font-charset:0;
        	mso-number-format:General;
        	text-align:general;
        	vertical-align:bottom;
        	border:none;
        	mso-background-source:auto;
        	mso-pattern:auto;
        	mso-protection:locked visible;
        	white-space:nowrap;
        	mso-rotate:0;}
        .xl65
        	{font-weight:700;
        	text-align:center;
        	background:yellow;
        	mso-pattern:black none;}
        .xl66
        	{mso-number-format:"0\.00";}
        .xl67
        	{color:red;
        	font-style:italic;
        	text-decoration:underline;
        	text-underline-style:single;
        	vertical-align:middle;
        	white-space:normal;}
        .xl68
        	{mso-number-format:"Short Date";}
        .xl69
        	{mso-number-format:"\#\,\#\#0\.00\\ \0022€\0022";
        	background:#DDEBF7;
        	mso-pattern:black none;}
        -->
        </style>
        </head>

        <body link="#467886" vlink="#96607D">

        <table border=0 cellpadding=0 cellspacing=0 width=256 style='border-collapse:
         collapse;width:192pt'>
        <!--StartFragment-->
         <col width=64 span=4 style='width:48pt'>
         <tr height=20 style='height:15.0pt'>
          <td colspan=2 height=20 class=xl65 width=128 style='height:15.0pt;
          width:96pt'>Quartal</td>
          <td class=xl65 width=64 style='border-left:none;width:48pt'>Summe</td>
          <td width=64 style='width:48pt'>Notiz</td>
         </tr>
         <tr height=20 style='height:15.0pt'>
          <td height=20 align=right style='height:15.0pt' x:num>1,5</td>
          <td class=xl66 align=right x:num="2.25">2,25</td>
          <td class=xl69 align=right x:num="3.75" x:fmla="=A2+B2">3,75 €</td>
          <td class=xl67 width=64 style='width:48pt'>Zeile 1<br>
            Zeile&nbsp;2 &amp; mehr</td>
         </tr>
         <tr height=20 style='height:15.0pt'>
          <td height=20 class=xl68 align=right style='height:15.0pt' x:num="46336">10.11.2026</td>
          <td align=center x:bool="TRUE">WAHR</td>
          <td x:str="007">007</td>
          <td></td>
         </tr>
        <!--EndFragment-->
        </table>

        </body>

        </html>
        """;

    private const string GoogleSheetsClipboard =
        "<meta charset=\"utf-8\"><google-sheets-html-origin><style type=\"text/css\"><!--td {border: 1px solid #cccccc;}br {mso-data-placement:same-cell;}--></style>"
        + "<table xmlns=\"http://www.w3.org/1999/xhtml\" cellspacing=\"0\" cellpadding=\"0\" dir=\"ltr\" border=\"1\" style=\"table-layout:fixed;font-size:10pt;font-family:Arial;width:0px;border-collapse:collapse;border:none\" data-sheets-root=\"1\">"
        + "<colgroup><col width=\"100\"/><col width=\"100\"/><col width=\"100\"/></colgroup><tbody>"
        + "<tr style=\"height:21px;\"><td style=\"overflow:hidden;padding:2px 3px 2px 3px;vertical-align:bottom;font-weight:bold;\">Name</td>"
        + "<td style=\"overflow:hidden;padding:2px 3px 2px 3px;vertical-align:bottom;font-weight:bold;text-align:right;\">Qty</td>"
        + "<td style=\"overflow:hidden;padding:2px 3px 2px 3px;vertical-align:bottom;font-weight:bold;background-color:#d9ead3;\">Price</td></tr>"
        + "<tr style=\"height:21px;\"><td style=\"overflow:hidden;padding:2px 3px 2px 3px;vertical-align:bottom;\" data-sheets-value=\"{&quot;1&quot;:2,&quot;2&quot;:&quot;Apple&quot;}\">Apple</td>"
        + "<td style=\"overflow:hidden;padding:2px 3px 2px 3px;vertical-align:bottom;text-align:right;\" data-sheets-value=\"{&quot;1&quot;:3,&quot;3&quot;:12}\">12</td>"
        + "<td style=\"overflow:hidden;padding:2px 3px 2px 3px;vertical-align:bottom;text-align:right;color:#ff0000;\" data-sheets-value=\"{&quot;1&quot;:3,&quot;3&quot;:1.25}\" data-sheets-numberformat=\"{&quot;1&quot;:2,&quot;2&quot;:&quot;#,##0.00&quot;,&quot;3&quot;:1}\">1.25</td></tr>"
        + "</tbody></table>";

    private static HtmlTableCell At(HtmlTable table, int row, int column) => table.Cells.Single(c => c.Row == row && c.Column == column);

    [Fact]
    public void Html_ExcelFragment_ValuesStylesAndSpans()
    {
        var table = HtmlTableFormat.Read(ExcelClipboard, German);

        Assert.Equal((3, 4), (table.RowCount, table.ColumnCount));
        var title = At(table, 0, 0);
        Assert.Equal(("Quartal", 2, 1), (title.Value.TextValue, title.ColumnSpan, title.RowSpan));
        Assert.True(title.Style.Font.Bold);
        Assert.Equal("#FFFF00", title.Style.Fill.Color);
        Assert.Equal(CellHorizontalAlignment.Center, title.Style.Alignment.Horizontal);
        Assert.Equal("Summe", At(table, 0, 2).Value.TextValue);
        Assert.False(At(table, 0, 3).Style.Font.Bold);
        Assert.DoesNotContain(table.Cells, c => c.Row == 0 && c.Column == 1);

        Assert.Equal(1.5, At(table, 1, 0).Value.NumberValue);
        Assert.Equal(2.25, At(table, 1, 1).Value.NumberValue);
        Assert.Equal("0.00", At(table, 1, 1).Style.NumberFormat);
        var sum = At(table, 1, 2);
        Assert.Equal((3.75, "A2+B2"), (sum.Value.NumberValue, sum.Formula));
        Assert.Equal("#,##0.00\\ \"€\"", sum.Style.NumberFormat);
        Assert.Equal("#DDEBF7", sum.Style.Fill.Color);

        var note = At(table, 1, 3);
        Assert.Equal("Zeile 1\nZeile 2 & mehr", note.Value.TextValue);
        Assert.Equal(("#FF0000", true, true), (note.Style.Font.Color, note.Style.Font.Italic, note.Style.Font.Underline));
        Assert.Equal((CellVerticalAlignment.Center, true), (note.Style.Alignment.Vertical, note.Style.Alignment.WrapText));

        var date = At(table, 2, 0);
        Assert.Equal(46336, date.Value.NumberValue);
        Assert.Equal(BuiltInNumberFormats.Get(14, German), date.Style.NumberFormat);
        Assert.True(At(table, 2, 1).Value.BooleanValue);
        Assert.Equal(CellValue.Text("007"), At(table, 2, 2).Value);
        Assert.Equal(CellValueKind.Empty, At(table, 2, 3).Value.Kind);
    }

    [Fact]
    public void Html_GoogleSheets_ValuesAndBold()
    {
        var table = HtmlTableFormat.Read(GoogleSheetsClipboard, CultureInfo.InvariantCulture);

        Assert.Equal((2, 3), (table.RowCount, table.ColumnCount));
        Assert.All(table.Cells.Where(c => c.Row == 0), c => Assert.True(c.Style.Font.Bold));
        Assert.Equal(["Name", "Qty", "Price"], table.Cells.Where(c => c.Row == 0).OrderBy(c => c.Column).Select(c => c.Value.TextValue));
        Assert.Equal("#D9EAD3", At(table, 0, 2).Style.Fill.Color);
        Assert.Equal(CellHorizontalAlignment.Right, At(table, 0, 1).Style.Alignment.Horizontal);
        Assert.Equal("Apple", At(table, 1, 0).Value.TextValue);
        Assert.False(At(table, 1, 0).Style.Font.Bold);
        Assert.Equal(12, At(table, 1, 1).Value.NumberValue);
        Assert.Equal(1.25, At(table, 1, 2).Value.NumberValue);
        Assert.Equal("#FF0000", At(table, 1, 2).Style.Font.Color);
    }

    [Fact]
    public void Html_GoogleSheetsNumbers_UseSheetsValueNotCulture()
    {
        var table = HtmlTableFormat.Read(GoogleSheetsClipboard, German);

        Assert.Equal(1.25, At(table, 1, 2).Value.NumberValue);
    }

    [Fact]
    public void Html_ScriptAndOnAttributes_Ignored()
    {
        const string html = """
            <script>alert('x')</script>
            <table onmouseover="alert(1)">
            <tr><td onclick="alert(2)" style="background:url(javascript:alert(3));color:expression(alert(4));font-weight:bold;position:fixed">Safe<script>document.write('evil')</script><img src=x onerror="alert(5)"></td>
            <td style="background-color:#00ff00;behavior:url(x.htc)"><iframe src="javascript:alert(6)">framed</iframe>2</td></tr>
            </table>
            """;

        var table = HtmlTableFormat.Read(html, CultureInfo.InvariantCulture);

        Assert.Equal(2, table.Cells.Count);
        var safe = At(table, 0, 0);
        Assert.Equal("Safe", safe.Value.TextValue);
        Assert.True(safe.Style.Font.Bold);
        Assert.Null(safe.Style.Fill.Color);
        Assert.Null(safe.Style.Font.Color);
        Assert.Equal(2, At(table, 0, 1).Value.NumberValue);
        Assert.Equal("#00FF00", At(table, 0, 1).Style.Fill.Color);
        Assert.DoesNotContain(table.Cells, c => c.Value.ToString().Contains("alert") || c.Value.ToString().Contains("evil"));
    }

    [Fact]
    public void Html_RowSpanShiftsLaterCells()
    {
        const string html = "<table><tr><td rowspan=2>a</td><td>b</td></tr><tr><td>c</td></tr><tr><td colspan=99999>d</td></tr></table>";

        var table = HtmlTableFormat.Read(html, CultureInfo.InvariantCulture);

        Assert.Equal(2, At(table, 0, 0).RowSpan);
        Assert.Equal("c", At(table, 1, 1).Value.TextValue);
        Assert.Equal(1000, At(table, 2, 0).ColumnSpan);
    }

    [Fact]
    public void Html_WithoutTable_ReturnsNull()
    {
        Assert.Null(HtmlTableFormat.Read("<p>Just <b>text</b></p>", CultureInfo.InvariantCulture));
        Assert.Null(HtmlTableFormat.Read("", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Html_Write_RoundTripsValuesStylesAndMerges()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, CellRef.Parse("A1"), CellValue.Text("Head <&>")));
        wb.Execute(new MergeCommand(ws, CellRange.Parse("A1:B1")));
        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("A1"), s => s with { Font = s.Font with { Bold = true }, Fill = new CellFill { Color = "#FFFF00" } }));
        wb.Execute(new SetValueCommand(ws, CellRef.Parse("A2"), CellValue.Number(1234.5)));
        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("A2"), s => s with { NumberFormat = "#,##0.00\" kg\"" }));
        wb.Execute(new SetValueCommand(ws, CellRef.Parse("B2"), CellValue.Text("x\ny")));

        var html = HtmlTableFormat.Write(ws, CellRange.Parse("A1:B2"), German);
        var table = HtmlTableFormat.Read(html, German);

        Assert.Contains("x:num=\"1234.5\"", html);
        Assert.Contains("1.234,50 kg", html);
        var head = At(table, 0, 0);
        Assert.Equal(("Head <&>", 2, true, "#FFFF00"), (head.Value.TextValue, head.ColumnSpan, head.Style.Font.Bold, head.Style.Fill.Color));
        Assert.Equal((1234.5, "#,##0.00\" kg\""), (At(table, 1, 0).Value.NumberValue, At(table, 1, 0).Style.NumberFormat));
        Assert.Equal("x\ny", At(table, 1, 1).Value.TextValue);
    }

    [Fact]
    public void Html_Write_SkipsFilteredRows()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        foreach (var (a1, text) in new[] { ("A1", "Fruit"), ("A2", "apple"), ("A3", "pear"), ("A4", "plum") })
            wb.Execute(new SetValueCommand(ws, CellRef.Parse(a1), CellValue.Text(text)));
        wb.Execute(new SetAutoFilterCommand(ws, CellRange.Parse("A1:A4")));
        wb.Execute(new ApplyFilterCommand(ws, 0, FilterCriteria.Values(new HashSet<string> { "apple", "plum" })));

        var table = HtmlTableFormat.Read(HtmlTableFormat.Write(ws, CellRange.Parse("A1:A4"), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        Assert.Equal(["Fruit", "apple", "plum"], table.Cells.OrderBy(c => c.Row).Select(c => c.Value.TextValue));
    }
}
