using System.IO.Compression;
using System.Xml.Linq;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class RulesTests
{
    private static readonly XNamespace M = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly DifferentialStyle Red = new(Fill: new CellFill { Color = "#FF0000" });
    private static readonly DifferentialStyle Green = new(Fill: new CellFill { Color = "#00FF00" });
    private static readonly DifferentialStyle Bold = new(Font: new DifferentialFont { Bold = true });

    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static CellRange G(string a1) => CellRange.Parse(a1);

    private static (Workbook Wb, Worksheet Ws) Sheet(params object[][] rows)
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var block = new CellValue[rows.Length, rows.Max(r => r.Length)];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                block[r, c] = rows[r][c] switch
                {
                    null => CellValue.Empty,
                    int i => CellValue.Number(i),
                    double d => CellValue.Number(d),
                    string s => CellValue.Text(s),
                    _ => throw new ArgumentException()
                };
        wb.Execute(new SetValuesCommand(ws, R("A1"), block));
        return (wb, ws);
    }

    private static (Workbook Wb, Worksheet Ws) Column(params object[] values) => Sheet(values.Select(v => new[] { v }).ToArray());

    private static ConditionalFormatEvaluator Format(Workbook wb, Worksheet ws, string ranges, params ConditionalRule[] rules)
    {
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting(ranges.Split(' ').Select(G).ToList(), rules)));
        return new ConditionalFormatEvaluator(wb, ws);
    }

    private static ConditionalValue Percent(int p) => new(ConditionalValueType.Percent, p.ToString());

    [Fact]
    public void CellIs_Between_AppliesDxf()
    {
        var (wb, ws) = Column(5, 10, 15, 20, 25);
        using var cf = Format(wb, ws, "A1:A5", new ConditionalRule.CellIs("between", "20", "10", Red));

        Assert.Null(cf.Evaluate(R("A1")));
        Assert.Equal(Red, cf.Evaluate(R("A2"))!.Style);
        Assert.Equal("#FF0000", cf.Evaluate(R("A4"))!.Style.Fill.Color);
        Assert.Null(cf.Evaluate(R("A5")));
        Assert.Null(cf.Evaluate(R("B2")));

        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([G("A1:A5")], [new ConditionalRule.CellIs("notBetween", "10", "20", Green, 2)])));
        Assert.Equal(Green, cf.Evaluate(R("A1"))!.Style);
        Assert.Equal(Red, cf.Evaluate(R("A3"))!.Style);
    }

    [Fact]
    public void Expression_RelativeToTopLeft()
    {
        var (wb, ws) = Sheet([null, "Amount"], [null, 150], [null, 50], [null, 101]);
        using var cf = Format(wb, ws, "A2:C10", new ConditionalRule.Expression("=$B2>100", Bold));

        foreach (var c in (string[])["A2", "B2", "C2", "A4", "C4"]) Assert.Equal(Bold, cf.Evaluate(R(c))?.Style);
        foreach (var c in (string[])["A3", "C3", "A5", "A1", "D2"]) Assert.Null(cf.Evaluate(R(c)));
        Assert.Null(ws[R("A2")]);
    }

    [Fact]
    public void ColorScale_ThreeStop_Interpolates()
    {
        var (wb, ws) = Column(0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, "text");
        using var cf = Format(wb, ws, "A1:A12", new ConditionalRule.ColorScale([
            new ColorScaleStop(new ConditionalValue(ConditionalValueType.Min), "#F8696B"),
            new ColorScaleStop(new ConditionalValue(ConditionalValueType.Percentile, "50"), "#FFEB84"),
            new ColorScaleStop(new ConditionalValue(ConditionalValueType.Max), "#63BE7B")
        ]));

        Assert.Equal("#F8696B", cf.Evaluate(R("A1"))!.ScaleColor);
        Assert.Equal("#FFEB84", cf.Evaluate(R("A6"))!.ScaleColor);
        Assert.Equal("#63BE7B", cf.Evaluate(R("A11"))!.ScaleColor);
        Assert.Equal("#FB9D75", cf.Evaluate(R("A3"))!.ScaleColor);
        Assert.Null(cf.Evaluate(R("A12")));
    }

    [Fact]
    public void DataBar_Fraction()
    {
        var (wb, ws) = Column(0, 25, 50, 75, 100);
        using var cf = Format(wb, ws, "A1:A5",
            new ConditionalRule.DataBar(new ConditionalValue(ConditionalValueType.Min), new ConditionalValue(ConditionalValueType.Max), "#638EC6"));

        Assert.Equal(0.5, cf.Evaluate(R("A3"))!.BarFraction);
        Assert.Equal(0, cf.Evaluate(R("A1"))!.BarFraction);
        Assert.Equal(1, cf.Evaluate(R("A5"))!.BarFraction);
        Assert.Equal("#638EC6", cf.Evaluate(R("A2"))!.BarColor);

        wb.Execute(new EditConditionalFormattingCommand(ws, 0, ws.ConditionalFormats[0] with
        {
            Rules = [new ConditionalRule.DataBar(new ConditionalValue(ConditionalValueType.Number, "0"), new ConditionalValue(ConditionalValueType.Number, "=50"), "#638EC6")]
        }));
        Assert.Equal(0.5, cf.Evaluate(R("A2"))!.BarFraction);
        Assert.Equal(1, cf.Evaluate(R("A4"))!.BarFraction);
    }

    [Fact]
    public void IconSet_ThreeArrows_Thresholds()
    {
        var (wb, ws) = Column(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        using var cf = Format(wb, ws, "A1:A10", new ConditionalRule.IconSet("3Arrows", [Percent(0), Percent(33), Percent(67)]));

        Assert.Equal("3Arrows:0", cf.Evaluate(R("A1"))!.Icon);
        Assert.Equal("3Arrows:0", cf.Evaluate(R("A3"))!.Icon);
        Assert.Equal("3Arrows:1", cf.Evaluate(R("A4"))!.Icon);
        Assert.Equal("3Arrows:1", cf.Evaluate(R("A7"))!.Icon);
        Assert.Equal("3Arrows:2", cf.Evaluate(R("A8"))!.Icon);
        Assert.False(cf.Evaluate(R("A8"))!.HideValue);

        wb.Execute(new EditConditionalFormattingCommand(ws, 0, new ConditionalFormatting([G("A1:A10")],
            [new ConditionalRule.IconSet("3Arrows", [Percent(0), Percent(33), new ConditionalValue(ConditionalValueType.Number, "8", GreaterOrEqual: false)], Reverse: true, ShowValue: false)])));
        Assert.Equal("3Arrows:2", cf.Evaluate(R("A1"))!.Icon);
        Assert.Equal("3Arrows:1", cf.Evaluate(R("A8"))!.Icon);
        Assert.Equal("3Arrows:0", cf.Evaluate(R("A9"))!.Icon);
        Assert.True(cf.Evaluate(R("A9"))!.HideValue);
    }

    [Fact]
    public void Top10_And_Duplicate()
    {
        var (wb, ws) = Sheet([1, "x"], [2, "y"], [3, "X"], [4, 5], [5, 5], [6], [7], [8], [9], [10]);
        using var cf = Format(wb, ws, "A1:A10", new ConditionalRule.Top10(3, false, false, Red), new ConditionalRule.Top10(20, true, true, Green));
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([G("B1:B10")], [new ConditionalRule.Duplicate(false, Bold)])));
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([G("C1:C3"), G("B1:B10")], [new ConditionalRule.Duplicate(true, Red)])));

        Assert.Equal(["A1", "A2", "A8", "A9", "A10"], Enumerable.Range(1, 10).Select(i => "A" + i).Where(a => cf.Evaluate(R(a)) != null));
        Assert.Equal(Red, cf.Evaluate(R("A8"))!.Style);
        Assert.Equal(Green, cf.Evaluate(R("A2"))!.Style);
        Assert.Equal(Bold, cf.Evaluate(R("B1"))!.Style);
        Assert.Equal(Bold, cf.Evaluate(R("B3"))!.Style);
        Assert.Equal(Bold, cf.Evaluate(R("B5"))!.Style);
        Assert.Equal(Red, cf.Evaluate(R("B2"))!.Style);
        Assert.Null(cf.Evaluate(R("B6")));
    }

    [Fact]
    public void StopIfTrue_PriorityOrder()
    {
        var (wb, ws) = Column(50, 20);
        var both = new DifferentialStyle(Font: new DifferentialFont { Bold = true }, Fill: new CellFill { Color = "#00FF00" });
        using var cf = Format(wb, ws, "A1:A2", new ConditionalRule.CellIs("greaterThan", "10", null, both, Priority: 2));
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([G("A1:A2")], [new ConditionalRule.CellIs("greaterThan", "40", null, Red, Priority: 1)])));

        Assert.Equal(new DifferentialStyle(both.Font, Red.Fill), cf.Evaluate(R("A1"))!.Style);
        Assert.Equal(both, cf.Evaluate(R("A2"))!.Style);

        wb.Execute(new EditConditionalFormattingCommand(ws, 1, new ConditionalFormatting([G("A1:A2")],
            [new ConditionalRule.CellIs("greaterThan", "40", null, Red, Priority: 1, StopIfTrue: true)])));
        Assert.Equal(Red, cf.Evaluate(R("A1"))!.Style);
        Assert.Equal(both, cf.Evaluate(R("A2"))!.Style);
    }

    [Fact]
    public void Aggregates_ComputedOncePerChange()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var block = new CellValue[10_000, 1];
        for (var i = 0; i < 10_000; i++) block[i, 0] = CellValue.Number(i);
        wb.Execute(new SetValuesCommand(ws, R("A1"), block));
        using var cf = Format(wb, ws, "A1:A10000", new ConditionalRule.ColorScale([
            new ColorScaleStop(new ConditionalValue(ConditionalValueType.Min), "#000000"),
            new ColorScaleStop(new ConditionalValue(ConditionalValueType.Max), "#FFFFFF")
        ]));

        for (var i = 0; i < 10_000; i++) cf.Evaluate(new CellRef(i, 0));
        Assert.Equal(1, cf.AggregateComputations);
        Assert.Equal("#FFFFFF", cf.Evaluate(R("A10000"))!.ScaleColor);

        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(-9_999)));
        for (var i = 0; i < 10_000; i++) cf.Evaluate(new CellRef(i, 0));
        Assert.Equal(2, cf.AggregateComputations);
        Assert.Equal("#808080", cf.Evaluate(R("A2"))!.ScaleColor);

        cf.Invalidate();
        cf.Evaluate(R("A2"));
        Assert.Equal(3, cf.AggregateComputations);
        cf.Dispose();
        wb.Execute(new SetValueCommand(ws, R("A2"), CellValue.Number(1)));
        cf.Evaluate(R("A2"));
        Assert.Equal(3, cf.AggregateComputations);
    }

    [Fact]
    public void Validation_WholeBetween_RejectsOutOfRange()
    {
        var (wb, ws) = Column(10);
        var rule = new DataValidation([G("B1:B10")], ValidationType.Whole, "between", "1", "=$A$1", AllowBlank: false, ErrorTitle: "Out of range",
            Error: "1 to 10");
        wb.Execute(new AddDataValidationCommand(ws, rule));

        Assert.True(DataValidator.Validate(wb, ws, R("B2"), CellValue.Number(5)).IsValid);
        Assert.True(DataValidator.Validate(wb, ws, R("B2"), CellValue.Number(10)).IsValid);
        var result = DataValidator.Validate(wb, ws, R("B2"), CellValue.Number(11));
        Assert.False(result.IsValid);
        Assert.Equal("1 to 10", result.Validation.Error);
        Assert.False(DataValidator.Validate(wb, ws, R("B3"), CellValue.Number(2.5)).IsValid);
        Assert.False(DataValidator.Validate(wb, ws, R("B3"), CellValue.Text("abc")).IsValid);
        Assert.False(DataValidator.Validate(wb, ws, R("B3"), CellValue.Empty).IsValid);
        var free = DataValidator.Validate(wb, ws, R("C1"), CellValue.Number(99));
        Assert.True(free.IsValid);
        Assert.Null(free.Validation);

        wb.Execute(new AddDataValidationCommand(ws, new DataValidation([G("D1:D5")], ValidationType.TextLength, "lessThanOrEqual", "3")));
        wb.Execute(new AddDataValidationCommand(ws, new DataValidation([G("E2:E5")], ValidationType.Custom, Formula1: "=AND(E2>$A$1,ISEVEN(E2))")));
        Assert.True(DataValidator.Validate(wb, ws, R("D1"), CellValue.Text("abc")).IsValid);
        Assert.False(DataValidator.Validate(wb, ws, R("D1"), CellValue.Text("abcd")).IsValid);
        Assert.True(DataValidator.Validate(wb, ws, R("E4"), CellValue.Number(12)).IsValid);
        Assert.False(DataValidator.Validate(wb, ws, R("E4"), CellValue.Number(11)).IsValid);
        Assert.False(DataValidator.Validate(wb, ws, R("E4"), CellValue.Number(8)).IsValid);
        Assert.Null(ws[R("E4")]);
    }

    [Fact]
    public void Validation_ListFromRangeAndLiteral()
    {
        var (wb, ws) = Sheet([null, null, null, null, "x"], [null, null, null, null, "y"], [null, null, null, null, 3]);
        var literal = new DataValidation([G("A1:A5")], ValidationType.List, Formula1: "\"a,b,c\"");
        var range = new DataValidation([G("C1:C5")], ValidationType.List, Formula1: "=$E$1:$E$3");
        wb.Execute(new AddDataValidationCommand(ws, literal));
        wb.Execute(new AddDataValidationCommand(ws, range));

        Assert.Equal(["a", "b", "c"], DataValidator.ListItems(wb, ws, literal));
        Assert.Equal(["x", "y", "3"], DataValidator.ListItems(wb, ws, range));
        Assert.True(DataValidator.Validate(wb, ws, R("A2"), CellValue.Text("b")).IsValid);
        Assert.True(DataValidator.Validate(wb, ws, R("A2"), CellValue.Text("B")).IsValid);
        Assert.False(DataValidator.Validate(wb, ws, R("A2"), CellValue.Text("d")).IsValid);
        Assert.True(DataValidator.Validate(wb, ws, R("C5"), CellValue.Number(3)).IsValid);
        Assert.True(DataValidator.Validate(wb, ws, R("C5"), CellValue.Text("y")).IsValid);
        Assert.False(DataValidator.Validate(wb, ws, R("C5"), CellValue.Text("z")).IsValid);
        Assert.True(DataValidator.Validate(wb, ws, R("C5"), CellValue.Empty).IsValid);
    }

    [Fact]
    public void InsertRow_ShiftsRulesAndFormulas()
    {
        var (wb, ws) = Sheet([null, null, null, 100], [null, 150], [null, 50], [null, 200]);
        using var cf = Format(wb, ws, "A2:C10 E2:E3", new ConditionalRule.Expression("=$B2>100", Bold), new ConditionalRule.CellIs("greaterThan", "$D$1", null, Red, 2));
        wb.Execute(new AddDataValidationCommand(ws, new DataValidation([G("B2:B5")], ValidationType.List, Formula1: "$E$1:$E$3")));
        wb.Execute(new AddSheetCommand("Other"));
        var second = wb.Sheets[1];
        wb.Execute(new AddConditionalFormattingCommand(second, new ConditionalFormatting([G("A1")], [new ConditionalRule.Expression("Sheet1!$B$2>0", Red)])));

        wb.Execute(new InsertRowsCommand(ws, 0, 1));

        var shifted = ws.ConditionalFormats[0];
        Assert.Equal([G("A3:C11"), G("E3:E4")], shifted.Ranges);
        Assert.Equal("=$B3>100", ((ConditionalRule.Expression)shifted.Rules[0]).Formula);
        Assert.Equal("$D$2", ((ConditionalRule.CellIs)shifted.Rules[1]).Formula1);
        Assert.Equal([G("B3:B6")], ws.DataValidations[0].Ranges);
        Assert.Equal("$E$2:$E$4", ws.DataValidations[0].Formula1);
        Assert.Equal("Sheet1!$B$3>0", ((ConditionalRule.Expression)second.ConditionalFormats[0].Rules[0]).Formula);
        Assert.Equal(Bold, cf.Evaluate(R("C3"))?.Style);
        Assert.Null(cf.Evaluate(R("C4")));

        wb.Undo.Undo();
        Assert.Equal([G("A2:C10"), G("E2:E3")], ws.ConditionalFormats[0].Ranges);
        Assert.Equal("=$B2>100", ((ConditionalRule.Expression)ws.ConditionalFormats[0].Rules[0]).Formula);
        Assert.Equal("$E$1:$E$3", ws.DataValidations[0].Formula1);

        wb.Execute(new DeleteRowsCommand(ws, 1, 1));
        var deleted = ws.ConditionalFormats[0];
        Assert.Equal([G("A2:C9"), G("E2:E2")], deleted.Ranges);
        Assert.Equal("=$B2>100", ((ConditionalRule.Expression)deleted.Rules[0]).Formula);
        Assert.Null(cf.Evaluate(R("A2")));
        Assert.Equal(Bold, cf.Evaluate(R("A3"))?.Style);
        Assert.Equal([G("B2:B4")], ws.DataValidations[0].Ranges);
        wb.Undo.Undo();
        Assert.Equal([G("A2:C10"), G("E2:E3")], ws.ConditionalFormats[0].Ranges);
        Assert.Equal([G("B2:B5")], ws.DataValidations[0].Ranges);

        wb.Execute(new ClearRulesCommand(ws, G("A1:C2")));
        Assert.Equal([G("A3:C10"), G("E2:E3")], ws.ConditionalFormats[0].Ranges);
        Assert.Equal("=$B3>100", ((ConditionalRule.Expression)ws.ConditionalFormats[0].Rules[0]).Formula);
        Assert.Equal([G("B3:B5")], ws.DataValidations[0].Ranges);
    }

    private const string Styles = """
        <fonts count="1"><font><sz val="11"/><name val="Calibri"/></font></fonts>
        <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
        <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
        <cellXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/></cellXfs>
        <dxfs count="2"><dxf><font><b/><color rgb="FF9C0006"/></font><fill><patternFill><bgColor rgb="FFFFC7CE"/></patternFill></fill></dxf><dxf><numFmt numFmtId="164" formatCode="0.0"/></dxf></dxfs>
        """;

    private const string Rules = """
        <conditionalFormatting sqref="A1:A10 C1:C3"><cfRule type="cellIs" dxfId="0" priority="2" operator="between" stopIfTrue="1"><formula>$D$1</formula><formula>20</formula></cfRule>
        <cfRule type="expression" dxfId="1" priority="1"><formula>_xlfn.XOR($B1&gt;1,TRUE)</formula></cfRule></conditionalFormatting>
        <conditionalFormatting sqref="B1:B10"><cfRule type="colorScale" priority="3"><colorScale><cfvo type="min"/><cfvo type="percentile" val="50"/><cfvo type="max"/><color theme="4"/><color rgb="FFFFEB84"/><color rgb="FF63BE7B"/></colorScale></cfRule>
        <cfRule type="dataBar" priority="4"><dataBar showValue="0"><cfvo type="min"/><cfvo type="num" val="$D$1"/><color rgb="FF638EC6"/></dataBar><extLst><ext uri="{B025F937-C7B1-47D3-B67F-A62EFF666E3E}" xmlns:x14="http://schemas.microsoft.com/office/spreadsheetml/2009/9/main"><x14:id>{AAAA}</x14:id></ext></extLst></cfRule>
        <cfRule type="iconSet" priority="5"><iconSet iconSet="3Arrows" reverse="1"><cfvo type="percent" val="0"/><cfvo type="percent" val="33"/><cfvo type="percent" val="67" gte="0"/></iconSet></cfRule>
        <cfRule type="containsText" dxfId="0" priority="6" operator="containsText" text="ab"><formula>NOT(ISERROR(SEARCH("ab",B1)))</formula></cfRule></conditionalFormatting>
        <dataValidations count="2" disablePrompts="1"><dataValidation type="list" allowBlank="1" showInputMessage="1" showErrorMessage="1" promptTitle="Pick" prompt="One of them" sqref="E1:E5 G1"><formula1>"a,b,c"</formula1></dataValidation>
        <dataValidation type="whole" errorStyle="warning" operator="greaterThan" showDropDown="1" error="Too small" sqref="F2:F9"><formula1>$D$1</formula1></dataValidation></dataValidations>
        <pageMargins left="0.7" right="0.7" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>
        <extLst><ext uri="{78C0D931-6437-407d-A8EE-F0AAD7539E65}" xmlns:x14="http://schemas.microsoft.com/office/spreadsheetml/2009/9/main"><x14:conditionalFormattings><x14:conditionalFormatting><x14:cfRule type="dataBar" id="{AAAA}"/></x14:conditionalFormatting></x14:conditionalFormattings></ext></extLst>
        """;

    private static byte[] RulesPackage() =>
        XlsxReadTests.Package([XlsxReadTests.Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row>", "", Rules.ReplaceLineEndings(""))], styles: Styles.ReplaceLineEndings(""));

    [Fact]
    public void Xlsx_CfDvDxf_RoundTrip_ExtLstPreserved()
    {
        var package = RulesPackage();
        var wb = XlsxReader.Read(new MemoryStream(package));
        var ws = wb.Sheets[0];

        Assert.Equal(2, wb.Styles.DifferentialStyles.Count);
        Assert.Equal(new DifferentialStyle(new DifferentialFont { Bold = true, Color = "#9C0006" }, new CellFill { Color = "#FFC7CE" }), wb.Styles.DifferentialStyles[0]);
        Assert.Equal(new DifferentialStyle(NumberFormat: "0.0"), wb.Styles.DifferentialStyles[1]);
        Assert.Equal(2, ws.ConditionalFormats.Count);
        var first = ws.ConditionalFormats[0];
        Assert.Equal([G("A1:A10"), G("C1:C3")], first.Ranges);
        var cellIs = Assert.IsType<ConditionalRule.CellIs>(first.Rules[0]);
        Assert.Equal(("between", "$D$1", "20", 2, true), (cellIs.Operator, cellIs.Formula1, cellIs.Formula2, cellIs.Priority, cellIs.StopIfTrue));
        Assert.Equal(wb.Styles.DifferentialStyles[0], cellIs.Style);
        Assert.Equal("XOR($B1>1,TRUE)", Assert.IsType<ConditionalRule.Expression>(first.Rules[1]).Formula);
        var rules = ws.ConditionalFormats[1].Rules;
        var scale = Assert.IsType<ConditionalRule.ColorScale>(rules[0]);
        Assert.Equal(["#4472C4", "#FFEB84", "#63BE7B"], scale.Stops.Select(s => s.Color));
        Assert.Equal(new ConditionalValue(ConditionalValueType.Percentile, "50"), scale.Stops[1].Value);
        var bar = Assert.IsType<ConditionalRule.DataBar>(rules[1]);
        Assert.Equal((new ConditionalValue(ConditionalValueType.Number, "$D$1"), "#638EC6"), (bar.Max, bar.Color));
        var icons = Assert.IsType<ConditionalRule.IconSet>(rules[2]);
        Assert.Equal(("3Arrows", true, true, false), (icons.Set, icons.Reverse, icons.ShowValue, icons.Thresholds[2].GreaterOrEqual));
        Assert.Equal((ContainsTextKind.Contains, "ab"), (((ConditionalRule.ContainsText)rules[3]).Kind, ((ConditionalRule.ContainsText)rules[3]).Text));
        var list = ws.DataValidations[0];
        Assert.Equal([G("E1:E5"), G("G1")], list.Ranges);
        Assert.Equal((ValidationType.List, "\"a,b,c\"", true, true, "Pick", "One of them", "stop"),
            (list.Type, list.Formula1, list.AllowBlank, list.ShowDropDown, list.PromptTitle, list.Prompt, list.ErrorStyle));
        var whole = ws.DataValidations[1];
        Assert.Equal((ValidationType.Whole, "greaterThan", "$D$1", false, false, "Too small", "warning"),
            (whole.Type, whole.Operator, whole.Formula1, whole.AllowBlank, whole.ShowDropDown, whole.Error, whole.ErrorStyle));
        Assert.True(wb.HasUnshiftedReferences);
        Assert.False(XlsxReader.Read(new MemoryStream(XlsxReadTests.Package([XlsxReadTests.Sheet("", "",
            Rules.ReplaceLineEndings("")[..Rules.ReplaceLineEndings("").IndexOf("<pageMargins", StringComparison.Ordinal)])]))).HasUnshiftedReferences);

        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        var written = Write(wb);
        Assert.Equal(Entry(package, "xl/styles.xml"), Entry(written, "xl/styles.xml"));
        var root = Xml(written).Root!;
        var formats = root.Elements(M + "conditionalFormatting").ToList();
        Assert.Equal(["A2:A11 C2:C4", "B2:B11"], formats.Select(f => (string)f.Attribute("sqref")));
        Assert.Equal(["$D$2", "20"], formats[0].Elements(M + "cfRule").First().Elements(M + "formula").Select(f => f.Value));
        Assert.Equal("_xlfn.XOR($B2>1,TRUE)", formats[0].Elements(M + "cfRule").Last().Element(M + "formula")!.Value);
        var original = Xml(package).Root!.Elements(M + "conditionalFormatting").Last().Elements(M + "cfRule").ToList();
        var after = formats[1].Elements(M + "cfRule").ToList();
        Assert.True(XNode.DeepEquals(original[0], after[0]));
        Assert.Equal("$D$2", (string)after[1].Element(M + "dataBar")!.Elements(M + "cfvo").Last().Attribute("val"));
        Assert.Equal("0", (string)after[1].Element(M + "dataBar")!.Attribute("showValue"));
        Assert.Equal("{AAAA}", after[1].Element(M + "extLst")!.Value);
        Assert.True(XNode.DeepEquals(original[2], after[2]));
        Assert.Equal("NOT(ISERROR(SEARCH(\"ab\",B2)))", after[3].Element(M + "formula")!.Value);
        var validations = root.Element(M + "dataValidations")!;
        Assert.Equal(("2", "1"), ((string)validations.Attribute("count"), (string)validations.Attribute("disablePrompts")));
        Assert.Equal(["E2:E6 G2", "F3:F10"], validations.Elements(M + "dataValidation").Select(d => (string)d.Attribute("sqref")));
        Assert.Equal("$D$2", validations.Elements(M + "dataValidation").Last().Element(M + "formula1")!.Value);
        Assert.True(XNode.DeepEquals(Xml(package).Root!.Element(M + "extLst"), root.Element(M + "extLst")));
        Assert.Equal(["sheetData", "conditionalFormatting", "conditionalFormatting", "dataValidations", "pageMargins", "extLst"],
            root.Elements().Select(e => e.Name.LocalName).SkipWhile(n => n != "sheetData"));

        var again = XlsxReader.Read(new MemoryStream(written)).Sheets[0];
        Assert.Equal([G("A2:A11"), G("C2:C4")], again.ConditionalFormats[0].Ranges);
        Assert.Equal(6, again.ConditionalFormats.Sum(c => c.Rules.Count));
        Assert.Equal("$D$2", ((ConditionalRule.DataBar)again.ConditionalFormats[1].Rules[1]).Max.Value);
        var back = again.DataValidations[1];
        Assert.Equal([G("F3:F10")], back.Ranges);
        Assert.Equal((ValidationType.Whole, "greaterThan", "$D$2", false, false, "Too small", "warning"),
            (back.Type, back.Operator, back.Formula1, back.AllowBlank, back.ShowDropDown, back.Error, back.ErrorStyle));
    }

    [Fact]
    public void NewRule_AppendsDxf_ExistingDxfsUnchanged()
    {
        var package = RulesPackage();
        var wb = XlsxReader.Read(new MemoryStream(package));
        var ws = wb.Sheets[0];
        var existing = wb.Styles.DifferentialStyles[1];
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([G("H1:H5")],
            [new ConditionalRule.CellIs("equal", "\"x\"", null, Green, 7), new ConditionalRule.Expression("H1>0", existing, 8),
                new ConditionalRule.Duplicate(false, Green, 9), new ConditionalRule.TimePeriod("today", Bold, 10)])));
        wb.Execute(new EditConditionalFormattingCommand(ws, 0, ws.ConditionalFormats[0] with
        {
            Rules = [((ConditionalRule.CellIs)ws.ConditionalFormats[0].Rules[0]) with { Style = Red }, ws.ConditionalFormats[0].Rules[1]]
        }));
        wb.Execute(new AddDataValidationCommand(ws, new DataValidation([G("H1:H5")], ValidationType.Decimal, "lessThan", "=2.5", PromptTitle: "Note")));

        var written = Write(wb);
        var dxfs = StylesXml(written).Element(M + "dxfs")!;
        var before = StylesXml(package).Element(M + "dxfs")!.Elements().ToList();
        var now = dxfs.Elements().ToList();
        Assert.Equal("5", (string)dxfs.Attribute("count"));
        Assert.Equal(5, now.Count);
        Assert.True(XNode.DeepEquals(before[0], now[0]));
        Assert.True(XNode.DeepEquals(before[1], now[1]));
        var rules = Xml(written).Root!.Elements(M + "conditionalFormatting").SelectMany(c => c.Elements(M + "cfRule")).ToList();
        Assert.Equal(["2", "1", null, null, null, "0", "3", "1", "3", "4"], rules.Select(r => (string?)r.Attribute("dxfId")));
        Assert.Equal("FLOOR(H1,1)=TODAY()", rules[^1].Element(M + "formula")!.Value);
        var dv = Xml(written).Root!.Element(M + "dataValidations")!.Elements().Last();
        Assert.Equal(("decimal", "lessThan", "1", "1", "Note", "2.5"), ((string)dv.Attribute("type"), (string)dv.Attribute("operator"),
            (string)dv.Attribute("showInputMessage"), (string)dv.Attribute("allowBlank"), (string)dv.Attribute("promptTitle"), dv.Element(M + "formula1")!.Value));

        var again = XlsxReader.Read(new MemoryStream(written));
        var styles = again.Styles.DifferentialStyles;
        Assert.Equal(5, styles.Count);
        Assert.Equal(Red, styles[2]);
        Assert.Equal(Green, styles[3]);
        Assert.Equal(Bold, styles[4]);
        var added = again.Sheets[0].ConditionalFormats[2].Rules;
        Assert.Equal([Green, existing, Green, Bold], added.Select(r => r switch
        {
            ConditionalRule.CellIs c => c.Style,
            ConditionalRule.Expression e => e.Style,
            ConditionalRule.Duplicate d => d.Style,
            ConditionalRule.TimePeriod t => t.Style,
            _ => null
        }));

        var fresh = Workbook.Create();
        fresh.Execute(new AddConditionalFormattingCommand(fresh.Sheets[0], new ConditionalFormatting([G("A1:A3")], [new ConditionalRule.Top10(1, false, false, Red)])));
        var reopened = XlsxReader.Read(new MemoryStream(Write(fresh)));
        Assert.Equal([Red], reopened.Styles.DifferentialStyles);
        Assert.Equal(Red, ((ConditionalRule.Top10)reopened.Sheets[0].ConditionalFormats[0].Rules[0]).Style);
    }

    [Fact]
    public void IncompleteRules_RejectedByCommands_UnreadableRulesKeptOnWrite()
    {
        var (wb, ws) = Column(1);
        ConditionalFormatting On(params ConditionalRule[] rules) => new([G("A1:A3")], rules);
        Assert.Throws<ArgumentException>(() => wb.Execute(new AddConditionalFormattingCommand(ws, On())));
        Assert.Throws<ArgumentException>(() => wb.Execute(new AddConditionalFormattingCommand(ws, On(new ConditionalRule.Expression(" ", Red)))));
        Assert.Throws<ArgumentException>(() => wb.Execute(new AddConditionalFormattingCommand(ws, On(new ConditionalRule.TimePeriod("decade", Red)))));
        Assert.Throws<ArgumentException>(() => wb.Execute(new AddConditionalFormattingCommand(ws, On(new ConditionalRule.CellIs("between", "1", null, Red)))));
        wb.Execute(new AddConditionalFormattingCommand(ws, On(new ConditionalRule.Expression("A1>0", Red))));
        Assert.Throws<ArgumentException>(() => wb.Execute(new EditConditionalFormattingCommand(ws, 0, On())));
        Assert.Single(ws.ConditionalFormats);

        ws.ConditionalFormats = [On()];
        Assert.Null(Xml(Write(wb)).Root!.Element(M + "conditionalFormatting"));

        const string unreadable = "<conditionalFormatting sqref=\"B1:B5\" pivot=\"1\"><cfRule type=\"expression\" dxfId=\"0\" priority=\"1\"/>"
                                  + "<cfRule type=\"timePeriod\" priority=\"2\" timePeriod=\"decade\"><formula>B1&gt;0</formula></cfRule>"
                                  + "<cfRule type=\"futureRule\" priority=\"3\" x=\"1\"/></conditionalFormatting>"
                                  + "<conditionalFormatting sqref=\"C1\"><cfRule type=\"expression\" priority=\"4\"><formula>C1&gt;0</formula></cfRule><cfRule type=\"bogus\" priority=\"5\"/></conditionalFormatting>";
        var package = XlsxReadTests.Package([XlsxReadTests.Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row>", "", unreadable)]);
        var read = XlsxReader.Read(new MemoryStream(package));
        var sheet = read.Sheets[0];
        Assert.Equal([3, 2], sheet.ConditionalFormats.Select(c => c.Rules.Count));
        using (var cf = new ConditionalFormatEvaluator(read, sheet)) Assert.Null(cf.Evaluate(R("B1")));
        read.Execute(new SetValueCommand(sheet, R("A1"), CellValue.Number(2)));
        var original = Xml(package).Root!.Elements(M + "conditionalFormatting").ToList();
        var written = Xml(Write(read)).Root!.Elements(M + "conditionalFormatting").ToList();
        Assert.True(XNode.DeepEquals(original[0], written[0]));
        Assert.True(XNode.DeepEquals(original[1], written[1]));

        read.Execute(new InsertColumnsCommand(sheet, 0, 1));
        written = Xml(Write(read)).Root!.Elements(M + "conditionalFormatting").ToList();
        Assert.Equal(["C1:C5", "D1"], written.Select(w => (string)w.Attribute("sqref")));
        Assert.True(XNode.DeepEquals(original[0].Elements().First(), written[0].Elements().First()));
        Assert.Equal("D1>0", written[1].Elements().First().Value);
        Assert.True(XNode.DeepEquals(original[1].Elements().Last(), written[1].Elements().Last()));
    }

    [Fact]
    public void DifferentialStyle_AppliesOnlySetParts()
    {
        var baseStyle = new CellStyle
        {
            Font = new CellFont { Bold = true, Italic = true, Size = 14 },
            Fill = new CellFill { Color = "#FFFF00" },
            Border = new CellBorder { Top = new CellBorderSide { Style = CellBorderStyle.Thick } },
            NumberFormat = "0.00"
        };
        var colorOnly = new DifferentialStyle(new DifferentialFont { Color = "#FF0000" });
        Assert.Equal(baseStyle with { Font = baseStyle.Font with { Color = "#FF0000" } }, colorOnly.ApplyTo(baseStyle));
        var explicitOff = new DifferentialStyle(new DifferentialFont { Bold = false }, new CellFill(),
            new DifferentialBorder { Bottom = new CellBorderSide { Style = CellBorderStyle.Thin, Color = "#0000FF" } }, "0%");
        Assert.Equal(baseStyle with
        {
            Font = baseStyle.Font with { Bold = false },
            Fill = new CellFill(),
            Border = baseStyle.Border with { Bottom = new CellBorderSide { Style = CellBorderStyle.Thin, Color = "#0000FF" } },
            NumberFormat = "0%"
        }, explicitOff.ApplyTo(baseStyle));

        const string styles = "<dxfs count=\"2\"><dxf><font><color rgb=\"FFFF0000\"/></font></dxf>"
                              + "<dxf><font><b val=\"0\"/><u val=\"none\"/><sz val=\"9\"/></font><border><bottom style=\"thin\"/></border></dxf></dxfs>";
        var wb = XlsxReader.Read(new MemoryStream(XlsxReadTests.Package([XlsxReadTests.Sheet("")], styles: styles)));
        Assert.Equal(colorOnly, wb.Styles.DifferentialStyles[0]);
        Assert.Equal(new DifferentialStyle(new DifferentialFont { Bold = false, Underline = false, Size = 9 },
            Border: new DifferentialBorder { Bottom = new CellBorderSide { Style = CellBorderStyle.Thin } }), wb.Styles.DifferentialStyles[1]);

        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(5)));
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([G("A1")],
            [new ConditionalRule.CellIs("greaterThan", "1", null, colorOnly, 1), new ConditionalRule.CellIs("greaterThan", "0", null, explicitOff, 2)])));
        using var cf = new ConditionalFormatEvaluator(wb, ws);
        var merged = cf.Evaluate(R("A1"))!.Style;
        Assert.Equal(new DifferentialFont { Bold = false, Color = "#FF0000" }, merged.Font);
        Assert.Equal("0%", merged.NumberFormat);

        var again = XlsxReader.Read(new MemoryStream(Write(wb))).Styles.DifferentialStyles;
        Assert.Equal([colorOnly, wb.Styles.DifferentialStyles[1], explicitOff], again);
    }

    [Fact]
    public void DifferentialBorder_EmptySidesAreUnset()
    {
        const string styles = "<dxfs count=\"1\"><dxf><border><left/><right/><top/><bottom style=\"thin\"/></border></dxf></dxfs>";
        var wb = XlsxReader.Read(new MemoryStream(XlsxReadTests.Package([XlsxReadTests.Sheet("")], styles: styles)));
        var dxf = wb.Styles.DifferentialStyles[0];
        var left = new CellBorderSide { Style = CellBorderStyle.Medium };
        var applied = dxf.ApplyTo(new CellStyle { Border = new CellBorder { Left = left } });
        Assert.Equal(left, applied.Border.Left);
        Assert.Equal(CellBorderStyle.Thin, applied.Border.Bottom.Style);

        var none = new DifferentialStyle(Border: new DifferentialBorder { Left = new CellBorderSide { Style = CellBorderStyle.None } });
        var ws = wb.Sheets[0];
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([G("A1")], [new ConditionalRule.Expression("TRUE", none)])));
        Assert.Contains(none, XlsxReader.Read(new MemoryStream(Write(wb))).Styles.DifferentialStyles);
    }

    [Fact]
    public void Validation_AnyNeverRejects()
    {
        var (wb, ws) = Column(1);
        wb.Execute(new AddDataValidationCommand(ws, new DataValidation([G("B1:B3")], ValidationType.Any, AllowBlank: false)));
        Assert.True(DataValidator.Validate(wb, ws, R("B1"), CellValue.Empty).IsValid);
        Assert.True(DataValidator.Validate(wb, ws, R("B1"), CellValue.Text("x")).IsValid);
    }

    [Fact]
    public void RuleCommands_UndoRedo()
    {
        var (wb, ws) = Column(1);
        var first = new ConditionalFormatting([G("A1:A3")], [new ConditionalRule.Expression("A1>0", Red)]);
        var second = new ConditionalFormatting([G("B1:B3")], [new ConditionalRule.Expression("B1>0", Green)]);
        var dv1 = new DataValidation([G("C1")], ValidationType.Whole, "greaterThan", "0");
        var dv2 = new DataValidation([G("C2")], ValidationType.List, Formula1: "\"a,b\"");
        wb.Execute(new AddConditionalFormattingCommand(ws, first));
        wb.Execute(new EditConditionalFormattingCommand(ws, 0, second));
        wb.Execute(new DeleteConditionalFormattingCommand(ws, 0));
        wb.Execute(new AddDataValidationCommand(ws, dv1));
        wb.Execute(new EditDataValidationCommand(ws, 0, dv2));
        wb.Execute(new DeleteDataValidationCommand(ws, 0));

        (ConditionalFormatting[] Formats, DataValidation[] Validations)[] expected =
        [
            ([], []), ([first], []), ([second], []), ([], []), ([], [dv1]), ([], [dv2]), ([], [])
        ];
        void Check(int i)
        {
            Assert.Equal(expected[i].Formats, ws.ConditionalFormats);
            Assert.Equal(expected[i].Validations, ws.DataValidations);
        }

        for (var i = expected.Length - 2; i >= 0; i--)
        {
            wb.Undo.Undo();
            Check(i);
        }

        for (var i = 1; i < expected.Length; i++)
        {
            wb.Undo.Redo();
            Check(i);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => wb.Execute(new DeleteDataValidationCommand(ws, 0)));
    }

    [Fact]
    public void RenameAndDeleteSheet_ReachRuleFormulas()
    {
        var (wb, data) = Column(1, 2, 3);
        wb.Execute(new AddSheetCommand("Other"));
        var other = wb.Sheets[1];
        wb.Execute(new AddConditionalFormattingCommand(other, new ConditionalFormatting([G("A1")], [new ConditionalRule.Expression("Sheet1!$A$1>0", Red)])));
        wb.Execute(new AddDataValidationCommand(other, new DataValidation([G("B1")], ValidationType.List, Formula1: "=Sheet1!$A$1:$A$3")));
        string Formula() => ((ConditionalRule.Expression)other.ConditionalFormats[0].Rules[0]).Formula;

        Assert.Equal(["1", "2", "3"], DataValidator.ListItems(wb, other, other.DataValidations[0]));
        wb.Execute(new RenameSheetCommand(data, "My Data"));
        Assert.Equal("'My Data'!$A$1>0", Formula());
        Assert.Equal("='My Data'!$A$1:$A$3", other.DataValidations[0].Formula1);
        Assert.Equal(["1", "2", "3"], DataValidator.ListItems(wb, other, other.DataValidations[0]));

        wb.Execute(new DeleteSheetCommand(data));
        Assert.Equal("#REF!>0", Formula());
        Assert.Equal("=#REF!", other.DataValidations[0].Formula1);
        Assert.Empty(DataValidator.ListItems(wb, other, other.DataValidations[0]));

        wb.Undo.Undo();
        Assert.Equal("'My Data'!$A$1>0", Formula());
        wb.Undo.Undo();
        Assert.Equal("Sheet1!$A$1>0", Formula());
        Assert.Equal("=Sheet1!$A$1:$A$3", other.DataValidations[0].Formula1);
    }

    private static byte[] Write(Workbook wb)
    {
        var ms = new MemoryStream();
        XlsxWriter.Write(wb, ms);
        return ms.ToArray();
    }

    private static byte[] Entry(byte[] zip, string name)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        using var s = archive.GetEntry(name)!.Open();
        var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    private static XDocument Xml(byte[] zip) => XDocument.Load(new MemoryStream(Entry(zip, "xl/worksheets/sheet1.xml")));

    private static XElement StylesXml(byte[] zip) => XDocument.Load(new MemoryStream(Entry(zip, "xl/styles.xml"))).Root!;
}
