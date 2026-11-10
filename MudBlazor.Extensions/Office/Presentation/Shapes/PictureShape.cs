namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A picture.</summary>
public sealed class PictureShape : SlideShape
{
    internal PictureShape()
    {
    }

    /// <summary>The image part name, for example <c>/ppt/media/image1.png</c>; null for a linked or missing image.</summary>
    public string ImagePart { get; internal set; }

    /// <summary>The content type of the image part, or null.</summary>
    public string ContentType { get; internal set; }
}
