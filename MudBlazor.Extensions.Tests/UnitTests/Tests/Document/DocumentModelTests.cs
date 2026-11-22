using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class DocumentModelTests
{
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
    public void Hyperlink_SafeTarget(string target, string expected)
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
