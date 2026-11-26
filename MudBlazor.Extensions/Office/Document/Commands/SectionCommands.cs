namespace MudBlazor.Extensions.Office.Document;

/// <summary>Sets the page size, orientation and margins of a section.</summary>
public sealed class SetPageSetupCommand : TextDocumentCommand
{
    private readonly string _sectionId;
    private readonly PageSetup _page;

    /// <summary>Creates the command.</summary>
    /// <param name="sectionId">The id of the section.</param>
    /// <param name="page">The page setup; width and height must be positive and the margins must leave a positive text width.</param>
    /// <exception cref="ArgumentNullException">The page setup is null.</exception>
    /// <exception cref="ArgumentException">The page setup is not usable.</exception>
    public SetPageSetupCommand(string sectionId, PageSetup page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (page.Width <= 0 || page.Height <= 0 || page.TextWidth <= 0 || page.Height - page.MarginTop - page.MarginBottom <= 0)
            throw new ArgumentException("The page leaves no room for text.", nameof(page));
        (_sectionId, _page) = (sectionId, page);
    }

    /// <inheritdoc />
    public override string Description => "Page setup";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var section = Sections.Find(document, _sectionId);
        return (section.Page == _page ? [] : [Sections.Replace(document, section, section with { Page = _page })], null);
    }
}

/// <summary>
/// Gives a section its own header or footer with one empty paragraph in the style <c>Header</c> or <c>Footer</c>. A first-page header turns on
/// the section's title page, an even-page header turns on different even and odd headers for the document. The caret moves into the new paragraph.
/// Nothing happens if the section already has its own header or footer of this type.
/// </summary>
/// <param name="sectionId">The id of the section.</param>
/// <param name="kind">Header or footer.</param>
/// <param name="type">The pages it applies to.</param>
public sealed class AddHeaderFooterCommand(string sectionId, HeaderFooterKind kind, HeaderFooterType type) : TextDocumentCommand
{
    /// <inheritdoc />
    public override string Description => kind == HeaderFooterKind.Header ? "Add header" : "Add footer";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var section = Sections.Find(document, sectionId);
        if (Sections.References(section, kind).ContainsKey(type)) return ([], null);
        var style = kind == HeaderFooterKind.Header ? "Header" : "Footer";
        var styles = StyleDefinitions.With(document.Styles, style, StyleType.Paragraph);
        var paragraph = new Paragraph { Id = document.NewId(), Format = new ParagraphFormat { StyleId = StyleDefinitions.Reference(styles, style) } };
        var edit = Sections.Create(document, section, kind, type, [paragraph]);
        return (document.WithDefinitions([edit], styles, null), d => Caret(d, paragraph.Id, 0));
    }
}

/// <summary>
/// Sets the content of a section's own header or footer to plain paragraphs of the style <c>Header</c> or <c>Footer</c>, one per line, all with the same alignment,
/// and creates the header or footer if the section has none. <c>{PAGE}</c> and <c>{NUMPAGES}</c> in a line become page number fields. Formatting inside
/// the old content is replaced. One undo step.
/// </summary>
/// <param name="sectionId">The id of the section.</param>
/// <param name="kind">Header or footer.</param>
/// <param name="type">The pages it applies to.</param>
/// <param name="lines">The text of the paragraphs; none means one empty paragraph.</param>
/// <param name="alignment">The alignment of the paragraphs.</param>
public sealed class SetHeaderFooterTextCommand(string sectionId, HeaderFooterKind kind, HeaderFooterType type, IReadOnlyList<string> lines, ParagraphAlignment alignment) : TextDocumentCommand
{
    /// <summary>The placeholder of the current page number.</summary>
    public const string PageToken = "{PAGE}";

    /// <summary>The placeholder of the number of pages.</summary>
    public const string PagesToken = "{NUMPAGES}";

    private static readonly System.Text.RegularExpressions.Regex Fields = new(@"\{(PAGE|NUMPAGES)\}", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public override string Description => kind == HeaderFooterKind.Header ? "Edit header" : "Edit footer";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var section = Sections.Find(document, sectionId);
        var style = kind == HeaderFooterKind.Header ? "Header" : "Footer";
        var styles = StyleDefinitions.With(document.Styles, style, StyleType.Paragraph);
        var format = new ParagraphFormat { StyleId = StyleDefinitions.Reference(styles, style), Alignment = alignment };
        var blocks = (lines.Count == 0 ? [""] : lines).Select(line => (Block)new Paragraph { Id = document.NewId(), Format = format, Inlines = Inlines(line ?? "") }).ToList();
        if (!Sections.References(section, kind).TryGetValue(type, out var id)) return (document.WithDefinitions([Sections.Create(document, section, kind, type, blocks)], styles, null), null);
        var index = document.HeaderFooterList.FindIndex(h => h.Id == id);
        return (document.WithDefinitions([new BlockEdit(index, 0, document.HeaderFooterList[index].Blocks, blocks)], styles, null), null);
    }

    private static List<Inline> Inlines(string line)
    {
        var inlines = new List<Inline>();
        var at = 0;
        foreach (System.Text.RegularExpressions.Match match in Fields.Matches(line))
        {
            if (match.Index > at) inlines.Add(new TextRun(line[at..match.Index]));
            var node = new System.Xml.Linq.XElement(FormatXml.W + "fldSimple", new System.Xml.Linq.XAttribute(FormatXml.W + "instr", match.Groups[1].Value),
                new System.Xml.Linq.XElement(FormatXml.W + "r", new System.Xml.Linq.XElement(FormatXml.W + "t", "1")));
            inlines.Add(new OpaqueInline("field", "1") { Instruction = match.Groups[1].Value, Nodes = [node] });
            at = match.Index + match.Length;
        }

        if (at < line.Length) inlines.Add(new TextRun(line[at..]));
        return inlines;
    }
}

