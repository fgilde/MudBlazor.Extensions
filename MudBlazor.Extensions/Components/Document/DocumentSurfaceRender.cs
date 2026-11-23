using System.Text;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

using Block = MudBlazor.Extensions.Office.Document.Block;

internal sealed class DocumentSurfaceRender(TextDocument document)
{
    private const double EmuPerPixel = 9525;
    private const double TwipsPerPixel = 15;
    private const int MaxStyleChain = 20;
    private static readonly HashSet<string> SymbolFonts = new(StringComparer.OrdinalIgnoreCase) { "Symbol", "Wingdings", "Wingdings 2", "Wingdings 3", "Webdings" };
    private static readonly ParagraphFormat NoParagraph = new();
    private readonly HashSet<string> _media = [];
    private (int Version, RunFormat Format)? _defaults;

    public SurfaceBlock Render(Block block) => block switch
    {
        Paragraph paragraph => ParagraphBlock(paragraph),
        Table table => TableBlock(table),
        OpaqueBlock { Visible: true } opaque => new SurfaceBlock(opaque.Id, "o", "", null, null, null, null, null, null, (opaque.Preview ?? []).Select(Render).ToList()),
        _ => new SurfaceBlock(block.Id, "h", "", null, null, null, null, null, null, null)
    };

    public string SheetCss()
    {
        var page = document.Sections.Count > 0 ? document.Sections[0].Page : PageSetup.A4;
        return $"box-sizing:border-box;width:{Pt(page.Width)}pt;padding:{Pt(page.MarginTop)}pt {Pt(page.MarginRight)}pt {Pt(page.MarginBottom)}pt {Pt(page.MarginLeft + page.Gutter)}pt;" +
               DocumentCss.Run(Defaults(), new RunFormat(), true);
    }

    public IReadOnlyCollection<string> TakeMedia()
    {
        var media = _media.ToList();
        _media.Clear();
        return media;
    }

    public string RunCss(Paragraph paragraph, RunFormat own) =>
        DocumentCss.Run(document.EffectiveRunFormat(paragraph, new TextRun("") { Format = own }), Basis(document.EffectiveRunFormat(paragraph, null)), true);

    private RunFormat Basis(RunFormat mark) => Defaults() with { Bold = mark.Bold, Italic = mark.Italic, SizePt = mark.SizePt, Font = mark.Font };

    private RunFormat Defaults()
    {
        if (_defaults is not { } defaults || defaults.Version != document.Version) _defaults = defaults = (document.Version, document.EffectiveRunFormat(new Paragraph(), null));
        return defaults.Format;
    }

    private SurfaceBlock ParagraphBlock(Paragraph paragraph)
    {
        var format = document.EffectiveParagraphFormat(paragraph);
        var defaults = Defaults();
        var mark = document.EffectiveRunFormat(paragraph, null);
        var basis = Basis(mark);
        var label = document.ListLabel(paragraph);
        var css = DocumentCss.Paragraph(format, NoParagraph, false, false) + DocumentCss.Spacing(format) + DocumentCss.Run(basis, defaults, true);
        return new SurfaceBlock(paragraph.Id, "p", css, paragraph.Text, label, label == null ? null : LabelCss(format, mark, basis), Runs(paragraph, basis), null, null, null);
    }

    private string LabelCss(ParagraphFormat format, RunFormat mark, RunFormat basis)
    {
        var level = format.Numbering is { } reference ? document.Numbering.Level(reference) : null;
        var own = level?.LabelFormat ?? new RunFormat();
        var label = (own with { Font = document.Theme.Font(own.Font), Color = document.Theme.Color(own.Color), Shading = document.Theme.Color(own.Shading) }).Over(mark);
        if (label.Font != null && SymbolFonts.Contains(label.Font)) label = label with { Font = mark.Font };
        var hanging = -(format.IndentFirstLine ?? 0);
        var css = new StringBuilder("display:inline-block;text-indent:0;");
        if (hanging > 0 && level?.Suffix is null or "tab") css.Append("min-width:").Append(Pt(hanging)).Append("pt;");
        if (level?.Suffix == "space") css.Append("padding-right:0.25em;");
        return css.Append(DocumentCss.Run(label, basis, true)).ToString();
    }

