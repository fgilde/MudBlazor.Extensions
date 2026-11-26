namespace MudBlazor.Extensions.Office.Document;

/// <summary>
/// Replaces a range with an inline picture. The size comes from the image at 96 dpi, scaled down to the section's text width with the aspect ratio
/// kept; an image whose size cannot be read gets one inch square. The picture is kept in memory until the document is saved; the same picture
/// inserted twice, or one the document already contains, is stored once.
/// </summary>
public sealed class InsertImageCommand : TextDocumentCommand
{
    private const long EmuPerPixel = 9525;
    private const long EmuPerTwip = 635;
    private const long DefaultSize = 914400;
    private static readonly Dictionary<string, string> Extensions = new()
    {
        ["image/png"] = "png", ["image/jpeg"] = "jpeg", ["image/gif"] = "gif", ["image/bmp"] = "bmp"
    };

    private readonly DocumentRange _at;
    private readonly byte[] _data;
    private readonly string _contentType;

    /// <summary>Creates the command.</summary>
    /// <param name="at">The range to replace, in either direction; an empty range inserts at a position.</param>
    /// <param name="data">The image file content.</param>
    /// <param name="contentType"><c>image/png</c>, <c>image/jpeg</c>, <c>image/gif</c> or <c>image/bmp</c>.</param>
    /// <exception cref="ArgumentNullException">The data is null.</exception>
    /// <exception cref="ArgumentException">The data is empty or the content type is not supported.</exception>
    public InsertImageCommand(DocumentRange at, byte[] data, string contentType)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0) throw new ArgumentException("The image is empty.", nameof(data));
        if (contentType == null || !Extensions.ContainsKey(contentType))
            throw new ArgumentException($"'{contentType}' is not a supported image type; use PNG, JPEG, GIF or BMP.", nameof(contentType));
        (_at, _data, _contentType) = (at, data.ToArray(), contentType);
    }

    /// <inheritdoc />
    public override string Description => "Insert picture";

    internal static string Extension(string contentType) => Extensions.GetValueOrDefault(contentType);

    internal static bool IsPending(string mediaPart) => mediaPart.StartsWith(PendingPrefix, StringComparison.Ordinal);

    private const string PendingPrefix = "pending:";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var start = document.Snap(_at).Start;
        var (paragraph, offset) = document.Resolve(start);
        var media = AddMedia(document, _data, _contentType);
        var (width, height) = Size(_data, _contentType, document.SectionOf(start.BlockId).Page.TextWidth * EmuPerTwip);
        var format = InlineEdit.InsertionStyle(paragraph, offset).Format;
        var image = new InlineImage(media, width, height) { Format = format with { StyleId = format.StyleId == InlineEdit.HyperlinkStyle ? null : format.StyleId } };
        return document.ReplaceRange(_at, [[image]]);
    }

    internal static string AddMedia(TextDocument document, byte[] data, string contentType) =>
        document.AddMedia(data, contentType, hash => $"{PendingPrefix}{hash}.{Extensions[contentType]}");

    internal static (long Width, long Height) Size(byte[] data, string contentType, long maxWidth, int? widthPx = null, int? heightPx = null)
    {
        var intrinsic = OfficeImageSize.Read(data, contentType) is ({ } iw and > 0, { } ih and > 0) ? (iw, ih) : default;
        double w, h;
        if (widthPx is { } fixedWidth && heightPx is { } fixedHeight) (w, h) = (fixedWidth, fixedHeight);
        else if (intrinsic == default) return (DefaultSize, DefaultSize);
        else if (widthPx is { } onlyWidth) (w, h) = (onlyWidth, onlyWidth * intrinsic.ih / intrinsic.iw);
        else if (heightPx is { } onlyHeight) (w, h) = (onlyHeight * intrinsic.iw / intrinsic.ih, onlyHeight);
        else (w, h) = intrinsic;
        var width = Math.Max(1, (long)Math.Round(w)) * EmuPerPixel;
        var height = Math.Max(1, (long)Math.Round(h)) * EmuPerPixel;
        return width <= maxWidth || maxWidth <= 0 ? (width, height) : (maxWidth, Math.Max(1, (long)Math.Round(height * (double)maxWidth / width)));
    }
}

/// <summary>Changes the displayed size of a picture.</summary>
public sealed class ResizeImageCommand : TextDocumentCommand
{
    private readonly DocumentPosition _at;
    private readonly long _width;
    private readonly long _height;

    /// <summary>Creates the command.</summary>
    /// <param name="at">The position of the picture (the position before it).</param>
    /// <param name="widthEmu">The width in EMU, positive.</param>
    /// <param name="heightEmu">The height in EMU, positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">A size is not positive.</exception>
    public ResizeImageCommand(DocumentPosition at, long widthEmu, long heightEmu)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(widthEmu, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(heightEmu, 1);
        (_at, _width, _height) = (at, widthEmu, heightEmu);
    }

    /// <inheritdoc />
    public override string Description => "Resize picture";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document) =>
        (Images.Map(document, _at, i => i with { WidthEmu = _width, HeightEmu = _height }), null);
}

/// <summary>Sets the alternative text of a picture.</summary>
/// <param name="at">The position of the picture (the position before it).</param>
/// <param name="altText">The text; null or empty removes it.</param>
public sealed class SetImageAltTextCommand(DocumentPosition at, string altText) : TextDocumentCommand
{
    /// <inheritdoc />
    public override string Description => "Alternative text";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document) =>
        (Images.Map(document, at, i => i with { AltText = string.IsNullOrEmpty(altText) ? null : altText }), null);
}

internal static class Images
{
    internal static List<BlockEdit> Map(TextDocument document, DocumentPosition at, Func<InlineImage, InlineImage> change)
    {
        ArgumentNullException.ThrowIfNull(at);
        var (paragraph, offset) = document.Resolve(at);
        if (InlineEdit.At(paragraph, offset) is not InlineImage image) throw new ArgumentException($"There is no picture at offset {offset} of {paragraph.Id}.", nameof(at));
        var changed = change(image);
        return changed == image
            ? []
            : document.MapParagraphs([paragraph.Id], p => p with { Inlines = p.Inlines.Select(i => ReferenceEquals(i, image) ? changed : i).ToList() });
    }
}
