using MudBlazor.Extensions.Office.Spreadsheet;

namespace MainSample.WebAssembly.Examples.Spreadsheet;

public static class SpreadsheetFeatureSamples
{
    private static readonly string[] Regions = ["North", "South", "East", "West"];
    private static readonly string[] Products = ["Laptop", "Phone", "Tablet", "Monitor", "Dock", "Camera"];
    private static readonly string[] Months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug"];

    public static Workbook CreateOverview()
    {
        var wb = Workbook.Create("Sales");
        var sheet = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        using (wb.Undo.BeginTransaction("Create overview"))
        {
            Fill(wb, sheet, "A1", Header("Product", "Q1", "Q2", "Q3", "Total"), Products.Select((p, i) => Row(p, 120 + i * 45, 90 + i * 60, 200 - i * 25, "")).ToArray());
            SetFormulas(wb, sheet, "E", 2, 7, row => $"SUM(B{row}:D{row})");
            wb.Execute(new CreateTableCommand(sheet, CellRange.Parse("A1:E7"), true));
            wb.Execute(new ApplyTableFilterCommand(sheet, sheet.Tables[0].Name, 4, FilterCriteria.Custom(CustomFilterOperator.GreaterThanOrEqual, "500")));

            sheet.Text(wb, "G1", "Region");
            sheet.Text(wb, "H1", "North");
            sheet.Text(wb, "G2", "Grand total");
            wb.Execute(new SetFormulaCommand(sheet, CellRef.Parse("H2"), "SUM(E2:E7)"));
            wb.Execute(new AddDataValidationCommand(sheet, new DataValidation([CellRange.Parse("H1")], ValidationType.List, Formula1: "\"North,South,East,West\"",
                PromptTitle: "Region", Prompt: "Pick a region from the list.", ErrorTitle: "Unknown region", Error: "Choose one of the listed regions.")));
            wb.Execute(new AddConditionalFormattingCommand(sheet, new ConditionalFormatting([CellRange.Parse("B2:D7")], [Scale()])));
            wb.Execute(new SetNoteCommand(sheet, new CellNote(CellRef.Parse("A1"), "MudEx", "Filtered to totals of 500 and more. Open the filter button of Total to change it.")));
            sheet.Text(wb, "G4", "mudex.org");
            wb.Execute(new SetLinkCommand(sheet, new CellLink(CellRef.Parse("G4"), "https://mudex.org", null, "mudex.org", "Open the MudBlazor.Extensions site")));
            wb.Execute(new InsertChartCommand(sheet, CellRange.Parse("A1:D7"), ChartKind.Column));
            wb.Execute(new UpdateChartCommand(sheet, sheet.Charts[0].Id, ChartKind.Column, "Sales per quarter", true));
            wb.Execute(new ResizeCommand(sheet, Axis.Column, 0, 1, 110));
        }

        wb.Undo.Clear();
        return wb;
    }

    public static Workbook CreateData()
    {
        var wb = Workbook.Create("Table");
        var table = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        using (wb.Undo.BeginTransaction("Create data demo"))
        {
            Fill(wb, table, "A1", OrderHeader, Orders());
            SetFormulas(wb, table, "E", 2, 13, row => $"C{row}*D{row}");
            wb.Execute(new CreateTableCommand(table, CellRange.Parse("A1:E13"), true));
            var name = table.Tables[0].Name;
            wb.Execute(new SetTotalsRowCommand(table, name, true));
            wb.Execute(new SetTotalsFunctionCommand(table, name, "Amount", "sum"));
            wb.Execute(new SetStyleCommand(table, CellRange.Parse("D2:E14"), s => s with { NumberFormat = "#,##0.00" }));

            wb.Execute(new AddSheetCommand("Sort and filter"));
            var filter = wb.Sheets[1];
            Fill(wb, filter, "A1", OrderHeader, Orders());
            SetFormulas(wb, filter, "E", 2, 13, row => $"C{row}*D{row}");
            wb.Execute(new SetAutoFilterCommand(filter, CellRange.Parse("A1:E13")));
            wb.Execute(new SortCommand(filter, CellRange.Parse("A1:E13"), [new SortKey(0), new SortKey(4, true)], true));
            wb.Execute(new ApplyFilterCommand(filter, 2, FilterCriteria.Custom(CustomFilterOperator.GreaterThan, "5")));
            wb.Execute(new SetNoteCommand(filter, new CellNote(CellRef.Parse("G1"), "MudEx", "Sorted by Region, then Amount descending. Rows with a Qty of 5 or less are filtered out.")));
            wb.Execute(new SetStyleCommand(filter, CellRange.Parse("D2:E13"), s => s with { NumberFormat = "#,##0.00" }));

            wb.Execute(new AddSheetCommand("Validation"));
            var validation = wb.Sheets[2];
            Fill(wb, validation, "A1", Header("Task", "Status", "Priority", "Due day"),
                [Row("Design review", "Open", 2, 5), Row("Fix login", "Done", 1, 3), Row("Release notes", "Blocked", 3, 12), Row("Update docs", "", 3, 20)]);
            wb.Execute(new AddDataValidationCommand(validation, new DataValidation([CellRange.Parse("B2:B20")], ValidationType.List, Formula1: "\"Open,Done,Blocked\"",
                PromptTitle: "Status", Prompt: "Open, Done or Blocked")));
            wb.Execute(new AddDataValidationCommand(validation, new DataValidation([CellRange.Parse("C2:C20")], ValidationType.Whole, "between", "1", "5",
                ErrorTitle: "Priority", Error: "Enter a whole number from 1 to 5.")));
            wb.Execute(new AddDataValidationCommand(validation, new DataValidation([CellRange.Parse("D2:D20")], ValidationType.Decimal, "between", "1", "31",
                ErrorTitle: "Due day", Error: "Use a day from 1 to 31.", ErrorStyle: "warning")));
            wb.Execute(new AddConditionalFormattingCommand(validation, new ConditionalFormatting([CellRange.Parse("B2:B20")],
            [
                new ConditionalRule.CellIs("equal", "\"Done\"", null, Style("#C6EFCE", "#006100"), 1),
                new ConditionalRule.CellIs("equal", "\"Blocked\"", null, Style("#FFC7CE", "#9C0006"), 2)
            ])));
            wb.Execute(new ResizeCommand(validation, Axis.Column, 0, 1, 140));
        }

        wb.Undo.Clear();
        return wb;
    }

