using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace MudBlazor.Extensions.Helper.Internal;

internal sealed class DicomFile
{
    private readonly byte[] _data;
    private bool _explicitVr = true;
    public int Rows { get; private set; }
    public int Columns { get; private set; }
    public int BitsAllocated { get; private set; } = 8;
    public int BitsStored { get; private set; } = 8;
    public int SamplesPerPixel { get; private set; } = 1;
    public int PlanarConfiguration { get; private set; }
    public string PhotometricInterpretation { get; private set; }
    public string TransferSyntax { get; private set; }
    public string PatientName { get; private set; }
    public string Modality { get; private set; }
    public string StudyDescription { get; private set; }
    public double? WindowCenter { get; private set; }
    public double? WindowWidth { get; private set; }
    public byte[] PixelData { get; private set; }

    private DicomFile(byte[] data) => _data = data;

    public static DicomFile Read(byte[] data)
    {
        var file = new DicomFile(data);
        file.Parse();
        return file;
    }

    private void Parse()
    {
        var position = _data.Length >= 132 && Encoding.ASCII.GetString(_data, 128, 4) == "DICM" ? 132 : 0;
        // File meta information is always explicit VR little endian.
        while (position + 8 <= _data.Length && ReadU16(position) == 0x0002)
        {
            var element = ReadElement(ref position, explicitVr: true);
            if (element.Group == 0x0002 && element.ElementNumber == 0x0010)
                TransferSyntax = Text(element.Value);
        }

        TransferSyntax ??= "1.2.840.10008.1.2";
        if (TransferSyntax == "1.2.840.10008.1.2.2")
            throw new NotSupportedException("Big-endian DICOM pixel data is not supported.");
        if (TransferSyntax != "1.2.840.10008.1.2" && TransferSyntax != "1.2.840.10008.1.2.1")
            throw new NotSupportedException($"Compressed DICOM transfer syntax {TransferSyntax} is not supported by the built-in viewer.");
        _explicitVr = TransferSyntax != "1.2.840.10008.1.2";

        while (position + 8 <= _data.Length)
        {
            var element = ReadElement(ref position, _explicitVr);
            var tag = ((uint)element.Group << 16) | element.ElementNumber;
            switch (tag)
            {
                case 0x00100010: PatientName = Text(element.Value).Replace('^', ' '); break;
                case 0x00080060: Modality = Text(element.Value); break;
                case 0x00081030: StudyDescription = Text(element.Value); break;
                case 0x00280002: SamplesPerPixel = U16(element.Value, 1); break;
                case 0x00280004: PhotometricInterpretation = Text(element.Value); break;
                case 0x00280006: PlanarConfiguration = U16(element.Value, 0); break;
                case 0x00280010: Rows = U16(element.Value, 0); break;
                case 0x00280011: Columns = U16(element.Value, 0); break;
                case 0x00280100: BitsAllocated = U16(element.Value, 8); break;
                case 0x00280101: BitsStored = U16(element.Value, BitsAllocated); break;
                case 0x00281050: WindowCenter = Number(Text(element.Value)); break;
                case 0x00281051: WindowWidth = Number(Text(element.Value)); break;
                case 0x7FE00010: PixelData = element.Value; position = _data.Length; break;
            }
        }
        if (Rows <= 0 || Columns <= 0 || PixelData == null)
            throw new InvalidDataException("The DICOM file has no readable pixel data.");
    }

    public byte[] RenderPng()
    {
        if (SamplesPerPixel == 1)
        {
            var pixels = new byte[Rows * Columns];
            if (BitsAllocated <= 8)
                Array.Copy(PixelData, pixels, Math.Min(pixels.Length, PixelData.Length));
            else
            {
                var values = new ushort[pixels.Length];
                var maximum = (1 << Math.Min(BitsStored, 16)) - 1;
                var center = WindowCenter ?? maximum / 2d;
                var width = Math.Max(1, WindowWidth ?? maximum);
                var low = center - width / 2d;
                for (var i = 0; i < values.Length && i * 2 + 1 < PixelData.Length; i++)
                {
                    values[i] = BinaryPrimitives.ReadUInt16LittleEndian(PixelData.AsSpan(i * 2, 2));
                    pixels[i] = (byte)Math.Clamp((values[i] - low) * 255d / width, 0, 255);
                }
            }
            if (PhotometricInterpretation?.StartsWith("MONOCHROME1", StringComparison.OrdinalIgnoreCase) == true)
                for (var i = 0; i < pixels.Length; i++) pixels[i] = (byte)(255 - pixels[i]);
            using var image = Image.LoadPixelData<L8>(pixels, Columns, Rows);
            return Encode(image);
        }

        if (SamplesPerPixel == 3 && BitsAllocated == 8)
        {
            var rgb = new byte[Rows * Columns * 3];
            if (PlanarConfiguration == 0) Array.Copy(PixelData, rgb, Math.Min(rgb.Length, PixelData.Length));
            else
            {
                var plane = Rows * Columns;
                for (var i = 0; i < plane && i + plane * 2 < PixelData.Length; i++)
                { rgb[i * 3] = PixelData[i]; rgb[i * 3 + 1] = PixelData[i + plane]; rgb[i * 3 + 2] = PixelData[i + plane * 2]; }
            }
            using var image = Image.LoadPixelData<Rgb24>(rgb, Columns, Rows);
            return Encode(image);
        }
        throw new NotSupportedException($"DICOM samples/pixel={SamplesPerPixel}, bits={BitsAllocated} is not supported.");
    }

    private Element ReadElement(ref int position, bool explicitVr)
    {
        var group = ReadU16(position); var element = ReadU16(position + 2); position += 4;
        int length;
        if (explicitVr)
        {
            var vr = Encoding.ASCII.GetString(_data, position, 2); position += 2;
            if (vr is "OB" or "OD" or "OF" or "OL" or "OW" or "SQ" or "UC" or "UR" or "UT" or "UN")
            { position += 2; length = checked((int)ReadU32(position)); position += 4; }
            else { length = ReadU16(position); position += 2; }
        }
        else { length = checked((int)ReadU32(position)); position += 4; }
        if (length < 0 || length == -1 || position + length > _data.Length)
            throw new InvalidDataException("Unsupported or invalid DICOM element length.");
        var value = _data.AsSpan(position, length).ToArray(); position += length;
        return new Element(group, element, value);
    }

    private ushort ReadU16(int position) => BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(position, 2));
    private uint ReadU32(int position) => BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(position, 4));
    private static int U16(byte[] value, int fallback) => value.Length >= 2 ? BinaryPrimitives.ReadUInt16LittleEndian(value) : fallback;
    private static string Text(byte[] value) => Encoding.UTF8.GetString(value).Trim('\0', ' ');
    private static double? Number(string value) => double.TryParse(value?.Split('\\')[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : null;
    private static byte[] Encode(Image image) { using var stream = new MemoryStream(); image.Save(stream, new PngEncoder()); return stream.ToArray(); }
    private readonly record struct Element(ushort Group, ushort ElementNumber, byte[] Value);
}
