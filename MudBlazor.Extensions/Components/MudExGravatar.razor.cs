using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

public partial class MudExGravatar : IMudExComponent
{
    [Inject] private IJSRuntime _jsRuntime { get; set; }
    private string _email = "fgilde@gmail.com";
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);
    private TaskCompletionSource _jsLoadSource = new();
    private string url;
    
    [Parameter]
    public string Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                _ = LoadGravatarAsync();
            }
        }
    }
    
    private void JsLoaded(string obj)
    {
        _jsLoadSource.SetResult();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            _ = LoadGravatarAsync();
    }

    private async Task<string> HashAsync(string email)
    {
        await _jsLoadSource.Task;
        await _jsRuntime.WaitForNamespaceAsync("CryptoJS");
        return await _jsRuntime.DInvokeAsync<string>((w,e) => w.CryptoJS.MD5(e.trim().toLowerCase()).toString(), email);
    }
    
    private async Task LoadGravatarAsync()
    {
        url = string.Empty;
        if (!string.IsNullOrEmpty(Email))
        {
            var hash = await HashAsync(Email);
            url = $"https://www.gravatar.com/avatar/{hash}?s=80&d=identicon";
            await InvokeAsync(StateHasChanged);
        }
    }
}