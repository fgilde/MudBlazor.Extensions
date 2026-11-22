namespace MudBlazor.Extensions.Office.Document;

/// <summary>A feature of a document that an export format may not be able to keep.</summary>
public enum DocumentFeature
{
    /// <summary>Character formatting such as fonts, sizes, colours, underline or highlighting.</summary>
    CharacterFormatting,

    /// <summary>Paragraph formatting such as alignment, indents, spacing or tab stops, and in Markdown page and column breaks.</summary>
    ParagraphFormatting,

    /// <summary>Paragraph, character and table styles whose effect the format cannot write; headings, list paragraphs and hyperlinks count as kept.</summary>
    Styles,

    /// <summary>Bulleted and numbered lists, and in Markdown start numbers, number formats other than 1. 2. 3. and numbered headings.</summary>
    Lists,

    /// <summary>Tables, and in Markdown cells with several paragraphs, list items or line breaks.</summary>
    Tables,

    /// <summary>Merged table cells.</summary>
    MergedCells,

    /// <summary>Tables inside table cells.</summary>
    NestedTables,

    /// <summary>
    /// Pictures, their placement and formats the target cannot hold: HTML and Markdown keep PNG, JPEG, GIF and BMP, RTF keeps PNG and JPEG; Markdown
    /// also loses a size other than the natural one.
    /// </summary>
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
        if (document.Sections.Count > 1 || document.Sections.Any(s => s.Columns > 1 || s.Page.Orientation == PageOrientation.Landscape && format != DocumentFileFormat.Rtf))
            losses.Add(DocumentFeature.Sections);
        if (document.Comments.Count > 0) losses.Add(DocumentFeature.Comments);
        return losses.Order().ToList();
    }

    private sealed class Scan(TextDocument document, DocumentFileFormat format, HashSet<DocumentFeature> losses)
    {
        private readonly bool _text = format == DocumentFileFormat.Text;
        private readonly bool _markdown = format == DocumentFileFormat.Markdown;
        private readonly bool _html = format == DocumentFileFormat.Html;
        private const string ListParagraphStyle = "ListParagraph";
        private const long EmuPerTwip = 635;
        private static readonly AbstractNumbering Decimal = ListTemplates.Create(ListTemplate.Decimal);
        private readonly bool _rtf = format == DocumentFileFormat.Rtf;
        private readonly Dictionary<(string, bool), bool> _styles = [];

        public void Blocks(IEnumerable<Block> blocks, int depth)
        {
            foreach (var block in blocks)
                switch (block)
                {
                    case Paragraph paragraph:
                        Paragraph(paragraph);
                        break;
                    case Table table:
                        Lost(DocumentFeature.Tables, _text || _markdown && table.Rows.SelectMany(r => r.Cells).Any(c =>
                            c.Blocks.Count(b => b is not Paragraph { Length: 0 }) > 1 ||
                            c.Blocks.OfType<Paragraph>().Any(p => document.ListLabel(p) != null || p.Inlines.Any(i => i is Break))));
                        Lost(DocumentFeature.NestedTables, depth > 0 && !_html);
                        Lost(DocumentFeature.MergedCells, !_html && !_rtf && table.Rows.SelectMany(r => r.Cells).Any(c => c.GridSpan > 1 || c.VMerge != VerticalMerge.None));
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
            var effective = document.EffectiveParagraphFormat(paragraph);
            Lost(DocumentFeature.ParagraphFormatting, LostParagraph(own) || _markdown && paragraph.Inlines.Any(i => i is Break { Kind: not BreakKind.Line }));
            Lost(DocumentFeature.Styles, own.StyleId != null && own.StyleId != document.Styles.DefaultParagraphStyle?.Id && own.StyleId != ListParagraphStyle &&
                                         (_text || !IsHeading(own.StyleId)) && StyleLost(own.StyleId, false));
            Lost(DocumentFeature.Lists, document.ListLabel(paragraph) != null && (_text || _rtf || _markdown && NumberingLost(effective)));
            foreach (var inline in paragraph.Inlines)
            {
                Lost(DocumentFeature.CharacterFormatting, LostRun(inline.Format));
                Lost(DocumentFeature.Styles, inline.Format.StyleId is { } style && (_text || style != InlineEdit.HyperlinkStyle) && StyleLost(style, true));
                Lost(DocumentFeature.Links, inline.Link != null && (_text || inline.Link.SafeTarget == null) || inline is Marker { Kind: "bookmarkStart", Name: { } name } && name != "_GoBack");
                Lost(DocumentFeature.Images, inline is InlineImage image && (_text || image.Placement != ImagePlacement.Inline ||
                                                                             !(_rtf ? RtfWriter.ImageTypes : HtmlWriter.ImageTypes).Contains(document.MediaContentType(image.MediaPart)?.ToLowerInvariant() ?? "") ||
                                                                             _markdown && !NaturalSize(image) ||
                                                                             _rtf && document.GetMedia(image.MediaPart)?.Length > RtfWriter.MaxImageBytes));
                Lost(DocumentFeature.Fields, inline is OpaqueInline { Kind: "field" or "fieldStart" or "fieldEnd" });
                Lost(DocumentFeature.OpaqueContent, inline is OpaqueInline { Kind: not ("field" or "fieldStart" or "fieldEnd") });
            }
        }

        private bool LostParagraph(ParagraphFormat format) =>
            !_rtf && (format.IndentRight != null || format.SpacingBefore != null || format.SpacingAfter != null || format.Line != null || format.KeepWithNext != null ||
                      format.KeepLines != null || format.WidowControl != null || format.Tabs != null ||
                      !_html && (format.Alignment != null || format.IndentLeft != null || format.IndentFirstLine != null || format.Shading != null) ||
                      _markdown && format.PageBreakBefore != null || _text && format.OutlineLevel != null);

        private bool LostRun(RunFormat format) =>
            !_html && !_rtf && (format.Underline != null || format.Color != null || format.SizePt != null || format.Highlight != null || format.Shading != null ||
                                format.Caps != null || format.SmallCaps != null || format.VerticalAlign != null || format.DoubleStrike != null || format.Hidden != null ||
                                format.Font != null && !(_markdown && format.Font == HtmlReader.Monospace) ||
                                _text && (format.Bold != null || format.Italic != null || format.Strike != null));

        private bool StyleLost(string id, bool character)
        {
            if (_styles.TryGetValue((id, character), out var lost)) return lost;
            var plain = new Paragraph { Inlines = [new TextRun("x")] };
            var styled = character
                ? plain with { Inlines = [new TextRun("x") { Format = new RunFormat { StyleId = id } }] }
                : plain with { Format = new ParagraphFormat { StyleId = id } };
            lost = !character && LostParagraph(Diff(document.EffectiveParagraphFormat(styled), document.EffectiveParagraphFormat(plain))) ||
                   LostRun(Diff(document.EffectiveRunFormat(styled, styled.Inlines[0]), document.EffectiveRunFormat(plain, plain.Inlines[0])));
            return _styles[(id, character)] = lost;
        }

        private bool NumberingLost(ParagraphFormat effective)
        {
            if (effective.OutlineLevel is < 9) return true;
            if (effective.Numbering is not { } numbering || document.Numbering.Level(numbering) is not { } level || level.Format is NumberFormat.Bullet or NumberFormat.None)
                return false;
            var expected = Decimal.Levels.FirstOrDefault(l => l.Level == level.Level);
            return expected == null || level.Format != expected.Format || level.Text != expected.Text || level.Start != expected.Start ||
                   document.Numbering.Instances.FirstOrDefault(i => i.Id == numbering.NumId)?.Overrides.Any(o => o.Level == numbering.Level && o.StartOverride != null) == true;
        }

        private bool NaturalSize(InlineImage image)
        {
            var data = document.GetMedia(image.MediaPart);
            var type = document.MediaContentType(image.MediaPart);
            return data == null || type == null ||
                   InsertImageCommand.Size(data, type, document.Sections[^1].Page.TextWidth * EmuPerTwip) == (image.WidthEmu, image.HeightEmu);
        }

        private static T? Changed<T>(T? value, T? basis) where T : struct => Equals(value, basis) ? null : value;

        private static string Changed(string value, string basis) => value == basis ? null : value;

        private static ParagraphFormat Diff(ParagraphFormat value, ParagraphFormat basis) => new()
        {
            Alignment = Changed(value.Alignment, basis.Alignment), IndentLeft = Changed(value.IndentLeft, basis.IndentLeft),
            IndentRight = Changed(value.IndentRight, basis.IndentRight), IndentFirstLine = Changed(value.IndentFirstLine, basis.IndentFirstLine),
            SpacingBefore = Changed(value.SpacingBefore, basis.SpacingBefore), SpacingAfter = Changed(value.SpacingAfter, basis.SpacingAfter),
            Line = Changed(value.Line, basis.Line), KeepWithNext = Changed(value.KeepWithNext, basis.KeepWithNext), KeepLines = Changed(value.KeepLines, basis.KeepLines),
            PageBreakBefore = Changed(value.PageBreakBefore, basis.PageBreakBefore), WidowControl = Changed(value.WidowControl, basis.WidowControl),
            OutlineLevel = Changed(value.OutlineLevel, basis.OutlineLevel), Tabs = Equals(value.Tabs, basis.Tabs) ? null : value.Tabs,
            Shading = Changed(value.Shading, basis.Shading)
        };

        private static RunFormat Diff(RunFormat value, RunFormat basis) => new()
        {
            Bold = Changed(value.Bold, basis.Bold), Italic = Changed(value.Italic, basis.Italic), Strike = Changed(value.Strike, basis.Strike),
            DoubleStrike = Changed(value.DoubleStrike, basis.DoubleStrike), Caps = Changed(value.Caps, basis.Caps), SmallCaps = Changed(value.SmallCaps, basis.SmallCaps),
            Hidden = Changed(value.Hidden, basis.Hidden), Underline = Changed(value.Underline, basis.Underline), SizePt = Changed(value.SizePt, basis.SizePt),
            Font = Changed(value.Font, basis.Font), Color = Changed(value.Color, basis.Color), Highlight = Changed(value.Highlight, basis.Highlight),
            Shading = Changed(value.Shading, basis.Shading), VerticalAlign = Changed(value.VerticalAlign, basis.VerticalAlign)
        };

        private static bool IsHeading(string styleId) => styleId.Length == 8 && styleId.StartsWith("Heading", StringComparison.Ordinal) && styleId[7] is >= '1' and <= '6';

        private void Lost(DocumentFeature feature, bool lost)
        {
            if (lost) losses.Add(feature);
        }
    }
}
