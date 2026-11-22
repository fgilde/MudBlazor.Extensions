using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>A block of a document body, a table cell or a header or footer.</summary>
public abstract record Block
{
    /// <summary>An id that identifies the block while the document is in memory; it is not stored in the file.</summary>
    public string Id { get; init; }

    internal XElement Source { get; init; }
}
