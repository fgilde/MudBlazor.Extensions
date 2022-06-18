
using MudBlazor.Extensions.Options;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Helper
{
    public static class Extensions
    {
        private static AnimationType[] typesWithoutPositionReplacement = { AnimationType.SlideIn };
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

        public static string GetAnimationCssStyle(this AnimationType type, TimeSpan duration, AnimationDirection? direction = null, AnimationTimingFunction animationTimingFunction = null, DialogPosition? targetPosition = null)
            => GetAnimationCssStyle(new[] { type }, duration, direction, animationTimingFunction, targetPosition);

        public static string GetAnimationCssStyle(this AnimationType[] types, TimeSpan duration, AnimationDirection? direction = null, AnimationTimingFunction animationTimingFunction = null, DialogPosition? targetPosition = null)
        {
            animationTimingFunction ??= AnimationTimingFunction.EaseIn;
            targetPosition ??= DialogPosition.TopCenter;
            return string.Join(',', types.SelectMany(type => targetPosition.GetPositionNames(!typesWithoutPositionReplacement.Contains(type)).Select(n => $"{ReplaceAnimation(type.ToDescriptionString(), n, direction)} {duration.TotalMilliseconds}ms {animationTimingFunction} 1 alternate")).Distinct());
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

    }
}