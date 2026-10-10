using System.Drawing;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Interop;

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
    /// Specify the area to save.
    /// Choose between the full image, the visible viewport or the selected area.
    /// </summary>
    public SaveImageMode AreaToSave { get; set; }
    
    /// <summary>
    /// Format in which to save the image.
    /// </summary>
    public ImageViewerExportFormat Format { get; set; } = ImageViewerExportFormat.Png;

    /// <summary>
    /// Gets the file extension (without dot) and mime type for the given format.
    /// </summary>
    public static (string Extension, string MimeType) GetFileType(ImageViewerExportFormat format) => format switch
    {
        ImageViewerExportFormat.Jpeg => ("jpg", "image/jpeg"),
        ImageViewerExportFormat.Webp => ("webp", "image/webp"),
        ImageViewerExportFormat.Bmp => ("bmp", "image/bmp"),
        ImageViewerExportFormat.Gif => ("gif", "image/gif"),
        ImageViewerExportFormat.Tiff => ("tiff", "image/tiff"),
        ImageViewerExportFormat.Tga => ("tga", "image/x-tga"),
        ImageViewerExportFormat.Qoi => ("qoi", "image/qoi"),
        ImageViewerExportFormat.Pbm => ("ppm", "image/x-portable-pixmap"),
        _ => ("png", "image/png")
    };

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
    /// Quite OK Image format.
    /// </summary>
    Qoi,
    
    /// <summary>
    /// Portable Bitmap format family, written as binary color pixmap (.ppm).
    /// </summary>
    Pbm
}

public enum SaveImageMode
{
    Full = 0,
    VisibleViewPort = 1,
    SelectedArea = 2
}

public class ImageAreaSelectedArgs
{
    public ImageAreaSelectedArgs(RectangleF area, BoundingClientRect rubberBandRect, byte[] imageBytes, string imageBlobUrl)
    {
        Area = area;
        RubberBandRect = rubberBandRect;
        ImageBytes = imageBytes;
        ImageBlobUrl = imageBlobUrl;
    }

    public RectangleF Area { get; private set; }
    public BoundingClientRect RubberBandRect { get; private set; }
    public byte[] ImageBytes { get; private set; }
    public string ImageBlobUrl { get; private set; }
}