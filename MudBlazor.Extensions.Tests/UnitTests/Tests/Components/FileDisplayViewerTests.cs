using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// Covers the hand written parsers behind the file viewers that do not rely on a JS library (log, notebook,
/// vCard/iCalendar, certificate and the MIME parser used for .eml). These are the parts that break silently,
/// so every viewer gets one assertion on parsed output rather than on its layout.
/// </summary>
public class FileDisplayViewerTests
{
    private sealed record FileInfos(string FileName, string ContentType, Stream ContentStream) : IMudExFileDisplayInfos
    {
        public string Url => null;

        public static FileInfos Text(string fileName, string contentType, string content)
            => new(fileName, contentType, new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }

    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    [Fact]
    public async Task Log_DetectsAndNormalizesLevels()
    {
        const string log = """
                           2026-09-08 10:00:00 INFO  Starting up
                           2026-09-08 10:00:01 WARNING Disk almost full
                           2026-09-08 10:00:02 ERROR Something failed
                           2026-09-08 10:00:03 DEBUG Details
                           """;

        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplayLog>(parameters => parameters
            .Add(c => c.FileDisplayInfos, FileInfos.Text("app.log", "text/plain", log)));

        var meta = await cut.Instance.FileMetaInformationAsync(null);

        Assert.Equal(4, meta["Lines"]);
        Assert.Equal(1, meta["Errors"]);
        Assert.Equal(1, meta["Warnings"]); // WARNING has to be normalized to WARN

        // The filter chips are ordered by severity, not by the order the levels appear in the file
        var chipLevels = cut.FindAll(".mud-ex-log-chip")
            .Select(chip => new string(chip.TextContent.Where(char.IsLetter).ToArray()))
            .ToList();
        Assert.Equal(new[] { "ERROR", "WARN", "INFO", "DEBUG" }, chipLevels);

        // Every chip starts enabled, so none of them may carry the "off" marker yet
        Assert.DoesNotContain(cut.FindAll(".mud-ex-log-chip"), chip => chip.ClassList.Contains("mud-ex-log-chip-off"));

        // The line counter has to come out formatted, not as the raw "{0} lines" placeholder
        Assert.Equal("4 lines", cut.Find(".mud-ex-log-count").TextContent.Trim());
    }

    [Fact]
    public async Task Notebook_ParsesCellsAndKernel()
    {
        const string notebook = """
                                {
                                  "metadata": { "kernelspec": { "language": "python", "display_name": "Python 3" } },
                                  "cells": [
                                    { "cell_type": "markdown", "source": ["# Title"] },
                                    { "cell_type": "code", "execution_count": 1, "source": ["print(1)"],
                                      "outputs": [ { "output_type": "stream", "text": ["1\n"] } ] }
                                  ]
                                }
                                """;

        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplayNotebook>(parameters => parameters
            .Add(c => c.FileDisplayInfos, FileInfos.Text("demo.ipynb", "application/x-ipynb+json", notebook)));

        var meta = await cut.Instance.FileMetaInformationAsync(null);

        Assert.Equal("Python 3", meta["Kernel"]);
        Assert.Equal(2, meta["Cells"]);
        Assert.Equal(1, meta["Code cells"]);
        Assert.Equal(1, meta["Markdown cells"]);
    }

    [Fact]
    public async Task VCard_FormatsAddressComponents()
    {
        const string vcf = """
                           BEGIN:VCARD
                           VERSION:3.0
                           FN:Florian Test
                           ORG:Cargonerds
                           ADR;TYPE=WORK:;;Hauptstrasse 1;Koeln;;50667;Germany
                           EMAIL:test@example.com
                           END:VCARD
                           """;

        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplayVCard>(parameters => parameters
            .Add(c => c.FileDisplayInfos, FileInfos.Text("contact.vcf", "text/vcard", vcf)));

        // The seven ";" separated ADR components have to collapse into one readable line
        Assert.Contains("Hauptstrasse 1, Koeln, 50667, Germany", cut.Markup);
        Assert.Contains("test@example.com", cut.Markup);
    }

