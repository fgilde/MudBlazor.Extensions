using System.Text.Json;
using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Office.Notebook;

internal abstract class JsonObjectDto
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extra { get; set; }
}

internal sealed class ManifestDto : JsonObjectDto
{
    public string FormatVersion { get; set; }

    public string Name { get; set; }

    public List<SectionDto> Sections { get; set; }
}

internal sealed class SectionDto : JsonObjectDto
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Color { get; set; }

    public List<string> Pages { get; set; }
}

internal sealed class PageDto : JsonObjectDto
{
    public string Title { get; set; }

    public int Level { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset Modified { get; set; }

    public List<JsonElement> Elements { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextDto), "text")]
[JsonDerivedType(typeof(InkDto), "ink")]
[JsonDerivedType(typeof(ShapeDto), "shape")]
[JsonDerivedType(typeof(ImageDto), "image")]
[JsonDerivedType(typeof(TableDto), "table")]
internal abstract class ElementDto : JsonObjectDto
{
    [JsonPropertyOrder(-1)]
    public string Id { get; set; }

    [JsonPropertyOrder(-1)]
    public double X { get; set; }

    [JsonPropertyOrder(-1)]
    public double Y { get; set; }

    [JsonPropertyOrder(-1)]
    public double Width { get; set; }

    [JsonPropertyOrder(-1)]
    public double Height { get; set; }
}

internal sealed class TextDto : ElementDto
{
    public List<ParagraphDto> Paragraphs { get; set; }
}

internal sealed class InkDto : ElementDto
{
    public double[] Points { get; set; }

    public string Color { get; set; }

    public double StrokeWidth { get; set; }

    public InkTool Tool { get; set; }
}

internal sealed class ShapeDto : ElementDto
{
    public NoteShapeKind Kind { get; set; }

    public string Stroke { get; set; }

    public double StrokeWidth { get; set; }

    public string Fill { get; set; }

    public bool FlipX { get; set; }

    public bool FlipY { get; set; }
}

internal sealed class ImageDto : ElementDto
{
    public string MediaId { get; set; }

    public string ContentType { get; set; }

    public string AltText { get; set; }
}

internal sealed class TableDto : ElementDto
{
    public double[] ColumnWidths { get; set; }

    public List<List<CellDto>> Rows { get; set; }

    public bool HeaderRow { get; set; }
}

internal sealed class CellDto : JsonObjectDto
{
    public List<ParagraphDto> Paragraphs { get; set; }
}

internal sealed class ParagraphDto : JsonObjectDto
{
    public List<RunDto> Runs { get; set; }

    public NoteListKind List { get; set; }

    public int Indent { get; set; }

    public bool Checked { get; set; }

    public NoteAlign Align { get; set; }

    public NoteHeading Heading { get; set; }
}

internal sealed class RunDto : JsonObjectDto
{
    public string Text { get; set; }

    public bool Bold { get; set; }

    public bool Italic { get; set; }

    public bool Underline { get; set; }

    public bool Strike { get; set; }

    public double? SizePt { get; set; }

    public string Color { get; set; }

    public string Highlight { get; set; }

    public string Font { get; set; }

    public string Link { get; set; }
}

internal sealed class CamelCaseEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase, false)
    where TEnum : struct, Enum;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    Converters =
    [
        typeof(CamelCaseEnumConverter<InkTool>), typeof(CamelCaseEnumConverter<NoteShapeKind>), typeof(CamelCaseEnumConverter<NoteListKind>),
        typeof(CamelCaseEnumConverter<NoteAlign>), typeof(CamelCaseEnumConverter<NoteHeading>)
    ])]
[JsonSerializable(typeof(ManifestDto))]
[JsonSerializable(typeof(PageDto))]
[JsonSerializable(typeof(ElementDto))]
internal sealed partial class NotebookJsonContext : JsonSerializerContext;

internal static class NotebookJson
{
    public static ManifestDto ToDto(Notebook notebook) => new()
    {
        FormatVersion = notebook.FormatVersion,
        Name = notebook.Name,
        Sections = notebook.SectionList.Select(s => new SectionDto { Id = s.Id, Name = s.Name, Color = s.Color, Pages = s.PageList.Select(p => p.Id).ToList(), Extra = s.Extra })
            .ToList(),
        Extra = notebook.Extra
    };

    public static PageDto ToDto(NotebookPage page) => new()
    {
        Title = page.Title,
        Level = page.Level,
        Created = page.Created,
        Modified = page.LastModified,
        Elements = page.ElementList.Select(ToJson).ToList(),
        Extra = page.Extra
    };

    public static NotebookPage ToPage(PageDto dto)
    {
        var page = new NotebookPage
        {
            Title = dto.Title ?? "", Level = Math.Clamp(dto.Level, 0, NotebookPage.MaxLevel), Created = dto.Created, LastModified = dto.Modified, Extra = dto.Extra
        };
        foreach (var element in dto.Elements ?? []) page.ElementList.Add(ToElement(element));
        if (page.ElementList.Select(e => e.Id).Distinct(StringComparer.Ordinal).Count() != page.ElementList.Count)
            throw new JsonException("Two elements have the same id.");
        return page;
    }

    private static JsonElement ToJson(NoteElement element) =>
        element is OpaqueElement opaque ? opaque.ToJson() : JsonSerializer.SerializeToElement(ToDto(element), NotebookJsonContext.Default.ElementDto);

