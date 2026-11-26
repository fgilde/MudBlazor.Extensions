using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

internal sealed record DocumentTableContext(Table Table, int Row, int Column, int LastRow, int LastColumn, int StartRow, int StartCell)
{
    public bool Spans => LastRow > Row || LastColumn > Column;

    public bool IsHeader => Table.Rows[0].IsHeader;

    public IReadOnlyList<(int Row, int GridColumn)> Cells() =>
        [.. Enumerable.Range(Row, LastRow - Row + 1).SelectMany(r => Enumerable.Range(Column, LastColumn - Column + 1).Select(c => (r, c)))
            .Where(p => Tables.Layout(Table.Rows[p.r]).Any(l => l.Column <= p.c && p.c < l.Column + l.Cell.GridSpan))];

    public static DocumentTableContext At(TextDocument document, DocumentRange range)
    {
        if (range.Start == null || range.End == null)
            return null;
        try
        {
            var (start, end) = range.Normalize(document);
            if (document.TableCellOf(document.Resolve(start).Paragraph) is not { } a)
                return null;
            var b = document.TableCellOf(document.Resolve(end).Paragraph) is { } last && last.Table.Id == a.Table.Id ? last : a;
            var (ca, cb) = (a.Table.ColumnOf(a.Table.Rows[a.Row], a.Table.Rows[a.Row].Cells[a.Cell]), b.Table.ColumnOf(b.Table.Rows[b.Row], b.Table.Rows[b.Row].Cells[b.Cell]));
            var (sa, sb) = (a.Table.Rows[a.Row].Cells[a.Cell].GridSpan, b.Table.Rows[b.Row].Cells[b.Cell].GridSpan);
            return new DocumentTableContext(a.Table, Math.Min(a.Row, b.Row), Math.Min(ca, cb), Math.Max(a.Row, b.Row), Math.Max(ca + sa, cb + sb) - 1, a.Row, a.Cell);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static (int Row, int Cell)? Neighbour(Table table, int row, int cell, bool forward)
    {
        if (forward)
            return cell + 1 < table.Rows[row].Cells.Count ? (row, cell + 1) : row + 1 < table.Rows.Count ? (row + 1, 0) : null;
        return cell > 0 ? (row, cell - 1) : row > 0 ? (row - 1, table.Rows[row - 1].Cells.Count - 1) : null;
    }
}
