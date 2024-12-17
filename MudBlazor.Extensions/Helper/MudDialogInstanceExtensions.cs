using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Helper;

public static class MudDialogInstanceExtensions
{
    public static void CloseAnimated(this MudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(); return Task.CompletedTask; }, jsRuntime);

    public static void CancelAnimated(this MudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Cancel(); return Task.CompletedTask; }, jsRuntime);

    public static void CloseAnimated(this MudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(result); return Task.CompletedTask; }, jsRuntime);
    public static void CloseAnimated<T>(this MudDialogInstance instance, T result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close<T>(result); return Task.CompletedTask; }, jsRuntime);

    public static void CloseAnimatedIf(this MudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(); return Task.CompletedTask; }, jsRuntime, true);

    public static void CancelAnimatedIf(this MudDialogInstance instance, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Cancel(); return Task.CompletedTask; }, jsRuntime, true);

    public static void CloseAnimatedIf(this MudDialogInstance instance, DialogResult result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close(result); return Task.CompletedTask; }, jsRuntime, true);

    public static void CloseAnimatedIf<T>(this MudDialogInstance instance, T result, IJSRuntime jsRuntime = null)
        => _ = AnimateClose(instance, (i) => { i.Close<T>(result); return Task.CompletedTask; }, jsRuntime, true);

    private static async Task AnimateClose(this MudDialogInstance instance, Func<MudDialogInstance, Task> callback, IJSRuntime jsRuntime = null, bool checkOptions = false)
    {
        var dialogId = DialogReferenceExtensions.PrepareDialogId(instance.Id);
        await (jsRuntime ?? JsImportHelper.GetInitializedJsRuntime()).InvokeVoidAsync("MudBlazorExtensions.closeDialogAnimated", dialogId, checkOptions);
        await callback(instance);
    }

}
