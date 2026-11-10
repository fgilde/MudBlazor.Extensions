using System.Globalization;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Spreadsheet;
using MudBlazor.Extensions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class SpreadsheetEditorTests : IAsyncLifetime
{
    private const string XlsxType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly List<BunitContext> _contexts = [];
    private readonly List<IRenderedComponent<MudPopoverProvider>> _popovers = [];
    private readonly List<IRenderedComponent<MudDialogProvider>> _dialogs = [];
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

    private sealed record Infos(string FileName, string ContentType, byte[] Bytes) : IMudExFileDisplayInfos
    {
        public string Url => null;
        public Stream ContentStream { get; } = new MemoryStream(Bytes);
    }

    private BunitContext NewContext() => NewContext(out _);

    private BunitContext NewContext(out IRenderedComponent<MudPopoverProvider> popovers)
    {
        var context = new BunitContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        popovers = context.Render<MudPopoverProvider>();
        _popovers.Add(popovers);
        _dialogs.Add(context.Render<MudDialogProvider>());
        return context;
    }

    private IRenderedComponent<MudExSpreadsheetEditor> Render(Action<ComponentParameterCollectionBuilder<MudExSpreadsheetEditor>> configure)
        => NewContext().Render(configure);

    private IRenderedComponent<MudExSpreadsheetEditor> Render(Workbook wb, bool readOnly = false)
        => Render(p => p.Add(x => x.Workbook, wb).Add(x => x.ReadOnly, readOnly));

    private static byte[] Xlsx(Workbook wb)
    {
        using var ms = new MemoryStream();
        XlsxWriter.Write(wb, ms);
        return ms.ToArray();
    }

    private static Workbook Book(params (string Sheet, string Cell, object Value)[] cells)
    {
        var wb = Workbook.Create(cells.Length > 0 ? cells[0].Sheet : "Sheet1");
        foreach (var (sheet, cell, value) in cells)
        {
            if (wb.FindSheet(sheet) == null)
                wb.Execute(new AddSheetCommand(sheet));
            var ws = wb.FindSheet(sheet);
            if (value is string s && s.StartsWith('='))
                wb.Execute(new SetFormulaCommand(ws, CellRef.Parse(cell), s[1..]));
            else
                wb.Execute(new SetValueCommand(ws, CellRef.Parse(cell), value is double d ? CellValue.Number(d) : CellValue.Text((string)value)));
        }

        wb.Undo.Clear();
        return wb;
    }

    private static MudExRibbonItemBase Command(IRenderedComponent<MudExSpreadsheetEditor> cut, string id)
        => cut.FindComponent<MudExRibbon>().Instance.Commands.SingleOrDefault(c => c.Id == id)
           ?? cut.FindComponents<MudExRibbonButton>().Select(b => b.Instance).Single(b => b.Id == id);

    private static Task Run(IRenderedComponent<MudExSpreadsheetEditor> cut, string id)
        => cut.InvokeAsync(() => Command(cut, id).ExecuteAsync());

    private static void Select(IRenderedComponent<MudExSpreadsheetEditor> cut, string range)
    {
        cut.Render(p => p.Add(x => x.Selection, SpreadsheetSelection.Of(CellRange.Parse(range))));
        cut.WaitForAssertion(() => Assert.Equal(CellRange.Parse(range), cut.FindComponent<MudExSpreadsheetGrid>().Instance.Selection.Range));
    }


    private sealed record ConsumedInfos(string FileName, string ContentType, byte[] Bytes) : IMudExFileDisplayInfos
    {
        public string Url => null;
        public Stream ContentStream { get; } = new MemoryStream(Bytes) { Position = Bytes.Length };
    }

    [Fact]
    public void OpensXlsx_WhenContentStreamWasAlreadyRead()
    {
        var bytes = Xlsx(Book(("Data", "A1", 42d)));

        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new ConsumedInfos("book.xlsx", XlsxType, bytes)));

        cut.WaitForAssertion(() => Assert.Equal(42, cut.Instance.CurrentWorkbook.Sheets[0][CellRef.Parse("A1")].Value.NumberValue));
    }

    [Fact]
    public async Task FileService_ReadsConsumedContentStreamFromStart()
    {
        var service = new MudExFileService(null, new ServiceCollection().BuildServiceProvider());
        var infos = new ConsumedInfos("a.txt", "text/plain", "hello"u8.ToArray());

        Assert.Equal("hello", await service.ReadAsStringFromFileDisplayInfosAsync(infos));
        infos.ContentStream.Position = infos.ContentStream.Length;
        await using var stream = await service.ReadStreamAsync(infos);
        Assert.Equal(5, stream.Length);
    }

    [Fact]
    public async Task EmptyXlsx_IsLoadError_NotNewWorkbook()
    {
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("book.xlsx", XlsxType, [])));

        cut.WaitForAssertion(() => Assert.Contains("The file is empty.", _dialogs[^1].Markup));
        Assert.Null(cut.Instance.CurrentWorkbook);
        Assert.False(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));
    }

    [Fact]
    public void EmptyCsv_OpensEmptySheet()
    {
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("data.csv", "text/csv", [])));

        cut.WaitForAssertion(() => Assert.Equal("data", cut.Instance.CurrentWorkbook.Sheets[0].Name));
    }
    [Fact]
    public void OpensXlsxFromFileDisplayInfos_ShowsSheetsAndValues()
    {
        var bytes = Xlsx(Book(("Data", "A1", 42d), ("Data", "A2", "Hello"), ("Data", "A3", "=A1*2"), ("Other", "B2", "x")));

        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("book.xlsx", XlsxType, bytes)));

        cut.WaitForAssertion(() => Assert.Equal(["Data", "Other"], cut.FindAll(".mud-ex-ssst-tab").Select(t => t.TextContent.Trim())));
        var cells = cut.FindAll(".mud-ex-ssg-cell").Select(c => c.TextContent.Trim()).ToList();
        Assert.Contains("42", cells);
        Assert.Contains("Hello", cells);
        Assert.Contains("84", cells);
        Assert.Equal("book.xlsx", cut.Instance.FileName);
        Assert.False(cut.Instance.IsDirty);
        Assert.Equal("Excel Workbook", cut.Instance.SaveFormats[0].Name);
    }

    [Fact]
    public async Task IsEditorForXlsxCsvXls_InFileDisplay()
    {
        var editor = new MudExSpreadsheetEditor();
        foreach (var (name, type) in new[] { ("a.xlsx", ""), ("a.xlsm", ""), ("a.xltx", ""), ("a.xls", ""), ("a.csv", ""), ("a.tsv", ""), ("noext", XlsxType), ("noext", "text/csv") })
            Assert.True(await editor.CanHandleFileAsync(new Infos(name, type, []), null), name + " " + type);
        Assert.False(await editor.CanHandleFileAsync(new Infos("a.txt", "text/plain", []), null));

        var priority = ((IMudExFileDisplay)editor).RenderPriority;
        Assert.Equal(10, priority);
        Assert.True(priority > ((IMudExFileDisplay)new MudExFileDisplayExcel()).RenderPriority);
        Assert.True(priority > ((IMudExFileDisplay)new MudExFileDisplayExcelUniver()).RenderPriority);

        var context = NewContext(out var provider);
        var display = context.Render<MudExFileDisplay>(p => p
            .Add(c => c.FileName, "book.xlsx")
            .Add(c => c.ContentType, XlsxType)
            .Add(c => c.ContentStream, new MemoryStream(Xlsx(Book(("Sheet1", "A1", 1d)))))
            .Add(c => c.AllowEdit, true));
        await display.InvokeAsync(() => display.FindComponent<MoveContent>().Instance.ElementFoundChanged(false));

        display.WaitForAssertion(() => Assert.NotEmpty(display.FindComponents<MudExSpreadsheetEditor>()));
        Assert.True(display.Instance.CanEdit);
        display.Find(".mud-menu button").Click();
        provider.WaitForAssertion(() => Assert.Contains(">Edit<", provider.Markup));
        display.WaitForAssertion(() => Assert.True(display.FindComponent<MudExSpreadsheetEditor>().Instance.ReadOnly));
    }

    [Fact]
    public async Task BoldButton_TogglesStyle_AndUndoRestores()
    {
        var wb = Book(("Sheet1", "A1", "text"));
        var cut = Render(wb);
        var sheet = wb.Sheets[0];
        bool Bold() => wb.Styles[sheet.EffectiveStyleId(CellRef.Parse("A1"))].Font.Bold;

        await Run(cut, "xl-bold");

        Assert.True(Bold());
        cut.WaitForAssertion(() => Assert.Contains(cut.FindAll(".mud-ex-ribbon-button"), b => b.GetAttribute("aria-label") == "Bold" && b.GetAttribute("aria-pressed") == "true"));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));

        await Run(cut, "xl-undo");

        Assert.False(Bold());
        cut.WaitForAssertion(() => Assert.Contains(cut.FindAll(".mud-ex-ribbon-button"), b => b.GetAttribute("aria-label") == "Bold" && b.GetAttribute("aria-pressed") == "false"));
    }

    [Fact]
    public async Task AutoSum_InsertsSumOfRegionAbove()
    {
        var wb = Book(("Sheet1", "A1", 1d), ("Sheet1", "A2", 2d), ("Sheet1", "A3", 3d));
        var cut = Render(wb);
        Select(cut, "A4");

        await Run(cut, "xl-autosum");

        var grid = cut.FindComponent<MudExSpreadsheetGrid>().Instance;
        cut.WaitForAssertion(() => Assert.Equal("=SUM(A1:A3)", grid.EditText));
        await cut.InvokeAsync(grid.CommitEditAsync);
        Assert.Equal("SUM(A1:A3)", wb.Sheets[0][CellRef.Parse("A4")].Formula);
        Assert.Equal(6, wb.Sheets[0][CellRef.Parse("A4")].Value.NumberValue);

        Select(cut, "B1");
        await Run(cut, "xl-autosum-AVERAGE");
        cut.WaitForAssertion(() => Assert.Equal("=AVERAGE(A1)", grid.EditText));
    }

    [Fact]
    public async Task FreezeTopRow_SetsFreeze()
    {
        var wb = Book(("Sheet1", "A1", 1d));
        var cut = Render(wb);

        await Run(cut, "xl-freeze-top-row");
        Assert.Equal(new CellRef(1, 0), wb.Sheets[0].FreezeAt);

        await Run(cut, "xl-freeze-first-column");
        Assert.Equal(new CellRef(0, 1), wb.Sheets[0].FreezeAt);

        Select(cut, "C3");
        await Run(cut, "xl-freeze-panes");
        Assert.Equal(new CellRef(2, 2), wb.Sheets[0].FreezeAt);

        await Run(cut, "xl-unfreeze");
        Assert.Null(wb.Sheets[0].FreezeAt);
    }


    [Fact]
    public void SheetTabs_KeyboardSelectsSheets()
    {
        var wb = Book(("A", "A1", 1d), ("B", "A1", 2d), ("C", "A1", 3d));
        var cut = Render(wb, readOnly: true);
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".mud-ex-ssst-tab").Count));
        Assert.Equal(["0", "-1", "-1"], cut.FindAll(".mud-ex-ssst-tab").Select(t => t.GetAttribute("tabindex")));

        cut.FindAll(".mud-ex-ssst-tab")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        cut.WaitForAssertion(() => Assert.Equal("B", cut.Instance.ActiveSheet.Name));
        cut.FindAll(".mud-ex-ssst-tab")[1].KeyDown(new KeyboardEventArgs { Key = "End" });
        cut.WaitForAssertion(() => Assert.Equal("C", cut.Instance.ActiveSheet.Name));
        cut.FindAll(".mud-ex-ssst-tab")[2].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        cut.WaitForAssertion(() => Assert.Equal("B", cut.Instance.ActiveSheet.Name));
        Assert.Equal(["-1", "0", "-1"], cut.FindAll(".mud-ex-ssst-tab").Select(t => t.GetAttribute("tabindex")));
    }
    [Fact]
    public async Task SheetTabs_AddRenameDeleteMove()
    {
        var wb = Book(("Sheet1", "A1", 1d));
        var cut = Render(wb);
        string[] Tabs() => cut.FindAll(".mud-ex-ssst-tab").Select(t => t.TextContent.Trim()).ToArray();

        cut.Find(".mud-ex-ssst-add").Click();
        cut.WaitForAssertion(() => Assert.Equal(["Sheet1", "Sheet2"], Tabs()));
        Assert.Same(wb.Sheets[1], cut.Instance.ActiveSheet);
        Assert.Contains("mud-ex-ssst-active", cut.FindAll(".mud-ex-ssst-tab")[1].ClassName);

        cut.FindAll(".mud-ex-ssst-tab")[1].DoubleClick();
        cut.WaitForAssertion(() => cut.Find("input.mud-ex-ssst-rename"));
        cut.Find("input.mud-ex-ssst-rename").Input("Data");
        cut.Find("input.mud-ex-ssst-rename").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        cut.WaitForAssertion(() => Assert.Equal(["Sheet1", "Data"], Tabs()));

        cut.FindAll(".mud-ex-ssst-tab")[0].ContextMenu();
        cut.WaitForAssertion(() => Assert.Contains("Move Right", _popovers[^1].Markup));

        var tabs = cut.FindComponent<MudExSpreadsheetSheetTabs>();
        await cut.InvokeAsync(() => tabs.Instance.MoveSheetAsync(1, 0));
        cut.WaitForAssertion(() => Assert.Equal(["Data", "Sheet1"], Tabs()));
        Assert.Equal("Data", cut.Instance.ActiveSheet.Name);

        await cut.InvokeAsync(() => tabs.Instance.DeleteSheetAsync(0));
        cut.WaitForAssertion(() => Assert.Equal(["Sheet1"], Tabs()));
        Assert.Equal("Sheet1", cut.Instance.ActiveSheet.Name);

        await cut.InvokeAsync(() => tabs.Instance.DeleteSheetAsync(0));
        Assert.Single(wb.Sheets);

        cut.FindAll(".mud-ex-ssst-tab")[0].Click();
        await cut.InvokeAsync(wb.Undo.Undo);
        cut.WaitForAssertion(() => Assert.Equal(["Data", "Sheet1"], Tabs()));
    }

    [Fact]
    public void StatusBar_ShowsSumOfSelection()
    {
        var wb = Book(("Sheet1", "A1", 1d), ("Sheet1", "A2", 2d), ("Sheet1", "A3", 3d), ("Sheet1", "A4", "x"));
        var cut = Render(wb);

        Select(cut, "A1:A4");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Sum: 6", cut.Find(".mud-ex-sssb-sum").TextContent.Trim());
            Assert.Equal("Average: 2", cut.Find(".mud-ex-sssb-average").TextContent.Trim());
            Assert.Equal("Count: 4", cut.Find(".mud-ex-sssb-count").TextContent.Trim());
        });

        Select(cut, "A1");
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-sssb-sum")));

        Select(cut, "A:A");
        cut.WaitForAssertion(() => Assert.Equal("Sum: 6", cut.Find(".mud-ex-sssb-sum").TextContent.Trim()));
    }

    [Fact]
    public async Task SaveAsCsv_ExportsActiveSheet()
    {
        var bytes = Xlsx(Book(("First", "A1", 1d), ("Second", "A1", "a"), ("Second", "B1", 2.5d), ("Second", "A2", "=B1*2")));
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("book.xlsx", XlsxType, bytes)));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".mud-ex-ssst-tab").Count));
        cut.FindAll(".mud-ex-ssst-tab")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal("Second", cut.Instance.ActiveSheet.Name));

        var csv = cut.Instance.SaveFormats.Single(f => f.Extension == "csv");
        await using var content = await cut.InvokeAsync(() => cut.Instance.ExportAsync(csv));

        Assert.Equal("book.csv", content.FileName);
        Assert.Equal("text/csv", content.ContentType);
        using var reader = new StreamReader(content.Stream, Encoding.UTF8);
        Assert.Equal("a,2.5\r\n5,\r\n", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Save_RoundTripsThroughXlsxWriter()
    {
        var bytes = Xlsx(Book(("Sheet1", "A1", 2d), ("Sheet1", "A2", "=A1*3"), ("Sheet1", "B1", "text")));
        byte[] saved = null;
        string savedName = null;
        var cut = Render(p => p
            .Add(x => x.FileDisplayInfos, new Infos("book.xlsx", XlsxType, bytes))
            .Add(x => x.SaveRequested, async (MudExFileSaveRequest request) =>
            {
                using var ms = new MemoryStream();
                await request.Content.Stream.CopyToAsync(ms);
                saved = ms.ToArray();
                savedName = request.Content.FileName;
                request.Handled = true;
            }));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.CurrentWorkbook));
        var wb = cut.Instance.CurrentWorkbook;
        await cut.InvokeAsync(() => wb.Execute(new SetValueCommand(wb.Sheets[0], CellRef.Parse("A1"), CellValue.Number(5))));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));

        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));

        Assert.False(cut.Instance.IsDirty);
        Assert.Equal("book.xlsx", savedName);
        var reread = XlsxReader.Read(new MemoryStream(saved)).Sheets[0];
        Assert.Equal(5, reread[CellRef.Parse("A1")].Value.NumberValue);
        Assert.Equal("A1*3", reread[CellRef.Parse("A2")].Formula);
        Assert.Equal(15, reread[CellRef.Parse("A2")].Value.NumberValue);
        Assert.Equal("text", reread[CellRef.Parse("B1")].Value.ToString());
    }


    [Fact]
    public void CtrlS_InStandaloneEditor_Saves()
    {
        var requests = 0;
        var cut = Render(p => p
            .Add(x => x.Workbook, Book(("Sheet1", "A1", 1d)))
            .Add(x => x.SaveRequested, (MudExFileSaveRequest r) => { requests++; r.Handled = true; }));

        cut.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "s", CtrlKey = true });

        cut.WaitForAssertion(() => Assert.Equal(1, requests));
    }

    [Fact]
    public async Task RibbonCommand_UnexpectedException_IsShown()
    {
        var wb = Book(("Sheet1", "A1", 1d));
        var cut = Render(wb);
        wb.Changed += (_, _) => throw new NotSupportedException("boom");

        await Run(cut, "xl-bold");

        cut.WaitForAssertion(() => Assert.Contains("boom", _dialogs[^1].Markup));
    }

    private static byte[] WithPageBreak(byte[] xlsx)
    {
        using var ms = new MemoryStream();
        ms.Write(xlsx);
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Update, true))
        {
            var entry = zip.GetEntry("xl/worksheets/sheet1.xml")!;
            string xml;
            using (var reader = new StreamReader(entry.Open()))
                xml = reader.ReadToEnd();
            entry.Delete();
            using var writer = new StreamWriter(zip.CreateEntry("xl/worksheets/sheet1.xml").Open());
            writer.Write(xml.Replace("</sheetData>", "</sheetData><protectedRanges><protectedRange sqref=\"A1:B2\" name=\"p\"/></protectedRanges>"));
        }

        return ms.ToArray();
    }

    private static int Count(string text, string part) => (text.Length - text.Replace(part, "").Length) / part.Length;

    [Fact]
    public async Task SaveAsCsv_WithSeveralSheets_WarnsOnce()
    {
        var bytes = Xlsx(Book(("First", "A1", 1d), ("Second", "A1", 2d)));
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("book.xlsx", XlsxType, bytes)));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.CurrentWorkbook));
        var csv = cut.Instance.SaveFormats.Single(f => f.Extension == "csv");

        await using (await cut.InvokeAsync(() => cut.Instance.ExportAsync(csv))) { }
        await using (await cut.InvokeAsync(() => cut.Instance.ExportAsync(csv))) { }

        cut.WaitForAssertion(() => Assert.Equal(1, Count(_dialogs[^1].Markup, "Only the active sheet")));
        Assert.Contains("First", _dialogs[^1].Markup);
    }

    [Fact]
    public async Task SaveAsCsv_UsesCultureListSeparator()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        var bytes = Xlsx(Book(("Sheet1", "A1", 1.5d), ("Sheet1", "B1", "x")));
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("book.xlsx", XlsxType, bytes)));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.CurrentWorkbook));

        await using var content = await cut.InvokeAsync(() => cut.Instance.ExportAsync(cut.Instance.SaveFormats.Single(f => f.Extension == "csv")));

        using var reader = new StreamReader(content.Stream, Encoding.UTF8);
        Assert.Equal("1,5;x\r\n", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task UnshiftedReferences_WarnOnceOnFirstStructureChange()
    {
        var bytes = WithPageBreak(Xlsx(Book(("Sheet1", "A1", 1d))));
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("book.xlsx", XlsxType, bytes)));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.CurrentWorkbook?.HasUnshiftedReferences));

        await Run(cut, "xl-bold");
        Assert.DoesNotContain("not updated", _dialogs[^1].Markup);
        await Run(cut, "xl-insert-rows");
        await Run(cut, "xl-insert-columns");

        cut.WaitForAssertion(() => Assert.Equal(1, Count(_dialogs[^1].Markup, "not updated")));
    }

    [Fact]
    public async Task SaveXls_SavesAsXlsxWithSaveAs()
    {
        var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "Spreadsheet", "sample.xls"));
        var requests = new List<(bool SaveAs, string FileName)>();
        var cut = Render(p => p
            .Add(x => x.FileDisplayInfos, new Infos("old.xls", "application/vnd.ms-excel", bytes))
            .Add(x => x.SaveRequested, (MudExFileSaveRequest r) => { requests.Add((r.SaveAs, r.Content.FileName)); r.Handled = true; }));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.CurrentWorkbook));

        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));
        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));

        Assert.Equal([(true, "old.xlsx"), (false, "old.xlsx")], requests);
        Assert.Equal("old.xlsx", cut.Instance.FileName);
    }

    [Fact]
    public async Task InsertSheet_SelectsNewSheet()
    {
        var cut = Render(Book(("Data", "A1", 1d)));

        await Run(cut, "xl-insert-sheet");

        cut.WaitForAssertion(() => Assert.Equal("Sheet2", cut.Instance.ActiveSheet.Name));
        Assert.Equal(["Sheet2", "Data"], cut.Instance.CurrentWorkbook.Sheets.Select(s => s.Name));
        Assert.Equal("Sheet2", cut.FindComponent<MudExSpreadsheetGrid>().Instance.ActiveSheet.Name);
    }

    [Fact]
    public async Task StructureCommand_CommitsOpenEditFirst()
    {
        var wb = Book(("Sheet1", "A1", 1d));
        var cut = Render(wb);
        Select(cut, "A2");
        var grid = cut.FindComponent<MudExSpreadsheetGrid>();
        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "7" });
        grid.Find("textarea.mud-ex-ssg-editor").Input("7");
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));

        await Run(cut, "xl-insert-rows");

        var sheet = wb.Sheets[0];
        Assert.False(grid.Instance.IsEditing);
        Assert.Equal(7, sheet[CellRef.Parse("A3")].Value.NumberValue);
        Assert.Null(sheet[CellRef.Parse("A2")]);
    }

    private static int RibbonRenders(IRenderedComponent<MudExSpreadsheetEditor> cut)
        => cut.FindComponents<MudExRibbonMenuItem>().Sum(c => c.RenderCount)
           + cut.FindComponents<MudExRibbonMenuButton>().Sum(c => c.RenderCount)
           + cut.FindComponents<MudExRibbonButton>().Sum(c => c.RenderCount);

    [Fact]
    public void SelectionChange_DoesNotRenderRibbonUnlessStyleChanges()
    {
        var wb = Book(("Sheet1", "A1", 1d), ("Sheet1", "A2", 2d), ("Sheet1", "A3", 3d));
        wb.Execute(new SetStyleCommand(wb.Sheets[0], CellRange.Parse("A3"), s => s with { Font = s.Font with { Bold = true } }));
        var cut = Render(wb);
        var grid = cut.FindComponent<MudExSpreadsheetGrid>();
        cut.WaitForAssertion(() => Assert.NotNull(cut.FindComponent<MudExSpreadsheetFormulaBar>()));
        var ribbonRenders = RibbonRenders(cut);

        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", ShiftKey = true });
        cut.WaitForAssertion(() => Assert.Equal("Sum: 3", cut.Find(".mud-ex-sssb-sum").TextContent.Trim()));

        Assert.Equal(ribbonRenders, RibbonRenders(cut));

        var names = FunctionRegistry.Default.Functions.Select(f => f.Info.Name).ToHashSet();
        var functionItems = cut.FindComponents<MudExRibbonMenuItem>().Where(c => names.Contains(c.Instance.Text)).ToList();
        Assert.True(functionItems.Count > 500, functionItems.Count.ToString());
        var functionRenders = functionItems.Sum(c => c.RenderCount);
        ribbonRenders = RibbonRenders(cut);
        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.WaitForAssertion(() => Assert.True(cut.FindComponents<MudExRibbonToggleButton>().Single(b => b.Instance.Id == "xl-bold").Instance.Checked));
        Assert.Equal(functionRenders, functionItems.Sum(c => c.RenderCount));
    }

    [Fact]
    public void RibbonColorDefault_IsExcelGreen()
    {
        Assert.Equal("#217346", new MudExSpreadsheetEditor().RibbonColor.ToString());

        var cut = Render(Workbook.Create());

        Assert.Equal("#217346", cut.FindComponent<MudExRibbon>().Instance.Color.ToString());
        Assert.StartsWith("--mud-ex-spreadsheet-accent:#217346;", cut.Find(".mud-ex-spreadsheet-editor").GetAttribute("style"));
    }

    [Fact]
    public void ReadOnly_ViewerHasNoEditCommands()
    {
        var wb = Book(("Sheet1", "A1", 1d));
        var cut = Render(wb, readOnly: true);

        var ribbon = cut.FindComponent<MudExRibbon>().Instance;
        cut.WaitForAssertion(() => Assert.Equal(["View"], cut.FindComponent<MudExRibbon>().FindAll("[role=tab]").Select(t => t.TextContent.Trim())));
        var visible = ribbon.Commands.Where(c => c.Tab is { Visible: true }).Select(c => c.Id).ToList();
        Assert.DoesNotContain("xl-bold", visible);
        Assert.DoesNotContain("xl-freeze-top-row", visible);
        Assert.DoesNotContain("xl-undo", ribbon.Commands.Select(c => c.Id));
        Assert.Contains("xl-show-gridlines", visible);
        Assert.True(cut.FindComponent<MudExSpreadsheetGrid>().Instance.ReadOnly);
        Assert.Empty(cut.FindAll(".mud-ex-ssst-add"));
        Assert.DoesNotContain(cut.FindComponents<MudMenu>(), m => m.FindAll(".mud-ex-ssst-tab").Count > 0);
        Assert.True(cut.Instance.ReadOnly);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task StandaloneWorkbookBinding_RaisesWorkbookChanged()
    {
        var wb = Book(("Sheet1", "A1", 1d));
        var raised = new List<Workbook>();
        var cut = Render(p => p
            .Add(x => x.Workbook, wb)
            .Add(x => x.WorkbookChanged, (Workbook w) => raised.Add(w)));
        Assert.Same(wb, cut.Instance.CurrentWorkbook);
        Assert.Empty(raised);

        await cut.InvokeAsync(() => wb.Execute(new SetValueCommand(wb.Sheets[0], CellRef.Parse("B1"), CellValue.Number(2))));
        cut.WaitForAssertion(() => Assert.Equal([wb], raised));
        Assert.True(cut.Instance.IsDirty);

        var fresh = Workbook.Create("Fresh");
        cut.Render(p => p.Add(x => x.Workbook, fresh));
        Assert.Same(fresh, cut.Instance.CurrentWorkbook);
        cut.WaitForAssertion(() => Assert.Equal(["Fresh"], cut.FindAll(".mud-ex-ssst-tab").Select(t => t.TextContent.Trim())));
        await cut.InvokeAsync(() => fresh.Execute(new SetValueCommand(fresh.Sheets[0], CellRef.Parse("A1"), CellValue.Number(1))));
        cut.WaitForAssertion(() => Assert.Same(fresh, raised[^1]));

        var clean = Render(p => p.Add(x => x.WorkbookChanged, (Workbook w) => raised.Add(w)));
        Assert.True(await clean.InvokeAsync(() => clean.Instance.NewAsync()));
        Assert.Same(clean.Instance.CurrentWorkbook, raised[^1]);
        Assert.Equal("Book1.xlsx", clean.Instance.FileName);
    }

    private sealed class CountingEditor : MudExSpreadsheetEditor
    {
        public int Renders { get; private set; }

        protected override void OnAfterRender(bool firstRender)
        {
            Renders++;
            base.OnAfterRender(firstRender);
        }
    }

    private sealed class BoundHost : ComponentBase
    {
        [Parameter] public Workbook Workbook { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b)
        {
            b.OpenComponent<CountingEditor>(0);
            b.AddComponentParameter(1, nameof(MudExSpreadsheetEditor.Workbook), Workbook);
            b.AddComponentParameter(2, nameof(MudExSpreadsheetEditor.WorkbookChanged), EventCallback.Factory.Create<Workbook>(this, w => Workbook = w));
            b.AddComponentParameter(3, nameof(MudExSpreadsheetEditor.IsDirtyChanged), EventCallback.Factory.Create<bool>(this, _ => { }));
            b.AddComponentParameter(4, nameof(MudExSpreadsheetEditor.ContentChanged), EventCallback.Factory.Create(this, () => { }));
            b.CloseComponent();
        }
    }

    [Fact]
    public async Task EditorRenders_OncePerWorkbookChange()
    {
        var wb = Book(("Sheet1", "A1", 1d));
        var host = NewContext().Render<BoundHost>(p => p.Add(x => x.Workbook, wb));
        var editor = host.FindComponent<CountingEditor>().Instance;
        host.WaitForAssertion(() => Assert.NotNull(editor.Grid));

        foreach (var value in new[] { 2d, 3d })
        {
            var before = editor.Renders;
            await host.InvokeAsync(() => wb.Execute(new SetValueCommand(wb.Sheets[0], CellRef.Parse("B1"), CellValue.Number(value))));
            host.WaitForAssertion(() => Assert.True(editor.IsDirty));
            await host.InvokeAsync(() => Task.Delay(50));
            Assert.Equal(before + 1, editor.Renders);
        }
    }

    [Theory]
    [InlineData("General", 1, 1.5, "0.00")]
    [InlineData("General", -1, 1.5, "0")]
    [InlineData("General", 1, 3, "0.0")]
    [InlineData("0.00", 1, 0, "0.000")]
    [InlineData("0.0", -1, 0, "0")]
    [InlineData("#,##0", 1, 0, "#,##0.0")]
    [InlineData("0.00%", -1, 0, "0.0%")]
    [InlineData("0.00E+00", 1, 0, "0.000E+00")]
    [InlineData("#,##0.00;[Red]-#,##0.00", 1, 0, "#,##0.000;[Red]-#,##0.000")]
    [InlineData("\"0\"0", 1, 0, "\"0\"0.0")]
    [InlineData("# ?/?", 1, 0, "# ?/?")]
    [InlineData("dd.mm.yyyy", 1, 0, "dd.mm.yyyy")]
    public void ChangeDecimals_AdjustsNumberFormats(string code, int delta, double sample, string expected)
        => Assert.Equal(expected, MudExSpreadsheetEditor.ChangeDecimals(code, delta, CellValue.Number(sample)));
}
