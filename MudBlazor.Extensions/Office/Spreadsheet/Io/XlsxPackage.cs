using System.IO.Compression;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>A package relationship.</summary>
/// <param name="Id">The relationship id.</param>
/// <param name="Type">The relationship type URI.</param>
/// <param name="Target">The target as written in the rels part.</param>
/// <param name="External">True for <c>TargetMode="External"</c>.</param>
internal sealed record XlsxRelationship(string Id, string Type, string Target, bool External);

/// <summary>A sheet entry of the source workbook with the parts of its XML the model does not represent.</summary>
internal sealed class XlsxSheetPart
{
    internal static readonly HashSet<string> Modelled = ["sheetPr", "dimension", "sheetViews", "sheetFormatPr", "cols", "sheetData", "mergeCells"];

    /// <summary>The sheet name as stored in the file.</summary>
    public string Name { get; init; }

    /// <summary>The <c>sheetId</c> attribute.</summary>
    public string SheetId { get; init; }

    /// <summary>The relationship id from workbook.xml.</summary>
    public string RelationshipId { get; init; }

    /// <summary>Zip entry name of the sheet part, or null if the relationship target is missing.</summary>
    public string PartName { get; init; }

    /// <summary>The original <c>state</c> attribute (<c>visible</c>, <c>hidden</c> or <c>veryHidden</c>), or null.</summary>
    public string State { get; init; }

    /// <summary>The worksheet read into the model, or null for chart sheets, dialog sheets and macro sheets.</summary>
    public Worksheet Worksheet { get; init; }

    /// <summary>The root element of the sheet part without children, keeping namespace declarations and <c>mc:Ignorable</c>.</summary>
    public XElement Root { get; set; }

    /// <summary>
    /// All top-level children of the worksheet in original order. <c>sheetData</c> is an empty placeholder marking its position;
    /// the other children are the original elements, including the partly modelled ones in <see cref="Modelled"/>
    /// (sheetPr, sheetViews, sheetFormatPr, cols keep attributes the model ignores).
    /// </summary>
    public List<XElement> Children { get; } = [];

    /// <summary>Children the model does not represent at all, in original order.</summary>
    public IEnumerable<XElement> Unmodelled => Children.Where(c => !Modelled.Contains(c.Name.LocalName));

    /// <summary>Cells holding a rich shared string: cell key to the shared string index of the original <c>si</c>.</summary>
    public Dictionary<long, int> RichStringCells { get; } = new();

    /// <summary>
    /// Cells whose original <c>cellXfs</c> index differs from <see cref="XlsxPackage.CanonicalXf"/> of their style id: cell key to
    /// the original xf index. Includes styled empty cells that are not in the model because their xf maps to style 0
    /// (for example an unlocked input cell). While a cell's style id is unchanged the writer emits this xf instead of the canonical one.
    /// </summary>
    public Dictionary<long, int> OriginalXf { get; } = new();

    /// <summary>Rows with a row style whose original xf index differs from the canonical xf of their style id: zero-based row to xf index.</summary>
    public Dictionary<int, int> OriginalRowXf { get; } = new();

    /// <summary>Columns whose original <c>col/@style</c> xf index differs from the canonical xf of their style id: zero-based column to xf index.</summary>
    public Dictionary<int, int> OriginalColXf { get; } = new();

    /// <summary>
    /// Row attributes the model does not represent (<c>outlineLevel</c>, <c>collapsed</c>, <c>thickTop</c>, <c>thickBot</c>, <c>ph</c>,
    /// <c>x14ac:dyDescent</c> …), by zero-based row; rows with equal attributes share one array. <c>spans</c> is dropped.
    /// </summary>
    public Dictionary<int, XAttribute[]> RowAttributes { get; } = new();

    /// <summary>Cell attributes the model does not represent (<c>cm</c>, <c>vm</c>, <c>ph</c> …), by cell key; only cells that have them.</summary>
    public Dictionary<long, XAttribute[]> CellAttributes { get; } = new();

    /// <summary>Cells whose formula was stored as an array formula (<c>f/@t="array"</c>), including single-cell ones, by cell key.</summary>
    public HashSet<long> ArrayFormulas { get; } = new();
}

/// <summary>
/// Inventory of a source xlsx package kept with a <see cref="Workbook"/> so the writer can copy untouched parts byte for byte and
/// re-emit what the model does not represent.
/// </summary>
internal sealed class XlsxPackage
{
    internal const string RelationshipsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    /// <summary>The original package bytes.</summary>
    public byte[] Source { get; init; }

    /// <summary>Zip entry names in original order.</summary>
    public List<string> PartNames { get; } = [];

