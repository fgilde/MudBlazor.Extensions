using System.Globalization;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Cells whose calculated value changed during one calculation.</summary>
/// <param name="cells">The formula cells that were evaluated and the spill cells that were filled or cleared.</param>
public sealed class CalculatedEventArgs(IReadOnlyCollection<CellChange> cells) : EventArgs
{
    /// <summary>The formula cells that were evaluated and the spill cells that were filled or cleared.</summary>
    public IReadOnlyCollection<CellChange> Cells { get; } = cells;
}

/// <summary>
/// Calculates the formulas of a workbook and keeps their results in <see cref="Cell.Value"/>. Cell edits recalculate only
/// the dependent cells in dependency order. Inserting or deleting rows or columns, adding, removing or renaming a sheet and
/// changing defined names recalculate only the formulas whose result can change (and their dependents); formulas whose
/// references just moved along keep their value. Parsed formulas are cached by their text. Cells on a
/// circular reference get <see cref="CellError.Circ"/>. Formulas that cannot be evaluated here (unknown functions or syntax the
/// parser does not support) keep the value they had when the engine attached or the graph was rebuilt, together with the cells of
/// their array result, until the formula itself is edited. Array results spill into the cells below and to the right, which
/// then hold computed values with <see cref="Cell.SpillAnchor"/> set; a non-empty cell in the way yields <see cref="CellError.Spill"/>.
/// Formulas calling volatile functions recalculate on every calculation. A formula that fails unexpectedly yields <see cref="CellError.Value"/>.
/// Dispose the engine to stop following the workbook.
/// </summary>
public sealed class CalcEngine : IDisposable
{
    private const int MaxSpillCells = 1_000_000;
    private const int MaxSpillPasses = 16;
    private const int BulkCells = 4096;

    private readonly Workbook _wb;
    private readonly FormulaEvaluator _evaluator;
    private static readonly FormulaNode Unparsable = new ErrorNode(CellError.Name);

    private readonly DependencyGraph _graph = new();
    private readonly Dictionary<string, FormulaNode> _parsed = new(StringComparer.Ordinal);
    private Dictionary<CellNode, FormulaEntry> _formulas = new();
    private HashSet<CellNode> _volatile = new();
    private HashSet<CellNode> _keep = new();
    private Dictionary<CellNode, (string Formula, CellValue Value)> _keptFormulas = new();
    private readonly Dictionary<(Worksheet Sheet, long Key), CellState> _before = new();
    private Dictionary<CellNode, SpillState> _spills = new();
    private Dictionary<string, string> _names = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<CellChange> _changed = new();
    private readonly List<CellNode> _grown = new();
    private readonly List<CellNode> _freed = new();

    /// <summary>Attaches an engine to a workbook, calculates all formulas and follows <see cref="Workbook.Changed"/> from then on.</summary>
    /// <param name="wb">The workbook.</param>
    /// <param name="functions">The functions to use, or null for <see cref="FunctionRegistry.Default"/>.</param>
    public CalcEngine(Workbook wb, FunctionRegistry functions = null)
    {
        _wb = wb;
        _evaluator = new FormulaEvaluator(functions ?? FunctionRegistry.Default) { SpillRange = SpillRange };
        wb.Changed += OnWorkbookChanged;
        RecalculateAll();
    }

    /// <summary>Raised after each calculation with the cells whose calculated value may have changed.</summary>
    public event EventHandler<CalculatedEventArgs> Calculated;

    /// <summary>The functions available to formulas.</summary>
    public FunctionRegistry Functions => _evaluator.Functions;

    /// <summary>
    /// Culture for all conversions between numbers and text in formulas (text in arithmetic, the &amp; operator, criteria,
    /// TEXT, VALUE, NUMBERVALUE, DATEVALUE and TIMEVALUE); null (the default) uses <see cref="CultureInfo.CurrentCulture"/>.
    /// Takes effect on the next calculation.
    /// </summary>
    public CultureInfo Culture
    {
        get => _evaluator.Culture;
        set => _evaluator.Culture = value;
    }

    /// <summary>Number of formula evaluations performed so far.</summary>
    public long EvaluationCount { get; private set; }

