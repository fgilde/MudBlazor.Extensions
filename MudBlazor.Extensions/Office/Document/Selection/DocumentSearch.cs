using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>Options of <see cref="DocumentSearch"/>.</summary>
/// <param name="MatchCase">True to distinguish upper and lower case.</param>
/// <param name="WholeWord">True to match only where neither neighbour is a letter, digit or underscore.</param>
/// <param name="Regex">True if the query is a regular expression; replacements may then refer to groups, for example <c>$1</c>.</param>
/// <param name="IncludeHeadersFooters">True to search the headers and footers after the body.</param>
public sealed record FindOptions(bool MatchCase = false, bool WholeWord = false, bool Regex = false, bool IncludeHeadersFooters = true);

/// <summary>The matches of a search.</summary>
/// <param name="Ranges">The ranges of the matches in document order.</param>
/// <param name="Truncated">True if the search deadline or the timeout of a regular expression ended the search early; <paramref name="Ranges"/> then
/// holds only the matches found until then.</param>
public sealed record FindResult(IReadOnlyList<DocumentRange> Ranges, bool Truncated);

/// <summary>The next match of a search.</summary>
/// <param name="Match">The range of the match, or null if none was found.</param>
/// <param name="Truncated">True if the search deadline or the timeout of a regular expression ended the search early; <paramref name="Match"/> may then
/// not be the next match.</param>
public sealed record FindNextResult(DocumentRange? Match, bool Truncated);

