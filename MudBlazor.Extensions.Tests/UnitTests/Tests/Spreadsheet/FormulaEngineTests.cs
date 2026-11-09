using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class FormulaEngineTests
{
    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static CellValue ValueAt(Worksheet ws, string a1) => ws[R(a1)]?.Value ?? default;

    private static (Workbook Wb, Worksheet Ws, CalcEngine Engine) Create(string sheet = "Sheet1")
    {
        var wb = Workbook.Create(sheet);
        return (wb, wb.Sheets[0], new CalcEngine(wb));
    }

    private static CellValue Eval(string formula)
    {
        var (wb, ws, _) = Create();
        wb.Execute(new SetFormulaCommand(ws, R("Z1"), formula));
        return ValueAt(ws, "Z1");
    }

    [Theory]
    [InlineData("=-2^2", 4)]
    [InlineData("=2^-1", 0.5)]
    [InlineData("=1+2*3", 7)]
    [InlineData("=10%*5", 0.5)]
    [InlineData("=(1+2)*3", 9)]
    [InlineData("=2^3^2", 64)]
    [InlineData("=-10%", -0.1)]
    [InlineData("=1+2=3", 0)]
    public void Parser_RespectsExcelPrecedence(string formula, double expected)
    {
        var v = Eval(formula);
        if (formula == "=1+2=3")
        {
            Assert.Equal(CellValue.Boolean(true), v);
            return;
        }

        Assert.Equal(CellValueKind.Number, v.Kind);
        Assert.Equal(expected, v.NumberValue, 10);
    }

    [Fact]
    public void Parser_ConcatBindsLooserThanArithmetic()
    {
        Assert.Equal(CellValue.Text("a2"), Eval("=\"a\"&1+1"));
        Assert.Equal(CellValue.Text("say \"hi\""), Eval("=\"say \"\"hi\"\"\""));
    }

    [Fact]
    public void Parser_AcceptsSemicolonSeparator()
    {
        Assert.Equal(CellValue.Number(3), Eval("=SUM(1;2)"));
        Assert.Equal("SUM(1,2)", FormulaParser.Format(FormulaParser.Parse("=SUM(1;2)")));
        Assert.Equal("IF(A1>0,\"x\",)", FormulaParser.Format(FormulaParser.Parse("if(a1>0;\"x\";)")));
        Assert.Equal(CellValue.Number(0), Eval("=IF(FALSE,1,)"));
        Assert.Equal(CellValue.Boolean(false), Eval("=IF(FALSE,1)"));
    }

    [Fact]
    public void Parser_SheetQualifiedAndQuotedRefs()
    {
        var node = FormulaParser.Parse("='My Sheet'!A1:B2");
        var range = Assert.IsType<RangeNode>(node);
        Assert.Equal("My Sheet", range.Sheet);
        Assert.Equal(CellRange.Parse("A1:B2"), range.Range);
        Assert.Equal("'My Sheet'!A1:B2", FormulaParser.Format(node));

        var quoted = Assert.IsType<RefNode>(FormulaParser.Parse("'It''s'!$C$3"));
        Assert.Equal("It's", quoted.Sheet);
        Assert.Equal(new CellRef(2, 2, true, true), quoted.Cell);
        Assert.Equal("'It''s'!$C$3", FormulaParser.Format(quoted));

        Assert.Equal("SUM(Data!A:A,1:1,(A1,B2),A1:B2 B1:C3)", FormulaParser.Format(FormulaParser.Parse("sum(Data!A:A, 1:1, (A1,B2), A1:B2 B1:C3)")));
        Assert.Equal("{1,-2;\"x\",TRUE}", FormulaParser.Format(FormulaParser.Parse("{1,-2;\"x\",TRUE}")));
        Assert.Equal("LOG10(A1)+Total", FormulaParser.Format(FormulaParser.Parse("LOG10(A1)+Total")));

        var (wb, ws, _) = Create();
        wb.Execute(new AddSheetCommand("My Sheet"));
        var other = wb.Sheets[1];
        wb.Execute(new SetValueCommand(other, R("A1"), CellValue.Number(1)));
        wb.Execute(new SetValueCommand(other, R("B2"), CellValue.Number(4)));
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=SUM('My Sheet'!A1:B2)"));
        Assert.Equal(CellValue.Number(5), ValueAt(ws, "A1"));
        wb.Execute(new SetValueCommand(other, R("A2"), CellValue.Number(10)));
        Assert.Equal(CellValue.Number(15), ValueAt(ws, "A1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("=1+")]
    [InlineData("=SUM(1,2")]
    [InlineData("=\"open")]
    [InlineData("=1 2")]
    [InlineData("={1,2;3}")]
    [InlineData("=A1)")]
    public void Parser_RejectsInvalidFormulas(string formula)
    {
        Assert.False(FormulaParser.TryParse(formula, out _, out var error));
        Assert.False(string.IsNullOrEmpty(error));
        Assert.Throws<FormatException>(() => FormulaParser.Parse(formula));
    }

    [Fact]
    public void Parser_LongestFormulaAndDeepNesting_AreBounded()
    {
        var terms = (FormulaParser.MaxLength - 1) / 2;
        var longest = "=1" + string.Concat(Enumerable.Repeat("+1", terms - 1));
        Assert.Equal(CellValue.Number(terms), Eval(longest));
        Assert.False(FormulaParser.TryParse(longest + "+1+1", out _, out _));
        Assert.False(FormulaParser.TryParse(new string('(', 100) + "1" + new string(')', 100), out _, out _));
        Assert.Equal(CellValue.Number(1), Eval("=" + new string('(', 60) + "1" + new string(')', 60)));
    }

    [Fact]
    public void Engine_InvalidFormulaOrUnknownFunction_YieldsNameError()
    {
        Assert.Equal(CellValue.Error(CellError.Name), Eval("=SUMME(1,2)"));
        Assert.Equal(CellValue.Error(CellError.Name), Eval("=1+"));
        Assert.Equal(CellValue.Error(CellError.Name), Eval("=NoSuchName"));
    }

    [Fact]
    public void Engine_RecalculatesOnlyDependents()
    {
        var (wb, ws, engine) = Create();
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=A1*2"));
        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=B1+1"));
        wb.Execute(new SetValueCommand(ws, R("D1"), CellValue.Number(10)));
        wb.Execute(new SetFormulaCommand(ws, R("E1"), "=D1+1"));
        wb.Execute(new SetFormulaCommand(ws, R("F1"), "=SUM(A1:A3)"));
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "C1"));
        Assert.Equal(CellValue.Number(11), ValueAt(ws, "E1"));

        var before = engine.EvaluationCount;
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(5)));
        Assert.Equal(3, engine.EvaluationCount - before);
        Assert.Equal(CellValue.Number(11), ValueAt(ws, "C1"));
        Assert.Equal(CellValue.Number(5), ValueAt(ws, "F1"));

        before = engine.EvaluationCount;
        wb.Execute(new SetValueCommand(ws, R("A2"), CellValue.Number(1)));
        Assert.Equal(1, engine.EvaluationCount - before);
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "F1"));

        before = engine.EvaluationCount;
        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("A1:F1"), CellStyle.Default with { Font = new CellFont { Bold = true } }));
        Assert.Equal(0, engine.EvaluationCount - before);

        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(5), ValueAt(ws, "F1"));
        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "C1"));
    }

    [Fact]
    public void Engine_WholeColumnRange_UsesStoredCellsOnly()
    {
        var (wb, ws, engine) = Create();
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
        wb.Execute(new SetValueCommand(ws, R("A1000000"), CellValue.Number(2)));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=SUM(A:A)"));
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "B1"));
        var before = engine.EvaluationCount;
        wb.Execute(new SetValueCommand(ws, R("A500000"), CellValue.Number(4)));
        Assert.Equal(CellValue.Number(7), ValueAt(ws, "B1"));
        Assert.Equal(1, engine.EvaluationCount - before);
    }

    [Fact]
    public void Engine_CircularReference_YieldsCircError()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=B1+1"));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=A1+1"));
        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=A1"));
        wb.Execute(new SetFormulaCommand(ws, R("D1"), "=D1"));
        var circ = CellValue.Error(CellError.Circ);
        Assert.Equal(circ, ValueAt(ws, "A1"));
        Assert.Equal(circ, ValueAt(ws, "B1"));
        Assert.Equal(circ, ValueAt(ws, "C1"));
        Assert.Equal(circ, ValueAt(ws, "D1"));

        wb.Execute(new SetValueCommand(ws, R("B1"), CellValue.Number(5)));
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "A1"));
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "C1"));
    }

    [Fact]
    public void Engine_LongChain_NoStackOverflow()
    {
        const int n = 10_000;
        var (wb, ws, engine) = Create();
        using (wb.Undo.BeginTransaction("chain"))
        {
            wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
            for (var i = 2; i <= n; i++)
                wb.Execute(new SetFormulaCommand(ws, new CellRef(i - 1, 0), $"=A{i - 1}+1"));
        }

        Assert.Equal(CellValue.Number(n), ValueAt(ws, "A" + n));
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(101)));
        Assert.Equal(CellValue.Number(n + 100), ValueAt(ws, "A" + n));
        engine.RecalculateAll();
        Assert.Equal(CellValue.Number(n + 100), ValueAt(ws, "A" + n));

        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=A" + n));
        Assert.Equal(CellValue.Error(CellError.Circ), ValueAt(ws, "A" + n));
    }

    [Fact]
    public void InsertRow_ShiftsFormulasAcrossSheets()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new AddSheetCommand("Other"));
        var other = wb.Sheets[1];
        wb.Execute(new SetValueCommand(ws, R("A5"), CellValue.Number(7)));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=A5*2"));
        wb.Execute(new SetFormulaCommand(ws, R("B2"), "=SUM(A1:A10)+$A$1"));
        wb.Execute(new SetFormulaCommand(other, R("A1"), "=Sheet1!A5+A5"));
        wb.Execute(new DefineNameCommand("Block", "Sheet1!$A$3:$A$6"));

        wb.Execute(new InsertRowsCommand(ws, 2, 2));
        Assert.Equal("A7*2", ws[R("B1")].Formula);
        Assert.Equal("SUM(A1:A12)+$A$1", ws[R("B2")].Formula);
        Assert.Equal("Sheet1!A7+A5", other[R("A1")].Formula);
        Assert.Equal("Sheet1!$A$5:$A$8", wb.DefinedNames["Block"]);
        Assert.Equal(CellValue.Number(14), ValueAt(ws, "B1"));
        Assert.Equal(CellValue.Number(7), ValueAt(other, "A1"));

        wb.Execute(new InsertColumnsCommand(ws, 0, 1));
        Assert.Equal("B7*2", ws[R("C1")].Formula);
        Assert.Equal("Sheet1!B7+A5", other[R("A1")].Formula);

        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal("A5*2", ws[R("B1")].Formula);
        Assert.Equal("SUM(A1:A10)+$A$1", ws[R("B2")].Formula);
        Assert.Equal("Sheet1!A5+A5", other[R("A1")].Formula);
        Assert.Equal("Sheet1!$A$3:$A$6", wb.DefinedNames["Block"]);
        Assert.Equal(CellValue.Number(14), ValueAt(ws, "B1"));

        wb.Undo.Redo();
        Assert.Equal("A7*2", ws[R("B1")].Formula);
    }

    [Fact]
    public void DeleteReferencedRow_YieldsRefError()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new SetValueCommand(ws, R("A2"), CellValue.Number(3)));
        wb.Execute(new SetValueCommand(ws, R("A3"), CellValue.Number(4)));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=A2*2"));
        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=SUM(A1:A3)"));
        wb.Execute(new SetFormulaCommand(ws, R("D1"), "=A3"));
        wb.Execute(new DefineNameCommand("Gone", "Sheet1!$A$2"));

        wb.Execute(new DeleteRowsCommand(ws, 1, 1));
        Assert.Equal("#REF!*2", ws[R("B1")].Formula);
        Assert.Equal(CellValue.Error(CellError.Ref), ValueAt(ws, "B1"));
        Assert.Equal("SUM(A1:A2)", ws[R("C1")].Formula);
        Assert.Equal(CellValue.Number(4), ValueAt(ws, "C1"));
        Assert.Equal("A2", ws[R("D1")].Formula);
        Assert.Equal("#REF!", wb.DefinedNames["Gone"]);

        wb.Undo.Undo();
        Assert.Equal("A2*2", ws[R("B1")].Formula);
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "B1"));
        Assert.Equal("SUM(A1:A3)", ws[R("C1")].Formula);
        Assert.Equal("Sheet1!$A$2", wb.DefinedNames["Gone"]);
    }

    [Fact]
    public void DeleteSheet_TurnsReferencesIntoRefErrorAndUndoRestores()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new AddSheetCommand("Data"));
        var data = wb.Sheets[1];
        wb.Execute(new SetValueCommand(data, R("A1"), CellValue.Number(2)));
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=Data!A1+1"));
        wb.Execute(new DefineNameCommand("Src", "Data!$A$1"));
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "A1"));

        wb.Execute(new DeleteSheetCommand(data));
        Assert.Equal("#REF!+1", ws[R("A1")].Formula);
        Assert.Equal(CellValue.Error(CellError.Ref), ValueAt(ws, "A1"));
        Assert.Equal("#REF!", wb.DefinedNames["Src"]);

        wb.Undo.Undo();
        Assert.Equal("Data!A1+1", ws[R("A1")].Formula);
        Assert.Equal("Data!$A$1", wb.DefinedNames["Src"]);
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "A1"));
    }

    [Fact]
    public void RenameSheet_UpdatesFormulas()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new AddSheetCommand("Other"));
        var other = wb.Sheets[1];
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
        wb.Execute(new SetFormulaCommand(other, R("A1"), "=Sheet1!A1+1"));
        wb.Execute(new SetFormulaCommand(other, R("A2"), "=Sheet10!A1"));

        wb.Execute(new RenameSheetCommand(ws, "My Data"));
        Assert.Equal("'My Data'!A1+1", other[R("A1")].Formula);
        Assert.Equal("Sheet10!A1", other[R("A2")].Formula);
        Assert.Equal(CellValue.Number(2), ValueAt(other, "A1"));

        wb.Undo.Undo();
        Assert.Equal("Sheet1!A1+1", other[R("A1")].Formula);
        Assert.Equal(CellValue.Number(2), ValueAt(other, "A1"));
    }

    [Fact]
    public void AddingMissingSheet_ResolvesReference()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=Later!A1+1"));
        Assert.Equal(CellValue.Error(CellError.Ref), ValueAt(ws, "A1"));
        wb.Execute(new AddSheetCommand("Later"));
        Assert.Equal(CellValue.Number(1), ValueAt(ws, "A1"));
    }

    [Fact]
    public void DefinedNames_AreEvaluatedAndTracked()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(2)));
        wb.Execute(new DefineNameCommand("Rate", "Sheet1!$A$1"));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=Rate*10"));
        Assert.Equal(CellValue.Number(20), ValueAt(ws, "B1"));
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(3)));
        Assert.Equal(CellValue.Number(30), ValueAt(ws, "B1"));
        wb.Execute(new DefineNameCommand("Self", "Self+1"));
        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=Self"));
        Assert.Equal(CellValueKind.Error, ValueAt(ws, "C1").Kind);
    }

    [Fact]
    public void Errors_Propagate()
    {
        Assert.Equal(CellValue.Error(CellError.Div0), Eval("=1/0"));
        Assert.Equal(CellValue.Error(CellError.Num), Eval("=0^0"));
        Assert.Equal(CellValue.Error(CellError.NA), Eval("=#N/A+1"));

        var (wb, ws, _) = Create();
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Text("abc")));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=A1+1"));
        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=1/0"));
        wb.Execute(new SetFormulaCommand(ws, R("D1"), "=SUM(C1,1)"));
        wb.Execute(new SetFormulaCommand(ws, R("E1"), "=IF(C1>0,1,2)"));
        wb.Execute(new SetFormulaCommand(ws, R("F1"), "=IF(TRUE,5,C1)"));
        Assert.Equal(CellValue.Error(CellError.Value), ValueAt(ws, "B1"));
        Assert.Equal(CellValue.Error(CellError.Div0), ValueAt(ws, "C1"));
        Assert.Equal(CellValue.Error(CellError.Div0), ValueAt(ws, "D1"));
        Assert.Equal(CellValue.Error(CellError.Div0), ValueAt(ws, "E1"));
        Assert.Equal(CellValue.Number(5), ValueAt(ws, "F1"));
    }

    [Fact]
    public void Operators_CoerceAndCompareLikeExcel()
    {
        Assert.Equal(CellValue.Number(0), Eval("=Y1"));
        Assert.Equal(CellValue.Boolean(true), Eval("=\"abc\"=\"ABC\""));
        Assert.Equal(CellValue.Boolean(true), Eval("=\"a\">1"));
        Assert.Equal(CellValue.Boolean(true), Eval("=TRUE>\"z\""));
        Assert.Equal(CellValue.Number(2), Eval("=TRUE+1"));
        Assert.Equal(CellValue.Boolean(true), Eval("=Y1=0"));
        Assert.Equal(CellValue.Number(10), Eval("=SUM({1,2;3,4})"));
        Assert.Equal(CellValue.Number(5), Eval("=SUM(({1,2}+1))"));
        Assert.Equal(CellValue.Error(CellError.Null), Eval("=SUM(A1:A2 B1:B2)"));
    }

    [Fact]
    public void ReferenceShifter_ShiftsAndInvalidates()
    {
        Assert.Equal("A6+B1", ReferenceShifter.ShiftRows("A5+B1", "Sheet1", 2, 1));
        Assert.Equal("SUM(A2:A4)", ReferenceShifter.ShiftRows("SUM(A2:A5)", "Sheet1", 2, -1));
        Assert.Equal("#REF!", ReferenceShifter.ShiftRows("A3", "Sheet1", 2, -1));
        Assert.Equal("SUM(A:A)", ReferenceShifter.ShiftRows("SUM(A:A)", "Sheet1", 0, 5));
        Assert.Equal("Other!A5", ReferenceShifter.ShiftRows("Other!A5", "Sheet1", 0, 5));
        Assert.Equal("C1+$D$2", ReferenceShifter.ShiftColumns("B1+$C$2", "Sheet1", 1, 1));
        Assert.Equal("  A1 + 1", ReferenceShifter.ShiftRows("  A1 + 1", "Sheet1", 5, 1));
        Assert.Equal("'New Name'!A1", ReferenceShifter.RenameSheet("old!A1", "OLD", "New Name"));
    }
}
