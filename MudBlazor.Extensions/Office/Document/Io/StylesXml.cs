using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Document.FormatXml;

namespace MudBlazor.Extensions.Office.Document;

internal static class StylesXml
{
    private static readonly string[] StyleOrder =
    [
        "name", "aliases", "basedOn", "next", "link", "autoRedefine", "hidden", "uiPriority", "semiHidden", "unhideWhenUsed", "qFormat", "locked", "personal",
        "personalCompose", "personalReply", "rsid", "pPr", "rPr", "tblPr", "trPr", "tcPr", "tblStylePr"
    ];

    private static readonly string[] DefaultsOrder = ["rPrDefault", "pPrDefault"];

    internal static XDocument Write(XDocument original, DocumentStyles read, DocumentStyles styles)
    {
        var xml = original != null ? new XDocument(original) : new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement(W + "styles", new XAttribute(XNamespace.Xmlns + "w", W)));
        var root = xml.Root!;
        var copies = original == null ? new Dictionary<XElement, XElement>() : original.Root!.Elements().Zip(root.Elements()).ToDictionary(p => p.First, p => p.Second);
        var unchanged = read.All.ToHashSet(ReferenceEqualityComparer.Instance);
        var current = new Dictionary<XElement, DocumentStyle>();
        foreach (var style in styles.All.Where(s => s.Source != null)) current.TryAdd(style.Source, style);
        foreach (var style in read.All.Where(s => s.Source != null && copies.ContainsKey(s.Source)))
            if (!current.TryGetValue(style.Source, out var now)) copies[style.Source].Remove();
            else if (!unchanged.Contains(now)) Patch(copies[style.Source], now);
        root.Add(styles.All.Where(s => s.Source == null).Select(New));

        if (styles.DefaultRunFormat != read.DefaultRunFormat) PatchRun(Default(root, "rPrDefault", "rPr"), styles.DefaultRunFormat);
        if (styles.DefaultParagraphFormat != read.DefaultParagraphFormat) PatchParagraph(Default(root, "pPrDefault", "pPr"), styles.DefaultParagraphFormat);
        return xml;
    }

    private static XElement Default(XElement root, string name, string properties)
    {
        var defaults = root.Element(W + "docDefaults");
        if (defaults == null) root.AddFirst(defaults = new XElement(W + "docDefaults"));
        return Ensure(Ensure(defaults, name, DefaultsOrder), properties, [properties]);
    }

    private static void Patch(XElement element, DocumentStyle style)
    {
        Patch(element, "pPr", properties => PatchParagraph(properties, style.ParagraphFormat));
        Patch(element, "rPr", properties => PatchRun(properties, style.RunFormat));
    }

    private static void Patch(XElement parent, string name, Action<XElement> patch)
    {
        var existing = parent.Element(W + name);
        var properties = existing ?? new XElement(W + name);
        patch(properties);
        if (existing == null && (properties.HasElements || properties.HasAttributes)) Place(parent, properties, StyleOrder);
    }

    private static XElement New(DocumentStyle style)
    {
        var element = new XElement(W + "style",
            new XAttribute(W + "type", style.Type.ToString().ToLowerInvariant()),
            style.IsDefault ? new XAttribute(W + "default", "1") : null,
            new XAttribute(W + "styleId", style.Id),
            Value("name", style.Name ?? style.Id), Value("basedOn", style.BasedOn), Value("next", style.Next), Value("link", style.Link),
            Value("uiPriority", Optional(style.UiPriority)),
            style.QuickFormat ? new XElement(W + "qFormat") : null);
        Patch(element, style);
        if (style.Type == StyleType.Table && TableXml.Properties(null, style.TableFormat) is { HasElements: true } tblPr) element.Add(tblPr);
        return element;
    }

    private static XElement Value(string name, string value) => value == null ? null : new XElement(W + name, new XAttribute(W + "val", value));

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
