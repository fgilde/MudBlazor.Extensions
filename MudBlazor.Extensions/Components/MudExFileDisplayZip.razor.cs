using System.IO.Compression;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Extensions;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Blazor.Extensions;
using Nextended.Blazor.Models;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;


public partial class MudExFileDisplayZip : IFileDisplayInfos
{

    [Inject] private IServiceProvider _serviceProvider { get; set; }
    private IJSRuntime JsRuntime => _serviceProvider.GetService<IJSRuntime>();
    private IStringLocalizer<MudExFileDisplayZip> _localizer => _serviceProvider.GetService<IStringLocalizer<MudExFileDisplayZip>>();

    [Parameter] public string SearchString { get; set; }
    [Parameter] public bool AllowSearch { get; set; } = true;
    [Parameter] public string RootFolderName { get; set; } = "ROOT";
    [Parameter] public string Url { get; set; }
    public string ContentType => "application/zip";
    [Parameter] public Stream ContentStream { get; set; }
    [Parameter] public bool ShowAsTree { get; set; } = true;
    [Parameter] public bool AllowToggleTree { get; set; } = true;
    [Parameter] public bool AllowDownload { get; set; } = true;
    [Parameter] public bool AllowPreview { get; set; } = true;
    [Parameter] public Color ActionButtonColor { get; set; }
    [Parameter] public PropertyFilterMode FilterMode { get; set; }
    [Parameter] public string ToolBarPaperClass { get; set; }
    [Parameter] public bool StickyToolbar { get; set; } = true;
    [Parameter] public string StickyToolbarTop { get; set; } = "0";
    [Parameter] public ItemSelectionMode SelectionMode { get; set; } = ItemSelectionMode.None;
    [Parameter] public IList<ZipBrowserFile> Selected { get; set; }
    [Parameter] public EventCallback<IList<ZipBrowserFile>> SelectedChanged { get; set; }
    public bool IsSelected(ZipBrowserFile entry) => entry != null && Selected?.Contains(entry) == true;
    [Parameter] public bool ShowContentError { get; set; } = true;

    [Parameter]
    public bool FallBackInIframe { get; set; }

    /// <summary>
    /// Set this to false to show everything in iframe/object tag otherwise zip, images audio and video will displayed in correct tags
    /// </summary>
    [Parameter]
    public bool ViewDependsOnContentType { get; set; } = true;

    [Parameter] public bool ImageAsBackgroundImage { get; set; } = false;
    [Parameter] public bool SandBoxIframes { get; set; } = true;

    /**
     * A function to handle content error. Return true if you have handled the error and false if you want to show the error message
     * For example you can reset Url here to create a proxy fallback or display own not supported image or what ever.
     * If you reset Url or Data here you need also to reset ContentType
     */
    [Parameter] public Func<IFileDisplayInfos, Task<ContentErrorResult>> HandleContentErrorFunc { get; set; }
    [Parameter] public string CustomContentErrorMessage { get; set; }

    private MudMenu _downloadMenu;
    private IBrowserFile _innerPreview;
    private string _innerPreviewUrl;
    private Stream _innerPreviewStream;
    private IList<ZipBrowserFile> _zipEntries;
    private (string tag, Dictionary<string, object> attributes) renderInfos;
    private HashSet<ZipStructure> _zipStructure;
    private bool _searchActive;
    private MudTextField<string> _searchBox;
    private bool _searchBoxBlur = false;

    
    protected override async Task OnInitializedAsync()
    {
        //Url = UriExtensions.AddParameterToUrl(Url, "cb", Guid.NewGuid().ToFormattedId());
        _zipEntries = (await GetZipEntriesAsync(ContentStream ?? await new HttpClient().GetStreamAsync(Url))).ToList();
        _zipStructure = ZipStructure.CreateStructure(_zipEntries, RootFolderName).ToHashSet();
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
            return asZip ? _localizer.TryLocalize("Download {0} with {1} files as zip", structure.Name, structure.ContainingFiles.Count()) : _localizer.TryLocalize("Download {0} files separately", structure.ContainingFiles.Count());
        return asZip ? _localizer.TryLocalize("Download file {0} as zip", structure.Name) : _localizer.TryLocalize("Download file {0}", structure.Name);
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

    private bool IsInSearch(ZipBrowserFile entry) 
        => string.IsNullOrEmpty(SearchString) || entry.FullName.Contains(SearchString, StringComparison.OrdinalIgnoreCase);

    private bool IsInSearch(ZipStructure context)
    {
        if (string.IsNullOrEmpty(SearchString))
            return true;
        return context.Name.Contains(SearchString, StringComparison.OrdinalIgnoreCase) || context?.Children?.Any(IsInSearch) == true;
    }

    private Task FilterKeyPress(KeyboardEventArgs arg)
    {
        if (arg.Key == "Escape")
        {
            if (!string.IsNullOrWhiteSpace(SearchString))
                SearchString = string.Empty;
            else
                _searchActive = false;
        }
        return Task.CompletedTask;
    }

    private Task FilterBoxBlur(FocusEventArgs arg)
    {
        _searchBoxBlur = true;
        _searchActive = false;
        Task.Delay(300).ContinueWith(t => _searchBoxBlur = false);
        return Task.CompletedTask;
    }
    private void ToggleSearchBox()
    {
        if (_searchBoxBlur)
            return;
        _searchActive = !_searchActive;
        _searchBox.FocusAsync();
    }

    private Task ExpandCollapse()
    {
        _zipStructure.Recursive(s => s.Children ?? Enumerable.Empty<ZipStructure>()).Where(s => s != null).Apply(s => s.IsExpanded = !s.IsExpanded);
        return Task.CompletedTask;
    }
    private string ToolbarStyle()
    {
        var res = string.Empty;
        if (StickyToolbar && !string.IsNullOrWhiteSpace(StickyToolbarTop))
            res += $"top: {StickyToolbarTop};";
        return res;
    }

    private Task Select(ZipStructure structure, MouseEventArgs args)
    {
        return structure.IsDirectory ? Task.CompletedTask : Select(structure.BrowserFile, args);
    }
    
    private async Task Select(ZipBrowserFile entry, MouseEventArgs args)
    {
        if (SelectionMode != ItemSelectionMode.None)
        {
            Selected ??= new List<ZipBrowserFile>();

            if (Selected.Contains(entry) && SelectionMode == ItemSelectionMode.Single)
            {
                Selected.Remove(entry);
                return;
            }

            if (SelectionMode == ItemSelectionMode.Single || (SelectionMode == ItemSelectionMode.MultiSelectWithCtrlKey && !args.CtrlKey))
                Selected.Clear();

            if (Selected.Contains(entry))
                Selected.Remove(entry);
            else
                Selected.Add(entry);

            await SelectedChanged.InvokeAsync(Selected);
        }
    }

}
