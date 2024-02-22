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
    public static Task<bool> IsWithin(this IJSRuntime runtime, MouseEventArgs args, ElementReference element) 
        => runtime.InvokeAsync<bool>("MudExEventHelper.isWithin", args, element).AsTask();

    /// <summary>
    /// Returns all current CssVariables
    /// </summary>
    public static async Task<CssVariable[]> GetCssVariablesAsync(this IJSRuntime js) 
        => await js.InvokeAsync<CssVariable[]>("MudExCssHelper.getCssVariables");

    /// <summary>
    /// Returns css variables by value
    /// </summary>
    public static async Task<CssVariable[]> FindCssVariablesByValueAsync(this IJSRuntime js, string value) 
        => await js.InvokeAsync<CssVariable[]>("MudExCssHelper.findCssVariables", value);

    /// <summary>
    /// Updates or creates a new css variable
    /// </summary>
    public static async Task SetCssVariableValueAsync(this IJSRuntime js, CssVariable pair) 
        => await js.SetCssVariableValueAsync(pair.Key, pair.Value);

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
    public static async Task SetCssVariableValueAsync(this IJSRuntime js, string key, object value)
    {
        if (value is MudColor color)
            await js.SetCssVariableValueAsync(key, color);
        if (value is System.Drawing.Color dc)
            await js.SetCssVariableValueAsync(key, dc.ToMudColor());
        if (value is Color enumColor)
            await js.SetCssVariableValueAsync(key, enumColor);
        else
            await js.SetCssVariableValueAsync(key, value.ToString());
    }

    /// <summary>
    /// Updates or creates a new css variable
    /// </summary>
    public static async Task SetCssVariableValueAsync(this IJSRuntime js, string key, Color color) 
        => await js.SetCssVariableValueAsync(key, color.CssVarDeclaration());

    /// <summary>
    /// Updates or creates a new css variable
    /// </summary>
    public static async Task SetCssVariableValueAsync(this IJSRuntime js, string key, MudColor color) 
        => await js.SetCssVariableValueAsync(key, color.ToString(MudColorOutputFormats.Hex));

    /// <summary>
    /// Updates or creates a new css variable
    /// </summary>
    public static async Task SetCssVariableValueAsync(this IJSRuntime js, string key, string value) 
        => await js.InvokeVoidAsync("MudExCssHelper.setCssVariableValue", key, value);

    /// <summary>
    /// Returns all CSS variables that containing color values
    /// </summary>
    public static async Task<KeyValuePair<string, MudColor>[]> GetCssColorVariablesAsync(this IJSRuntime js)
    {
        var all = await js.GetCssVariablesAsync();
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