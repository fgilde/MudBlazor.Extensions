using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class WorksheetChildOrder
{
    private static readonly string[] Names =
    [
        "sheetPr", "dimension", "sheetViews", "sheetFormatPr", "cols", "sheetData", "sheetCalcPr", "sheetProtection", "protectedRanges",
        "scenarios", "autoFilter", "sortState", "dataConsolidate", "customSheetViews", "mergeCells", "phoneticPr", "conditionalFormatting",
        "dataValidations", "hyperlinks", "printOptions", "pageMargins", "pageSetup", "headerFooter", "rowBreaks", "colBreaks",
        "customProperties", "cellWatches", "ignoredErrors", "smartTags", "drawing", "legacyDrawing", "legacyDrawingHF", "drawingHF",
        "picture", "oleObjects", "controls", "webPublishItems", "tableParts", "extLst"
    ];

    private static readonly Dictionary<string, int> Rank = Names.Select((n, i) => (n, i)).ToDictionary(p => p.n, p => p.i);

    internal static int Of(string localName) => Rank.GetValueOrDefault(localName, -1);

    internal static List<XElement> Arrange(IReadOnlyList<XElement> original, IEnumerable<XElement> generated, XNamespace ns)
    {
        var items = new List<(int Rank, int Unknown, int Position, XElement Element)>();
        var previous = -1;
        for (var i = 0; i < original.Count; i++)
        {
            var e = original[i];
            var rank = e.Name.Namespace == ns ? Of(e.Name.LocalName) : -1;
            if (rank >= 0) previous = rank;
            if (XlsxSheetPart.Modelled.Contains(e.Name.LocalName) && rank >= 0) continue;
            items.Add(rank >= 0 ? (rank, 0, i, e) : (previous, 1, i, e));
        }

        items.AddRange(generated.Where(e => e != null).Select(e => (Of(e.Name.LocalName), 0, -1, e)));
        return items.OrderBy(x => x.Rank).ThenBy(x => x.Unknown).ThenBy(x => x.Position).Select(x => x.Element).ToList();
    }
}
