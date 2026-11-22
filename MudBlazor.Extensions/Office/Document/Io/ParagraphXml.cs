using System.Text;
using System.Xml;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal static class ParagraphXml
{
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
        var sectPr = topLevel ? context.SectionEnd(paragraph.Id) : null;
        if (context.IsOriginal(paragraph))
        {
            if (!topLevel || XNode.DeepEquals(source.Element(W + "pPr")?.Element(W + "sectPr"), sectPr)) return source;
            var copy = new XElement(source);
            var properties = copy.Element(W + "pPr");
            if (properties == null) copy.AddFirst(properties = new XElement(W + "pPr"));
            SetSectionEnd(properties, sectPr);
            if (!properties.HasElements && !properties.HasAttributes) properties.Remove();
            return copy;
        }

        var element = new XElement(W + "p", source?.Attributes());
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
        var items = inlines.Where(i => i is not Marker { Kind: "proofErr" }).ToList();
        for (var i = 0; i < items.Count;)
        {
            var link = items[i].Link;
            var j = i + 1;
            while (j < items.Count && ReferenceEquals(items[j].Link, link)) j++;
            var content = Wrap(Runs(items.GetRange(i, j - i), context).Select(r => (r.Element, Wrappers(r.Origin))).ToList(), 0);
            if (link == null)
                foreach (var element in content) yield return element;
            else
                yield return context.Hyperlink(link, content);
            i = j;
        }
    }

    private static IEnumerable<(XElement Element, XElement Origin)> Runs(List<Inline> inlines, DocxWriteContext context)
    {
        for (var i = 0; i < inlines.Count;)
        {
            var key = DocxWriteContext.Key(inlines[i]);
            var j = i + 1;
            while (key != null && j < inlines.Count && DocxWriteContext.Key(inlines[j]) == key) j++;
            var group = inlines.GetRange(i, j - i);
            if (key != null && context.IsOriginalGroup(key, group))
                foreach (var source in group.SelectMany(DocxWriteContext.Sources).Distinct())
                    yield return (Final(source), source);
            else
                foreach (var inline in group)
                foreach (var item in Inline(inline))
                    yield return item;
            i = j;
        }
    }

    private static IEnumerable<(XElement Element, XElement Origin)> Inline(Inline inline)
    {
        if (inline is OpaqueInline { Sources: not null } opaque)
        {
            foreach (var source in opaque.Sources) yield return (Final(source), source);
            yield break;
        }

        var run = inline.Source;
        if (run != null && run.Name != W + "r")
        {
            yield return (run, run);
            yield break;
        }

        var content = inline switch
        {
            TextRun text => Text(text.Text),
            _ when inline.Node != null => inline.Node,
            Tab => new XElement(W + "tab"),
            Break { Kind: BreakKind.Line } => new XElement(W + "br"),
            Break other => new XElement(W + "br", new XAttribute(W + "type", other.Kind == BreakKind.Page ? "page" : "column")),
            OpaqueInline token => Text(token.DisplayText),
            _ => null
        };
        if (content != null) yield return (new XElement(W + "r", run?.Attributes(), RunProperties(run?.Element(W + "rPr"), inline.Format), content), run);
    }

    private static XElement RunProperties(XElement original, RunFormat format)
    {
        var rPr = original != null ? new XElement(original) : new XElement(W + "rPr");
        rPr.Element(W + "rPrChange")?.Remove();
        PatchRun(rPr, format ?? new RunFormat());
        return rPr.HasElements || rPr.HasAttributes ? rPr : null;
    }

    private static XElement Final(XElement source)
    {
        if (source.Name != W + "r" || source.Element(W + "rPr")?.Element(W + "rPrChange") == null) return source;
        var copy = new XElement(source);
        copy.Element(W + "rPr")!.Element(W + "rPrChange")!.Remove();
        return copy;
    }

    private static XElement Text(string text)
    {
        text ??= "";
        var valid = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
            if (XmlConvert.IsXmlChar(text[i])) valid.Append(text[i]);
            else if (i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], text[i])) valid.Append(text, i++, 2);
        if (valid.Length == 0) return null;
        var value = valid.ToString();
        var preserve = value != value.Trim() || value.Contains("  ", StringComparison.Ordinal);
        return new XElement(W + "t", preserve ? new XAttribute(XNamespace.Xml + "space", "preserve") : null, value);
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
