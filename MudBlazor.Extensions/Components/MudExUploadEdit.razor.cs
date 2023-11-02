using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;
using Nextended.Blazor.Extensions;
using Nextended.Core;
using BrowserFileExtensions = Nextended.Blazor.Extensions.BrowserFileExtensions;
using Nextended.Blazor.Models;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Contracts;
using MudBlazor.Extensions.Services;
using Nextended.Core.Extensions;
using System.Collections.Concurrent;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A Component to edit and upload a list of files
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class MudExUploadEdit<T> where T : IUploadableFile, new()
{
    const string DropPlaceholderText = "Drop files here";


    #region Parameters

    /// <summary>
    /// Template can used for the drop zone part if no item is added
    /// </summary>
    [Parameter] public RenderFragment<MudExUploadEdit<T>> DropZoneTemplate { get; set; }

    /// <summary>
    /// Item render template
    /// </summary>
    [Parameter] public RenderFragment<T> ItemTemplate { get; set; }

    /// <summary>
    /// Dialog options for external file dialog
    /// </summary>
    [Parameter] public DialogOptionsEx ExternalDialogOptions { get; set; } = new() { CloseButton = true, DragMode = MudDialogDragMode.Simple, Resizeable = true, MaxWidth = MaxWidth.Small, FullWidth = true, Animations = new[] { AnimationType.FlipX } };
    
    /// <summary>
    /// Set this to true to use the original colors for the images of the external file picker
    /// </summary>
    [Parameter] public bool ColoredImagesForExternalFilePicker { get; set; } = true;

    /// <summary>
    /// Specify how external file providers are rendered
    /// </summary>
    [Parameter] public ExternalProviderRendering ExternalProviderRendering { get; set; } = ExternalProviderRendering.ImagesNewLine;

    /// <summary>
    /// The text displayed when picker are rendered in the add external dialog
    /// </summary>
    [Parameter, SafeCategory("Data")] public string TextAddExternal { get; set; } = "Add External";

    /// <summary>
    /// Client ID for One Drive
    /// </summary>
    [Parameter, SafeCategory("Data")] public string OneDriveClientId { get; set; }

    /// <summary>
    /// Client ID for Google Drive
    /// </summary>
    [Parameter, SafeCategory("Data")] public string GoogleDriveClientId { get; set; }

    /// <summary>
    /// The API key for DropBox
    /// </summary>
    [Parameter, SafeCategory("Data")] public string DropBoxApiKey { get; set; }

    /// <summary>
    /// Variant of action buttons
    /// </summary>
    [Parameter] public Variant ButtonVariant { get; set; } = Variant.Text;

    /// <summary>
    /// Color of action buttons
    /// </summary>
    [Parameter] public Color ButtonColor { get; set; } = Color.Primary;

    /// <summary>
    /// Size of action buttons
    /// </summary>
    [Parameter] public Size ButtonSize { get; set; } = Size.Small;

    /// <summary>
    /// Alignment of action buttons
    /// </summary>
    [Parameter] public Justify ButtonsJustify { get; set; } = Justify.Center;


    /// <summary>
    /// Specify Theme to use for code file previews
    /// </summary>
    [Parameter, SafeCategory("Behaviour")]
    public CodeBlockTheme CodeBlockTheme { get; set; } = CodeBlockTheme.AtomOneDark;

    /// <summary>
    /// Specify how temporary urls are created
    /// </summary>
    [Parameter, SafeCategory("Behaviour")]
    public StreamUrlHandling StreamUrlHandling { get; set; } = StreamUrlHandling.BlobUrl;

    /// <summary>
    /// Set this to true to initially render native and ignore registered IMudExFileDisplay
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool ForceNativeRender { get; set; }

    /// <summary>
    /// The text displayed in the drop zone. 
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextDropZone { get; set; } = DropPlaceholderText;

    /// <summary>
    /// The text for the upload files button.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextUploadFiles { get; set; } = "Upload Files";

    /// <summary>
    /// The text for the upload file button.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextUploadFile { get; set; } = "Upload File";

    /// <summary>
    /// The text for the upload folder button.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextUploadFolder { get; set; } = "Upload Folder";

    /// <summary>
    /// The text for the add URL button.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextAddUrl { get; set; } = "Add Url";

    /// <summary>
    /// The text for google drive picker.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextAddFromGoogle { get; set; } = "Google Drive";

    /// <summary>
    /// The text for drop box picker
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextAddFromDropBox { get; set; } = "Drop Box";

    /// <summary>
    /// The text for one drive picker
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextAddFromOneDrive { get; set; } = "One Drive";

    /// <summary>
    /// The text for the remove all button.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextRemoveAll { get; set; } = "Remove All";

    /// <summary>
    /// The error text displayed when a file is duplicated.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextErrorDuplicateFile { get; set; } = "The file ({0}) has already been added";

    /// <summary>
    /// The error text displayed when a file exceeds the maximum size.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextErrorMaxFileSize { get; set; } = "The file has exceeded the maximum size of {0} with {1}. File size is {2}";

    /// <summary>
    /// The error text displayed when the maximum number of files is exceeded.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextErrorMaxFileCount { get; set; } = "A maximum of {0} files are allowed";

    /// <summary>
    /// The error text displayed when a file's MIME type is not allowed.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextErrorExtensionNotAllowed { get; set; } = "Files with the extension ({0}) are not allowed. Only following types are allowed '{1}'. Try one of these extensions ({2})";

    /// <summary>
    /// The error text displayed when a file's MIME type is forbidden.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextErrorExtensionForbidden { get; set; } = "Files with the extension ({0}) are not allowed. Following types are forbidden '{1}' ({2})";

    /// <summary>
    /// The error text displayed when a file's MIME type is not allowed.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextErrorMimeTypeNotAllowed { get; set; } = "Files of this type ({0}) are not allowed. Only following types are allowed '{1}'. Try one of these extensions ({2})";

    /// <summary>
    /// The error text displayed when a file's MIME type is forbidden.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextErrorMimeTypeForbidden { get; set; } = "Files of this type ({0}) are not allowed. Following types are forbidden '{1}' ({2})";

    /// <summary>
    /// The title text displayed in the add URL dialog.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextAddUrlTitle { get; set; } = "Add external Url";

    /// <summary>
    /// The message text displayed in the add URL dialog.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextAddUrlMessage { get; set; } = "Enter the URL to existing file";

    /// <summary>
    /// The animation type for errors.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public AnimationType ErrorAnimation { get; set; } = AnimationType.Pulse;

    /// <summary>
    /// The label displayed in the component.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string Label { get; set; }

    /// <summary>
    /// Defines whether the component is read only.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// The helper text displayed in the component.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string HelperText { get; set; }

    /// <summary>
    /// The variant of the component.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public Variant Variant { get; set; }

    /// <summary>
    /// Defines whether renaming of files is allowed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowRename { get; set; } = true;

    /// <summary>
    /// Defines whether adding of external URL is allowed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowExternalUrl { get; set; } = true;

    /// <summary>
    /// Defines whether adding of external files from Google Drive is allowed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowGoogleDrive { get; set; } = true;

    /// <summary>
    /// Defines whether adding of external files from Drop Box is allowed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowDropBox { get; set; } = true;

    /// <summary>
    /// Defines whether adding of external files from Microsoft One Drive or office 365 is allowed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowOneDrive{ get; set; } = true;

    /// <summary>
    /// The ID of the upload field.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string UploadFieldId { get; set; }

    /// <summary>
    /// Mime types for MimeRestrictions based on the <see cref="MimeRestrictionType"/> this types are allowed or forbidden.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string[] MimeTypes
    {
        get => _mimeTypes;
        set
        {
            if (_mimeTypes != value && (_mimeTypes == null ? value != null : (value == null || !_mimeTypes.SequenceEqual(value))))
            {
                _mimeTypes = value;
                UpdateAllowed();
            }
        }
    }

    /// <summary>
    /// Extensions for FileRestrictions based on the <see cref="ExtensionRestrictionType"/> this types are allowed or forbidden.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string[] Extensions
    {
        get => _extensions;
        set
        {
            if (_extensions != value && (_extensions == null ? value != null : (value == null || !_extensions.SequenceEqual(value))))
            {
                _extensions = value;
                UpdateAllowed();
            }
        }
    }

    /// <summary>
    /// The type of the MIME restriction.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public RestrictionType MimeRestrictionType
    {
        get => _mimeRestrictionType;
        set
        {
            if (_mimeRestrictionType != value)
            {
                _mimeRestrictionType = value;
                UpdateAllowed();
            }
        }
    }

    /// <summary>
    /// The type of the restriction for extensions.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public RestrictionType ExtensionRestrictionType
    {
        get => _extensionRestrictionType;
        set
        {
            if (_extensionRestrictionType != value)
            {
                _extensionRestrictionType = value;
                UpdateAllowed();
            }
        }
    }

    /// <summary>
    /// The size for external file picker images that is used if <see cref="ExternalProviderRendering"/> is set to Image
    /// </summary>
    [Parameter] public MudExDimension ExternalPickerImageSize { get; set; } = new(62);

    /// <summary>
    /// The maximum file size allowed in bytes.
    /// </summary>
    [Parameter, SafeCategory("Validation")]
    public long? MaxFileSize { get; set; } = null;

    /// <summary>
    /// The maximum height allowed.
    /// </summary>
    [Parameter, SafeCategory("Validation")]
    public int MaxHeight { get; set; }

    /// <summary>
    /// The minimum height allowed.
    /// </summary>
    [Parameter, SafeCategory("Validation")]
    public int MinHeight { get; set; }

    /// <summary>
    /// The maximum number of multiple files allowed.
    /// </summary>
    [Parameter, SafeCategory("Validation")]
    public int MaxMultipleFiles { get; set; } = 100;

    /// <summary>
    /// The upload requests.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public IList<T> UploadRequests { get; set; }

    /// <summary>
    /// Defines whether multiple files can be uploaded.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowMultiple { get; set; } = true;

    /// <summary>
    /// Defines whether folder upload is allowed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowFolderUpload { get; set; } = true;

    /// <summary>
    /// Defines whether preview of the files is allowed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowPreview { get; set; } = true;

    /// <summary>
    /// Defines whether the file upload button is displayed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool ShowFileUploadButton { get; set; } = true;

    /// <summary>
    /// Defines whether the folder upload button is displayed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool ShowFolderUploadButton { get; set; } = true;

    /// <summary>
    /// Defines whether the clear button is displayed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool ShowClearButton { get; set; } = true;

    /// <summary>
    /// Defines whether removing of items is allowed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowRemovingItems { get; set; } = true;

    /// <summary>
    /// The mode of selecting items.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public SelectItemsMode SelectItemsMode { get; set; } = SelectItemsMode.None;

    /// <summary>
    /// Defines whether zip or other archives files should be automatically extracted.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AutoExtractArchive { get; set; } = false;

    /// <summary>
    /// The current upload request.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public T UploadRequest
    {
        get
        {
            if (UploadRequests == null || UploadRequests.Count == 0)
                return default!;
            return UploadRequests.First();
        }
        set
        {
            var list = UploadRequests ??= new List<T>();
            if (list.Count > 0)
                list[0] = value;
            else
                list.Add(value);
        }
    }

    /// <summary>
    /// Defines whether duplicates are allowed.
    /// If no duplicates are allowed and this is false please ensure <see cref="AutoLoadFileDataBytes"/> is true, otherwise the duplicate check will not work
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowDuplicates { get; set; }

    /// <summary>
    /// Defines whether errors should be displayed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool DisplayErrors { get; set; } = true;

    /// <summary>
    /// The selected requests.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public IList<T> SelectedRequests { get; set; }

    /// <summary>
    /// The time to remove an error after it has been displayed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public TimeSpan RemoveErrorAfter { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Defines whether errors should be automatically removed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AutoRemoveError { get; set; }

    /// <summary>
    /// Defines whether errors should be removed when there are changes.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool RemoveErrorOnChange { get; set; } = true;

    /// <summary>
    /// Defines whether file drop is allowed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowDrop { get; set; } = true;

    /// <summary>
    /// The callback that will be invoked when an error occurs.
    /// </summary>
    [Parameter, SafeCategory("Event")]
    public EventCallback<string> OnError { get; set; }

    /// <summary>
    /// The callback that will be invoked when upload requests change.
    /// </summary>
    [Parameter, SafeCategory("Event")]
    public EventCallback<IList<T>> UploadRequestsChanged { get; set; }

    /// <summary>
    /// The callback that will be invoked when an upload request is removed.
    /// </summary>
    [Parameter, SafeCategory("Event")]
    public EventCallback<T> UploadRequestRemoved { get; set; }

    /// <summary>
    /// The callback that will be invoked when an upload request changes.
    /// </summary>
    [Parameter, SafeCategory("Event")]
    public EventCallback<T> UploadRequestChanged { get; set; }

    /// <summary>
    /// The callback that will be invoked when selected requests change.
    /// </summary>
    [Parameter, SafeCategory("Event")]
    public EventCallback<IList<T>> SelectedRequestsChanged { get; set; }

    /// <summary>
    /// The function that will be invoked to handle preview content errors.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> HandlePreviewContentErrorFunc { get; set; }

    /// <summary>
    /// The function that will be invoked to resolve preview data URLs.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public Func<T, Task<string>> ResolvePreviewDataUrlFunc { get; set; }

    /// <summary>
    /// The function that will be invoked to resolve content types from URLs.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public Func<string, Task<string>> ResolveContentTypeFromUrlFunc { get; set; }

    /// <summary>
    /// If true file icons are colored
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public bool ColorizeIcons { get; set; }

    /// <summary>
    /// If true icons are colored
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public DropZoneClickAction DropZoneClickAction { get; set; }

    /// <summary>
    /// If this is true the file will be loaded into memory and the data will be available in the <see cref="UploadableFile.Data"/> property.
    /// Otherwise the data will only be loaded automatically when user clicks preview. 
    /// If false you can use <see cref="UploadableFile.EnsureDataLoadedAsync"/> or <see cref="EnsureDataLoadedAsync"/> to load the data manually when needed.
    /// Notice if you disable this, the duplicate from <see cref="AllowDuplicates"/> check will not work
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AutoLoadFileDataBytes { get; set; } = true;

    /// <summary>
    /// Set this to false to load the file data before adding the request.
    /// Otherwise data will be loaded in background.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool LoadFileDataBytesInBackground { get; set; } = true;

    /// <summary>
    /// If this is true then while the file is loading the progress bar will be shown.
    /// Otherwise the progress bar will be shown indeterminate what is faster for the UI.
    /// Notice this only works when <see cref="LoadFileDataBytesInBackground"/> is true
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool ShowProgressForLoadingData { get; set; } = false;

    #endregion


    [Inject] private MudExFileService FileService { get; set; }

    private string _errorMessage = string.Empty;
    private CancellationTokenSource _tokenSource;

    private InputFile _inputFile;
    private List<T> _withErrors = new();
    private string[] _extensions;
    private string[] _mimeTypes;
    private RestrictionType _mimeRestrictionType = RestrictionType.WhiteList;
    private RestrictionType _extensionRestrictionType = RestrictionType.WhiteList;
    private bool _loading;
    private HashSet<BrowserFileWithPath> _paths = new();
    private MudExGoogleFilePicker _googleFilePicker;
    private MudExOneDriveFilePicker _oneDriveFilePicker;
    private MudExDropBoxFilePicker _dropBoxFilePicker;
    private bool _urlDialogVisible;
    private string _externalUrl;

    bool HasValidDropZoneClickAction =>
        DropZoneClickAction != DropZoneClickAction.None
        && (DropZoneClickAction != DropZoneClickAction.UploadFolder || AllowFolderUpload)
        && (DropZoneClickAction != DropZoneClickAction.PickFromGoogleDrive || CanUseGoogleDrive)
        && (DropZoneClickAction != DropZoneClickAction.AddUrl || AllowExternalUrl)
        || (DropZoneClickAction == DropZoneClickAction.UploadFile);

    private ConcurrentDictionary<T, (Task Task, long Size, long ReadBytes)> _loadings = new();
    private bool IsLoading(T request) => _loadings.ContainsKey(request);
    private string FindPath(IBrowserFile file) => _paths?.FirstOrDefault(f => f.Name == file.Name)?.RelativePath ?? string.Empty;

    /// <inheritdoc />
    protected override Task OnInitializedAsync()
    {
        UploadFieldId ??= $"{nameof(MudExUploadEdit<T>)}-FileInput-{Guid.NewGuid().ToFormattedId()}";
        GoogleDriveClientId ??= MudExConfiguration.GoogleDriveClientId;
        OneDriveClientId ??= MudExConfiguration.OneDriveClientId;
        DropBoxApiKey ??= MudExConfiguration.DropBoxApiKey;
        return base.OnInitializedAsync();
    }


    /// <summary>
    /// Called from JS to update mappings
    /// </summary>
    [JSInvokable]
    public void UpdatePathMappings(BrowserFileWithPath[] files)
    {
        _paths.AddRange(files);
    }

    /// <inheritdoc />
    public override object[] GetJsArguments()
    {
        return new object[] { ElementReference, _inputFile.Element, AllowFolderUpload, CreateDotNetObjectReference() };
    }

    /// <inheritdoc />
    public override Task ImportModuleAndCreateJsAsync()
    {
        if (AllowDrop && !ReadOnly && _inputFile != null)
            return base.ImportModuleAndCreateJsAsync();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns whether the component has data.
    /// </summary>
    /// <returns></returns>
    public bool HasData()
    {
        return UploadRequests is { Count: > 0 } && UploadRequests.Any(x => (x.Data != null && x.Data.Any() || !string.IsNullOrWhiteSpace(x.Url)));
    }

    /// <summary>
    /// Ensures all data is loaded for all files.
    /// </summary>
    public Task EnsureDataLoadedAsync(HttpClient client = null) => Task.WhenAll(UploadRequests.Select(r => r.EnsureDataLoadedAsync(client)));

    private async Task UploadFiles(InputFileChangeEventArgs e)
    {
        if (AllowMultiple)
            await Task.WhenAll(e.GetMultipleFiles(MaxMultipleFiles).Select(Add));
        else
        {
            (UploadRequests ??= new List<T>()).Clear();
            await Add(e.File);
        }

        await RaiseChangedAsync();
    }

    private string StyleStr()
    {
        var str = $"{(MaxHeight != default ? $"max-height:{MaxHeight}px;" : string.Empty)} {(MinHeight != default ? $"min-height:{MinHeight}px;" : string.Empty)}";
        return $"{str}{Style}";
    }

    private void SetLoading(bool loading)
    {
        if (_loading != loading)
        {
            _loading = loading;
            StateHasChanged();
        }
    }

    private async Task Add(IEnumerable<IBrowserFile> files)
    {
        SetLoading(true);
        await Task.WhenAll(files.Select(Add));
        SetLoading(false);
    }

    private async Task Add(IBrowserFile file)
    {
        if (IsDisposed) return;

        if (IsAllowed(file))
        {
            if (AutoExtractArchive && file.IsArchive())
            {
                SetLoading(true);
                await Add(await GetZipEntriesAsync(file));
                return;
            }


            if (IsDisposed) return;

            var extension = Path.GetExtension(file.Name);

            var request = new T
            {
                Size = file.Size,
                FileName = file.Name,
                ContentType = BrowserFileExt.GetContentType(file),
                Extension = extension,
                Path = file is IArchivedBrowserFile fileInArchive ? fileInArchive.Path : FindPath(file)
            };
            
            await LoadDataIfAsync(file, request, LoadFileDataBytesInBackground);
            
            Add(request);
        }
    }

    private async Task LoadDataIfAsync(IBrowserFile file, T request, bool readInBackground)
    {
        byte[] buffer;
        if (!readInBackground && AutoLoadFileDataBytes)
        {
            await using var stream = file.OpenReadStream(file.Size);
            buffer = new byte[file.Size];
            await stream.ReadStreamInChunksAsync(buffer);
            request.Data = buffer;
        }
        else
        {
            (Task Task, long Size, int ReadBytes) loading = (null, file.Size, 0);
            var loadTask = new Func<Task>(async () =>
            {
                await using var stream = file.OpenReadStream(file.Size);
                buffer = new byte[file.Size];
                await stream.ReadStreamInChunksAsync(buffer, breakCondition: () => IsDisposed,
                    bytesReadCallback: !ShowProgressForLoadingData
                        ? null
                        : totalBytes =>
                        {
                            loading.ReadBytes = totalBytes;
                            _loadings.AddOrUpdate(request, loading);
                            CallStateHasChanged();
                        });
                request.Data = buffer;
                _loadings.Remove(request, out _);
                if (AutoLoadFileDataBytes && AlreadyExists(request, true)) 
                    Remove(request, true);

                await InvokeAsync(StateHasChanged);
            });
            if (request is UploadableFile uploadableFile)
            {
                uploadableFile.Size = file.Size;
                uploadableFile.LoadTask = loadTask;
            }

            if (AutoLoadFileDataBytes)
            {
                loading.Task = Task.Run(loadTask);
                _loadings.AddOrUpdate(request, loading);
            }
        }
    }
    
    private async Task<IList<IArchivedBrowserFile>> GetZipEntriesAsync(IBrowserFile file)
    {
        var data = await FileService.ReadArchiveAsync(file.OpenReadStream(file.Size), file.Name, BrowserFileExt.GetContentType(file));
        return data.List;
    }

    private bool IsAllowed(IBrowserFile file)
    {
        if (MaxFileSize != null && MaxFileSize.Value != default && MaxFileSize.Value > 0 && file.Size > MaxFileSize)
            return !SetError(TryLocalize(TextErrorMaxFileSize, BrowserFileExtensions.GetReadableFileSize(MaxFileSize.Value, LocalizerToUse), BrowserFileExtensions.GetReadableFileSize(file.Size - MaxFileSize.Value, LocalizerToUse), file.GetReadableFileSize(LocalizerToUse)));

        return IsExtensionAllowed(Path.GetExtension(file.Name)) && IsAllowed(BrowserFileExt.GetContentType(file));
    }

    private bool IsAllowed(IUploadableFile file)
    {
        if (MaxFileSize != null && MaxFileSize.Value != default && MaxFileSize.Value > 0 && file.Data.Length > MaxFileSize)
            return !SetError(TryLocalize(TextErrorMaxFileSize, BrowserFileExtensions.GetReadableFileSize(MaxFileSize.Value, LocalizerToUse), BrowserFileExtensions.GetReadableFileSize(file.Data.Length - MaxFileSize.Value, LocalizerToUse), BrowserFileExtensions.GetReadableFileSize(file.Data.Length, LocalizerToUse)));

        return IsExtensionAllowed(file.Extension) && IsAllowed(file.ContentType);
    }


    private string[] GetAllowedMimeTypes(bool calc)
    {
        var mimes = calc ? MimeType.AllTypes : MimeTypes;
        List<string> result;
        if (MimeRestrictionType == RestrictionType.WhiteList)
            result = MimeTypes?.Any() == true ? mimes.Where(m => MimeType.Matches(m, MimeTypes)).ToList() : new List<string>();
        else
            result = MimeTypes?.Any() == true ? mimes.Where(m => !MimeType.Matches(m, MimeTypes)).ToList() : new List<string>();

        if (ExtensionRestrictionType == RestrictionType.WhiteList && Extensions?.Any() == true)
            result.AddRange(Extensions.Select(MimeType.GetMimeType));
        else if (Extensions?.Any() == true)
            result = result.Except(Extensions.Select(MimeType.GetMimeType)).ToList();
        return result.Distinct().ToArray();
    }

    private string[] GetAllowedExtensions(bool calc)
    {
        var mimeTypes = calc ? MimeType.AllTypes : MimeTypes;

        var mimes = MimeTypes?.Any() == true ? mimeTypes.Where(m => MimeType.Matches(m, MimeTypes)).ToList() : new List<string>();
        List<string> result;
        if (MimeRestrictionType == RestrictionType.WhiteList)
        {
            result = mimes.Select(MimeType.GetExtension).ToList();
            result.AddRange(MimeTypes?.Where(MimeType.IsZip).Select(_ => "zip") ?? Array.Empty<string>());
            result.AddRange(MimeTypes?.Where(MimeType.IsRar).Select(_ => "rar") ?? Array.Empty<string>());
            result.AddRange(MimeTypes?.Where(MimeType.IsTar).Select(_ => "tar") ?? Array.Empty<string>());
        }
        else
            result = MimeTypes?.Any() == true ? mimeTypes.Select(MimeType.GetExtension).Except(MimeTypes.Select(MimeType.GetExtension)).ToList() : mimeTypes.Select(MimeType.GetExtension).ToList();

        if (ExtensionRestrictionType == RestrictionType.WhiteList && Extensions?.Any() == true)
            result.AddRange(Extensions);
        else if (Extensions?.Any() == true)
            result = result.Except(Extensions).ToList();

        return result.Distinct().ToArray();
    }

    private string _accept = "*";
    private string[] _allowedExtensions;
    private string[] _allowedMimeTypes;
    private void UpdateAllowed()
    {
        if (IsRendered)
        {
            _allowedExtensions = GetAllowedExtensions(true);
            _allowedMimeTypes = GetAllowedMimeTypes(true);

            var newAccept = GetAccept();
            if (newAccept != _accept)
            {
                _accept = newAccept;
                StateHasChanged();
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            UpdateAllowed();
        }
    }


    private string GetAccept()
    {
        return $"{string.Join(",", _allowedMimeTypes)},{string.Join(",", _allowedExtensions)}";
    }

    private bool IsExtensionAllowed(string extension)
    {
        if (!ExtensionAllowed(extension))
        {
            if (ExtensionRestrictionType == RestrictionType.WhiteList)
                return !SetError(TryLocalize(TextErrorExtensionNotAllowed, extension, string.Join(',', GetAllowedMimeTypes(false)), string.Join(',', GetAllowedExtensions(false))));
            return !SetError(TryLocalize(TextErrorExtensionForbidden, extension, string.Join(',', Extensions.Select(MimeType.GetMimeType)), string.Join(',', Extensions)));
        }

        return true;
    }

    private bool IsAllowed(string mimeType)
    {
        if (!MimeTypeAllowed(mimeType))
        {
            if (MimeRestrictionType == RestrictionType.WhiteList)
                return !SetError(TryLocalize(TextErrorMimeTypeNotAllowed, mimeType, string.Join(',', GetAllowedMimeTypes(false)), string.Join(',', GetAllowedExtensions(false))));
            return !SetError(TryLocalize(TextErrorMimeTypeForbidden, mimeType, string.Join(',', MimeTypes), string.Join(',', MimeTypes.Select(MimeType.GetExtension))));
        }

        if (UploadRequests?.Count >= Math.Max(1, MaxMultipleFiles))
            return !SetError(TryLocalize(TextErrorMaxFileCount, MaxMultipleFiles));

        return true;
    }

    private bool ExtensionAllowed(string extension)
    {
        if (Extensions?.Any() != true) return true;
        return _allowedExtensions.Any(e => string.Equals(e.EnsureStartsWith('.'), extension.EnsureStartsWith('.'), StringComparison.CurrentCultureIgnoreCase));
    }

    private bool MimeTypeAllowed(string mimeType)
    {
        if (MimeTypes?.Any() != true) return true;
        return MimeType.Matches(mimeType, _allowedMimeTypes) || (MimeRestrictionType == RestrictionType.WhiteList && MimeType.Matches(mimeType, MimeTypes));
    }

    private bool SetError(string message = default)
    {
        _tokenSource?.Cancel();
        _tokenSource = new CancellationTokenSource();
        var hasError = !string.IsNullOrWhiteSpace(message);
        _errorMessage = message;
        if (hasError)
        {
            if (AutoRemoveError)
                Task.Delay(RemoveErrorAfter).ContinueWith(_ => SetError(), _tokenSource.Token);
            OnError.InvokeAsync(_errorMessage);
        }
        else
        {
            _withErrors.Clear();
        }
        InvokeAsync(StateHasChanged);
        return hasError;
    }

    private Task RaiseChangedAsync()
    {
        return AllowMultiple
            ? UploadRequestsChanged.InvokeAsync(UploadRequests)
            : UploadRequestChanged.InvokeAsync(UploadRequest);
    }

    /// <summary>
    /// Removes the given request from the list of requests.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="keepError">if true errors will not removed independent of RemoveErrorOnChange flag</param>
    public void Remove(T request, bool keepError = false)
    {
        UploadRequests.Remove(request);
        UploadRequestRemoved.InvokeAsync(request);
        var pathEntry = _paths.FirstOrDefault(p => p.RelativePath == request.Path);
        if (pathEntry != null)
            _paths.Remove(pathEntry);
        if (RemoveErrorOnChange && !keepError)
            SetError();
        RaiseChangedAsync();
        CallStateHasChanged();
    }

    /// <summary>
    /// Removes all requests from the list of requests.
    /// </summary>
    public void RemoveAll()
    {
        var array = UploadRequests?.ToArray() ?? Array.Empty<T>();
        UploadRequests?.Clear();
        _paths.Clear();
        foreach (var item in array)
            UploadRequestRemoved.InvokeAsync(item);
        if (RemoveErrorOnChange)
            SetError();
        RaiseChangedAsync();
        CallStateHasChanged();
    }

    /// <summary>
    /// Display Upload File
    /// </summary>
    public Task Upload(MouseEventArgs arg) => Upload(null, arg);

    /// <summary>
    /// Display Upload File
    /// </summary>
    public async Task Upload(string id = null, MouseEventArgs arg = null) => await JsRuntime.InvokeVoidAsync("MudExEventHelper.clickElementById", id ?? UploadFieldId);

    /// <summary>
    /// Display Upload folder
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public async Task UploadFolder(MouseEventArgs arg) => await JsReference.InvokeVoidAsync("selectFolder");

    /// <summary>
    /// Returns true if the given request is selected
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public bool IsSelected(T request) => SelectedRequests?.Contains(request) == true;

    private async Task Select(T request, MouseEventArgs args)
    {
        if (SelectItemsMode != SelectItemsMode.None)
        {
            SelectedRequests ??= new List<T>();

            if (SelectedRequests.Contains(request) && SelectItemsMode == SelectItemsMode.Single)
            {
                SelectedRequests.Remove(request);
                return;
            }

            if (SelectItemsMode == SelectItemsMode.Single || (SelectItemsMode == SelectItemsMode.MultiSelectWithCtrlKey && !args.CtrlKey))
                SelectedRequests.Clear();

            if (SelectedRequests.Contains(request))
                SelectedRequests.Remove(request);
            else
                SelectedRequests.Add(request);

            await SelectedRequestsChanged.InvokeAsync(SelectedRequests);
        }
    }

    private string GetIcon(T request) => BrowserFileExt.IconForFile(request.ContentType);

    private MudExColor GetIconColor(T request) => ColorizeIcons ? BrowserFileExt.GetPreferredColor(request.ContentType) : Color.Inherit;


    private async Task Preview(T request)
    {
        if (request.Data == null /*&& string.IsNullOrWhiteSpace(request.Url)*/) // TODO: check if this is needed
        {
            _loadings.AddOrUpdate(request, (null, request.Size, 0));
            await request.EnsureDataLoadedAsync();
            _loadings.TryRemove(request, out _);
        }
        var parameters = new DialogParameters {
            { nameof(MudExFileDisplay.HandleContentErrorFunc), HandlePreviewContentErrorFunc },
            { nameof(MudExFileDisplay.Dense), true },
            { nameof(MudExFileDisplay.StreamUrlHandling), StreamUrlHandling },
            { nameof(MudExFileDisplay.ForceNativeRender), ForceNativeRender },
            { nameof(MudExFileDisplay.ColorizeIcons), ColorizeIcons },
            {
                nameof(MudExFileDisplay.ParametersForSubControls), new Dictionary<string, object>
                {
                    {nameof(MudExFileDisplayCode.Theme), CodeBlockTheme}
                }
            }
        };
        if (MudExFileDisplayZip.CanHandleFileAsArchive(request.ContentType) && request.Data != null)
        {
            using var ms = new MemoryStream(request.Data);
            var res = await DialogService.ShowFileDisplayDialog(ms, request.FileName, request.ContentType, null, parameters);
            await res.Result;
        }
        else
        {
            var dataUrl = await ResolvePreviewUrlAsync(request);
            await DialogService.ShowFileDisplayDialog(dataUrl, request.FileName, request.ContentType, null, parameters);
        }
    }

    /// <summary>
    /// resolves the preview data url
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    protected virtual async Task<string> ResolvePreviewUrlAsync(T request)
    {
        if (ResolvePreviewDataUrlFunc != null)
            return await ResolvePreviewDataUrlFunc(request);
        return (request.Url ?? await FileService.CreateDataUrlAsync(request.Data, request.ContentType, StreamUrlHandling == StreamUrlHandling.BlobUrl));
    }

    private bool IsValidUrl(string s) => Uri.TryCreate(s, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    
    private void AddUrl() => _urlDialogVisible = !_urlDialogVisible;

    /// <summary>
    /// Resolve content type
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    protected virtual Task<string> ResolveContentTypeFromUrlAsync(string url)
    {
        if (ResolveContentTypeFromUrlFunc != null)
            return ResolveContentTypeFromUrlFunc(url);
        string urlExtension = Path.GetExtension(url);
        if (!string.IsNullOrWhiteSpace(urlExtension))
            return Task.FromResult(MimeType.GetMimeType(urlExtension));
        return MimeType.ReadMimeTypeFromUrlAsync(url);
    }

    private async Task Add(string url)
    {
        var contentType = await ResolveContentTypeFromUrlAsync(url);
        var request = new T
        {
            Extension = Path.GetExtension(url),
            ContentType = contentType ?? "application/octet-stream",
            FileName = Path.GetFileName(url),
            Data = Array.Empty<byte>(),
            Url = url
        };
        if (IsAllowed(request.ContentType))
        {
            Add(request);
            await RaiseChangedAsync();
        }
    }

    private bool AlreadyExists(T request, bool setError)
    {
        var existing = AllowDuplicates || UploadRequests == null || UploadRequests.Count == 0 ? default : UploadRequests
            .Where(r => (r.Data != null && request.Data != null && r.Data.SequenceEqual(request.Data)) || (!string.IsNullOrWhiteSpace(r.Url) && !string.IsNullOrWhiteSpace(request.Url) && r.Url == request.Url))
            .FirstOrDefault(r => !EqualityComparer<T>.Default.Equals(r, request));
        if (!EqualityComparer<T>.Default.Equals(existing, default))
        {
            if (setError)
            {
                _withErrors.Add(existing);
                SetError(TryLocalize(TextErrorDuplicateFile, request.FileName));
            }
            return true;
        }

        return false;
    }

    private void Add(T request)
    {
        if (!AllowMultiple)
            (UploadRequests ??= new List<T>()).Clear();

        if (AlreadyExists(request, true))
            return;

        if (RemoveErrorOnChange)
            SetError();

        (UploadRequests ??= new List<T>()).Add(request);
        StateHasChanged();
    }

    private string GetTextDropZone()
    {
        if (TextDropZone != DropPlaceholderText || IsLocalized(TextDropZone))
            return TryLocalize(TextDropZone);
        return AllowDrop switch
        {
            true when AllowFolderUpload => TryLocalize("Drop/Paste files or folders here"),
            true => TryLocalize("Drop/Paste files here"),
            _ => AllowFolderUpload ? TryLocalize("Drop/Paste folders here") : TryLocalize("Drop/Paste is not allowed")
        };
    }

    /// <summary>
    /// Executes the DropZone Click Action
    /// </summary>
    public async Task DropZoneClick(string id)
    {
        if (DropZoneClickAction == DropZoneClickAction.UploadFile)
            await Upload(id);
        else if (DropZoneClickAction == DropZoneClickAction.UploadFolder && AllowFolderUpload)
            await UploadFolder(null);
        else if (DropZoneClickAction == DropZoneClickAction.AddUrl && AllowExternalUrl)
            AddUrl();
        else if (DropZoneClickAction == DropZoneClickAction.PickFromGoogleDrive && CanUseGoogleDrive)
            await _googleFilePicker.PickAsync();
        else if (DropZoneClickAction == DropZoneClickAction.PickFromOneDrive && CanUseOneDrive)
            await _oneDriveFilePicker.PickAsync();
        else if (DropZoneClickAction == DropZoneClickAction.PickFromDropBox && CanUseDropBox)
            await _dropBoxFilePicker.PickAsync();
    }

    private Task Add(IEnumerable<IUploadableFile> items)
    {
        foreach (var i in items)
        {
            var request = new T
            {
                Size = i.Size,
                Data = i.Data,
                FileName = i.FileName,
                ContentType = i.ContentType,
                Extension = i.Extension,
                Path = i.Path,
                Url = i.Data == null || i.Data.Length == 0 ? i.Url : null
            };
            if (request is UploadableFile uploadableFile)
                uploadableFile.LoadTask = () => i.EnsureDataLoadedAsync();
            
            if (IsAllowed(request))
                Add(request);
        }
        _urlDialogVisible = false;
        return RaiseChangedAsync();
    }

    private PickerActionViewMode ExternalPickerIconsActionViewMode() => ExternalProviderRendering is ExternalProviderRendering.ImagesNewLine or ExternalProviderRendering.Images or ExternalProviderRendering.IntegratedInDialogAsImages ? PickerActionViewMode.Image : PickerActionViewMode.Button;
    private bool RemoveColorsFromExternalPickerIcons() => !ColoredImagesForExternalFilePicker;
    private bool CanUseGoogleDrive => AllowGoogleDrive && !string.IsNullOrEmpty(GoogleDriveClientId); 
    private bool CanUseDropBox => AllowDropBox && !string.IsNullOrEmpty(DropBoxApiKey); 
    private bool CanUseOneDrive => AllowOneDrive && !string.IsNullOrEmpty(OneDriveClientId);
    private bool RenderPickerInDialog => ExternalProviderRendering is ExternalProviderRendering.IntegratedInDialogAsButtons or ExternalProviderRendering.IntegratedInDialogAsImages;
    private bool AnyExternalFilePicker() => CanUseGoogleDrive || CanUseDropBox || CanUseOneDrive;
}
