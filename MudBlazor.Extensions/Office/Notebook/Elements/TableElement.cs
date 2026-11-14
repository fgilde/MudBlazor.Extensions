using System.Text.Json;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>A cell of a <see cref="TableElement"/>.</summary>
public sealed record NoteTableCell
{
    /// <summary>The paragraphs of the cell.</summary>
    public IReadOnlyList<NoteParagraph> Paragraphs { get; init; } = [];

    internal Dictionary<string, JsonElement> Extra { get; init; }

    /// <summary>Compares the paragraphs by value.</summary>
    /// <param name="other">The other cell.</param>
    /// <returns>True if both cells hold equal paragraphs.</returns>
    public bool Equals(NoteTableCell other) => other is not null && Paragraphs.SequenceEqual(other.Paragraphs);

    /// <inheritdoc />
    public override int GetHashCode() => Paragraphs.Count;
}

/// <summary>A table of text cells. Its height follows the content; every row has one cell per column.</summary>
public sealed class TableElement : NoteElement
{
    internal TableElement()
    {
    }

    /// <summary>The column widths in pixels.</summary>
    public IReadOnlyList<double> ColumnWidths { get; internal set; } = [];

    /// <summary>The rows, each with one cell per column.</summary>
    public IReadOnlyList<IReadOnlyList<NoteTableCell>> Rows { get; internal set; } = [];

    /// <summary>True if the first row is a header row.</summary>
    public bool HeaderRow { get; internal set; }
}
