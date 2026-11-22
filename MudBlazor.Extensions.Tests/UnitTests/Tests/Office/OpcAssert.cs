using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Office;

internal static class OpcAssert
{
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static Dictionary<string, byte[]> Parts(byte[] package)
    {
        using var zip = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read);
        return zip.Entries.ToDictionary(e => "/" + e.FullName, e =>
        {
            using var stream = e.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        });
    }

    public static XDocument Xml(byte[] package, string part) => XDocument.Parse(Encoding.UTF8.GetString(Parts(package)[part]).TrimStart('\uFEFF'));

    public static void AssertNoDanglingRelationships(byte[] package)
    {
        var opc = OpcPackage.Open(package);
        var parts = opc.PartNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var rels in parts.Where(p => p.EndsWith(".rels", StringComparison.Ordinal)))
        {
            var source = rels.Replace("/_rels/", "/", StringComparison.Ordinal)[..^5];
            Assert.True(source == "/" || parts.Contains(source), $"Rels without source {rels}");
            foreach (var rel in opc.Relationships(source).Where(r => r.TargetMode == null))
                Assert.True(parts.Contains(opc.ResolveTarget(source, rel.Target)), $"{rels}: {rel.Id} -> {rel.Target}");
        }

        foreach (var part in parts.Where(p => p.EndsWith(".xml", StringComparison.Ordinal) && p != "/[Content_Types].xml"))
        {
            var ids = opc.Relationships(part).Select(r => r.Id).ToHashSet();
            foreach (var reference in opc.ReadXml(part).Descendants().Attributes().Where(a => a.Name.Namespace == R && a.Value.Length > 0))
                Assert.True(ids.Contains(reference.Value), $"{part}: {reference.Name.LocalName}={reference.Value} has no relationship");
        }
    }
}
