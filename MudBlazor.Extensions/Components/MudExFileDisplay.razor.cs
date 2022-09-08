using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Nextended.Blazor.Helper;
using Nextended.Blazor.Models;
using Nextended.Core.Extensions;
using PSC.Blazor.Components.BrowserDetect;

namespace MudBlazor.Extensions.Components;


public partial class MudExFileDisplay
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
    [Parameter] public bool AllowDownload { get; set; } = true;
    [Parameter] public string FileName { get; set; }
    [Parameter] public Stream ContentStream { get; set; }

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

    public string MediaType => ContentType?.Split("/")?.FirstOrDefault()?.ToLower();

    private bool _isZip;
    private (string tag, Dictionary<string, object> attributes) renderInfos;
    private BrowserInfo _info;


    protected override Task OnParametersSetAsync()
    {
        _isZip = ViewDependsOnContentType && !string.IsNullOrEmpty(ContentType) && MimeTypeHelper.IsZip(ContentType);
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
                            {"src", Url},
                            {"loading", "lazy"},
                            {"alt", FileName},
                            {"data-mimetype", ContentType}
                        })
                        : ("div", new()
                        {
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
                        {"preload", "metadata"},
                        {"loading", "lazy"},
                        {"controls", true},
                        {"src", Url},
                        {"type", ContentType}
                    });
                case "audio":
                    return ("audio", new()
                    {
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
                {"data", Url},
                {"onerror", "document.getElementById('content-type-display-error').classList.add('visible')"},
                {"loading", "lazy"},
                {"type", ContentType}
            });
        }

        return ("iframe", new()
        {
            {"src", Url},
            {"loading", "lazy"},
            {"sandbox", true}
        });
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
}