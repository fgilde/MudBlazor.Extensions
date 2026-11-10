namespace MudBlazor.Extensions.Office;

/// <summary>A reversible edit of a document.</summary>
/// <typeparam name="TDocument">The document type the command edits.</typeparam>
public interface IDocumentCommand<in TDocument>
{
    /// <summary>Short description of the edit, shown for undo and redo.</summary>
    string Description { get; }

    /// <summary>Applies the edit. Runs again for redo and validates before mutating.</summary>
    /// <param name="document">The document to edit.</param>
    void Apply(TDocument document);

    /// <summary>Reverts the edit.</summary>
    /// <param name="document">The document to edit.</param>
    void Revert(TDocument document);
}
