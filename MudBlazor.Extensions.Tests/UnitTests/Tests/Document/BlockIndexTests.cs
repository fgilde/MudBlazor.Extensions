using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class BlockIndexTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Typing_InLargeDocument_BuildsIndexOnce()
    {
        var document = TextDocument.FromBlocks(Enumerable.Range(0, 10_000).Select(i => P("p" + i)));
        var id = document.Blocks[5000].Id;
        document.FindBlock(id);
        var builds = document.IndexBuilds;

        for (var i = 0; i < 50; i++) document.ApplyTyping(id, [new TextRun("p5000" + new string('x', i + 1))], T0.AddMilliseconds(i * 10));

        Assert.Equal(builds, document.IndexBuilds);
        Assert.Same(document.Blocks[5000], document.FindBlock(id));
        Assert.Equal("p5000" + new string('x', 50), ((Paragraph)document.Blocks[5000]).Text);
    }

    [Fact]
    public void TypingInNestedCell_UpdatesNestedEntries_NoRebuild()
    {
        var document = TextDocument.FromBlocks([P("before"), NestedTable(), P("after")]);
        var nested = document.AllParagraphs().Single(p => p.Text == "inner b");
        var path = document.PositionOf(nested.Id, 3).Path;
        var builds = document.IndexBuilds;

        for (var i = 0; i < 20; i++) document.ApplyTyping(nested.Id, [new TextRun("inner b" + new string('x', i + 1))], T0.AddMilliseconds(i * 10));

        Assert.Equal(builds, document.IndexBuilds);
        var current = document.AllParagraphs().Single(p => p.Id == nested.Id);
        Assert.NotSame(nested, current);
        Assert.Same(current, document.FindBlock(nested.Id));
        Assert.Equal(path.ToArray(), document.PositionOf(nested.Id, 3).Path.ToArray());
    }

    [Fact]
    public void ReplaceAll_20000Matches_BuildsIndexAtMostTwice()
    {
        var document = TextDocument.FromBlocks(Enumerable.Range(0, 10_000).Select(_ => P("a a")));
        var before = document.IndexBuilds;

        Assert.Equal(20_000, DocumentSearch.ReplaceAll(document, "a", "c"));

        Assert.InRange(document.IndexBuilds - before, 0, 2);
        Assert.All(document.Blocks, b => Assert.Equal("c c", ((Paragraph)b).Text));
        before = document.IndexBuilds;
        document.Undo.Undo();
        Assert.InRange(document.IndexBuilds - before, 0, 2);
        Assert.All(document.Blocks, b => Assert.Equal("a a", ((Paragraph)b).Text));
    }

    [Fact]
    public void Split_StaysConsistent()
    {
        var document = TextDocument.FromBlocks(Enumerable.Range(0, 100).Select(i => P("p" + i)));
        var oldId = document.Blocks[50].Id;
        document.FindBlock(oldId);

        document.Execute(new SplitParagraphCommand(document.PositionOf(oldId, 1)));

        var newId = document.Blocks[51].Id;
        Assert.NotEqual(oldId, newId);
        Assert.All(document.AllParagraphs(true), p => Assert.Same(p, document.FindBlock(p.Id)));
        Assert.Equal(document.BlockIndex(oldId) + 1, document.BlockIndex(newId));
        Assert.Equal(51, document.BlockIndex(newId));
    }

    [Fact]
    public void MergeCells_SameIdsNewPaths_IsReindexed()
    {
        var document = TextDocument.FromBlocks([T(Row([P("a")], [P("b")]), Row([P("c")], [P("d")])), P("")]);
        var table = (Table)document.Blocks[0];
        var movedId = table.Rows[0].Cells[1].Blocks[0].Id;
        Assert.Equal("b", document.Resolve(document.PositionOf(movedId, 0)).Paragraph.Text);

        document.Execute(new MergeTableCellsCommand(table.Id, 0, 0, 0, 1));

        var position = document.PositionOf(movedId, 0);
        Assert.Equal(new[] { new CellStep(0, 0, 1) }, position.Path.ToArray());
        var (paragraph, _) = document.Resolve(position);
        Assert.Equal(movedId, paragraph.Id);
        Assert.Equal("b", paragraph.Text);
    }

    [Fact]
    public void BlockIndex_TopNestedUnknownHeader()
    {
        var document = TextDocument.FromBlocks([P("a"), NestedTable(), P("b")]);
        document.HeaderFooterList.Add(new HeaderFooter { Id = "h1", Kind = HeaderFooterKind.Header, Blocks = [document.Identify(P("h0")), document.Identify(P("h1"))] });
        document.InvalidateIndex();
        var nested = document.AllParagraphs().Single(p => p.Text == "inner a");

        Assert.Equal(0, document.BlockIndex(document.Blocks[0].Id));
        Assert.Equal(1, document.BlockIndex(document.Blocks[1].Id));
        Assert.Equal(2, document.BlockIndex(document.Blocks[2].Id));
        Assert.Equal(-1, document.BlockIndex(nested.Id));
        Assert.Equal(-1, document.BlockIndex("nope"));
        Assert.Equal(-1, document.BlockIndex(null));
        Assert.Equal(1, document.BlockIndex(document.HeadersFooters[0].Blocks[1].Id));
    }

    private static Paragraph P(string text) => new() { Inlines = [new TextRun(text)] };

    private static Table T(params TableRow[] rows) => new() { Grid = [2000, 2000], Rows = rows };

    private static TableRow Row(params Block[][] cells) => new(cells.Select(c => new TableCell(c)).ToList());

    private static Table NestedTable() =>
        T(Row([P("a")], [P("b")]),
          Row([P("outer"), T(Row([P("inner a")], [P("inner b")]))], [P("c")]));
}
