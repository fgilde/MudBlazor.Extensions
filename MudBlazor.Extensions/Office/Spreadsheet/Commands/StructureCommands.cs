namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Shared implementation of inserting rows or columns; references in all formulas and defined names move along.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="axis">Rows or columns.</param>
/// <param name="index">Zero-based index at which the new rows or columns appear.</param>
/// <param name="count">How many to insert.</param>
public abstract class InsertCommandBase(Worksheet sheet, Axis axis, int index, int count) : IWorkbookCommand
{
    private List<(CellRef Ref, CellState State)> _dropped = new();
    private FormulaSnapshot _formulas;

    /// <inheritdoc />
    public string Description => axis == Axis.Row ? "Insert rows" : "Insert columns";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        var limit = AxisLimit(axis);
        if (count < 1 || count > limit) throw new ArgumentOutOfRangeException(nameof(count));
        if (index < 0 || index >= limit) throw new ArgumentOutOfRangeException(nameof(index));
        _dropped = sheet.Insert(axis, index, count);
        _formulas = wb.RewriteFormulas((f, host) => ReferenceShifter.Shift(f, axis, sheet.Name, index, count, host));
        wb.Notify(axis == Axis.Row ? StructureChangeKind.RowsInserted : StructureChangeKind.ColumnsInserted, sheet, index, count);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        wb.RestoreFormulas(_formulas);
        sheet.Delete(axis, index, count);
        foreach (var (r, state) in _dropped) sheet.Restore(r, state);
        wb.Notify(axis == Axis.Row ? StructureChangeKind.RowsDeleted : StructureChangeKind.ColumnsDeleted, sheet, index, count);
    }

    internal static int AxisLimit(Axis axis) => axis == Axis.Row ? CellRef.MaxRows : CellRef.MaxColumns;
}

/// <summary>Shared implementation of deleting rows or columns; references in all formulas and defined names move along and references to deleted cells become <c>#REF!</c>.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="axis">Rows or columns.</param>
/// <param name="index">Zero-based index of the first row or column to delete.</param>
/// <param name="count">How many to delete.</param>
public abstract class DeleteCommandBase(Worksheet sheet, Axis axis, int index, int count) : IWorkbookCommand
{
    private DeleteSnapshot _snapshot;
    private FormulaSnapshot _formulas;

    /// <inheritdoc />
    public string Description => axis == Axis.Row ? "Delete rows" : "Delete columns";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        Check(axis, index, count);
        _snapshot = sheet.Delete(axis, index, count);
        _formulas = wb.RewriteFormulas((f, host) => ReferenceShifter.Shift(f, axis, sheet.Name, index, -count, host));
        wb.Notify(axis == Axis.Row ? StructureChangeKind.RowsDeleted : StructureChangeKind.ColumnsDeleted, sheet, index, count);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        wb.RestoreFormulas(_formulas);
        sheet.RestoreDeleted(axis, index, count, _snapshot);
        wb.Notify(axis == Axis.Row ? StructureChangeKind.RowsInserted : StructureChangeKind.ColumnsInserted, sheet, index, count);
    }

    internal static void Check(Axis axis, int index, int count)
    {
        if (index < 0 || count < 1 || (long)index + count > InsertCommandBase.AxisLimit(axis))
            throw new ArgumentOutOfRangeException(nameof(count), "The rows or columns lie outside the worksheet limits.");
    }
}

/// <summary>Inserts rows; existing rows at and below the index move down.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="index">Zero-based row index at which the new rows appear.</param>
/// <param name="count">How many rows to insert.</param>
public sealed class InsertRowsCommand(Worksheet sheet, int index, int count) : InsertCommandBase(sheet, Axis.Row, index, count);

/// <summary>Inserts columns; existing columns at and right of the index move right.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="index">Zero-based column index at which the new columns appear.</param>
/// <param name="count">How many columns to insert.</param>
public sealed class InsertColumnsCommand(Worksheet sheet, int index, int count) : InsertCommandBase(sheet, Axis.Column, index, count);

