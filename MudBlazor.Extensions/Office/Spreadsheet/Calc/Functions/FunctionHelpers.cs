using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class Fn
{
    internal static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    internal static void Add(this FunctionRegistry registry, string category, string signature, string description,
        Func<FunctionContext, IReadOnlyList<CalcArg>, CalcValue> body, bool isVolatile = false)
    {
        var open = signature.IndexOf('(');
        int min = 0, max = 0, depth = 0, start = open + 1;
        var variadic = false;
        for (var i = start; i < signature.Length; i++)
        {
            var ch = signature[i];
            if (ch == '[') depth++;
            else if (ch == ']') depth--;
            if ((ch != ',' || depth != 0) && i != signature.Length - 1) continue;
            var p = signature[start..i].Trim();
            start = i + 1;
            if (p.Length == 0) continue;
            if (p == "...") variadic = true;
            else
            {
                max++;
                if (p[0] != '[') min++;
            }
        }

        registry.Register(new DelegateFunction(new FunctionInfo(signature[..open], category, signature, description, isVolatile),
            min, variadic ? 255 : max, body));
    }

    internal static Func<FunctionContext, IReadOnlyList<CalcArg>, CalcValue> Lift(Func<CellValue[], CellValue> f) => Lift((_, a) => f(a));

    internal static Func<FunctionContext, IReadOnlyList<CalcArg>, CalcValue> Lift(Func<FunctionContext, CellValue[], CellValue> f) => (ctx, args) =>
    {
        var values = new CalcValue[args.Count];
        var multi = false;
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = args[i].IsMissing ? CalcValue.Empty : args[i].Value;
            multi |= IsMulti(values[i]);
        }

        if (!multi) return Safe(ctx, f, values.Select(ctx.ToScalar).ToArray());
        var arrays = values.Select(ctx.ToArray).ToArray();
        if (arrays.Any(a => a == null)) return CalcValue.Error(CellError.Value);
        var result = new CalcArray(arrays.Max(a => a.Rows), arrays.Max(a => a.Columns));
        for (var r = 0; r < result.Rows; r++)
            for (var c = 0; c < result.Columns; c++)
                result[r, c] = Safe(ctx, f, arrays.Select(a => Pick(a, r, c)).ToArray());
        return CalcValue.FromArray(result);
    };

    internal static bool IsMulti(CalcValue v) => v.Kind == CalcValueKind.Array && (v.Array.Rows > 1 || v.Array.Columns > 1)
                                                 || (v.Kind == CalcValueKind.Reference && (v.Areas.Count > 1 || !v.Areas[0].Range.IsSingleCell));

    internal static CellValue Pick(CalcArray a, int r, int c)
    {
        if (a.Rows == 1) r = 0;
        if (a.Columns == 1) c = 0;
        return r < a.Rows && c < a.Columns ? a[r, c] : CellValue.Error(CellError.NA);
    }

    private static CellValue Safe(FunctionContext ctx, Func<FunctionContext, CellValue[], CellValue> f, CellValue[] a)
    {
        try
        {
            return f(ctx, a);
        }
        catch (CalcErrorException ex)
        {
            return CellValue.Error(ex.Error);
        }
    }

    internal static CalcErrorException Error(CellError error) => new(error);

    internal static double Num(CellValue v) => Coercion.TryToNumber(v, out var n, out var e) ? n : throw Error(e);

    internal static double Num(CellValue[] a, int i, double fallback) => i < a.Length ? Num(a[i]) : fallback;

    internal static int Int(CellValue v)
    {
        var n = Math.Truncate(Num(v));
        return n is >= int.MinValue and <= int.MaxValue ? (int)n : throw Error(CellError.Num);
    }

    internal static int Int(CellValue[] a, int i, int fallback) => i < a.Length ? Int(a[i]) : fallback;

    internal static bool Bool(CellValue v) => Coercion.TryToBoolean(v, out var b, out var e) ? b : throw Error(e);

    internal static bool Bool(CellValue[] a, int i, bool fallback) => i < a.Length ? Bool(a[i]) : fallback;

    internal static string Str(CellValue v) => v.Kind == CellValueKind.Error ? throw Error(v.ErrorValue) : Coercion.ToText(v);

    internal static CellValue Number(double d) => double.IsFinite(d) ? CellValue.Number(d) : CellValue.Error(CellError.Num);

    internal static CellValue Text(string s) => s.Length > 32767 ? CellValue.Error(CellError.Value) : CellValue.Text(s);

    internal static double Div(double x, double y) => y == 0 ? throw Error(CellError.Div0) : x / y;

    internal static CalcValue Value(CalcArg arg) => arg.IsMissing ? CalcValue.Empty : arg.Value;

    internal static CellValue Scalar(FunctionContext ctx, CalcArg arg) => arg.IsMissing ? default : ctx.ToScalar(arg.Value);

    internal static int OptionalInt(FunctionContext ctx, IReadOnlyList<CalcArg> a, int i, int fallback) =>
        a.Count > i && !a[i].IsMissing ? Int(Scalar(ctx, a[i])) : fallback;

    internal static CalcArray Array(FunctionContext ctx, CalcArg arg) =>
        ctx.ToArray(Value(arg)) ?? throw Error(CellError.Value);

    internal static List<double> Numbers(FunctionContext ctx, IReadOnlyList<CalcArg> args, int from = 0, bool all = false)
    {
        var list = new List<double>();
        for (var i = from; i < args.Count; i++)
        {
            if (args[i].IsMissing)
            {
                list.Add(0);
                continue;
            }

            var value = args[i].Value;
            if (value.Kind == CalcValueKind.Scalar)
            {
                if (value.Scalar.Kind != CellValueKind.Empty) list.Add(Num(value.Scalar));
                continue;
            }

            foreach (var v in ctx.Values(value))
            {
                switch (v.Kind)
                {
                    case CellValueKind.Error: throw Error(v.ErrorValue);
                    case CellValueKind.Number: list.Add(v.NumberValue); break;
                    case CellValueKind.Boolean when all: list.Add(v.BooleanValue ? 1 : 0); break;
                    case CellValueKind.Text when all: list.Add(0); break;
                }
            }
        }

        return list;
    }

    internal static List<double> Numbers(FunctionContext ctx, CalcArg arg) => Numbers(ctx, [arg]);

    internal static Grid ToGrid(FunctionContext ctx, CalcValue value, bool trim = false)
    {
        if (value.Kind != CalcValueKind.Reference) return new Grid(ctx.ToArray(value));
        if (value.Areas.Count != 1) throw Error(CellError.Value);
        var (sheet, range) = value.Areas[0];
        return new Grid(sheet, trim ? FormulaEvaluator.TrimToUsed(sheet, range) : range);
    }

    internal static Grid ToGrid(FunctionContext ctx, CalcArg arg, bool trim = false) => ToGrid(ctx, Value(arg), trim);

    internal static string WildcardPattern(string pattern, bool anchored)
    {
        var sb = new StringBuilder(anchored ? "^" : "");
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '~' && i + 1 < pattern.Length && pattern[i + 1] is '*' or '?' or '~') sb.Append(Regex.Escape(pattern[++i].ToString()));
            else if (c == '*') sb.Append(".*");
            else if (c == '?') sb.Append('.');
            else sb.Append(Regex.Escape(c.ToString()));
        }

        return anchored ? sb.Append('$').ToString() : sb.ToString();
    }

    internal static bool Like(string text, string pattern) =>
        pattern.IndexOfAny(['*', '?', '~']) < 0
            ? string.Equals(text, pattern, StringComparison.OrdinalIgnoreCase)
            : Regex.IsMatch(text, WildcardPattern(pattern, true), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    internal static bool SameValue(CellValue a, CellValue b, bool wildcards)
    {
        if (a.Kind == CellValueKind.Empty) a = CellValue.Number(0);
        if (a.Kind != b.Kind) return false;
        return a.Kind switch
        {
            CellValueKind.Text => wildcards ? Like(b.TextValue, a.TextValue) : string.Equals(a.TextValue, b.TextValue, StringComparison.OrdinalIgnoreCase),
            CellValueKind.Number => Coercion.CompareNumbers(a.NumberValue, b.NumberValue) == 0,
            CellValueKind.Boolean => a.BooleanValue == b.BooleanValue,
            _ => a.ErrorValue == b.ErrorValue
        };
    }

    internal static double SerialArg(FunctionContext ctx, CellValue v)
    {
        if (v.Kind == CellValueKind.Text && !Coercion.TryToNumber(v, out _, out _))
            return TryParseDate(v.TextValue, ctx.Culture, out var parsed) ? parsed : throw Error(CellError.Value);

        var serial = Num(v);
        return serial is < 0 or >= ExcelDate.MaxSerial + 1 ? throw Error(CellError.Num) : serial;
    }

    internal static bool TryParseDate(string text, CultureInfo culture, out double serial)
    {
        serial = 0;
        if (!DateTime.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault, out var d)) return false;
        if (d.Date == DateTime.MinValue)
        {
            serial = d.TimeOfDay.TotalDays;
            return true;
        }

        if (d.Year < 1900) return false;
        serial = ExcelDate.ToSerial(d);
        return true;
    }

    internal static int Day(FunctionContext ctx, CellValue v) => (int)Math.Floor(SerialArg(ctx, v));

    internal static CellValue Serial(double serial) =>
        serial is < 0 or >= ExcelDate.MaxSerial + 1 ? CellValue.Error(CellError.Num) : CellValue.Number(serial);
}

