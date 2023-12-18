using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using Nextended.Blazor.Helper;
using MudBlazor.Extensions.Services;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using YamlDotNet.Core.Tokens;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to display a file in a iframe or object tag or in a registered viewer that implements IMudExFileDisplay
/// </summary>
public partial class MudExFileDisplay : IMudExFileDisplayInfos
{

    #region private fields

    private bool internalCall;
    private bool _isNativeRendered;
    private string _id = Guid.NewGuid().ToString();
    private (string tag, Dictionary<string, object> attributes)? renderInfos;
    private BrowserInfo _info;
    private bool internalOverwrite;
    private List<IMudExFileDisplay> _possibleRenderControls;
    private (Type ControlType, bool ShouldAddDiv, IDictionary<string, object> Parameters) _componentForFile;
    private Stream _contentStream;
    private bool _errorClosed;

    [Inject] private IJsApiService JsApiService { get; set; }
    [Inject] private MudExFileService FileService { get; set; }

    #endregion

    #region Parameters and Properties

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
    public string MediaType => ContentType?.Split("/")?.FirstOrDefault()?.ToLower();

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
            PossiblePlugin = BrowserContentTypePlugin.Find(Info.BrowserName, ContentType);
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
        if (internalCall)
            return !(internalCall = false);
        var result = HandleContentErrorFunc != null ? await HandleContentErrorFunc(this) : MudExFileDisplayContentErrorResult.Unhandled;
        if (HandleContentErrorFunc != null && result != null)
        {
            internalCall = true;
            internalOverwrite = true;
            UpdateChangedFields(result);
            StateHasChanged();
        }