/// <summary>Deletes rows together with their cells; rows below move up.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="index">Zero-based index of the first row to delete.</param>
/// <param name="count">How many rows to delete.</param>
public sealed class DeleteRowsCommand(Worksheet sheet, int index, int count) : DeleteCommandBase(sheet, Axis.Row, index, count);

/// <summary>Deletes columns together with their cells; columns to the right move left.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="index">Zero-based index of the first column to delete.</param>
/// <param name="count">How many columns to delete.</param>
public sealed class DeleteColumnsCommand(Worksheet sheet, int index, int count) : DeleteCommandBase(sheet, Axis.Column, index, count);

/// <summary>Hides or unhides a run of rows or columns.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="axis">Rows or columns.</param>
/// <param name="index">Zero-based first index.</param>
/// <param name="count">Number of rows or columns.</param>
/// <param name="hidden">True to hide, false to unhide.</param>
public sealed class SetHiddenCommand(Worksheet sheet, Axis axis, int index, int count, bool hidden) : IWorkbookCommand
{
    private readonly List<int> _changed = new();

    /// <inheritdoc />
    public string Description => (hidden ? "Hide " : "Unhide ") + (axis == Axis.Row ? "rows" : "columns");

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        DeleteCommandBase.Check(axis, index, count);
        _changed.Clear();
        for (var i = index; i < index + count; i++)
        {
            if ((axis == Axis.Row ? sheet.IsRowHidden(i) : sheet.IsColumnHidden(i)) == hidden) continue;
            _changed.Add(i);
            sheet.SetHidden(axis, i, hidden);
        }

        wb.Notify(StructureChangeKind.Layout, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        foreach (var i in _changed) sheet.SetHidden(axis, i, !hidden);
        wb.Notify(StructureChangeKind.Layout, sheet);
    }
}

/// <summary>
/// Sets the width of columns or height of rows; a null size restores the standard. Resizing a whole axis changes the
/// sheet default instead of storing one entry per index.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="axis">Rows (height) or columns (width).</param>
/// <param name="index">Zero-based first index.</param>
/// <param name="count">Number of rows or columns.</param>
/// <param name="size">Size in pixels, or null for the standard size.</param>
public sealed class ResizeCommand(Worksheet sheet, Axis axis, int index, int count, double? size) : IWorkbookCommand
{
    private readonly List<(int Index, double? Size)> _before = new();
    private List<KeyValuePair<int, double>> _entriesBefore;
    private double _defaultBefore;

    /// <inheritdoc />
    public string Description => axis == Axis.Row ? "Resize rows" : "Resize columns";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        DeleteCommandBase.Check(axis, index, count);
        _before.Clear();
        _entriesBefore = null;
        var standard = axis == Axis.Row ? Worksheet.StandardRowHeight : Worksheet.StandardColumnWidth;
        if (index == 0 && count == InsertCommandBase.AxisLimit(axis))
        {
            _entriesBefore = sheet.SizeEntries(axis);
            _defaultBefore = sheet.DefaultSize(axis);
            sheet.ReplaceSizes(axis, []);
            sheet.SetDefaultSize(axis, size ?? standard);
        }
        else
        {
            var target = size is { } s && s != sheet.DefaultSize(axis) ? s : (double?)null;
            for (var i = index; i < index + count; i++)
            {
                _before.Add((i, sheet.SizeOrNull(axis, i)));
                sheet.SetSize(axis, i, target);
            }
        }

        wb.Notify(StructureChangeKind.Layout, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        if (_entriesBefore != null)
        {
            sheet.ReplaceSizes(axis, _entriesBefore);
            sheet.SetDefaultSize(axis, _defaultBefore);
        }
        else
        {
            foreach (var (i, old) in _before) sheet.SetSize(axis, i, old);
        }

        wb.Notify(StructureChangeKind.Layout, sheet);
    }
}

