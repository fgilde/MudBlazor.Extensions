using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The selection of a spreadsheet grid: the active cell and one or more ranges. The last range is the one extended with
/// Shift; its <see cref="CellRange.Start"/> is the anchor and its <see cref="CellRange.End"/> the moving corner.
/// </summary>
/// <param name="Active">The active cell, which receives typed input.</param>
/// <param name="Ranges">The selected ranges; never empty.</param>
public sealed record SpreadsheetSelection(CellRef Active, IReadOnlyList<CellRange> Ranges)
{
    /// <summary>The last (current) range.</summary>
    public CellRange Range => Ranges[^1];

    /// <summary>True if exactly one cell is selected.</summary>
    public bool IsSingleCell => Ranges.Count == 1 && Ranges[0].IsSingleCell;

    /// <summary>Creates a selection of one cell.</summary>
    /// <param name="cell">The cell.</param>
    /// <returns>The selection.</returns>
    public static SpreadsheetSelection Single(CellRef cell) => new(cell.Relative, [new CellRange(cell.Relative, cell.Relative)]);

    /// <summary>Creates a selection of one range with the active cell at its start.</summary>
    /// <param name="range">The range.</param>
    /// <returns>The selection.</returns>
    public static SpreadsheetSelection Of(CellRange range) => new(range.Start.Relative, [new CellRange(range.Start.Relative, range.End.Relative)]);

    /// <summary>Tests whether a cell is inside any selected range.</summary>
    /// <param name="cell">The cell.</param>
    /// <returns>True if selected.</returns>
    public bool Contains(CellRef cell) => Ranges.Any(r => r.Contains(cell));

    /// <inheritdoc />
    public bool Equals(SpreadsheetSelection other) =>
        other is not null && Active == other.Active && Ranges.SequenceEqual(other.Ranges);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Active, Ranges.Count, Ranges.Count > 0 ? Ranges[^1] : default);
}
