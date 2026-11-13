using System.Buffers.Binary;

namespace MudBlazor.Extensions.Office.Notebook;

internal readonly record struct OneId(Guid Guid, uint N);

internal sealed record OneObject(uint Jcid, long Offset, long Length, Dictionary<uint, Guid> Ids);

internal sealed class OneSpace(OneStore store)
{
    public Dictionary<OneId, OneObject> Objects { get; } = [];

    public Dictionary<uint, OneId> Roots { get; } = [];

    public OneObject Object(OneId id, uint jcid) => Objects.TryGetValue(id, out var o) && o.Jcid == jcid ? o : null;

    public OneObject Root(uint role, uint jcid) => Roots.TryGetValue(role, out var id) ? Object(id, jcid) : null;

    public OneProperties Properties(OneObject o) => store.Properties(o);
}

internal sealed class OneStore
{
    private const int HeaderSize = 1024;
    private const int MaxNodes = 1_000_000;
    private const ulong FragmentHeader = 0xA4567AB1F5F7F4C4;
    private const ulong FragmentFooter = 0x8BC215C38233BA4B;
    private const uint DefaultContent = 1;
    private static readonly Guid SectionFile = new("7B5C52E4-D88C-4DA7-AEB1-5378D02996D3");
    private static readonly Guid TableOfContentsFile = new("43FF2FA1-EFD9-4C76-9EE2-10EA5722765F");
    private static readonly Guid RevisionStoreFormat = new("109ADD3F-911B-49F5-A5D0-1791EDC8AED8");
    private static readonly Guid PackagedFormat = new("638DE92F-A6D4-4BC1-9A36-B3FC2511A5B7");

    private readonly byte[] _data;
    private readonly Dictionary<uint, uint> _committed = [];
    private int _nodes;
    private long _parsed;

    private OneStore(byte[] data) => _data = data;

    public OneId Root { get; private set; }

    public Dictionary<OneId, OneSpace> Spaces { get; } = [];

    public static OneStore Read(Stream stream, NotebookLoadOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var store = new OneStore(ReadAll(stream, Math.Min(options.MaxTotalBytes, options.MaxEntryBytes)));
        store.ReadHeader();
        return store;
    }

    public OneProperties Properties(OneObject o)
    {
        if ((_parsed += o.Length) > 4L * _data.Length + (1 << 20)) throw Damaged("objects are referenced too often");
        return OneProperties.Read(At(o.Offset, o.Length), o.Ids);
    }

    internal static InvalidDataException Damaged(string what) => new($"The OneNote file is damaged: {what}.");

