using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Nextended.Blazor.Models;
using Nextended.Core;
using Nextended.Core.Extensions;
using PSC.Blazor.Components.BrowserDetect;

namespace MudBlazor.Extensions.Components;


public partial class MudExFileDisplay : IAsyncDisposable, IFileDisplayInfos
{
    
    [Inject] private IServiceProvider _serviceProvider { get; set; }
    private IJSRuntime JsRuntime => _serviceProvider.GetService<IJSRuntime>();
    private IStringLocalizer<MudExFileDisplay> _localizer => _serviceProvider.GetService<IStringLocalizer<MudExFileDisplay>>();

    [Parameter] public string Url { get; set; }
    [Parameter] public string ContentType { get; set; }
    
    /**
     * Should be true if file is not a binary one
     */
    [Parameter]
    public bool FallBackInIframe { get; set; }

    /// <summary>
    /// Set this to false to show everything in iframe/object tag otherwise zip, images audio and video will displayed in correct tags
    /// </summary>
    [Parameter]
    public bool ViewDependsOnContentType { get; set; } = true;

    [Parameter] public bool ImageAsBackgroundImage { get; set; } = false;
    [Parameter] public bool ShowContentError { get; set; } = true;
    [Parameter] public bool SandBoxIframes { get; set; } = true;
    [Parameter] public bool AllowDownload { get; set; } = true;
    [Parameter] public string FileName { get; set; }
    [Parameter] public Stream ContentStream { get; set; }
    
    /**
     * A function to handle content error. Return true if you have handled the error and false if you want to show the error message
     * For example you can reset Url here to create a proxy fallback or display own not supported image or what ever.
     * If you reset Url or Data here you need also to reset ContentType
     */
    [Parameter] public Func<IFileDisplayInfos, Task<ContentErrorResult>> HandleContentErrorFunc { get; set; }
    [Parameter] public string CustomContentErrorMessage { get; set; }
    public string MediaType => ContentType?.Split("/")?.FirstOrDefault()?.ToLower();
    public BrowserContentTypePlugin PossiblePlugin { get; private set; }
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
    
    protected DotNetObjectReference<MudExFileDisplay> callbackReference;
    private string _id = Guid.NewGuid().ToString();
    private bool _isZip;
    private (string tag, Dictionary<string, object> attributes) renderInfos;
    private BrowserInfo _info;
    private bool internalOverwrite;

    protected override Task OnInitializedAsync()
    {
        callbackReference = DotNetObjectReference.Create(this);
        JsRuntime?.InvokeVoidAsync("MudBlazorExtensions.initMudExFileDisplay", callbackReference, _id);
        return base.OnInitializedAsync();
    }
    

    protected override Task OnParametersSetAsync()
    {
        _isZip = ViewDependsOnContentType && !string.IsNullOrEmpty(ContentType) && MimeType.IsZip(ContentType);
        if (!internalOverwrite)
            renderInfos = GetRenderInfos();
        return base.OnParametersSetAsync();
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

    private bool internalCall;
    [JSInvokable]
    public async Task<bool> HandleContentError()
    {
        if (internalCall)
            return !(internalCall = false);
        var result = HandleContentErrorFunc != null ? await HandleContentErrorFunc(this) : ContentErrorResult.Unhandled;
        if (HandleContentErrorFunc != null && result != null)
        {
            internalCall = true;
            internalOverwrite = true;
            UpdateChangedFields(result);
            StateHasChanged();
        }

        return result?.IsHandled ?? false;
    }

    private void UpdateChangedFields(ContentErrorResult result)
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
        if (renderInfos.tag == "object" && JsRuntime != null && urlChanged)
            JsRuntime.InvokeVoidAsync("eval", $"document.querySelector('object[data-id=\"{_id}\"]').data += ' ';");
    }

    private async Task Download(MouseEventArgs arg)
    {
        if (string.IsNullOrWhiteSpace(Url) && ContentStream != null)
        {
            ContentStream.Position = 0;
            Url = await DataUrl.GetDataUrlAsync(ContentStream.ToByteArray(), ContentType);
        }

        await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
        {
            Url,
            FileName = $"{FileName}",
            MimeType = ContentType
        });
    }

    private void CloseContentError()
    {
        JsRuntime.InvokeVoidAsync("eval",
            "document.getElementById('content-type-display-error').classList.remove('visible')");
    }

    public ValueTask DisposeAsync()
    {
        return JsRuntime?.InvokeVoidAsync("MudBlazorExtensions.disposeMudExFileDisplay", _id) ?? ValueTask.CompletedTask;
    }
}

public sealed class ContentErrorResult: IFileDisplayInfos
{
    public static ContentErrorResult Unhandled => new() { IsHandled = false };
    public static ContentErrorResult Handled => new() { IsHandled = true };
    public static ContentErrorResult DisplayMessage(string message) => new ContentErrorResult().WithMessage(message);
    public static ContentErrorResult RedirectTo(string url, string contentType = "") => new() { IsHandled = true, ContentType = contentType, Url = url };
    public static ContentErrorResult RedirectTo(Stream stream, string contentType = "") => new() { IsHandled = true, ContentType = contentType, ContentStream = stream };
    
    public ContentErrorResult WithMessage(string message)
    {
        IsHandled = false;
        Message = message;
        return this;
    }
    public bool IsHandled { get; set; }
    public string Url { get; set; }
    public string ContentType { get; set; }
    public string Message { get; set; }
    public Stream ContentStream { get; set; }
    public bool? FallBackInIframe { get; set; }
    public bool? SandBoxIframes { get; set; }
}

public interface IFileDisplayInfos
{
    public string Url { get; }
    public string ContentType { get; }
    public Stream ContentStream { get; }
}