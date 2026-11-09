using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>A changed cell value or formula.</summary>
/// <param name="Sheet">The sheet containing the cell.</param>
/// <param name="Cell">The cell address.</param>
public readonly record struct CellChange(Worksheet Sheet, CellRef Cell);

/// <summary>A range whose formatting changed (no value or formula change).</summary>
/// <param name="Sheet">The sheet containing the range.</param>
/// <param name="Range">The formatted range.</param>
public readonly record struct RangeChange(Worksheet Sheet, CellRange Range);

/// <summary>Kinds of structural workbook changes.</summary>
public enum StructureChangeKind
{
    /// <summary>Rows were inserted; <see cref="StructureChange.Index"/> and <see cref="StructureChange.Count"/> describe them.</summary>
    RowsInserted,

    /// <summary>Rows were deleted.</summary>
    RowsDeleted,

    /// <summary>Columns were inserted.</summary>
    ColumnsInserted,

    /// <summary>Columns were deleted.</summary>
    ColumnsDeleted,

    /// <summary>A sheet was added; <see cref="StructureChange.Index"/> is its position.</summary>
    SheetAdded,

    /// <summary>A sheet was removed; <see cref="StructureChange.Index"/> is its former position.</summary>
    SheetRemoved,

    /// <summary>A sheet was renamed; <see cref="StructureChange.OldName"/> holds the previous name.</summary>
    SheetRenamed,

    /// <summary>A sheet was moved; <see cref="StructureChange.Index"/> is its new position.</summary>
    SheetMoved,

    /// <summary>A defined name was added, changed or removed.</summary>
    DefinedNamesChanged,

    /// <summary>Hidden state, sizes, merges, freeze panes or tab color of a sheet changed.</summary>
    Layout
}

/// <summary>A structural change with enough information to shift references.</summary>
/// <param name="Kind">What happened.</param>
/// <param name="Sheet">The affected sheet (null for defined names).</param>
/// <param name="Index">Zero-based first row, column or sheet position.</param>
/// <param name="Count">Number of rows or columns.</param>
/// <param name="OldName">Previous sheet name for <see cref="StructureChangeKind.SheetRenamed"/>.</param>
public readonly record struct StructureChange(StructureChangeKind Kind, Worksheet Sheet, int Index, int Count, string OldName);

/// <summary>Describes everything that changed during one command, transaction, undo or redo.</summary>
public sealed class WorkbookChangedEventArgs : EventArgs
{
    internal WorkbookChangedEventArgs(IReadOnlyCollection<CellChange> cells, IReadOnlyList<RangeChange> ranges,
        IReadOnlyList<StructureChange> structure)
    {
        Cells = cells;
        FormattedRanges = ranges;
        Structure = structure;
    }

    /// <summary>Cells whose value or formula changed through cell commands. Structural changes are reported only in <see cref="Structure"/>; consumers shift or recompute from there.</summary>
    public IReadOnlyCollection<CellChange> Cells { get; }

    /// <summary>Ranges whose formatting changed.</summary>
    public IReadOnlyList<RangeChange> FormattedRanges { get; }

    /// <summary>Structural changes in the order they happened.</summary>
    public IReadOnlyList<StructureChange> Structure { get; }
}

/// <summary>A workbook of worksheets with shared styles, defined names and an undo stack.</summary>
public sealed class Workbook
{
    private readonly List<Worksheet> _sheets = new();
    private readonly Dictionary<string, string> _definedNames = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<CellChange> _cells = new();
    private List<RangeChange> _ranges = new();
    private List<StructureChange> _structure = new();
    private int _batchDepth;

    internal Workbook() => Undo = new UndoStack(this);

    /// <summary>The sheets in tab order.</summary>
    public IReadOnlyList<Worksheet> Sheets => _sheets;

    /// <summary>Shared cell styles.</summary>
    public StyleTable Styles { get; } = new();

    /// <summary>Defined names mapped to their reference text (for example <c>Sheet1!$A$1:$B$2</c>), case-insensitive.</summary>
    public IReadOnlyDictionary<string, string> DefinedNames => _definedNames;

    /// <summary>The undo and redo history.</summary>
    public UndoStack Undo { get; }

    /// <summary>Raised once after each command, transaction, undo or redo with everything that changed.</summary>
    public event EventHandler<WorkbookChangedEventArgs> Changed;

    /// <summary>Creates a workbook with one empty sheet.</summary>
    /// <param name="firstSheet">Name of the first sheet.</param>
    /// <returns>The new workbook.</returns>
    public static Workbook Create(string firstSheet = "Sheet1")
    {
        var wb = new Workbook();
        ValidateSheetName(firstSheet, null, wb);
        wb._sheets.Add(new Worksheet(firstSheet, wb));
        return wb;
    }

