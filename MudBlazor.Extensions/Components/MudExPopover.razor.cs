using BlazorJS.JsInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace MudBlazor.Extensions.Components
{
    public partial class MudExPopover 
    {
        #region Delegating Member

        [CascadingParameter(Name = "RightToLeft")]
        public bool RightToLeft { get; set; }

        //
        // Summary:
        //     Sets the maxheight the popover can have when open.
        [Parameter]
        [Category("Appearance")]
        public int? MaxHeight { get; set; }

        //
        // Summary:
        //     If true, will apply default MudPaper classes.
        [Parameter]
        [Category("Appearance")]
        public bool Paper { get; set; } = true;


        //
        // Summary:
        //     The higher the number, the heavier the drop-shadow.
        [Parameter]
        [Category("Appearance")]
        public int Elevation { get; set; } = 8;


        //
        // Summary:
        //     If true, border-radius is set to 0.
        [Parameter]
        [Category("Appearance")]
        public bool Square { get; set; }

        //
        // Summary:
        //     If true, the popover is visible.
        [Parameter]
        [Category("Behavior")]
        public bool Open { get; set; }

        //
        // Summary:
        //     If true the popover will be fixed position instead of absolute.
        [Parameter]
        [Category("Behavior")]
        public bool Fixed { get; set; }

        //
        // Summary:
        //     Sets the length of time that the opening transition takes to complete.
        [Parameter]
        [Category("Appearance")]
        public double Duration { get; set; } = 251.0;


        //
        // Summary:
        //     Sets the amount of time in milliseconds to wait from opening the popover before
        //     beginning to perform the transition.
        [Parameter]
        [Category("Appearance")]
        public double Delay { get; set; }

        //
        // Summary:
        //     Sets the direction the popover will start from relative to its parent.
        [Obsolete("Use AnchorOrigin and TransformOrigin instead.", true)]
        [Parameter]
        public Direction Direction { get; set; }

        //
        // Summary:
        //     Set the anchor point on the element of the popover. The anchor point will determinate
        //     where the popover will be placed.
        [Parameter]
        [Category("Appearance")]
        public Origin AnchorOrigin { get; set; }

        //
        // Summary:
        //     Sets the intersection point if the anchor element. At this point the popover
        //     will lay above the popover. This property in conjunction with AnchorPlacement
        //     determinate where the popover will be placed.
        [Parameter]
        [Category("Appearance")]
        public Origin TransformOrigin { get; set; }

        //
        // Summary:
        //     Set the overflow behavior of a popover and controls how the element should react
        //     if there is not enough space for the element to be visible Defaults to none,
        //     which doens't apply any overflow logic
        [Parameter]
        [Category("Appearance")]
        public OverflowBehavior OverflowBehavior { get; set; } = OverflowBehavior.FlipOnOpen;


        //
        // Summary:
        //     If true, the select menu will open either above or bellow the input depending
        //     on the direction.
        [ExcludeFromCodeCoverage]
        [Obsolete("Use AnchorOrigin and TransformOrigin instead.", true)]
        [Parameter]
        public bool OffsetX { get; set; }

        //
        // Summary:
        //     If true, the select menu will open either before or after the input depending
        //     on the direction.
        [ExcludeFromCodeCoverage]
        [Obsolete("Use AnchorOrigin and TransformOrigin instead.", true)]
        [Parameter]
        public bool OffsetY { get; set; }

        //
        // Summary:
        //     If true, the popover will have the same width at its parent element, default
        //     to false
        [Parameter]
        [Category("Appearance")]
        public bool RelativeWidth { get; set; }

        //
        // Summary:
        //     Child content of the component.
        [Parameter]
        [Category("Behavior")]
        public RenderFragment ChildContent { get; set; }
        #endregion

        [Parameter] public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(650);
        [Parameter] public bool AutoCloseOnBlur { get; set; } = true;
        [Parameter] public string SelectorsForIgnoreBlur { get; set; }

        [Parameter] public AnimationType Animation { get; set; } = AnimationType.SlideIn;
        [Parameter] public AnimationTimingFunction AnimationTimingFunction { get; set; }

        [Parameter] public EventCallback<PointerEventArgs> OnBlur { get; set; }

        private BlazorJSEventInterop<PointerEventArgs> _jsEvent;
        private string classId = $"mud-ex-popover-{Guid.NewGuid()}";
        
        public new string Style => AnimationStyle();
        
        protected override async Task OnInitializedAsync()
        {            
            //if (string.IsNullOrEmpty(Class) || !Class.Contains(classId))
            //    Class += $" {classId}";
            _jsEvent = new BlazorJSEventInterop<PointerEventArgs>(JsRuntime);
            await _jsEvent.OnBlur(OnFocusLeft, $".{classId}", SelectorsForIgnoreBlur);
            await base.OnInitializedAsync();
        }


        private string AnimationStyle()
        {
            if (Open)
                return $"animation: {new[] { Animation }.GetAnimationCssStyle(AnimationDuration, AnimationDirection.In, AnimationTimingFunction)}";
            else
                return $"animation: {new[] { AnimationType.RubberBand }.GetAnimationCssStyle(AnimationDuration, AnimationDirection.In, AnimationTimingFunction)}";
        }

        protected override Task OnParametersSetAsync()
        {
            //EnsureClass();
         //  Style = Open ? AnimationStyle() : "";
            return base.OnParametersSetAsync();
        }

        private Task OnFocusLeft(PointerEventArgs arg)
        {
            if (AutoCloseOnBlur)
            {
                Open = false;
                StateHasChanged();
            }
            OnBlur.InvokeAsync(arg);
            return Task.CompletedTask;
        }

        private RenderFragment Inherited() => builder =>
        {
            base.BuildRenderTree(builder);
        };
    
    }
}
