using System.Text.Json;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>
/// Writes the notebook format. Unchanged pages, error pages and media still in use are copied from the loaded file; changed pages are written anew; media
/// no page uses are left out. Entries outside <c>pages/</c> and <c>media/</c> are kept.
/// </summary>
internal static class NotebookWriter
{
    public static void Write(Notebook notebook, Stream target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (notebook.ReadOnlyFormat) throw new InvalidOperationException("The notebook was written by a newer version and cannot be saved.");
        var package = notebook.Package;
        using var writer = new OpcPackageWriter(part => package?.Entry(part[1..]), target);
        writer.Put("/" + NotebookPackage.ManifestEntry, JsonSerializer.SerializeToUtf8Bytes(NotebookJson.ToDto(notebook), NotebookJsonContext.Default.ManifestDto));

        var media = new HashSet<string>(StringComparer.Ordinal);
        foreach (var page in notebook.SectionList.SelectMany(s => s.PageList))
        {
            if (page.LoadError != null)
            {
                if (page.SourceEntry != null) writer.Copy("/" + page.SourceEntry);
                if (page.SourceText != null) media.UnionWith(KnownMedia(notebook).Where(id => page.SourceText.Contains(id, StringComparison.Ordinal)));
                continue;
            }

            media.UnionWith(page.ElementList.OfType<ImageElement>().Select(i => i.MediaId).Where(id => id != null));
            if (!page.Modified && page.SourceEntry != null) writer.Copy("/" + page.SourceEntry);
            else writer.Put("/" + NotebookPackage.PageEntry(page.Id), JsonSerializer.SerializeToUtf8Bytes(NotebookJson.ToDto(page), NotebookJsonContext.Default.PageDto));
        }

        foreach (var id in media)
            if (notebook.PendingMedia.TryGetValue(id, out var pending)) writer.Put("/" + NotebookPackage.MediaEntryName(id, pending.Extension), pending.Data);
            else if (package?.MediaEntry(id) is { } entry) writer.Copy("/" + entry);

        if (package == null) return;
        foreach (var name in package.Names.Where(IsForeign)) writer.Copy("/" + name);
    }

    private static IEnumerable<string> KnownMedia(Notebook notebook) => notebook.PendingMedia.Keys.Concat(notebook.Package?.MediaIds ?? []);

    private static bool IsForeign(string name) =>
        name != NotebookPackage.ManifestEntry && !name.StartsWith(NotebookPackage.PagesFolder, StringComparison.Ordinal)
                                              && !name.StartsWith(NotebookPackage.MediaFolder, StringComparison.Ordinal);
}
