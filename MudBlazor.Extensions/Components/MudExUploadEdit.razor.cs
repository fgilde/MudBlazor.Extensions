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

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A Component to edit and upload a list of files
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class MudExUploadEdit<T> where T: IUploadableFile, new()
{

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
    public string TextErrorMimeTypeNotAllowed { get; set; } = "Files of this type ({0}) are not allowed. Only following types are allowed '{1}'. Try one of these extensions ({2})";

    /// <summary>
    /// The error text displayed when a file's MIME type is forbidden.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string TextErrorMimeTypeNotForbidden { get; set; } = "Files of this type ({0}) are not allowed. Following types are forbidden '{1}' ({2})";

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
            _mimeTypes = value;
            UpdateAcceptInfo();
        }
    }

    /// <summary>
    /// The type of the MIME restriction.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public MimeTypeRestrictionType MimeRestrictionType { get; set; } = MimeTypeRestrictionType.WhiteList;

    /// <summary>
    /// The maximum file size allowed.
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
    /// Defines whether zip files should be automatically extracted.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AutoExtractZip { get; set; } = false;

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
    private string _accept;
    private string _acceptExtensions;
    private string[] _mimeTypes;


    /// <inheritdoc />
    protected override Task OnInitializedAsync()
    {
        UploadFieldId ??= $"{nameof(MudExUploadEdit<T>)}-FileInput-{Guid.NewGuid()}";
        UpdateAcceptInfo();
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

    private void UpdateAcceptInfo()
    {
        _accept = string.Join(",", (MimeTypes ?? Array.Empty<string>()).Distinct());
        var extensions = (MimeTypes?.Select(MimeType.GetExtension) ?? Array.Empty<string>()).ToList();
        if (MimeTypes?.Any(MimeType.IsZip) == true)
            extensions.Add(".zip");
        if (MimeTypes?.Any(MimeType.IsRar) == true)
            extensions.Add(".rar");
        if (MimeTypes?.Any(MimeType.IsTar) == true)
            extensions.Add(".tar");
        _acceptExtensions = string.Join(",", extensions.Distinct());
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

    private Task Add(IEnumerable<IBrowserFile> files)
    {
        return Task.WhenAll(files.Select(Add));
    }

    private async Task Add(IBrowserFile file)
    {
        if (IsDisposed) return;

        if (IsAllowed(file))
        {
            if (AutoExtractZip && file.IsZipFile())
            {
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
                ContentType = file.ContentType,
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

    private async Task<IList<ZipBrowserFile>> GetZipEntriesAsync(IBrowserFile file)
    {
        var stream = file.OpenReadStream(file.Size);
        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        return new ZipArchive(ms).Entries.Select(entry => new ZipBrowserFile(entry)).ToList();
    }

    private bool IsAllowed(IBrowserFile file)
    {
        if (MaxFileSize != null && MaxFileSize.Value != default && MaxFileSize.Value > 0 && file.Size > MaxFileSize)
            return !SetError(TryLocalize(TextErrorMaxFileSize, BrowserFileExtensions.GetReadableFileSize(MaxFileSize.Value, LocalizerToUse), BrowserFileExtensions.GetReadableFileSize(file.Size - MaxFileSize.Value, LocalizerToUse), file.GetReadableFileSize(LocalizerToUse)));

        return IsAllowed(file.ContentType);
    }

    private string GetAccept()
    {
        if (MimeRestrictionType == MimeTypeRestrictionType.WhiteList)
            return $"{_accept},{_acceptExtensions}";
        return "*";
    }

    private bool IsAllowed(string mimeType)
    {
        if (!MimeTypeAllowed(mimeType))
        {
            if (MimeRestrictionType == MimeTypeRestrictionType.WhiteList)
                return !SetError(TryLocalize(TextErrorMimeTypeNotAllowed, mimeType, _accept, _acceptExtensions));
            return !SetError(TryLocalize(TextErrorMimeTypeNotForbidden, mimeType, _accept, _acceptExtensions));
        }

        if (UploadRequests?.Count >= Math.Max(1, MaxMultipleFiles))
            return !SetError(TryLocalize(TextErrorMaxFileCount, MaxMultipleFiles));

        return true;
    }

    private bool MimeTypeAllowed(string mimeType)
    {
        if (MimeTypes?.Any() != true) return true;
        var hasMatched = MimeType.Matches(mimeType, MimeTypes);
        return (MimeRestrictionType != MimeTypeRestrictionType.WhiteList || hasMatched) && (MimeRestrictionType != MimeTypeRestrictionType.BlackList || !hasMatched);
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
            { nameof(MudExFileDisplay.ColorizeIcons), ColorizeIcons }
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
        if(ResolvePreviewDataUrlFunc != null)
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
