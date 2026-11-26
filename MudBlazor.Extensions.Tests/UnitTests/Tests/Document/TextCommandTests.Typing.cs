using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public partial class TextCommandTests
{
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
}