    /// <summary>Default content types by lower-case extension.</summary>
    public Dictionary<string, string> DefaultContentTypes { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Override content types by part name with leading slash.</summary>
    public Dictionary<string, string> OverrideContentTypes { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Zip entry name of the workbook part.</summary>
    public string WorkbookPart { get; set; }

    /// <summary>Content type of the workbook part (macro-enabled, template or plain).</summary>
    public string WorkbookContentType => ContentType(WorkbookPart);

    /// <summary>Relationships of the workbook part in original order.</summary>
    public List<XlsxRelationship> WorkbookRelationships { get; } = [];

    /// <summary>Zip entry name of the shared strings part, or null.</summary>
    public string SharedStringsPart { get; set; }

    /// <summary>Zip entry name of the styles part, or null.</summary>
    public string StylesPart { get; set; }

    /// <summary>Zip entry name of the theme part, or null.</summary>
    public string ThemePart { get; set; }

    /// <summary>All sheets of workbook.xml in tab order, including sheets that are not worksheets.</summary>
    public List<XlsxSheetPart> Sheets { get; } = [];

    /// <summary>The original <c>definedNames</c> element (sheet-scoped, hidden and commented names live only here), or null.</summary>
    public XElement DefinedNames { get; set; }

    /// <summary>True if the workbook uses the 1904 date system; serials are kept as stored.</summary>
    public bool Date1904 { get; set; }

    /// <summary>Style id in <see cref="Workbook.Styles"/> for each <c>cellXfs</c> index.</summary>
    public int[] XfStyleIds { get; set; } = [0];

    /// <summary>Original <c>si</c> elements of shared strings with runs or phonetic text, by shared string index.</summary>
    public Dictionary<int, XElement> RichStrings { get; } = new();

    /// <summary>
    /// The first <c>cellXfs</c> index that produced each style id (index = style id; style 0 is xf 0). Several xfs that differ only in
    /// attributes the model ignores (protection, quotePrefix, xfId, rotation …) share one style id; the cells keep their own xf in
    /// <see cref="XlsxSheetPart.OriginalXf"/>.
    /// </summary>
    public int[] CanonicalXf { get; set; } = [0];

    /// <summary>Content type of a zip entry from its override or extension default.</summary>
    /// <param name="partName">Zip entry name without leading slash.</param>
    /// <returns>The content type, or null.</returns>
    public string ContentType(string partName) =>
        partName == null ? null
        : OverrideContentTypes.TryGetValue("/" + partName, out var type) ? type
        : DefaultContentTypes.GetValueOrDefault(Path.GetExtension(partName).TrimStart('.'));

    internal static XElement Child(XElement e, string name) => e?.Elements().FirstOrDefault(c => c.Name.LocalName == name);

    internal static ZipArchiveEntry Find(ZipArchive zip, string name) =>
        name == null ? null : zip.GetEntry(name) ?? zip.Entries.FirstOrDefault(e => string.Equals(e.FullName, name, StringComparison.OrdinalIgnoreCase));

    internal static string RelsPartOf(string part)
    {
        var slash = part.LastIndexOf('/');
        return part[..(slash + 1)] + "_rels/" + part[(slash + 1)..] + ".rels";
    }

    internal static string ResolveTarget(string sourcePart, string target)
    {
        if (target.StartsWith('/')) return Normalize(target[1..]);
        var slash = sourcePart.LastIndexOf('/');
        return Normalize(sourcePart[..(slash + 1)] + target);
    }

    internal static List<XlsxRelationship> ReadRelationships(ZipArchive zip, string part)
    {
        var entry = Find(zip, RelsPartOf(part));
        if (entry == null) return [];
        using var s = entry.Open();
        return XDocument.Load(s).Root.Elements().Where(e => e.Name.LocalName == "Relationship")
            .Select(e => new XlsxRelationship((string)e.Attribute("Id"), (string)e.Attribute("Type"), (string)e.Attribute("Target"),
                (string)e.Attribute("TargetMode") == "External"))
            .ToList();
    }

    internal static bool IsType(XlsxRelationship rel, string type) => rel.Type != null && rel.Type.EndsWith("/" + type, StringComparison.Ordinal);

    private static string Normalize(string path)
    {
        var parts = new List<string>();
        foreach (var p in Uri.UnescapeDataString(path).Split('/'))
        {
            if (p is "" or ".") continue;
            if (p == "..")
            {
                if (parts.Count > 0) parts.RemoveAt(parts.Count - 1);
            }
            else parts.Add(p);
        }

        return string.Join('/', parts);
    }
}
