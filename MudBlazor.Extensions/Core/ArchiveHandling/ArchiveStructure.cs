using Nextended.Core.Extensions;
using Nextended.Core.Types;
using System.IO.Compression;


namespace MudBlazor.Extensions.Core.ArchiveHandling;


public class ArchiveStructure : Hierarchical<ArchiveStructure>
{
    public ArchiveStructure(IArchiveBrowserFile browserFile)
        : this(browserFile.Name)
    {
        BrowserFile = browserFile;
    }

    public ArchiveStructure(string name)
    {
        Name = name;
        IsExpanded = true;
    }

    public string Name { get; set; }

    public bool IsDirectory => BrowserFile == null || BrowserFile.IsDirectory;

    public bool IsDownloading { get; set; }

    public long Size => IsDirectory ? ContainingFiles.Sum(f => f.Size) : BrowserFile.Size;

    public IEnumerable<IArchiveBrowserFile> ContainingFiles
        => Children?.Recursive(s => s.Children ?? Enumerable.Empty<ArchiveStructure>()).Where(s => s is { IsDirectory: false }).Select(s => s.BrowserFile);

    public IArchiveBrowserFile BrowserFile { get; set; }

    public async Task<byte[]> ToArchiveBytesAsync()
    {
        var archive = await ToArchiveAsync();
        await using (archive.Stream)
        {
            using var zipArchive = archive.Archive;
            return archive.Stream.ToArray();
        }
    }

    private async Task<(MemoryStream Stream, ZipArchive Archive)> ToArchiveAsync()
    {
        var ms = new MemoryStream();
        using ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Create, true);

        var path = IsDirectory ? string.Join('/', Path.Skip(1).Select(s => s.Name)) : BrowserFile?.Path ?? "";
        path = !string.IsNullOrWhiteSpace(path) ? path.EnsureEndsWith('/') : path;

        foreach (var file in IsDirectory ? ContainingFiles : new[] { BrowserFile })
        {
            var entry = archive.CreateEntry(file.FullName.Substring(path.Length), CompressionLevel.Optimal);
            await using var stream = entry.Open();
            await stream.WriteAsync(file.FileBytes);
        }

        return (ms, archive);
    }

    private static void EnsurePartExists(ArchiveStructure archiveContent, List<string> parts, string p, IList<IArchiveBrowserFile> archiveEntries)
    {
        if (parts.Any() && !string.IsNullOrEmpty(parts.FirstOrDefault()))
        {
            var title = parts.First();

            var child = archiveContent.Children.SingleOrDefault(x => x.Name == title);

            if (child == null)
            {
                child = new ArchiveStructure(title)
                {
                    Parent = archiveContent,
                    Children = FindByPath(archiveEntries, p).ToHashSet()
                };

                archiveContent.Children.Add(child);
            }

            EnsurePartExists(child, parts.Skip(1).ToList(), p, archiveEntries);
        }
    }

    private static IEnumerable<ArchiveStructure> FindByPath(IList<IArchiveBrowserFile> archiveEntries, string path = default)
    {
        return archiveEntries.Where(f => !f.IsDirectory && f.Path == path).Select(file => new ArchiveStructure(file));
    }

    public static ArchiveStructure CreateStructure(IList<IArchiveBrowserFile> archiveEntries, string rootFolderName)
    {
        var paths = archiveEntries.Select(file => file.Path).Distinct().ToArray();
        var root = new ArchiveStructure(rootFolderName) { Children = FindByPath(archiveEntries, "").ToHashSet() };
        foreach (var p in paths)
        {
            var parts = p.Split('/');
            EnsurePartExists(root, parts.ToList(), p, archiveEntries);
        }
        return  root;
    }
}
