using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Contains extension methods for IJSRuntime to call MudBlazor extensions JavaScript functions.
/// </summary>
public static class JsRuntimeExtensions
{
    /// <summary>
    /// Checks if given mouse args are within given element.
    /// </summary>
    public static Task<bool> IsWithin(this IJSRuntime runtime, MouseEventArgs args, ElementReference element)
    {
        return runtime.InvokeAsync<bool>("MudExEventHelper.isWithin", args, element).AsTask();
    }
}