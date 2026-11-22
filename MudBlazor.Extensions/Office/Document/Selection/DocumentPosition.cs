namespace MudBlazor.Extensions.Office.Document;

/// <summary>A step from a table into one of its cells.</summary>
/// <param name="Row">The row index.</param>
/// <param name="Cell">The cell index within the row.</param>
/// <param name="Block">The block index within the cell.</param>
public readonly record struct CellStep(int Row, int Cell, int Block);

/// <summary>A caret position: a character offset in a paragraph.</summary>
/// <param name="BlockId">The id of the top-level block of the body or of a header or footer.</param>
/// <param name="Path">The steps from that block into nested table cells; empty outside tables.</param>
/// <param name="Offset">The character offset in the paragraph's <see cref="Paragraph.Text"/>.</param>
public sealed record DocumentPosition(string BlockId, IReadOnlyList<CellStep> Path, int Offset)
{
    /// <summary>The steps from <see cref="BlockId"/> into nested table cells; empty outside tables.</summary>
    public IReadOnlyList<CellStep> Path { get; init; } = Path ?? [];

    /// <inheritdoc />
    public bool Equals(DocumentPosition other) =>
        other != null && BlockId == other.BlockId && Offset == other.Offset && Path.SequenceEqual(other.Path);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = HashCode.Combine(BlockId, Offset);
        foreach (var step in Path) hash = HashCode.Combine(hash, step);
        return hash;
    }
}
