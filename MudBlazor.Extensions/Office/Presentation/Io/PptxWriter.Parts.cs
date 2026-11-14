using System.Text.RegularExpressions;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

public static partial class PptxWriter
{
    private static readonly XNamespace SlideZoomNs = "http://schemas.microsoft.com/office/powerpoint/2016/slidezoom";

    private static readonly HashSet<string> OwnedTypes =
    [
        "chart", "chartEx", "chartUserShapes", "chartStyle", "chartColorStyle", "themeOverride", "package", "oleObject",
        "diagramData", "diagramLayout", "diagramQuickStyle", "diagramColors", "diagramDrawing", "tags"
    ];

    internal static bool Owned(string type) => OwnedTypes.Contains(type[(type.LastIndexOf('/') + 1)..]);

    private static XElement CreationId(XDocument slide) => slide.Root?.Element(P + "extLst")?.Descendants(P14 + "creationId").FirstOrDefault();

    private static string NamePattern(string part)
    {
        var escaped = part.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);
        var match = Regex.Match(escaped, @"\d*(\.[^./]*)?$");
        return escaped[..match.Index] + "{0}" + match.Groups[1].Value;
    }

    private static void RemoveReferences(XDocument xml, List<OpcRelationship> rels, Func<OpcRelationship, bool> drop)
    {
        var ids = rels.Where(drop).Select(r => r.Id).ToHashSet();
        if (ids.Count == 0) return;
        rels.RemoveAll(r => ids.Contains(r.Id));
        if (xml == null) return;
        foreach (var reference in xml.Descendants().Attributes().Where(a => a.Name.Namespace == R && ids.Contains(a.Value)).ToList())
        {
            if (reference.Parent!.Document != xml) continue;
            var target = SlideZoomOf(reference.Parent) ?? reference.Parent;
            var parent = target.Parent;
            target.Remove();
            while (parent is { HasElements: false } && parent.Name.LocalName is "ext" or "extLst")
            {
                var next = parent.Parent;
                parent.Remove();
                parent = next;
            }
        }
    }

    private static XElement SlideZoomOf(XElement element)
    {
        var shape = element.Ancestors().FirstOrDefault(SlideXml.IsShape);
        if (shape?.Parent?.Parent is { } outer && outer.Name.LocalName == "AlternateContent") shape = outer;
        return shape?.Descendants().Any(d => d.Name.Namespace == SlideZoomNs) == true ? shape : null;
    }

    private sealed partial class Job
    {
        private readonly Dictionary<string, List<OpcRelationship>> _writtenRels = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _origins = new(StringComparer.OrdinalIgnoreCase);

        private bool IsDead(string part, OpcRelationship rel) => rel.TargetMode == null && _removed.Contains(_opc.ResolveTarget(part, rel.Target));

        private string CopyPart(string part, Dictionary<string, string> copied, Action<XDocument> transform = null)
        {
            var origin = _origins.GetValueOrDefault(part, part);
            if (_opc.Entry(origin) == null) return part;
            if (copied.TryGetValue(origin, out var existing)) return existing;
            var name = writer.NextPartName(NamePattern(origin));
            copied[origin] = name;
            _origins[name] = origin;
            if (transform == null) writer.Put(name, _opc.ReadBytes(origin));
            else
            {
                var xml = _opc.ReadXml(origin);
                transform(xml);
                writer.Put(name, xml);
            }

            if (_opc.OverrideContentTypes.TryGetValue(origin, out var contentType)) SetOverride(name, contentType);
            var rels = _opc.Relationships(origin);
            if (rels.Count > 0)
                PutRelationships(name, rels.Select(r => r.TargetMode != null
                    ? r
                    : r with { Target = Relative(name, Owned(r.Type) ? CopyPart(_opc.ResolveTarget(origin, r.Target), copied) : _opc.ResolveTarget(origin, r.Target)) }).ToList());
            return name;
        }

        private void UnlinkCopiedParts()
        {
            foreach (var part in _opc.PartNames.Where(p => !_done.Contains(p) && !p.EndsWith(".rels", StringComparison.OrdinalIgnoreCase)).ToList())
            {
                var rels = _opc.Relationships(part).ToList();
                if (!rels.Any(r => IsDead(part, r))) continue;
                var xml = _opc.ContentType(part)?.EndsWith("xml", StringComparison.OrdinalIgnoreCase) == true ? _opc.ReadXml(part) : null;
                RemoveReferences(xml, rels, r => IsDead(part, r));
                if (xml != null) Put(part, xml);
                PutRelationships(part, rels);
            }
        }

        private void RemoveOrphans(IEnumerable<string> deleted)
        {
            var candidates = Reach(deleted, _opc.Relationships);
            var live = Reach(["/"], p => _writtenRels.TryGetValue(p, out var rels) ? rels : _removed.Contains(p) ? [] : _opc.Relationships(p));
            foreach (var part in candidates.Where(p => !live.Contains(p) && !_removed.Contains(p) && _opc.Entry(p) != null).ToList())
            {
                _removed.Add(part);
                Remove(part);
            }
        }

        private HashSet<string> Reach(IEnumerable<string> starts, Func<string, IReadOnlyList<OpcRelationship>> relationships)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>(starts);
            while (queue.TryDequeue(out var part))
                foreach (var rel in relationships(part).Where(r => r.TargetMode == null))
                {
                    var target = _opc.ResolveTarget(part, rel.Target);
                    if (seen.Add(target)) queue.Enqueue(target);
                }

            return seen;
        }
    }
}
