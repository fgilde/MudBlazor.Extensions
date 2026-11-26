namespace MudBlazor.Extensions.Office.Document;

/// <summary>
/// Makes paragraphs list items, or removes the list if all of them already are items of a list of this kind. The list of the preceding paragraph in
/// the same container is continued if it is of this kind; otherwise a new list definition and instance are added. Items keep their level.
/// </summary>
/// <param name="paragraphIds">The ids of the paragraphs, in the body, in table cells or in headers and footers.</param>
/// <param name="template">The kind of list.</param>
public sealed class ApplyListCommand(IReadOnlyList<string> paragraphIds, ListTemplate template) : TextDocumentCommand
{
    private readonly IReadOnlyList<string> _paragraphIds = paragraphIds?.ToArray() ?? throw new ArgumentNullException(nameof(paragraphIds));

    /// <inheritdoc />
    public override string Description => template == ListTemplate.Bullet ? "Bullets" : "Numbering";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var paragraphs = _paragraphIds.Select(id => (id == null ? null : document.FindBlock(id)) as Paragraph ??
                                                    throw new ArgumentException($"{id} is not a paragraph of the document.", nameof(paragraphIds))).ToList();
        var order = document.AllParagraphs(true).Select((p, i) => (p.Id, i)).ToDictionary(p => p.Id, p => p.i);
        paragraphs = paragraphs.OrderBy(p => order[p.Id]).ToList();
        if (paragraphs.Count == 0) return ([], null);
        var numbering = document.Numbering;
        if (paragraphs.All(p => Lists.Of(document, p) is { } reference && Lists.KindOf(numbering, reference) == template))
            return (document.MapParagraphs(_paragraphIds, p => Lists.Unnumbered(document, p)), null);

        int numId;
        if (document.PreviousSibling(paragraphs[0].Id) is Paragraph previous && Lists.Of(document, previous) is { } continued && Lists.KindOf(numbering, continued) == template)
            numId = continued.NumId;
        else
        {
            var definition = ListTemplates.Create(template) with { Id = numbering.Abstracts.Select(a => a.Id + 1).DefaultIfEmpty(0).Max() };
            numId = Lists.NextId(numbering);
            numbering = new Numbering([.. numbering.Abstracts, definition], [.. numbering.Instances, new NumberingInstance(numId, definition.Id, [])]);
        }

        var edits = document.MapParagraphs(_paragraphIds, p => Lists.WithNumbering(p, new NumberingRef(numId, Lists.Of(document, p)?.Level ?? Lists.DirectLevel(p))));
        return (document.WithDefinitions(edits, null, numbering), null);
    }
}

/// <summary>
/// Moves list items up or down by levels, clamped to 0 to 8. Paragraphs that are not list items change their left indent by 720 twips (half an inch)
/// per level instead, not below 0.
/// </summary>
/// <param name="paragraphIds">The ids of the paragraphs.</param>
/// <param name="delta">The number of levels; positive moves deeper.</param>
public sealed class ChangeListLevelCommand(IReadOnlyList<string> paragraphIds, int delta) : TextDocumentCommand
{
    private const int IndentStep = 720;
    private readonly IReadOnlyList<string> _paragraphIds = paragraphIds?.ToArray() ?? throw new ArgumentNullException(nameof(paragraphIds));

    /// <inheritdoc />
    public override string Description => delta < 0 ? "Decrease indent" : "Increase indent";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document) =>
        (document.MapParagraphs(_paragraphIds, p =>
        {
            if (Lists.Of(document, p) is { } reference)
                return Math.Clamp(reference.Level + delta, 0, 8) is var level && level == reference.Level ? p : Lists.WithNumbering(p, reference with { Level = level });
            var indent = document.EffectiveParagraphFormat(p).IndentLeft ?? 0;
            var changed = Math.Max(0, indent + IndentStep * delta);
            return changed == indent ? p : p with { Format = p.Format with { IndentLeft = changed } };
        }), null);
}

/// <summary>
/// Restarts the numbering at a list item: a new list instance of the same definition with a start override for the item's level is assigned to the
/// item and to every following paragraph of the old instance.
/// </summary>
public sealed class RestartNumberingCommand : TextDocumentCommand
{
    private readonly string _paragraphId;
    private readonly int _startAt;