    /// <summary>Finds a sheet by name, ignoring case.</summary>
    /// <param name="name">The sheet name.</param>
    /// <returns>The sheet, or null.</returns>
    public Worksheet FindSheet(string name) =>
        _sheets.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Applies a command and records it as one undo step (or adds it to the open transaction).</summary>
    /// <param name="cmd">The command.</param>
    public void Execute(IWorkbookCommand cmd)
    {
        BeginBatch();
        try
        {
            cmd.Apply(this);
            Undo.Record(cmd);
        }
        finally
        {
            EndBatch();
        }
    }

    internal List<Worksheet> SheetList => _sheets;

    internal XlsxPackage Package { get; set; }

    internal Dictionary<string, string> DefinedNameMap => _definedNames;

    internal void BeginBatch() => _batchDepth++;

    internal void EndBatch()
    {
        if (--_batchDepth > 0 || (_cells.Count == 0 && _ranges.Count == 0 && _structure.Count == 0)) return;
        var args = new WorkbookChangedEventArgs(_cells, _ranges, _structure);
        _cells = new();
        _ranges = new();
        _structure = new();
        Changed?.Invoke(this, args);
    }

    internal void Notify(Worksheet sheet, CellRef cell)
    {
        sheet.Modified = true;
        Package?.Sheets.Find(p => p.Worksheet == sheet)?.ArrayFormulas.Remove(cell.Relative.Key);
        _cells.Add(new CellChange(sheet, cell.Relative));
    }

    internal void NotifyFormatted(Worksheet sheet, CellRange range)
    {
        sheet.Modified = true;
        _ranges.Add(new RangeChange(sheet, range));
    }

    internal void Notify(StructureChangeKind kind, Worksheet sheet, int index = 0, int count = 0, string oldName = null)
    {
        if (sheet != null && kind is not (StructureChangeKind.SheetMoved or StructureChangeKind.SheetRenamed or StructureChangeKind.SheetRemoved
                or StructureChangeKind.DefinedNamesChanged))
            sheet.Modified = true;
        _structure.Add(new StructureChange(kind, sheet, index, count, oldName));
    }

    internal static void ValidateSheetName(string name, Worksheet ignore, Workbook wb)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 31 || name.IndexOfAny(['\\', '/', '?', '*', '[', ']', ':']) >= 0
            || name[0] == '\'' || name[^1] == '\'')
            throw new ArgumentException($"'{name}' is not a valid sheet name.", nameof(name));
        if (wb._sheets.Any(s => s != ignore && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"A sheet named '{name}' already exists.", nameof(name));
    }

    internal static string QuoteSheetName(string name) =>
        !char.IsDigit(name[0]) && name.All(c => char.IsLetterOrDigit(c) || c == '_') && !CellRef.TryParse(name, out _)
            ? name
            : "'" + name.Replace("'", "''") + "'";

    internal FormulaSnapshot RewriteFormulas(Func<string, string, string> rewrite)
    {
        var cells = new List<(Worksheet, CellRef, string)>();
        foreach (var sheet in _sheets)
            foreach (var (r, cell) in sheet.UsedCells())
            {
                if (cell.Formula == null) continue;
                var text = rewrite(cell.Formula, sheet.Name);
                if (text == cell.Formula) continue;
                cells.Add((sheet, r, cell.Formula));
                cell.Formula = text;
                sheet.Modified = true;
            }

        List<(XElement, string)> localNames = null;
        foreach (var name in Package?.DefinedNames?.Elements().Where(e => e.Name.LocalName == "definedName" && e.Attribute("localSheetId") != null) ?? [])
        {
            var host = int.TryParse((string)name.Attribute("localSheetId"), out var id) && id >= 0 && id < Package.Sheets.Count
                ? Package.Sheets[id].Worksheet?.Name ?? Package.Sheets[id].Name
                : null;
            var text = rewrite(name.Value, host);
            if (text == name.Value) continue;
            (localNames ??= []).Add((name, name.Value));
            name.Value = text;
        }

        Dictionary<string, string> names = null;
        foreach (var (key, value) in _definedNames.ToList())
        {
            var text = rewrite(value, null);
            if (text == value) continue;
            names ??= new Dictionary<string, string>(_definedNames, StringComparer.OrdinalIgnoreCase);
            _definedNames[key] = text;
        }

        return new FormulaSnapshot(cells, names, localNames);
    }

    internal void RestoreFormulas(FormulaSnapshot snapshot)
    {
        foreach (var (sheet, r, formula) in snapshot.Cells)
        {
            sheet[r].Formula = formula;
            sheet.Modified = true;
        }

        foreach (var (name, text) in snapshot.LocalNames ?? []) name.Value = text;
        if (snapshot.Names == null) return;
        _definedNames.Clear();
        foreach (var (key, value) in snapshot.Names) _definedNames[key] = value;
    }
}

internal sealed record FormulaSnapshot(
    List<(Worksheet Sheet, CellRef Ref, string Formula)> Cells,
    Dictionary<string, string> Names,
    List<(XElement Name, string Text)> LocalNames = null);
