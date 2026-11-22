using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal static class StylesXml
{
    internal static DocumentStyles Read(XDocument styles)
    {
        var root = styles?.Root;
        var defaults = root?.Element(W + "docDefaults");
        return new DocumentStyles(
            root?.Elements(W + "style").Select(Style) ?? [],
            Run(defaults?.Element(W + "rPrDefault")?.Element(W + "rPr")),
            FormatXml.Paragraph(defaults?.Element(W + "pPrDefault")?.Element(W + "pPr")));
    }

    private static DocumentStyle Style(XElement style)
    {
        var conditions = new Dictionary<string, (ParagraphFormat, RunFormat)>();
        foreach (var condition in style.Elements(W + "tblStylePr"))
            if ((string)condition.Attribute(W + "type") is { } type)
                conditions[type] = (FormatXml.Paragraph(condition.Element(W + "pPr")) ?? new ParagraphFormat(), Run(condition.Element(W + "rPr")) ?? new RunFormat());
        return new DocumentStyle
        {
            Id = (string)style.Attribute(W + "styleId"),
            Name = Val(style, "name"),
            Type = (string)style.Attribute(W + "type") switch
            {
                "character" => StyleType.Character,
                "table" => StyleType.Table,
                "numbering" => StyleType.Numbering,
                _ => StyleType.Paragraph
            },
            BasedOn = Val(style, "basedOn"),
            Next = Val(style, "next"),
            Link = Val(style, "link"),
            QuickFormat = OnOff(style, "qFormat") == true,
            IsDefault = (string)style.Attribute(W + "default") is "1" or "true" or "on",
            UiPriority = Int(style.Element(W + "uiPriority"), "val"),
            RunFormat = Run(style.Element(W + "rPr")) ?? new RunFormat(),
            ParagraphFormat = FormatXml.Paragraph(style.Element(W + "pPr")) ?? new ParagraphFormat(),
            TableFormat = FormatXml.Table(style.Element(W + "tblPr")) ?? new TableFormat(),
            Conditions = conditions,
            Source = style
        };
    }
}