    /// <summary>The range filled by the array result of a formula cell, or null if the cell does not spill (or is blocked).</summary>
    /// <param name="sheet">The sheet.</param>
    /// <param name="anchor">The formula cell.</param>
    /// <returns>The spill range including the formula cell.</returns>
    public CellRange? SpillRange(Worksheet sheet, CellRef anchor) =>
        _spills.TryGetValue(new CellNode(sheet, anchor.Relative.Key), out var s) && s.Active ? s.Range : null;

    /// <summary>Rebuilds the dependency graph from all formulas and calculates every formula.</summary>
    public void RecalculateAll()
    {
        _graph.Clear();
        _formulas.Clear();
        _volatile.Clear();
        _spills.Clear();
        _keep.Clear();
        _keptFormulas.Clear();
        _names = new Dictionary<string, string>(_wb.DefinedNameMap, StringComparer.OrdinalIgnoreCase);
        foreach (var sheet in _wb.Sheets)
            foreach (var key in sheet.FormulaKeys)
                RegisterOrKeep(new CellNode(sheet, key), sheet[CellRef.FromKey(key)]);

        var kept = new Dictionary<CellNode, CellRange>();
        foreach (var sheet in _wb.Sheets)
        foreach (var key in sheet.SpillKeys.ToArray())
        {
            var r = CellRef.FromKey(key);
            if (sheet[r]?.SpillAnchor is not { } a) continue;
            var anchor = new CellNode(sheet, a.Relative.Key);
            if (!_keep.Contains(anchor))
            {
                ClearSpillCell(sheet, r);
                continue;
            }

            var range = kept.TryGetValue(anchor, out var k) ? k : new CellRange(a.Relative, a.Relative);
            kept[anchor] = CellRange.FromBounds(Math.Min(range.Top, r.Row), Math.Min(range.Left, r.Column), Math.Max(range.Bottom, r.Row),
                Math.Max(range.Right, r.Column));
        }

        foreach (var (anchor, range) in kept) _spills[anchor] = new SpillState(range, true);
        Recalculate([.. _formulas.Keys.Where(n => !_keep.Contains(n))]);
    }

    /// <summary>Updates the graph for changed cells (values or formulas) and recalculates the cells that depend on them.</summary>
    /// <param name="changed">The changed cells.</param>
    public void OnCellsChanged(IEnumerable<(Worksheet Sheet, CellRef Cell)> changed)
    {
        var starts = new List<CellNode>();
        var affected = new HashSet<CellNode>();
        Changed(changed, affected, starts);
        Finish(affected, starts);
    }

    private void OnBlocksChanged(WorkbookChangedEventArgs e)
    {
        var starts = new List<CellNode>();
        var affected = new HashSet<CellNode>();
        Changed(e.SingleCells.Where(c => !e.ValueRanges.Any(r => r.Sheet == c.Sheet && r.Range.Contains(c.Cell))).Select(c => (c.Sheet, c.Cell)), affected, starts);
        foreach (var (sheet, range) in e.ValueRanges)
        {
            if (!_wb.Sheets.Contains(sheet)) continue;
            var cells = sheet.FormulaKeys.Concat(sheet.SpillKeys).Select(CellRef.FromKey).Where(range.Contains).ToHashSet();
            cells.UnionWith(_formulas.Keys.Where(n => n.Sheet == sheet && range.Contains(n.Cell)).Select(n => n.Cell));
            Changed(cells.Select(c => (sheet, c)), affected, starts);

            foreach (var (anchor, spill) in _spills)
                if (anchor.Sheet == sheet && spill.Range.Intersects(range) && affected.Add(anchor))
                    starts.Add(anchor);
            foreach (var (node, _) in _formulas)
                if (_graph.Precedents(node).Any(a => a.Sheet == sheet && a.Range.Intersects(range)) && affected.Add(node))
                    starts.Add(node);
        }

        Finish(affected, starts);
    }

    private void Finish(HashSet<CellNode> affected, List<CellNode> starts)
    {
        foreach (var node in _volatile)
            if (affected.Add(node))
                starts.Add(node);
        Close(affected, starts);
        Recalculate(affected);
    }

