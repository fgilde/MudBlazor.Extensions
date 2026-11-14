using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace MudBlazor.Extensions.Office.Notebook;

internal static class NotebookWriter
{
    public static void Write(Notebook notebook, Stream target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (notebook.ReadOnlyFormat) throw new InvalidOperationException("The notebook was written by a newer version and cannot be saved.");
        using var buffer = new MemoryStream();
        using (var writer = new OpcPackageWriter(part => notebook.Package?.Entry(part[1..]), buffer)) Write(notebook, writer);
        CheckLimits(buffer, notebook.Limits);
        buffer.Position = 0;
        buffer.CopyTo(target);
    }

    private static void CheckLimits(MemoryStream buffer, NotebookLoadOptions limits)
    {
        buffer.Position = 0;
        using var zip = new ZipArchive(buffer, ZipArchiveMode.Read, true);
        if (buffer.Length > NotebookPackage.MaxFileBytes(limits) || zip.Entries.Any(e => e.Length > limits.MaxEntryBytes) || zip.Entries.Sum(e => e.Length) > limits.MaxTotalBytes)
            throw new InvalidOperationException($"The notebook is larger than {limits.MaxTotalBytes} bytes and could not be opened again. Remove pictures and save again.");
    }

    private static void Write(Notebook notebook, OpcPackageWriter writer)
    {
        var package = notebook.Package;
        var manifest = JsonSerializer.SerializeToUtf8Bytes(NotebookJson.ToDto(notebook), NotebookJsonContext.Default.ManifestDto);
        writer.Put("/" + NotebookPackage.ManifestEntry, manifest);

        var known = new HashSet<string>(notebook.PendingMedia.Keys.Concat(package?.MediaIds ?? []), StringComparer.OrdinalIgnoreCase);
        var lengths = known.Select(id => id.Length).ToHashSet();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var foreign = package?.Names.Where(IsForeign).ToList() ?? [];
        MarkUsed(manifest, known, lengths, used);
        foreach (var name in foreign) MarkUsed(package.Read(name), known, lengths, used);
        foreach (var page in notebook.SectionList.SelectMany(s => s.PageList))
        {
            byte[] json;
            if (page.SourceEntry != null && (page.LoadError != null || !page.Modified))
            {
                writer.Copy("/" + page.SourceEntry);
                json = package.Read(page.SourceEntry);
            }
            else if (page.LoadError == null)
            {
                json = JsonSerializer.SerializeToUtf8Bytes(NotebookJson.ToDto(page), NotebookJsonContext.Default.PageDto);
                writer.Put("/" + NotebookPackage.PageEntry(page.Id), json);
            }
            else continue;

            MarkUsed(json, known, lengths, used);
            foreach (var image in page.ElementList.OfType<ImageElement>())
                if (image.MediaId != null && known.TryGetValue(image.MediaId, out var id)) used.Add(id);
        }

        foreach (var id in used)
            if (notebook.PendingMedia.TryGetValue(id, out var pending)) writer.Put("/" + NotebookPackage.MediaEntryName(id, pending.Extension), pending.Data);
            else writer.Copy("/" + package.MediaEntry(id));

        foreach (var name in foreign) writer.Copy("/" + name);
    }

    private static void MarkUsed(ReadOnlySpan<byte> json, HashSet<string> known, HashSet<int> lengths, HashSet<string> used)
    {
        for (var i = 0; i < json.Length;)
        {
            var start = i;
            while (i < json.Length && IsIdByte(json[i])) i++;
            if (i == start) i++;
            else if (lengths.Contains(i - start) && known.TryGetValue(Encoding.ASCII.GetString(json[start..i]), out var id)) used.Add(id);
        }
    }

    private static bool IsIdByte(byte b) => b is >= (byte)'a' and <= (byte)'z' or >= (byte)'A' and <= (byte)'Z' or >= (byte)'0' and <= (byte)'9' or (byte)'-' or (byte)'_';

    private static bool IsForeign(string name) =>
        !name.Equals(NotebookPackage.ManifestEntry, StringComparison.OrdinalIgnoreCase) && !name.StartsWith(NotebookPackage.PagesFolder, StringComparison.OrdinalIgnoreCase)
                                                                                     && !NotebookPackage.IsMedia(name);
}
