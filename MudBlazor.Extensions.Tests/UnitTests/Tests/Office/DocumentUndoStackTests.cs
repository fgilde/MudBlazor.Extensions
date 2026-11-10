using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Office;

public class DocumentUndoStackTests
{
    private sealed class Doc
    {
        public List<string> Items { get; } = [];
    }

    private sealed class Add(string item) : IDocumentCommand<Doc>
    {
        public string Description => "Add " + item;

        public void Apply(Doc document) => document.Items.Add(item);

        public void Revert(Doc document) => document.Items.Remove(item);
    }

    private static void Run(DocumentUndoStack<Doc> stack, Doc doc, string item)
    {
        var command = new Add(item);
        command.Apply(doc);
        stack.Record(command);
    }

    [Fact]
    public void UndoStack_TransactionNested_IsOneStep()
    {
        var doc = new Doc();
        var stack = new DocumentUndoStack<Doc>(doc);
        using (stack.BeginTransaction("outer"))
        {
            Run(stack, doc, "a");
            using (stack.BeginTransaction("inner")) Run(stack, doc, "b");
            Run(stack, doc, "c");
        }

        Assert.Equal("outer", stack.UndoDescription);
        stack.Undo();
        Assert.Empty(doc.Items);
        Assert.False(stack.CanUndo);
        stack.Redo();
        Assert.Equal(["a", "b", "c"], doc.Items);
    }

    [Fact]
    public void UndoStack_EmptyTransaction_RecordsNothing()
    {
        var stack = new DocumentUndoStack<Doc>(new Doc());
        using (stack.BeginTransaction("nothing")) { }

        Assert.False(stack.CanUndo);
        Assert.Null(stack.UndoDescription);
    }

    [Fact]
    public void UndoStack_MaxDepth_DropsOldest()
    {
        var doc = new Doc();
        var stack = new DocumentUndoStack<Doc>(doc) { MaxDepth = 2 };
        foreach (var item in new[] { "a", "b", "c" }) Run(stack, doc, item);

        stack.Undo();
        stack.Undo();
        Assert.False(stack.CanUndo);
        Assert.Equal(["a"], doc.Items);
    }

    [Fact]
    public void UndoStack_RedoClearedByNewCommand()
    {
        var doc = new Doc();
        var stack = new DocumentUndoStack<Doc>(doc);
        Run(stack, doc, "a");
        stack.Undo();
        Assert.True(stack.CanRedo);

        Run(stack, doc, "b");
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void UndoStack_OnTransactionEnd_RunsOncePerOutermostScope()
    {
        var doc = new Doc();
        var ends = 0;
        var stack = new DocumentUndoStack<Doc>(doc, _ => ends++);
        using (stack.BeginTransaction("outer"))
        {
            using (stack.BeginTransaction("inner")) Run(stack, doc, "a");
            Run(stack, doc, "b");
            Assert.Equal(0, ends);
        }

        Assert.Equal(1, ends);
    }
}