        return result?.IsHandled ?? false;
    }

    /// <inheritdoc/>
    public override object[] GetJsArguments() => base.GetJsArguments().Concat(new object[] { _id }).ToArray();

    private void UpdateRenderInfos()
    {
        if (_componentForFile.ControlType != null) return;
        renderInfos = null;
        StateHasChanged();

        renderInfos = GetRenderInfos();
        StateHasChanged();
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<Stream>(nameof(ContentStream), out var stream) && ContentStream != stream)
                             || (parameters.TryGetValue<string>(nameof(Url), out var url) && Url != url);

        await base.SetParametersAsync(parameters);

        if (!updateRequired)
            return;

        _errorClosed = false;
        ErrorMessage = null;

        await EnsureUrlAsync();

        if (internalOverwrite)
            return;

        UpdateRenderInfos();
    }

    /// <inheritdoc/>
    protected override Task OnParametersSetAsync()
    {
        _possibleRenderControls = GetServices<IMudExFileDisplay>().Where(c => c.GetType() != GetType() && c.CanHandleFile(this)).ToList();
        if (ViewDependsOnContentType)
            _componentForFile = GetComponentForFile(_possibleRenderControls.FirstOrDefault(c => c.StartsActive && !ForceNativeRender));
        if (!internalOverwrite)
            renderInfos = GetRenderInfos();
        return base.OnParametersSetAsync();
    }


    private (Type ControlType, bool ShouldAddDiv, IDictionary<string, object> Parameters) GetComponentForFile(IMudExFileDisplay fileComponent)
    {
        var type = fileComponent?.GetType();
        if (type == null)
            return default;
        var parameters = ComponentRenderHelper.GetCompatibleParameters(this, type);
        parameters.Add(nameof(IMudExFileDisplay.FileDisplayInfos), this);

        if (ParametersForSubControls != null)
        {
            foreach (var param in ParametersForSubControls.Where(k => ComponentRenderHelper.IsValidParameter(type, k.Key, k.Value)))
            {
                parameters[param.Key] = param.Value;
            }
        }


        return (type, fileComponent.WrapInMudExFileDisplayDiv, parameters);
    }

    private (string tag, Dictionary<string, object> attributes) GetRenderInfos()
    {
        if (ViewDependsOnContentType && !string.IsNullOrEmpty(MediaType))
        {
            switch (MediaType)
            {
                case "image":
                    return !ImageAsBackgroundImage
                        ? ("img", new()
                        {
                            {"data-id", _id},
                            {"src", Url},
                            {"loading", "lazy"},
                            {"alt", FileName},
                            {"data-mimetype", ContentType}
                        })
                        : ("div", new()
                        {
                            {"data-id", _id},
                            {"style", $"background-image:url('{Url}')"},
                            {"class", "mud-ex-file-display-img-box"},
                            {"src", Url},
                            {"loading", "lazy"},
                            {"alt", FileName},
                            {"data-mimetype", ContentType}
                        });
                case "video":
                    return ("video", new()
                    {
                        {"data-id", _id},
                        {"preload", "metadata"},
                        {"loading", "lazy"},
                        {"controls", true},
                        {"src", Url},
                        {"type", ContentType}
                    });
                case "audio":
                    return ("audio", new()
                    {
                        {"data-id", _id},
                        {"preload", "metadata"},
                        {"loading", "lazy"},
                        {"controls", true},
                        {"src", Url},
                        {"type", ContentType}
                    });
            }
        }

        if (!FallBackInIframe) // wenn binary
        {
            return ("object", new()
            {
                {"data-id", _id},
                {"data", Url},
                {"onerror", $"{GetJsOnError()}"},
                {"loading", "lazy"},
                {"type", ContentType}
            });
        }

        return ("iframe", new()
        {
            {"data-id", _id},
            {"src", Url},
            {"onerror", $"{GetJsOnError()}"},
            {"loading", "lazy"},
            {"sandbox", SandBoxIframes}
        });
    }

    private string GetJsOnError() =>
        @$"
            var displayMessage = !window.__mudExFileDisplay || !window.__mudExFileDisplay['{_id}'];
            var elementToShow = document.getElementById('content-type-display-error');
            if(displayMessage && elementToShow)
            {{
                elementToShow.classList.add('visible');
            }}
            else {{
                setTimeout(function(){{
                    window.__mudExFileDisplay['{_id}'].callBackReference.invokeMethodAsync('{nameof(HandleContentError)}').then(function(isHandled){{
                        if(!isHandled && elementToShow)
                        {{
                            document.getElementById('content-type-display-error').classList.add('visible');
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

    private async Task Download(MouseEventArgs arg)
    {
        await EnsureUrlAsync(true);

        await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
        {
            Url,
            FileName = $"{FileName}",
            MimeType = ContentType
        });
    }

    private async Task EnsureUrlAsync(bool force = false)
    {
        if ((_componentForFile.ControlType == null || force) && string.IsNullOrWhiteSpace(Url) && ContentStream != null)
            Url = await FileService.ReadDataUrlForStreamAsync(ContentStream, ContentType, StreamUrlHandling == StreamUrlHandling.BlobUrl);
    }

    private async Task<string> GetUrlAsync()
    {
        await EnsureUrlAsync(true);
        return Url;
    }

    private async Task CopyUrl(MouseEventArgs arg) => await JsApiService.CopyToClipboardAsync(await GetUrlAsync());
    //private async Task OpenInNewTab(MouseEventArgs arg) => await JsApiService.OpenInNewTabAsync(await GetUrlAsync());
    // noreferrer is important that's because otherwise the new window is opened in the same process with the opener window.
    private async Task OpenInNewTab(MouseEventArgs arg) => await JsRuntime.InvokeVoidAsync("window.open", await GetUrlAsync(), "_blank", "noreferrer");

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

    private async Task ShowInfo()
    {
        var selfGenerated = FileInfo == null;
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

        var infoObject = FileInfo ?? new
        {
            File = FileName,
            ContentType = ContentType,
            Url = effectiveStream is { Length: > 0 } ? null : Url,
            Size = size
        };

        var options = DialogServiceExt.DefaultOptions();
        options.CloseButton = true;
        await Get<IDialogService>().ShowObject(infoObject, TryLocalize("Info"), Icons.Material.Filled.Info, options, meta =>
        {
            if (selfGenerated)
                meta.Properties().Where(p => p.Value == null).Ignore();
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        Url = null;
        _componentForFile.ControlType = null;
        await FileService.DisposeAsync();
    }

    public void ShowError(string message)
    {
        if (ErrorMessage != message)
        {
            _errorClosed = false;
            ErrorMessage = message;
            StateHasChanged();
        }
    }
}