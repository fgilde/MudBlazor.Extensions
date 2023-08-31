using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;
using Nextended.Core.Extensions;
using MudBlazor.Utilities;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Helper;


/// <summary>
/// Static Util class with some small css helping methods or types
/// </summary>
[HasDocumentation("MudExCss.md")]
public static partial class MudExCss
{

    private static readonly AnimationType[] TypesWithoutPositionReplacement = { AnimationType.SlideIn };


    internal static string GetClassname<T>(MudExBaseInput<T> baseInput, Func<bool> shrinkWhen) =>
    new CssBuilder("mud-input")
        .AddClass($"mud-ex-base-input")
        .AddClass($"mud-input-{baseInput.Variant.GetDescription()}")
        .AddClass($"mud-input-margin-{baseInput.Margin.GetDescription()}", when: () => baseInput.Margin != Margin.None)
        .AddClass("mud-input-underline", when: () => !baseInput.DisableUnderLine && baseInput.Variant != Variant.Outlined)
        .AddClass("mud-shrink", when: shrinkWhen)
        .AddClass("mud-disabled", baseInput.Disabled)
        .AddClass("mud-input-error", baseInput.HasErrors)
        .AddClass("mud-ltr", baseInput.GetInputType() == InputType.Email || baseInput.GetInputType() == InputType.Telephone)
        .AddClass(baseInput.Class)
        .Build();

    internal static string GetInputClassname<T>(MudExBaseInput<T> baseInput) =>
        new CssBuilder("mud-input-slot")
            .AddClass($"mud-ex-base-input")
            .AddClass("mud-input-root")
            .AddClass($"mud-input-root-{baseInput.Variant.GetDescription()}")
            .AddClass($"mud-input-root-margin-{baseInput.Margin.GetDescription()}", when: () => baseInput.Margin != Margin.None)
            .AddClass("ms-4", baseInput.AdornmentStart != null && baseInput.Variant == Variant.Text)
            .AddClass(baseInput.Class)
            .Build();

    internal static string GetAdornmentClassname<T>(MudExBaseInput<T> baseInput) =>
        new CssBuilder("mud-input-adornment")
            .AddClass($"mud-input-adornment-start", baseInput.AdornmentStart != null || ((!string.IsNullOrEmpty(baseInput.AdornmentText) || !string.IsNullOrEmpty(baseInput.AdornmentIcon)) && baseInput.Adornment == Adornment.Start))
            .AddClass($"mud-input-adornment-end", baseInput.AdornmentEnd != null || ((!string.IsNullOrEmpty(baseInput.AdornmentText) || !string.IsNullOrEmpty(baseInput.AdornmentIcon)) && baseInput.Adornment == Adornment.End))
            .AddClass($"mud-text", !string.IsNullOrEmpty(baseInput.AdornmentText))
            .AddClass($"mud-input-root-filled-shrink", baseInput.Variant == Variant.Filled)
            .AddClass(baseInput.Class)
            .Build();


    /// <summary>
    /// Returns an applicable style string as animation for given animations options
    /// </summary>
    public static string GetAnimationCssStyle(this AnimationType type, TimeSpan? duration = null, AnimationDirection? direction = null, AnimationTimingFunction animationTimingFunction = null, DialogPosition? targetPosition = null)
        => GetAnimationCssStyle(new[] {type}, duration ?? TimeSpan.FromMilliseconds(500), direction, animationTimingFunction, targetPosition);

    /// <summary>
    /// Returns an applicable style string as animation for given animations options
    /// </summary>
    public static string GetAnimationCssStyle(this AnimationType[] types, TimeSpan duration, AnimationDirection? direction = null, AnimationTimingFunction animationTimingFunction = null, DialogPosition? targetPosition = null)
    {
        animationTimingFunction ??= AnimationTimingFunction.EaseIn;
        targetPosition ??= DialogPosition.TopCenter;
        var result = string.Join(',', types.SelectMany(type => targetPosition.GetPositionNames(!TypesWithoutPositionReplacement.Contains(type)).Select(n => $"{ReplaceAnimation(type.GetDescription(), n, direction)} {duration.TotalMilliseconds}ms {animationTimingFunction} 1 alternate")).Distinct());
        return result;
    }

