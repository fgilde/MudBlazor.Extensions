using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Resolves the URL of an image part of a presentation: a blob URL once the browser registered one, otherwise a data URI. Only raster images get
/// blob URLs; SVG and other types stay data URIs, whose documents never run in the page's origin.
/// </summary>
public sealed class SlideImageUrls
{
    internal static readonly HashSet<string> RasterTypes = new(StringComparer.OrdinalIgnoreCase) { "image/png", "image/jpeg", "image/gif", "image/bmp", "image/webp" };

    private readonly Presentation _presentation;
    private readonly Dictionary<string, string> _blobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _dataUris = new(StringComparer.OrdinalIgnoreCase);

    internal SlideImageUrls(Presentation presentation) => _presentation = presentation;

    /// <summary>Registers the blob URL of an image part; it replaces the data URI.</summary>
    /// <param name="partName">The image part name.</param>
    /// <param name="url">The blob URL.</param>
    public void Register(string partName, string url)
    {
        _blobs[partName] = url;
        _dataUris.Remove(partName);
    }

    /// <summary>Gets the URL of an image part, or null if the part does not exist.</summary>
    /// <param name="partName">The image part name.</param>
    public string UrlFor(string partName)
    {
        if (partName == null) return null;
        if (_blobs.TryGetValue(partName, out var blob)) return blob;
        if (_dataUris.TryGetValue(partName, out var cached)) return cached;
        var (data, contentType) = Load(partName);
        return data == null ? null : _dataUris[partName] = $"data:{SafeType(contentType)};base64,{Convert.ToBase64String(data)}";
    }

    internal bool NeedsBlob(string partName) => !_blobs.ContainsKey(partName) && RasterTypes.Contains(ContentTypeOf(partName) ?? "");

    internal static string SafeType(string contentType) =>
        contentType is { Length: > 6 } && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && contentType.All(c => char.IsAsciiLetterOrDigit(c) || c is '/' or '+' or '-' or '.')
            ? contentType
            : "application/octet-stream";

    private string ContentTypeOf(string partName) =>
        _presentation.PendingMedia.TryGetValue(partName, out var pending) ? pending.ContentType : _presentation.Package?.Opc.ContentType(partName);

    internal (byte[] Data, string ContentType) Load(string partName)
    {
        if (_presentation.PendingMedia.TryGetValue(partName, out var pending)) return pending;
        var opc = _presentation.Package?.Opc;
        return opc != null && opc.PartNames.Contains(partName) ? (opc.ReadBytes(partName), opc.ContentType(partName)) : (null, null);
    }
}
