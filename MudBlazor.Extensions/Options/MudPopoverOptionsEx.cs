using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Options
{
    public class MudPopoverOptionsEx
    {
        public IJSRuntime JsRuntime { get; set; }

        /// <summary>
        /// Click on any of this selectors will not close the popover if AutoHideOnBlur is true
        /// </summary>
        public string[] ExcludedBlurSelectors { get; set; }
        public bool AutoHideOnBlur { get; set; }
        public AnimationType[] Animations { get; set; }
        public AnimationTimingFunction AnimationTimingFunction { get; set; } = AnimationTimingFunction.EaseInOut;
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(500);
        public double AnimationDurationInMs
        {
            get => AnimationDuration.TotalMilliseconds;
            set => AnimationDuration = TimeSpan.FromMilliseconds(value);
        }

        public string AnimationTimingFunctionString => AnimationTimingFunction.ToString();
        public string[] AnimationDescriptions => Animations.Select(type => type.ToDescriptionString()).ToArray();
        
    }
}