namespace MudBlazor.Extensions.Office;

/// <summary>Undo and redo history of a document. Each user action is exactly one step; transactions group commands.</summary>
/// <typeparam name="TDocument">The document type the commands edit.</typeparam>
public sealed class DocumentUndoStack<TDocument>
{
    private readonly TDocument _document;
    private readonly Action<TDocument> _onTransactionEnd;
    private readonly List<IDocumentCommand<TDocument>> _undo = new();
    private readonly List<IDocumentCommand<TDocument>> _redo = new();
    private List<IDocumentCommand<TDocument>> _transaction;
    private string _transactionDescription;
    private int _transactionDepth;

    internal DocumentUndoStack(TDocument document, Action<TDocument> onTransactionEnd = null)
    {
        _document = document;
        _onTransactionEnd = onTransactionEnd;
    }

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

    internal bool InTransaction => _transaction != null;

    /// <summary>Undoes the latest step.</summary>
    public void Undo()
    {
        if (!CanUndo) throw new InvalidOperationException("Nothing to undo.");
        var command = _undo[^1];
        command.Revert(_document);
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(command);
    }

    /// <summary>Redoes the latest undone step.</summary>
    public void Redo()
    {
        if (!CanRedo) throw new InvalidOperationException("Nothing to redo.");
        var command = _redo[^1];
        command.Apply(_document);
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(command);
    }

    /// <summary>Drops the whole history, for example after loading a file.</summary>
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    /// <summary>
    /// Groups all commands recorded until the returned scope is disposed into one undo step. Nested transactions merge into the
    /// outermost one; empty ones are dropped.
    /// </summary>
    /// <param name="description">Description of the combined step.</param>
    /// <returns>The scope to dispose to end the transaction.</returns>
    public IDisposable BeginTransaction(string description)
    {
        if (_transactionDepth++ == 0)
        {
            _transaction = new List<IDocumentCommand<TDocument>>();
            _transactionDescription = description;
        }

        return new Scope(this);
    }

    internal void Record(IDocumentCommand<TDocument> command)
    {
        _redo.Clear();
        if (_transaction != null)
        {
            _transaction.Add(command);
            return;
        }

        _undo.Add(command);
        if (_undo.Count > MaxDepth) _undo.RemoveRange(0, _undo.Count - MaxDepth);
    }

    private void EndTransaction()
    {
        if (--_transactionDepth != 0) return;
        var commands = _transaction;
        _transaction = null;
        if (commands.Count == 0) return;
        Record(new CompositeCommand(_transactionDescription, commands));
        _onTransactionEnd?.Invoke(_document);
    }

    private sealed class Scope(DocumentUndoStack<TDocument> owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            owner.EndTransaction();
        }
    }

    private sealed class CompositeCommand(string description, IReadOnlyList<IDocumentCommand<TDocument>> commands) : IDocumentCommand<TDocument>
    {
        public string Description => description;

        public void Apply(TDocument document)
        {
            foreach (var command in commands) command.Apply(document);
        }

        public void Revert(TDocument document)
        {
            for (var i = commands.Count - 1; i >= 0; i--) commands[i].Revert(document);
        }
    }
}
