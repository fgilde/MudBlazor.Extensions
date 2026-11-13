using System.Text;
using System.Text.Json;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>
/// Writes the notebook format into a buffer and copies it to the target only when complete. Unchanged pages and error pages are copied from the loaded
/// file, changed pages are written anew. A medium is kept if the JSON of any page names its id, also inside unknown properties or opaque elements;
/// other media are left out. Entries outside <c>notebook.json</c>, <c>pages/</c> and the media files are kept.
/// </summary>
internal static class NotebookWriter
{
    public static void Write(Notebook notebook, Stream target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (notebook.ReadOnlyFormat) throw new InvalidOperationException("The notebook was written by a newer version and cannot be saved.");
        using var buffer = new MemoryStream();
        using (var writer = new OpcPackageWriter(part => notebook.Package?.Entry(part[1..]), buffer)) Write(notebook, writer);
        buffer.Position = 0;
        buffer.CopyTo(target);
    }

    private static void Write(Notebook notebook, OpcPackageWriter writer)
    {
        var package = notebook.Package;
        writer.Put("/" + NotebookPackage.ManifestEntry, JsonSerializer.SerializeToUtf8Bytes(NotebookJson.ToDto(notebook), NotebookJsonContext.Default.ManifestDto));

        var known = new HashSet<string>(notebook.PendingMedia.Keys.Concat(package?.MediaIds ?? []), StringComparer.OrdinalIgnoreCase);
        var lengths = known.Select(id => id.Length).ToHashSet();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
        }

        foreach (var id in used)
            if (notebook.PendingMedia.TryGetValue(id, out var pending)) writer.Put("/" + NotebookPackage.MediaEntryName(id, pending.Extension), pending.Data);
            else writer.Copy("/" + package.MediaEntry(id));

        if (package == null) return;
        foreach (var name in package.Names.Where(IsForeign)) writer.Copy("/" + name);
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
