using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Resolves the URL of a notebook medium: a blob URL once the browser registered one, otherwise a data URI. Only raster images get blob URLs;
/// SVG stays a data URI, whose document never runs in the page's origin.
/// </summary>
public sealed class NotebookImageUrls
{
    private readonly Notebook _notebook;
    private readonly Dictionary<string, string> _blobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _dataUris = new(StringComparer.OrdinalIgnoreCase);

    internal NotebookImageUrls(Notebook notebook) => _notebook = notebook;

    internal int Version { get; private set; }

    /// <summary>Registers the blob URL of a medium; it replaces the data URI.</summary>
    /// <param name="mediaId">The media id.</param>
    /// <param name="url">The blob URL.</param>
    public void Register(string mediaId, string url)
    {
        _blobs[mediaId] = url;
        _dataUris.Remove(mediaId);
        Version++;
    }

    /// <summary>Gets the URL of a medium, or null if the notebook has no such medium.</summary>
    /// <param name="mediaId">The media id.</param>
    public string UrlFor(string mediaId)
    {
        if (mediaId == null) return null;
        if (_blobs.TryGetValue(mediaId, out var blob)) return blob;
        if (_dataUris.TryGetValue(mediaId, out var cached)) return cached;
        var (data, contentType) = Load(mediaId);
        return data == null ? null : _dataUris[mediaId] = $"data:{SlideImageUrls.SafeType(contentType)};base64,{Convert.ToBase64String(data)}";
    }

    internal bool NeedsBlob(string mediaId) => mediaId != null && !_blobs.ContainsKey(mediaId) && SlideImageUrls.RasterTypes.Contains(_notebook.GetMediaType(mediaId) ?? "");

    internal (byte[] Data, string ContentType) Load(string mediaId) => (_notebook.GetMedia(mediaId), _notebook.GetMediaType(mediaId));
}
