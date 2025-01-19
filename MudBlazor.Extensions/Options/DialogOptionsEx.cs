using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Options
{
    /// <summary>
    /// Extended Dialog Options class, inheriting from DialogOptions and ICloneable.
    /// </summary>
    public partial class DialogOptionsEx : ICloneable
    {
        internal MudExAppearanceService AppearanceService { get; set; }
        internal MudExAppearanceService GetAppearanceService() => AppearanceService ?? new MudExAppearanceService();

        /// <summary>
        /// Sets the current instance of DialogOptionsEx as the default option for dialogs.
        /// </summary>
        /// <returns>A reference to the current instance.</returns>
        public DialogOptionsEx SetAsDefaultDialogOptions()
        {
            OverriddenDefaultOptions = true;
            DefaultDialogOptions = CloneOptions();
            return this;
        }

        internal DotNetObjectReference<object> DotNet { get; set; }

        /// <summary>
        /// The location of the dialog.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>null</c>.
        /// </remarks>
        public DialogPosition? Position { get; set; }

        /// <summary>
        /// The maximum allowed width of the dialog.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>null</c>.
        /// </remarks>
        public MaxWidth? MaxWidth { get; set; }

        /// <summary>
        /// Allows closing the dialog by clicking outside of the dialog.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>true</c>.
        /// </remarks>
        public bool? BackdropClick { get; set; }

        /// <summary>
        /// Allows closing the dialog by pressing the Escape key.
        /// </summary>
        public bool? CloseOnEscapeKey { get; set; }

        /// <summary>
        /// Hides the dialog header.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>false</c>.
        /// </remarks>
        public bool? NoHeader { get; set; }

        /// <summary>
        /// Shows a close button in the top-right corner of the dialog.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>false</c>.
        /// </remarks>
        public bool? CloseButton { get; set; }

        /// <summary>
        /// Sets the size of the dialog to the entire screen.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>false</c>.
        /// </remarks>
        public bool? FullScreen { get; set; }

        /// <summary>
        /// Sets the width of the dialog to the width of the screen.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>false</c>.
        /// </remarks>
        public bool? FullWidth { get; set; }

        /// <summary>
        /// The custom CSS classes to apply to the dialog background.
        /// </summary>
        /// <remarks>
        /// Multiple classes must be separated by spaces.
        /// </remarks>
        public string? BackgroundClass { get; set; }

        /// <summary>
        /// If true the dialog will keep the max size constraints for resizing defined with <see cref="MaxHeight"/> and <see cref="MaxWidth"/>.
        /// </summary>
        public bool KeepMaxSizeConstraints { get; set; } = false;

        /// <summary>
        /// If true the dialog will keep the relations to the parent component.
        /// That means sizes and positions will set to values in percentages.
        /// </summary>
        public bool KeepRelations { get; set; } = true;

        /// <summary>
        /// If true the dialog will be animated on close
        /// </summary>
        public bool AnimateClose { get; set; } = true;

        /// <summary>
        /// The look and feel of the dialog component dialog.
        /// </summary>
        public MudExAppearance DialogAppearance { get; set; }

        /// <summary>
        /// The look and feel of the dialog background.
        /// </summary>
        public MudExAppearance DialogBackgroundAppearance { get; set; }

        /// <summary>
        /// The JavaScript Runtime used to interact with the browser's JavaScript host environment. 
        /// </summary>
        public IJSRuntime JsRuntime { get; set; }

        /// <summary>
        /// A boolean value indicating whether the dialog is modal or not.
        /// </summary>
        public bool Modal { get; set; } = true;

        /// <summary>
        /// A nullable boolean value indicating whether the maximize button is available or not.
        /// </summary>
        public bool? MaximizeButton { get; set; }

        /// <summary>
        /// A nullable boolean value indicating whether the minimize button is available or not.
        /// </summary>
        public bool? MinimizeButton { get; set; }

        /// <summary>
        /// Here you can set your Own custom position where the dialog should be shown.
        /// Please notice that this only works when <see cref="ShowAtCursor"/> is false. 
        /// Please also notice when this is set the <see cref="DialogOptions.Position"/> will be ignored for the position but still used for the animation.
        /// </summary>
        public MudExPosition? CustomPosition { get; set; } = null;

        /// <summary>
        /// Here you can set your own custom size that the dialog should have.
        /// Please notice when this is set the <see cref="DialogOptions.MaxWidth"/>, <see cref="MaxHeight"/>, <see cref="FullHeight"/> and <see cref="DialogOptions.FullWidth"/> will be ignored.
        /// </summary>
        public MudExDimension? CustomSize { get; set; } = null;

        /// <summary>
        /// A boolean value indicating whether the dialog is shown at the cursor location or not.
        /// Please notice if this is true <see cref="Position"/> and <see cref="CustomPosition"/> will be ignored.
        /// </summary>
        public bool ShowAtCursor { get; set; }

        /// <summary>
        /// The point on the dialog box where the cursor is placed on showing the dialog box.
        /// this is only used when <see cref="ShowAtCursor"/> is true.
        /// </summary>
        public Origin CursorPositionOrigin { get; set; } = Origin.CenterCenter;

        /// <summary>
        /// A boolean value indicating whether the dialog is resizable or not.
        /// </summary>
        public bool Resizeable { get; set; }

        /// <summary>
        /// An array of MudDialogButton.
        /// </summary>
        public MudDialogButton[] Buttons { get; set; }

        /// <summary>
        /// Here you can set a callback that will be called before the dialog is closed.
        /// Ensure your callback returns a boolean value and has the JsInvokable attribute set.
        /// </summary>
        public string CanCloseCallbackName { get; set; }

        /// <summary>
        /// the .NET object reference where the <see cref="CanCloseCallbackName"/> is located.
        /// if you don't specify reference the dialog itself will be the reference.Then you need to have the CanCloseCallback Method on your DialogClass
        /// </summary>
        [IgnoreOnObjectEdit]
        public DotNetObjectReference<object> CanCloseCallbackReference { get; set; }


        /// <summary>
        /// A MudDialogDragMode value indicating the drag mode of the dialog box.
        /// </summary>
        public MudDialogDragMode DragMode { get; set; }

        /// <summary>
        /// A nullable boolean value indicating whether the dialog full height mode is enabled or not.
        /// </summary>
        public bool? FullHeight { get; set; }

        /// <summary>
        /// MaxHeight is similar to original MaxWidth
        /// </summary>
        public MaxHeight? MaxHeight { get; set; } = null;


        /// <summary>
        /// A nullable boolean value indicating whether the dialog position margin is disabled or not.
        /// </summary>
        public bool? DisablePositionMargin { get; set; }

        /// <summary>
        /// A nullable boolean value indicating whether the dialog size margin x-axis is disabled or not.
        /// </summary>
        public bool? DisableSizeMarginX { get; set; }

        /// <summary>
        /// A nullable boolean value indicating whether the dialog size margin y-axis is disabled or not.
        /// </summary>
        public bool? DisableSizeMarginY { get; set; }

        /// <summary>
        /// An animation type value indicating the animation effect of the dialog box.
        /// </summary>
        public AnimationType? Animation
        {
            get => Animations?.FirstOrDefault();
            set => Animations = new[] { value ?? AnimationType.Default };
        }

        /// <summary>
        /// An array of animation type values indicating the set of animation effects of the dialog box.
        /// </summary>
        public AnimationType[] Animations { get; set; } = { AnimationType.Default };

        /// <summary>
        /// An animation timing function value indicating the easing function used for the dialog box animation transitions.
        /// </summary>
        public AnimationTimingFunction AnimationTimingFunction { get; set; } = AnimationTimingFunction.EaseInOut;

        /// <summary>
        /// A time span value indicating the duration of the dialog animation effect.
        /// </summary>
        public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// A double value indicating the dialog animation duration in milliseconds.
        /// </summary>
        public double AnimationDurationInMs
        {
            get => AnimationDuration.TotalMilliseconds;
            set => AnimationDuration = TimeSpan.FromMilliseconds(value);
        }

        /// <summary>
        /// The description string of the cursor position.
        /// </summary>
        public string CursorPositionOriginName => CursorPositionOrigin.GetDescription();

        /// <summary>
        /// A css style string indicating the animation effects of the dialog box.
        /// </summary>
        public string AnimationStyle => Animations?.Any() == true
            ? Animations.GetAnimationCssStyle(AnimationDuration, AnimationDirection.In, AnimationTimingFunction,
                Position)
            : string.Empty;
      

        /// <summary>
        /// Method that returns a clone object of the current instance.
        /// </summary>
        /// <returns>A cloned instance of the current object.</returns>
        public DialogOptionsEx CloneOptions()
        {                        
            var res = Clone() as DialogOptionsEx;
            if (res == null)
                return null;
            res.DialogAppearance = DialogAppearance?.Clone() as MudExAppearance;
            res.DialogBackgroundAppearance = DialogBackgroundAppearance?.Clone() as MudExAppearance;
            return res;
        }

        /// <summary>       
        /// Method that returns a cloned instance of the current object. 
        /// </summary>
        /// <returns>A cloned object instance of the current object.</returns>
        public object Clone() => MemberwiseClone();

        public DialogOptions ToMudDialogOptions() => new()
        {
            MaxWidth = MaxWidth,
            NoHeader = NoHeader,
            BackdropClick = BackdropClick,
            BackgroundClass = BackgroundClass,
            CloseButton = CloseButton,
            CloseOnEscapeKey = CloseOnEscapeKey,
            FullScreen = FullScreen,
            FullWidth = FullWidth,
            Position = Position,
        };
        
        public static DialogOptionsEx FromMudDialogOptions (DialogOptions options) => new()
        {
            MaxWidth = options.MaxWidth,
            NoHeader = options.NoHeader,
            BackdropClick = options.BackdropClick,
            BackgroundClass = options.BackgroundClass,
            CloseButton = options.CloseButton,
            CloseOnEscapeKey = options.CloseOnEscapeKey,
            FullScreen = options.FullScreen,
            FullWidth = options.FullWidth,
            Position = options.Position,
        };

        public static implicit operator DialogOptionsEx(DialogOptions options) => FromMudDialogOptions(options);
        public static implicit operator DialogOptions(DialogOptionsEx options) => options.ToMudDialogOptions();

    }
}