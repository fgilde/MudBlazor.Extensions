using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
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
    private IList<IArchivedBrowserFile> _zipEntries;    
    private HashSet<MudExArchiveStructure> _zipStructure;
    private string _contentType;

    private MudExTreeView<MudExArchiveStructure> _treeView;

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name { get; } = nameof(MudExFileDisplayZip);


    /// <inheritdoc />
    public bool WrapInMudExFileDisplayDiv => false;

    #region Parameters

    /// <summary>
    /// Specify types of IMudExFileDisplay that should be ignored
    /// </summary>
    [Parameter]
    public Type[] IgnoredRenderControls { get; set; }

    /// <summary>
    /// Reference to the parent MudDialog if the component is used inside a MudDialog
    /// </summary>
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; }

    /// <summary>
    /// Behavior for handling stream urls
    /// </summary>
    [Parameter, SafeCategory("Behaviour")]
    public StreamUrlHandling StreamUrlHandling { get; set; } = StreamUrlHandling.BlobUrl;

    /// <summary>
    /// If true, compact vertical padding will be applied to items.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool Dense { get; set; }

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
            ContentType = value.ContentType;
        }
    }

    /// <summary>
    /// Specify parameters for viewer controls. If a possible IMudExFileDisplay is found for current content type this parameters will be forwarded
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public IDictionary<string, object> ParametersForSubControls { get; set; }

    /// <summary>
    /// Set this to true to initially render native and ignore registered IMudExFileDisplay
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool ForceNativeRender { get; set; }

	/// <summary>
	/// SearchString for current filter
	/// </summary>
	[Parameter, SafeCategory("Filtering")]
    public string SearchString { get; set; }

    /// <summary>
    /// The filter values for the component.
    /// </summary>
    [Parameter, SafeCategory("Filtering")] 
    public List<string> Filters { get; set; }

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
    public string ContentType
    {
        get => _contentType ?? "application/zip";
        set => _contentType = value;
    }

    /// <inheritdoc />
    [Parameter, SafeCategory("Data")]
    public Stream ContentStream { get; set; }

    /// <summary>
    /// True to display structure as tree
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    [Obsolete("Use ViewMode instead")]
    public bool ShowAsTree
    {
        get => ViewMode == TreeViewMode.Default;
        set => ViewMode = value ? TreeViewMode.Default : TreeViewMode.FlatList;
    }

    /// <summary>
    /// True to display structure as tree
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public TreeViewMode ViewMode { get; set; } = TreeViewMode.Default;

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
    public IList<IArchivedBrowserFile> Selected { get; set; }

    /// <summary>
    /// Event on selection change
    /// </summary>
    [Parameter, SafeCategory("Selecting")] 
    public EventCallback<IList<IArchivedBrowserFile>> SelectedChanged { get; set; }

    /// <summary>
    /// Returns true if given ZipFile entry is selected
    /// </summary>
    public bool IsSelected(IArchivedBrowserFile entry) => entry != null && Selected?.Contains(entry) == true;

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

    /// <summary>
    /// If true icons are colored
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public bool ColorizeIcons { get; set; }

    /// <summary>
    /// If true icons are colored
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public MudExColor IconColor { get; set; } = Color.Inherit;

    /// <summary>
    /// Gets or sets a value indicating whether the collection should be virtualized.
    /// </summary>
    [Parameter]
    public bool Virtualize { get; set; } = true;

    #endregion

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<Stream>(nameof(ContentStream), out var stream) && ContentStream != stream)
                             || (parameters.TryGetValue<string>(nameof(Url), out var url) && Url != url);

        await base.SetParametersAsync(parameters);

        if (!updateRequired || (string.IsNullOrEmpty(Url) && ContentStream == null))
            return;

        //_ = CreateStructure().ContinueWith(_ => InvokeAsync(StateHasChanged));
       await CreateStructure();
       StateHasChanged();
    }
    
    private async Task CreateStructure()
    {
        var archive = await FileService.ReadArchiveAsync(ContentStream ?? await new HttpClient().GetStreamAsync(Url), RootFolderName, ContentType);
        _zipStructure = archive.Structure;
        _zipEntries = archive.List;
    }
    
    private async Task Preview(IArchivedBrowserFile file)
    {
        _innerPreview = file;
        
        //_innerPreviewStream = new MemoryStream(await file.GetBytesAsync());
        //if(!MimeType.IsArchive(file.ContentType))
        //    _innerPreviewUrl = await fileService.CreateDataUrlAsync(file, StreamUrlHandling == StreamUrlHandling.BlobUrl);
        
        if (MimeType.IsArchive(file.ContentType))
            _innerPreviewStream = new MemoryStream(await file.GetBytesAsync());
        else
            _innerPreviewUrl = await FileService.CreateDataUrlAsync(file, StreamUrlHandling == StreamUrlHandling.BlobUrl);
    }

    private void ClosePreview()
    {
        _innerPreview = null;
        _innerPreviewStream = null;
        _innerPreviewUrl = null;
    }

    private string DownloadText(MudExArchiveStructure structure, bool asZip)
    {
        if (structure.IsDirectory)
            return asZip ? TryLocalize("Download {0} with {1} files as zip", structure.Name, structure.ContainingFiles.Count()) : TryLocalize("Download {0} files separately", structure.ContainingFiles.Count());
        return asZip ? TryLocalize("Download file {0} as zip", structure.Name) : TryLocalize("Download file {0}", structure.Name);
    }

    private Task DownloadAsync(IArchivedBrowserFile file)
    {
        return file.DownloadAsync(JsRuntime);
    }

    private async void DownloadAsync(MudExArchiveStructure zip, bool asZip = false)
    {
        if (zip.IsDownloading) return;
        _= _downloadMenu?.CloseMenuAsync();
        SetDownloadStatus(zip, true);

        if (asZip)
        {
            await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
            {
                Url = await FileService.CreateDataUrlAsync(await zip.ToArchiveBytesAsync(), "application/zip", StreamUrlHandling == StreamUrlHandling.BlobUrl),
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

    private void SetDownloadStatus(MudExArchiveStructure structure, bool isDownloading)
    {
        structure.Children?.Recursive(s => s.Children ?? Enumerable.Empty<MudExArchiveStructure>()).Where(s => s != null).Apply(s => s.IsDownloading = isDownloading);
        structure.IsDownloading = isDownloading;
        StateHasChanged();
    }
    

    private string ToolbarStyle()
    {
        return MudExStyleBuilder.Default
            .WithBackgroundColor(Color.Surface)
            .WithTop(StickyToolbarTop, StickyToolbar && !string.IsNullOrWhiteSpace(StickyToolbarTop))
            .Style;
    }

    private Task Select(MudExArchiveStructure structure, MouseEventArgs args) => structure.IsDirectory ? Task.CompletedTask : Select(structure.BrowserFile, args);

    private async Task Select(IArchivedBrowserFile entry, MouseEventArgs args)
    {
        if (SelectionMode != ItemSelectionMode.None)
        {
            Selected ??= new List<IArchivedBrowserFile>();

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
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService) => Task.FromResult(CanHandleFileAsArchive(fileDisplayInfos.ContentType));

    /// <summary>
    /// Returns true if the MudExFileDisplay Component can handle the file as an archive.
    /// </summary>
    public static bool CanHandleFileAsArchive(string contentType) => MimeType.IsArchive(contentType);

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();        
        _zipStructure = null;
        _zipEntries?.Clear();
        _zipEntries = null;
        ClosePreview();
        if(FileService != null)
            await FileService.DisposeAsync();
    }

    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>()
        {
            {"Folders", _zipStructure.Recursive(s => s.Children ?? new HashSet<MudExArchiveStructure>()).Count(s => s.IsDirectory)},
            {"Files",_zipStructure.Recursive(s => s.Children ?? new HashSet<MudExArchiveStructure>()).Count(s => !s.IsDirectory)},
        });
    }
}
