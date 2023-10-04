using System.Text;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Services;
using Nextended.Core.Encode;

namespace TryMudEx.Client.Pages.Index;

public partial class Index
{
    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private MudExFileService FileService { get; set; }
    [Inject] private IJSRuntime JsRuntime { get; set; }
    [Inject] public ILocalStorageService Storage { get; set; }
    private MudExCodeView _codeView;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(firstRender)
            await Storage.RemoveItemAsync("__temp_code");
    }

    private async Task UseCodeClick()
    {
        //Link="/snippet/samples/CardList"
        var code = _codeView.Code;
        var blobUrl = await FileService.CreateDataUrlAsync(Encoding.UTF8.GetBytes(code), "text/plain", true);
        
        NavigationManager.NavigateTo($"/snippet/from/{blobUrl.EncodeDecode().Base64.Encode()}");
        _= JsRuntime.InvokeVoidAsync(Models.Try.ChangeDisplayUrl, "/snippet");
    }
}