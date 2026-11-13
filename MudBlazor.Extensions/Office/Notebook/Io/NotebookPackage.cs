using System.IO.Compression;

namespace MudBlazor.Extensions.Office.Notebook;

internal static class NotebookMediaTypes
{
    private static readonly Dictionary<string, string> ByExtension = new(StringComparer.Ordinal)
    {
        ["png"] = "image/png", ["jpg"] = "image/jpeg", ["gif"] = "image/gif", ["webp"] = "image/webp", ["bmp"] = "image/bmp", ["svg"] = "image/svg+xml"
    };

    public static string ExtensionOf(string contentType) => ByExtension.FirstOrDefault(e => string.Equals(e.Value, contentType, StringComparison.OrdinalIgnoreCase)).Key;

    public static string Normalize(string contentType) => ExtensionOf(contentType) is { } extension ? ByExtension[extension] : null;

    public static bool IsExtension(string extension) => ByExtension.ContainsKey(extension);
}

/// <summary>A read-only view of a notebook zip with checked entry paths and sizes. Entry names are zip names without leading slash.</summary>
internal sealed class NotebookPackage
{
    public const string ManifestEntry = "notebook.json";
    public const string PagesFolder = "pages/";
    public const string MediaFolder = "media/";

    private readonly Dictionary<string, ZipArchiveEntry> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _media = new(StringComparer.Ordinal);

    private NotebookPackage(byte[] source, NotebookLoadOptions options)
    {
        var zip = new ZipArchive(new MemoryStream(source, false), ZipArchiveMode.Read);
        long total = 0;
        foreach (var entry in zip.Entries)
        {
            if (!IsSafe(entry.FullName)) throw new InvalidDataException($"The notebook contains the unsafe entry path {entry.FullName}.");
            if (entry.FullName.EndsWith('/')) continue;
            if (entry.Length > options.MaxEntryBytes) throw new InvalidDataException($"The notebook entry {entry.FullName} is larger than {options.MaxEntryBytes} bytes.");
            if ((total += entry.Length) > options.MaxTotalBytes) throw new InvalidDataException($"The notebook is larger than {options.MaxTotalBytes} bytes unpacked.");
            if (!_entries.TryAdd(entry.FullName, entry)) throw new InvalidDataException($"The notebook contains the entry {entry.FullName} twice.");
            if (MediaId(entry.FullName) is { } id) _media.TryAdd(id, entry.FullName);
        }
    }

    public IReadOnlyCollection<string> Names => _entries.Keys;

    public IReadOnlyCollection<string> MediaIds => _media.Keys;

    public static NotebookPackage Open(Stream stream, NotebookLoadOptions options)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return new NotebookPackage(buffer.ToArray(), options);
    }

    public static string PageEntry(string pageId) => PagesFolder + pageId + ".json";

    public static string MediaEntryName(string mediaId, string extension) => MediaFolder + mediaId + "." + extension;

    public ZipArchiveEntry Entry(string name) => _entries.GetValueOrDefault(name);

    public string MediaEntry(string mediaId) => _media.GetValueOrDefault(mediaId);

    public byte[] Read(string name)
    {
        var entry = _entries[name];
        using var stream = entry.Open();
        var data = new byte[entry.Length];
        if (stream.ReadAtLeast(data, data.Length, false) < data.Length || stream.ReadByte() != -1)
            throw new InvalidDataException($"The notebook entry {name} does not match its declared size.");
        return data;
    }

    private static string MediaId(string name)
    {
        if (!name.StartsWith(MediaFolder, StringComparison.Ordinal)) return null;
        var file = name[MediaFolder.Length..];
        var dot = file.LastIndexOf('.');
        return dot > 0 && NoteValues.IsId(file[..dot]) && NotebookMediaTypes.IsExtension(file[(dot + 1)..]) ? file[..dot] : null;
    }

    private static bool IsSafe(string name) =>
        name.Length > 0 && name[0] != '/' && !name.Contains('\\') && !name.Contains(':') && name.Split('/').All(s => s is not ("." or ".."));
}
