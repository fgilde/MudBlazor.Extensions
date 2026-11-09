namespace MudBlazor.Extensions.Office.Spreadsheet;

internal readonly record struct EvalScope(Workbook Workbook, Worksheet Sheet, CellRef Cell, int NameDepth = 0);

internal sealed class FormulaEvaluator(FunctionRegistry functions)
{
    internal const int MaxNameDepth = 32;

    private readonly Dictionary<string, FormulaNode> _names = new();

    public FunctionRegistry Functions => functions;

    public CalcValue Evaluate(FormulaNode node, EvalScope scope) => node switch
    {
        NumberNode n => CalcValue.Number(n.Value),
        TextNode t => CalcValue.Text(t.Value),
        BoolNode b => CalcValue.Boolean(b.Value),
        ErrorNode e => CalcValue.Error(e.Error),
        RefNode r => Reference(scope, r.Sheet, new CellRange(r.Cell, r.Cell)),
        RangeNode r => Reference(scope, r.Sheet, r.Range),
        NameNode n => EvaluateName(n.Name, scope),
        ParenNode p => Evaluate(p.Inner, scope),
        UnaryNode { Operator: '-' } u => Map(Evaluate(u.Operand, scope), scope,
            v => Arithmetic(CellValue.Number(0), v, (_, y) => Number(-y))),
        UnaryNode u => Evaluate(u.Operand, scope),
        PercentNode p => Map(Evaluate(p.Operand, scope), scope,
            v => Arithmetic(v, CellValue.Number(100), (x, y) => Number(x / y))),
        BinaryNode b => EvaluateBinary(b, scope),
        CallNode c => Call(c, scope),
        ArrayConstNode a => ArrayConst(a, scope),
        _ => CalcValue.Empty
    };

    public CellValue ToScalar(CalcValue value, EvalScope scope)
    {
        switch (value.Kind)
        {
            case CalcValueKind.Scalar:
                return value.Scalar;
            case CalcValueKind.Array:
                return value.Array[0, 0];
        }

        if (value.Areas.Count != 1) return CellValue.Error(CellError.Value);
        var (sheet, range) = value.Areas[0];
        if (range.IsSingleCell) return GetValue(sheet, range.Start);
        if (range.ColumnCount == 1 && scope.Cell.Row >= range.Top && scope.Cell.Row <= range.Bottom)
            return GetValue(sheet, new CellRef(scope.Cell.Row, range.Left));
        if (range.RowCount == 1 && scope.Cell.Column >= range.Left && scope.Cell.Column <= range.Right)
            return GetValue(sheet, new CellRef(range.Top, scope.Cell.Column));
        return CellValue.Error(CellError.Value);
    }

    internal static CellValue GetValue(Worksheet sheet, CellRef cell) => sheet[cell]?.Value ?? default;

    internal static CalcArray ToArray(CalcValue value)
    {
        switch (value.Kind)
        {
            case CalcValueKind.Array:
                return value.Array;
            case CalcValueKind.Scalar:
                var single = new CalcArray(1, 1);
                single[0, 0] = value.Scalar;
                return single;
        }

        if (value.Areas.Count != 1) return null;
        var (sheet, range) = value.Areas[0];
        var array = new CalcArray(range.RowCount, range.ColumnCount);
        foreach (var c in sheet.ExistingIn(range))
            array[c.Row - range.Top, c.Column - range.Left] = sheet[c].Value;
        return array;
    }

    private static CalcValue Reference(EvalScope scope, string sheetName, CellRange range)
    {
        var sheet = sheetName == null ? scope.Sheet : scope.Workbook.FindSheet(sheetName);
        return sheet == null ? CalcValue.Error(CellError.Ref) : CalcValue.FromReference([new RefArea(sheet, range.Normalized)]);
    }

