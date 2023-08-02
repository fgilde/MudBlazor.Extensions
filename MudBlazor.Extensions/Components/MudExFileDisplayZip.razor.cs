﻿using System.IO.Compression;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Blazor.Extensions;
using Nextended.Blazor.Models;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;


/// <summary>
/// A Component to display the content of a zip file
/// </summary>
public partial class MudExFileDisplayZip : IMudExFileDisplayInfos, IMudExFileDisplay
{
    private MudMenu _downloadMenu;
    private IBrowserFile _innerPreview;
    private string _innerPreviewUrl;
    private Stream _innerPreviewStream;
    private IList<ZipBrowserFile> _zipEntries;
    //private (string tag, Dictionary<string, object> attributes) renderInfos;
    private HashSet<ZipStructure> _zipStructure;

    /// <inheritdoc />
    public string Name { get; } = nameof(MudExFileDisplayZip);


    /// <inheritdoc />
    public bool WrapInMudExFileDisplayDiv => false;

    /// <inheritdoc />
    [Parameter, SafeCategory("Data")]
    public IMudExFileDisplayInfos FileDisplayInfos
    {
        get => this;
        set
        {
            RootFolderName = value.FileName;
            Url = value.Url;
            ContentStream = value.ContentStream;
        }
    }

    /// <summary>
    /// SearchString for current filter
    /// </summary>
    [Parameter, SafeCategory("Filtering")]
    public string SearchString { get; set; }

    /// <summary>
    /// If true user is able to search
    /// </summary>
    [Parameter, SafeCategory("Filtering")]
    public bool AllowSearch { get; set; } = true;

    /// <summary>
    /// Name of root folder
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string RootFolderName { get; set; } = "ROOT";

    /// <summary>
    /// Url
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string Url { get; set; }

    /// <summary>
    /// Content Type 
    /// </summary>
    [SafeCategory("Data")]
    public string ContentType => "application/zip";

    /// <inheritdoc />
    [Parameter, SafeCategory("Data")]
    public Stream ContentStream { get; set; }

    /// <summary>
    /// True to display structure as tree
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool ShowAsTree { get; set; } = true;

    /// <summary>
    /// If true user can toggle between flat and tree view
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowToggleTree { get; set; } = true;

    /// <summary>
    /// If true user can download all or specific files from zip
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowDownload { get; set; } = true;

    /// <summary>
    /// If true user can preview containing files
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowPreview { get; set; } = true;

    /// <summary>
    /// Button Color for action icon button
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public Color ActionButtonColor { get; set; }

    /// <summary>
    /// PropertyFilterMode
    /// </summary>
    [Parameter, SafeCategory("Filtering")]
    public PropertyFilterMode FilterMode { get; set; }

    /// <summary>
    /// Css Class for toolbar paper
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string ToolBarPaperClass { get; set; }

    /// <summary>
    /// True to have a sticky toolbar on top
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool StickyToolbar { get; set; } = true;

    /// <summary>
    /// Top position if toolbar is sticky
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string StickyToolbarTop { get; set; } = "0";

    /// <summary>
    /// File Selection Mode
    /// </summary>
    [Parameter, SafeCategory("Selecting")]
    public ItemSelectionMode SelectionMode { get; set; } = ItemSelectionMode.None;


    /// <summary>
    /// Selected files
    /// </summary>
    [Parameter, SafeCategory("Selecting")] 
    public IList<ZipBrowserFile> Selected { get; set; }

    /// <summary>
    /// Event on selection change
    /// </summary>
    [Parameter, SafeCategory("Selecting")] 
    public EventCallback<IList<ZipBrowserFile>> SelectedChanged { get; set; }

    /// <summary>
    /// Returns true if given ZipFile entry is selected
    /// </summary>
    public bool IsSelected(ZipBrowserFile entry) => entry != null && Selected?.Contains(entry) == true;

    /// <summary>
    /// Show content error
    /// </summary>
    [Parameter] public bool ShowContentError { get; set; } = true;

    /// <summary>
    /// Set to true to render all failures in iframe fallback
    /// </summary>
    [Parameter] public bool FallBackInIframe { get; set; }

    /// <summary>
    /// Set this to false to show everything in iframe/object tag otherwise zip, images audio and video will displayed in correct tags
    /// </summary>
    [Parameter] public bool ViewDependsOnContentType { get; set; } = true;

    /// <summary>
    /// Render images as background image instead of img tag
    /// </summary>
    [Parameter] public bool ImageAsBackgroundImage { get; set; } = false;

    /// <summary>
    /// Use sandbox mode for iframe
    /// </summary>
    [Parameter] public bool SandBoxIframes { get; set; } = true;

    /// <summary>
    /// A function to handle content error.
    /// Return true if you have handled the error and false if you want to show the error message For example you can reset Url here to create a proxy fallback or display own not supported image or what ever.
    /// If you reset Url or Data here you need also to reset ContentType
    /// </summary>
    [Parameter] public Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> HandleContentErrorFunc { get; set; }
    
    /// <summary>
    /// Custom error message for content error
    /// </summary>
    [Parameter] public string CustomContentErrorMessage { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(Url) || ContentStream != null)
            {

                _zipEntries = (await GetZipEntriesAsync(ContentStream ?? await new HttpClient().GetStreamAsync(Url)))
                    .ToList();
                _zipStructure = ZipStructure.CreateStructure(_zipEntries, RootFolderName).ToHashSet();
            }
        }
        catch
        {
            //ignored
        }
        await base.OnParametersSetAsync();
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
            return asZip ? TryLocalize("Download {0} with {1} files as zip", structure.Name, structure.ContainingFiles.Count()) : TryLocalize("Download {0} files separately", structure.ContainingFiles.Count());
        return asZip ? TryLocalize("Download file {0} as zip", structure.Name) : TryLocalize("Download file {0}", structure.Name);
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
        return context.Name.Contains(SearchString, StringComparison.OrdinalIgnoreCase) || context.Children?.Any(IsInSearch) == true;
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

    /// <inheritdoc />
    public string FileName
    {
        get => RootFolderName;
        set => RootFolderName = value;
    }

    /// <inheritdoc />
    public bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return MimeType.IsZip(fileDisplayInfos.ContentType);
    }
}
