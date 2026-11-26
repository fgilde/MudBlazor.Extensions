using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Document;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentEditorObjectsTests : DocumentEditorFormattingTestBase
{
    private static string CellId(TextDocument document, int row, int cell) => ((Paragraph)document.Blocks.OfType<Table>().Single().Rows[row].Cells[cell].Blocks[0]).Id;

    private static Table TableOf(TextDocument document) => document.Blocks.OfType<Table>().Single();

    private static bool TabVisible(IRenderedComponent<MudExDocumentEditor> cut, string text) =>
        cut.FindComponents<MudExRibbonTab>().Single(t => t.Instance.Contextual && t.Instance.Text == text).Instance.Visible;

    private async Task<(TextDocument Document, IRenderedComponent<MudExDocumentEditor> Cut)> TableEditor()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 5, 5);
        await cut.InvokeAsync(() => cut.Instance.InsertTableAsync((2, 2)));
        var id = CellId(document, 0, 0);
        await Select(cut, id, 0, id, 0);
        return (document, cut);
    }

    [Fact]
    public async Task TableTab_FollowsSelection_AndEditsRowsColumnsMergeBordersShadingHeader()
    {
        var (document, cut) = await TableEditor();
        cut.WaitForAssertion(() => Assert.True(TabVisible(cut, "Table")));

        await cut.InvokeAsync(() => cut.Instance.InsertRowAsync(true));
        await cut.InvokeAsync(() => cut.Instance.InsertColumnAsync(true));
        Assert.Equal((3, 3), (TableOf(document).Rows.Count, TableOf(document).Grid.Count));

        await Select(cut, CellId(document, 0, 0), 0, CellId(document, 0, 1), 0);
        await cut.InvokeAsync(() => cut.Instance.MergeCellsAsync());
        Assert.Equal(2, TableOf(document).Rows[0].Cells.Count);
        Assert.Equal(2, TableOf(document).Rows[0].Cells[0].GridSpan);
        await cut.InvokeAsync(() => cut.Instance.SetShadingAsync("#FF0000"));
        Assert.Equal("#FF0000", TableOf(document).Rows[0].Cells[0].Shading, true);
        await cut.InvokeAsync(() => cut.Instance.SetBordersAsync("none"));
        Assert.Equal("nil", TableOf(document).Format.Borders.Top.Style);
        await cut.InvokeAsync(() => cut.Instance.ToggleHeaderRowAsync());
        Assert.True(TableOf(document).Rows[0].IsHeader);

        await cut.InvokeAsync(() => cut.Instance.SplitCellAsync());
        Assert.Equal(3, TableOf(document).Rows[0].Cells.Count);
        await cut.InvokeAsync(() => cut.Instance.DeleteRowsAsync());
        Assert.Equal(2, TableOf(document).Rows.Count);
        await cut.InvokeAsync(() => cut.Instance.DeleteTableAsync());
        Assert.Empty(document.Blocks.OfType<Table>());
    }

    [Fact]
    public async Task Tab_MovesThroughCells_AndAddsRowInLastCell()
    {
        var (document, cut) = await TableEditor();
        var surface = Surface(cut);
        async Task Press(string key, string id) => await cut.InvokeAsync(() => surface.OnCommandAsync(
            new SurfaceSync(Seen, [], new SurfaceSelection(new SurfacePoint(id, 0), new SurfacePoint(id, 0)), false), null, key, null));

        await Press("tab", CellId(document, 0, 0));
        Assert.Equal(CellId(document, 0, 1), document.Resolve(cut.Instance.Selection.Start).Paragraph.Id);
        await Press("untab", CellId(document, 0, 1));
        Assert.Equal(CellId(document, 0, 0), document.Resolve(cut.Instance.Selection.Start).Paragraph.Id);
        await Press("tab", CellId(document, 1, 1));
        Assert.Equal(3, TableOf(document).Rows.Count);
        Assert.Equal(CellId(document, 2, 0), document.Resolve(cut.Instance.Selection.Start).Paragraph.Id);
    }

    [Fact]
    public async Task ImageSizeAndColumnWidthsActions_BuildOneCommandEach()
    {
        var (document, cut) = await TableEditor();
        var table = TableOf(document);
        var command = DocumentEditActions.For(document, "columnWidths", cut.Instance.Selection, $"{table.Id};100;200");
        Assert.NotNull(command);
        document.Execute(command);
        Assert.Equal([1500, 3000], TableOf(document).Grid);
        Assert.Null(DocumentEditActions.For(document, "columnWidths", cut.Instance.Selection, $"{table.Id};100"));
        Assert.Null(DocumentEditActions.For(document, "imageSize", cut.Instance.Selection, "10,10"));
    }

    [Fact]
    public async Task PictureTab_ResizesWithLockedRatio_ResetsAndSetsAltText()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        await Select(cut, 0, 5, 5);
        await cut.InvokeAsync(() => cut.Instance.InsertPictureAsync(ObjectCommandTests.Png(40, 20), "image/png"));
        cut.WaitForAssertion(() => Assert.False(TabVisible(cut, "Picture")));
        var id = P(document).Id;
        await Select(cut, id, 5, id, 6);
        cut.WaitForAssertion(() => Assert.True(TabVisible(cut, "Picture")));
        var original = (InlineImage)P(document).Inlines[^1];

        await cut.InvokeAsync(() => cut.Instance.SetPictureSizeAsync(2.54, null));
        var resized = (InlineImage)P(document).Inlines[^1];
        Assert.Equal(2, resized.WidthEmu * 1.0 / resized.HeightEmu, 2);
        Assert.NotEqual(original.WidthEmu, resized.WidthEmu);

        await cut.InvokeAsync(() => cut.Instance.ResetPictureSizeAsync());
        Assert.Equal(original.WidthEmu, ((InlineImage)P(document).Inlines[^1]).WidthEmu);
        await cut.InvokeAsync(() => cut.Instance.SetAltTextAsync("a cat"));
        Assert.Equal("a cat", ((InlineImage)P(document).Inlines[^1]).AltText);

        var command = DocumentEditActions.For(document, "imageSize", cut.Instance.Selection, "80,40");
        document.Execute(command);
        Assert.Equal(80 * 9525, ((InlineImage)P(document).Inlines[^1]).WidthEmu);
    }

    [Fact]
    public async Task ContextMenu_ShowsTableItemsInTable_AndMiniToolbarFormats()
    {
        var (document, cut) = await TableEditor();
        await cut.InvokeAsync(() => Surface(cut).OnContextMenuAsync(10, 20));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#wd-menu-row-below")));
        cut.Find("#wd-menu-row-below").Click();
        cut.WaitForAssertion(() => Assert.Equal(3, TableOf(document).Rows.Count));
        Assert.Empty(cut.FindAll(".mud-ex-doc-menu"));

        var id = CellId(document, 0, 0);
        document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 0)), "word"));
        await Select(cut, id, 0, id, 4);
        await cut.InvokeAsync(() => Surface(cut).OnMiniToolbarAsync(true, 50, 60));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#wd-bold-mini")));
        cut.Find("#wd-bold-mini").Click();
        cut.WaitForAssertion(() => Assert.True(document.EffectiveRunFormat((Paragraph)document.FindBlock(id), InlineEdit.At((Paragraph)document.FindBlock(id), 0)).Bold));
        await cut.InvokeAsync(() => Surface(cut).OnMiniToolbarAsync(false, 0, 0));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-doc-mini")));
    }
}
