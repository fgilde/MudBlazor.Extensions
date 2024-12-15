using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Helper;

public static class MudDialogInstanceExtensions
{
    public static void CloseAnimated(this MudDialogInstance instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Close(), TaskScheduler.FromCurrentSynchronizationContext());

    public static void CancelAnimated(this MudDialogInstance instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());

    public static void CloseAnimated(this MudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Close(result), TaskScheduler.FromCurrentSynchronizationContext());

    public static void CloseAnimated<T>(this MudDialogInstance instance, T result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime).ContinueWith(_ => instance.Close<T>(result), TaskScheduler.FromCurrentSynchronizationContext());

    public static void CloseAnimatedIf(this MudDialogInstance instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime, true).ContinueWith(_ => instance.Close(), TaskScheduler.FromCurrentSynchronizationContext());

    public static void CancelAnimatedIf(this MudDialogInstance instance, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime, true).ContinueWith(_ => instance.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());

    public static void CloseAnimatedIf(this MudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime, true).ContinueWith(_ => instance.Close(result), TaskScheduler.FromCurrentSynchronizationContext());

    public static void CloseAnimatedIf<T>(this MudDialogInstance instance, T result, IJSRuntime jsRuntime = null)
        => AnimateClose(instance, jsRuntime, true).ContinueWith(_ => instance.Close<T>(result), TaskScheduler.FromCurrentSynchronizationContext());

    private static Task AnimateClose(this MudDialogInstance instance, IJSRuntime jsRuntime = null, bool checkOptions = false)
    {
        var dialogId = DialogReferenceExtensions.PrepareDialogId(instance.Id);
        return (jsRuntime ?? JsImportHelper.GetInitializedJsRuntime()).InvokeVoidAsync("MudBlazorExtensions.closeDialogAnimated", dialogId, checkOptions).AsTask();
    }
}
