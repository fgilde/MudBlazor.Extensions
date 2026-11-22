using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>
/// Writes a <see cref="TextDocument"/> as a .docx or .dotx file. Unchanged parts are copied from the loaded file and unchanged blocks keep their original
/// XML; changed blocks are patched in their original XML, so content the editor does not model stays as it was.
/// </summary>
public static partial class DocxWriter
{
    internal const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string Wml = "application/vnd.openxmlformats-officedocument.wordprocessingml.";

    /// <summary>Writes a document. Nothing is written to the stream if writing fails.</summary>
    /// <param name="document">The document.</param>
    /// <param name="target">The stream that receives the file.</param>
    /// <param name="asTemplate">
    /// True to write a template, false to write a document, null to keep the type of the loaded file. A macro-enabled file stays macro-enabled (.docm/.dotm).
    /// </param>
    /// <exception cref="ArgumentNullException">The document or the stream is null.</exception>
    /// <exception cref="ArgumentException">The stream is not writable.</exception>
    /// <exception cref="InvalidOperationException">The document was not loaded or created by <see cref="TextDocument"/>.</exception>
    public static void Write(TextDocument document, Stream target, bool? asTemplate = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(target);
        if (!target.CanWrite) throw new ArgumentException("The stream is not writable.", nameof(target));
        var package = document.Package ?? throw new InvalidOperationException("The document was not loaded from a package.");
        using var buffer = new MemoryStream();
        using (var writer = new OpcPackageWriter(package.Opc, buffer))
            new Job(document, package, writer, asTemplate).Run();
        buffer.Position = 0;
        buffer.CopyTo(target);
    }

    private sealed partial class Job(TextDocument document, DocxPackage package, OpcPackageWriter writer, bool? asTemplate)
    {
        private readonly OpcPackage _opc = package.Opc;
        private readonly DocxWriteContext _context = new(package);
        private readonly HashSet<string> _done = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _overrides = new(package.Opc.OverrideContentTypes, StringComparer.OrdinalIgnoreCase);
        private List<OpcRelationship> _mainRelationships;
        private bool _contentTypesChanged;
        private bool _mainRelationshipsChanged;

        internal void Run()
        {
            if (asTemplate is { } template && MainContentType(_opc.ContentType(package.MainPart), template) is { } type) SetOverride(package.MainPart, type);
            _mainRelationships = Relationships(package.MainPart);
            WriteStyles();
            WriteNumbering();
            WriteBody();
            WriteHeadersFooters();
            WriteProperties();
            if (_mainRelationshipsChanged) PutRelationships(package.MainPart, _mainRelationships);
            if (_contentTypesChanged)
            {
                writer.PutContentTypes(_opc.DefaultContentTypes, _overrides);
                _done.Add("/[Content_Types].xml");
            }

            foreach (var part in _opc.PartNames.Where(p => !_done.Contains(p))) writer.Copy(part);
        }

        private static string MainContentType(string current, bool template)
        {
            if (current != null && current.Contains(".template", StringComparison.Ordinal) == template) return null;
            if (current?.Contains("macroEnabled", StringComparison.Ordinal) != true) return Wml + (template ? "template" : "document") + ".main+xml";
            return template ? "application/vnd.ms-word.template.macroEnabledTemplate.main+xml" : "application/vnd.ms-word.document.macroEnabled.main+xml";
        }

        private void WriteBody()
        {
            var final = document.SectionList.LastOrDefault();
            var ends = new Dictionary<string, Section>();
            foreach (var section in document.SectionList.Where(s => s != final && s.LastBlockId != null)) ends.TryAdd(section.LastBlockId, section);
            _context.Begin(_mainRelationships, ends);
            var blocks = document.BlockList.Select(b => _context.Block(b, true)).Where(e => e != null).ToList();
            if (final != null && (final.Source != null || !package.Sections.Contains(final, ReferenceEqualityComparer.Instance)))
                blocks.Add(SectionXml.Write(final, package.HeaderIds));
            _mainRelationshipsChanged |= _context.RelationshipsChanged;
            var original = package.MainXml.Root!.Element(W + "body")!;
            if (blocks.SequenceEqual(original.Elements(), ReferenceEqualityComparer.Instance)) return;
            var xml = new XDocument(package.MainXml);
            xml.Root!.Element(W + "body")!.ReplaceNodes(blocks);
            _context.RemoveOrphans(xml.Root);
            if (!XNode.DeepEquals(xml, package.MainXml)) Put(package.MainPart, xml);
        }

