using System.Globalization;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>Colors the spreadsheet grid uses to draw a table style.</summary>
/// <param name="HeaderFill">Fill of the header row, or null for none.</param>
/// <param name="HeaderFont">Font color of the header row, or null for the default.</param>
/// <param name="BandFill">Fill of every other data row.</param>
/// <param name="Border">Color of the header and totals separator lines, or null for none.</param>
/// <param name="DataFont">Font color of the data rows, or null for the default.</param>
public sealed record SpreadsheetTableStyle(string HeaderFill, string HeaderFont, string BandFill, string Border, string DataFont);

/// <summary>
/// The built-in table style names (TableStyleLight1–21, TableStyleMedium1–28, TableStyleDark1–11) with a generated palette for each,
/// built from seven accent colors. Unknown names look like TableStyleMedium2.
/// </summary>
public static class SpreadsheetTableStyles
{
    private const string White = "#FFFFFF";
    private const string Black = "#000000";
    private static readonly string[] Accents = ["#595959", "#4472C4", "#ED7D31", "#A5A5A5", "#FFC000", "#5B9BD5", "#70AD47"];
    private static readonly (string Family, int Count)[] Families = [("Light", 21), ("Medium", 28), ("Dark", 11)];

    /// <summary>All built-in table style names in gallery order.</summary>
    public static IReadOnlyList<string> Names { get; } =
        Families.SelectMany(f => Enumerable.Range(1, f.Count).Select(i => "TableStyle" + f.Family + i.ToString(CultureInfo.InvariantCulture))).ToArray();

    /// <summary>Returns the palette of a table style.</summary>
    /// <param name="name">Style name such as TableStyleMedium2.</param>
    /// <returns>The palette; unknown names get the palette of TableStyleMedium2.</returns>
    public static SpreadsheetTableStyle Palette(string name)
    {
        foreach (var (family, count) in Families)
        {
            var prefix = "TableStyle" + family;
            if (name?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true
                && int.TryParse(name.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n >= 1 && n <= count)
                return Build(family, n);
        }

        return Build("Medium", 2);
    }

    /// <summary>Returns the table look of a cell: header, totals and banding formats of the table's style.</summary>
    /// <param name="table">The table.</param>
    /// <param name="cell">A cell inside the table.</param>
    /// <returns>The look, or null for cells outside the table or a table without style.</returns>
    public static DifferentialStyle Format(SheetTable table, CellRef cell)
    {
        if (table == null || string.IsNullOrEmpty(table.StyleName) || !table.Range.Contains(cell)) return null;
        var p = Palette(table.StyleName);
        var line = p.Border == null ? null : new CellBorderSide { Style = CellBorderStyle.Thin, Color = p.Border };
        if (table.HeaderRow && cell.Row == table.Range.Top)
            return new DifferentialStyle(new DifferentialFont { Bold = true, Color = p.HeaderFont }, Fill(p.HeaderFill), line == null ? null : new DifferentialBorder { Bottom = line });
        if (table.TotalsRow && cell.Row == table.Range.Bottom)
            return new DifferentialStyle(new DifferentialFont { Bold = true }, null, line == null ? null : new DifferentialBorder { Top = line });
        var data = table.DataRange;
        var banded = (table.BandedRows && (cell.Row - data.Top) % 2 == 0) || (table.BandedColumns && (cell.Column - data.Left) % 2 == 0);
        var bold = (table.FirstColumn && cell.Column == data.Left) || (table.LastColumn && cell.Column == data.Right);
        return new DifferentialStyle(new DifferentialFont { Bold = bold ? true : null, Color = p.DataFont }, banded ? Fill(p.BandFill) : null);
    }

    private static CellFill Fill(string color) => color == null ? null : new CellFill { Color = color };

    private static SpreadsheetTableStyle Build(string family, int n)
    {
        var accent = Accents[(n - 1) % Accents.Length];
        var variant = (n - 1) / Accents.Length;
        return (family, variant) switch
        {
            ("Light", 0) => new(null, null, Mix(accent, White, 0.8), accent, null),
            ("Light", 1) => new(accent, White, Mix(accent, White, 0.9), accent, null),
            ("Light", _) => new(null, null, Mix(accent, White, 0.8), Mix(accent, White, 0.4), null),
            ("Medium", 0) => new(accent, White, Mix(accent, White, 0.8), Mix(accent, White, 0.4), null),
            ("Medium", 1) => new(accent, White, Mix(accent, White, 0.6), White, null),
            ("Medium", 2) => new(Mix(accent, Black, 0.25), White, Mix(accent, White, 0.85), accent, null),
            ("Medium", _) => new(Mix(accent, White, 0.6), null, Mix(accent, White, 0.8), Mix(accent, White, 0.4), null),
            (_, 0) => new(Black, White, Mix(accent, Black, 0.25), null, White),
            _ => new(Black, White, Mix(accent, White, 0.6), Black, null)
        };
    }

    private static string Mix(string from, string to, double t)
    {
        static int Channel(string color, int i) => Convert.ToInt32(color.Substring(1 + i * 2, 2), 16);
        return "#" + string.Concat(Enumerable.Range(0, 3).Select(i =>
            ((int)Math.Round(Channel(from, i) + (Channel(to, i) - Channel(from, i)) * t)).ToString("X2", CultureInfo.InvariantCulture)));
    }
}
