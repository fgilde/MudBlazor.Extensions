using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public partial class DocxWriterTests
{
    private static readonly string[] Stories = ["/document", "/header", "/footer", "/footnotes", "/endnotes", "/comments"];

    [Fact]
    public void Writer_ChangedTable_KeepsRowAndCellWrappers_UntouchedRowsIdentical()
    {
        static string Cell(string text) => $"<w:tc><w:p><w:r><w:t>{text}</w:t></w:r></w:p></w:tc>";
        var source = new DocxBuilder().Body(
            "<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr><w:tblGrid><w:gridCol w:w=\"3000\"/><w:gridCol w:w=\"3000\"/></w:tblGrid>" +
            $"<w:customXml w:element=\"row\"><w:tr>{Cell("r1a")}{Cell("r1b")}</w:tr></w:customXml>" +
            $"<w:tr><w:sdt><w:sdtPr><w:id w:val=\"3\"/></w:sdtPr><w:sdtContent>{Cell("r2a")}</w:sdtContent></w:sdt>{Cell("r2b")}</w:tr>" +
            $"<w:sdt><w:sdtPr><w:id w:val=\"4\"/></w:sdtPr><w:sdtContent><w:tr>{Cell("r3a")}{Cell("r3b")}</w:tr></w:sdtContent></w:sdt>" +
            $"<w:tr><w:trPr><w:del w:id=\"5\" w:author=\"A\"/></w:trPr>{Cell("gone")}{Cell("gone")}</w:tr>" +
            $"<w:tr><w:trPr><w:trHeight w:val=\"400\"/></w:trPr>{Cell("r4a")}{Cell("r4b")}</w:tr>" +
            "</w:tbl><w:p/>").Build();
        var document = Load(source);
        foreach (var text in new[] { "r1b", "r2a", "r3a" })
        {
            var id = document.AllParagraphs().Single(p => p.Text == text).Id;
            document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 1), "X"));
        }

        var output = Save(document);

        var before = Body(source).Element(W + "tbl")!;
        var after = Body(output).Element(W + "tbl")!;
        Assert.Equal(before.Elements().Select(e => e.Name), after.Elements().Select(e => e.Name));
        Assert.Equal("r1aX1b", after.Element(W + "customXml")!.Value);
        Assert.Equal("row", (string)after.Element(W + "customXml")!.Attribute(W + "element"));
        Assert.True(XNode.DeepEquals(before.Element(W + "tr")!.Element(W + "sdt")!.Element(W + "sdtPr"), after.Element(W + "tr")!.Element(W + "sdt")!.Element(W + "sdtPr")));
        Assert.True(XNode.DeepEquals(before.Element(W + "sdt")!.Element(W + "sdtPr"), after.Element(W + "sdt")!.Element(W + "sdtPr")));
        Assert.Equal("X2a", after.Element(W + "tr")!.Element(W + "sdt")!.Value);
        Assert.Equal("X3ar3b", after.Element(W + "sdt")!.Value);
        Assert.True(XNode.DeepEquals(before.Elements(W + "tr").Last(), after.Elements(W + "tr").Last()));
        Assert.True(XNode.DeepEquals(before.Elements(W + "tr").ElementAt(1), after.Elements(W + "tr").ElementAt(1)));
        Assert.True(XNode.DeepEquals(before.Element(W + "tr")!.Elements(W + "tc").Single(), after.Element(W + "tr")!.Elements(W + "tc").Single()));
        AssertConsistent(output);
        Assert.Equal(DocumentDescriber.Describe(document), DocumentDescriber.Describe(Load(output)));
    }

    [Fact]
    public void Writer_NewTableAndCells_WrittenFromModel()
    {
        var document = TextDocument.Create();
        var table = new Table
        {
            Format = new TableFormat { StyleId = null, Width = 6000, Alignment = TableAlignment.Center, Look = TableLook.FirstRow | TableLook.BandedRows,
                Borders = new TableBorders(new Border("single", 0.5, "auto"), null, null, null, new Border("dashed", 1, "#FF0000"), null) },
            Grid = [3000, 3000],
            Rows =
            [
                new TableRow([new TableCell([new Paragraph { Inlines = [new TextRun("h")] }]) { GridSpan = 2, Shading = "#EEEEEE", VerticalAlign = CellVerticalAlignment.Center }])
                    { IsHeader = true, Height = 500 },
                new TableRow([new TableCell([new Paragraph()]) { VMerge = VerticalMerge.Restart, Width = 3000 }]) { GridAfter = 1 }
            ]
        };
        document.BlockList.Insert(0, document.Identify(table));

        var output = Save(document);

        AssertConsistent(output);
        Assert.Equal(DocumentDescriber.Describe(document), DocumentDescriber.Describe(Load(output)));
    }

    internal static void AssertConsistent(byte[] package)
    {
        var opc = OpcPackage.Open(package);
        var parts = opc.PartNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts.Where(p => !p.EndsWith(".rels", StringComparison.Ordinal) && p != "/[Content_Types].xml"))
            Assert.True(opc.ContentType(part) != null, $"No content type for {part}");
        foreach (var name in opc.OverrideContentTypes.Keys) Assert.True(parts.Contains(name), $"Override without part {name}");
        OpcAssert.AssertNoDanglingRelationships(package);

        var main = opc.ResolveTarget("/", opc.Relationships("/").Single(r => r.Type.EndsWith("/officeDocument", StringComparison.Ordinal)).Target);
        var mainRels = opc.Relationships(main);
        var stories = mainRels.Where(r => r.TargetMode == null && Stories.Any(s => r.Type.EndsWith(s, StringComparison.Ordinal)))
            .Select(r => opc.ResolveTarget(main, r.Target)).Prepend(main).Distinct().ToList();
        var xml = stories.ToDictionary(s => s, opc.ReadXml);
        foreach (var (part, story) in xml)
        {
            var ids = opc.Relationships(part).Select(r => r.Id).ToHashSet();
            foreach (var reference in story.Descendants().Attributes().Where(a => a.Name.Namespace == R && a.Name.LocalName is "id" or "embed" or "link"))
                Assert.True(ids.Contains(reference.Value), $"{part}: {reference.Value}");
            var starts = story.Descendants(W + "bookmarkStart").Select(b => (string)b.Attribute(W + "id")).ToList();
            Assert.Equal(starts.Count, starts.Distinct().Count());
            foreach (var id in starts) Assert.Single(story.Descendants(W + "bookmarkEnd"), e => (string)e.Attribute(W + "id") == id);
            foreach (var pPr in story.Descendants(W + "pPr")) Assert.True(pPr.Elements(W + "sectPr").Count() <= 1, $"{part}: two sectPr in one pPr");
            foreach (var cell in story.Descendants(W + "tc")) Assert.Equal(W + "p", cell.Elements().Last().Name);
            foreach (var table in story.Descendants(W + "tbl"))
            {
                var columns = table.Element(W + "tblGrid")!.Elements(W + "gridCol").Count();
                foreach (var row in table.Descendants(W + "tr").Where(r => r.Ancestors(W + "tbl").First() == table))
                {
                    var spans = row.Descendants(W + "tc").Where(c => c.Ancestors(W + "tr").First() == row).Sum(c => Int(c.Element(W + "tcPr")?.Element(W + "gridSpan")) ?? 1);
                    var extra = (Int(row.Element(W + "trPr")?.Element(W + "gridBefore")) ?? 0) + (Int(row.Element(W + "trPr")?.Element(W + "gridAfter")) ?? 0);
                    Assert.True(spans + extra <= columns, $"{part}: row spans {spans + extra} of {columns} columns");
                }
            }
        }

        var paraIds = xml.Values.SelectMany(d => d.Descendants().Attributes(W14 + "paraId")).Select(a => a.Value).ToList();
        Assert.Equal(paraIds.Count, paraIds.Distinct().Count());
        var docPrIds = xml.Values.SelectMany(d => d.Descendants(Wp + "docPr")).Select(d => (string)d.Attribute("id")).ToList();
        Assert.Equal(docPrIds.Count, docPrIds.Distinct().Count());

        var body = xml[main].Root!.Element(W + "body")!;
        Assert.Equal(W + "sectPr", body.Elements().Last().Name);

        var styles = Story(opc, main, mainRels, "/styles")?.Descendants(W + "style").Select(s => (string)s.Attribute(W + "styleId")).ToHashSet() ?? [];
        var numbers = Story(opc, main, mainRels, "/numbering")?.Root!.Elements(W + "num").Select(n => (string)n.Attribute(W + "numId")).ToHashSet() ?? [];
        foreach (var story in xml.Values)
        {
            foreach (var reference in story.Descendants().Where(e => e.Name == W + "pStyle" || e.Name == W + "rStyle" || e.Name == W + "tblStyle"))
                Assert.Contains((string)reference.Attribute(W + "val"), styles);
            foreach (var numId in story.Descendants(W + "numId").Select(n => (string)n.Attribute(W + "val")).Where(v => v != "0"))
                Assert.Contains(numId, numbers);
        }
    }

    private static XDocument Story(OpcPackage opc, string main, IEnumerable<OpcRelationship> rels, string type) =>
        rels.FirstOrDefault(r => r.Type.EndsWith(type, StringComparison.Ordinal)) is { } rel ? opc.ReadXml(opc.ResolveTarget(main, rel.Target)) : null;

    private static int? Int(XElement element) => int.TryParse((string)element?.Attribute(W + "val"), out var value) ? value : null;
}
