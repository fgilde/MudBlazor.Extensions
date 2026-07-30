using System.IO.Compression;
using System.Text;

namespace Playzor.Blazor;

/// <summary>
/// Encodes snippets for playzor urls: all files joined with a unit separator, deflated,
/// base64url encoded. Mirrors Try.Core.InlineCode on the playground side — the format is
/// part of the public url contract, so do not change it without changing both sides.
/// </summary>
public static class PlayzorCode
{
    private const char Separator = (char)31;

    public static string Encode(IEnumerable<KeyValuePair<string, string>> files)
    {
        var parts = files.SelectMany(f => new[] { f.Key, f.Value ?? string.Empty });
        var bytes = Encoding.UTF8.GetBytes(string.Join(Separator, parts));

        using var compressed = new MemoryStream();
        using (var compressor = new DeflateStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            compressor.Write(bytes, 0, bytes.Length);
        }

        return Base64UrlEncode(compressed.ToArray());
    }

    public static string Encode(string mainRazorContent, string mainFileName = "__Main.razor")
        => Encode(new[] { new KeyValuePair<string, string>(mainFileName, mainRazorContent) });

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
