using MudBlazor.Extensions.Office.Presentation;
using AnimationDirection = MudBlazor.Extensions.Office.Presentation.AnimationDirection;
using AnimationPreset = MudBlazor.Extensions.Office.Presentation.AnimationPreset;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideEditor
{
    private static readonly TransitionKind[] TransitionKinds =
    [
        TransitionKind.None, TransitionKind.Cut, TransitionKind.Fade, TransitionKind.Push, TransitionKind.Wipe, TransitionKind.Split,
        TransitionKind.Cover, TransitionKind.Uncover, TransitionKind.Zoom, TransitionKind.Dissolve, TransitionKind.RandomBars
    ];

    private static readonly AnimationChoice[] AnimationChoices =
        new[] { AnimationClass.Entrance, AnimationClass.Exit }
            .SelectMany(c => new[] { AnimationPreset.Appear, AnimationPreset.Fade, AnimationPreset.FlyIn, AnimationPreset.Wipe, AnimationPreset.Zoom }
                .Select(p => new AnimationChoice(c, p)))
            .ToArray();

    private static readonly AnimationDirection[] AnimationDirections =
        [AnimationDirection.FromBottom, AnimationDirection.FromLeft, AnimationDirection.FromRight, AnimationDirection.FromTop];

    private static readonly AnimationTrigger[] Triggers = [AnimationTrigger.OnClick, AnimationTrigger.WithPrevious, AnimationTrigger.AfterPrevious];

    private MudExRibbon _ribbon;
    private MudExRibbonTab _animationsTab;
    private int _tabIndex;
    private bool _animationPaneVisible;
    private Slide _effectSlide;
    private int? _effectIndex;

    private sealed record AnimationChoice(AnimationClass Class, AnimationPreset Preset);

    private SlideTransition CurrentTransition => _current?.Transition ?? SlideTransition.None;

    private TransitionDirection[] TransitionDirections => TransitionXml.Directions(CurrentTransition.Kind);

    private bool ShowAnimationOrder => !IsReadOnly && (_animationPaneVisible || (_animationsTab != null && _ribbon?.Tabs.ElementAtOrDefault(_tabIndex) == _animationsTab));

    private int? SelectedEffectIndex =>
        ReferenceEquals(_effectSlide, _current) && _effectIndex is { } index && index < (_current?.Animations.Count ?? 0) ? index : null;

    private IReadOnlyList<int> SelectedEffects
    {
        get
        {
            if (_current == null)
                return [];
            var effects = _current.Animations;
            if (SelectedEffectIndex is { } index && (SelectedShapes.Count == 0 || SelectedShapes.Contains(effects[index].ShapeId)))
                return [index];
            return Enumerable.Range(0, effects.Count).Where(i => SelectedShapes.Contains(effects[i].ShapeId)).ToList();
        }
    }

    private AnimationEffect SelectedEffect => SelectedEffects is [var first, ..] ? _current.Animations[first] : null;

    private AnimationChoice CurrentAnimation =>
        SelectedEffect is { Class: AnimationClass.Entrance or AnimationClass.Exit, Preset: not AnimationPreset.Other } effect ? new AnimationChoice(effect.Class, effect.Preset) : null;

    private bool CanAnimate => HasShapeSelection && SelectedShapeList.Any(s => s.Parent == null);

    private bool HasDirectionalEffect => SelectedEffect is { Preset: AnimationPreset.FlyIn or AnimationPreset.Wipe, ParagraphIndex: null };

    private static string TriggerName(AnimationTrigger trigger) => trigger switch
    {
        AnimationTrigger.OnClick => "On Click",
        AnimationTrigger.WithPrevious => "With Previous",
        _ => "After Previous"
    };

    private static AnimationEffect WithDuration(AnimationEffect effect, double seconds) =>
        effect.Preset == AnimationPreset.Other || effect.ParagraphIndex != null ? effect : effect with { DurationSeconds = seconds };

    internal static string TransitionName(TransitionKind kind) => kind switch
    {
        TransitionKind.RandomBars => "Random Bars",
        _ => kind.ToString()
    };

    internal static string AnimationName(AnimationClass @class, AnimationPreset preset) => (@class, preset) switch
    {
        (_, AnimationPreset.Other) => "Custom",
        (AnimationClass.Exit, AnimationPreset.Appear) => "Disappear",
        (AnimationClass.Exit, AnimationPreset.FlyIn) => "Fly Out",
        (AnimationClass.Exit, _) => $"{preset} Out",
        (_, AnimationPreset.FlyIn) => "Fly In",
        _ => preset.ToString()
    };

    private static string DirectionName(AnimationDirection direction) => direction switch
    {
        AnimationDirection.FromLeft => "From Left",
        AnimationDirection.FromRight => "From Right",
        AnimationDirection.FromTop => "From Top",
        _ => "From Bottom"
    };

    private static string DirectionId(AnimationDirection direction) => DirectionName(direction).Replace(' ', '-').ToLowerInvariant();

    private Task SetTransitionAsync(SlideTransition transition) =>
        _selection.Count == 0 || transition == CurrentTransition ? Task.CompletedTask : RunCommandAsync(new SetTransitionCommand(_selection, transition));

    private Task SetTransitionKindAsync(TransitionKind kind) =>
        SetTransitionAsync(CurrentTransition with { Kind = kind, Direction = TransitionDirection.None });

    private Task ApplyTransitionToAllAsync() =>
        _current == null ? Task.CompletedTask : RunCommandAsync(new SetTransitionCommand(_presentation.Slides, CurrentTransition));

    private Task ApplyAnimationAsync(AnimationChoice choice) => RunAsync(() =>
    {
        var slide = _current;
        var shapes = SelectedShapeList.Where(s => s.Parent == null).ToList();
        if (slide == null || choice == null || shapes.Count == 0)
            return;
        var direction = choice.Preset is AnimationPreset.FlyIn or AnimationPreset.Wipe ? AnimationDirection.FromBottom : AnimationDirection.None;
        using (_presentation.Undo.BeginTransaction("Add animation"))
            foreach (var shape in shapes)
            {
                var effects = slide.Animations;
                var index = effects.Select((e, i) => (e, i)).FirstOrDefault(x => x.e.ShapeId == shape.Id && x.e.ParagraphIndex == null, (null, -1)).i;
                if (index < 0)
                {
                    _presentation.Execute(new AddAnimationCommand(slide,
                        new AnimationEffect(shape.Id, choice.Class, choice.Preset, AnimationTrigger.OnClick, 0, choice.Preset == AnimationPreset.Appear ? 0 : 0.5, direction, null)));
                    continue;
                }

                var effect = effects[index];
                var keep = direction != AnimationDirection.None && effect.Direction != AnimationDirection.None;
                var duration = effect.Preset == AnimationPreset.Appear && choice.Preset != AnimationPreset.Appear ? 0.5 : effect.DurationSeconds;
                _presentation.Execute(new SetAnimationCommand(slide, index,
                    effect with { Class = choice.Class, Preset = choice.Preset, Direction = keep ? effect.Direction : direction, DurationSeconds = duration }));
            }
    });

    private Task ChangeEffectsAsync(Func<AnimationEffect, AnimationEffect> change) => RunAsync(() =>
    {
        var slide = _current;
        if (slide == null)
            return;
        using (_presentation.Undo.BeginTransaction("Change animation"))
            foreach (var index in SelectedEffects)
                if (change(slide.Animations[index]) is { } changed && changed != slide.Animations[index])
                    _presentation.Execute(new SetAnimationCommand(slide, index, changed));
    });

    private Task SetEffectDirectionAsync(AnimationDirection direction) =>
        ChangeEffectsAsync(e => e.Preset is AnimationPreset.FlyIn or AnimationPreset.Wipe && e.ParagraphIndex == null ? e with { Direction = direction } : e);

    private async Task SelectEffectAsync(int index)
    {
        (_effectSlide, _effectIndex) = (_current, index);
        if (_current?.Animations.ElementAtOrDefault(index) is { } effect && _canvas != null && SlideShape.Flatten(_current.Shapes).Any(s => s.Id == effect.ShapeId))
            await _canvas.SelectAsync([effect.ShapeId]);
    }
}
