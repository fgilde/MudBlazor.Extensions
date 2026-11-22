using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>Options of <see cref="DocumentSearch"/>.</summary>
/// <param name="MatchCase">True to distinguish upper and lower case.</param>
/// <param name="WholeWord">True to match only where neither neighbour is a letter, digit or underscore.</param>
/// <param name="Regex">True if the query is a regular expression; replacements may then refer to groups, for example <c>$1</c>.</param>
/// <param name="IncludeHeadersFooters">True to search the headers and footers after the body.</param>
public sealed record FindOptions(bool MatchCase = false, bool WholeWord = false, bool Regex = false, bool IncludeHeadersFooters = true);

/// <summary>
/// Finds and replaces text in a <see cref="TextDocument"/>. Matches run over <see cref="Paragraph.Text"/> and never cross paragraphs or tokens such as
/// images. Regular expressions run with a one second timeout; a timeout ends the search with the matches found so far.
/// </summary>
public static class DocumentSearch
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Finds all matches in document order.</summary>
    /// <param name="document">The document.</param>
    /// <param name="query">The text or regular expression; null or empty finds nothing.</param>
    /// <param name="options">The options; null for the defaults.</param>
    /// <returns>The ranges of the matches.</returns>
    /// <exception cref="ArgumentException">The query is not a valid regular expression.</exception>
    public static IReadOnlyList<DocumentRange> FindAll(TextDocument document, string query, FindOptions options = null) =>
        Matches(document, query, options).Select(m => m.Range).ToList();

    /// <summary>Finds the first match that starts at or after a position, wrapping around to the start of the document.</summary>
    /// <param name="document">The document.</param>
    /// <param name="from">The position to search from, usually the end of the selection.</param>
    /// <param name="query">The text or regular expression.</param>
    /// <param name="options">The options; null for the defaults.</param>
    /// <returns>The range of the match, or null if there is none.</returns>
    /// <exception cref="ArgumentException">The query is not a valid regular expression.</exception>
    public static DocumentRange? FindNext(TextDocument document, DocumentPosition from, string query, FindOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(from);
        var all = FindAll(document, query, options);
        return all.Count == 0 ? null : all.FirstOrDefault(r => document.Compare(r.Start, from) >= 0, all[0]);
    }

    /// <summary>
    /// Replaces all matches as one undo step, back to front. Each replacement takes the format of the first matched character.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="query">The text or regular expression.</param>
    /// <param name="replacement">The replacement; with <see cref="FindOptions.Regex"/> it may refer to groups, for example <c>$1</c>.</param>
    /// <param name="options">The options; null for the defaults.</param>
    /// <returns>The number of replaced matches.</returns>
    /// <exception cref="ArgumentException">The query is not a valid regular expression.</exception>
    public static int ReplaceAll(TextDocument document, string query, string replacement, FindOptions options = null)
    {
        options ??= new FindOptions();
        var matches = Matches(document, query, options);
        if (matches.Count == 0) return 0;
        using (document.Undo.BeginTransaction("Replace all"))
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var (range, match, paragraph) = matches[i];
                var text = options.Regex ? match.Result(replacement ?? "") : replacement;
                document.Execute(new ReplaceTextCommand(range, text, InlineEdit.At(paragraph, range.Start.Offset)?.Format));
            }

        return matches.Count;
    }

    private static List<(DocumentRange Range, Match Match, Paragraph Paragraph)> Matches(TextDocument document, string query, FindOptions options)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= new FindOptions();
        var result = new List<(DocumentRange, Match, Paragraph)>();
        if (string.IsNullOrEmpty(query)) return result;
        var pattern = options.Regex ? query : Regex.Escape(query);
        if (options.WholeWord) pattern = $@"(?<!\w)(?:{pattern})(?!\w)";
        var regex = new Regex(pattern, (options.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase) | RegexOptions.CultureInvariant, MatchTimeout);
        try
        {
            foreach (var paragraph in document.AllParagraphs(options.IncludeHeadersFooters))
            {
                var text = paragraph.Text;
                for (var start = 0; start <= text.Length;)
                {
                    var stop = text.IndexOf('￼', start);
                    if (stop < 0) stop = text.Length;
                    for (var match = regex.Match(text, start, stop - start); match.Success; match = match.NextMatch())
                        if (match.Length > 0)
                            result.Add((new DocumentRange(document.PositionOf(paragraph.Id, match.Index), document.PositionOf(paragraph.Id, match.Index + match.Length)), match, paragraph));
                    start = stop + 1;
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
        }

        return result;
    }
}
