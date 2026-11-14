using System.Diagnostics;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;
using Xunit.Abstractions;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

/// <summary>Runs only with MUDEX_PERF=1, e.g. <c>dotnet test -c Debug -p:Optimize=true --filter Category=Perf</c>.</summary>
public sealed class PerfFactAttribute : FactAttribute
{
    public PerfFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("MUDEX_PERF") != "1") Skip = "Set MUDEX_PERF=1 to run performance measurements.";
    }
}

[Trait("Category", "Perf")]
public class SpreadsheetPerfTests(ITestOutputHelper output)
{
    internal const int Rows = 100_000;

    internal static (Workbook Wb, Worksheet Ws) SumSheet(int rows = Rows)
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var values = new CellValue[rows, 4];
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < 4; c++)
                values[r, c] = CellValue.Number(r + c);
        wb.Execute(new SetValuesCommand(ws, new CellRef(0, 0), values));
        using (wb.Undo.BeginTransaction("Formulas"))
            for (var r = 1; r <= rows; r++)
                wb.Execute(new SetFormulaCommand(ws, new CellRef(r - 1, 4), $"=SUM(A{r}:D{r})"));
        wb.Undo.Clear();
        return (wb, ws);
    }

    [PerfFact]
    public void InsertRowAtTop_100kSumFormulas()
    {
        var (wb, ws) = SumSheet();
        var notified = 0L;
        wb.Changed += (_, _) => notified = Stopwatch.GetTimestamp();
        var engine = new CalcEngine(wb);
        var calculated = 0L;
        wb.Changed += (_, _) => calculated = Stopwatch.GetTimestamp();

        for (var i = 0; i < 5; i++)
        {
            var start = Stopwatch.GetTimestamp();
            wb.Execute(new InsertRowsCommand(ws, 0, 1));
            var end = Stopwatch.GetTimestamp();
            output.WriteLine($"insert: total {Ms(start, end)} ms, command {Ms(start, notified)} ms, engine {Ms(notified, calculated)} ms");
            start = Stopwatch.GetTimestamp();
            wb.Undo.Undo();
            end = Stopwatch.GetTimestamp();
            output.WriteLine($"undo:   total {Ms(start, end)} ms, command {Ms(start, notified)} ms, engine {Ms(notified, calculated)} ms");
        }

        Assert.Equal(CellValue.Number(6), ws[CellRef.Parse("E1")].Value);
        GC.KeepAlive(engine);
    }

    private static double Ms(long from, long to) => Math.Round(Stopwatch.GetElapsedTime(from, to).TotalMilliseconds, 1);
}
