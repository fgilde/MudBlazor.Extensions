using System.Text;
using System.Xml;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal static class ParagraphXml
{
    private const char ObjectReplacement = (char)0xFFFC;

    internal static Block Read(XElement paragraph, InlineXml inlines)
    {
        var properties = paragraph.Element(W + "pPr");
        var mark = properties?.Element(W + "rPr");
        var content = inlines.Read(paragraph);
        if ((mark?.Element(W + "del") ?? mark?.Element(W + "moveFrom")) != null && content.All(i => i is Marker))
            return new OpaqueBlock { Kind = "trackedDeletion", Visible = false, Source = paragraph };
        return new Paragraph { Format = FormatXml.Paragraph(properties) ?? new ParagraphFormat(), MarkFormat = Run(mark), Inlines = content, Source = paragraph };
    }

    internal static XElement Write(Paragraph paragraph, DocxWriteContext context, bool topLevel)
    {
        var source = paragraph.Source;
        var first = source == null || context.Claim(source);
        var sectPr = topLevel ? context.SectionEnd(paragraph.Id) : null;
        if (context.IsOriginal(paragraph))
        {
            if (first && (!topLevel || XNode.DeepEquals(source.Element(W + "pPr")?.Element(W + "sectPr"), sectPr))) return source;
            var copy = new XElement(source.Name, DocxWriteContext.Attributes(source, first), source.Nodes());
            if (!topLevel) return copy;
            var properties = copy.Element(W + "pPr");
            if (properties == null) copy.AddFirst(properties = new XElement(W + "pPr"));
            SetSectionEnd(properties, sectPr);
            if (!properties.HasElements && !properties.HasAttributes) properties.Remove();
            return copy;
        }

        var element = new XElement(W + "p", source == null ? null : DocxWriteContext.Attributes(source, first));
        var pPr = source?.Element(W + "pPr") is { } original ? new XElement(original) : new XElement(W + "pPr");
        pPr.Element(W + "pPrChange")?.Remove();
        pPr.Element(W + "rPr")?.Elements().Where(e => e.Name.Namespace == W && e.Name.LocalName is "ins" or "del" or "moveFrom" or "moveTo" or "rPrChange").Remove();
        PatchParagraph(pPr, paragraph.Format);
        if (paragraph.MarkFormat != null) PatchRun(Ensure(pPr, "rPr", ParagraphOrder), paragraph.MarkFormat);
        if (pPr.Element(W + "rPr") is { HasElements: false, HasAttributes: false } empty) empty.Remove();
        SetSectionEnd(pPr, sectPr);
        if (pPr.HasElements || pPr.HasAttributes) element.Add(pPr);
        element.Add(Content(paragraph.Inlines, context));
        return element;
    }

    private static void SetSectionEnd(XElement pPr, XElement sectPr)
    {
        pPr.Elements(W + "sectPr").Remove();
        if (sectPr != null) Place(pPr, sectPr, ParagraphOrder);
    }

    private static IEnumerable<XElement> Content(IReadOnlyList<Inline> inlines, DocxWriteContext context)
    {
        var items = inlines.Where(i => i is not Marker
        {
            Kind: "proofErr" or "moveFromRangeStart" or "moveFromRangeEnd" or "moveToRangeStart" or "moveToRangeEnd"
        }).ToList();
        var owners = new Dictionary<XElement, List<Inline>>();
        foreach (var inline in items)
        foreach (var owner in DocxWriteContext.Parts(inline).Select(p => p.Owner).Distinct())
        {
            if (!owners.TryGetValue(owner, out var list)) owners[owner] = list = [];
            list.Add(inline);
        }

        var written = new HashSet<XElement>();
        for (var i = 0; i < items.Count;)
        {
            var link = items[i].Link;
            var j = i + 1;
            while (j < items.Count && ReferenceEquals(items[j].Link, link)) j++;
            var pieces = items.GetRange(i, j - i).SelectMany(inline => Pieces(inline, owners, written, context));
            var content = Wrap(pieces.Select(p => (p.Element, Wrappers(p.Origin))).ToList(), 0);
            if (link == null)
                foreach (var element in content) yield return element;
            else
                yield return context.Hyperlink(link, content);
            i = j;
        }
    }

    private static IEnumerable<(XElement Element, XElement Origin)> Pieces(Inline inline, Dictionary<XElement, List<Inline>> owners, HashSet<XElement> written,
        DocxWriteContext context)
    {
        var parts = DocxWriteContext.Parts(inline).ToList();
        if (parts.Count == 0)
        {
            if (Content(inline, null) is { Count: > 0 } content) yield return (new XElement(W + "r", RunProperties(null, inline.Format), content), null);
            yield break;
        }

        var change = context.FormatChange(inline);
        foreach (var (owner, node) in parts)
        {
            if (context.IsIntact(owner, owners[owner]))
            {
                if (written.Add(owner)) yield return (Final(owner, context.Claim(owner)), owner);
            }
            else if (owner.Name != W + "r")
            {
                if (written.Add(owner)) yield return (Patched(owner, change, context.Claim(owner)), owner);
            }
            else if (Content(inline, node) is { Count: > 0 } content)
            {
                var first = context.Claim(owner);
                var rPr = change is { } c ? Delta(owner.Element(W + "rPr"), c.Old, c.New) : RunProperties(owner.Element(W + "rPr"), inline.Format);
                yield return (new XElement(W + "r", DocxWriteContext.Attributes(owner, first), rPr, content), owner);
            }
        }
    }

    private static List<XElement> Content(Inline inline, XElement node) => inline switch
    {
        TextRun text => Text(text.Text),
        _ when node != null => [node],
        Tab => [new XElement(W + "tab")],
        Break { Kind: BreakKind.Line } => [new XElement(W + "br")],
        Break other => [new XElement(W + "br", new XAttribute(W + "type", other.Kind == BreakKind.Page ? "page" : "column"))],
        OpaqueInline token => Text(token.DisplayText),
        _ => []
    };

    private static XElement RunProperties(XElement original, RunFormat format)
    {
        var rPr = original != null ? new XElement(original) : new XElement(W + "rPr");
        rPr.Element(W + "rPrChange")?.Remove();
        PatchRun(rPr, format ?? new RunFormat());
        return rPr.HasElements || rPr.HasAttributes ? rPr : null;
    }

    private static XElement Delta(XElement original, RunFormat old, RunFormat format)
    {
        var rPr = original != null ? new XElement(original) : new XElement(W + "rPr");
        rPr.Element(W + "rPrChange")?.Remove();
        PatchRun(rPr, old, format);
        return rPr.HasElements || rPr.HasAttributes ? rPr : null;
    }

    private static XElement Patched(XElement element, (RunFormat Old, RunFormat New)? change, bool first)
    {
        if (change is not { } c) return Final(element, first);
        var copy = new XElement(element.Name, DocxWriteContext.Attributes(element, first), element.Nodes());
        foreach (var run in copy.DescendantsAndSelf().Where(e => e.Name == W + "r" || e.Name == InlineXml.M + "r").ToList())
        {
            if (Delta(run.Element(W + "rPr"), c.Old, c.New) is not { } rPr)
            {
                run.Element(W + "rPr")?.Remove();
                continue;
            }

            if (run.Element(W + "rPr") is { } existing) existing.ReplaceWith(rPr);
            else if (run.Element(InlineXml.M + "rPr") is { } math) math.AddAfterSelf(rPr);
            else run.AddFirst(rPr);
        }

        return copy;
    }

    private static XElement Final(XElement source, bool first)
    {
        var tracked = source.Name == W + "r" && source.Element(W + "rPr")?.Element(W + "rPrChange") != null;
        if (first && !tracked) return source;
        var copy = new XElement(source.Name, DocxWriteContext.Attributes(source, first), source.Nodes());
        copy.Element(W + "rPr")?.Element(W + "rPrChange")?.Remove();
        return copy;
    }

    private static List<XElement> Text(string text)
    {
        var result = new List<XElement>();
        var run = new StringBuilder();
        text ??= "";
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is '\t' or '\n' or '\r' or '\v' or '\f')
            {
                Flush();
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                result.Add(c switch
                {
                    '\t' => new XElement(W + "tab"),
                    '\f' => new XElement(W + "br", new XAttribute(W + "type", "page")),
                    _ => new XElement(W + "br")
                });
            }
            else if (c != ObjectReplacement && XmlConvert.IsXmlChar(c)) run.Append(c);
            else if (i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], c)) run.Append(text, i++, 2);
        }

        Flush();
        return result;

        void Flush()
        {
            if (run.Length == 0) return;
            var value = run.ToString();
            var preserve = value != value.Trim() || value.Contains("  ", StringComparison.Ordinal);
            result.Add(new XElement(W + "t", preserve ? new XAttribute(XNamespace.Xml + "space", "preserve") : null, value));
            run.Clear();
        }
    }

    private static XElement[] Wrappers(XElement origin) =>
        origin?.Ancestors().TakeWhile(a => a.Name != W + "p").Where(a => a.Name == W + "dir" || a.Name == W + "bdo").Reverse().ToArray() ?? [];

    private static IEnumerable<XElement> Wrap(List<(XElement Element, XElement[] Path)> items, int depth)
    {
        for (var i = 0; i < items.Count;)
        {
            var wrapper = At(items[i].Path, depth);
            var j = i + 1;
            while (j < items.Count && At(items[j].Path, depth) == wrapper) j++;
            if (wrapper == null)
                for (var k = i; k < j; k++) yield return items[k].Element;
            else
                yield return new XElement(wrapper.Name, wrapper.Attributes(), Wrap(items.GetRange(i, j - i), depth + 1));
            i = j;
        }
    }

    private static XElement At(XElement[] path, int depth) => depth < path.Length ? path[depth] : null;
}
