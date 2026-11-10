namespace MudBlazor.Extensions.Office.Spreadsheet;

public sealed partial class CalcEngine
{
    private bool Apply(StructureChange change)
    {
        var shift = change.Kind is StructureChangeKind.RowsInserted or StructureChangeKind.RowsDeleted or StructureChangeKind.ColumnsInserted
            or StructureChangeKind.ColumnsDeleted;
        if (shift ? !_wb.Sheets.Contains(change.Sheet)
                : change.Kind == StructureChangeKind.SheetAdded && (change.Sheet.FormulaKeys.Count > 0 || change.Sheet.SpillKeys.Count > 0))
            return false;
        var dirty = new HashSet<CellNode>();
        var starts = new List<CellNode>();
        if (shift) Shift(change, dirty, starts);
        Reconcile(change, shift, dirty, starts);
        foreach (var node in _volatile) dirty.Add(node);
        starts.AddRange(dirty);
        Close(dirty, starts);
        Recalculate(dirty);
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

        int Map(int v) => insert ? v >= index ? v + count : v : v < index ? v : v < end ? -1 : v - count;
        (int Start, int End) Span(CellRange r) => rows ? (r.Top, r.Bottom) : (r.Left, r.Right);

        bool Touches(RefArea a)
        {
            if (a.Sheet != sheet) return false;
            var (s, e) = Span(a.Range);
            return insert ? s < index && e >= index || e + count >= limit : e >= index && s < end;
        }

        bool MovesAlong(RefArea a) => a.Sheet == sheet && Span(a.Range).Start >= (insert ? index : end);

        bool Shifted(RefArea a) => a.Sheet == sheet && Span(a.Range).End >= index;

        CellNode? MapNode(CellNode n)
        {
            if (n.Sheet != sheet) return n;
            var c = n.Cell;
            var v = Map(rows ? c.Row : c.Column);
            return v < 0 || v >= limit ? null : new CellNode(sheet, (rows ? new CellRef(v, c.Column) : new CellRef(c.Row, v)).Key);
        }

        CellRange? MapRange(CellRange r)
        {
            var (s, e) = Span(r);
            var ns = insert ? Map(s) : s < index ? s : s >= end ? s - count : index;
            var ne = insert ? Math.Min(Map(e), limit - 1) : e < index ? e : e >= end ? e - count : index - 1;
            if (ne < ns || ns >= limit) return null;
            return rows ? CellRange.FromBounds(ns, r.Left, ne, r.Right) : CellRange.FromBounds(r.Top, ns, r.Bottom, ne);
        }

        var formulas = new Dictionary<CellNode, FormulaEntry>(_formulas.Count);
        var unchanged = new List<(CellNode Node, List<RefArea> Areas)>(_formulas.Count);
        var stale = new List<CellNode>();
        var placed = new List<(CellNode Node, List<RefArea> Areas)>();
        var volatileNodes = new HashSet<CellNode>();
        var keep = new HashSet<CellNode>();
        var keptFormulas = new Dictionary<CellNode, (string, CellValue)>();
        var spills = new Dictionary<CellNode, SpillState>();
        foreach (var (node, entry) in _formulas)
        {
            var areas = _graph.Precedents(node);
            var spilled = _spills.TryGetValue(node, out var spill);
            if (MapNode(node) is not { } target)
            {
                stale.Add(node);
                if (spilled && spill.Active && MapRange(spill.Range) is { } rest)
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
            var same = target.Sheet[target.Cell]?.Formula == entry.Text;
            if (same) unchanged.Add((target, areas));
            if (moved) stale.Add(node);
            if (moved && same) placed.Add((target, areas));
            if (_volatile.Contains(node)) volatileNodes.Add(target);
            if (_keep.Contains(node)) keep.Add(target);
            if (_keptFormulas.TryGetValue(node, out var kept)) keptFormulas[target] = kept;
            if (!spilled) continue;
            if (node.Sheet != sheet) spills[target] = spill;
            else if (MapRange(spill.Range) is { } range) spills[target] = spill with { Range = range };
        }

        _formulas = formulas;
        _volatile = volatileNodes;
        _keep = keep;
        _keptFormulas = keptFormulas;
        _spills = spills;
        if (stale.Count * 2 > formulas.Count)
        {
            _graph.Clear();
            foreach (var (node, areas) in unchanged) _graph.Set(node, areas);
        }
        else
        {
            foreach (var node in stale) _graph.Remove(node);
            foreach (var (node, areas) in placed) _graph.Set(node, areas);
        }

        foreach (var key in sheet.FormulaKeys)
        {
            var node = new CellNode(sheet, key);
            if (_formulas.ContainsKey(node)) continue;
            RegisterOrKeep(node, sheet[CellRef.FromKey(key)]);
            dirty.Add(node);
        }
    }

    private void Reconcile(StructureChange change, bool shift, HashSet<CellNode> dirty, List<CellNode> starts)
    {
        var namesChanged = _names.Count != _wb.DefinedNameMap.Count
                           || _wb.DefinedNameMap.Any(p => !_names.TryGetValue(p.Key, out var text) || text != p.Value);
        if (namesChanged) _names = new Dictionary<string, string>(_wb.DefinedNameMap, StringComparer.OrdinalIgnoreCase);
        var tables = StructuredReferences.Signature(_wb);
        namesChanged |= tables != _tables;
        _tables = tables;
        var sheets = _wb.Sheets.ToHashSet();
        var newName = change.Kind is StructureChangeKind.SheetAdded or StructureChangeKind.SheetRenamed ? change.Sheet.Name : null;
        foreach (var (node, entry) in _formulas.ToList())
        {
            var cell = sheets.Contains(node.Sheet) ? node.Sheet[node.Cell] : null;
            if (cell?.Formula == null)
            {
                Forget(node);
                if (sheets.Contains(node.Sheet)) starts.Add(node);
                continue;
            }

            var textChanged = cell.Formula != entry.Text;
            var names = namesChanged && entry.UsesNames;
            if (!textChanged && !names && (newName == null || !ReferenceShifter.CanReference(entry.Text, newName))) continue;
            _keep.Remove(node);
            _keptFormulas.Remove(node);
            RegisterOrKeep(node, cell);
            if (!shift || names || CountRefErrors(entry.Text) != CountRefErrors(cell.Formula)) dirty.Add(node);
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
        _keep.Remove(node);
        _keptFormulas.Remove(node);
        _spills.Remove(node);
    }
}
