using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Helper;

public static class MudDialogInstanceExtensions
{
    public static void CloseAnimated(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(); return Task.CompletedTask; }, jsRuntime);

    public static void CancelAnimated(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Cancel(); return Task.CompletedTask; }, jsRuntime);

    public static void CloseAnimated(this IMudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(result); return Task.CompletedTask; }, jsRuntime);
    public static void CloseAnimated<T>(this IMudDialogInstance instance, T result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close<T>(result); return Task.CompletedTask; }, jsRuntime);

    public static void CloseAnimatedIf(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(); return Task.CompletedTask; }, jsRuntime, true);

    public static void CancelAnimatedIf(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Cancel(); return Task.CompletedTask; }, jsRuntime, true);

    public static void CloseAnimatedIf(this IMudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(result); return Task.CompletedTask; }, jsRuntime, true);

    public static void CloseAnimatedIf<T>(this IMudDialogInstance instance, T result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close<T>(result); return Task.CompletedTask; }, jsRuntime, true);


    public static void CloseAnimated(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(); return Task.CompletedTask; }, jsRuntime);

    public static void CancelAnimated(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(DialogResult.Cancel()); return Task.CompletedTask; }, jsRuntime);

    public static void CloseAnimated(this IDialogReference instance, DialogResult result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(result); return Task.CompletedTask; }, jsRuntime);
    public static void CloseAnimated<T>(this IDialogReference instance, T result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(DialogResult.Ok(result)); return Task.CompletedTask; }, jsRuntime);

    public static void CloseAnimatedIf(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(); return Task.CompletedTask; }, jsRuntime, true);

    public static void CancelAnimatedIf(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(DialogResult.Cancel()); return Task.CompletedTask; }, jsRuntime, true);

    public static void CloseAnimatedIf(this IDialogReference instance, DialogResult result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(result); return Task.CompletedTask; }, jsRuntime, true);

    public static void CloseAnimatedIf<T>(this IDialogReference instance, T result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(DialogResult.Ok(result)); return Task.CompletedTask; }, jsRuntime, true);

    private static async Task AnimateClose(this IMudDialogInstance instance, Func<IMudDialogInstance, Task> callback, IJSRuntime jsRuntime = null, bool checkOptions = false)
    {        
        try
        {
            var dialogId = DialogReferenceExtensions.PrepareDialogId(instance.Id);
            await (jsRuntime ?? JsImportHelper.GetInitializedJsRuntime()).InvokeVoidAsync("MudBlazorExtensions.closeDialogAnimated", dialogId, checkOptions);
        }
        finally
        {
            await callback(instance);
        }
        
    }

    private static async Task AnimateClose(this IDialogReference instance, Func<IDialogReference, Task> callback, IJSRuntime jsRuntime = null, bool checkOptions = false)
    {        
        try
        {            
            var dialogId = DialogReferenceExtensions.PrepareDialogId(instance.Id);
            await (jsRuntime ?? JsImportHelper.GetInitializedJsRuntime()).InvokeVoidAsync("MudBlazorExtensions.closeDialogAnimated", dialogId, checkOptions);
        }
        finally
        {
            await callback(instance);
        }

    }

}