    private List<SurfaceRun> Runs(Paragraph paragraph, RunFormat basis)
    {
        var runs = new List<SurfaceRun>();
        var text = new StringBuilder();
        string textCss = null;
        foreach (var inline in paragraph.Inlines)
        {
            if (inline is Marker or TextRun { Length: 0 }) continue;
            var format = document.EffectiveRunFormat(paragraph, inline);
            var css = (format.Hidden == true ? "display:none;" : "") + DocumentCss.Run(format, basis, true);
            if (inline is TextRun or Tab)
            {
                if (css != textCss) Flush(runs, text, textCss);
                textCss = css;
                text.Append(Paragraph.TextOf(inline));
                continue;
            }

            Flush(runs, text, textCss);
            runs.Add(inline switch
            {
                Break { Kind: BreakKind.Line } => new SurfaceRun("v", null, css, null, 0, 0),
                Break => new SurfaceRun("f", null, css, null, 0, 0),
                InlineImage image => ImageRun(image, css),
                OpaqueInline token => new SurfaceRun("o", token.DisplayText ?? "", css, null, 0, 0),
                _ => new SurfaceRun("o", "", css, null, 0, 0)
            });
        }

        Flush(runs, text, textCss);
        return runs;
    }

    private static void Flush(List<SurfaceRun> runs, StringBuilder text, string css)
    {
        if (text.Length == 0) return;
        runs.Add(new SurfaceRun("t", text.ToString(), css, null, 0, 0));
        text.Clear();
    }

    private SurfaceRun ImageRun(InlineImage image, string css)
    {
        if (image.MediaPart != null && document.GetMedia(image.MediaPart) != null) _media.Add(image.MediaPart);
        return new SurfaceRun("i", null, css, image.MediaPart, Pixels(image.WidthEmu / EmuPerPixel), Pixels(image.HeightEmu / EmuPerPixel));
    }

    private SurfaceBlock TableBlock(Table table)
    {
        var styles = TableStyles(table).ToList();
        var bordered = Bordered(table.Format.Borders) || styles.Any(s => Bordered(s.TableFormat.Borders));
        var border = bordered ? "border:0.5pt solid #000000;" : "border:1px dotted #BFBFBF;";
        var rows = new List<SurfaceRow>();
        for (var r = 0; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            var cells = Tables.Visible(table, r).Select(c => new SurfaceCell(c.Cell.GridSpan, c.RowSpan, CellCss(c.Cell, border), c.Cell.Blocks.Select(Render).ToList())).ToList();
            if (row.GridBefore > 0) cells.Insert(0, new SurfaceCell(row.GridBefore, 1, "border:none;", []));
            if (row.GridAfter > 0) cells.Add(new SurfaceCell(row.GridAfter, 1, "border:none;", []));
            rows.Add(new SurfaceRow(row.Height is { } height ? $"height:{Pt(height)}pt;" : "", cells));
        }

        var grid = table.Grid.Select(w => Pixels(w / TwipsPerPixel)).ToList();
        var css = new StringBuilder("border-collapse:collapse;");
        if (grid.Count > 0) css.Append("table-layout:fixed;width:").Append(grid.Sum(w => (long)w)).Append("px;");
        var alignment = table.Format.Alignment ?? styles.Select(s => s.TableFormat.Alignment).FirstOrDefault(a => a != null);
        if (alignment == TableAlignment.Center) css.Append("margin-left:auto;margin-right:auto;");
        else if (alignment == TableAlignment.Right) css.Append("margin-left:auto;");
        return new SurfaceBlock(table.Id, "t", css.ToString(), null, null, null, null, grid, rows, null);
    }

    private string CellCss(TableCell cell, string border)
    {
        var css = new StringBuilder(border);
        if (DocumentCss.Color(document.Theme.Color(cell.Shading)) is { } shading) css.Append("background-color:").Append(shading).Append(';');
        if (cell.VerticalAlign is CellVerticalAlignment.Center or CellVerticalAlignment.Bottom)
            css.Append("vertical-align:").Append(cell.VerticalAlign == CellVerticalAlignment.Center ? "middle" : "bottom").Append(';');
        return css.ToString();
    }

    private IEnumerable<DocumentStyle> TableStyles(Table table)
    {
        var seen = new HashSet<string>();
        for (var style = document.Styles.Find(table.Format.StyleId) ?? document.Styles.DefaultTableStyle; style != null && seen.Count < MaxStyleChain && seen.Add(style.Id ?? "");
             style = document.Styles.Find(style.BasedOn))
            yield return style;
    }

    private static bool Bordered(TableBorders borders) =>
        borders != null && new[] { borders.Top, borders.Bottom, borders.Left, borders.Right, borders.InsideH, borders.InsideV }.Any(b => b != null && b.Style is not ("nil" or "none"));

    private static string Pt(int twips) => DocumentCss.Number(twips / 20.0);

    private static int Pixels(double value) => (int)Math.Clamp(Math.Round(value), 1, int.MaxValue);
}
