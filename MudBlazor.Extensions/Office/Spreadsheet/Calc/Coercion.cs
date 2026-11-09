using System.Globalization;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Excel conversion and comparison rules between numbers, text, booleans and blanks.</summary>
public static class Coercion
{
    [ThreadStatic] internal static CultureInfo EvaluationCulture;

    /// <summary>
    /// The culture for converting between numbers and text: <see cref="CalcEngine.Culture"/> while a formula is evaluated,
    /// otherwise <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    public static CultureInfo Culture => EvaluationCulture ?? CultureInfo.CurrentCulture;

    /// <summary>
    /// Converts a value to a number: blank is 0, booleans are 1 or 0, text is read like cell input in <see cref="Culture"/>
    /// (numbers, percentages, currency, dates and times).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="number">The number when successful.</param>
    /// <param name="error">The error to report when unsuccessful (the value's own error, or <see cref="CellError.Value"/>).</param>
    /// <returns>True if the value converts.</returns>
    public static bool TryToNumber(CellValue value, out double number, out CellError error)
    {
        error = CellError.Value;
        number = 0;
        switch (value.Kind)
        {
            case CellValueKind.Empty:
                return true;
            case CellValueKind.Number:
                number = value.NumberValue;
                return true;
            case CellValueKind.Boolean:
                number = value.BooleanValue ? 1 : 0;
                return true;
            case CellValueKind.Text:
                return CellInputParser.TryParseValue(value.TextValue, Culture, out number);
            default:
                error = value.ErrorValue;
                return false;
        }
    }

    /// <summary>Converts a value to a boolean: numbers are true unless 0, text must be TRUE or FALSE, blank is false.</summary>
    /// <param name="value">The value.</param>
    /// <param name="result">The boolean when successful.</param>
    /// <param name="error">The error to report when unsuccessful.</param>
    /// <returns>True if the value converts.</returns>
    public static bool TryToBoolean(CellValue value, out bool result, out CellError error)
    {
        error = CellError.Value;
        result = false;
        switch (value.Kind)
        {
            case CellValueKind.Empty:
                return true;
            case CellValueKind.Number:
                result = value.NumberValue != 0;
                return true;
            case CellValueKind.Boolean:
                result = value.BooleanValue;
                return true;
            case CellValueKind.Text when value.TextValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase):
                result = true;
                return true;
            case CellValueKind.Text:
                return value.TextValue.Equals("FALSE", StringComparison.OrdinalIgnoreCase);
            default:
                error = value.ErrorValue;
                return false;
        }
    }

    /// <summary>
    /// Converts a value to text as the <c>&amp;</c> operator and the text functions do: numbers with up to 15 significant
    /// digits and the decimal separator of <see cref="Culture"/> (no thousands separators), booleans as TRUE or FALSE.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The text.</returns>
    public static string ToText(CellValue value) => value.Kind == CellValueKind.Number
        ? value.NumberValue.ToString("G15", Culture)
        : value.ToString();

    /// <summary>Compares two numbers like Excel, treating numbers that agree in their first 15 significant digits as equal.</summary>
    /// <param name="x">Left number.</param>
    /// <param name="y">Right number.</param>
    /// <returns>Negative, zero or positive.</returns>
    public static int CompareNumbers(double x, double y)
    {
        if (x == y) return 0;
        if (Math.Abs(x - y) > Math.Max(Math.Abs(x), Math.Abs(y)) * 1e-14) return x.CompareTo(y);
        return Round15(x).CompareTo(Round15(y));
    }

    private static double Round15(double v) =>
        double.Parse(v.ToString("G15", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>
    /// Compares two non-error values like Excel's comparison operators: blank adapts to the other side,
    /// numbers sort before text before booleans, and text compares case-insensitively.
    /// </summary>
    /// <param name="a">Left value.</param>
    /// <param name="b">Right value.</param>
    /// <returns>Negative, zero or positive.</returns>
    public static int Compare(CellValue a, CellValue b)
    {
        if (a.Kind == CellValueKind.Empty) a = BlankLike(b);
        if (b.Kind == CellValueKind.Empty) b = BlankLike(a);
        if (a.Kind != b.Kind) return Rank(a).CompareTo(Rank(b));
        return a.Kind switch
        {
            CellValueKind.Text => string.Compare(a.TextValue, b.TextValue, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase),
            CellValueKind.Boolean => a.BooleanValue.CompareTo(b.BooleanValue),
            _ => CompareNumbers(a.NumberValue, b.NumberValue)
        };
    }

    private static CellValue BlankLike(CellValue other) => other.Kind switch
    {
        CellValueKind.Text => CellValue.Text(""),
        CellValueKind.Boolean => CellValue.Boolean(false),
        _ => CellValue.Number(0)
    };

    private static int Rank(CellValue v) => v.Kind switch
    {
        CellValueKind.Text => 1,
        CellValueKind.Boolean => 2,
        _ => 0
    };
}
