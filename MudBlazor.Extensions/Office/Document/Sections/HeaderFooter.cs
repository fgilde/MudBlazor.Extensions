namespace MudBlazor.Extensions.Office.Document;

/// <summary>Whether a <see cref="HeaderFooter"/> is a header or a footer.</summary>
public enum HeaderFooterKind
{
    /// <summary>A header.</summary>
    Header,

    /// <summary>A footer.</summary>
    Footer
}

/// <summary>The pages a header or footer reference applies to.</summary>
public enum HeaderFooterType
{
    /// <summary>All pages without a more specific one.</summary>
    Default,

    /// <summary>The first page of the section, if the section has a title page.</summary>
    First,

    /// <summary>Even pages, if the document has different even and odd headers.</summary>
    Even
}

/// <summary>A header or footer part.</summary>
public sealed record HeaderFooter
{
    /// <summary>The id sections use to reference it.</summary>
    public string Id { get; init; }

    /// <summary>Whether it is a header or a footer.</summary>
    public HeaderFooterKind Kind { get; init; }

    /// <summary>The content.</summary>
    public IReadOnlyList<Block> Blocks { get; init; } = [];

    internal string PartName { get; init; }
}
