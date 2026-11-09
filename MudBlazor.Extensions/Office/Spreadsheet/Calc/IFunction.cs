using System.Globalization;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>A worksheet function.</summary>
public interface IFunction
{
    /// <summary>The function name in upper case, for example <c>SUM</c>.</summary>
    string Name { get; }

    /// <summary>Catalog information for autocomplete and help.</summary>
    FunctionInfo Info { get; }

    /// <summary>Computes the result. Arguments are evaluated lazily on first access.</summary>
    /// <param name="ctx">The evaluation context.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>A scalar, array or reference. Throwing <see cref="CalcErrorException"/> yields that error.</returns>
    CalcValue Invoke(FunctionContext ctx, IReadOnlyList<CalcArg> args);
}

/// <summary>Catalog information about a function.</summary>
/// <param name="Name">The function name.</param>
/// <param name="Category">Category such as Math, Logical or Text.</param>
/// <param name="Signature">Signature shown while typing, for example <c>SUM(number1, [number2], ...)</c>.</param>
/// <param name="Description">Short English description.</param>
/// <param name="IsVolatile">True if the result can change without any precedent changing (RAND, NOW, OFFSET, ...); formulas calling it recalculate on every calculation.</param>
public sealed record FunctionInfo(string Name, string Category, string Signature, string Description, bool IsVolatile = false);

/// <summary>Thrown by function implementations to return an error value.</summary>
/// <param name="error">The error.</param>
public sealed class CalcErrorException(CellError error) : Exception(CellValue.Error(error).ToString())
{
    /// <summary>The error value to return.</summary>
    public CellError Error { get; } = error;
}

/// <summary>A lazily evaluated function argument.</summary>
public sealed class CalcArg
{
    private readonly FormulaEvaluator _evaluator;
    private readonly EvalScope _scope;
    private CalcValue? _value;

    internal CalcArg(FormulaNode node, FormulaEvaluator evaluator, EvalScope scope)
    {
        Node = node;
        _evaluator = evaluator;
        _scope = scope;
    }

    /// <summary>The argument expression.</summary>
    public FormulaNode Node { get; }

    /// <summary>True if the argument position is empty, as in <c>IF(A1,,1)</c>.</summary>
    public bool IsMissing => Node is MissingArgNode;

    /// <summary>The evaluated argument; references stay references. Evaluated once on first access.</summary>
    public CalcValue Value => _value ??= _evaluator.Evaluate(Node, _scope);
}

/// <summary>Access to the workbook and value helpers for function implementations.</summary>
public sealed class FunctionContext
{
    private readonly FormulaEvaluator _evaluator;
    private readonly EvalScope _scope;

    internal FunctionContext(FormulaEvaluator evaluator, EvalScope scope)
    {
        _evaluator = evaluator;
        _scope = scope;
    }

    /// <summary>The workbook.</summary>
    public Workbook Workbook => _scope.Workbook;

    /// <summary>The sheet of the formula being evaluated.</summary>
    public Worksheet Sheet => _scope.Sheet;

    /// <summary>The cell of the formula being evaluated.</summary>
    public CellRef Cell => _scope.Cell;

    /// <summary>The culture for TEXT, VALUE, NUMBERVALUE, DATEVALUE and TIMEVALUE (<see cref="CalcEngine.Culture"/>, by default the current culture).</summary>
    public CultureInfo Culture => _evaluator.Culture ?? CultureInfo.CurrentCulture;

    internal LetBinding Locals => _scope.Locals;

    internal CalcValue Evaluate(FormulaNode node, LetBinding locals = null) =>
        _evaluator.Evaluate(node, locals == null ? _scope : _scope with { Locals = locals });

    /// <summary>Reads a cell value; unstored cells are blank.</summary>
    /// <param name="sheet">The sheet.</param>
    /// <param name="cell">The cell.</param>
    /// <returns>The value.</returns>
    public CellValue GetValue(Worksheet sheet, CellRef cell) => FormulaEvaluator.GetValue(sheet, cell);

    /// <summary>
    /// Reduces a value to a scalar: arrays give their top-left value, single-cell references their value, and larger
    /// references the cell in the formula's row or column (implicit intersection) or <c>#VALUE!</c>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The scalar.</returns>
    public CellValue ToScalar(CalcValue value) => _evaluator.ToScalar(value, _scope);

    /// <summary>
    /// Materializes a value as an array (a scalar becomes 1x1). References of more than 65,536 cells are cut at the end of
    /// the sheet's used range, since the cells beyond it are blank.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The array, or null for multi-area references.</returns>
    public CalcArray ToArray(CalcValue value) => FormulaEvaluator.ToArray(value);

    /// <summary>
    /// Enumerates the values of a scalar, an array (row by row) or a reference. References yield only non-blank stored
    /// cells, row by row, so whole-column ranges stay cheap.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The values.</returns>
    public IEnumerable<CellValue> Values(CalcValue value)
    {
        switch (value.Kind)
        {
            case CalcValueKind.Scalar:
                yield return value.Scalar;
                break;
            case CalcValueKind.Array:
                for (var r = 0; r < value.Array.Rows; r++)
                    for (var c = 0; c < value.Array.Columns; c++)
                        yield return value.Array[r, c];
                break;
            default:
                foreach (var area in value.Areas)
                {
                    var cells = area.Sheet.ExistingIn(area.Range);
                    cells.Sort((a, b) => a.Row != b.Row ? a.Row.CompareTo(b.Row) : a.Column.CompareTo(b.Column));
                    foreach (var c in cells)
                        if (area.Sheet[c].Value is { Kind: not CellValueKind.Empty } v)
                            yield return v;
                }

                break;
        }
    }
}

/// <summary>A function implemented by a delegate, with argument count checking.</summary>
/// <param name="info">Catalog information; its name is the function name.</param>
/// <param name="minArgs">Minimum number of arguments.</param>
/// <param name="maxArgs">Maximum number of arguments, or -1 for no limit.</param>
/// <param name="invoke">The implementation.</param>
public sealed class DelegateFunction(FunctionInfo info, int minArgs, int maxArgs,
    Func<FunctionContext, IReadOnlyList<CalcArg>, CalcValue> invoke) : IFunction
{
    /// <inheritdoc />
    public string Name => info.Name;

    /// <inheritdoc />
    public FunctionInfo Info => info;

    /// <inheritdoc />
    public CalcValue Invoke(FunctionContext ctx, IReadOnlyList<CalcArg> args) =>
        args.Count < minArgs || (maxArgs >= 0 && args.Count > maxArgs) ? CalcValue.Error(CellError.Value) : invoke(ctx, args);
}
