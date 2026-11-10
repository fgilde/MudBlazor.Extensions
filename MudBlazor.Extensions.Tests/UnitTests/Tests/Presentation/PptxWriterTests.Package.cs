using System.IO.Compression;
using System.Text;
using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public partial class PptxWriterTests
{
    [Fact]
    public void Writer_OrphanWithLinkToDeletedSlide_IsRemovedWithItsContentType()
    {
        var builder = new PptxBuilder()
            .Slide(Shape(2, "One", Xfrm(0, 0, 10, 10)))
            .Slide(Shape(2, "Two", Xfrm(0, 0, 10, 10)))
            .Slide(Shape(2, "Three", Xfrm(0, 0, 10, 10)), rels: Rel("rId5", "tags", "../tags/tag1.xml"));
        builder.ExtraParts["ppt/tags/tag1.xml"] = ("officedocument.presentationml.tags", "<p:tagLst " + Ns + "><p:tag name=\"X\" val=\"Y\"/></p:tagLst>");
        builder.ExtraParts["ppt/tags/_rels/tag1.xml.rels"] = (null, RelsXml(Rel("rId1", "slide", "../slides/slide2.xml")));
        var presentation = builder.Load();
        presentation.Execute(new DeleteSlidesCommand([presentation.Slides[1], presentation.Slides[2]]));

        var output = Save(presentation);

        AssertConsistent(output);
        Assert.DoesNotContain("/ppt/tags/tag1.xml", Parts(output).Keys);
    }

    [Fact]
    public void Writer_NotesWithoutBodyPlaceholder_GetsOne()
    {
        var builder = new PptxBuilder { NotesMaster = true }.Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10)), rels: Rel("rId2", "notesSlide", "../notesSlides/notesSlide1.xml"));
        builder.ExtraParts["ppt/notesSlides/notesSlide1.xml"] = ("officedocument.presentationml.notesSlide",
            $"<p:notes {Ns}><p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>" +
            Placeholder(2, "Slide Image Placeholder 1", "type=\"sldImg\"", "", "") + "</p:spTree></p:cSld></p:notes>");
        builder.ExtraParts["ppt/notesSlides/_rels/notesSlide1.xml.rels"] = (null, RelsXml(Rel("rId1", "slide", "../slides/slide1.xml")));
        var presentation = builder.Load();
        presentation.Execute(new SetNotesCommand(presentation.Slides[0], "Speaker\nnotes"));

        var output = Save(presentation);

        AssertConsistent(output);
        Assert.Equal("Speaker\nnotes", Load(output).Slides[0].Notes);
    }

    [Fact]
    public void Writer_UnknownSchemeColor_KeepsFill()
    {
        var presentation = new PptxBuilder().Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10) + "<a:solidFill><a:srgbClr val=\"FF0000\"/></a:solidFill>")).Load();
        presentation.Execute(new SetShapeStyleCommand(presentation.Slides[0], [2], new ShapeFill(ShapeFillKind.Solid, "scheme:accent7"), null));

        var xml = Xml(Save(presentation), "/ppt/slides/slide1.xml");

        Assert.Empty(xml.Descendants(A + "schemeClr"));
        Assert.Equal("FF0000", (string)xml.Descendants(A + "srgbClr").Single().Attribute("val"));
    }

    [Fact]
    public void Load_StrictPresentation_Throws()
    {
        var builder = new PptxBuilder().Slide("");
        var bytes = builder.Build();
        var parts = Parts(bytes);
        parts["/ppt/presentation.xml"] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(parts["/ppt/presentation.xml"])
            .Replace("http://schemas.openxmlformats.org/presentationml/2006/main", "http://purl.oclc.org/ooxml/presentationml/main", StringComparison.Ordinal));

        Assert.Throws<InvalidDataException>(() => Load(Zip(parts)));
    }

    [Fact]
    public void PackageWriter_SamePartTwice_Throws_NextPartNameSkipsUsedNames()
    {
        var opc = OpcPackage.Open(new PptxBuilder().Slide("").Slide("").Build());
        using var writer = new OpcPackageWriter(opc, new MemoryStream());

        Assert.Equal("/ppt/slides/slide3.xml", writer.NextPartName("/ppt/slides/slide{0}.xml"));
        Assert.Equal("/ppt/slides/slide4.xml", writer.NextPartName("/ppt/slides/slide{0}.xml"));
        writer.Put("/ppt/x.xml", [1]);
        Assert.Throws<InvalidOperationException>(() => writer.Put("/ppt/X.xml", [2]));
        writer.Copy("/ppt/slides/slide1.xml");
        Assert.Throws<InvalidOperationException>(() => writer.Copy("/ppt/slides/slide1.xml"));
    }

    private static byte[] Zip(Dictionary<string, byte[]> parts)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            foreach (var (name, bytes) in parts)
            {
                using var entry = zip.CreateEntry(name.TrimStart('/')).Open();
                entry.Write(bytes);
            }

        return ms.ToArray();
    }
}
