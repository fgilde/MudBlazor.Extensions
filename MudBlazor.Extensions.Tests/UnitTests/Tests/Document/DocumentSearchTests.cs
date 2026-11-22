using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class DocumentSearchTests
{
    [Fact]
    public void Find_AcrossRunsSplitByProofErr_Matches()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>Hel</w:t></w:r><w:proofErr w:type=\"spellStart\"/><w:r><w:t xml:space=\"preserve\">lo </w:t></w:r>" +
                                              "<w:bookmarkStart w:id=\"0\" w:name=\"Mark\"/><w:r><w:t>World</w:t></w:r><w:bookmarkEnd w:id=\"0\"/></w:p>").Load();
        var id = document.Blocks[0].Id;

        var match = Assert.Single(DocumentSearch.FindAll(document, "Hello World"));

        Assert.Equal(new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 11)), match);
    }

    [Fact]
    public void Find_WholeWordAndMatchCase()
    {
        var document = TextDocument.FromBlocks([P("The cat scattered Cats, cat_x and cat.")]);

        Assert.Equal(5, DocumentSearch.FindAll(document, "cat").Count);
        Assert.Equal(["cat", "cat"], DocumentSearch.FindAll(document, "cat", new FindOptions(WholeWord: true)).Select(document.TextOf));
        Assert.Equal(["Cat"], DocumentSearch.FindAll(document, "Cat", new FindOptions(MatchCase: true)).Select(document.TextOf));
        Assert.Empty(DocumentSearch.FindAll(document, "Cat", new FindOptions(MatchCase: true, WholeWord: true)));
        Assert.Equal(3, DocumentSearch.FindAll(document, "cats?", new FindOptions(Regex: true, WholeWord: true)).Count);
    }

    [Fact]
    public void Find_InNestedTableAndHeader()
    {
        var document = TextDocument.FromBlocks([P("before"), T(Row([P("outer"), T(Row([P("x inner a")], [P("inner b")]))]))]);
        document.HeaderFooterList.Add(new HeaderFooter { Id = "h1", Kind = HeaderFooterKind.Header, Blocks = [document.Identify(P("inner header"))] });

        var all = DocumentSearch.FindAll(document, "inner");
        var bodyOnly = DocumentSearch.FindAll(document, "inner", new FindOptions(IncludeHeadersFooters: false));

        Assert.Equal(3, all.Count);
        Assert.Equal(2, bodyOnly.Count);
        Assert.All(all, r => Assert.Equal("inner", document.TextOf(r)));
        Assert.Equal([2, 0, 0], all.Select(r => r.Start.Offset));
        Assert.Equal(document.HeadersFooters[0].Blocks[0].Id, all[2].Start.BlockId);
        Assert.Equal([new CellStep(0, 0, 1), new CellStep(0, 1, 0)], all[1].Start.Path);
    }

    [Fact]
    public void Find_NeverMatchesTokenOrAcrossParagraphs()
    {
        var document = TextDocument.FromBlocks(
        [
            new Paragraph { Inlines = [new TextRun("ab"), new OpaqueInline("field", "x"), new TextRun("cd")] },
            P("ef")
        ]);

        Assert.Empty(DocumentSearch.FindAll(document, "b.c", new FindOptions(Regex: true)));
        Assert.Empty(DocumentSearch.FindAll(document, "\uFFFC"));
        Assert.Empty(DocumentSearch.FindAll(document, "d\\s*e", new FindOptions(Regex: true)));
        Assert.Empty(DocumentSearch.FindAll(document, "x"));
        Assert.Empty(DocumentSearch.FindAll(document, ""));
        Assert.Empty(DocumentSearch.FindAll(document, "z*", new FindOptions(Regex: true)));
        Assert.Equal(3, DocumentSearch.FindAll(document, "[a-f]{2}", new FindOptions(Regex: true)).Count);
    }

    [Fact]
    public void FindNext_FromPosition_WrapsAround()
    {
        var document = TextDocument.FromBlocks([P("a x"), P("x b x")]);
        var first = document.Blocks[0].Id;
        var second = document.Blocks[1].Id;

        Assert.Equal(document.PositionOf(second, 0), DocumentSearch.FindNext(document, document.PositionOf(first, 3), "x")!.Value.Start);
        Assert.Equal(document.PositionOf(second, 4), DocumentSearch.FindNext(document, document.PositionOf(second, 1), "x")!.Value.Start);
        Assert.Equal(document.PositionOf(first, 2), DocumentSearch.FindNext(document, document.PositionOf(second, 5), "x")!.Value.Start);
        Assert.Null(DocumentSearch.FindNext(document, document.PositionOf(first, 0), "missing"));
    }

    [Fact]
    public void ReplaceAll_Regex_SwapsGroups()
    {
        var document = TextDocument.FromBlocks([P("12-34 and 5-6")]);

        var count = DocumentSearch.ReplaceAll(document, @"(\d+)-(\d+)", "$2-$1", new FindOptions(Regex: true));

        Assert.Equal(2, count);
        Assert.Equal("34-12 and 6-5", ((Paragraph)document.Blocks[0]).Text);
    }

    [Fact]
    public void ReplaceAll_OneUndoStep_ReturnsCount()
    {
        var document = TextDocument.FromBlocks([P("a b a"), T(Row([P("a")])), P("none")]);
        var before = DocumentDescriber.Describe(document);
        var blocks = document.Blocks.ToList();
        var events = 0;
        document.Changed += (_, _) => events++;

        var count = DocumentSearch.ReplaceAll(document, "a", "x$1y");

        Assert.Equal(3, count);
        Assert.Equal(["x$1y b x$1y", "x$1y", "none"], document.AllParagraphs().Select(p => p.Text));
        Assert.Equal(1, events);
        Assert.Same(blocks[2], document.Blocks[2]);
        document.Undo.Undo();
        Assert.False(document.Undo.CanUndo);
        Assert.Equal(before, DocumentDescriber.Describe(document));
        Assert.True(blocks.SequenceEqual(document.Blocks, ReferenceEqualityComparer.Instance));
        Assert.Equal(0, DocumentSearch.ReplaceAll(document, "missing", "x"));
        Assert.True(document.Undo.CanRedo);
    }

    [Fact]
    public void ReplaceAll_KeepsFormatOfFirstCharacter()
    {
        var bold = new RunFormat { Bold = true };
        var document = TextDocument.FromBlocks(
        [
            new Paragraph { Inlines = [new TextRun("x") { Format = new RunFormat { Italic = true } }, new TextRun("f") { Format = bold }, new TextRun("oo")] }
        ]);

        DocumentSearch.ReplaceAll(document, "foo", "bar");

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("xbar", paragraph.Text);
        var replaced = Assert.IsType<TextRun>(paragraph.Inlines[1]);
        Assert.Equal("bar", replaced.Text);
        Assert.Equal(bold, replaced.Format);
    }

    [Fact]
    public void Find_CatastrophicRegex_StopsQuietly()
    {
        var document = TextDocument.FromBlocks([P("aa"), P(new string('a', 40) + "b"), P("aaa")]);
        var first = document.Blocks[0].Id;

        var matches = DocumentSearch.Find(document, "(a+)+$", new FindOptions(Regex: true), TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(5));
        var stopped = DocumentSearch.Find(document, "a", new FindOptions(), TimeSpan.FromSeconds(1), TimeSpan.FromTicks(-1));
        var complete = DocumentSearch.Find(document, "a");

        Assert.Equal([new DocumentRange(document.PositionOf(first, 0), document.PositionOf(first, 2))], matches.Ranges);
        Assert.True(matches.Truncated);
        Assert.Empty(stopped.Ranges);
        Assert.True(stopped.Truncated);
        Assert.Equal((45, false), (complete.Ranges.Count, complete.Truncated));
        Assert.Equal(complete.Ranges, DocumentSearch.FindAll(document, "a"));
        Assert.Equal(complete.Ranges[2], DocumentSearch.FindNext(document, complete.Ranges[1].End, "a", null, out var truncated));
        Assert.False(truncated);
    }

    [Fact]
    public void ReplaceAll_CutShortBySearchDeadlineOrRegexTimeout_ChangesNothingAndThrows()
    {
        var document = TextDocument.FromBlocks([P("aa"), P(new string('a', 40) + "b"), P("aaa")]);
        var before = document.AllParagraphs().Select(p => p.Text).ToList();

        Assert.Throws<TimeoutException>(() => DocumentSearch.ReplaceAll(document, "a", "x", null, TimeSpan.FromSeconds(1), TimeSpan.FromTicks(-1)));
        Assert.ThrowsAny<TimeoutException>(() =>
            DocumentSearch.ReplaceAll(document, "(a+)+$", "x", new FindOptions(Regex: true), TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(5)));

        Assert.Equal(before, document.AllParagraphs().Select(p => p.Text));
        Assert.False(document.Undo.CanUndo);
        Assert.Equal(45, DocumentSearch.ReplaceAll(document, "a", "x"));
        Assert.True(document.Undo.CanUndo);
    }

    [Fact]
    public void ReplaceAll_MatchInsideSurrogatePair_ReplacesWholeCharacter()
    {
        var document = TextDocument.FromBlocks([P("\uD83D\uDE00 smile"), P("x\uD83D\uDE00")]);

        var count = DocumentSearch.ReplaceAll(document, "^.", "X", new FindOptions(Regex: true));
        var dots = DocumentSearch.FindAll(document, ".", new FindOptions(Regex: true));

        Assert.Equal(2, count);
        Assert.Equal(["X smile", "X\uD83D\uDE00"], document.AllParagraphs().Select(p => p.Text));
        Assert.Equal("\uD83D\uDE00", document.TextOf(dots[^1]));
        Assert.Equal(9, dots.Count);
    }

    [Fact]
    public void ReplaceAll_GroupInsideSurrogatePair_WidenedToWholeCharacter()
    {
        var document = TextDocument.FromBlocks([P("\uD83D\uDE00")]);

        DocumentSearch.ReplaceAll(document, ".", "<$0>", new FindOptions(Regex: true));

        Assert.Equal("<\uD83D\uDE00>", ((Paragraph)document.Blocks[0]).Text);
    }

    [Fact]
    public void ReplaceAll_WholeLink_ClearsHyperlinkStyle()
    {
        var link = new Hyperlink("https://example.org", null, null);
        var style = new RunFormat { StyleId = "Hyperlink", Bold = true };
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("a "), new TextRun("link") { Link = link, Format = style }, new TextRun(" b")] }]);

        DocumentSearch.ReplaceAll(document, "link", "x");

        var replaced = ((Paragraph)document.Blocks[0]).Inlines.OfType<TextRun>().Single(r => r.Text.Contains('x'));
        Assert.Null(replaced.Link);
        Assert.Equal(style with { StyleId = null }, replaced.Format);
    }

    private static Paragraph P(string text) => new() { Inlines = [new TextRun(text)] };

    private static Table T(params TableRow[] rows) => new() { Grid = [2000, 2000], Rows = rows };

    private static TableRow Row(params Block[][] cells) => new(cells.Select(c => new TableCell(c)).ToList());
}
