﻿using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Options;

public partial class DialogOptionsEx
{

    /// <summary>
    /// Recommended default met config to render dialog options in mudex object edit.
    /// </summary>
    public static void ConfigureObjectEditMeta(ObjectEditMeta<DialogOptionsEx> meta)
    {
        meta.Properties(o => o.AnimationDuration, o => o.Buttons, o => o.AnimationTimingFunction, o => o.Animation,
            o => o.AnimationStyle, o => o.JsRuntime, o => o.CursorPositionOriginName,
            o => o.AnimationStyle).Ignore();
        meta.Properties(
            o => o.Animations
        ).WrapInMudItem(i => i.xs = 12);
        meta.Property(o => o.BackgroundClass).Ignore();
        meta.Properties(o => o.MaxWidth, o => o.MaxHeight).WithOrder(0).WithGroup("Options");
        meta.Properties(o => o.DragMode, o => o.Position, o => o.AnimationDurationInMs).WithOrder(1)
            .WithGroup("Options");
        meta.Properties().Where(p =>
                (p.PropertyInfo.DeclaringType == typeof(DialogOptionsEx) ||
                 p.PropertyInfo.DeclaringType == typeof(DialogOptions))
                && (p.PropertyInfo.PropertyType == typeof(bool) || p.PropertyInfo.PropertyType.IsNullableBool()))
            .WithGroup("Options").WithOrder(50).WrapInMudItem(i =>
            {
                i.xl = 6;
                i.lg = 6;
                i.xs = 12;
            });
        meta.Properties(o => o.Position).WrapInMudItem(i =>
        {
            i.xl = 12;
            i.lg = 12;
            i.xs = 12;
        });

        meta.Property(o => o.ShowAtCursor).WithOrder(99);
        meta.Property(o => o.CursorPositionOrigin).WithOrder(100).WithGroup("Options")
            .AsReadOnlyIf(m => !m.ShowAtCursor);

        meta.WrapEachInMudItem(i =>
        {
            i.xl = 6;
            i.lg = 6;
            i.xs = 12;
        });

        meta.Properties(m => m.CustomPosition, m => m.CustomSize).Ignore(); // TODO: Create Dimension edit and MudExSize edit 
    }


    /// <summary>
    /// A boolean value indicating whether the default options are overriden.
    /// </summary>
    internal static bool OverriddenDefaultOptions { get; set; }

    /// <summary>
    /// The default dialog options object.
    /// </summary>
    public static DialogOptionsEx DefaultDialogOptions { get; set; } = new()
    {
        DragMode = MudDialogDragMode.Simple,
        CloseButton = true,
        BackdropClick = true,
        MaxWidth = MudBlazor.MaxWidth.ExtraSmall,
        FullWidth = true,
        Animations = new[] { AnimationType.FlipX }
    };

    /// <summary>
    /// Dialog options for file display dialog.
    /// </summary>
    public static DialogOptionsEx FileDisplayDialogOptions => new()
    {
        CloseButton = true,
        MaxWidth = MudBlazor.MaxWidth.ExtraExtraLarge,
        FullWidth = true,
        BackdropClick = true,
        MaximizeButton = true,
        DragMode = MudDialogDragMode.Simple,
        Position = DialogPosition.BottomCenter,
        Animations = new[] { AnimationType.FadeIn, AnimationType.SlideIn },
        AnimationDuration = TimeSpan.FromSeconds(1),
        DisablePositionMargin = true,
        DisableSizeMarginX = false,
        DisableSizeMarginY = false,
        FullHeight = true,
        Resizeable = true
    };

    /// <summary>
    /// Dialog preset to slide in from right.
    /// </summary>
    public static DialogOptionsEx SlideInFromRight => new()
    {
        MaximizeButton = true,
        CloseButton = true,
        FullHeight = true,
        CloseOnEscapeKey = true,
        MaxWidth = MudBlazor.MaxWidth.Medium,
        FullWidth = true,
        DragMode = MudDialogDragMode.Simple,
        Animations = new[] { AnimationType.SlideIn },
        Position = DialogPosition.CenterRight,
        DisableSizeMarginY = true,
        DisablePositionMargin = true
    };

    /// <summary>
    /// Dialog preset to slide in from left.
    /// </summary>
    public static DialogOptionsEx SlideInFromLeft => SlideInFromRight.SetProperties(o => o.Position = DialogPosition.CenterLeft);
    
    /// <summary>
    /// Dialog preset to slide in from top.
    /// </summary>
    public static DialogOptionsEx SlideInFromTop => SlideInFromRight.SetProperties(o =>
    {
        o.Position = DialogPosition.TopCenter;
        o.MaxWidth = MudBlazor.MaxWidth.ExtraExtraLarge;
        o.DisableSizeMarginX = true;
        o.FullHeight = false;
    });

    /// <summary>
    /// Dialog preset to slide in from bottom.
    /// </summary>
    public static DialogOptionsEx SlideInFromBottom => SlideInFromTop.SetProperties(o =>
    {
        o.Position = DialogPosition.BottomCenter;
    });

}