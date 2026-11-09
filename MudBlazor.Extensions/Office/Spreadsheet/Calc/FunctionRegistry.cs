namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Case-insensitive lookup of worksheet functions by name.</summary>
public sealed class FunctionRegistry
{
    private readonly Dictionary<string, IFunction> _functions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The registry with all built-in functions.</summary>
    public static FunctionRegistry Default { get; } = CreateDefault();

    /// <summary>All registered functions.</summary>
    public IReadOnlyCollection<IFunction> Functions => _functions.Values;

    /// <summary>Adds or replaces a function.</summary>
    /// <param name="function">The function.</param>
    public void Register(IFunction function) => _functions[function.Name] = function;

    /// <summary>Finds a function; the file prefixes <c>_xlfn.</c> and <c>_xlws.</c> are ignored.</summary>
    /// <param name="name">The function name.</param>
    /// <param name="function">The function when found.</param>
    /// <returns>True if the function exists.</returns>
    public bool TryGet(string name, out IFunction function)
    {
        foreach (var prefix in (ReadOnlySpan<string>)["_xlfn._xlws.", "_xlfn.", "_xlws."])
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return _functions.TryGetValue(name[prefix.Length..], out function);
        return _functions.TryGetValue(name, out function);
    }

    private static FunctionRegistry CreateDefault()
    {
        var registry = new FunctionRegistry();
        registry.Register(new DelegateFunction(
            new FunctionInfo("SUM", "Math", "SUM(number1, [number2], ...)", "Adds all numbers in the arguments."), 1, 255, Sum));
        registry.Register(new DelegateFunction(
            new FunctionInfo("IF", "Logical", "IF(logical_test, [value_if_true], [value_if_false])",
                "Returns one value if a condition is true and another if it is false."), 1, 3, If));
        return registry;
    }

    private static CalcValue Sum(FunctionContext ctx, IReadOnlyList<CalcArg> args)
    {
        double sum = 0;
        foreach (var arg in args)
        {
            if (arg.IsMissing) continue;
            var value = arg.Value;
            if (value.Kind == CalcValueKind.Scalar)
            {
                if (!Coercion.TryToNumber(value.Scalar, out var n, out var error)) return CalcValue.Error(error);
                sum += n;
                continue;
            }

            foreach (var v in ctx.Values(value))
            {
                if (v.Kind == CellValueKind.Error) return v;
                if (v.Kind == CellValueKind.Number) sum += v.NumberValue;
            }
        }

        return double.IsFinite(sum) ? CalcValue.Number(sum) : CalcValue.Error(CellError.Num);
    }

    private static CalcValue If(FunctionContext ctx, IReadOnlyList<CalcArg> args)
    {
        var condition = ctx.ToScalar(args[0].Value);
        if (!Coercion.TryToBoolean(condition, out var result, out var error)) return CalcValue.Error(error);
        var index = result ? 1 : 2;
        if (index >= args.Count) return CalcValue.Boolean(result);
        return args[index].IsMissing ? CalcValue.Number(0) : args[index].Value;
    }
}
