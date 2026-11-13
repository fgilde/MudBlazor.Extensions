using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>The class of an animation effect.</summary>
public enum AnimationClass
{
    /// <summary>The target appears.</summary>
    Entrance,

    /// <summary>The target disappears.</summary>
    Exit,

    /// <summary>The target is emphasised and stays visible.</summary>
    Emphasis,

    /// <summary>The target moves along a path.</summary>
    Path,

    /// <summary>Any other effect, for example a media command.</summary>
    Other
}

/// <summary>The preset of an entrance or exit effect.</summary>
public enum AnimationPreset
{
    /// <summary>Appears or disappears at once.</summary>
    Appear,

    /// <summary>Fades in or out.</summary>
    Fade,

    /// <summary>Flies in from or out to an edge of the slide.</summary>
    FlyIn,

    /// <summary>An edge moves across and reveals or hides the target.</summary>
    Wipe,

    /// <summary>Grows from or shrinks to its centre.</summary>
    Zoom,

    /// <summary>An effect the editor keeps but does not model; the slide show plays it as <see cref="Appear"/> and never hides the target with it.</summary>
    Other
}

/// <summary>What starts an animation effect.</summary>
public enum AnimationTrigger
{
    /// <summary>A click starts the effect.</summary>
    OnClick,

    /// <summary>The effect starts together with the previous one.</summary>
    WithPrevious,

    /// <summary>The effect starts when the previous ones have finished.</summary>
    AfterPrevious
}

/// <summary>The direction of a fly-in or wipe effect; for exit effects the edge the target leaves to.</summary>
public enum AnimationDirection
{
    /// <summary>No direction.</summary>
    None,

    /// <summary>From the left.</summary>
    FromLeft,

    /// <summary>From the right.</summary>
    FromRight,

    /// <summary>From the top.</summary>
    FromTop,

    /// <summary>From the bottom.</summary>
    FromBottom
}

/// <summary>An effect of the main animation sequence of a slide.</summary>
/// <param name="ShapeId">The id of the animated shape.</param>
/// <param name="Class">The class of the effect.</param>
/// <param name="Preset">The preset; <see cref="AnimationPreset.Other"/> for every effect that is not a modelled entrance or exit.</param>
/// <param name="Trigger">What starts the effect.</param>
/// <param name="DelaySeconds">The delay after the start in seconds.</param>
/// <param name="DurationSeconds">The duration in seconds.</param>
/// <param name="Direction">The direction of a fly-in or wipe.</param>
/// <param name="ParagraphIndex">The animated paragraph of the shape's text, or null if the whole shape is animated.</param>
public sealed record AnimationEffect(
    uint ShapeId,
    AnimationClass Class,
    AnimationPreset Preset,
    AnimationTrigger Trigger,
    double DelaySeconds,
    double DurationSeconds,
    AnimationDirection Direction,
    int? ParagraphIndex)
{
    internal XElement Source { get; init; }

    /// <summary>Compares the modelled values; the original file content an effect was read from does not take part.</summary>
    /// <param name="other">The other effect.</param>
    /// <returns>True if both effects have the same values.</returns>
    public bool Equals(AnimationEffect other) =>
        other is not null && ShapeId == other.ShapeId && Class == other.Class && Preset == other.Preset && Trigger == other.Trigger && DelaySeconds.Equals(other.DelaySeconds)
        && DurationSeconds.Equals(other.DurationSeconds) && Direction == other.Direction && ParagraphIndex == other.ParagraphIndex;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(ShapeId, Class, Preset, Trigger, DelaySeconds, DurationSeconds, Direction, ParagraphIndex);
}
