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
        wb.Execute(new DefineNameCommand("TaxRate", "Sheet1!$A$1"));
        wb.Execute(new DefineNameCommand("Double", "TaxRate*2"));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=TaxRate*3"));
        wb.Execute(new SetFormulaCommand(ws, R("B2"), "=SUM(taxrate,Double)"));
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("C1"), null, "TaxRate", null, null)));
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([CellRange.Parse("A1")],
            [new ConditionalRule.Expression("A1>=TaxRate", new DifferentialStyle(Fill: new CellFill { Color = "#FF0000" }))])));

        wb.Execute(new RenameNameCommand("TAXRATE", "Price"));
        Assert.Equal(["Double", "Price"], wb.DefinedNames.Keys.Order());
        Assert.Equal("Price*2", wb.DefinedNames["Double"]);
        Assert.Equal(("Price*3", "SUM(Price,Double)", "Price"), (ws[R("B1")].Formula, ws[R("B2")].Formula, ws.LinkAt(R("C1"))!.Location));
        Assert.Equal("A1>=Price", ((ConditionalRule.Expression)ws.ConditionalFormats[0].Rules[0]).Formula);
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(6)));
        Assert.Equal((CellValue.Number(18), CellValue.Number(18)), (ws[R("B1")].Value, ws[R("B2")].Value));

        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(["Double", "TaxRate"], wb.DefinedNames.Keys.Order());
        Assert.Equal(("TaxRate*2", "TaxRate*3", "TaxRate"), (wb.DefinedNames["Double"], ws[R("B1")].Formula, ws.LinkAt(R("C1"))!.Location));
        Assert.Equal(CellValue.Number(15), ws[R("B2")].Value);

        Assert.Throws<ArgumentException>(() => wb.Execute(new RenameNameCommand("TaxRate", "double")));
        Assert.Throws<ArgumentException>(() => wb.Execute(new RenameNameCommand("Missing", "Other")));
    }

    [Theory]
    [InlineData("Q1")]
    [InlineData("R1C1")]
    [InlineData("RC")]
    [InlineData("C")]
    [InlineData("A$1")]
    [InlineData("2024")]
    [InlineData("Total Sales")]
    [InlineData("TRUE")]
    [InlineData("XFD1048576")]
    [InlineData("")]
    public void InvalidNames_AreRejected(string name)
    {
        var wb = Workbook.Create();
        wb.Execute(new DefineNameCommand("Tax", "Sheet1!$A$1"));
        wb.Execute(new SetFormulaCommand(wb.Sheets[0], R("B1"), "=Tax*2"));
        Assert.False(Workbook.IsValidName(name));
        Assert.Throws<ArgumentException>(() => wb.Execute(new RenameNameCommand("Tax", name)));
        Assert.Throws<ArgumentException>(() => wb.Execute(new DefineNameCommand(name, "Sheet1!$A$2")));
        Assert.Equal(("Tax*2", 1), (wb.Sheets[0][R("B1")].Formula, wb.DefinedNames.Count));
    }

    [Theory]
    [InlineData("Tax_2024")]
    [InlineData("_hidden")]
    [InlineData(@"\back")]
    [InlineData("Sales.Total")]
    [InlineData("_xlnm.Print_Titles")]
    public void ValidNames_AreAccepted(string name)
    {
        Assert.True(Workbook.IsValidName(name));
        var wb = Workbook.Create();
        wb.Execute(new DefineNameCommand(name, "Sheet1!$A$1"));
        Assert.True(wb.DefinedNames.ContainsKey(name));
    }

    [Fact]
    public void FunctionNames_AreValidNames_LikeExcel()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(4)));
        wb.Execute(new DefineNameCommand("Tax", "Sheet1!$A$1"));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=Tax*2+SUM(1,2)"));
        wb.Execute(new RenameNameCommand("Tax", "Sum"));
        Assert.Equal(("Sum*2+SUM(1,2)", CellValue.Number(11)), (ws[R("B1")].Formula, ws[R("B1")].Value));
    }

    [Fact]
    public void RenameName_RefusedWhenASheetNameShadowsIt()
    {
        var package = XlsxReadTests.Package([XlsxReadTests.Sheet("<row r=\"1\"><c r=\"A1\"><f>Tax*2</f><v>2</v></c></row>")],
            workbookExtra: "<definedNames><definedName name=\"Tax\">Sheet1!$B$1</definedName><definedName name=\"Tax\" localSheetId=\"0\">Sheet1!$C$1</definedName></definedNames>");
        var wb = XlsxReader.Read(new MemoryStream(package));
        Assert.Throws<ArgumentException>(() => wb.Execute(new RenameNameCommand("Tax", "Levy")));
        Assert.Equal("Tax*2", wb.Sheets[0][R("A1")].Formula);
    }
}