internal sealed class Grid
{
    private readonly CalcArray _array;
    private readonly Worksheet _sheet;
    private readonly CellRange _range;

    public Grid(CalcArray array)
    {
        _array = array;
        Rows = array.Rows;
        Columns = array.Columns;
    }

    public Grid(Worksheet sheet, CellRange range)
    {
        _sheet = sheet;
        _range = range;
        Rows = range.RowCount;
        Columns = range.ColumnCount;
    }

    public int Rows { get; }

    public int Columns { get; }

    public int Length => Rows * Columns;

    public bool IsVector => Rows == 1 || Columns == 1;

    public CellValue this[int r, int c] => _array != null
        ? _array[r, c]
        : FormulaEvaluator.GetValue(_sheet, new CellRef(_range.Top + r, _range.Left + c));

    public CellValue this[int i] => Rows == 1 ? this[0, i] : this[i, 0];

    public CalcValue Slice(int top, int left, int rows, int columns)
    {
        if (_array == null)
            return CalcValue.FromReference([new RefArea(_sheet, CellRange.FromBounds(_range.Top + top, _range.Left + left,
                _range.Top + top + rows - 1, _range.Left + left + columns - 1))]);
        if (rows == 1 && columns == 1) return _array[top, left];
        var result = new CalcArray(rows, columns);
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < columns; c++)
                result[r, c] = _array[top + r, left + c];
        return CalcValue.FromArray(result);
    }

    public CalcValue At(int i) => Rows == 1 ? Slice(0, i, 1, 1) : Slice(i, 0, 1, 1);
}
