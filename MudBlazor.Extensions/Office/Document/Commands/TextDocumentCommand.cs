namespace MudBlazor.Extensions.Office.Document;

/// <summary>
/// Base of the commands that edit a <see cref="TextDocument"/>. The first apply computes the change; redo replays it and revert restores the
/// original block instances.
/// </summary>
public abstract class TextDocumentCommand : IDocumentCommand<TextDocument>
{
    private IReadOnlyList<BlockEdit> _edits;
    private Func<TextDocument, DocumentRange> _after;

    private protected TextDocumentCommand()
    {
    }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <summary>The caret or selection after the command was applied; <c>default</c> before that and for commands that leave the selection as it is.</summary>
    public DocumentRange After { get; private set; }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">A position, range or id of the command does not belong to the document.</exception>
    /// <exception cref="InvalidOperationException">The document changed since the command was applied before.</exception>
    public void Apply(TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_edits == null)
        {
            var (edits, after) = Compute(document);
            document.ApplyEdits(edits, true);
            (_edits, _after) = (edits, after);
        }
        else
            document.ApplyEdits(_edits, true);

        After = _after?.Invoke(document) ?? default;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The command was not applied or the document changed since.</exception>
    public void Revert(TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.ApplyEdits(_edits ?? throw new InvalidOperationException("The command was not applied."), false);
    }

    internal void Absorb(TextDocumentCommand later)
    {
        var edits = _edits.ToList();
        foreach (var edit in later._edits)
        {
            var index = edits.FindIndex(e => e.Container == edit.Container && e.Index == edit.Index);
            if (index >= 0)
                edits[index] = edits[index] with { After = edit.After };
            else
                edits.Add(edit);
        }

        (_edits, _after, After) = (edits, later._after, later.After);
    }

    private protected abstract (IReadOnlyList<BlockEdit> Edits, Func<TextDocument, DocumentRange> After) Compute(TextDocument document);

    private protected static DocumentRange Caret(TextDocument document, string paragraphId, int offset)
    {
        var position = document.PositionOf(paragraphId, offset);
        return new DocumentRange(position, position);
    }
}
