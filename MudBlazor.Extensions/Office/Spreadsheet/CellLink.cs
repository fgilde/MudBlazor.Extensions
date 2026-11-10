using System.Net;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>A hyperlink on a cell.</summary>
/// <param name="Cell">The linked cell.</param>
/// <param name="Target">External target such as a web address, stored as a relationship of the sheet; null for a link inside the workbook.</param>
/// <param name="Location">
/// Place inside the workbook, for example <c>'Sheet 2'!A1</c> or a defined name, used when <paramref name="Target"/> is null. It follows row,
/// column and sheet changes like a formula. With an external target it is the fragment of that target and stays as written.
/// </param>
/// <param name="Display">Text shown for the link, or null.</param>
/// <param name="Tooltip">Text shown when hovering the link, or null.</param>
public sealed record CellLink(CellRef Cell, string Target, string Location, string Display, string Tooltip)
{
    private static readonly string[] SafeSchemes = ["http", "https", "mailto"];

    /// <summary>
    /// True if the link may be followed when clicked: a location inside the workbook that is not a URI with a scheme, or an absolute
    /// <c>http</c>, <c>https</c> or <c>mailto</c> target. Any other target (<c>file</c>, relative paths, <c>javascript</c> …) should only be shown as text.
    /// </summary>
    public bool IsSafe => string.IsNullOrEmpty(Target)
        ? !string.IsNullOrEmpty(Location) && (CellRange.TryParse(Location, out _) || !Uri.TryCreate(WebUtility.HtmlDecode(Location), UriKind.Absolute, out _))
        : Uri.TryCreate(Target, UriKind.Absolute, out var uri) && SafeSchemes.Contains(uri.Scheme);

    internal XElement Source { get; init; }

    internal string RelationshipId { get; init; }

    internal (int Rows, int Columns) Span { get; init; }

    internal CellRange Area => CellRange.FromBounds(Cell.Row, Cell.Column, Math.Min(Cell.Row + Span.Rows, CellRef.MaxRows - 1),
        Math.Min(Cell.Column + Span.Columns, CellRef.MaxColumns - 1));

    internal CellLink Shift(Axis axis, int index, int count) =>
        Worksheet.MoveRange(Area, axis, index, count) is not { } moved ? null
        : moved == Area ? this
        : this with { Cell = new CellRef(moved.Top, moved.Left), Span = (moved.Bottom - moved.Top, moved.Right - moved.Left) };
}
