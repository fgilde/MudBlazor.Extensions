namespace MudBlazor.Extensions.Office.Document;

/// <summary>What changed in a text document.</summary>
[Flags]
public enum DocumentChangeKind
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>Text or formatting of body blocks changed. Edits in headers and footers report <see cref="HeadersFooters"/> instead, without <see cref="Content"/>.</summary>
    Content = 1,

    /// <summary>Blocks were added, removed or moved, or a block was replaced by one with a different id (at any depth, also inside table cells).</summary>
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

    /// <summary>The ids of the top-level blocks (of the body or of a header or footer) that changed, including added ones.</summary>
    public IReadOnlyCollection<string> ChangedBlockIds { get; }

    /// <summary>The ids of the top-level blocks that were removed.</summary>
    public IReadOnlyCollection<string> RemovedBlockIds { get; }

    /// <summary>The kinds of change.</summary>
    public DocumentChangeKind Kind { get; }
}