    internal FormulaNode ParseName(string name, Workbook wb)
    {
        if (!wb.DefinedNames.TryGetValue(name, out var text)) return null;
        if (_names.TryGetValue(text, out var node)) return node;
        return _names[text] = FormulaParser.TryParse(text, out node, out _) ? node : new ErrorNode(CellError.Name);
    }

    private CalcValue EvaluateName(string name, EvalScope scope)
    {
        if (scope.NameDepth >= MaxNameDepth) return CalcValue.Error(CellError.Circ);
        var node = ParseName(name, scope.Workbook);
        return node == null ? CalcValue.Error(CellError.Name) : Evaluate(node, scope with { NameDepth = scope.NameDepth + 1 });
    }

    private CalcValue EvaluateBinary(BinaryNode b, EvalScope scope)
    {
        var spine = new Stack<BinaryNode>();
        FormulaNode node = b;
        while (node is BinaryNode inner)
        {
            spine.Push(inner);
            node = inner.Left;
        }

        var left = Evaluate(node, scope);
        while (spine.Count > 0)
        {
            var (op, _, rightNode) = spine.Pop();
            var right = Evaluate(rightNode, scope);
            left = op switch
            {
                BinaryOp.Range or BinaryOp.Intersect or BinaryOp.Union => ReferenceOp(op, left, right),
                _ => Combine(left, right, scope, (x, y) => Apply(op, x, y))
            };
        }

        return left;
    }

    private static CalcValue ReferenceOp(BinaryOp op, CalcValue left, CalcValue right)
    {
        if (left.IsError) return left;
        if (right.IsError) return right;
        if (left.Kind != CalcValueKind.Reference || right.Kind != CalcValueKind.Reference) return CalcValue.Error(CellError.Value);
        if (op == BinaryOp.Union) return CalcValue.FromReference([.. left.Areas, .. right.Areas]);
        if (op == BinaryOp.Range)
        {
            if (left.Areas.Count != 1 || right.Areas.Count != 1 || left.Areas[0].Sheet != right.Areas[0].Sheet)
                return CalcValue.Error(CellError.Value);
            var (a, b) = (left.Areas[0].Range, right.Areas[0].Range);
            return CalcValue.FromReference([new RefArea(left.Areas[0].Sheet, CellRange.FromBounds(
                Math.Min(a.Top, b.Top), Math.Min(a.Left, b.Left), Math.Max(a.Bottom, b.Bottom), Math.Max(a.Right, b.Right)))]);
        }

        var areas = new List<RefArea>();
        foreach (var x in left.Areas)
            foreach (var y in right.Areas)
                if (x.Sheet == y.Sheet && x.Range.Intersects(y.Range))
                    areas.Add(new RefArea(x.Sheet, CellRange.FromBounds(Math.Max(x.Range.Top, y.Range.Top),
                        Math.Max(x.Range.Left, y.Range.Left), Math.Min(x.Range.Bottom, y.Range.Bottom), Math.Min(x.Range.Right, y.Range.Right))));
        return areas.Count == 0 ? CalcValue.Error(CellError.Null) : CalcValue.FromReference(areas);
    }

    private static bool IsMulti(CalcValue v) => v.Kind == CalcValueKind.Array
                                                || (v.Kind == CalcValueKind.Reference && (v.Areas.Count > 1 || !v.Areas[0].Range.IsSingleCell));

    private CalcValue Map(CalcValue value, EvalScope scope, Func<CellValue, CellValue> f)
    {
        if (!IsMulti(value)) return f(ToScalar(value, scope));
        var source = ToArray(value);
        if (source == null) return CalcValue.Error(CellError.Value);
        var result = new CalcArray(source.Rows, source.Columns);
        for (var r = 0; r < source.Rows; r++)
            for (var c = 0; c < source.Columns; c++)
                result[r, c] = f(source[r, c]);
        return CalcValue.FromArray(result);
    }

