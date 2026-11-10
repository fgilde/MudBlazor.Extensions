using System.Globalization;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Presentation.PptxReader;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>
/// Writes a presentation as a .pptx file. Unchanged parts are copied from the loaded file; changed slides are patched in their original XML, so content
/// the editor does not model stays as it was.
/// </summary>
public static partial class PptxWriter
{
    internal const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    internal const string ImageRel = RelNs + "/image";
    private const string ContentTypePrefix = "application/vnd.openxmlformats-officedocument.presentationml.";
    private static readonly XNamespace P14 = "http://schemas.microsoft.com/office/powerpoint/2010/main";

    private static readonly string NewSlide =
        $"<p:sld {BlankPresentation.Namespaces}><p:cSld><p:spTree>{BlankPresentation.TreeStart}</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>";

    private static readonly string NewNotes =
        $"<p:notes {BlankPresentation.Namespaces}><p:cSld><p:spTree>{BlankPresentation.TreeStart}" +
        "<p:sp><p:nvSpPr><p:cNvPr id=\"2\" name=\"Slide Image Placeholder 1\"/><p:cNvSpPr><a:spLocks noGrp=\"1\" noRot=\"1\" noChangeAspect=\"1\"/></p:cNvSpPr>" +
        "<p:nvPr><p:ph type=\"sldImg\"/></p:nvPr></p:nvSpPr><p:spPr/></p:sp>" +
        "<p:sp><p:nvSpPr><p:cNvPr id=\"3\" name=\"Notes Placeholder 2\"/><p:cNvSpPr><a:spLocks noGrp=\"1\"/></p:cNvSpPr><p:nvPr><p:ph type=\"body\" idx=\"1\"/></p:nvPr></p:nvSpPr>" +
        "<p:spPr/><p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody></p:sp>" +
        "</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:notes>";

