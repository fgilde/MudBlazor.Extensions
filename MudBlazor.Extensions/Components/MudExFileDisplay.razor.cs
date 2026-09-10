using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using Nextended.Blazor.Helper;
using MudBlazor.Extensions.Services;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Options;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;
using MudBlazor.Interop;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to display a file in a iframe or object tag or in a registered viewer that implements IMudExFileDisplay
/// </summary>
public partial class MudExFileDisplay : IMudExFileDisplayInfos
{

    #region private fields
    
    private (MudExDimension? Dimension, MudExPosition? Position)? _lastInfoDialogLocation;
    private DynamicComponent _currentFileDisplay;
    private bool _internalCall;
    private bool _isNativeRendered;
    private string _id = Guid.NewGuid().ToString();
    private (string tag, Dictionary<string, object> attributes)? renderInfos;
    private BrowserInfo _info;
    private bool _internalOverwrite;
    private List<IMudExFileDisplay> _possibleRenderControls;

    // Replacement for the original (Type, bool, IDictionary<...>) ValueTuple — ValueTuple
    // elements cannot carry [DynamicallyAccessedMembers] so the trimmer would drop the
    // members needed when the Type is rendered via DynamicComponent. The mutable record
    // class allows ControlType to be cleared without re-allocating.
    private sealed class ComponentForFileInfo
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type ControlType { get; set; }

        public bool ShouldAddDiv { get; init; }

