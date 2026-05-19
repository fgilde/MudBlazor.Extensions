using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;
using Nextended.Core;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Contains extension methods for IJSRuntime to call MudBlazor extensions JavaScript functions.
/// </summary>
public static class JsRuntimeExtensions
{

    /// <summary>
    /// Checks if given mouse args are within given element.
    /// </summary>
    public static Task<bool> IsWithin(this IJSRuntime runtime, MouseEventArgs args, ElementReference element, CancellationToken ct = default)
        => runtime.InvokeAsync<bool>("MudExEventHelper.isWithin", ct, args, element).AsTask();

    /// <summary>
    /// Returns all current CssVariables
    /// </summary>
    public static async Task<CssVariable[]> GetCssVariablesAsync(this IJSRuntime js, CancellationToken ct = default)
        => await js.InvokeAsync<CssVariable[]>("MudExCssHelper.getCssVariables", ct);

    /// <summary>
    /// Returns css variables by value
    /// </summary>
    public static async Task<CssVariable[]> FindCssVariablesByValueAsync(this IJSRuntime js, string value, CancellationToken ct = default)
        => await js.InvokeAsync<CssVariable[]>("MudExCssHelper.findCssVariables", ct, value);

    /// <summary>
    /// Updates or creates a new css variable
    /// </summary>
    public static async Task SetCssVariableValueAsync(this IJSRuntime js, CssVariable pair, CancellationToken ct = default)
        => await js.SetCssVariableValueAsync(pair.Key, pair.Value, ct);

    /// <summary>
    /// Updates or creates a new css variable
    /// </summary>
    public static async Task SetCssVariableValueAsync(this IJSRuntime js, string key, object value, params object[] fallbackValues)
    {
        var toSet = new[] { value }.Intersect(fallbackValues.EmptyIfNull()).FirstOrDefault(v => v != null);
        if (toSet != null)
            await js.SetCssVariableValueAsync(key, toSet);
    }

    /// <summary>
    /// Updates or creates a new css variable
    /// </summary>
    public static async Task SetCssVariableValueAsync(this IJSRuntime js, string key, object value, CancellationToken ct = default)
    {
        if (value is MudColor color)
            await js.SetCssVariableValueAsync(key, color, ct);
        if (value is System.Drawing.Color dc)
            await js.SetCssVariableValueAsync(key, dc.ToMudColor(), ct);
        if (value is Color enumColor)
            await js.SetCssVariableValueAsync(key, enumColor, ct);
        else
            await js.SetCssVariableValueAsync(key, value.ToString(), ct);
    }

    /// <summary>
    /// Updates or creates a new css variable
    /// </summary>
    public static async Task SetCssVariableValueAsync(this IJSRuntime js, string key, Color color, CancellationToken ct = default)
        => await js.SetCssVariableValueAsync(key, color.CssVarDeclaration(), ct);

    /// <summary>
    /// Updates or creates a new css variable
    /// </summary>
    public static async Task SetCssVariableValueAsync(this IJSRuntime js, string key, MudColor color, CancellationToken ct = default)
        => await js.SetCssVariableValueAsync(key, color.ToString(MudColorOutputFormats.Hex), ct);

    /// <summary>
    /// Updates or creates a new css variable
    /// </summary>
    public static async Task SetCssVariableValueAsync(this IJSRuntime js, string key, string value, CancellationToken ct = default)
        => await js.InvokeVoidAsync("MudExCssHelper.setCssVariableValue", ct, key, value);

    /// <summary>
    /// Returns all CSS variables that containing color values
    /// </summary>
    public static async Task<KeyValuePair<string, MudColor>[]> GetCssColorVariablesAsync(this IJSRuntime js, CancellationToken ct = default)
    {
        var all = await js.GetCssVariablesAsync(ct);
        var res = all.Where(v => v.Value.ToLower().StartsWith("#") || v.Value.ToLower().StartsWith("rgb") || v.Value.ToLower().StartsWith("hsl"))
            .Select(k =>
            {
                var color = Check.TryCatch<MudColor, Exception>(() => new MudColor(k.Value));
                return new KeyValuePair<string, MudColor>(k.Key, color);
            }).Where(k => k.Value?.IsValid() == true)
        .ToArray();
        return res;
    }
}
