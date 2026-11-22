using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public partial class TextCommandTests
{
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
    public void Split_SecondHalfInheritsSource()
    {
        var document = new DocxBuilder().Body("<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr><w:r><w:t>abcd</w:t></w:r></w:p>").Load();
        var original = (Paragraph)document.Blocks[0];
        Assert.NotNull(original.Source);

        document.Execute(new SplitParagraphCommand(document.PositionOf(original.Id, 2)));

        var (first, second) = ((Paragraph)document.Blocks[0], (Paragraph)document.Blocks[1]);
        Assert.Same(original.Source, first.Source);
        Assert.Same(original.Source, second.Source);
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
}
