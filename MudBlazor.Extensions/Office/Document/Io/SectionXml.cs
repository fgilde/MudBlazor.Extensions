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
