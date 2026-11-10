namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Undo and redo history of a workbook. Each user action is exactly one step; transactions group commands.</summary>
public sealed class UndoStack
{
    private readonly Workbook _wb;
    private readonly List<IWorkbookCommand> _undo = new();
    private readonly List<IWorkbookCommand> _redo = new();
    private List<IWorkbookCommand> _transaction;
    private string _transactionDescription;
    private int _transactionDepth;

    internal UndoStack(Workbook wb) => _wb = wb;

    /// <summary>Maximum number of undo steps kept; the oldest are dropped first.</summary>
    public int MaxDepth { get; set; } = 100;

    /// <summary>True if a step can be undone (never while a transaction is open).</summary>
    public bool CanUndo => _transactionDepth == 0 && _undo.Count > 0;

    /// <summary>True if a step can be redone (never while a transaction is open).</summary>
    public bool CanRedo => _transactionDepth == 0 && _redo.Count > 0;

    /// <summary>Description of the step that would be undone, or null.</summary>
    public string UndoDescription => CanUndo ? _undo[^1].Description : null;

    /// <summary>Description of the step that would be redone, or null.</summary>
    public string RedoDescription => CanRedo ? _redo[^1].Description : null;

    /// <summary>Undoes the latest step.</summary>
    public void Undo()
    {
        if (!CanUndo) throw new InvalidOperationException("Nothing to undo.");
        var cmd = _undo[^1];
        Run(() => cmd.Revert(_wb));
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(cmd);
    }

    /// <summary>Redoes the latest undone step.</summary>
    public void Redo()
    {
        if (!CanRedo) throw new InvalidOperationException("Nothing to redo.");
        var cmd = _redo[^1];
        Run(() => cmd.Apply(_wb));
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(cmd);
    }

    /// <summary>Drops the whole history, for example after loading a file.</summary>
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    /// <summary>
    /// Groups all commands executed until the returned scope is disposed into one undo step and one
    /// <see cref="Workbook.Changed"/> event. Nested transactions merge into the outermost one; empty ones are dropped.
    /// </summary>
    /// <param name="description">Description of the combined step.</param>
    /// <returns>The scope to dispose to end the transaction.</returns>
    public IDisposable BeginTransaction(string description)
    {
        if (_transactionDepth++ == 0)
        {
            _transaction = new List<IWorkbookCommand>();
            _transactionDescription = description;
        }

        _wb.BeginBatch();
        return new Scope(this);
    }

    internal void Record(IWorkbookCommand cmd)
    {
        _redo.Clear();
        if (_transaction != null)
        {
            _transaction.Add(cmd);
            return;
        }

        _undo.Add(cmd);
        if (_undo.Count > MaxDepth) _undo.RemoveRange(0, _undo.Count - MaxDepth);
    }

    private void Run(Action action)
    {
        _wb.BeginBatch();
        try
        {
            action();
        }
        finally
        {
            _wb.EndBatch();
        }
    }

    private void EndTransaction()
    {
        if (--_transactionDepth == 0)
        {
            var commands = _transaction;
            _transaction = null;
            if (commands.Count > 0) Record(new CompositeCommand(_transactionDescription, commands));
        }

        _wb.EndBatch();
    }

    private sealed class Scope(UndoStack owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            owner.EndTransaction();
        }
    }

    internal sealed class CompositeCommand(string description, IReadOnlyList<IWorkbookCommand> commands) : IWorkbookCommand
    {
        public string Description => description;

        public void Apply(Workbook wb)
        {
            foreach (var cmd in commands) cmd.Apply(wb);
        }

        public void Revert(Workbook wb)
        {
            for (var i = commands.Count - 1; i >= 0; i--) commands[i].Revert(wb);
        }
    }
}
