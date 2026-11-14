namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>An animation effect with its start within its build step.</summary>
/// <param name="Effect">The effect.</param>
/// <param name="StartSeconds">The start in seconds after the step began, including the effect's delay.</param>
public sealed record ScheduledEffect(AnimationEffect Effect, double StartSeconds);

/// <summary>The effects one click of the slide show plays: an on-click effect and the effects that follow it with or after the previous one.</summary>
/// <param name="Automatic">True for a first step that starts without a click, because the slide's first effect starts with or after the previous one.</param>
/// <param name="Effects">The effects in sequence order.</param>
public sealed record BuildStep(bool Automatic, IReadOnlyList<ScheduledEffect> Effects)
{
    /// <summary>The time in seconds until the last effect of the step has finished.</summary>
    public double DurationSeconds => Effects.Select(e => e.StartSeconds + e.Effect.DurationSeconds).DefaultIfEmpty().Max();
}

/// <summary>The build steps of a slide in the slide show and the visibility of its animated shapes and paragraphs.</summary>
public static class SlideBuildSteps
{
    /// <summary>Gets the build steps of a slide.</summary>
    /// <param name="slide">The slide.</param>
    /// <returns>The steps in show order.</returns>
    public static IReadOnlyList<BuildStep> For(Slide slide)
    {
        ArgumentNullException.ThrowIfNull(slide);
        var steps = new List<BuildStep>();
        List<ScheduledEffect> effects = null;
        foreach (var (effect, newStep, _, _, start) in Schedule(slide.Animations))
        {
            if (newStep) steps.Add(new BuildStep(effect.Trigger != AnimationTrigger.OnClick, effects = []));
            effects!.Add(new ScheduledEffect(effect, start));
        }

        return steps;
    }

    /// <summary>
    /// Gets the targets that are hidden when the slide is shown: a shape or paragraph is hidden exactly when its first effect is an entrance with a known preset.
    /// </summary>
    /// <param name="slide">The slide.</param>
    /// <returns>The hidden targets; a null paragraph stands for the whole shape.</returns>
    public static IReadOnlySet<(uint Shape, int? Paragraph)> InitiallyHidden(Slide slide) =>
        VisibleAfter(slide, 0).Where(e => !e.Value).Select(e => e.Key).ToHashSet();

    /// <summary>
    /// Gets the visibility of the animated targets after a number of build steps, for stepping forward, back or jumping. Entrances and every effect with
    /// <see cref="AnimationPreset.Other"/> make the target visible; only exits with a known preset hide it.
    /// </summary>
    /// <param name="slide">The slide.</param>
    /// <param name="stepCount">The number of steps played, including an automatic first step; larger numbers mean all steps.</param>
    /// <returns>For every animated shape or paragraph, true if it is visible. Targets that are not listed are visible.</returns>
    public static IReadOnlyDictionary<(uint Shape, int? Paragraph), bool> VisibleAfter(Slide slide, int stepCount)
    {
        ArgumentNullException.ThrowIfNull(slide);
        ArgumentOutOfRangeException.ThrowIfNegative(stepCount);
        var state = new Dictionary<(uint Shape, int? Paragraph), bool>();
        foreach (var effect in slide.Animations)
            state.TryAdd(Target(effect), !(effect.Class == AnimationClass.Entrance && effect.Preset != AnimationPreset.Other));
        foreach (var effect in For(slide).Take(stepCount).SelectMany(s => s.Effects).Select(e => e.Effect))
            if (effect.Class == AnimationClass.Entrance || effect.Preset == AnimationPreset.Other) state[Target(effect)] = true;
            else if (effect.Class == AnimationClass.Exit) state[Target(effect)] = false;
        return state;
    }

    internal static IEnumerable<(AnimationEffect Effect, bool NewStep, bool NewSlice, double SliceStart, double Start)> Schedule(IEnumerable<AnimationEffect> effects)
    {
        var first = true;
        double sliceStart = 0, end = 0;
        foreach (var effect in effects)
        {
            var newStep = first || effect.Trigger == AnimationTrigger.OnClick;
            var newSlice = newStep || effect.Trigger == AnimationTrigger.AfterPrevious;
            if (newStep) end = 0;
            if (newSlice) sliceStart = end;
            first = false;
            var start = sliceStart + effect.DelaySeconds;
            end = Math.Max(end, start + effect.DurationSeconds);
            yield return (effect, newStep, newSlice, sliceStart, start);
        }
    }

    private static (uint, int?) Target(AnimationEffect effect) => (effect.ShapeId, effect.ParagraphIndex);
}
