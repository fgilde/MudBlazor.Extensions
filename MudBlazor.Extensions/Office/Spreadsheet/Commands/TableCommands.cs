using System.Globalization;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Turns a range into a table with the next free name <c>TableN</c>. With headers, the first row holds the column names (empty and
/// duplicate names are made unique and written back as text); without headers the table has no header row and columns are named
/// <c>Column1</c>, <c>Column2</c> … A sheet auto filter overlapping the range is removed.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="range">The cells of the table, including the header row.</param>
/// <param name="hasHeaders">True if the first row holds the column names.</param>
/// <param name="styleName">A built-in Excel table style such as <c>TableStyleMedium2</c>.</param>
public sealed class CreateTableCommand(Worksheet sheet, CellRange range, bool hasHeaders, string styleName = "TableStyleMedium2") : IWorkbookCommand
{
    private TableUndo _undo;

    /// <inheritdoc />
    public string Description => "Format as table";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var area = range.Normalized;
        if (hasHeaders && area.RowCount == 1) area = CellRange.FromBounds(area.Top, area.Left, area.Top + 1, area.Right);
        Worksheet.Check(area);
        TableEdits.CheckFree(sheet, area, null, false);
        _undo = new TableUndo(wb, sheet);
        var table = SheetTable.Create(TableEdits.NextId(wb), TableEdits.NextName(wb), area, hasHeaders, TableEdits.Columns(sheet, area, hasHeaders, null), styleName);
        TableEdits.WriteHeaders(_undo, sheet, table);
        if (sheet.AutoFilter is { } filter && filter.Range.Intersects(area)) _undo.ClearAutoFilter();
        sheet.Tables = [.. sheet.Tables, table];
        _undo.Done(true, area);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _undo.Revert();
}

/// <summary>Changes the cells of a table. The header row stays where it is; new columns take their names from the header cells.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="table">The table name.</param>
/// <param name="range">The new range including header and totals rows.</param>
public sealed class ResizeTableCommand(Worksheet sheet, string table, CellRange range) : IWorkbookCommand
{
    private TableUndo _undo;

    /// <inheritdoc />
    public string Description => "Resize table";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var t = TableEdits.Get(sheet, table);
        var area = range.Normalized;
        Worksheet.Check(area);
        if (area.Top != t.Range.Top) throw new InvalidOperationException("The header row of a table cannot move.");
        if (area.RowCount < (t.HeaderRow ? 1 : 0) + (t.TotalsRow ? 1 : 0) + 1) throw new InvalidOperationException("A table needs at least one data row.");
        TableEdits.CheckFree(sheet, area, t);
        _undo = new TableUndo(wb, sheet);
        var resized = t.With(range: area, columns: TableEdits.Columns(sheet, area, t.HeaderRow, t));
        TableEdits.Replace(sheet, t, resized);
        TableEdits.WriteHeaders(_undo, sheet, resized);
        _undo.Done(true, t.Range, area);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _undo.Revert();
}

/// <summary>Renames a table and updates the structured references in all formulas and defined names.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="table">The current table name.</param>
/// <param name="newName">The new name: letters, digits, underscores and periods, starting with a letter or underscore, not a cell reference, unique among tables and defined names.</param>
public sealed class RenameTableCommand(Worksheet sheet, string table, string newName) : IWorkbookCommand
{
    private TableUndo _undo;

    /// <inheritdoc />
    public string Description => "Rename table";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var t = TableEdits.Get(sheet, table);
        TableEdits.ValidateName(wb, newName, t);
        _undo = new TableUndo(wb, sheet);
        var names = new[] { t.Name, t.DisplayName }.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _undo.Rewrite((f, _, _) => names.Aggregate(f, (text, old) => StructuredReferences.RenameTable(text, old, newName)));
        var current = TableEdits.Get(sheet, t.Name);
        TableEdits.Replace(sheet, current, current.With(name: newName));
        _undo.Done(true);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _undo.Revert();
}

/// <summary>Removes a table but keeps its cells; structured references to it become absolute A1 references.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="table">The table name.</param>
public sealed class ConvertToRangeCommand(Worksheet sheet, string table) : IWorkbookCommand
{
    private TableUndo _undo;

    /// <inheritdoc />
    public string Description => "Convert to range";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var t = TableEdits.Get(sheet, table);
        _undo = new TableUndo(wb, sheet);
        _undo.Rewrite((f, host, cell) => StructuredReferences.Replace(f, t.Name, node => TableEdits.ToA1(wb, sheet, t, node, host, cell)));
        sheet.Tables = sheet.Tables.Where(x => !x.Matches(t.Name)).ToList();
        _undo.Done(true, t.Range);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _undo.Revert();
}

/// <summary>Changes the style options of a table; null keeps an option.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="table">The table name.</param>
/// <param name="styleName">A built-in Excel table style such as <c>TableStyleLight9</c>, or an empty string for none.</param>
/// <param name="bandedRows">Shade every other row.</param>
/// <param name="bandedColumns">Shade every other column.</param>
/// <param name="firstColumn">Emphasize the first column.</param>
/// <param name="lastColumn">Emphasize the last column.</param>
/// <param name="hasAutoFilter">Show filter buttons (only with a header row).</param>
public sealed class SetTableOptionsCommand(Worksheet sheet, string table, string styleName = null, bool? bandedRows = null, bool? bandedColumns = null,
    bool? firstColumn = null, bool? lastColumn = null, bool? hasAutoFilter = null) : IWorkbookCommand
{
    private TableUndo _undo;

    /// <inheritdoc />
    public string Description => "Table options";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var t = TableEdits.Get(sheet, table);
        _undo = new TableUndo(wb, sheet);
        TableEdits.Replace(sheet, t, t.With(style: styleName, bandedRows: bandedRows, bandedColumns: bandedColumns, firstColumn: firstColumn,
            lastColumn: lastColumn, autoFilter: hasAutoFilter));
        _undo.Done(false, t.Range);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _undo.Revert();
}

/// <summary>
/// Shows or hides the totals row. Showing it takes the empty row below the table, writes <c>Total</c> into the first column and a
/// <c>SUBTOTAL</c> formula for every column with a totals function (the last column gets <c>sum</c> if none has one); hiding it clears the row.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="table">The table name.</param>
/// <param name="show">True to show the totals row.</param>
public sealed class SetTotalsRowCommand(Worksheet sheet, string table, bool show) : IWorkbookCommand
{
    private TableUndo _undo;

    /// <inheritdoc />
    public string Description => "Totals row";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var t = TableEdits.Get(sheet, table);
        _undo = null;
        if (t.TotalsRow == show) return;
        var r = t.Range;
        if (show)
        {
            var band = CellRange.FromBounds(r.Bottom + 1, r.Left, r.Bottom + 1, r.Right);
            if (band.Top >= CellRef.MaxRows || sheet.ExistingIn(band).Any(c => sheet[c].HasContent)) throw new InvalidOperationException("The row below the table is not empty.");
            TableEdits.CheckFree(sheet, band, null);
            _undo = new TableUndo(wb, sheet);
            var columns = t.Columns.ToList();
            if (columns.All(c => c.TotalsFunction == null)) columns[^1] = columns[^1] with { TotalsFunction = "sum", TotalsLabel = null };
            if (columns[0].TotalsFunction == null) columns[0] = columns[0] with { TotalsLabel = columns[0].TotalsLabel ?? "Total" };
            var shown = t.With(range: CellRange.FromBounds(r.Top, r.Left, band.Top, r.Right), totalsRow: true, columns: columns);
            TableEdits.Replace(sheet, t, shown);
            for (var i = 0; i < columns.Count; i++)
            {
                var at = new CellRef(band.Top, r.Left + i);
                if (TableEdits.Subtotal(columns[i].TotalsFunction, shown.Name, columns[i].Name) is { } formula) _undo.Set(at, default, formula);
                else if (columns[i].TotalsLabel is { } label) _undo.Set(at, CellValue.Text(label));
            }

            _undo.Done(true, shown.Range);
            return;
        }

        _undo = new TableUndo(wb, sheet);
        foreach (var c in sheet.ExistingIn(CellRange.FromBounds(r.Bottom, r.Left, r.Bottom, r.Right))) _undo.Set(c, default);
        TableEdits.Replace(sheet, t, t.With(range: CellRange.FromBounds(r.Top, r.Left, r.Bottom - 1, r.Right), totalsRow: false));
        _undo.Done(true, r);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _undo?.Revert();
}

/// <summary>Sets the totals row function of a column and the matching <c>SUBTOTAL</c> formula in its totals cell, as Excel does.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="table">The table name.</param>
/// <param name="column">The column name.</param>
/// <param name="function"><c>sum</c>, <c>average</c>, <c>count</c>, <c>countNums</c>, <c>max</c>, <c>min</c>, <c>stdDev</c>, <c>var</c>, or null for none.</param>
public sealed class SetTotalsFunctionCommand(Worksheet sheet, string table, string column, string function) : IWorkbookCommand
{
    private TableUndo _undo;

    /// <inheritdoc />
    public string Description => "Totals function";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var t = TableEdits.Get(sheet, table);
        if (!t.TotalsRow) throw new InvalidOperationException("The table has no totals row.");
        var i = t.ColumnIndex(column);
        if (i < 0) throw new ArgumentException($"The table has no column named '{column}'.", nameof(column));
        var formula = TableEdits.Subtotal(function, t.Name, t.Columns[i].Name);
        if (function != null && formula == null) throw new ArgumentException($"'{function}' is not a totals function.", nameof(function));
        _undo = new TableUndo(wb, sheet);
        TableEdits.Replace(sheet, t, t.With(columns: t.Columns.Select((c, k) => k == i ? c with { TotalsFunction = function, TotalsLabel = function == null ? c.TotalsLabel : null } : c).ToList()));
        _undo.Set(new CellRef(t.Range.Bottom, t.Range.Left + i), default, formula);
        _undo.Done(false);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => _undo.Revert();
}

internal sealed class HeaderSyncCommand : IWorkbookCommand
{
    private List<TableUndo> _undo = [];

    public string Description => "Rename columns";

    internal bool Changed => _undo.Count > 0;

    public void Apply(Workbook wb)
    {
        _undo = [];
        foreach (var sheet in wb.Sheets)
            foreach (var table in sheet.Tables.Where(t => t.HeaderRow).ToList())
                if (TableEdits.SyncHeader(wb, sheet, table) is { } undo)
                    _undo.Add(undo);
    }

    public void Revert(Workbook wb)
    {
        for (var i = _undo.Count - 1; i >= 0; i--) _undo[i].Revert();
    }
}

internal sealed class TableUndo
{
    private readonly Workbook _wb;
    private readonly Worksheet _sheet;
    private readonly List<(Worksheet Sheet, IReadOnlyList<SheetTable> Tables)> _tables;
    private readonly List<(CellRef Ref, CellState State)> _cells = [];
    private readonly List<CellRange> _ranges = [];
    private FormulaSnapshot _formulas;
    private (AutoFilter Filter, IReadOnlySet<int> Rows)? _filter;
    private bool _structure;

    internal TableUndo(Workbook wb, Worksheet sheet)
    {
        _wb = wb;
        _sheet = sheet;
        _tables = wb.Sheets.Select(s => (s, s.Tables)).ToList();
    }

    internal void Set(CellRef at, CellValue value, string formula = null)
    {
        _cells.Add((at, _sheet.Capture(at)));
        var cell = _sheet.Ensure(at);
        cell.Value = value;
        cell.Formula = formula;
        cell.SpillAnchor = null;
        _sheet.Prune(at);
    }

    internal void Rewrite(Func<string, string, CellRef?, string> rewrite) => _formulas = _wb.RewriteFormulas(rewrite);

    internal void ClearAutoFilter()
    {
        _filter ??= (_sheet.AutoFilter, _sheet.FilteredRows);
        _sheet.AutoFilter = null;
        _sheet.ReapplyFilters();
    }

    internal void Done(bool structure, params CellRange[] ranges)
    {
        if (Worksheet.FiltersLost(_tables.First(p => p.Sheet == _sheet).Tables, _sheet.Tables, true))
        {
            _filter ??= (_sheet.AutoFilter, _sheet.FilteredRows);
            _sheet.ReapplyFilters();
        }

        _structure = structure;
        _ranges.AddRange(ranges);
        Notify();
    }

    internal void Revert()
    {
        if (_formulas != null) _wb.RestoreFormulas(_formulas);
        foreach (var (sheet, tables) in _tables) sheet.Tables = tables;
        for (var i = _cells.Count - 1; i >= 0; i--) _sheet.Restore(_cells[i].Ref, _cells[i].State);
        if (_filter is var (filter, rows))
        {
            _sheet.AutoFilter = filter;
            _sheet.SetFiltered(rows);
        }

        Notify();
    }

    private void Notify()
    {
        _sheet.Modified = true;
        foreach (var (at, _) in _cells) _wb.Notify(_sheet, at);
        foreach (var r in _ranges) _wb.NotifyFormatted(_sheet, r);
        if (_filter != null) _wb.Notify(StructureChangeKind.Layout, _sheet);
        if (_structure) _wb.Notify(StructureChangeKind.DefinedNamesChanged, _sheet);
    }
}

internal static class TableEdits
{
    internal static SheetTable Get(Worksheet sheet, string name) =>
        sheet.Tables.FirstOrDefault(t => t.Matches(name)) ?? throw new ArgumentException($"The sheet has no table named '{name}'.", nameof(name));

    internal static void Replace(Worksheet sheet, SheetTable old, SheetTable now) => sheet.Tables = sheet.Tables.Select(t => t == old ? now : t).ToList();

    internal static void CheckFree(Worksheet sheet, CellRange area, SheetTable ignore, bool autoFilter = true)
    {
        if (sheet.Tables.Any(t => t != ignore && t.Range.Intersects(area))) throw new InvalidOperationException("A table cannot overlap another table.");
        if (autoFilter && sheet.AutoFilter?.Range.Intersects(area) == true) throw new InvalidOperationException("A table cannot overlap the auto filter of the sheet.");
        if (sheet.Merges.Any(m => m.Intersects(area))) throw new InvalidOperationException("A table cannot contain merged cells.");
    }

    internal static void ValidateName(Workbook wb, string name, SheetTable ignore)
    {
        if (!Workbook.IsIdentifier(name))
            throw new ArgumentException($"'{name}' is not a valid table name.", nameof(name));
        if (wb.Tables.Any(t => t != ignore && t.Matches(name)) || wb.DefinedNames.ContainsKey(name))
            throw new ArgumentException($"The name '{name}' is already in use.", nameof(name));
    }

    internal static int NextId(Workbook wb) => wb.Tables.Select(t => t.Id).DefaultIfEmpty(0).Max() + 1;

    internal static string NextName(Workbook wb)
    {
        var i = 1;
        while (wb.FindTable("Table" + i) != null || wb.DefinedNames.ContainsKey("Table" + i)) i++;
        return "Table" + i;
    }

    internal static List<TableColumn> Columns(Worksheet sheet, CellRange area, bool headers, SheetTable keep) =>
        SheetTable.Unique(Enumerable.Range(area.Left, area.ColumnCount).Select(c =>
            keep != null && c >= keep.Range.Left && c <= keep.Range.Right
                ? keep.Columns[c - keep.Range.Left]
                : new TableColumn(headers ? HeaderText(sheet[new CellRef(area.Top, c)]?.Value ?? default) : null)));

    internal static string HeaderText(CellValue value) => value.Kind == CellValueKind.Empty ? null : value.ToString();

    internal static void WriteHeaders(TableUndo undo, Worksheet sheet, SheetTable table)
    {
        if (!table.HeaderRow) return;
        for (var i = 0; i < table.Columns.Count; i++)
        {
            var at = new CellRef(table.Range.Top, table.Range.Left + i);
            if (sheet[at] is { Formula: null, Value.Kind: CellValueKind.Text } cell && cell.Value.TextValue == table.Columns[i].Name) continue;
            undo.Set(at, CellValue.Text(table.Columns[i].Name));
        }
    }

    internal static string Subtotal(string function, string table, string column) =>
        function?.ToLowerInvariant() switch
        {
            "sum" => 109, "average" => 101, "count" => 103, "countnums" => 102, "max" => 104, "min" => 105, "stddev" => 107, "var" => 110, _ => 0
        } is var n and > 0
            ? $"SUBTOTAL({n},{StructuredReferences.Format(new StructuredRefNode(table, column, null, StructuredItem.Data), false)})"
            : null;

    internal static string ToA1(Workbook wb, Worksheet tableSheet, SheetTable table, StructuredRefNode node, string hostName, CellRef? cell)
    {
        var host = hostName == null ? null : wb.FindSheet(hostName);
        if (node.Table == null && cell == null || StructuredReferences.Find(wb, host, cell ?? default, node, out _)?.Matches(table.Name) != true) return null;
        var value = StructuredReferences.Resolve(wb, host, cell ?? default, node);
        if (value.Kind != CalcValueKind.Reference) return "#REF!";
        var range = value.Areas[0].Range;
        var thisRow = node.Items == StructuredItem.ThisRow;
        var start = range.Start with { RowAbsolute = !thisRow, ColumnAbsolute = true };
        var end = range.End with { RowAbsolute = !thisRow, ColumnAbsolute = true };
        return (host == tableSheet ? "" : Workbook.QuoteSheetName(tableSheet.Name) + "!") + new CellRange(start, end).ToA1();
    }

    internal static IWorkbookCommand SyncHeaders(Workbook wb)
    {
        if (wb.Sheets.All(s => s.Tables.Count == 0)) return null;
        var sync = new HeaderSyncCommand();
        sync.Apply(wb);
        return sync.Changed ? sync : null;
    }

    internal static (string[] Names, bool[] Plain) HeaderNames(Worksheet sheet, SheetTable table, Func<CellRef, Cell, CellValue> value)
    {
        var count = table.Columns.Count;
        var names = new string[count];
        var plain = new bool[count];
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < count; i++)
        {
            var name = table.Columns[i].Name;
            plain[i] = sheet[Header(table, i)] is { Formula: null, Value.Kind: CellValueKind.Text } cell && cell.Value.TextValue == name && taken.Add(name);
            if (plain[i]) names[i] = name;
        }

        for (var i = 0; i < count; i++)
        {
            if (plain[i]) continue;
            var at = Header(table, i);
            var v = value(at, sheet[at]);
            var text = v.Kind switch
            {
                CellValueKind.Empty => null,
                CellValueKind.Text => v.TextValue,
                _ => NumberFormat.Format(v, sheet.Workbook.Styles[sheet.EffectiveStyleId(at)].NumberFormat, CultureInfo.CurrentCulture, out _)
            };
            taken.Add(names[i] = SheetTable.UniqueName(text, taken));
        }

        return (names, plain);
    }

    private static CellRef Header(SheetTable table, int column) => new(table.Range.Top, table.Range.Left + column);

    internal static TableUndo SyncHeader(Workbook wb, Worksheet sheet, SheetTable table)
    {
        var (names, plain) = HeaderNames(sheet, table, (at, cell) => cell?.Formula is { } f ? Evaluate(wb, sheet, at, f, cell.Value) : cell?.Value ?? default);
        if (plain.All(p => p)) return null;
        var undo = new TableUndo(wb, sheet);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < names.Length; i++)
        {
            if (!plain[i]) undo.Set(Header(table, i), CellValue.Text(names[i]));
            if (names[i] != table.Columns[i].Name) map[table.Columns[i].Name] = names[i];
        }

        if (map.Count > 0)
        {
            undo.Rewrite((f, host, at) => StructuredReferences.RenameColumns(f,
                node => node.Table != null ? table.Matches(node.Table) : at is { } c && wb.FindSheet(host)?.TableAt(c)?.Matches(table.Name) == true, map));
            var current = Get(sheet, table.Name);
            Replace(sheet, current, current.With(columns: current.Columns.Select((c, k) => c with { Name = names[k] }).ToList()));
        }

        undo.Done(map.Count > 0);
        return undo;
    }

    private static CellValue Evaluate(Workbook wb, Worksheet sheet, CellRef at, string formula, CellValue cached)
    {
        if (!FormulaParser.TryParse(formula, out var ast, out _)) return cached;
        try
        {
            var evaluator = new FormulaEvaluator(FunctionRegistry.Default);
            var scope = new EvalScope(wb, sheet, at);
            return evaluator.ToScalar(evaluator.EvaluateFormula(ast, scope), scope);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return cached;
        }
    }

    internal static Func<string, string, CellRef?, string> DropReferences(Workbook wb, Worksheet sheet, IReadOnlyList<SheetTable> before, Axis axis, int index, int count)
    {
        var gone = new List<(SheetTable Table, CellRange? Range, HashSet<string> Columns)>();
        foreach (var t in before)
        {
            var now = sheet.Tables.FirstOrDefault(x => x.Matches(t.Name));
            if (now == null) gone.Add((t, Worksheet.MoveRange(t.Range, axis, index, -count), null));
            else if (now.Columns.Count != t.Columns.Count)
                gone.Add((t, now.Range, t.Columns.Select(c => c.Name).Except(now.Columns.Select(c => c.Name), StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)));
        }

        if (gone.Count == 0) return null;
        return (formula, host, cell) => gone.Aggregate(formula, (text, g) => StructuredReferences.Replace(text, g.Table.Name, node =>
            (node.Table != null ? g.Table.Matches(node.Table) : cell is { } c && wb.FindSheet(host) == sheet && g.Range?.Contains(c) == true)
            && (g.Columns == null || node.Column1 != null && g.Columns.Contains(node.Column1) || node.Column2 != null && g.Columns.Contains(node.Column2))
                ? "#REF!"
                : null));
    }

    internal static Action OnFormulaSet(Workbook wb, Worksheet sheet, CellRef cell)
    {
        if (sheet[cell]?.Formula is not { } formula || sheet.TableAt(cell) is not { } table || !table.DataRange.Contains(cell)) return null;
        var i = cell.Column - table.Range.Left;
        var data = table.DataRange;
        var column = CellRange.FromBounds(data.Top, cell.Column, data.Bottom, cell.Column);
        if (table.Columns[i].CalculatedFormula == null && sheet.ExistingIn(column).Any(c => c != cell && sheet[c].HasContent)) return null;
        var undo = new TableUndo(wb, sheet);
        for (var row = data.Top; row <= data.Bottom; row++)
            if (row != cell.Row)
                undo.Set(new CellRef(row, cell.Column), default, ReferenceShifter.Translate(formula, row - cell.Row, 0));
        Replace(sheet, table, table.With(columns: table.Columns.Select((c, k) => k == i ? c with { CalculatedFormula = formula } : c).ToList()));
        undo.Done(false);
        return undo.Revert;
    }

    internal static void AfterInsert(Worksheet sheet, Axis axis, int index, int count)
    {
        foreach (var table in sheet.Tables)
        {
            var r = table.Range;
            if (axis == Axis.Column)
            {
                if (table.HeaderRow && index > r.Left && index <= r.Right)
                    for (var c = index; c < index + count && c <= r.Right; c++)
                        Put(sheet, new CellRef(r.Top, c), CellValue.Text(table.Columns[c - r.Left].Name), null);
                continue;
            }

            var data = table.DataRange;
            if (index <= r.Top || index < data.Top || index > data.Bottom) continue;
            for (var i = 0; i < table.Columns.Count; i++)
            {
                if (table.Columns[i].CalculatedFormula is not { } calculated) continue;
                var column = r.Left + i;
                var source = index - 1 >= data.Top ? index - 1 : index + count;
                var formula = sheet[new CellRef(source, column)]?.Formula;
                for (var row = index; row < index + count && row <= data.Bottom; row++)
                    Put(sheet, new CellRef(row, column), default, formula == null ? calculated : ReferenceShifter.Translate(formula, row - source, 0));
            }
        }
    }

    private static void Put(Worksheet sheet, CellRef at, CellValue value, string formula)
    {
        var cell = sheet.Ensure(at);
        cell.Value = value;
        cell.Formula = formula;
        sheet.Sync(at);
    }
}
