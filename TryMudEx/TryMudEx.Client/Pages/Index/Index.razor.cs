using System.Text;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Services;
using Nextended.Core.Encode;
using TryMudEx.Client.Models;
using TryMudEx.Client.Services;

namespace TryMudEx.Client.Pages.Index;

public partial class Index
{
    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private MudExFileService FileService { get; set; }
    [Inject] private IJSRuntime JsRuntime { get; set; }
    [Inject] private LayoutService LayoutService { get; set; }
    [Inject] public ILocalStorageService Storage { get; set; }
    private MudExCodeView _codeView;

    protected override Task OnInitializedAsync()
    {
        LayoutService.DarkChanged += (sender, b) => StateHasChanged();
        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // dropping the code must drop the editor session with it, otherwise the
            // restored dock layout points at panels for files that no longer exist
            await Storage.RemoveItemAsync("__temp_code");
            await Storage.RemoveItemAsync(ReplStorageKeys.OpenFiles);
            await Storage.RemoveItemAsync(ReplStorageKeys.Layout);
        }
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