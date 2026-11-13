namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>The effect of a slide transition.</summary>
public enum TransitionKind
{
    /// <summary>No effect.</summary>
    None,

    /// <summary>The slide replaces the previous one at once.</summary>
    Cut,

    /// <summary>The slide fades in.</summary>
    Fade,

    /// <summary>The slide pushes the previous one out.</summary>
    Push,

    /// <summary>An edge moves across and reveals the slide.</summary>
    Wipe,

    /// <summary>The slide is revealed from the middle or the edges.</summary>
    Split,

    /// <summary>The slide moves in over the previous one.</summary>
    Cover,

    /// <summary>The previous slide moves away and uncovers the slide.</summary>
    Uncover,

    /// <summary>The slide zooms in or out.</summary>
    Zoom,

    /// <summary>The slide dissolves in.</summary>
    Dissolve,

    /// <summary>The slide appears in random bars.</summary>
    RandomBars,

    /// <summary>An effect the editor keeps but does not model; the slide show plays it as a fade.</summary>
    Other
}

/// <summary>The direction of a slide transition.</summary>
public enum TransitionDirection
{
    /// <summary>The default direction of the effect.</summary>
    None,

    /// <summary>To the left (push, wipe, cover, uncover).</summary>
    Left,

    /// <summary>To the right (push, wipe, cover, uncover).</summary>
    Right,

    /// <summary>Upwards (push, wipe, cover, uncover).</summary>
    Up,

    /// <summary>Downwards (push, wipe, cover, uncover).</summary>
    Down,

    /// <summary>Inwards (zoom).</summary>
    In,

    /// <summary>Outwards (zoom).</summary>
    Out,

    /// <summary>Horizontal (split, random bars).</summary>
    Horizontal,

    /// <summary>Vertical (split, random bars).</summary>
    Vertical
}

/// <summary>The transition that brings a slide on screen in the slide show, and how the show advances from the slide.</summary>
/// <param name="Kind">The effect.</param>
/// <param name="Direction">The direction of the effect, as it is written in the file; <see cref="TransitionDirection.None"/> for effects without one.</param>
/// <param name="DurationSeconds">The duration of the effect in seconds. A changed transition is saved with the nearest of 0.5, 0.75 and 1 seconds.</param>
/// <param name="AdvanceOnClick">True if a click advances to the next slide.</param>
/// <param name="AdvanceAfterSeconds">The time after which the show advances by itself, or null.</param>
public sealed record SlideTransition(TransitionKind Kind, TransitionDirection Direction, double DurationSeconds, bool AdvanceOnClick, double? AdvanceAfterSeconds)
{
    /// <summary>No transition; a click advances.</summary>
    public static SlideTransition None { get; } = new(TransitionKind.None, TransitionDirection.None, 0.5, true, null);
}