    private static byte[] ReadAll(Stream stream, long limit)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = stream.Read(chunk)) > 0)
        {
            if (buffer.Length + read > Math.Min(limit, Array.MaxLength)) throw new InvalidDataException($"The OneNote file is larger than {limit} bytes.");
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private void ReadHeader()
    {
        if (_data.Length < HeaderSize) throw new InvalidDataException("The file is not a OneNote section (.one): it is too short.");
        var header = _data.AsSpan(0, HeaderSize);
        var fileType = new Guid(header[..16]);
        var format = new Guid(header.Slice(48, 16));
        if (format == PackagedFormat)
            throw new InvalidDataException("OneNote files in the packaged format of SharePoint and OneDrive (FSSHTTP) are not supported. Export the section from OneNote as a .one file.");
        if (fileType == TableOfContentsFile) throw new InvalidDataException("The file is a OneNote table of contents (.onetoc2), which holds no pages. Open a section file (.one).");
        if (fileType != SectionFile || format != RevisionStoreFormat) throw new InvalidDataException("The file is not a OneNote section (.one).");

        ReadTransactions(Reference64(header, 160), BinaryPrimitives.ReadUInt32LittleEndian(header[96..]));
        var spaces = new List<Node>();
        foreach (var node in ReadList(Reference64(header, 172)))
        {
            if (node.Id == 0x004) Root = Id(node, 0);
            else if (node.Id == 0x008) spaces.Add(node);
        }

        foreach (var node in spaces)
            Spaces.TryAdd(Id(node, 0), ReadSpace(node));
        if (!Spaces.ContainsKey(Root)) throw Damaged("the root object space is missing");
    }

    private void ReadTransactions((long Offset, long Length) fragment, uint transactions)
    {
        var pending = new Dictionary<uint, uint>();
        var visited = new HashSet<long>();
        uint done = 0;
        while (done < transactions && fragment.Offset >= 0)
        {
            if (fragment.Length < 12 || !visited.Add(fragment.Offset)) throw Damaged("transaction log");
            var data = At(fragment.Offset, fragment.Length);
            var count = (data.Length - 12) / 8;
            for (var i = 0; i < count && done < transactions; i++)
            {
                var source = BinaryPrimitives.ReadUInt32LittleEndian(data[(i * 8)..]);
                if (source == 1)
                {
                    foreach (var (list, nodes) in pending) _committed[list] = nodes;
                    pending.Clear();
                    done++;
                }
                else if (source != 0) pending[source] = BinaryPrimitives.ReadUInt32LittleEndian(data[(i * 8 + 4)..]);
            }

            fragment = Reference64(data, data.Length - 12);
        }
    }

    private OneSpace ReadSpace(Node reference)
    {
        var space = new OneSpace(this);
        var revisionList = ReadList(reference).LastOrDefault(n => n.Id == 0x010);
        if (revisionList.Id == 0) return space;

        var revisions = new Dictionary<OneId, Revision>();
        var latest = new Dictionary<(OneId Context, uint Role), OneId>();
        Revision current = null;
        var ids = new Dictionary<uint, Guid>();
        foreach (var node in ReadList(revisionList))
        {
            switch (node.Id)
            {
                case 0x01B or 0x01E or 0x01F:
                    current = new Revision(Id(node, 0), Id(node, 20));
                    revisions[current.Id] = current;
                    latest[(node.Id == 0x01F ? Id(node, 46) : default, U32(node, node.Id == 0x01B ? 48 : 40))] = current.Id;
                    break;
                case 0x01C:
                    current = null;
                    break;
                case 0x021 or 0x022:
                    ids = [];
                    break;
                case 0x024:
                    ids[U32(node, 0)] = Guid(node, 4);
                    break;
                case 0x059 when current != null:
                    current.Roots[U32(node, 4)] = Compact(U32(node, 0), ids);
                    break;
                case 0x05A when current != null:
                    current.Roots[U32(node, 20)] = Id(node, 0);
                    break;
                case 0x05C:
                    latest[(default, U32(node, 20))] = Id(node, 0);
                    break;
                case 0x05D:
                    latest[(Id(node, 24), U32(node, 20))] = Id(node, 0);
                    break;
                case 0x07C:
                    throw new InvalidDataException("Password-protected OneNote sections are not supported.");
                case 0x0B0 when current != null:
                    ReadGroup(node, current);
                    break;
                case 0x0A4 or 0x0A5 or 0x0C4 or 0x0C5 when current != null:
                    Declare(node, ids, current);
                    break;
            }
        }

        var chain = new List<Revision>();
        var seen = new HashSet<OneId>();
        for (var r = latest.TryGetValue((default, DefaultContent), out var rid) ? revisions.GetValueOrDefault(rid) : null;
             r != null && seen.Add(r.Id);
             r = revisions.GetValueOrDefault(r.Dependent))
            chain.Add(r);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            foreach (var (id, o) in chain[i].Objects) space.Objects[id] = o;
            foreach (var (role, id) in chain[i].Roots) space.Roots[role] = id;
        }

        return space;
    }

    private void ReadGroup(Node reference, Revision revision)
    {
        var ids = new Dictionary<uint, Guid>();
        foreach (var node in ReadList(reference))
        {
            if (node.Id is 0x021 or 0x022) ids = [];
            else if (node.Id == 0x024) ids[U32(node, 0)] = Guid(node, 4);
            else if (node.Id is 0x0A4 or 0x0A5 or 0x0C4 or 0x0C5) Declare(node, ids, revision);
            else if (node.Id == 0x07C) throw new InvalidDataException("Password-protected OneNote sections are not supported.");
        }
    }

    private void Declare(Node node, Dictionary<uint, Guid> ids, Revision revision)
    {
        if (node.RefOffset < 0) return;
        revision.Objects[Compact(U32(node, 0), ids)] = new OneObject(U32(node, 4), node.RefOffset, node.RefLength, ids);
    }

    private List<Node> ReadList(Node reference) =>
        reference.RefOffset < 0 ? throw Damaged("a file node list is missing") : ReadList((reference.RefOffset, reference.RefLength));

    private List<Node> ReadList((long Offset, long Length) fragment)
    {
        var nodes = new List<Node>();
        var visited = new HashSet<long>();
        uint? list = null;
        var limit = uint.MaxValue;
        uint sequence = 0;
        while (fragment.Offset >= 0 && nodes.Count < limit)
        {
            if (fragment.Length < 36 || !visited.Add(fragment.Offset)) throw Damaged("file node list");
            var data = At(fragment.Offset, fragment.Length);
            var id = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
            if (BinaryPrimitives.ReadUInt64LittleEndian(data) != FragmentHeader || BinaryPrimitives.ReadUInt64LittleEndian(data[^8..]) != FragmentFooter)
                throw Damaged("file node list");
            if (list == null) limit = _committed.GetValueOrDefault((list = id).Value, uint.MaxValue);
            else if (id != list || BinaryPrimitives.ReadUInt32LittleEndian(data[12..]) != ++sequence) throw Damaged("file node list fragments");

            var end = data.Length - 20;
            for (var o = 16; nodes.Count < limit && o + 4 <= end;)
            {
                var header = BinaryPrimitives.ReadUInt32LittleEndian(data[o..]);
                var nodeId = header & 0x3FF;
                if (nodeId is 0 or 0xFF) break;
                var size = (int)(header >> 10 & 0x1FFF);
                if (size < 4 || o + size > end || ++_nodes > MaxNodes) throw Damaged("file node");
                var body = data.Slice(o + 4, size - 4);
                var (refOffset, refLength, used) = (header >> 27 & 0xF) is 1 or 2 ? ChunkReference(body, header >> 23 & 3, header >> 25 & 3) : (-1L, 0L, 0);
                nodes.Add(new Node(nodeId, fragment.Offset + o + 4 + used, fragment.Offset + o + size, refOffset, refLength));
                o += size;
            }

            fragment = Reference64(data, end);
        }

        return nodes;
    }

    private (long Offset, long Length, int Used) ChunkReference(ReadOnlySpan<byte> body, uint offsetFormat, uint lengthFormat)
    {
        var offsetSize = offsetFormat == 0 ? 8 : offsetFormat == 2 ? 2 : 4;
        var lengthSize = lengthFormat switch { 0 => 4, 1 => 8, 2 => 1, _ => 2 };
        if (body.Length < offsetSize + lengthSize) throw Damaged("file node");
        var offset = offsetSize switch
        {
            8 => BinaryPrimitives.ReadUInt64LittleEndian(body),
            4 => BinaryPrimitives.ReadUInt32LittleEndian(body),
            _ => BinaryPrimitives.ReadUInt16LittleEndian(body)
        };
        var nil = offset == (offsetSize == 8 ? ulong.MaxValue : offsetSize == 4 ? uint.MaxValue : ushort.MaxValue);
        var rest = body[offsetSize..];
        var length = lengthSize switch
        {
            8 => BinaryPrimitives.ReadUInt64LittleEndian(rest),
            4 => BinaryPrimitives.ReadUInt32LittleEndian(rest),
            2 => BinaryPrimitives.ReadUInt16LittleEndian(rest),
            _ => rest[0]
        };
        if (offsetFormat >= 2) offset *= 8;
        if (lengthFormat >= 2) length *= 8;
        if (nil) return (-1, 0, offsetSize + lengthSize);
        Check(offset, length);
        return ((long)offset, (long)length, offsetSize + lengthSize);
    }

    private (long Offset, long Length) Reference64(ReadOnlySpan<byte> data, int at)
    {
        if (at < 0 || at + 12 > data.Length) throw Damaged("file chunk reference");
        var offset = BinaryPrimitives.ReadUInt64LittleEndian(data[at..]);
        var length = BinaryPrimitives.ReadUInt32LittleEndian(data[(at + 8)..]);
        if (offset == ulong.MaxValue || offset == 0 && length == 0) return (-1, 0);
        Check(offset, length);
        return ((long)offset, length);
    }

    private void Check(ulong offset, ulong length)
    {
        if (offset > (ulong)_data.Length || length > (ulong)_data.Length - offset) throw Damaged("a reference points outside the file");
    }

    private ReadOnlySpan<byte> At(long offset, long length)
    {
        Check((ulong)offset, (ulong)length);
        return _data.AsSpan((int)offset, (int)length);
    }

    private ReadOnlySpan<byte> Field(Node node, int offset, int length) =>
        node.Body + offset + length <= node.End ? _data.AsSpan((int)node.Body + offset, length) : throw Damaged("file node");

    private uint U32(Node node, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(Field(node, offset, 4));

    private Guid Guid(Node node, int offset) => new(Field(node, offset, 16));

    private OneId Id(Node node, int offset) => new(Guid(node, offset), U32(node, offset + 16));

    internal static OneId Compact(uint value, Dictionary<uint, Guid> ids) => ids.TryGetValue(value >> 8, out var guid) ? new OneId(guid, value & 0xFF) : default;

    private readonly record struct Node(uint Id, long Body, long End, long RefOffset, long RefLength);

    private sealed class Revision(OneId id, OneId dependent)
    {
        public OneId Id => id;

        public OneId Dependent => dependent;

        public Dictionary<OneId, OneObject> Objects { get; } = [];

        public Dictionary<uint, OneId> Roots { get; } = [];
    }
}
