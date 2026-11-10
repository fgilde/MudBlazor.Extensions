using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office;

/// <summary>A package relationship.</summary>
/// <param name="Id">The relationship id.</param>
/// <param name="Type">The relationship type URI.</param>
/// <param name="Target">The target as written in the rels part.</param>
/// <param name="TargetMode">The <c>TargetMode</c> attribute (<c>External</c>), or null for internal targets.</param>
internal sealed record OpcRelationship(string Id, string Type, string Target, string TargetMode);

/// <summary>A read-only view of an OPC (zip) package. Part names carry a leading slash and compare case-insensitively.</summary>
internal sealed class OpcPackage
{
    private const string ContentTypesPart = "/[Content_Types].xml";
    private static readonly XmlReaderSettings XmlSettings = new() { DtdProcessing = DtdProcessing.Prohibit, CloseInput = false };

    private readonly ZipArchive _zip;
    private readonly Dictionary<string, ZipArchiveEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _defaults = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _overrides = new(StringComparer.OrdinalIgnoreCase);

    private OpcPackage(byte[] source)
    {
        Source = source;
        _zip = new ZipArchive(new MemoryStream(source, false), ZipArchiveMode.Read);
        foreach (var entry in _zip.Entries)
            if (!entry.FullName.EndsWith('/')) _entries["/" + entry.FullName] = entry;

        if (!_entries.ContainsKey(ContentTypesPart)) return;
        foreach (var e in ReadXml(ContentTypesPart).Root!.Elements())
            switch (e.Name.LocalName)
            {
                case "Default":
                    _defaults[(string)e.Attribute("Extension")] = (string)e.Attribute("ContentType");
                    break;
                case "Override":
                    _overrides[(string)e.Attribute("PartName")] = (string)e.Attribute("ContentType");
                    break;
            }
    }

    /// <summary>The package bytes as opened.</summary>
    public byte[] Source { get; }

    /// <summary>Names of all parts, with leading slash.</summary>
    public IReadOnlyCollection<string> PartNames => _entries.Keys;

    /// <summary>Extension defaults of <c>[Content_Types].xml</c>.</summary>
    public IReadOnlyDictionary<string, string> DefaultContentTypes => _defaults;

    /// <summary>Part overrides of <c>[Content_Types].xml</c>.</summary>
    public IReadOnlyDictionary<string, string> OverrideContentTypes => _overrides;

    /// <summary>Opens a package.</summary>
    /// <param name="source">The zip bytes.</param>
    /// <returns>The package.</returns>
    public static OpcPackage Open(byte[] source) => new(source);

    internal ZipArchiveEntry Entry(string part) => _entries.GetValueOrDefault(part);

    /// <summary>Reads a part as XML; DTDs are rejected.</summary>
    /// <param name="part">The part name.</param>
    /// <returns>The document.</returns>
    public XDocument ReadXml(string part)
    {
        using var stream = Open(part);
        using var reader = XmlReader.Create(stream, XmlSettings);
        return XDocument.Load(reader);
    }

    /// <summary>Reads a part's raw bytes.</summary>
    /// <param name="part">The part name.</param>
    /// <returns>The bytes.</returns>
    public byte[] ReadBytes(string part)
    {
        using var stream = Open(part);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>Reads the relationships of a part; empty if it has no rels part.</summary>
    /// <param name="part">The source part name, or <c>/</c> for the package relationships.</param>
    /// <returns>The relationships in file order.</returns>
    public IReadOnlyList<OpcRelationship> Relationships(string part)
    {
        var rels = RelsPartOf(part);
        if (!_entries.ContainsKey(rels)) return [];
        return ReadXml(rels).Root!.Elements().Where(e => e.Name.LocalName == "Relationship")
            .Select(e => new OpcRelationship((string)e.Attribute("Id"), (string)e.Attribute("Type"), (string)e.Attribute("Target"), (string)e.Attribute("TargetMode")))
            .ToList();
    }

    /// <summary>Resolves a relationship target against its source part.</summary>
    /// <param name="sourcePart">The source part name.</param>
    /// <param name="target">The target as written in the rels part.</param>
    /// <returns>The part name with leading slash.</returns>
    public string ResolveTarget(string sourcePart, string target)
    {
        var path = target.StartsWith('/') ? target : sourcePart[..(sourcePart.LastIndexOf('/') + 1)] + target;
        var parts = new List<string>();
        foreach (var p in Uri.UnescapeDataString(path).Split('/'))
        {
            if (p is "" or ".") continue;
            if (p == "..")
            {
                if (parts.Count > 0) parts.RemoveAt(parts.Count - 1);
            }
            else parts.Add(p);
        }

        return "/" + string.Join('/', parts);
    }

    /// <summary>The content type of a part from its override or extension default.</summary>
    /// <param name="part">The part name.</param>
    /// <returns>The content type, or null.</returns>
    public string ContentType(string part) =>
        _overrides.TryGetValue(part, out var type) ? type : _defaults.GetValueOrDefault(Path.GetExtension(part).TrimStart('.'));

    internal static string RelsPartOf(string part)
    {
        var slash = part.LastIndexOf('/');
        return part[..(slash + 1)] + "_rels/" + part[(slash + 1)..] + ".rels";
    }

    private Stream Open(string part) =>
        (_entries.GetValueOrDefault(part) ?? throw new FileNotFoundException("Package part not found.", part)).Open();
}
