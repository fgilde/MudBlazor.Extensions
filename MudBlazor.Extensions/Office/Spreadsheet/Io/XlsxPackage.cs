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

    /// <summary>Content type of a zip entry from its override or extension default.</summary>
    /// <param name="partName">Zip entry name without leading slash.</param>
    /// <returns>The content type, or null.</returns>
    public string ContentType(string partName) =>
        partName == null ? null
        : OverrideContentTypes.TryGetValue("/" + partName, out var type) ? type
        : DefaultContentTypes.GetValueOrDefault(Path.GetExtension(partName).TrimStart('.'));

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
