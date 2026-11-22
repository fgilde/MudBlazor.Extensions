using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class TextCommandTests
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
        Assert.Null(paragraph.Inlines[2].Source);
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

    [Fact]
    public void SetRunFormat_BoldMiddleWord_ThreeRuns_NeighboursKeepSource()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>one two three</w:t></w:r></w:p>").Load();
        var id = document.Blocks[0].Id;
        var source = ((Paragraph)document.Blocks[0]).Inlines[0].Source;
        Assert.NotNull(source);

        document.Execute(new SetRunFormatCommand(Range(document, id, 4, id, 7), f => f with { Bold = true }, "Bold"));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal(["one ", "two", " three"], paragraph.Inlines.Select(i => ((TextRun)i).Text));
        Assert.Equal([null, true, null], paragraph.Inlines.Select(i => i.Format.Bold));
        Assert.All(paragraph.Inlines, i => Assert.Same(source, i.Source));
        Assert.Equal("Bold", document.Undo.UndoDescription);
    }

    [Fact]
    public void SetRunFormat_BoldOnThenOff_SingleRunAgain()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>one two three</w:t></w:r></w:p>").Load();
        var id = document.Blocks[0].Id;
        var original = ((Paragraph)document.Blocks[0]).Inlines[0];

        document.Execute(new SetRunFormatCommand(Range(document, id, 4, id, 7), f => f with { Bold = true }, "Bold"));
        document.Execute(new SetRunFormatCommand(Range(document, id, 4, id, 7), f => f with { Bold = null }, "Not bold"));

        var run = Assert.Single(((Paragraph)document.Blocks[0]).Inlines);
        Assert.Equal(original, run);
        Assert.Same(original.Source, run.Source);
    }

    [Fact]
    public void SetHyperlink_OnThenOff_NeverMergesRunsWithDifferentSource()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>ab</w:t></w:r><w:r><w:t>cd</w:t></w:r></w:p>").Load();
        var id = document.Blocks[0].Id;

        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 4), "https://example.com"));
        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 4), null));

        Assert.Equal(["ab", "cd"], ((Paragraph)document.Blocks[0]).Inlines.Select(i => ((TextRun)i).Text));
    }

    [Fact]
    public void SetRunFormat_NothingChanges_NoUndoStep()
    {
        var document = TextDocument.FromBlocks([P("abc")]);
        var id = document.Blocks[0].Id;

        document.Execute(new SetRunFormatCommand(Range(document, id, 1, id, 2), f => f, "Same"));

        Assert.False(document.Undo.CanUndo);
    }

    [Fact]
    public void SetParagraphFormat_ChangesOnlyListedParagraphs()
    {
        var document = Standard();
        var cell = document.AllParagraphs().Single(p => p.Text == "inner a");
        var untouched = document.Blocks[1];

        document.Execute(new SetParagraphFormatCommand([document.Blocks[0].Id, cell.Id], f => f with { Alignment = ParagraphAlignment.Center }, "Center"));

        Assert.Equal(ParagraphAlignment.Center, ((Paragraph)document.Blocks[0]).Format.Alignment);
        Assert.Equal(ParagraphAlignment.Center, ((Paragraph)document.FindBlock(cell.Id)).Format.Alignment);
        Assert.Same(untouched, document.Blocks[1]);
        Assert.Throws<ArgumentException>(() => document.Execute(new SetParagraphFormatCommand(["missing"], f => f, "x")));
    }

    [Fact]
    public void Split_EndOfHeading1_NextUsesNormal()
    {
        var document = WithBuiltInStyles(P("Title") with { Format = new ParagraphFormat { StyleId = "Heading1" } });
        var id = document.Blocks[0].Id;

        document.Execute(new SplitParagraphCommand(document.PositionOf(id, 2)));
        document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[1].Id, 3)));

        Assert.Equal(["Ti", "tle", ""], document.Blocks.Cast<Paragraph>().Select(p => p.Text));
        Assert.Equal(["Heading1", "Heading1", null], document.Blocks.Cast<Paragraph>().Select(p => p.Format.StyleId));
        Assert.Equal("Normal", document.Styles.DefaultParagraphStyle.Id);
    }

    [Fact]
    public void Split_EmptyListParagraph_RemovesNumbering_NoSplit()
    {
        var document = TextDocument.FromBlocks([P("item") with { Format = new ParagraphFormat { Numbering = new NumberingRef(1, 0) } }]);
        var command = new SplitParagraphCommand(document.PositionOf(document.Blocks[0].Id, 4));
        document.Execute(command);
        var empty = document.Blocks[1];

        document.Execute(new SplitParagraphCommand(document.PositionOf(empty.Id, 0)));

        Assert.Equal(2, document.Blocks.Count);
        Assert.Equal(new NumberingRef(1, 0), ((Paragraph)document.Blocks[0]).Format.Numbering);
        Assert.Null(((Paragraph)document.Blocks[1]).Format.Numbering);
        Assert.Equal(empty.Id, document.Blocks[1].Id);
        Assert.Equal(document.PositionOf(empty.Id, 0), command.After.End);
    }

    [Fact]
    public void Split_EmptyParagraphWithStyleNumbering_CancelsWithNumIdZero()
    {
        var document = WithBuiltInStyles(new Paragraph { Format = new ParagraphFormat { StyleId = "ListNumber" } });
        document.Styles.StyleList.Add(new DocumentStyle { Id = "ListNumber", Type = StyleType.Paragraph, ParagraphFormat = new ParagraphFormat { Numbering = new NumberingRef(3, 0) } });

        document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[0].Id, 0)));

        Assert.Equal(new NumberingRef(0, 0), ((Paragraph)Assert.Single(document.Blocks)).Format.Numbering);
    }

    [Fact]
    public void Split_SecondHalfHasNoSource()
    {
        var document = new DocxBuilder().Body("<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:t>abcd</w:t></w:r></w:p>").Load();
        var original = (Paragraph)document.Blocks[0];
        Assert.NotNull(original.Source);

        document.Execute(new SplitParagraphCommand(document.PositionOf(original.Id, 2)));

        var (first, second) = ((Paragraph)document.Blocks[0], (Paragraph)document.Blocks[1]);
        Assert.Same(original.Source, first.Source);
        Assert.Null(second.Source);
        Assert.Equal(original.Format, second.Format);
        Assert.Equal(["ab", "cd"], new[] { first.Text, second.Text });
        Assert.Same(original.Inlines[0].Source, second.Inlines[0].Source);
    }

    [Fact]
    public void Split_SectionEnd_MovesLastBlockId()
    {
        var document = WithSections(out var first, out _);

        document.Execute(new SplitParagraphCommand(document.PositionOf(first.Id, 1)));

        Assert.Equal(document.Blocks[1].Id, document.Sections[0].LastBlockId);
        Assert.Equal(PageSetup.Letter, document.Sections[0].Page);
        document.Undo.Undo();
        Assert.Equal(first.Id, document.Sections[0].LastBlockId);
    }

    [Fact]
    public void Merge_SectionEnd_RemovesSection_LaterSetupWins()
    {
        var document = WithSections(out var first, out var sections);
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        document.Execute(new MergeParagraphsCommand(first.Id));

        Assert.Equal("ab", ((Paragraph)Assert.Single(document.Blocks)).Text);
        var section = Assert.Single(document.Sections);
        Assert.Equal(PageSetup.A4, section.Page);
        Assert.Null(section.LastBlockId);
        Assert.True(events.Single().Kind.HasFlag(DocumentChangeKind.Sections));

        document.Undo.Undo();
        Assert.True(sections.SequenceEqual(document.Sections, ReferenceEqualityComparer.Instance));
    }

    [Fact]
    public void Merge_NextEndsSection_SectionEndMovesToMergedParagraph()
    {
        var document = TextDocument.FromBlocks([P("a"), P("b"), P("c")]);
        document.SectionList.AddRange([new Section { Id = "s1", LastBlockId = document.Blocks[1].Id, Page = PageSetup.Letter }, new Section { Id = "s2" }]);

        document.Execute(new MergeParagraphsCommand(document.Blocks[0].Id));

        Assert.Equal(document.Blocks[0].Id, document.Sections[0].LastBlockId);
        Assert.Equal(2, document.Sections.Count);
    }

    [Fact]
    public void Merge_BeforeTable_IsNoOp_NoUndoStep()
    {
        var document = Standard();
        var raised = 0;
        document.Changed += (_, _) => raised++;

        document.Execute(new MergeParagraphsCommand(document.Blocks[1].Id));
        document.Execute(new MergeParagraphsCommand(document.Blocks[^1].Id));

        Assert.False(document.Undo.CanUndo);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void InsertBreak_PageBreak_IsInline()
    {
        var document = TextDocument.FromBlocks([P("ab")]);
        var id = document.Blocks[0].Id;
        var command = new InsertBreakCommand(document.PositionOf(id, 1), BreakKind.Page);

        document.Execute(command);

        Assert.Equal("a\fb", ((Paragraph)document.Blocks[0]).Text);
        Assert.Equal(document.PositionOf(id, 2), command.After.Start);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///c:/windows")]
    [InlineData("data:text/html,x")]
    [InlineData("relative/path")]
    public void SetHyperlink_JavascriptScheme_Throws(string target)
    {
        var document = TextDocument.FromBlocks([P("abc")]);
        var id = document.Blocks[0].Id;

        Assert.Throws<ArgumentException>(() => new SetHyperlinkCommand(Range(document, id, 0, id, 3), target));
    }

    [Fact]
    public void SetHyperlink_SetsSharedLink_AnchorAndRemove()
    {
        var document = TextDocument.FromBlocks([P("abcdef")]);
        var id = document.Blocks[0].Id;

        document.Execute(new SetHyperlinkCommand(Range(document, id, 1, id, 4), "https://example.com"));
        var linked = (Paragraph)document.Blocks[0];
        Assert.Equal(["a", "bcd", "ef"], linked.Inlines.Select(i => ((TextRun)i).Text));
        Assert.Equal("https://example.com", linked.Inlines[1].Link.SafeTarget);

        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 2), "#Mark"));
        Assert.Equal("Mark", ((Paragraph)document.Blocks[0]).Inlines[0].Link.Anchor);

        document.Execute(new SetHyperlinkCommand(Range(document, id, 0, id, 6), null));
        Assert.All(((Paragraph)document.Blocks[0]).Inlines, i => Assert.Null(i.Link));
    }

    [Fact]
    public void ReplaceParagraphContent_UnchangedInlinesKeepSource()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>keep</w:t></w:r><w:r><w:t>edit</w:t></w:r></w:p>").Load();
        var paragraph = (Paragraph)document.Blocks[0];

        document.Execute(new ReplaceParagraphContentCommand(paragraph.Id, [paragraph.Inlines[0], new TextRun("edited")]));

        var edited = (Paragraph)document.Blocks[0];
        Assert.Same(paragraph.Inlines[0], edited.Inlines[0]);
        Assert.NotNull(edited.Inlines[0].Source);
        Assert.Equal("keepedited", edited.Text);
        Assert.Same(paragraph.Source, edited.Source);
    }

    [Fact]
    public void Typing_WithinWindow_OneUndoStep()
    {
        var document = TextDocument.FromBlocks([P("")]);
        var id = document.Blocks[0].Id;

        Type(document, id, "H", T0);
        Type(document, id, "He", T0.AddMilliseconds(400));
        Type(document, id, "Hel", T0.AddMilliseconds(1400));

        Assert.Equal("Hel", ((Paragraph)document.Blocks[0]).Text);
        Assert.Equal(1, Steps(document));
        Assert.Equal(TimeSpan.FromSeconds(1), document.TypingMergeWindow);
    }

    [Fact]
    public void Typing_PauseOverWindow_NewStep()
    {
        var document = TextDocument.FromBlocks([P("")]);
        var id = document.Blocks[0].Id;

        Type(document, id, "H", T0);
        Type(document, id, "He", T0.AddMilliseconds(1001));

        Assert.Equal(2, Steps(document));
    }

    [Fact]
    public void Typing_CommandBetween_NewStep()
    {
        var document = TextDocument.FromBlocks([P(""), P("other")]);
        var id = document.Blocks[0].Id;

        Type(document, id, "H", T0);
        document.Execute(new SetParagraphFormatCommand([document.Blocks[1].Id], f => f with { Alignment = ParagraphAlignment.Right }, "Right"));
        Type(document, id, "He", T0.AddMilliseconds(100));

        Assert.Equal(3, Steps(document));
    }

    [Fact]
    public void Typing_UndoBetween_NewStep()
    {
        var document = TextDocument.FromBlocks([P("")]);
        var id = document.Blocks[0].Id;

        Type(document, id, "H", T0);
        Type(document, id, "He", T0.AddMilliseconds(100));
        document.Undo.Undo();
        Type(document, id, "x", T0.AddMilliseconds(200));
        Type(document, id, "xy", T0.AddMilliseconds(300));

        Assert.Equal("xy", ((Paragraph)document.Blocks[0]).Text);
        Assert.Equal(1, Steps(document));
    }

    [Fact]
    public void Typing_EndTyping_NewStep()
    {
        var document = TextDocument.FromBlocks([P("")]);
        var id = document.Blocks[0].Id;

        Type(document, id, "H", T0);
        document.EndTyping();
        Type(document, id, "He", T0.AddMilliseconds(100));

        Assert.Equal(2, Steps(document));
    }

    [Fact]
    public void Typing_UndoRestoresStateBeforeFirstBatch()
    {
        var document = TextDocument.FromBlocks([P("a"), T(Row([P("b")])), P("c")]);
        var first = document.Blocks[0];
        var table = document.Blocks[1];
        var cell = document.AllParagraphs().Single(p => p.Text == "b");

        Type(document, first.Id, "a1", T0);
        Type(document, cell.Id, "b1", T0.AddMilliseconds(200));
        Type(document, first.Id, "a12", T0.AddMilliseconds(400));
        Type(document, cell.Id, "b12", T0.AddMilliseconds(600));
        var typed = DocumentDescriber.Describe(document);

        Assert.Equal(1, Steps(document));
        document.Undo.Undo();
        Assert.Same(first, document.Blocks[0]);
        Assert.Same(table, document.Blocks[1]);
        Assert.Same(cell, document.FindBlock(cell.Id));
        document.Undo.Redo();
        Assert.Equal(typed, DocumentDescriber.Describe(document));
        Assert.Equal("Typing", document.Undo.UndoDescription);
    }

    [Fact]
    public void Changed_TypingReportsOnlyThatParagraph()
    {
        var document = Standard();
        var target = document.Blocks[1];
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        Type(document, target.Id, "Second line!", T0);
        Type(document, target.Id, "Second line!!", T0.AddMilliseconds(100));

        Assert.Equal(2, events.Count);
        Assert.All(events, e =>
        {
            Assert.Equal([target.Id], e.ChangedBlockIds);
            Assert.Empty(e.RemovedBlockIds);
            Assert.Equal(DocumentChangeKind.Content, e.Kind);
        });
    }

    [Fact]
    public void Changed_TypingInNestedCell_ReportsOnlyItsTable()
    {
        var document = Standard();
        var table = document.Blocks[2];
        var target = document.AllParagraphs().Single(p => p.Text == "inner b");
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        Type(document, target.Id, "inner b!", T0);

        var args = Assert.Single(events);
        Assert.Equal([table.Id], args.ChangedBlockIds);
        Assert.Empty(args.RemovedBlockIds);
        Assert.Equal(DocumentChangeKind.Content, args.Kind);
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
