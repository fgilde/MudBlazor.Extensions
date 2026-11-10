using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class SpreadsheetDataFeaturesTests : IAsyncLifetime
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

    private IRenderedComponent<MudExSpreadsheetEditor> Render(Workbook wb)
    {
        var context = new BunitContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _popovers = context.Render<MudPopoverProvider>();
        _dialogs = context.Render<MudDialogProvider>();
        return context.Render<MudExSpreadsheetEditor>(p => p.Add(x => x.Workbook, wb));
    }

    private static Workbook Book(params (string Cell, object Value)[] cells)
    {
        var wb = Workbook.Create();
        foreach (var (cell, value) in cells)
            wb.Execute(new SetValueCommand(wb.Sheets[0], CellRef.Parse(cell), value is double d ? CellValue.Number(d) : CellValue.Text((string)value)));
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

    private static string CellStyle(IRenderedComponent<MudExSpreadsheetEditor> cut, string a1)
    {
        var at = CellRef.Parse(a1);
        return cut.Find($".mud-ex-ssg-cell[data-r='{at.Row}'][data-c='{at.Column}']").GetAttribute("style");
    }

    private static Workbook Fruits()
    {
        var wb = Book(("A1", "Fruit"), ("A2", "apple"), ("A3", "pear"), ("A4", "apple"));
        wb.Execute(new SetAutoFilterCommand(wb.Sheets[0], CellRange.Parse("A1:A4")));
        wb.Undo.Clear();
        return wb;
    }

    [Fact]
    public async Task DataTab_SortAscending_SortsSelectionRegion()
    {
        var wb = Book(("A1", "Name"), ("B1", "Qty"), ("A2", "c"), ("B2", 3d), ("A3", "a"), ("B3", 1d), ("A4", "b"), ("B4", 2d));
        var cut = Render(wb);
        Select(cut, "B3");

        await Run(cut, "xl-sort-asc");

        var sheet = wb.Sheets[0];
        Assert.Equal(["Name", "a", "b", "c"], Enumerable.Range(0, 4).Select(r => sheet[new CellRef(r, 0)].Value.ToString()));
        Assert.Equal([1d, 2d, 3d], Enumerable.Range(1, 3).Select(r => sheet[new CellRef(r, 1)].Value.NumberValue));

        await Run(cut, "xl-sort-desc");
        Assert.Equal(["Name", "c", "b", "a"], Enumerable.Range(0, 4).Select(r => sheet[new CellRef(r, 0)].Value.ToString()));
    }

    [Fact]
    public void FilterMenu_UncheckValue_HidesRows()
    {
        var wb = Fruits();
        var cut = Render(wb);

        cut.Find(".mud-ex-ssg-filter-btn[data-c='0']").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(_popovers.FindAll(".mud-ex-ssfm-value")));
        Assert.Equal(["apple", "pear"], _popovers.FindAll(".mud-ex-ssfm-value").Select(v => v.TextContent.Trim()));
        _popovers.Find(".mud-ex-ssfm-value[data-value='pear'] input").Change(false);
        _popovers.Find(".mud-ex-ssfm-ok").Click();

        var sheet = wb.Sheets[0];
        cut.WaitForAssertion(() => Assert.True(sheet.IsRowFiltered(2)));
        Assert.False(sheet.IsRowFiltered(1));
        Assert.False(sheet.IsRowFiltered(3));
        Assert.Equal(["apple"], sheet.AutoFilter.Columns[0].Shown);
        cut.WaitForAssertion(() => Assert.Contains("mud-ex-ssg-filtered", cut.Find(".mud-ex-ssg-filter-btn[data-c='0']").ClassName));
    }

    [Fact]
    public void TableHeaderFilter_HidesRows()
    {
        var wb = Book(("A1", "Fruit"), ("B1", "Qty"), ("A2", "apple"), ("B2", 1d), ("A3", "pear"), ("B3", 2d), ("A4", "apple"), ("B4", 3d));
        wb.Execute(new CreateTableCommand(wb.Sheets[0], CellRange.Parse("A1:B4"), true));
        wb.Undo.Clear();
        var cut = Render(wb);

        cut.Find(".mud-ex-ssg-filter-btn[data-c='0']").Click();
        cut.WaitForAssertion(() => Assert.Equal(["apple", "pear"], _popovers.FindAll(".mud-ex-ssfm-value").Select(v => v.TextContent.Trim())));
        _popovers.Find(".mud-ex-ssfm-value[data-value='apple'] input").Change(false);
        _popovers.Find(".mud-ex-ssfm-ok").Click();

        var sheet = wb.Sheets[0];
        cut.WaitForAssertion(() => Assert.True(sheet.IsRowFiltered(1)));
        Assert.False(sheet.IsRowFiltered(2));
        Assert.Equal(["pear"], sheet.Tables[0].Filter.Columns[0].Shown);
        cut.WaitForAssertion(() => Assert.Contains("mud-ex-ssg-filtered", cut.Find(".mud-ex-ssg-filter-btn[data-c='0']").ClassName));
    }

    [Fact]
    public void FilterValues_OnlyFromRowsShownByOtherColumns()
    {
        var wb = Book(("A1", "Fruit"), ("B1", "Color"), ("A2", "apple"), ("B2", "red"), ("A3", "pear"), ("B3", "green"), ("A4", "apple"), ("B4", "yellow"));
        var sheet = wb.Sheets[0];
        wb.Execute(new SetAutoFilterCommand(sheet, CellRange.Parse("A1:B4")));
        wb.Execute(new ApplyFilterCommand(sheet, 0, FilterCriteria.Values(new HashSet<string> { "apple" })));
        wb.Execute(new ApplyFilterCommand(sheet, 1, FilterCriteria.Values(new HashSet<string> { "red" })));
        var cut = Render(wb);

        cut.Find(".mud-ex-ssg-filter-btn[data-c='1']").Click();

        cut.WaitForAssertion(() => Assert.Equal(["red", "yellow"], _popovers.FindAll(".mud-ex-ssfm-value").Select(v => v.TextContent.Trim())));
        Assert.Equal([true, false], _popovers.FindAll(".mud-ex-ssfm-value input").Select(i => i.HasAttribute("checked")));
    }

    [Fact]
    public async Task Validation_StopError_KeepsEditOpen()
    {
        var wb = Book(("A2", 5d));
        wb.Execute(new AddDataValidationCommand(wb.Sheets[0], new DataValidation([CellRange.Parse("A1:A3")], ValidationType.Whole, "between", "1", "10",
            ErrorTitle: "Out of range", Error: "Enter 1 to 10.")));
        var cut = Render(wb);
        var grid = Select(cut, "A1");
        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "2" });
        grid.Find("textarea.mud-ex-ssg-editor").Input("20");

        var commit = cut.InvokeAsync(grid.Instance.CommitEditAsync);
        cut.WaitForAssertion(() => Assert.Contains("Enter 1 to 10.", _dialogs.Markup));
        Assert.Contains("Out of range", _dialogs.Markup);
        await cut.InvokeAsync(() => _dialogs.FindAll("button").Single(b => b.TextContent.Trim() == "Retry").Click());
        await commit;

        Assert.True(grid.Instance.IsEditing);
        Assert.Null(wb.Sheets[0][CellRef.Parse("A1")]);

        grid.Find("textarea.mud-ex-ssg-editor").Input("7");
        await cut.InvokeAsync(grid.Instance.CommitEditAsync);
        Assert.False(grid.Instance.IsEditing);
        Assert.Equal(7, wb.Sheets[0][CellRef.Parse("A1")].Value.NumberValue);
    }

    [Fact]
    public void Validation_InputMessage_ShownForSelectedCell()
    {
        var wb = Book();
        wb.Execute(new AddDataValidationCommand(wb.Sheets[0], new DataValidation([CellRange.Parse("B2")], ValidationType.Whole, "between", "1", "10",
            PromptTitle: "Quantity", Prompt: "Enter 1 to 10.")));
        var cut = Render(wb);
        Assert.Empty(cut.FindAll(".mud-ex-ssg-dv-prompt"));

        Select(cut, "B2");

        cut.WaitForAssertion(() => Assert.Equal("QuantityEnter 1 to 10.", cut.Find(".mud-ex-ssg-dv-prompt").TextContent.Trim()));
        Assert.Equal(cut.Find(".mud-ex-ssg-dv-prompt").Id, cut.Find(".mud-ex-ssg-ime").GetAttribute("aria-describedby"));
        Select(cut, "B3");
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ssg-dv-prompt")));
    }

    [Fact]
    public async Task Validation_TypedFormula_IsCheckedByItsResult()
    {
        var wb = Book(("B1", 50d));
        wb.Execute(new AddDataValidationCommand(wb.Sheets[0], new DataValidation([CellRange.Parse("A1")], ValidationType.Whole, "between", "1", "10",
            Error: "Enter 1 to 10.")));
        var cut = Render(wb);
        var grid = Select(cut, "A1");
        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "=" });
        grid.Find("textarea.mud-ex-ssg-editor").Input("=B1*2");

        var commit = cut.InvokeAsync(grid.Instance.CommitEditAsync);
        cut.WaitForAssertion(() => Assert.Contains("Enter 1 to 10.", _dialogs.Markup));
        await cut.InvokeAsync(() => _dialogs.FindAll("button").Single(b => b.TextContent.Trim() == "Cancel").Click());
        await commit;
        Assert.Null(wb.Sheets[0][CellRef.Parse("A1")]);

        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "=" });
        grid.Find("textarea.mud-ex-ssg-editor").Input("=B1/10");
        await cut.InvokeAsync(grid.Instance.CommitEditAsync);
        Assert.Equal("B1/10", wb.Sheets[0][CellRef.Parse("A1")].Formula);
    }

    [Fact]
    public void Validation_ListShowsDropdownItems()
    {
        var wb = Book();
        wb.Execute(new AddDataValidationCommand(wb.Sheets[0], new DataValidation([CellRange.Parse("A2:A3")], ValidationType.List, Formula1: "\"red,green,blue\"")));
        var cut = Render(wb);
        Assert.Empty(cut.FindAll(".mud-ex-ssg-dv-btn"));
        Select(cut, "A2");

        cut.WaitForAssertion(() => cut.Find(".mud-ex-ssg-dv-btn").Click());
        cut.WaitForAssertion(() => Assert.Equal(["red", "green", "blue"], _popovers.FindAll(".mud-ex-ssg-dv-item").Select(i => i.TextContent.Trim())));
        _popovers.FindAll(".mud-ex-ssg-dv-item").Single(i => i.TextContent.Trim() == "green").Click();

        cut.WaitForAssertion(() => Assert.Equal("green", wb.Sheets[0][CellRef.Parse("A2")]?.Value.ToString()));
    }

    [Fact]
    public void ConditionalFormat_RedText_RendersDxf()
    {
        var wb = Book(("A1", 3d), ("A2", 10d));
        var style = new DifferentialStyle(new DifferentialFont { Color = "#FF0000" }, new CellFill { Color = "#FFC7CE" });
        wb.Execute(new AddConditionalFormattingCommand(wb.Sheets[0], new ConditionalFormatting([CellRange.Parse("A1:A2")],
            [new ConditionalRule.CellIs("greaterThan", "5", null, style)])));
        var cut = Render(wb);

        cut.WaitForAssertion(() => Assert.Contains("color:#FF0000", CellStyle(cut, "A2")));
        Assert.Contains("background:#FFC7CE", CellStyle(cut, "A2"));
        Assert.DoesNotContain("#FF0000", CellStyle(cut, "A1"));

        wb.Execute(new SetValueCommand(wb.Sheets[0], CellRef.Parse("A1"), CellValue.Number(6)));
        cut.WaitForAssertion(() => Assert.Contains("color:#FF0000", CellStyle(cut, "A1")));
    }

    [Fact]
    public async Task DataBar_RendersGradientWidth()
    {
        var wb = Book(("A1", 0d), ("A2", 5d), ("A3", 10d));
        var cut = Render(wb);
        Select(cut, "A1:A3");

        await Run(cut, "xl-cf-bar-blue");

        Assert.IsType<ConditionalRule.DataBar>(Assert.Single(Assert.Single(wb.Sheets[0].ConditionalFormats).Rules));
        cut.WaitForAssertion(() => Assert.Contains("linear-gradient", CellStyle(cut, "A2")));
        Assert.Contains("50%", CellStyle(cut, "A2"));
        Assert.Contains("100%", CellStyle(cut, "A3"));
    }

    [Fact]
    public async Task FormatAsTable_CreatesTableWithBanding()
    {
        var wb = Book(("A1", "Name"), ("B1", "Qty"), ("A2", "a"), ("B2", 1d), ("A3", "b"), ("B3", 2d), ("A4", "c"), ("B4", 3d));
        var cut = Render(wb);
        Select(cut, "A2");
        var gallery = cut.FindComponents<MudExRibbonGallery<string>>().Single(g => g.Instance.Id == "xl-format-table");

        var pick = cut.InvokeAsync(() => gallery.Instance.SelectedItemChanged.InvokeAsync("TableStyleMedium9"));
        cut.WaitForAssertion(() => Assert.Contains("A1:B4", _dialogs.Markup));
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-sstable-ok").Click());
        await pick;

        var table = Assert.Single(wb.Sheets[0].Tables);
        Assert.Equal(CellRange.Parse("A1:B4"), table.Range);
        Assert.Equal("TableStyleMedium9", table.StyleName);
        Assert.True(table.HeaderRow);
        var palette = SpreadsheetTableStyles.Palette("TableStyleMedium9");
        cut.WaitForAssertion(() => Assert.Contains("background:" + palette.HeaderFill, CellStyle(cut, "A1")));
        Assert.Contains("background:" + palette.BandFill, CellStyle(cut, "B2"));
        Assert.DoesNotContain("background:", CellStyle(cut, "B3"));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".mud-ex-ssg-filter-btn").Count));
    }

    [Fact]
    public async Task CellStyles_Gallery_AppliesStyle()
    {
        var wb = Book(("A1", "ok"));
        var cut = Render(wb);
        Select(cut, "A1");
        var gallery = cut.FindComponents<MudExRibbonGallery<SpreadsheetCellStyle>>().Single(g => g.Instance.Id == "xl-cell-styles");
        var good = gallery.Instance.Items.Single(s => s.Name == "Good");

        await cut.InvokeAsync(() => gallery.Instance.SelectedItemChanged.InvokeAsync(good));

        var style = wb.Styles[wb.Sheets[0].EffectiveStyleId(CellRef.Parse("A1"))];
        Assert.Equal("#C6EFCE", style.Fill.Color);
        Assert.Equal("#006100", style.Font.Color);
        Assert.True(wb.Undo.CanUndo);
    }

    private static int RibbonRenders(IRenderedComponent<MudExSpreadsheetEditor> cut)
        => cut.FindComponents<MudExRibbonMenuItem>().Sum(c => c.RenderCount)
           + cut.FindComponents<MudExRibbonMenuButton>().Sum(c => c.RenderCount)
           + cut.FindComponents<MudExRibbonButton>().Sum(c => c.RenderCount)
           + cut.FindComponents<MudExRibbonGallery<string>>().Sum(c => c.RenderCount)
           + cut.FindComponents<MudExRibbonGallery<SpreadsheetCellStyle>>().Sum(c => c.RenderCount);

    [Fact]
    public void RibbonRenderCount_UnchangedOnSelection()
    {
        var wb = Book(("A1", 1d), ("A2", 2d), ("A3", 3d));
        wb.Execute(new SetStyleCommand(wb.Sheets[0], CellRange.Parse("A3"), s => s with { Font = s.Font with { Bold = true } }));
        var cut = Render(wb);
        var grid = cut.FindComponent<MudExSpreadsheetGrid>();
        cut.WaitForAssertion(() => Assert.NotNull(cut.FindComponent<MudExSpreadsheetFormulaBar>()));
        var renders = RibbonRenders(cut);

        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", ShiftKey = true });
        cut.WaitForAssertion(() => Assert.Equal("Sum: 3", cut.Find(".mud-ex-sssb-sum").TextContent.Trim()));
        Assert.Equal(renders, RibbonRenders(cut));

        var galleries = cut.FindComponents<MudExRibbonGallery<string>>().Sum(c => c.RenderCount)
                        + cut.FindComponents<MudExRibbonGallery<SpreadsheetCellStyle>>().Sum(c => c.RenderCount);
        var cfItems = cut.FindComponents<MudExRibbonMenuItem>().Where(c => c.Instance.Id?.StartsWith("xl-cf-") == true).ToList();
        Assert.NotEmpty(cfItems);
        var cfRenders = cfItems.Sum(c => c.RenderCount);
        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.WaitForAssertion(() => Assert.True(cut.FindComponents<MudExRibbonToggleButton>().Single(b => b.Instance.Id == "xl-bold").Instance.Checked));
        Assert.Equal(cfRenders, cfItems.Sum(c => c.RenderCount));
        Assert.Equal(galleries, cut.FindComponents<MudExRibbonGallery<string>>().Sum(c => c.RenderCount)
                                + cut.FindComponents<MudExRibbonGallery<SpreadsheetCellStyle>>().Sum(c => c.RenderCount));
    }

    [Fact]
    public async Task SortErrorOnMergedRange_IsShownNotThrown()
    {
        var wb = Book(("A1", 2d), ("A2", 1d));
        wb.Execute(new MergeCommand(wb.Sheets[0], CellRange.Parse("B1:B2")));
        var cut = Render(wb);
        Select(cut, "A1:B2");

        await Run(cut, "xl-sort-asc");

        cut.WaitForAssertion(() => Assert.Contains("merged", _dialogs.Markup));
        Assert.Equal(2, wb.Sheets[0][CellRef.Parse("A1")].Value.NumberValue);
    }

    [Fact]
    public async Task FilterToggle_OnRegion_ThenClearAndRemove()
    {
        var wb = Book(("A1", "Fruit"), ("A2", "apple"), ("A3", "pear"));
        var cut = Render(wb);
        Select(cut, "A2");

        await Run(cut, "xl-filter");
        var sheet = wb.Sheets[0];
        Assert.Equal(CellRange.Parse("A1:A3"), sheet.AutoFilter.Range);
        await cut.InvokeAsync(() => wb.Execute(new ApplyFilterCommand(sheet, 0, FilterCriteria.Values(new HashSet<string> { "pear" }))));
        Assert.True(sheet.IsRowFiltered(1));

        await Run(cut, "xl-filter-clear");
        Assert.False(sheet.IsRowFiltered(1));
        await Run(cut, "xl-filter");
        Assert.Null(sheet.AutoFilter);
    }

    [Fact]
    public async Task HighlightGreaterThan_DialogAddsRule()
    {
        var wb = Book(("A1", 3d), ("A2", 10d));
        var cut = Render(wb);
        Select(cut, "A1:A2");

        var open = Run(cut, "xl-cf-greater");
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-sscf-value1 input")));
        _dialogs.Find(".mud-ex-sscf-value1 input").Input("5");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-sscf-ok").Click());
        await open;

        var rule = Assert.IsType<ConditionalRule.CellIs>(Assert.Single(Assert.Single(wb.Sheets[0].ConditionalFormats).Rules));
        Assert.Equal(("greaterThan", "5"), (rule.Operator, rule.Formula1));
        cut.WaitForAssertion(() => Assert.Contains("color:#9C0006", CellStyle(cut, "A2")));
    }

    [Fact]
    public async Task ValidationDialog_AddsListRule()
    {
        var wb = Book();
        var cut = Render(wb);
        Select(cut, "B2:B4");

        var open = Run(cut, "xl-data-validation");
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-ssdv-ok")));
        await cut.InvokeAsync(() => _dialogs.FindComponents<MudSelect<ValidationType>>().Single().Instance.SelectOption(ValidationType.List));
        cut.WaitForAssertion(() => _dialogs.Find(".mud-ex-ssdv-formula1 input").Input("yes,no"));
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-ssdv-ok").Click());
        await open;

        var dv = Assert.Single(wb.Sheets[0].DataValidations);
        Assert.Equal((ValidationType.List, "\"yes,no\"", CellRange.Parse("B2:B4")), (dv.Type, dv.Formula1, dv.Ranges[0]));
        Assert.Equal(["yes", "no"], DataValidator.ListItems(wb, wb.Sheets[0], dv));
    }
}
