using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public partial class TextCommandTests
{
    [Fact]
    public void ReplaceText_AcrossParagraphs_MergesKeepsFirstFormat()
    {
        var document = TextDocument.FromBlocks(
        [
            P("Hello") with { Format = new ParagraphFormat { Alignment = ParagraphAlignment.Center } },
            P("middle"),
            P("World") with { Format = new ParagraphFormat { Alignment = ParagraphAlignment.Right } }
        ]);
        var first = document.Blocks[0];
        var command = new ReplaceTextCommand(Range(document, first.Id, 2, document.Blocks[2].Id, 3), "X");

        document.Execute(command);

        var merged = Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
        Assert.Equal("HeXld", merged.Text);
        Assert.Equal(first.Id, merged.Id);
        Assert.Equal(ParagraphAlignment.Center, merged.Format.Alignment);
        Assert.Equal(new DocumentRange(document.PositionOf(first.Id, 3), document.PositionOf(first.Id, 3)), command.After);
    }

    [Fact]
    public void ReplaceText_WithNewlines_SplitsIntoThreeParagraphs()
    {
        var document = TextDocument.FromBlocks([P("[]") with { Format = new ParagraphFormat { StyleId = "Body" } }]);
        var id = document.Blocks[0].Id;
        var command = new ReplaceTextCommand(Range(document, id, 1, id, 1), "a\nb\nc");

        document.Execute(command);

        Assert.Equal(["[a", "b", "c]"], document.Blocks.Cast<Paragraph>().Select(p => p.Text));
        Assert.Equal(id, document.Blocks[0].Id);
        Assert.All(document.Blocks.Cast<Paragraph>(), p => Assert.Equal("Body", p.Format.StyleId));
        Assert.Equal(3, document.Blocks.Select(b => b.Id).Distinct().Count());
        Assert.Equal(document.PositionOf(document.Blocks[2].Id, 1), command.After.End);
        Assert.True(command.After.IsEmpty);
    }

    [Fact]
    public void ReplaceText_TabsAndLineBreaks_BecomeInlines()
    {
        var document = TextDocument.FromBlocks([P("")]);
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 0), "a\tb\vc"));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("a\tb\vc", paragraph.Text);
        Assert.Collection(paragraph.Inlines, i => Assert.IsType<TextRun>(i), i => Assert.IsType<Tab>(i), i => Assert.IsType<TextRun>(i),
            i => Assert.Equal(BreakKind.Line, Assert.IsType<Break>(i).Kind), i => Assert.IsType<TextRun>(i));
    }

    [Fact]
    public void ReplaceText_OverBookmark_KeepsMarkersAtStart_DropsProofErr()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>Hel</w:t></w:r><w:proofErr w:type=\"spellStart\"/><w:r><w:t xml:space=\"preserve\">lo </w:t></w:r>" +
                                              "<w:bookmarkStart w:id=\"0\" w:name=\"Mark\"/><w:r><w:t>World</w:t></w:r><w:bookmarkEnd w:id=\"0\"/></w:p>").Load();
        var id = document.Blocks[0].Id;
        Assert.Equal("lo Wo", document.TextOf(Range(document, id, 3, id, 8)));

        document.Execute(new ReplaceTextCommand(Range(document, id, 3, id, 8), "X"));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("HelXrld", paragraph.Text);
        Assert.Equal(["r Hel", "m bookmarkStart", "r X", "r rld", "m bookmarkEnd"], paragraph.Inlines.Select(Short));
        Assert.Same(paragraph.Inlines[0].Source, paragraph.Inlines[2].Source);
        Assert.NotNull(paragraph.Inlines[3].Source);
        Assert.Equal("Mark", ((Marker)paragraph.Inlines[1]).Name);
    }

    [Fact]
    public void ReplaceText_AcrossParagraphs_MovesMarkersOfDeletedParagraphsToStart()
    {
        var document = TextDocument.FromBlocks(
        [
            new Paragraph { Inlines = [new TextRun("ab"), new Marker("bookmarkStart") { Name = "x" }, new TextRun("cd")] },
            new Paragraph { Inlines = [new Marker("commentRangeStart"), new Marker("proofErr"), new TextRun("ef")] },
            new Paragraph { Inlines = [new TextRun("g"), new Marker("bookmarkEnd"), new TextRun("hi")] }
        ]);

        document.Execute(new ReplaceTextCommand(Range(document, document.Blocks[0].Id, 1, document.Blocks[2].Id, 2), ""));

        Assert.Equal(["r a", "m bookmarkStart", "m commentRangeStart", "m bookmarkEnd", "r i"], ((Paragraph)Assert.Single(document.Blocks)).Inlines.Select(Short));
    }

    [Fact]
    public void ReplaceText_AtLinkEnd_TypedTextOutsideLink()
    {
        var link = new Hyperlink("https://example.com", null, null);
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("See "), new TextRun("example") { Link = link, Format = new RunFormat { StyleId = "Hyperlink" } }] }]);
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 11, id, 11), "!"));
        document.Execute(new ReplaceTextCommand(Range(document, id, 7, id, 7), "-"));
        document.Execute(new ReplaceTextCommand(Range(document, id, 4, id, 4), "the "));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("See the exa-mple!", paragraph.Text);
        Assert.Equal([null, link, null], paragraph.Inlines.Select(i => i.Link));
        Assert.Equal(["See the ", "exa-mple", "!"], paragraph.Inlines.Select(i => ((TextRun)i).Text));
        Assert.Equal(["Hyperlink", null], paragraph.Inlines.Skip(1).Select(i => i.Format.StyleId));
    }

    [Fact]
    public void ReplaceText_FormatFromCharacterBefore_AtStartFromCharacterAfter_OrExplicit()
    {
        var bold = new RunFormat { Bold = true };
        var italic = new RunFormat { Italic = true };
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("B") { Format = bold }, new TextRun("I") { Format = italic }] }]);
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 0), "<"));
        document.Execute(new ReplaceTextCommand(Range(document, id, 2, id, 2), "b"));
        document.Execute(new ReplaceTextCommand(Range(document, id, 4, id, 4), "x", new RunFormat { Underline = "single" }));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal(["<Bb", "I", "x"], paragraph.Inlines.Select(i => ((TextRun)i).Text));
        Assert.Equal([bold, italic, new RunFormat { Underline = "single" }], paragraph.Inlines.Select(i => i.Format));
    }

    [Fact]
    public void ReplaceText_MarkersExactlyAtStartAndEnd()
    {
        var document = TextDocument.FromBlocks(
        [
            new Paragraph { Inlines = [new TextRun("ab"), new Marker("bookmarkStart"), new TextRun("cd"), new Marker("bookmarkEnd"), new TextRun("ef")] },
            new Paragraph { Inlines = [new TextRun("ab"), new Marker("proofErr"), new TextRun("cd"), new Marker("proofErr"), new TextRun("ef")] }
        ]);
        var (first, second) = (document.Blocks[0].Id, document.Blocks[1].Id);

        document.Execute(new ReplaceTextCommand(Range(document, first, 2, first, 4), "X"));
        document.Execute(new ReplaceTextCommand(Range(document, second, 2, second, 4), "X"));

        Assert.Equal(["r ab", "m bookmarkStart", "r X", "m bookmarkEnd", "r ef"], ((Paragraph)document.Blocks[0]).Inlines.Select(Short));
        Assert.Equal(["r abX", "m proofErr", "r ef"], ((Paragraph)document.Blocks[1]).Inlines.Select(Short));
    }

    [Fact]
    public void ReplaceText_ReversedRange_SameAsForward()
    {
        var document = TextDocument.FromBlocks([P("Hello World")]);
        var id = document.Blocks[0].Id;
        var command = new ReplaceTextCommand(Range(document, id, 8, id, 2), "X");

        document.Execute(command);

        Assert.Equal("HeXrld", ((Paragraph)document.Blocks[0]).Text);
        Assert.Equal(document.PositionOf(id, 3), command.After.Start);
    }

    [Fact]
    public void ReplaceText_CarriageReturnsTrailingNewlineAndFormFeed()
    {
        var document = TextDocument.FromBlocks([P("")]);
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 0), "a\r\nb\rc\fd\n"));

        Assert.Equal(["a", "b", "c\fd", ""], document.Blocks.Cast<Paragraph>().Select(p => p.Text));
        Assert.Equal(BreakKind.Page, ((Paragraph)document.Blocks[2]).Inlines.OfType<Break>().Single().Kind);
        Assert.Equal(id, document.Blocks[0].Id);
    }

    [Fact]
    public void ReplaceText_OffsetInsideSurrogatePair_SnapsOutward()
    {
        var document = TextDocument.FromBlocks([P("a\uD83D\uDE00b"), P("c\uD83D\uDE00d")]);
        var (first, second) = (document.Blocks[0].Id, document.Blocks[1].Id);

        document.Execute(new ReplaceTextCommand(Range(document, first, 2, first, 2), "X"));
        document.Execute(new ReplaceTextCommand(Range(document, second, 2, second, 3), "Y"));
        document.Execute(new SetRunFormatCommand(Range(document, first, 0, first, 3), f => f with { Bold = true }, "Bold"));

        var bolded = (Paragraph)document.Blocks[0];
        Assert.Equal("aX\uD83D\uDE00b", bolded.Text);
        Assert.Equal("cYd", ((Paragraph)document.Blocks[1]).Text);
        Assert.Equal(["aX\uD83D\uDE00", "b"], bolded.Inlines.Select(i => ((TextRun)i).Text));
    }

    [Fact]
    public void ReplaceText_OverToken_DeletesItWhole()
    {
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("a"), new OpaqueInline("field", "1") { Instruction = "PAGE" }, new TextRun("b")] }]);
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 1, id, 2), ""));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("ab", paragraph.Text);
        Assert.DoesNotContain(paragraph.Inlines, i => i is OpaqueInline);
    }

    [Fact]
    public void ReplaceText_IntoTable_ClearsCoveredCells_KeepsStructure()
    {
        var document = TextDocument.FromBlocks([P("before"), T(Row([P("a")], [P("b")]), Row([P("c")], [P("d1"), P("d2")])), P("after")]);
        var d2 = document.AllParagraphs().Single(p => p.Text == "d2");
        var table = (Table)document.Blocks[1];

        var command = new ReplaceTextCommand(Range(document, document.Blocks[0].Id, 3, d2.Id, 1), "X");
        document.Execute(command);

        Assert.Equal(document.PositionOf(document.Blocks[0].Id, 4), command.After.Start);
        Assert.True(command.After.IsEmpty);
        Assert.Equal(["befX", "", "", "", "2", "after"], document.AllParagraphs().Select(p => p.Text));
        var edited = (Table)document.Blocks[1];
        Assert.Equal(table.Id, edited.Id);
        Assert.Equal(2, edited.Rows.Count);
        Assert.All(edited.Rows, r => Assert.Equal(2, r.Cells.Count));
        Assert.Equal(d2.Id, edited.Rows[1].Cells[1].Blocks.Single().Id);
        Assert.Equal(3, document.Blocks.Count);
    }

    [Fact]
    public void ReplaceText_FromCellToBody_ClearsLaterCells_RemovesBlocksBetween()
    {
        var document = TextDocument.FromBlocks([T(Row([P("a"), T(Row([P("x")]))], [P("b")])), P("gone"), P("after")]);
        var a = document.AllParagraphs().First();

        document.Execute(new ReplaceTextCommand(Range(document, a.Id, 0, document.Blocks[2].Id, 2), "Y"));

        Assert.Equal(["Y", "", "ter"], document.AllParagraphs().Select(p => p.Text));
        Assert.Equal(2, document.Blocks.Count);
        Assert.IsType<Paragraph>(((Table)document.Blocks[0]).Rows[0].Cells[0].Blocks[^1]);
    }

    [Fact]
    public void ReplaceText_BodyToHeader_Throws()
    {
        var document = Standard();
        var header = document.HeadersFooters[0].Blocks[0];

        Assert.Throws<ArgumentException>(() => document.Execute(new ReplaceTextCommand(Range(document, document.Blocks[0].Id, 0, header.Id, 1), "")));
        Assert.False(document.Undo.CanUndo);
    }

    [Fact]
    public void ReplaceText_EmptyRangeEmptyText_IsNoOp()
    {
        var document = Standard();
        var id = document.Blocks[0].Id;
        var changed = 0;
        document.Changed += (_, _) => changed++;
        var version = document.Version;

        document.Execute(new ReplaceTextCommand(Range(document, id, 2, id, 2), ""));

        Assert.False(document.Undo.CanUndo);
        Assert.False(document.Modified);
        Assert.Equal(0, changed);
        Assert.Equal(version, document.Version);
    }
}
