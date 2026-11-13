namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>
/// Sets the transition of slides. A direction of <see cref="TransitionDirection.None"/> means the default direction of the effect.
/// <see cref="TransitionKind.Other"/> can only be kept on slides that already have such a transition.
/// </summary>
/// <param name="slides">The slides.</param>
/// <param name="transition">The transition.</param>
public sealed class SetTransitionCommand(IReadOnlyList<Slide> slides, SlideTransition transition) : IDocumentCommand<Presentation>
{
    private List<(Slide Slide, SlideTransition Before)> _before;

    /// <inheritdoc />
    public string Description => "Transition";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        ArgumentNullException.ThrowIfNull(transition);
        var value = Normalize(transition);
        var ordered = SlideEdit.Ordered(document, slides);
        if (value.Kind == TransitionKind.Other && ordered.Any(s => s.Transition.Kind != TransitionKind.Other))
            throw new ArgumentException("A transition the editor does not model cannot be set.", nameof(transition));
        _before = ordered.Where(s => s.Transition != value).Select(s => (s, s.Transition)).ToList();
        foreach (var (slide, _) in _before)
        {
            slide.Transition = value;
            document.Notify(slide, PresentationChangeKind.Content);
        }
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        foreach (var (slide, before) in _before)
        {
            slide.Transition = before;
            document.Notify(slide, PresentationChangeKind.Content);
        }
    }

    private static SlideTransition Normalize(SlideTransition transition)
    {
        var directions = TransitionXml.Directions(transition.Kind);
        if (!Enum.IsDefined(transition.Kind) || transition.Direction != TransitionDirection.None && !directions.Contains(transition.Direction))
            throw new ArgumentException("The direction does not fit the transition.", nameof(transition));
        if (!AnimationEdit.IsTime(transition.DurationSeconds) || transition.AdvanceAfterSeconds is { } after && !AnimationEdit.IsTime(after))
            throw new ArgumentException("Durations must be finite and not negative.", nameof(transition));
        return transition.Direction == TransitionDirection.None && directions.Length > 0 ? transition with { Direction = directions[0] } : transition;
    }
}

/// <summary>
/// Adds an effect to the main animation sequence of a slide. Only entrance and exit effects with the presets Appear, Fade, FlyIn, Wipe and Zoom on a
/// whole top-level shape can be added; fly-in and wipe need a direction, the others none.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="effect">The effect.</param>
/// <param name="index">The position in <see cref="Slide.Animations"/>, or null to append.</param>
public sealed class AddAnimationCommand(Slide slide, AnimationEffect effect, int? index = null) : IDocumentCommand<Presentation>
{
    private readonly AnimationEdit _edit = new();

    /// <inheritdoc />
    public string Description => "Add animation";

