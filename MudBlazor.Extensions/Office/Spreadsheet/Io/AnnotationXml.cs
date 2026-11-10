using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class AnnotationXml
{
    internal const string CommentsContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.comments+xml";
    internal const string VmlContentType = "application/vnd.openxmlformats-officedocument.vmlDrawing";

    private const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Singleline;
    private static readonly Regex ShapeBlock = new(@"<v:shape\b[^>]*(?<!/)>.*?</v:shape>", Options);
    private static readonly Regex NoteType = new(@"ObjectType\s*=\s*[""']Note[""']", Options);
    private static readonly Regex ClientData = new(@"<x:ClientData\b", Options);
    private static readonly Regex RowTag = new(@"(?<=<x:Row>\s*)\d+(?=\s*</x:Row>)", Options);
    private static readonly Regex ColumnTag = new(@"(?<=<x:Column>\s*)\d+(?=\s*</x:Column>)", Options);
    private static readonly Regex AnchorTag = new(@"(?<=<x:Anchor>)[^<]*(?=</x:Anchor>)", Options);
    private static readonly Regex Digits = new(@"\d+", Options);
    private static readonly Regex ShapeId = new(@"\bid\s*=\s*[""']_x0000_s(\d+)", Options);
    private static readonly Regex NoteShapeType = new(@"<v:shapetype\b[^>]*\bid\s*=\s*[""']_x0000_t202[""']", Options);
    private static readonly Regex FirstShape = new(@"<v:shape\b", Options);
    private static readonly Regex IdMap = new(@"<o:idmap\b[^>]*\bdata\s*=\s*[""']([^""']*)", Options);

    internal static void Read(ZipArchive zip, XlsxSheetPart part, Worksheet ws)
    {
        if (part.PartName == null) return;
        var rels = ReadRelationships(zip, part.PartName);
        var hyperlinks = SheetChild(part, "hyperlinks")?.Elements().Where(e => e.Name.LocalName == "hyperlink").ToList() ?? [];
        ws.Links = hyperlinks.Select(e => Link(e, rels)).OfType<CellLink>().ToList();
        part.OpaqueLinks.AddRange(hyperlinks.Where(e => !CellRange.TryParse((string)e.Attribute("ref"), out _)));
        ws.NotesReadOnly = rels.Any(r => IsType(r, "threadedComment"));

        var shapes = new Dictionary<long, string>();
        var legacyId = RelationshipId(SheetChild(part, "legacyDrawing"));
        if (legacyId != null && Entry(zip, part, rels.FirstOrDefault(r => r.Id == legacyId)) is { } vml)
        {
            part.VmlPart = vml.FullName;
            part.VmlRelationshipId = legacyId;
            using (var s = vml.Open()) part.VmlText = ReadVml(s);
            var notes = ShapeBlock.Matches(part.VmlText).Select(m => m.Value).Where(v => NoteType.IsMatch(v)).ToList();
            foreach (var shape in notes)
                if (ShapeCell(shape) is { } cell)
                    shapes.TryAdd(cell.Key, shape);
            part.VmlOnlyNotes = ClientData.Count(part.VmlText) == notes.Count;
        }

        var commentsRel = rels.FirstOrDefault(r => IsType(r, "comments"));
        if (Entry(zip, part, commentsRel) is { } comments)
        {
            part.CommentsPart = comments.FullName;
            part.CommentsRelationshipId = commentsRel.Id;
            try
            {
                using var s = comments.Open();
                part.CommentsRoot = LoadXml(s).Root;
            }
            catch (XmlException)
            {
                ws.NotesReadOnly = true;
                part.OriginalNotes = ws.Notes;
                return;
            }

            var authors = Child(part.CommentsRoot, "authors")?.Elements().Select(a => SheetXmlReader.Decode(a.Value)).ToList() ?? [];
            ws.Notes = Child(part.CommentsRoot, "commentList")?.Elements().Where(e => e.Name.LocalName == "comment").Select(e => Note(e, authors, shapes))
                .OfType<CellNote>().ToList() ?? [];
        }

        part.OriginalNotes = ws.Notes;
    }

    internal static string ReadVml(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.Latin1, detectEncodingFromByteOrderMarks: false);
        return reader.ReadToEnd();
    }

    internal static XElement WriteLinks(XElement original, IReadOnlyList<CellLink> links, IReadOnlyList<XElement> opaque, XNamespace ns, Func<CellLink, string> relationship)
    {
        if (links.Count == 0 && opaque.Count == 0) return null;
        var e = original != null ? new XElement(original.Name, original.Attributes()) : new XElement(ns + "hyperlinks");
        foreach (var link in links)
        {
            var h = link.Source != null ? new XElement(link.Source) : new XElement(ns + "hyperlink");
            h.SetAttributeValue("ref", link.Area.ToA1());
            var id = relationship(link);
            var existing = h.Attributes().FirstOrDefault(a => a.Name.LocalName == "id" && a.Name.Namespace != XNamespace.None);
            if (existing != null && id != null) existing.Value = id;
            else
            {
                existing?.Remove();
                if (id != null) h.SetAttributeValue(XName.Get("id", RelationshipsNs), id);
            }

            h.SetAttributeValue("location", Empty(link.Location));
            h.SetAttributeValue("display", SharedStringsWriter.Escape(Empty(link.Display)));
            h.SetAttributeValue("tooltip", SharedStringsWriter.Escape(Empty(link.Tooltip)));
            e.Add(h);
        }

        e.Add(opaque);
        return e;
    }

    internal static XDocument WriteComments(XElement original, IReadOnlyList<CellNote> notes)
    {
        var ns = original?.Name.Namespace ?? XNamespace.Get(SheetXmlWriter.MainNs);
        var root = original != null ? new XElement(original) : new XElement(ns + "comments");
        var authors = Child(root, "authors");
        if (authors == null) root.AddFirst(authors = new XElement(ns + "authors"));
        var names = authors.Elements().Select(a => SheetXmlReader.Decode(a.Value)).ToList();
        var list = Child(root, "commentList");
        if (list == null) authors.AddAfterSelf(list = new XElement(ns + "commentList"));
        list.RemoveNodes();
        foreach (var note in notes.OrderBy(n => n.Cell.Row).ThenBy(n => n.Cell.Column))
        {
            var author = names.IndexOf(note.Author ?? "");
            if (author < 0)
            {
                names.Add(note.Author ?? "");
                author = names.Count - 1;
            }

            var c = note.Source != null ? new XElement(note.Source) : new XElement(ns + "comment");
            c.SetAttributeValue("ref", note.Cell.Relative.ToA1());
            c.SetAttributeValue("authorId", author);
            if (c.Attribute("shapeId") == null) c.SetAttributeValue("shapeId", 0);
            var text = Child(c, "text");
            if (text == null || SheetXmlReader.Text(text, out _) != (note.Text ?? ""))
            {
                text?.Remove();
                c.AddFirst(new XElement(ns + "text", new XElement(ns + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), SharedStringsWriter.Escape(note.Text ?? ""))));
            }

            list.Add(c);
        }

        authors.RemoveNodes();
        authors.Add(names.Select(n => new XElement(ns + "author", SharedStringsWriter.Escape(n))));
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root);
    }

    internal static string WriteVml(string original, IReadOnlyList<CellNote> notes, int drawing)
    {
        var text = original ?? Header(drawing);
        var byShape = new Dictionary<string, CellNote>();
        foreach (var note in notes.Where(n => n.Shape != null)) byShape.TryAdd(note.Shape, note);
        var placed = new HashSet<CellNote>(ReferenceEqualityComparer.Instance);
        text = ShapeBlock.Replace(text, m => !NoteType.IsMatch(m.Value) ? m.Value
            : byShape.TryGetValue(m.Value, out var note) && placed.Add(note) ? Move(m.Value, note.Cell)
            : "");
        var added = notes.Where(n => !placed.Contains(n)).ToList();
        if (added.Count > 0)
        {
            var id = ShapeId.Matches(text).Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).DefaultIfEmpty(1024 * Drawings(text).DefaultIfEmpty(1).Min()).Max();
            var shapes = string.Concat(added.Select(n => NewShape(n.Cell, ++id)));
            var end = text.LastIndexOf("</xml>", StringComparison.OrdinalIgnoreCase);
            text = end < 0 ? text + shapes : text.Insert(end, shapes);
            if (!NoteShapeType.IsMatch(text)) text = text.Insert(FirstShape.Match(text).Index, ShapeType);
        }

        return ClientData.IsMatch(text) || ShapeBlock.IsMatch(text) ? text : null;
    }

    internal static int MaxDrawing(string vml) => Drawings(vml).DefaultIfEmpty(0).Max();

    private static IEnumerable<int> Drawings(string vml) =>
        IdMap.Matches(vml).SelectMany(m => Digits.Matches(m.Groups[1].Value)).Select(d => int.TryParse(d.Value, CultureInfo.InvariantCulture, out var n) ? n : 0);

    private static CellLink Link(XElement e, List<XlsxRelationship> rels)
    {
        if (!CellRange.TryParse((string)e.Attribute("ref"), out var range)) return null;
        range = range.Normalized;
        var id = RelationshipId(e);
        var target = id == null ? null : rels.FirstOrDefault(r => r.Id == id)?.Target;
        return new CellLink(new CellRef(range.Top, range.Left), target, (string)e.Attribute("location"), SheetXmlReader.Decode((string)e.Attribute("display")),
            SheetXmlReader.Decode((string)e.Attribute("tooltip")))
        {
            Source = e,
            RelationshipId = target == null ? null : id,
            Span = (range.Bottom - range.Top, range.Right - range.Left)
        };
    }

    private static CellNote Note(XElement e, List<string> authors, Dictionary<long, string> shapes)
    {
        if (!CellRef.TryParse((string)e.Attribute("ref"), out var cell)) return null;
        cell = cell.Relative;
        var author = int.TryParse((string)e.Attribute("authorId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) && i >= 0 && i < authors.Count ? authors[i] : "";
        var text = Child(e, "text") is { } t ? SheetXmlReader.Text(t, out _) : "";
        return new CellNote(cell, author, text) { Source = e, Shape = shapes.GetValueOrDefault(cell.Key) };
    }

    private static CellRef? ShapeCell(string shape) =>
        int.TryParse(RowTag.Match(shape).Value, CultureInfo.InvariantCulture, out var row) && int.TryParse(ColumnTag.Match(shape).Value, CultureInfo.InvariantCulture, out var col)
        && row < CellRef.MaxRows && col < CellRef.MaxColumns
            ? new CellRef(row, col)
            : null;

    private static string Move(string shape, CellRef cell)
    {
        if (ShapeCell(shape) is not { } from || from == cell) return shape;
        var (rows, columns) = (cell.Row - from.Row, cell.Column - from.Column);
        shape = RowTag.Replace(shape, cell.Row.ToString(CultureInfo.InvariantCulture), 1);
        shape = ColumnTag.Replace(shape, cell.Column.ToString(CultureInfo.InvariantCulture), 1);
        return AnchorTag.Replace(shape, anchor =>
        {
            var i = 0;
            return Digits.Replace(anchor.Value, d => (i++ switch
            {
                0 or 4 => Math.Max(0, int.Parse(d.Value, CultureInfo.InvariantCulture) + columns),
                2 or 6 => Math.Max(0, int.Parse(d.Value, CultureInfo.InvariantCulture) + rows),
                _ => int.Parse(d.Value, CultureInfo.InvariantCulture)
            }).ToString(CultureInfo.InvariantCulture));
        }, 1);
    }

    private const string ShapeType =
        "<v:shapetype id=\"_x0000_t202\" coordsize=\"21600,21600\" o:spt=\"202\"\r\n  path=\"m,l,21600r21600,l21600,xe\">\r\n  <v:stroke joinstyle=\"miter\"/>\r\n"
        + "  <v:path gradientshapeok=\"t\" o:connecttype=\"rect\"/>\r\n </v:shapetype>";

    private static string Header(int drawing) =>
        "<xml xmlns:v=\"urn:schemas-microsoft-com:vml\"\r\n xmlns:o=\"urn:schemas-microsoft-com:office:office\"\r\n xmlns:x=\"urn:schemas-microsoft-com:office:excel\">\r\n"
        + $" <o:shapelayout v:ext=\"edit\">\r\n  <o:idmap v:ext=\"edit\" data=\"{drawing}\"/>\r\n </o:shapelayout>" + ShapeType + "</xml>";

    private static string NewShape(CellRef cell, int id)
    {
        var (r, c) = (cell.Row, cell.Column);
        var top = Math.Max(0, r - 1);
        return FormattableString.Invariant(
            $"<v:shape id=\"_x0000_s{id}\" type=\"#_x0000_t202\" style='position:absolute;\r\n  margin-left:59.25pt;margin-top:1.5pt;width:108pt;height:59.25pt;z-index:{id % 1024};\r\n")
               + "  visibility:hidden' fillcolor=\"#ffffe1\" o:insetmode=\"auto\">\r\n  <v:fill color2=\"#ffffe1\"/>\r\n  <v:shadow on=\"t\" color=\"black\" obscured=\"t\"/>\r\n"
               + "  <v:path o:connecttype=\"none\"/>\r\n  <v:textbox style='mso-direction-alt:auto'>\r\n   <div style='text-align:left'></div>\r\n  </v:textbox>\r\n"
               + "  <x:ClientData ObjectType=\"Note\">\r\n   <x:MoveWithCells/>\r\n   <x:SizeWithCells/>\r\n"
               + FormattableString.Invariant($"   <x:Anchor>\r\n    {c + 1}, 15, {top}, 10, {c + 3}, 15, {top + 4}, 4</x:Anchor>\r\n   <x:AutoFill>False</x:AutoFill>\r\n")
               + FormattableString.Invariant($"   <x:Row>{r}</x:Row>\r\n   <x:Column>{c}</x:Column>\r\n  </x:ClientData>\r\n </v:shape>");
    }

    private static ZipArchiveEntry Entry(ZipArchive zip, XlsxSheetPart part, XlsxRelationship rel) =>
        rel == null || rel.External ? null : Find(zip, ResolveTarget(part.PartName, rel.Target));

    private static XElement SheetChild(XlsxSheetPart part, string name) => part.Children.FirstOrDefault(c => c.Name.LocalName == name);

    private static string Empty(string s) => string.IsNullOrEmpty(s) ? null : s;
}
