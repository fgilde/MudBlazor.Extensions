using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Core.ArchiveHandling;

using Nextended.Core.Types;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class ArchiveStructure : Hierarchical<ArchiveStructure>
{
    public ArchiveStructure(ArchiveBrowserFile browserFile)
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

    public IEnumerable<ArchiveBrowserFile> ContainingFiles
        => Children?.Recursive(s => s.Children ?? Enumerable.Empty<ArchiveStructure>()).Where(s => s is { IsDirectory: false }).Select(s => s.BrowserFile);

    public ArchiveBrowserFile BrowserFile { get; set; }

    public async Task<MemoryStream> ToArchiveAsync()
    {
        var ms = new MemoryStream();

        using (var archive = ArchiveFactory.Create(ArchiveType.Zip))
        {
            var path = IsDirectory ? string.Join('/', Path.Skip(1).Select(s => s.Name)) : BrowserFile?.Path ?? "";
            path = !string.IsNullOrWhiteSpace(path) ? path.EnsureEndsWith('/') : path;

            foreach (var file in IsDirectory ? ContainingFiles : new[] { BrowserFile })
            {
                using var entryStream = new MemoryStream(file.FileBytes);                
                archive.AddEntry(file.FullName.Substring(path.Length), entryStream, true);
            }
            archive.SaveTo(ms, CompressionType.Deflate);
        }        
        return ms;
    }

    public async Task<byte[]> ToArchiveBytesAsync()
    {
        var ms = await ToArchiveAsync();
        return ms.ToArray();
    }

    private static void EnsurePartExists(ArchiveStructure archiveContent, List<string> parts, string p, IList<ArchiveBrowserFile> archiveEntries)
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

    private static IEnumerable<ArchiveStructure> FindByPath(IList<ArchiveBrowserFile> archiveEntries, string path = default)
    {
        return archiveEntries.Where(f => !f.IsDirectory && f.Path == path).Select(file => new ArchiveStructure(file));
    }

    public static HashSet<ArchiveStructure> CreateStructure(IList<ArchiveBrowserFile> archiveEntries, string rootFolderName)
    {
        var paths = archiveEntries.Select(file => file.Path).Distinct().ToArray();
        var root = new ArchiveStructure(rootFolderName) { Children = FindByPath(archiveEntries, "").ToHashSet() };
        foreach (var p in paths)
        {
            var parts = p.Split('/');
            EnsurePartExists(root, parts.ToList(), p, archiveEntries);
        }

        return new[] { root }.ToHashSet();
    }
}
