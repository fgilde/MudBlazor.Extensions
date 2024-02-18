using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Options for saving the current image in the image viewer.
/// </summary>
public class MudExImageViewerSaveOptions
{
    /// <summary>
    /// File name for the saved image.
    /// </summary>
    public string FileName { get; set; }
    
    /// <summary>
    /// If true, only the visible part of the image will be saved.
    /// </summary>
    public bool VisibleViewPortOnly { get; set; }
    
    /// <summary>
    /// Format in which to save the image.
    /// </summary>
    public ImageViewerExportFormat Format { get; set; } = ImageViewerExportFormat.Png;

    /// <summary>
    /// Gets the image format for the current options as IImageFormat.
    /// </summary>
    public IImageFormat GetImageFormat() => GetImageFormat(Format);

    /// <summary>
    /// Gets the image format for the given format as IImageFormat.
    /// </summary>
    public static IImageFormat GetImageFormat(ImageViewerExportFormat format)
    {
        return format switch
            {
                ImageViewerExportFormat.Png => PngFormat.Instance,
                ImageViewerExportFormat.Jpeg => JpegFormat.Instance,
                ImageViewerExportFormat.Webp => WebpFormat.Instance,
                ImageViewerExportFormat.Bmp => BmpFormat.Instance,
                ImageViewerExportFormat.Gif => GifFormat.Instance,
                ImageViewerExportFormat.Tiff => TiffFormat.Instance,
                ImageViewerExportFormat.Tga => TgaFormat.Instance,
                ImageViewerExportFormat.Qoi => QoiFormat.Instance,
                ImageViewerExportFormat.Pbm => PbmFormat.Instance,
                _ => null
            };
    }

}

/// <summary>
/// Available formats for saving the image in the image viewer.
/// </summary>
public enum ImageViewerExportFormat
{
    /// <summary>
    /// Portable Network Graphics format.
    /// </summary>
    Png,
    
    /// <summary>
    /// Joint Photographic Experts Group format.
    /// </summary>
    Jpeg,
    
    /// <summary>
    /// WebP format.
    /// </summary>
    Webp,
    
    /// <summary>
    /// Bitmap format.
    /// </summary>
    Bmp,
    
    /// <summary>
    /// Graphics Interchange Format.
    /// </summary>
    Gif,
    
    /// <summary>
    /// Tagged Image File Format.
    /// </summary>
    Tiff,
    
    /// <summary>
    /// Truevision Targa format.
    /// </summary>
    Tga,
    
    /// <summary>
    /// QuickTime Image format.
    /// </summary>
    Qoi,
    
    /// <summary>
    /// Portable Bitmap format.
    /// </summary>
    Pbm
}