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

public class MudExImageViewerSaveOptions
{
    public string FileName { get; set; }
    public bool VisibleViewPortOnly { get; set; }
    public ImageViewerExportFormat Format { get; set; } = ImageViewerExportFormat.Png;

    public IImageFormat GetImageFormat() => GetImageFormat(Format);

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

public enum ImageViewerExportFormat
{
    Png,
    Jpeg,
    Webp,
    Bmp,
    Gif,
    Tiff,
    Tga,
    Qoi,
    Pbm
}