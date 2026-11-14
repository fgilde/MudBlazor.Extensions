using System.Globalization;
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
    public void AddSheet_DoesNotRecalculateFormulas()
    {
        var (wb, ws, engine) = Create();
        for (var r = 1; r <= 50; r++)
        {
            wb.Execute(new SetValueCommand(ws, R("A" + r), CellValue.Number(r)));
            wb.Execute(new SetFormulaCommand(ws, R("B" + r), $"=A{r}*2"));
        }

        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=Later!A1+1"));
        var before = engine.EvaluationCount;
        wb.Execute(new AddSheetCommand("Empty"));
        wb.Execute(new MoveSheetCommand(wb.Sheets[1], 0));
        Assert.Equal(0, engine.EvaluationCount - before);
        wb.Execute(new AddSheetCommand("Later"));
        Assert.Equal(1, engine.EvaluationCount - before);
        Assert.Equal(CellValue.Number(1), ValueAt(ws, "C1"));
    }

    [Fact]
    public void InsertRow_RecalculatesOnlyFormulasWhoseResultCanChange()
    {
        var (wb, ws, engine) = Create();
        for (var r = 1; r <= 10; r++)
        {
            wb.Execute(new SetValueCommand(ws, R("A" + r), CellValue.Number(r)));
            wb.Execute(new SetFormulaCommand(ws, R("B" + r), $"=A{r}*2"));
        }

        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=SUM(A1:A10)"));
        wb.Execute(new SetFormulaCommand(ws, R("D1"), "=C1+1"));
        wb.Execute(new SetFormulaCommand(ws, R("E20"), "=ROW()"));
        wb.Execute(new SetFormulaCommand(ws, R("F1"), "=COUNTBLANK(A1:A10)"));
        var before = engine.EvaluationCount;

        wb.Execute(new InsertRowsCommand(ws, 4, 1));
        Assert.Equal(4, engine.EvaluationCount - before);
        Assert.Equal(CellValue.Number(20), ValueAt(ws, "B11"));
        Assert.Equal(CellValue.Number(55), ValueAt(ws, "C1"));
        Assert.Equal(CellValue.Number(56), ValueAt(ws, "D1"));
        Assert.Equal(CellValue.Number(21), ValueAt(ws, "E21"));
        Assert.Equal(CellValue.Number(1), ValueAt(ws, "F1"));

        before = engine.EvaluationCount;
        wb.Undo.Undo();
        Assert.Equal(4, engine.EvaluationCount - before);
        Assert.Equal(CellValue.Number(20), ValueAt(ws, "B10"));
        Assert.Equal(CellValue.Number(20), ValueAt(ws, "E20"));
        Assert.Equal(CellValue.Number(0), ValueAt(ws, "F1"));

        before = engine.EvaluationCount;
        wb.Execute(new InsertRowsCommand(ws, 30, 2));
        Assert.Equal(0, engine.EvaluationCount - before);
    }

    [Fact]
    public void DeleteRow_UndoRestoresReferencesThroughCellsAndNames()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new SetValueCommand(ws, R("A5"), CellValue.Number(7)));
        wb.Execute(new SetValueCommand(ws, R("A6"), CellValue.Number(8)));
        wb.Execute(new DefineNameCommand("Five", "Sheet1!$A$5"));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=A5"));
        wb.Execute(new SetFormulaCommand(ws, R("B2"), "=Five"));
        wb.Execute(new SetFormulaCommand(ws, R("B3"), "=A6"));
        wb.Execute(new SetFormulaCommand(ws, R("A10"), "=A5+1"));

        wb.Execute(new DeleteRowsCommand(ws, 4, 1));
        Assert.Equal(CellValue.Error(CellError.Ref), ValueAt(ws, "B1"));
        Assert.Equal(CellValue.Error(CellError.Ref), ValueAt(ws, "B2"));
        Assert.Equal(CellValue.Number(8), ValueAt(ws, "B3"));
        Assert.Equal(CellValue.Error(CellError.Ref), ValueAt(ws, "A9"));

        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(7), ValueAt(ws, "B1"));
        Assert.Equal(CellValue.Number(7), ValueAt(ws, "B2"));
        Assert.Equal(CellValue.Number(8), ValueAt(ws, "B3"));
        Assert.Equal(CellValue.Number(8), ValueAt(ws, "A10"));
        wb.Execute(new SetValueCommand(ws, R("A5"), CellValue.Number(1)));
        Assert.Equal(CellValue.Number(1), ValueAt(ws, "B2"));
        Assert.Equal(CellValue.Number(2), ValueAt(ws, "A10"));
    }

    [Fact]
    public void InsertRow_InsideSpillAndAboveIntersection_Respills()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new AddSheetCommand("Data"));
        var data = wb.Sheets[1];
        for (var r = 1; r <= 10; r++)
        {
            wb.Execute(new SetValueCommand(data, R("A" + r), CellValue.Number(r * 10)));
            wb.Execute(new SetValueCommand(data, R("B" + r), CellValue.Text("<=" + r * 10)));
        }
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=SEQUENCE(5)"));
        wb.Execute(new SetFormulaCommand(ws, R("C5"), "=COUNTIF(Data!A1:A10,Data!B1:B10)"));
        Assert.Equal(CellValue.Number(5), ValueAt(ws, "C5"));

        wb.Execute(new InsertRowsCommand(ws, 2, 1));
        for (var r = 1; r <= 5; r++) Assert.Equal(CellValue.Number(r), ValueAt(ws, "A" + r));
        Assert.Null(ws[R("A6")]);
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "C6"));

        wb.Undo.Undo();
        for (var r = 1; r <= 5; r++) Assert.Equal(CellValue.Number(r), ValueAt(ws, "A" + r));
        Assert.Null(ws[R("A6")]);
        Assert.Equal(CellValue.Number(5), ValueAt(ws, "C5"));

        wb.Execute(new DeleteRowsCommand(ws, 1, 1));
        for (var r = 1; r <= 5; r++) Assert.Equal(CellValue.Number(r), ValueAt(ws, "A" + r));
        Assert.Equal(CellValue.Number(4), ValueAt(ws, "C4"));
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(120, 60)]
    public void SetValues_RecalculatesDependentsAndDropsReplacedFormulas(int rows, int columns)
    {
        var (wb, ws, engine) = Create();
        var last = new CellRef(rows - 1, columns - 1).ToA1();
        var below = new CellRef(rows + 2, 0).ToA1();
        var right = new CellRef(0, columns + 2).ToA1();
        wb.Execute(new SetFormulaCommand(ws, R("B2"), "=7"));
        wb.Execute(new SetFormulaCommand(ws, R(below), $"=SUM(A1:{last})"));
        wb.Execute(new SetFormulaCommand(ws, R(right), "=B2*2"));
        wb.Execute(new SetFormulaCommand(ws, new CellRef(rows + 5, 5), "=A1+1"));
        wb.Execute(new SetFormulaCommand(ws, new CellRef(rows - 1, columns + 4), "=SEQUENCE(1,1)"));
        wb.Execute(new SetFormulaCommand(ws, new CellRef(rows - 2, columns - 1), "=SEQUENCE(3)"));
        Assert.Equal(CellValue.Number(14), ValueAt(ws, right));

        var values = new CellValue[rows, columns];
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < columns; c++)
                values[r, c] = CellValue.Number(1);
        var before = engine.EvaluationCount;
        wb.Execute(new SetValuesCommand(ws, R("A1"), values));
        Assert.Equal(CellValue.Number(rows * columns), ValueAt(ws, below));
        Assert.Equal(CellValue.Number(2), ValueAt(ws, right));
        Assert.Equal(CellValue.Number(2), ws[new CellRef(rows + 5, 5)].Value);
        Assert.Null(ws[R("B2")].Formula);
        Assert.Equal(CellValue.Number(1), ws[new CellRef(rows - 2, columns - 1)].Value);
        Assert.Null(ws[new CellRef(rows, columns - 1)]);
        Assert.Equal(3, engine.EvaluationCount - before);

        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(14), ValueAt(ws, right));
        Assert.Equal(CellValue.Number(10), ValueAt(ws, below));
        Assert.Equal(CellValue.Number(3), ws[new CellRef(rows, columns - 1)].Value);
    }

    [Fact]
    public void Shift_RecalculatesRowAndColumnOfMovedReferences()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new AddSheetCommand("Other"));
        var other = wb.Sheets[1];
        for (var r = 5; r <= 8; r++)
        {
            wb.Execute(new SetValueCommand(ws, R("A" + r), CellValue.Number(r)));
            wb.Execute(new SetValueCommand(ws, R("B" + r), CellValue.Text("<=" + r)));
        }

        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=COLUMN(D1)"));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=ROW(A5)"));
        wb.Execute(new SetFormulaCommand(ws, R("E1"), "=SUMPRODUCT(ROW(A5:A9))"));
        wb.Execute(new SetFormulaCommand(other, R("B6"), "=COUNTIF(Sheet1!A5:A8,Sheet1!B5:B8)"));

        void Expect(double column, double row, double rows, double count)
        {
            Assert.Equal(CellValue.Number(column), ValueAt(ws, "A1"));
            Assert.Equal(CellValue.Number(row), ValueAt(ws, "B1"));
            Assert.Equal(CellValue.Number(rows), ValueAt(ws, "E1"));
            Assert.Equal(CellValue.Number(count), ValueAt(other, "B6"));
        }

        Expect(4, 5, 35, 2);
        wb.Execute(new InsertRowsCommand(ws, 1, 1));
        Expect(4, 6, 40, 1);
        wb.Execute(new InsertColumnsCommand(ws, 1, 1));
        Assert.Equal(CellValue.Number(5), ValueAt(ws, "A1"));
        wb.Undo.Undo();
        Expect(4, 6, 40, 1);
        wb.Undo.Undo();
        Expect(4, 5, 35, 2);

        wb.Execute(new DeleteRowsCommand(ws, 1, 1));
        Expect(4, 4, 30, 3);
        wb.Execute(new DeleteColumnsCommand(ws, 1, 1));
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "A1"));
        wb.Undo.Undo();
        wb.Undo.Undo();
        Expect(4, 5, 35, 2);
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

    [Theory]
    [InlineData("X", "X+X", null, null)]
    [InlineData("A", "B+B", "B", "A+A")]
    [InlineData("A", "B+D", "B", "D+D")]
    public void DefinedNames_BranchingCycles_YieldCircQuickly(string n1, string t1, string n2, string t2)
    {
        var (wb, ws, _) = Create();
        if (n2 != null) wb.Execute(new DefineNameCommand(n2, t2));
        if (t2 == "D+D") wb.Execute(new DefineNameCommand("D", "A+A"));
        var task = Task.Run(() =>
        {
            wb.Execute(new DefineNameCommand(n1, t1));
            wb.Execute(new SetFormulaCommand(ws, R("A1"), "=" + n1));
        });
        Assert.True(task.Wait(TimeSpan.FromSeconds(10)), "Evaluating a self-referencing name did not finish.");
        Assert.Equal(CellValue.Error(CellError.Circ), ValueAt(ws, "A1"));
    }

    [Fact]
    public void DefinedNames_BranchingChain_IsEvaluatedOncePerName()
    {
        var (wb, ws, _) = Create();
        var task = Task.Run(() =>
        {
            using (wb.Undo.BeginTransaction("names"))
            {
                for (var i = 0; i < 29; i++) wb.Execute(new DefineNameCommand("Step_" + i, $"Step_{i + 1}+Step_{i + 1}"));
                wb.Execute(new DefineNameCommand("Step_29", "1"));
            }

            wb.Execute(new SetFormulaCommand(ws, R("A1"), "=Step_0"));
        });
        Assert.True(task.Wait(TimeSpan.FromSeconds(10)), "Evaluating a branching name chain did not finish.");
        Assert.Equal(CellValue.Number(1 << 29), ValueAt(ws, "A1"));
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

    [Fact]
    public void Comparison_UsesFifteenSignificantDigits()
    {
        Assert.Equal(CellValue.Boolean(true), Eval("=0.1+0.2=0.3"));
        Assert.Equal(CellValue.Boolean(false), Eval("=0.1+0.2<>0.3"));
        Assert.Equal(CellValue.Boolean(false), Eval("=0.1+0.2>0.3"));
        Assert.Equal(CellValue.Boolean(true), Eval("=0.1+0.2>=0.3"));
        Assert.Equal(CellValue.Boolean(false), Eval("=1.00000000000001=1"));
        Assert.Equal(CellValue.Boolean(true), Eval("=1.000000000000001=1"));

        var (wb, ws, _) = Create();
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=0.1+0.2"));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=MATCH(0.3,A1:A1,0)"));
        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=COUNTIF(A1:A1,0.3)"));
        wb.Execute(new SetFormulaCommand(ws, R("D1"), "=XLOOKUP(0.3,A1:A1,A1:A1,\"none\")"));
        Assert.Equal(CellValue.Number(1), ValueAt(ws, "B1"));
        Assert.Equal(CellValue.Number(1), ValueAt(ws, "C1"));
        Assert.Equal(CellValueKind.Number, ValueAt(ws, "D1").Kind);
    }

    [Fact]
    public void DefinedNameUnion_ShiftsOnInsertAndRenamesWithSheet()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new DefineNameCommand("Print_Titles", "Sheet1!$A:$A,Sheet1!$1:$1"));

        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        Assert.Equal("Sheet1!$A:$A,Sheet1!$2:$2", wb.DefinedNames["Print_Titles"]);
        wb.Execute(new InsertColumnsCommand(ws, 0, 2));
        Assert.Equal("Sheet1!$C:$C,Sheet1!$2:$2", wb.DefinedNames["Print_Titles"]);
        wb.Execute(new RenameSheetCommand(ws, "Data"));
        Assert.Equal("Data!$C:$C,Data!$2:$2", wb.DefinedNames["Print_Titles"]);

        wb.Undo.Undo();
        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal("Sheet1!$A:$A,Sheet1!$1:$1", wb.DefinedNames["Print_Titles"]);
    }

    [Fact]
    public void UnparsableFormula_KnownReferencesStillShift()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new SetFormulaCommand(ws, R("Z1"), "=SUM(Table1[Col])+A5"));
        wb.Execute(new InsertRowsCommand(ws, 2, 1));
        Assert.Equal("SUM(Table1[Col])+A6", ws[R("Z1")].Formula);
        wb.Undo.Undo();
        Assert.Equal("SUM(Table1[Col])+A5", ws[R("Z1")].Formula);

        Assert.Equal("SUM( Table1[[#This Row],[A1]] ) + A6", ReferenceShifter.ShiftRows("SUM( Table1[[#This Row],[A1]] ) + A5", "Sheet1", 0, 1));
        Assert.Equal("[@A1]*B3", ReferenceShifter.ShiftRows("[@A1]*B2", "Sheet1", 0, 1));
        Assert.Equal("Data!Local+Data!A1", ReferenceShifter.RenameSheet("Sheet1!Local+Sheet1!A1", "Sheet1", "Data"));
        Assert.Equal("#REF!+1", ReferenceShifter.RemoveSheet("'My Sheet'!Local+1", "My Sheet"));
        Assert.Equal("SUM(Sheet1:Last!A1)", ReferenceShifter.RenameSheet("SUM(Sheet1:Sheet3!A1)", "Sheet3", "Last"));
        Assert.Equal("SUM('First Sheet:Sheet3'!A1)", ReferenceShifter.RenameSheet("SUM(Sheet1:Sheet3!A1)", "Sheet1", "First Sheet"));
        Assert.Equal("SUM(Sheet1:Sheet3!A1)", ReferenceShifter.ShiftRows("SUM(Sheet1:Sheet3!A1)", "Sheet1", 0, 1));
        Assert.Equal("A2#+1", ReferenceShifter.ShiftRows("A1#+1", "Sheet1", 0, 1));
        Assert.Equal("SUM(A2, 1.50)", ReferenceShifter.ShiftRows("SUM(A1, 1.50)", "Sheet1", 0, 1));
        Assert.Equal("\"unterminated", ReferenceShifter.ShiftRows("\"unterminated", "Sheet1", 0, 1));
    }

    [Fact]
    public void StructurePrefilter_SkipsFormulasThatCannotReferenceTheSheet()
    {
        Assert.False(ReferenceShifter.CanReference("A1+SUM(B:B)", "Sheet1"));
        Assert.False(ReferenceShifter.CanReference("Other!A1", "Sheet1"));
        Assert.True(ReferenceShifter.CanReference("sheet1!A1", "Sheet1"));
        Assert.True(ReferenceShifter.CanReference("'Bob''s'!A1", "Bob's"));
        Assert.Equal("A5", ReferenceShifter.Shift("A5", Axis.Row, "Sheet1", 0, 1, "Other"));
        Assert.Equal("Sheet1!A6", ReferenceShifter.Shift("Sheet1!A5", Axis.Row, "Sheet1", 0, 1, "Other"));
    }

    [Fact]
    public void WholeColumnArrayOperation_IsCappedToUsedRange()
    {
        var (wb, ws, _) = Create();
        for (var i = 0; i < 10; i++) wb.Execute(new SetValueCommand(ws, new CellRef(i, 0), CellValue.Number(i + 1)));
        var before = GC.GetAllocatedBytesForCurrentThread();
        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=SUM(A:A*2)"));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(CellValue.Number(110), ValueAt(ws, "C1"));
        Assert.True(allocated < 4_000_000, $"allocated {allocated} bytes");
    }

    [Fact]
    public void FunctionException_YieldsValueErrorWithoutStoppingRecalc()
    {
        var functions = new FunctionRegistry();
        functions.Register(new DelegateFunction(new FunctionInfo("BOOM", "Test", "BOOM()", "Throws."), 0, 0,
            (_, _) => throw new InvalidOperationException("boom")));
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        _ = new CalcEngine(wb, functions);
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=BOOM()+A1"));
        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=A1+1"));
        Assert.Equal(CellValue.Error(CellError.Value), ValueAt(ws, "B1"));
        Assert.Equal(CellValue.Number(2), ValueAt(ws, "C1"));
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(5)));
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "C1"));
    }

    [Fact]
    public void DefaultRegistry_IsReadOnly_CopyAcceptsCustomFunctions()
    {
        var custom = new DelegateFunction(new FunctionInfo("TWICE", "Test", "TWICE(x)", "Doubles."), 1, 1,
            (ctx, a) => CalcValue.Number(ctx.ToScalar(a[0].Value).NumberValue * 2));
        Assert.Throws<InvalidOperationException>(() => FunctionRegistry.Default.Register(custom));
        Assert.False(FunctionRegistry.Default.TryGet("TWICE", out _));

        var functions = FunctionRegistry.Default.Copy();
        functions.Register(custom);
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        _ = new CalcEngine(wb, functions);
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=TWICE(SUM(1,2))"));
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "A1"));
    }

    [Fact]
    public void InsufficientStackInFunction_YieldsValueError()
    {
        var functions = new FunctionRegistry();
        functions.Register(new DelegateFunction(new FunctionInfo("DEEP", "Test", "DEEP()", "Throws."), 0, 0,
            (_, _) => throw new InsufficientExecutionStackException()));
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        _ = new CalcEngine(wb, functions);
        wb.Execute(new SetFormulaCommand(ws, R("A1"), "=DEEP()"));
        Assert.Equal(CellValue.Error(CellError.Value), ValueAt(ws, "A1"));
    }

    [Fact]
    public void NegativeZero_IsNormalised()
    {
        var v = Eval("=0*-1");
        Assert.Equal(CellValueKind.Number, v.Kind);
        Assert.False(double.IsNegative(v.NumberValue));
        Assert.Equal("0", v.ToString());
        Assert.Equal(CellValue.Text("x0"), Eval("=\"x\"&-0"));
        Assert.False(double.IsNegative(CellValue.Number(-0.0).NumberValue));
    }

    [Fact]
    public void RangeOfRanges_TracksBoundingBox()
    {
        var (wb, ws, _) = Create();
        wb.Execute(new SetFormulaCommand(ws, R("E1"), "=SUM(A1:A2:C3)"));
        Assert.Equal(CellValue.Number(0), ValueAt(ws, "E1"));
        wb.Execute(new SetValueCommand(ws, R("C3"), CellValue.Number(4)));
        Assert.Equal(CellValue.Number(4), ValueAt(ws, "E1"));
        wb.Execute(new SetValueCommand(ws, R("B2"), CellValue.Number(3)));
        Assert.Equal(CellValue.Number(7), ValueAt(ws, "E1"));
    }

    [Fact]
    public void ReplacedOrClearedFormula_DropsWideDependencies()
    {
        var (wb, ws, engine) = Create();
        wb.Execute(new SetFormulaCommand(ws, R("A5"), "=SUM(1:1)"));
        wb.Execute(new SetFormulaCommand(ws, R("A6"), "=SUM(1:1)"));
        wb.Execute(new SetFormulaCommand(ws, R("A5"), "=5"));
        wb.Execute(new ClearCommand(ws, new CellRange(R("A6"), R("A6"))));
        var before = engine.EvaluationCount;
        wb.Execute(new SetValueCommand(ws, R("B1"), CellValue.Number(1)));
        Assert.Equal(0, engine.EvaluationCount - before);
    }

    [Fact]
    public void Dispose_StopsFollowingTheWorkbook()
    {
        var (wb, ws, engine) = Create();
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=A1"));
        engine.Dispose();
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(2)));
        Assert.Equal(CellValue.Number(1), ValueAt(ws, "B1"));
    }

    [Fact]
    public void NumberToText_IsSharedAndFollowsCurrentCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal(CellValue.Text("x1,5"), Eval("=\"x\"&1.5"));
            Assert.Equal(CellValue.Text("x1,5"), Eval("=CONCAT(\"x\",1.5)"));
            Assert.Equal(CellValue.Text("1234567,5|0,3"), Eval("=TEXTJOIN(\"|\",TRUE,1234567.5,0.1+0.2)"));
            Assert.Equal(CellValue.Text("0,3"), Eval("=\"\"&(0.1+0.2)"));
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal(CellValue.Text("x1.5"), Eval("=CONCAT(\"x\",1.5)"));
            Assert.Equal(CellValue.Text("1234567.5"), Eval("=\"\"&1234567.5"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