/// <summary>
/// Removes a section's own reference to a header or footer, so the section inherits the previous section's. A header or footer no section refers to
/// any more is removed from the document, and its part is dropped when the document is saved.
/// </summary>
/// <param name="sectionId">The id of the section.</param>
/// <param name="kind">Header or footer.</param>
/// <param name="type">The pages it applies to.</param>
public sealed class RemoveHeaderFooterCommand(string sectionId, HeaderFooterKind kind, HeaderFooterType type) : TextDocumentCommand
{
    /// <inheritdoc />
    public override string Description => kind == HeaderFooterKind.Header ? "Remove header" : "Remove footer";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var section = Sections.Find(document, sectionId);
        if (!Sections.References(section, kind).TryGetValue(type, out var id)) return ([], null);
        var references = Sections.References(section, kind).Where(r => r.Key != type).ToDictionary(r => r.Key, r => r.Value);
        var changed = kind == HeaderFooterKind.Header ? section with { Headers = references } : section with { Footers = references };
        var edit = Sections.Replace(document, section, changed);
        if (!edit.SectionsAfter.Any(s => s.Headers.Values.Concat(s.Footers.Values).Contains(id)))
            edit = edit with { HeadersFootersBefore = document.HeaderFooterList.ToList(), HeadersFootersAfter = document.HeaderFooterList.Where(h => h.Id != id).ToList() };
        return ([edit], null);
    }
}

/// <summary>
/// Inserts a section break into a body paragraph: the paragraph is split at the position, its first half ends a new section with the page setup,
/// headers and footers of the section it was part of, and the second half stays in that section, which then starts as given. The caret moves to
/// the start of the second half.
/// </summary>
/// <param name="at">Where to break; a position in a paragraph of the body, not in a table, header or footer.</param>
/// <param name="start">Where the section after the break starts.</param>
public sealed class InsertSectionBreakCommand(DocumentPosition at, SectionStart start) : TextDocumentCommand
{
    /// <inheritdoc />
    public override string Description => "Insert section break";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(at);
        var position = document.Snap(new DocumentRange(at, at)).Start;
        if (position.Path.Count > 0 || document.BlockList.All(b => b.Id != position.BlockId))
            throw new ArgumentException("A section break can only be inserted into a paragraph of the body.", nameof(at));
        var (edits, after) = document.ReplaceRange(new DocumentRange(position, position), [[], []]);
        var edit = edits[0];
        var sections = (edit.SectionsAfter ?? document.SectionList).ToList();
        var sectionId = document.SectionOf(position.BlockId).Id;
        var index = sections.FindIndex(s => s.Id == sectionId);
        var number = document.SectionList.Max(s => int.TryParse(s.Id.AsSpan(1), out var n) ? n : 0) + 1;
        sections.Insert(index, sections[index] with { Id = "s" + number, LastBlockId = position.BlockId });
        sections[index + 1] = sections[index + 1] with { Start = start };
        return ([edit with { SectionsBefore = document.SectionList.ToList(), SectionsAfter = sections }], after);
    }
}

/// <summary>Sets the core document properties.</summary>
/// <param name="properties">The properties.</param>
public sealed class SetDocumentPropertiesCommand(DocumentProperties properties) : TextDocumentCommand
{
    private readonly DocumentProperties _properties = properties ?? throw new ArgumentNullException(nameof(properties));

    /// <inheritdoc />
    public override string Description => "Document properties";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document) =>
        (_properties == document.Properties ? [] : [new BlockEdit(-1, 0, [], []) { PropertiesBefore = document.Properties, PropertiesAfter = _properties }], null);
}

internal static class Sections
{
    internal static Section Find(TextDocument document, string sectionId) =>
        document.SectionList.FirstOrDefault(s => s.Id == sectionId) ?? throw new ArgumentException($"{sectionId} is not a section of the document.", nameof(sectionId));

    internal static IReadOnlyDictionary<HeaderFooterType, string> References(Section section, HeaderFooterKind kind) =>
        kind == HeaderFooterKind.Header ? section.Headers : section.Footers;

    internal static BlockEdit Create(TextDocument document, Section section, HeaderFooterKind kind, HeaderFooterType type, IReadOnlyList<Block> blocks)
    {
        var existing = (document.Package?.MainRelationships ?? []).Concat(document.HeaderFooterList.Select(h => new OpcRelationship(h.Id, "", "", null)));
        var part = new HeaderFooter { Id = OpcPackageWriter.NextId(existing), Kind = kind, Blocks = blocks };
        var references = new Dictionary<HeaderFooterType, string>(References(section, kind)) { [type] = part.Id };
        var changed = kind == HeaderFooterKind.Header ? section with { Headers = references } : section with { Footers = references };
        if (type == HeaderFooterType.First) changed = changed with { TitlePage = true };
        var edit = Replace(document, section, changed) with { HeadersFootersBefore = document.HeaderFooterList.ToList(), HeadersFootersAfter = [.. document.HeaderFooterList, part] };
        if (type == HeaderFooterType.Even && !document.EvenAndOddHeaders) edit = edit with { EvenAndOddBefore = false, EvenAndOddAfter = true };
        return edit;
    }

    internal static BlockEdit Replace(TextDocument document, Section section, Section changed) =>
        new(-1, 0, [], [])
        {
            SectionsBefore = document.SectionList.ToList(), SectionsAfter = document.SectionList.Select(s => ReferenceEquals(s, section) ? changed : s).ToList()
        };
}