        public IDictionary<string, object> Parameters { get; init; }
    }

    private ComponentForFileInfo _componentForFile;
    private Stream _contentStream;
    private bool _errorClosed;
    private CancellationTokenSource _componentCts = new();
    private long? _fileMetaSize;
    private IDictionary<string, object> _fileMetaInformation;
    private object _fileMetaSourceKey;
    private int _metaProbes;

    [Inject] private IJsApiService JsApiService { get; set; }

    /// <summary>
    /// Reference to the FileService
    /// </summary>
    [Inject] public MudExFileService FileService { get; set; }

    #endregion

    #region Parameters and Properties

    /// <summary>
    /// Status text to display in center of file display
    /// </summary>
    //[Parameter, SafeCategory("Appearance")]
    public string StatusText { get; private set; }

    /// <summary>
    /// Specify types of IMudExFileDisplay that should be ignored
    /// </summary>
    [Parameter]
    public Type[] IgnoredRenderControls { get; set; }

    /// <summary>
    /// When true the name of current render control is displayed on the top right
    /// </summary>
    [Parameter]
    public bool ShowRenderer { get; set; }

    /// <summary>
    /// When true the Filename is displayed on the top right
    /// </summary>
    [Parameter]
    public bool ShowFileName { get; set; }

    /// <summary>
    /// How to handle the stream url
    /// </summary>
    [Parameter, SafeCategory("Behaviour")]
    public StreamUrlHandling StreamUrlHandling { get; set; } = StreamUrlHandling.BlobUrl;

    /// <summary>
    /// You can set this object with any simple data object that then is used to display file infos
    /// </summary>
    [Parameter]
    [SafeCategory("Data")]
    public object FileInfo { get; set; }

    /// <summary>
    /// If true, compact vertical padding will be applied to items.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool Dense { get; set; }

    /// <summary>
    /// Url to access file can also be a data Url
    /// </summary>
    [Parameter]
    [SafeCategory("Data")]
    public string Url { get; set; }

    /// <summary>
    /// ContentType of loaded file
    /// </summary>
    [Parameter]
    [SafeCategory("Data")]
    public string ContentType { get; set; }

    /// <summary>
    /// Can close file
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool CanClose { get; set; }

    /// <summary>
    /// Event for on close click
    /// </summary>
    [Parameter]
    [SafeCategory("Click action")]
    public EventCallback OnCloseClick { get; set; }

    /**
     * Should be true if file is not a binary one
     */
    [Parameter]
    [SafeCategory("Validation")]
    public bool FallBackInIframe { get; set; }

    /// <summary>
    /// Set this to false to show everything in iframe/object tag otherwise zip, images audio and video will displayed in correct tags
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool ViewDependsOnContentType { get; set; } = true;

    /// <summary>
    /// Set this to true to initially render native and ignore registered IMudExFileDisplay
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool ForceNativeRender { get; set; }

    /// <summary>
    /// Set to true to use image as background-url instead of img tag
    /// </summary>
    [Parameter]
    [SafeCategory("Appearance")]
    public bool ImageAsBackgroundImage { get; set; } = false;

    /// <summary>
    /// Set to true to display content error is content can't displayed
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool ShowContentError { get; set; } = true;

    /// <summary>
    /// Set to true to use sandbox mode on iframe to disallow some danger js invocation
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool SandBoxIframes { get; set; } = true;

    /// <summary>
    /// Set to true to allow user to download the loaded file
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool AllowDownload { get; set; } = true;

    /// <summary>
    /// Set to true to allow user to download the loaded file and specify directly a folder to save file in
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool AllowSaveAs { get; set; } = true;

    /// <summary>
    /// Set to true to allow user to copy the file url to clipboard
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool AllowCopyUrl { get; set; } = true;

    /// <summary>
    /// Set to true to allow user to open url in new tab
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")]
    public bool AllowOpenInNewTab { get; set; } = true;

    /// <summary>
    /// Filename
    /// </summary>
    [Parameter]
    [SafeCategory("Common")]
    public string FileName { get; set; }

    /// <summary>
    /// Content stream of the file.
    /// Note: This stream should not be closed or disposed.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public Stream ContentStream
    {
        get => _contentStream;
        set
        {
            if (_contentStream == value) return;
            _contentStream = value;
            Url = _contentStream != null ? null : Url;
        }
    }

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
    /// Set to true to render a <see cref="MudExFileMetaView"/> with the file metadata inline, at the
    /// position given by <see cref="FileMetaPlacement"/>. Fed from the same data the info dialog uses.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool ShowFileMeta { get; set; }

    /// <summary>
    /// Where the inline <see cref="MudExFileMetaView"/> is rendered relative to the viewer content when
    /// <see cref="ShowFileMeta"/> is true.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExFileMetaPlacement FileMetaPlacement { get; set; } = MudExFileMetaPlacement.Below;

    /// <summary>
    /// Raised whenever the metadata of the displayed file changes: the same set the info dialog shows, which
    /// is the file's own properties plus whatever the active <see cref="IMudExFileDisplay"/> contributes.
    /// </summary>
    /// <remarks>
    /// A viewer can only report what it has parsed, and parsing happens after the first render - a csv reports
    /// its column and row counts, a pdf its page count, an mp3 its bitrate. So this fires again when the
    /// viewer's contribution actually arrives, not just once. Consumers that render their own
    /// <see cref="MudExFileMetaView"/> somewhere else - a separate panel, a sidebar - bind to this instead of
    /// collecting anything themselves.
    /// </remarks>
    [Parameter, SafeCategory("Behavior")]
    public EventCallback<IDictionary<string, object>> FileMetaInformationChanged { get; set; }

    /// <summary>
    /// A function to handle content error.
    /// Return true if you have handled the error and false if you want to show the error message
    /// For example you can reset Url here to create a proxy fallback or display own not supported image or what ever.
    /// If you reset Url or Data here you need also to reset ContentType
    /// </summary>
    [Parameter]
    [SafeCategory("Validation")]
    public Func<IMudExFileDisplayInfos, Task<MudExFileDisplayContentErrorResult>> HandleContentErrorFunc { get; set; }

    /// <summary>
    /// Custom content error message to show if a native content from object or iframe raises an error
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")] public string CustomContentErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a general error message
    /// </summary>
    [Parameter]
    [SafeCategory("Behavior")] public string ErrorMessage { get; set; }


    /// <summary>
    /// Specify parameters for viewer controls. If a possible IMudExFileDisplay is found for current content type this parameters will be forwarded
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public IDictionary<string, object> ParametersForSubControls { get; set; }

    /// <summary>
    /// Media Type for current file
    /// </summary>
    public string MediaType
    {
        get
        {
            var ct = ContentType;
            if (ct == null) return null;
            var idx = ct.IndexOf('/');
            return idx >= 0 ? ct[..idx].ToLowerInvariant() : ct.ToLowerInvariant();
        }
    }

    /// <summary>
    /// Returns a plugin that is useful to show the content if the content cant displayed 
    /// </summary>
    public BrowserContentTypePlugin PossiblePlugin { get; private set; }

    /// <summary>
    /// Current browser informations
    /// </summary>
    public BrowserInfo Info
    {
        get => _info;
        set
        {
            _info = value;

            // Null when browser detection has not answered - no javascript at all during prerendering and
            // under test. Then there is no plugin to look up either.
            PossiblePlugin = value == null ? null : BrowserContentTypePlugin.Find(value.BrowserName, ContentType);
            StateHasChanged();
        }
    }

    #endregion

    /// <summary>
    /// Called if the js side has a content render error.
    /// This function returns true if it's handled and false if not.
    /// </summary>
    [JSInvokable]
    public async Task<bool> HandleContentError()
    {
        if (_internalCall)
            return !(_internalCall = false);
        var result = HandleContentErrorFunc != null ? await HandleContentErrorFunc(this) : MudExFileDisplayContentErrorResult.Unhandled;
        if (HandleContentErrorFunc != null && result != null)
        {
            _internalCall = true;
            _internalOverwrite = true;
            UpdateChangedFields(result);
            StateHasChanged();
        }

        return result?.IsHandled ?? false;
    }

    /// <inheritdoc/>
    public override object[] GetJsArguments() => base.GetJsArguments().Concat(new object[] { _id }).ToArray();

    /// <summary>
    /// Set the status text to display in center of file display
    /// </summary>
    public async Task<MudExFileDisplay> SetStatusTextAsync(string text)
    {
        StatusText = text;
        CallStateHasChanged();
        await Task.Delay(100);
        return this;
    }

    /// <summary>
    /// Opens the file in a new tab
    /// </summary>
    /// <returns></returns>
    public async Task OpenFileInNewTabAsync() => await JsRuntime.InvokeVoidAsync("window.open", await GetUrlAsync(), "_blank", "noreferrer");

    /// <summary>
    /// Remove the status text
    /// </summary>
    public Task<MudExFileDisplay> RemoveStatusTextAsync() => SetStatusTextAsync(null);

    private void UpdateRenderInfos()
    {
        if (_componentForFile?.ControlType != null) return;
        renderInfos = null;
        StateHasChanged();

        renderInfos = GetRenderInfos();
        StateHasChanged();
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<Stream>(nameof(ContentStream), out var stream) && ContentStream != stream)
                             || (parameters.TryGetValue<string>(nameof(Url), out var url) && Url != url);

        if(updateRequired)
            _componentForFile = null;

        await base.SetParametersAsync(parameters);

        if (!updateRequired)
            return;

        _errorClosed = false;
        ErrorMessage = null;
        // the banner is made visible by inline js on the native object tag - resetting the flags
        // is not enough, otherwise the next file keeps showing the error of the previous one
        CloseContentError();

        await EnsureUrlAsync();

        if (_internalOverwrite)
            return;

        UpdateRenderInfos();
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        var fs = Get<IMudExFileService>();
        var possibleRenderControls = GetServices<IMudExFileDisplay>().Where(c => c.GetType() != GetType() && !Ignored(c)).ToList();
        _possibleRenderControls = new List<IMudExFileDisplay>();
        foreach (var possibleRenderControl in possibleRenderControls)
        {
            if (await possibleRenderControl.CanHandleFileAsync(this, fs))
                _possibleRenderControls.Add(possibleRenderControl);
        }

        if (ViewDependsOnContentType && _componentForFile == null)
            _componentForFile = GetComponentForFile(_possibleRenderControls.FirstOrDefault(c => c.StartsActive && !ForceNativeRender));
        if (!_internalOverwrite)
            renderInfos = GetRenderInfos();
        await base.OnParametersSetAsync();
    }

    private bool Ignored(IMudExFileDisplay mudExFileDisplay)
    {
        if(IgnoredRenderControls == null || IgnoredRenderControls.Length == 0)
            return false;
        return IgnoredRenderControls.Any(t => t.IsInstanceOfType(mudExFileDisplay));
    }


    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "IMudExFileDisplay implementations are Blazor components resolved from DI; their public members are kept by the Razor compiler + DI roots.")]
    private ComponentForFileInfo GetComponentForFile(IMudExFileDisplay fileComponent)
    {
        var type = fileComponent?.GetType();
        if (type == null)
            return null;
        var parameters = ComponentRenderHelper.GetCompatibleParameters(this, type);

        parameters.Add(nameof(IMudExFileDisplay.FileDisplayInfos), this);

        if (ParametersForSubControls != null)
        {
            foreach (var param in ParametersForSubControls.Where(k => ComponentRenderHelper.IsValidParameter(type, k.Key, k.Value)))
            {
                parameters[param.Key] = param.Value;
            }
        }


        return new ComponentForFileInfo
        {
            ControlType = type,
            ShouldAddDiv = fileComponent.WrapInMudExFileDisplayDiv,
            Parameters = parameters,
        };
    }

    /// <summary>
    /// Returns the tag and attributes for the file
    /// </summary>
    public static (string tag, Dictionary<string, object> attributes) GetFileRenderInfos(
        string id,
        string url, 
        string fileName, string contentType, bool viewDependsOnContentType = true, 
        bool imageAsBackgroundImage = false, bool fallBackInIframe = false, bool sandBoxIframes = true, string onErrorMethod = "")
    {
        string mediaType = null;
        if (contentType != null)
        {
            var idx = contentType.IndexOf('/');
            mediaType = idx >= 0 ? contentType[..idx].ToLowerInvariant() : contentType.ToLowerInvariant();
        }
        if (viewDependsOnContentType && !string.IsNullOrEmpty(mediaType))
        {
            switch (mediaType)
            {
                case "image":
                    return !imageAsBackgroundImage
                        ? ("img", new()
                        {
                            {"data-id", id},
                            {"src", url},
                            {"loading", "lazy"},
                            {"alt", fileName},
                            {"data-mimetype", contentType}
                        })
                        : ("div", new()
                        {
                            {"data-id", id},
                            {"style", $"background-image:url('{url}')"},
                            {"class", "mud-ex-file-display-img-box"},
                            {"src", url},
                            {"loading", "lazy"},
                            {"alt", fileName},
                            {"data-mimetype", contentType}
                        });
                case "video":
                    return ("video", new()
                    {
                        {"data-id", id},
                        {"preload", "metadata"},
                        {"loading", "lazy"},
                        {"controls", true},
                        {"src", url},
                        {"type", contentType}
                    });
                case "audio":
                    return ("audio", new()
                    {
                        {"data-id", id},
                        {"preload", "metadata"},
                        {"loading", "lazy"},
                        {"controls", true},
                        {"src", url},
                        {"type", contentType}
                    });
            }
        }

        if (!fallBackInIframe) // if binary
        {
            return ("object", new()
            {
                {"data-id", id},
                {"data", url},
                {"onerror", $"{onErrorMethod}"},
                {"loading", "lazy"},
                {"type", contentType}
            });
        }

        return ("iframe", new()
        {
            {"data-id", id},
            {"src", url},
            {"onerror", $"{onErrorMethod}"},
            {"loading", "lazy"},
            {"sandbox", sandBoxIframes}
        });
    }

    private (string tag, Dictionary<string, object> attributes) GetRenderInfos()
    {
        return GetFileRenderInfos(_id, Url, FileName, ContentType, ViewDependsOnContentType, ImageAsBackgroundImage, FallBackInIframe, SandBoxIframes, GetJsOnError());
    }

    private string GetJsOnError() =>
        @$"
            var mudExErrorElement = function() {{ return document.getElementById('content-type-display-error'); }};
            var entry = window.__mudExFileDisplay && window.__mudExFileDisplay['{_id}'];
            if(!entry)
            {{
                var element = mudExErrorElement();
                if(element) {{ element.classList.add('visible'); }}
            }}
            else
            {{
                setTimeout(function(){{
                    entry.callBackReference.invokeMethodAsync('{nameof(HandleContentError)}').then(function(isHandled){{
                        var element = mudExErrorElement();
                        if(!isHandled && element)
                        {{
                            element.classList.add('visible');
                        }}
                    }});
                }}, 0)
            }}
        ";

    private void UpdateChangedFields(MudExFileDisplayContentErrorResult result)
    {
        bool urlChanged = false;
        if (result.ContentStream != null && result.ContentStream != ContentStream)
            ContentStream = result.ContentStream;
        if (result.FallBackInIframe != null && result.FallBackInIframe.Value != FallBackInIframe)
            FallBackInIframe = result.FallBackInIframe.Value;
        if (result.SandBoxIframes != null && result.SandBoxIframes.Value != SandBoxIframes)
            SandBoxIframes = result.SandBoxIframes.Value;
        if (!string.IsNullOrWhiteSpace(result.Message) && result.Message != CustomContentErrorMessage)
            CustomContentErrorMessage = result.Message;
        if (!string.IsNullOrWhiteSpace(result.ContentType) && result.ContentType != ContentType)
            ContentType = result.ContentType;
        if (!string.IsNullOrWhiteSpace(result.Url) && result.Url != Url)
        {
            urlChanged = true;
            Url = result.Url;
        }

        renderInfos = GetRenderInfos();
        if (renderInfos.Value.tag == "object" && JsRuntime != null && urlChanged)
            JsRuntime.InvokeVoidAsync("eval", $"document.querySelector('object[data-id=\"{_id}\"]').data += ' ';");
    }

    /// <summary>
    /// Downloads the file using the browser download functionality.
    /// </summary>
    public async Task DownloadFileAsync()
    {
        await EnsureUrlAsync(true);

        await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
        {
            Url,
            FileName = $"{FileName}",
            MimeType = ContentType
        });
    }

    /// <summary>
    /// Opens a save file dialog using the File System Access API (showSaveFilePicker) and saves the file to the selected location.
    /// Falls back to a regular download if the API is not supported by the browser.
    /// </summary>
    public async Task SaveFileAsAsync()
    {
        await EnsureUrlAsync(true);

        await JsRuntime.InvokeAsync<bool>("MudBlazorExtensions.saveFileAs", new
        {
            Url,
            FileName = $"{FileName}",
            MimeType = ContentType
        });
    }

    private async Task EnsureUrlAsync(bool force = false)
    {
        if ((_componentForFile?.ControlType == null || force) && string.IsNullOrWhiteSpace(Url) && ContentStream != null)
            Url = await FileService.ReadDataUrlForStreamAsync(ContentStream, ContentType, StreamUrlHandling == StreamUrlHandling.BlobUrl, _componentCts?.Token ?? CancellationToken.None);
    }

    private async Task<string> GetUrlAsync()
    {
        await EnsureUrlAsync(true);
        return Url;
    }

    private async Task CopyUrl(MouseEventArgs arg) => await JsApiService.CopyToClipboardAsync(await GetUrlAsync());
    //private async Task OpenInNewTab(MouseEventArgs arg) => await JsApiService.OpenInNewTabAsync(await GetUrlAsync());
    // noreferrer is important that's because otherwise the new window is opened in the same process with the opener window.
    private async Task OpenInNewTab(MouseEventArgs arg) => await OpenFileInNewTabAsync();

    private void CloseContentError()
    {
        JsRuntime.InvokeVoidAsync("eval",
            "document.getElementById('content-type-display-error').classList.remove('visible')");
    }

    private void RenderWith(IMudExFileDisplay fileComponent)
    {
        CloseContentError();
        _componentForFile = GetComponentForFile(fileComponent);
        if (fileComponent == null)
        {
            EnsureUrlAsync().ContinueWith(_ => UpdateRenderInfos());
        }
        else
        {
            UpdateRenderInfos();
        }
    }

    /// <summary>
    /// Returns the file infos as dictionary with keys like File, ContentType, Url, Size and more
    /// </summary>
    /// <param name="withEmptyValues"></param>
    /// <returns></returns>
    public async Task<IDictionary<string, object>> GetFileFinfosAsync(bool withEmptyValues)
    {
        Stream effectiveStream = ContentStream;
        string size = null;

        if (ContentStream != null)
        {
            try
            {
                if (ContentStream.Length > 0)
                {
                    size = Nextended.Blazor.Extensions.BrowserFileExtensions.GetReadableFileSize(ContentStream.Length);
                }
            }
            catch (NotSupportedException)
            {
                effectiveStream = await ContentStream.CopyStreamAsync();
                size = Nextended.Blazor.Extensions.BrowserFileExtensions.GetReadableFileSize(effectiveStream.Length);
            }
        }
        var dict = new Dictionary<string, object>()
        {
            { "File", FileName },
            { "ContentType", ContentType },
            { "Url", effectiveStream is { Length: > 0 } ? null : Url },
            { "Size", size }
        };

        var meta = await GetActiveDisplayMetaInformationAsync();
        if (meta != null)
            dict.MergeWith(meta);

        if (withEmptyValues)
        {
            foreach (var o in dict.Where(o => o.Value == null))
                dict[o.Key] = string.Empty;
        }
        else
        {
            dict = dict.Where(o => o.Value != null).ToDictionary(o => o.Key, o => o.Value);
        }

        return dict;
    }

    /// <summary>
    /// Gathers the extra meta information the currently active <see cref="IMudExFileDisplay"/> contributes,
    /// via <see cref="IMudExFileDisplay.FileMetaInformationAsync"/>. Shared by <see cref="GetFileFinfosAsync"/>
    /// (used by the info dialog) and by the inline <see cref="MudExFileMetaView"/> rendering.
    /// </summary>
    /// <remarks>
    /// Public because a consumer that renders its own <see cref="MudExFileMetaView"/> beside this component -
    /// <see cref="MudExFileManager"/> does - needs the viewer's contribution without the intrinsic file
    /// properties <see cref="GetFileFinfosAsync"/> adds, which it already has.
    /// </remarks>
    public async Task<IDictionary<string, object>> GetActiveDisplayMetaInformationAsync()
    {
        if (_currentFileDisplay is not { Instance: IMudExFileDisplay fileDisplay })
            return null;
        try
        {
            return await fileDisplay.FileMetaInformationAsync(this);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    /// <summary>
    /// Gathers the data for the inline <see cref="MudExFileMetaView"/> shown when <see cref="ShowFileMeta"/>
    /// is true: the raw file size plus the same extra meta information the info dialog shows.
    /// </summary>
    private async Task RefreshFileMetaViewDataAsync()
    {
        long? size = null;
        if (ContentStream != null)
        {
            try
            {
                if (ContentStream.Length > 0)
                    size = ContentStream.Length;
            }
            catch (NotSupportedException)
            {
                size = (await ContentStream.CopyStreamAsync()).Length;
            }
        }

        _fileMetaSize = size;
        _fileMetaInformation = await GetActiveDisplayMetaInformationAsync();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!ShowFileMeta && !FileMetaInformationChanged.HasDelegate)
            return;

        // The size is read from the source and can cost a stream copy, so it is only re-read when the source
        // itself changed.
        var key = (Url, ContentStream, _currentFileDisplay?.Instance);
        if (!Equals(_fileMetaSourceKey, key))
        {
            _fileMetaSourceKey = key;
            _metaProbes = 0;
            await RefreshFileMetaViewDataAsync();
            await ReportFileMetaInformationAsync();
            StateHasChanged();
            return;
        }

        // The viewer's own contribution only exists once it has parsed the file - a csv counts its columns, an
        // mp3 reads its tags. Asking again and comparing is what makes those values appear at all.
        var meta = await GetActiveDisplayMetaInformationAsync();
        if (!SameMetaInformation(_fileMetaInformation, meta))
        {
            _fileMetaInformation = meta;
            await ReportFileMetaInformationAsync();
            StateHasChanged();
            return;
        }

        // Still nothing, and a viewer is supposed to be there. Its reference is only assigned after it has
        // rendered, and its own renders do not bring us back here - so without asking again the first, empty
        // answer would stay the last one, and the metadata would only appear once something else triggers a
        // render (pressing info, for instance). Bounded, so a viewer that never reports anything stops it.
        if (meta is not { Count: > 0 } && _componentForFile?.ControlType != null && _metaProbes++ < MetaProbeLimit)
            StateHasChanged();
    }

    // How often to come back for a viewer's metadata before accepting that it has none.
    private const int MetaProbeLimit = 5;

    /// <summary>
    /// Re-reads the active viewer's metadata and publishes it when it changed.
    /// </summary>
    /// <remarks>
    /// A viewer calls this - through <c>FileDisplayInfos.NotifyMetaChangedAsync()</c> - once it has parsed the
    /// file, because that is the only moment it knows its row count, bitrate or page count. Without it the
    /// value would only surface when something else happens to trigger a render here.
    /// </remarks>
    public async Task RefreshMetaInformationAsync()
    {
        if (!ShowFileMeta && !FileMetaInformationChanged.HasDelegate)
            return;

        var meta = await GetActiveDisplayMetaInformationAsync();
        if (SameMetaInformation(_fileMetaInformation, meta))
            return;

        _fileMetaInformation = meta;
        await ReportFileMetaInformationAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ReportFileMetaInformationAsync()
    {
        if (FileMetaInformationChanged.HasDelegate)
            await FileMetaInformationChanged.InvokeAsync(await GetFileFinfosAsync(false));
    }

    private static bool SameMetaInformation(IDictionary<string, object> left, IDictionary<string, object> right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left == null || right == null || left.Count != right.Count)
            return false;

        return left.All(entry => right.TryGetValue(entry.Key, out var value) && Equals(entry.Value, value));
    }

    /// <summary>
    /// Shows the info dialog with file information
    /// </summary>
    /// <returns></returns>
    public Task ShowInfoFileInfoAsync() => ShowInfoFileInfoAsync(false);

    /// <summary>
    /// Shows the info dialog with file information
    /// </summary>
    /// <param name="showEmptyValues"></param>
    /// <returns></returns>
    public async Task ShowInfoFileInfoAsync(bool showEmptyValues)
    {
        var dict = await GetFileFinfosAsync(showEmptyValues);

        var infoObject = FileInfo ?? ReflectionHelper.CreateTypeAndDeserialize(dict);
        
        var options = DialogServiceExt.DefaultOptions();
        options.CloseButton = true;
        options.Resizeable = true;
        options.DragMode = MudDialogDragMode.Simple;
        if (_lastInfoDialogLocation.HasValue)
        {
            options.CustomPosition = _lastInfoDialogLocation.Value.Position;
            options.CustomSize = _lastInfoDialogLocation.Value.Dimension;
            options.MaxWidth = MaxWidth.ExtraExtraLarge;
            options.MaxHeight = MaxHeight.ExtraExtraLarge;
        }

        Get<IDialogEventService>()
            .Subscribe<DialogDragEndEvent>(HandleDragged)
            .Subscribe<DialogResizedEvent>(HandleResized);

        await Get<IDialogService>().ShowObjectAsync(infoObject, TryLocalize("Info"), Icons.Material.Filled.Info, options, meta =>
        {
            meta.AllProperties.WrapInMudItem(i => i.xs = 6);
            meta.Property("Url").WrapInMudItem(i => i.xs = 12);
        });

        Get<IDialogEventService>()
            .Unsubscribe<DialogDragEndEvent>(HandleDragged)
            .Unsubscribe<DialogResizedEvent>(HandleResized);
    }


    private Task HandleDragged(DialogDragEndEvent arg)
    {
        return Store(arg.Rect);
    }

    private Task HandleResized(DialogResizedEvent arg)
    {
        return Store(arg.Rect);
    }

    private Task Store(BoundingClientRect argRect)
    {
        if (argRect is { Left: > 0, Top: > 0, Width: > 0, Height: > 0 })
        {
            _lastInfoDialogLocation = (argRect.ToDimension(CssUnit.Percentage), argRect.ToPosition(CssUnit.Percentage));
        }
        return Task.CompletedTask;
    }


    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        try { _componentCts?.Cancel(); }
        catch { /* token may already be disposed */ }
        _componentCts?.Dispose();
        _componentCts = null;

        await base.DisposeAsync();
        Url = null;
        if (_componentForFile != null)
            _componentForFile.ControlType = null;
        await FileService.DisposeAsync();
    }

    /// <summary>
    /// Shows an error message
    /// </summary>
    public void ShowError(string message, [CallerMemberName] string callerName = "")
    {
        if (ErrorMessage != message)
        {
            _errorClosed = false;
            ErrorMessage = $"{message}";
            StateHasChanged();
        }
    }
}