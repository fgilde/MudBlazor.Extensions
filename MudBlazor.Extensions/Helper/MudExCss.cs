using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;
using Nextended.Core.Extensions;
using Microsoft.JSInterop;
using MudBlazor.Utilities;
using Nextended.Core;
using MudBlazor.Extensions.Attribute;
using System.Drawing;

namespace MudBlazor.Extensions.Helper;

[HasDocumentation("MudExCss.md")]
public static partial class MudExCss
{

    private static AnimationType[] typesWithoutPositionReplacement = { AnimationType.SlideIn };

    public static string GetAnimationCssStyle(this AnimationType type, TimeSpan duration, AnimationDirection? direction = null, AnimationTimingFunction animationTimingFunction = null, DialogPosition? targetPosition = null)
        => GetAnimationCssStyle(new[] {type}, duration, direction, animationTimingFunction, targetPosition);

    public static string Get(Classes cls, params Classes[] other) => MudExCssBuilder.From(cls, other).Class;

    public static MudExStyleBuilder CreateStyle(Action<MudExStyleBuilder> action = null)
    {
        var builder = new MudExStyleBuilder();
        action?.Invoke(builder);
        return builder;
    }

    public static string GetAnimationCssStyle(this AnimationType[] types, TimeSpan duration, AnimationDirection? direction = null, AnimationTimingFunction animationTimingFunction = null, DialogPosition? targetPosition = null)
    {
        animationTimingFunction ??= AnimationTimingFunction.EaseIn;
        targetPosition ??= DialogPosition.TopCenter;
        var result = string.Join(',', types.SelectMany(type => targetPosition.GetPositionNames(!typesWithoutPositionReplacement.Contains(type)).Select(n => $"{ReplaceAnimation(type.ToDescriptionString(), n, direction)} {duration.TotalMilliseconds}ms {animationTimingFunction} 1 alternate")).Distinct());
        return result;
    }

    private static string ReplaceAnimation(string animationDesc, string position, AnimationDirection? direction)
    { 
        string fallBackPosition = string.IsNullOrWhiteSpace(position) ? "Down" : position;
        animationDesc = animationDesc.Replace("{InOut?}", direction.HasValue ? Enum.GetName(direction.Value) ?? string.Empty : string.Empty);
        animationDesc = animationDesc.Replace("{InOut}", Enum.GetName(direction ?? AnimationDirection.In));
        animationDesc = animationDesc.Replace("{Pos?}", position?.ToUpper(true) ?? "");
        animationDesc = animationDesc.Replace("{Pos}", fallBackPosition.ToUpper(true));
        animationDesc = animationDesc.Replace("{pos?}", position?.ToLower(true) ?? "");
        animationDesc = animationDesc.Replace("{pos}", fallBackPosition.ToLower(true));
        return animationDesc;
    }

    [Obsolete("Use MudExStyleBuilder instead")]
    public static string GenerateCssString(object obj, string existingCss = "") => MudExStyleBuilder.GenerateStyleString(obj, existingCss);

    [Obsolete("Use MudExStyleBuilder instead")]
    public static string GenerateCssString(object obj, CssUnit cssUnit, string existingCss = "") => MudExStyleBuilder.GenerateStyleString(obj, cssUnit, existingCss);

    [Obsolete("Use MudExStyleBuilder instead")]
    public static string CombineCSSStrings(string cssString, string leadingCssString) => MudExStyleBuilder.CombineStyleStrings(cssString, leadingCssString);

    [Obsolete("Use MudExStyleBuilder instead")]
    public static T CssStringToObject<T>(string css) where T : new() => MudExStyleBuilder.StyleStringToObject<T>(css);

    public static async Task<KeyValuePair<string, string>[]> GetCssVariablesAsync()
    {
        var js = await JsImportHelper.GetInitializedJsRuntimeAsync();
        var res = await js.InvokeAsync<KeyValuePair<string, string>[]>("MudExCssHelper.getCssVariables");        
        return res;
    }

    public static async Task<KeyValuePair<string, string>[]> FindCssVariablesByValueAsync(string value)
    {
        var js = await JsImportHelper.GetInitializedJsRuntimeAsync();
        var res = await js.InvokeAsync<KeyValuePair<string, string>[]>("MudExCssHelper.findCssVariables", value);
        return res;
    }

    public static async Task SetCssVariableValueAsync(KeyValuePair<string, string> pair)
    {
        await SetCssVariableValueAsync(pair.Key, pair.Value);
    }

    public static async Task SetCssVariableValueAsync(string key, object value, params object[] fallbackValues)
    {
        var toSet = new[] {value}.Intersect(fallbackValues.EmptyIfNull()).FirstOrDefault(v => v != null);
        if (toSet != null)
            await SetCssVariableValueAsync(key, toSet);
    }

    public static async Task SetCssVariableValueAsync(string key, object value)
    {
        if (value is MudColor color)
            await SetCssVariableValueAsync(key, color);
        if (value is System.Drawing.Color dc)
            await SetCssVariableValueAsync(key, dc.ToMudColor());
        if (value is Color enumColor)
            await SetCssVariableValueAsync(key, enumColor);
        else
            await SetCssVariableValueAsync(key, value.ToString());
    }

    public static async Task SetCssVariableValueAsync(string key, Color color)
    {
         await SetCssVariableValueAsync(key, color.CssVarDeclaration());
    }

    public static async Task SetCssVariableValueAsync(string key, MudColor color)
    {
        await SetCssVariableValueAsync(key, color.ToString(MudColorOutputFormats.Hex));
    }
    
    public static async Task SetCssVariableValueAsync(string key, string value)
    {
        var js = await JsImportHelper.GetInitializedJsRuntimeAsync();
        await js.InvokeVoidAsync("MudExCssHelper.setCssVariableValue", key, value);        
    }

    public static async Task<KeyValuePair<string, MudColor>[]> GetCssColorVariablesAsync()
    {
        var all = await GetCssVariablesAsync();
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