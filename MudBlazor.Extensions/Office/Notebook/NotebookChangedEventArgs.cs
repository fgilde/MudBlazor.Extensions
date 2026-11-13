namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>What changed in a notebook.</summary>
[Flags]
public enum NotebookChangeKind
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>Elements or text of pages changed.</summary>
    Content = 1,

    /// <summary>Pages were added, removed, moved, renamed or indented.</summary>
    Pages = 2,

    /// <summary>Sections were added, removed, renamed, recoloured or moved.</summary>
    Sections = 4
}

/// <summary>Describes everything that changed during one command or transaction.</summary>
public sealed class NotebookChangedEventArgs : EventArgs
{
    internal NotebookChangedEventArgs(IReadOnlyCollection<NotebookPage> pages, NotebookChangeKind kind)
    {
        Pages = pages;
        Kind = kind;
    }

    /// <summary>The affected pages, including removed ones.</summary>
    public IReadOnlyCollection<NotebookPage> Pages { get; }

    /// <summary>The kinds of change.</summary>
    public NotebookChangeKind Kind { get; }
}
