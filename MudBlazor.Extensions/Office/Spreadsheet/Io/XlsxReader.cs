using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using static MudBlazor.Extensions.Office.Spreadsheet.XlsxPackage;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Reads Office Open XML workbooks (<c>.xlsx</c>, <c>.xlsm</c>, <c>.xltx</c>, <c>.xltm</c>) into a <see cref="Workbook"/>.</summary>
public static class XlsxReader
{
    /// <summary>
    /// Reads a workbook: sheets with names, visibility and tab colors, defined names, cell values, formulas (shared formulas
    /// expanded per cell, array formulas spilling from their first cell) with their cached results, styles, column widths, row
    /// heights, hidden rows and columns, merged ranges and frozen panes. The workbook keeps the source package so parts and
    /// worksheet content the model does not represent survive saving.
    /// </summary>
    /// <param name="stream">The package; it is read to the end and left open.</param>
    /// <returns>The workbook. Formula cells hold their cached values until a <see cref="CalcEngine"/> recalculates them.</returns>
    /// <exception cref="InvalidDataException">The stream is not a spreadsheet package.</exception>
    public static Workbook Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            return ReadPackage(stream);
        }
        catch (Exception ex) when (ex is not ObjectDisposedException && ex is NullReferenceException or ArgumentException or FormatException or XmlException or KeyNotFoundException
                                       or InvalidOperationException or IndexOutOfRangeException or OverflowException)
        {
            throw new InvalidDataException("The stream is not a valid spreadsheet package.", ex);
        }
    }

    private static Workbook ReadPackage(Stream stream)
    {
        byte[] source;
        if (stream.CanSeek)
        {
            source = new byte[stream.Length - stream.Position];
            stream.ReadExactly(source);
        }
        else
        {
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            source = copy.ToArray();
        }

        var package = new XlsxPackage { Source = source };
        using var zip = new ZipArchive(new MemoryStream(source, false), ZipArchiveMode.Read);
        package.PartNames.AddRange(zip.Entries.Select(e => e.FullName));
        ReadContentTypes(zip, package);

        var main = XlsxPackage.ReadRelationships(zip, "").FirstOrDefault(r => XlsxPackage.IsType(r, "officeDocument") && !r.External)
                   ?? throw new InvalidDataException("The package has no workbook part.");
        package.WorkbookPart = XlsxPackage.ResolveTarget("", main.Target);
        package.WorkbookRelationships.AddRange(XlsxPackage.ReadRelationships(zip, package.WorkbookPart));
        string Target(string type) =>
            package.WorkbookRelationships.FirstOrDefault(r => XlsxPackage.IsType(r, type) && !r.External) is { } rel
                ? XlsxPackage.ResolveTarget(package.WorkbookPart, rel.Target)
                : null;
        package.SharedStringsPart = Target("sharedStrings");
        package.StylesPart = Target("styles");
        package.ThemePart = Target("theme");

        var root = Load(zip, package.WorkbookPart)?.Root ?? throw new InvalidDataException("The package has no workbook part.");
        var wb = new Workbook { Package = package };
        package.Date1904 = (string)Child(root, "workbookPr")?.Attribute("date1904") is "1" or "true";

        var theme = Open(zip, package.ThemePart, ThemeColors.Read) ?? ThemeColors.Office;
        var (xfStyleIds, canonicalXf) = Open(zip, package.StylesPart, s => StylesXmlReader.Read(s, theme, wb.Styles));
        package.XfStyleIds = xfStyleIds is { Length: > 0 } ? xfStyleIds : [0];
        package.CanonicalXf = canonicalXf is { Length: > 0 } ? canonicalXf : [0];
        var strings = Open(zip, package.SharedStringsPart, s => ReadSharedStrings(s, package)) ?? [];

        var cells = 0;
        foreach (var sheet in Child(root, "sheets")?.Elements().Where(e => e.Name.LocalName == "sheet") ?? [])
        {
            var id = (string)sheet.Attributes().FirstOrDefault(a => a.Name.LocalName == "id" && a.Name.Namespace != XNamespace.None);
            var rel = package.WorkbookRelationships.FirstOrDefault(r => r.Id == id && !r.External);
            var state = (string)sheet.Attribute("state");
            var isWorksheet = rel != null && XlsxPackage.IsType(rel, "worksheet");
            var partName = rel == null ? null : XlsxPackage.ResolveTarget(package.WorkbookPart, rel.Target);
            var ws = isWorksheet ? new Worksheet((string)sheet.Attribute("name"), wb) { Hidden = state is "hidden" or "veryHidden" } : null;
            var part = new XlsxSheetPart
            {
                Name = (string)sheet.Attribute("name"),
                SheetId = (string)sheet.Attribute("sheetId"),
                RelationshipId = id,
                PartName = partName,
                State = state,
                Worksheet = ws
            };
            package.Sheets.Add(part);
            if (ws == null) continue;
            wb.SheetList.Add(ws);
            var entry = Find(zip, partName);
            if (entry == null) continue;
            using var s = entry.Open();
            ws.CellLimit = Workbook.MaxCells - cells;
            SheetXmlReader.Read(s, entry.Length, ws, part, strings, package, theme);
            ws.Tables = TableXml.Read(zip, part);
            AnnotationXml.Read(zip, part, ws);
            ws.CellLimit = int.MaxValue;
            cells += ws.CellCount;
        }

        if (wb.Sheets.Count == 0) wb.SheetList.Add(new Worksheet("Sheet1", wb));

        package.DefinedNames = Child(root, "definedNames");
        foreach (var name in package.DefinedNames?.Elements().Where(e => e.Name.LocalName == "definedName" && e.Attribute("localSheetId") == null) ?? [])
            if ((string)name.Attribute("name") is { Length: > 0 } key)
                wb.DefinedNameMap[key] = SheetXmlWriter.FromFile(name.Value);

        package.DetectUnshiftedReferences();
        return wb;
    }

    private static T Open<T>(ZipArchive zip, string part, Func<Stream, T> read)
    {
        var entry = XlsxPackage.Find(zip, part);
        if (entry == null) return default;
        using var s = entry.Open();
        return read(s);
    }

    private static XDocument Load(ZipArchive zip, string part) => Open(zip, part, XlsxPackage.LoadXml);

    private static void ReadContentTypes(ZipArchive zip, XlsxPackage package)
    {
        foreach (var e in Load(zip, "[Content_Types].xml")?.Root?.Elements() ?? [])
        {
            var type = (string)e.Attribute("ContentType");
            if (e.Name.LocalName == "Default" && (string)e.Attribute("Extension") is { } ext) package.DefaultContentTypes[ext] = type;
            else if (e.Name.LocalName == "Override" && (string)e.Attribute("PartName") is { } name) package.OverrideContentTypes[name] = type;
        }
    }

    private static string[] ReadSharedStrings(Stream stream, XlsxPackage package)
    {
        var list = new List<string>();
        using var reader = XmlReader.Create(stream, SheetXmlReader.Settings);
        reader.MoveToContent();
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "si")
            {
                reader.Read();
                continue;
            }

            if (list.Count >= Workbook.MaxCells) throw Worksheet.TooManyCells();
            var si = (XElement)XNode.ReadFrom(reader);
            list.Add(SheetXmlReader.Text(si, out var rich));
            if (rich) package.RichStrings[list.Count - 1] = si;
        }

        return [.. list];
    }
}
