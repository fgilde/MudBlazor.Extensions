namespace MudBlazor.Extensions.Office.Document;

/// <summary>A feature of a document that an export format may not be able to keep.</summary>
public enum DocumentFeature
{
    /// <summary>Character formatting such as fonts, sizes, colours, underline or highlighting.</summary>
    CharacterFormatting,

    /// <summary>Paragraph formatting such as alignment, indents, spacing or tab stops.</summary>
    ParagraphFormatting,

    /// <summary>Paragraph, character and table styles.</summary>
    Styles,

    /// <summary>Bulleted and numbered lists.</summary>
    Lists,

    /// <summary>Tables.</summary>
    Tables,

    /// <summary>Merged table cells.</summary>
    MergedCells,

    /// <summary>Tables inside table cells.</summary>
    NestedTables,

    /// <summary>Pictures, their placement and formats other than PNG, JPEG, GIF and BMP.</summary>
    Images,

    /// <summary>Hyperlinks, link targets other than <c>http</c>, <c>https</c>, <c>mailto</c> and <c>#anchor</c>, and bookmarks.</summary>
    Links,

    /// <summary>Headers and footers.</summary>
    HeadersFooters,

    /// <summary>Several sections, landscape pages or text columns.</summary>
    Sections,

    /// <summary>Comments.</summary>
    Comments,

    /// <summary>Fields such as page numbers; only their displayed text is kept.</summary>
    Fields,

    /// <summary>Content the editor does not model, such as content controls, equations, footnotes or embedded objects.</summary>
    OpaqueContent
}

/// <summary>Tells what an export to a file format loses.</summary>
public static class DocumentExport
{
    /// <summary>
    /// Lists the features a document uses that <see cref="TextDocument.Save"/> cannot keep in a format. Formatting counts if a paragraph or inline
    /// sets it directly; formatting that comes from styles counts as <see cref="DocumentFeature.Styles"/>.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="format">The target format.</param>
    /// <returns>The lost features in enum order; empty for <see cref="DocumentFileFormat.Docx"/> and <see cref="DocumentFileFormat.Dotx"/>.</returns>
    /// <exception cref="ArgumentNullException">The document is null.</exception>
    /// <exception cref="NotSupportedException">The format cannot be written (see <see cref="DocumentFileFormats.CanWrite"/>).</exception>
    public static IReadOnlyList<DocumentFeature> Losses(TextDocument document, DocumentFileFormat format)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!DocumentFileFormats.CanWrite(format)) throw new NotSupportedException($"Writing {format} files is not supported.");
        if (format is DocumentFileFormat.Docx or DocumentFileFormat.Dotx) return [];
        var losses = new HashSet<DocumentFeature>();
        new Scan(document, format, losses).Blocks(document.Blocks, 0);
        if (document.HeadersFooters.Any(h => h.Blocks.Any(b => b is not Paragraph { Length: 0 }))) losses.Add(DocumentFeature.HeadersFooters);
        if (document.Sections.Count > 1 || document.Sections.Any(s => s.Columns > 1 || s.Page.Orientation == PageOrientation.Landscape)) losses.Add(DocumentFeature.Sections);
        if (document.Comments.Count > 0) losses.Add(DocumentFeature.Comments);
        return losses.Order().ToList();
    }

    private sealed class Scan(TextDocument document, DocumentFileFormat format, HashSet<DocumentFeature> losses)
    {
        private readonly bool _text = format == DocumentFileFormat.Text;
        private readonly bool _markdown = format == DocumentFileFormat.Markdown;
        private readonly bool _html = format == DocumentFileFormat.Html;

        public void Blocks(IEnumerable<Block> blocks, int depth)
        {
            foreach (var block in blocks)
                switch (block)
                {
                    case Paragraph paragraph:
                        Paragraph(paragraph);
                        break;
                    case Table table:
                        Lost(DocumentFeature.Tables, _text);
                        Lost(DocumentFeature.NestedTables, depth > 0 && !_html);
                        Lost(DocumentFeature.MergedCells, !_html && table.Rows.SelectMany(r => r.Cells).Any(c => c.GridSpan > 1 || c.VMerge != VerticalMerge.None));
                        Lost(DocumentFeature.Styles, table.Format.StyleId != null && (_text || table.Format.StyleId != Tables.GridStyle));
                        foreach (var cell in table.Rows.SelectMany(r => r.Cells)) Blocks(cell.Blocks, depth + 1);
                        break;
                    case OpaqueBlock { Visible: true } opaque:
                        Lost(DocumentFeature.OpaqueContent, true);
                        if (opaque.Preview != null) Blocks(opaque.Preview, depth);
                        break;
                }
        }

        private void Paragraph(Paragraph paragraph)
        {
            var own = paragraph.Format;
            Lost(DocumentFeature.ParagraphFormatting,
                own.IndentRight != null || own.SpacingBefore != null || own.SpacingAfter != null || own.Line != null || own.KeepWithNext != null ||
                own.KeepLines != null || own.WidowControl != null || own.Tabs != null ||
                !_html && (own.Alignment != null || own.IndentLeft != null || own.IndentFirstLine != null || own.Shading != null) ||
                _markdown && own.PageBreakBefore != null || _text && own.OutlineLevel != null);
            Lost(DocumentFeature.Styles, own.StyleId != null && own.StyleId != document.Styles.DefaultParagraphStyle?.Id && (_text || !IsHeading(own.StyleId)));
            Lost(DocumentFeature.Lists, _text && document.ListLabel(paragraph) != null);
            var preformatted = _markdown && HtmlWriter.IsPreformatted(document, paragraph);
            foreach (var inline in paragraph.Inlines)
            {
                var run = inline.Format;
                Lost(DocumentFeature.CharacterFormatting, !_html && (
                    run.Underline != null || run.Color != null || run.SizePt != null || run.Highlight != null || run.Shading != null || run.Caps != null ||
                    run.SmallCaps != null || run.VerticalAlign != null || run.DoubleStrike != null || run.Hidden != null ||
                    run.Font != null && !(preformatted && run.Font == HtmlReader.Monospace) ||
                    _text && (run.Bold != null || run.Italic != null || run.Strike != null)));
                Lost(DocumentFeature.Styles, run.StyleId != null && (_text || run.StyleId != InlineEdit.HyperlinkStyle));
                Lost(DocumentFeature.Links, inline.Link != null && (_text || inline.Link.SafeTarget == null) || inline is Marker { Kind: "bookmarkStart", Name: { } name } && name != "_GoBack");
                Lost(DocumentFeature.Images, inline is InlineImage image && (_text || image.Placement != ImagePlacement.Inline ||
                                                                             !HtmlWriter.ImageTypes.Contains(document.MediaContentType(image.MediaPart)?.ToLowerInvariant() ?? "")));
                Lost(DocumentFeature.Fields, inline is OpaqueInline { Kind: "field" or "fieldStart" or "fieldEnd" });
                Lost(DocumentFeature.OpaqueContent, inline is OpaqueInline { Kind: not ("field" or "fieldStart" or "fieldEnd") });
            }
        }

        private static bool IsHeading(string styleId) => styleId.Length == 8 && styleId.StartsWith("Heading", StringComparison.Ordinal) && styleId[7] is >= '1' and <= '6';

        private void Lost(DocumentFeature feature, bool lost)
        {
            if (lost) losses.Add(feature);
        }
    }
}
