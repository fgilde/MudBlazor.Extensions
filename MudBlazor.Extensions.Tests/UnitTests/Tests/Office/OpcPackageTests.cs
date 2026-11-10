using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Office;

public class OpcPackageTests
{
    private static byte[] Build()
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            Add(zip, "[Content_Types].xml",
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"XML\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/ppt/slides/slide1.xml\" ContentType=\"application/slide+xml\"/></Types>");
            Add(zip, "ppt/slides/slide1.xml", "<sld/>");
            Add(zip, "ppt/slides/_rels/slide1.xml.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"t/image\" Target=\"../media/image1.png\"/>" +
                "<Relationship Id=\"rId2\" Type=\"t/hyperlink\" Target=\"https://x.test\" TargetMode=\"External\"/></Relationships>");
            Add(zip, "ppt/slides/slide2.xml", "<sld2/>");
        }

        return ms.ToArray();
    }

    private static void Add(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name);
        entry.LastWriteTime = new DateTimeOffset(2020, 5, 5, 10, 0, 0, TimeSpan.Zero);
        using var s = entry.Open();
        s.Write(Encoding.UTF8.GetBytes(content));
    }

    [Fact]
    public void OpcPackage_ResolvesRelativeTargets()
    {
        var package = OpcPackage.Open(Build());

        Assert.Equal("/ppt/media/image1.png", package.ResolveTarget("/ppt/slides/slide1.xml", "../media/image1.png"));
        Assert.Equal("/ppt/theme/theme1.xml", package.ResolveTarget("/ppt/slides/slide1.xml", "/ppt/theme/theme1.xml"));
        var rels = package.Relationships("/ppt/slides/slide1.xml");
        Assert.Equal(2, rels.Count);
        Assert.Equal("External", rels[1].TargetMode);
        Assert.Equal("application/slide+xml", package.ContentType("/ppt/slides/slide1.xml"));
        Assert.Equal("application/xml", package.ContentType("/ppt/slides/slide2.xml"));
    }

    [Fact]
    public void OpcPackageWriter_CopiedPartsKeepContent_PutPartsHave1980Timestamp()
    {
        var package = OpcPackage.Open(Build());
        using var target = new MemoryStream();
        using (var writer = new OpcPackageWriter(package, target))
        {
            writer.Copy("/ppt/slides/slide1.xml");
            writer.Put("/ppt/slides/slide2.xml", new XDocument(new XElement("changed")));
            writer.Put("/ppt/media/a.bin", [1, 2, 3]);
        }

        using var zip = new ZipArchive(new MemoryStream(target.ToArray()));
        var copied = zip.GetEntry("ppt/slides/slide1.xml")!;
        Assert.Equal(2020, copied.LastWriteTime.Year);
        using (var reader = new StreamReader(copied.Open())) Assert.Equal("<sld/>", reader.ReadToEnd());
        Assert.Equal(1980, zip.GetEntry("ppt/slides/slide2.xml")!.LastWriteTime.Year);
        Assert.Equal(1980, zip.GetEntry("ppt/media/a.bin")!.LastWriteTime.Year);
        Assert.Contains("changed", OpcPackage.Open(target.ToArray()).ReadXml("/ppt/slides/slide2.xml").ToString());
    }

    [Fact]
    public void OpcPackageWriter_NextPartName_SkipsExisting()
    {
        var package = OpcPackage.Open(Build());
        using var writer = new OpcPackageWriter(package, new MemoryStream());

        Assert.Equal("/ppt/slides/slide3.xml", writer.NextPartName("/ppt/slides/slide{0}.xml"));
        writer.Put("/ppt/slides/slide3.xml", [0]);
        Assert.Equal("/ppt/slides/slide4.xml", writer.NextPartName("/ppt/slides/slide{0}.xml"));
    }
}
