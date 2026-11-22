using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using MudBlazor.Extensions.Helper.Internal;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class OdtLimitTests
{
    private static readonly PackageLimits Defaults = PackageLimits.Default;

    [Fact]
    public void Odt_Load_DeepNestingInCitationTitleAndMeta_ThrowsInvalidData()
    {
        var deep = Nest("<text:span>", "</text:span>", 100000, "x");

        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder().Body(
            $"<text:p>a<text:note text:note-class=\"footnote\"><text:note-citation>{deep}</text:note-citation><text:note-body/></text:note></text:p>").Build()));
        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder().File("Pictures/a.png", ObjectCommandTests.Png(2, 2)).Body(
            $"<text:p><draw:frame text:anchor-type=\"as-char\"><draw:image xlink:href=\"Pictures/a.png\"/><svg:title>{deep}</svg:title></draw:frame></text:p>").Build()));
        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder().Meta($"<dc:title>{deep}</dc:title>").Build()));
        Assert.Equal("x", Load(new OdtBuilder().Meta($"<dc:title>{Nest("<text:span>", "</text:span>", 200, "x")}</dc:title>").Build()).Properties.Title);
    }

    [Fact]
    public void Odt_Load_RepeatedHeavyCell_IsRejected_RepeatedRowsAreDistinct()
    {
        var heavy = new string('x', 2000);

        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder().Body(
            $"<table:table><table:table-row table:number-rows-repeated=\"1000\"><table:table-cell table:number-columns-repeated=\"63\"><text:p>{heavy}</text:p></table:table-cell>" +
            "</table:table-row></table:table>").Build()));
        Assert.Throws<InvalidDataException>(() => Load(new OdtBuilder().Body(
            $"<table:table><table:table-row table:number-rows-repeated=\"1000\"><table:table-cell><text:p>{heavy}{heavy}{heavy}</text:p></table:table-cell>" +
            "</table:table-row></table:table>").Build(), Defaults with { Budget = 4 * 1024 * 1024 }));
        var table = (Table)new OdtBuilder().Body(
                "<table:table><table:table-row table:number-rows-repeated=\"3\"><table:table-cell table:number-columns-repeated=\"2\"><text:p>r</text:p></table:table-cell>" +
                "</table:table-row></table:table>")
            .Load().Blocks[0];

        var cells = table.Rows.SelectMany(r => r.Cells).ToList();
        Assert.Equal(6, cells.Count);
        Assert.Equal(6, cells.Select(c => c.Blocks[0].Id).Distinct().Count());
        Assert.Equal(6, cells.Select(c => c.Blocks).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Equal(3, table.Rows.Select(r => r.Cells).Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void Odt_Load_ManyFramesOnOnePicture_ReadOnceAndStoredOnce()
    {
        var png = ObjectCommandTests.Png(400, 400, 7);
        var frame = "<draw:frame text:anchor-type=\"as-char\" svg:width=\"1cm\" svg:height=\"1cm\"><draw:image xlink:href=\"Pictures/a.png\"/></draw:frame>";

        var document = new OdtBuilder().File("Pictures/a.png", png).Body("<text:p>" + string.Concat(Enumerable.Repeat(frame, 5000)) + "</text:p>").Load();

        var images = ((Paragraph)document.Blocks[0]).Inlines.Cast<InlineImage>().ToList();
        Assert.Equal(5000, images.Count);
        Assert.Single(images.Select(i => i.MediaPart).Distinct());
        Assert.Equal(png, document.GetMedia(images[0].MediaPart));
    }

    [Fact]
    public void Odt_Load_PackageCaps()
    {
        var odt = Rich().Build();
        var content = Encoding.UTF8.GetByteCount(Content(odt));

        Assert.NotNull(Load(odt, Defaults));
        Assert.Throws<InvalidDataException>(() => Load(odt, Defaults with { MaxEntries = 2 }));
        Assert.Throws<InvalidDataException>(() => Load(odt, Defaults with { MaxTotalLength = content }));
        Assert.Throws<InvalidDataException>(() => Load(odt, Defaults with { MaxEntryLength = content - 1 }));
        Assert.Throws<InvalidDataException>(() => Load(odt, Defaults with { MaxXmlLength = content - 1 }));
        Assert.Throws<InvalidDataException>(() => Load(odt, Defaults with { Budget = 100 }));
        Assert.Throws<InvalidDataException>(() => Load(Declare(odt, "content.xml", content - 10)));
        Assert.Throws<InvalidDataException>(() => Load(Declare(odt, "content.xml", content + 10)));
        Assert.Throws<InvalidDataException>(() => Load(Zip64(("content.xml", 1L << 62), ("styles.xml", 1L << 62), ("meta.xml", 1L << 62))));
        Assert.Throws<InvalidDataException>(() => Load(Zip64(("content.xml", -5))));
        var bigMimetype = new OdtBuilder { MimeType = new string('a', 1024 * 1024) }.Body("<text:p>m</text:p>").Build();
        Assert.Equal("m", ((Paragraph)Load(bigMimetype).Blocks[0]).Text);
        using var package = OpenDocumentFile.OpenPackage(bigMimetype);
        Assert.Null(package.MediaType);
    }

    private static OdtBuilder Rich() => new OdtBuilder().Meta("<dc:title>T</dc:title>").Body("<text:p>hello <text:span>world</text:span></text:p><text:p>second</text:p>");

    private static string Content(byte[] odt)
    {
        using var zip = new ZipArchive(new MemoryStream(odt));
        using var reader = new StreamReader(zip.GetEntry("content.xml")!.Open());
        return reader.ReadToEnd();
    }

    private static string Nest(string open, string close, int depth, string inner) =>
        string.Concat(Enumerable.Repeat(open, depth)) + inner + string.Concat(Enumerable.Repeat(close, depth));

    private static TextDocument Load(byte[] bytes, PackageLimits limits = null) =>
        limits == null ? TextDocument.Load(new MemoryStream(bytes), DocumentFileFormat.Odt) : OdtReader.Read(bytes, limits);

    private static byte[] Declare(byte[] zip, string name, long size)
    {
        var bytes = zip.ToArray();
        var encoded = Encoding.ASCII.GetBytes(name);
        for (var i = 0; i + 46 < bytes.Length; i++)
        {
            var local = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i)) == 0x04034b50 && bytes.AsSpan(i + 30).StartsWith(encoded) &&
                        BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(i + 26)) == encoded.Length;
            var central = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i)) == 0x02014b50 && bytes.AsSpan(i + 46).StartsWith(encoded) &&
                          BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(i + 28)) == encoded.Length;
            if (local) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i + 22), (uint)size);
            if (central) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i + 24), (uint)size);
        }

        return bytes;
    }

    private static byte[] Zip64(params (string Name, long Declared)[] entries)
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        var offsets = new List<long>();
        foreach (var (name, _) in entries)
        {
            offsets.Add(stream.Position);
            writer.Write(0x04034b50u);
            writer.Write((ushort)45);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write((ushort)name.Length);
            writer.Write((ushort)0);
            writer.Write(Encoding.ASCII.GetBytes(name));
        }

        var directory = stream.Position;
        for (var i = 0; i < entries.Length; i++)
        {
            writer.Write(0x02014b50u);
            writer.Write((ushort)45);
            writer.Write((ushort)45);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0xFFFFFFFFu);
            writer.Write((ushort)entries[i].Name.Length);
            writer.Write((ushort)12);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(0u);
            writer.Write((uint)offsets[i]);
            writer.Write(Encoding.ASCII.GetBytes(entries[i].Name));
            writer.Write((ushort)1);
            writer.Write((ushort)8);
            writer.Write(entries[i].Declared);
        }

        var size = stream.Position - directory;
        writer.Write(0x06054b50u);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)entries.Length);
        writer.Write((ushort)entries.Length);
        writer.Write((uint)size);
        writer.Write((uint)directory);
        writer.Write((ushort)0);
        writer.Flush();
        return stream.ToArray();
    }
}
