using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class SpreadsheetGridTests : IAsyncLifetime
{
    private readonly List<BunitContext> _contexts = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();
    }

    private BunitContext NewContext()
    {
        var context = new BunitContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private IRenderedComponent<MudExSpreadsheetGrid> Render(Workbook wb, Action<ComponentParameterCollectionBuilder<MudExSpreadsheetGrid>> configure = null)
        => NewContext().Render<MudExSpreadsheetGrid>(p =>
        {
            p.Add(x => x.Workbook, wb);
            configure?.Invoke(p);
        });

    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static void Key(IRenderedComponent<MudExSpreadsheetGrid> cut, string key, bool ctrl = false, bool shift = false, bool alt = false) =>
        cut.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = key, CtrlKey = ctrl, ShiftKey = shift, AltKey = alt });

    private static void Type(IRenderedComponent<MudExSpreadsheetGrid> cut, string text)
    {
        Key(cut, text[..1]);
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsEditing));
        cut.Find("textarea.mud-ex-ssg-editor").Input(text);
    }

    private static Workbook Big(int rows, int columns)
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        ws.Reserve(rows * columns);
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < columns; c++)
                ws.Ensure(new CellRef(r, c)).Value = CellValue.Number(r * columns + c);
        return wb;
    }

    [Fact]
    public async Task RendersOnlyVisibleWindow()
    {
        var wb = Big(100_000, 10);
        var cut = Render(wb);

        const int limit = (40 + 20) * 10;
        var cells = cut.FindAll(".mud-ex-ssg-cell");
        Assert.InRange(cells.Count, 1, limit);
        Assert.DoesNotContain(cells, e => e.GetAttribute("data-r") == "1000");
        var canvas = cut.Find(".mud-ex-ssg-canvas").GetAttribute("style");
        Assert.Contains("height:", canvas);

        await cut.InvokeAsync(() => cut.Instance.OnViewportChanged(50_000 * Worksheet.StandardRowHeight, 0, 1200, 800));

        cut.WaitForAssertion(() =>
        {
            var moved = cut.FindAll(".mud-ex-ssg-cell");
            Assert.InRange(moved.Count, 1, limit);
            Assert.Contains(moved, e => e.GetAttribute("data-r") == "50005" && e.GetAttribute("data-c") == "0");
            Assert.DoesNotContain(moved, e => e.GetAttribute("data-r") == "0");
        });
        Assert.True(cut.FindAll(".mud-ex-ssg-rh").Count <= 40 + 20);
    }

    [Fact]
    public void Typing_ThenEnter_CommitsAndMovesDown()
    {
        var wb = Workbook.Create();
        var cut = Render(wb);

        Type(cut, "abc");
        Key(cut, "Enter");

        cut.WaitForAssertion(() => Assert.False(cut.Instance.IsEditing));
        Assert.Equal("abc", wb.Sheets[0][R("A1")].Value.TextValue);
        Assert.Equal(R("A2"), cut.Instance.Selection.Active);
        Assert.Contains(cut.FindAll(".mud-ex-ssg-cell"), e => e.TextContent == "abc");

        Type(cut, "=A1&\"!\"");
        Key(cut, "Tab");
        cut.WaitForAssertion(() => Assert.Equal("abc!", wb.Sheets[0][R("A2")].Value.TextValue));
        Assert.Equal(R("B2"), cut.Instance.Selection.Active);
    }

    [Fact]
    public void Escape_CancelsEdit()
    {
        var wb = Workbook.Create();
        wb.Execute(new SetValueCommand(wb.Sheets[0], R("A1"), CellValue.Number(7)));
        var cut = Render(wb);

        Type(cut, "xyz");
        Key(cut, "Escape");

        cut.WaitForAssertion(() => Assert.False(cut.Instance.IsEditing));
        Assert.Equal(7, wb.Sheets[0][R("A1")].Value.NumberValue);
        Assert.Equal(R("A1"), cut.Instance.Selection.Active);
    }

    [Fact]
    public void GermanInput_ParsesNumberAndDate()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var wb = Workbook.Create();
            var ws = wb.Sheets[0];
            var cut = Render(wb);

            foreach (var input in new[] { "1,5", "09.11.2026", "12%", "=SUM(A1;A3)", "=SUMME(A1:A3)" })
            {
                Type(cut, input);
                Key(cut, "Enter");
                cut.WaitForAssertion(() => Assert.False(cut.Instance.IsEditing));
            }

            Assert.Equal(1.5, ws[R("A1")].Value.NumberValue);
            Assert.Equal(ExcelDate.FromDate(2026, 11, 9), ws[R("A2")].Value.NumberValue);
            Assert.Contains("yyyy", wb.Styles[ws[R("A2")].StyleId].NumberFormat);
            Assert.Equal(0.12, ws[R("A3")].Value.NumberValue, 10);
            Assert.Equal("0%", wb.Styles[ws[R("A3")].StyleId].NumberFormat);
            Assert.Equal(1.62, ws[R("A4")].Value.NumberValue, 10);
            Assert.Equal(CellError.Name, ws[R("A5")].Value.ErrorValue);
            Assert.Contains(cut.FindAll(".mud-ex-ssg-cell"), e => e.TextContent == "1,5");
            Assert.Contains(cut.FindAll(".mud-ex-ssg-cell"), e => e.TextContent == "09.11.2026");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void CtrlArrow_JumpsToRegionEdge()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        foreach (var a1 in new[] { "A1", "A2", "A3", "A4", "A5", "A10", "C1" })
            wb.Execute(new SetValueCommand(ws, R(a1), CellValue.Number(1)));
        var cut = Render(wb);

        Key(cut, "ArrowDown", ctrl: true);
        cut.WaitForAssertion(() => Assert.Equal(R("A5"), cut.Instance.Selection.Active));
        Key(cut, "ArrowDown", ctrl: true);
        cut.WaitForAssertion(() => Assert.Equal(R("A10"), cut.Instance.Selection.Active));
        Key(cut, "ArrowDown", ctrl: true);
        cut.WaitForAssertion(() => Assert.Equal(new CellRef(CellRef.MaxRows - 1, 0), cut.Instance.Selection.Active));
        Assert.Contains(cut.FindAll(".mud-ex-ssg-rh"), e => e.TextContent == CellRef.MaxRows.ToString());

        Key(cut, "Home", ctrl: true);
        Key(cut, "ArrowRight", ctrl: true);
        cut.WaitForAssertion(() => Assert.Equal(R("C1"), cut.Instance.Selection.Active));

        Key(cut, "Home", ctrl: true);
        Key(cut, "ArrowDown", ctrl: true, shift: true);
        cut.WaitForAssertion(() => Assert.Equal(CellRange.Parse("A1:A5"), cut.Instance.Selection.Range.Normalized));
        Assert.Equal(R("A1"), cut.Instance.Selection.Active);
    }

    [Fact]
    public void FormulaBar_ShowsFormula_EditsActiveCell()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(2)));
        wb.Execute(new SetFormulaCommand(ws, R("A2"), "=A1*3"));
        var ctx = NewContext();
        var grid = ctx.Render<MudExSpreadsheetGrid>(p => p.Add(x => x.Workbook, wb));
        var bar = ctx.Render<MudExSpreadsheetFormulaBar>(p => p.Add(x => x.Grid, grid.Instance));

        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        bar.WaitForAssertion(() => Assert.Equal("=A1*3", bar.Find("input.mud-ex-ssg-fx-input").GetAttribute("value")));
        Assert.Equal("A2", bar.Find("input.mud-ex-ssg-namebox").GetAttribute("value"));

        var input = bar.Find("input.mud-ex-ssg-fx-input");
        input.Focus();
        input.Input("=A1*10");
        bar.Find("input.mud-ex-ssg-fx-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        bar.WaitForAssertion(() => Assert.Equal(20, ws[R("A2")].Value.NumberValue));
        Assert.Equal("A1*10", ws[R("A2")].Formula);
        Assert.Equal(R("A3"), grid.Instance.Selection.Active);

        var box = bar.Find("input.mud-ex-ssg-namebox");
        box.Input("C7");
        box.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        bar.WaitForAssertion(() => Assert.Equal(R("C7"), grid.Instance.Selection.Active));
    }

    [Fact]
    public void FormulaAssist_SuggestsSumAndShowsSignature()
    {
        var assist = FormulaAssist.Analyze("=SU", 3, FunctionRegistry.Default, ["Sales"]);
        Assert.Contains(assist.Suggestions, s => s.Name == "SUM");
        Assert.DoesNotContain(assist.Suggestions, s => s.Name == "AVERAGE");
        Assert.Equal("SU", assist.Prefix);

        var names = FormulaAssist.Analyze("=sa", 3, FunctionRegistry.Default, ["Sales"]);
        Assert.Contains(names.Suggestions, s => s.Name == "Sales" && s.IsName);

        var tip = FormulaAssist.Analyze("=SUM(A1,", 8, FunctionRegistry.Default, []);
        Assert.Equal("SUM", tip.Function?.Name);
        Assert.Equal(1, tip.ArgumentIndex);
        Assert.Empty(FormulaAssist.Analyze("SU", 2, FunctionRegistry.Default, []).Suggestions);

        var wb = Workbook.Create();
        var cut = Render(wb);
        Type(cut, "=SU");
        cut.WaitForAssertion(() => Assert.Contains(cut.FindAll(".mud-ex-ssg-assist-item"), e => e.TextContent.Contains("SUM")));

        Key(cut, "Tab");
        cut.WaitForAssertion(() => Assert.StartsWith("=SU", cut.Instance.EditText));
        cut.WaitForAssertion(() => Assert.Contains("(", cut.Instance.EditText));
        cut.Find("textarea.mud-ex-ssg-editor").Input("=SUM(");
        cut.WaitForAssertion(() => Assert.Contains("number1", cut.Find(".mud-ex-ssg-signature").TextContent));
    }

    [Fact]
    public void MergedCell_RendersSpanningElement()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Text("Title")));
        wb.Execute(new MergeCommand(ws, CellRange.Parse("A1:C2")));
        var cut = Render(wb);

        var merged = cut.Find(".mud-ex-ssg-merged");
        Assert.Equal("Title", merged.TextContent);
        var style = merged.GetAttribute("style");
        Assert.Contains($"width:{(3 * Worksheet.StandardColumnWidth + 1).ToString(CultureInfo.InvariantCulture)}px", style);
        Assert.Contains($"height:{(2 * Worksheet.StandardRowHeight + 1).ToString(CultureInfo.InvariantCulture)}px", style);
        Assert.DoesNotContain(cut.FindAll(".mud-ex-ssg-cell:not(.mud-ex-ssg-merged)"), e => e.GetAttribute("data-r") == "0" && e.GetAttribute("data-c") == "1");

        Key(cut, "ArrowRight");
        cut.WaitForAssertion(() => Assert.Equal(R("D1"), cut.Instance.Selection.Active));
    }


    private static IRenderedComponent<MudExSpreadsheetGrid> SelectRange(IRenderedComponent<MudExSpreadsheetGrid> cut, string range)
    {
        cut.Render(p => p.Add(x => x.Selection, SpreadsheetSelection.Of(CellRange.Parse(range))));
        return cut;
    }

    private string LastClipboard() =>
        (string)_contexts[^1].JSInterop.Invocations.Last(i => i.Identifier == "writeClipboard").Arguments[0];

    [Fact]
    public async Task CutPaste_MovesFormulaWithoutAdjustingReferences()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        for (var r = 0; r < 10; r++) wb.Execute(new SetValueCommand(ws, new CellRef(r, 0), CellValue.Number(r + 1)));
        wb.Execute(new SetFormulaCommand(ws, R("A11"), "=SUM(A1:A10)"));
        var cut = Render(wb);

        SelectRange(cut, "A11");
        await cut.InvokeAsync(() => cut.Instance.CutAsync());
        SelectRange(cut, "C11");
        await cut.InvokeAsync(() => cut.Instance.PasteTextAsync(LastClipboard()));

        Assert.Equal("SUM(A1:A10)", ws[R("C11")].Formula);
        Assert.Equal(55, ws[R("C11")].Value.NumberValue);
        Assert.False(ws[R("A11")]?.HasContent == true);
    }

    [Fact]
    public async Task ConditionalFormatChange_RedrawsOnlyRuleRows()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        for (var r = 0; r < 20; r++)
        {
            wb.Execute(new SetValueCommand(ws, new CellRef(r, 0), CellValue.Number(r)));
            wb.Execute(new SetValueCommand(ws, new CellRef(r, 2), CellValue.Number(r)));
        }

        var style = new DifferentialStyle(new DifferentialFont { Color = "#FF0000" }, null);
        wb.Execute(new AddConditionalFormattingCommand(ws, new ConditionalFormatting([CellRange.Parse("A1:A5")], [new ConditionalRule.CellIs("greaterThan", "2", null, style)])));
        var cut = Render(wb);
        Dictionary<int, int> Renders() => cut.FindComponents<SpreadsheetGridRow>().ToDictionary(r => r.Instance.Row, r => r.RenderCount);
        var before = Renders();

        await cut.InvokeAsync(() => wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(5))));

        cut.WaitForAssertion(() => Assert.Contains("color:#FF0000", cut.Find(".mud-ex-ssg-cell[data-r='0'][data-c='0']").GetAttribute("style")));
        var after = Renders();
        Assert.Equal([0, 1, 2, 3, 4], after.Where(p => p.Value != before[p.Key]).Select(p => p.Key).Order());
    }

    [Fact]
    public void Grid_AriaRolesAndIndices()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
        wb.Execute(new SetValueCommand(ws, R("B2"), CellValue.Number(2)));
        var cut = Render(wb);

        var root = cut.Find(".mud-ex-spreadsheet-grid");
        Assert.Equal("grid", root.GetAttribute("role"));
        Assert.Equal("true", root.GetAttribute("aria-multiselectable"));
        var header = cut.FindAll("[role=columnheader]").First(h => h.TextContent == "B");
        Assert.Equal("3", header.GetAttribute("aria-colindex"));
        Assert.Equal("1", header.ParentElement.GetAttribute("aria-rowindex"));
        var rowHeader = cut.FindAll("[role=rowheader]").First(h => h.TextContent == "2");
        Assert.Equal("row", rowHeader.ParentElement.GetAttribute("role"));
        Assert.Equal("3", rowHeader.ParentElement.GetAttribute("aria-rowindex"));

        IElement Active() => cut.Find("#" + cut.Find(".mud-ex-ssg-ime").GetAttribute("aria-activedescendant"));
        var a1 = Active();
        Assert.Equal(("gridcell", "2", "true"), (a1.GetAttribute("role"), a1.GetAttribute("aria-colindex"), a1.GetAttribute("aria-selected")));
        Assert.Equal(("row", "2"), (a1.ParentElement.GetAttribute("role"), a1.ParentElement.GetAttribute("aria-rowindex")));

        Key(cut, "ArrowDown");
        Key(cut, "ArrowRight", shift: true);
        cut.WaitForAssertion(() => Assert.Equal(("1", "0"), (Active().GetAttribute("data-r"), Active().GetAttribute("data-c"))));
        Assert.Equal("3", Active().ParentElement.GetAttribute("aria-rowindex"));
        var b2 = cut.Find(".mud-ex-ssg-cell[data-r='1'][data-c='1']");
        Assert.Equal(("3", "true"), (b2.GetAttribute("aria-colindex"), b2.GetAttribute("aria-selected")));
        Assert.Null(cut.Find(".mud-ex-ssg-cell[data-r='0'][data-c='0']").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task StructuredReferenceOutsideTable_IsReportedNotThrown()
    {
        var wb = Workbook.Create();
        var errors = new List<Exception>();
        var cut = Render(wb, p => p.Add(x => x.OnError, (Exception e) => errors.Add(e)));

        Type(cut, "=[Amount]*2");
        await cut.InvokeAsync(cut.Instance.CommitEditAsync);

        Assert.IsType<ArgumentException>(Assert.Single(errors));
        Assert.True(cut.Instance.IsEditing);
        Assert.Null(wb.Sheets[0][R("A1")]);
    }

    [Fact]
    public async Task PasteHtmlFormula_ShiftsRelativeToTarget()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("C5"), CellValue.Number(4)));
        wb.Execute(new SetFormulaCommand(ws, R("B2"), "=A2*2+$A$1"));
        var cut = Render(wb);

        SelectRange(cut, "B2");
        await cut.InvokeAsync(() => cut.Instance.CopyAsync());
        var write = _contexts[^1].JSInterop.Invocations.Last(i => i.Identifier == "writeClipboard");
        Key(cut, "Escape");
        SelectRange(cut, "D5");
        await cut.InvokeAsync(() => cut.Instance.PasteAsync((string)write.Arguments[0], (string)write.Arguments[1]));

        Assert.Equal("C5*2+$A$1", ws[R("D5")].Formula);
        Assert.Equal(8, ws[R("D5")].Value.NumberValue);
    }

    [Fact]
    public async Task CutToOtherSheet_PrefixesSourceSheet()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new AddSheetCommand("Other Sheet"));
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(4)));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=A1*2+SUM($A$1:A1)+'Other Sheet'!A1"));
        var cut = Render(wb);

        SelectRange(cut, "B1");
        await cut.InvokeAsync(() => cut.Instance.CutAsync());
        cut.Render(p => p.Add(x => x.SheetIndex, 1));
        SelectRange(cut, "C3");
        await cut.InvokeAsync(() => cut.Instance.PasteTextAsync(LastClipboard()));

        var target = wb.Sheets[1][R("C3")];
        Assert.Equal("Sheet1!A1*2+SUM(Sheet1!$A$1:A1)+'Other Sheet'!A1", target.Formula);
        Assert.Equal(12, target.Value.NumberValue);
        Assert.False(ws[R("B1")]?.HasContent == true);
    }

    [Fact]
    public async Task CutPaste_OverlappingMoveDown_KeepsAllValues()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        for (var r = 0; r < 3; r++) wb.Execute(new SetValueCommand(ws, new CellRef(r, 0), CellValue.Number(r + 1)));
        var cut = Render(wb);

        SelectRange(cut, "A1:A3");
        await cut.InvokeAsync(() => cut.Instance.CutAsync());
        SelectRange(cut, "A2");
        await cut.InvokeAsync(() => cut.Instance.PasteTextAsync(LastClipboard()));

        Assert.False(ws[R("A1")]?.HasContent == true);
        Assert.Equal([1d, 2d, 3d], new[] { "A2", "A3", "A4" }.Select(a => ws[R(a)].Value.NumberValue));
    }

    [Fact]
    public async Task PasteStream_WritesCells()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var cut = Render(wb);
        var text = string.Concat(Enumerable.Range(1, 5000).Select(i => $"{i}\tvalue {i}\r\n"));

        await cut.InvokeAsync(() => cut.Instance.PasteStreamAsync(new TestStreamReference(text)));

        Assert.Equal(5000, ws[R("A5000")].Value.NumberValue);
        Assert.Equal("value 5000", ws[R("B5000")].Value.TextValue);
    }

    [Fact]
    public void CommitFailure_ReportsError_AndKeepsEditOpen()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var errors = new List<Exception>();
        var fail = true;
        wb.Changed += (_, _) =>
        {
            if (fail) throw new InvalidOperationException("boom");
        };
        var cut = Render(wb, p => p.Add(x => x.OnError, (Exception e) => errors.Add(e)));

        Type(cut, "42");
        Key(cut, "Enter");

        cut.WaitForAssertion(() => Assert.Equal("boom", Assert.Single(errors).Message));
        Assert.True(cut.Instance.IsEditing);
        Assert.Equal("42", cut.Instance.EditText);
        Assert.Equal(R("A1"), cut.Instance.Selection.Active);

        fail = false;
        Key(cut, "Enter");
        cut.WaitForAssertion(() => Assert.False(cut.Instance.IsEditing));
        Assert.Equal(42, ws[R("A1")].Value.NumberValue);
    }

    [Fact]
    public void Paste_Failure_IsReported()
    {
        var wb = Workbook.Create();
        var errors = new List<Exception>();
        wb.Changed += (_, _) => throw new InvalidOperationException("boom");
        var cut = Render(wb, p => p.Add(x => x.OnError, (Exception e) => errors.Add(e)));

        cut.InvokeAsync(() => cut.Instance.PasteTextAsync("1\t2")).GetAwaiter().GetResult();

        Assert.Equal("boom", Assert.Single(errors).Message);
    }

    [Fact]
    public void Shortcuts_SaveAndPercent()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var saves = 0;
        var cut = Render(wb, p => p.Add(x => x.SaveKeyPressed, () => saves++));

        Key(cut, "s", ctrl: true);
        Assert.Equal(1, saves);
        Type(cut, "1");
        Key(cut, "s", ctrl: true);
        Assert.Equal(2, saves);
        Key(cut, "Escape");

        Key(cut, "%", ctrl: true, shift: true);
        Assert.Equal("0%", wb.Styles[ws.EffectiveStyleId(R("A1"))].NumberFormat);
        Key(cut, "5", ctrl: true);
        Assert.True(wb.Styles[ws.EffectiveStyleId(R("A1"))].Font.Strikethrough);
    }

    [Fact]
    public async Task SetValuesBlock_RefreshesGridWithoutWalkingCells()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var cut = Render(wb);
        var values = new CellValue[100_000, 30];
        for (var r = 0; r < 100_000; r++)
            for (var c = 0; c < 30; c++)
                values[r, c] = CellValue.Number(r * 30 + c);

        await cut.InvokeAsync(() => wb.Execute(new SetValuesCommand(ws, R("A1"), values)));

        cut.WaitForAssertion(() => Assert.Contains(cut.FindAll(".mud-ex-ssg-cell"), e => e.TextContent.Trim() == "31"));
        Assert.Equal(99_999, cut.Instance.ActiveSheet.UsedRange.Bottom);
    }
    [Fact]
    public async Task Paste_TabSeparatedText_FillsRange()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var cut = Render(wb);
        Key(cut, "ArrowDown");
        Key(cut, "ArrowRight");

        await cut.InvokeAsync(() => cut.Instance.PasteTextAsync("1\t2\r\n3\t=B2+C2\r\n"));

        Assert.Equal(1, ws[R("B2")].Value.NumberValue);
        Assert.Equal(2, ws[R("C2")].Value.NumberValue);
        Assert.Equal(3, ws[R("B3")].Value.NumberValue);
        Assert.Equal(3, ws[R("C3")].Value.NumberValue);
        Assert.Equal(CellRange.Parse("B2:C3"), cut.Instance.Selection.Range.Normalized);

        wb.Undo.Undo();
        Assert.Null(ws[R("B2")]);
        Assert.Null(ws[R("C3")]);
    }

    [Fact]
    public async Task ReadOnly_BlocksEditing()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
        var cut = Render(wb, p => p.Add(x => x.ReadOnly, true));

        Key(cut, "a");
        Key(cut, "F2");
        await cut.InvokeAsync(() => cut.Instance.BeginEditAsync("x"));
        Assert.False(cut.Instance.IsEditing);

        Key(cut, "Delete");
        await cut.InvokeAsync(() => cut.Instance.PasteTextAsync("5"));
        Key(cut, "b", ctrl: true);
        Assert.Equal(1, ws[R("A1")].Value.NumberValue);
        Assert.Equal(0, ws[R("A1")].StyleId);
        Assert.False(wb.Undo.CanUndo && wb.Undo.UndoDescription != "Set value");
        Assert.Empty(cut.FindAll(".mud-ex-ssg-fill"));

        Key(cut, "ArrowDown");
        cut.WaitForAssertion(() => Assert.Equal(R("A2"), cut.Instance.Selection.Active));
    }

    [Fact]
    public void AxisMetrics_OffsetsAndLookup()
    {
        var axis = new SpreadsheetAxis(20, new Dictionary<int, double> { [2] = 50, [5] = 10 }, new HashSet<int> { 3 }, 1000);
        Assert.Equal(0, axis.Offset(0));
        Assert.Equal(40, axis.Offset(2));
        Assert.Equal(90, axis.Offset(3));
        Assert.Equal(90, axis.Offset(4));
        Assert.Equal(110, axis.Offset(5));
        Assert.Equal(120, axis.Offset(6));
        Assert.Equal(0, axis.Size(3));
        Assert.Equal(2, axis.IndexAt(89));
        Assert.Equal(4, axis.IndexAt(90));
        Assert.Equal(6, axis.IndexAt(120));
        Assert.Equal(999, axis.IndexAt(1e9));
    }
}
