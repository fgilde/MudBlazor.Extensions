namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>An image whose bytes are a medium of the notebook (<see cref="Notebook.GetMedia"/>).</summary>
public sealed class ImageElement : NoteElement
{
    internal ImageElement()
    {
    }

    /// <summary>The media id: the content hash of the image bytes.</summary>
    public string MediaId { get; internal set; }

    /// <summary>The image content type, one of the supported image types, or null if the file named an unsupported one.</summary>
    public string ContentType { get; internal set; }

    /// <summary>The alternative text.</summary>
    public string AltText { get; internal set; } = "";
}
