namespace MudBlazor.Extensions.Office.Document;

internal static class EffectiveFormat
{
    private const int MaxChain = 20;

    private static readonly (Func<RunFormat, bool?> Get, Func<RunFormat, bool, RunFormat> Set)[] Toggles =
    [
        (f => f.Bold, (f, v) => f with { Bold = v }),
        (f => f.Italic, (f, v) => f with { Italic = v }),
        (f => f.Caps, (f, v) => f with { Caps = v }),
        (f => f.SmallCaps, (f, v) => f with { SmallCaps = v }),
        (f => f.Strike, (f, v) => f with { Strike = v }),
        (f => f.DoubleStrike, (f, v) => f with { DoubleStrike = v }),
        (f => f.Hidden, (f, v) => f with { Hidden = v })
    ];

    internal static ParagraphFormat Paragraph(DocumentStyles styles, Numbering numbering, DocumentTheme theme, Table table, Paragraph paragraph)
    {
        var style = ParagraphStyle(styles, paragraph);
        var chain = Chain(styles, style);
        var direct = paragraph.Format;
        var reference = NumberingOf(chain, direct);
        var level = reference is { NumId: not 0 } r ? numbering.Level(r)?.Indent : null;
        ParagraphFormat[] layers =
        [
            styles.DefaultParagraphFormat,
            .. TableLayers(styles, table, paragraph).Select(l => l.Paragraph),
            direct.Numbering == null ? level : null,
            .. chain.Select(s => s.ParagraphFormat),
            direct.Numbering != null ? level : null,
            direct
        ];
        var result = layers.Aggregate(new ParagraphFormat(), Merge);
        return result with
        {
            StyleId = style?.Id,
            Numbering = reference is { NumId: not 0 } ? reference : null,
            Tabs = result.Tabs?.Where(t => t.Alignment != TabAlignment.Clear).OrderBy(t => t.Position).ToList(),
            Shading = theme.Color(result.Shading)
        };
    }

    internal static RunFormat Run(DocumentStyles styles, DocumentTheme theme, Table table, Paragraph paragraph, Inline inline)
    {
        var direct = inline == null ? paragraph.MarkFormat ?? new RunFormat() : inline.Format ?? new RunFormat();
        var characterStyle = styles.Find(direct.StyleId);
        RunFormat[] levels =
        [
            TableLayers(styles, table, paragraph).Select(l => l.Run).Aggregate(new RunFormat(), (basis, format) => format.Over(basis)),
            Chain(styles, ParagraphStyle(styles, paragraph)).Aggregate(new RunFormat(), (basis, style) => style.RunFormat.Over(basis)),
            Chain(styles, characterStyle).Aggregate(new RunFormat(), (basis, style) => style.RunFormat.Over(basis))
        ];
        var result = direct.Over(levels.Aggregate(styles.DefaultRunFormat, (basis, format) => format.Over(basis)));
        foreach (var (get, set) in Toggles)
        {
            var value = get(styles.DefaultRunFormat) ?? false;
            foreach (var format in levels)
                if (get(format) == true)
                    value = !value;
            result = set(result, get(direct) ?? value);
        }

        return result with { StyleId = characterStyle?.Id, Font = theme.Font(result.Font), Color = theme.Color(result.Color), Shading = theme.Color(result.Shading) };
    }

    internal static NumberingRef? NumberingOf(DocumentStyles styles, Paragraph paragraph) =>
        NumberingOf(Chain(styles, ParagraphStyle(styles, paragraph)), paragraph.Format);

    private static NumberingRef? NumberingOf(List<DocumentStyle> chain, ParagraphFormat direct) =>
        direct.Numbering ?? chain.Select(s => s.ParagraphFormat.Numbering).LastOrDefault(n => n != null);

    private static DocumentStyle ParagraphStyle(DocumentStyles styles, Paragraph paragraph) => styles.Find(paragraph.Format.StyleId) ?? styles.DefaultParagraphStyle;

    private static List<DocumentStyle> Chain(DocumentStyles styles, DocumentStyle style)
    {
        var chain = new List<DocumentStyle>();
        var seen = new HashSet<string>();
        for (; style != null && chain.Count < MaxChain && seen.Add(style.Id ?? ""); style = styles.Find(style.BasedOn)) chain.Add(style);
        chain.Reverse();
        return chain;
    }

    private static ParagraphFormat Merge(ParagraphFormat basis, ParagraphFormat format) =>
        format == null
            ? basis
            : format.Over(basis) with { Tabs = basis.Tabs == null || format.Tabs == null ? format.Tabs ?? basis.Tabs : [.. basis.Tabs.Where(b => format.Tabs.All(t => t.Position != b.Position)), .. format.Tabs] };

    private static IEnumerable<(ParagraphFormat Paragraph, RunFormat Run)> TableLayers(DocumentStyles styles, Table table, Paragraph paragraph)
    {
        if (table == null) yield break;
        var chain = Chain(styles, styles.Find(table.Format.StyleId) ?? styles.DefaultTableStyle);
        foreach (var style in chain) yield return (style.ParagraphFormat, style.RunFormat);
        foreach (var condition in Conditions(table, paragraph))
        foreach (var style in chain)
            if (style.Conditions.TryGetValue(condition, out var format))
                yield return format;
    }

    private static IEnumerable<string> Conditions(Table table, Paragraph paragraph)
    {
        var (row, cell) = CellOf(table, paragraph);
        if (row < 0) yield break;
        var look = table.Format.Look;
        var cells = table.Rows[row].Cells.Count;
        var firstRow = look.HasFlag(TableLook.FirstRow) && row == 0;
        var lastRow = look.HasFlag(TableLook.LastRow) && row == table.Rows.Count - 1;
        var firstColumn = look.HasFlag(TableLook.FirstColumn) && cell == 0;
        var lastColumn = look.HasFlag(TableLook.LastColumn) && cell == cells - 1;
        if (look.HasFlag(TableLook.BandedColumns) && !firstColumn && !lastColumn)
            yield return (cell - (look.HasFlag(TableLook.FirstColumn) ? 1 : 0)) % 2 == 0 ? "band1Vert" : "band2Vert";
        if (look.HasFlag(TableLook.BandedRows) && !firstRow && !lastRow)
            yield return (row - (look.HasFlag(TableLook.FirstRow) ? 1 : 0)) % 2 == 0 ? "band1Horz" : "band2Horz";
        if (firstColumn) yield return "firstCol";
        if (lastColumn) yield return "lastCol";
        if (firstRow) yield return "firstRow";
        if (lastRow) yield return "lastRow";
        if (firstRow && firstColumn) yield return "nwCell";
        if (firstRow && lastColumn) yield return "neCell";
        if (lastRow && firstColumn) yield return "swCell";
        if (lastRow && lastColumn) yield return "seCell";
    }

    private static (int Row, int Cell) CellOf(Table table, Paragraph paragraph)
    {
        for (var r = 0; r < table.Rows.Count; r++)
        for (var c = 0; c < table.Rows[r].Cells.Count; c++)
            if (table.Rows[r].Cells[c].Blocks.Any(b => ReferenceEquals(b, paragraph) || b.Id != null && b.Id == paragraph.Id))
                return (r, c);
        return (-1, -1);
    }
}
