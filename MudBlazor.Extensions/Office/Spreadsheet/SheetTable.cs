using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>A column of a <see cref="SheetTable"/>.</summary>
/// <param name="Name">The column name, equal to the text of its header cell; unique in the table, ignoring case.</param>
/// <param name="TotalsFunction">Excel's totals row function (<c>sum</c>, <c>average</c>, <c>count</c>, <c>countNums</c>, <c>max</c>, <c>min</c>,
/// <c>stdDev</c>, <c>var</c> or <c>custom</c>), or null for none.</param>
/// <param name="CalculatedFormula">The formula every data cell of a calculated column holds (without leading equals sign), or null.</param>
public sealed record TableColumn(string Name, string TotalsFunction = null, string CalculatedFormula = null)
{
    internal int Id { get; init; }

    internal XElement Source { get; init; }
}

/// <summary>
/// An Excel table (list object) on a worksheet. Tables are immutable; change them with the table commands. Formulas address them with
/// structured references such as <c>Sales[Amount]</c> and <c>[@Qty]</c>. Table and column names are case-insensitive.
/// </summary>
public sealed class SheetTable
{
    internal SheetTable(string name, CellRange range, IReadOnlyList<TableColumn> columns)
    {
        Name = DisplayName = name;
        Range = range;
        Columns = columns;
    }

    /// <summary>The table name used in structured references.</summary>
    public string Name { get; private set; }

    /// <summary>The display name stored in the file; equal to <see cref="Name"/> for tables created or renamed here.</summary>
    public string DisplayName { get; private set; }

    /// <summary>The whole table including the header and totals rows.</summary>
    public CellRange Range { get; private set; }

    /// <summary>True if the first row of <see cref="Range"/> holds the column names.</summary>
    public bool HeaderRow { get; private set; } = true;

    /// <summary>True if the last row of <see cref="Range"/> is a totals row.</summary>
    public bool TotalsRow { get; private set; }

    /// <summary>The columns from left to right, one per column of <see cref="Range"/>.</summary>
    public IReadOnlyList<TableColumn> Columns { get; private set; }

    /// <summary>The built-in Excel table style, such as <c>TableStyleMedium2</c>, or null for none.</summary>
    public string StyleName { get; private set; }

    /// <summary>True to shade every other data row.</summary>
    public bool BandedRows { get; private set; }

    /// <summary>True to shade every other column.</summary>
    public bool BandedColumns { get; private set; }

    /// <summary>True to emphasize the first column.</summary>
    public bool FirstColumn { get; private set; }

    /// <summary>True to emphasize the last column.</summary>
    public bool LastColumn { get; private set; }

    /// <summary>True if the header row shows filter buttons.</summary>
    public bool HasAutoFilter { get; private set; }

    /// <summary>The data rows: <see cref="Range"/> without header and totals rows.</summary>
    public CellRange DataRange => CellRange.FromBounds(Range.Top + (HeaderRow ? 1 : 0), Range.Left, Range.Bottom - (TotalsRow ? 1 : 0), Range.Right);

