using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class SpreadsheetChartsNotesTests : IAsyncLifetime
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
        return context.Render<MudExSpreadsheetEditor>(p => p.Add(x => x.Workbook, wb).Add(x => x.Author, "Tester"));
    }

    private static Workbook Book(params (string Cell, object Value)[] cells)
    {
        var wb = Workbook.Create();
        foreach (var (cell, value) in cells)
            wb.Execute(new SetValueCommand(wb.Sheets[0], CellRef.Parse(cell), value is double d ? CellValue.Number(d) : CellValue.Text((string)value)));
        wb.Undo.Clear();
        return wb;
    }

    private static Workbook Sales() =>
        Book(("A1", "Region"), ("B1", "Q1"), ("C1", "Q2"), ("A2", "North"), ("B2", 10d), ("C2", 20d), ("A3", "South"), ("B3", 30d), ("C3", 40d));

    private static Task Run(IRenderedComponent<MudExSpreadsheetEditor> cut, string id)
        => cut.InvokeAsync(() => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).ExecuteAsync());

    private static IRenderedComponent<MudExSpreadsheetGrid> Select(IRenderedComponent<MudExSpreadsheetEditor> cut, string range)
    {
        cut.Render(p => p.Add(x => x.Selection, SpreadsheetSelection.Of(CellRange.Parse(range))));
        var grid = cut.FindComponent<MudExSpreadsheetGrid>();
        cut.WaitForAssertion(() => Assert.Equal(CellRange.Parse(range), grid.Instance.Selection.Range));
        return grid;
    }

    private static double[] Values(ChartSeries<double> series) => series.Data.Values.Select(v => (double)v).ToArray();

    [Fact]
    public async Task InsertChart_RendersMudChartWithSeries()
    {
        var wb = Sales();
        var cut = Render(wb);
        var grid = Select(cut, "A1:C3");

        await Run(cut, "xl-chart-column");

        var chart = Assert.Single(wb.Sheets[0].Charts);
        Assert.Equal(ChartKind.Column, chart.Kind);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ssg-chart.mud-ex-ssg-chart-selected")));
        Assert.Equal(8, cut.FindAll(".mud-ex-ssg-chart-handle").Count);
        var mud = cut.FindComponent<MudChart<double>>();
        Assert.Equal(ChartType.Bar, mud.Instance.ChartType);
        Assert.Equal(["Q1", "Q2"], mud.Instance.ChartSeries.Select(s => s.Name));
        Assert.Equal([10d, 30d], Values(mud.Instance.ChartSeries[0]));
        Assert.Equal(["North", "South"], mud.Instance.ChartLabels);

        await cut.InvokeAsync(() => grid.Instance.OnChartMoved(chart.Id, grid.Instance.X(5) + 4, grid.Instance.Y(6), 300, 200));
        var moved = Assert.Single(wb.Sheets[0].Charts);
        Assert.Equal(new CellRef(6, 5), moved.From.Relative);
        Assert.Equal(4, moved.FromOffset.X, 3);

        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "Delete" });
        Assert.Empty(wb.Sheets[0].Charts);
        Assert.Equal("North", wb.Sheets[0][CellRef.Parse("A2")].Value.TextValue);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ssg-chart")));

        await cut.InvokeAsync(wb.Undo.Undo);
        await cut.InvokeAsync(wb.Undo.Undo);
        await cut.InvokeAsync(wb.Undo.Undo);
        Assert.Empty(wb.Sheets[0].Charts);
        Assert.False(wb.Undo.CanUndo);
    }

    [Fact]
    public async Task AreaChart_RendersSvgPath()
    {
        var wb = Sales();
        var cut = Render(wb);
        Select(cut, "A1:C3");

        await Run(cut, "xl-chart-area");

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".mud-ex-ssg-chart svg path.mud-ex-sschart-area").Count));
        Assert.All(cut.FindAll("path.mud-ex-sschart-area"), p => Assert.StartsWith("M", p.GetAttribute("d")));
        Assert.Empty(cut.FindComponents<MudChart<double>>());

        Select(cut, "A1:C3");
        await Run(cut, "xl-chart-bar");
        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("rect.mud-ex-sschart-bar").Count));
        Assert.Empty(cut.FindComponents<MudChart<double>>());
    }

    [Fact]
    public async Task ChartUpdatesWhenSourceCellChanges()
    {
        var wb = Sales();
        var ws = wb.Sheets[0];
        wb.Execute(new InsertChartCommand(ws, CellRange.Parse("A1:C3"), ChartKind.Line));
        var cut = NewContext().Render<MudExSpreadsheetGrid>(p => p.Add(x => x.Workbook, wb));
        var view = cut.FindComponent<MudExSpreadsheetChart>();
        var data = view.Instance.Data;

        await cut.InvokeAsync(() => wb.Execute(new SetValueCommand(ws, CellRef.Parse("H40"), CellValue.Number(5))));
        Assert.Same(data, view.Instance.Data);

        await cut.InvokeAsync(() => wb.Execute(new SetValueCommand(ws, CellRef.Parse("B2"), CellValue.Number(99))));
        cut.WaitForAssertion(() => Assert.NotSame(data, view.Instance.Data));
        Assert.Equal([99d, 30d], Values(cut.FindComponent<MudChart<double>>().Instance.ChartSeries[0]));

        var id = ws.Charts[0].Id;
        await cut.InvokeAsync(() => wb.Execute(new MoveChartCommand(ws, id, new CellRef(900, 0), new CellRef(915, 6))));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ssg-chart")));
    }

    [Fact]
    public async Task PieChart_InReadOnlyGrid_IsLocked()
    {
        var wb = Sales();
        wb.Execute(new InsertChartCommand(wb.Sheets[0], CellRange.Parse("A1:C3"), ChartKind.Pie));
        var cut = NewContext().Render<MudExSpreadsheetGrid>(p => p.Add(x => x.Workbook, wb).Add(x => x.ReadOnly, true));

        var pie = cut.FindComponent<MudChart<double>>();
        Assert.Equal(ChartType.Pie, pie.Instance.ChartType);
        Assert.Equal("true", cut.Find(".mud-ex-ssg-chart").GetAttribute("data-locked"));
        await cut.InvokeAsync(() => cut.Instance.OnChartSelect(wb.Sheets[0].Charts[0].Id));
        Assert.Empty(cut.FindAll(".mud-ex-ssg-chart-selected"));
    }

    [Fact]
    public async Task FrozenPanes_ChartsAndNotesRenderInTheirPane()
    {
        var wb = Sales();
        var ws = wb.Sheets[0];
        wb.Execute(new SetNoteCommand(ws, new CellNote(CellRef.Parse("A1"), "Ann", "Top")));
        var cut = Render(wb);
        var grid = Select(cut, "A1:C3");
        await Run(cut, "xl-chart-column");
        var id = Assert.Single(ws.Charts).Id;
        await cut.InvokeAsync(() => grid.Instance.OnChartMoved(id, grid.Instance.X(5), grid.Instance.Y(10), 200, 80));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ssg-pane-main .mud-ex-ssg-chart")));

        await cut.InvokeAsync(() => wb.Execute(new FreezeCommand(ws, CellRef.Parse("A20"))));
        await cut.InvokeAsync(() => grid.Instance.OnNoteHover(0, 0));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".mud-ex-ssg-pane-top .mud-ex-ssg-chart")));
        Assert.Empty(cut.FindAll(".mud-ex-ssg-pane-main .mud-ex-ssg-chart"));
        Assert.Contains("overflow:hidden", cut.Find(".mud-ex-ssg-pane-top .mud-ex-ssg-charts").GetAttribute("style"));
        Assert.Contains("Top", cut.Find(".mud-ex-ssg-pane-top .mud-ex-ssg-note-box").TextContent);
    }

    [Fact]
    public async Task Note_IndicatorAndEdit()
    {
        var wb = Book(("B2", "x"));
        var ws = wb.Sheets[0];
        wb.Execute(new SetNoteCommand(ws, new CellNote(CellRef.Parse("B2"), "Ann", "Check this")));
        wb.Undo.Clear();
        var cut = Render(wb);
        var grid = Select(cut, "B2");

        Assert.NotNull(cut.Find(".mud-ex-ssg-cell.mud-ex-ssg-noted[data-r='1'][data-c='1'] .mud-ex-ssg-note-mark"));
        await cut.InvokeAsync(() => grid.Instance.OnNoteHover(1, 1));
        cut.WaitForAssertion(() => Assert.Contains("Check this", cut.Find(".mud-ex-ssg-note-box").TextContent));
        Assert.Contains("Ann", cut.Find(".mud-ex-ssg-note-box").TextContent);
        await cut.InvokeAsync(() => grid.Instance.OnNoteHover(-1, -1));
        Assert.Empty(cut.FindAll(".mud-ex-ssg-note-box"));

        Select(cut, "C3");
        await Run(cut, "xl-note-new");
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".mud-ex-ssg-note-box textarea")));
        cut.Find(".mud-ex-ssg-note-box textarea").Input("Hello");
        await cut.InvokeAsync(() => cut.Find(".mud-ex-ssg-note-save").Click());

        var note = ws.Notes.Single(n => n.Cell.Relative == CellRef.Parse("C3"));
        Assert.Equal(("Tester", "Hello"), (note.Author, note.Text));
        Assert.Empty(cut.FindAll(".mud-ex-ssg-note-box"));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".mud-ex-ssg-noted[data-r='2'][data-c='2']")));

        Select(cut, "B2");
        await cut.InvokeAsync(() => grid.Instance.OnContextMenu("cell", 1, 1, 40, 60));
        cut.WaitForAssertion(() => Assert.NotEmpty(_popovers.FindAll(".mud-ex-ss-cm-note-delete")));
        await cut.InvokeAsync(() => _popovers.Find(".mud-ex-ss-cm-note-delete").Click());
        Assert.DoesNotContain(ws.Notes, n => n.Cell.Relative == CellRef.Parse("B2"));

        ws.NotesReadOnly = true;
        await Run(cut, "xl-note-new");
        Assert.Empty(cut.FindAll(".mud-ex-ssg-note-box textarea"));
    }

    [Fact]
    public async Task Note_Draft_KeptOnSaveAndSheetSwitch()
    {
        var wb = Book(("B2", "x"));
        wb.Execute(new AddSheetCommand("Two"));
        wb.Undo.Clear();
        var ws = wb.Sheets[0];
        var cut = Render(wb);

        Select(cut, "C3");
        await Run(cut, "xl-note-new");
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".mud-ex-ssg-note-box textarea")));
        cut.Find(".mud-ex-ssg-note-box textarea").Input("Draft");
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));
        await cut.InvokeAsync(async () => await (await cut.Instance.ExportAsync()).DisposeAsync());
        Assert.Equal("Draft", ws.Notes.Single().Text);

        Select(cut, "D4");
        await Run(cut, "xl-note-new");
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".mud-ex-ssg-note-box textarea")));
        cut.Find(".mud-ex-ssg-note-box textarea").Input("Second");
        cut.FindAll(".mud-ex-ssst-tab")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        cut.WaitForAssertion(() => Assert.Equal("Two", cut.Instance.ActiveSheet.Name));
        Assert.Equal("Second", ws.Notes.Single(n => n.Cell.Relative == CellRef.Parse("D4")).Text);
    }

    [Fact]
    public async Task Link_CtrlClick_OpensSafeTargetOnly()
    {
        var wb = Book(("A1", "site"), ("A2", "bad"), ("A3", "jump"));
        var ws = wb.Sheets[0];
        wb.Execute(new AddSheetCommand("Other"));
        wb.Execute(new SetLinkCommand(ws, new CellLink(CellRef.Parse("A1"), "https://example.com/a", null, null, "Example")));
        wb.Execute(new SetLinkCommand(ws, new CellLink(CellRef.Parse("A2"), "javascript:alert(1)", null, null, null)));
        wb.Execute(new SetLinkCommand(ws, new CellLink(CellRef.Parse("A3"), null, "Other!B5", null, null)));
        var sheetIndex = -1;
        var cut = NewContext().Render<MudExSpreadsheetGrid>(p => p.Add(x => x.Workbook, wb).Add(x => x.SheetIndexChanged, i => sheetIndex = i));
        var js = _contexts[^1].JSInterop;

        Assert.Contains("mud-ex-ssg-link", cut.Find(".mud-ex-ssg-cell[data-r='0'][data-c='0']").ClassName);
        Assert.DoesNotContain("mud-ex-ssg-link", cut.Find(".mud-ex-ssg-cell[data-r='1'][data-c='0']").ClassName);
        Assert.Contains("mud-ex-ssg-link", cut.Find(".mud-ex-ssg-cell[data-r='2'][data-c='0']").ClassName);
        Assert.Empty(cut.FindAll("a[href]"));
        Assert.DoesNotContain("Other!B5", cut.Markup);
        Assert.Equal("https://example.com/a", cut.Find(".mud-ex-ssg-cell[data-r='0'][data-c='0']").GetAttribute("data-href"));
        Assert.Single(cut.FindAll("[data-href]"));

        await cut.InvokeAsync(() => cut.Instance.OnLinkClick(0, 0));
        var open = Assert.Single(js.Invocations, i => i.Identifier == "openLink");
        Assert.Equal("https://example.com/a", open.Arguments[0]);

        await cut.InvokeAsync(() => cut.Instance.OnLinkClick(1, 0));
        Assert.Single(js.Invocations, i => i.Identifier == "openLink");

        await cut.InvokeAsync(() => cut.Instance.OnLinkClick(2, 0));
        Assert.Single(js.Invocations, i => i.Identifier == "openLink");
        Assert.Equal(1, sheetIndex);
    }

    [Fact]
    public async Task LinkDialog_WritesDisplayTextInOneUndoStep()
    {
        var wb = Book(("A1", "x"));
        var cut = Render(wb);
        Select(cut, "D4");

        var open = Run(cut, "xl-link");
        cut.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-sslink-address input")));
        _dialogs.Find(".mud-ex-sslink-address input").Input("https://mudex.org");
        _dialogs.Find(".mud-ex-sslink-display input").Input("MudEx");
        await cut.InvokeAsync(() => _dialogs.Find(".mud-ex-sslink-ok").Click());
        await open;

        var ws = wb.Sheets[0];
        Assert.Equal("MudEx", ws[CellRef.Parse("D4")].Value.TextValue);
        Assert.Equal("https://mudex.org", ws.LinkAt(CellRef.Parse("D4")).Target);
        await cut.InvokeAsync(wb.Undo.Undo);
        Assert.Null(ws.LinkAt(CellRef.Parse("D4")));
        Assert.False(ws[CellRef.Parse("D4")]?.HasContent == true);
        Assert.False(wb.Undo.CanUndo);
    }

    [Fact]
    public async Task Print_RendersStaticTableOfSelection()
    {
        var wb = Book(("A1", "Name"), ("B1", "Qty"), ("A2", "apple"), ("B2", 3d), ("C5", "far"));
        wb.Execute(new SetStyleCommand(wb.Sheets[0], CellRange.Parse("A1"), s => s with { Font = s.Font with { Bold = true } }));
        var cut = Render(wb);
        var print = _contexts[^1].JSInterop.SetupVoid("printNode", _ => true);
        Select(cut, "A1:B2");

        await cut.InvokeAsync(cut.Instance.PrintAsync);

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".mud-ex-ss-print tr").Count));
        Assert.Equal(["Name", "Qty", "apple", "3"], cut.FindAll(".mud-ex-ss-print td").Select(td => td.TextContent));
        Assert.Contains("font-weight:700", cut.Find(".mud-ex-ss-print td").GetAttribute("style"));
        Assert.DoesNotContain("far", cut.Find(".mud-ex-ss-print").TextContent);

        await cut.InvokeAsync(print.SetVoidResult);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-ss-print")));

        print = _contexts[^1].JSInterop.SetupVoid("printNode", _ => true);
        Select(cut, "B2");
        await cut.InvokeAsync(cut.Instance.PrintAsync);
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll(".mud-ex-ss-print tr").Count));
        Assert.Equal(3, cut.FindAll(".mud-ex-ss-print tr")[0].QuerySelectorAll("td").Length);
        Assert.Contains("far", cut.Find(".mud-ex-ss-print").TextContent);
    }
}
