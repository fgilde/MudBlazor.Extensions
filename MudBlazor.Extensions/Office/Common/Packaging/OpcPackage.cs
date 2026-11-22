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

/// <summary>Limits for reading a package: the total declared uncompressed size and the XML elements of all parts read while loading.</summary>
/// <param name="MaxTotalLength">The largest sum of the declared uncompressed entry sizes.</param>
/// <param name="MaxElements">The most XML elements in all parts read until <see cref="OpcPackage.EndLoad"/>.</param>
internal sealed record OpcLimits(long MaxTotalLength, long MaxElements);

/// <summary>A read-only view of an OPC (zip) package. Part names carry a leading slash and compare case-insensitively.</summary>
internal sealed class OpcPackage
{
    private const string ContentTypesPart = "/[Content_Types].xml";
    private const long MaxPartLength = 256L * 1024 * 1024;
    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        return c;
    }).ToArray();

    private static readonly XmlReaderSettings XmlSettings = new() { DtdProcessing = DtdProcessing.Prohibit, CloseInput = false };

    private readonly ZipArchive _zip;
    private readonly Dictionary<string, ZipArchiveEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _defaults = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _overrides = new(StringComparer.OrdinalIgnoreCase);

    private readonly bool _exact;
    private long _elementBudget;

    private OpcPackage(byte[] source, OpcLimits limits)
    {
        Source = source;
        (_exact, _elementBudget) = (limits != null, limits?.MaxElements ?? long.MaxValue);
        _zip = new ZipArchive(new MemoryStream(source, false), ZipArchiveMode.Read);
        long total = 0;
        foreach (var entry in _zip.Entries)
        {
            if (limits != null && (entry.Length < 0 || entry.Length > limits.MaxTotalLength - total))
                throw LoadLimit.Exceeded($"The package is larger than {limits.MaxTotalLength} bytes uncompressed.");
            total += entry.Length;
            if (!entry.FullName.EndsWith('/')) _entries["/" + entry.FullName] = entry;
        }

        if (!_entries.ContainsKey(ContentTypesPart)) return;
        foreach (var e in ReadXml(ContentTypesPart).Root!.Elements())
            switch (e.Name.LocalName)
            {
                case "Default" when (string)e.Attribute("Extension") is { } extension:
                    _defaults[extension] = (string)e.Attribute("ContentType");
                    break;
                case "Override" when (string)e.Attribute("PartName") is { } partName:
                    _overrides[partName] = (string)e.Attribute("ContentType");
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
    public static OpcPackage Open(byte[] source) => new(source, null);

    /// <summary>Opens a package with limits: every entry read must have its declared size and CRC-32, and the XML read until <see cref="EndLoad"/> shares an element budget.</summary>
    /// <param name="source">The zip bytes.</param>
    /// <param name="limits">The limits.</param>
    /// <returns>The package.</returns>
    public static OpcPackage Open(byte[] source, OpcLimits limits) => new(source, limits);

    /// <summary>Ends the element budget; later reads (for example while saving) are not counted.</summary>
    public void EndLoad() => _elementBudget = long.MaxValue;

    internal ZipArchiveEntry Entry(string part) => _entries.GetValueOrDefault(part);

    /// <summary>Reads a part as XML; DTDs and elements nested deeper than 256 levels are rejected with <see cref="InvalidDataException"/>.</summary>
    /// <param name="part">The part name.</param>
    /// <returns>The document.</returns>
    public XDocument ReadXml(string part)
    {
        using var stream = Open(part);
        var document = BoundedXmlReader.Load(XmlReader.Create(stream, XmlSettings), BoundedXmlReader.MaxDepth, _elementBudget, out var elements);
        if (_elementBudget != long.MaxValue) _elementBudget -= elements;
        return document;
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

    private Stream Open(string part)
    {
        var entry = _entries.GetValueOrDefault(part) ?? throw new FileNotFoundException("Package part not found.", part);
        if (entry.Length > MaxPartLength) throw LoadLimit.Exceeded($"The package part {part} is larger than 256 MB.");
        return _exact ? new MemoryStream(ReadDeclared(entry, "package part " + part), false) : entry.Open();
    }

    internal static byte[] ReadDeclared(ZipArchiveEntry entry, string name)
    {
        const int Chunk = 81920;
        using var source = entry.Open();
        using var data = new MemoryStream((int)Math.Min(entry.Length, 4L * 1024 * 1024));
        var buffer = new byte[Chunk];
        int read;
        while (data.Length <= entry.Length && (read = source.Read(buffer, 0, (int)Math.Min(Chunk, entry.Length - data.Length + 1))) > 0)
            data.Write(buffer, 0, read);
        var bytes = data.Capacity == data.Length ? data.GetBuffer() : data.ToArray();
        if (bytes.Length != entry.Length || Crc32(bytes) != entry.Crc32) throw LoadLimit.Exceeded($"The {name} does not have its declared size or checksum.");
        return bytes;
    }

    private static uint Crc32(byte[] data)
    {
        var crc = uint.MaxValue;
        foreach (var b in data) crc = CrcTable[(byte)(crc ^ b)] ^ (crc >> 8);
        return ~crc;
    }
}
