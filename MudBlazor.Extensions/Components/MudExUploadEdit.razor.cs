using System.IO.Compression;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Nextended.Core.Extensions;
using MudBlazor.Extensions.Extensions;
using MudBlazor.Extensions.Options;
using Nextended.Blazor.Extensions;
using Nextended.Core;
using BrowserFileExtensions = Nextended.Blazor.Extensions.BrowserFileExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Nextended.Blazor.Models;
using MudBlazor.Extensions.Helper;


namespace MudBlazor.Extensions.Components;

public partial class MudExUploadEdit<T> : IAsyncDisposable where T: IUploadableFile, new()
{
    [Inject] private IServiceProvider _serviceProvider { get; set; }
    private IJSRuntime _jsRuntime => _serviceProvider.GetService<IJSRuntime>();
    private IDialogService _dialogService => _serviceProvider.GetService<IDialogService>();
    private IStringLocalizer _localizer => Localizer ?? _serviceProvider.GetService<IStringLocalizer<MudExUploadEdit<T>>>() ?? _serviceProvider.GetService<IStringLocalizer>();
    
    [Parameter] public IStringLocalizer Localizer { get; set; }
    [Parameter] public string TextDropZone { get; set; } = "Drop files here";
    [Parameter] public string TextUploadFiles { get; set; } = "Upload Files";
    [Parameter] public string TextUploadFile { get; set; } = "Upload File";
    [Parameter] public string TextUploadFolder { get; set; } = "Upload Folder";
    [Parameter] public string TextAddUrl { get; set; } = "Add Url";
    [Parameter] public string TextRemoveAll { get; set; } = "Remove All";
    [Parameter] public string TextErrorDuplicateFile { get; set; } = "The file ({0}) has already been added";
    [Parameter] public string TextErrorMaxFileSize { get; set; } = "The file has exceeded the maximum size of {0} with {1}. File size is {2}";
    [Parameter] public string TextErrorMaxFileCount { get; set; } = "A maximum of {0} files are allowed";
    [Parameter] public string TextErrorMimeTypeNotAllowed { get; set; } = "Files of this type ({0}) are not allowed. Only following types are allowed '{1}'. Try one of these extensions ({2})";
    [Parameter] public string TextErrorMimeTypeNotForbidden { get; set; } = "Files of this type ({0}) are not allowed. Following types are forbidden '{1}' ({2})";
    [Parameter] public string TextAddUrlTitle { get; set; } = "Add external Url";
    [Parameter] public string TextAddUrlMessage { get; set; } = "Enter the URL to existing file";
    [Parameter] public AnimationType ErrorAnimation { get; set; } = AnimationType.Pulse;
    [Parameter] public string Label { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public string HelperText { get; set; }
    [Parameter] public Variant Variant { get; set; }
    [Parameter] public bool AllowRename { get; set; } = true;
    [Parameter] public bool AllowExternalUrl { get; set; } = true;
    [Parameter] public string UploadFieldId { get; set; }

    [Parameter]
    public string[] MimeTypes
    {
        get => _mimeTypes;
        set
        {
            _mimeTypes = value;
            UpdateAcceptInfo();
        }
    }

    [Parameter] public MimeTypeRestrictionType MimeRestrictionType { get; set; } = MimeTypeRestrictionType.WhiteList;
    [Parameter] public long? MaxFileSize { get; set; } = null;
    [Parameter] public int MaxHeight { get; set; }
    [Parameter] public int MinHeight { get; set; }

    [Parameter] public string Class { get; set; }
    [Parameter] public string Style { get; set; }

    [Parameter] public int MaxMultipleFiles { get; set; } = 100;
    [Parameter] public IList<T> UploadRequests { get; set; }
    [Parameter] public bool AllowMultiple { get; set; } = true;
    [Parameter] public bool AllowFolderUpload { get; set; } = true;
    [Parameter] public bool AllowPreview { get; set; } = true;
    [Parameter] public bool ShowFileUploadButton { get; set; } = true;
    [Parameter] public bool ShowFolderUploadButton { get; set; } = true;
    [Parameter] public bool ShowClearButton { get; set; } = true;
    [Parameter] public bool AllowRemovingItems { get; set; } = true;
    [Parameter] public SelectItemsMode SelectItemsMode { get; set; } = SelectItemsMode.None;
    [Parameter] public bool AutoExtractZip { get; set; } = false;

    [Parameter]
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

    [Parameter] public bool AllowDuplicates { get; set; }
    [Parameter] public bool DisplayErrors { get; set; } = true;
    [Parameter] public IList<T> SelectedRequests { get; set; }
    [Parameter] public TimeSpan RemoveErrorAfter { get; set; } = TimeSpan.FromSeconds(5);
    [Parameter] public bool AutoRemoveError { get; set; }
    [Parameter] public bool RemoveErrorOnChange { get; set; } = true;
    [Parameter] public bool AllowDrop { get; set; } = true;

    [Parameter] public EventCallback<string> OnError { get; set; }
    [Parameter] public EventCallback<IList<T>> UploadRequestsChanged { get; set; }
    [Parameter] public EventCallback<T> UploadRequestRemoved { get; set; }
    [Parameter] public EventCallback<T> UploadRequestChanged { get; set; }
    [Parameter] public EventCallback<IList<T>> SelectedRequestsChanged { get; set; }
    
    [Parameter] public Func<IFileDisplayInfos, Task<ContentErrorResult>> HandlePreviewContentErrorFunc { get; set; }

    [Parameter]
    public Func<T, Task<string>> ResolvePreviewDataUrlFunc { get; set; }

    [Parameter]
    public Func<string, Task<string>> ResolveContentTypeFromUrlFunc { get; set; }

    public bool HasData()
    {
        return UploadRequests is { Count: > 0 } && UploadRequests.Any(x => (x.Data != null && x.Data.Any() || !string.IsNullOrWhiteSpace(x.Url)));
    }

    private string _errorMessage = string.Empty;
    private CancellationTokenSource _tokenSource;
    private ElementReference dropZoneElement;
    private InputFile inputFile;
    private IJSObjectReference _module;
    private IJSObjectReference _dropZoneInstance;
    private List<T> _withErrors = new();
    private string _accept;
    private string _acceptExtensions;
    private string[] _mimeTypes;

    protected override Task OnInitializedAsync()
    {
        UploadFieldId ??= $"{nameof(MudExUploadEdit<T>)}-FileInput-{Guid.NewGuid()}";
        UpdateAcceptInfo();
        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if ((firstRender || _module == null || _dropZoneInstance == null) && AllowDrop && !ReadOnly && inputFile != null)
        {

            var references = await _jsRuntime.ImportModuleAndCreateJsAsync<MudExUploadEdit<T>>(dropZoneElement, inputFile.Element, AllowFolderUpload);
            _module = references.moduleReference;
            _dropZoneInstance = references.jsObjectReference;
        }
    }

    private void UpdateAcceptInfo()
    {
        _accept = string.Join(",", (MimeTypes ?? Array.Empty<string>()).Distinct());
        var extensions = (MimeTypes?.Select(MimeType.GetExtension) ?? Array.Empty<string>()).ToList();
        if (MimeTypes?.Any(MimeType.IsZip) == true)
            extensions.Add(".zip");
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
        if (IsAllowed(file))
        {
            if (AutoExtractZip && file.IsZipFile())
            {
                await Add(await GetZipEntriesAsync(file));
                return;
            }

            var buffer = new byte[file.Size];
            var extension = Path.GetExtension(file.Name);
            await file.OpenReadStream(file.Size).ReadAsync(buffer);
            var request = new T { Data = buffer, FileName = file.Name, ContentType = file.ContentType, Extension = extension };
            Add(request);
        }
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
            return !SetError(_localizer.TryLocalize(TextErrorMaxFileSize, BrowserFileExtensions.GetReadableFileSize(MaxFileSize.Value, _localizer), BrowserFileExtensions.GetReadableFileSize(file.Size - MaxFileSize.Value, _localizer), file.GetReadableFileSize(_localizer)));

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
                return !SetError(_localizer.TryLocalize(TextErrorMimeTypeNotAllowed, mimeType, _accept, _acceptExtensions));
            return !SetError(_localizer.TryLocalize(TextErrorMimeTypeNotForbidden, mimeType, _accept, _acceptExtensions));
        }

        if (UploadRequests?.Count >= Math.Max(1, MaxMultipleFiles))
            return !SetError(_localizer.TryLocalize(TextErrorMaxFileCount, MaxMultipleFiles));

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


    public async ValueTask DisposeAsync()
    {
        if (_dropZoneInstance != null)
        {
            await _dropZoneInstance.InvokeVoidAsync("dispose");
            await _dropZoneInstance.DisposeAsync();
        }

        if (_module != null)
            await _module.DisposeAsync();

    }
    
    public void Remove(T request)
    {
        UploadRequests.Remove(request);
        UploadRequestRemoved.InvokeAsync(request);
        if (RemoveErrorOnChange)
            SetError();
        RaiseChangedAsync();
        StateHasChanged();
    }

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

    public Task Upload(MouseEventArgs arg = null)
    {
        //return _jsRuntime.InvokeVoidAsync("MudExBrowserHelper.clickOnElement", "#" + UploadFieldId).AsTask();
        return _jsRuntime.InvokeVoidAsync($"(document.querySelector('#{UploadFieldId}'))?.click()").AsTask();
    }

    public async Task UploadFolder(MouseEventArgs arg)
    {
        await _dropZoneInstance.InvokeVoidAsync("selectFolder");
    }

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


    private async Task Preview(T request)
    {
        if (MimeType.IsZip(request.ContentType) && request.Data != null)
        {
            var ms = new MemoryStream(request.Data);
            await _dialogService.ShowFileDisplayDialog(ms, request.FileName, request.ContentType);
        }
        else
        {
            // TODO: Maybe ... some fn to get the preview of the file
            //var dataUrl = _navigationManager.ToAbsoluteServerUri(request.Url ?? await DataUrl.GetDataUrlAsync(request.Data, request.ContentType));
            var dataUrl = await ResolvePreviewUrlAsync(request);
            await _dialogService.ShowFileDisplayDialog(dataUrl, request.FileName, request.ContentType, HandlePreviewContentErrorFunc);
        }
    }

    protected virtual async Task<string> ResolvePreviewUrlAsync(T request)
    {
        if(ResolvePreviewDataUrlFunc != null)
            return await ResolvePreviewDataUrlFunc(request);
        return (request.Url ?? await DataUrl.GetDataUrlAsync(request.Data, request.ContentType));
    }

    private bool IsValidUrl(string s) => Uri.TryCreate(s, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

    private async Task AddUrl()
    {
        var parameters = new DialogParameters
        {
            {nameof(MudExPromptDialog.Message), _localizer.TryLocalize(TextAddUrlMessage)},
            {nameof(MudExPromptDialog.Icon), Icons.Material.Filled.Web},
            {nameof(MudExPromptDialog.OkText), _localizer.TryLocalize(TextAddUrl)},
            {nameof(MudExPromptDialog.CanConfirm), IsValidUrl},
            {nameof(MudExPromptDialog.Value), string.Empty},
        };

        var options = new DialogOptionsEx { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, Animations = new[] { AnimationType.FlipX } };

        var res = await _dialogService.ShowEx<MudExPromptDialog>(_localizer.TryLocalize(TextAddUrlTitle), parameters, options);
        var dialogResult = (await res.Result);

        if (!dialogResult.Cancelled && dialogResult.Data != null && IsValidUrl(dialogResult.Data.ToString()))
            await Add(dialogResult.Data.ToString());

    }

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
            SetError(_localizer.TryLocalize(TextErrorDuplicateFile, request.FileName));
            return;
        }

        if (RemoveErrorOnChange)
            SetError();

        (UploadRequests ??= new List<T>()).Add(request);
        StateHasChanged();
    }
}
