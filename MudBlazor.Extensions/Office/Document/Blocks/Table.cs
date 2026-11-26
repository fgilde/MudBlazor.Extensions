using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>How a cell takes part in a vertical merge (<c>w:vMerge</c>).</summary>
public enum VerticalMerge
{
    /// <summary>Not merged.</summary>
    None,

    /// <summary>Starts a merged range that continues in the cells below.</summary>
    Restart,

    /// <summary>Continues the merged range of the cell above.</summary>
    Continue
}

/// <summary>Vertical alignment of the content of a cell.</summary>
public enum CellVerticalAlignment
{
    /// <summary>At the top.</summary>
    Top,

    /// <summary>Centred.</summary>
    Center,

    /// <summary>At the bottom.</summary>
    Bottom
}

/// <summary>A table (<c>w:tbl</c>).</summary>
public sealed record Table : Block
{
    /// <summary>The table's own formatting.</summary>
    public TableFormat Format { get; init; } = new();

    /// <summary>The widths of the grid columns in twips. The list is treated as immutable after assignment: replace the table instead of changing it.</summary>
    public IReadOnlyList<int> Grid { get; init; } = [];

    /// <summary>The rows. The list is treated as immutable after assignment: replace the table instead of changing it.</summary>
    public IReadOnlyList<TableRow> Rows { get; init; } = [];

    /// <summary>Gets the grid column a cell starts in, counting the row's <see cref="TableRow.GridBefore"/> and the spans of the cells before it.</summary>
    /// <param name="row">A row of this table.</param>
    /// <param name="cell">A cell of the row, compared by reference.</param>
    /// <returns>The zero-based grid column.</returns>
    /// <exception cref="ArgumentException">The row is not part of this table or the cell is not part of the row.</exception>
    public int ColumnOf(TableRow row, TableCell cell)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (!Rows.Any(r => ReferenceEquals(r, row))) throw new ArgumentException("The row is not part of the table.", nameof(row));
        var column = row.GridBefore;
        foreach (var other in row.Cells)
        {
            if (ReferenceEquals(other, cell)) return column;
            column += other.GridSpan;
        }

        throw new ArgumentException("The cell is not part of the row.", nameof(cell));
    }
}

/// <summary>A row of a <see cref="Table"/>.</summary>
/// <param name="Cells">
/// The cells; a cell spans <see cref="TableCell.GridSpan"/> grid columns. The list is treated as immutable after assignment: replace the row instead
/// of changing it.
/// </param>
public sealed record TableRow(IReadOnlyList<TableCell> Cells)
{
    /// <summary>The row height in twips, or null for automatic.</summary>
    public int? Height { get; init; }

    /// <summary>True if the row repeats as header on every page.</summary>
    public bool IsHeader { get; init; }

    /// <summary>The number of grid columns left empty before the first cell (<c>w:gridBefore</c>).</summary>
    public int GridBefore { get; init; }

    /// <summary>The number of grid columns left empty after the last cell (<c>w:gridAfter</c>).</summary>
    public int GridAfter { get; init; }

    internal XElement Source { get; init; }

    internal bool Copy { get; init; }
}

/// <summary>A cell of a <see cref="TableRow"/>.</summary>
/// <param name="Blocks">
/// The content; Word requires at least one paragraph. The list is treated as immutable after assignment: replace the cell instead of changing it.
/// </param>
public sealed record TableCell(IReadOnlyList<Block> Blocks)
{
    /// <summary>The number of grid columns the cell spans, at least 1.</summary>
    public int GridSpan { get; init; } = 1;

    /// <summary>How the cell takes part in a vertical merge.</summary>
    public VerticalMerge VMerge { get; init; }

    /// <summary>The preferred width in twips, or null for automatic.</summary>
    public int? Width { get; init; }

    /// <summary>The shading fill colour (<c>#RRGGBB</c>, <c>auto</c> or <c>theme:&lt;name&gt;</c>), or null to take it from the table style.</summary>
    public string Shading { get; init; }

    /// <summary>The cell's own borders, or null to take them from the table.</summary>
    public TableBorders Borders { get; init; }

    /// <summary>The vertical alignment of the content, or null for the default (top).</summary>
    public CellVerticalAlignment? VerticalAlign { get; init; }

    internal XElement Source { get; init; }

    internal bool Copy { get; init; }
}
