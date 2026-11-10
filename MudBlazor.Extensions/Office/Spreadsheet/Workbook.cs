using System.Collections;
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
        IReadOnlyList<StructureChange> structure, IReadOnlyList<RangeChange> valueRanges = null)
    {
        SingleCells = cells;
        ValueRanges = valueRanges ?? [];
        Cells = ValueRanges.Count == 0 ? cells : new ChangedCells(cells, ValueRanges);
        FormattedRanges = ranges;
        Structure = structure;
    }

    /// <summary>
    /// Cells whose value or formula changed through cell commands, including every cell of <see cref="ValueRanges"/> (enumerated on
    /// demand; a cell can appear twice when blocks overlap). Structural changes are reported only in <see cref="Structure"/>; consumers
    /// shift or recompute from there.
    /// </summary>
    public IReadOnlyCollection<CellChange> Cells { get; }

    /// <summary>
    /// Blocks whose values were replaced at once (<see cref="SetValuesCommand"/> and its undo). Their cells are part of <see cref="Cells"/> too;
    /// consumers can handle a block as a whole instead of cell by cell.
    /// </summary>
    public IReadOnlyList<RangeChange> ValueRanges { get; }

    internal IReadOnlyCollection<CellChange> SingleCells { get; }

    private sealed class ChangedCells : IReadOnlyCollection<CellChange>
    {
        private readonly IReadOnlyCollection<CellChange> _cells;
        private readonly IReadOnlyList<RangeChange> _ranges;

        public ChangedCells(IReadOnlyCollection<CellChange> cells, IReadOnlyList<RangeChange> ranges)
        {
            _cells = cells;
            _ranges = ranges;
            Count = (int)Math.Min(int.MaxValue, cells.Count(c => !InRange(c)) + ranges.Sum(r => r.Range.Area));
        }

        public int Count { get; }

        public IEnumerator<CellChange> GetEnumerator()
        {
            foreach (var c in _cells)
                if (!InRange(c))
                    yield return c;
            foreach (var (sheet, range) in _ranges)
                foreach (var c in range.Cells())
                    yield return new CellChange(sheet, c);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private bool InRange(CellChange c)
        {
            foreach (var r in _ranges)
                if (r.Sheet == c.Sheet && r.Range.Contains(c.Cell))
                    return true;
            return false;
        }
    }

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
    private List<RangeChange> _valueRanges = new();
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

    /// <summary>
    /// Maximum number of cells (and of shared strings) <see cref="XlsxReader"/>, <see cref="XlsReader"/> and <see cref="CsvFormat"/>
    /// create for one workbook; a larger file throws <see cref="InvalidDataException"/>. Protects servers from files that would exhaust
    /// memory. Defaults to 10,000,000. Editing is not limited. Set it once at startup.
    /// </summary>
    public static int MaxCells { get; set; } = 10_000_000;

    /// <summary>
    /// True if the source file contains content whose cell references are not moved when rows, columns or sheets change:
    /// hyperlinks, comments, drawings and charts, pivot tables and their
    /// caches, external links, page breaks, ignored errors, protected ranges or extension lists. After inserting or deleting rows or
    /// columns or renaming a sheet these keep pointing at their original cells; editors should warn once.
    /// </summary>
    public bool HasUnshiftedReferences => Package?.HasUnshiftedReferences == true;

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

    /// <summary>All tables of all sheets in tab order.</summary>
    public IReadOnlyList<SheetTable> Tables => _sheets.SelectMany(s => s.Tables).ToList();

    /// <summary>Finds a table by name, ignoring case.</summary>
    /// <param name="name">The table name.</param>
    /// <returns>The table, or null.</returns>
    public SheetTable FindTable(string name) => FindTable(name, out _);

    internal SheetTable FindTable(string name, out Worksheet sheet)
    {
        foreach (var s in _sheets)
            foreach (var t in s.Tables)
                if (t.Matches(name))
                {
                    sheet = s;
                    return t;
                }

        sheet = null;
        return null;
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
        if (--_batchDepth > 0 || (_cells.Count == 0 && _ranges.Count == 0 && _structure.Count == 0 && _valueRanges.Count == 0)) return;
        var args = new WorkbookChangedEventArgs(_cells, _ranges, _structure, _valueRanges);
        _cells = new();
        _ranges = new();
        _valueRanges = new();
        _structure = new();
        Changed?.Invoke(this, args);
    }

    internal void Notify(Worksheet sheet, CellRef cell)
    {
        sheet.Modified = true;
        Package?.Sheets.Find(p => p.Worksheet == sheet)?.ArrayFormulas.Remove(cell.Relative.Key);
        _cells.Add(new CellChange(sheet, cell.Relative));
    }

    internal void NotifyValues(Worksheet sheet, CellRange range)
    {
        sheet.Modified = true;
        if (Package?.Sheets.Find(p => p.Worksheet == sheet) is { } part)
            foreach (var key in part.ArrayFormulas.Keys.Where(k => range.Contains(CellRef.FromKey(k))).ToList())
                part.ArrayFormulas.Remove(key);
        _valueRanges.Add(new RangeChange(sheet, range));
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

    internal FormulaSnapshot RewriteFormulas(Func<string, string, string> rewrite) => RewriteFormulas((f, host, _) => rewrite(f, host));

    internal FormulaSnapshot RewriteFormulas(Func<string, string, CellRef?, string> rewrite)
    {
        var cells = new List<(Worksheet, CellRef, string)>();
        foreach (var sheet in _sheets)
            foreach (var key in sheet.FormulaKeys)
            {
                var r = CellRef.FromKey(key);
                var cell = sheet[r];
                var text = rewrite(cell.Formula, sheet.Name, r);
                if (text == cell.Formula) continue;
                cells.Add((sheet, r, cell.Formula));
                cell.Formula = text;
                sheet.Modified = true;
            }

        List<(Worksheet, IReadOnlyList<ConditionalFormatting>, IReadOnlyList<DataValidation>)> rules = null;
        foreach (var sheet in _sheets)
        {
            var formats = sheet.ConditionalFormats.Select(c => c.Rewrite(f => rewrite(f, sheet.Name, null))).ToList();
            var validations = sheet.DataValidations.Select(d => d.Rewrite(f => rewrite(f, sheet.Name, null))).ToList();
            if (formats.SequenceEqual(sheet.ConditionalFormats, ReferenceEqualityComparer.Instance)
                && validations.SequenceEqual(sheet.DataValidations, ReferenceEqualityComparer.Instance)) continue;
            (rules ??= []).Add((sheet, sheet.ConditionalFormats, sheet.DataValidations));
            sheet.ConditionalFormats = formats;
            sheet.DataValidations = validations;
            sheet.Modified = true;
        }

        List<(Worksheet, IReadOnlyList<SheetTable>)> tables = null;
        foreach (var sheet in _sheets)
        {
            var list = sheet.Tables.Select(t => t.RewriteFormulas(f => rewrite(f, sheet.Name, t.DataRange.Start))).ToList();
            if (list.SequenceEqual(sheet.Tables, ReferenceEqualityComparer.Instance)) continue;
            (tables ??= []).Add((sheet, sheet.Tables));
            sheet.Tables = list;
            sheet.Modified = true;
        }

        List<(XElement, string)> localNames = null;
        foreach (var name in Package?.DefinedNames?.Elements().Where(e => e.Name.LocalName == "definedName" && e.Attribute("localSheetId") != null) ?? [])
        {
            var host = int.TryParse((string)name.Attribute("localSheetId"), out var id) && id >= 0 && id < Package.Sheets.Count
                ? Package.Sheets[id].Worksheet?.Name ?? Package.Sheets[id].Name
                : null;
            var text = rewrite(name.Value, host, null);
            if (text == name.Value) continue;
            (localNames ??= []).Add((name, name.Value));
            name.Value = text;
        }

        Dictionary<string, string> names = null;
        foreach (var (key, value) in _definedNames.ToList())
        {
            var text = rewrite(value, null, null);
            if (text == value) continue;
            names ??= new Dictionary<string, string>(_definedNames, StringComparer.OrdinalIgnoreCase);
            _definedNames[key] = text;
        }

        return new FormulaSnapshot(cells, names, localNames, rules, tables);
    }

    internal void RestoreFormulas(FormulaSnapshot snapshot)
    {
        foreach (var (sheet, r, formula) in snapshot.Cells)
        {
            sheet[r].Formula = formula;
            sheet.Modified = true;
        }

        foreach (var (name, text) in snapshot.LocalNames ?? []) name.Value = text;
        foreach (var (sheet, tables) in snapshot.Tables ?? []) sheet.Tables = tables;
        foreach (var (sheet, formats, validations) in snapshot.Rules ?? [])
        {
            sheet.ConditionalFormats = formats;
            sheet.DataValidations = validations;
            sheet.Modified = true;
        }

        if (snapshot.Names == null) return;
        _definedNames.Clear();
        foreach (var (key, value) in snapshot.Names) _definedNames[key] = value;
    }
}

internal sealed record FormulaSnapshot(
    List<(Worksheet Sheet, CellRef Ref, string Formula)> Cells,
    Dictionary<string, string> Names,
    List<(XElement Name, string Text)> LocalNames = null,
    List<(Worksheet Sheet, IReadOnlyList<ConditionalFormatting> Formats, IReadOnlyList<DataValidation> Validations)> Rules = null,
    List<(Worksheet Sheet, IReadOnlyList<SheetTable> Tables)> Tables = null);
