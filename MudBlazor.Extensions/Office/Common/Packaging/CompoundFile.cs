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
    private const int MaxDirectorySectors = 64;

    private static readonly byte[] Signature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private readonly byte[] _data;
    private readonly int _sectorSize;
    private readonly uint[] _fat;
    private readonly Dictionary<string, (uint Start, long Length)> _streams = new(StringComparer.Ordinal);
    private readonly (uint Start, long Length) _miniStream;
    private (byte[] Data, uint[] Fat) _mini;

    private CompoundFile(byte[] data)
    {
        _data = data;
        var shift = U16(0x1E);
        if (shift is not (9 or 12) || U16(0x1A) != (shift == 9 ? 3 : 4)) throw Invalid("The compound file has an unsupported version or sector size.");
        if (U16(0x1C) != 0xFFFE || U16(0x20) != 6 || U32(0x38) != MiniStreamCutoff) throw Invalid("The compound file has an invalid header.");
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

        _fat = Uints(Sectors(fatSectors));
        var directory = Sectors(Chain(U32(0x30), MaxEntries * 128 / _sectorSize));
        if (!HasRoot(directory)) throw Invalid("The compound file has no root entry.");
        _miniStream = (BinaryPrimitives.ReadUInt32LittleEndian(directory.AsSpan(0x74)), Length(directory, 0, shift));
        foreach (var (name, type, at) in RootEntries(directory))
            if (type == 2) _streams.TryAdd(name, (BinaryPrimitives.ReadUInt32LittleEndian(directory.AsSpan(at + 0x74)), Length(directory, at, shift)));
    }

    public IReadOnlyCollection<string> StreamNames => _streams.Keys;

    internal static HashSet<string> RootEntryNames(Stream content, long start)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var header = new byte[512];
        content.Position = start;
        if (content.ReadAtLeast(header, header.Length, false) < header.Length) return names;
        var shift = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(0x1E));
        if (shift is not (9 or 12)) return names;
        var size = 1 << shift;
        var fat = Enumerable.Range(0, (int)Math.Min(BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x2C)), 109u))
            .Select(i => BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x4C + 4 * i))).ToList();
        var buffer = new byte[size];
        using var directory = new MemoryStream();
        var sector = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x30));
        for (var count = 0; count < MaxDirectorySectors && Sector(content, start, sector, buffer); count++)
        {
            directory.Write(buffer);
            var index = (int)(sector / (size / 4));
            if (index >= fat.Count || !Sector(content, start, fat[index], buffer)) break;
            sector = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan((int)(sector % (size / 4)) * 4));
        }

        var entries = directory.ToArray();
        if (HasRoot(entries))
            foreach (var (name, type, _) in RootEntries(entries))
                if (type is 1 or 2) names.Add(name);
        return names;
    }

    private static bool HasRoot(byte[] directory) => directory.Length >= 128 && directory[0x42] == 5;

    private static IEnumerable<(string Name, int Type, int At)> RootEntries(byte[] directory)
    {
        var pending = new Stack<uint>([BinaryPrimitives.ReadUInt32LittleEndian(directory.AsSpan(0x4C))]);
        var visited = new HashSet<uint>();
        while (pending.TryPop(out var id))
        {
            if (id >= directory.Length / 128 || !visited.Add(id)) continue;
            var at = (int)id * 128;
            var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(directory.AsSpan(at + 0x40));
            if (nameLength is >= 2 and <= 64 && nameLength % 2 == 0) yield return (Encoding.Unicode.GetString(directory, at, nameLength - 2), directory[at + 0x42], at);
            pending.Push(BinaryPrimitives.ReadUInt32LittleEndian(directory.AsSpan(at + 0x44)));
            pending.Push(BinaryPrimitives.ReadUInt32LittleEndian(directory.AsSpan(at + 0x48)));
        }
    }

    private static bool Sector(Stream content, long start, uint sector, byte[] buffer)
    {
        var offset = start + ((long)sector + 1) * buffer.Length;
        if (sector >= 0xFFFFFFFA || offset + buffer.Length > content.Length) return false;
        content.Position = offset;
        return content.ReadAtLeast(buffer, buffer.Length, false) == buffer.Length;
    }

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
        if (_mini.Data == null)
        {
            var data = Read(_miniStream.Start, _miniStream.Length, _fat, _sectorSize, _data, _sectorSize);
            _mini = (data, Uints(Sectors(Chain(U32(0x3C), _fat.Length))));
        }

        return Read(stream.Start, stream.Length, _mini.Fat, MiniSectorSize, _mini.Data, 0);
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

    private List<uint> Chain(uint start, int maxSectors)
    {
        var chain = new List<uint>();
        var seen = new HashSet<uint>();
        for (var sector = start; sector != EndOfChain; sector = _fat[sector])
        {
            if (sector >= _fat.Length || !seen.Add(sector)) throw Invalid("The compound file has a broken or cyclic sector chain.");
            if (chain.Count == maxSectors) throw Invalid("The compound file has a chain with too many sectors.");
            Offset(sector);
            chain.Add(sector);
        }

        return chain;
    }

    private byte[] Sectors(List<uint> sectors)
    {
        var result = new byte[(long)sectors.Count * _sectorSize];
        var length = 0;
        foreach (var sector in sectors)
        {
            var offset = Offset(sector);
            var count = Math.Min(_sectorSize, _data.Length - offset);
            Array.Copy(_data, offset, result, length, count);
            length += count;
        }

        return length == result.Length ? result : result[..length];
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
