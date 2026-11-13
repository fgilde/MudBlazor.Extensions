using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideTableTests : IAsyncLifetime
{
    private const long Inch = 914400;

    private readonly BunitContext _context = new();
    private IRenderedComponent<MudPopoverProvider> _popovers = null!;

    public Task InitializeAsync()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _popovers = _context.Render<MudPopoverProvider>();
        _context.Render<MudDialogProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private IRenderedComponent<MudExSlideEditor> RenderEditor(Presentation presentation)
    {
        var cut = _context.Render<MudExSlideEditor>(p => p.Add(x => x.Presentation, presentation));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-canvas .mud-ex-slide")));
        return cut;
    }

    private static MudExSlideCanvas Canvas(IRenderedComponent<MudExSlideEditor> cut) => cut.FindComponent<MudExSlideCanvas>().Instance;

    private static Task Run(IRenderedComponent<MudExSlideEditor> cut, string id)
        => cut.InvokeAsync(() => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).ExecuteAsync());

    private static Task Key(IRenderedComponent<MudExSlideEditor> cut, string key, bool shift = false) =>
        cut.Find(".mud-ex-slide-canvas").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = key, ShiftKey = shift });

    private static async Task SelectCellsAsync(IRenderedComponent<MudExSlideEditor> cut, int row, int column, int row2, int column2)
    {
        await cut.InvokeAsync(() => Canvas(cut).OnSelectionAsync([3]));
        await cut.InvokeAsync(() => Canvas(cut).OnCellSelectAsync(3, row, column, row2, column2));
    }

    private static SlideTextParagraphDto[] Dom(string text) => [new() { Source = 0, Runs = [new() { Source = 0, Text = text }] }];

    [Fact]
    public async Task Table_InsertFromGrid_AddsTableShape()
    {
        var presentation = TableTests.Deck();
        var slide = presentation.Slides[1];
        var cut = RenderEditor(presentation);
        await cut.InvokeAsync(() => cut.FindAll(".mud-ex-slide-panel-item")[1].Click());

        await cut.InvokeAsync(() => cut.FindAll("[role='tab']").First(t => t.TextContent.Trim() == "Insert").Click());
        cut.WaitForAssertion(() => Assert.Contains(cut.FindAll("button.mud-ex-ribbon-menu-face"), b => b.GetAttribute("aria-label") == "Table"));
        await cut.InvokeAsync(() => cut.FindAll("button.mud-ex-ribbon-menu-face").First(b => b.GetAttribute("aria-label") == "Table").Click());
        _popovers.WaitForAssertion(() => Assert.Equal(80, _popovers.FindAll(".mud-ex-slide-table-picker [data-rows]").Count));
        _popovers.Find(".mud-ex-slide-table-picker [data-rows='3'][data-columns='4']").Click();

        var table = Assert.IsType<TableShape>(slide.Shapes[^1]);
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal(4, table.ColumnWidths.Count);
        var width = presentation.SlideWidth * 8 / 10 / 4 * 4;
        Assert.Equal(new ShapeTransform((presentation.SlideWidth - width) / 2, (presentation.SlideHeight - 3 * 370840) / 2, width, 3 * 370840), table.Transform);
        Assert.Equal(TableShape.DefaultStyleId, table.StyleId);
        Assert.Equal([table.Id], cut.Instance.SelectedShapes);
        Assert.Equal("Insert table", presentation.Undo.UndoDescription);
        cut.WaitForAssertion(() => Assert.Equal(12, cut.FindAll($".mud-ex-slide-canvas [data-shape-id='{table.Id}'] [data-cell]").Count));

        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.DoesNotContain(slide.Shapes, s => s is TableShape);
    }

    [Fact]
    public void Table_RendersHeaderWithAccentFill()
    {
        var presentation = TableTests.Deck();
        var cut = RenderEditor(presentation);
        var table = TableTests.Table(presentation);
        var master = presentation.Slides[0].Layout!.Master;

        var styles = SlideTableStyles.For(table, master);
        Assert.Equal(("#4472C4", "#FFFFFF", true), (styles[0][1].Fill, styles[0][1].TextColor, styles[0][1].Bold));
        Assert.Equal((false, "#000000"), (styles[1][1].Bold, styles[1][1].TextColor));
        Assert.Equal(ColorResolver.Resolve("scheme:accent1 tint=40000", master), styles[1][1].Fill);
        Assert.Equal(ColorResolver.Resolve("scheme:accent1 tint=20000", master), styles[2][1].Fill);
        Assert.Equal("#FF0000", styles[3][2].Fill);

        string Cell(int row, int column) => $".mud-ex-slide-canvas [data-shape-id='3'] [data-cell='{row},{column}']";
        Assert.Contains("background-color:#4472C4", cut.Find(Cell(0, 1)).GetAttribute("style"));
        var header = cut.Find(Cell(0, 1) + " .mud-ex-slide-paragraph span").GetAttribute("style")!;
        Assert.Contains("color:#FFFFFF", header);
        Assert.Contains("font-weight:bold", header);
        Assert.DoesNotContain("font-weight:bold", cut.Find(Cell(1, 1) + " .mud-ex-slide-paragraph span").GetAttribute("style"));
        Assert.Contains("background-color:#FF0000", cut.Find(Cell(3, 2)).GetAttribute("style"));
        Assert.Contains("grid-row:1 / span 3", cut.Find(Cell(0, 0)).GetAttribute("style"));
        Assert.Empty(cut.FindAll(Cell(1, 0)));
        Assert.Equal("B", cut.Find(Cell(0, 1)).TextContent.Trim());
        Assert.Empty(cut.FindAll(".mud-ex-slide-canvas [data-shape-id='3'].mud-ex-slide-opaque"));

        table.StyleId = "{93296810-A885-4BE3-A3E7-6D5BEEA58F35}";
        Assert.Equal(ColorResolver.Resolve("scheme:accent6", master), SlideTableStyles.For(table, master)[0][1].Fill);
    }

    [Fact]
    public async Task Table_TabInLastCell_AddsRow()
    {
        var presentation = TableTests.Deck();
        var table = TableTests.Table(presentation);
        var cut = RenderEditor(presentation);

        await SelectCellsAsync(cut, 0, 0, 0, 0);
        await Key(cut, "Tab");
        Assert.Equal((0, 1, 1, 1), Canvas(cut).SelectedCells);
        await Key(cut, "Tab", shift: true);
        Assert.Equal((0, 0, 3, 1), Canvas(cut).SelectedCells);
        Assert.False(presentation.Undo.CanUndo);

        await cut.InvokeAsync(() => Canvas(cut).OnCellSelectAsync(3, 3, 2, 3, 2));
        await Key(cut, "Tab");
        Assert.Equal(5, table.Rows.Count);
        Assert.Equal("Insert rows", presentation.Undo.UndoDescription);
        Assert.Equal((4, 0, 1, 1), Canvas(cut).SelectedCells);
        Assert.Equal([3u], cut.Instance.SelectedShapes);

        await Key(cut, "F2");
        var editor = cut.FindComponent<MudExSlideTextEditor>().Instance;
        await cut.InvokeAsync(() => editor.OnTextTabAsync(Dom("new"), false));
        Assert.Equal("new", TableTests.Text(table.Rows[4].Cells[0]));
        Assert.Equal((4, 1, 1, 1), Canvas(cut).SelectedCells);
        cut.WaitForAssertion(() => Assert.True(Canvas(cut).IsEditingText));
        Assert.NotSame(editor, cut.FindComponent<MudExSlideTextEditor>().Instance);
    }

    [Fact]
    public async Task Table_CellEdit_IsOneUndoStep()
    {
        var presentation = TableTests.Deck();
        var table = TableTests.Table(presentation);
        var before = table.Rows[0].Cells[1].Text;
        var cut = RenderEditor(presentation);

        await SelectCellsAsync(cut, 0, 1, 0, 1);
        await Key(cut, "x");
        var editor = cut.FindComponent<MudExSlideTextEditor>();
        Assert.Equal("x", editor.Instance.Text);
        Assert.Contains("color:#FFFFFF", SlideTextDom.ToDom(editor.Instance.Shape, presentation)[0].Runs[0].Style);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-slide-canvas [data-cell='0,1'] .mud-ex-slide-text")));
        await Key(cut, "y");
        Assert.Same(editor.Instance, cut.FindComponent<MudExSlideTextEditor>().Instance);

        await cut.InvokeAsync(() => editor.Instance.OnTextInputAsync(Dom("xy")));
        Assert.Same(before, table.Rows[0].Cells[1].Text);
        var saved = await cut.InvokeAsync(() => cut.Instance.ExportAsync());

        Assert.Equal("xy", TableTests.Text(table.Rows[0].Cells[1]));
        Assert.Equal("xy", TableTests.Text(TableTests.Table(Presentation.Load(saved.Stream)).Rows[0].Cells[1]));
        Assert.Equal("Edit text", presentation.Undo.UndoDescription);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindComponents<MudExSlideTextEditor>()));
        await cut.InvokeAsync(() => presentation.Undo.Undo());
        Assert.Same(before, table.Rows[0].Cells[1].Text);
        Assert.False(presentation.Undo.CanUndo);

        await cut.InvokeAsync(() => Canvas(cut).OnCellSelectAsync(3, 2, 2, 2, 2));
        await Key(cut, "F2");
        editor = cut.FindComponent<MudExSlideTextEditor>();
        await cut.InvokeAsync(() => editor.Instance.OnTextCommitAsync(Dom("F")));
        Assert.False(presentation.Undo.CanUndo);

        await cut.InvokeAsync(() => Canvas(cut).OnCellSelectAsync(3, 2, 1, 2, 2));
        await Run(cut, "pp-bold");
        Assert.True(table.Rows[2].Cells[1].Text.Paragraphs[0].Runs[0].Format.Bold);
        Assert.True(table.Rows[2].Cells[2].Text.Paragraphs[0].Runs[0].Format.Bold);
        Assert.Null(table.Rows[3].Cells[1].Text.Paragraphs[0].Runs[0].Format.Bold);
        Assert.Equal("Format text", presentation.Undo.UndoDescription);
    }

    [Fact]
    public async Task Table_ContextTab_OnlyWhenTableSelected()
    {
        var presentation = TableTests.Deck();
        var cut = RenderEditor(presentation);
        bool Visible() => cut.FindComponent<MudExRibbon>().Instance.Tabs.Single(t => t.Text == "Table").Visible;

        Assert.False(Visible());
        Assert.True(cut.FindComponent<MudExRibbon>().Instance.Tabs.Single(t => t.Text == "Table").Contextual);
        await cut.InvokeAsync(() => Canvas(cut).OnSelectionAsync([2]));
        Assert.False(Visible());
        await cut.InvokeAsync(() => Canvas(cut).OnSelectionAsync([3]));
        cut.WaitForAssertion(() => Assert.True(Visible()));
        Assert.Contains(cut.FindAll("[role='tab']"), t => t.TextContent.Contains("Table"));
        await cut.InvokeAsync(() => Canvas(cut).OnSelectionAsync([2, 3]));
        cut.WaitForAssertion(() => Assert.False(Visible()));
    }

    [Fact]
    public async Task Table_MergeFromRibbon_UsesSelectedRange()
    {
        var presentation = TableTests.Deck();
        var table = TableTests.Table(presentation);
        var cut = RenderEditor(presentation);
        bool Disabled(string id) => cut.FindComponent<MudExRibbon>().Instance.Commands.Single(c => c.Id == id).Disabled;

        await SelectCellsAsync(cut, 1, 1, 1, 1);
        Assert.Equal((1, 1, 1, 2), Canvas(cut).SelectedCells);
        cut.WaitForAssertion(() => Assert.True(Disabled("pp-table-merge")));
        Assert.False(Disabled("pp-table-split"));

        await cut.InvokeAsync(() => Canvas(cut).OnCellSelectAsync(3, 1, 1, 2, 2));
        cut.WaitForAssertion(() => Assert.False(Disabled("pp-table-merge")));
        await Run(cut, "pp-table-merge");

        Assert.Equal("A[3x1] B C | _ D/E/F[2x2] _ | _ _ _ | G H I", TableTests.Grid(table));
        Assert.Equal("Merge cells", presentation.Undo.UndoDescription);
        Assert.Equal((1, 1, 2, 2), Canvas(cut).SelectedCells);

        await Run(cut, "pp-table-split");
        Assert.Equal("A[3x1] B C | _ D/E/F · | _ · · | G H I", TableTests.Grid(table));

        await cut.InvokeAsync(() => Canvas(cut).OnCellSelectAsync(3, 2, 0, 2, 1));
        Assert.Equal((0, 0, 3, 2), Canvas(cut).SelectedCells);
        await Run(cut, "pp-table-insert-below");
        Assert.Equal(7, table.Rows.Count);
        Assert.Equal("", TableTests.Text(table.Rows[3].Cells[1]));
        await Run(cut, "pp-table-delete-columns");
        Assert.Single(table.ColumnWidths);
    }
}
