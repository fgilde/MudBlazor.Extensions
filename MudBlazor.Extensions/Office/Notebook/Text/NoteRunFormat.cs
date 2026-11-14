namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>The formatting of a run of text.</summary>
/// <param name="Bold">Bold.</param>
/// <param name="Italic">Italic.</param>
/// <param name="Underline">Underlined.</param>
/// <param name="Strike">Struck through.</param>
/// <param name="SizePt">The font size in points, or null for the default.</param>
/// <param name="Color">The text colour as <c>#RRGGBB</c>, or null for the default.</param>
/// <param name="Highlight">The highlight colour as <c>#RRGGBB</c>, or null for none.</param>
/// <param name="Font">The font family, or null for the default.</param>
/// <param name="Link">The link target; only <c>http</c>, <c>https</c> and <c>mailto</c> links are kept when a file is read.</param>
public sealed record NoteRunFormat(
    bool Bold = false,
    bool Italic = false,
    bool Underline = false,
    bool Strike = false,
    double? SizePt = null,
    string Color = null,
    string Highlight = null,
    string Font = null,
    string Link = null);
