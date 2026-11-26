using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal static class SectionXml
{
    internal static Section Read(XElement sectPr, string id, string lastBlockId, IReadOnlyDictionary<string, string> headerIds) => new()
    {
        Id = id,
        Page = Page(sectPr),
        LastBlockId = lastBlockId,
        Start = Val(sectPr, "type") switch
        {
            "continuous" => SectionStart.Continuous,
            "evenPage" => SectionStart.EvenPage,
            "oddPage" => SectionStart.OddPage,
            "nextColumn" => SectionStart.NextColumn,
            _ => SectionStart.NextPage
        },
        TitlePage = OnOff(sectPr, "titlePg") == true,
        Columns = Math.Max(1, Int(sectPr.Element(W + "cols"), "num") ?? 1),
        Headers = References(sectPr, "headerReference", headerIds),
        Footers = References(sectPr, "footerReference", headerIds),
        Source = sectPr
    };

    private static readonly string[] Order =
    [
        "headerReference", "footerReference", "footnotePr", "endnotePr", "type", "pgSz", "pgMar", "paperSrc", "pgBorders", "lnNumType", "pgNumType", "cols",
        "formProt", "vAlign", "noEndnote", "titlePg", "textDirection", "bidi", "rtlGutter", "docGrid", "printerSettings", "sectPrChange"
    ];

    internal static XElement Write(Section section, IReadOnlyDictionary<string, string> headerIds)
    {
        var source = section.Source;
        var sectPr = source != null ? new XElement(source) : new XElement(W + "sectPr");
        var current = Read(sectPr, null, null, headerIds);
        if (source == null || current.Page != section.Page) SetPage(sectPr, section.Page, current.Page.Width != section.Page.Width || current.Page.Height != section.Page.Height);
        if (current.Start != section.Start)
            SetVal(sectPr, "type", section.Start switch
            {
                SectionStart.Continuous => "continuous",
                SectionStart.EvenPage => "evenPage",
                SectionStart.OddPage => "oddPage",
                SectionStart.NextColumn => "nextColumn",
                _ => null
            }, Order);
        if (current.TitlePage != section.TitlePage) SetOnOff(sectPr, "titlePg", section.TitlePage ? true : null, Order);
        if (!Same(current.Headers, section.Headers) || !Same(current.Footers, section.Footers))
        {
            sectPr.Elements().Where(e => e.Name == W + "headerReference" || e.Name == W + "footerReference").Remove();
            sectPr.AddFirst(References("headerReference", section.Headers).Concat(References("footerReference", section.Footers)));
        }

        return source != null && XNode.DeepEquals(sectPr, source) ? source : sectPr;
    }

    private static IEnumerable<XElement> References(string name, IReadOnlyDictionary<HeaderFooterType, string> references) =>
        references.OrderBy(r => r.Key).Select(r => new XElement(W + name,
            new XAttribute(W + "type", r.Key switch { HeaderFooterType.First => "first", HeaderFooterType.Even => "even", _ => "default" }),
            new XAttribute(InlineXml.R + "id", r.Value)));

    private static bool Same(IReadOnlyDictionary<HeaderFooterType, string> a, IReadOnlyDictionary<HeaderFooterType, string> b) =>
        a.Count == b.Count && a.All(r => b.TryGetValue(r.Key, out var id) && id == r.Value);

    private static void SetPage(XElement sectPr, PageSetup page, bool resized)
    {
        var size = Ensure(sectPr, "pgSz", Order);
        if (resized) size.SetAttributeValue(W + "code", null);
        size.SetAttributeValue(W + "w", Number(page.Width));
        size.SetAttributeValue(W + "h", Number(page.Height));
        size.SetAttributeValue(W + "orient", page.Orientation == PageOrientation.Landscape ? "landscape" : null);
        var margin = Ensure(sectPr, "pgMar", Order);
        foreach (var (name, value) in new[]
                 {
                     ("top", page.MarginTop), ("right", page.MarginRight), ("bottom", page.MarginBottom), ("left", page.MarginLeft),
                     ("header", page.HeaderDistance), ("footer", page.FooterDistance), ("gutter", page.Gutter)
                 })
            margin.SetAttributeValue(W + name, Number(value));
    }

    private static Dictionary<HeaderFooterType, string> References(XElement sectPr, string name, IReadOnlyDictionary<string, string> headerIds)
    {
        var result = new Dictionary<HeaderFooterType, string>();
        foreach (var reference in sectPr.Elements(W + name))
            if ((string)reference.Attribute(InlineXml.R + "id") is { } id)
                result[(string)reference.Attribute(W + "type") switch { "first" => HeaderFooterType.First, "even" => HeaderFooterType.Even, _ => HeaderFooterType.Default }] = headerIds.GetValueOrDefault(id, id);
        return result;
    }

    private static PageSetup Page(XElement sectPr)
    {
        var size = sectPr.Element(W + "pgSz");
        var margin = sectPr.Element(W + "pgMar");
        var a4 = PageSetup.A4;
        return new PageSetup(
            Int(size, "w") ?? a4.Width, Int(size, "h") ?? a4.Height,
            (string)size?.Attribute(W + "orient") == "landscape" ? PageOrientation.Landscape : PageOrientation.Portrait,
            Int(margin, "top") ?? a4.MarginTop, Int(margin, "right") ?? a4.MarginRight, Int(margin, "bottom") ?? a4.MarginBottom, Int(margin, "left") ?? a4.MarginLeft,
            Int(margin, "header") ?? a4.HeaderDistance, Int(margin, "footer") ?? a4.FooterDistance, Int(margin, "gutter") ?? a4.Gutter);
    }
}
