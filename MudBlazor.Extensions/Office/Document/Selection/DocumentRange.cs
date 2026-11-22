namespace MudBlazor.Extensions.Office.Document;

/// <summary>A range between two positions; <see cref="Start"/> may lie after <see cref="End"/> for a backward selection.</summary>
/// <param name="Start">Where the range starts (the anchor of a selection).</param>
/// <param name="End">Where the range ends (the caret of a selection).</param>
public readonly record struct DocumentRange(DocumentPosition Start, DocumentPosition End)
{
    /// <summary>True if start and end are the same position.</summary>
    public bool IsEmpty => Equals(Start, End);

    /// <summary>Returns the range with start before end in document order.</summary>
    /// <param name="document">The document the positions belong to.</param>
    /// <returns>The ordered range.</returns>
    public DocumentRange Normalize(TextDocument document) => document.Compare(Start, End) <= 0 ? this : new DocumentRange(End, Start);
}
