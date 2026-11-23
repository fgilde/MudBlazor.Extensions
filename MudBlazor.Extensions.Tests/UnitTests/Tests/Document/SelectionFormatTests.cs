using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class SelectionFormatTests
{
    private static readonly RunFormat Bold = new() { Bold = true, Font = "Arial" };

    private static Paragraph P(params Inline[] inlines) => new() { Inlines = inlines };

    private static TextRun R(string text, RunFormat format = null) => new(text) { Format = format ?? new RunFormat() };

    private static DocumentRange At(TextDocument d, int p1, int o1, int p2, int o2) =>
        new(d.PositionOf(d.Blocks[p1].Id, o1), d.PositionOf(d.Blocks[p2].Id, o2));

    private static DocxBuilder Styled() => new DocxBuilder().Styles(
        "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>" +
        "<w:style w:type=\"paragraph\" w:styleId=\"Heading1\"><w:name w:val=\"heading 1\"/><w:basedOn w:val=\"Normal\"/></w:style>");

    [Fact]
    public void Caret_AfterBoldCharacter_IsBold()
    {
        var d = TextDocument.FromBlocks([P(R("ab", Bold), R("cd"))]);
        Assert.True(d.FormatOf(At(d, 0, 1, 0, 1)).Run.Bold);
        Assert.True(d.FormatOf(At(d, 0, 2, 0, 2)).Run.Bold);
        Assert.False(d.FormatOf(At(d, 0, 3, 0, 3)).Run.Bold);
    }

    [Fact]
    public void Caret_AtParagraphStart_TakesFollowingRun()
    {
        var d = TextDocument.FromBlocks([P(R("ab", Bold))]);
        Assert.True(d.FormatOf(At(d, 0, 0, 0, 0)).Run.Bold);
    }

    [Fact]
    public void Caret_InEmptyParagraph_TakesMarkFormat()
    {
        var d = TextDocument.FromBlocks([new Paragraph { MarkFormat = new RunFormat { SizePt = 16 } }]);
        Assert.Equal(16, d.FormatOf(At(d, 0, 0, 0, 0)).Run.SizePt);
    }

    [Fact]
    public void Range_HalfBold_BoldIsNull_FontKept()
    {
        var d = TextDocument.FromBlocks([P(R("ab", Bold), R("cd", new RunFormat { Font = "Arial" }))]);
        var format = d.FormatOf(At(d, 0, 0, 0, 4)).Run;
        Assert.Null(format.Bold);
        Assert.Equal("Arial", format.Font);
    }

    [Fact]
    public void Range_Reversed_SameAsForward()
    {
        var d = TextDocument.FromBlocks([P(R("ab", Bold)), P(R("cd"))]);
        var forward = d.FormatOf(At(d, 0, 0, 1, 2));
        var reversed = d.FormatOf(At(d, 1, 2, 0, 0));
        Assert.Equal(forward, reversed);
        Assert.Null(forward.Run.Bold);
    }

    [Fact]
    public void Range_TwoAlignments_AlignmentNull()
    {
        var d = TextDocument.FromBlocks([
            new Paragraph { Inlines = [R("a")], Format = new ParagraphFormat { Alignment = ParagraphAlignment.Center } },
            new Paragraph { Inlines = [R("b")], Format = new ParagraphFormat { Alignment = ParagraphAlignment.Right } }]);
        Assert.Null(d.FormatOf(At(d, 0, 0, 1, 1)).Paragraph.Alignment);
        Assert.Equal(ParagraphAlignment.Center, d.FormatOf(At(d, 0, 0, 0, 1)).Paragraph.Alignment);
    }

    [Fact]
    public void Range_Heading1AndNormal_StyleIdNull()
    {
        var d = Styled().Body("<w:p><w:pPr><w:pStyle w:val=\"Heading1\"/></w:pPr><w:r><w:t>a</w:t></w:r></w:p><w:p><w:r><w:t>b</w:t></w:r></w:p>").Load();
        Assert.Null(d.FormatOf(At(d, 0, 0, 1, 1)).StyleId);
        Assert.Equal("Heading1", d.FormatOf(At(d, 0, 0, 0, 1)).StyleId);
        Assert.Equal("Normal", d.FormatOf(At(d, 1, 0, 1, 1)).StyleId);
    }

    [Fact]
    public void Range_AllBullets_ListIsBullet()
    {
        var d = TextDocument.FromBlocks([P(R("a")), P(R("b")), P(R("c"))]);
        d.Execute(new ApplyListCommand([d.Blocks[0].Id, d.Blocks[1].Id], ListTemplate.Bullet));
        Assert.Equal(ListTemplate.Bullet, d.FormatOf(At(d, 0, 0, 1, 1)).List);
        Assert.Null(d.FormatOf(At(d, 0, 0, 2, 1)).List);
        d.Execute(new ApplyListCommand([d.Blocks[2].Id], ListTemplate.Decimal));
        Assert.Null(d.FormatOf(At(d, 0, 0, 2, 1)).List);
        Assert.Equal(ListTemplate.Decimal, d.FormatOf(At(d, 2, 0, 2, 1)).List);
    }

    [Fact]
    public void Range_OverImageOnly_FallsBackToCaretRuleAtStart()
    {
        var italic = new RunFormat { Italic = true };
        var d = TextDocument.FromBlocks([P(R("x", new RunFormat { Underline = "single" }), new InlineImage("media/a.png", 10, 10) { Format = italic }, R("a", Bold))]);
        var format = d.FormatOf(At(d, 0, 1, 0, 2)).Run;
        Assert.Equal("single", format.Underline);
        Assert.False(format.Italic);
        Assert.False(format.Bold);
        var text = d.FormatOf(At(d, 0, 0, 0, 3)).Run;
        Assert.Null(text.Underline);
        Assert.False(text.Italic);
    }

    [Fact]
    public void Range_ImageBetweenText_IgnoredInMerge()
    {
        var d = TextDocument.FromBlocks([P(R("a", Bold), new InlineImage("media/a.png", 10, 10) { Format = new RunFormat { Italic = true } }, R("b", Bold))]);
        var format = d.FormatOf(At(d, 0, 0, 0, 3)).Run;
        Assert.True(format.Bold);
        Assert.False(format.Italic);
    }

    [Fact]
    public void Caret_RightAfterImage_TakesImageFormat()
    {
        var d = TextDocument.FromBlocks([P(R("a"), new InlineImage("media/a.png", 10, 10) { Format = new RunFormat { Italic = true } }, R("b", Bold))]);
        var format = d.FormatOf(At(d, 0, 2, 0, 2)).Run;
        Assert.True(format.Italic);
        Assert.False(format.Bold);
    }

    [Fact]
    public void Range_ZeroLengthOverlapAtEnds_NotCounted()
    {
        var d = TextDocument.FromBlocks([P(R("ab", Bold), R("cd"))]);
        Assert.False(d.FormatOf(At(d, 0, 2, 0, 4)).Run.Bold);
        Assert.True(d.FormatOf(At(d, 0, 0, 0, 2)).Run.Bold);
    }

    [Fact]
    public void Range_Tab_Counts()
    {
        var d = TextDocument.FromBlocks([P(R("a", Bold), new Tab { Format = new RunFormat { Bold = false } }, R("b", Bold))]);
        Assert.Null(d.FormatOf(At(d, 0, 0, 0, 3)).Run.Bold);
        Assert.False(d.FormatOf(At(d, 0, 1, 0, 2)).Run.Bold);
    }

    [Fact]
    public void Range_EndingAtStartOfNextParagraph_IgnoresThatParagraph()
    {
        var d = Styled().Body("<w:p><w:pPr><w:pStyle w:val=\"Heading1\"/><w:jc w:val=\"center\"/></w:pPr><w:r><w:t>a</w:t></w:r></w:p><w:p><w:pPr><w:jc w:val=\"left\"/></w:pPr><w:r><w:t>b</w:t></w:r></w:p>").Load();
        d.Execute(new ApplyListCommand([d.Blocks[0].Id], ListTemplate.Bullet));
        var format = d.FormatOf(At(d, 0, 0, 1, 0));
        Assert.Equal("Heading1", format.StyleId);
        Assert.Equal(ParagraphAlignment.Center, format.Paragraph.Alignment);
        Assert.Equal(ListTemplate.Bullet, format.List);
        Assert.Equal(format, d.FormatOf(At(d, 1, 0, 0, 0)));
    }

    [Fact]
    public void Caret_InTableCell_TakesCellFormat()
    {
        var table = new Table { Grid = [2000], Rows = [new TableRow([new TableCell([P(R("a", Bold))])])] };
        var d = TextDocument.FromBlocks([P(R("x")), table]);
        var cell = d.AllParagraphs().Single(p => p.Text == "a");
        Assert.True(d.FormatOf(new DocumentRange(d.PositionOf(cell.Id, 1), d.PositionOf(cell.Id, 1))).Run.Bold);
    }

    [Fact]
    public void Range_AcrossNestedTable_Merges()
    {
        var inner = new Table { Grid = [2000], Rows = [new TableRow([new TableCell([P(R("in", Bold))])])] };
        var outer = new Table { Grid = [2000], Rows = [new TableRow([new TableCell([P(R("out", Bold)), inner])])] };
        var d = TextDocument.FromBlocks([P(R("x", Bold)), outer, P(R("y", Bold))]);
        var all = d.AllParagraphs().ToList();
        var range = new DocumentRange(d.PositionOf(all[0].Id, 0), d.PositionOf(all[^1].Id, 1));
        Assert.True(d.FormatOf(range).Run.Bold);
        var cells = new DocumentRange(d.PositionOf(all[1].Id, 0), d.PositionOf(all[2].Id, 2));
        Assert.True(d.FormatOf(cells).Run.Bold);
    }

    [Fact]
    public void CaretLateInLargeDocument_VisitsFewParagraphs()
    {
        var d = TextDocument.FromBlocks(Enumerable.Range(0, 10000).Select(i => (Block)P(R("p" + i))));
        var before = d.ParagraphVisits;
        d.FormatOf(At(d, 9990, 1, 9990, 1));
        Assert.InRange(d.ParagraphVisits - before, 1, 99);
    }

    [Fact]
    public void Range_AcrossTableCells_Merges()
    {
        var table = new Table { Grid = [2000, 2000], Rows = [new TableRow([new TableCell([P(R("a", Bold))]), new TableCell([P(R("b"))])])] };
        var d = TextDocument.FromBlocks([P(R("x", Bold)), table]);
        var inner = d.AllParagraphs().Where(p => p.Text is "a" or "b").ToList();
        var range = new DocumentRange(d.PositionOf(inner[0].Id, 0), d.PositionOf(inner[1].Id, 1));
        Assert.Null(d.FormatOf(range).Run.Bold);
    }
}
