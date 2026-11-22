namespace MudBlazor.Extensions.Office.Document;

public sealed partial class TextDocument
{
    /// <summary>Finds the paragraph a position points into.</summary>
    /// <param name="position">The position.</param>
    /// <returns>The paragraph and the offset in its <see cref="Paragraph.Text"/>.</returns>
    /// <exception cref="ArgumentNullException">The position is null.</exception>
    /// <exception cref="ArgumentException">The position does not point into a paragraph of the document.</exception>
    public (Paragraph Paragraph, int Offset) Resolve(DocumentPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        var location = Locate(position.BlockId);
        return Walk(Container(location.Container)[location.Index], position.Path) is Paragraph paragraph && position.Offset >= 0 && position.Offset <= paragraph.Length
            ? (paragraph, position.Offset)
            : throw Invalid(position);
    }

    /// <summary>Gets the position of an offset in a paragraph of the body, of a table cell or of a header or footer.</summary>
    /// <param name="paragraphId">The paragraph id.</param>
    /// <param name="offset">The offset in the paragraph's <see cref="Paragraph.Text"/>.</param>
    /// <returns>The position.</returns>
    /// <exception cref="ArgumentNullException">The paragraph id is null.</exception>
    /// <exception cref="ArgumentException">The document has no paragraph with this id.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The offset lies outside the paragraph.</exception>
    public DocumentPosition PositionOf(string paragraphId, int offset)
    {
        ArgumentNullException.ThrowIfNull(paragraphId);
        if (!TryLocate(paragraphId, out var location) || location.Block is not Paragraph paragraph)
            throw new ArgumentException($"{paragraphId} is not a paragraph of the document.", nameof(paragraphId));
        if (offset < 0 || offset > paragraph.Length) throw new ArgumentOutOfRangeException(nameof(offset), offset, $"The paragraph has {paragraph.Length} characters.");
        return new DocumentPosition(Container(location.Container)[location.Index].Id, Array.AsReadOnly(location.Path), offset);
    }

    /// <summary>Compares two positions in document order: the body first, then the headers and footers.</summary>
    /// <param name="a">The first position.</param>
    /// <param name="b">The second position.</param>
    /// <returns>Less than zero if <paramref name="a"/> comes first, zero if both are equal, greater than zero otherwise.</returns>
    /// <exception cref="ArgumentNullException">A position is null.</exception>
    public int Compare(DocumentPosition a, DocumentPosition b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var la = Locate(a.BlockId);
        var lb = Locate(b.BlockId);
        var result = (la.Container, la.Index).CompareTo((lb.Container, lb.Index));
        for (var i = 0; result == 0 && i < Math.Min(a.Path.Count, b.Path.Count); i++)
            result = (a.Path[i].Row, a.Path[i].Cell, a.Path[i].Block).CompareTo((b.Path[i].Row, b.Path[i].Cell, b.Path[i].Block));
        return result != 0 ? result : (a.Path.Count, a.Offset).CompareTo((b.Path.Count, b.Offset));
    }

    /// <summary>Lists the paragraphs a range touches in document order, depth-first through tables.</summary>
    /// <param name="range">The range, in either direction.</param>
    /// <returns>The paragraphs.</returns>
    /// <exception cref="ArgumentNullException">A position of the range is null.</exception>
    public IEnumerable<Paragraph> ParagraphsIn(DocumentRange range)
    {
        ArgumentNullException.ThrowIfNull(range.Start, nameof(range));
        ArgumentNullException.ThrowIfNull(range.End, nameof(range));
        var (start, end) = range.Normalize(this);
        return ParagraphsBetween(Resolve(start).Paragraph.Id, Resolve(end).Paragraph.Id);
    }

    private IEnumerable<Paragraph> ParagraphsBetween(string first, string last)
    {
        var inRange = false;
        foreach (var paragraph in AllParagraphs(true))
        {
            inRange |= paragraph.Id == first;
            if (!inRange) continue;
            yield return paragraph;
            if (paragraph.Id == last) yield break;
        }
    }

    /// <summary>Lists all paragraphs in document order, depth-first through tables.</summary>
    /// <param name="includeHeadersFooters">True to list the paragraphs of the headers and footers after the body.</param>
    /// <returns>The paragraphs.</returns>
    public IEnumerable<Paragraph> AllParagraphs(bool includeHeadersFooters = false) =>
        ParagraphsOf(includeHeadersFooters ? BlockList.Concat(HeaderFooterList.SelectMany(h => h.Blocks)) : BlockList);

    /// <summary>Gets the text of a range; paragraphs are joined by <c>\n</c>.</summary>
    /// <param name="range">The range, in either direction.</param>
    /// <returns>The text.</returns>
    public string TextOf(DocumentRange range)
    {
        var (start, end) = range.Normalize(this);
        var first = Resolve(start);
        var last = Resolve(end);
        return string.Join("\n", ParagraphsIn(range).Select(p =>
        {
            var text = p.Text;
            return text[(p.Id == first.Paragraph.Id ? first.Offset : 0)..(p.Id == last.Paragraph.Id ? last.Offset : text.Length)];
        }));
    }

    /// <summary>Gets the word at a position: the letters, digits and underscores around the character after it.</summary>
    /// <param name="position">The position.</param>
    /// <returns>The range of the word, or an empty range at the position if the character after it is not part of a word.</returns>
    public DocumentRange WordAt(DocumentPosition position)
    {
        var (paragraph, offset) = Resolve(position);
        var text = paragraph.Text;
        if (offset >= text.Length || !IsWordChar(text[offset])) return new DocumentRange(position, position);
        var start = offset;
        while (start > 0 && IsWordChar(text[start - 1])) start--;
        var end = offset;
        while (end < text.Length && IsWordChar(text[end])) end++;
        return new DocumentRange(position with { Offset = start }, position with { Offset = end });
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static IEnumerable<Paragraph> ParagraphsOf(IEnumerable<Block> blocks)
    {
        foreach (var block in blocks)
            if (block is Paragraph paragraph)
                yield return paragraph;
            else if (block is Table table)
                foreach (var nested in ParagraphsOf(table.Rows.SelectMany(r => r.Cells).SelectMany(c => c.Blocks)))
                    yield return nested;
    }

    private BlockLocation Locate(string topBlockId) =>
        TryLocate(topBlockId, out var location) && location.Path.Length == 0
            ? location
            : throw new ArgumentException($"{topBlockId} is not a top-level block of the document.");

    private static ArgumentException Invalid(DocumentPosition position) => new($"{position} does not point into a paragraph of the document.", nameof(position));
}
