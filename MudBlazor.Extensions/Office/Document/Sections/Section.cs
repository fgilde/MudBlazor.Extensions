using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>Where a section starts.</summary>
public enum SectionStart
{
    /// <summary>On a new page.</summary>
    NextPage,

    /// <summary>On the same page.</summary>
    Continuous,

    /// <summary>On the next even page.</summary>
    EvenPage,

    /// <summary>On the next odd page.</summary>
    OddPage,

    /// <summary>In the next column.</summary>
    NextColumn
}

/// <summary>A section of the document body (<c>w:sectPr</c>).</summary>
/// <remarks>
/// A section ends at a body paragraph with <c>w:pPr/w:sectPr</c> or at the end of the body. A <c>w:sectPr</c> on a paragraph inside a body-level
/// <c>w:sdt</c> or <c>w:customXml</c> does not start a new section.
/// </remarks>
public sealed record Section
{
    /// <summary>An id that identifies the section while the document is in memory.</summary>
    public string Id { get; init; }

    /// <summary>The page size and margins.</summary>
    public PageSetup Page { get; init; } = PageSetup.A4;

    /// <summary>The id of the paragraph that ends the section, or null for the final section.</summary>
    public string LastBlockId { get; init; }

    /// <summary>Where the section starts.</summary>
    public SectionStart Start { get; init; }

    /// <summary>True if the first page has its own header and footer.</summary>
    public bool TitlePage { get; init; }

    /// <summary>The number of text columns (read-only information).</summary>
    public int Columns { get; init; } = 1;

    /// <summary>The ids of the headers the section references itself; see <see cref="TextDocument.HeaderFor"/> for the effective ones.</summary>
    public IReadOnlyDictionary<HeaderFooterType, string> Headers { get; init; } = new Dictionary<HeaderFooterType, string>();

    /// <summary>The ids of the footers the section references itself; see <see cref="TextDocument.HeaderFor"/> for the effective ones.</summary>
    public IReadOnlyDictionary<HeaderFooterType, string> Footers { get; init; } = new Dictionary<HeaderFooterType, string>();

    internal XElement Source { get; init; }
}