    private static ElementDto ToDto(NoteElement element)
    {
        ElementDto dto = element switch
        {
            TextElement text => new TextDto { Paragraphs = ToDto(text.Paragraphs) },
            InkElement ink => new InkDto
            {
                Points = ink.Points.SelectMany(p => new[] { p.X, p.Y, p.Pressure }).ToArray(), Color = ink.Color, StrokeWidth = ink.StrokeWidth, Tool = ink.Tool
            },
            ShapeElement shape => new ShapeDto
            {
                Kind = shape.Kind, Stroke = shape.Stroke, StrokeWidth = shape.StrokeWidth, Fill = shape.Fill, FlipX = shape.FlipX, FlipY = shape.FlipY
            },
            ImageElement image => new ImageDto { MediaId = image.MediaId, ContentType = image.ContentType, AltText = image.AltText },
            TableElement table => new TableDto
            {
                ColumnWidths = table.ColumnWidths.ToArray(), HeaderRow = table.HeaderRow,
                Rows = table.Rows.Select(r => r.Select(c => new CellDto { Paragraphs = ToDto(c.Paragraphs), Extra = c.Extra }).ToList()).ToList()
            },
            _ => throw new NotSupportedException($"The element type {element.GetType().Name} cannot be saved.")
        };
        dto.Id = element.Id;
        dto.X = element.X;
        dto.Y = element.Y;
        dto.Width = element.Width;
        dto.Height = element.Height;
        dto.Extra = element.Extra;
        return dto;
    }

    private static List<ParagraphDto> ToDto(IReadOnlyList<NoteParagraph> paragraphs) => paragraphs.Select(p => new ParagraphDto
    {
        Runs = p.Runs.Select(r => new RunDto
        {
            Text = r.Text, Bold = r.Format.Bold, Italic = r.Format.Italic, Underline = r.Format.Underline, Strike = r.Format.Strike, SizePt = r.Format.SizePt,
            Color = r.Format.Color, Highlight = r.Format.Highlight, Font = r.Format.Font, Link = r.Format.Link, Extra = r.Extra
        }).ToList(),
        List = p.List, Indent = p.Indent, Checked = p.Checked, Align = p.Align, Heading = p.Heading, Extra = p.Extra
    }).ToList();

    private static NoteElement ToElement(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object) throw new JsonException("An element is not an object.");
        try
        {
            return ToElement(json.Deserialize(NotebookJsonContext.Default.ElementDto));
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            return OpaqueElement.From(json);
        }
    }

    private static NoteElement ToElement(ElementDto dto)
    {
        NoteElement element = dto switch
        {
            TextDto text => new TextElement { Paragraphs = ToParagraphs(text.Paragraphs) },
            InkDto ink => new InkElement
            {
                Points = ToPoints(ink.Points), Color = Color(ink.Color) ?? "#000000", StrokeWidth = Math.Max(0, ink.StrokeWidth), Tool = ink.Tool
            },
            ShapeDto shape => new ShapeElement
            {
                Kind = shape.Kind, Stroke = Color(shape.Stroke) ?? "#000000", StrokeWidth = Math.Max(0, shape.StrokeWidth), Fill = Color(shape.Fill),
                FlipX = shape.FlipX, FlipY = shape.FlipY
            },
            ImageDto image => new ImageElement
            {
                MediaId = NoteValues.IsId(image.MediaId) ? image.MediaId : null, ContentType = NotebookMediaTypes.Normalize(image.ContentType), AltText = image.AltText ?? ""
            },
            TableDto table => ToTable(table),
            _ => throw new JsonException("Unknown element type.")
        };
        element.Id = NoteValues.IsId(dto.Id) ? dto.Id : throw new JsonException("An element has no valid id.");
        element.X = dto.X;
        element.Y = dto.Y;
        element.Width = Math.Max(0, dto.Width);
        element.Height = Math.Max(0, dto.Height);
        element.Extra = dto.Extra;
        return element;
    }

    private static TableElement ToTable(TableDto dto)
    {
        var widths = dto.ColumnWidths ?? [];
        var rows = (dto.Rows ?? []).Select(r => (IReadOnlyList<NoteTableCell>)(r ?? []).Select(c => new NoteTableCell
        {
            Paragraphs = ToParagraphs(c?.Paragraphs), Extra = c?.Extra
        }).ToList()).ToList();
        if (rows.Any(r => r.Count != widths.Length)) throw new JsonException("A table row does not have one cell per column.");
        return new TableElement { ColumnWidths = widths.Select(w => Math.Max(0, w)).ToList(), Rows = rows, HeaderRow = dto.HeaderRow };
    }

    private static IReadOnlyList<InkPoint> ToPoints(double[] values)
    {
        values ??= [];
        if (values.Length % 3 != 0) throw new JsonException("Ink points must be triples of x, y and pressure.");
        return Enumerable.Range(0, values.Length / 3).Select(i => new InkPoint(values[3 * i], values[3 * i + 1], Math.Clamp(values[3 * i + 2], 0, 1))).ToList();
    }

    private static IReadOnlyList<NoteParagraph> ToParagraphs(List<ParagraphDto> paragraphs) => (paragraphs ?? []).Select(p => p == null
        ? new NoteParagraph()
        : new NoteParagraph
        {
            Runs = (p.Runs ?? []).Where(r => r != null).Select(r => new NoteRun(r.Text, new NoteRunFormat(r.Bold, r.Italic, r.Underline, r.Strike,
                r.SizePt is > 0 and <= 1000 ? r.SizePt : null, Color(r.Color), Color(r.Highlight), NoteValues.IsFont(r.Font) ? r.Font : null, NoteValues.IsSafeLink(r.Link) ? r.Link : null)) { Extra = r.Extra })
                .ToList(),
            List = p.List, Indent = Math.Clamp(p.Indent, 0, NoteParagraph.MaxIndent), Checked = p.Checked, Align = p.Align, Heading = p.Heading, Extra = p.Extra
        }).ToList();

    private static string Color(string value) => NoteValues.IsColor(value) ? value : null;
}