        private void WriteHeadersFooters()
        {
            foreach (var part in document.HeaderFooterList)
            {
                var original = package.HeadersFooters.FirstOrDefault(h => string.Equals(h.PartName, part.PartName, StringComparison.OrdinalIgnoreCase));
                if (original == null || TextDocument.SameItems(original.Blocks, part.Blocks) || _done.Contains(part.PartName)) continue;
                var rels = Relationships(part.PartName);
                _context.Begin(rels, null);
                var xml = _opc.ReadXml(part.PartName);
                var blocks = part.Blocks.Select(b => _context.Block(b, false)).Where(e => e != null).ToList();
                if (blocks.Count == 0) blocks.Add(new XElement(W + "p"));
                xml.Root!.ReplaceNodes(blocks);
                _context.RemoveOrphans(xml.Root);
                if (!_context.RelationshipsChanged && XNode.DeepEquals(xml, _opc.ReadXml(part.PartName))) continue;
                Put(part.PartName, xml);
                if (_context.RelationshipsChanged) PutRelationships(part.PartName, rels);
            }
        }

        private List<OpcRelationship> Relationships(string part) =>
            _opc.Relationships(part).Where(r => r.Id != null && r.Type != null && r.Target != null).ToList();

        private void Put(string part, XDocument xml)
        {
            writer.Put(part, xml);
            _done.Add(part);
        }

        private void PutRelationships(string part, IEnumerable<OpcRelationship> rels)
        {
            writer.PutRelationships(part, rels);
            _done.Add(OpcPackage.RelsPartOf(part));
        }

        private void SetOverride(string part, string contentType)
        {
            _overrides[part] = contentType;
            _contentTypesChanged = true;
        }
    }
}

internal sealed class DocxWriteContext
{
    private const string HyperlinkType = DocxWriter.RelNs + "/hyperlink";
    private static readonly XNamespace W14 = "http://schemas.microsoft.com/office/word/2010/wordml";
    private readonly DocxPackage _package;
    private readonly HashSet<object> _originals = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<XElement, List<Inline>> _owned = new();
    private readonly Dictionary<object, Inline> _tokens = new();
    private readonly HashSet<XElement> _claimed = new();
    private readonly Dictionary<XElement, Hyperlink> _links = new();
    private readonly HashSet<string> _droppedMoves = [];
    private readonly HashSet<string> _droppedComments = [];
    private List<OpcRelationship> _relationships;
    private IReadOnlyDictionary<string, Section> _sectionEnds;

    internal DocxWriteContext(DocxPackage package)
    {
        _package = package;
        foreach (var block in package.Blocks.Concat(package.HeadersFooters.SelectMany(h => h.Blocks))) Collect(block);
    }

    internal bool RelationshipsChanged { get; private set; }

    internal static IEnumerable<(XElement Owner, XElement Node)> Parts(Inline inline)
    {
        if (inline is OpaqueInline { Nodes: { } nodes }) return nodes.Select(n => (OpaqueInline.Owner(n), n));
        return inline.Source == null ? [] : [(inline.Source, inline.Node)];
    }

    internal static IEnumerable<XAttribute> Attributes(XElement source, bool first) =>
        first
            ? source.Attributes()
            : source.Attributes().Where(a => a.Name != W14 + "paraId" && a.Name != W14 + "textId" && !(a.Name.Namespace == W && a.Name.LocalName.StartsWith("rsid", StringComparison.Ordinal)));

    internal void Begin(List<OpcRelationship> relationships, IReadOnlyDictionary<string, Section> sectionEnds)
    {
        _relationships = relationships;
        _sectionEnds = sectionEnds;
        RelationshipsChanged = false;
        _droppedMoves.Clear();
        _droppedComments.Clear();
    }

