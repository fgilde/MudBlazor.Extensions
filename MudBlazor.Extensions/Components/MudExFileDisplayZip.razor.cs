using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Extensions;
using Nextended.Blazor.Extensions;
using Nextended.Blazor.Models;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;


public partial class MudExFileDisplayZip
{
    [Inject] private IJSRuntime JsRuntime { get; set; }
    [Parameter] public string RootFolderName { get; set; } = "ROOT";
    [Parameter] public string Url { get; set; }
    [Parameter] public Stream ContentStream { get; set; }
    [Parameter] public bool ShowAsTree { get; set; } = true;
    [Parameter] public bool AllowToggleTree { get; set; } = true;
    [Parameter] public bool AllowDownload { get; set; } = true;
    [Parameter] public bool AllowPreview { get; set; } = true;
    [Parameter] public Color ActionButtonColor { get; set; }

    private MudMenu _downloadMenu;
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
                    Parent = zipContent,
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

        return new[] { root }.ToHashSet();
    }

    protected override async Task OnInitializedAsync()
    {
        //Url = UriExtensions.AddParameterToUrl(Url, "cb", Guid.NewGuid().ToFormattedId());
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

    private string DownloadText(ZipStructure structure, bool asZip)
    {
        if (structure.IsDirectory)
            return asZip ? _localizer["Download {0} with {1} files as zip", structure.Name, structure.ContainingFiles.Count()] : _localizer["Download {0} files separately", structure.ContainingFiles.Count()];
        return asZip ? _localizer["Download file {0} as zip", structure.Name] : _localizer["Download file {0}", structure.Name];
    }

    private Task DownloadAsync(ZipBrowserFile file)
    {
        return file.DownloadAsync(JsRuntime);
    }

    private async void DownloadAsync(ZipStructure zip, bool asZip = false)
    {
        if (!zip.IsDownloading)
        {
            _downloadMenu?.CloseMenu();
            SetDownloadStatus(zip, true);

            if (asZip)
            {
                await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
                {
                    Url = await DataUrl.GetDataUrlAsync(await zip.ToArchiveBytesAsync(), "application/zip"),
                    FileName = $"{Path.ChangeExtension(zip.Name, "zip")}",
                    MimeType = "application/zip"
                });
            }
            else
            {
                await (zip.IsDirectory ? Task.WhenAll(zip.ContainingFiles.Where(file => !file.IsDirectory).Select(DownloadAsync)) : DownloadAsync(zip.BrowserFile));
            }

            SetDownloadStatus(zip, false);
        }
    }

    private void SetDownloadStatus(ZipStructure structure, bool isDownloading)
    {
        structure.Children?.Recursive(s => s.Children ?? Enumerable.Empty<ZipStructure>()).Where(s => s != null).Apply(s => s.IsDownloading = isDownloading);
        structure.IsDownloading = isDownloading;
        StateHasChanged();
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

    public bool IsDownloading { get; set; }

    public long Size => IsDirectory ? ContainingFiles.Sum(f => f.Size) : BrowserFile.Size;

    public IEnumerable<ZipBrowserFile> ContainingFiles
        => Children?.Recursive(s => s.Children ?? Enumerable.Empty<ZipStructure>()).Where(s => s is { IsDirectory: false }).Select(s => s.BrowserFile);

    public ZipBrowserFile BrowserFile { get; set; }

    public async Task<(MemoryStream Stream, ZipArchive Archive)> ToArchiveAsync()
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

    public async Task<byte[]> ToArchiveBytesAsync()
    {
        var archive = await ToArchiveAsync();
        await using (archive.Stream)
        {
            using var zipArchive = archive.Archive;
            return archive.Stream.ToArray();
        }
    }
}