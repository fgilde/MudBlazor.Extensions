using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class StructureRecalculationTests
{
    private static readonly string[] Formulas =
    [
        "=SUM(A1:A8)", "=A3*2", "=$A$2+B1", "=ROW()", "=COLUMN()+A1", "=SEQUENCE(3,2)", "=Data!A1:A6", "=COUNTIF(Data!A1:A9,Data!B1:B9)",
        "=Data!A4+1", "=COUNTBLANK(A1:D6)", "=Rng*2", "=SUM(B:B)", "=INDEX(A1:A9,3)", "=A1:A9*1", "=IFERROR(C2+1,0)", "=SUM(2:2)",
        "=ROW(A5)", "=COLUMN(D1)", "=SUMPRODUCT(ROW(A5:A9))", "=COUNTIF(Data!A1:A9,Data!A1:A9)"
    ];

    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static CellValue ValueAt(Worksheet ws, string a1) => ws[R(a1)]?.Value ?? default;

    [Fact]
    public void InsertRowAbove100kFormulas_ReindexesNothing()
    {
        var (wb, ws) = SpreadsheetPerfTests.SumSheet();
        var engine = new CalcEngine(wb);
        var indexed = engine.GraphIndexOperations;
        var evaluations = engine.EvaluationCount;

        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        Assert.Equal(indexed, engine.GraphIndexOperations);
        Assert.Equal(evaluations, engine.EvaluationCount);
        Assert.Equal("SUM(A50001:D50001)", ws[R("E50001")].Formula);

        wb.Execute(new SetValueCommand(ws, R("A50001"), CellValue.Number(1000)));
        Assert.Equal(CellValue.Number(151003), ValueAt(ws, "E50001"));
        Assert.Equal(evaluations + 1, engine.EvaluationCount);

        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(indexed, engine.GraphIndexOperations);
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "E1"));
        wb.Execute(new SetValueCommand(ws, R("D2"), CellValue.Number(10)));
        Assert.Equal(CellValue.Number(16), ValueAt(ws, "E2"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(211)]
    [InlineData(216)]
    public void StructureChanges_MatchFullRecalculation(int seed)
    {
        var rnd = new Random(seed);
        var (wb, ws, data) = RandomWorkbook(rnd);
        for (var step = 0; step < 60; step++)
        {
            var sheet = rnd.Next(4) == 0 ? data : ws;
            if (!UndoOrRedo(wb, rnd)) Structure(wb, sheet, rnd);
            AssertMatchesFullRecalculation(wb);
        }
    }

    [Fact]
    public void UndoDeleteRow_RestoresShrunkRangeAndItsValue()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        _ = new CalcEngine(wb);
        for (var r = 1; r <= 8; r++) wb.Execute(new SetValueCommand(ws, R("A" + r), CellValue.Number(r)));
        wb.Execute(new SetFormulaCommand(ws, R("C2"), "=SUM(A1:A8)"));

        wb.Execute(new DeleteRowsCommand(ws, 0, 1));
        Assert.Equal(CellValue.Number(35), ValueAt(ws, "C1"));
        wb.Undo.Undo();
        Assert.Equal("SUM(A1:A8)", ws[R("C2")].Formula);
        Assert.Equal(CellValue.Number(36), ValueAt(ws, "C2"));
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(11)));
        Assert.Equal(CellValue.Number(46), ValueAt(ws, "C2"));
    }

    [Fact]
    public void MixedTransaction_NoFullRecalc()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var engine = new CalcEngine(wb);
        using (wb.Undo.BeginTransaction("Fill"))
            for (var r = 1; r <= 200; r++)
            {
                wb.Execute(new SetValueCommand(ws, R("A" + r), CellValue.Number(r)));
                wb.Execute(new SetFormulaCommand(ws, R("B" + r), $"=A{r}*2"));
            }

        var before = engine.EvaluationCount;
        using (wb.Undo.BeginTransaction("Mixed"))
        {
            wb.Execute(new SetValueCommand(ws, R("A5"), CellValue.Number(100)));
            wb.Execute(new InsertRowsCommand(ws, 0, 1));
            wb.Execute(new SetValueCommand(ws, R("A11"), CellValue.Number(50)));
            wb.Execute(new DeleteRowsCommand(ws, 2, 1));
            wb.Execute(new SetFormulaCommand(ws, R("C1"), "=SUM(B1:B4)"));
        }

        Assert.Equal(before + 3, engine.EvaluationCount);
        Assert.Equal(CellValue.Number(200), ValueAt(ws, "B5"));
        Assert.Equal(CellValue.Number(100), ValueAt(ws, "B10"));
        Assert.Equal(CellValue.Number(16), ValueAt(ws, "C1"));
        AssertMatchesFullRecalculation(wb);

        before = engine.EvaluationCount;
        wb.Undo.Undo();
        Assert.True(engine.EvaluationCount - before <= 3);
        Assert.Equal(CellValue.Number(10), ValueAt(ws, "B5"));
        AssertMatchesFullRecalculation(wb);
        wb.Undo.Redo();
        Assert.Equal(CellValue.Number(100), ValueAt(ws, "B10"));
        AssertMatchesFullRecalculation(wb);
    }

    [Fact]
    public void LayoutChanges_RecalculateHiddenSensitiveFormulasOnlyWhenVisibilityChanges()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var engine = new CalcEngine(wb);
        for (var r = 1; r <= 5; r++) wb.Execute(new SetValueCommand(ws, R("A" + r), CellValue.Number(r)));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=SUBTOTAL(109,A1:A5)"));
        wb.Execute(new SetFormulaCommand(ws, R("B2"), "=SUM(A1:A5)"));
        wb.Execute(new SetFormulaCommand(ws, R("B3"), "=B1*2"));
        var before = engine.EvaluationCount;

        wb.Execute(new ResizeCommand(ws, Axis.Row, 0, 1, 30));
        wb.Execute(new ResizeCommand(ws, Axis.Column, 0, 1, 80));
        wb.Execute(new FreezeCommand(ws, R("B2")));
        Assert.Equal(before, engine.EvaluationCount);

        wb.Execute(new SetHiddenCommand(ws, Axis.Row, 1, 1, true));
        Assert.Equal((CellValue.Number(13), CellValue.Number(26)), (ValueAt(ws, "B1"), ValueAt(ws, "B3")));
        Assert.Equal(before + 2, engine.EvaluationCount);
        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(15), ValueAt(ws, "B1"));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(102)]
    [InlineData(116)]
    [InlineData(1045)]
    public void MixedTransactions_MatchFullRecalculation(int seed)
    {
        var rnd = new Random(seed);
        var (wb, ws, data) = RandomWorkbook(rnd);
        for (var step = 0; step < 40; step++)
        {
            if (!UndoOrRedo(wb, rnd))
                using (wb.Undo.BeginTransaction("Mixed"))
                    for (var i = rnd.Next(2, 6); i > 0; i--)
                    {
                        var sheet = rnd.Next(4) == 0 ? data : ws;
                        if (rnd.Next(2) == 0) Structure(wb, sheet, rnd);
                        else Edit(wb, sheet, rnd);
                    }

            AssertMatchesFullRecalculation(wb);
        }
    }

    private static (Workbook Wb, Worksheet Ws, Worksheet Data) RandomWorkbook(Random rnd)
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        _ = new CalcEngine(wb);
        wb.Execute(new AddSheetCommand("Data"));
        var data = wb.Sheets[1];
        for (var r = 1; r <= 9; r++)
        {
            wb.Execute(new SetValueCommand(data, R("A" + r), CellValue.Number(r)));
            wb.Execute(new SetValueCommand(data, R("B" + r), CellValue.Text(">" + r % 4)));
        }

        wb.Execute(new DefineNameCommand("Rng", "Sheet1!$A$4"));
        for (var r = 3; r <= 7; r++) wb.Execute(new SetFormulaCommand(data, R("D" + r), "=COUNTIF(Sheet1!A1:A9,Sheet1!A1:A9)+ROW(Sheet1!C4)"));
        using (wb.Undo.BeginTransaction("Bulk"))
            for (var r = 21; r <= 5020; r++)
                wb.Execute(new SetFormulaCommand(ws, R("H" + r), $"=A{r - 18}+Data!A{r % 9 + 1}+SUM(G{r}:G{r + 2})"));
        for (var i = 0; i < 40; i++) Edit(wb, ws, rnd);
        return (wb, ws, data);
    }

    private static void Edit(Workbook wb, Worksheet sheet, Random rnd)
    {
        var at = new CellRef(rnd.Next(0, 12), rnd.Next(0, 6));
        if (rnd.Next(3) == 0) wb.Execute(new SetValueCommand(sheet, at, CellValue.Number(rnd.Next(1, 50))));
        else wb.Execute(new SetFormulaCommand(sheet, at, Formulas[rnd.Next(Formulas.Length)]));
    }

    private static void Structure(Workbook wb, Worksheet sheet, Random rnd)
    {
        var index = rnd.Next(0, 10);
        var count = rnd.Next(1, 3);
        switch (rnd.Next(4))
        {
            case 0: wb.Execute(new InsertRowsCommand(sheet, index, count)); break;
            case 1: wb.Execute(new DeleteRowsCommand(sheet, index, count)); break;
            case 2: wb.Execute(new InsertColumnsCommand(sheet, index % 5, count)); break;
            default: wb.Execute(new DeleteColumnsCommand(sheet, index % 5, count)); break;
        }
    }

    private static bool UndoOrRedo(Workbook wb, Random rnd)
    {
        switch (rnd.Next(6))
        {
            case 0:
                if (wb.Undo.CanUndo) wb.Undo.Undo();
                return true;
            case 1:
                if (wb.Undo.CanRedo) wb.Undo.Redo();
                return true;
            default:
                return false;
        }
    }

    private static void AssertMatchesFullRecalculation(Workbook wb)
    {
        var incremental = Snapshot(wb);
        new CalcEngine(wb).Dispose();
        Assert.Equal(Snapshot(wb), incremental);
    }

    private static string Snapshot(Workbook book) => string.Join("\n", book.Sheets.SelectMany(sh => sh.UsedCells()
        .Where(c => c.Cell.Value.Kind != CellValueKind.Empty || c.Cell.Formula != null)
        .OrderBy(c => c.Ref.Key).Select(c => $"{sh.Name}!{c.Ref.ToA1()}={c.Cell.Value}|{c.Cell.Formula}")));
}
