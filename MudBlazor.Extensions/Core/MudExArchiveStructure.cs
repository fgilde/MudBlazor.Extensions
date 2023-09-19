using Nextended.Blazor.Models;

namespace MudBlazor.Extensions.Core;

public class MudExArchiveStructure : ArchiveStructure
{
    public MudExArchiveStructure(IArchivedBrowserFile browserFile) : base(browserFile)
    {}

    public MudExArchiveStructure(string name) : base(name)
    {}

    public new HashSet<MudExArchiveStructure> Children
    {
        get => base.Children.OfType<MudExArchiveStructure>().ToHashSet();
        set => base.Children = new HashSet<ArchiveStructure>(value);
    }

    public static MudExArchiveStructure CreateStructure(
        IList<IArchivedBrowserFile> archiveEntries,
        string rootFolderName)
    {
        var array = archiveEntries.Select(file => file.Path).Distinct().ToArray();
        var archiveStructure = new MudExArchiveStructure(rootFolderName)
        {
            Children = FindByPath(archiveEntries, "").Select(a => new MudExArchiveStructure(a.BrowserFile)).ToHashSet()
        };
        var archiveContent = archiveStructure;
        foreach (var p in array)
        {
            string[] source = p.Split('/');
            EnsurePartExists(archiveContent, source.ToList(), p, archiveEntries);
        }
        return archiveContent;
    }
}