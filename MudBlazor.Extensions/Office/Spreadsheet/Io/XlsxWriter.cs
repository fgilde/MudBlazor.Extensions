using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Writes a <see cref="Workbook"/> as an Office Open XML workbook (<c>.xlsx</c>, or <c>.xlsm</c> for macro-enabled sources).</summary>
public static class XlsxWriter
{
    private const string PackageRelationshipsNs = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string ContentTypesNs = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string ContentTypesPart = "[Content_Types].xml";
    private const string SpreadsheetType = "application/vnd.openxmlformats-officedocument.spreadsheetml.";
    private const string WorksheetType = SpreadsheetType + "worksheet+xml";

    /// <summary>
    /// Writes the workbook. A workbook read by <see cref="XlsxReader"/> is written surgically: workbook.xml, the worksheets,
    /// shared strings and styles are regenerated (keeping worksheet content, attributes and styles the model does not represent),
    /// the calculation chain is removed, and every other part of the source package is copied unchanged, so macros, pivot
    /// tables, drawings and custom XML survive and a macro-enabled workbook stays macro-enabled. Any other workbook is written as a
    /// minimal package without a theme. Formulas are written with their calculated values; Excel does not recalculate on open.
    /// </summary>
    /// <param name="wb">The workbook.</param>
    /// <param name="target">The stream to write the package to; it is left open.</param>
    public static void Write(Workbook wb, Stream target)
    {
        ArgumentNullException.ThrowIfNull(wb);
        ArgumentNullException.ThrowIfNull(target);
        var package = wb.Package;
        using var source = package == null ? null : new ZipArchive(new MemoryStream(package.Source, false), ZipArchiveMode.Read);
        new PackageWriter(wb, package, source).Write(target);
    }

