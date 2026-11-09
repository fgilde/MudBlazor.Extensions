using System.Globalization;
using System.Text;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MainSample.WebAssembly.Examples.Spreadsheet;

public static class SpreadsheetSamples
{
    public static Workbook CreateBudget()
    {
        var wb = Workbook.Create("Budget");
        var sheet = wb.Sheets[0];
        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"];
        (string Name, double[] Values)[] rows =
        [
            ("Rent", [1200, 1200, 1200, 1200, 1200, 1200]),
            ("Groceries", [420, 395.5, 440, 410, 455.25, 430]),
            ("Transport", [95, 80, 110, 90, 130, 85]),
            ("Insurance", [210, 210, 210, 210, 210, 210]),
            ("Leisure", [150, 320, 90, 275, 180, 400]),
            ("Savings", [300, 300, 350, 350, 400, 400])
        ];

        using (wb.Undo.BeginTransaction("Create budget"))
        {
            sheet.Set(wb, "A1", "Category");
            for (var m = 0; m < months.Length; m++)
                sheet.Set(wb, new CellRef(0, m + 1).ToA1(), months[m]);
            sheet.Set(wb, "H1", "Total");
            sheet.Set(wb, "I1", "Average");
            sheet.Set(wb, "J1", "Status");

            for (var r = 0; r < rows.Length; r++)
            {
                var row = r + 2;
                sheet.Set(wb, $"A{row}", rows[r].Name);
                for (var m = 0; m < months.Length; m++)
                    wb.Execute(new SetValueCommand(sheet, new CellRef(row - 1, m + 1), CellValue.Number(rows[r].Values[m])));
                wb.Execute(new SetFormulaCommand(sheet, CellRef.Parse($"H{row}"), $"SUM(B{row}:G{row})"));
                wb.Execute(new SetFormulaCommand(sheet, CellRef.Parse($"I{row}"), $"AVERAGE(B{row}:G{row})"));
                wb.Execute(new SetFormulaCommand(sheet, CellRef.Parse($"J{row}"), $"IF(H{row}>2000,\"High\",\"OK\")"));
            }

            sheet.Set(wb, "A8", "Total");
            foreach (var col in "BCDEFGH")
                wb.Execute(new SetFormulaCommand(sheet, CellRef.Parse($"{col}8"), $"SUM({col}2:{col}7)"));
            wb.Execute(new SetFormulaCommand(sheet, CellRef.Parse("I8"), "AVERAGE(I2:I7)"));

            sheet.Set(wb, "A10", "Lookup category");
            sheet.Set(wb, "B10", "Insurance");
            sheet.Set(wb, "A11", "Yearly total");
            wb.Execute(new SetFormulaCommand(sheet, CellRef.Parse("B11"), "XLOOKUP(B10,A2:A7,H2:H7,\"not found\")"));

            sheet.Set(wb, "A13", "Categories above 2000 (FILTER spill)");
            wb.Execute(new SetFormulaCommand(sheet, CellRef.Parse("A14"), "FILTER(A2:A7,H2:H7>2000,\"none\")"));

            wb.Execute(new SetStyleCommand(sheet, CellRange.Parse("A1:J1"), s => s with
            {
                Font = s.Font with { Bold = true, Color = "#FFFFFF" },
                Fill = new CellFill { Color = "#217346" },
                Alignment = s.Alignment with { Horizontal = CellHorizontalAlignment.Center }
            }));
            wb.Execute(new SetStyleCommand(sheet, CellRange.Parse("B2:I8"), s => s with { NumberFormat = "#,##0.00" }));
            wb.Execute(new SetStyleCommand(sheet, CellRange.Parse("A8:J8"), s => s with
            {
                Font = s.Font with { Bold = true },
                Border = s.Border with { Top = new CellBorderSide { Style = CellBorderStyle.Thin, Color = "#000000" } }
            }));
            wb.Execute(new SetStyleCommand(sheet, CellRange.Parse("A13"), s => s with { Font = s.Font with { Italic = true } }));
            wb.Execute(new SetStyleCommand(sheet, CellRange.Parse("B11"), s => s with { Font = s.Font with { Bold = true }, NumberFormat = "#,##0.00" }));
            wb.Execute(new ResizeCommand(sheet, Axis.Column, 0, 1, 130));
            wb.Execute(new FreezeCommand(sheet, CellRef.Parse("B2")));
        }

        wb.Undo.Clear();
        return wb;
    }

    public static Workbook CreateLarge(int rows = 100_000, int columns = 30)
    {
        var sb = new StringBuilder(rows * columns * 6);
        for (var c = 0; c < columns; c++)
            sb.Append(c == 0 ? "" : ",").Append(c == 0 ? "Id" : $"Col{c}");
        sb.Append('\n');
        var rnd = new Random(42);
        for (var r = 1; r <= rows; r++)
        {
            sb.Append(r);
            for (var c = 1; c < columns; c++)
                sb.Append(',').Append(rnd.Next(0, 10_000).ToString(CultureInfo.InvariantCulture));
            sb.Append('\n');
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        var wb = CsvFormat.Read(stream, ',');
        wb.Undo.Clear();
        return wb;
    }

    private static void Set(this Worksheet sheet, Workbook wb, string a1, string text) =>
        wb.Execute(new SetValueCommand(sheet, CellRef.Parse(a1), CellValue.Text(text)));
}
