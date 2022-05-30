using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Nextended.Blazor.Helper;
using Nextended.Blazor.Models;

namespace MudBlazor.Extensions.Components;


public partial class MudExFileDisplay
{
    [Inject] private IJSRuntime _jsRuntime { get; set; }

    [Parameter] public string Url { get; set; }
    [Parameter] public string ContentType { get; set; }

    /**
     * Should be true if file is not a binary one
     */
    [Parameter] public bool FallBackInIframe { get; set; }
    [Parameter] public bool AllowDownload { get; set; } = true; 
    [Parameter] public string FileName { get; set; }
    [Parameter] public Stream ContentStream { get; set; }
    
    public string MediaType => ContentType?.Split("/").FirstOrDefault()?.ToLower();

    private bool _isZip;
    private (string tag, Dictionary<string, object> attributes) renderInfos;


    protected override async Task OnInitializedAsync()
    {
        _isZip = MimeTypeHelper.IsZip(ContentType);
        renderInfos = GetRenderInfos();
        await base.OnInitializedAsync();
    }

    private (string tag, Dictionary<string, object> attributes) GetRenderInfos()
    {
        if (MediaType == "image")
        {
            return ("img", new ()
            {
                {"src", Url},
                {"alt", FileName },
                {"data-mimetype", ContentType }
            });
        }

        if (MediaType == "video")
        {
            return ("video", new ()
            {
                {"preload", "metadata"},
                {"controls", null},
                {"src", Url},
                {"type", ContentType}
            });
        }

        if (MediaType == "audio")
        {
            return ("audio", new ()
            {
                {"preload", "metadata"},
                {"controls", null},
                {"src", Url},
                {"type", ContentType}
            });
        }

        if (!FallBackInIframe) // wenn binary
        {
            return ("object", new()
            {
                { "data", Url },
                { "type", ContentType }
            });
        }
        return ("iframe", new()
        {
            { "src", Url },
            { "sandbox", "sandbox" }
        });
    }

    private async Task Download(MouseEventArgs arg)
    {
        if (string.IsNullOrWhiteSpace(Url) && ContentStream != null)
        {
            ContentStream.Position = 0;
            Url = DataUrl.GetDataUrl(ZipBrowserFile.GetBytes(ContentStream), ContentType);
        }
        //await _jsRuntime.InvokeVoidAsync(JsNamespace.Get("BrowserHelper", "download"), new
        //{
        //    Url,
        //    FileName = $"{FileName}",
        //    MimeType = ContentType
        //});
    }
}
