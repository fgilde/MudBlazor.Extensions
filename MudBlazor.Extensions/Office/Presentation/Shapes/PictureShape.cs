namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>The part of an image that is shown (<c>a:srcRect</c>), as fractions of the image size cut off at each edge.</summary>
/// <param name="Left">The fraction cut off at the left.</param>
/// <param name="Top">The fraction cut off at the top.</param>
/// <param name="Right">The fraction cut off at the right.</param>
/// <param name="Bottom">The fraction cut off at the bottom.</param>
public sealed record ImageCrop(double Left, double Top, double Right, double Bottom);

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

    /// <summary>The cropping of the image, or null if the whole image is shown.</summary>
    public ImageCrop Crop { get; internal set; }
}
