using System.IO.Compression;
using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public partial class ObjectCommandTests
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Wp = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace Pic = "http://schemas.openxmlformats.org/drawingml/2006/picture";
    private const string MainPart = "/word/document.xml";
    private const string ImageType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";

    [Fact]
    public void InsertImage_ScaledToTextWidth()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        var before = document.Blocks[0];

        document.Execute(new InsertImageCommand(Caret(document, id, 0), Png(2000, 1000), "image/png"));

        var image = Assert.IsType<InlineImage>(Assert.Single(((Paragraph)document.Blocks[0]).Inlines));
        Assert.Equal(5731510, image.WidthEmu);
        Assert.Equal(2865755, image.HeightEmu);
        Assert.Equal(ImagePlacement.Inline, image.Placement);
        Assert.Equal(Png(2000, 1000), document.GetMedia(image.MediaPart));
        Assert.Equal("image/png", document.MediaContentType(image.MediaPart));

        document.Execute(new InsertImageCommand(Caret(document, id, 1), Png(20, 10), "image/png"));
        document.Execute(new InsertImageCommand(Caret(document, id, 2), [1, 2, 3], "image/gif"));

        var images = ((Paragraph)document.Blocks[0]).Inlines.Cast<InlineImage>().ToList();
        Assert.Equal((190500L, 95250L), (images[1].WidthEmu, images[1].HeightEmu));
        Assert.Equal((914400L, 914400L), (images[2].WidthEmu, images[2].HeightEmu));

        document.Undo.Undo();
        document.Undo.Undo();
        document.Undo.Undo();
        Assert.Same(before, document.Blocks[0]);
    }

    [Fact]
    public void InsertImage_Svg_Throws()
    {
        var document = TextDocument.Create();
        var at = Caret(document, document.Blocks[0].Id, 0);

        Assert.Throws<ArgumentException>(() => new InsertImageCommand(at, "<svg/>"u8.ToArray(), "image/svg+xml"));
        Assert.Throws<ArgumentException>(() => new InsertImageCommand(at, [], "image/png"));
        Assert.Throws<ArgumentNullException>(() => new InsertImageCommand(at, null!, "image/png"));
    }

    [Fact]
    public void Writer_InsertImage_MediaPartDefaultTypeRelUniqueDocPrId()
    {
        var existing = Png(10, 10, 1);
        var document = WithImage(existing).Load();
        var id = document.Blocks[1].Id;

        document.Execute(new InsertImageCommand(Caret(document, id, 0), Png(400, 200), "image/png"));
        document.Execute(new SetImageAltTextCommand(id, 0, "New"));
        var output = DocxWriterTests.Save(document);

        DocxWriterTests.AssertConsistent(output);
        var opc = OpcPackage.Open(output);
        Assert.Equal("image/png", opc.DefaultContentTypes["png"]);
        Assert.Equal(Png(400, 200), opc.ReadBytes("/word/media/image2.png"));
        var drawing = Body(output).Elements(W + "p").ElementAt(1).Descendants(W + "drawing").Single();
        var embed = (string)drawing.Descendants(A + "blip").Single().Attribute(R + "embed");
        var rel = Assert.Single(opc.Relationships(MainPart), r => r.Id == embed);
        Assert.Equal((ImageType, "media/image2.png"), (rel.Type, rel.Target));
        Assert.Equal("6", (string)drawing.Descendants(Wp + "docPr").Single().Attribute("id"));
        Assert.Equal("6", (string)drawing.Descendants(Pic + "cNvPr").Single().Attribute("id"));
        Assert.Equal("New", (string)drawing.Descendants(Wp + "docPr").Single().Attribute("descr"));
        Assert.Equal(["3810000", "1905000"], drawing.Descendants(Wp + "extent").Single().Attributes().Select(a => a.Value));
        var reloaded = TextDocument.Load(new MemoryStream(output));
        var image = (InlineImage)((Paragraph)reloaded.Blocks[1]).Inlines[0];
        Assert.Equal(("/word/media/image2.png", 3810000L, 1905000L, "New"), (image.MediaPart, image.WidthEmu, image.HeightEmu, image.AltText));
    }

    [Fact]
    public void SamePictureTwice_OneMediaPart()
    {
        var existing = Png(10, 10, 1);
        var document = WithImage(existing).Load();
        var id = document.Blocks[1].Id;

        document.Execute(new InsertImageCommand(Caret(document, id, 0), Png(30, 30), "image/png"));
        document.Execute(new InsertImageCommand(Caret(document, id, 1), Png(30, 30), "image/png"));
        document.Execute(new InsertImageCommand(Caret(document, id, 2), existing, "image/png"));
        var output = DocxWriterTests.Save(document);

        DocxWriterTests.AssertConsistent(output);
        var opc = OpcPackage.Open(output);
        Assert.Equal(["/word/media/image1.png", "/word/media/image2.png"], opc.PartNames.Where(p => p.StartsWith("/word/media/", StringComparison.Ordinal)).Order());
        var targets = Body(output).Elements(W + "p").ElementAt(1).Descendants(A + "blip")
            .Select(b => opc.Relationships(MainPart).Single(r => r.Id == (string)b.Attribute(R + "embed")).Target).ToList();
        Assert.Equal(["media/image2.png", "media/image2.png", "media/image1.png"], targets);
        var docPrIds = Body(output).Descendants(Wp + "docPr").Select(d => (string)d.Attribute("id")).ToList();
        Assert.Equal(docPrIds.Count, docPrIds.Distinct().Count());
    }

    [Fact]
    public void Writer_InsertedThenDeletedImage_NoMediaPartNoRelationship()
    {
        var source = DocxWriterTests.Save(TextDocument.Create());
        var document = TextDocument.Load(new MemoryStream(source));
        var id = document.Blocks[0].Id;

        document.Execute(new InsertImageCommand(Caret(document, id, 0), Png(30, 30), "image/png"));
        document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 1)), ""));
        var output = DocxWriterTests.Save(document);

        var opc = OpcPackage.Open(output);
        Assert.DoesNotContain(opc.PartNames, p => p.Contains("/media/", StringComparison.Ordinal));
        Assert.DoesNotContain(opc.Relationships(MainPart), r => r.Type == ImageType);
        DocxWriterTests.AssertConsistent(output);
    }

    [Fact]
    public void Writer_ImageInHeader_RelationshipInHeaderRels()
    {
        var document = new DocxBuilder().Header("<w:p><w:r><w:t>Head</w:t></w:r></w:p>").Body("<w:p/>").Load();
        var header = document.HeadersFooters[0].Blocks[0].Id;

        document.Execute(new InsertImageCommand(Caret(document, header, 4), Png(30, 30), "image/png"));
        var output = DocxWriterTests.Save(document);

        DocxWriterTests.AssertConsistent(output);
        var opc = OpcPackage.Open(output);
        var embed = (string)OpcAssert.Xml(output, "/word/header1.xml").Descendants(A + "blip").Single().Attribute(R + "embed");
        Assert.Equal("media/image1.png", opc.Relationships("/word/header1.xml").Single(r => r.Id == embed).Target);
        Assert.DoesNotContain(opc.Relationships(MainPart), r => r.Type == ImageType);
    }

    [Fact]
    public void Writer_ImageIntoGapOfSharedRun_WrittenInModelOrder()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>a</w:t><w:tab/><w:t>b</w:t></w:r></w:p>").Load();
        var id = document.Blocks[0].Id;

        document.Execute(new InsertImageCommand(Caret(document, id, 1), Png(30, 30), "image/png"));
        document.Execute(new InsertImageCommand(Caret(document, id, 3), Png(30, 30), "image/png"));
        var output = DocxWriterTests.Save(document);

        var names = Body(output).Element(W + "p")!.Elements(W + "r").SelectMany(r => r.Elements().Where(e => e.Name != W + "rPr")).Select(e => e.Name.LocalName);
        Assert.Equal(["t", "drawing", "tab", "drawing", "t"], names);
        DocxWriterTests.AssertConsistent(output);
    }

    [Fact]
    public void ResizeImage_PatchesExtents_UndoRestoresOriginal()
    {
        var source = WithImage(Png(10, 10, 1)).Build();
        var document = TextDocument.Load(new MemoryStream(source));
        var id = document.Blocks[0].Id;
        var original = document.Blocks[0];

        document.Execute(new ResizeImageCommand(id, 0, 952500, 476250));
        document.Execute(new SetImageAltTextCommand(id, 0, null));
        var output = DocxWriterTests.Save(document);

        var drawing = Body(output).Element(W + "p")!.Descendants(W + "drawing").Single();
        Assert.Equal(["952500", "476250"], drawing.Descendants(Wp + "extent").Single().Attributes().Select(a => a.Value));
        Assert.Equal(["952500", "476250"], drawing.Descendants(A + "ext").Single().Attributes().Select(a => a.Value));
        Assert.Null(drawing.Descendants(Wp + "docPr").Single().Attribute("descr"));
        Assert.Equal("5", (string)drawing.Descendants(Wp + "docPr").Single().Attribute("id"));
        DocxWriterTests.AssertConsistent(output);
        Assert.Throws<ArgumentException>(() => document.Execute(new ResizeImageCommand(document.Blocks[1].Id, 0, 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResizeImageCommand(id, 0, 0, 1));

        document.Undo.Undo();
        document.Undo.Undo();

        Assert.Same(original, document.Blocks[0]);
        Assert.True(XNode.DeepEquals(OpcAssert.Xml(source, MainPart), OpcAssert.Xml(DocxWriterTests.Save(document), MainPart)));
    }

    private static DocxBuilder WithImage(byte[] png) => new DocxBuilder().Media("rId7", "image1.png", png).Body(
        "<w:p><w:r><w:drawing><wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\"><wp:extent cx=\"95250\" cy=\"95250\"/><wp:docPr id=\"5\" name=\"Picture 1\" descr=\"Old\"/>" +
        "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:pic><pic:nvPicPr><pic:cNvPr id=\"0\" name=\"image1.png\"/><pic:cNvPicPr/></pic:nvPicPr>" +
        "<pic:blipFill><a:blip r:embed=\"rId7\"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"95250\" cy=\"95250\"/></a:xfrm>" +
        "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p><w:p><w:r><w:t>text</w:t></w:r></w:p>");

    private static DocumentRange Caret(TextDocument document, string paragraphId, int offset)
    {
        var position = document.PositionOf(paragraphId, offset);
        return new DocumentRange(position, position);
    }

    private static XElement Body(byte[] package) => OpcAssert.Xml(package, MainPart).Root!.Element(W + "body")!;

    internal static byte[] Png(int width, int height, byte shade = 0)
    {
        using var stream = new MemoryStream();
        stream.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var header = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header, width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        Chunk(stream, "IHDR", header);
        using var pixels = new MemoryStream();
        using (var zlib = new ZLibStream(pixels, CompressionLevel.Optimal, true))
            for (var y = 0; y < height; y++)
            {
                zlib.WriteByte(0);
                for (var x = 0; x < width; x++) zlib.WriteByte(shade);
            }

        Chunk(stream, "IDAT", pixels.ToArray());
        Chunk(stream, "IEND", []);
        return stream.ToArray();
    }

    private static void Chunk(Stream stream, string type, byte[] data)
    {
        var length = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        byte[] body = [.. type.Select(c => (byte)c), .. data];
        stream.Write(body);
        var crc = 0xFFFFFFFFu;
        foreach (var b in body)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }

        var check = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(check, ~crc);
        stream.Write(check);
    }
}