    internal void DropMoveRange(string id) => _droppedMoves.Add(id);

    internal void DropComment(string id) => _droppedComments.Add(id);

    internal void RemoveOrphans(XElement root)
    {
        if (_droppedMoves.Count == 0 && _droppedComments.Count == 0) return;
        root.Descendants().Where(e => e.Name.Namespace == W && (string)e.Attribute(W + "id") is { } id &&
                                      (e.Name.LocalName is "moveFromRangeStart" or "moveFromRangeEnd" or "moveToRangeStart" or "moveToRangeEnd" && _droppedMoves.Contains(id) ||
                                       e.Name.LocalName is "commentRangeStart" or "commentRangeEnd" && _droppedComments.Contains(id))).Remove();
    }

    internal bool IsOriginal(object item) => _originals.Contains(item);

    internal bool IsIntact(XElement owner, IReadOnlyList<Inline> inlines) => _owned.TryGetValue(owner, out var original) && original.SequenceEqual(inlines);

    internal bool Claim(XElement source) => _claimed.Add(source);

    internal (RunFormat Old, RunFormat New)? FormatChange(Inline inline) =>
        inline is not (TextRun or Tab or Break) && _tokens.TryGetValue(TokenKey(inline), out var original) && original.Format != inline.Format
            ? (original.Format ?? new RunFormat(), inline.Format ?? new RunFormat())
            : null;

    internal XElement Block(Block block, bool topLevel) => block switch
    {
        Paragraph paragraph => ParagraphXml.Write(paragraph, this, topLevel),
        Table table => IsOriginal(table) ? table.Source : TableXml.Write(table, this),
        _ => block.Source
    };

    internal XElement SectionEnd(string paragraphId) =>
        paragraphId != null && _sectionEnds != null && _sectionEnds.TryGetValue(paragraphId, out var section) ? SectionXml.Write(section, _package.HeaderIds) : null;

    internal XElement Hyperlink(Hyperlink link, IEnumerable<XElement> content)
    {
        var original = link.Source != null ? _links.GetValueOrDefault(link.Source) : null;
        var element = link.Source != null ? new XElement(link.Source.Name, link.Source.Attributes()) : new XElement(W + "hyperlink", new XAttribute(W + "history", "1"));
        if (original?.Target != link.Target || link.Source == null) element.SetAttributeValue(InlineXml.R + "id", link.Target == null ? null : External(link.Target));
        if (original?.Anchor != link.Anchor) element.SetAttributeValue(W + "anchor", link.Anchor);
        if (original?.Tooltip != link.Tooltip) element.SetAttributeValue(W + "tooltip", link.Tooltip);
        element.Add(content);
        return element;
    }

    private static object TokenKey(Inline inline) => inline is OpaqueInline { Nodes: { } nodes } ? nodes : (inline.Source, inline.Node);

    private string External(string target)
    {
        if (_relationships.FirstOrDefault(r => r.Type == HyperlinkType && r.Target == target && r.TargetMode == "External") is { } existing) return existing.Id;
        var relationship = new OpcRelationship(OpcPackageWriter.NextId(_relationships), HyperlinkType, target, "External");
        _relationships.Add(relationship);
        RelationshipsChanged = true;
        return relationship.Id;
    }

    private void Collect(Block block)
    {
        _originals.Add(block);
        if (block is Paragraph paragraph)
            foreach (var inline in paragraph.Inlines)
            {
                if (inline is not (TextRun or Tab or Break)) _tokens.TryAdd(TokenKey(inline), inline);
                if (inline.Link?.Source is { } link) _links.TryAdd(link, inline.Link);
                foreach (var owner in Parts(inline).Select(p => p.Owner).Distinct())
                {
                    if (!_owned.TryGetValue(owner, out var list)) _owned[owner] = list = [];
                    list.Add(inline);
                }
            }

        if (block is not Table table) return;
        foreach (var row in table.Rows)
        {
            _originals.Add(row);
            foreach (var cell in row.Cells)
            {
                _originals.Add(cell);
                foreach (var child in cell.Blocks) Collect(child);
            }
        }
    }
}
