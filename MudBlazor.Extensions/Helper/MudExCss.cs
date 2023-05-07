using MudBlazor.Extensions.Core;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MudBlazor.Extensions.Options;
using Nextended.Core.Extensions;
using Microsoft.JSInterop;
using MudBlazor.Utilities;
using Nextended.Core;
using MudBlazor.Extensions.Attribute;

namespace MudBlazor.Extensions.Helper;

[HasDocumentation("MudExCss.md")]
public static class MudExCss
{
    private static readonly string[] PropertiesToAddUnits = { "height", "width", "min-height", "min-width", "max-height", "max-width", 
        "padding", "padding-top", "padding-right", "padding-bottom", "padding-left", "margin", "margin-top", "margin-right", "margin-bottom",
        "margin-left", "border-width", "border-top-width", "border-right-width", "border-bottom-width", "border-left-width", "font-size", "letter-spacing", 
        "line-height", "word-spacing", "text-indent", "column-gap", "column-width", "top", "right", "bottom", "left", "transform", "translate", "translateX", 
        "translateY", "translateZ", "translate3d", "rotate", "rotateX", "rotateY", "rotateZ", "scale", "scaleX", "scaleY", "scaleZ", "scale3d", 
        "skew", "skewX", "skewY", "perspective"};

    private static AnimationType[] typesWithoutPositionReplacement = { AnimationType.SlideIn };

    public static string GetAnimationCssStyle(this AnimationType type, TimeSpan duration, AnimationDirection? direction = null, AnimationTimingFunction animationTimingFunction = null, DialogPosition? targetPosition = null)
        => GetAnimationCssStyle(new[] {type}, duration, direction, animationTimingFunction, targetPosition);

    

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

    public static string GenerateCssString(object obj, string existingCss = "")
    {
        return GenerateCssString(obj, CssUnit.Pixels, existingCss);
    }
    
    public static string GenerateCssString(object obj, CssUnit cssUnit, string existingCss = "")
    {
        string unit = cssUnit.ToDescriptionString();
        var cssBuilder = new StringBuilder();

        // Get all of the properties of the object
        var properties = obj.GetType().GetProperties();

        // Iterate through the properties and append their names and values to the CSS string
        foreach (var property in properties)
        {
            // Split the property name on any uppercase character and concatenate the split parts with a hyphen
            var cssPropertyName = Regex.Replace(property.Name, "(?<!^)([A-Z])", "-$1").ToLower();

            // Surround the property value in quotes if it is a string
            object propertyValue = property.GetValue(obj, null);
            if (propertyValue != null)
            {
                if (propertyValue is Color color)
                    propertyValue = color.CssVarDeclaration();
                var formattedPropertyValue = propertyValue is string ? $"{propertyValue}" : propertyValue?.ToString();

                // If the property is an integer and its name is in the list of properties that should have units added, add the specified unit
                //if (propertyValue is int && PropertiesToAddUnits.Contains(cssPropertyName))
                if (int.TryParse(formattedPropertyValue, out _) && PropertiesToAddUnits.Contains(cssPropertyName))
                    formattedPropertyValue += unit;

                cssBuilder.Append(cssPropertyName + ": " + formattedPropertyValue + ";");
            }
        }

        return string.IsNullOrEmpty(existingCss) ? cssBuilder.ToString() : CombineCSSStrings(cssBuilder.ToString(), existingCss);
    }

    public static string CombineCSSStrings(string cssString, string leadingCssString)
    {
        // Create a dictionary to store the CSS properties and values
        var cssProperties = new Dictionary<string, string>();

        // Use a regular expression to extract the properties and values from the first CSS string
        var cssRegex = new Regex(@"([\w-]+)\s*:\s*([^;]+)");
        var cssProperties1 = cssRegex.Matches(cssString);
        foreach (Match property in cssProperties1)
        {
            // Trim any leading or trailing whitespace from the key and value
            var key = property.Groups[1].Value.Trim();
            var value = property.Groups[2].Value.Trim();

            // Add the property to the dictionary if it doesn't already exist
            if (!cssProperties.ContainsKey(key))
            {
                cssProperties.Add(key, value);
            }
        }

        // Use a regular expression to extract the properties and values from the second CSS string
        var cssProperties2 = cssRegex.Matches(leadingCssString);
        foreach (Match property in cssProperties2)
        {
            // Trim any leading or trailing whitespace from the key and value
            string key = property.Groups[1].Value.Trim();
            string value = property.Groups[2].Value.Trim();

            // Add the property to the dictionary or update its value if it already exists
            if (cssProperties.ContainsKey(key))
            {
                cssProperties[key] = value;
            }
            else
            {
                cssProperties.Add(key, value);
            }
        }

        return cssProperties.Aggregate("", (current, property) => current + (property.Key + ": " + property.Value + "; "));
    }


    public static T CssStringToObject<T>(string css) where T : new()
    {
        T obj = new T();

        // Split the CSS string into individual properties
        string[] properties = css.Split(';');

        // Iterate through the properties and set the corresponding property values on the object
        foreach (string property in properties)
        {
            // Split the property into its name and value
            string[] propertyParts = property.Split(':');
            if (propertyParts.Length != 2)
            {
                continue;
            }

            string propertyName = propertyParts[0].Trim();
            string propertyValue = propertyParts[1].Trim();

            // Convert the property name to camelCase
            propertyName = Regex.Replace(propertyName, "-([a-z])", m => m.Groups[1].Value.ToUpperInvariant());

            // Try to set the property value on the object
            try
            {
                PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(obj, propertyValue);
                }
            }
            catch (Exception)
            {
                // Ignore any errors that occur while setting the property value
            }
        }

        return obj;
    }

    public static async Task<KeyValuePair<string, string>[]> GetCssVariablesAsync()
    {
        var js = await JsImportHelper.GetInitializedJsRuntime();
        var res = await js.InvokeAsync<KeyValuePair<string, string>[]>("MudExCssHelper.getCssVariables");        
        return res;
    }

    public static async Task<KeyValuePair<string, string>[]> FindCssVariablesByValueAsync(string value)
    {
        var js = await JsImportHelper.GetInitializedJsRuntime();
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
        var js = await JsImportHelper.GetInitializedJsRuntime();
        await js.InvokeVoidAsync("MudExCssHelper.setCssVariableValue", key, value);        
    }

    public static async Task<KeyValuePair<string, Utilities.MudColor>[]> GetCssColorVariablesAsync()
    {
        var all = await GetCssVariablesAsync();
        var res = all.Select(k =>
        {
            var color = Check.TryCatch<MudColor, Exception>(() => new MudColor(k.Value));            
            return new KeyValuePair<string, MudColor>(k.Key, color);
        }).Where(k => k.Value != null)
        .ToArray();
        return res;
    }
}