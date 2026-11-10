namespace MudBlazor.Extensions.Office.Spreadsheet;

public sealed partial class CalcEngine
{
    private static bool IsShift(StructureChange c) => c.Kind is StructureChangeKind.RowsInserted or StructureChangeKind.RowsDeleted
        or StructureChangeKind.ColumnsInserted or StructureChangeKind.ColumnsDeleted;

    private bool Apply(IReadOnlyList<StructureChange> changes, IEnumerable<(Worksheet Sheet, CellRef Cell)> cells)
    {
        if (changes.Any(c => IsShift(c) ? !_wb.Sheets.Contains(c.Sheet)
                : c.Kind == StructureChangeKind.SheetAdded && (c.Sheet.FormulaKeys.Count > 0 || c.Sheet.SpillKeys.Count > 0)))
            return false;
        var dirty = new HashSet<CellNode>();
        var starts = new List<CellNode>();
        var shifted = new HashSet<Worksheet>();
        foreach (var change in changes.Where(IsShift))
        {
            Shift(change, dirty, starts);
            shifted.Add(change.Sheet);
        }

        foreach (var sheet in shifted) RegisterMissing(sheet, dirty);
        Reconcile(changes, dirty, starts);
        if (HiddenChanged()) dirty.UnionWith(_hiddenSensitive);
        starts.AddRange(dirty);
        Changed(cells, dirty, starts);
        Finish(dirty, starts);
        return true;
    }

    private void Shift(StructureChange change, HashSet<CellNode> dirty, List<CellNode> starts)
    {
        var sheet = change.Sheet;
        var rows = change.Kind is StructureChangeKind.RowsInserted or StructureChangeKind.RowsDeleted;
        var insert = change.Kind is StructureChangeKind.RowsInserted or StructureChangeKind.ColumnsInserted;
        var (index, count) = (change.Index, change.Count);
        var end = index + count;
        var limit = rows ? CellRef.MaxRows : CellRef.MaxColumns;

        var shift = new AxisShift(sheet, rows ? Axis.Row : Axis.Column, index, insert ? count : -count);
        (int Start, int End) Span(CellRange r) => rows ? (r.Top, r.Bottom) : (r.Left, r.Right);

        bool Touches(RefArea a)
        {
            if (a.Sheet != sheet) return false;
            var (s, e) = Span(a.Range);
            return insert ? s < index && e >= index || e + count >= limit : e >= index && s < end;
        }

        bool MovesAlong(RefArea a) => a.Sheet == sheet && Span(a.Range).Start >= (insert ? index : end);

        bool Shifted(RefArea a) => a.Sheet == sheet && Span(a.Range).End >= index;

        CellNode? MapNode(CellNode n) => n.Sheet != sheet ? n : shift.Forward(n.Cell) is { } c ? new CellNode(sheet, c.Key) : null;

        var carried = dirty.Select(MapNode).OfType<CellNode>().ToList();
        dirty.Clear();
        dirty.UnionWith(carried);
        carried = starts.Select(MapNode).OfType<CellNode>().ToList();
        starts.Clear();
        starts.AddRange(carried);
        var formulas = new Dictionary<CellNode, FormulaEntry>(_formulas.Count);
        var volatileNodes = new HashSet<CellNode>();
        var hiddenSensitive = new HashSet<CellNode>();
        var keep = new HashSet<CellNode>();
        var keptFormulas = new Dictionary<CellNode, (string, CellValue)>();
        var spills = new Dictionary<CellNode, SpillState>();
        foreach (var (node, entry) in _formulas)
        {
            var areas = _graph.Precedents(node);
            var spilled = _spills.TryGetValue(node, out var spill);
            if (MapNode(node) is not { } target)
            {
                _graph.Remove(node);
                if (spilled && spill.Active && shift.Forward(spill.Range) is { } rest)
                    foreach (var c in rest.Cells())
                        starts.Add(new CellNode(sheet, c.Key));
                continue;
            }

            var moved = target != node;
            if (areas.Any(Touches) || moved && entry.Positional
                                   || areas.Any(a => entry.Positional && Shifted(a) || !a.Range.IsSingleCell && (moved ? !MovesAlong(a) : Shifted(a)))
                                   || spilled && node.Sheet == sheet && Touches(new RefArea(sheet, spill.Range)))
                dirty.Add(target);
            formulas[target] = entry;
            if (_volatile.Contains(node)) volatileNodes.Add(target);
            if (_hiddenSensitive.Contains(node)) hiddenSensitive.Add(target);
            if (_keep.Contains(node)) keep.Add(target);
            if (_keptFormulas.TryGetValue(node, out var kept)) keptFormulas[target] = kept;
            if (!spilled) continue;
            if (node.Sheet != sheet) spills[target] = spill;
            else if (shift.Forward(spill.Range) is { } range) spills[target] = spill with { Range = range };
        }

        _formulas = formulas;
        _volatile = volatileNodes;
        _hiddenSensitive = hiddenSensitive;
        _keep = keep;
        _keptFormulas = keptFormulas;
        _spills = spills;
        _graph.Shift(shift);
    }

