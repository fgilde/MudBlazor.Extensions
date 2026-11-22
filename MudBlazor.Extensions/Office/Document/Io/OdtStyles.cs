using System.Globalization;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

internal sealed class OdtStyles
{
    internal static readonly XNamespace OfficeNs = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
    internal static readonly XNamespace StyleNs = "urn:oasis:names:tc:opendocument:xmlns:style:1.0";
    internal static readonly XNamespace TextNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
    internal static readonly XNamespace TableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
    internal static readonly XNamespace DrawNs = "urn:oasis:names:tc:opendocument:xmlns:drawing:1.0";
    internal static readonly XNamespace FoNs = "urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0";
    internal static readonly XNamespace SvgNs = "urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0";
    internal static readonly XNamespace XLinkNs = "http://www.w3.org/1999/xlink";
    internal static readonly XNamespace DcNs = "http://purl.org/dc/elements/1.1/";

    private const int MaxChain = 32;
    private const double MaxTwips = 1_000_000;
    private static readonly IReadOnlyDictionary<XName, string> Empty = new Dictionary<XName, string>();

    private static readonly Dictionary<string, string> Underlines = new()
    {
        ["solid"] = "single", ["dotted"] = "dotted", ["dash"] = "dash", ["long-dash"] = "dashLong", ["dot-dash"] = "dotDash", ["dot-dot-dash"] = "dotDotDash",
        ["wave"] = "wave"
    };

    private readonly Dictionary<(string Family, string Name), XElement> _named = new();
    private readonly Dictionary<(string Family, string Name), XElement> _body = new();
    private readonly Dictionary<(string Family, string Name), XElement> _master = new();
    private readonly Dictionary<string, XElement> _defaults = new();
    private readonly Dictionary<string, string> _fonts = new();
    private readonly Dictionary<(bool, string), OdtParagraphStyle> _paragraphs = new();
    private readonly Dictionary<IReadOnlyDictionary<XName, string>, RunFormat> _runs = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<(bool, string, IReadOnlyDictionary<XName, string>), IReadOnlyDictionary<XName, string>> _spans = new();

    internal OdtStyles(XDocument content, XDocument styles)
    {
        Add(styles?.Root?.Element(OfficeNs + "styles"), _named);
        Add(content.Root?.Element(OfficeNs + "automatic-styles"), _body);
        Add(styles?.Root?.Element(OfficeNs + "automatic-styles"), _master);
        foreach (var face in new[] { styles, content }.SelectMany(d => d?.Root?.Element(OfficeNs + "font-face-decls")?.Elements(StyleNs + "font-face") ?? []))
            if (Attr(face, StyleNs + "name") is { } name && Attr(face, SvgNs + "font-family") is { } family)
                _fonts[name] = FirstFamily(family);
    }

    internal static string Attr(XElement element, XName name) => (string)element?.Attribute(name);