/// <summary>
/// Finds and replaces text in a <see cref="TextDocument"/>. Matches run over <see cref="Paragraph.Text"/> and never cross paragraphs or tokens such as
/// images; a match that would end inside a surrogate pair is widened to the whole character. Regular expressions run with a one second timeout per
/// match and the whole search with a five second deadline; either ends the search with the matches found so far, which
/// <see cref="Find(TextDocument, string, FindOptions)"/> and <see cref="FindNextWithStatus(TextDocument, DocumentPosition, string, FindOptions)"/>
/// report as truncated and <see cref="ReplaceAll(TextDocument, string, string, FindOptions)"/> rejects.
/// </summary>
public static class DocumentSearch
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SearchDeadline = TimeSpan.FromSeconds(5);
    private static readonly Regex Substitution = new(@"\$(?:[$`'_+&]|\{([^}]*)\}|(\d+))", RegexOptions.CultureInvariant, MatchTimeout);

    /// <summary>Finds all matches in document order; a search cut short returns the matches found until then (see <see cref="Find(TextDocument, string, FindOptions)"/>).</summary>
    /// <param name="document">The document.</param>
    /// <param name="query">The text or regular expression; null or empty finds nothing.</param>
    /// <param name="options">The options; null for the defaults.</param>
    /// <returns>The ranges of the matches.</returns>
    /// <exception cref="ArgumentException">The query is not a valid regular expression.</exception>
    public static IReadOnlyList<DocumentRange> FindAll(TextDocument document, string query, FindOptions options = null) => Find(document, query, options).Ranges;

    /// <summary>Finds all matches in document order and tells whether the search ended early.</summary>
    /// <param name="document">The document.</param>
    /// <param name="query">The text or regular expression; null or empty finds nothing.</param>
    /// <param name="options">The options; null for the defaults.</param>
    /// <returns>The matches, and whether the deadline or a regular expression timeout cut the search short.</returns>
    /// <exception cref="ArgumentException">The query is not a valid regular expression.</exception>
    public static FindResult Find(TextDocument document, string query, FindOptions options = null) => Find(document, query, options, MatchTimeout, SearchDeadline);

    internal static FindResult Find(TextDocument document, string query, FindOptions options, TimeSpan timeout, TimeSpan deadline)
    {
        var matches = Matches(document, query, options, timeout, deadline, out var truncated);
        return new FindResult(matches.Select(m => m.Range).ToList(), truncated);
    }

    /// <summary>Finds the first match that starts at or after a position, wrapping around to the start of the document.</summary>
    /// <param name="document">The document.</param>
    /// <param name="from">The position to search from, usually the end of the selection.</param>
    /// <param name="query">The text or regular expression.</param>
    /// <param name="options">The options; null for the defaults.</param>
    /// <returns>The range of the match, or null if there is none.</returns>
    /// <exception cref="ArgumentException">The query is not a valid regular expression.</exception>
    public static DocumentRange? FindNext(TextDocument document, DocumentPosition from, string query, FindOptions options = null) =>
        FindNextWithStatus(document, from, query, options).Match;

    /// <summary>Finds the first match that starts at or after a position, wrapping around, and tells whether the search ended early.</summary>
    /// <param name="document">The document.</param>
    /// <param name="from">The position to search from, usually the end of the selection.</param>
    /// <param name="query">The text or regular expression.</param>
    /// <param name="options">The options; null for the defaults.</param>
    /// <returns>The match, or null if none was found, and whether the deadline or a regular expression timeout cut the search short.</returns>
    /// <exception cref="ArgumentException">The query is not a valid regular expression.</exception>
    public static FindNextResult FindNextWithStatus(TextDocument document, DocumentPosition from, string query, FindOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(from);
        var (all, truncated) = Find(document, query, options);
        return new FindNextResult(all.Count == 0 ? null : all.FirstOrDefault(r => document.Compare(r.Start, from) >= 0, all[0]), truncated);
    }

    /// <summary>
    /// Replaces all matches as one undo step, back to front. Each replacement takes the format of the first matched character. The replacement is
    /// all or nothing: when the search deadline or a regular expression timeout cuts the search short, nothing is changed and no undo step is recorded.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="query">The text or regular expression.</param>
    /// <param name="replacement">The replacement; with <see cref="FindOptions.Regex"/> it may refer to groups, for example <c>$1</c>.</param>
    /// <param name="options">The options; null for the defaults.</param>
    /// <returns>The number of replaced matches.</returns>
    /// <exception cref="ArgumentException">The query is not a valid regular expression.</exception>
    /// <exception cref="TimeoutException">The search was cut short; the document is unchanged.</exception>
    public static int ReplaceAll(TextDocument document, string query, string replacement, FindOptions options = null) =>
        ReplaceAll(document, query, replacement, options, MatchTimeout, SearchDeadline);

    internal static int ReplaceAll(TextDocument document, string query, string replacement, FindOptions options, TimeSpan timeout, TimeSpan deadline)
    {
        options ??= new FindOptions();
        var matches = Matches(document, query, options, timeout, deadline, out var truncated);
        if (truncated) throw new TimeoutException("The search did not finish in time; nothing was replaced.");
        if (matches.Count == 0) return 0;
        using (document.Undo.BeginTransaction("Replace all"))
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var (range, match, paragraph) = matches[i];
                var text = options.Regex ? Result(match, replacement ?? "", paragraph.Text) : replacement;
                document.Execute(new ReplaceTextCommand(range, text, InlineEdit.At(paragraph, range.Start.Offset)?.Format));
            }

        return matches.Count;
    }

    internal static string ReplacementFor(TextDocument document, DocumentRange match, string query, string replacement, FindOptions options)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= new FindOptions();
        if (string.IsNullOrEmpty(query) || match.Start == null || match.End == null || match.Start.BlockId != match.End.BlockId) return null;
        Paragraph paragraph;
        try { paragraph = document.Resolve(match.Start with { Offset = 0 }).Paragraph; }
        catch (ArgumentException) { return null; }
        var text = paragraph.Text;
        var (from, to) = (match.Start.Offset, match.End.Offset);
        if (from < 0 || to < from || to > text.Length) return null;
        var segmentStart = from == 0 ? 0 : text.LastIndexOf('\uFFFC', from - 1) + 1;
        var stop = text.IndexOf('\uFFFC', from);
        var regex = Build(query, options, MatchTimeout);
        try
        {
            for (var found = regex.Match(text, segmentStart, (stop < 0 ? text.Length : stop) - segmentStart); found.Success && found.Index <= from; found = found.NextMatch())
                if (found.Index == from && found.Length == to - from && found.Length > 0)
                    return options.Regex ? Result(found, replacement ?? "", text) : replacement;
        }
        catch (RegexMatchTimeoutException)
        {
        }

        return null;
    }

    private static Regex Build(string query, FindOptions options, TimeSpan timeout)
    {
        var pattern = options.Regex ? query : Regex.Escape(query);
        if (options.WholeWord) pattern = $@"(?<!\w)(?:{pattern})(?!\w)";
        return new Regex(pattern, (options.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase) | RegexOptions.CultureInvariant, timeout);
    }

    private static string Result(Match match, string replacement, string text)
    {
        if (match.Groups.Values.All(g => !g.Success || Widen(g, text) == (g.Index, g.Index + g.Length))) return match.Result(replacement);
        return Substitution.Replace(replacement, token => token.Value switch
        {
            "$$" => "$",
            "$`" => text[..Widen(match, text).From],
            "$'" => text[Widen(match, text).To..],
            "$_" => text,
            "$+" => Value(match.Groups.Count > 1 ? match.Groups[^1] : match.Groups[0], text),
            "$&" => Value(match.Groups[0], text),
            _ => Reference(match, token, text)
        });
    }

    private static string Reference(Match match, Match token, string text)
    {
        if (token.Groups[1].Success) return match.Groups.TryGetValue(token.Groups[1].Value, out var named) ? Value(named, text) : token.Value;
        var digits = token.Groups[2].Value;
        for (var length = digits.Length; length > 0; length--)
            if (match.Groups.TryGetValue(digits[..length], out var group))
                return Value(group, text) + digits[length..];
        return token.Value;
    }

    private static string Value(Group group, string text) => group.Success ? text[Widen(group, text).From..Widen(group, text).To] : "";

    private static (int From, int To) Widen(Capture capture, string text) =>
        (TextDocument.SnapOffset(text, capture.Index, true), TextDocument.SnapOffset(text, capture.Index + capture.Length, false));

    private static List<(DocumentRange Range, Match Match, Paragraph Paragraph)> Matches(TextDocument document, string query, FindOptions options, TimeSpan timeout,
        TimeSpan deadline, out bool truncated)
    {
        ArgumentNullException.ThrowIfNull(document);
        truncated = false;
        options ??= new FindOptions();
        var result = new List<(DocumentRange, Match, Paragraph)>();
        if (string.IsNullOrEmpty(query)) return result;
        var regex = Build(query, options, timeout);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            foreach (var paragraph in document.AllParagraphs(options.IncludeHeadersFooters))
            {
                if (watch.Elapsed > deadline)
                {
                    truncated = true;
                    break;
                }

                var text = paragraph.Text;
                var covered = 0;
                for (var start = 0; start <= text.Length;)
                {
                    var stop = text.IndexOf('\uFFFC', start);
                    if (stop < 0) stop = text.Length;
                    for (var match = regex.Match(text, start, stop - start); match.Success; match = match.NextMatch())
                    {
                        var from = TextDocument.SnapOffset(text, match.Index, true);
                        var to = TextDocument.SnapOffset(text, match.Index + match.Length, false);
                        if (match.Length == 0 || from < covered) continue;
                        result.Add((new DocumentRange(document.PositionOf(paragraph.Id, from), document.PositionOf(paragraph.Id, to)), match, paragraph));
                        covered = to;
                    }

                    start = stop + 1;
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            truncated = true;
        }

        return result;
    }
}
