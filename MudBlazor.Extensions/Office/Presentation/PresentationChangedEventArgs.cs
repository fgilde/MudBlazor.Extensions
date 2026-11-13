namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>What changed in a presentation.</summary>
[Flags]
public enum PresentationChangeKind
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>Shapes or text of the slides changed.</summary>
    Content = 1,

    /// <summary>Slides were added.</summary>
    SlidesAdded = 2,

    /// <summary>Slides were removed.</summary>
    SlidesRemoved = 4,

    /// <summary>The slide order or hidden state changed.</summary>
    SlideOrder = 8,

    /// <summary>Speaker notes changed.</summary>
    Notes = 16,

    /// <summary>Sections were added, removed, renamed or moved.</summary>
    Sections = 32
}

/// <summary>Describes everything that changed during one command or transaction.</summary>
public sealed class PresentationChangedEventArgs : EventArgs
{
    internal PresentationChangedEventArgs(IReadOnlyCollection<Slide> slides, PresentationChangeKind kind)
    {
        Slides = slides;
        Kind = kind;
    }

    /// <summary>The affected slides, including removed ones.</summary>
    public IReadOnlyCollection<Slide> Slides { get; }

    /// <summary>The kinds of change.</summary>
    public PresentationChangeKind Kind { get; }
}
