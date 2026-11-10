using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>A note (classic Excel comment) on a cell.</summary>
/// <param name="Cell">The annotated cell.</param>
/// <param name="Author">The author's name.</param>
/// <param name="Text">The plain text; formatting runs from the file are kept while the text is unchanged.</param>
public sealed record CellNote(CellRef Cell, string Author, string Text)
{
    internal XElement Source { get; init; }

    internal string Shape { get; init; }

    internal CellNote Shift(Axis axis, int index, int count) =>
        Worksheet.MoveRange(new CellRange(Cell, Cell), axis, index, count) is not { } moved ? null
        : moved.Start == Cell ? this
        : this with { Cell = moved.Start };
}
