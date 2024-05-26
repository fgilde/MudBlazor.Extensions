using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Helper;

public static class MudDialogInstanceExtensions
{
    public static void CloseAnimated(this MudDialogInstance instance, IJSRuntime jsRuntime = null) 
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Close());

    public static void CancelAnimated(this MudDialogInstance instance, IJSRuntime jsRuntime = null) 
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Cancel());

    public static void CloseAnimated(this MudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null) 
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Close(result));

    public static void CloseAnimated<T>(this MudDialogInstance instance, T result, IJSRuntime jsRuntime = null) 
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Close<T>(result));

    private static Task AnimateClose(this MudDialogInstance instance, IJSRuntime jsRuntime = null)
    {        
        var dialogId = DialogReferenceExtensions.PrepareDialogId(instance.Id);
        return (jsRuntime ?? JsImportHelper.GetInitializedJsRuntime()).InvokeVoidAsync("MudBlazorExtensions.closeDialogAnimated", dialogId).AsTask();        
    }
}