/// <summary>
/// Merges a range; existing merges that overlap it are replaced. Like Excel, only the top-left value and formula are kept.
/// </summary>
/// <param name="sheet">The sheet.</param>
/// <param name="range">The range to merge.</param>
public sealed class MergeCommand(Worksheet sheet, CellRange range) : IWorkbookCommand
{
    private List<CellRange> _before;
    private readonly List<(CellRef Ref, CellState State)> _cleared = new();

    /// <inheritdoc />
    public string Description => "Merge cells";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        Worksheet.Check(range);
        if (range.IsSingleCell) throw new ArgumentException("A merge needs at least two cells.", nameof(range));
        _before = sheet.CopyMerges();
        _cleared.Clear();
        foreach (var c in sheet.ExistingIn(range))
        {
            if (c.Row == range.Top && c.Column == range.Left) continue;
            var state = sheet.Capture(c);
            if (state.Value.Kind == CellValueKind.Empty && state.Formula == null) continue;
            _cleared.Add((c, state));
            sheet.Restore(c, new CellState(default, null, state.StyleId));
            wb.Notify(sheet, c);
        }

        sheet.ReplaceMerges(_before.Where(m => !m.Intersects(range)).Append(range.Normalized));
        wb.Notify(StructureChangeKind.Layout, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        sheet.ReplaceMerges(_before);
        foreach (var (c, state) in _cleared)
        {
            sheet.Restore(c, state);
            wb.Notify(sheet, c);
        }

        wb.Notify(StructureChangeKind.Layout, sheet);
    }
}

/// <summary>Removes every merge that intersects a range.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="range">The range to unmerge.</param>
public sealed class UnmergeCommand(Worksheet sheet, CellRange range) : IWorkbookCommand
{
    private List<CellRange> _before;

    /// <inheritdoc />
    public string Description => "Unmerge cells";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        Worksheet.Check(range);
        _before = sheet.CopyMerges();
        sheet.ReplaceMerges(_before.Where(m => !m.Intersects(range)));
        wb.Notify(StructureChangeKind.Layout, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        sheet.ReplaceMerges(_before);
        wb.Notify(StructureChangeKind.Layout, sheet);
    }
}

/// <summary>Freezes the panes above and left of a cell, or removes the freeze when the cell is null or A1.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="at">First scrolling cell, or null.</param>
public sealed class FreezeCommand(Worksheet sheet, CellRef? at) : IWorkbookCommand
{
    private CellRef? _before;

    /// <inheritdoc />
    public string Description => "Freeze panes";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        if (at is { } a) Worksheet.Check(a);
        _before = sheet.FreezeAt;
        sheet.FreezeAt = at is { Row: 0, Column: 0 } or null ? null : at.Value.Relative;
        wb.Notify(StructureChangeKind.Layout, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        sheet.FreezeAt = _before;
        wb.Notify(StructureChangeKind.Layout, sheet);
    }
}

/// <summary>Adds a new empty sheet. After execution <see cref="Sheet"/> holds it.</summary>
/// <param name="name">Name of the new sheet.</param>
/// <param name="index">Tab position, or -1 to append.</param>
public sealed class AddSheetCommand(string name, int index = -1) : IWorkbookCommand
{
    private Worksheet _sheet;
    private int _position;

    /// <summary>The created sheet, available after the command was applied.</summary>
    public Worksheet Sheet => _sheet;

    /// <inheritdoc />
    public string Description => "Add sheet";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        Workbook.ValidateSheetName(name, null, wb);
        _sheet ??= new Worksheet(name, wb);
        _position = index < 0 || index > wb.SheetList.Count ? wb.SheetList.Count : index;
        wb.SheetList.Insert(_position, _sheet);
        wb.Notify(StructureChangeKind.SheetAdded, _sheet, _position);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        wb.SheetList.Remove(_sheet);
        wb.Notify(StructureChangeKind.SheetRemoved, _sheet, _position);
    }
}

/// <summary>Deletes a sheet; references to it in formulas and defined names become <c>#REF!</c>. The last visible sheet cannot be deleted.</summary>
/// <param name="sheet">The sheet to delete.</param>
public sealed class DeleteSheetCommand(Worksheet sheet) : IWorkbookCommand
{
    private int _position;
    private FormulaSnapshot _formulas;

