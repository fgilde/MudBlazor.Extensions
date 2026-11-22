using System.Buffers.Binary;
using System.Text;

namespace MudBlazor.Extensions.Office;

internal sealed class CompoundFile
{
    private const uint EndOfChain = 0xFFFFFFFE;
    private const int MiniSectorSize = 64;
    private const int MiniStreamCutoff = 4096;
    private const int MaxEntries = 65536;
    private const long MaxStreamLength = 256L * 1024 * 1024;

    private static readonly byte[] Signature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private readonly byte[] _data;
    private readonly int _sectorSize;
    private readonly uint[] _fat;
    private readonly Dictionary<string, (uint Start, long Length)> _streams = new(StringComparer.Ordinal);
    private readonly (uint Start, long Length) _miniStream;
    private uint[] _miniFat;
    private byte[] _miniData;

    private CompoundFile(byte[] data)
    {
        _data = data;
        var shift = U16(0x1E);
        if (shift is not (9 or 12)) throw Invalid("The compound file has an unsupported sector size.");
        _sectorSize = 1 << shift;
        var fatCount = U32(0x2C);
        if (fatCount > (ulong)data.Length / (ulong)_sectorSize) throw Invalid("The compound file has more FAT sectors than it is long.");
        var fatSectors = new List<uint>();
        for (var i = 0; i < Math.Min(fatCount, 109u); i++) fatSectors.Add(U32(0x4C + 4 * i));
        var seen = new HashSet<uint>();
        for (var difat = U32(0x44); fatSectors.Count < fatCount; difat = U32(Offset(difat) + _sectorSize - 4))
        {
            if (!seen.Add(difat)) throw Invalid("The compound file has a cyclic DIFAT chain.");
            var offset = Offset(difat);
            for (var i = 0; i < _sectorSize / 4 - 1 && fatSectors.Count < fatCount; i++) fatSectors.Add(U32(offset + 4 * i));
        }

        _fat = Uints(fatSectors.SelectMany(s => Sector(s)).ToArray());
        var directory = Chain(U32(0x30)).SelectMany(s => Sector(s)).ToArray();
        if (directory.Length / 128 > MaxEntries) throw Invalid("The compound file has too many directory entries.");
        if (directory.Length < 128 || directory[0x42] != 5) throw Invalid("The compound file has no root entry.");
        _miniStream = (BinaryPrimitives.ReadUInt32LittleEndian(directory.AsSpan(0x74)), Length(directory, 0, shift));
        var pending = new Stack<uint>([BinaryPrimitives.ReadUInt32LittleEndian(directory.AsSpan(0x4C))]);
        var visited = new HashSet<uint>();
        while (pending.TryPop(out var id))
        {
            if (id >= directory.Length / 128 || !visited.Add(id)) continue;
            var at = (int)id * 128;
            var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(directory.AsSpan(at + 0x40));
            if (directory[at + 0x42] == 2 && nameLength is >= 2 and <= 64 && nameLength % 2 == 0)
                _streams.TryAdd(Encoding.Unicode.GetString(directory, at, nameLength - 2),
                    (BinaryPrimitives.ReadUInt32LittleEndian(directory.AsSpan(at + 0x74)), Length(directory, at, shift)));
            pending.Push(BinaryPrimitives.ReadUInt32LittleEndian(directory.AsSpan(at + 0x44)));
            pending.Push(BinaryPrimitives.ReadUInt32LittleEndian(directory.AsSpan(at + 0x48)));
        }
    }

    public IReadOnlyCollection<string> StreamNames => _streams.Keys;

    public static CompoundFile Open(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < 512 || !data.AsSpan(0, 8).SequenceEqual(Signature)) throw Invalid("The file is not a compound file.");
        return new CompoundFile(data);
    }

    public byte[] ReadStream(string name)
    {
        if (!_streams.TryGetValue(name, out var stream)) throw Invalid($"The compound file has no stream {name}.");
        if (stream.Length >= MiniStreamCutoff) return Read(stream.Start, stream.Length, _fat, _sectorSize, _data, _sectorSize);
        if (_miniData == null)
        {
            _miniData = Read(_miniStream.Start, _miniStream.Length, _fat, _sectorSize, _data, _sectorSize);
            _miniFat = Uints(Chain(U32(0x3C)).SelectMany(s => Sector(s)).ToArray());
        }

        return Read(stream.Start, stream.Length, _miniFat, MiniSectorSize, _miniData, 0);
    }

    private static byte[] Read(uint start, long length, uint[] fat, int sectorSize, byte[] source, int origin)
    {
        if (length > MaxStreamLength || length > source.Length) throw Invalid("A compound file stream is longer than the file.");
        var result = new byte[length];
        var seen = new HashSet<uint>();
        var position = 0;
        for (var sector = start; position < length; sector = fat[sector])
        {
            if (sector >= fat.Length || !seen.Add(sector)) throw Invalid("The compound file has a broken or cyclic sector chain.");
            var offset = origin + (long)sector * sectorSize;
            var count = (int)Math.Min(sectorSize, length - position);
            if (offset + count > source.Length) throw Invalid("The compound file is truncated.");
            Array.Copy(source, offset, result, position, count);
            position += count;
        }

        return result;
    }

    private List<uint> Chain(uint start)
    {
        var chain = new List<uint>();
        var seen = new HashSet<uint>();
        for (var sector = start; sector != EndOfChain; sector = _fat[sector])
        {
            if (sector >= _fat.Length || !seen.Add(sector)) throw Invalid("The compound file has a broken or cyclic sector chain.");
            chain.Add(sector);
        }

        return chain;
    }

    private byte[] Sector(uint sector)
    {
        var offset = Offset(sector);
        return _data.AsSpan(offset, Math.Min(_sectorSize, _data.Length - offset)).ToArray();
    }

    private int Offset(uint sector)
    {
        var offset = ((long)sector + 1) * _sectorSize;
        if (sector >= 0xFFFFFFFA || offset >= _data.Length) throw Invalid("The compound file refers to a sector beyond its end.");
        return (int)offset;
    }

    private static long Length(byte[] directory, int at, int shift) =>
        shift == 9 ? BinaryPrimitives.ReadUInt32LittleEndian(directory.AsSpan(at + 0x78)) : (long)Math.Min(BinaryPrimitives.ReadUInt64LittleEndian(directory.AsSpan(at + 0x78)), (ulong)long.MaxValue);

    private static uint[] Uints(byte[] bytes)
    {
        var values = new uint[bytes.Length / 4];
        for (var i = 0; i < values.Length; i++) values[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4 * i));
        return values;
    }

    private int U16(int at) => BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(at));

    private uint U32(int at) => at + 4 <= _data.Length ? BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(at)) : throw Invalid("The compound file is truncated.");

    private static InvalidDataException Invalid(string message) => new(message);
}
