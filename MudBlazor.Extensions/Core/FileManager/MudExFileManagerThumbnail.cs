using Nextended.Core;

namespace MudBlazor.Extensions.Core.FileManager;

/// <summary>
/// What a file area tile shows of a file: the picture itself, or the first lines of its text.
/// </summary>
/// <remarks>
/// A thumbnail is a glance at the file, not a viewer. Reading only the head keeps a level of entries cheap,
/// and anything that is neither an image nor text simply keeps its icon.
/// </remarks>
public sealed class MudExFileManagerThumbnail
{
    private const int MaxBytes = 64 * 1024;
    private const int MaxTextLength = 400;

    private MudExFileManagerThumbnail(string dataUrl, string text)
    {
        DataUrl = dataUrl;
        Text = text;
    }

    /// <summary>The image itself, ready for an img tag.</summary>
    public string DataUrl { get; }

    /// <summary>The beginning of the file's text.</summary>
    public string Text { get; }

    /// <summary>Reads the head of a file and turns it into what a tile can show, or null for neither.</summary>
    public static async Task<MudExFileManagerThumbnail> ReadAsync(Stream stream, string contentType, string fileName)
    {
        var type = string.IsNullOrEmpty(contentType) ? MimeType.GetMimeType(fileName ?? string.Empty) : contentType;

        using var buffer = new MemoryStream();
        var chunk = new byte[8 * 1024];
        int read;
        while (buffer.Length < MaxBytes && (read = await stream.ReadAsync(chunk, 0, chunk.Length)) > 0)
            buffer.Write(chunk, 0, read);

        var bytes = buffer.ToArray();
        if (bytes.Length == 0)
            return null;

        if (type?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
            return new MudExFileManagerThumbnail($"data:{type};base64,{Convert.ToBase64String(bytes)}", null);

        if (!IsTextual(type, fileName))
            return null;

        var text = System.Text.Encoding.UTF8.GetString(bytes).Replace("\0", string.Empty);
        text = text.Length > MaxTextLength ? text.Substring(0, MaxTextLength) : text;
        return string.IsNullOrWhiteSpace(text) ? null : new MudExFileManagerThumbnail(null, text);
    }

    private static bool IsTextual(string contentType, string fileName)
    {
        if (contentType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        // Types that are text but not announced as such.
        if (contentType is not null && (contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
                                        || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
                                        || contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase)))
            return true;

        var extension = System.IO.Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return extension is ".md" or ".txt" or ".log" or ".csv" or ".json" or ".xml" or ".yml" or ".yaml"
            or ".cs" or ".js" or ".ts" or ".css" or ".html" or ".razor" or ".sql" or ".geojson";
    }
}
