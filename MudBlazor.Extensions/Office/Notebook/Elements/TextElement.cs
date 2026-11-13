namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>A block of text placed anywhere on the page. Its height follows the text unless <see cref="NoteElement.Height"/> is set.</summary>
public sealed class TextElement : NoteElement
{
    internal TextElement()
    {
    }

    /// <summary>The paragraphs in order.</summary>
    public IReadOnlyList<NoteParagraph> Paragraphs { get; internal set; } = [];
}