    private sealed class PackageWriter(Workbook wb, XlsxPackage package, ZipArchive source)
    {
        private readonly Dictionary<string, Action<Stream>> _parts = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _removed = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _added = [];
        private readonly Dictionary<string, string> _defaults = new(package?.DefaultContentTypes ?? new Dictionary<string, string>
        {
            ["rels"] = "application/vnd.openxmlformats-package.relationships+xml",
            ["xml"] = "application/xml"
        }, StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _overrides = new(package?.OverrideContentTypes ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        private readonly List<XlsxRelationship> _rels = [.. package?.WorkbookRelationships ?? []];
        private readonly string _workbookPart = package?.WorkbookPart ?? "xl/workbook.xml";
        private Dictionary<string, List<string>> _references;

        public void Write(Stream target)
        {
            var typeBase = _rels.Select(r => r.Type).FirstOrDefault(t => t != null && (t.EndsWith("/worksheet", StringComparison.Ordinal)
                                                                                      || t.EndsWith("/styles", StringComparison.Ordinal)
                                                                                      || t.EndsWith("/sharedStrings", StringComparison.Ordinal)))
                is { } known ? known[..known.LastIndexOf('/')] : RelationshipsNs;
            if (package == null)
            {
                _overrides["/" + _workbookPart] = SpreadsheetType + "sheet.main+xml";
                _parts["_rels/.rels"] = s => WriteRelationships(s,
                    [new XlsxRelationship("rId1", typeBase + "/officeDocument", _workbookPart, false)]);
                _added.Add("_rels/.rels");
            }

            foreach (var rel in _rels.Where(r => IsType(r, "calcChain")).ToList())
            {
                _rels.Remove(rel);
                if (!rel.External) Remove(ResolveTarget(_workbookPart, rel.Target));
            }

            var doc = package == null
                ? new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement(XName.Get("workbook", SheetXmlWriter.MainNs),
                    new XAttribute(XNamespace.Xmlns + "r", typeBase), new XElement(XName.Get("bookViews", SheetXmlWriter.MainNs),
                        new XElement(XName.Get("workbookView", SheetXmlWriter.MainNs))), new XElement(XName.Get("sheets", SheetXmlWriter.MainNs))))
                : Load(package.WorkbookPart);
            var ns = doc.Root!.Name.Namespace;
            var theme = package?.ThemePart is { } themePart && Find(source, themePart) is { } themeEntry ? Read(themeEntry, ThemeColors.Read) : ThemeColors.Office;
            var stylesDoc = package?.StylesPart is { } stylesPart && Find(source, stylesPart) != null ? Load(stylesPart) : null;
            var styles = new StylesXmlWriter(stylesDoc, wb.Styles, package?.CanonicalXf ?? [0], CultureInfo.CurrentCulture);
            var stylesPartName = stylesDoc != null ? package.StylesPart : AddPart("styles", typeBase, "styles.xml");
            var sourceSst = package?.SharedStringsPart is { } existingSst ? Find(source, existingSst) : null;
            var strings = sourceSst == null ? new SharedStringsWriter() : Read(sourceSst, s => new SharedStringsWriter(s));
            var sstPart = sourceSst != null ? package.SharedStringsPart
                : wb.Sheets.Any(s => s.UsedCells().Any(c => c.Cell.Formula == null && c.Cell.Value.Kind == CellValueKind.Text))
                    ? AddPart("sharedStrings", typeBase, "sharedStrings.xml")
                    : null;

            var sheets = Sheets(typeBase);
            UpdateWorkbook(doc, ns, typeBase, sheets);
            _parts[_workbookPart] = s => SheetXmlWriter.Save(doc, s);
            var tables = sheets.Where(x => x.Worksheet != null).ToDictionary(x => x.Worksheet, x => Tables(x, typeBase));
            foreach (var sheet in sheets.Where(x => x.Worksheet != null && (x.Original == null || x.Worksheet.Modified || Find(source, x.PartName) == null)))
                _parts[sheet.PartName] = s => SheetXmlWriter.Write(s, sheet.Worksheet, sheet.Original, package, styles, strings, theme, tables[sheet.Worksheet]);

            var relsPart = RelsPartOf(_workbookPart);
            if (package == null || !_rels.SequenceEqual(package.WorkbookRelationships)) _parts[relsPart] = s => WriteRelationships(s, _rels);
            if (package == null) _added.Add(relsPart);
            if (package == null || !_overrides.OrderBy(p => p.Key).SequenceEqual(package.OverrideContentTypes.OrderBy(p => p.Key))
                                || !_defaults.OrderBy(p => p.Key).SequenceEqual(package.DefaultContentTypes.OrderBy(p => p.Key)))
                _parts[ContentTypesPart] = WriteContentTypes;
            if (package == null) _added.Insert(0, ContentTypesPart);

            using var zip = new ZipArchive(target, ZipArchiveMode.Create, true);
            var deferred = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { stylesPartName };
            if (sstPart != null) deferred.Add(sstPart);
            var names = (source?.Entries.Select(e => e.FullName) ?? []).Concat(_added).Concat(_parts.Keys).Where(n => !deferred.Contains(n)).Concat(deferred);
            var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
            {
                if (_removed.Contains(name) || !written.Add(name)) continue;
                if (name.Equals(stylesPartName, StringComparison.OrdinalIgnoreCase))
                {
                    if (styles.Changed) Put(zip, name, styles.Save);
                    else Copy(zip, name);
                }
                else if (name.Equals(sstPart, StringComparison.OrdinalIgnoreCase))
                {
                    if (strings.Unchanged) Copy(zip, name);
                    else Put(zip, name, s => strings.Write(s, ns));
                }
                else if (_parts.TryGetValue(name, out var write)) Put(zip, name, write);
                else Copy(zip, name);
            }
        }

        private sealed record SheetEntry(Worksheet Worksheet, XlsxSheetPart Original, int OriginalIndex, string PartName, string RelationshipId, string SheetId);

        private List<SheetEntry> Sheets(string typeBase)
        {
            var originals = package?.Sheets ?? [];
            var list = new List<SheetEntry>();
            var nextSheetId = originals.Select(p => int.TryParse(p.SheetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0)
                .DefaultIfEmpty(0).Max() + 1;
            foreach (var ws in wb.Sheets)
            {
                var index = originals.FindIndex(p => p.Worksheet == ws && p.PartName != null);
                if (index >= 0)
                {
                    var o = originals[index];
                    list.Add(new SheetEntry(ws, o, index, o.PartName, o.RelationshipId, o.SheetId));
                    continue;
                }

                var part = AddPart("worksheet", typeBase, "worksheets/sheet{0}.xml", WorksheetType);
                list.Add(new SheetEntry(ws, null, -1, part, _rels[^1].Id, (nextSheetId++).ToString(CultureInfo.InvariantCulture)));
            }

            for (var i = 0; i < originals.Count; i++)
            {
                var o = originals[i];
                if (o.Worksheet == null) list.Insert(Math.Min(i, list.Count), new SheetEntry(null, o, i, o.PartName, o.RelationshipId, o.SheetId));
                else if (!wb.Sheets.Contains(o.Worksheet))
                {
                    _rels.RemoveAll(r => r.Id == o.RelationshipId);
                    if (o.PartName != null) Remove(o.PartName);
                }
            }

            return list;
        }

        private void UpdateWorkbook(XDocument doc, XNamespace ns, string relNs, List<SheetEntry> sheets)
        {
            var root = doc.Root!;
            var sheetsElement = Child(root, "sheets") ?? new XElement(ns + "sheets");
            if (sheetsElement.Parent == null) root.Add(sheetsElement);
            var originalElements = sheetsElement.Elements().Where(e => e.Name.LocalName == "sheet").ToList();
            var map = new Dictionary<int, int>();
            sheetsElement.RemoveNodes();
            for (var i = 0; i < sheets.Count; i++)
            {
                var entry = sheets[i];
                if (entry.OriginalIndex >= 0) map[entry.OriginalIndex] = i;
                var element = entry.OriginalIndex >= 0 && entry.OriginalIndex < originalElements.Count
                    ? new XElement(originalElements[entry.OriginalIndex])
                    : new XElement(ns + "sheet", new XAttribute("name", ""), new XAttribute("sheetId", entry.SheetId), new XAttribute(XName.Get("id", relNs), entry.RelationshipId));
                if (entry.Worksheet is { } ws)
                {
                    element.SetAttributeValue("name", ws.Name);
                    element.SetAttributeValue("state", ws.Hidden ? entry.Original?.State is "hidden" or "veryHidden" ? entry.Original.State : "hidden" : null);
                }

                sheetsElement.Add(element);
            }

            var names = new List<XElement>();
            var global = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in package?.DefinedNames?.Elements().Where(e => e.Name.LocalName == "definedName") ?? [])
            {
                var copy = new XElement(name);
                if (copy.Attribute("localSheetId") is { } local)
                {
                    if (!int.TryParse(local.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var old) || !map.TryGetValue(old, out var now)) continue;
                    local.Value = now.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    var key = (string)copy.Attribute("name") ?? "";
                    if (!wb.DefinedNames.TryGetValue(key, out var value) || !global.Add(key)) continue;
                    copy.Value = SheetXmlWriter.FormulaText(value);
                }

                names.Add(copy);
            }

            foreach (var (key, value) in wb.DefinedNames)
                if (global.Add(key))
                    names.Add(new XElement(ns + "definedName", new XAttribute("name", key), SheetXmlWriter.FormulaText(value)));

            Child(root, "definedNames")?.Remove();
            if (names.Count > 0)
            {
                var definedNames = new XElement(ns + "definedNames", names);
                var anchor = root.Elements().LastOrDefault(e => e.Name.LocalName is "sheets" or "functionGroups" or "externalReferences");
                anchor!.AddAfterSelf(definedNames);
            }

            foreach (var view in Child(root, "bookViews")?.Elements() ?? [])
                foreach (var attribute in (ReadOnlySpan<string>)["activeTab", "firstSheet"])
                    if (view.Attribute(attribute) is { } a)
                        a.Value = int.TryParse(a.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var old) && map.TryGetValue(old, out var now)
                            ? now.ToString(CultureInfo.InvariantCulture)
                            : "0";
        }

        private List<string> Tables(SheetEntry sheet, string typeBase)
        {
            var original = sheet.Original?.TableParts ?? [];
            var ids = new List<string>();
            if (original.Count == 0 && sheet.Worksheet.Tables.Count == 0) return ids;
            var rels = sheet.Original != null && Find(source, RelsPartOf(sheet.PartName)) != null ? ReadRelationships(source, sheet.PartName) : [];
            var changed = false;
            foreach (var table in sheet.Worksheet.Tables)
            {
                if (table.PartName != null && original.Any(p => p.PartName == table.PartName))
                {
                    ids.Add(table.RelationshipId);
                    if (table.Dirty) _parts[table.PartName] = s => SheetXmlWriter.Save(new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), TableXml.Write(table)), s);
                    continue;
                }

                var existing = new HashSet<string>((source?.Entries.Select(e => e.FullName) ?? []).Concat(_added), StringComparer.OrdinalIgnoreCase);
                var folder = _workbookPart[..(_workbookPart.LastIndexOf('/') + 1)] + "tables/table";
                var n = 1;
                while (existing.Contains(folder + n + ".xml")) n++;
                var part = folder + n + ".xml";
                _added.Add(part);
                _overrides["/" + part] = TableXml.ContentType;
                var id = 1;
                while (rels.Any(r => r.Id == "rId" + id)) id++;
                rels.Add(new XlsxRelationship("rId" + id, typeBase + "/table", Relative(sheet.PartName, part), false));
                ids.Add("rId" + id);
                changed = true;
                _parts[part] = s => SheetXmlWriter.Save(new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), TableXml.Write(table)), s);
            }

            foreach (var (id, part) in original.Where(p => sheet.Worksheet.Tables.All(t => t.PartName != p.PartName)))
            {
                rels.RemoveAll(r => r.Id == id);
                Remove(part);
                changed = true;
            }

            if (changed) _parts[RelsPartOf(sheet.PartName)] = s => WriteRelationships(s, rels);
            return ids;
        }

        private static string Relative(string from, string to)
        {
            var a = from.Split('/')[..^1];
            var b = to.Split('/');
            var i = 0;
            while (i < a.Length && i < b.Length - 1 && a[i] == b[i]) i++;
            return string.Concat(Enumerable.Repeat("../", a.Length - i)) + string.Join('/', b[i..]);
        }

        private string AddPart(string type, string typeBase, string target, string contentType = null)
        {
            var folder = _workbookPart[..(_workbookPart.LastIndexOf('/') + 1)];
            var existing = new HashSet<string>((source?.Entries.Select(e => e.FullName) ?? []).Concat(_added), StringComparer.OrdinalIgnoreCase);
            var n = 1;
            var name = target;
            while (target.Contains("{0}", StringComparison.Ordinal) && existing.Contains(folder + (name = string.Format(CultureInfo.InvariantCulture, target, n++)))
                   && !_removed.Contains(folder + name))
            {
            }

            var id = 1;
            while (_rels.Any(r => r.Id == "rId" + id)) id++;
            _rels.Add(new XlsxRelationship("rId" + id, typeBase + "/" + type, name, false));
            _overrides["/" + folder + name] = contentType ?? SpreadsheetType + type + "+xml";
            _removed.Remove(folder + name);
            _added.Add(folder + name);
            return folder + name;
        }

        private void Remove(string part)
        {
            if (!_removed.Add(part)) return;
            _overrides.Remove("/" + part);
            var relsPart = RelsPartOf(part);
            if (Find(source, relsPart) == null) return;
            _removed.Add(relsPart);
            foreach (var rel in ReadRelationships(source, part).Where(r => !r.External))
            {
                var target = ResolveTarget(part, rel.Target);
                if (!References().Any(p => !_removed.Contains(p.Key) && p.Value.Contains(target, StringComparer.OrdinalIgnoreCase))
                    && !_rels.Any(r => !r.External && ResolveTarget(_workbookPart, r.Target).Equals(target, StringComparison.OrdinalIgnoreCase)))
                    Remove(target);
            }
        }

        private Dictionary<string, List<string>> References()
        {
            if (_references != null) return _references;
            _references = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in source.Entries.Where(e => e.FullName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase)))
            {
                var name = entry.FullName;
                var slash = name.LastIndexOf("_rels/", StringComparison.OrdinalIgnoreCase);
                if (slash < 0) continue;
                var owner = name[..slash] + name[(slash + 6)..^5];
                if (owner.Equals(_workbookPart, StringComparison.OrdinalIgnoreCase)) continue;
                _references[name] = ReadRelationships(source, owner).Where(r => !r.External).Select(r => ResolveTarget(owner, r.Target)).ToList();
            }

            return _references;
        }

        private XDocument Load(string part) => Read(Find(source, part), LoadXml);

        private static T Read<T>(ZipArchiveEntry entry, Func<Stream, T> read)
        {
            using var s = entry.Open();
            return read(s);
        }

        private static void Put(ZipArchive zip, string name, Action<Stream> write)
        {
            using var s = zip.CreateEntry(name, CompressionLevel.Optimal).Open();
            write(s);
        }

        private void Copy(ZipArchive zip, string name)
        {
            var from = source?.GetEntry(name);
            if (from == null) return;
            var to = zip.CreateEntry(from.FullName, CompressionLevel.Optimal);
            to.LastWriteTime = from.LastWriteTime;
            using var input = from.Open();
            using var output = to.Open();
            input.CopyTo(output);
        }

        private static void WriteRelationships(Stream stream, IEnumerable<XlsxRelationship> rels)
        {
            using var w = XmlWriter.Create(stream, SheetXmlWriter.Settings);
            w.WriteStartDocument(true);
            w.WriteStartElement("Relationships", PackageRelationshipsNs);
            foreach (var rel in rels)
            {
                w.WriteStartElement("Relationship", PackageRelationshipsNs);
                w.WriteAttributeString("Id", rel.Id);
                w.WriteAttributeString("Type", rel.Type);
                w.WriteAttributeString("Target", rel.Target);
                if (rel.External) w.WriteAttributeString("TargetMode", "External");
                w.WriteEndElement();
            }

            w.WriteEndElement();
        }

        private void WriteContentTypes(Stream stream)
        {
            using var w = XmlWriter.Create(stream, SheetXmlWriter.Settings);
            w.WriteStartDocument(true);
            w.WriteStartElement("Types", ContentTypesNs);
            foreach (var (ext, type) in _defaults)
            {
                w.WriteStartElement("Default", ContentTypesNs);
                w.WriteAttributeString("Extension", ext);
                w.WriteAttributeString("ContentType", type);
                w.WriteEndElement();
            }

            foreach (var (part, type) in _overrides)
            {
                w.WriteStartElement("Override", ContentTypesNs);
                w.WriteAttributeString("PartName", part);
                w.WriteAttributeString("ContentType", type);
                w.WriteEndElement();
            }

            w.WriteEndElement();
        }
    }
}