    [Fact]
    public async Task Calendar_UnescapesTextValues()
    {
        // "\," is an escaped comma and "\n" a line break per RFC 5545 - both must not survive verbatim
        const string ics = """
                           BEGIN:VCALENDAR
                           BEGIN:VEVENT
                           SUMMARY:Team Meeting
                           DTSTART:20260908T090000Z
                           DTEND:20260908T100000Z
                           LOCATION:Koeln\, Hauptstrasse 1
                           END:VEVENT
                           END:VCALENDAR
                           """;

        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplayVCard>(parameters => parameters
            .Add(c => c.FileDisplayInfos, FileInfos.Text("meeting.ics", "text/calendar", ics)));

        Assert.Contains("Team Meeting", cut.Markup);
        Assert.Contains("Koeln, Hauptstrasse 1", cut.Markup);
        Assert.DoesNotContain(@"Koeln\, Hauptstrasse", cut.Markup);
    }

    [Fact]
    public async Task Certificate_ReadsSubjectFromPem()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=MudEx Viewer Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var pem = new string(PemEncoding.Write("CERTIFICATE", certificate.RawData));

        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplayCertificate>(parameters => parameters
            .Add(c => c.FileDisplayInfos, FileInfos.Text("test.pem", "application/x-pem-file", pem)));

        Assert.Contains("MudEx Viewer Test", cut.Markup);
        Assert.Contains(certificate.Thumbprint, cut.Markup);
    }

    [Fact]
    public void MimeMessage_DecodesEncodedWordsAndPerPartCharsets()
    {
        var subject = $"=?UTF-8?Q?Gr=C3=BC=C3=9Fe?=";
        var from = $"=?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes("Ärger"))}?= <a@b.de>";
        var htmlBase64 = Convert.ToBase64String(Encoding.Latin1.GetBytes("<p>Grüße</p>"));

        var raw = string.Join("\r\n",
            $"From: {from}",
            $"Subject: {subject}",
            "MIME-Version: 1.0",
            "Content-Type: multipart/alternative; boundary=\"bnd\"",
            "",
            "--bnd",
            "Content-Type: text/plain; charset=\"utf-8\"",
            "Content-Transfer-Encoding: quoted-printable",
            "",
            "Sch=C3=B6ne Gr=C3=BC=C3=9Fe",
            "--bnd",
            "Content-Type: text/html; charset=\"iso-8859-1\"",
            "Content-Transfer-Encoding: base64",
            "",
            htmlBase64,
            "--bnd--");

        var message = MimeMessage.Parse(raw);

        Assert.Equal("Grüße", message.Subject);
        Assert.StartsWith("Ärger", message.From);
        Assert.Contains("Schöne Grüße", message.TextBody);
        // The html part declares latin-1, so decoding it as UTF-8 would produce replacement characters here
        Assert.Contains("Grüße", message.HtmlBody);
    }

