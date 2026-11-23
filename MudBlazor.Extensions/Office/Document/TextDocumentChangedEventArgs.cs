namespace MudBlazor.Extensions.Office.Document;

/// <summary>What changed in a text document.</summary>
[Flags]
public enum DocumentChangeKind
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>Text or formatting of body blocks changed. Edits in headers and footers report <see cref="HeadersFooters"/> instead, without <see cref="Content"/>.</summary>
    Content = 1,

    /// <summary>
    /// Blocks were added, removed or moved, or a block was replaced by one with a different id (at any depth, also inside table cells; see
    /// <see cref="TextDocumentChangedEventArgs.AddedNestedBlockIds"/> and <see cref="TextDocumentChangedEventArgs.RemovedNestedBlockIds"/>).
    /// </summary>
    Structure = 2,

    /// <summary>Styles changed; every block may be affected.</summary>
    Styles = 4,

    /// <summary>List definitions or numbering changed; list labels may be affected everywhere.</summary>
    Numbering = 8,

    /// <summary>Sections or page setup changed.</summary>
    Sections = 16,

    /// <summary>Headers or footers changed.</summary>
    HeadersFooters = 32,

    /// <summary>Document properties changed.</summary>
    Properties = 64
}

/// <summary>Describes everything that changed during one command, transaction, undo or redo.</summary>
public sealed class TextDocumentChangedEventArgs : EventArgs
{
    internal TextDocumentChangedEventArgs(IReadOnlyCollection<string> changedBlockIds, IReadOnlyCollection<string> removedBlockIds, DocumentChangeKind kind)
    {
        ChangedBlockIds = changedBlockIds;
        RemovedBlockIds = removedBlockIds;
        Kind = kind;
    }

    /// <summary>
    /// The ids of the blocks at any depth (top-level blocks, blocks in table cells, blocks of headers and footers) that were replaced by a changed
    /// instance with the same id. A table is included when a block inside it changed.
    /// </summary>
    public IReadOnlyCollection<string> ChangedNestedBlockIds { get; internal init; } = [];

    /// <summary>The ids of the blocks at any depth that were added.</summary>
    public IReadOnlyCollection<string> AddedNestedBlockIds { get; internal init; } = [];

    /// <summary>The ids of the blocks at any depth that were removed, including the blocks inside a removed table.</summary>
    public IReadOnlyCollection<string> RemovedNestedBlockIds { get; internal init; } = [];

    /// <summary>The ids of the top-level blocks (of the body or of a header or footer) that changed, including added ones.</summary>
    public IReadOnlyCollection<string> ChangedBlockIds { get; }

    /// <summary>The ids of the blocks that were removed: top-level blocks and blocks dropped from tables.</summary>
    public IReadOnlyCollection<string> RemovedBlockIds { get; }

    /// <summary>The kinds of change.</summary>
    public DocumentChangeKind Kind { get; }

    /// <summary>
    /// The caret or selection that belongs to the change: after a command, a redo or the end of a transaction the
    /// <see cref="TextDocumentCommand.After"/> of the (last) command; after an undo the <see cref="TextDocumentCommand.Before"/> of the step (of the
    /// first command of a transaction); null when unknown.
    /// </summary>
    public DocumentRange? Selection { get; internal init; }
}
