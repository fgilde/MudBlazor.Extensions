using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Helper;

public static class MudDialogInstanceExtensions
{
    // --- IMudDialogInstance async methods ---

    public static Task CloseAnimatedAsync(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(); return Task.CompletedTask; }, jsRuntime);

    public static Task CancelAnimatedAsync(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Cancel(); return Task.CompletedTask; }, jsRuntime);

    public static Task CloseAnimatedAsync(this IMudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(result); return Task.CompletedTask; }, jsRuntime);

    public static Task CloseAnimatedAsync<T>(this IMudDialogInstance instance, T result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close<T>(result); return Task.CompletedTask; }, jsRuntime);

    public static Task CloseAnimatedIfAsync(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(); return Task.CompletedTask; }, jsRuntime, true);

    public static Task CancelAnimatedIfAsync(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Cancel(); return Task.CompletedTask; }, jsRuntime, true);

    public static Task CloseAnimatedIfAsync(this IMudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(result); return Task.CompletedTask; }, jsRuntime, true);

    public static Task CloseAnimatedIfAsync<T>(this IMudDialogInstance instance, T result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close<T>(result); return Task.CompletedTask; }, jsRuntime, true);

    // --- IDialogReference async methods ---

    public static Task CloseAnimatedAsync(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(); return Task.CompletedTask; }, jsRuntime);

    public static Task CancelAnimatedAsync(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(DialogResult.Cancel()); return Task.CompletedTask; }, jsRuntime);

    public static Task CloseAnimatedAsync(this IDialogReference instance, DialogResult result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(result); return Task.CompletedTask; }, jsRuntime);

    public static Task CloseAnimatedAsync<T>(this IDialogReference instance, T result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(DialogResult.Ok(result)); return Task.CompletedTask; }, jsRuntime);

    public static Task CloseAnimatedIfAsync(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(); return Task.CompletedTask; }, jsRuntime, true);

    public static Task CancelAnimatedIfAsync(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(DialogResult.Cancel()); return Task.CompletedTask; }, jsRuntime, true);

    public static Task CloseAnimatedIfAsync(this IDialogReference instance, DialogResult result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(result); return Task.CompletedTask; }, jsRuntime, true);

    public static Task CloseAnimatedIfAsync<T>(this IDialogReference instance, T result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, (i) => { i.Close(DialogResult.Ok(result)); return Task.CompletedTask; }, jsRuntime, true);

    // --- IMudDialogInstance obsolete methods ---

    [Obsolete("Use CloseAnimatedAsync instead.")]
    public static void CloseAnimated(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedAsync(instance, jsRuntime);

    [Obsolete("Use CancelAnimatedAsync instead.")]
    public static void CancelAnimated(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = CancelAnimatedAsync(instance, jsRuntime);

    [Obsolete("Use CloseAnimatedAsync instead.")]
    public static void CloseAnimated(this IMudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedAsync(instance, result, jsRuntime);

    [Obsolete("Use CloseAnimatedAsync instead.")]
    public static void CloseAnimated<T>(this IMudDialogInstance instance, T result, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedAsync(instance, result, jsRuntime);

    [Obsolete("Use CloseAnimatedIfAsync instead.")]
    public static void CloseAnimatedIf(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedIfAsync(instance, jsRuntime);

    [Obsolete("Use CancelAnimatedIfAsync instead.")]
    public static void CancelAnimatedIf(this IMudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = CancelAnimatedIfAsync(instance, jsRuntime);

    [Obsolete("Use CloseAnimatedIfAsync instead.")]
    public static void CloseAnimatedIf(this IMudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedIfAsync(instance, result, jsRuntime);

    [Obsolete("Use CloseAnimatedIfAsync instead.")]
    public static void CloseAnimatedIf<T>(this IMudDialogInstance instance, T result, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedIfAsync(instance, result, jsRuntime);

    // --- IDialogReference obsolete methods ---

    [Obsolete("Use CloseAnimatedAsync instead.")]
    public static void CloseAnimated(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedAsync(instance, jsRuntime);

    [Obsolete("Use CancelAnimatedAsync instead.")]
    public static void CancelAnimated(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => _ = CancelAnimatedAsync(instance, jsRuntime);

    [Obsolete("Use CloseAnimatedAsync instead.")]
    public static void CloseAnimated(this IDialogReference instance, DialogResult result, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedAsync(instance, result, jsRuntime);

    [Obsolete("Use CloseAnimatedAsync instead.")]
    public static void CloseAnimated<T>(this IDialogReference instance, T result, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedAsync(instance, result, jsRuntime);

    [Obsolete("Use CloseAnimatedIfAsync instead.")]
    public static void CloseAnimatedIf(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedIfAsync(instance, jsRuntime);

    [Obsolete("Use CancelAnimatedIfAsync instead.")]
    public static void CancelAnimatedIf(this IDialogReference instance, IJSRuntime jsRuntime = null)
        => _ = CancelAnimatedIfAsync(instance, jsRuntime);

    [Obsolete("Use CloseAnimatedIfAsync instead.")]
    public static void CloseAnimatedIf(this IDialogReference instance, DialogResult result, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedIfAsync(instance, result, jsRuntime);

    [Obsolete("Use CloseAnimatedIfAsync instead.")]
    public static void CloseAnimatedIf<T>(this IDialogReference instance, T result, IJSRuntime jsRuntime = null)
        => _ = CloseAnimatedIfAsync(instance, result, jsRuntime);

    // --- Private helpers ---

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
