using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>A named cell style of the Cell Styles gallery.</summary>
/// <param name="Name">The name, for example Good or Heading 1.</param>
/// <param name="Style">The parts of the cell format the style sets.</param>
public sealed record SpreadsheetCellStyle(string Name, DifferentialStyle Style)
{
    /// <summary>Applies the style like Excel: each part the style sets (font, fill, border, number format) replaces that part of the cell format.</summary>
    /// <param name="cellStyle">The current cell format.</param>
    /// <returns>The new cell format.</returns>
    public CellStyle ApplyTo(CellStyle cellStyle)
    {
        var basis = cellStyle with
        {
            Font = Style.Font == null ? cellStyle.Font : new CellFont { Name = cellStyle.Font.Name, Size = cellStyle.Font.Size },
            Border = Style.Border == null ? cellStyle.Border : new CellBorder()
        };
        return Style.ApplyTo(basis);
    }
}

/// <summary>The cell styles offered by the Cell Styles gallery of <see cref="MudExSpreadsheetEditor"/>.</summary>
public static class SpreadsheetCellStyles
{
    /// <summary>Creates the styles; the currency style uses the current culture.</summary>
    /// <returns>Normal, Good, Bad, Neutral, Heading 1–4, Input, Output, Calculation, Note, Percent and Currency.</returns>
    public static IReadOnlyList<SpreadsheetCellStyle> Create()
    {
        const string heading = "#44546A";
        return
        [
            new("Normal", new DifferentialStyle(new DifferentialFont(), new CellFill(), new DifferentialBorder(), NumberFormat.General)),
            new("Good", Colored("#006100", "#C6EFCE")),
            new("Bad", Colored("#9C0006", "#FFC7CE")),
            new("Neutral", Colored("#9C5700", "#FFEB9C")),
            new("Heading 1", Heading(15, heading, Bottom(CellBorderStyle.Thick, "#4472C4"))),
            new("Heading 2", Heading(13, heading, Bottom(CellBorderStyle.Thick, "#A2B8E1"))),
            new("Heading 3", Heading(11, heading, Bottom(CellBorderStyle.Medium, "#8EA9DB"))),
            new("Heading 4", Heading(11, heading, new DifferentialBorder())),
            new("Input", Boxed(new DifferentialFont { Color = "#3F3F76" }, "#FFCC99", "#7F7F7F")),
            new("Output", Boxed(new DifferentialFont { Bold = true, Color = "#3F3F3F" }, "#F2F2F2", "#3F3F3F")),
            new("Calculation", Boxed(new DifferentialFont { Bold = true, Color = "#FA7D00" }, "#F2F2F2", "#7F7F7F")),
            new("Note", Boxed(null, "#FFFFCC", "#B2B2B2")),
            new("Percent", new DifferentialStyle(NumberFormat: "0%")),
            new("Currency", new DifferentialStyle(NumberFormat: MudExSpreadsheetEditor.NumberFormatCode("Currency")))
        ];
    }

    private static DifferentialStyle Colored(string font, string fill) => new(new DifferentialFont { Color = font }, new CellFill { Color = fill });

    private static DifferentialStyle Heading(double size, string color, DifferentialBorder border) =>
        new(new DifferentialFont { Bold = true, Size = size, Color = color }, null, border);

    private static DifferentialBorder Bottom(CellBorderStyle style, string color) => new() { Bottom = new CellBorderSide { Style = style, Color = color } };

    private static DifferentialStyle Boxed(DifferentialFont font, string fill, string border)
    {
        var side = new CellBorderSide { Style = CellBorderStyle.Thin, Color = border };
        return new DifferentialStyle(font, new CellFill { Color = fill }, new DifferentialBorder { Left = side, Right = side, Top = side, Bottom = side });
    }
}
