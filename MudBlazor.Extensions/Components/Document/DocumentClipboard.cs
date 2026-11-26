using System.Text;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

internal sealed class DocumentClipboard
{
    internal const int MaxChars = 8 * 1024 * 1024;
    internal const long MaxTextBytes = 32L * 1024 * 1024;
    internal const long MaxImageBytes = 16L * 1024 * 1024;
    internal const string Unreadable = "The clipboard content cannot be pasted.";
    internal const string TooLarge = "The clipboard content is too large to paste.";

    private (string Clip, TextDocument Extract)? _entry;

    internal void Store(string clip, TextDocument extract) => _entry = clip == null || extract == null ? null : (clip, extract);

    internal bool Holds(string clip) => clip != null && _entry?.Clip == clip;

    internal IDocumentCommand<TextDocument> Paste(TextDocument document, DocumentRange range, string html, string text, byte[] image, string imageType, string clip)
    {
        if (html?.Length > MaxChars || text?.Length > MaxChars) throw new InvalidDataException(TooLarge);
        if (Holds(clip)) return new InsertDocumentCommand(range, _entry.Value.Extract);
        if (!string.IsNullOrEmpty(html) && ReadHtml(html) is { } source) return new InsertDocumentCommand(range, source);
        if (image is { Length: > 0 } && imageType != null && InsertImageCommand.Extension(imageType) != null) return new InsertImageCommand(range, image, imageType);
        return string.IsNullOrEmpty(text) ? null : new ReplaceTextCommand(range, text.Replace("\r\n", "\n").Replace('\r', '\n'));
    }

    internal static async Task<string> ReadTextAsync(IJSStreamReference stream)
    {
        if (stream == null) return null;
        var bytes = await ReadBytesAsync(stream, MaxTextBytes);
        var text = new UTF8Encoding(false).GetString(bytes);
        return text.Length > MaxChars ? throw new InvalidDataException(TooLarge) : text;
    }

    internal static async Task<byte[]> ReadBytesAsync(IJSStreamReference stream, long max = MaxImageBytes)
    {
        if (stream == null) return null;
        try
        {
            await using (stream)
            {
                if (stream.Length > max) throw new InvalidDataException(TooLarge);
                await using var data = await stream.OpenReadStreamAsync(max);
                using var buffer = new MemoryStream();
                await data.CopyToAsync(buffer);
                return buffer.ToArray();
            }
        }
        catch (IOException)
        {
            throw new InvalidDataException(TooLarge);
        }
    }

    private static TextDocument ReadHtml(string html)
    {
        TextDocument source;
        try
        {
            using (DocumentLoadLimits.BeginLoad())
                source = HtmlReader.Read(html);
        }
        catch (Exception e) when (e is InvalidDataException or InsufficientExecutionStackException)
        {
            throw new InvalidDataException(Unreadable);
        }

        return source.Blocks.Any(b => b is not Paragraph { Inlines.Count: 0 }) ? source : null;
    }
}
