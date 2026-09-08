using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Helper.Internal;

/// <summary>
/// A minimal OOXML PowerPoint (.pptx) reader that turns every slide into a block of html. Positions and sizes are
/// emitted as container query units (cqw) relative to the slide width, so the whole slide scales with its
/// container without any JavaScript. Placeholders without their own transform inherit it from the slide layout and
/// then from the slide master, the way PowerPoint resolves them.
/// </summary>
/// <remarks>
/// Deliberately covers the common case only: text shapes, pictures, solid fills, groups and speaker notes. Charts,
/// SmartArt, tables, theme colors, gradients and animations are ignored - a slide using them renders without those
/// parts rather than failing.
/// </remarks>
internal sealed class PptxPresentation : IDisposable
{
    private const long EmuPerPoint = 12700;

    private static readonly XNamespace P = "http://schemas.openxmlformats.org/presentationml/2006/main";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private readonly ZipArchive _archive;

    private PptxPresentation(ZipArchive archive)
    {
        _archive = archive;
    }

    /// <summary>Slide width in EMU, the reference for every emitted cqw value.</summary>
    public long SlideWidth { get; private set; } = 12192000;

    /// <summary>Slide height in EMU.</summary>
    public long SlideHeight { get; private set; } = 6858000;

    /// <summary>All slides in presentation order.</summary>
    public List<PptxSlide> Slides { get; } = new();

    public static PptxPresentation Open(Stream stream)
    {
        var presentation = new PptxPresentation(new ZipArchive(stream, ZipArchiveMode.Read));
        presentation.Load();
        return presentation;
    }

    private void Load()
    {
        var presentation = ReadXml("ppt/presentation.xml")
                           ?? throw new FormatException("ppt/presentation.xml is missing, this is not a valid PPTX file.");

        var size = presentation.Root?.Element(P + "sldSz");
        if (size != null)
        {
            SlideWidth = Attr(size, "cx") ?? SlideWidth;
            SlideHeight = Attr(size, "cy") ?? SlideHeight;
        }

        var relations = ReadRelations("ppt/presentation.xml");
        var slideIds = presentation.Root?.Element(P + "sldIdLst")?.Elements(P + "sldId") ?? Enumerable.Empty<XElement>();

        var number = 0;
        foreach (var slideId in slideIds)
        {
            var relationId = slideId.Attribute(R + "id")?.Value;
            if (relationId == null || !relations.TryGetValue(relationId, out var target))
                continue;

            var path = Combine("ppt", target);
            var slide = ReadSlide(path, ++number);
            if (slide != null)
                Slides.Add(slide);
        }
    }

    private PptxSlide ReadSlide(string path, int number)
    {
        var document = ReadXml(path);
        var tree = document?.Root?.Element(P + "cSld")?.Element(P + "spTree");
        if (tree == null)
            return null;

        var relations = ReadRelations(path);
        var placeholders = ReadLayoutPlaceholders(path, relations);

        var html = new StringBuilder();
        foreach (var shape in tree.Elements())
            AppendShape(html, shape, path, relations, placeholders, new Transform());

        return new PptxSlide
        {
            Number = number,
            Title = FindTitle(tree),
            Html = html.ToString(),
            Notes = ReadNotes(path, relations)
        };
    }

    /// <summary>
    /// Collects the placeholder transforms of the slide layout and, behind it, the slide master. Keys are
    /// "type:idx" plus the bare type, because a slide may reference a placeholder by either.
    /// </summary>
    private Dictionary<string, Transform> ReadLayoutPlaceholders(string slidePath, Dictionary<string, string> slideRelations)
    {
        var result = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);

        var layoutTarget = slideRelations.Values.FirstOrDefault(v => v.Contains("slideLayout", StringComparison.OrdinalIgnoreCase));
        if (layoutTarget == null)
            return result;

        var layoutPath = Combine(GetDirectory(slidePath), layoutTarget);
        var masterTarget = ReadRelations(layoutPath).Values.FirstOrDefault(v => v.Contains("slideMaster", StringComparison.OrdinalIgnoreCase));

        // Master first, so the layout can overwrite it.
        if (masterTarget != null)
            AddPlaceholders(result, Combine(GetDirectory(layoutPath), masterTarget));
        AddPlaceholders(result, layoutPath);

