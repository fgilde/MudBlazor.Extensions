namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Shared implementation of inserting rows or columns.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="axis">Rows or columns.</param>
/// <param name="index">Zero-based index at which the new rows or columns appear.</param>
/// <param name="count">How many to insert.</param>
public abstract class InsertCommandBase(Worksheet sheet, Axis axis, int index, int count) : IWorkbookCommand
{
    /// <inheritdoc />
    public string Description => axis == Axis.Row ? "Insert rows" : "Insert columns";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        if (count < 1 || index < 0) throw new ArgumentOutOfRangeException(nameof(count));
        sheet.Insert(axis, index, count);
        wb.Notify(axis == Axis.Row ? StructureChangeKind.RowsInserted : StructureChangeKind.ColumnsInserted, sheet, index, count);
        wb.NotifyShifted(sheet, axis, index);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        sheet.Delete(axis, index, count);
        wb.Notify(axis == Axis.Row ? StructureChangeKind.RowsDeleted : StructureChangeKind.ColumnsDeleted, sheet, index, count);
        wb.NotifyShifted(sheet, axis, index);
    }
}

/// <summary>Shared implementation of deleting rows or columns.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="axis">Rows or columns.</param>
/// <param name="index">Zero-based index of the first row or column to delete.</param>
/// <param name="count">How many to delete.</param>
public abstract class DeleteCommandBase(Worksheet sheet, Axis axis, int index, int count) : IWorkbookCommand
{
    private DeleteSnapshot _snapshot;

    /// <inheritdoc />
    public string Description => axis == Axis.Row ? "Delete rows" : "Delete columns";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        if (count < 1 || index < 0) throw new ArgumentOutOfRangeException(nameof(count));
        _snapshot = sheet.Delete(axis, index, count);
        wb.Notify(axis == Axis.Row ? StructureChangeKind.RowsDeleted : StructureChangeKind.ColumnsDeleted, sheet, index, count);
        foreach (var (r, _) in _snapshot.Cells) wb.Notify(sheet, r);
        wb.NotifyShifted(sheet, axis, index);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        sheet.RestoreDeleted(axis, index, count, _snapshot);
        wb.Notify(axis == Axis.Row ? StructureChangeKind.RowsInserted : StructureChangeKind.ColumnsInserted, sheet, index, count);
        foreach (var (r, _) in _snapshot.Cells) wb.Notify(sheet, r);
        wb.NotifyShifted(sheet, axis, index);
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

/// <summary>Sets the width of columns or height of rows; a null size restores the default.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="axis">Rows (height) or columns (width).</param>
/// <param name="index">Zero-based first index.</param>
/// <param name="count">Number of rows or columns.</param>
/// <param name="size">Size in pixels, or null for the default.</param>
public sealed class ResizeCommand(Worksheet sheet, Axis axis, int index, int count, double? size) : IWorkbookCommand
{
    private readonly List<(int Index, double? Size)> _before = new();

    /// <inheritdoc />
    public string Description => axis == Axis.Row ? "Resize rows" : "Resize columns";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        _before.Clear();
        var fallback = axis == Axis.Row ? Worksheet.DefaultRowHeight : Worksheet.DefaultColumnWidth;
        var target = size is { } s && s != fallback ? s : (double?)null;
        for (var i = index; i < index + count; i++)
        {
            _before.Add((i, sheet.SizeOrNull(axis, i)));
            sheet.SetSize(axis, i, target);
        }

        wb.Notify(StructureChangeKind.Layout, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        foreach (var (i, old) in _before) sheet.SetSize(axis, i, old);
        wb.Notify(StructureChangeKind.Layout, sheet);
    }
}

/// <summary>Merges a range; existing merges that overlap it are replaced. Cell data is not modified.</summary>
/// <param name="sheet">The sheet.</param>
/// <param name="range">The range to merge.</param>
public sealed class MergeCommand(Worksheet sheet, CellRange range) : IWorkbookCommand
{
    private List<CellRange> _before;

    /// <inheritdoc />
    public string Description => "Merge cells";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        if (range.IsSingleCell) throw new ArgumentException("A merge needs at least two cells.", nameof(range));
        _before = sheet.CopyMerges();
        sheet.ReplaceMerges(_before.Where(m => !m.Intersects(range)).Append(range.Normalized));
        wb.Notify(StructureChangeKind.Layout, sheet);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        sheet.ReplaceMerges(_before);
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
        _sheet ??= new Worksheet(name);
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

/// <summary>Deletes a sheet; the last remaining sheet cannot be deleted.</summary>
/// <param name="sheet">The sheet to delete.</param>
public sealed class DeleteSheetCommand(Worksheet sheet) : IWorkbookCommand
{
    private int _position;

    /// <inheritdoc />
    public string Description => "Delete sheet";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        if (wb.SheetList.Count < 2) throw new InvalidOperationException("A workbook needs at least one sheet.");
        _position = wb.SheetList.IndexOf(sheet);
        wb.SheetList.RemoveAt(_position);
        wb.Notify(StructureChangeKind.SheetRemoved, sheet, _position);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        wb.SheetList.Insert(_position, sheet);
        wb.Notify(StructureChangeKind.SheetAdded, sheet, _position);
    }
}

/// <summary>Renames a sheet and updates sheet references inside defined names. Formula references are handled by the engine.</summary>
/// <param name="sheet">The sheet to rename.</param>
/// <param name="newName">The new name.</param>
public sealed class RenameSheetCommand(Worksheet sheet, string newName) : IWorkbookCommand
{
    private string _oldName;
    private Dictionary<string, string> _names;

    /// <inheritdoc />
    public string Description => "Rename sheet";

    /// <inheritdoc />
    public void Apply(Workbook wb)
    {
        Workbook.ValidateSheetName(newName, sheet, wb);
        _oldName = sheet.Name;
        _names = new Dictionary<string, string>(wb.DefinedNameMap, StringComparer.OrdinalIgnoreCase);
        sheet.Name = newName;
        wb.RenameInDefinedNames(_oldName, newName);
        wb.Notify(StructureChangeKind.SheetRenamed, sheet, oldName: _oldName);
    }

    /// <inheritdoc />
    public void Revert(Workbook wb)
    {
        sheet.Name = _oldName;
        wb.DefinedNameMap.Clear();
        foreach (var (key, value) in _names) wb.DefinedNameMap[key] = value;
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
