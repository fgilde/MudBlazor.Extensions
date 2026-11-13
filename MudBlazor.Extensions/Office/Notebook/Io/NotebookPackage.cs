using System.IO.Compression;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Office.Notebook;

internal static class NotebookMediaTypes
{
    private static readonly Dictionary<string, string> ByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["png"] = "image/png", ["jpg"] = "image/jpeg", ["gif"] = "image/gif", ["webp"] = "image/webp", ["bmp"] = "image/bmp", ["svg"] = "image/svg+xml"
    };

    public static string ExtensionOf(string contentType) => ByExtension.FirstOrDefault(e => string.Equals(e.Value, contentType, StringComparison.OrdinalIgnoreCase)).Key;

    public static string Normalize(string contentType) => ExtensionOf(contentType) is { } extension ? ByExtension[extension] : null;

    public static string TypeOf(string extension) => ByExtension.GetValueOrDefault(extension);
}

/// <summary>
/// A read-only view of a notebook zip. Entry names are zip names without leading slash and compare ignoring case, as the writer does. Every entry is
/// inflated once while opening: the bytes actually produced count against the limits and must match the declared size and checksum. The manifest and
/// page JSON are kept; a corrupt page entry is reported through <see cref="Problem"/> instead of failing the whole file.
/// </summary>
internal sealed class NotebookPackage
{
    public const string ManifestEntry = "notebook.json";
    public const string PagesFolder = "pages/";
    public const string MediaFolder = "media/";

    private readonly Dictionary<string, ZipArchiveEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, byte[]> _json = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _problems = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _media = new(StringComparer.OrdinalIgnoreCase);

    private NotebookPackage(Stream source, NotebookLoadOptions options)
    {
        var zip = new ZipArchive(source, ZipArchiveMode.Read);
        var buffer = new byte[81920];
        long total = 0;
        foreach (var entry in zip.Entries)
        {
            var name = entry.FullName;
            if (!IsSafe(name)) throw new InvalidDataException($"The notebook contains the unsafe entry path {name}.");
            if (name.EndsWith('/')) continue;
            if (!_entries.TryAdd(name, entry)) throw new InvalidDataException($"The notebook contains the entry {name} twice (names are compared ignoring case).");
            if (MediaId(name) is { } id && !_media.TryAdd(id, name)) throw new InvalidDataException($"The notebook contains the medium {id} twice.");

            var json = IsPage(name) || name.Equals(ManifestEntry, StringComparison.OrdinalIgnoreCase) ? new MemoryStream() : null;
            var (length, problem) = Inflate(entry, Math.Min(options.MaxEntryBytes, options.MaxTotalBytes - total), json, buffer);
            if (length > options.MaxEntryBytes) throw new InvalidDataException($"The notebook entry {name} is larger than {options.MaxEntryBytes} bytes.");
            if ((total += length) > options.MaxTotalBytes) throw new InvalidDataException($"The notebook is larger than {options.MaxTotalBytes} bytes unpacked.");
            if (problem == null)
            {
                if (json != null) _json[name] = json.ToArray();
                continue;
            }

            if (!IsPage(name)) throw new InvalidDataException($"The notebook entry {name} is corrupt: {problem}");
            _entries.Remove(name);
            _problems[name] = $"The page file is corrupt: {problem}";
        }
    }

    public IReadOnlyCollection<string> Names => _entries.Keys;

    public IReadOnlyCollection<string> MediaIds => _media.Keys;

    public static NotebookPackage Open(Stream stream, NotebookLoadOptions options)
    {
        var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = stream.Read(chunk)) > 0)
        {
            if (buffer.Length + read > options.MaxTotalBytes) throw new InvalidDataException($"The notebook file is larger than {options.MaxTotalBytes} bytes.");
            buffer.Write(chunk, 0, read);
        }

        buffer.Position = 0;
        return new NotebookPackage(buffer, options);
    }

    public static string PageEntry(string pageId) => PagesFolder + pageId + ".json";

    public static string MediaEntryName(string mediaId, string extension) => MediaFolder + mediaId + "." + extension;

    public ZipArchiveEntry Entry(string name) => _entries.GetValueOrDefault(name);

    public string Problem(string name) => _problems.GetValueOrDefault(name);

    public string MediaEntry(string mediaId) => _media.GetValueOrDefault(mediaId);

    public string MediaType(string mediaId) => MediaEntry(mediaId) is { } name ? NotebookMediaTypes.TypeOf(name[(name.LastIndexOf('.') + 1)..]) : null;

    public static bool IsMedia(string name) => MediaId(name) != null;

    public byte[] Read(string name)
    {
        if (_json.TryGetValue(name, out var json)) return json;
        var entry = _entries[name];
        using var stream = entry.Open();
        var data = new byte[entry.Length];
        if (stream.ReadAtLeast(data, data.Length, false) < data.Length || stream.ReadByte() != -1)
            throw new InvalidDataException($"The notebook entry {name} does not match its declared size.");
        return data;
    }

    private static (long Length, string Problem) Inflate(ZipArchiveEntry entry, long limit, Stream copy, byte[] buffer)
    {
        long length = 0;
        var crc = Crc32.Initial;
        try
        {
            using var stream = entry.Open();
            int read;
            while ((read = stream.Read(buffer)) > 0)
            {
                if ((length += read) > limit) return (length, null);
                crc = Crc32.Update(crc, buffer.AsSpan(0, read));
                copy?.Write(buffer, 0, read);
            }
        }
        catch (Exception e) when (e is InvalidDataException or IOException)
        {
            return (length, e.Message);
        }

        if (length != entry.Length) return (length, "the unpacked size does not match the declared size.");
        return Crc32.Final(crc) != entry.Crc32 ? (length, "the checksum does not match.") : (length, null);
    }

    private static bool IsPage(string name) => name.StartsWith(PagesFolder, StringComparison.OrdinalIgnoreCase);

    private static string MediaId(string name)
    {
        if (!name.StartsWith(MediaFolder, StringComparison.OrdinalIgnoreCase)) return null;
        var file = name[MediaFolder.Length..];
        var dot = file.LastIndexOf('.');
        return dot > 0 && NoteValues.IsId(file[..dot]) && NoteValues.IsId(file[(dot + 1)..]) ? file[..dot] : null;
    }

    private static bool IsSafe(string name) =>
        name.Length > 0 && name[0] != '/' && !name.Contains('\\') && !name.Contains(':') && name.Split('/').All(s => s is not ("." or ".."));
}
