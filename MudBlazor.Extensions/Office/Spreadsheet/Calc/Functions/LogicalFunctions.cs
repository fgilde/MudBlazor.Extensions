namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class LogicalFunctions
{
    private const string Cat = "Logical";

    internal static void Register(FunctionRegistry r)
    {
        r.Add(Cat, "IF(logical_test, [value_if_true], [value_if_false])", "Returns one value if a condition is true and another if it is false.", If);
        r.Add(Cat, "IFS(logical_test1, value_if_true1, ...)", "Returns the value of the first condition that is true.", (ctx, a) =>
        {
            if (a.Count % 2 != 0) return CalcValue.Error(CellError.NA);
            for (var i = 0; i < a.Count; i += 2)
                if (Fn.Bool(Fn.Scalar(ctx, a[i])))
                    return Result(a[i + 1]);
            return CalcValue.Error(CellError.NA);
        });
        r.Add(Cat, "IFERROR(value, value_if_error)", "Returns a value if an expression is an error and the expression otherwise.",
            (ctx, a) => Catch(ctx, a, v => v.Kind == CellValueKind.Error));
        r.Add(Cat, "IFNA(value, value_if_na)", "Returns a value if an expression is #N/A and the expression otherwise.",
            (ctx, a) => Catch(ctx, a, v => v is { Kind: CellValueKind.Error, ErrorValue: CellError.NA }));
        r.Add(Cat, "AND(logical1, [logical2], ...)", "Returns TRUE if all arguments are true.", (ctx, a) => Logic(ctx, a, (n, t) => t == n));
        r.Add(Cat, "OR(logical1, [logical2], ...)", "Returns TRUE if any argument is true.", (ctx, a) => Logic(ctx, a, (_, t) => t > 0));
        r.Add(Cat, "XOR(logical1, [logical2], ...)", "Returns TRUE if an odd number of arguments are true.", (ctx, a) => Logic(ctx, a, (_, t) => t % 2 == 1));
        r.Add(Cat, "NOT(logical)", "Reverses a logical value.", Fn.Lift(a => CellValue.Boolean(!Fn.Bool(a[0]))));
        r.Add(Cat, "TRUE()", "Returns the logical value TRUE.", (_, _) => CalcValue.Boolean(true));
        r.Add(Cat, "FALSE()", "Returns the logical value FALSE.", (_, _) => CalcValue.Boolean(false));
        r.Add(Cat, "SWITCH(expression, value1, result1, [default_or_value2, result2], ...)",
            "Compares an expression with a list of values and returns the result of the first match, or the default.", (ctx, a) =>
            {
                var value = Fn.Scalar(ctx, a[0]);
                if (value.Kind == CellValueKind.Error) return value;
                var i = 1;
                for (; i + 1 < a.Count; i += 2)
                    if (Fn.SameValue(Fn.Scalar(ctx, a[i]), value, false))
                        return Result(a[i + 1]);
                return i < a.Count ? Result(a[i]) : CalcValue.Error(CellError.NA);
            });
        r.Add(Cat, "LET(name1, name_value1, calculation_or_name2, ...)", "Assigns names to calculation results and returns the final calculation.", (ctx, a) =>
        {
            if (a.Count % 2 == 0) return CalcValue.Error(CellError.Value);
            var locals = ctx.Locals;
            for (var i = 0; i + 1 < a.Count; i += 2)
            {
                if (a[i].Node is not NameNode name) return CalcValue.Error(CellError.Value);
                locals = new LetBinding(name.Name, ctx.Evaluate(a[i + 1].Node, locals), locals);
            }

            return ctx.Evaluate(a[^1].Node, locals);
        });
    }

    private static CalcValue Result(CalcArg arg) => arg.IsMissing ? CalcValue.Number(0) : arg.Value;

    private static CalcValue If(FunctionContext ctx, IReadOnlyList<CalcArg> args)
    {
        var test = args[0].Value;
        if (!Fn.IsMulti(test))
        {
            if (!Coercion.TryToBoolean(ctx.ToScalar(test), out var result, out var error)) return CalcValue.Error(error);
            var index = result ? 1 : 2;
            return index < args.Count ? Result(args[index]) : CalcValue.Boolean(result);
        }

        var conditions = Fn.Array(ctx, args[0]);
        var yes = args.Count > 1 ? ctx.ToArray(Result(args[1])) : null;
        var no = args.Count > 2 ? ctx.ToArray(Result(args[2])) : null;
        if ((args.Count > 1 && yes == null) || (args.Count > 2 && no == null)) return CalcValue.Error(CellError.Value);
        var array = new CalcArray(Math.Max(conditions.Rows, Math.Max(yes?.Rows ?? 1, no?.Rows ?? 1)),
            Math.Max(conditions.Columns, Math.Max(yes?.Columns ?? 1, no?.Columns ?? 1)));
        for (var r = 0; r < array.Rows; r++)
            for (var c = 0; c < array.Columns; c++)
            {
                var test1 = Fn.Pick(conditions, r, c);
                if (!Coercion.TryToBoolean(test1, out var b, out var error))
                    array[r, c] = CellValue.Error(error);
                else if (b)
                    array[r, c] = yes == null ? CellValue.Boolean(true) : Fn.Pick(yes, r, c);
                else
                    array[r, c] = no == null ? CellValue.Boolean(false) : Fn.Pick(no, r, c);
            }

        return CalcValue.FromArray(array);
    }

    private static CalcValue Catch(FunctionContext ctx, IReadOnlyList<CalcArg> args, Func<CellValue, bool> isCaught)
    {
        var value = Fn.Value(args[0]);
        if (!Fn.IsMulti(value)) return isCaught(ctx.ToScalar(value)) ? Result(args[1]) : value;
        var source = Fn.Array(ctx, args[0]);
        var fallback = Fn.Scalar(ctx, args[1]);
        var array = new CalcArray(source.Rows, source.Columns);
        for (var r = 0; r < source.Rows; r++)
            for (var c = 0; c < source.Columns; c++)
                array[r, c] = isCaught(source[r, c]) ? fallback : source[r, c];
        return CalcValue.FromArray(array);
    }

    private static CalcValue Logic(FunctionContext ctx, IReadOnlyList<CalcArg> args, Func<int, int, bool> decide)
    {
        int count = 0, trues = 0;
        foreach (var arg in args)
        {
            if (arg.IsMissing)
            {
                count++;
                continue;
            }

            var value = arg.Value;
            if (value.Kind == CalcValueKind.Scalar)
            {
                if (value.Scalar.Kind == CellValueKind.Empty) continue;
                count++;
                if (Fn.Bool(value.Scalar)) trues++;
                continue;
            }

            foreach (var v in ctx.Values(value))
            {
                if (v.Kind == CellValueKind.Error) return v;
                if (v.Kind is not (CellValueKind.Boolean or CellValueKind.Number)) continue;
                count++;
                if (v.NumberValue != 0 || v.BooleanValue) trues++;
            }
        }

        return count == 0 ? CalcValue.Error(CellError.Value) : CalcValue.Boolean(decide(count, trues));
    }
}
