using System.Globalization;
using MudBlazor.Extensions.Components;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Components.DocumentSurfaceRenderTests;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentSurfacePatchTests
{
    [Fact]
    public void Patch_Typing_InPlaceUpsertOfThatParagraphOnly()
    {
        var document = Paragraphs("a", "b", "c");
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);
        var id = document.Blocks[1].Id;

        document.ApplyTyping([new TypedParagraph(id, "bx", 2)], T0);
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 5);

        Assert.Equal(5, patch.Seq);
        Assert.False(patch.Reset);
        Assert.Null(patch.SheetCss);
        Assert.Empty(patch.Remove);
        var upsert = Assert.Single(patch.Upsert);
        Assert.True(upsert.InPlace);
        Assert.Equal((id, "bx"), (upsert.Block.Id, upsert.Block.Text));
    }

    [Fact]
    public void Patch_Split_InsertsAfterThePreviousBlock()
    {
        var document = Paragraphs("a", "bc", "d");
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);
        var (first, second) = (document.Blocks[0].Id, document.Blocks[1].Id);

        document.Execute(new SplitParagraphCommand(document.PositionOf(second, 1)));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        Assert.Equal(2, patch.Upsert.Count);
        Assert.All(patch.Upsert, u => Assert.False(u.InPlace));
        Assert.Equal((second, first), (patch.Upsert[0].Block.Id, patch.Upsert[0].After));
        Assert.Equal((document.Blocks[2].Id, second), (patch.Upsert[1].Block.Id, patch.Upsert[1].After));
        Assert.Equal("c", patch.Upsert[1].Block.Text);
    }

    [Fact]
    public void Patch_TypingInCell_UpsertsTheNestedParagraphOnly()
    {
        var document = WithTable();
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);
        var cellParagraph = Cell(document, 0, 1);

        document.ApplyTyping([new TypedParagraph(cellParagraph, "x", 1)], T0);
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        var upsert = Assert.Single(patch.Upsert);
        Assert.True(upsert.InPlace);
        Assert.Equal(("p", cellParagraph, "x"), (upsert.Block.K, upsert.Block.Id, upsert.Block.Text));
    }

    [Fact]
    public void Patch_CellShadingChanged_UpsertsTheWholeTable()
    {
        var document = WithTable();
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);
        var table = document.Blocks.OfType<Table>().Single();

        document.Execute(new SetTableCellFormatCommand(table.Id, [(0, 0)], c => c with { Shading = "#FFFF00" }, "Shading"));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        var upsert = Assert.Single(patch.Upsert);
        Assert.Equal(("t", table.Id), (upsert.Block.K, upsert.Block.Id));
        Assert.Contains("background-color:#FFFF00", upsert.Block.Rows[0].Cells[0].Css);
    }

    [Fact]
    public void Patch_RowInserted_UpsertsTheWholeTable()
    {
        var document = WithTable();
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);
        var table = document.Blocks.OfType<Table>().Single();
        var index = document.BlockIndex(table.Id);

        document.Execute(new InsertTableRowsCommand(table.Id, 1, 1));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        var upsert = Assert.Single(patch.Upsert);
        Assert.Equal(("t", table.Id, document.Blocks[index - 1].Id), (upsert.Block.K, upsert.Block.Id, upsert.After));
        Assert.Equal(3, upsert.Block.Rows.Count);
        var ids = DocumentSurfacePatches.Ids(patch).ToList();
        Assert.Contains(table.Id, ids);
        Assert.Contains(Cell(document, 1, 0), ids);
        Assert.Equal(1 + 6, ids.Count);
    }

    [Fact]
    public void Patch_ListItemInserted_RefreshesFollowingLabels()
    {
        var document = Paragraphs("1", "2", "3", "4", "5");
        document.Execute(new ApplyListCommand(document.Blocks.Select(b => b.Id).ToList(), ListTemplate.Decimal));
        var render = new DocumentSurfaceRender(document);
        var seq = 0;
        DocumentSurfacePatches.Initial(document, render, () => ++seq);
        var events = Record(document);

        document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[1].Id, 1)));
        var patch = DocumentSurfacePatches.Change(document, render, events[0], ++seq);

        var inserted = Assert.Single(patch.Upsert, u => !u.InPlace);
        Assert.Equal((document.Blocks[2].Id, document.Blocks[1].Id, "3."), (inserted.Block.Id, inserted.After, inserted.Block.Label));
        Assert.Equal(["4.", "5.", "6."], patch.Upsert.Where(u => u.InPlace).Select(u => u.Block.Label));

        document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[0].Id, 1)));
        var next = DocumentSurfacePatches.Change(document, render, events[1], ++seq);

        Assert.Equal(["3.", "4.", "5.", "6.", "7."], next.Upsert.Where(u => u.InPlace).Select(u => u.Block.Label));
    }

    [Fact]
    public void Patch_StyleChanged_IsReset()
    {
        var document = Paragraphs("a", "b");
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);

        document.Execute(new SetDefaultFontCommand("Arial", 12));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 3);

        Assert.True(patch.Reset);
        Assert.Contains("'Arial','Liberation Sans',sans-serif", patch.SheetCss);
        Assert.Equal(document.Blocks.Select(b => b.Id), patch.Upsert.Select(u => u.Block.Id));
        Assert.Null(patch.Upsert[0].After);
        Assert.Equal(document.Blocks[0].Id, patch.Upsert[1].After);
    }

    [Fact]
    public void Patches_StyleResetWithRemoval_IsChunkedAndIdsListTheRemoved()
    {
        var document = Paragraphs("a", "b", "c");
        var render = new DocumentSurfaceRender(document);
        var seq = 0;
        DocumentSurfacePatches.Initial(document, render, () => ++seq);
        var events = Record(document);
        var removed = document.Blocks[1].Id;

        using (document.Undo.BeginTransaction("Both"))
        {
            document.Execute(new MergeParagraphsCommand(document.Blocks[0].Id));
            document.Execute(new SetDefaultFontCommand("Arial", 12));
        }

        var patches = DocumentSurfacePatches.Changes(document, render, events[0], () => ++seq, 1);

        Assert.Equal([2, 3], patches.Select(p => p.Seq));
        Assert.Equal([true, false], patches.Select(p => p.Reset));
        Assert.NotNull(patches[0].SheetCss);
        Assert.Null(patches[1].SheetCss);
        Assert.All(patches, p => Assert.Empty(p.Remove));
        Assert.Equal(document.Blocks.Select(b => b.Id), patches.SelectMany(p => p.Upsert).Select(u => u.Block.Id));
        Assert.Contains(removed, DocumentSurfacePatches.Ids(patches[0], events[0]));
        Assert.DoesNotContain(removed, DocumentSurfacePatches.Ids(patches[0]));
    }

    [Fact]
    public void Patch_InsertAtIndexZero_AfterIsNull()
    {
        var document = Created("a", "b");
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);

        document.Execute(new InsertTableCommand(document.PositionOf(document.Blocks[0].Id, 0), 1, 1));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        var table = Assert.Single(patch.Upsert, u => u.Block.K == "t");
        Assert.Null(table.After);
        Assert.Equal(document.Blocks[0].Id, table.Block.Id);
    }

    [Fact]
    public void Patch_TableRemoved_IdsIncludeItsParagraphs()
    {
        var document = WithTable();
        var render = new DocumentSurfaceRender(document);
        var seq = 0;
        DocumentSurfacePatches.Initial(document, render, () => ++seq);
        var events = Record(document);
        var table = document.Blocks.OfType<Table>().Single().Id;
        var nested = Enumerable.Range(0, 2).SelectMany(r => Enumerable.Range(0, 2).Select(c => Cell(document, r, c))).ToList();

        document.Execute(new DeleteTableCommand(table));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), ++seq);

        Assert.Contains(table, patch.Remove);
        Assert.All(nested.Append(table), id => Assert.Contains(id, DocumentSurfacePatches.Ids(patch, events[0])));
    }

    [Fact]
    public void Patch_ParagraphReplacedByTable_AppliesInOrder()
    {
        var document = Created("a", "", "c");
        var render = new DocumentSurfaceRender(document);
        var seq = 0;
        DocumentSurfacePatches.Initial(document, render, () => ++seq);
        var before = document.Blocks.Select(b => b.Id).ToList();
        var events = Record(document);
        var removed = document.Blocks[2].Id;

        using (document.Undo.BeginTransaction("Replace"))
        {
            document.Execute(new InsertTableCommand(document.PositionOf(document.Blocks[1].Id, 0), 1, 1));
            document.Execute(new MergeParagraphsCommand(document.Blocks[2].Id));
        }

        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), ++seq);

        Assert.Equal([removed], patch.Remove);
        Assert.Equal(document.Blocks.Select(b => b.Id), AppliedOrder(before, patch));
    }

    [Fact]
    public void Patch_ThreeBlocksInserted_EachAfterIsPresentWhenApplied()
    {
        var document = Created("a", "bc", "d");
        var render = new DocumentSurfaceRender(document);
        var before = document.Blocks.Select(b => b.Id).ToList();
        var events = Record(document);

        document.Execute(new InsertTableCommand(document.PositionOf(document.Blocks[1].Id, 1), 1, 1));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        Assert.Equal(3, patch.Upsert.Count);
        Assert.Equal(document.Blocks.Select(b => b.Id), AppliedOrder(before, patch));
    }

    [Fact]
    public void Patch_TypingInNestedTable_UpsertsThatParagraphOnly()
    {
        var inner = new Table { Grid = [1000], Rows = [new TableRow([new TableCell([new Paragraph { Inlines = [new TextRun("deep")] }])])] };
        var document = TextDocument.FromBlocks([new Table { Grid = [2000], Rows = [new TableRow([new TableCell([inner, new Paragraph()])])] }]);
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);
        var deep = ((Table)((Table)document.Blocks[0]).Rows[0].Cells[0].Blocks[0]).Rows[0].Cells[0].Blocks[0].Id;

        document.ApplyTyping([new TypedParagraph(deep, "deeper", 6)], T0);
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        var upsert = Assert.Single(patch.Upsert);
        Assert.True(upsert.InPlace);
        Assert.Equal((deep, "deeper"), (upsert.Block.Id, upsert.Block.Text));
    }

    [Fact]
    public void Patch_PageSetup_SendsSheetCssOnly()
    {
        var document = TextDocument.Create();
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);

        document.Execute(new SetPageSetupCommand(document.Sections[0].Id, PageSetup.Letter));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        Assert.Contains("width:612pt", patch.SheetCss);
        Assert.False(patch.Reset);
        Assert.Empty(patch.Upsert);
    }

    [Fact]
    public void Patch_HeaderChanged_ProducesNothing()
    {
        var document = TextDocument.Create();
        document.Execute(new AddHeaderFooterCommand(document.Sections[0].Id, HeaderFooterKind.Header, HeaderFooterType.Default));
        var header = document.HeadersFooters[0].Blocks[0].Id;
        var render = new DocumentSurfaceRender(document);
        var seq = 0;
        DocumentSurfacePatches.Initial(document, render, () => ++seq);
        var events = Record(document);

        document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(header, 0), document.PositionOf(header, 0)), "head"));
        document.Execute(new SplitParagraphCommand(document.PositionOf(header, 2)));
        document.Execute(new MergeParagraphsCommand(header));

        Assert.Equal(3, events.Count);
        Assert.NotEmpty(events[2].RemovedBlockIds);
        foreach (var change in events)
        {
            var patch = Assert.Single(DocumentSurfacePatches.Changes(document, render, change, () => ++seq));
            Assert.Empty(patch.Upsert);
            Assert.Empty(patch.Remove);
            Assert.Null(patch.SheetCss);
        }
    }

    [Fact]
    public void Refresh_UpsertsKnownParagraphsInPlace()
    {
        var document = WithTable();
        var render = new DocumentSurfaceRender(document);
        var cellParagraph = Cell(document, 1, 1);

        var patch = DocumentSurfacePatches.Refresh(document, render, [cellParagraph, "unknown", document.Blocks.OfType<Table>().Single().Id], 4);

        Assert.Equal(4, patch.Seq);
        var upsert = Assert.Single(patch.Upsert);
        Assert.True(upsert.InPlace);
        Assert.Equal(cellParagraph, upsert.Block.Id);
    }

    [Fact]
    public void Initial_ChunksOf500_FirstIsReset()
    {
        var document = TextDocument.FromBlocks(Enumerable.Range(0, 1200).Select(i => new Paragraph { Inlines = [new TextRun(i.ToString(CultureInfo.InvariantCulture))] }));
        var seq = 0;

        var patches = DocumentSurfacePatches.Initial(document, new DocumentSurfaceRender(document), () => ++seq);

        Assert.Equal([1, 2, 3], patches.Select(p => p.Seq));
        Assert.Equal([true, false, false], patches.Select(p => p.Reset));
        Assert.NotNull(patches[0].SheetCss);
        Assert.Null(patches[1].SheetCss);
        Assert.Equal([500, 500, 200], patches.Select(p => p.Upsert.Count));
        Assert.Null(patches[0].Upsert[0].After);
        Assert.Equal(patches[0].Upsert[^1].Block.Id, patches[1].Upsert[0].After);
        Assert.Equal(document.Blocks.Select(b => b.Id), patches.SelectMany(p => p.Upsert).Select(u => u.Block.Id));
    }
}
