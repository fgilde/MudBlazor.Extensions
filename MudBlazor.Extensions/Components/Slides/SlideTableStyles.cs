using System.Xml.Linq;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

internal sealed record SlideTableCellStyle(string Fill, string TextColor, bool Bold);

internal static class SlideTableStyles
{
    internal const long LineWidth = 12700;
    private const long MarginX = 91440;
    private const long MarginY = 45720;

    private static readonly Dictionary<string, string> Accents = new(StringComparer.OrdinalIgnoreCase)
    {
        ["{21E4AEA4-8DFA-4A89-87EB-49C32662AFE8}"] = "accent2",
        ["{F5AB1C69-6EDB-4FF4-983F-18BD219EF322}"] = "accent3",
        ["{00A15C55-8517-42AA-B614-E9B94910E393}"] = "accent4",
        ["{7DF18680-E054-41AD-8BC1-D1AEF772440D}"] = "accent5",
        ["{93296810-A885-4BE3-A3E7-6D5BEEA58F35}"] = "accent6"
    };

    internal static SlideTableCellStyle[][] For(TableShape table, SlideMaster master)
    {
        var accent = "scheme:" + Accents.GetValueOrDefault(table.StyleId ?? "", "accent1");
        var (rows, columns) = (table.Rows.Count, table.ColumnWidths.Count);
        return table.Rows.Select((row, r) => row.Cells.Select((cell, c) =>
        {
            var strong = table.FirstRow && r == 0 || table.LastRow && r == rows - 1 || table.FirstColumn && c == 0 || table.LastColumn && c == columns - 1;
            var band = table.BandedRows && (r - (table.FirstRow ? 1 : 0)) % 2 == 0 || table.BandedColumns && (c - (table.FirstColumn ? 1 : 0)) % 2 == 0;
            var fill = cell.Fill switch
            {
                { Kind: ShapeFillKind.Solid } own => Resolve(own.Color, table, master),
                { Kind: ShapeFillKind.None } => null,
                _ => Resolve(strong ? accent : $"{accent} tint={(band ? 40000 : 20000)}", table, master)
            };
            return new SlideTableCellStyle(fill, Resolve(strong ? "scheme:lt1" : "scheme:dk1", table, master), strong);
        }).ToArray()).ToArray();
    }

    internal static string LineColor(TableShape table, SlideMaster master) => Resolve("scheme:lt1", table, master);

    internal static AutoShape TextShape(TableShape table, int row, int column, SlideTableCellStyle style, SlideMaster master)
    {
        var cell = table.Rows[row].Cells[column];
        var t = table.ResolvedTransform;
        var (x, width) = Span(table.ColumnWidths, column, cell.ColumnSpan, t.X, t.Width);
        var (y, height) = Span(table.Rows.Select(r => r.Height).ToList(), row, cell.RowSpan, t.Y, t.Height);
        return new AutoShape
        {
            Id = table.Id, Name = table.Name, Master = master, ColorMap = table.ColorMap,
            Transform = new ShapeTransform(x, y, width, height),
            Text = cell.Text ?? new TextBody { Paragraphs = [new TextParagraph()] },
            InheritsFrom = new AutoShape
            {
                Master = master,
                Source = Defaults(style),
                Text = new TextBody
                {
                    Anchor = cell.Anchor ?? TextAnchor.Top, Wrap = true, InsetLeft = cell.MarginLeft ?? MarginX, InsetTop = cell.MarginTop ?? MarginY,
                    InsetRight = cell.MarginRight ?? MarginX, InsetBottom = cell.MarginBottom ?? MarginY
                }
            }
        };
    }

    internal static (int Row, int Column) Anchor(TableShape table, int row, int column)
    {
        row = Math.Clamp(row, 0, table.Rows.Count - 1);
        column = Math.Clamp(column, 0, table.ColumnWidths.Count - 1);
        return Anchors(table).Where(a => a.Row <= row && a.Column <= column && row < a.Row + a.RowSpan && column < a.Column + a.ColumnSpan)
            .Select(a => ((int, int)?)(a.Row, a.Column)).FirstOrDefault() ?? (row, column);
    }

    internal static IEnumerable<(int Row, int Column, int RowSpan, int ColumnSpan)> Anchors(TableShape table) =>
        table.Rows.SelectMany((row, r) => row.Cells.Select((cell, c) => (Cell: cell, Row: r, Column: c)))
            .Where(e => !e.Cell.IsMergedAway).Select(e => (e.Row, e.Column, e.Cell.RowSpan, e.Cell.ColumnSpan));

    internal static (int Row, int Column, int RowCount, int ColumnCount) Range(TableShape table, int row, int column, int row2, int column2)
    {
        var (rows, columns) = (table.Rows.Count - 1, table.ColumnWidths.Count - 1);
        int top = Math.Clamp(Math.Min(row, row2), 0, rows), bottom = Math.Clamp(Math.Max(row, row2), 0, rows);
        int left = Math.Clamp(Math.Min(column, column2), 0, columns), right = Math.Clamp(Math.Max(column, column2), 0, columns);
        var anchors = Anchors(table).ToList();
        for (var grown = true; grown;)
        {
            grown = false;
            foreach (var (r, c, rowSpan, columnSpan) in anchors)
            {
                var (last, end) = (r + rowSpan - 1, c + columnSpan - 1);
                if (r > bottom || last < top || c > right || end < left || r >= top && last <= bottom && c >= left && end <= right)
                    continue;
                (top, bottom, left, right) = (Math.Min(top, r), Math.Max(bottom, last), Math.Min(left, c), Math.Max(right, end));
                grown = true;
            }
        }

        return (top, left, bottom - top + 1, right - left + 1);
    }

    private static (long Start, long Size) Span(IReadOnlyList<long> sizes, int index, int span, long origin, long total)
    {
        var sum = (double)Math.Max(1, sizes.Sum());
        return (origin + (long)Math.Round(sizes.Take(index).Sum() / sum * total), (long)Math.Round(sizes.Skip(index).Take(span).Sum() / sum * total));
    }

    private static string Resolve(string color, TableShape table, SlideMaster master) => ColorResolver.Resolve(color, master, table.ColorMap);

    private static XElement Defaults(SlideTableCellStyle style) =>
        new(PptxReader.P + "sp", new XElement(PptxReader.P + "txBody", new XElement(PptxReader.A + "lstStyle",
            Enumerable.Range(1, 9).Select(level => new XElement(PptxReader.A + $"lvl{level}pPr",
                new XElement(PptxReader.A + "defRPr", style.Bold ? new XAttribute("b", "1") : null,
                    ColorResolver.ToXml(style.TextColor) is { } color ? new XElement(PptxReader.A + "solidFill", color) : null))))));
}
