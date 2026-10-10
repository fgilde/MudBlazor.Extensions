using System.Buffers.Binary;
using System.Text;
using System.IO.Compression;
using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Checks the sample files that ship with the WebAssembly sample against the viewers that render them. The
/// parsers have their own tests on inline content; this one exists because a sample file can be regenerated with
/// broken line endings or truncated content and then every viewer looks empty in the demo without any test failing.
/// </summary>
public class SampleDataFileTests
{
    private sealed record FileInfos(string FileName, string ContentType, Stream ContentStream) : IMudExFileDisplayInfos
    {
        public string Url => null;
    }

    // Walk up from the test output directory until the repository root shows up, so the test does not depend
    // on the exact build output layout.
    private static readonly string SampleDirectory = FindSampleDirectory();

    private static string FindSampleDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "Samples", "MainSample.WebAssembly", "wwwroot", "sample-data");
            if (Directory.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("sample-data directory not found above " + AppContext.BaseDirectory);
    }

    // A MemoryStream completes its copy synchronously; a FileStream would finish after bUnit is done rendering.
    private static Stream Load(string name) => new MemoryStream(File.ReadAllBytes(Path.Combine(SampleDirectory, name)));

    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    [Theory]
    [InlineData("sample.vcf")]
    [InlineData("sample.ics")]
    [InlineData("sample.eml")]
    [InlineData("sample.log")]
    [InlineData("sample.srt")]
    [InlineData("sample.vtt")]
    [InlineData("sample.patch")]
    public void TextSamples_HaveNoDoubledCarriageReturns(string name)
    {
        var bytes = File.ReadAllBytes(Path.Combine(SampleDirectory, name));

        // "\r\r\n" is what a CRLF write into already CRLF terminated content leaves behind. The viewers tolerate
        // it now, but a sample shipping it means the file was written through a broken pipeline.
        for (var i = 0; i < bytes.Length - 1; i++)
            Assert.False(bytes[i] == 13 && bytes[i + 1] == 13, $"{name} contains a doubled carriage return at byte {i}");
    }

    [Fact]
    public async Task SampleVcf_RendersBothContacts()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplayVCard>(parameters => parameters
            .Add(c => c.FileDisplayInfos, new FileInfos("sample.vcf", "text/vcard", Load("sample.vcf"))));

        var meta = await cut.Instance.FileMetaInformationAsync(null);

        Assert.Equal(2, meta["Contacts"]);
        Assert.Contains("Ada Lovelace", cut.Markup);
        Assert.Contains("Grace Hopper", cut.Markup);
        Assert.Contains("1 Babbage Street, London, UK", cut.Markup);
    }

    [Fact]
    public async Task SampleIcs_RendersBothEvents()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplayVCard>(parameters => parameters
            .Add(c => c.FileDisplayInfos, new FileInfos("sample.ics", "text/calendar", Load("sample.ics"))));

        var meta = await cut.Instance.FileMetaInformationAsync(null);

        Assert.Equal(2, meta["Events"]);
        Assert.Contains("Sprint Planning", cut.Markup);
        Assert.Contains("Room 42", cut.Markup);
    }

    [Fact]
    public void SampleEml_HasHeadersAndBothBodies()
    {
        var message = MimeMessage.Parse(Encoding.Latin1.GetString(File.ReadAllBytes(Path.Combine(SampleDirectory, "sample.eml"))));

        Assert.Equal("Welcome to MudBlazor.Extensions", message.Subject);
        Assert.Contains("jane.doe@example.com", message.From);
        Assert.Contains("plain text sample", message.TextBody);
        Assert.Contains("<strong>HTML</strong>", message.HtmlBody);
    }

    [Fact]
    public void SampleEpub_InlinesCoverAndChapterImage()
    {
        using var book = EpubBook.Open(Load("sample.epub"));

        Assert.Equal("MudEx EPUB Sample", book.Title);
        Assert.Equal(3, book.Chapters.Count);
        Assert.StartsWith("data:image/png;base64,", book.CoverDataUrl);

        // The chapters live in OEBPS/text/, the image in OEBPS/images/ - the href has to step out of the
        // chapter directory or it silently resolves to nothing and the reader shows a broken image.
        var html = book.RenderChapter(book.Chapters[1]);
        Assert.Contains("data:image/png;base64,", html);
        Assert.Contains("<style>", html);

        // Assert on the tags, not on the bare paths: this chapter's own prose names both files as examples,
        // so a plain substring check would find them in the text and prove nothing.
        Assert.DoesNotContain("src=\"../images/logo.png\"", html);
        Assert.DoesNotContain("<link", html);
    }

    [Fact]
    public async Task SamplePem_RendersCertificateAndSubjectAlternativeNames()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplayCertificate>(parameters => parameters
            .Add(c => c.FileDisplayInfos, new FileInfos("sample.pem", "application/x-pem-file", Load("sample.pem"))));

        var meta = await cut.Instance.FileMetaInformationAsync(null);

        Assert.Equal(1, meta["Certificates"]);
        Assert.Contains("mudex.demo.local", cut.Markup);
        Assert.Contains("DNS-Name=localhost", cut.Markup);
    }

    [Fact]
    public void NewImageSamples_AreValidAndSvgzInflates()
    {
        // decoding happens in the browser (MudExImageCodecs.js), so only the headers are checked here
        Assert.Equal(96, BinaryPrimitives.ReadUInt16LittleEndian(File.ReadAllBytes(Path.Combine(SampleDirectory, "sample.tga")).AsSpan(12)));
        Assert.Equal("qoif", Encoding.ASCII.GetString(File.ReadAllBytes(Path.Combine(SampleDirectory, "sample.qoi")), 0, 4));
        foreach (var (name, magic) in new[] { ("sample.pbm", "P1"), ("sample.pgm", "P5"), ("sample.ppm", "P6") })
            Assert.Equal(magic, Encoding.ASCII.GetString(File.ReadAllBytes(Path.Combine(SampleDirectory, name)), 0, 2));

        using var compressed = File.OpenRead(Path.Combine(SampleDirectory, "sample.svgz"));
        using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        Assert.Contains("<svg", reader.ReadToEnd());
    }

    [Fact]
    public void SampleMhtml_ResolvesItsEmbeddedImage()
    {
        var raw = Encoding.Latin1.GetString(File.ReadAllBytes(Path.Combine(SampleDirectory, "sample.mhtml")));
        var message = MimeMessage.Parse(raw);
        var html = message.ResolveCidImages(message.HtmlBody);

        Assert.Contains("MHTML sample", html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.DoesNotContain("cid:mudex-logo", html);
    }

    [Fact]
    public void ComicAndPackageSamples_ContainRenderableContent()
    {
        using (var comic = new ZipArchive(Load("sample.cbz"), ZipArchiveMode.Read))
            Assert.Equal(3, comic.Entries.Count(e => e.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)));

        var cbrBytes = File.ReadAllBytes(Path.Combine(SampleDirectory, "sample.cbr"));
        Assert.Equal("Rar!", Encoding.ASCII.GetString(cbrBytes, 0, 4));

        foreach (var name in new[] { "sample.odt", "sample.ods", "sample.odp", "sample.odg" })
        {
            using var document = OpenDocumentFile.Open(File.ReadAllBytes(Path.Combine(SampleDirectory, name)));
            Assert.NotEmpty(document.Pages);
            Assert.Contains('<', string.Join(' ', document.Pages.Select(p => p.Html)));
        }

        foreach (var name in new[] { "sample.xps", "sample.oxps" })
        {
            using var document = XpsFile.Open(File.ReadAllBytes(Path.Combine(SampleDirectory, name)));
            Assert.Single(document.Pages);
            Assert.Contains("MudEx XPS sample", document.Pages[0].Svg);
        }
    }

    [Fact]
    public void DicomAndBinaryDataSamples_HaveValidStructure()
    {
        var dicom = DicomFile.Read(File.ReadAllBytes(Path.Combine(SampleDirectory, "sample.dcm")));
        Assert.Equal(128, dicom.Columns);
        Assert.Equal(96, dicom.Rows);
        var png = dicom.RenderPng();
        Assert.Equal(128, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)));
        Assert.Equal(96, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));

        foreach (var name in new[] { "sample.psd", "sample.psb" })
            Assert.Equal("8BPS", Encoding.ASCII.GetString(File.ReadAllBytes(Path.Combine(SampleDirectory, name)), 0, 4));
        Assert.Contains("ftypavif", Encoding.ASCII.GetString(File.ReadAllBytes(Path.Combine(SampleDirectory, "sample.avif")), 0, 32));
        foreach (var name in new[] { "sample.heic", "sample.heif" })
            Assert.Contains("heic", Encoding.ASCII.GetString(File.ReadAllBytes(Path.Combine(SampleDirectory, name)), 0, 32));
        foreach (var name in new[] { "sample.parquet", "sample.arrow", "sample.feather" })
            Assert.True(new FileInfo(Path.Combine(SampleDirectory, name)).Length > 100, name);
    }
}
