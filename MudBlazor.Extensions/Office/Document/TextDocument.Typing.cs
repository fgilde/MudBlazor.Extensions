namespace MudBlazor.Extensions.Office.Document;

public sealed partial class TextDocument
{
    private (TextDocumentCommand Step, int Version, DateTimeOffset At)? _typing;

    /// <summary>The longest pause between two typing batches that still merge into one undo step; one second by default.</summary>
    public TimeSpan TypingMergeWindow { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Applies a batch of typing: replaces the inlines of a paragraph. The batch merges into the previous undo step if that step is typing, no other
    /// command, undo or redo ran in between, <see cref="EndTyping"/> was not called and at most <see cref="TypingMergeWindow"/> passed. A merged step
    /// undoes to the state before its first batch.
    /// </summary>
    /// <param name="paragraphId">The id of the paragraph.</param>
    /// <param name="inlines">The new inlines; instances passed back unchanged keep their origin in the file.</param>
    /// <param name="at">When the batch was typed.</param>
    /// <exception cref="ArgumentException">The document has no paragraph with this id.</exception>
    public void ApplyTyping(string paragraphId, IReadOnlyList<Inline> inlines, DateTimeOffset at) =>
        Type(new ReplaceParagraphContentCommand(paragraphId, inlines) { Typing = true }, at);

    /// <summary>
    /// Applies a batch of typing given as the new text of paragraphs: only the changed span of each paragraph is replaced (found from the old text,
    /// the new text and the caret), so every untouched inline keeps its instance and origin in the file. Line feeds, carriage returns and U+FFFC are
    /// dropped from the inserted text; tabs and vertical tabs become <see cref="Tab"/> and line <see cref="Break"/> inlines. The whole batch is one
    /// undo step and raises one <see cref="Changed"/> event, and merges with the previous typing step like
    /// <see cref="ApplyTyping(string, IReadOnlyList{Inline}, DateTimeOffset)"/>.
    /// </summary>
    /// <param name="paragraphs">The typed paragraphs.</param>
    /// <param name="at">When the batch was typed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="paragraphs"/>, an entry or its text is null.</exception>
    /// <exception cref="ArgumentException">An id is not a paragraph of the document; nothing is changed.</exception>
    public void ApplyTyping(IReadOnlyList<TypedParagraph> paragraphs, DateTimeOffset at) => Type(new TypeTextCommand(paragraphs), at);

    /// <summary>Ends the current typing step: the next typing batch starts a new undo step.</summary>
    public void EndTyping() => _typing = null;

    private void Type(TextDocumentCommand command, DateTimeOffset at)
    {
        var previous = _typing is { } typing && typing.Version == Version && at - typing.At <= TypingMergeWindow ? typing.Step : null;
        var step = command;
        if (!Execute(command, previous == null ? null : Amend)) return;
        _typing = (step, Version, at);

        bool Amend(IDocumentCommand<TextDocument> last)
        {
            if (!ReferenceEquals(last, previous)) return false;
            previous.Absorb(command);
            step = previous;
            return true;
        }
    }

    private bool Execute(IDocumentCommand<TextDocument> command, Func<IDocumentCommand<TextDocument>, bool> amend)
    {
        ArgumentNullException.ThrowIfNull(command);
        var notifications = _notifications;
        command.Apply(this);
        if (notifications == _notifications) return false;
        if (amend == null || !Undo.TryAmendLast(amend)) Undo.Record(command);
        if (!Undo.InTransaction) RaiseChanged();
        return true;
    }
}
