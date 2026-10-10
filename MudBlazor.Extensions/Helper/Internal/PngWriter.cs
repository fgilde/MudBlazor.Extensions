using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace MudBlazor.Extensions.Helper.Internal;

/// <summary>
/// Writes 8-bit grayscale or RGB pixels as PNG, so viewers that decode pixels themselves (DICOM, EPSI previews)
/// can hand them to the browser. Everything else is decoded and encoded by the browser.
/// </summary>
internal static class PngWriter
{
    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (var k = 0; k < 8; k++)
            c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        return c;
    }).ToArray();

    public static byte[] Write(ReadOnlySpan<byte> pixels, int width, int height, int channels)
    {
        if (channels is not (1 or 3))
            throw new ArgumentOutOfRangeException(nameof(channels));
        var stride = width * channels;
        if (pixels.Length < stride * height)
            throw new ArgumentException("Not enough pixel data for the image size.", nameof(pixels));

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            for (var y = 0; y < height; y++)
            {
                zlib.WriteByte(0); // filter type None
                zlib.Write(pixels.Slice(y * stride, stride));
            }
        }

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; // bit depth
        header[9] = channels == 1 ? (byte)0 : (byte)2; // grayscale or truecolor

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", compressed.GetBuffer().AsSpan(0, (int)compressed.Length));
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        stream.Write(number);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = 0xFFFFFFFFu;
        foreach (var b in typeBytes)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        foreach (var b in data)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        BinaryPrimitives.WriteUInt32BigEndian(number, crc ^ 0xFFFFFFFFu);
        stream.Write(number);
    }
}
