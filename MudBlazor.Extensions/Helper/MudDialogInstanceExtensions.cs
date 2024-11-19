using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Helper;

public static class MudDialogInstanceExtensions
{
    public static void CloseAnimated(this IMudDialogInstance instance, IJSRuntime jsRuntime = null) 
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Close());

    public static void CancelAnimated(this IMudDialogInstance instance, IJSRuntime jsRuntime = null) 
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Cancel());

    public static void CloseAnimated(this IMudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null) 
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Close(result));

    public static void CloseAnimated<T>(this IMudDialogInstance instance, T result, IJSRuntime jsRuntime = null) 
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Close<T>(result));

    public static void CloseAnimatedIf(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime, true).ContinueWith(_ => instance.Close());

    public static void CancelAnimatedIf(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime, true).ContinueWith(_ => instance.Cancel());

    public static void CloseAnimatedIf(this IMudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime, true).ContinueWith(_ => instance.Close(result));

    public static void CloseAnimatedIf<T>(this IMudDialogInstance instance, T result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime, true).ContinueWith(_ => instance.Close<T>(result));

    private static Task AnimateClose(this IMudDialogInstance instance, IJSRuntime jsRuntime = null, bool checkOptions = false)
    {        
        var dialogId = DialogReferenceExtensions.PrepareDialogId(instance.Id);
        return (jsRuntime ?? JsImportHelper.GetInitializedJsRuntime()).InvokeVoidAsync("MudBlazorExtensions.closeDialogAnimated", dialogId, checkOptions).AsTask();        
    }
}