    private CalcValue Combine(CalcValue left, CalcValue right, EvalScope scope, Func<CellValue, CellValue, CellValue> f)
    {
        if (!IsMulti(left) && !IsMulti(right)) return f(ToScalar(left, scope), ToScalar(right, scope));
        var a = ToArray(left);
        var b = ToArray(right);
        if (a == null || b == null) return CalcValue.Error(CellError.Value);
        var result = new CalcArray(Math.Max(a.Rows, b.Rows), Math.Max(a.Columns, b.Columns));
        for (var r = 0; r < result.Rows; r++)
            for (var c = 0; c < result.Columns; c++)
                result[r, c] = f(Pick(a, r, c), Pick(b, r, c));
        return CalcValue.FromArray(result);
    }

    private static CellValue Pick(CalcArray a, int r, int c)
    {
        if (a.Rows == 1) r = 0;
        if (a.Columns == 1) c = 0;
        return r < a.Rows && c < a.Columns ? a[r, c] : CellValue.Error(CellError.NA);
    }

    private static CellValue Apply(BinaryOp op, CellValue a, CellValue b)
    {
        switch (op)
        {
            case BinaryOp.Add: return Arithmetic(a, b, (x, y) => Number(x + y));
            case BinaryOp.Subtract: return Arithmetic(a, b, (x, y) => Number(x - y));
            case BinaryOp.Multiply: return Arithmetic(a, b, (x, y) => Number(x * y));
            case BinaryOp.Divide: return Arithmetic(a, b, (x, y) => y == 0 ? CellValue.Error(CellError.Div0) : Number(x / y));
            case BinaryOp.Power: return Arithmetic(a, b, Power);
        }

        if (a.Kind == CellValueKind.Error) return a;
        if (b.Kind == CellValueKind.Error) return b;
        if (op == BinaryOp.Concat) return CellValue.Text(Coercion.ToText(a) + Coercion.ToText(b));
        var cmp = Coercion.Compare(a, b);
        return CellValue.Boolean(op switch
        {
            BinaryOp.Equal => cmp == 0,
            BinaryOp.NotEqual => cmp != 0,
            BinaryOp.Less => cmp < 0,
            BinaryOp.LessOrEqual => cmp <= 0,
            BinaryOp.Greater => cmp > 0,
            _ => cmp >= 0
        });
    }

    private static CellValue Arithmetic(CellValue a, CellValue b, Func<double, double, CellValue> f)
    {
        if (a.Kind == CellValueKind.Error) return a;
        if (b.Kind == CellValueKind.Error) return b;
        if (!Coercion.TryToNumber(a, out var x, out var error) || !Coercion.TryToNumber(b, out var y, out error))
            return CellValue.Error(error);
        return f(x, y);
    }

    private static CellValue Power(double x, double y) => x == 0 && y == 0 ? CellValue.Error(CellError.Num)
        : x == 0 && y < 0 ? CellValue.Error(CellError.Div0)
        : Number(Math.Pow(x, y));

    private static CellValue Number(double value) =>
        double.IsFinite(value) ? CellValue.Number(value) : CellValue.Error(CellError.Num);

    private CalcValue Call(CallNode c, EvalScope scope)
    {
        if (!functions.TryGet(c.Name, out var function)) return CalcValue.Error(CellError.Name);
        var args = new CalcArg[c.Args.Count];
        for (var i = 0; i < args.Length; i++) args[i] = new CalcArg(c.Args[i], this, scope);
        try
        {
            return function.Invoke(new FunctionContext(this, scope), args);
        }
        catch (CalcErrorException ex)
        {
            return CalcValue.Error(ex.Error);
        }
    }

    private CalcValue ArrayConst(ArrayConstNode a, EvalScope scope)
    {
        var array = new CalcArray(a.Rows.Count, a.Rows[0].Count);
        for (var r = 0; r < array.Rows; r++)
            for (var c = 0; c < array.Columns; c++)
                array[r, c] = ToScalar(Evaluate(a.Rows[r][c], scope), scope);
        return CalcValue.FromArray(array);
    }
}
