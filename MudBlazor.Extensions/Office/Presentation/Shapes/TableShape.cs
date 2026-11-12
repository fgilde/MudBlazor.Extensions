namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>
/// A table (<c>a:tbl</c> in a graphic frame). Every row has one cell per column; a merged range keeps its covered cells, marked as
/// <see cref="TableCell.IsMergedAway"/>.
/// </summary>
public sealed class TableShape : SlideShape
{
    /// <summary>The style of new tables: Medium Style 2 - Accent 1, the PowerPoint default.</summary>
    public const string DefaultStyleId = "{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}";

    internal TableShape()
    {
    }

    /// <summary>The column widths in EMU.</summary>
    public IReadOnlyList<long> ColumnWidths { get; internal set; } = [];

    /// <summary>The rows from top to bottom.</summary>
    public IReadOnlyList<TableRow> Rows { get; internal set; } = [];

    /// <summary>The id of the table style, for example <see cref="DefaultStyleId"/>, or null for none.</summary>
    public string StyleId { get; internal set; }

    /// <summary>True if the style formats the first row as a header.</summary>
    public bool FirstRow { get; internal set; }

    /// <summary>True if the style formats the first column.</summary>
    public bool FirstColumn { get; internal set; }

    /// <summary>True if the style formats the last row as a total row.</summary>
    public bool LastRow { get; internal set; }

    /// <summary>True if the style formats the last column.</summary>
    public bool LastColumn { get; internal set; }

    /// <summary>True if the style bands the rows.</summary>
    public bool BandedRows { get; internal set; }

    /// <summary>True if the style bands the columns.</summary>
    public bool BandedColumns { get; internal set; }

    internal TableShape Snapshot() => (TableShape)MemberwiseClone();

    internal void Restore(TableShape state)
    {
        (Transform, ColumnWidths, Rows, StyleId) = (state.Transform, state.ColumnWidths, state.Rows, state.StyleId);
        (FirstRow, FirstColumn, LastRow, LastColumn, BandedRows, BandedColumns) =
            (state.FirstRow, state.FirstColumn, state.LastRow, state.LastColumn, state.BandedRows, state.BandedColumns);
    }

    internal bool SameAs(TableShape other) =>
        Transform == other.Transform && StyleId == other.StyleId &&
        (FirstRow, FirstColumn, LastRow, LastColumn, BandedRows, BandedColumns) ==
        (other.FirstRow, other.FirstColumn, other.LastRow, other.LastColumn, other.BandedRows, other.BandedColumns) &&
        ColumnWidths.SequenceEqual(other.ColumnWidths) && Rows.Count == other.Rows.Count &&
        Rows.Zip(other.Rows).All(r => r.First.Height == r.Second.Height && r.First.Source == r.Second.Source && r.First.Cells.SequenceEqual(r.Second.Cells));
}
