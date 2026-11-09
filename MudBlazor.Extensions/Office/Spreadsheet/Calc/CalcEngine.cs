namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Calculates the formulas of a workbook and keeps their results in <see cref="Cell.Value"/>. Cell edits recalculate only
/// the dependent cells in dependency order; structural changes and defined-name changes rebuild the graph. Cells on a
/// circular reference get <see cref="CellError.Circ"/>.
/// </summary>
public sealed class CalcEngine
{
    private readonly Workbook _wb;
    private readonly FormulaEvaluator _evaluator;
    private readonly DependencyGraph _graph = new();
    private readonly Dictionary<CellNode, FormulaNode> _formulas = new();

    /// <summary>Attaches an engine to a workbook, calculates all formulas and follows <see cref="Workbook.Changed"/> from then on.</summary>
    /// <param name="wb">The workbook.</param>
    /// <param name="functions">The functions to use, or null for <see cref="FunctionRegistry.Default"/>.</param>
    public CalcEngine(Workbook wb, FunctionRegistry functions = null)
    {
        _wb = wb;
        _evaluator = new FormulaEvaluator(functions ?? FunctionRegistry.Default);
        wb.Changed += OnWorkbookChanged;
        RecalculateAll();
    }

    /// <summary>The functions available to formulas.</summary>
    public FunctionRegistry Functions => _evaluator.Functions;

    /// <summary>Number of formula evaluations performed so far.</summary>
    public long EvaluationCount { get; private set; }

    /// <summary>Rebuilds the dependency graph from all formulas and calculates every formula.</summary>
    public void RecalculateAll()
    {
        _graph.Clear();
        _formulas.Clear();
        foreach (var sheet in _wb.Sheets)
            foreach (var (r, cell) in sheet.UsedCells())
                if (cell.Formula != null)
                    Register(new CellNode(sheet, r.Key), cell.Formula);
        Recalculate([.. _formulas.Keys]);
    }

    /// <summary>Updates the graph for changed cells (values or formulas) and recalculates the cells that depend on them.</summary>
    /// <param name="changed">The changed cells.</param>
    public void OnCellsChanged(IEnumerable<(Worksheet Sheet, CellRef Cell)> changed)
    {
        var queue = new Queue<CellNode>();
        var affected = new HashSet<CellNode>();
        foreach (var (sheet, cell) in changed)
        {
            if (!_wb.Sheets.Contains(sheet)) continue;
            var node = new CellNode(sheet, cell.Relative.Key);
            if (sheet[cell]?.Formula is { } formula)
            {
                Register(node, formula);
                affected.Add(node);
            }
            else
            {
                _formulas.Remove(node);
                _graph.Remove(node);
            }

            queue.Enqueue(node);
        }

        while (queue.Count > 0)
            foreach (var dependent in _graph.Dependents(queue.Dequeue()))
                if (affected.Add(dependent))
                    queue.Enqueue(dependent);
        Recalculate(affected);
    }

    private void OnWorkbookChanged(object sender, WorkbookChangedEventArgs e)
    {
        if (e.Structure.Any(s => s.Kind is not (StructureChangeKind.Layout or StructureChangeKind.SheetMoved)))
            RecalculateAll();
        else if (e.Cells.Count > 0)
            OnCellsChanged(e.Cells.Select(c => (c.Sheet, c.Cell)));
    }

    private void Register(CellNode node, string formula)
    {
        var ast = FormulaParser.TryParse(formula, out var parsed, out _) ? parsed : new ErrorNode(CellError.Name);
        _formulas[node] = ast;
        var precedents = new List<RefArea>();
        Collect(ast, node.Sheet, 0, precedents);
        _graph.Set(node, precedents);
    }

    private void Collect(FormulaNode ast, Worksheet host, int depth, List<RefArea> into) =>
        ast.Rewrite(n =>
        {
            switch (n)
            {
                case RefNode r when Resolve(r.Sheet, host) is { } sheet:
                    into.Add(new RefArea(sheet, new CellRange(r.Cell.Relative, r.Cell.Relative)));
                    break;
                case RangeNode r when Resolve(r.Sheet, host) is { } sheet:
                    into.Add(new RefArea(sheet, r.Range.Normalized));
                    break;
                case NameNode name when depth < FormulaEvaluator.MaxNameDepth && _evaluator.ParseName(name.Name, _wb) is { } target:
                    Collect(target, host, depth + 1, into);
                    break;
            }

            return n;
        });

    private Worksheet Resolve(string sheet, Worksheet host) => sheet == null ? host : _wb.FindSheet(sheet);

    private void Recalculate(HashSet<CellNode> affected)
    {
        foreach (var component in Components(affected))
        {
            if (component.Count == 1 && !_graph.Dependents(component[0]).Contains(component[0]))
            {
                Evaluate(component[0]);
                continue;
            }

            foreach (var node in component)
                node.Sheet[node.Cell].Value = CellValue.Error(CellError.Circ);
        }
    }

    private void Evaluate(CellNode node)
    {
        var scope = new EvalScope(_wb, node.Sheet, node.Cell);
        var value = _evaluator.ToScalar(_evaluator.Evaluate(_formulas[node], scope), scope);
        node.Sheet[node.Cell].Value = value.Kind == CellValueKind.Empty ? CellValue.Number(0) : value;
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
            calls.Push((n, _graph.Dependents(n).GetEnumerator()));
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
}
