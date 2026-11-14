using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office;

/// <summary>Writes an OPC package or another zip package: parts are copied unchanged from a source package or put as new content.</summary>
internal sealed class OpcPackageWriter : IDisposable
{
    private const string RelationshipsNs = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string ContentTypesNs = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly DateTimeOffset EntryTime = new(new DateTime(1980, 1, 1));

    private readonly Func<string, ZipArchiveEntry> _source;
    private readonly ZipArchive _zip;
    private readonly HashSet<string> _written = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _next = [];

    public OpcPackageWriter(OpcPackage source, Stream target)
        : this(source.Entry, target)
    {
    }

    public OpcPackageWriter(Func<string, ZipArchiveEntry> source, Stream target)
    {
        _source = source;
        _zip = new ZipArchive(target, ZipArchiveMode.Create, true);
    }

    /// <summary>Copies a part of the source package unchanged, keeping its timestamp.</summary>
    /// <param name="part">The part name.</param>
    public void Copy(string part)
    {
        var from = _source(part) ?? throw new FileNotFoundException("Package part not found.", part);
        Claim(part);
        var to = _zip.CreateEntry(from.FullName, CompressionLevel.Optimal);
        to.LastWriteTime = from.LastWriteTime;
        using var input = from.Open();
        using var output = to.Open();
        input.CopyTo(output);
    }

    /// <summary>Writes a part with the given bytes.</summary>
    /// <param name="part">The part name.</param>
    /// <param name="content">The content.</param>
    public void Put(string part, byte[] content) => Put(part, stream => stream.Write(content));

    /// <summary>Writes a part with the given XML.</summary>
    /// <param name="part">The part name.</param>
    /// <param name="xml">The document.</param>
    public void Put(string part, XDocument xml) => Put(part, stream =>
    {
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false });
        xml.Save(writer);
    });

    /// <summary>Writes the rels part of a part.</summary>
    /// <param name="sourcePart">The source part name, or <c>/</c> for the package relationships.</param>
    /// <param name="relationships">The relationships.</param>
    public void PutRelationships(string sourcePart, IEnumerable<OpcRelationship> relationships)
    {
        XNamespace ns = RelationshipsNs;
        Put(OpcPackage.RelsPartOf(sourcePart), new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(ns + "Relationships", relationships.Select(r => new XElement(ns + "Relationship",
                new XAttribute("Id", r.Id), new XAttribute("Type", r.Type), new XAttribute("Target", r.Target),
                r.TargetMode == null ? null : new XAttribute("TargetMode", r.TargetMode))))));
    }

    /// <summary>Writes <c>[Content_Types].xml</c>.</summary>
    /// <param name="defaults">Content types by extension.</param>
    /// <param name="overrides">Content types by part name.</param>
    public void PutContentTypes(IReadOnlyDictionary<string, string> defaults, IReadOnlyDictionary<string, string> overrides)
    {
        XNamespace ns = ContentTypesNs;
        Put("/[Content_Types].xml", new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(ns + "Types",
                defaults.Select(d => new XElement(ns + "Default", new XAttribute("Extension", d.Key), new XAttribute("ContentType", d.Value))),
                overrides.Select(o => new XElement(ns + "Override", new XAttribute("PartName", o.Key), new XAttribute("ContentType", o.Value))))));
    }

    /// <summary>Returns the next part name of the pattern that neither the source package nor this writer uses; each call returns a new name.</summary>
    /// <param name="pattern">A pattern with one number placeholder, for example <c>/ppt/slides/slide{0}.xml</c>.</param>
    /// <returns>The free part name.</returns>
    public string NextPartName(string pattern)
    {
        for (var i = _next.GetValueOrDefault(pattern, 1);; i++)
        {
            var name = string.Format(pattern, i);
            if (_written.Contains(name) || _source(name) != null) continue;
            _next[pattern] = i + 1;
            return name;
        }
    }

    public void Dispose() => _zip.Dispose();

    private void Put(string part, Action<Stream> write)
    {
        Claim(part);
        var entry = _zip.CreateEntry(part.TrimStart('/'), CompressionLevel.Optimal);
        entry.LastWriteTime = EntryTime;
        using var stream = entry.Open();
        write(stream);
    }

    private void Claim(string part)
    {
        if (!_written.Add(part)) throw new InvalidOperationException($"The part {part} is written twice.");
    }
}
