using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public partial class TextCommandTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("replace")]
    [InlineData("replaceIntoTable")]
    [InlineData("replaceFromTable")]
    [InlineData("replaceInHeader")]
    [InlineData("runFormat")]
    [InlineData("paragraphFormat")]
    [InlineData("split")]
    [InlineData("splitInCell")]
    [InlineData("merge")]
    [InlineData("break")]
    [InlineData("link")]
    [InlineData("unlink")]
    [InlineData("content")]
    public void Command_ApplyRevertRedo_SnapshotsAndInstances(string name)
    {
        var document = Standard();
        var command = CommandFor(document, name);
        var before = DocumentDescriber.Describe(document);
        var blocks = document.Blocks.ToList();
        var sections = document.Sections.ToList();
        var headerBlocks = document.HeadersFooters.SelectMany(h => h.Blocks).ToList();
        var untouched = document.Blocks[^1];

        document.Execute(command);

        var after = DocumentDescriber.Describe(document);
        Assert.NotEqual(before, after);
        Assert.Same(untouched, document.Blocks[^1]);
        Assert.Equal(1, Steps(document));
        Assert.All(document.AllParagraphs(true), p => Assert.Same(p, document.FindBlock(p.Id)));
        Assert.Equal(document.AllParagraphs(true).Count(), document.AllParagraphs(true).Select(p => p.Id).Distinct().Count());

        document.Undo.Undo();

        Assert.Equal(before, DocumentDescriber.Describe(document));
        Assert.True(blocks.SequenceEqual(document.Blocks, ReferenceEqualityComparer.Instance));
        Assert.True(sections.SequenceEqual(document.Sections, ReferenceEqualityComparer.Instance));
        Assert.True(headerBlocks.SequenceEqual(document.HeadersFooters.SelectMany(h => h.Blocks), ReferenceEqualityComparer.Instance));

        document.Undo.Redo();

        Assert.Equal(after, DocumentDescriber.Describe(document));
    }

    [Fact]
    public void Undo_RestoresOriginalInstances_BodyAndNestedCellParagraph()
    {
        var document = Standard();
        var body = document.Blocks[1];
        var table = document.Blocks[2];
        var nested = document.AllParagraphs().Single(p => p.Text == "inner b");

        document.Execute(new ReplaceTextCommand(Range(document, body.Id, 0, body.Id, 3), "X"));
        document.Execute(new ReplaceTextCommand(Range(document, nested.Id, 1, nested.Id, 4), "Y"));
        Assert.NotSame(table, document.Blocks[2]);
        document.Undo.Undo();
        document.Undo.Undo();

        Assert.Same(body, document.Blocks[1]);
        Assert.Same(table, document.Blocks[2]);
        Assert.Same(nested, document.FindBlock(nested.Id));
    }

    private static void Type(TextDocument document, string paragraphId, string text, DateTimeOffset at) =>
        document.ApplyTyping(paragraphId, [new TextRun(text)], at);

    private static int Steps(TextDocument document)
    {
        var steps = 0;
        while (document.Undo.CanUndo)
        {
            document.Undo.Undo();
            steps++;
        }

        for (var i = 0; i < steps; i++) document.Undo.Redo();
        return steps;
    }

    private static IDocumentCommand<TextDocument> CommandFor(TextDocument document, string name)
    {
        var b0 = document.Blocks[0].Id;
        var b1 = document.Blocks[1].Id;
        string Cell(string text) => document.AllParagraphs().Single(p => p.Text == text).Id;
        var header = document.HeadersFooters[0].Blocks[0].Id;
        return name switch
        {
            "replace" => new ReplaceTextCommand(Range(document, b0, 2, b1, 3), "X"),
            "replaceIntoTable" => new ReplaceTextCommand(Range(document, b1, 3, Cell("inner a"), 2), "Y\nZ"),
            "replaceFromTable" => new ReplaceTextCommand(Range(document, Cell("b"), 0, document.Blocks[3].Id, 0), ""),
            "replaceInHeader" => new ReplaceTextCommand(Range(document, header, 0, header, 6), "Top"),
            "runFormat" => new SetRunFormatCommand(Range(document, b0, 0, Cell("b"), 1), f => f with { Bold = true }, "Bold"),
            "paragraphFormat" => new SetParagraphFormatCommand([b0, Cell("inner b")], f => f with { Alignment = ParagraphAlignment.Center }, "Center"),
            "split" => new SplitParagraphCommand(document.PositionOf(b0, 5)),
            "splitInCell" => new SplitParagraphCommand(document.PositionOf(Cell("inner a"), 2)),
            "merge" => new MergeParagraphsCommand(b0),
            "break" => new InsertBreakCommand(document.PositionOf(b1, 2), BreakKind.Page),
            "link" => new SetHyperlinkCommand(Range(document, b0, 0, b0, 5), "https://example.com"),
            "unlink" => new SetHyperlinkCommand(Range(document, b1, 0, b1, 6), null),
            "content" => new ReplaceParagraphContentCommand(Cell("inner b"), [new TextRun("typed")]),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
    }

    private static TextDocument Standard()
    {
        var link = new Hyperlink("https://example.org", null, null);
        var document = TextDocument.FromBlocks(
        [
            P("Hello World"),
            new Paragraph { Inlines = [new TextRun("Second") { Link = link }, new TextRun(" line")] },
            T(Row([P("a")], [P("b")]), Row([P("outer"), T(Row([P("inner a")], [P("inner b")]))], [P("c")])),
            P("Untouched")
        ]);
        document.HeaderFooterList.Add(new HeaderFooter { Id = "h1", Kind = HeaderFooterKind.Header, Blocks = [document.Identify(P("Header text"))] });
        document.SectionList.AddRange([new Section { Id = "s1", LastBlockId = document.Blocks[0].Id, Page = PageSetup.Letter }, new Section { Id = "s2" }]);
        return document;
    }

    private static TextDocument WithSections(out Paragraph first, out List<Section> sections)
    {
        var document = TextDocument.FromBlocks([P("a"), P("b")]);
        first = (Paragraph)document.Blocks[0];
        document.SectionList.AddRange([new Section { Id = "s1", LastBlockId = first.Id, Page = PageSetup.Letter }, new Section { Id = "s2", Page = PageSetup.A4 }]);
        sections = document.Sections.ToList();
        return document;
    }

    private static TextDocument WithBuiltInStyles(params Block[] blocks)
    {
        var document = TextDocument.FromBlocks(blocks);
        document.Styles = new DocumentStyles(BuiltInStyles.All, null, null);
        return document;
    }

    private static DocumentRange Range(TextDocument document, string startId, int start, string endId, int end) =>
        new(document.PositionOf(startId, start), document.PositionOf(endId, end));

    private static string Short(Inline inline) => inline switch
    {
        TextRun r => "r " + r.Text,
        Marker m => "m " + m.Kind,
        _ => inline.GetType().Name
    };

    private static Paragraph P(string text) => new() { Inlines = [new TextRun(text)] };

    private static Table T(params TableRow[] rows) => new() { Grid = [2000, 2000], Rows = rows };

    private static TableRow Row(params Block[][] cells) => new(cells.Select(c => new TableCell(c)).ToList());
}
