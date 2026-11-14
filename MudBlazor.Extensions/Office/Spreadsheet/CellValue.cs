using System.Globalization;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>The kind of data stored in a <see cref="CellValue"/>.</summary>
public enum CellValueKind
{
    /// <summary>No value.</summary>
    Empty,

    /// <summary>A number (also used for dates).</summary>
    Number,

    /// <summary>A text string.</summary>
    Text,

    /// <summary>A boolean.</summary>
    Boolean,

    /// <summary>An error value.</summary>
    Error
}

/// <summary>Spreadsheet error values.</summary>
public enum CellError
{
    /// <summary>#NULL!</summary>
    Null,

    /// <summary>#DIV/0!</summary>
    Div0,

    /// <summary>#VALUE!</summary>
    Value,

    /// <summary>#REF!</summary>
    Ref,

    /// <summary>#NAME?</summary>
    Name,

    /// <summary>#NUM!</summary>
    Num,

    /// <summary>#N/A</summary>
    NA,

    /// <summary>#SPILL!</summary>
    Spill,

    /// <summary>#CALC!</summary>
    Calc,

    /// <summary>#CIRC! (circular reference)</summary>
    Circ
}

/// <summary>Immutable cell value: empty, number, text, boolean or error. The default value is empty.</summary>
public readonly record struct CellValue
{
    private readonly double _raw;

    /// <summary>The kind of value.</summary>
    public CellValueKind Kind { get; private init; }

    /// <summary>The text for <see cref="CellValueKind.Text"/>.</summary>
    public string TextValue { get; private init; }

    /// <summary>The number for <see cref="CellValueKind.Number"/>.</summary>
    public double NumberValue => Kind == CellValueKind.Number ? _raw : 0;

    /// <summary>The boolean for <see cref="CellValueKind.Boolean"/>.</summary>
    public bool BooleanValue => Kind == CellValueKind.Boolean && _raw != 0;

    /// <summary>The error for <see cref="CellValueKind.Error"/>.</summary>
    public CellError ErrorValue => Kind == CellValueKind.Error ? (CellError)(int)_raw : default;

    private CellValue(CellValueKind kind, double raw, string text)
    {
        Kind = kind;
        _raw = raw;
        TextValue = text;
    }

    /// <summary>The empty value.</summary>
    public static CellValue Empty => default;

    /// <summary>Creates a number value; negative zero becomes zero.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The value.</returns>
    public static CellValue Number(double value) => new(CellValueKind.Number, value + 0.0, null);

    /// <summary>Creates a text value; null yields the empty value.</summary>
    /// <param name="value">The text.</param>
    /// <returns>The value.</returns>
    public static CellValue Text(string value) => value == null ? default : new CellValue(CellValueKind.Text, 0, value);

    /// <summary>Creates a boolean value.</summary>
    /// <param name="value">The boolean.</param>
    /// <returns>The value.</returns>
    public static CellValue Boolean(bool value) => new(CellValueKind.Boolean, value ? 1 : 0, null);

    /// <summary>Creates an error value.</summary>
    /// <param name="error">The error.</param>
    /// <returns>The value.</returns>
    public static CellValue Error(CellError error) => new(CellValueKind.Error, (int)error, null);

    /// <summary>Formats the value with invariant culture (numbers round-trip, errors use their Excel codes).</summary>
    /// <returns>The text form.</returns>
    public override string ToString() => Kind switch
    {
        CellValueKind.Number => _raw.ToString("R", CultureInfo.InvariantCulture),
        CellValueKind.Text => TextValue,
        CellValueKind.Boolean => BooleanValue ? "TRUE" : "FALSE",
        CellValueKind.Error => ErrorValue switch
        {
            CellError.Null => "#NULL!",
            CellError.Div0 => "#DIV/0!",
            CellError.Value => "#VALUE!",
            CellError.Ref => "#REF!",
            CellError.Name => "#NAME?",
            CellError.Num => "#NUM!",
            CellError.NA => "#N/A",
            CellError.Spill => "#SPILL!",
            CellError.Calc => "#CALC!",
            _ => "#CIRC!"
        },
        _ => ""
    };
}