    public static Workbook CreateCharts()
    {
        var wb = Workbook.Create("Charts");
        var sheet = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        using (wb.Undo.BeginTransaction("Create charts demo"))
        {
            Fill(wb, sheet, "A1", Header("Month", "Revenue", "Costs", "Profit"),
                Months.Select((m, i) => Row(m, 1000 + i * 150 + (i % 3) * 80, 700 + i * 60, 0)).ToArray());
            SetFormulas(wb, sheet, "D", 2, 9, row => $"B{row}-C{row}");
            Fill(wb, sheet, "A12", Header("Region", "Revenue"), Regions.Select((r, i) => Row(r, 400 + i * 130)).ToArray());

            (string Range, ChartKind Kind, string Title, string From, string To)[] charts =
            [
                ("A1:C9", ChartKind.Column, "Revenue and costs", "F1", "N14"),
                ("A1:D9", ChartKind.Line, "Revenue, costs and profit", "F16", "N30"),
                ("A12:B16", ChartKind.Pie, "Revenue by region", "A19", "E34")
            ];
            foreach (var (range, kind, title, from, to) in charts)
            {
                wb.Execute(new InsertChartCommand(sheet, CellRange.Parse(range), kind));
                var id = sheet.Charts[^1].Id;
                wb.Execute(new UpdateChartCommand(sheet, id, kind, title, true));
                wb.Execute(new MoveChartCommand(sheet, id, CellRef.Parse(from), CellRef.Parse(to)));
            }

            wb.Execute(new SetStyleCommand(sheet, CellRange.Parse("B2:D9"), s => s with { NumberFormat = "#,##0" }));
        }

        wb.Undo.Clear();
        return wb;
    }

    private static CellValue[] OrderHeader => Header("Region", "Product", "Qty", "Price", "Amount");

    private static CellValue[][] Orders()
    {
        var rnd = new Random(7);
        return Enumerable.Range(0, 12)
            .Select(i => Row(Regions[i % Regions.Length], Products[i * 5 % Products.Length], rnd.Next(1, 13), Math.Round(20 + rnd.NextDouble() * 480, 2), ""))
            .ToArray();
    }

    private static ConditionalRule Scale() => new ConditionalRule.ColorScale(
    [
        new ColorScaleStop(new ConditionalValue(ConditionalValueType.Min), "#F8696B"),
        new ColorScaleStop(new ConditionalValue(ConditionalValueType.Percentile, "50"), "#FFEB84"),
        new ColorScaleStop(new ConditionalValue(ConditionalValueType.Max), "#63BE7B")
    ]);

    private static DifferentialStyle Style(string fill, string font) =>
        new(new DifferentialFont { Color = font }, new CellFill { Color = fill });

    private static CellValue[] Header(params string[] names) => names.Select(CellValue.Text).ToArray();

    private static CellValue[] Row(params object[] values) => values.Select(v => v switch
    {
        string { Length: 0 } => CellValue.Empty,
        string s => CellValue.Text(s),
        _ => CellValue.Number(Convert.ToDouble(v))
    }).ToArray();

    private static void Fill(Workbook wb, Worksheet sheet, string origin, CellValue[] header, CellValue[][] rows)
    {
        var values = new CellValue[rows.Length + 1, header.Length];
        for (var c = 0; c < header.Length; c++)
        {
            values[0, c] = header[c];
            for (var r = 0; r < rows.Length; r++)
                values[r + 1, c] = rows[r][c];
        }

        wb.Execute(new SetValuesCommand(sheet, CellRef.Parse(origin), values));
    }

    private static void SetFormulas(Workbook wb, Worksheet sheet, string column, int first, int last, Func<int, string> formula)
    {
        for (var row = first; row <= last; row++)
            wb.Execute(new SetFormulaCommand(sheet, CellRef.Parse(column + row), formula(row)));
    }

    private static void Text(this Worksheet sheet, Workbook wb, string a1, string text) =>
        wb.Execute(new SetValueCommand(sheet, CellRef.Parse(a1), CellValue.Text(text)));
}