    [Fact]
    public void CertificateReader_AgreesWithThePlatform()
    {
        // The viewer decodes certificates itself because the platform stack is unavailable on browser-wasm.
        // On the desktop both are available, so the platform is used as the reference implementation here.
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=MudEx Reader Test, O=Cargonerds, C=DE", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var alternativeNames = new SubjectAlternativeNameBuilder();
        alternativeNames.AddDnsName("mudex.local");
        alternativeNames.AddDnsName("localhost");
        request.CertificateExtensions.Add(alternativeNames.Build());
        using var expected = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));

        var pem = new string(PemEncoding.Write("CERTIFICATE", expected.RawData));
        var actual = Assert.Single(X509CertificateReader.Read(Encoding.ASCII.GetBytes(pem)));

        Assert.Equal(expected.Subject, actual.Subject);
        Assert.Equal(expected.Issuer, actual.Issuer);
        Assert.Equal(expected.SerialNumber, actual.SerialNumber);
        Assert.Equal(expected.Thumbprint, actual.Thumbprint);
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.SignatureAlgorithm.FriendlyName, actual.SignatureAlgorithm);
        Assert.Equal(expected.NotBefore, actual.NotBefore.LocalDateTime);
        Assert.Equal(expected.NotAfter, actual.NotAfter.LocalDateTime);
        Assert.Equal("RSA 2048 bit", actual.PublicKeyDescription);
        Assert.Equal(new[] { "DNS-Name=mudex.local", "DNS-Name=localhost" }, actual.SubjectAlternativeNames);
        Assert.Equal("MudEx Reader Test", actual.DisplayName);
        Assert.False(actual.IsExpired);
    }

    [Fact]
    public void CertificateReader_ReadsAWholePemChain()
    {
        var pem = new StringBuilder();
        var subjects = new[] { "CN=Leaf", "CN=Intermediate", "CN=Root" };

        foreach (var subject in subjects)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
            pem.AppendLine(new string(PemEncoding.Write("CERTIFICATE", certificate.RawData)));
        }

        // A private key in the same bundle is common and must not be mistaken for a certificate
        using var extraKey = RSA.Create(2048);
        pem.AppendLine(new string(PemEncoding.Write("PRIVATE KEY", extraKey.ExportPkcs8PrivateKey())));

        var certificates = X509CertificateReader.Read(Encoding.ASCII.GetBytes(pem.ToString()));

        Assert.Equal(3, certificates.Count);
        Assert.Equal(new[] { "Leaf", "Intermediate", "Root" }, certificates.Select(c => c.DisplayName));
    }

    [Fact]
    public void CertificateReader_ReadsRawDer()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Der Only", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var expected = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));

        var actual = Assert.Single(X509CertificateReader.Read(expected.RawData));

        Assert.Equal(expected.Thumbprint, actual.Thumbprint);
        Assert.Equal("Der Only", actual.DisplayName);
    }

    [Fact]
    public void LineEndings_CollapseEveryVariant()
    {
        // "\r\r\n" is what a tool leaves behind when it writes CRLF into content that already ended in CR.
        // A plain Replace("\r\n", "\n") turns it into "\r\n" again and every later line comparison fails.
        Assert.Equal("a\nb\nc\nd", "a\r\nb\r\r\nc\rd".NormalizeLineEndings());
        Assert.Equal(new[] { "a", "b", "" }, "a\r\r\nb\r\r\n".SplitLines());
    }

    [Fact]
    public async Task VCard_SurvivesDoubledCarriageReturns()
    {
        // Same content as the readable test above, written with the doubled carriage returns that broke the view
        var vcf = string.Join("\r\r\n",
            "BEGIN:VCARD",
            "VERSION:3.0",
            "FN:Ada Lovelace",
            "ADR:;;1 Babbage Street;London;;;UK",
            "EMAIL:ada@example.com",
            "END:VCARD") + "\r\r\n";

        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplayVCard>(parameters => parameters
            .Add(c => c.FileDisplayInfos, FileInfos.Text("contact.vcf", "text/vcard", vcf)));

        var meta = await cut.Instance.FileMetaInformationAsync(null);

        Assert.Equal(1, meta["Contacts"]);
        Assert.Contains("Ada Lovelace", cut.Markup);
        Assert.Contains("1 Babbage Street, London, UK", cut.Markup);
    }

    [Fact]
    public void MimeMessage_FindsBodyDespiteDoubledCarriageReturns()
    {
        var raw = string.Join("\r\r\n",
            "From: jane@example.com",
            "Subject: Hello",
            "MIME-Version: 1.0",
            "Content-Type: multipart/alternative; boundary=\"bnd\"",
            "",
            "--bnd",
            "Content-Type: text/plain; charset=UTF-8",
            "Content-Transfer-Encoding: 7bit",
            "",
            "Plain body text.",
            "--bnd",
            "Content-Type: text/html; charset=UTF-8",
            "Content-Transfer-Encoding: 7bit",
            "",
            "<p>Html body text.</p>",
            "--bnd--") + "\r\r\n";

        var message = MimeMessage.Parse(raw);

        // Before the fix the blank separator line never matched, so everything was headers and the body was empty
        Assert.Equal("Hello", message.Subject);
        Assert.Contains("Plain body text.", message.TextBody);
        Assert.Contains("Html body text.", message.HtmlBody);
    }

    [Fact]
    public async Task Subtitle_ParsesWebVttWithoutIndexLines()
    {
        var vtt = string.Join("\n",
            "WEBVTT",
            "",
            "NOTE",
            "A comment block that has to be skipped.",
            "",
            "00:00.000 --> 00:03.500 line:90%",
            "First cue, <b>tags</b> stripped.",
            "",
            "01:02.250 --> 01:05.000",
            "Second cue.");

        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplaySubtitle>(parameters => parameters
            .Add(c => c.FileDisplayInfos, FileInfos.Text("movie.vtt", "text/vtt", vtt)));

        var meta = await cut.Instance.FileMetaInformationAsync(null);

        Assert.Equal(2, meta["Cues"]);
        Assert.Equal("00:01:02", meta["Duration"]); // the hour part is optional in WebVTT timecodes
        Assert.Contains("First cue, tags stripped.", cut.Markup);
    }

    [Fact]
    public async Task Subtitle_ParsesSubStationAlphaDialogue()
    {
        var ass = string.Join("\n",
            "[Events]",
            "Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text",
            @"Dialogue: 0,0:00:05.00,0:00:08.00,Default,,0,0,0,,{\i1}Text with, a comma{\i0}",
            "Dialogue: 0,0:00:09.00,0:00:12.00,Default,,0,0,0,,Second\\Nline");

        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplaySubtitle>(parameters => parameters
            .Add(c => c.FileDisplayInfos, FileInfos.Text("movie.ass", "text/plain", ass)));

        var meta = await cut.Instance.FileMetaInformationAsync(null);

        Assert.Equal(2, meta["Cues"]);
        // The comma inside the text must survive, and the override tags must not
        Assert.Contains("Text with, a comma", cut.Markup);
        Assert.DoesNotContain(@"{\i1}", cut.Markup);
    }

    [Fact]
    public void Diff_SplitsFilesAndCountsChanges()
    {
        var patch = string.Join("\n",
            "diff --git a/src/Calculator.cs b/src/Calculator.cs",
            "index 3f8a1b2..9c4d5e6 100644",
            "--- a/src/Calculator.cs",
            "+++ b/src/Calculator.cs",
            "@@ -12,4 +12,6 @@ public class Calculator",
            " public int Divide(int a, int b)",
            " {",
            "-    return a / b;",
            "+    if (b == 0)",
            "+        throw new DivideByZeroException();",
            "+    return a / b;",
            " }",
            "diff --git a/docs/NEW.md b/docs/NEW.md",
            "new file mode 100644",
            "--- /dev/null",
            "+++ b/docs/NEW.md",
            "@@ -0,0 +1,2 @@",
            "+# New",
            "+Created by this patch");

        var files = MudExFileDisplayDiff.Parse(patch);

        Assert.Equal(2, files.Count);
        Assert.Equal("src/Calculator.cs", files[0].Path);
        Assert.Equal(3, files[0].Additions);
        Assert.Equal(1, files[0].Deletions);
        Assert.True(files[1].IsNew);
        Assert.Equal("docs/NEW.md", files[1].Path);

        // Line numbers have to advance independently for the old and the new side
        var context = files[0].Hunks[0].Lines.First(l => l.Kind == MudExFileDisplayDiff.DiffLineKind.Context);
        Assert.Equal(12, context.OldNumber);
        Assert.Equal(12, context.NewNumber);
    }

    [Fact]
    public async Task Har_ReadsEntriesAndFailureCount()
    {
        const string har = """
                           {
                             "log": {
                               "version": "1.2",
                               "creator": { "name": "Test", "version": "1.0" },
                               "entries": [
                                 {
                                   "startedDateTime": "2026-09-08T08:15:02.100Z", "time": 210.4,
                                   "request": { "method": "GET", "url": "https://example.com/api/items?page=1", "headers": [], "queryString": [{"name":"page","value":"1"}] },
                                   "response": { "status": 200, "statusText": "OK", "headers": [], "content": { "size": 1024, "mimeType": "application/json" } },
                                   "timings": { "blocked": 1.2, "dns": -1, "wait": 200.0, "receive": 9.2 }
                                 },
                                 {
                                   "startedDateTime": "2026-09-08T08:15:03.100Z", "time": 88.0,
                                   "request": { "method": "POST", "url": "https://example.com/api/items", "headers": [], "queryString": [] },
                                   "response": { "status": 500, "statusText": "Internal Server Error", "headers": [], "content": { "size": 64, "mimeType": "application/json" } },
                                   "timings": { "blocked": 0.4, "wait": 80.0, "receive": 7.6 }
                                 }
                               ]
                             }
                           }
                           """;

        await using var context = CreateContext();
        var cut = context.Render<MudExFileDisplayHar>(parameters => parameters
            .Add(c => c.FileDisplayInfos, FileInfos.Text("capture.har", "application/har+json", har)));

        var meta = await cut.Instance.FileMetaInformationAsync(null);

        Assert.Equal(2, meta["Requests"]);
        Assert.Equal(1, meta["Failed"]);
        Assert.Equal("Test 1.0", meta["Creator"]);
        Assert.Contains("/api/items?page=1", cut.Markup);
    }

    [Fact]
    public void GeoJson_ConvertsGpxTrackAndWaypoints()
    {
        const string gpx = """
                           <?xml version="1.0" encoding="UTF-8"?>
                           <gpx version="1.1" xmlns="http://www.topografix.com/GPX/1/1">
                             <wpt lat="50.9413" lon="6.9583"><name>Start</name><desc>Bridge</desc></wpt>
                             <trk>
                               <name>Walk</name>
                               <trkseg>
                                 <trkpt lat="50.9413" lon="6.9583"/>
                                 <trkpt lat="50.9384" lon="6.9601"/>
                                 <trkpt lat="50.9310" lon="6.9628"/>
                               </trkseg>
                             </trk>
                           </gpx>
                           """;

        var geoJson = GeoJsonConverter.ToGeoJson("track.gpx", gpx);
        using var doc = JsonDocument.Parse(geoJson);
        var features = doc.RootElement.GetProperty("features");

        Assert.Equal("FeatureCollection", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal(2, features.GetArrayLength());

        var point = features[0].GetProperty("geometry");
        Assert.Equal("Point", point.GetProperty("type").GetString());
        // GeoJSON is longitude first, GPX is latitude first - the order must be swapped
        Assert.Equal(6.9583, point.GetProperty("coordinates")[0].GetDouble(), 4);
        Assert.Equal(50.9413, point.GetProperty("coordinates")[1].GetDouble(), 4);

        var line = features[1].GetProperty("geometry");
        Assert.Equal("LineString", line.GetProperty("type").GetString());
        Assert.Equal(3, line.GetProperty("coordinates").GetArrayLength());
    }

    [Fact]
    public void GeoJson_ConvertsKmlPlacemarks()
    {
        const string kml = """
                           <?xml version="1.0" encoding="UTF-8"?>
                           <kml xmlns="http://www.opengis.net/kml/2.2">
                             <Document>
                               <Placemark>
                                 <name>Office</name>
                                 <Point><coordinates>6.9603,50.9375,0</coordinates></Point>
                               </Placemark>
                               <Placemark>
                                 <name>Route</name>
                                 <LineString><coordinates>
                                   6.9603,50.9375,0
                                   9.9937,53.5511,0
                                 </coordinates></LineString>
                               </Placemark>
                             </Document>
                           </kml>
                           """;

        var geoJson = GeoJsonConverter.ToGeoJson("places.kml", kml);
        using var doc = JsonDocument.Parse(geoJson);
        var features = doc.RootElement.GetProperty("features");

        Assert.Equal(2, features.GetArrayLength());
        Assert.Equal("Office", features[0].GetProperty("properties").GetProperty("name").GetString());
        Assert.Equal("Point", features[0].GetProperty("geometry").GetProperty("type").GetString());
        Assert.Equal("LineString", features[1].GetProperty("geometry").GetProperty("type").GetString());
    }

    [Fact]
    public void Epub_ReadsSpineAndInlinesResources()
    {
        using var book = EpubBook.Open(BuildEpub());

        Assert.Equal("Test Book", book.Title);
        Assert.Equal("Some Author", book.Author);
        Assert.Equal(2, book.Chapters.Count);
        Assert.Equal("First chapter", book.Chapters[0].Title); // from the nav document, not the file name

        var html = book.RenderChapter(book.Chapters[0]);
        Assert.Contains("<style>", html);                        // the linked stylesheet is inlined
        Assert.Contains("data:image/png;base64,", html);          // the relative image became a data uri
        Assert.DoesNotContain("../styles/book.css", html);
    }

    [Fact]
    public void Pptx_RendersShapePositionsInContainerUnits()
    {
        using var presentation = PptxPresentation.Open(BuildPptx());

        Assert.Single(presentation.Slides);
        Assert.Equal(12192000, presentation.SlideWidth);
        Assert.Equal("Hello slide", presentation.Slides[0].Title);

        var html = presentation.Slides[0].Html;
        Assert.Contains("Hello slide", html);
        // 1219200 EMU is exactly a tenth of the slide width, so it has to become 10 cqw
        Assert.Contains("left:10.000cqw", html);
        Assert.Contains("width:50.000cqw", html);
    }

    private static MemoryStream BuildEpub()
    {
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==");

        return BuildZip(zip =>
        {
            zip["META-INF/container.xml"] = """
                <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
                  <rootfiles><rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/></rootfiles>
                </container>
                """;
            zip["OEBPS/content.opf"] = """
                <package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="id">
                  <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
                    <dc:title>Test Book</dc:title>
                    <dc:creator>Some Author</dc:creator>
                    <dc:language>en</dc:language>
                  </metadata>
                  <manifest>
                    <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav"/>
                    <item id="css" href="styles/book.css" media-type="text/css"/>
                    <item id="img" href="images/pixel.png" media-type="image/png"/>
                    <item id="c1" href="text/c1.xhtml" media-type="application/xhtml+xml"/>
                    <item id="c2" href="text/c2.xhtml" media-type="application/xhtml+xml"/>
                  </manifest>
                  <spine><itemref idref="c1"/><itemref idref="c2"/></spine>
                </package>
                """;
            zip["OEBPS/nav.xhtml"] = """
                <html xmlns="http://www.w3.org/1999/xhtml"><head><title>Contents</title></head><body>
                  <nav><ol>
                    <li><a href="text/c1.xhtml">First chapter</a></li>
                    <li><a href="text/c2.xhtml">Second chapter</a></li>
                  </ol></nav>
                </body></html>
                """;
            zip["OEBPS/styles/book.css"] = "body { color: #123456; }";
            zip["OEBPS/text/c1.xhtml"] = """
                <html xmlns="http://www.w3.org/1999/xhtml"><head><title>Chapter one</title>
                <link rel="stylesheet" type="text/css" href="../styles/book.css" /></head>
                <body><p>Body</p><img src="../images/pixel.png" alt="pixel" /></body></html>
                """;
            zip["OEBPS/text/c2.xhtml"] = """
                <html xmlns="http://www.w3.org/1999/xhtml"><head><title>Chapter two</title></head><body><p>Body</p></body></html>
                """;
        }, ("OEBPS/images/pixel.png", png));
    }

    private static MemoryStream BuildPptx() => BuildZip(zip =>
    {
        zip["ppt/presentation.xml"] = """
            <p:presentation xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"
                            xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <p:sldIdLst><p:sldId id="256" r:id="rId1"/></p:sldIdLst>
              <p:sldSz cx="12192000" cy="6858000"/>
            </p:presentation>
            """;
        zip["ppt/_rels/presentation.xml.rels"] = """
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide" Target="slides/slide1.xml"/>
            </Relationships>
            """;
        zip["ppt/slides/slide1.xml"] = """
            <p:sld xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"
                   xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
              <p:cSld><p:spTree>
                <p:sp>
                  <p:nvSpPr><p:nvPr><p:ph type="title"/></p:nvPr></p:nvSpPr>
                  <p:spPr><a:xfrm><a:off x="1219200" y="609600"/><a:ext cx="6096000" cy="1219200"/></a:xfrm></p:spPr>
                  <p:txBody><a:bodyPr anchor="ctr"/><a:p><a:r><a:rPr sz="4400" b="1"/><a:t>Hello slide</a:t></a:r></a:p></p:txBody>
                </p:sp>
              </p:spTree></p:cSld>
            </p:sld>
            """;
    });

    private static MemoryStream BuildZip(Action<Dictionary<string, string>> textEntries, params (string Path, byte[] Bytes)[] binaryEntries)
    {
        var entries = new Dictionary<string, string>();
        textEntries(entries);

        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var (path, content) in entries)
            {
                using var writer = new StreamWriter(archive.CreateEntry(path).Open());
                writer.Write(content);
            }
            foreach (var (path, bytes) in binaryEntries)
            {
                using var entryStream = archive.CreateEntry(path).Open();
                entryStream.Write(bytes);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