    /// <inheritdoc />
    public string Description => "Delete sheet";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        _position = wb.SheetList.IndexOf(sheet);
        if (_position < 0) throw new InvalidOperationException("The sheet does not belong to this workbook.");
        if (!wb.SheetList.Any(x => x != sheet && !x.Hidden))
            throw new InvalidOperationException("A workbook needs at least one visible sheet.");
        wb.SheetList.RemoveAt(_position);
        _formulas = wb.RewriteFormulas((f, _) => ReferenceShifter.RemoveSheet(f, sheet.Name));
        wb.Notify(StructureChangeKind.SheetRemoved, sheet, _position);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        wb.RestoreFormulas(_formulas);
        wb.SheetList.Insert(_position, sheet);
        wb.Notify(StructureChangeKind.SheetAdded, sheet, _position);
    }
}

/// <summary>Renames a sheet and updates sheet references in all formulas and defined names.</summary>
/// <param name="sheet">The sheet to rename.</param>
/// <param name="newName">The new name.</param>
public sealed class RenameSheetCommand(Worksheet sheet, string newName) : IWorkbookCommand
{
    private string _oldName;
    private FormulaSnapshot _formulas;

    /// <inheritdoc />
    public string Description => "Rename sheet";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        Workbook.ValidateSheetName(newName, sheet, wb);
        _oldName = sheet.Name;
        sheet.Name = newName;
        _formulas = wb.RewriteFormulas((f, _) => ReferenceShifter.RenameSheet(f, _oldName, newName));
        wb.Notify(StructureChangeKind.SheetRenamed, sheet, oldName: _oldName);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        sheet.Name = _oldName;
        wb.RestoreFormulas(_formulas);
        wb.Notify(StructureChangeKind.SheetRenamed, sheet, oldName: newName);
    }
}

/// <summary>Moves a sheet to another tab position.</summary>
/// <param name="sheet">The sheet to move.</param>
/// <param name="newIndex">The target position.</param>
public sealed class MoveSheetCommand(Worksheet sheet, int newIndex) : IWorkbookCommand
{
    private int _oldIndex;

    /// <inheritdoc />
    public string Description => "Move sheet";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        _oldIndex = wb.SheetList.IndexOf(sheet);
        MoveTo(wb, Math.Clamp(newIndex, 0, wb.SheetList.Count - 1));
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => MoveTo(wb, _oldIndex);

    private void MoveTo(Workbook wb, int index)
    {
        wb.SheetList.Remove(sheet);
        wb.SheetList.Insert(index, sheet);
        wb.Notify(StructureChangeKind.SheetMoved, sheet, index);
    }
}

/// <summary>Sets the tab color of a sheet.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="color">Color as #RRGGBB, or null to clear.</param>
public sealed class SetTabColorCommand(Worksheet sheet, string color) : IWorkbookCommand
{
    private string _before;

    /// <inheritdoc />
    public string Description => "Tab color";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        _before = sheet.TabColor;
        sheet.TabColor = color;
        wb.Notify(StructureChangeKind.Layout, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        sheet.TabColor = _before;
        wb.Notify(StructureChangeKind.Layout, sheet);
    }
}

/// <summary>Adds, changes or (with a null reference) removes a defined name.</summary>
/// <param name="name">The name.</param>
/// <param name="refersTo">Reference text such as <c>Sheet1!$A$1:$B$2</c>, or null to remove the name.</param>
public sealed class DefineNameCommand(string name, string refersTo) : IWorkbookCommand
{
    private string _before;

    /// <inheritdoc />
    public string Description => "Define name";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A name is required.", nameof(name));
        _before = wb.DefinedNameMap.GetValueOrDefault(name);
        Set(wb, refersTo);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb) => Set(wb, _before);

    private void Set(Workbook wb, string value)
    {
        if (value == null) wb.DefinedNameMap.Remove(name);
        else wb.DefinedNameMap[name] = value;
        wb.Notify(StructureChangeKind.DefinedNamesChanged, null);
    }
}
