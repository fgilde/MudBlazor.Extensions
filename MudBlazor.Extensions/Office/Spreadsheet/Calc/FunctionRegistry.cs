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
        MathFunctions.Register(registry);
        StatisticalFunctions.Register(registry);
        LogicalFunctions.Register(registry);
        TextFunctions.Register(registry);
        DateTimeFunctions.Register(registry);
        LookupFunctions.Register(registry);
        ConditionalFunctions.Register(registry);
        InformationFunctions.Register(registry);
        FinancialFunctions.Register(registry);
        DynamicArrayFunctions.Register(registry);
        return registry;
    }
}
