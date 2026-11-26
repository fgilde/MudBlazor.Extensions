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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UndoStack_MaxDepthBelowOne_Throws(int depth)
    {
        var stack = new DocumentUndoStack<Doc>(new Doc());

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => stack.MaxDepth = depth);
        Assert.Equal("value", error.ParamName);
        Assert.Contains("at least 1", error.Message);
        Assert.Equal(100, stack.MaxDepth);
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
    public void UndoStack_TryAmendLast_RefusedWithRedoOrOpenTransaction()
    {
        var doc = new Doc();
        var stack = new DocumentUndoStack<Doc>(doc);
        var calls = 0;
        bool Amend(IDocumentCommand<Doc> command)
        {
            calls++;
            return command.Description == "Add b";
        }

        Assert.False(stack.TryAmendLast(Amend));
        Run(stack, doc, "a");
        Assert.False(stack.TryAmendLast(Amend));
        Run(stack, doc, "b");
        Assert.True(stack.TryAmendLast(Amend));
        Assert.Equal(2, calls);

        using (stack.BeginTransaction("open"))
        {
            Assert.False(stack.TryAmendLast(Amend));
        }

        stack.Undo();
        Assert.False(stack.TryAmendLast(Amend));
        Assert.Equal(2, calls);
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

    [Fact]
    public void UndoStack_LastStep_TracksUndoRedoAndTransactions()
    {
        var doc = new Doc();
        var seen = new List<(IDocumentCommand<Doc> Step, bool Reverted)>();
        DocumentUndoStack<Doc> stack = null;
        stack = new DocumentUndoStack<Doc>(doc, _ => seen.Add((stack.LastStep, stack.LastStepReverted)));
        Assert.Null(stack.LastStep);

        var a = new Add("a");
        a.Apply(doc);
        stack.Record(a);
        Assert.Same(a, stack.LastStep);
        Assert.False(stack.LastStepReverted);
        Assert.Equal([a], DocumentUndoStack<Doc>.Flatten(a));

        stack.Undo();
        Assert.Same(a, seen[^1].Step);
        Assert.True(seen[^1].Reverted);
        stack.Redo();
        Assert.Same(a, seen[^1].Step);
        Assert.False(seen[^1].Reverted);

        var (b, c) = (new Add("b"), new Add("c"));
        using (stack.BeginTransaction("bc"))
        {
            b.Apply(doc);
            stack.Record(b);
            using (stack.BeginTransaction("inner"))
            {
                c.Apply(doc);
                stack.Record(c);
            }

            Assert.Same(a, stack.LastStep);
        }

        var (step, reverted) = seen[^1];
        Assert.Equal(3, seen.Count);
        Assert.Equal("bc", step.Description);
        Assert.False(reverted);
        Assert.Equal([b, c], DocumentUndoStack<Doc>.Flatten(step));

        stack.Undo();
        Assert.Same(step, seen[^1].Step);
        Assert.True(seen[^1].Reverted);
        Assert.Equal(["a"], doc.Items);
    }
}
