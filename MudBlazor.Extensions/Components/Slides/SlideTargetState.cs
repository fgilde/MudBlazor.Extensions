using System.Globalization;
using MudBlazor.Extensions.Office.Presentation;
using AnimationDirection = MudBlazor.Extensions.Office.Presentation.AnimationDirection;
using AnimationPreset = MudBlazor.Extensions.Office.Presentation.AnimationPreset;

namespace MudBlazor.Extensions.Components;

/// <summary>How a shape or paragraph appears at one point of the slide show.</summary>
/// <param name="Hidden">True if the target is not visible once the current build step has played.</param>
/// <param name="Effect">The effect the target plays in the current build step, or null.</param>
/// <param name="StartSeconds">When the effect starts, in seconds after the step began.</param>
public sealed record SlideTargetState(bool Hidden, AnimationEffect Effect = null, double StartSeconds = 0)
{
    internal string CssClass => Effect == null ? null : $"mud-ex-show-anim mud-ex-show-{(Exit ? "exit" : "entr")}-{Name}";

    internal string CssStyle =>
        (Hidden ? "visibility:hidden;" : "") +
        (Effect == null ? "" : $"animation-delay:{Seconds(StartSeconds)}s;animation-duration:{Seconds(Effect.DurationSeconds)}s;");

    private bool Exit => Effect.Class == AnimationClass.Exit && Effect.Preset != AnimationPreset.Other;

    private string Name => Effect.Preset switch
    {
        AnimationPreset.Fade => "fade",
        AnimationPreset.FlyIn => "fly-" + Side,
        AnimationPreset.Wipe => "wipe-" + Side,
        AnimationPreset.Zoom => "zoom",
        _ => "appear"
    };

    private string Side => Effect.Direction switch
    {
        AnimationDirection.FromLeft => "left",
        AnimationDirection.FromRight => "right",
        AnimationDirection.FromTop => "top",
        _ => "bottom"
    };

    private static string Seconds(double value) => Math.Round(Math.Max(0, value), 3).ToString(CultureInfo.InvariantCulture);
}
