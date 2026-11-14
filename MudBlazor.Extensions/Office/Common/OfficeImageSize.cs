using System.Buffers.Binary;
using System.Globalization;
using System.Xml;

namespace MudBlazor.Extensions.Office;

internal static class OfficeImageSize
{
    private const string SvgNamespace = "http://www.w3.org/2000/svg";

    internal static (double Width, double Height)? Read(byte[] data, string contentType)
    {
        try
        {
            return contentType switch
            {
                "image/png" when data.Length >= 24 => (BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16)), BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(20))),
                "image/gif" when data.Length >= 10 => (BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(6)), BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(8))),
                "image/bmp" when data.Length >= 26 => (BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(18)), Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(22)))),
                "image/jpeg" => Jpeg(data),
                "image/webp" when data.Length >= 30 => WebP(data),
                "image/svg+xml" => Svg(data),
                _ => null
            };
        }
        catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException or XmlException)
        {
            return null;
        }
    }

    private static (double, double)? Jpeg(byte[] data)
    {
        for (var i = 2; i + 8 < data.Length;)
        {
            if (data[i] != 0xFF)
                return null;
            var marker = data[i + 1];
            if (marker == 0xFF)
            {
                i++;
                continue;
            }

            if (marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
                return (BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i + 7)), BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i + 5)));
            i += 2 + BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i + 2));
        }

        return null;
    }

    private static (double, double)? WebP(byte[] data)
    {
        static int Uint24(byte[] d, int at) => d[at] | d[at + 1] << 8 | d[at + 2] << 16;
        return System.Text.Encoding.ASCII.GetString(data, 12, 4) switch
        {
            "VP8 " => (BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(26)) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(28)) & 0x3FFF),
            "VP8L" => ((BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(21)) & 0x3FFF) + 1, (BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(21)) >> 14 & 0x3FFF) + 1),
            "VP8X" => (Uint24(data, 24) + 1, Uint24(data, 27) + 1),
            _ => null
        };
    }

    private static (double, double)? Svg(byte[] data)
    {
        using var reader = XmlReader.Create(new MemoryStream(data), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        if (!reader.ReadToFollowing("svg", SvgNamespace))
            return null;
        var (width, height) = (Pixels(reader.GetAttribute("width")), Pixels(reader.GetAttribute("height")));
        var box = reader.GetAttribute("viewBox")?.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(v => double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : 0).ToArray();
        if (width is > 0 && height is > 0)
            return (width.Value, height.Value);
        return box is [_, _, > 0 and var w, > 0 and var h] ? (width ?? (height is > 0 ? height.Value * w / h : w), height ?? (width is > 0 ? width.Value * h / w : h)) : null;
    }

    private static double? Pixels(string value) =>
        double.TryParse(value?.Trim().Replace("px", "", StringComparison.OrdinalIgnoreCase), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && n > 0 && double.IsFinite(n) ? n : null;
}
