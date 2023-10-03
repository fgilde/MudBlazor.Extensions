using System.Diagnostics;
using System.IO.Compression;
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

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A Component to edit and upload a list of files
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class MudExUploadEdit<T> where T : IUploadableFile, new()
{
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
    public string TextDropZone { get; set; } = "Drop files here";

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
    /// Extensions for FileRestrictions based on the <see cref="ExtensionsRestrictionType"/> this types are allowed or forbidden.
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
    /// If true icons are colored
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public bool ColorizeIcons { get; set; }


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

    /// <inheritdoc />
    protected override Task OnInitializedAsync()
    {
        UploadFieldId ??= $"{nameof(MudExUploadEdit<T>)}-FileInput-{Guid.NewGuid()}";
        return base.OnInitializedAsync();
    }

    /// <inheritdoc />
    public override object[] GetJsArguments()
    {
        return new object[] { ElementReference, _inputFile.Element, AllowFolderUpload };
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

            byte[] buffer;
            using (var stream = file.OpenReadStream(file.Size))
            {
                buffer = new byte[file.Size];
                await ReadStreamInChunksAsync(stream, buffer);
            }

            if (IsDisposed) return;

            var extension = Path.GetExtension(file.Name);
            var request = new T
            {
                Data = buffer,
                FileName = file.Name,
                ContentType = BrowserFileExt.GetContentType(file),
                Extension = extension
            };
            Add(request);
        }
    }

    private async Task ReadStreamInChunksAsync(Stream stream, byte[] buffer)
    {
        const int chunkSize = 4096;
        int bytesRead;
        int totalBytesRead = 0;

        do
        {
            if (IsDisposed) return;

            bytesRead = await stream.ReadAsync(buffer, totalBytesRead, Math.Min(chunkSize, buffer.Length - totalBytesRead));
            totalBytesRead += bytesRead;
        }
        while (bytesRead > 0 && totalBytesRead < buffer.Length);
    }

    [Inject] private MudExFileService fileService { get; set; }
    private async Task<IList<IArchivedBrowserFile>> GetZipEntriesAsync(IBrowserFile file)
    {
        var data = await fileService.ReadArchiveAsync(file.OpenReadStream(file.Size), file.Name, BrowserFileExt.GetContentType(file));
        return data.List;
    }

    private bool IsAllowed(IBrowserFile file)
    {
        if (MaxFileSize != null && MaxFileSize.Value != default && MaxFileSize.Value > 0 && file.Size > MaxFileSize)
            return !SetError(TryLocalize(TextErrorMaxFileSize, BrowserFileExtensions.GetReadableFileSize(MaxFileSize.Value, LocalizerToUse), BrowserFileExtensions.GetReadableFileSize(file.Size - MaxFileSize.Value, LocalizerToUse), file.GetReadableFileSize(LocalizerToUse)));

        return IsExtensionAllowed(Path.GetExtension(file.Name)) && IsAllowed(BrowserFileExt.GetContentType(file));
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
        StateHasChanged();
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
    public void Remove(T request)
    {
        UploadRequests.Remove(request);
        UploadRequestRemoved.InvokeAsync(request);
        if (RemoveErrorOnChange)
            SetError();
        RaiseChangedAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Removes all requests from the list of requests.
    /// </summary>
    public void RemoveAll()
    {
        var array = UploadRequests?.ToArray() ?? Array.Empty<T>();
        UploadRequests?.Clear();
        foreach (var item in array)
            UploadRequestRemoved.InvokeAsync(item);
        if (RemoveErrorOnChange)
            SetError();
        RaiseChangedAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Display Upload File
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public async Task Upload(MouseEventArgs arg = null)
    {
        //return _jsRuntime.InvokeVoidAsync("MudExBrowserHelper.clickOnElement", "#" + UploadFieldId).AsTask();
        await JsRuntime.InvokeVoidAsync($"(document.querySelector('#{UploadFieldId}'))?.click()").AsTask();
    }

    /// <summary>
    /// Display Upload folder
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public async Task UploadFolder(MouseEventArgs arg)
    {
        await JsReference.InvokeVoidAsync("selectFolder");
    }

    /// <summary>
    /// Returns true if the given request is selected
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public bool IsSelected(T request)
    {
        return SelectedRequests?.Contains(request) == true;
    }

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

    private string GetIcon(T request)
    {
        return BrowserFileExt.IconForFile(request.ContentType);
    }

    private MudExColor GetIconColor(T request)
    {
        return ColorizeIcons ? BrowserFileExt.GetPreferredColor(request.ContentType) : Color.Inherit;
    }


    private async Task Preview(T request)
    {
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
            // TODO: Maybe ... some fn to get the preview of the file          
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

    private async Task AddUrl()
    {
#pragma warning disable CS8974 // Converting method group to non-delegate type
        var parameters = new DialogParameters
        {
            {nameof(MudExPromptDialog.Message), TryLocalize(TextAddUrlMessage)},
            {nameof(MudExPromptDialog.Icon), Icons.Material.Filled.Web},
            {nameof(MudExPromptDialog.OkText), TryLocalize(TextAddUrl)},
            {nameof(MudExPromptDialog.CanConfirm), IsValidUrl},
            {nameof(MudExPromptDialog.Value), string.Empty},
        };
#pragma warning restore CS8974 // Converting method group to non-delegate type

        var options = new DialogOptionsEx { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, Animations = new[] { AnimationType.FlipX } };

        var res = await DialogService.ShowEx<MudExPromptDialog>(TryLocalize(TextAddUrlTitle), parameters, options);
        var dialogResult = (await res.Result);

        if (!dialogResult.Canceled && dialogResult.Data != null && IsValidUrl(dialogResult.Data.ToString()))
            await Add(dialogResult.Data.ToString());

    }

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

    private void Add(T request)
    {
        if (!AllowMultiple)
            (UploadRequests ??= new List<T>()).Clear();

        var existing = AllowDuplicates || UploadRequests == null || UploadRequests.Count == 0 ? default : UploadRequests.FirstOrDefault(r => (r.Data != null && request.Data != null && r.Data.SequenceEqual(request.Data)) || (!string.IsNullOrWhiteSpace(r.Url) && !string.IsNullOrWhiteSpace(request.Url) && r.Url == request.Url));
        if (!EqualityComparer<T>.Default.Equals(existing, default))
        {
            _withErrors.Add(existing);
            SetError(TryLocalize(TextErrorDuplicateFile, request.FileName));
            return;
        }

        if (RemoveErrorOnChange)
            SetError();

        (UploadRequests ??= new List<T>()).Add(request);
        StateHasChanged();
    }
}
