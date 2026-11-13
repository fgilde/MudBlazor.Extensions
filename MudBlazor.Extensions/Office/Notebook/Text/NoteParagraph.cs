using System.Text.Json;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>The list kind of a paragraph.</summary>
public enum NoteListKind
{
    /// <summary>No list.</summary>
    None,

    /// <summary>A bulleted list.</summary>
    Bullet,

    /// <summary>A numbered list; numbers count per indent level.</summary>
    Number,

    /// <summary>A to-do list with a check box.</summary>
    Todo
}

/// <summary>The horizontal alignment of a paragraph.</summary>
public enum NoteAlign
{
    /// <summary>Left aligned.</summary>
    Left,

    /// <summary>Centred.</summary>
    Center,

    /// <summary>Right aligned.</summary>
    Right
}

/// <summary>The heading style of a paragraph.</summary>
public enum NoteHeading
{
    /// <summary>Normal text.</summary>
    None,

    /// <summary>Heading 1.</summary>
    H1,

    /// <summary>Heading 2.</summary>
    H2,

    /// <summary>Heading 3.</summary>
    H3
}

/// <summary>A paragraph of a text element or table cell.</summary>
public sealed record NoteParagraph
{
    internal const int MaxIndent = 8;

    /// <summary>The runs in order.</summary>
    public IReadOnlyList<NoteRun> Runs { get; init; } = [];

    /// <summary>The list kind.</summary>
    public NoteListKind List { get; init; }

    /// <summary>The indent level, 0 to 8.</summary>
    public int Indent { get; init; }

    /// <summary>True if the check box of a <see cref="NoteListKind.Todo"/> item is ticked.</summary>
    public bool Checked { get; init; }

    /// <summary>The alignment.</summary>
    public NoteAlign Align { get; init; }

    /// <summary>The heading style.</summary>
    public NoteHeading Heading { get; init; }

    internal Dictionary<string, JsonElement> Extra { get; init; }

    /// <summary>Compares all values, the runs by value.</summary>
    /// <param name="other">The other paragraph.</param>
    /// <returns>True if both paragraphs are equal.</returns>
    public bool Equals(NoteParagraph other) =>
        other is not null && List == other.List && Indent == other.Indent && Checked == other.Checked && Align == other.Align && Heading == other.Heading
        && Runs.SequenceEqual(other.Runs);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(List, Indent, Checked, Align, Heading, Runs.Count);
}
