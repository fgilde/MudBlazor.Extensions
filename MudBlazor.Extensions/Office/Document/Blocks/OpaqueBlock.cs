namespace MudBlazor.Extensions.Office.Document;

/// <summary>A block the editor keeps unchanged but does not model, such as a content control or a tracked deletion.</summary>
public sealed record OpaqueBlock : Block
{
    /// <summary>
    /// What the block is: <c>contentControl</c>, <c>customXml</c>, <c>altChunk</c>, <c>marker</c>, <c>trackedDeletion</c> or <c>other</c>.
    /// </summary>
    public string Kind { get; init; }

    /// <summary>True if the block is shown; false for markers and tracked deletions.</summary>
    public bool Visible { get; init; } = true;

    /// <summary>Read-only content to display in place of the block, or null.</summary>
    public IReadOnlyList<Block> Preview { get; init; }
}