    /// <summary>Writes a presentation as a .pptx file.</summary>
    /// <param name="presentation">The presentation.</param>
    /// <param name="target">The stream that receives the file.</param>
    public static void Write(Presentation presentation, Stream target)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(target);
        var package = presentation.Package ?? throw new InvalidOperationException("The presentation was not loaded from a package.");
        using var writer = new OpcPackageWriter(package.Opc, target);
        new Job(presentation, package, writer).Run();
    }

    internal static string Relative(string source, string target)
    {
        var from = source.Split('/')[1..^1];
        var to = target.Split('/')[1..];
        var common = 0;
        while (common < from.Length && common < to.Length - 1 && string.Equals(from[common], to[common], StringComparison.OrdinalIgnoreCase)) common++;
        return string.Concat(Enumerable.Repeat("../", from.Length - common)) + string.Join('/', to[common..]);
    }

    internal static string NextId(IEnumerable<OpcRelationship> relationships) =>
        "rId" + (relationships.Select(r => r.Id.StartsWith("rId", StringComparison.Ordinal) && int.TryParse(r.Id.AsSpan(3), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0)
            .DefaultIfEmpty().Max() + 1);

    private sealed partial class Job(Presentation presentation, PptxPackage package, OpcPackageWriter writer)
    {
        private readonly OpcPackage _opc = package.Opc;
        private readonly Dictionary<string, string> _defaults = new(package.Opc.DefaultContentTypes, StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _overrides = new(package.Opc.OverrideContentTypes, StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _done = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _removed = new(StringComparer.OrdinalIgnoreCase);
        private bool _contentTypesChanged;
        private int _notes;
        private long _nextCreationId;

        internal void Run()
        {
            var parts = package.Slides.ToDictionary(s => s.Slide);
            var live = presentation.SlideList.ToHashSet();
            var deleted = package.Slides.Where(s => !live.Contains(s.Slide)).ToList();
            var kept = package.Slides.Except(deleted).ToList();
            foreach (var part in deleted)
            {
                _removed.Add(part.PartName);
                if (part.NotesPart != null && !kept.Any(k => string.Equals(k.NotesPart, part.NotesPart, StringComparison.OrdinalIgnoreCase))) _removed.Add(part.NotesPart);
            }

            _nextCreationId = package.Slides.Select(s => Long((string)CreationId(s.Xml)?.Attribute("val")) ?? 0).DefaultIfEmpty().Max() + 1;
            var presentationRels = _opc.Relationships(package.PresentationPart).ToList();
            var nextSlideId = Math.Max(255, package.PresentationXml.Root!.Element(P + "sldIdLst")?.Elements(P + "sldId")
                .Select(e => Long((string)e.Attribute("id")) ?? 0).DefaultIfEmpty().Max() ?? 0) + 1;
            WriteMedia();
            var entries = new List<SlideEntry>();
            foreach (var slide in presentation.SlideList)
            {
                if (parts.TryGetValue(slide, out var part))
                {
                    if (slide.Modified) WriteSlide(slide, part.PartName, part);
                    else if (part.NotesPart != null) _notes++;
                    entries.Add(new SlideEntry(part.RelationshipId, part.SlideId, true));
                    continue;
                }

                var name = writer.NextPartName("/ppt/slides/slide{0}.xml");
                WriteSlide(slide, name, slide.CopyOf != null && parts.TryGetValue(slide.CopyOf, out var basis) ? basis : null);
                var rel = new OpcRelationship(NextId(presentationRels), RelNs + "/slide", Relative(package.PresentationPart, name), null);
                presentationRels.Add(rel);
                entries.Add(new SlideEntry(rel.Id, (uint)nextSlideId++, false));
            }

            if (!presentation.SlideList.SequenceEqual(package.Slides.Select(s => s.Slide))) WritePresentation(entries, presentationRels);
            foreach (var part in _removed) Remove(part);
            if (deleted.Count > 0)
            {
                UnlinkCopiedParts();
                RemoveOrphans(deleted.Select(d => d.PartName));
            }

            if (_done.Count > 0) WriteAppProperties();
            if (_contentTypesChanged)
            {
                writer.PutContentTypes(_defaults, _overrides);
                _done.Add("/[Content_Types].xml");
            }

            foreach (var part in _opc.PartNames.Where(p => !_done.Contains(p)).ToList()) writer.Copy(part);
        }

        private void WriteMedia()
        {
            var used = presentation.SlideList.SelectMany(s => SlideShape.Flatten(s.Shapes)).OfType<PictureShape>().Select(p => p.ImagePart).Where(p => p != null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var (part, (data, contentType)) in presentation.PendingMedia)
            {
                if (!used.Contains(part)) continue;
                writer.Put(part, data);
                _done.Add(part);
                var extension = Path.GetExtension(part).TrimStart('.');
                if (_defaults.ContainsKey(extension) || _overrides.ContainsKey(part)) continue;
                _defaults[extension] = contentType;
                _contentTypesChanged = true;
            }
        }

        private void WriteSlide(Slide slide, string partName, PptxSlidePart source)
        {
            var isNew = source?.PartName != partName;
            var xml = source != null ? new XDocument(source.Xml) : XDocument.Parse(NewSlide);
            var copied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var rels = source == null
                ? []
                : source.Relationships.Where(r => !isNew || !r.Type.EndsWith("/notesSlide", StringComparison.Ordinal))
                    .Select(r => isNew && r.TargetMode == null && Owned(r.Type)
                        ? r with { Target = Relative(partName, CopyPart(_opc.ResolveTarget(source.PartName, r.Target), copied)) }
                        : Retarget(r, source.PartName, partName)).ToList();
            if (source == null && (slide.Layout ?? presentation.LayoutList.FirstOrDefault())?.PartName is { } layout)
                rels.Add(new OpcRelationship(NextId(rels), RelNs + "/slideLayout", Relative(partName, layout), null));
            var root = xml.Root!;
            if (root.Element(P + "cSld")?.Element(P + "spTree") is { } tree)
                new SlideXml(partName, source?.PartName, rels, _opc, (target, transform) => Relative(partName, CopyPart(target, [], transform))).WriteTree(tree, slide.ShapeList);
            RemoveReferences(xml, rels, r => isNew && r.Type.EndsWith("/comments", StringComparison.Ordinal) || IsDead(partName, r));
            if (isNew && source != null && CreationId(xml) is { } creationId) creationId.SetAttributeValue("val", (uint)(_nextCreationId++ % uint.MaxValue));
            var hidden = (string)root.Attribute("show") is "0" or "false";
            if (hidden != slide.Hidden) root.SetAttributeValue("show", slide.Hidden ? "0" : null);
            WriteNotes(slide, partName, source, isNew, rels);
            if (!isNew && rels.Count == source.Relationships.Count && XNode.DeepEquals(xml, source.Xml)) return;
            Put(partName, xml);
            if (isNew || rels.Count != source.Relationships.Count) PutRelationships(partName, rels);
            if (isNew) SetOverride(partName, ContentTypePrefix + "slide+xml");
        }

        private void WriteNotes(Slide slide, string partName, PptxSlidePart source, bool isNew, List<OpcRelationship> rels)
        {
            var notesSource = source?.NotesPart;
            if (!isNew && notesSource != null)
            {
                _notes++;
                if (_done.Contains(notesSource) || slide.Notes == NotesText(source.NotesXml)) return;
                var patched = new XDocument(source.NotesXml);
                TextBodyXml.WriteNotes(patched, slide.Notes);
                var patchedRels = _opc.Relationships(notesSource).ToList();
                var count = patchedRels.Count;
                RemoveReferences(patched, patchedRels, r => IsDead(notesSource, r));
                Put(notesSource, patched);
                if (patchedRels.Count != count) PutRelationships(notesSource, patchedRels);
                return;
            }

            var copy = isNew && notesSource != null;
            if (!copy && (slide.Notes.Length == 0 || package.NotesMasterPart == null)) return;
            var notesPart = writer.NextPartName("/ppt/notesSlides/notesSlide{0}.xml");
            var xml = copy ? new XDocument(source.NotesXml) : XDocument.Parse(NewNotes);
            TextBodyXml.WriteNotes(xml, slide.Notes);
            var notesRels = copy
                ? _opc.Relationships(notesSource).Where(r => !r.Type.EndsWith("/slide", StringComparison.Ordinal)).Select(r => Retarget(r, notesSource, notesPart)).ToList()
                : [new OpcRelationship("rId1", RelNs + "/notesMaster", Relative(notesPart, package.NotesMasterPart), null)];
            RemoveReferences(xml, notesRels, r => IsDead(notesPart, r));
            notesRels.Add(new OpcRelationship(NextId(notesRels), RelNs + "/slide", Relative(notesPart, partName), null));
            Put(notesPart, xml);
            PutRelationships(notesPart, notesRels);
            SetOverride(notesPart, ContentTypePrefix + "notesSlide+xml");
            rels.Add(new OpcRelationship(NextId(rels), RelNs + "/notesSlide", Relative(partName, notesPart), null));
            _notes++;
        }

        private void WritePresentation(List<SlideEntry> entries, List<OpcRelationship> rels)
        {
            var xml = new XDocument(package.PresentationXml);
            var root = xml.Root!;
            var list = root.Element(P + "sldIdLst");
            var originals = list?.Elements(P + "sldId").ToList() ?? [];
            var items = entries.Select(e => e.Kept && originals.Find(o => (string)o.Attribute(R + "id") == e.RelationshipId) is { } original
                ? new XElement(original)
                : new XElement(P + "sldId", new XAttribute("id", e.SlideId), new XAttribute(R + "id", e.RelationshipId))).ToList();
            if (list == null)
            {
                list = new XElement(P + "sldIdLst");
                if (root.Elements().LastOrDefault(e => e.Name == P + "sldMasterIdLst" || e.Name == P + "notesMasterIdLst" || e.Name == P + "handoutMasterIdLst") is { } before)
                    before.AddAfterSelf(list);
                else root.AddFirst(list);
            }

            list.ReplaceNodes(items);
            if (items.Count == 0) list.Remove();
            RemoveReferences(xml, rels, r => IsDead(package.PresentationPart, r));
            WriteSections(root, entries);
            Put(package.PresentationPart, xml);
            if (!rels.SequenceEqual(_opc.Relationships(package.PresentationPart))) PutRelationships(package.PresentationPart, rels);
        }

        private void WriteSections(XElement root, List<SlideEntry> entries)
        {
            var sections = root.Descendants(P14 + "section").ToList();
            if (sections.Count == 0) return;
            var home = new Dictionary<uint, int>();
            for (var i = 0; i < sections.Count; i++)
                foreach (var id in sections[i].Descendants(P14 + "sldId"))
                    home.TryAdd((uint)(Long((string)id.Attribute("id")) ?? 0), i);
            var original = package.Slides.Select(s => s.SlideId).ToList();
            var anchored = Anchored(entries.Select(e => e.Kept ? original.IndexOf(e.SlideId) : -1).ToList());
            var members = sections.Select(_ => new List<uint>()).ToList();
            var current = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                if (anchored.Contains(i) && home.TryGetValue(entries[i].SlideId, out var section)) current = Math.Max(current, section);
                members[current].Add(entries[i].SlideId);
            }

            for (var i = 0; i < sections.Count; i++)
            {
                var ids = sections[i].Element(P14 + "sldIdLst");
                if (ids == null) sections[i].AddFirst(ids = new XElement(P14 + "sldIdLst"));
                ids.ReplaceNodes(members[i].Select(id => new XElement(P14 + "sldId", new XAttribute("id", id))));
            }
        }

        private static HashSet<int> Anchored(List<int> positions)
        {
            var length = new int[positions.Count];
            var previous = new int[positions.Count];
            var best = -1;
            for (var i = 0; i < positions.Count; i++)
            {
                if (positions[i] < 0) continue;
                (length[i], previous[i]) = (1, -1);
                for (var j = 0; j < i; j++)
                    if (positions[j] >= 0 && positions[j] < positions[i] && length[j] + 1 > length[i])
                        (length[i], previous[i]) = (length[j] + 1, j);
                if (best < 0 || length[i] > length[best]) best = i;
            }

            var result = new HashSet<int>();
            for (var i = best; i >= 0; i = previous[i]) result.Add(i);
            return result;
        }

        private void WriteAppProperties()
        {
            if (_opc.Relationships("/").FirstOrDefault(r => r.Type.EndsWith("/extended-properties", StringComparison.Ordinal) && r.TargetMode == null) is not { } rel) return;
            var part = _opc.ResolveTarget("/", rel.Target);
            if (_opc.Entry(part) == null) return;
            var xml = _opc.ReadXml(part);
            var changed = false;
            void Set(string name, int value)
            {
                var text = value.ToString(CultureInfo.InvariantCulture);
                if (xml.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == name) is not { } element || element.Value == text) return;
                element.Value = text;
                changed = true;
            }

            Set("Slides", presentation.SlideList.Count);
            Set("Notes", _notes);
            Set("HiddenSlides", presentation.SlideList.Count(s => s.Hidden));
            if (changed) Put(part, xml);
        }

        private OpcRelationship Retarget(OpcRelationship rel, string from, string to) =>
            rel.TargetMode != null || string.Equals(from[..from.LastIndexOf('/')], to[..to.LastIndexOf('/')], StringComparison.OrdinalIgnoreCase)
                ? rel
                : rel with { Target = Relative(to, _opc.ResolveTarget(from, rel.Target)) };

        private void Put(string part, XDocument xml)
        {
            writer.Put(part, xml);
            _done.Add(part);
        }

        private void PutRelationships(string part, IEnumerable<OpcRelationship> rels)
        {
            var list = rels.ToList();
            _writtenRels[part] = list;
            writer.PutRelationships(part, list);
            _done.Add(OpcPackage.RelsPartOf(part));
        }

        private void SetOverride(string part, string contentType)
        {
            _overrides[part] = contentType;
            _contentTypesChanged = true;
        }

        private void Remove(string part)
        {
            _done.Add(part);
            _done.Add(OpcPackage.RelsPartOf(part));
            if (_overrides.Remove(part)) _contentTypesChanged = true;
        }
    }

    private readonly record struct SlideEntry(string RelationshipId, uint SlideId, bool Kept);
}
