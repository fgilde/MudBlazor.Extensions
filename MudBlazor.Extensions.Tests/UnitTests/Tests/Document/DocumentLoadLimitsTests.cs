using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class DocumentLoadLimitsTests
{
    public static TheoryData<string, DocumentFileFormat> Files() => new()
    {
        { "txt", DocumentFileFormat.Text }, { "md", DocumentFileFormat.Markdown }, { "html", DocumentFileFormat.Html }, { "rtf", DocumentFileFormat.Rtf },
        { "document-all-features.docx", DocumentFileFormat.Docx }, { "sample.odt", DocumentFileFormat.Odt }, { "Rich.doc", DocumentFileFormat.Doc }
    };

    [Theory]
    [MemberData(nameof(Files))]
    public void BlocksAndInlines_JustUnderLoads_JustOverIsInvalidData(string file, DocumentFileFormat format)
    {
        var bytes = Bytes(file);
        var paragraphs = Identified(TextDocument.Load(new MemoryStream(bytes), format)).ToList();
        var (blocks, inlines) = (paragraphs.Count, paragraphs.OfType<Paragraph>().Sum(p => (long)p.Inlines.Count));

        Load(bytes, format, DocumentLoadLimits.Desktop with { Blocks = blocks, Inlines = inlines });
        Assert.Throws<InvalidDataException>(() => Load(bytes, format, DocumentLoadLimits.Desktop with { Blocks = blocks - 1 }));
        Assert.Throws<InvalidDataException>(() => Load(bytes, format, DocumentLoadLimits.Desktop with { Inlines = inlines - 1 }));
    }

    [Theory]
    [InlineData("txt", DocumentFileFormat.Text)]
    [InlineData("md", DocumentFileFormat.Markdown)]
    [InlineData("html", DocumentFileFormat.Html)]
    [InlineData("rtf", DocumentFileFormat.Rtf)]
    [InlineData("Rich.doc", DocumentFileFormat.Doc)]
    public void TextCharacters_JustUnderLoads_JustOverIsInvalidData(string file, DocumentFileFormat format)
    {
        var bytes = Bytes(file);
        var characters = format switch
        {
            DocumentFileFormat.Doc => BinaryPrimitives.ReadInt32LittleEndian(WordDocument(bytes).AsSpan(0x4C)),
            _ => Encoding.UTF8.GetCharCount(bytes)
        };
        DocumentLoadLimits Limits(int value) => DocumentLoadLimits.Desktop with { TextCharacters = value, RtfCharacters = value };

        Load(bytes, format, Limits(characters));
        Assert.Throws<InvalidDataException>(() => Load(bytes, format, Limits(characters - 1)));
    }

    [Fact]
    public void Packages_SizeAndElementBudgets_AndLyingEntryHeaders()
    {
        var docx = Bytes("document-all-features.docx");
        using var zip = new ZipArchive(new MemoryStream(docx));
        var total = zip.Entries.Sum(e => e.Length);

        Load(docx, DocumentFileFormat.Docx, DocumentLoadLimits.Desktop with { PackageBytes = total });
        Assert.Throws<InvalidDataException>(() => Load(docx, DocumentFileFormat.Docx, DocumentLoadLimits.Desktop with { PackageBytes = total - 1 }));
        Assert.Throws<InvalidDataException>(() => Load(docx, DocumentFileFormat.Docx, DocumentLoadLimits.Desktop with { DocxElements = 100 }));
        Assert.Throws<InvalidDataException>(() => Load(Bytes("sample.odt"), DocumentFileFormat.Odt, DocumentLoadLimits.Desktop with { TextCharacters = 100 }));
        Assert.Throws<InvalidDataException>(() => Load(Lying(docx, "word/document.xml"), DocumentFileFormat.Docx, DocumentLoadLimits.Desktop));
        Assert.Equal(DocumentLoadLimits.Desktop, DocumentLoadLimits.Current);
        Assert.True(DocumentLoadLimits.Browser.RtfCharacters < DocumentLoadLimits.Browser.TextCharacters);
    }

    private static IEnumerable<Block> Identified(TextDocument document) =>
        document.Blocks.Concat(document.HeadersFooters.SelectMany(h => h.Blocks)).SelectMany(Subtree);

    private static IEnumerable<Block> Subtree(Block block) => block switch
    {
        Table table => table.Rows.SelectMany(r => r.Cells).SelectMany(c => c.Blocks).SelectMany(Subtree).Prepend(block),
        OpaqueBlock { Preview: { } preview } => preview.SelectMany(Subtree).Prepend(block),
        _ => [block]
    };

    private static void Load(byte[] bytes, DocumentFileFormat format, DocumentLoadLimits limits)
    {
        using (DocumentLoadLimits.Use(limits)) TextDocument.Load(new MemoryStream(bytes), format);
    }

    private static byte[] Bytes(string file) => file switch
    {
        "txt" => Encoding.UTF8.GetBytes("first line\nsecond\tline\nthird"),
        "md" => Encoding.UTF8.GetBytes("# Head\n\nSome **bold** text\n\n- one\n- two\n"),
        "html" => Encoding.UTF8.GetBytes("<h1>Head</h1><p>Some <b>bold</b> text</p><table><tr><td>a</td><td>b</td></tr></table>"),
        "rtf" => Encoding.ASCII.GetBytes(@"{\rtf1\ansi {\b Head}\par Some {\i italic} text\par last}"),
        _ => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Document", file))
    };

    private static byte[] WordDocument(byte[] doc) => MudBlazor.Extensions.Office.CompoundFile.Open(doc).ReadStream("WordDocument");

    private static byte[] Lying(byte[] package, string entryName)
    {
        var bytes = (byte[])package.Clone();
        var name = Encoding.ASCII.GetBytes(entryName);
        for (var at = 0; at + 46 < bytes.Length; at++)
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at)) == 0x02014B50 &&
                bytes.AsSpan(at + 46, Math.Min(name.Length, bytes.Length - at - 46)).SequenceEqual(name))
            {
                var size = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at + 24));
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 24), size - 10);
                return bytes;
            }

        throw new InvalidOperationException("No central directory entry " + entryName);
    }
}
