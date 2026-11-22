using System.Globalization;
using System.Text;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal sealed class InlineXml
{
    internal static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    internal static readonly XNamespace M = "http://schemas.openxmlformats.org/officeDocument/2006/math";
    internal static readonly XNamespace Mc = "http://schemas.openxmlformats.org/markup-compatibility/2006";
    internal static readonly XNamespace Wp = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    internal static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace Pic = "http://schemas.openxmlformats.org/drawingml/2006/picture";
    private static readonly XNamespace C = "http://schemas.openxmlformats.org/drawingml/2006/chart";
    private static readonly XNamespace Dgm = "http://schemas.openxmlformats.org/drawingml/2006/diagram";
    private static readonly XNamespace Wps = "http://schemas.microsoft.com/office/word/2010/wordprocessingShape";

    internal static readonly HashSet<string> Markers =
    [
        "bookmarkStart", "bookmarkEnd", "proofErr", "commentRangeStart", "commentRangeEnd", "permStart", "permEnd",
        "moveFromRangeStart", "moveFromRangeEnd", "moveToRangeStart", "moveToRangeEnd"
    ];

    private readonly OpcPackage _opc;
    private readonly string _part;
    private readonly Dictionary<string, OpcRelationship> _rels = new();
    private readonly TextDocument _document;
    private int _footnotes;
    private int _endnotes;
    private int _openFields;
    private bool _awaitingSeparate;

    internal InlineXml(OpcPackage opc, string part, IEnumerable<OpcRelationship> rels, TextDocument document)
    {
        _opc = opc;
        _part = part;
        _document = document;
        foreach (var rel in rels) _rels.TryAdd(rel.Id, rel);
    }

    internal List<Inline> Read(XElement paragraph)
    {
        var items = new List<Item>();
        Flatten(paragraph, null, items);
        var result = new List<Inline>();
        var i = 0;
        if (_awaitingSeparate && items.Count > 0)
        {
            var end = Match(items, 0, out var separate);
            var closed = separate < 0 && end >= 0;
            var stop = separate >= 0 ? separate : closed ? end : items.Count - 1;
            _awaitingSeparate = separate < 0 && !closed;
            if (closed) _openFields--;
            result.Add(Field(closed ? "fieldEnd" : "other", items, 0, stop, 1) with { Instruction = null, DisplayText = "" });
            i = stop + 1;
        }

        for (; i < items.Count; i++)
        {
            var item = items[i];
            if (item.IsFieldChar("begin"))
            {
                var end = Match(items, i + 1, out var separate);
                if (end >= 0)
                {
                    result.Add(Field("field", items, i, end, 0));
                    i = end;
                    continue;
                }

                var last = separate >= 0 ? separate : items.Count - 1;
                result.Add(Field("fieldStart", items, i, last, 0));
                _openFields++;
                _awaitingSeparate = separate < 0;
                i = last;
            }
            else if (item.IsFieldChar("end") && _openFields > 0)
            {
                _openFields--;
                result.Add(Field("fieldEnd", items, i, i, 1) with { Instruction = null });
            }
            else if (Map(items, ref i) is { } inline)
                result.Add(inline);
        }

        return result;
    }

    private void Flatten(XElement container, Hyperlink link, List<Item> items)
    {
        foreach (var child in container.Elements())
        {
            if (child.Name.Namespace == W)
                switch (child.Name.LocalName)
                {
                    case "pPr" or "del" or "moveFrom":
                        continue;
                    case "r":
                        var format = Run(child.Element(W + "rPr")) ?? new RunFormat();
                        foreach (var runChild in child.Elements())
                            if (runChild.Name != W + "rPr")
                                items.Add(new Item(runChild, child, format, link));
                        continue;
                    case "hyperlink":
                        Flatten(child, Link(child), items);
                        continue;
                    case "ins" or "moveTo" or "dir" or "bdo":
                        Flatten(child, link, items);
                        continue;
                }

            items.Add(new Item(child, null, null, link));
        }
    }

    private Hyperlink Link(XElement hyperlink) =>
        new((string)hyperlink.Attribute(R + "id") is { } id && _rels.TryGetValue(id, out var rel) ? rel.Target : null,
            (string)hyperlink.Attribute(W + "anchor"), (string)hyperlink.Attribute(W + "tooltip")) { Source = hyperlink };

    private static int Match(List<Item> items, int from, out int separate)
    {
        separate = -1;
        var depth = 1;
        for (var j = from; j < items.Count; j++)
            if (items[j].IsFieldChar("begin")) depth++;
            else if (items[j].IsFieldChar("separate") && depth == 1 && separate < 0) separate = j;
            else if (items[j].IsFieldChar("end") && --depth == 0) return j;
        return -1;
    }

    private static OpaqueInline Field(string kind, List<Item> items, int from, int to, int depth)
    {
        var instruction = new StringBuilder();
        var text = new StringBuilder();
        var separated = false;
        RunFormat format = null;
        for (var j = from; j <= to; j++)
        {
            var item = items[j];
            if (item.IsFieldChar("begin")) depth++;
            else if (item.IsFieldChar("end")) depth--;
            else if (item.IsFieldChar("separate") && depth == 1) separated = true;
            else if (item.Run != null && item.Node.Name == W + "instrText" && depth == 1 && !separated) instruction.Append(item.Node.Value);
            else if (item.Run != null && item.Node.Name == W + "t" && separated)
            {
                text.Append(item.Node.Value);
                format ??= item.Format;
            }
        }

        var first = items[from];
        return new OpaqueInline(kind, text.ToString())
        {
            Instruction = instruction.ToString().Trim(),
            Sources = items.Skip(from).Take(to - from + 1).Select(i => i.Run ?? i.Node).Distinct().ToList(),
            Source = first.Run ?? first.Node,
            Format = format ?? first.Format ?? new RunFormat(),
            Link = first.Link
        };
    }

    private Inline Map(List<Item> items, ref int index)
    {
        var item = items[index];
        var node = item.Node;
        var inline = item.Run != null ? RunChild(node) : Element(node);
        if (inline == null) return null;
        if (inline is OpaqueInline { Kind: "footnote" or "endnote" } note && IsOn(node.Attribute(W + "customMarkFollows")))
        {
            var mark = index + 1 < items.Count && items[index + 1] is { Run: not null } next && next.Node.Name == W + "t" ? items[++index] : (Item?)null;
            inline = note with { DisplayText = mark?.Node.Value ?? "", Sources = new[] { item.Run, mark?.Run }.Where(r => r != null).Distinct().ToList() };
        }
        else if (inline is OpaqueInline { Kind: "footnote" })
            inline = ((OpaqueInline)inline) with { DisplayText = (++_footnotes).ToString(CultureInfo.InvariantCulture) };
        else if (inline is OpaqueInline { Kind: "endnote" })
            inline = ((OpaqueInline)inline) with { DisplayText = NumberingLabels.Roman(++_endnotes).ToLowerInvariant() };

        return inline with { Source = item.Run ?? node, Format = item.Format ?? inline.Format, Link = item.Link };
    }

    private Inline RunChild(XElement node)
    {
        if (node.Name == Mc + "AlternateContent")
            return (node.Elements(Mc + "Choice").Select(c => c.Elements().FirstOrDefault()).FirstOrDefault(e => e != null) ??
                    node.Element(Mc + "Fallback")?.Elements().FirstOrDefault()) is { } choice
                ? RunChild(choice)
                : Opaque("other");
        if (node.Name.Namespace != W) return Opaque("other");
        return node.Name.LocalName switch
        {
            "t" => new TextRun(node.Value),
            "tab" => new Tab(),
            "br" => new Break((string)node.Attribute(W + "type") switch { "page" => BreakKind.Page, "column" => BreakKind.Column, _ => BreakKind.Line }),
            "cr" => new Break(BreakKind.Line),
            "noBreakHyphen" => new TextRun("\u2011"),
            "softHyphen" => new TextRun("\u00AD"),
            "lastRenderedPageBreak" or "delText" or "delInstrText" => null,
            "commentReference" => new Marker("commentReference"),
            "footnoteReference" => Opaque("footnote"),
            "endnoteReference" => Opaque("endnote"),
            "sym" => Opaque("symbol",
                int.TryParse((string)node.Attribute(W + "char"), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code) && code is > 0 and <= 0xFFFF ? ((char)code).ToString() : ""),
            "pict" => Opaque("vml"),
            "object" => Opaque("ole"),
            "drawing" => Drawing(node),
            "ruby" => Opaque("ruby", node.Element(W + "rubyBase") is { } rubyBase ? Text(rubyBase) : ""),
            _ => Opaque("other")
        };
    }

    private static Inline Element(XElement node)
    {
        if (node.Name == M + "oMath" || node.Name == M + "oMathPara") return Opaque("equation", Text(node, M + "t"));
        if (node.Name.Namespace != W) return Opaque("other");
        var name = node.Name.LocalName;
        if (Markers.Contains(name)) return new Marker(name) { Name = (string)node.Attribute(W + "name") };
        return name switch
        {
            "fldSimple" => Opaque("field", Text(node)) with { Instruction = ((string)node.Attribute(W + "instr"))?.Trim(), Sources = [node] },
            "sdt" => Opaque("contentControl", Text(node)),
            "smartTag" => Opaque("smartTag", Text(node)),
            "customXml" => Opaque("customXml", Text(node)),
            _ => Opaque("other")
        };
    }

    private Inline Drawing(XElement drawing)
    {
        var frame = drawing.Element(Wp + "inline") ?? drawing.Element(Wp + "anchor");
        var data = frame?.Element(A + "graphic")?.Element(A + "graphicData");
        if (data?.Element(Pic + "pic") is { } picture && MediaPart(picture) is { } media)
        {
            var extent = frame.Element(Wp + "extent");
            return new InlineImage(media, Long(extent, "cx"), Long(extent, "cy"))
            {
                AltText = (string)frame.Element(Wp + "docPr")?.Attribute("descr"),
                Placement = frame.Name == Wp + "inline"
                    ? ImagePlacement.Inline
                    : frame.Element(Wp + "positionH")?.Element(Wp + "align")?.Value switch { "right" => ImagePlacement.Right, "center" => ImagePlacement.Center, _ => ImagePlacement.Left }
            };
        }

        return Opaque(data?.Element(C + "chart") != null ? "chart" : data?.Element(Dgm + "relIds") != null ? "diagram" : data?.Element(Wps + "wsp") != null ? "shape" : "drawing");
    }

    private string MediaPart(XElement picture)
    {
        if ((string)picture.Descendants(A + "blip").FirstOrDefault()?.Attribute(R + "embed") is not { } id || !_rels.TryGetValue(id, out var rel) || DocxReader.IsExternal(rel)) return null;
        var part = _opc.ResolveTarget(_part, rel.Target);
        if (_opc.Entry(part) == null) return null;
        if (!_document.Media.ContainsKey(part)) _document.Media[part] = (_opc.ReadBytes(part), _opc.ContentType(part));
        return part;
    }

    private static OpaqueInline Opaque(string kind, string text = "") => new(kind, text);

    internal static string Text(XElement node) => Text(node, W + "t");

    private static string Text(XElement node, XName text)
    {
        var result = new StringBuilder();
        foreach (var child in node.Elements())
            if (child.Name == text) result.Append(child.Value);
            else if (child.Name != W + "del" && child.Name != W + "moveFrom") result.Append(Text(child, text));
        return result.ToString();
    }

    private static long Long(XElement element, string attribute) =>
        long.TryParse((string)element?.Attribute(attribute), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private static bool IsOn(XAttribute attribute) => (string)attribute is "1" or "true" or "on";

    private readonly record struct Item(XElement Node, XElement Run, RunFormat Format, Hyperlink Link)
    {
        public bool IsFieldChar(string type) => Run != null && Node.Name == W + "fldChar" && (string)Node.Attribute(W + "fldCharType") == type;
    }
}