    private void Changed(IEnumerable<(Worksheet Sheet, CellRef Cell)> changed, HashSet<CellNode> affected, List<CellNode> starts)
    {
        foreach (var (sheet, cell) in changed)
        {
            if (!_wb.Sheets.Contains(sheet)) continue;
            var node = new CellNode(sheet, cell.Relative.Key);
            var stored = sheet[cell];
            _keep.Remove(node);
            if (stored?.Formula is { } formula)
            {
                if (!Register(node, formula) && stored.Value.Kind != CellValueKind.Empty && _keptFormulas.GetValueOrDefault(node) == (formula, stored.Value)) _keep.Add(node);
                else affected.Add(node);
            }
            else
            {
                _formulas.Remove(node);
                _graph.Remove(node);
                _volatile.Remove(node);
                _keptFormulas.Remove(node);
                ClearSpill(node);
                if (stored?.SpillAnchor is { } anchor && !SpillCovers(new CellNode(sheet, anchor.Key), node.Cell))
                    ClearSpillCell(sheet, node.Cell);
            }

            starts.Add(node);
            AddAnchorsOver(node, affected, starts);
        }
    }

    private void OnWorkbookChanged(object sender, WorkbookChangedEventArgs e)
    {
        var structure = e.Structure.Where(s => s.Kind is not (StructureChangeKind.Layout or StructureChangeKind.SheetMoved)).ToList();
        if (structure.Count == 0)
        {
            if (e.ValueRanges.Sum(r => (long)r.Range.Area) > BulkCells) OnBlocksChanged(e);
            else if (e.Cells.Count > 0) OnCellsChanged(e.Cells.Select(c => (c.Sheet, c.Cell)));
        }
        else if (structure.Count > 1 || e.Cells.Count > 0 || !Apply(structure[0]))
            RecalculateAll();
    }

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
            if (areas.Any(Touches) || moved && (entry.Positional || areas.Any(a => !a.Range.IsSingleCell && !MovesAlong(a)))
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

    private void RegisterOrKeep(CellNode node, Cell cell)
    {
        if (Register(node, cell.Formula) || cell.Value.Kind == CellValueKind.Empty) return;
        _keep.Add(node);
        _keptFormulas[node] = (cell.Formula, cell.Value);
    }

    private bool Register(CellNode node, string formula)
    {
        if (!_parsed.TryGetValue(formula, out var ast))
        {
            if (_parsed.Count > 2 * _formulas.Count + 1024)
            {
                _parsed.Clear();
                foreach (var entry in _formulas.Values) _parsed[entry.Text] = entry.Ast;
            }

            _parsed[formula] = ast = FormulaParser.TryParse(formula, out var parsed, out _) ? parsed : Unparsable;
        }

        var precedents = new List<RefArea>();
        var info = Collect(ast, node.Sheet, precedents);
        _formulas[node] = new FormulaEntry(formula, ast, info.Positional, info.Names);
        if (info.Volatile) _volatile.Add(node);
        else _volatile.Remove(node);
        _graph.Set(node, precedents);
        return ast != Unparsable && !info.Unknown;
    }

    private (bool Volatile, bool Unknown, bool Positional, bool Names) Collect(FormulaNode ast, Worksheet host, List<RefArea> into)
    {
        bool isVolatile = false, unknown = false, positional = false, names = false;
        var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<(FormulaNode Node, int Depth)>();
        stack.Push((ast, 0));
        while (stack.Count > 0)
        {
            var (n, depth) = stack.Pop();
            switch (n)
            {
                case RefNode r when Resolve(r.Sheet, host) is { } sheet:
                    into.Add(new RefArea(sheet, new CellRange(r.Cell.Relative, r.Cell.Relative)));
                    break;
                case RangeNode r when Resolve(r.Sheet, host) is { } sheet:
                    into.Add(new RefArea(sheet, r.Range.Normalized));
                    break;
                case BinaryNode { Op: BinaryOp.Range } b when BoundingBox(b, host) is { } box:
                    into.Add(box);
                    break;
                case NameNode name:
                    names = true;
                    if (depth < FormulaEvaluator.MaxNameDepth && _evaluator.ParseName(name.Name, _wb) is { } target && expanded.Add(name.Name))
                        stack.Push((target, depth + 1));
                    break;
                case CallNode call:
                    if (!_evaluator.Functions.TryGet(call.Name, out var f)) unknown = true;
                    else if (f.Info.IsVolatile) isVolatile = true;
                    if (f != null && (f.Name is "ROW" or "COLUMN" || !FunctionRegistry.Default.TryGet(f.Name, out var builtIn) || builtIn != f)) positional = true;
                    foreach (var arg in call.Args) stack.Push((arg, depth));
                    break;
                case BinaryNode b:
                    stack.Push((b.Left, depth));
                    stack.Push((b.Right, depth));
                    break;
                case UnaryNode u:
                    stack.Push((u.Operand, depth));
                    break;
                case PercentNode p:
                    stack.Push((p.Operand, depth));
                    break;
                case ParenNode p:
                    stack.Push((p.Inner, depth));
                    break;
            }
        }

        return (isVolatile, unknown, positional, names);
    }

    private RefArea? BoundingBox(BinaryNode range, Worksheet host)
    {
        var parts = new Stack<FormulaNode>();
        FormulaNode node = range;
        while (node is BinaryNode { Op: BinaryOp.Range } b)
        {
            parts.Push(b.Right);
            node = b.Left;
        }

        parts.Push(node);
        RefArea? box = null;
        foreach (var part in parts)
        {
            var area = part switch
            {
                RefNode { Spill: false } r when Resolve(r.Sheet, host) is { } s => new RefArea(s, new CellRange(r.Cell.Relative, r.Cell.Relative)),
                RangeNode r when Resolve(r.Sheet, host) is { } s => new RefArea(s, r.Range.Normalized),
                _ => (RefArea?)null
            };
            if (area is not { } a || (box is { } x && x.Sheet != a.Sheet)) return null;
            box = box is { } y
                ? new RefArea(a.Sheet, CellRange.FromBounds(Math.Min(y.Range.Top, a.Range.Top), Math.Min(y.Range.Left, a.Range.Left),
                    Math.Max(y.Range.Bottom, a.Range.Bottom), Math.Max(y.Range.Right, a.Range.Right)))
                : a;
        }

        return box;
    }

    private Worksheet Resolve(string sheet, Worksheet host) => sheet == null ? host : _wb.FindSheet(sheet);

    private IEnumerable<CellNode> Next(CellNode node)
    {
        foreach (var d in _graph.Dependents(node)) yield return d;
        if (!_spills.TryGetValue(node, out var spill) || !spill.Active) yield break;
        foreach (var c in spill.Range.Cells())
            if (c != node.Cell)
                foreach (var d in _graph.Dependents(new CellNode(node.Sheet, c.Key)))
                    yield return d;
    }

    private void Close(HashSet<CellNode> affected, IEnumerable<CellNode> starts)
    {
        var queue = new Queue<CellNode>(starts);
        while (queue.Count > 0)
            foreach (var dependent in Next(queue.Dequeue()))
                if (affected.Add(dependent))
                    queue.Enqueue(dependent);
    }

    private void AddAnchorsOver(CellNode cell, HashSet<CellNode> affected, List<CellNode> starts)
    {
        foreach (var (anchor, spill) in _spills)
            if (anchor.Sheet == cell.Sheet && anchor != cell && spill.Range.Contains(cell.Cell) && affected.Add(anchor))
                starts.Add(anchor);
    }

    private bool SpillCovers(CellNode anchor, CellRef cell) => _spills.TryGetValue(anchor, out var s) && s.Range.Contains(cell);

    private void Recalculate(HashSet<CellNode> affected)
    {
        var rerunVolatile = _volatile.Count > 0;
        var volatileStarts = new List<CellNode>();
        for (var pass = 0; pass < MaxSpillPasses; pass++)
        {
            var starts = new List<CellNode>(_grown);
            starts.AddRange(_freed);
            starts.AddRange(volatileStarts);
            volatileStarts.Clear();
            foreach (var freed in _freed) AddAnchorsOver(freed, affected, starts);
            _grown.Clear();
            _freed.Clear();
            Close(affected, starts);
            if (affected.Count == 0) break;
            var evaluatedPlain = false;
            foreach (var component in Components(affected))
            {
                if (component.Count == 1 && !Next(component[0]).Contains(component[0]))
                {
                    Evaluate(component[0]);
                    evaluatedPlain |= !_volatile.Contains(component[0]);
                    continue;
                }

                foreach (var node in component)
                {
                    if (_keep.Contains(node)) continue;
                    ClearSpill(node);
                    Touch(node.Sheet, node.Cell);
                    node.Sheet[node.Cell].Value = CellValue.Error(CellError.Circ);
                    _changed.Add(new CellChange(node.Sheet, node.Cell));
                }
            }

            affected = new HashSet<CellNode>();
            if (!rerunVolatile || !evaluatedPlain) continue;
            rerunVolatile = false;
            volatileStarts.AddRange(_volatile);
            affected.UnionWith(_volatile);
        }

        _grown.Clear();
        _freed.Clear();
        foreach (var ((sheet, key), state) in _before)
            if (sheet.Capture(CellRef.FromKey(key)) != state)
                sheet.Modified = true;
        _before.Clear();
        if (_changed.Count == 0) return;
        var args = new CalculatedEventArgs(_changed.ToList());
        _changed.Clear();
        Calculated?.Invoke(this, args);
    }

    private void Evaluate(CellNode node)
    {
        if (_keep.Contains(node)) return;
        var scope = new EvalScope(_wb, node.Sheet, node.Cell);
        CalcValue result;
        var previousCulture = Coercion.EvaluationCulture;
        Coercion.EvaluationCulture = Culture ?? CultureInfo.CurrentCulture;
        try
        {
            result = _evaluator.EvaluateFormula(_formulas[node].Ast, scope);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            result = CalcValue.Error(CellError.Value);
        }
        finally
        {
            Coercion.EvaluationCulture = previousCulture;
        }

        var array = result.Kind switch
        {
            CalcValueKind.Array when result.Array.Rows > 1 || result.Array.Columns > 1 => result.Array,
            CalcValueKind.Reference when result.Areas.Count == 1 && !result.Areas[0].Range.IsSingleCell =>
                result.Areas[0].Range.Area > MaxSpillCells ? null : FormulaEvaluator.ToArray(result, false),
            _ => null
        };
        CellValue value;
        if (array != null) value = Spill(node, array);
        else if (result.Kind == CalcValueKind.Reference && result.Areas.Count == 1 && !result.Areas[0].Range.IsSingleCell)
            value = SpillBlocked(node, null);
        else
        {
            ClearSpill(node);
            value = _evaluator.ToScalar(result, scope);
        }

        Touch(node.Sheet, node.Cell);
        node.Sheet[node.Cell].Value = value.Kind == CellValueKind.Empty ? CellValue.Number(0) : value;
        _changed.Add(new CellChange(node.Sheet, node.Cell));
        EvaluationCount++;
    }

    private CellValue Spill(CellNode node, CalcArray array)
    {
        var anchor = node.Cell;
        if ((long)array.Rows * array.Columns > MaxSpillCells || anchor.Row + array.Rows > CellRef.MaxRows
                                                               || anchor.Column + array.Columns > CellRef.MaxColumns)
            return SpillBlocked(node, null);
        var range = CellRange.FromBounds(anchor.Row, anchor.Column, anchor.Row + array.Rows - 1, anchor.Column + array.Columns - 1);
        var sheet = node.Sheet;
        foreach (var c in sheet.ExistingIn(range))
            if (c != anchor && sheet[c] is { HasContent: true } blocker && blocker.SpillAnchor != anchor)
                return SpillBlocked(node, range);

        var old = _spills.TryGetValue(node, out var previous) && previous.Active ? previous.Range : (CellRange?)null;
        if (old is { } oldRange)
            foreach (var c in sheet.ExistingIn(oldRange))
                if (!range.Contains(c) && sheet[c].SpillAnchor == anchor)
                    ClearSpillCell(sheet, c);

        for (var r = 0; r < array.Rows; r++)
            for (var col = 0; col < array.Columns; col++)
            {
                if (r == 0 && col == 0) continue;
                var target = new CellRef(anchor.Row + r, anchor.Column + col);
                var v = array[r, col];
                if (v.Kind == CellValueKind.Empty) v = CellValue.Number(0);
                Touch(sheet, target);
                var cell = sheet.Ensure(target);
                if (cell.SpillAnchor == anchor && cell.Value == v && old?.Contains(target) == true) continue;
                cell.Value = v;
                cell.SpillAnchor = anchor;
                sheet.Sync(target);
                _changed.Add(new CellChange(sheet, target));
                if (old?.Contains(target) != true) _grown.Add(new CellNode(sheet, target.Key));
            }

        _spills[node] = new SpillState(range, true);
        return array[0, 0];
    }

    private CellValue SpillBlocked(CellNode node, CellRange? intended)
    {
        ClearSpill(node);
        if (intended is { } range)
        {
            foreach (var c in node.Sheet.ExistingIn(range))
                if (node.Sheet[c].SpillAnchor == node.Cell)
                    ClearSpillCell(node.Sheet, c);
            _spills[node] = new SpillState(range, false);
        }

        return CellValue.Error(CellError.Spill);
    }

    private void ClearSpill(CellNode node)
    {
        if (!_spills.Remove(node, out var spill) || !spill.Active) return;
        foreach (var c in node.Sheet.ExistingIn(spill.Range))
            if (node.Sheet[c].SpillAnchor == node.Cell)
                ClearSpillCell(node.Sheet, c);
    }

    private void Touch(Worksheet sheet, CellRef c) => _before.TryAdd((sheet, c.Relative.Key), sheet.Capture(c.Relative));

    private void ClearSpillCell(Worksheet sheet, CellRef c)
    {
        Touch(sheet, c);
        var cell = sheet[c];
        sheet.Restore(c, new CellState(default, cell.Formula, cell.StyleId));
        _changed.Add(new CellChange(sheet, c));
        _freed.Add(new CellNode(sheet, c.Key));
    }

    private List<List<CellNode>> Components(HashSet<CellNode> nodes)
    {
        var index = new Dictionary<CellNode, int>();
        var low = new Dictionary<CellNode, int>();
        var stack = new Stack<CellNode>();
        var onStack = new HashSet<CellNode>();
        var calls = new Stack<(CellNode Node, IEnumerator<CellNode> Next)>();
        var result = new List<List<CellNode>>();

        void Push(CellNode n)
        {
            index[n] = low[n] = index.Count;
            stack.Push(n);
            onStack.Add(n);
            calls.Push((n, Next(n).GetEnumerator()));
        }

        foreach (var root in nodes)
        {
            if (index.ContainsKey(root)) continue;
            Push(root);
            while (calls.Count > 0)
            {
                var (v, next) = calls.Peek();
                if (next.MoveNext())
                {
                    var w = next.Current;
                    if (!nodes.Contains(w)) continue;
                    if (!index.ContainsKey(w)) Push(w);
                    else if (onStack.Contains(w)) low[v] = Math.Min(low[v], index[w]);
                    continue;
                }

                calls.Pop();
                if (calls.Count > 0)
                {
                    var parent = calls.Peek().Node;
                    low[parent] = Math.Min(low[parent], low[v]);
                }

                if (low[v] != index[v]) continue;
                var component = new List<CellNode>();
                CellNode x;
                do
                {
                    x = stack.Pop();
                    onStack.Remove(x);
                    component.Add(x);
                } while (x != v);

                result.Add(component);
            }
        }

        result.Reverse();
        return result;
    }

    /// <summary>Stops following <see cref="Workbook.Changed"/>; calculated values stay as they are.</summary>
    public void Dispose() => _wb.Changed -= OnWorkbookChanged;

    private readonly record struct SpillState(CellRange Range, bool Active);

    private sealed record FormulaEntry(string Text, FormulaNode Ast, bool Positional, bool UsesNames);
}
