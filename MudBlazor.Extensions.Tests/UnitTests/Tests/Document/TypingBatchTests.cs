using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class TypingBatchTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Diff_RepeatedCharacter_AnchorsAtCaret()
    {
        Assert.Equal((0, 0, 1), TextDiff.Compute("aa", "aaa", 1));
        Assert.Equal((1, 1, 2), TextDiff.Compute("aa", "aaa", 2));
        Assert.Equal((2, 2, 3), TextDiff.Compute("aa", "aaa", 3));
        Assert.Equal((2, 2, 3), TextDiff.Compute("aa", "aaa", null));
    }

    [Fact]
    public void Diff_Deletion() => Assert.Equal((1, 2, 1), TextDiff.Compute("abc", "ac", 1));

    [Fact]
    public void Diff_Replacement() => Assert.Equal((1, 2, 2), TextDiff.Compute("hello", "hallo", 2));

    [Fact]
    public void Diff_SurrogatePair_NeverSplit() => Assert.Equal((1, 3, 3), TextDiff.Compute("a\uD83D\uDE00b", "a\uD83D\uDE01b", 3));

    [Fact]
    public void Typing_KeepsUntouchedRunsAndSources()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>keep</w:t></w:r><w:r><w:rPr><w:b/></w:rPr><w:t>bold</w:t></w:r></w:p>").Load();
        var paragraph = (Paragraph)document.Blocks[0];

        document.ApplyTyping([new TypedParagraph(paragraph.Id, "keepXbold", 5)], T0);

        var typed = (Paragraph)document.Blocks[0];
        Assert.Equal("keepX", Assert.IsType<TextRun>(typed.Inlines[0]).Text);
        Assert.NotNull(typed.Inlines[0].Source);
        Assert.Same(paragraph.Inlines[0].Source, typed.Inlines[0].Source);
        Assert.Same(paragraph.Inlines[1], typed.Inlines[1]);
    }

    [Fact]
    public void Typing_ExplicitFormat_AppliesToInsertedTextOnly()
    {
        var document = TextDocument.FromBlocks([P("ab")]);

        document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "aXb", 2, new RunFormat { Bold = true })], T0);

        var inlines = ((Paragraph)document.Blocks[0]).Inlines;
        Assert.Equal(["a", "X", "b"], inlines.Select(i => ((TextRun)i).Text));
        Assert.True(inlines[1].Format.Bold);
        Assert.Null(inlines[0].Format.Bold);
        Assert.Null(inlines[2].Format.Bold);
    }

    [Fact]
    public void Typing_DeleteAcrossBookmark_KeepsMarker()
    {
        var marker = new Marker("bookmarkStart");
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("Hel"), marker, new TextRun("lo")] }]);

        document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "Helo", 3)], T0);

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Same(marker, Assert.Single(paragraph.Inlines, i => i is Marker));
        Assert.Equal("Helo", paragraph.Text);
    }

    [Fact]
    public void Typing_RemovesNewlinesAndObjectCharactersFromInsertedText()
    {
        var document = TextDocument.FromBlocks([P("")]);

        document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "a\nb\r\uFFFCc")], T0);

        Assert.Equal("abc", ((Paragraph)document.Blocks[0]).Text);
    }

    [Fact]
    public void Typing_TabAndLineBreak_BecomeInlines()
    {
        var document = TextDocument.FromBlocks([P("")]);

        document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "a\tb\vc", 5)], T0);

        var inlines = ((Paragraph)document.Blocks[0]).Inlines;
        Assert.Collection(inlines,
            i => Assert.IsType<TextRun>(i),
            i => Assert.IsType<Tab>(i),
            i => Assert.IsType<TextRun>(i),
            i => Assert.Equal(BreakKind.Line, Assert.IsType<Break>(i).Kind),
            i => Assert.IsType<TextRun>(i));
    }

    [Fact]
    public void Typing_TwoParagraphsInOneTable_OneStepOneChangedEvent()
    {
        var document = TextDocument.FromBlocks([new Table { Grid = [2000, 2000], Rows = [new TableRow([new TableCell([P("a")]), new TableCell([P("b")])])] }]);
        var a = document.AllParagraphs().Single(p => p.Text == "a");
        var b = document.AllParagraphs().Single(p => p.Text == "b");
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        document.ApplyTyping([new TypedParagraph(a.Id, "a1", 2), new TypedParagraph(b.Id, "b1")], T0);

        var args = Assert.Single(events);
        Assert.Contains(a.Id, args.ChangedNestedBlockIds);
        Assert.Contains(b.Id, args.ChangedNestedBlockIds);
        Assert.Equal(1, Steps(document));
        document.Undo.Undo();
        Assert.Same(a, document.FindBlock(a.Id));
        Assert.Same(b, document.FindBlock(b.Id));
    }

    [Fact]
    public void Typing_BatchesMergeWithinWindowAndWithTheOldOverload()
    {
        var document = TextDocument.FromBlocks([P("")]);
        var id = document.Blocks[0].Id;

        document.ApplyTyping(id, [new TextRun("H")], T0);
        document.ApplyTyping([new TypedParagraph(id, "He", 2)], T0.AddMilliseconds(400));
        document.ApplyTyping([new TypedParagraph(id, "Hel", 3)], T0.AddMilliseconds(900));

        Assert.Equal("Hel", ((Paragraph)document.Blocks[0]).Text);
        Assert.Equal(1, Steps(document));
        Assert.Equal("Typing", document.Undo.UndoDescription);
        document.Undo.Undo();
        Assert.Equal("", ((Paragraph)document.Blocks[0]).Text);
    }

    [Fact]
    public void Typing_UnknownParagraph_ThrowsAndChangesNothing()
    {
        var document = TextDocument.FromBlocks([P("a")]);
        var before = DocumentDescriber.Describe(document);
        var events = 0;
        document.Changed += (_, _) => events++;

        Assert.Throws<ArgumentNullException>(() => document.ApplyTyping(null, T0));
        Assert.Throws<ArgumentException>(() => document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "ab", 2), new TypedParagraph("nope", "x")], T0));

        Assert.Equal(before, DocumentDescriber.Describe(document));
        Assert.Equal(0, events);
        Assert.False(document.Undo.CanUndo);
    }

    [Fact]
    public void Typing_UnchangedText_RecordsNoStepAndNoEvent()
    {
        var document = TextDocument.FromBlocks([P("same")]);
        var events = 0;
        document.Changed += (_, _) => events++;

        document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "same", 4)], T0);

        Assert.Equal(0, events);
        Assert.False(document.Undo.CanUndo);
    }

    [Fact]
    public void Diff_CaretAfterTheChange_NeverGrowsTheSpan()
    {
        var bold = new TextRun("b") { Format = new RunFormat { Bold = true } };
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("a"), bold] }]);

        Assert.Equal((0, 0, 1), TextDiff.Compute("ab", "Xab", 3));
        document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "Xab", 3)], T0);

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("Xab", paragraph.Text);
        Assert.Same(bold, paragraph.Inlines[^1]);
        Assert.Equal((0, 1, 0), TextDiff.Compute("abc", "bc", 2));
    }

    [Fact]
    public void Diff_CaretAfterTheChange_KeepsImage()
    {
        var image = new InlineImage("media/image1.png", 100, 100);
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("a"), image, new TextRun("b")] }]);

        Assert.Equal((0, 0, 1), TextDiff.Compute("a\uFFFCb", "Xa\uFFFCb", 4));
        document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "Xa\uFFFCb", 4)], T0);

        Assert.Same(image, ((Paragraph)document.Blocks[0]).Inlines.Single(i => i is InlineImage));
    }

    [Fact]
    public void Diff_Exhaustive_ValidMinimalSplits()
    {
        var alphabet = "ab\uD83D\uDE00\uFFFC";
        var strings = new List<string> { "" };
        for (var length = 1; length <= 3; length++)
            strings.AddRange(strings.Where(s => s.Length == length - 1).SelectMany(s => alphabet.Select(c => s + c)).ToList());

        foreach (var before in strings)
        foreach (var after in strings)
        foreach (var caret in Enumerable.Range(-1, after.Length + 3).Select(c => (int?)c).Append(null))
        {
            var (start, oldEnd, newEnd) = TextDiff.Compute(before, after, caret);
            Assert.True(start <= oldEnd && start <= newEnd && oldEnd <= before.Length && newEnd <= after.Length, $"{before}|{after}|{caret}");
            Assert.Equal(after, before[..start] + after[start..newEnd] + before[oldEnd..]);
            Assert.False(Splits(before, start) || Splits(after, start) || Splits(before, oldEnd) || Splits(after, newEnd), $"{before}|{after}|{caret}");
        }

        static bool Splits(string text, int offset) => offset > 0 && offset < text.Length && char.IsHighSurrogate(text[offset - 1]) && char.IsLowSurrogate(text[offset]);
    }

    [Fact]
    public void Typing_ObjectInsideReplacedSpan_KeepsInstance()
    {
        var image = new InlineImage("media/image1.png", 100, 100);
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("a"), image, new TextRun("b")] }]);

        var rerender = document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "Xa\uFFFCbY", 5)], T0);

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("Xa\uFFFCbY", paragraph.Text);
        Assert.Same(image, paragraph.Inlines.Single(i => i is InlineImage));
        Assert.Empty(rerender);
    }

    [Fact]
    public void Typing_ColumnBreakInsideReplacedSpan_KeepsInstance()
    {
        var column = new Break(BreakKind.Column);
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("a"), column, new TextRun("b")] }]);

        document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "Xa\fbY", 5)], T0);

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("Xa\fbY", paragraph.Text);
        Assert.Same(column, paragraph.Inlines.Single(i => i is Break));
        Assert.Equal(BreakKind.Column, column.Kind);
    }

    [Fact]
    public void Typing_FilteredCharacters_ReportedForRerenderWithoutStep()
    {
        var document = TextDocument.FromBlocks([P("ab")]);
        var id = document.Blocks[0].Id;
        var events = 0;
        document.Changed += (_, _) => events++;

        var rerender = document.ApplyTyping([new TypedParagraph(id, "\uFFFCab", 1)], T0);

        Assert.Equal([id], rerender);
        Assert.Equal(0, events);
        Assert.False(document.Undo.CanUndo);
        Assert.Empty(document.ApplyTyping([new TypedParagraph(id, "abc", 3)], T0));
    }

    [Fact]
    public void Typing_DuplicateParagraph_ThrowsAndChangesNothing()
    {
        var document = TextDocument.FromBlocks([P("a")]);
        var id = document.Blocks[0].Id;
        var before = DocumentDescriber.Describe(document);

        Assert.Throws<ArgumentException>(() => document.ApplyTyping([new TypedParagraph(id, "ab", 2), new TypedParagraph(id, "abc", 3)], T0));

        Assert.Equal(before, DocumentDescriber.Describe(document));
        Assert.False(document.Undo.CanUndo);
    }

    [Fact]
    public void Typing_After_IsTheCaretMappedThroughFiltering()
    {
        var document = TextDocument.FromBlocks([P("")]);
        var id = document.Blocks[0].Id;
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        document.ApplyTyping([new TypedParagraph(id, "X\uFFFCY", 2)], T0);
        document.EndTyping();
        document.ApplyTyping([new TypedParagraph(id, "XYZ", 3)], T0);

        Assert.Equal("XYZ", ((Paragraph)document.Blocks[0]).Text);
        Assert.Equal(document.PositionOf(id, 1), events[0].Selection?.Start);
        Assert.Equal(new DocumentRange(document.PositionOf(id, 3), document.PositionOf(id, 3)), events[1].Selection);
    }

    private static Paragraph P(string text) => new() { Inlines = [new TextRun(text)] };

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
}
