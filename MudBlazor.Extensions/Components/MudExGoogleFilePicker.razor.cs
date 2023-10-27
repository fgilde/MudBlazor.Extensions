using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

public partial class MudExGoogleFilePicker : IMudExExternalFilePicker
{
    private bool _ready;
    [Parameter] 
    public string ClientId { get; set; } = "787005879852-vkv0cduhl70u087pq4a8s2jtkdgv1n6s.apps.googleusercontent.com";
    [Parameter] 
    public string ApiKey { get; set; } = "AIzaSyAmIdisQ2aGSXdDjYR0fuQ2M0m0BEJr_nI";
    [Parameter] 
    public string AppId { get; set; } = "search-271314"; // 787005879852

    public string Image { get; }

    public Task<string> PickAsync(CancellationToken cancellation = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the JavaScript arguments to pass to the component.
    /// </summary>
    public override object[] GetJsArguments()
    {
        return new[] { ElementReference, CreateDotNetObjectReference(), Options() };
    }

    private object Options()
    {
        return new
        {
            ClientId,
            ApiKey,
            AppId,
        };
    }

    [JSInvokable]
    public void OnReady()
    {
        _ready = true;
        CallStateHasChanged();
    }

    private async Task ShowPicker()
    {
        try
        {
            await JsReference.InvokeVoidAsync("handleAuthClick");
        }
        catch (Exception ex)
        {
            // Handle the exception or log it
            Console.WriteLine("Error showing picker: " + ex.Message);
        }
    }
}