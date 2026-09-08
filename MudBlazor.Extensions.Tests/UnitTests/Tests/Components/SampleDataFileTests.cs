using System.Text;
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
}
