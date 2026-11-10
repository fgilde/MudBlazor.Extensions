using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A static, not virtualized table of a range of a worksheet with values, fonts, fills, borders and merges, used for printing. Hidden rows and
/// columns are left out, and the table stops after <see cref="MaxCells"/> cells.
/// </summary>
public partial class MudExSpreadsheetPrintView
{
    private List<CellRange> _merges = [];
    private HashSet<int> _rows = [];
    private HashSet<int> _cols = [];

    /// <summary>The sheet to print.</summary>
    [Parameter] public Worksheet Sheet { get; set; }

    /// <summary>The range to print.</summary>
    [Parameter] public CellRange Range { get; set; }

    /// <summary>The most cells printed; later rows are left out with a message.</summary>
    [Parameter] public int MaxCells { get; set; } = 20000;

    private (List<int> Rows, List<int> Columns, bool Truncated) Layout()
    {
        var r = Range.Normalized;
        var cols = Enumerable.Range(r.Left, r.ColumnCount).Where(c => !Sheet.HiddenColumns.Contains(c)).ToList();
        var maxRows = Math.Max(1, MaxCells / Math.Max(1, cols.Count));
        var rows = new List<int>();
        var truncated = false;
        for (var row = r.Top; row <= r.Bottom; row++)
        {
            if (Sheet.IsRowHidden(row)) continue;
            if (rows.Count == maxRows)
            {
                truncated = true;
                break;
            }

            rows.Add(row);
        }

        _rows = [.. rows];
        _cols = [.. cols];
        var printed = rows.Count == 0 || cols.Count == 0 ? r : CellRange.FromBounds(r.Top, r.Left, rows[^1], r.Right);
        _merges = Sheet.Merges.Where(m => m.Intersects(printed)).Select(m => Clip(m.Normalized, printed)).ToList();
        return (rows, cols, truncated);
    }

    private static CellRange Clip(CellRange m, CellRange r) =>
        CellRange.FromBounds(Math.Max(m.Top, r.Top), Math.Max(m.Left, r.Left), Math.Min(m.Bottom, r.Bottom), Math.Min(m.Right, r.Right));

    private bool Covered(CellRef at) => _merges.Any(m => m.Contains(at) && m.Start != at);

    private (int Rows, int Columns) Span(CellRef at) =>
        _merges.FirstOrDefault(x => x.Start == at) is var m && m.Start == at
            ? (Enumerable.Range(m.Top, m.RowCount).Count(_rows.Contains), Enumerable.Range(m.Left, m.ColumnCount).Count(_cols.Contains))
            : (1, 1);

    private static string Px(double value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "px";

    private string Text(CellRef at)
    {
        var cell = Sheet[at];
        return cell?.HasContent == true ? NumberFormat.Format(cell.Value, Sheet.Workbook.Styles[Sheet.EffectiveStyleId(at)].NumberFormat, CultureInfo.CurrentCulture, out _) : "";
    }

    private string CellStyle(CellRef at)
    {
        var s = Sheet.Workbook.Styles[Sheet.EffectiveStyleId(at)];
        var sb = new StringBuilder();
        if (s.Font.Bold) sb.Append("font-weight:700;");
        if (s.Font.Italic) sb.Append("font-style:italic;");
        if (s.Font.Underline || s.Font.Strikethrough) sb.Append("text-decoration:").Append(s.Font.Underline ? "underline " : "").Append(s.Font.Strikethrough ? "line-through" : "").Append(';');
        if (s.Font.Size > 0) sb.Append("font-size:").Append(s.Font.Size.ToString("0.##", CultureInfo.InvariantCulture)).Append("pt;");
        if (Color(s.Font.Color) is { } color) sb.Append("color:").Append(color).Append(';');
        if (Color(s.Fill.Color) is { } fill) sb.Append("background:").Append(fill).Append(';');
        sb.Append(s.Alignment.Horizontal switch
        {
            CellHorizontalAlignment.Center => "text-align:center;",
            CellHorizontalAlignment.Right => "text-align:right;",
            CellHorizontalAlignment.General when Sheet[at]?.Value.Kind == CellValueKind.Number => "text-align:right;",
            _ => ""
        });
        if (s.Alignment.WrapText) sb.Append("white-space:pre-wrap;");
        Side(sb, "left", s.Border.Left);
        Side(sb, "right", s.Border.Right);
        Side(sb, "top", s.Border.Top);
        Side(sb, "bottom", s.Border.Bottom);
        return sb.ToString();
    }

    private static void Side(StringBuilder sb, string side, CellBorderSide b)
    {
        if (b.Style == CellBorderStyle.None) return;
        sb.Append("border-").Append(side).Append(":1px solid ").Append(Color(b.Color) ?? "#000").Append(';');
    }

    private static string Color(string color) =>
        color is { Length: 7 or 9 } && color[0] == '#' && color.Skip(1).All(char.IsAsciiHexDigit) ? color : null;
}