    internal static int? Int(XElement element, XName name) =>
        int.TryParse(Attr(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    internal static double? Twips(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        var split = value.Length;
        while (split > 0 && char.IsAsciiLetter(value[split - 1])) split--;
        if (!double.TryParse(value.AsSpan(0, split), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) return null;
        var twips = number * value[split..].ToLowerInvariant() switch
        {
            "cm" => 1440 / 2.54,
            "mm" => 144 / 2.54,
            "in" or "inch" => 1440,
            "pt" => 20,
            "pc" => 240,
            "px" => 15,
            _ => double.NaN
        };
        return double.IsFinite(twips) && Math.Abs(twips) <= MaxTwips ? twips : null;
    }

    internal static int? RoundTwips(string value) => Twips(value) is { } twips ? (int)Math.Round(twips) : null;

    internal static string Color(string value) =>
        value is ['#', _, _, _, _, _, _] && int.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _) ? value.ToUpperInvariant() : null;

    internal XElement Find(bool header, string family, string name) =>
        name == null ? null : (header ? _master : _body).GetValueOrDefault((family, name)) ?? _named.GetValueOrDefault((family, name));

    internal XElement ListStyle(bool header, string name) => Find(header, "list", name);

    internal XElement PageLayout(string name) => Find(true, "page-layout", name);

    internal OdtParagraphStyle ParagraphStyle(bool header, string name)
    {
        if (_paragraphs.TryGetValue((header, name ?? ""), out var cached)) return cached;
        var chain = Chain(header, "paragraph", name);
        var defaults = _defaults.GetValueOrDefault("paragraph");
        var paragraph = chain.Aggregate(Merge(Empty, defaults?.Element(StyleNs + "paragraph-properties")), (p, s) => Merge(p, s.Element(StyleNs + "paragraph-properties")));
        var text = chain.Aggregate(Merge(Empty, defaults?.Element(StyleNs + "text-properties")), (t, s) => Merge(t, s.Element(StyleNs + "text-properties")));
        var builtIn = chain.Where(s => s.Parent?.Name == OfficeNs + "styles").Reverse().Select(BuiltIn).FirstOrDefault(id => id != null);
        var format = ParagraphFormatOf(paragraph);
        if (!string.IsNullOrEmpty(Attr(chain.LastOrDefault(), StyleNs + "master-page-name"))) format = format with { PageBreakBefore = true };
        return _paragraphs[(header, name ?? "")] = new OdtParagraphStyle(format, paragraph.GetValueOrDefault(FoNs + "break-after") == "page", builtIn, text);
    }

    internal IReadOnlyDictionary<XName, string> Span(bool header, string name, IReadOnlyDictionary<XName, string> basis) =>
        name == null ? basis :
        _spans.TryGetValue((header, name, basis), out var cached) ? cached :
        _spans[(header, name, basis)] = Chain(header, "text", name).Aggregate(basis, (t, s) => Merge(t, s.Element(StyleNs + "text-properties")));

    internal RunFormat Run(IReadOnlyDictionary<XName, string> text)
    {
        if (_runs.TryGetValue(text, out var cached)) return cached;
        var weight = text.GetValueOrDefault(FoNs + "font-weight");
        var underline = text.GetValueOrDefault(StyleNs + "text-underline-style");
        var strike = text.GetValueOrDefault(StyleNs + "text-line-through-style") is { } through && through != "none";
        var doubleStrike = strike && text.GetValueOrDefault(StyleNs + "text-line-through-type") == "double";
        var size = Twips(text.GetValueOrDefault(FoNs + "font-size")) / 20;
        var position = text.GetValueOrDefault(StyleNs + "text-position")?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var raise = position switch
        {
            "super" => 1,
            "sub" => -1,
            _ => double.TryParse(position?.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ? Math.Sign(percent) : 0
        };
        var family = text.GetValueOrDefault(FoNs + "font-family") is { } direct ? FirstFamily(direct) : null;
        var fontName = text.GetValueOrDefault(StyleNs + "font-name");
        return _runs[text] = new RunFormat
        {
            Bold = weight == "bold" || int.TryParse(weight, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) && numeric >= 600 ? true : null,
            Italic = text.GetValueOrDefault(FoNs + "font-style") is "italic" or "oblique" ? true : null,
            Underline = underline == null || underline == "none" ? null :
                text.GetValueOrDefault(StyleNs + "text-underline-type") == "double" ? "double" : Underlines.GetValueOrDefault(underline, "single"),
            Strike = strike && !doubleStrike ? true : null,
            DoubleStrike = doubleStrike ? true : null,
            SizePt = size is { } points and > 0 ? Math.Min(points, 1638) : null,
            Color = Color(text.GetValueOrDefault(FoNs + "color")),
            Font = family ?? (fontName == null ? null : _fonts.GetValueOrDefault(fontName, fontName)),
            Shading = Color(text.GetValueOrDefault(FoNs + "background-color")),
            VerticalAlign = raise > 0 ? VerticalAlign.Superscript : raise < 0 ? VerticalAlign.Subscript : null
        };
    }

    private static ParagraphFormat ParagraphFormatOf(IReadOnlyDictionary<XName, string> paragraph)
    {
        int? Length(string name, bool positive = false) =>
            RoundTwips(paragraph.GetValueOrDefault(FoNs + name)) is { } twips ? positive ? Math.Max(0, twips) : twips : null;
        var rightToLeft = paragraph.GetValueOrDefault(StyleNs + "writing-mode") is "rl-tb" or "rl";
        return new ParagraphFormat
        {
            Alignment = paragraph.GetValueOrDefault(FoNs + "text-align") switch
            {
                "left" => ParagraphAlignment.Left,
                "start" => rightToLeft ? ParagraphAlignment.Right : ParagraphAlignment.Left,
                "center" => ParagraphAlignment.Center,
                "right" => ParagraphAlignment.Right,
                "end" => rightToLeft ? ParagraphAlignment.Left : ParagraphAlignment.Right,
                "justify" => ParagraphAlignment.Justify,
                _ => null
            },
            IndentLeft = Length("margin-left"),
            IndentRight = Length("margin-right"),
            IndentFirstLine = Length("text-indent"),
            SpacingBefore = Length("margin-top", true),
            SpacingAfter = Length("margin-bottom", true),
            PageBreakBefore = paragraph.GetValueOrDefault(FoNs + "break-before") == "page" ? true : null
        };
    }

    private static IReadOnlyDictionary<XName, string> Merge(IReadOnlyDictionary<XName, string> basis, XElement properties)
    {
        if (properties == null || !properties.HasAttributes) return basis;
        var merged = new Dictionary<XName, string>(basis);
        foreach (var attribute in properties.Attributes())
            if (attribute.Name == FoNs + "font-size" && attribute.Value.Trim().EndsWith('%'))
            {
                if (double.TryParse(attribute.Value.Trim().TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) && double.IsFinite(percent))
                    merged[attribute.Name] = ((Twips(basis.GetValueOrDefault(attribute.Name)) / 20 ?? 12) * percent / 100).ToString("0.###", CultureInfo.InvariantCulture) + "pt";
            }
            else
                merged[attribute.Name] = attribute.Value;
        return merged;
    }

    private List<XElement> Chain(bool header, string family, string name)
    {
        var chain = new List<XElement>();
        var seen = new HashSet<string>();
        for (var style = Find(header, family, name); style != null && chain.Count < MaxChain && seen.Add(Attr(style, StyleNs + "name")); style = Find(header, family, Attr(style, StyleNs + "parent-style-name")))
            chain.Insert(0, style);
        return chain;
    }

    private static string BuiltIn(XElement style)
    {
        var name = Attr(style, StyleNs + "display-name") ?? Attr(style, StyleNs + "name");
        return BuiltInStyles.All.FirstOrDefault(s => s.Type == StyleType.Paragraph && !s.IsDefault &&
                                                     (string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) || s.Id == name))?.Id;
    }

    private static string FirstFamily(string families) => families.Split(',')[0].Trim().Trim('\'', '"').Trim() is { Length: > 0 } family ? family : null;

    private void Add(XElement container, Dictionary<(string, string), XElement> styles)
    {
        foreach (var element in container?.Elements() ?? [])
            if (element.Name == StyleNs + "default-style" && Attr(element, StyleNs + "family") is { } defaultFamily)
                _defaults[defaultFamily] = element;
            else if (Attr(element, StyleNs + "name") is { } name)
            {
                var family = element.Name == StyleNs + "style" ? Attr(element, StyleNs + "family") :
                    element.Name == TextNs + "list-style" ? "list" :
                    element.Name == StyleNs + "page-layout" ? "page-layout" : null;
                if (family != null) styles.TryAdd((family, name), element);
            }
    }
}

internal sealed record OdtParagraphStyle(ParagraphFormat Format, bool BreakAfter, string BuiltIn, IReadOnlyDictionary<XName, string> Text);