        return result;
    }

    private void AddPlaceholders(Dictionary<string, Transform> target, string path)
    {
        var tree = ReadXml(path)?.Root?.Element(P + "cSld")?.Element(P + "spTree");
        if (tree == null)
            return;

        foreach (var shape in tree.Descendants(P + "sp"))
        {
            var placeholder = shape.Descendants(P + "ph").FirstOrDefault();
            var transform = ReadTransform(shape);
            if (placeholder == null || transform == null)
                continue;

            var type = placeholder.Attribute("type")?.Value ?? "body";
            var index = placeholder.Attribute("idx")?.Value;

            target[$"{type}:{index}"] = transform;
            target[type] = transform;
        }
    }

    private void AppendShape(StringBuilder html, XElement shape, string slidePath,
        Dictionary<string, string> relations, Dictionary<string, Transform> placeholders, Transform groupOffset)
    {
        if (shape.Name == P + "grpSp")
        {
            AppendGroup(html, shape, slidePath, relations, placeholders, groupOffset);
            return;
        }

        if (shape.Name == P + "pic")
        {
            AppendPicture(html, shape, slidePath, relations, groupOffset);
            return;
        }

        if (shape.Name != P + "sp")
            return;

        var transform = ResolveTransform(shape, placeholders);
        if (transform == null)
            return;

        transform = groupOffset.Apply(transform);

        var body = shape.Element(P + "txBody");
        var fill = SolidFill(shape.Element(P + "spPr"));
        var paragraphs = body?.Elements(A + "p").ToList();
        var hasText = paragraphs?.Any(p => p.Descendants(A + "t").Any(t => t.Value.Length > 0)) == true;

        if (fill == null && !hasText)
            return;

        var style = new StringBuilder(Position(transform));
        if (fill != null)
            style.Append($"background-color:{fill};");
        style.Append($"justify-content:{VerticalAlign(body)};");

        html.Append($"<div class=\"mud-ex-pptx-shape\" style=\"{style}\">");
        if (paragraphs != null)
        {
            var isTitle = IsTitle(shape);
            foreach (var paragraph in paragraphs)
                AppendParagraph(html, paragraph, isTitle);
        }
        html.Append("</div>");
    }

    private void AppendGroup(StringBuilder html, XElement group, string slidePath,
        Dictionary<string, string> relations, Dictionary<string, Transform> placeholders, Transform groupOffset)
    {
        // A group maps its children's coordinate space (chOff/chExt) onto its own box (off/ext).
        var xfrm = group.Element(P + "grpSpPr")?.Element(A + "xfrm");
        var outer = ReadTransform(group);
        var childOffset = xfrm?.Element(A + "chOff");
        var childExtent = xfrm?.Element(A + "chExt");

        var mapping = new Transform();
        if (outer != null && childOffset != null && childExtent != null)
        {
            var childWidth = Attr(childExtent, "cx") ?? 0;
            var childHeight = Attr(childExtent, "cy") ?? 0;
            mapping = new Transform
            {
                X = outer.X,
                Y = outer.Y,
                ChildX = Attr(childOffset, "x") ?? 0,
                ChildY = Attr(childOffset, "y") ?? 0,
                ScaleX = childWidth == 0 ? 1 : (double)outer.Width / childWidth,
                ScaleY = childHeight == 0 ? 1 : (double)outer.Height / childHeight,
                IsMapping = true
            };
        }

        var combined = groupOffset.Combine(mapping);
        foreach (var child in group.Elements())
            AppendShape(html, child, slidePath, relations, placeholders, combined);
    }

    private void AppendPicture(StringBuilder html, XElement picture, string slidePath,
        Dictionary<string, string> relations, Transform groupOffset)
    {
        var transform = ReadTransform(picture);
        var embedId = picture.Descendants(A + "blip").FirstOrDefault()?.Attribute(R + "embed")?.Value;
        if (transform == null || embedId == null || !relations.TryGetValue(embedId, out var target))
            return;

        var dataUrl = DataUrl(Combine(GetDirectory(slidePath), target));
        if (dataUrl == null)
            return;

        transform = groupOffset.Apply(transform);
        html.Append($"<img class=\"mud-ex-pptx-image\" style=\"{Position(transform)}\" src=\"{dataUrl}\" alt=\"\" />");
    }

    private void AppendParagraph(StringBuilder html, XElement paragraph, bool isTitle)
    {
        var properties = paragraph.Element(A + "pPr");
        var align = properties?.Attribute("algn")?.Value switch
        {
            "ctr" => "center",
            "r" => "right",
            "just" => "justify",
            _ => "left"
        };
        var level = int.TryParse(properties?.Attribute("lvl")?.Value, out var lvl) ? lvl : 0;

        var text = new StringBuilder();
        foreach (var node in paragraph.Elements())
        {
            if (node.Name == A + "br")
            {
                text.Append("<br />");
                continue;
            }
            if (node.Name != A + "r")
                continue;

            var run = node.Element(A + "t")?.Value;
            if (string.IsNullOrEmpty(run))
                continue;

            text.Append($"<span style=\"{RunStyle(node.Element(A + "rPr"), isTitle)}\">{WebUtility.HtmlEncode(run)}</span>");
        }

        if (text.Length == 0)
        {
            html.Append("<p class=\"mud-ex-pptx-paragraph\">&nbsp;</p>");
            return;
        }

        var indent = level > 0 ? $"padding-left:{Cqw(level * 300000L)}cqw;" : string.Empty;
        html.Append($"<p class=\"mud-ex-pptx-paragraph\" style=\"text-align:{align};{indent}\">{text}</p>");
    }

    private string RunStyle(XElement runProperties, bool isTitle)
    {
        // "sz" is in hundredths of a point; without it fall back to PowerPoint's default placeholder sizes.
        var sizeInPoints = double.TryParse(runProperties?.Attribute("sz")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sz)
            ? sz / 100
            : isTitle ? 44 : 18;

        var style = new StringBuilder($"font-size:{Cqw((long)(sizeInPoints * EmuPerPoint))}cqw;");

        if (runProperties?.Attribute("b")?.Value == "1")
            style.Append("font-weight:bold;");
        if (runProperties?.Attribute("i")?.Value == "1")
            style.Append("font-style:italic;");
        if (runProperties?.Attribute("u") != null && runProperties.Attribute("u")!.Value != "none")
            style.Append("text-decoration:underline;");

        var color = SolidFillColor(runProperties);
        if (color != null)
            style.Append($"color:{color};");

        var font = runProperties?.Element(A + "latin")?.Attribute("typeface")?.Value;
        if (!string.IsNullOrEmpty(font))
            style.Append($"font-family:'{font.Replace("'", string.Empty)}',sans-serif;");

        return style.ToString();
    }

    private static bool IsTitle(XElement shape)
    {
        var type = shape.Descendants(P + "ph").FirstOrDefault()?.Attribute("type")?.Value;
        return type is "title" or "ctrTitle";
    }

    private string FindTitle(XElement tree)
    {
        var title = tree.Elements(P + "sp").FirstOrDefault(IsTitle);
        var text = title?.Descendants(A + "t").Select(t => t.Value).ToList();
        return text is { Count: > 0 } ? string.Join(" ", text).Trim() : null;
    }

    private string ReadNotes(string slidePath, Dictionary<string, string> relations)
    {
        var notesTarget = relations.Values.FirstOrDefault(v => v.Contains("notesSlide", StringComparison.OrdinalIgnoreCase));
        if (notesTarget == null)
            return null;

        var notes = ReadXml(Combine(GetDirectory(slidePath), notesTarget));
        var paragraphs = notes?.Root?.Descendants(A + "p")
            .Select(p => string.Concat(p.Descendants(A + "t").Select(t => t.Value)).Trim())
            .Where(t => t.Length > 0)
            .ToList();

        return paragraphs is { Count: > 0 } ? string.Join(Environment.NewLine, paragraphs) : null;
    }

    private Transform ResolveTransform(XElement shape, Dictionary<string, Transform> placeholders)
    {
        var own = ReadTransform(shape);
        if (own != null)
            return own;

        var placeholder = shape.Descendants(P + "ph").FirstOrDefault();
        if (placeholder == null)
            return null;

        var type = placeholder.Attribute("type")?.Value ?? "body";
        var index = placeholder.Attribute("idx")?.Value;

        if (placeholders.TryGetValue($"{type}:{index}", out var byIndex))
            return byIndex;
        return placeholders.TryGetValue(type, out var byType) ? byType : null;
    }

    private static Transform ReadTransform(XElement shape)
    {
        var xfrm = shape.Descendants(A + "xfrm").FirstOrDefault();
        var offset = xfrm?.Element(A + "off");
        var extent = xfrm?.Element(A + "ext");
        if (offset == null || extent == null)
            return null;

        return new Transform
        {
            X = Attr(offset, "x") ?? 0,
            Y = Attr(offset, "y") ?? 0,
            Width = Attr(extent, "cx") ?? 0,
            Height = Attr(extent, "cy") ?? 0
        };
    }

    private string Position(Transform transform) =>
        $"left:{Cqw(transform.X)}cqw;top:{Cqw(transform.Y)}cqw;width:{Cqw(transform.Width)}cqw;height:{Cqw(transform.Height)}cqw;";

    /// <summary>EMU expressed as a percentage of the slide width, which is what one cqw unit is.</summary>
    private string Cqw(long emu) => (emu / (double)SlideWidth * 100).ToString("F3", CultureInfo.InvariantCulture);

    private static string SolidFill(XElement properties) => SolidFillColor(properties);

    private static string SolidFillColor(XElement parent)
    {
        var value = parent?.Element(A + "solidFill")?.Element(A + "srgbClr")?.Attribute("val")?.Value;
        return value == null ? null : $"#{value}";
    }

    private static string VerticalAlign(XElement body) => body?.Element(A + "bodyPr")?.Attribute("anchor")?.Value switch
    {
        "ctr" => "center",
        "b" => "flex-end",
        _ => "flex-start"
    };

    private static long? Attr(XElement element, string name)
        => long.TryParse(element.Attribute(name)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private Dictionary<string, string> ReadRelations(string partPath)
    {
        var relationsPath = $"{GetDirectory(partPath)}/_rels/{Path.GetFileName(partPath)}.rels".TrimStart('/');
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var document = ReadXml(relationsPath);
        if (document?.Root == null)
            return result;

        foreach (var relation in document.Root.Elements())
        {
            var id = relation.Attribute("Id")?.Value;
            var target = relation.Attribute("Target")?.Value;
            if (id != null && target != null)
                result[id] = target;
        }
        return result;
    }

    private ZipArchiveEntry FindEntry(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        return _archive.Entries.FirstOrDefault(e => string.Equals(e.FullName.Replace('\\', '/'), normalized, StringComparison.OrdinalIgnoreCase));
    }

    private XDocument ReadXml(string path)
    {
        var entry = FindEntry(path);
        if (entry == null)
            return null;

        try
        {
            using var stream = entry.Open();
            return XDocument.Load(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string DataUrl(string path)
    {
        var entry = FindEntry(path);
        if (entry == null)
            return null;

        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return $"data:{MimeTypeFor(path)};base64,{Convert.ToBase64String(ms.ToArray())}";
    }

    private static string MimeTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".webp" => "image/webp",
        ".bmp" => "image/bmp",
        ".tif" or ".tiff" => "image/tiff",
        ".emf" => "image/emf",
        ".wmf" => "image/wmf",
        _ => "application/octet-stream"
    };

    private static string GetDirectory(string path)
    {
        var index = path.LastIndexOf('/');
        return index < 0 ? string.Empty : path[..index];
    }

    private static string Combine(string directory, string target)
    {
        var segments = new List<string>();
        if (directory.Length > 0)
            segments.AddRange(directory.Split('/', StringSplitOptions.RemoveEmptyEntries));

        foreach (var segment in target.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
                continue;
            if (segment == "..")
            {
                if (segments.Count > 0)
                    segments.RemoveAt(segments.Count - 1);
                continue;
            }
            segments.Add(segment);
        }

        return string.Join('/', segments);
    }

    public void Dispose() => _archive?.Dispose();

    /// <summary>Position and size of a shape in EMU, plus the mapping a surrounding group applies to it.</summary>
    private sealed class Transform
    {
        public long X { get; init; }
        public long Y { get; init; }
        public long Width { get; init; }
        public long Height { get; init; }
        public long ChildX { get; init; }
        public long ChildY { get; init; }
        public double ScaleX { get; init; } = 1;
        public double ScaleY { get; init; } = 1;
        public bool IsMapping { get; init; }

        /// <summary>Maps a child transform through this group mapping.</summary>
        public Transform Apply(Transform child) => !IsMapping
            ? child
            : new Transform
            {
                X = X + (long)((child.X - ChildX) * ScaleX),
                Y = Y + (long)((child.Y - ChildY) * ScaleY),
                Width = (long)(child.Width * ScaleX),
                Height = (long)(child.Height * ScaleY)
            };

        /// <summary>Chains this mapping with a nested one so nested groups keep working.</summary>
        public Transform Combine(Transform inner)
        {
            if (!IsMapping)
                return inner;
            if (!inner.IsMapping)
                return this;

            var mapped = Apply(inner);
            return new Transform
            {
                X = mapped.X,
                Y = mapped.Y,
                Width = mapped.Width,
                Height = mapped.Height,
                ChildX = inner.ChildX,
                ChildY = inner.ChildY,
                ScaleX = inner.ScaleX * ScaleX,
                ScaleY = inner.ScaleY * ScaleY,
                IsMapping = true
            };
        }
    }
}

/// <summary>One rendered slide of a presentation.</summary>
internal sealed class PptxSlide
{
    /// <summary>One based slide number.</summary>
    public int Number { get; init; }
    /// <summary>Text of the title placeholder, if the slide has one.</summary>
    public string Title { get; init; }
    /// <summary>The slide content as html, positioned in container query units.</summary>
    public string Html { get; init; }
    /// <summary>Speaker notes of the slide.</summary>
    public string Notes { get; init; }
}
