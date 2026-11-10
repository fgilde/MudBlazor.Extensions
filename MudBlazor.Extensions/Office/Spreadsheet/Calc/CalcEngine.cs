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
public sealed partial class CalcEngine : IDisposable
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
    private HashSet<CellNode> _hiddenSensitive = new();
    private long _hiddenVersion;
    private HashSet<CellNode> _keep = new();
    private Dictionary<CellNode, (string Formula, CellValue Value)> _keptFormulas = new();
    private readonly Dictionary<(Worksheet Sheet, long Key), CellState> _before = new();
    private Dictionary<CellNode, SpillState> _spills = new();
    private Dictionary<string, string> _names = new(StringComparer.OrdinalIgnoreCase);
    private string _tables = "";
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

    internal long GraphIndexOperations => _graph.IndexOperations;

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
        _hiddenSensitive.Clear();
        _hiddenVersion = _wb.HiddenVersion;
        _spills.Clear();
        _keep.Clear();
        _keptFormulas.Clear();
        _names = new Dictionary<string, string>(_wb.DefinedNameMap, StringComparer.OrdinalIgnoreCase);
        _tables = StructuredReferences.Signature(_wb);
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
        affected.RemoveWhere(n => !_formulas.ContainsKey(n));
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
                _hiddenSensitive.Remove(node);
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
        var layout = e.Structure.Any(s => s.Kind == StructureChangeKind.Layout);
        var bulk = e.ValueRanges.Sum(r => (long)r.Range.Area) > BulkCells;
        if (structure.Count == 0)
        {
            if (bulk) OnBlocksChanged(e);
            else if (e.Cells.Count > 0) OnCellsChanged(e.Cells.Select(c => (c.Sheet, c.Cell)));
            if (layout) OnRowsShownOrHidden();
        }
        else if (bulk || !Apply(structure, e.Cells.Select(c => (c.Sheet, c.Cell))))
            RecalculateAll();
    }

    private void OnRowsShownOrHidden()
    {
        if (HiddenChanged() && _hiddenSensitive.Count > 0) Finish([.. _hiddenSensitive], [.. _hiddenSensitive]);
    }

    private bool HiddenChanged()
    {
        if (_wb.HiddenVersion == _hiddenVersion) return false;
        _hiddenVersion = _wb.HiddenVersion;
        return true;
    }

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
