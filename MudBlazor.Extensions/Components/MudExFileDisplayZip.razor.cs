using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Nextended.Blazor.Extensions;
using Nextended.Blazor.Models;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;


public partial class MudExFileDisplayZip
{
    [Parameter] public string RootFolderName { get; set; } = "ROOT";
    [Parameter] public string Url { get; set; }
    [Parameter] public Stream ContentStream { get; set; }
    [Parameter] public bool ShowAsTree { get; set; } = true;
    [Parameter] public bool AllowToggleTree { get; set; } = true;

    private IBrowserFile _innerPreview;
    private string _innerPreviewUrl;
    private Stream _innerPreviewStream;
    private IList<ZipBrowserFile> _zipEntries;
    private (string tag, Dictionary<string, object> attributes) renderInfos;
    
    private HashSet<ZipStructure> _zipStructure;

    void EnsurePartExists(ZipStructure zipContent, List<string> parts, string p)
    {
        if (parts.Any() && !string.IsNullOrEmpty(parts.FirstOrDefault()))
        {
            var title = parts.First();

            var child = zipContent.Children.SingleOrDefault(x => x.Name == title);

            if (child == null)
            {
                child = new ZipStructure(title)
                {
                    Children = FindByPath(p).ToHashSet()
                };

                zipContent.Children.Add(child);
            }

            EnsurePartExists(child, parts.Skip(1).ToList(), p);
        }
    }

    IEnumerable<ZipStructure> FindByPath(string path = default)
    {
        return _zipEntries.Where(f => !f.IsDirectory && f.Path == path).Select(file => new ZipStructure(file));
    }


    HashSet<ZipStructure> CreateStructure()
    {
        var paths = _zipEntries.Select(file => file.Path).Distinct().ToArray();
        var root = new ZipStructure(RootFolderName) { Children = FindByPath("").ToHashSet() };
        foreach (var p in paths)
        {
            var parts = p.Split('/');
            EnsurePartExists(root, parts.ToList(), p);
        }

        return new []{root}.ToHashSet();
    }

    protected override async Task OnInitializedAsync()
    {
        _zipEntries = (await GetZipEntriesAsync(ContentStream ?? await new HttpClient().GetStreamAsync(Url))).ToList();
        _zipStructure = CreateStructure().ToHashSet();
        await base.OnInitializedAsync();
    }

    private async Task<IList<ZipBrowserFile>> GetZipEntriesAsync(Stream stream)
    {
        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        return new ZipArchive(ms).Entries.Select(entry => new ZipBrowserFile(entry)).ToList();
    }


    private async Task Preview(ZipBrowserFile file)
    {
        _innerPreview = file;
        if (file.IsZipFile())
            _innerPreviewStream = new MemoryStream(await file.GetBytesAsync());
        else
            _innerPreviewUrl = await file.GetDataUrlAsync();
    }

    private void ClosePreview()
    {
        _innerPreview = null;
        _innerPreviewStream = null;
        _innerPreviewUrl = null;
    }
}

public class ZipStructure : Hierarchical<ZipStructure>
{
    public ZipStructure(ZipBrowserFile browserFile)
        : this(browserFile.Name)
    {
        BrowserFile = browserFile;
    }

    public ZipStructure(string name)
    {
        Name = name;
        IsExpanded = true;
    }

    public string Name { get; set; }

    public bool IsDirectory => BrowserFile == null || BrowserFile.IsDirectory;

    public ZipBrowserFile BrowserFile { get; set; }
}