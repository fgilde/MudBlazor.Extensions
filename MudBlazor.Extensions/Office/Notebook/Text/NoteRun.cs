using System.Text.Json;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>A run of uniformly formatted text.</summary>
/// <param name="Text">The text.</param>
/// <param name="Format">The formatting; null means none.</param>
public sealed record NoteRun(string Text, NoteRunFormat Format = null)
{
    /// <summary>The text.</summary>
    public string Text { get; init; } = Text ?? "";

    /// <summary>The formatting.</summary>
    public NoteRunFormat Format { get; init; } = Format ?? new NoteRunFormat();

    internal Dictionary<string, JsonElement> Extra { get; init; }

    /// <summary>Compares text and formatting.</summary>
    /// <param name="other">The other run.</param>
    /// <returns>True if text and formatting are equal.</returns>
    public bool Equals(NoteRun other) => other is not null && Text == other.Text && Format == other.Format;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Text, Format);
}