    /// <inheritdoc />
    public void Apply(Presentation document) => _edit.Apply(document, slide, (list, live) =>
    {
        AnimationEdit.EnsureCreatable(slide, effect);
        var at = index ?? live.Count;
        if (at < 0 || at > live.Count) throw new ArgumentOutOfRangeException(nameof(index));
        list.Insert(at < live.Count ? live[at] : list.Count, effect);
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _edit.Revert(document, slide);
}

/// <summary>Removes an effect from the main animation sequence of a slide.</summary>
/// <param name="slide">The slide.</param>
/// <param name="index">The position in <see cref="Slide.Animations"/>.</param>
public sealed class RemoveAnimationCommand(Slide slide, int index) : IDocumentCommand<Presentation>
{
    private readonly AnimationEdit _edit = new();

    /// <inheritdoc />
    public string Description => "Remove animation";

    /// <inheritdoc />
    public void Apply(Presentation document) => _edit.Apply(document, slide, (list, live) => list.RemoveAt(AnimationEdit.At(live, index, nameof(index))));

    /// <inheritdoc />
    public void Revert(Presentation document) => _edit.Revert(document, slide);
}

/// <summary>Moves an effect within the main animation sequence of a slide.</summary>
/// <param name="slide">The slide.</param>
/// <param name="index">The current position in <see cref="Slide.Animations"/>.</param>
/// <param name="targetIndex">The position in <see cref="Slide.Animations"/> after the move.</param>
public sealed class MoveAnimationCommand(Slide slide, int index, int targetIndex) : IDocumentCommand<Presentation>
{
    private readonly AnimationEdit _edit = new();

    /// <inheritdoc />
    public string Description => "Move animation";

    /// <inheritdoc />
    public void Apply(Presentation document) => _edit.Apply(document, slide, (list, live) =>
    {
        var from = AnimationEdit.At(live, index, nameof(index));
        AnimationEdit.At(live, targetIndex, nameof(targetIndex));
        var effect = list[from];
        list.RemoveAt(from);
        var rest = live.Where(i => i != from).Select(i => i > from ? i - 1 : i).ToList();
        list.Insert(targetIndex < rest.Count ? rest[targetIndex] : list.Count, effect);
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _edit.Revert(document, slide);
}

/// <summary>
/// Replaces an effect of the main animation sequence of a slide. Trigger and delay can be changed on every effect; any other change needs an effect that
/// <see cref="AddAnimationCommand"/> accepts.
/// </summary>
/// <param name="slide">The slide.</param>
/// <param name="index">The position in <see cref="Slide.Animations"/>.</param>
/// <param name="effect">The new effect.</param>
public sealed class SetAnimationCommand(Slide slide, int index, AnimationEffect effect) : IDocumentCommand<Presentation>
{
    private readonly AnimationEdit _edit = new();

    /// <inheritdoc />
    public string Description => "Change animation";

    /// <inheritdoc />
    public void Apply(Presentation document) => _edit.Apply(document, slide, (list, live) =>
    {
        var at = AnimationEdit.At(live, index, nameof(index));
        ArgumentNullException.ThrowIfNull(effect);
        var value = effect.Source == null ? effect with { Source = list[at].Source } : effect;
        if (list[at] with { Trigger = value.Trigger, DelaySeconds = value.DelaySeconds } == value) AnimationEdit.EnsureTiming(value);
        else AnimationEdit.EnsureCreatable(slide, value);
        list[at] = value;
    });

    /// <inheritdoc />
    public void Revert(Presentation document) => _edit.Revert(document, slide);
}

internal sealed class AnimationEdit
{
    private static readonly AnimationPreset[] Creatable = [AnimationPreset.Appear, AnimationPreset.Fade, AnimationPreset.FlyIn, AnimationPreset.Wipe, AnimationPreset.Zoom];
    private List<AnimationEffect> _before;
    private List<AnimationEffect> _after;

    internal void Apply(Presentation document, Slide slide, Action<List<AnimationEffect>, List<int>> change)
    {
        if (_after == null)
        {
            SlideEdit.Ordered(document, [slide]);
            var list = slide.AnimationList.ToList();
            change(list, slide.LiveAnimations());
            if (Same(list, slide.AnimationList)) return;
            (_before, _after) = (slide.AnimationList.ToList(), list);
        }

        Set(document, slide, _after);
    }

    internal void Revert(Presentation document, Slide slide)
    {
        if (_before != null) Set(document, slide, _before);
    }

    internal static int At(List<int> live, int index, string name) =>
        index >= 0 && index < live.Count ? live[index] : throw new ArgumentOutOfRangeException(name);

    internal static bool Same(IReadOnlyList<AnimationEffect> a, IReadOnlyList<AnimationEffect> b) =>
        a.Count == b.Count && a.Zip(b).All(p => p.First == p.Second && p.First.Source == p.Second.Source);

    internal static bool IsTime(double seconds) => double.IsFinite(seconds) && seconds >= 0;

    internal static bool IsCreatable(AnimationEffect effect) =>
        effect.Class is AnimationClass.Entrance or AnimationClass.Exit && Creatable.Contains(effect.Preset) && effect.ParagraphIndex == null
        && (effect.Preset is AnimationPreset.FlyIn or AnimationPreset.Wipe
            ? effect.Direction != AnimationDirection.None && Enum.IsDefined(effect.Direction)
            : effect.Direction == AnimationDirection.None);

    internal static void EnsureTiming(AnimationEffect effect)
    {
        if (!Enum.IsDefined(effect.Trigger) || !IsTime(effect.DelaySeconds) || !IsTime(effect.DurationSeconds))
            throw new ArgumentException("The trigger or a time of the effect is invalid.", nameof(effect));
    }

    internal static void EnsureCreatable(Slide slide, AnimationEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        EnsureTiming(effect);
        if (!IsCreatable(effect))
            throw new ArgumentException("Only entrance and exit effects with Appear, Fade, FlyIn, Wipe or Zoom on a whole shape can be created.", nameof(effect));
        if (slide.ShapeList.All(s => s.Id != effect.ShapeId)) throw new ArgumentException("The shape is not a top-level shape of the slide.", nameof(effect));
    }

    private static void Set(Presentation document, Slide slide, List<AnimationEffect> effects)
    {
        slide.AnimationList.Clear();
        slide.AnimationList.AddRange(effects);
        document.Notify(slide, PresentationChangeKind.Content);
    }
}
