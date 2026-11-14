namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>What a <see cref="CalcValue"/> holds.</summary>
public enum CalcValueKind
{
    /// <summary>A single <see cref="CellValue"/>.</summary>
    Scalar,

    /// <summary>A rectangular <see cref="CalcArray"/>.</summary>
    Array,

    /// <summary>One or more referenced areas, read lazily.</summary>
    Reference
}

/// <summary>A rectangular area of a worksheet.</summary>
/// <param name="Sheet">The worksheet.</param>
/// <param name="Range">The normalized range.</param>
public readonly record struct RefArea(Worksheet Sheet, CellRange Range);

/// <summary>A rectangle of values addressed by zero-based row and column.</summary>
public sealed class CalcArray
{
    private readonly CellValue[] _values;

    /// <summary>Creates an array of empty values.</summary>
    /// <param name="rows">Number of rows (at least 1).</param>
    /// <param name="columns">Number of columns (at least 1).</param>
    public CalcArray(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
        _values = new CellValue[(long)rows * columns];
    }

    /// <summary>Number of rows.</summary>
    public int Rows { get; }

    /// <summary>Number of columns.</summary>
    public int Columns { get; }

    /// <summary>Gets or sets a value.</summary>
    /// <param name="row">Zero-based row.</param>
    /// <param name="column">Zero-based column.</param>
    public CellValue this[int row, int column]
    {
        get => _values[row * Columns + column];
        set => _values[row * Columns + column] = value;
    }
}

/// <summary>Result of evaluating an expression: a scalar, an array or a lazy reference. The default value is the empty scalar.</summary>
public readonly struct CalcValue
{
    private CalcValue(CalcValueKind kind, CellValue scalar, CalcArray array, IReadOnlyList<RefArea> areas)
    {
        Kind = kind;
        Scalar = scalar;
        Array = array;
        Areas = areas;
    }

    /// <summary>What the value holds.</summary>
    public CalcValueKind Kind { get; }

    /// <summary>The value for <see cref="CalcValueKind.Scalar"/>.</summary>
    public CellValue Scalar { get; }

    /// <summary>The array for <see cref="CalcValueKind.Array"/>.</summary>
    public CalcArray Array { get; }

    /// <summary>The areas for <see cref="CalcValueKind.Reference"/>.</summary>
    public IReadOnlyList<RefArea> Areas { get; }

    /// <summary>True for a scalar error value.</summary>
    public bool IsError => Kind == CalcValueKind.Scalar && Scalar.Kind == CellValueKind.Error;

    /// <summary>The empty scalar.</summary>
    public static CalcValue Empty => default;

    /// <summary>Wraps a cell value.</summary>
    /// <param name="value">The value.</param>
    public static implicit operator CalcValue(CellValue value) => new(CalcValueKind.Scalar, value, null, null);

    /// <summary>Creates a number.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The value.</returns>
    public static CalcValue Number(double value) => CellValue.Number(value);

    /// <summary>Creates a text value.</summary>
    /// <param name="value">The text.</param>
    /// <returns>The value.</returns>
    public static CalcValue Text(string value) => CellValue.Text(value);

    /// <summary>Creates a boolean.</summary>
    /// <param name="value">The boolean.</param>
    /// <returns>The value.</returns>
    public static CalcValue Boolean(bool value) => CellValue.Boolean(value);

    /// <summary>Creates an error.</summary>
    /// <param name="error">The error.</param>
    /// <returns>The value.</returns>
    public static CalcValue Error(CellError error) => CellValue.Error(error);

    /// <summary>Wraps an array.</summary>
    /// <param name="array">The array.</param>
    /// <returns>The value.</returns>
    public static CalcValue FromArray(CalcArray array) => new(CalcValueKind.Array, default, array, null);

    /// <summary>Creates a reference to one or more areas.</summary>
    /// <param name="areas">The areas.</param>
    /// <returns>The value.</returns>
    public static CalcValue FromReference(IReadOnlyList<RefArea> areas) => new(CalcValueKind.Reference, default, null, areas);
}
