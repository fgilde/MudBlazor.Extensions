namespace MudBlazor.Extensions.Office.Spreadsheet;

public sealed partial class CalcEngine
{
    private void RegisterOrKeep(CellNode node, Cell cell)
    {
        if (Register(node, cell.Formula) || cell.Value.Kind == CellValueKind.Empty) return;
        _keep.Add(node);
        _keptFormulas[node] = (cell.Formula, cell.Value);
    }

    private bool Relocate(CellNode node, FormulaEntry entry, string formula)
    {
        var ast = Parse(formula);
        if (ast == Unparsable) return false;
        _formulas[node] = entry with { Text = formula, Ast = ast };
        return true;
    }

    private FormulaNode Parse(string formula)
    {
        if (_parsed.TryGetValue(formula, out var ast)) return ast;
        if (_parsed.Count > 2 * _formulas.Count + 1024)
        {
            _parsed.Clear();
            foreach (var entry in _formulas.Values) _parsed[entry.Text] = entry.Ast;
        }

        return _parsed[formula] = FormulaParser.TryParse(formula, out var parsed, out _) ? parsed : Unparsable;
    }

    private bool Register(CellNode node, string formula)
    {
        var ast = Parse(formula);
        var precedents = new List<RefArea>();
        var info = Collect(ast, node.Sheet, node.Cell, precedents);
        _formulas[node] = new FormulaEntry(formula, ast, info.Positional, info.Names, info.HiddenSensitive);
        if (info.Volatile) _volatile.Add(node);
        else _volatile.Remove(node);
        _graph.Set(node, precedents);
        return ast != Unparsable && !info.Unknown;
    }

    private (bool Volatile, bool Unknown, bool Positional, bool Names, bool HiddenSensitive) Collect(FormulaNode ast, Worksheet host, CellRef cell, List<RefArea> into)
    {
        bool isVolatile = false, unknown = false, positional = false, names = false, hidden = false;
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
                case StructuredRefNode s:
                    names = true;
                    if (StructuredReferences.Find(_wb, host, cell, s, out _) == null) unknown = true;
                    else if (StructuredReferences.Resolve(_wb, host, cell, s) is { Kind: CalcValueKind.Reference } area) into.AddRange(area.Areas);
                    break;
                case NameNode name:
                    names = true;
                    if (depth < FormulaEvaluator.MaxNameDepth && _evaluator.ParseName(name.Name, _wb) is { } target && expanded.Add(name.Name))
                        stack.Push((target, depth + 1));
                    break;
                case CallNode call:
                    if (!_evaluator.Functions.TryGet(call.Name, out var f)) unknown = true;
                    else if (f.Info.IsVolatile) isVolatile = true;
                    if (f?.Name is "SUBTOTAL" or "AGGREGATE") hidden = true;
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

        return (isVolatile, unknown, positional, names, hidden);
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
}
