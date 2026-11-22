using System.Text.RegularExpressions;
using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class RealFileRoundTripTests
{
    private const string BodyMark = "EditMarkBody ";
    private const string CellMark = "EditMarkCell ";
    private const string HeaderMark = "EditMarkHeader ";
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public static TheoryData<string> Files() => new(Directory.GetFiles(Folder, "*.docx").Select(Path.GetFileName).Order());

    private static string Folder => Path.Combine(AppContext.BaseDirectory, "TestData", "Document");

    [Fact]
    public void AllFeatures_Build_SaveLoad_EqualsModel_AndConsistent()
    {
        var document = DocumentAllFeatures.Build();

        var bytes = DocxWriterTests.Save(document);

        DocxWriterTests.AssertConsistent(bytes);
        var loaded = TextDocument.Load(new MemoryStream(bytes));
        Assert.Equal(WithoutMediaNames(DocumentDescriber.Describe(document)), WithoutMediaNames(DocumentDescriber.Describe(loaded)));
        Assert.Equal(2, loaded.Sections.Count);
        Assert.Equal((PageOrientation.Landscape, true, 2, 1), (loaded.Sections[1].Page.Orientation, loaded.Sections[1].TitlePage, loaded.Sections[1].Headers.Count,
            loaded.Sections[1].Footers.Count));
        Assert.Equal(["Landscape header", "Landscape footer", "First page header"], loaded.HeadersFooters.Select(h => ((Paragraph)h.Blocks[0]).Text));
        var table = loaded.Blocks.OfType<Table>().Single();
        Assert.True(table.Rows[0].IsHeader);
        Assert.Equal(2, table.Rows[1].Cells[1].GridSpan);
        Assert.Equal("#D9E2F3", table.Rows[0].Cells[0].Shading);
        Assert.IsType<Table>(table.Rows[2].Cells[0].Blocks[1]);
        var inlines = loaded.AllParagraphs().SelectMany(p => p.Inlines).ToList();
        Assert.Equal("A grey test picture", inlines.OfType<InlineImage>().Single().AltText);
        Assert.Equal("https://example.com/", inlines.Single(i => i.Link != null).Link.SafeTarget);
        Assert.Contains(inlines, i => i is Break { Kind: BreakKind.Page });
        var labels = loaded.AllParagraphs().Select(loaded.ListLabel).Where(l => l != null).ToList();
        Assert.Equal(["\u2022", "\u2022", "\u2022", "1.", "2.", "a.", "b.", "3.", "1.", "2."], labels);
        Assert.Equal((DocumentAllFeatures.Author, DocumentAllFeatures.Author), (loaded.Properties.Author, loaded.Properties.LastModifiedBy));

        if (Environment.GetEnvironmentVariable("MUDEX_WRITE_FIXTURES") == "1")
            File.WriteAllBytes(Path.Combine(Root(), "Samples", "MainSample.WebAssembly", "wwwroot", "sample-data", "document-all-features.docx"), bytes);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void RealFiles_RoundTrip_UnchangedByteIdentical_EditedConsistent(string file)
    {
        var source = File.ReadAllBytes(Path.Combine(Folder, file));

        var unchanged = DocxWriterTests.Save(TextDocument.Load(new MemoryStream(source)));
        var before = OpcAssert.Parts(source);
        var after = OpcAssert.Parts(unchanged);
        Assert.Equal(before.Keys.Order(), after.Keys.Order());
        foreach (var (part, bytes) in before) Assert.True(bytes.AsSpan().SequenceEqual(after[part]), $"{file}: {part}");

        var document = TextDocument.Load(new MemoryStream(source));
        var edited = new List<(string Id, string Mark)> { (document.Blocks.OfType<Paragraph>().First().Id, BodyMark) };
        if (document.Blocks.OfType<Table>().FirstOrDefault()?.Rows[0].Cells[0].Blocks.OfType<Paragraph>().FirstOrDefault() is { } cell) edited.Add((cell.Id, CellMark));
        if (document.HeadersFooters.FirstOrDefault(h => h.Kind == HeaderFooterKind.Header)?.Blocks.OfType<Paragraph>().FirstOrDefault() is { } header)
            edited.Add((header.Id, HeaderMark));
        foreach (var (id, mark) in edited)
        {
            var at = document.PositionOf(id, 0);
            document.Execute(new ReplaceTextCommand(new DocumentRange(at, at), mark));
        }

        var output = DocxWriterTests.Save(document);

        DocxWriterTests.AssertConsistent(output);
        var reloaded = TextDocument.Load(new MemoryStream(output));
        foreach (var (id, mark) in edited) Assert.StartsWith(mark, ((Paragraph)reloaded.FindBlock(id)).Text);
        var (sourceBody, outputBody) = (Body(source).ToList(), Body(output).ToList());
        Assert.Equal(sourceBody.Count, outputBody.Count);
        var marks = edited.Where(e => e.Mark != HeaderMark).Select(e => e.Mark.Trim()).ToList();
        for (var i = 0; i < sourceBody.Count; i++)
            if (!XNode.DeepEquals(sourceBody[i], outputBody[i]))
                Assert.True(marks.Any(m => Text(outputBody[i]).Contains(m, StringComparison.Ordinal)), $"{file}: body child {i} ({outputBody[i].Name.LocalName}) changed without an edit");
    }

    [Fact]
    public void DocumentSources_AreAsciiOnly()
    {
        var root = Root();
        var files = new[] { Path.Combine(root, "MudBlazor.Extensions", "Office", "Document"), Path.Combine(root, "MudBlazor.Extensions.Tests", "UnitTests", "Tests", "Document") }
            .SelectMany(d => Directory.GetFiles(d, "*.cs", SearchOption.AllDirectories)).ToList();

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var bytes = File.ReadAllBytes(file);
            var start = bytes.AsSpan().StartsWith((byte[])[0xEF, 0xBB, 0xBF]) ? 3 : 0;
            Assert.True(bytes.Skip(start).All(b => b < 0x80), file);
        }
    }

    [Fact]
    public void AllFormats_ExportImport_NoThrow()
    {
        var document = DocumentAllFeatures.Build();
        var expected = new Dictionary<DocumentFileFormat, DocumentFeature[]>
        {
            [DocumentFileFormat.Html] = [DocumentFeature.ParagraphFormatting, DocumentFeature.Styles, DocumentFeature.HeadersFooters, DocumentFeature.Sections],
            [DocumentFileFormat.Markdown] =
            [
                DocumentFeature.CharacterFormatting, DocumentFeature.ParagraphFormatting, DocumentFeature.Styles, DocumentFeature.Lists, DocumentFeature.Tables,
                DocumentFeature.MergedCells, DocumentFeature.NestedTables, DocumentFeature.HeadersFooters, DocumentFeature.Sections
            ],
            [DocumentFileFormat.Text] =
            [
                DocumentFeature.CharacterFormatting, DocumentFeature.ParagraphFormatting, DocumentFeature.Styles, DocumentFeature.Lists, DocumentFeature.Tables,
                DocumentFeature.MergedCells, DocumentFeature.NestedTables, DocumentFeature.Images, DocumentFeature.Links, DocumentFeature.HeadersFooters,
                DocumentFeature.Sections
            ],
            [DocumentFileFormat.Rtf] = [DocumentFeature.Lists, DocumentFeature.NestedTables, DocumentFeature.HeadersFooters, DocumentFeature.Sections]
        };

        foreach (var (format, losses) in expected)
        {
            using var stream = new MemoryStream();
            document.Save(stream, format);
            var loaded = TextDocument.Load(new MemoryStream(stream.ToArray()), format);

            Assert.Equal(losses, DocumentExport.Losses(document, format));
            var text = string.Join("\n", loaded.AllParagraphs().Select(p => p.Text));
            foreach (var expectedText in new[] { "Text formatting", "Centred paragraph.", "Step two", "Merged across two columns", "n4", "Visit example.com", "Landscape section" })
                Assert.True(text.Contains(expectedText, StringComparison.Ordinal), $"{format}: {expectedText}");
            DocxWriterTests.AssertConsistent(DocxWriterTests.Save(loaded));
        }
    }

    private static IEnumerable<XElement> Body(byte[] package)
    {
        var opc = OpcPackage.Open(package);
        var main = opc.ResolveTarget("/", opc.Relationships("/").Single(r => r.Type.EndsWith("/officeDocument", StringComparison.Ordinal)).Target);
        return opc.ReadXml(main).Root!.Element(W + "body")!.Elements();
    }

    private static string WithoutMediaNames(string description) => Regex.Replace(description, @"img \S+ ", "img ");

    private static string Text(XElement element) => string.Concat(element.Descendants(W + "t").Select(t => t.Value));

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "global.json"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("No repository root.");
    }
}
