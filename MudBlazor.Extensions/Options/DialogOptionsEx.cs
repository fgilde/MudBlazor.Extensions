using System;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Options
{
    public class DialogOptionsEx : DialogOptions
    {
        public bool? MaximizeButton { get; set; }
        //public bool? MinimizeButton { get; set; }
        public bool Resizeable { get; set; }
        public MudDialogButton[] Buttons { get; set; }
        public MudDialogDragMode DragMode { get; set; }
        public bool? FullHeight { get; set; }
        public bool? DisablePositionMargin { get; set; }
        public bool? DisableSizeMarginX { get; set; }
        public bool? DisableSizeMarginY { get; set; }
        public AnimationType? Animation { get; set; }
        public AnimationTimingFunction AnimationTimingFunction { get; set; } = AnimationTimingFunction.EaseInOut;
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(500);
        public double AnimationDurationInMs
        {
            get => AnimationDuration.TotalMilliseconds;
            set => AnimationDuration = TimeSpan.FromMilliseconds(value);
        }

        public string AnimationTimingFunctionString => AnimationTimingFunction.ToString();
        public string[] DialogPositionNames => Position.GetPositionNames();
        public string DialogPositionDescription => Position.ToDescriptionString();
        public string AnimationDescription => Animation.ToDescriptionString();
        
    }
}