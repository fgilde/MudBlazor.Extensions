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
/// the dependent cells in dependency order; structural changes and defined-name changes rebuild the graph. Cells on a
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

    private readonly Workbook _wb;
    private readonly FormulaEvaluator _evaluator;
    private readonly DependencyGraph _graph = new();
    private readonly Dictionary<CellNode, FormulaNode> _formulas = new();
    private readonly HashSet<CellNode> _volatile = new();
    private readonly HashSet<CellNode> _keep = new();
    private readonly Dictionary<CellNode, SpillState> _spills = new();
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
        var cells = _wb.Sheets.SelectMany(sheet => sheet.UsedCells().Select(c => (Sheet: sheet, c.Ref, c.Cell))).ToList();
        foreach (var (sheet, r, cell) in cells)
            if (cell.Formula != null && !Register(new CellNode(sheet, r.Key), cell.Formula) && cell.Value.Kind != CellValueKind.Empty)
                _keep.Add(new CellNode(sheet, r.Key));

        var kept = new Dictionary<CellNode, CellRange>();
        foreach (var (sheet, r, cell) in cells)
        {
            if (cell.SpillAnchor is not { } a) continue;
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
        foreach (var (sheet, cell) in changed)
        {
            if (!_wb.Sheets.Contains(sheet)) continue;
            var node = new CellNode(sheet, cell.Relative.Key);
            var stored = sheet[cell];
            _keep.Remove(node);
            if (stored?.Formula is { } formula)
            {
                Register(node, formula);
                affected.Add(node);
            }
            else
            {
                _formulas.Remove(node);
                _graph.Remove(node);
                _volatile.Remove(node);
                ClearSpill(node);
                if (stored?.SpillAnchor is { } anchor && !SpillCovers(new CellNode(sheet, anchor.Key), node.Cell))
                    ClearSpillCell(sheet, node.Cell);
            }

            starts.Add(node);
            AddAnchorsOver(node, affected, starts);
        }

        foreach (var node in _volatile)
            if (affected.Add(node))
                starts.Add(node);
        Close(affected, starts);
        Recalculate(affected);
    }

    private void OnWorkbookChanged(object sender, WorkbookChangedEventArgs e)
    {
        if (e.Structure.Any(s => s.Kind is not (StructureChangeKind.Layout or StructureChangeKind.SheetMoved)))
            RecalculateAll();
        else if (e.Cells.Count > 0)
            OnCellsChanged(e.Cells.Select(c => (c.Sheet, c.Cell)));
    }

    private bool Register(CellNode node, string formula)
    {
        var parsedOk = FormulaParser.TryParse(formula, out var parsed, out _);
        var ast = parsedOk ? parsed : new ErrorNode(CellError.Name);
        _formulas[node] = ast;
        var precedents = new List<RefArea>();
        if (Collect(ast, node.Sheet, precedents, out var unknown)) _volatile.Add(node);
        else _volatile.Remove(node);
        _graph.Set(node, precedents);
        return parsedOk && !unknown;
    }

    private bool Collect(FormulaNode ast, Worksheet host, List<RefArea> into, out bool unknown)
    {
        var isVolatile = false;
        unknown = false;
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
                case NameNode name when depth < FormulaEvaluator.MaxNameDepth && _evaluator.ParseName(name.Name, _wb) is { } target:
                    stack.Push((target, depth + 1));
                    break;
                case CallNode call:
                    if (!_evaluator.Functions.TryGet(call.Name, out var f)) unknown = true;
                    else if (f.Info.IsVolatile) isVolatile = true;
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

        return isVolatile;
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
            result = _evaluator.Evaluate(_formulas[node], scope);
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
                var cell = sheet.Ensure(target);
                if (cell.SpillAnchor == anchor && cell.Value == v && old?.Contains(target) == true) continue;
                cell.Value = v;
                cell.SpillAnchor = anchor;
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

    private void ClearSpillCell(Worksheet sheet, CellRef c)
    {
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
}
