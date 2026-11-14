using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class SpreadsheetDialogsTests : IAsyncLifetime
{
    private readonly List<BunitContext> _contexts = [];
    private IRenderedComponent<MudPopoverProvider> _popovers;
    private IRenderedComponent<MudDialogProvider> _dialogs;
    private CultureInfo _culture;

    public Task InitializeAsync()
    {
        _culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        CultureInfo.CurrentCulture = _culture;
        foreach (var context in _contexts)
            await context.DisposeAsync();
    }

    private BunitContext NewContext()
    {
        var context = new BunitContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        return context;
    }

    private IRenderedComponent<MudExSpreadsheetEditor> Render(Workbook wb)
    {
        var context = NewContext();
        _popovers = context.Render<MudPopoverProvider>();
        _dialogs = context.Render<MudDialogProvider>();
        return context.Render<MudExSpreadsheetEditor>(p => p.Add(x => x.Workbook, wb));
    }

    private IRenderedComponent<MudExSpreadsheetGrid> RenderGrid(Workbook wb) =>
        NewContext().Render<MudExSpreadsheetGrid>(p => p.Add(x => x.Workbook, wb));

    private static Workbook Book(params (string Cell, object Value)[] cells)
    {
        var wb = Workbook.Create();
        foreach (var (cell, value) in cells)
            wb.Execute(new SetValueCommand(wb.Sheets[0], CellRef.Parse(cell), value is double d ? CellValue.Number(d) : CellValue.Text((string)value)));
        wb.Undo.Clear();
        return wb;
    }

    private static Workbook Fruits()
    {
        var wb = Book(("A1", "Fruit"), ("A2", "apple"), ("A3", "pear"), ("A4", "apple"), ("A5", "plum"), ("B2", 10d), ("B3", 5d));
        wb.Execute(new SetAutoFilterCommand(wb.Sheets[0], CellRange.Parse("A1:B5")));
        wb.Execute(new ApplyFilterCommand(wb.Sheets[0], 0, FilterCriteria.Values(new HashSet<string> { "apple" })));
        wb.Undo.Clear();
        return wb;
    }

    private static Task Run(IRenderedComponent<MudExSpreadsheetEditor> cut, string id)
        => cut.InvokeAsync(() => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).ExecuteAsync());

    private static IRenderedComponent<MudExSpreadsheetGrid> Select(IRenderedComponent<MudExSpreadsheetEditor> cut, string range)
    {
        cut.Render(p => p.Add(x => x.Selection, SpreadsheetSelection.Of(CellRange.Parse(range))));
        var grid = cut.FindComponent<MudExSpreadsheetGrid>();
        cut.WaitForAssertion(() => Assert.Equal(CellRange.Parse(range), grid.Instance.Selection.Range));
        return grid;
    }

    private static void Select(IRenderedComponent<MudExSpreadsheetGrid> grid, string range)
    {
        grid.Render(p => p.Add(x => x.Selection, SpreadsheetSelection.Of(CellRange.Parse(range))));
        Assert.Equal(CellRange.Parse(range), grid.Instance.Selection.Range);
    }

    private static void Key(IRenderedComponent<MudExSpreadsheetGrid> grid, string key, bool ctrl = false, bool shift = false) =>
        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = key, CtrlKey = ctrl, ShiftKey = shift });

    private static string Text(Workbook wb, string a1) => wb.Sheets[0][CellRef.Parse(a1)]?.Value.ToString();

    [Fact]
    public async Task Copy_WritesTsvAndHtml()
    {
        var wb = Fruits();
        wb.Execute(new SetStyleCommand(wb.Sheets[0], CellRange.Parse("A1"), s => s with { Font = s.Font with { Bold = true } }));
        var cut = RenderGrid(wb);
        Select(cut, "A1:B5");

        await cut.InvokeAsync(cut.Instance.CopyAsync);

        var call = _contexts[^1].JSInterop.Invocations.Last(i => i.Identifier == "writeClipboard");
        Assert.Equal("Fruit\t\r\napple\t10\r\napple\t\r\n", (string)call.Arguments[0]);
        var html = (string)call.Arguments[1];
        Assert.Contains("<table", html);
        Assert.Contains("font-weight:700", html);
        Assert.DoesNotContain("pear", html);
        Assert.Equal(["Fruit", "apple", "apple"], HtmlTableFormat.Read(html, CultureInfo.InvariantCulture).Cells.Where(c => c.Column == 0).Select(c => c.Value.TextValue));
    }

    [Fact]
    public async Task Paste_ExcelHtml_ValuesBoldFillAndMerge()
    {
        var wb = Book();
        var cut = RenderGrid(wb);
        Select(cut, "B2");
        const string html = "<html><head><style>td{font-weight:400}.xl65{font-weight:700;background:yellow}</style></head><body><table>"
                            + "<tr><td colspan=2 class=xl65>Title</td></tr><tr><td x:num=\"1.5\">1.5</td><td onclick=\"alert(1)\">x<script>alert(2)</script></td></tr></table></body></html>";

        await cut.InvokeAsync(() => cut.Instance.PasteStreamAsync(new TestStreamReference("Title\r\n1.5\tx\r\n"), new TestStreamReference(html)));

        var ws = wb.Sheets[0];
        Assert.Equal("Title", Text(wb, "B2"));
        Assert.Contains(CellRange.Parse("B2:C2"), ws.Merges);
        var style = wb.Styles[ws.EffectiveStyleId(CellRef.Parse("B2"))];
        Assert.True(style.Font.Bold);
        Assert.Equal("#FFFF00", style.Fill.Color);
        Assert.Equal(1.5, ws[CellRef.Parse("B3")].Value.NumberValue);
        Assert.Equal("x", Text(wb, "C3"));
        Assert.Equal(CellRange.Parse("B2:C3"), cut.Instance.Selection.Range);
        wb.Undo.Undo();
        Assert.Null(ws[CellRef.Parse("B2")]?.Value.TextValue);
        Assert.Empty(ws.Merges);
    }

    [Fact]
    public async Task Fill_InFilteredRange_SkipsHiddenRows()
    {
        var wb = Fruits();
        var cut = RenderGrid(wb);
        var ws = wb.Sheets[0];
        Select(cut, "B2:B5");

        Key(cut, "d", ctrl: true);

        Assert.Equal(10, ws[CellRef.Parse("B4")].Value.NumberValue);
        Assert.Equal(5, ws[CellRef.Parse("B3")].Value.NumberValue);
        Assert.Null(ws[CellRef.Parse("B5")]);
        wb.Undo.Undo();
        Assert.Null(ws[CellRef.Parse("B4")]);

        Select(cut, "B2");
        await cut.InvokeAsync(() => cut.Instance.FillToAsync(CellRange.Parse("B2:B5")));
        Assert.Equal(10, ws[CellRef.Parse("B4")].Value.NumberValue);
        Assert.Equal(5, ws[CellRef.Parse("B3")].Value.NumberValue);
        Assert.Null(ws[CellRef.Parse("B5")]);
    }

    [Fact]
    public void Delete_InFilteredRange_ClearsOnlyVisibleRows()
    {
        var wb = Fruits();
        var cut = RenderGrid(wb);
        Select(cut, "B2:B5");

        Key(cut, "Delete");

        Assert.False(wb.Sheets[0][CellRef.Parse("B2")]?.HasContent == true);
        Assert.Equal(5, wb.Sheets[0][CellRef.Parse("B3")].Value.NumberValue);
        Assert.Equal("pear", Text(wb, "A3"));
    }

    [Fact]
    public async Task FindReplace_ReplaceAll_OneUndoStep()
    {
        var wb = Book(("A1", "apple pie"), ("A2", "Apple"), ("B1", "pear"), ("C3", "apples"));
        var cut = Render(wb);
        var grid = Select(cut, "A1");

        Key(grid, "h", ctrl: true);
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-ssfind-with input")));
        _dialogs.Find(".mud-ex-ssfind-what input").Input("apple");
        _dialogs.Find(".mud-ex-ssfind-with input").Input("cherry");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssfind-replace-all").Click());

        Assert.Equal(["cherry pie", "cherry", "pear", "cherrys"], new[] { "A1", "A2", "B1", "C3" }.Select(a => Text(wb, a)));
        cut.WaitForAssertion(() => Assert.Contains("3", _dialogs.Find(".mud-ex-ssfind-status").TextContent));
        Assert.Equal("Replace All", wb.Undo.UndoDescription);

        await cut.InvokeAsync(wb.Undo.Undo);
        Assert.Equal(["apple pie", "Apple", "pear", "apples"], new[] { "A1", "A2", "B1", "C3" }.Select(a => Text(wb, a)));
        Assert.False(wb.Undo.CanUndo);
    }

    [Fact]
    public async Task Find_Next_SelectsMatchesInOrderAndOtherSheets()
    {
        var wb = Book(("B1", "x"), ("A3", "x"));
        wb.Execute(new AddSheetCommand("Two"));
        wb.Execute(new SetValueCommand(wb.Sheets[1], CellRef.Parse("C2"), CellValue.Text("box")));
        var cut = Render(wb);
        var grid = Select(cut, "A1");

        Key(grid, "f", ctrl: true);
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-ssfind-what input")));
        Assert.Empty(_dialogs.FindAll(".mud-ex-ssfind-with input"));
        _dialogs.Find(".mud-ex-ssfind-what input").Input("x");
        await cut.InvokeAsync(() => _dialogs.FindComponents<MudSelect<bool>>().First().Instance.SelectOption(true));

        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssfind-next").Click());
        cut.WaitForAssertion(() => Assert.Equal(CellRef.Parse("B1"), cut.FindComponent<MudExSpreadsheetGrid>().Instance.Selection.Active));
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssfind-next").Click());
        cut.WaitForAssertion(() => Assert.Equal(CellRef.Parse("A3"), cut.FindComponent<MudExSpreadsheetGrid>().Instance.Selection.Active));
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssfind-next").Click());
        cut.WaitForAssertion(() => Assert.Equal("Two", cut.Instance.ActiveSheet.Name));
        cut.WaitForAssertion(() => Assert.Equal(CellRef.Parse("C2"), cut.FindComponent<MudExSpreadsheetGrid>().Instance.Selection.Active));
    }

    [Fact]
    public async Task NameManager_AddEditDelete()
    {
        var wb = Book(("A1", 1d), ("B2", 2d));
        var cut = Render(wb);
        Select(cut, "A1");

        var open = Run(cut, "xl-name-manager");
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-ssnames-name input")));
        Assert.Equal("=Sheet1!$A$1", _dialogs.Find(".mud-ex-ssnames-refers input").GetAttribute("value"));
        _dialogs.Find(".mud-ex-ssnames-name input").Input("Rate");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssnames-save").Click());
        Assert.Equal("Sheet1!$A$1", wb.DefinedNames["Rate"]);
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-sslist-item[data-name='Rate']")));

        _dialogs.Find(".mud-ex-ssnames-name input").Input("Tax");
        _dialogs.Find(".mud-ex-ssnames-refers input").Input("=Sheet1!$B$2");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssnames-save").Click());
        Assert.Equal(["Tax"], wb.DefinedNames.Keys);
        Assert.Equal("Sheet1!$B$2", wb.DefinedNames["Tax"]);
        await cut.InvokeAsync(wb.Undo.Undo);
        Assert.Equal(["Rate"], wb.DefinedNames.Keys);
        await cut.InvokeAsync(wb.Undo.Redo);

        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-sslist-item[data-name='Tax']").Click());
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssnames-delete").Click());
        Assert.Empty(wb.DefinedNames);

        _dialogs.Find(".mud-ex-ssnames-name input").Input("Broken");
        _dialogs.Find(".mud-ex-ssnames-refers input").Input("=Sheet1!$B$2+");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssnames-save").Click());
        Assert.NotEmpty(_dialogs.FindAll(".mud-ex-ssnames-error"));
        Assert.Empty(wb.DefinedNames);

        _dialogs.Find(".mud-ex-ssnames-name input").Input("A1");
        Assert.True(_dialogs.Find(".mud-ex-ssnames-save").HasAttribute("disabled"));
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssnames-close").Click());
        await open;
    }

    [Fact]
    public async Task NameManager_Rename_RewritesFormulas()
    {
        var wb = Book(("A1", 2d));
        wb.Execute(new DefineNameCommand("Rate", "Sheet1!$A$1"));
        wb.Execute(new SetFormulaCommand(wb.Sheets[0], CellRef.Parse("B1"), "=Rate*10"));
        wb.Undo.Clear();
        var cut = Render(wb);

        var open = Run(cut, "xl-name-manager");
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-sslist-item[data-name='Rate']")));
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-sslist-item[data-name='Rate']").Click());
        _dialogs.Find(".mud-ex-ssnames-name input").Input("Tax");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssnames-save").Click());

        Assert.Equal(["Tax"], wb.DefinedNames.Keys);
        Assert.Equal("Tax*10", wb.Sheets[0][CellRef.Parse("B1")].Formula);
        Assert.Equal(20, wb.Sheets[0][CellRef.Parse("B1")].Value.NumberValue);
        await cut.InvokeAsync(wb.Undo.Undo);
        Assert.Equal("Rate*10", wb.Sheets[0][CellRef.Parse("B1")].Formula);
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssnames-close").Click());
        await open;
    }

    [Fact]
    public async Task GoTo_UnknownName_IsNotDefined()
    {
        var wb = Book();
        var cut = Render(wb);
        var grid = Select(cut, "A1");

        Key(grid, "F5");
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-ssgoto-reference input")));
        _dialogs.Find(".mud-ex-ssgoto-reference input").Input("Missing");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssgoto-ok").Click());

        cut.WaitForAssertion(() => Assert.Contains("Reference is not valid.", _dialogs.Markup));
        Assert.Empty(wb.DefinedNames);
        Assert.Equal(CellRef.Parse("A1"), grid.Instance.Selection.Active);
    }

    [Fact]
    public async Task InsertFunction_InsertsSignature()
    {
        var wb = Book();
        var cut = Render(wb);
        var grid = Select(cut, "C3");
        var sum = FunctionRegistry.Default.Functions.Single(f => f.Name == "SUM").Info;

        Key(grid, "F3", shift: true);
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-ssfn-search input")));
        _dialogs.Find(".mud-ex-ssfn-search input").Input("sum");
        cut.WaitForAssertion(() => Assert.Equal("SUM", _dialogs.FindAll(".mud-ex-sslist-item").First().GetAttribute("data-name")));
        _dialogs.Find(".mud-ex-sslist-item[data-name='SUM']").Click();
        cut.WaitForAssertion(() => Assert.Equal(sum.Signature, _dialogs.Find(".mud-ex-ssfn-signature").TextContent.Trim()));
        Assert.Contains(sum.Description, _dialogs.Find(".mud-ex-ssfn-description").TextContent);
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssfn-ok").Click());

        cut.WaitForAssertion(() => Assert.True(grid.Instance.IsEditing));
        Assert.Equal("=SUM(", grid.Instance.EditText);
    }

    [Fact]
    public async Task GoTo_F5_SelectsReference()
    {
        var wb = Book();
        var cut = Render(wb);
        var grid = Select(cut, "A1");

        Key(grid, "F5");
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-ssgoto-reference input")));
        _dialogs.Find(".mud-ex-ssgoto-reference input").Input("D7:E8");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssgoto-ok").Click());

        cut.WaitForAssertion(() => Assert.Equal(CellRange.Parse("D7:E8"), grid.Instance.Selection.Range));
    }

    [Fact]
    public async Task ContextMenu_InsertRowAbove()
    {
        var wb = Book(("A1", 1d), ("A2", 2d));
        var cut = Render(wb);
        var grid = cut.FindComponent<MudExSpreadsheetGrid>();

        await cut.InvokeAsync(() => grid.Instance.OnContextMenu("cell", 1, 0, 40, 60));
        Assert.Equal(CellRef.Parse("A2"), grid.Instance.Selection.Active);
        cut.WaitForAssertion(() => Assert.NotEmpty(_popovers.FindAll(".mud-ex-ss-cm-insert-rows")));
        Assert.Empty(_popovers.FindAll(".mud-ex-ss-cm-row-height"));
        await cut.InvokeAsync(() => _popovers.Find(".mud-ex-ss-cm-insert-rows").Click());

        var ws = wb.Sheets[0];
        Assert.Equal(1, ws[CellRef.Parse("A1")].Value.NumberValue);
        Assert.Null(ws[CellRef.Parse("A2")]);
        Assert.Equal(2, ws[CellRef.Parse("A3")].Value.NumberValue);
    }

    [Fact]
    public async Task ContextMenu_RowHeader_SelectsRowAndHides()
    {
        var wb = Book(("A1", 1d));
        var cut = Render(wb);
        var grid = cut.FindComponent<MudExSpreadsheetGrid>();

        await cut.InvokeAsync(() => grid.Instance.OnContextMenu("row", 2, -1, 10, 60));
        Assert.True(grid.Instance.Selection.Range.IsWholeRows);
        cut.WaitForAssertion(() => Assert.NotEmpty(_popovers.FindAll(".mud-ex-ss-cm-hide")));
        Assert.Empty(_popovers.FindAll(".mud-ex-ss-cm-insert-columns"));
        await cut.InvokeAsync(() => _popovers.Find(".mud-ex-ss-cm-hide").Click());

        Assert.True(wb.Sheets[0].IsRowHidden(2));
    }

    [Fact]
    public async Task ContextMenu_FilterBySelectedValue()
    {
        var wb = Book(("A1", "Fruit"), ("A2", "apple"), ("A3", "pear"), ("A4", "apple"));
        var cut = Render(wb);
        var grid = cut.FindComponent<MudExSpreadsheetGrid>();

        await cut.InvokeAsync(() => grid.Instance.OnContextMenu("cell", 2, 0, 40, 60));
        cut.WaitForAssertion(() => Assert.NotEmpty(_popovers.FindAll(".mud-ex-ss-cm-filter-value")));
        await cut.InvokeAsync(() => _popovers.Find(".mud-ex-ss-cm-filter-value").Click());

        var ws = wb.Sheets[0];
        Assert.Equal(CellRange.Parse("A1:A4"), ws.AutoFilter.Range);
        Assert.Equal([1, 3], ws.FilteredRows.Order());
        Assert.Equal("Filter", wb.Undo.UndoDescription);
    }

    [Fact]
    public async Task RowHeightDialog_SetsHeightInPoints()
    {
        var wb = Book();
        var cut = Render(wb);
        Select(cut, "A2:A3");

        var open = Run(cut, "xl-row-height");
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-sssize-value input")));
        _dialogs.Find(".mud-ex-sssize-value input").Input("30");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-sssize-ok").Click());
        await open;

        Assert.Equal(40, wb.Sheets[0].RowHeight(1));
        Assert.Equal(40, wb.Sheets[0].RowHeight(2));
    }
}
