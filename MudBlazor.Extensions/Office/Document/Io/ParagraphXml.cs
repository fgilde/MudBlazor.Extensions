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
}
