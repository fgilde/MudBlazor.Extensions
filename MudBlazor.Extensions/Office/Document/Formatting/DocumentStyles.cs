using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>The kind of a <see cref="DocumentStyle"/>.</summary>
public enum StyleType
{
    /// <summary>A paragraph style; it may also carry character formatting.</summary>
    Paragraph,

    /// <summary>A character style for runs.</summary>
    Character,

    /// <summary>A table style.</summary>
    Table,

    /// <summary>A numbering style.</summary>
    Numbering
}

/// <summary>A style definition (<c>w:style</c>).</summary>
public sealed record DocumentStyle
{
    /// <summary>The style id referenced by paragraphs, runs and tables.</summary>
    public string Id { get; init; }

    /// <summary>The display name; Word's built-in names are lower case, for example <c>heading 1</c>.</summary>
    public string Name { get; init; }

    /// <summary>The kind of style.</summary>
    public StyleType Type { get; init; }

    /// <summary>The id of the style this one inherits from, or null.</summary>
    public string BasedOn { get; init; }

    /// <summary>The id of the style for the paragraph that follows when Enter is pressed, or null for the same style.</summary>
    public string Next { get; init; }

    /// <summary>The id of the linked paragraph or character style, or null.</summary>
    public string Link { get; init; }

    /// <summary>True if the style is shown in the style gallery.</summary>
    public bool QuickFormat { get; init; }

    /// <summary>True if the style is the default for its type.</summary>
    public bool IsDefault { get; init; }

    /// <summary>True if this is a built-in definition that the file does not contain yet.</summary>
    public bool BuiltIn { get; init; }

    /// <summary>The sort priority in the gallery, lower first; null sorts last.</summary>
    public int? UiPriority { get; init; }

    /// <summary>The character formatting of the style.</summary>
    public RunFormat RunFormat { get; init; } = new();

    /// <summary>The paragraph formatting of the style.</summary>
    public ParagraphFormat ParagraphFormat { get; init; } = new();

    /// <summary>The table formatting of a table style.</summary>
    public TableFormat TableFormat { get; init; } = new();

    internal IReadOnlyDictionary<string, (ParagraphFormat Paragraph, RunFormat Run)> Conditions { get; init; } =
        new Dictionary<string, (ParagraphFormat, RunFormat)>();

    internal XElement Source { get; init; }
}

/// <summary>The styles of a document with the document defaults.</summary>
public sealed class DocumentStyles
{
    internal DocumentStyles(IEnumerable<DocumentStyle> styles, RunFormat defaultRunFormat, ParagraphFormat defaultParagraphFormat)
    {
        StyleList = styles.ToList();
        DefaultRunFormat = defaultRunFormat ?? new RunFormat();
        DefaultParagraphFormat = defaultParagraphFormat ?? new ParagraphFormat();
    }

    /// <summary>The styles defined in the file, in file order.</summary>
    public IReadOnlyList<DocumentStyle> All => StyleList;

    /// <summary>The default paragraph style, or null if the file defines none.</summary>
    public DocumentStyle DefaultParagraphStyle => DefaultOf(StyleType.Paragraph);

    /// <summary>The default table style, or null if the file defines none.</summary>
    public DocumentStyle DefaultTableStyle => DefaultOf(StyleType.Table);

    /// <summary>The document default character formatting (<c>w:rPrDefault</c>).</summary>
    public RunFormat DefaultRunFormat { get; }

    /// <summary>The document default paragraph formatting (<c>w:pPrDefault</c>).</summary>
    public ParagraphFormat DefaultParagraphFormat { get; }

    /// <summary>
    /// The styles for the style gallery: the quick paragraph and character styles of the file by <see cref="DocumentStyle.UiPriority"/>, then the
    /// built-in quick styles the file does not define yet, without duplicate names.
    /// </summary>
    public IReadOnlyList<DocumentStyle> Gallery =>
        StyleList.Where(s => s.QuickFormat && s.Type is StyleType.Paragraph or StyleType.Character).OrderBy(s => s.UiPriority ?? int.MaxValue)
            .Concat(BuiltInStyles.All.Where(s => s.QuickFormat && Find(s.Id) == null).OrderBy(s => s.UiPriority))
            .DistinctBy(s => s.Name ?? s.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

    internal List<DocumentStyle> StyleList { get; }

    /// <summary>Finds a style of the file by id.</summary>
    /// <param name="id">The style id.</param>
    /// <returns>The style, or null if the file defines none with this id.</returns>
    public DocumentStyle Find(string id) => id == null ? null : StyleList.FirstOrDefault(s => s.Id == id);

    /// <summary>Finds a style of the file by name, ignoring case.</summary>
    /// <param name="name">The style name, for example <c>heading 1</c>.</param>
    /// <returns>The style, or null if the file defines none with this name.</returns>
    public DocumentStyle FindByName(string name) => StyleList.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    private DocumentStyle DefaultOf(StyleType type) => StyleList.FirstOrDefault(s => s.IsDefault && s.Type == type);
}
