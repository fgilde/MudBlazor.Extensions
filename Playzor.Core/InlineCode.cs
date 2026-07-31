namespace Playzor.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Snippets that are not stored on the server travel inside the url: all files are joined
    /// with a unit separator, deflated and base64url encoded. Keep the format stable — links
    /// are shared publicly and Playzor.Blazor re-implements the encoder.
    /// </summary>
    public static class InlineCode
    {
        private const char Separator = (char)31;

        public static string Encode(IEnumerable<CodeFile> codeFiles)
        {
            if (codeFiles == null) throw new ArgumentNullException(nameof(codeFiles));

            var parts = codeFiles.SelectMany(f => new[] { f.Path, f.Content ?? string.Empty });
            return Encode(string.Join(Separator, parts));
        }

        public static string Encode(string rawCode)
        {
            var bytes = Encoding.UTF8.GetBytes(rawCode);

            using var compressed = new MemoryStream();
            using (var compressor = new DeflateStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                compressor.Write(bytes, 0, bytes.Length);
            }

            return Base64UrlEncode(compressed.ToArray());
        }

        public static IEnumerable<CodeFile> Decode(string urlEncodedBase64CompressedCode)
        {
            var bytes = Base64UrlDecode(urlEncodedBase64CompressedCode);

            using var uncompressed = new MemoryStream();
            using (var compressedStream = new MemoryStream(bytes))
            using (var uncompressor = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                uncompressor.CopyTo(uncompressed);
            }

            var codeString = Encoding.UTF8.GetString(uncompressed.ToArray());
            var codeElements = codeString.Split(Separator);

            var codeFiles = new List<CodeFile>();
            for (var i = 0; i + 1 < codeElements.Length; i += 2)
            {
                codeFiles.Add(new CodeFile { Path = codeElements[i], Content = codeElements[i + 1] });
            }

            return codeFiles;
        }

        // base64url per RFC 4648 §5 — same output as WebEncoders, without the aspnetcore dependency
        public static string Base64UrlEncode(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public static byte[] Base64UrlDecode(string value)
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
        }
    }
}
