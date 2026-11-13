using System.Text.Json;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>A page of a notebook section: a free surface of elements that grows with its content.</summary>
public sealed class NotebookPage
{
    internal const int MaxLevel = 2;

    internal NotebookPage()
    {
    }

    /// <summary>The page id, unique in the notebook.</summary>
    public string Id { get; internal set; } = NoteValues.NewId();

    /// <summary>The page title.</summary>
    public string Title { get; internal set; } = "";

    /// <summary>The subpage level, 0 for a page and 1 or 2 for subpages.</summary>
    public int Level { get; internal set; }

    /// <summary>When the page was created.</summary>
    public DateTimeOffset Created { get; internal set; } = DateTimeOffset.UtcNow;

    /// <summary>When the page content was last changed.</summary>
    public DateTimeOffset LastModified { get; internal set; } = DateTimeOffset.UtcNow;

    /// <summary>The elements; the list order is the z-order.</summary>
    public IReadOnlyList<NoteElement> Elements => ElementList;

    /// <summary>True if the page changed since it was loaded.</summary>
    public bool Modified { get; internal set; }

    /// <summary>Incremented by every change of the page; use it as render key.</summary>
    public int Version { get; internal set; }

    /// <summary>Why the page content could not be read, or null. Such a page is empty and read-only; its file is saved unchanged if it could be unpacked, a corrupt file is dropped.</summary>
    public string LoadError { get; internal set; }

    /// <summary>The hull of all elements; empty for a page without elements.</summary>
    public NoteBounds ContentBounds => ElementList.Count == 0 ? default : ElementList.Select(e => e.Bounds).Aggregate((a, b) => a.Union(b));

    internal List<NoteElement> ElementList { get; } = [];

    internal Dictionary<string, JsonElement> Extra { get; set; }

    internal string SourceEntry { get; set; }
}
