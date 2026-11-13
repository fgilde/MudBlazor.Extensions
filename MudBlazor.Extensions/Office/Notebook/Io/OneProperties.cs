using System.Buffers.Binary;

namespace MudBlazor.Extensions.Office.Notebook;

internal sealed class OneProperties
{
    private const int MaxNesting = 8;
    private readonly Dictionary<uint, object> _values = [];

    private OneProperties()
    {
    }

    public static OneProperties Read(ReadOnlySpan<byte> data, Dictionary<uint, Guid> ids)
    {
        var reader = new Reader(data, ids);
        var oids = reader.Stream(out var extended, out var noOsids);
        var osids = noOsids ? [] : reader.Stream(out extended, out _);
        if (!noOsids && extended) reader.Stream(out _, out _);
        var properties = new OneProperties();
        reader.Set(properties._values, oids, osids, 0);
        return properties;
    }

    public byte[] Bytes(uint id) => _values.GetValueOrDefault(id) as byte[];

    public ulong? Number(uint id) => _values.GetValueOrDefault(id) as ulong?;

    public float? Float(uint id) => Number(id) is { } n && BitConverter.UInt32BitsToSingle((uint)n) is var f && float.IsFinite(f) ? f : null;

    public bool Bool(uint id) => _values.GetValueOrDefault(id) is true;

    public IReadOnlyList<OneId> Ids(uint id) => _values.GetValueOrDefault(id) as OneId[] ?? [];

    private ref struct Reader(ReadOnlySpan<byte> data, Dictionary<uint, Guid> ids)
    {
        private readonly ReadOnlySpan<byte> _data = data;
        private int _at;
        private int _nextOid;
        private int _nextOsid;

        public OneId[] Stream(out bool extended, out bool noOsids)
        {
            var header = UInt32();
            var count = (int)(header & 0xFFFFFF);
            if (count > (_data.Length - _at) / 4) throw OneStore.Damaged("object references");
            extended = (header & 0x40000000) != 0;
            noOsids = (header & 0x80000000) != 0;
            var result = new OneId[count];
            for (var i = 0; i < count; i++) result[i] = OneStore.Compact(UInt32(), ids);
            return result;
        }

        public void Set(Dictionary<uint, object> values, OneId[] oids, OneId[] osids, int depth)
        {
            if (depth > MaxNesting) throw OneStore.Damaged("property sets nested too deeply");
            var count = UInt16();
            if (count > (_data.Length - _at) / 4) throw OneStore.Damaged("property set");
            var prids = new uint[count];
            for (var i = 0; i < count; i++) prids[i] = UInt32();
            foreach (var prid in prids)
            {
                var type = prid >> 26 & 0x1F;
                object value = type switch
                {
                    0x1 => null,
                    0x2 => (prid & 0x80000000) != 0,
                    0x3 => (ulong)Take(1)[0],
                    0x4 => (ulong)BinaryPrimitives.ReadUInt16LittleEndian(Take(2)),
                    0x5 => (ulong)BinaryPrimitives.ReadUInt32LittleEndian(Take(4)),
                    0x6 => BinaryPrimitives.ReadUInt64LittleEndian(Take(8)),
                    0x7 => Take(Length()).ToArray(),
                    0x8 => Next(oids, ref _nextOid, 1),
                    0x9 => Next(oids, ref _nextOid, Length()),
                    0xA => Next(osids, ref _nextOsid, 1),
                    0xB => Next(osids, ref _nextOsid, Length()),
                    0xC => null,
                    0xD => Discard(Length()),
                    0x10 => Array(oids, osids, depth),
                    0x11 => Nested(oids, osids, depth),
                    _ => throw OneStore.Damaged("unknown property type")
                };
                values[prid & 0x7FFFFFFF] = value;
            }
        }

        private object Array(OneId[] oids, OneId[] osids, int depth)
        {
            var count = Length();
            if (count == 0) return null;
            UInt32();
            if (count > (_data.Length - _at) / 2) throw OneStore.Damaged("property array");
            for (var i = 0; i < count; i++) Set([], oids, osids, depth + 1);
            return null;
        }

        private object Nested(OneId[] oids, OneId[] osids, int depth)
        {
            Set([], oids, osids, depth + 1);
            return null;
        }

        private static object Discard(int _) => null;

        private static OneId[] Next(OneId[] source, ref int next, int count)
        {
            if (count > source.Length - next) throw OneStore.Damaged("object references");
            var result = source.AsSpan(next, count).ToArray();
            next += count;
            return result;
        }

        private int Length()
        {
            var length = UInt32();
            return length <= int.MaxValue ? (int)length : throw OneStore.Damaged("property length");
        }

        private uint UInt32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(4));

        private ushort UInt16() => BinaryPrimitives.ReadUInt16LittleEndian(Take(2));

        private ReadOnlySpan<byte> Take(int count)
        {
            if (count > _data.Length - _at) throw OneStore.Damaged("property data");
            var result = _data.Slice(_at, count);
            _at += count;
            return result;
        }
    }
}