    /// <summary>Finds a column by name, ignoring case.</summary>
    /// <param name="name">The column name.</param>
    /// <returns>The zero-based position in <see cref="Columns"/>, or -1.</returns>
    public int ColumnIndex(string name)
    {
        for (var i = 0; i < Columns.Count; i++)
            if (string.Equals(Columns[i].Name, name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    internal int Id { get; private set; }

    internal XElement Source { get; private set; }

    internal string PartName { get; private set; }

    internal string RelationshipId { get; private set; }

    internal bool Dirty { get; private set; }

    internal static SheetTable FromFile(XElement source, string partName, string relationshipId, int id, string name, string displayName, CellRange range,
        bool headerRow, bool totalsRow, IReadOnlyList<TableColumn> columns, string style, bool bandedRows, bool bandedColumns, bool firstColumn,
        bool lastColumn, bool autoFilter) =>
        new(name, range, columns)
        {
            Source = source, PartName = partName, RelationshipId = relationshipId, Id = id, DisplayName = displayName, HeaderRow = headerRow,
            TotalsRow = totalsRow, StyleName = style, BandedRows = bandedRows, BandedColumns = bandedColumns, FirstColumn = firstColumn,
            LastColumn = lastColumn, HasAutoFilter = autoFilter
        };

    internal static SheetTable Create(int id, string name, CellRange range, bool headerRow, IReadOnlyList<TableColumn> columns, string style) =>
        new(name, range, columns) { Id = id, HeaderRow = headerRow, HasAutoFilter = headerRow, StyleName = style, BandedRows = true, Dirty = true };

    internal SheetTable With(string name = null, CellRange? range = null, bool? totalsRow = null, IReadOnlyList<TableColumn> columns = null,
        string style = null, bool? bandedRows = null, bool? bandedColumns = null, bool? firstColumn = null, bool? lastColumn = null, bool? autoFilter = null)
    {
        var copy = (SheetTable)MemberwiseClone();
        if (name != null) copy.Name = copy.DisplayName = name;
        copy.Range = range ?? Range;
        copy.TotalsRow = totalsRow ?? TotalsRow;
        copy.Columns = columns ?? Columns;
        copy.StyleName = style ?? StyleName;
        copy.BandedRows = bandedRows ?? BandedRows;
        copy.BandedColumns = bandedColumns ?? BandedColumns;
        copy.FirstColumn = firstColumn ?? FirstColumn;
        copy.LastColumn = lastColumn ?? LastColumn;
        copy.HasAutoFilter = HeaderRow && (autoFilter ?? HasAutoFilter);
        copy.Dirty = true;
        return copy;
    }

    internal SheetTable RewriteFormulas(Func<string, string> rewrite)
    {
        List<TableColumn> columns = null;
        for (var i = 0; i < Columns.Count; i++)
        {
            if (Columns[i].CalculatedFormula is not { } formula || rewrite(formula) is var text && text == formula) continue;
            columns ??= [.. Columns];
            columns[i] = columns[i] with { CalculatedFormula = text };
        }

        return columns == null ? this : With(columns: columns);
    }

    internal bool Matches(string name) =>
        string.Equals(Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(DisplayName, name, StringComparison.OrdinalIgnoreCase);

    internal SheetTable Shift(Axis axis, int index, int count)
    {
        var start = axis == Axis.Row ? Range.Top : Range.Left;
        var end = axis == Axis.Row ? Range.Bottom : Range.Right;
        if (end < index) return this;
        if (Worksheet.MoveRange(Range, axis, index, count) is not { } moved) return null;
        if (axis == Axis.Row)
        {
            if (count > 0) return With(range: moved);
            if (HeaderRow && start >= index && start < index - count) return null;
            var totals = TotalsRow && !(end >= index && end < index - count);
            var data = moved.RowCount - (HeaderRow ? 1 : 0) - (totals ? 1 : 0);
            return data < 1 ? null : With(range: moved, totalsRow: totals);
        }

        var columns = Columns.ToList();
        if (count > 0 && index > start)
            columns.InsertRange(index - start, Enumerable.Range(0, count).Select(_ => new TableColumn(null)));
        else if (count < 0)
            for (var c = Math.Min(end, index - count - 1); c >= Math.Max(start, index); c--)
                columns.RemoveAt(c - start);
        return With(range: moved, columns: Unique(columns));
    }

    internal static List<TableColumn> Unique(IEnumerable<TableColumn> columns)
    {
        var list = columns.ToList();
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var i in Enumerable.Range(0, list.Count).OrderBy(i => string.IsNullOrEmpty(list[i].Name)))
        {
            var name = UniqueName(list[i].Name, taken);
            taken.Add(name);
            if (name != list[i].Name) list[i] = list[i] with { Name = name };
        }

        return list;
    }

    internal static string UniqueName(string wanted, IReadOnlySet<string> taken)
    {
        if (string.IsNullOrEmpty(wanted))
        {
            var i = 1;
            while (taken.Contains("Column" + i)) i++;
            return "Column" + i;
        }

        if (!taken.Contains(wanted)) return wanted;
        var n = 2;
        while (taken.Contains(wanted + n)) n++;
        return wanted + n;
    }
}