    /// <summary>Creates the command.</summary>
    /// <param name="paragraphId">The id of the list item.</param>
    /// <param name="startAt">The value the item gets.</param>
    /// <exception cref="ArgumentOutOfRangeException">The start value is negative.</exception>
    public RestartNumberingCommand(string paragraphId, int startAt = 1)
    {
        if (startAt < 0) throw new ArgumentOutOfRangeException(nameof(startAt), startAt, "The start value must not be negative.");
        (_paragraphId, _startAt) = (paragraphId, startAt);
    }

    /// <inheritdoc />
    public override string Description => "Restart numbering";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var paragraphId = _paragraphId;
        if (paragraphId == null || document.FindBlock(paragraphId) is not Paragraph paragraph || Lists.Of(document, paragraph) is not { } reference ||
            document.Numbering.Instances.FirstOrDefault(i => i.Id == reference.NumId) is not { } instance)
            throw new ArgumentException($"{paragraphId} is not a list item of the document.", nameof(paragraphId));
        var numId = Lists.NextId(document.Numbering);
        var overrides = instance.Overrides.Where(o => o.Level != reference.Level)
            .Append(new LevelOverride(reference.Level, _startAt, instance.Overrides.FirstOrDefault(o => o.Level == reference.Level)?.Definition))
            .OrderBy(o => o.Level)
            .ToList();
        var numbering = new Numbering(document.Numbering.Abstracts, [.. document.Numbering.Instances, new NumberingInstance(numId, instance.AbstractId, overrides)]);
        var levels = document.AllParagraphs(true).SkipWhile(p => p.Id != paragraphId)
            .Select(p => (p.Id, Reference: Lists.Of(document, p)))
            .Where(p => p.Reference?.NumId == reference.NumId)
            .ToDictionary(p => p.Id, p => p.Reference!.Value.Level);
        var edits = document.MapParagraphs(levels.Keys, p => Lists.WithNumbering(p, new NumberingRef(numId, levels[p.Id])));
        return (document.WithDefinitions(edits, null, numbering), null);
    }
}

/// <summary>Removes paragraphs from their lists; numbering that comes from the paragraph style is cancelled with list id 0.</summary>
/// <param name="paragraphIds">The ids of the paragraphs.</param>
public sealed class RemoveListCommand(IReadOnlyList<string> paragraphIds) : TextDocumentCommand
{
    private readonly IReadOnlyList<string> _paragraphIds = paragraphIds?.ToArray() ?? throw new ArgumentNullException(nameof(paragraphIds));

    /// <inheritdoc />
    public override string Description => "Remove list";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document) =>
        (document.MapParagraphs(_paragraphIds, p => Lists.Unnumbered(document, p)), null);
}

internal static class Lists
{
    internal static NumberingRef? Of(TextDocument document, Paragraph paragraph) =>
        EffectiveFormat.NumberingOf(document.Styles, paragraph) is { NumId: not 0 } reference ? reference : null;

    internal static ListTemplate? KindOf(Numbering numbering, NumberingRef reference)
    {
        if (numbering.Level(reference) is not { } level) return null;
        if (level.Format == NumberFormat.Bullet) return ListTemplate.Bullet;
        return numbering.Level(reference with { Level = 1 }) is not { } second || second.Format == NumberFormat.Decimal ? ListTemplate.Decimal : ListTemplate.Multilevel;
    }

    internal static int DirectLevel(Paragraph paragraph) =>
        paragraph.Format.Numbering == null && paragraph.Source?.Element(FormatXml.W + "pPr")?.Element(FormatXml.W + "numPr") is { } numPr && numPr.Element(FormatXml.W + "numId") == null
            ? Math.Clamp(FormatXml.Int(numPr.Element(FormatXml.W + "ilvl"), "val") ?? 0, 0, 8)
            : 0;

    internal static int NextId(Numbering numbering) => Math.Max(0, numbering.Instances.Select(i => i.Id).DefaultIfEmpty(0).Max()) + 1;

    internal static Paragraph WithNumbering(Paragraph paragraph, NumberingRef reference) =>
        paragraph.Format.Numbering == reference ? paragraph : paragraph with { Format = paragraph.Format with { Numbering = reference } };

    internal static Paragraph Unnumbered(TextDocument document, Paragraph paragraph)
    {
        var own = paragraph.Format with { Numbering = null };
        var format = EffectiveFormat.NumberingOf(document.Styles, paragraph with { Format = own }) is { NumId: not 0 } ? paragraph.Format with { Numbering = new NumberingRef(0, 0) } : own;
        return format == paragraph.Format ? paragraph : paragraph with { Format = format };
    }
}
