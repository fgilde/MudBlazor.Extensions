using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class DocumentModelTests
{
    [Fact]
    public void ListLabels_KeptWhileTyping_RecomputedAfterFormatOrStructureChanges()
    {
        var document = TextFormat.FromText("a\nb\nc");
        var ids = document.Blocks.Select(b => b.Id).ToList();
        document.Execute(new ApplyListCommand(ids, ListTemplate.Decimal));
        Assert.Equal("2.", document.ListLabel((Paragraph)document.Blocks[1]));
        var computed = document.LabelComputations;

        for (var i = 0; i < 20; i++)
        {
            var at = document.PositionOf(ids[1], 1);
            document.Execute(new ReplaceTextCommand(new DocumentRange(at, at), "x"));
            Assert.Equal("2.", document.ListLabel((Paragraph)document.Blocks[1]));
        }

        Assert.Equal(computed, document.LabelComputations);
        document.Execute(new SplitParagraphCommand(document.PositionOf(ids[0], 1)));
        Assert.Equal("3.", document.ListLabel((Paragraph)document.Blocks[2]));
        Assert.Equal(computed + 1, document.LabelComputations);
        document.Execute(new RemoveListCommand([ids[0]]));
        Assert.Equal("1.", document.ListLabel((Paragraph)document.Blocks[1]));
        Assert.Equal(computed + 2, document.LabelComputations);
    }

    [Fact]
    public void LargeDeletionsPastesAndTables_BuildTheBlockIndexAConstantNumberOfTimes()
    {
        var document = TextFormat.FromText(string.Join("\n", Enumerable.Range(0, 10_000).Select(i => "line " + i)));
        DocumentPosition At(int block, int offset = 0) => document.PositionOf(document.Blocks[block].Id, offset);
        int Builds(Action action)
        {
            var before = document.IndexBuilds;
            action();
            return document.IndexBuilds - before;
        }

        Assert.InRange(Builds(() => document.Execute(new ReplaceTextCommand(new DocumentRange(At(1000), At(6000)), ""))), 0, 6);
        Assert.Equal(5000, document.Blocks.Count);
        Assert.InRange(Builds(() => document.Undo.Undo()), 0, 6);
        Assert.Equal(10_000, document.Blocks.Count);

        var paste = TextFormat.FromText(string.Join("\n", Enumerable.Range(0, 5_000).Select(i => "pasted " + i)));
        Assert.InRange(Builds(() => document.Execute(new InsertDocumentCommand(new DocumentRange(At(10), At(10)), paste))), 0, 6);
        Assert.Equal(14_999, document.Blocks.Count);
        Assert.InRange(Builds(() => document.Undo.Undo()), 0, 6);

        document.Execute(new InsertTableCommand(At(0), 200, 20));
        var table = document.Blocks.OfType<Table>().Single();
        Assert.InRange(Builds(() => document.Execute(new DeleteTableCommand(table.Id))), 0, 6);
        Assert.Empty(document.Blocks.OfType<Table>());
    }

    [Fact]
    public void Paragraph_Text_MapsTabsBreaksTokensAndSkipsMarkers()
    {
        var paragraph = new Paragraph
        {
            Inlines =
            [
                new TextRun("a"), new Tab(), new TextRun("b"), new Break(BreakKind.Line), new TextRun("c"), new Break(BreakKind.Page),
                new OpaqueInline("field", "1"), new Marker("bookmarkStart")
            ]
        };

        Assert.Equal("a\tb\vc\f\uFFFC", paragraph.Text);
        Assert.Equal(7, paragraph.Length);
    }

    [Fact]
    public void Resolve_PositionInNestedTableCell()
    {
        var document = TextDocument.FromBlocks([NestedTable()]);
        var outer = document.Blocks[0];
        var inner = (Table)((Table)outer).Rows[1].Cells[0].Blocks[1];

        var (paragraph, offset) = document.Resolve(new DocumentPosition(outer.Id, [new(1, 0, 1), new(0, 1, 0)], 2));

        Assert.Equal("inner b", paragraph.Text);
        Assert.Same(inner.Rows[0].Cells[1].Blocks[0], paragraph);
        Assert.Equal(2, offset);
    }

    [Fact]
    public void PositionOf_ParagraphInCell_RoundTripsThroughResolve()
    {
        var document = TextDocument.FromBlocks([P("before"), NestedTable()]);
        var target = document.AllParagraphs().Single(p => p.Text == "inner b");

        var position = document.PositionOf(target.Id, 3);

        Assert.Equal(new DocumentPosition(document.Blocks[1].Id, [new(1, 0, 1), new(0, 1, 0)], 3), position);
        Assert.Equal((target, 3), document.Resolve(position));
    }

    [Fact]
    public void Ids_UniqueAcrossBodyCellsAndHeaders()
    {
        var document = TextDocument.FromBlocks([P("a"), NestedTable(), P("b")]);
        document.HeaderFooterList.Add(new HeaderFooter { Id = "h1", Kind = HeaderFooterKind.Header, Blocks = [document.Identify(P("header")), document.Identify(NestedTable())] });

        var ids = AllBlocks(document.Blocks).Concat(document.HeadersFooters.SelectMany(h => AllBlocks(h.Blocks))).Select(b => b.Id).ToList();

        Assert.Equal(19, ids.Count);
        Assert.All(ids, id => Assert.Matches("^b[0-9]+$", id));
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(AllBlocks(document.HeadersFooters[0].Blocks), b => Assert.Same(b, document.FindBlock(b.Id)));
        Assert.Same(document.Blocks[1], document.FindBlock(document.Blocks[1].Id));
        Assert.Null(document.FindBlock("missing"));
    }

    [Fact]
    public void TextOf_RangeAcrossParagraphs()
    {
        var document = TextDocument.FromBlocks([P("Hello"), P("World")]);

        var text = document.TextOf(new DocumentRange(document.PositionOf(document.Blocks[0].Id, 2), document.PositionOf(document.Blocks[1].Id, 3)));

        Assert.Equal("llo\nWor", text);
    }

    [Fact]
    public void Compare_DocumentOrder_CellBeforeFollowingParagraph()
    {
        var document = TextDocument.FromBlocks([NestedTable(), P("after")]);
        var cell = document.PositionOf(document.AllParagraphs().Single(p => p.Text == "inner b").Id, 5);
        var after = document.PositionOf(document.Blocks[1].Id, 0);
        var earlierCell = document.PositionOf(document.AllParagraphs().Single(p => p.Text == "outer").Id, 2);

        Assert.True(document.Compare(cell, after) < 0);
        Assert.True(document.Compare(after, cell) > 0);
        Assert.True(document.Compare(earlierCell, cell) < 0);
        Assert.Equal(0, document.Compare(cell, document.PositionOf(document.AllParagraphs().Single(p => p.Text == "inner b").Id, 5)));
    }

    [Fact]
    public void Normalize_BackwardRange_OrdersStartBeforeEnd()
    {
        var document = TextDocument.FromBlocks([P("Hello"), P("World")]);
        var start = document.PositionOf(document.Blocks[0].Id, 4);
        var end = document.PositionOf(document.Blocks[1].Id, 1);

        Assert.Equal(new DocumentRange(start, end), new DocumentRange(end, start).Normalize(document));
        Assert.Equal(new DocumentRange(start, end), new DocumentRange(start, end).Normalize(document));
        Assert.True(new DocumentRange(start, start with { Path = [] }).IsEmpty);
        Assert.False(new DocumentRange(start, end).IsEmpty);
    }

    [Fact]
    public void WordAt_InsideWord_ReturnsWord_AtSpace_ReturnsEmpty()
    {
        var document = TextDocument.FromBlocks([P("Hello World")]);
        var id = document.Blocks[0].Id;

        var word = document.WordAt(document.PositionOf(id, 8));
        var space = document.WordAt(document.PositionOf(id, 5));

        Assert.Equal(new DocumentRange(document.PositionOf(id, 6), document.PositionOf(id, 11)), word);
        Assert.Equal("World", document.TextOf(word));
        Assert.True(space.IsEmpty);
        Assert.Equal(5, space.Start.Offset);
    }

    [Fact]
    public void RunFormat_Over_FillsOnlyNulls()
    {
        var own = new RunFormat { Bold = false, SizePt = 14, Color = "#FF0000" };
        var basis = new RunFormat { Bold = true, Italic = true, SizePt = 11, Font = "Calibri", VerticalAlign = VerticalAlign.Superscript };

        var effective = own.Over(basis);

        Assert.Equal(new RunFormat { Bold = false, Italic = true, SizePt = 14, Font = "Calibri", Color = "#FF0000", VerticalAlign = VerticalAlign.Superscript }, effective);
        Assert.Equal(new ParagraphFormat { StyleId = "Body", SpacingAfter = 120, Line = new LineSpacing(240, LineRule.Auto) },
            new ParagraphFormat { SpacingAfter = 120 }.Over(new ParagraphFormat { StyleId = "Body", SpacingAfter = 0, Line = new LineSpacing(240, LineRule.Auto) }));
    }

    [Theory]
    [InlineData("javascript:alert(1)", null)]
    [InlineData(" javascript:alert(1)", null)]
    [InlineData("file:///c:/windows", null)]
    [InlineData("https://example.com", "https://example.com")]
    [InlineData("http://example.com/a?b=c", "http://example.com/a?b=c")]
    [InlineData("mailto:a@b.c", "mailto:a@b.c")]
    [InlineData("#_Toc1", "#_Toc1")]
    public void Hyperlink_SafeTarget(string target, string? expected)
    {
        Assert.Equal(expected, new Hyperlink(target, null, null).SafeTarget);
        Assert.Equal("#_Toc2", new Hyperlink(null, "_Toc2", null).SafeTarget);
        Assert.Null(new Hyperlink(null, null, null).SafeTarget);
    }

    [Fact]
    public void HeaderFor_InheritsFromPreviousSection()
    {
        var document = TextDocument.FromBlocks([P("one"), P("two"), P("three")]);
        HeaderFooter Part(string id, HeaderFooterKind kind) => new() { Id = id, Kind = kind, Blocks = [document.Identify(P(id))] };
        document.HeaderFooterList.AddRange([Part("h1", HeaderFooterKind.Header), Part("h2", HeaderFooterKind.Header), Part("f1", HeaderFooterKind.Footer)]);
        document.SectionList.AddRange(
        [
            new Section { Id = "s1", Page = PageSetup.A4, LastBlockId = document.Blocks[0].Id, Headers = Refs((HeaderFooterType.Default, "h1")), Footers = Refs((HeaderFooterType.Default, "f1")) },
            new Section { Id = "s2", Page = PageSetup.A4, LastBlockId = document.Blocks[1].Id, Headers = Refs((HeaderFooterType.First, "h2")) },
            new Section { Id = "s3", Page = PageSetup.Letter }
        ]);
        var last = document.Sections[2];

        Assert.Equal("h1", document.HeaderFor(last, HeaderFooterKind.Header, HeaderFooterType.Default).Id);
        Assert.Equal("h2", document.HeaderFor(last, HeaderFooterKind.Header, HeaderFooterType.First).Id);
        Assert.Equal("f1", document.HeaderFor(last, HeaderFooterKind.Footer, HeaderFooterType.Default).Id);
        Assert.Null(document.HeaderFor(last, HeaderFooterKind.Footer, HeaderFooterType.Even));
        Assert.Null(document.HeaderFor(document.Sections[0], HeaderFooterKind.Header, HeaderFooterType.First));
        Assert.Same(document.HeadersFooters[1], document.HeaderFooter("h2"));
        Assert.Equal(11906 - 2 * 1440, PageSetup.A4.TextWidth);
    }

    [Fact]
    public void Execute_NoOpCommand_RecordsNoStep_RaisesNoChanged()
    {
        var document = TextDocument.FromBlocks([P("a")]);
        var raised = 0;
        document.Changed += (_, _) => raised++;

        document.Execute(new DelegateCommand(_ => { }, _ => { }));

        Assert.False(document.Undo.CanUndo);
        Assert.Equal(0, raised);
        Assert.False(document.Modified);
        Assert.Equal(0, document.Version);
    }

    [Fact]
    public void Changed_OncePerTransaction_WithChangedBlockIds()
    {
        var document = TextDocument.FromBlocks([P("first"), NestedTable(), P("last")]);
        var before = DocumentDescriber.Describe(document);
        var firstId = document.Blocks[0].Id;
        var table = document.Blocks[1];
        var cellParagraph = document.AllParagraphs().Single(p => p.Text == "inner b");
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        using (document.Undo.BeginTransaction("Edit"))
        {
            document.Execute(Replace(cellParagraph, cellParagraph with { Inlines = [new TextRun("edited")] }));
            document.Execute(Replace(document.Blocks[0], document.Blocks[0] with { Id = "replacement" }));
            Assert.Empty(events);
        }

        var args = Assert.Single(events);
        Assert.Equal(new[] { table.Id, "replacement" }.Order(), args.ChangedBlockIds.Order());
        Assert.Equal([firstId], args.RemovedBlockIds);
        Assert.Equal(DocumentChangeKind.Content | DocumentChangeKind.Structure, args.Kind);
        Assert.True(document.Modified);
        Assert.NotSame(table, document.Blocks[1]);
        Assert.Equal("edited", document.Resolve(document.PositionOf(cellParagraph.Id, 0)).Paragraph.Text);
        Assert.Same(((Table)table).Rows[0], ((Table)document.Blocks[1]).Rows[0]);
        Assert.True(document.Undo.CanUndo);

        document.Undo.Undo();

        Assert.Equal(2, events.Count);
        Assert.Equal(before, DocumentDescriber.Describe(document));
        Assert.Same(cellParagraph, document.FindBlock(cellParagraph.Id));
        Assert.Null(document.FindBlock("replacement"));
    }

    [Fact]
    public void Notify_AfterBlockRemoved_ReportsRemovedId_IndexNotStale()
    {
        var document = TextDocument.FromBlocks([P("a"), P("b"), P("c")]);
        var removed = document.Blocks[1];
        var last = document.Blocks[2];
        Assert.Same(last, document.FindBlock(last.Id));
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        document.Execute(new DelegateCommand(d =>
        {
            d.BlockList.RemoveAt(1);
            d.Notify(removed.Id, DocumentChangeKind.Structure);
        }, d =>
        {
            d.BlockList.Insert(1, removed);
            d.Notify(removed.Id, DocumentChangeKind.Structure);
        }));

        var args = Assert.Single(events);
        Assert.Equal([removed.Id], args.RemovedBlockIds);
        Assert.Empty(args.ChangedBlockIds);
        Assert.Null(document.FindBlock(removed.Id));
        Assert.Same(last, document.FindBlock(last.Id));
        Assert.Equal(new DocumentPosition(last.Id, [], 1), document.PositionOf(last.Id, 1));

        document.Undo.Undo();

        Assert.Equal([removed.Id], events[1].ChangedBlockIds);
        Assert.Same(removed, document.FindBlock(removed.Id));
    }

    [Fact]
    public void ReplaceBlock_InHeader_ReplacesHeaderPart_ReportsHeadersFooters()
    {
        var document = TextDocument.FromBlocks([P("body")]);
        document.HeaderFooterList.Add(new HeaderFooter { Id = "h1", Kind = HeaderFooterKind.Header, Blocks = [document.Identify(NestedTable())] });
        var header = document.HeadersFooters[0];
        var body = document.Blocks[0];
        var target = document.AllParagraphs(true).Single(p => p.Text == "inner a");
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        document.Execute(Replace(target, target with { Inlines = [new TextRun("header edit")] }));

        var args = Assert.Single(events);
        Assert.Equal(DocumentChangeKind.HeadersFooters, args.Kind);
        Assert.Equal([header.Blocks[0].Id], args.ChangedBlockIds);
        Assert.NotSame(header, document.HeadersFooters[0]);
        Assert.Equal("h1", document.HeadersFooters[0].Id);
        Assert.Same(body, document.Blocks[0]);
        Assert.Equal("header edit", ((Paragraph)document.FindBlock(target.Id)).Text);
        Assert.Equal(new DocumentPosition(header.Blocks[0].Id, [new(1, 0, 1), new(0, 0, 0)], 0), document.PositionOf(target.Id, 0));
    }

    [Fact]
    public void ReplaceBlock_AssignsMissingIds_RejectsForeignIds_NestedIdChangeIsStructure()
    {
        var document = TextDocument.FromBlocks([P("first"), NestedTable()]);
        var table = document.Blocks[1];
        var target = document.AllParagraphs().Single(p => p.Text == "inner b");
        Assert.Same(target, document.EnsureIds(target));
        Assert.Throws<ArgumentException>(() => document.ReplaceBlock(target, target with { Id = document.Blocks[0].Id }));
        Assert.Throws<ArgumentException>(() => document.ReplaceBlock(target, T(Row([target]), Row([document.Blocks[0]]))));
        var twice = P("twice") with { Id = "twice" };
        Assert.Throws<ArgumentException>(() => document.ReplaceBlock(target, T(Row([twice], [twice]))));
        Assert.Same(table, document.Blocks[1]);
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        document.Execute(Replace(target, T(Row([P("x")], [target]))));

        var args = Assert.Single(events);
        Assert.Equal(DocumentChangeKind.Content | DocumentChangeKind.Structure, args.Kind);
        Assert.Equal([table.Id], args.ChangedBlockIds);
        Assert.Empty(args.RemovedBlockIds);
        var inserted = (Table)((Table)((Table)document.Blocks[1]).Rows[1].Cells[0].Blocks[1]).Rows[0].Cells[1].Blocks[0];
        Assert.NotNull(inserted.Id);
        Assert.Same(inserted, document.FindBlock(inserted.Id));
        Assert.All(document.AllParagraphs(), p => Assert.Same(p, document.FindBlock(p.Id)));
        Assert.Same(target, document.FindBlock(target.Id));
    }

    [Fact]
    public void Resolve_InvalidPositions_Throw()
    {
        var document = TextDocument.FromBlocks([P("Hello"), NestedTable()]);
        var paragraph = document.Blocks[0].Id;
        var table = document.Blocks[1].Id;
        var nested = document.AllParagraphs().Single(p => p.Text == "inner a").Id;

        Assert.Throws<ArgumentException>(() => document.Resolve(new DocumentPosition(table, [new(5, 0, 0)], 0)));
        Assert.Throws<ArgumentException>(() => document.Resolve(new DocumentPosition(table, [new(1, 3, 0)], 0)));
        Assert.Throws<ArgumentException>(() => document.Resolve(new DocumentPosition(table, [new(1, 0, 2)], 0)));
        Assert.Throws<ArgumentException>(() => document.Resolve(new DocumentPosition(table, [new(-1, 0, 0)], 0)));
        Assert.Throws<ArgumentException>(() => document.Resolve(new DocumentPosition(table, [], 0)));
        Assert.Throws<ArgumentException>(() => document.Resolve(new DocumentPosition(paragraph, [new(0, 0, 0)], 0)));
        Assert.Throws<ArgumentException>(() => document.Resolve(new DocumentPosition(paragraph, [], 6)));
        Assert.Throws<ArgumentException>(() => document.Resolve(new DocumentPosition(paragraph, [], -1)));
        Assert.Throws<ArgumentException>(() => document.Resolve(new DocumentPosition("missing", [], 0)));
        Assert.Throws<ArgumentException>(() => document.Resolve(new DocumentPosition(nested, [], 0)));
        Assert.Throws<ArgumentException>(() => document.PositionOf(table, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.PositionOf(paragraph, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.PositionOf(nested, -1));
        Assert.IsNotType<CellStep[]>(document.PositionOf(nested, 0).Path);
        var valid = document.PositionOf(paragraph, 0);
        Assert.Throws<ArgumentNullException>(() => document.Resolve(null!));
        Assert.Throws<ArgumentNullException>(() => document.PositionOf(null!, 0));
        Assert.Throws<ArgumentNullException>(() => document.Compare(null!, valid));
        Assert.Throws<ArgumentNullException>(() => document.Compare(valid, null!));
        Assert.Throws<ArgumentNullException>(() => document.ParagraphsIn(new DocumentRange(valid, null!)));
        Assert.Throws<ArgumentNullException>(() => document.ParagraphsIn(default));
    }

    [Fact]
    public void Notify_AfterBlockAddedByListMutation_ReportsChanged_FindBlockFinds()
    {
        var document = TextDocument.FromBlocks([P("a"), P("b")]);
        Assert.Same(document.Blocks[1], document.FindBlock(document.Blocks[1].Id));
        var added = document.Identify(NestedTable());
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        document.Execute(new DelegateCommand(d =>
        {
            d.BlockList.Insert(1, added);
            d.Notify(added.Id, DocumentChangeKind.Structure);
        }, d =>
        {
            d.BlockList.RemoveAt(1);
            d.Notify(added.Id, DocumentChangeKind.Structure);
        }));

        var args = Assert.Single(events);
        Assert.Equal([added.Id], args.ChangedBlockIds);
        Assert.Empty(args.RemovedBlockIds);
        Assert.Same(added, document.FindBlock(added.Id));
        var nested = AllBlocks([added]).Last();
        Assert.Same(nested, document.FindBlock(nested.Id));
        Assert.Equal(added.Id, document.PositionOf(nested.Id, 0).BlockId);
    }

    [Fact]
    public void ReplaceBlock_ReturnsIdentifiedReplacement_UndoRestoresOriginal()
    {
        var document = TextDocument.FromBlocks([P("a"), NestedTable()]);
        var old = document.Blocks[0];
        var oldTable = document.Blocks[1];

        var returned = document.ReplaceBlock(old, P("new"));

        Assert.NotNull(returned.Id);
        Assert.Same(returned, document.Blocks[0]);
        Assert.Same(returned, document.FindBlock(returned.Id));

        Assert.Same(old, document.ReplaceBlock(returned, old));
        Assert.Same(old, document.Blocks[0]);
        Assert.Same(old, document.FindBlock(old.Id));
        Assert.Null(document.FindBlock(returned.Id));

        var table = (Table)document.ReplaceBlock(oldTable, T(Row([P("x")])));
        Assert.Same(oldTable, document.ReplaceBlock(table, oldTable));
        Assert.Same(oldTable, document.Blocks[1]);
    }

    [Fact]
    public void ReplaceBlock_TableLosesRow_DroppedNestedIdsRemoved()
    {
        var document = TextDocument.FromBlocks([P("a"), NestedTable()]);
        var table = (Table)document.Blocks[1];
        var dropped = AllBlocks(table.Rows[1].Cells.SelectMany(c => c.Blocks)).Select(b => b.Id).ToList();
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        document.Execute(Replace(table, table with { Rows = [table.Rows[0]] }));

        var args = Assert.Single(events);
        Assert.Equal([table.Id], args.ChangedBlockIds);
        Assert.Equal(dropped.Order(), args.RemovedBlockIds.Order());
        Assert.Equal(5, dropped.Count);
        Assert.All(dropped, id => Assert.Null(document.FindBlock(id)));
        Assert.Same(table.Rows[0].Cells[0].Blocks[0], document.FindBlock(table.Rows[0].Cells[0].Blocks[0].Id));
    }

    [Fact]
    public void ParagraphsIn_AcrossTable_DepthFirst()
    {
        var document = TextDocument.FromBlocks([P("before"), NestedTable(), P("after")]);
        var range = new DocumentRange(document.PositionOf(document.Blocks[2].Id, 3), document.PositionOf(document.Blocks[0].Id, 2));
        var fromCell = new DocumentRange(document.PositionOf(document.AllParagraphs().Single(p => p.Text == "outer").Id, 1), document.PositionOf(document.Blocks[2].Id, 2));

        Assert.Equal(["before", "a", "b", "outer", "inner a", "inner b", "c", "after"], document.ParagraphsIn(range).Select(p => p.Text));
        Assert.Equal("fore\na\nb\nouter\ninner a\ninner b\nc\naft", document.TextOf(range));
        Assert.Equal(["outer", "inner a", "inner b", "c", "after"], document.ParagraphsIn(fromCell).Select(p => p.Text));
    }

    [Fact]
    public void ValueSemantics_NullPathsTabsAndTextCache()
    {
        var position = new DocumentPosition("b1", [new(0, 0, 0)], 0) with { Path = null };
        Assert.NotNull(position.Path);
        Assert.Empty(position.Path);
        Assert.Empty(new DocumentPosition("b1", null, 0).Path);
        Assert.Equal(default, default(DocumentRange).Normalize(TextDocument.FromBlocks([P("a")])));
        Assert.True(default(DocumentRange).IsEmpty);

        var tabs = new ParagraphFormat { Tabs = [new TabStop(720, TabAlignment.Left, null), new TabStop(1440, TabAlignment.Decimal, "dot")] };
        var same = new ParagraphFormat { Tabs = [new TabStop(720, TabAlignment.Left, null), new TabStop(1440, TabAlignment.Decimal, "dot")] };
        Assert.Equal(tabs, same);
        Assert.Equal(tabs.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(tabs, same with { Tabs = [new TabStop(720, TabAlignment.Left, null)] });

        IReadOnlyList<Inline> inlines = [new TextRun(null), new TextRun("ab")];
        var paragraph = new Paragraph { Inlines = inlines };
        var fresh = new Paragraph { Inlines = inlines };
        Assert.Equal("ab", paragraph.Text);
        Assert.Equal(2, paragraph.Length);
        Assert.Equal(fresh, paragraph);
        Assert.Equal("xyz", (paragraph with { Inlines = [new TextRun("xyz")] }).Text);
        Assert.Equal(3, (paragraph with { Inlines = [new TextRun("xyz")] }).Length);
    }

    private static IDocumentCommand<TextDocument> Replace(Block old, Block replacement) =>
        new DelegateCommand(d => d.ReplaceBlock(old, replacement), d => d.ReplaceBlock(replacement, old));

    private static Paragraph P(string text) => new() { Inlines = [new TextRun(text)] };

    private static Table T(params TableRow[] rows) => new() { Grid = [2000, 2000], Rows = rows };

    private static TableRow Row(params Block[][] cells) => new(cells.Select(c => new TableCell(c)).ToList());

    private static Table NestedTable() =>
        T(Row([P("a")], [P("b")]),
          Row([P("outer"), T(Row([P("inner a")], [P("inner b")]))], [P("c")]));

    private static IReadOnlyDictionary<HeaderFooterType, string> Refs(params (HeaderFooterType Type, string Id)[] refs) => refs.ToDictionary(r => r.Type, r => r.Id);

    private static IEnumerable<Block> AllBlocks(IEnumerable<Block> blocks) =>
        blocks.SelectMany(b => b is Table t ? new[] { b }.Concat(AllBlocks(t.Rows.SelectMany(r => r.Cells).SelectMany(c => c.Blocks))) : [b]);

    private sealed class DelegateCommand(Action<TextDocument> apply, Action<TextDocument> revert) : IDocumentCommand<TextDocument>
    {
        public string Description => "Test";

        public void Apply(TextDocument document) => apply(document);

        public void Revert(TextDocument document) => revert(document);
    }
}
