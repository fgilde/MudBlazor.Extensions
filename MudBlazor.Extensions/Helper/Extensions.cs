
using Microsoft.Extensions.Localization;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Helper
{
    public static class Extensions
    {
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

        public static string TryLocalize(this IStringLocalizer localizer, string text, params object[] args)
        {
            bool hasArgs = args is {Length: > 0};
            if (text is null)
                return null;
            return localizer != null ? (hasArgs ? localizer[text, args.Where(a => a != null).ToArray()] : localizer[text]) : string.Format(text, args);
        }

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


    }
}