    private void RegisterMissing(Worksheet sheet, HashSet<CellNode> dirty)
    {
        foreach (var key in sheet.FormulaKeys)
        {
            var node = new CellNode(sheet, key);
            if (_formulas.ContainsKey(node)) continue;
            RegisterOrKeep(node, sheet[CellRef.FromKey(key)]);
            dirty.Add(node);
        }
    }

    private void Reconcile(IReadOnlyList<StructureChange> changes, HashSet<CellNode> dirty, List<CellNode> starts)
    {
        var shift = changes.All(IsShift);
        var newNames = changes.Where(c => c.Kind is StructureChangeKind.SheetAdded or StructureChangeKind.SheetRenamed).Select(c => c.Sheet.Name).ToList();
        var namesChanged = _names.Count != _wb.DefinedNameMap.Count
                           || _wb.DefinedNameMap.Any(p => !_names.TryGetValue(p.Key, out var text) || text != p.Value);
        if (namesChanged) _names = new Dictionary<string, string>(_wb.DefinedNameMap, StringComparer.OrdinalIgnoreCase);
        var tables = StructuredReferences.Signature(_wb);
        namesChanged |= tables != _tables;
        _tables = tables;
        var sheets = _wb.Sheets.ToHashSet();
        foreach (var (node, entry) in _formulas.ToList())
        {
            var cell = sheets.Contains(node.Sheet) ? node.Sheet[node.Cell] : null;
            if (cell?.Formula == null)
            {
                if (sheets.Contains(node.Sheet))
                {
                    ClearSpill(node);
                    starts.Add(node);
                }

                Forget(node);
                continue;
            }

            var textChanged = cell.Formula != entry.Text;
            var names = namesChanged && entry.UsesNames;
            if (!textChanged && !names && !newNames.Any(n => ReferenceShifter.CanReference(entry.Text, n))) continue;
            var relocated = shift && !names && CountRefErrors(entry.Text) == CountRefErrors(cell.Formula);
            var kept = _keep.Contains(node) || _keptFormulas.ContainsKey(node);
            if (relocated && !kept && Relocate(node, entry, cell.Formula)) continue;
            _keep.Remove(node);
            _keptFormulas.Remove(node);
            RegisterOrKeep(node, cell);
            if (!relocated || !kept) dirty.Add(node);
        }
    }

    private static int CountRefErrors(string formula)
    {
        var n = 0;
        for (var i = formula.IndexOf("#REF!", StringComparison.OrdinalIgnoreCase); i >= 0; i = formula.IndexOf("#REF!", i + 5, StringComparison.OrdinalIgnoreCase)) n++;
        return n;
    }

    private void Forget(CellNode node)
    {
        _formulas.Remove(node);
        _graph.Remove(node);
        _volatile.Remove(node);
        _hiddenSensitive.Remove(node);
        _keep.Remove(node);
        _keptFormulas.Remove(node);
        _spills.Remove(node);
    }
}
