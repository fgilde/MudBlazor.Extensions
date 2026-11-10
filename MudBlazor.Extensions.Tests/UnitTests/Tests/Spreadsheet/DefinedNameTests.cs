using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class DefinedNameTests
{
    private static CellRef R(string a1) => CellRef.Parse(a1);

    [Fact]
    public void RenameName_RewritesFormulasNamesRulesAndLinks()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(5)));
        wb.Execute(new DefineNameCommand("Rate", "Sheet1!$A$1"));
        wb.Execute(new DefineNameCommand("Double", "Rate*2"));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=Rate*3"));
        wb.Execute(new SetFormulaCommand(ws, R("B2"), "=SUM(rate,Double)"));
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("C1"), null, "Rate", null, null)));
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([CellRange.Parse("A1")],
            [new ConditionalRule.Expression("A1>=Rate", new DifferentialStyle(Fill: new CellFill { Color = "#FF0000" }))])));

        wb.Execute(new RenameNameCommand("RATE", "Price"));
        Assert.Equal(["Double", "Price"], wb.DefinedNames.Keys.Order());
        Assert.Equal("Price*2", wb.DefinedNames["Double"]);
        Assert.Equal(("Price*3", "SUM(Price,Double)", "Price"), (ws[R("B1")].Formula, ws[R("B2")].Formula, ws.LinkAt(R("C1"))!.Location));
        Assert.Equal("A1>=Price", ((ConditionalRule.Expression)ws.ConditionalFormats[0].Rules[0]).Formula);
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(6)));
        Assert.Equal((CellValue.Number(18), CellValue.Number(18)), (ws[R("B1")].Value, ws[R("B2")].Value));

        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(["Double", "Rate"], wb.DefinedNames.Keys.Order());
        Assert.Equal(("Rate*2", "Rate*3", "Rate"), (wb.DefinedNames["Double"], ws[R("B1")].Formula, ws.LinkAt(R("C1"))!.Location));
        Assert.Equal(CellValue.Number(15), ws[R("B2")].Value);

        Assert.Throws<ArgumentException>(() => wb.Execute(new RenameNameCommand("Rate", "double")));
        Assert.Throws<ArgumentException>(() => wb.Execute(new RenameNameCommand("Missing", "Other")));
    }
}
