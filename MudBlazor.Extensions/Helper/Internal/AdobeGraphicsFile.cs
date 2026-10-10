using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Helper.Internal;

/// <summary>
/// Reads the browser-friendly representation embedded in Illustrator and PostScript files.
/// It deliberately does not execute PostScript: PDF-compatible Illustrator data and the two
/// preview formats defined by the EPS specification are safe to hand to the existing viewers.
/// </summary>
internal static class AdobeGraphicsFile
{
    private static readonly Regex PreviewHeaderRegex = new(
        @"(?m)^%%BeginPreview:\s*(\d+)\s+(\d+)\s+(\d+)\s+(\d+)[^\r\n]*",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private const int MaximumHeaderLength = 1024 * 1024;
    private const long MaximumPreviewPixels = 40_000_000;

    public static AdobeGraphicsDocument Read(byte[] bytes, string fileName)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (IsPdf(bytes))
        {
            var pdfText = Encoding.Latin1.GetString(bytes, 0, Math.Min(bytes.Length, MaximumHeaderLength));
            return CreateDocument(fileName, pdfText, AdobeGraphicsRepresentation.Pdf, bytes, "application/pdf", null, null);
        }

        var postScript = PostScriptSection(bytes);
        var text = Encoding.Latin1.GetString(postScript, 0, Math.Min(postScript.Length, MaximumHeaderLength));

        if (TryReadDosTiffPreview(bytes, out var tiff))
            return CreateDocument(fileName, text, AdobeGraphicsRepresentation.TiffPreview, tiff, "image/tiff", null, null);

        if (TryReadEpsiPreview(text, out var png, out var width, out var height))
            return CreateDocument(fileName, text, AdobeGraphicsRepresentation.EpsiPreview, png, "image/png", width, height);

        return CreateDocument(fileName, text, AdobeGraphicsRepresentation.None, null, null, null, null);
    }

    private static AdobeGraphicsDocument CreateDocument(
        string fileName,
        string header,
        AdobeGraphicsRepresentation representation,
        byte[] previewData,
        string previewContentType,
        int? previewWidth,
        int? previewHeight)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        var format = extension switch
        {
            ".ai" => "Adobe Illustrator",
            ".ait" => "Adobe Illustrator template",
            ".eps" or ".epsf" or ".epsi" => "Encapsulated PostScript",
            ".ps" => "PostScript",
            _ => header.StartsWith("%PDF-", StringComparison.Ordinal) ? "Adobe Illustrator / PDF" : "PostScript"
        };

