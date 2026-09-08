using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Helper.Internal;

/// <summary>
/// A minimal EPUB (2 and 3) reader built on <see cref="ZipArchive"/> from the BCL. Resolves the OPF package from
/// META-INF/container.xml, reads metadata, spine order and the navigation document, and can render a single
/// chapter into a self contained html document with its stylesheets and images inlined - which is what an iframe
/// needs, since it cannot resolve relative urls back into the zip.
/// </summary>
internal sealed class EpubBook : IDisposable
{
    private static readonly Regex CssUrlRegex = new(@"url\(\s*['""]?(?<url>[^'"")]+)['""]?\s*\)", RegexOptions.Compiled);
    private static readonly Regex TitleRegex = new(@"<title[^>]*>(?<title>.*?)</title>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private readonly ZipArchive _archive;
    private readonly Dictionary<string, string> _manifest = new(StringComparer.OrdinalIgnoreCase);
    private string _opfDirectory = string.Empty;

    private EpubBook(ZipArchive archive)
    {
        _archive = archive;
    }

    /// <summary>Book title from the OPF metadata.</summary>
    public string Title { get; private set; }

    /// <summary>Author(s) from the OPF metadata.</summary>
    public string Author { get; private set; }

    /// <summary>Language code from the OPF metadata.</summary>
    public string Language { get; private set; }

    /// <summary>Cover image as data uri, if the book declares one.</summary>
    public string CoverDataUrl { get; private set; }

    /// <summary>The chapters in reading order as defined by the spine.</summary>
    public List<EpubChapter> Chapters { get; } = new();

    public static EpubBook Open(Stream stream)
    {
        var book = new EpubBook(new ZipArchive(stream, ZipArchiveMode.Read));
        book.Load();
        return book;
    }

    private void Load()
    {
        var container = ReadXml("META-INF/container.xml")
                        ?? throw new FormatException("META-INF/container.xml is missing, this is not a valid EPUB file.");

        var opfPath = container.Descendants().FirstOrDefault(e => e.Name.LocalName == "rootfile")?.Attribute("full-path")?.Value
                      ?? throw new FormatException("The EPUB container does not reference a package document.");

        _opfDirectory = GetDirectory(opfPath);
        var opf = ReadXml(opfPath) ?? throw new FormatException($"The package document '{opfPath}' is missing.");

        ReadMetadata(opf);
        ReadManifest(opf);
        ReadSpine(opf);
        ApplyNavigationTitles(opf);
    }

    private void ReadMetadata(XDocument opf)
    {
        var metadata = opf.Descendants().FirstOrDefault(e => e.Name.LocalName == "metadata");
        if (metadata == null)
            return;

        Title = Meta(metadata, "title");
        Author = string.Join(", ", metadata.Elements().Where(e => e.Name.LocalName == "creator").Select(e => e.Value.Trim()).Where(v => v.Length > 0));
        Language = Meta(metadata, "language");
    }

    private static string Meta(XElement metadata, string localName)
        => metadata.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value?.Trim();

    private void ReadManifest(XDocument opf)
    {
        string coverId = null;
        var metadata = opf.Descendants().FirstOrDefault(e => e.Name.LocalName == "metadata");
        if (metadata != null)
        {
            coverId = metadata.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "meta" && e.Attribute("name")?.Value == "cover")
                ?.Attribute("content")?.Value;
        }

        foreach (var item in opf.Descendants().Where(e => e.Name.LocalName == "item"))
        {
            var id = item.Attribute("id")?.Value;
            var href = item.Attribute("href")?.Value;
            if (id == null || href == null)
                continue;

            _manifest[id] = href;

            var isCover = id == coverId || item.Attribute("properties")?.Value?.Contains("cover-image") == true;
            if (isCover && CoverDataUrl == null)
                CoverDataUrl = DataUrl(Combine(_opfDirectory, href));
        }
    }

