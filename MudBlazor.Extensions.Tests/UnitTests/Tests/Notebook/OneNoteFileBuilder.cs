using System.Buffers.Binary;
using System.Text;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

internal sealed record OneParagraph(string Text, int Indent = 0, bool Bold = false);

internal sealed record OneOutline(float X, float Y, float Width, params OneParagraph[] Paragraphs);

internal sealed record OnePage(string Title, DateTime Created, params OneOutline[] Outlines);

internal sealed class OneNoteFileBuilder
{
    public static readonly Guid SectionFile = new("7B5C52E4-D88C-4DA7-AEB1-5378D02996D3");
    public static readonly Guid StoreFormat = new("109ADD3F-911B-49F5-A5D0-1791EDC8AED8");
    private static readonly Guid FileGuid = new("5D1E0C42-6A1B-4B0E-9C3E-2F6A7B8C9D01");

    private readonly MemoryStream _out = new();
    private readonly List<(uint List, uint Nodes)> _log = [];
    private uint _nextList = 0x10;

    public static byte[] Build(params OnePage[] pages) => new OneNoteFileBuilder().Write(pages);

    private byte[] Write(OnePage[] pages)
    {
        _out.Write(new byte[1024]);
        var root = new Ex(FileGuid, 1);
        var pageSpaces = pages.Select((page, i) => (Id: new Ex(FileGuid, (uint)(i + 2)), page)).ToList();

        var spaces = new List<byte[]>();
        foreach (var (id, page) in pageSpaces) spaces.Add(SpaceReference(id, PageObjects(page)));

        var sectionGuid = Guid.NewGuid();
        var series = new Props().Osids(0x2C001D63, pageSpaces.Select(p => p.Id).ToArray());
        var section = new Props().Oids(0x24001C20, new Ex(sectionGuid, 2));
        spaces.Insert(0, SpaceReference(root, new Space(sectionGuid, [(0x00060007, section), (0x00060008, series)], new Dictionary<uint, uint> { [1] = 1 })));

        var rootList = List([Node(0x004, root.Bytes()), .. spaces]);
        var log = new MemoryStream();
        foreach (var (list, nodes) in _log) log.Write(U32(list).Concat(U32(nodes)).ToArray());
        log.Write(U32(1).Concat(U32(0)).ToArray());
        log.Write(Nil());
        var logChunk = Chunk(log.ToArray());

        var data = _out.ToArray();
        SectionFile.ToByteArray().CopyTo(data, 0);
        Guid.NewGuid().ToByteArray().CopyTo(data, 16);
        StoreFormat.ToByteArray().CopyTo(data, 48);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(96), 1);
        Reference64(data, 160, logChunk);
        Reference64(data, 172, rootList);
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(196), (ulong)data.Length);
        return data;
    }

    private Space PageObjects(OnePage page)
    {
        var guid = Guid.NewGuid();
        var objects = new List<(uint Jcid, Props Props)>();
        Ex Add(uint jcid, Props props)
        {
            objects.Add((jcid, props));
            return new Ex(guid, (uint)objects.Count);
        }

        Ex[] Elements(OneParagraph[] paragraphs, ref int i, int indent)
        {
            var result = new List<Ex>();
            while (i < paragraphs.Length && paragraphs[i].Indent == indent)
            {
                var paragraph = paragraphs[i++];
                var children = i < paragraphs.Length && paragraphs[i].Indent > indent ? Elements(paragraphs, ref i, indent + 1) : [];
                var text = new Props().Text(paragraph.Text);
                if (paragraph.Bold) text.Oids(0x24001E13, Add(0x0012004D, new Props().Bool(0x08001C04, true)));
                var element = new Props().Oids(0x24001C1F, Add(0x0006000E, text));
                if (children.Length > 0) element.Oids(0x24001C20, children);
                result.Add(Add(0x0006000D, element));
            }

            return [.. result];
        }

        var outlines = new List<Ex>();
        foreach (var outline in page.Outlines)
        {
            var i = 0;
            outlines.Add(Add(0x0006000C, new Props().Oids(0x24001C20, Elements(outline.Paragraphs, ref i, 0))
                .Float(0x14001C14, outline.X).Float(0x14001C15, outline.Y).Float(0x14001C1B, outline.Width)));
        }

        var pageNode = Add(0x0006000B, new Props().Oids(0x24001C20, [.. outlines]).U32(0x14001D7A, 1_400_000_000));
        var manifest = Add(0x00060037, new Props().Oids(0x24001C1F, pageNode));
        var meta = Add(0x00020030, new Props().Bytes(0x1C001CF3, Encoding.Unicode.GetBytes(page.Title + "\0"))
            .U64(0x18001C65, (ulong)page.Created.ToFileTimeUtc()).U32(0x14001DFF, 1));
        return new Space(guid, objects, new Dictionary<uint, uint> { [1] = manifest.N, [2] = meta.N });
    }

    private byte[] SpaceReference(Ex id, Space space)
    {
        var groupId = new Ex(Guid.NewGuid(), 1);
        var group = new List<byte[]> { Node(0x0B4, groupId.Bytes()), Node(0x022, []) };
        var ids = new List<Guid> { space.Guid, FileGuid };
        for (var i = 0; i < ids.Count; i++) group.Add(Node(0x024, U32((uint)i).Concat(ids[i].ToByteArray()).ToArray()));
        group.Add(Node(0x028, []));
        for (var i = 0; i < space.Objects.Count; i++)
        {
            var (jcid, props) = space.Objects[i];
            var data = Chunk(props.ToBytes(ids));
            group.Add(Node(0x0A4, U32(Compact(new Ex(space.Guid, (uint)i + 1), ids)).Concat(U32(jcid)).Concat(new byte[] { 0, 1 }).ToArray(), data, compressed: true));
        }

        group.Add(Node(0x0B8, []));
        var groupList = List(group);

        var rid = new Ex(Guid.NewGuid(), 1);
        var revision = new List<byte[]>
        {
            Node(0x014, id.Bytes().Concat(U32(0)).ToArray()),
            Node(0x01E, rid.Bytes().Concat(new Ex(Guid.Empty, 0).Bytes()).Concat(U32(1)).Concat(new byte[2]).ToArray()),
            Node(0x0B0, groupId.Bytes(), groupList)
        };
        foreach (var (role, n) in space.Roots) revision.Add(Node(0x05A, new Ex(space.Guid, n).Bytes().Concat(U32(role)).ToArray()));
        revision.Add(Node(0x01C, []));
        var revisionList = List(revision);
        var manifestList = List([Node(0x00C, id.Bytes()), Node(0x010, [], revisionList)]);
        return Node(0x008, id.Bytes(), manifestList);
    }

    private (long Offset, long Length) List(IReadOnlyList<byte[]> nodes)
    {
        var list = _nextList++;
        var fragment = new MemoryStream();
        fragment.Write(BitConverter.GetBytes(0xA4567AB1F5F7F4C4));
        fragment.Write(U32(list));
        fragment.Write(U32(0));
        foreach (var node in nodes) fragment.Write(node);
        fragment.Write(Nil());
        fragment.Write(BitConverter.GetBytes(0x8BC215C38233BA4B));
        _log.Add((list, (uint)nodes.Count));
        return Chunk(fragment.ToArray());
    }

    private (long Offset, long Length) Chunk(byte[] data)
    {
        var offset = _out.Length;
        _out.Write(data);
        while (_out.Length % 8 != 0) _out.WriteByte(0);
        return (offset, data.Length);
    }

    private static byte[] Node(uint id, byte[] body, (long Offset, long Length)? reference = null, bool compressed = false)
    {
        var refBytes = reference is not { } r ? []
            : compressed ? U32((uint)(r.Offset / 8)).Concat(BitConverter.GetBytes((ushort)((r.Length + 7) / 8))).ToArray()
            : BitConverter.GetBytes((ulong)r.Offset).Concat(U32((uint)r.Length)).ToArray();
        var size = 4 + refBytes.Length + body.Length;
        var baseType = reference == null ? 0u : id is 0x008 or 0x010 or 0x0B0 ? 2u : 1u;
        var formats = compressed ? 3u << 23 | 3u << 25 : 0u;
        return U32(id | (uint)size << 10 | formats | baseType << 27).Concat(refBytes).Concat(body).ToArray();
    }

    private static void Reference64(byte[] data, int at, (long Offset, long Length) reference)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(at), (ulong)reference.Offset);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(at + 8), (uint)reference.Length);
    }

    private static byte[] Nil() => BitConverter.GetBytes(ulong.MaxValue).Concat(U32(0)).ToArray();

    private static byte[] U32(uint value) => BitConverter.GetBytes(value);

    private static uint Compact(Ex id, List<Guid> ids) => (uint)ids.IndexOf(id.Guid) << 8 | id.N;

    private sealed record Space(Guid Guid, List<(uint Jcid, Props Props)> Objects, Dictionary<uint, uint> Roots);

    private readonly record struct Ex(Guid Guid, uint N)
    {
        public byte[] Bytes() => Guid.ToByteArray().Concat(U32(N)).ToArray();
    }

    private sealed class Props
    {
        private readonly List<(uint Prid, byte[] Data, Ex[] Oids, Ex[] Osids)> _items = [];

        public Props Oids(uint prid, params Ex[] ids) => Add(prid, OneNoteFileBuilder.U32((uint)ids.Length), ids, []);

        public Props Osids(uint prid, params Ex[] ids) => Add(prid, OneNoteFileBuilder.U32((uint)ids.Length), [], ids);

        public Props Bool(uint prid, bool value) => Add(value ? prid | 0x80000000 : prid, [], [], []);

        public Props U32(uint prid, uint value) => Add(prid, OneNoteFileBuilder.U32(value), [], []);

        public Props U64(uint prid, ulong value) => Add(prid, BitConverter.GetBytes(value), [], []);

        public Props Float(uint prid, float value) => Add(prid, BitConverter.GetBytes(value), [], []);

        public Props Bytes(uint prid, byte[] value) => Add(prid, OneNoteFileBuilder.U32((uint)value.Length).Concat(value).ToArray(), [], []);

        public Props Text(string text) => text.All(c => c <= 0xFF && c is < '\u0080' or >= ' ')
            ? Bytes(0x1C003498, text.Select(c => (byte)c).ToArray())
            : Bytes(0x1C001C22, Encoding.Unicode.GetBytes(text));

        public byte[] ToBytes(List<Guid> ids)
        {
            var oids = _items.SelectMany(i => i.Oids).ToList();
            var osids = _items.SelectMany(i => i.Osids).ToList();
            var result = new List<byte>(OneNoteFileBuilder.U32((uint)oids.Count | (osids.Count == 0 ? 0x80000000 : 0)));
            foreach (var id in oids) result.AddRange(OneNoteFileBuilder.U32(Compact(id, ids)));
            if (osids.Count > 0)
            {
                result.AddRange(OneNoteFileBuilder.U32((uint)osids.Count));
                foreach (var id in osids) result.AddRange(OneNoteFileBuilder.U32(Compact(id, ids)));
            }

            result.AddRange(BitConverter.GetBytes((ushort)_items.Count));
            foreach (var item in _items) result.AddRange(OneNoteFileBuilder.U32(item.Prid));
            foreach (var item in _items) result.AddRange(item.Data);
            return [.. result];
        }

        private Props Add(uint prid, byte[] data, Ex[] oids, Ex[] osids)
        {
            _items.Add((prid, data, oids, osids));
            return this;
        }
    }
}
