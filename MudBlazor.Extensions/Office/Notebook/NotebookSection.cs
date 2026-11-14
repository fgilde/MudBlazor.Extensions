using System.Text.Json;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>A section of a notebook: a coloured tab holding pages.</summary>
public sealed class NotebookSection
{
    internal const string DefaultColor = "#7719AA";

    internal NotebookSection()
    {
    }

    /// <summary>The section id, unique in the notebook.</summary>
    public string Id { get; internal set; } = NoteValues.NewId();

    /// <summary>The section name.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>The tab colour as <c>#RRGGBB</c>.</summary>
    public string Color { get; internal set; } = DefaultColor;

    /// <summary>The pages in order; a section always has at least one.</summary>
    public IReadOnlyList<NotebookPage> Pages => PageList;

    internal List<NotebookPage> PageList { get; } = [];

    internal Dictionary<string, JsonElement> Extra { get; set; }
}