    /// <summary>
    /// Can used jus to quickly access some classes.
    /// MudExCss.Get(MudExCss.Classes.Dialog.FullHeightContent, "overflow-hidden", MudExCss.Classes.Dialog._Initial);
    /// </summary>
    public static string Get(Classes cls, params Classes[] other) => MudExCssBuilder.From(cls, other).Class;

    /// <summary>
    /// Creates a style builder with applied styles from given action
    /// </summary>
    public static MudExStyleBuilder CreateStyle(Action<MudExStyleBuilder> action = null)
    {
        var builder = new MudExStyleBuilder();
        action?.Invoke(builder);
        return builder;
    }
    

    [Obsolete("Use MudExStyleBuilder instead")]
    public static string GenerateCssString(object obj, string existingCss = "") => MudExStyleBuilder.GenerateStyleString(obj, existingCss);

    [Obsolete("Use MudExStyleBuilder instead")]
    public static string GenerateCssString(object obj, CssUnit cssUnit, string existingCss = "") => MudExStyleBuilder.GenerateStyleString(obj, cssUnit, existingCss);

    [Obsolete("Use MudExStyleBuilder instead")]
    public static string CombineCSSStrings(string cssString, string leadingCssString) => MudExStyleBuilder.CombineStyleStrings(cssString, leadingCssString);

    [Obsolete("Use MudExStyleBuilder instead")]
    public static T CssStringToObject<T>(string css) where T : new() => MudExStyleBuilder.StyleStringToObject<T>(css);

    [Obsolete("Use extension method for IJSRuntime instead from MudBlazor.Extensions.Helper.JsRuntimeExtensions")]
    public static Task<KeyValuePair<string, string>[]> GetCssVariablesAsync() => JsImportHelper.GetInitializedJsRuntime().GetCssVariablesAsync();

    [Obsolete("Use extension method for IJSRuntime instead from MudBlazor.Extensions.Helper.JsRuntimeExtensions")]
    public static Task<KeyValuePair<string, string>[]> FindCssVariablesByValueAsync(string value) => JsImportHelper.GetInitializedJsRuntime().FindCssVariablesByValueAsync(value);

    [Obsolete("Use extension method for IJSRuntime instead from MudBlazor.Extensions.Helper.JsRuntimeExtensions")]
    public static Task SetCssVariableValueAsync(KeyValuePair<string, string> pair) => JsImportHelper.GetInitializedJsRuntime().SetCssVariableValueAsync(pair);

    [Obsolete("Use extension method for IJSRuntime instead from MudBlazor.Extensions.Helper.JsRuntimeExtensions")]
    public static Task SetCssVariableValueAsync(string key, object value, params object[] fallbackValues) => JsImportHelper.GetInitializedJsRuntime().SetCssVariableValueAsync(key, value, fallbackValues);

    [Obsolete("Use extension method for IJSRuntime instead from MudBlazor.Extensions.Helper.JsRuntimeExtensions")]
    public static Task SetCssVariableValueAsync(string key, object value) => JsImportHelper.GetInitializedJsRuntime().SetCssVariableValueAsync(key, value);

    [Obsolete("Use extension method for IJSRuntime instead from MudBlazor.Extensions.Helper.JsRuntimeExtensions")]
    public static Task SetCssVariableValueAsync(string key, Color color) => JsImportHelper.GetInitializedJsRuntime().SetCssVariableValueAsync(key, color);

    [Obsolete("Use extension method for IJSRuntime instead from MudBlazor.Extensions.Helper.JsRuntimeExtensions")]
    public static Task SetCssVariableValueAsync(string key, MudColor color) => JsImportHelper.GetInitializedJsRuntime().SetCssVariableValueAsync(key, color);

    [Obsolete("Use extension method for IJSRuntime instead from MudBlazor.Extensions.Helper.JsRuntimeExtensions")]
    public static Task SetCssVariableValueAsync(string key, string value) => JsImportHelper.GetInitializedJsRuntime().SetCssVariableValueAsync(key, value);

    [Obsolete("Use extension method for IJSRuntime instead from MudBlazor.Extensions.Helper.JsRuntimeExtensions")]
    public static Task<KeyValuePair<string, MudColor>[]> GetCssColorVariablesAsync() => JsImportHelper.GetInitializedJsRuntime().GetCssColorVariablesAsync();

    
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
}