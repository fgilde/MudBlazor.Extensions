using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The animation pane of the slide editor: the effects of a slide in sequence order with the click that starts them and the animated shape.
/// Effects are reordered by dragging or with Move Up and Move Down, and removed; every change is one undo step.
/// </summary>
public partial class MudExSlideAnimationPane
{
    private IReadOnlyList<(AnimationEffect Effect, int Step)> _effects = [];
    private int? _drag;

    /// <summary>The presentation the slide belongs to.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The slide whose effects are listed.</summary>
    [Parameter] public Slide Slide { get; set; }

    /// <summary>The index of the selected effect in <see cref="Slide.Animations"/>, or null.</summary>
    [Parameter] public int? SelectedIndex { get; set; }

    /// <summary>Raised with the index of the effect the user selected or moved.</summary>
    [Parameter] public EventCallback<int> SelectedIndexChanged { get; set; }

    /// <summary>Raised with the command for a reorder or removal; the host executes it.</summary>
    [Parameter] public EventCallback<IDocumentCommand<Presentation>> CommandRequested { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Slide == null)
        {
            _effects = [];
            return;
        }

        var steps = SlideBuildSteps.For(Slide);
        var first = steps.Count > 0 && steps[0].Automatic ? 0 : 1;
        _effects = steps.SelectMany((s, i) => s.Effects.Select(e => (e.Effect, i + first))).ToList();
    }

    internal static string IconOf(AnimationEffect effect) => effect.Class switch
    {
        AnimationClass.Entrance => Icons.Material.Filled.Login,
        AnimationClass.Exit => Icons.Material.Filled.Logout,
        _ => Icons.Material.Filled.AutoAwesome
    };

    private string NameOf(AnimationEffect effect)
    {
        var name = SlideShape.Flatten(Slide.Shapes).FirstOrDefault(s => s.Id == effect.ShapeId)?.Name ?? effect.ShapeId.ToString();
        return effect.ParagraphIndex is { } paragraph ? $"{name}: {TryLocalize("Paragraph {0}", paragraph + 1)}" : name;
    }

    private async Task MoveAsync(int index, int target)
    {
        if (Slide == null || index == target || index < 0 || target < 0 || index >= _effects.Count || target >= _effects.Count)
            return;
        await CommandRequested.InvokeAsync(new MoveAnimationCommand(Slide, index, target));
        await SelectedIndexChanged.InvokeAsync(target);
    }

    private Task DropAsync(int target)
    {
        var from = _drag;
        _drag = null;
        return from is { } index ? MoveAsync(index, target) : Task.CompletedTask;
    }

    private Task RemoveAsync() =>
        Slide == null || SelectedIndex is not { } index || index >= _effects.Count ? Task.CompletedTask : CommandRequested.InvokeAsync(new RemoveAnimationCommand(Slide, index));
}