    private void ReadSpine(XDocument opf)
    {
        foreach (var itemRef in opf.Descendants().Where(e => e.Name.LocalName == "itemref"))
        {
            var id = itemRef.Attribute("idref")?.Value;
            if (id == null || !_manifest.TryGetValue(id, out var href))
                continue;

            var path = Combine(_opfDirectory, href);
            Chapters.Add(new EpubChapter { Id = id, Path = path, Title = TitleFromDocument(path) ?? Path.GetFileNameWithoutExtension(path) });
        }
    }

    /// <summary>
    /// The spine has no titles, so prefer the labels of the EPUB 3 nav document or the EPUB 2 ncx if present.
    /// </summary>
    private void ApplyNavigationTitles(XDocument opf)
    {
        var navHref = opf.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "item" && e.Attribute("properties")?.Value?.Contains("nav") == true)
            ?.Attribute("href")?.Value;

        var labels = navHref != null ? ReadNavLabels(Combine(_opfDirectory, navHref)) : ReadNcxLabels(opf);

        foreach (var chapter in Chapters)
        {
            // Nav entries can point at a fragment inside a chapter file - the file itself is what we can show.
            var match = labels.FirstOrDefault(l => PathsMatch(l.Key, chapter.Path));
            if (match.Value != null)
                chapter.Title = match.Value;
        }
    }

    private Dictionary<string, string> ReadNavLabels(string navPath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var nav = ReadXml(navPath);
        if (nav == null)
            return result;

        var navDirectory = GetDirectory(navPath);
        foreach (var anchor in nav.Descendants().Where(e => e.Name.LocalName == "a"))
        {
            var href = anchor.Attribute("href")?.Value;
            if (href == null)
                continue;
            result[Combine(navDirectory, href.Split('#')[0])] = anchor.Value.Trim();
        }
        return result;
    }

    private Dictionary<string, string> ReadNcxLabels(XDocument opf)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ncxId = opf.Descendants().FirstOrDefault(e => e.Name.LocalName == "spine")?.Attribute("toc")?.Value;
        if (ncxId == null || !_manifest.TryGetValue(ncxId, out var ncxHref))
            return result;

        var ncxPath = Combine(_opfDirectory, ncxHref);
        var ncx = ReadXml(ncxPath);
        if (ncx == null)
            return result;

        var ncxDirectory = GetDirectory(ncxPath);
        foreach (var navPoint in ncx.Descendants().Where(e => e.Name.LocalName == "navPoint"))
        {
            var href = navPoint.Descendants().FirstOrDefault(e => e.Name.LocalName == "content")?.Attribute("src")?.Value;
            var label = navPoint.Descendants().FirstOrDefault(e => e.Name.LocalName == "text")?.Value?.Trim();
            if (href != null && label != null)
                result[Combine(ncxDirectory, href.Split('#')[0])] = label;
        }
        return result;
    }

    /// <summary>
    /// Returns the chapter as a standalone html document: stylesheets are inlined, images and fonts referenced
    /// from the markup or from the css are replaced by data uris, so the result renders inside an iframe srcdoc.
    /// </summary>
    public string RenderChapter(EpubChapter chapter)
    {
        var html = ReadText(chapter.Path);
        if (html == null)
            return "<p>This chapter could not be read.</p>";

        var directory = GetDirectory(chapter.Path);
        html = InlineStylesheets(html, directory);
        html = InlineImages(html, directory);

        // Links would try to navigate the sandboxed iframe to a path that does not exist, so neutralize them.
        html = Regex.Replace(html, @"(<a\b[^>]*?)\shref\s*=\s*(['""])(?!https?:|mailto:)[^'""]*\2", "$1", RegexOptions.IgnoreCase);

        return html;
    }

    private string InlineStylesheets(string html, string directory)
    {
        return Regex.Replace(html, @"<link\b[^>]*?href\s*=\s*(['""])(?<href>[^'""]+)\1[^>]*?>", match =>
        {
            var tag = match.Value;
            if (!tag.Contains("stylesheet", StringComparison.OrdinalIgnoreCase))
                return tag;

            var css = ReadText(Combine(directory, match.Groups["href"].Value));
            if (css == null)
                return string.Empty;

            var cssDirectory = GetDirectory(Combine(directory, match.Groups["href"].Value));
            return $"<style>{InlineCssUrls(css, cssDirectory)}</style>";
        }, RegexOptions.IgnoreCase);
    }

    private string InlineCssUrls(string css, string directory) => CssUrlRegex.Replace(css, match =>
    {
        var url = match.Groups["url"].Value;
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return match.Value;

        var dataUrl = DataUrl(Combine(directory, url.Split('#')[0]));
        return dataUrl == null ? match.Value : $"url({dataUrl})";
    });

    private string InlineImages(string html, string directory)
    {
        // Covers <img src>, <image xlink:href> and <image href> - svg wrapped covers are common in EPUB.
        return Regex.Replace(html, @"(?<attribute>\b(?:src|xlink:href|href)\s*=\s*)(?<quote>['""])(?<url>[^'""]+)\k<quote>", match =>
        {
            var url = match.Groups["url"].Value;
            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return match.Value;

            var dataUrl = DataUrl(Combine(directory, url.Split('#')[0]));
            return dataUrl == null ? match.Value : $"{match.Groups["attribute"].Value}\"{dataUrl}\"";
        }, RegexOptions.IgnoreCase);
    }

    private string TitleFromDocument(string path)
    {
        var html = ReadText(path);
        if (html == null)
            return null;

        var title = TitleRegex.Match(html).Groups["title"].Value.Trim();
        if (title.Length > 0)
            return title;

        // Fall back to the first heading, many books leave <title> empty.
        var heading = Regex.Match(html, @"<h[1-6][^>]*>(?<text>.*?)</h[1-6]>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        title = Regex.Replace(heading.Groups["text"].Value, "<[^>]+>", string.Empty).Trim();
        return title.Length > 0 ? title : null;
    }

    private ZipArchiveEntry FindEntry(string path)
    {
        var normalized = Normalize(path);
        return _archive.Entries.FirstOrDefault(e => string.Equals(Normalize(e.FullName), normalized, StringComparison.OrdinalIgnoreCase));
    }

    private string ReadText(string path)
    {
        var entry = FindEntry(path);
        if (entry == null)
            return null;

        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        return reader.ReadToEnd();
    }

    private XDocument ReadXml(string path)
    {
        var text = ReadText(path);
        try
        {
            return text == null ? null : XDocument.Parse(text);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string DataUrl(string path)
    {
        var entry = FindEntry(path);
        if (entry == null)
            return null;

        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return $"data:{MimeTypeFor(path)};base64,{Convert.ToBase64String(ms.ToArray())}";
    }

    private static string MimeTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".webp" => "image/webp",
        ".woff" => "font/woff",
        ".woff2" => "font/woff2",
        ".ttf" => "font/ttf",
        ".otf" => "font/otf",
        _ => "application/octet-stream"
    };

    private static bool PathsMatch(string a, string b) => string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

    private static string GetDirectory(string path)
    {
        var index = path.LastIndexOf('/');
        return index < 0 ? string.Empty : path[..index];
    }

    /// <summary>Resolves a href relative to a directory inside the archive, including "../" segments.</summary>
    private static string Combine(string directory, string href)
    {
        if (href.StartsWith('/'))
            return Normalize(href.TrimStart('/'));

        var segments = new List<string>();
        if (directory.Length > 0)
            segments.AddRange(directory.Split('/', StringSplitOptions.RemoveEmptyEntries));

        foreach (var segment in href.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
                continue;
            if (segment == "..")
            {
                if (segments.Count > 0)
                    segments.RemoveAt(segments.Count - 1);
                continue;
            }
            segments.Add(segment);
        }

        return string.Join('/', segments);
    }

    private static string Normalize(string path) => Uri.UnescapeDataString(path.Replace('\\', '/')).TrimStart('/');

    public void Dispose() => _archive?.Dispose();
}

/// <summary>One entry of the EPUB spine.</summary>
internal sealed class EpubChapter
{
    /// <summary>Manifest id of the chapter.</summary>
    public string Id { get; init; }
    /// <summary>Path of the chapter document inside the archive.</summary>
    public string Path { get; init; }
    /// <summary>Display title, taken from the navigation document or the document itself.</summary>
    public string Title { get; set; }
}
