using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class UndoSelectionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Execute_ReportsAfter()
    {
        var (document, p1, _) = Create();
        var events = Record(document);

        document.Execute(new ReplaceTextCommand(Caret(document, p1, 2), "X"));

        Assert.Equal(Caret(document, p1, 3), Assert.Single(events).Selection);
    }

    [Fact]
    public void Undo_ReportsSelectionPassedToExecute()
    {
        var (document, p1, _) = Create();
        var selection = Range(document, p1, 1, p1, 4);
        document.Execute(new ReplaceTextCommand(selection, "X"), selection);
        var events = Record(document);

        document.Undo.Undo();

        Assert.Equal("Hello", ((Paragraph)document.FindBlock(p1)).Text);
        Assert.Equal(selection, Assert.Single(events).Selection);
    }

    [Fact]
    public void Redo_ReportsAfter()
    {
        var (document, p1, _) = Create();
        document.Execute(new ReplaceTextCommand(Range(document, p1, 1, p1, 4), "XY"), Range(document, p1, 1, p1, 4));
        document.Undo.Undo();
        var events = Record(document);

        document.Undo.Redo();

        Assert.Equal(Caret(document, p1, 3), Assert.Single(events).Selection);
    }

    [Fact]
    public void Transaction_UndoReportsFirstBefore_RedoReportsLastAfter()
    {
        var (document, p1, p2) = Create();
        var s1 = Range(document, p1, 0, p1, 2);
        var s2 = Range(document, p2, 1, p2, 1);
        var second = new ReplaceTextCommand(s2, "Z");
        using (document.Undo.BeginTransaction("Both"))
        {
            document.Execute(new ReplaceTextCommand(s1, "A"), s1);
            document.Execute(second, s2);
        }

        var events = Record(document);
        document.Undo.Undo();
        document.Undo.Redo();

        Assert.Equal(2, events.Count);
        Assert.Equal(s1, events[0].Selection);
        Assert.Equal(Caret(document, p2, 2), events[1].Selection);
        Assert.Equal(second.After, events[1].Selection);
    }

    [Fact]
    public void TypingBatches_UndoReportsCaretWhereTypingStarted()
    {
        var (document, p1, _) = Create();
        document.ApplyTyping([new TypedParagraph(p1, "HelloA", 6)], T0);
        document.ApplyTyping([new TypedParagraph(p1, "HelloAB", 7)], T0.AddMilliseconds(200));
        document.ApplyTyping([new TypedParagraph(p1, "HelloABC", 8)], T0.AddMilliseconds(400));
        var events = Record(document);

        document.Undo.Undo();

        Assert.False(document.Undo.CanUndo);
        Assert.Equal("Hello", ((Paragraph)document.FindBlock(p1)).Text);
        Assert.Equal(Caret(document, p1, 5), Assert.Single(events).Selection);

        document.Undo.Redo();

        Assert.Equal("HelloABC", ((Paragraph)document.FindBlock(p1)).Text);
        Assert.Equal(Caret(document, p1, 8), events[1].Selection);
    }

    [Fact]
    public void ExecuteAfterUndo_ReportsTheNewCommandsAfter()
    {
        var (document, p1, p2) = Create();
        document.Execute(new ReplaceTextCommand(Caret(document, p1, 2), "X"), Caret(document, p1, 2));
        document.Undo.Undo();
        var events = Record(document);

        document.Execute(new ReplaceTextCommand(Caret(document, p2, 1), "YZ"), Caret(document, p2, 1));

        Assert.False(document.Undo.CanRedo);
        Assert.Equal(Caret(document, p2, 3), Assert.Single(events).Selection);
    }

    [Fact]
    public void NoSelection_UndoReportsNull()
    {
        var (document, p1, _) = Create();
        document.Execute(new ReplaceTextCommand(Caret(document, p1, 2), "X"));
        var events = Record(document);

        document.Undo.Undo();

        Assert.Null(Assert.Single(events).Selection);
    }

    private static (TextDocument Document, string P1, string P2) Create()
    {
        var document = TextDocument.FromBlocks([P("Hello"), P("World")]);
        return (document, document.Blocks[0].Id, document.Blocks[1].Id);
    }

    private static List<TextDocumentChangedEventArgs> Record(TextDocument document)
    {
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);
        return events;
    }

    private static DocumentRange Caret(TextDocument document, string id, int offset) => Range(document, id, offset, id, offset);

    private static DocumentRange Range(TextDocument document, string startId, int start, string endId, int end) =>
        new(document.PositionOf(startId, start), document.PositionOf(endId, end));

    private static Paragraph P(string text) => new() { Inlines = [new TextRun(text)] };
}
