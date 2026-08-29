
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Helper
{
    /// <summary>
    /// Some basic extensions for MudBlazor components or types.
    /// </summary>
    public static class Extensions
    {
        internal static ParameterView ToParameterView(this IDictionary<string, object> dict)
        {
            return ParameterView.FromDictionary(dict);
        }

        /// <summary>
        /// Returns the names of the given DialogPosition enum value.
        /// </summary>
        public static string[] GetPositionNames(this DialogPosition? position, bool switchPositions = false)
        {
            var res = position.HasValue ? Enum<DialogPosition>.GetName(position.Value).SplitByUpperCase().Select(n => n.ToLower()).ToArray() : Array.Empty<string>();

            if (switchPositions)
            {
                for (var index = 0; index < res.Length; index++)
                {
                    var re = res[index];
                    res[index] = re.Replace("top", "down").Replace("bottom", "up").Replace("right", "$right")
                        .Replace("left", "right").Replace("$right", "left");
                }
            }

            return res;
        }

        /// <summary>
        /// Try to localize the given text using the provided localizer.
        /// </summary>
        public static string TryLocalize(this IStringLocalizer localizer, string text, params object[] args)
        {
            bool hasArgs = args is {Length: > 0};
            if (text is null)
                return null;
            if (localizer != null)
                return hasArgs ? localizer[text, args.Where(a => a != null).ToArray()] : localizer[text];
            if (!hasArgs)
                return text; // without args there is nothing to substitute, and "{0}" in the text would throw
            try
            {
                return string.Format(text, args);
            }
            catch (FormatException)
            {
                return text; // a text that is not a valid format string is still better than an exception
            }
        }

        /// <summary>
        /// Returns whether the given text is localized using the provided localizer.
        /// </summary>
        public static bool IsLocalized(this IStringLocalizer localizer, string text, params object[] args)
        {
            if (text is null || localizer is null)
            {
                return false;
            }

            bool hasArgs = args is { Length: > 0 };
            var localizedValue = hasArgs ? localizer[text, args.Where(a => a != null).ToArray()] : localizer[text];

            return !localizedValue.ResourceNotFound;
        }

        /// <summary>
        /// Converts the given data to an HTML string.
        /// </summary>
        public static string ToHtml(this (string tag, Dictionary<string, object> attributes) data, string style = "", string cls = "")
        {
            string tag = data.tag;
            var attributes = data.attributes;
            if (!string.IsNullOrEmpty(style))
                attributes["style"] = MudExStyleBuilder.CombineStyleStrings(style, attributes.TryGetValue("style", out var styleValue) ? styleValue?.ToString() ?? string.Empty : string.Empty);
            
            if (!string.IsNullOrEmpty(cls))
                attributes["class"] = MudExCssBuilder.From(attributes.TryGetValue("class", out var clsValue) ? clsValue?.ToString() ?? string.Empty : string.Empty).AddClass(cls).Build();

            var attributesString = string.Join(" ", attributes.Select(kv => $"{kv.Key}=\"{kv.Value}\""));
            return $"<{tag} {attributesString}></{tag}>";
        }

    }
}