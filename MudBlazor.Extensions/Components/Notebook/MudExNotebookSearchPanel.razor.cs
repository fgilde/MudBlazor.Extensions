using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The search pane of <see cref="MudExNotebookEditor"/>: finds text in page titles, text blocks and table cells of the current page or the whole
/// notebook and lists the hits by section and page.
/// </summary>
public partial class MudExNotebookSearchPanel
{
    private const int SnippetContext = 40;

    private IReadOnlyList<NotebookSearchResult> _results = [];
    private (Notebook, NotebookPage, string, int)? _searched;

    /// <summary>The notebook to search.</summary>
    [Parameter] public Notebook Notebook { get; set; }

    /// <summary>The page searched unless <see cref="WholeNotebook"/> is set.</summary>
    [Parameter] public NotebookPage Page { get; set; }

    /// <summary>If true every page of the notebook is searched.</summary>
    [Parameter] public bool WholeNotebook { get; set; }

    /// <summary>Raised when the user switches between the page and the notebook.</summary>
    [Parameter] public EventCallback<bool> WholeNotebookChanged { get; set; }

    /// <summary>The searched text; case is ignored.</summary>
    [Parameter] public string Query { get; set; }

    /// <summary>Raised when the user changes the searched text.</summary>
    [Parameter] public EventCallback<string> QueryChanged { get; set; }

    /// <summary>Raised when the user picks a hit.</summary>
    [Parameter] public EventCallback<NotebookSearchResult> ResultSelected { get; set; }

    /// <summary>Raised when the user closes the pane.</summary>
    [Parameter] public EventCallback Closed { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateResults();
    }

    private void UpdateResults()
    {
        var key = (Notebook, WholeNotebook ? null : Page, Query, Notebook?.Revision ?? 0);
        if (key == _searched)
            return;
        _searched = key;
        _results = Search(key.Item1, key.Item2, key.Query);
    }

    private async Task SetQueryAsync(string query)
    {
        Query = query;
        UpdateResults();
        await QueryChanged.InvokeAsync(query);
    }

    private async Task SetScopeAsync(bool wholeNotebook)
    {
        WholeNotebook = wholeNotebook;
        UpdateResults();
        await WholeNotebookChanged.InvokeAsync(wholeNotebook);
    }

    internal static IReadOnlyList<NotebookSearchResult> Search(Notebook notebook, NotebookPage page, string query)
    {
        var term = query?.Trim();
        if (notebook == null || string.IsNullOrEmpty(term))
            return [];
        var results = new List<NotebookSearchResult>();
        foreach (var section in notebook.Sections)
            foreach (var candidate in section.Pages.Where(p => page == null || ReferenceEquals(p, page)))
            {
                if (Snippet(candidate.Title, term) is { } title)
                    results.Add(new NotebookSearchResult(section, candidate, null, title));
                foreach (var element in candidate.Elements)
                    if (TextsOf(element).Select(t => Snippet(t, term)).FirstOrDefault(s => s != null) is { } hit)
                        results.Add(new NotebookSearchResult(section, candidate, element.Id, hit));
            }

        return results;
    }

    private static IEnumerable<string> TextsOf(NoteElement element) => (element switch
    {
        TextElement text => text.Paragraphs,
        TableElement table => table.Rows.SelectMany(r => r).SelectMany(c => c.Paragraphs),
        _ => []
    }).Select(p => string.Concat(p.Runs.Select(r => r.Text)));

    private static string Snippet(string text, string term)
    {
        var at = text?.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) ?? -1;
        if (at < 0)
            return null;
        var start = Math.Max(0, at - SnippetContext);
        var end = Math.Min(text.Length, at + term.Length + SnippetContext);
        return (start > 0 ? "…" : "") + text[start..end].ReplaceLineEndings(" ") + (end < text.Length ? "…" : "");
    }
}

/// <summary>A hit of <see cref="MudExNotebookSearchPanel"/>.</summary>
/// <param name="Section">The section of the page.</param>
/// <param name="Page">The page.</param>
/// <param name="ElementId">The id of the text block or table, or null if the page title matched.</param>
/// <param name="Text">The matched text with some context.</param>
public sealed record NotebookSearchResult(NotebookSection Section, NotebookPage Page, string ElementId, string Text);