        return new AdobeGraphicsDocument
        {
            Format = format,
            Representation = representation,
            PreviewData = previewData,
            PreviewContentType = previewContentType,
            PreviewWidth = previewWidth,
            PreviewHeight = previewHeight,
            Version = ReadVersion(header),
            Title = ReadDscValue(header, "Title"),
            Creator = ReadDscValue(header, "Creator"),
            CreationDate = ReadDscValue(header, "CreationDate"),
            BoundingBox = ReadBoundingBox(header)
        };
    }

    private static bool IsPdf(byte[] bytes)
        => bytes.Length >= 5
           && bytes[0] == '%'
           && bytes[1] == 'P'
           && bytes[2] == 'D'
           && bytes[3] == 'F'
           && bytes[4] == '-';

    private static byte[] PostScriptSection(byte[] bytes)
    {
        if (!HasDosEpsHeader(bytes))
            return bytes;

        var offset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4));
        var length = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8, 4));
        return TrySlice(bytes, offset, length, out var postScript) ? postScript : bytes;
    }

    private static bool TryReadDosTiffPreview(byte[] bytes, out byte[] preview)
    {
        preview = null;
        if (!HasDosEpsHeader(bytes))
            return false;

        var offset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(20, 4));
        var length = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(24, 4));
        return TrySlice(bytes, offset, length, out preview);
    }

    private static bool HasDosEpsHeader(byte[] bytes)
        => bytes.Length >= 30
           && bytes[0] == 0xC5
           && bytes[1] == 0xD0
           && bytes[2] == 0xD3
           && bytes[3] == 0xC6;

    private static bool TrySlice(byte[] bytes, uint offset, uint length, out byte[] result)
    {
        result = null;
        if (length == 0 || offset > bytes.Length || length > bytes.Length - (long)offset)
            return false;

        result = bytes.AsSpan((int)offset, (int)length).ToArray();
        return true;
    }

    private static bool TryReadEpsiPreview(string text, out byte[] png, out int width, out int height)
    {
        png = null;
        width = 0;
        height = 0;

        var match = PreviewHeaderRegex.Match(text);
        if (!match.Success
            || !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out width)
            || !int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out height)
            || !int.TryParse(match.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var depth)
            || width <= 0
            || height <= 0
            || (long)width * height > MaximumPreviewPixels
            || depth is not (1 or 2 or 4 or 8))
            return false;

        var previewEnd = text.IndexOf("%%EndPreview", match.Index + match.Length, StringComparison.Ordinal);
        if (previewEnd < 0)
            return false;

        var hex = new StringBuilder();
        var previewText = text.AsSpan(match.Index + match.Length, previewEnd - match.Index - match.Length);
        foreach (var character in previewText)
        {
            if (Uri.IsHexDigit(character))
                hex.Append(character);
        }

        var bytesPerRow = ((long)width * depth + 7) / 8;
        var requiredBytes = bytesPerRow * height;
        if (requiredBytes > int.MaxValue || hex.Length < requiredBytes * 2)
            return false;

        var bitmap = new byte[(int)requiredBytes];
        for (var i = 0; i < bitmap.Length; i++)
            bitmap[i] = (byte)((HexValue(hex[i * 2]) << 4) | HexValue(hex[i * 2 + 1]));

        var gray = new byte[width * height];
        var maximumSample = (1 << depth) - 1;
        for (var sourceY = 0; sourceY < height; sourceY++)
        {
            var targetY = height - sourceY - 1;
            for (var x = 0; x < width; x++)
            {
                var bitOffset = x * depth;
                var sourceByte = bitmap[sourceY * (int)bytesPerRow + bitOffset / 8];
                var shift = 8 - depth - bitOffset % 8;
                var sample = (sourceByte >> shift) & maximumSample;
                // EPSI defines zero as white and the maximum value as black.
                gray[targetY * width + x] = (byte)(255 - sample * 255 / maximumSample);
            }
        }

        png = PngWriter.Write(gray, width, height, 1);
        return true;
    }

    private static int HexValue(char value)
        => value <= '9'
            ? value - '0'
            : char.ToUpperInvariant(value) - 'A' + 10;

    private static string ReadVersion(string header)
    {
        var firstLineEnd = header.IndexOfAny(new[] { '\r', '\n' });
        var firstLine = (firstLineEnd < 0 ? header : header[..firstLineEnd]).Trim();
        return firstLine.StartsWith("%PDF-", StringComparison.Ordinal)
            ? firstLine[1..]
            : firstLine.StartsWith("%!PS-Adobe-", StringComparison.Ordinal)
                ? firstLine[2..]
                : null;
    }

    private static string ReadBoundingBox(string header)
    {
        var value = ReadDscValue(header, "HiResBoundingBox") ?? ReadDscValue(header, "BoundingBox");
        return value?.Equals("(atend)", StringComparison.OrdinalIgnoreCase) == true ? null : value;
    }

    private static string ReadDscValue(string header, string name)
    {
        if (string.IsNullOrEmpty(header))
            return null;

        var marker = $"%%{name}:";
        var start = header.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return null;

        start += marker.Length;
        var end = header.IndexOfAny(new[] { '\r', '\n' }, start);
        var value = (end < 0 ? header[start..] : header[start..end]).Trim();
        if (value.Length >= 2 && value[0] == '(' && value[^1] == ')')
            value = value[1..^1];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

internal sealed class AdobeGraphicsDocument
{
    public string Format { get; init; }
    public AdobeGraphicsRepresentation Representation { get; init; }
    public byte[] PreviewData { get; init; }
    public string PreviewContentType { get; init; }
    public int? PreviewWidth { get; init; }
    public int? PreviewHeight { get; init; }
    public string Version { get; init; }
    public string Title { get; init; }
    public string Creator { get; init; }
    public string CreationDate { get; init; }
    public string BoundingBox { get; init; }
}

internal enum AdobeGraphicsRepresentation
{
    None,
    Pdf,
    TiffPreview,
    EpsiPreview
}
