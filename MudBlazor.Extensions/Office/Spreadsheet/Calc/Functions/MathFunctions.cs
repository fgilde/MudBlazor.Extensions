using System.Text;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class MathFunctions
{
    private const string Cat = "Math";

    internal static void Register(FunctionRegistry r)
    {
        r.Add(Cat, "SUM(number1, [number2], ...)", "Adds all numbers in the arguments.",
            (ctx, a) => Fn.Number(Fn.Numbers(ctx, a).Sum()));
        r.Add(Cat, "PRODUCT(number1, [number2], ...)", "Multiplies all numbers in the arguments.", (ctx, a) =>
        {
            var n = Fn.Numbers(ctx, a);
            return Fn.Number(n.Count == 0 ? 0 : n.Aggregate(1.0, (x, y) => x * y));
        });
        r.Add(Cat, "SUMPRODUCT(array1, [array2], ...)", "Multiplies corresponding entries of the arrays and returns the sum of the products.", SumProduct);
        r.Add(Cat, "SUMSQ(number1, [number2], ...)", "Returns the sum of the squares of the arguments.",
            (ctx, a) => Fn.Number(Fn.Numbers(ctx, a).Sum(x => x * x)));
        r.Add(Cat, "SUMX2MY2(array_x, array_y)", "Returns the sum of the differences of squares of corresponding values.",
            (ctx, a) => Pairs(ctx, a, (x, y) => x * x - y * y));
        r.Add(Cat, "SUMX2PY2(array_x, array_y)", "Returns the sum of the sums of squares of corresponding values.",
            (ctx, a) => Pairs(ctx, a, (x, y) => x * x + y * y));
        r.Add(Cat, "SUMXMY2(array_x, array_y)", "Returns the sum of squares of differences of corresponding values.",
            (ctx, a) => Pairs(ctx, a, (x, y) => (x - y) * (x - y)));

        r.Add(Cat, "ROUND(number, num_digits)", "Rounds a number to a number of digits, halves away from zero.",
            Fn.Lift(a => Fn.Number(Round(Fn.Num(a[0]), Fn.Int(a[1]), m => Math.Round(m, MidpointRounding.AwayFromZero)))));
        r.Add(Cat, "ROUNDUP(number, num_digits)", "Rounds a number away from zero.",
            Fn.Lift(a => Fn.Number(Round(Fn.Num(a[0]), Fn.Int(a[1]), m => m < 0 ? Math.Floor(m) : Math.Ceiling(m)))));
        r.Add(Cat, "ROUNDDOWN(number, num_digits)", "Rounds a number toward zero.",
            Fn.Lift(a => Fn.Number(Round(Fn.Num(a[0]), Fn.Int(a[1]), Math.Truncate))));
        r.Add(Cat, "TRUNC(number, [num_digits])", "Truncates a number to an integer or to a number of digits.",
            Fn.Lift(a => Fn.Number(Round(Fn.Num(a[0]), Fn.Int(a, 1, 0), Math.Truncate))));
        r.Add(Cat, "MROUND(number, multiple)", "Rounds a number to the nearest multiple.", Fn.Lift(a =>
        {
            var (n, m) = (Fn.Num(a[0]), Fn.Num(a[1]));
            if (m == 0) return CellValue.Number(0);
            if (n * m < 0) throw Fn.Error(CellError.Num);
            return Fn.Number(Round(n / m, 0, x => Math.Round(x, MidpointRounding.AwayFromZero)) * m);
        }));
        r.Add(Cat, "INT(number)", "Rounds a number down to the nearest integer.", Math1(Math.Floor));
        r.Add(Cat, "MOD(number, divisor)", "Returns the remainder after division; the result has the sign of the divisor.", Fn.Lift(a =>
        {
            var (n, d) = (Fn.Num(a[0]), Fn.Num(a[1]));
            return Fn.Number(n - d * Math.Floor(Fn.Div(n, d)));
        }));
        r.Add(Cat, "QUOTIENT(numerator, denominator)", "Returns the integer portion of a division.",
            Fn.Lift(a => Fn.Number(Math.Truncate(Fn.Div(Fn.Num(a[0]), Fn.Num(a[1]))))));
        r.Add(Cat, "ABS(number)", "Returns the absolute value of a number.", Math1(Math.Abs));
        r.Add(Cat, "SIGN(number)", "Returns 1 for positive numbers, -1 for negative numbers and 0 for zero.", Math1(x => Math.Sign(x)));
        r.Add(Cat, "POWER(number, power)", "Returns a number raised to a power.", Fn.Lift(a =>
        {
            var (x, y) = (Fn.Num(a[0]), Fn.Num(a[1]));
            if (x == 0 && y == 0) throw Fn.Error(CellError.Num);
            if (x == 0 && y < 0) throw Fn.Error(CellError.Div0);
            return Fn.Number(Math.Pow(x, y));
        }));
        r.Add(Cat, "SQRT(number)", "Returns the positive square root.", Math1(Math.Sqrt));
        r.Add(Cat, "SQRTPI(number)", "Returns the square root of a number multiplied by pi.", Math1(x => Math.Sqrt(x * Math.PI)));
        r.Add(Cat, "EXP(number)", "Returns e raised to the power of a number.", Math1(Math.Exp));
        r.Add(Cat, "LN(number)", "Returns the natural logarithm.", Math1(x => x <= 0 ? double.NaN : Math.Log(x)));
        r.Add(Cat, "LOG10(number)", "Returns the base-10 logarithm.", Math1(x => x <= 0 ? double.NaN : Math.Log10(x)));
        r.Add(Cat, "LOG(number, [base])", "Returns the logarithm of a number to a base (default 10).", Fn.Lift(a =>
        {
            var (x, b) = (Fn.Num(a[0]), Fn.Num(a, 1, 10));
            if (x <= 0 || b <= 0) throw Fn.Error(CellError.Num);
            return Fn.Number(Fn.Div(Math.Log(x), Math.Log(b)));
        }));
        r.Add(Cat, "PI()", "Returns the value of pi.", (_, _) => CalcValue.Number(Math.PI));
        r.Add(Cat, "RAND()", "Returns a random number between 0 and 1.", (_, _) => CalcValue.Number(Random.Shared.NextDouble()), true);
        r.Add(Cat, "RANDBETWEEN(bottom, top)", "Returns a random integer between two numbers.", Fn.Lift(a =>
        {
            var (lo, hi) = (Math.Ceiling(Fn.Num(a[0])), Math.Floor(Fn.Num(a[1])));
            if (lo > hi) throw Fn.Error(CellError.Num);
            return CellValue.Number(Math.Floor(lo + Random.Shared.NextDouble() * (hi - lo + 1)));
        }), true);

        r.Add(Cat, "CEILING(number, significance)", "Rounds a number up to the nearest multiple of significance.", Fn.Lift(a =>
        {
            var (n, s) = (Fn.Num(a[0]), Fn.Num(a[1]));
            if (s == 0) return CellValue.Number(0);
            if (n > 0 && s < 0) throw Fn.Error(CellError.Num);
            return Fn.Number(Math.Ceiling(Quotient(n, s)) * s);
        }));
        r.Add(Cat, "FLOOR(number, significance)", "Rounds a number down to the nearest multiple of significance.", Fn.Lift(a =>
        {
            var (n, s) = (Fn.Num(a[0]), Fn.Num(a[1]));
            if (s == 0) throw Fn.Error(CellError.Div0);
            if (n > 0 && s < 0) throw Fn.Error(CellError.Num);
            return Fn.Number(Math.Floor(Quotient(n, s)) * s);
        }));
        r.Add(Cat, "CEILING.MATH(number, [significance], [mode])", "Rounds a number up to the nearest integer or multiple of significance.",
            Fn.Lift(a => RoundTo(Fn.Num(a[0]), Fn.Num(a, 1, 1), true, Fn.Num(a, 2, 0) != 0)));
        r.Add(Cat, "FLOOR.MATH(number, [significance], [mode])", "Rounds a number down to the nearest integer or multiple of significance.",
            Fn.Lift(a => RoundTo(Fn.Num(a[0]), Fn.Num(a, 1, 1), false, Fn.Num(a, 2, 0) != 0)));
        r.Add(Cat, "CEILING.PRECISE(number, [significance])", "Rounds a number up to the nearest multiple of significance, regardless of sign.",
            Fn.Lift(a => RoundTo(Fn.Num(a[0]), Fn.Num(a, 1, 1), true, false)));
        r.Add(Cat, "ISO.CEILING(number, [significance])", "Rounds a number up to the nearest multiple of significance, regardless of sign.",
            Fn.Lift(a => RoundTo(Fn.Num(a[0]), Fn.Num(a, 1, 1), true, false)));
        r.Add(Cat, "FLOOR.PRECISE(number, [significance])", "Rounds a number down to the nearest multiple of significance, regardless of sign.",
            Fn.Lift(a => RoundTo(Fn.Num(a[0]), Fn.Num(a, 1, 1), false, false)));
        r.Add(Cat, "EVEN(number)", "Rounds a number away from zero to the nearest even integer.",
            Math1(x => Math.Sign(x) * Math.Ceiling(Math.Abs(x) / 2) * 2));
        r.Add(Cat, "ODD(number)", "Rounds a number away from zero to the nearest odd integer.", Math1(x =>
        {
            var odd = Math.Ceiling(Math.Abs(x));
            if (odd % 2 == 0) odd++;
            return x < 0 ? -odd : odd;
        }));

        r.Add(Cat, "FACT(number)", "Returns the factorial of a number.", Math1(x => x < 0 ? double.NaN : Factorial(Math.Truncate(x), 1)));
        r.Add(Cat, "FACTDOUBLE(number)", "Returns the double factorial of a number.", Math1(x => x < -1 ? double.NaN : Factorial(Math.Truncate(x), 2)));
        r.Add(Cat, "COMBIN(number, number_chosen)", "Returns the number of combinations for a given number of items.",
            Fn.Lift(a => Fn.Number(Combin(Math.Truncate(Fn.Num(a[0])), Math.Truncate(Fn.Num(a[1]))))));
        r.Add(Cat, "COMBINA(number, number_chosen)", "Returns the number of combinations with repetitions.", Fn.Lift(a =>
        {
            var (n, k) = (Math.Truncate(Fn.Num(a[0])), Math.Truncate(Fn.Num(a[1])));
            if (n < 0 || k < 0) throw Fn.Error(CellError.Num);
            return Fn.Number(n == 0 && k == 0 ? 1 : Combin(n + k - 1, k));
        }));
        r.Add(Cat, "PERMUT(number, number_chosen)", "Returns the number of permutations for a given number of items.", Fn.Lift(a =>
        {
            var (n, k) = (Math.Truncate(Fn.Num(a[0])), Math.Truncate(Fn.Num(a[1])));
            if (n < 0 || k < 0 || n < k) throw Fn.Error(CellError.Num);
            var p = 1.0;
            for (var i = 0; i < k; i++) p *= n - i;
            return Fn.Number(p);
        }));
        r.Add(Cat, "PERMUTATIONA(number, number_chosen)", "Returns the number of permutations with repetitions.", Fn.Lift(a =>
        {
            var (n, k) = (Math.Truncate(Fn.Num(a[0])), Math.Truncate(Fn.Num(a[1])));
            return n < 0 || k < 0 ? throw Fn.Error(CellError.Num) : Fn.Number(Math.Pow(n, k));
        }));
        r.Add(Cat, "MULTINOMIAL(number1, [number2], ...)", "Returns the ratio of the factorial of a sum to the product of factorials.", (ctx, a) =>
        {
            var n = Fn.Numbers(ctx, a).Select(Math.Truncate).ToList();
            if (n.Any(x => x < 0)) return CalcValue.Error(CellError.Num);
            return Fn.Number(n.Aggregate((Sum: 0.0, Result: 1.0), (acc, x) => (acc.Sum + x, acc.Result * Combin(acc.Sum + x, x))).Result);
        });
        r.Add(Cat, "GCD(number1, [number2], ...)", "Returns the greatest common divisor.", (ctx, a) => Divisors(ctx, a, (x, y) => Gcd(x, y)));
        r.Add(Cat, "LCM(number1, [number2], ...)", "Returns the least common multiple.",
            (ctx, a) => Divisors(ctx, a, (x, y) => x == 0 || y == 0 ? 0 : x / Gcd(x, y) * y));

        r.Add(Cat, "SUBTOTAL(function_num, ref1, [ref2], ...)", "Returns a subtotal of references, always ignoring filtered rows (1-11 include, 101-111 ignore rows hidden by hand).", (ctx, a) =>
        {
            var fn = Fn.Int(Fn.Scalar(ctx, a[0]));
            if (fn % 100 is < 1 or > 11 || fn is > 111 or (> 11 and < 101)) return CalcValue.Error(CellError.Value);
            return Aggregate(fn % 100, Collect(ctx, a, 1, true, fn > 100, false, true), 0);
        });
        r.Add(Cat, "AGGREGATE(function_num, options, ref1, [ref2], ...)", "Returns an aggregate (1-19) optionally ignoring hidden rows, errors and nested subtotals.", (ctx, a) =>
        {
            var fn = Fn.Int(Fn.Scalar(ctx, a[0]));
            var options = Fn.Int(Fn.Scalar(ctx, a[1]));
            if (fn is < 1 or > 19 || options is < 0 or > 7) return CalcValue.Error(CellError.Value);
            var withK = fn >= 14;
            if (withK && a.Count != 4) return CalcValue.Error(CellError.Value);
            var values = Collect(ctx, withK ? [a[2]] : a, withK ? 0 : 2, options % 2 == 1, options % 2 == 1, options is 2 or 3 or 6 or 7, options <= 3);
            return Aggregate(fn, values, withK ? Fn.Num(Fn.Scalar(ctx, a[3])) : 0);
        });

        r.Add(Cat, "SIN(number)", "Returns the sine of an angle in radians.", Math1(Math.Sin));
        r.Add(Cat, "COS(number)", "Returns the cosine of an angle in radians.", Math1(Math.Cos));
        r.Add(Cat, "TAN(number)", "Returns the tangent of an angle in radians.", Math1(Math.Tan));
        r.Add(Cat, "ASIN(number)", "Returns the arcsine in radians.", Math1(Math.Asin));
        r.Add(Cat, "ACOS(number)", "Returns the arccosine in radians.", Math1(Math.Acos));
        r.Add(Cat, "ATAN(number)", "Returns the arctangent in radians.", Math1(Math.Atan));
        r.Add(Cat, "ATAN2(x_num, y_num)", "Returns the arctangent of the x and y coordinates in radians.", Fn.Lift(a =>
        {
            var (x, y) = (Fn.Num(a[0]), Fn.Num(a[1]));
            return x == 0 && y == 0 ? throw Fn.Error(CellError.Div0) : Fn.Number(Math.Atan2(y, x));
        }));
        r.Add(Cat, "SINH(number)", "Returns the hyperbolic sine.", Math1(Math.Sinh));
        r.Add(Cat, "COSH(number)", "Returns the hyperbolic cosine.", Math1(Math.Cosh));
        r.Add(Cat, "TANH(number)", "Returns the hyperbolic tangent.", Math1(Math.Tanh));
        r.Add(Cat, "ASINH(number)", "Returns the inverse hyperbolic sine.", Math1(Math.Asinh));
        r.Add(Cat, "ACOSH(number)", "Returns the inverse hyperbolic cosine.", Math1(Math.Acosh));
        r.Add(Cat, "ATANH(number)", "Returns the inverse hyperbolic tangent.", Math1(x => Math.Abs(x) >= 1 ? double.NaN : Math.Atanh(x)));
        r.Add(Cat, "COT(number)", "Returns the cotangent of an angle in radians.", Math1(x => Fn.Div(1, Math.Tan(x))));
        r.Add(Cat, "CSC(number)", "Returns the cosecant of an angle in radians.", Math1(x => Fn.Div(1, Math.Sin(x))));
        r.Add(Cat, "SEC(number)", "Returns the secant of an angle in radians.", Math1(x => 1 / Math.Cos(x)));
        r.Add(Cat, "COTH(number)", "Returns the hyperbolic cotangent.", Math1(x => Fn.Div(1, Math.Tanh(x))));
        r.Add(Cat, "CSCH(number)", "Returns the hyperbolic cosecant.", Math1(x => Fn.Div(1, Math.Sinh(x))));
        r.Add(Cat, "SECH(number)", "Returns the hyperbolic secant.", Math1(x => 1 / Math.Cosh(x)));
        r.Add(Cat, "ACOT(number)", "Returns the arccotangent in radians.", Math1(x => Math.PI / 2 - Math.Atan(x)));
        r.Add(Cat, "ACOTH(number)", "Returns the inverse hyperbolic cotangent.",
            Math1(x => Math.Abs(x) <= 1 ? double.NaN : 0.5 * Math.Log((x + 1) / (x - 1))));
        r.Add(Cat, "DEGREES(angle)", "Converts radians to degrees.", Math1(x => x * 180 / Math.PI));
        r.Add(Cat, "RADIANS(angle)", "Converts degrees to radians.", Math1(x => x * Math.PI / 180));

        r.Add(Cat, "BASE(number, radix, [min_length])", "Converts a number to text in another base.", Fn.Lift(a =>
        {
            var (n, radix, len) = (Math.Truncate(Fn.Num(a[0])), Fn.Int(a[1]), Fn.Int(a, 2, 0));
            if (n < 0 || n >= 9007199254740992 || radix is < 2 or > 36 || len is < 0 or > 255) throw Fn.Error(CellError.Num);
            var sb = new StringBuilder();
            for (var v = (long)n; v > 0; v /= radix) sb.Insert(0, "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[(int)(v % radix)]);
            return CellValue.Text(sb.ToString().PadLeft(Math.Max(len, 1), '0'));
        }));
        r.Add(Cat, "DECIMAL(text, radix)", "Converts text in a given base to a decimal number.", Fn.Lift(a =>
        {
            var (text, radix) = (Fn.Str(a[0]).Trim(), Fn.Int(a[1]));
            if (radix is < 2 or > 36 || text.Length > 255) throw Fn.Error(CellError.Num);
            double n = 0;
            foreach (var ch in text.ToUpperInvariant())
            {
                var d = char.IsAsciiDigit(ch) ? ch - '0' : char.IsAsciiLetterUpper(ch) ? ch - 'A' + 10 : 99;
                if (d >= radix) throw Fn.Error(CellError.Num);
                n = n * radix + d;
            }

            return Fn.Number(n);
        }));
        r.Add(Cat, "ROMAN(number, [form])", "Converts an Arabic numeral to a Roman numeral (classic form).", Fn.Lift(a =>
        {
            var n = Fn.Int(a[0]);
            if (n is < 0 or > 3999) throw Fn.Error(CellError.Value);
            var sb = new StringBuilder();
            foreach (var (value, symbol) in Roman)
                for (; n >= value; n -= value)
                    sb.Append(symbol);
            return CellValue.Text(sb.ToString());
        }));
        r.Add(Cat, "ARABIC(text)", "Converts a Roman numeral to an Arabic numeral.", Fn.Lift(a =>
        {
            var text = Fn.Str(a[0]).Trim().ToUpperInvariant();
            var negative = text.StartsWith('-');
            if (negative) text = text[1..];
            var n = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var v = RomanDigit(text[i]);
                n += i + 1 < text.Length && RomanDigit(text[i + 1]) > v ? -v : v;
            }

            return CellValue.Number(negative ? -n : n);
        }));

        r.Add(Cat, "MMULT(array1, array2)", "Returns the matrix product of two arrays.", (ctx, a) =>
        {
            var (x, y) = (Matrix(ctx, a[0]), Matrix(ctx, a[1]));
            if (x.GetLength(1) != y.GetLength(0)) return CalcValue.Error(CellError.Value);
            var result = new CalcArray(x.GetLength(0), y.GetLength(1));
            for (var i = 0; i < result.Rows; i++)
                for (var j = 0; j < result.Columns; j++)
                {
                    double s = 0;
                    for (var k = 0; k < x.GetLength(1); k++) s += x[i, k] * y[k, j];
                    result[i, j] = Fn.Number(s);
                }

            return CalcValue.FromArray(result);
        });
        r.Add(Cat, "MDETERM(array)", "Returns the matrix determinant of an array.", (ctx, a) =>
        {
            var m = Square(ctx, a[0]);
            return Fn.Number(Invert(m, null));
        });
        r.Add(Cat, "MINVERSE(array)", "Returns the inverse matrix of an array.", (ctx, a) =>
        {
            var m = Square(ctx, a[0]);
            var n = m.GetLength(0);
            var inverse = new double[n, n];
            if (Invert(m, inverse) == 0) return CalcValue.Error(CellError.Num);
            var result = new CalcArray(n, n);
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                    result[i, j] = Fn.Number(inverse[i, j]);
            return CalcValue.FromArray(result);
        });
        r.Add(Cat, "MUNIT(dimension)", "Returns the unit matrix of a dimension.", (ctx, a) =>
        {
            var n = Fn.Int(Fn.Scalar(ctx, a[0]));
            if (n is < 1 or > 1024) return CalcValue.Error(CellError.Value);
            var result = new CalcArray(n, n);
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                    result[i, j] = CellValue.Number(i == j ? 1 : 0);
            return CalcValue.FromArray(result);
        });
    }

    private static readonly (int Value, string Symbol)[] Roman =
        [(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")];

    private static int RomanDigit(char c) => Roman.FirstOrDefault(p => p.Symbol.Length == 1 && p.Symbol[0] == c).Value is var v and > 0
        ? v
        : throw Fn.Error(CellError.Value);

    private static Func<FunctionContext, IReadOnlyList<CalcArg>, CalcValue> Math1(Func<double, double> f) =>
        Fn.Lift(a => Fn.Number(f(Fn.Num(a[0]))));

    private static double Quotient(double n, double s) => Math.Round(n / s, 12);

    private static CellValue RoundTo(double n, double significance, bool up, bool awayFromZero)
    {
        var s = Math.Abs(significance);
        if (s == 0) return CellValue.Number(0);
        var q = Quotient(n, s);
        var ceiling = n < 0 && awayFromZero ? !up : up;
        return Fn.Number((ceiling ? Math.Ceiling(q) : Math.Floor(q)) * s);
    }

    internal static double Round(double x, int digits, Func<decimal, decimal> op)
    {
        if (digits > 15 || Math.Abs(x) >= 1e27 || (digits > 0 && Math.Abs(x) * Math.Pow(10, digits) >= 1e27)) return x;
        digits = Math.Max(digits, -27);
        var f = 1m;
        for (var i = 0; i < Math.Abs(digits); i++) f *= 10;
        var m = (decimal)x;
        return (double)(digits >= 0 ? op(m * f) / f : op(m / f) * f);
    }

    private static double Factorial(double n, int step)
    {
        if (n > 170 * step) return double.PositiveInfinity;
        var result = 1.0;
        for (var i = n; i > 1; i -= step) result *= i;
        return result;
    }

    private static double Combin(double n, double k)
    {
        if (n < 0 || k < 0 || n < k) throw Fn.Error(CellError.Num);
        k = Math.Min(k, n - k);
        var result = 1.0;
        for (var i = 1; i <= k; i++) result = result * (n - k + i) / i;
        return Math.Round(result);
    }

    private static double Gcd(double a, double b)
    {
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }

    private static CalcValue Divisors(FunctionContext ctx, IReadOnlyList<CalcArg> args, Func<double, double, double> combine)
    {
        var numbers = Fn.Numbers(ctx, args).Select(Math.Truncate).ToList();
        if (numbers.Any(n => n < 0)) return CalcValue.Error(CellError.Num);
        return numbers.Count == 0 ? CalcValue.Error(CellError.Value) : Fn.Number(numbers.Aggregate(combine));
    }

    private static CalcValue SumProduct(FunctionContext ctx, IReadOnlyList<CalcArg> args)
    {
        var arrays = args.Select(a => Fn.Array(ctx, a)).ToList();
        var (rows, cols) = (arrays[0].Rows, arrays[0].Columns);
        if (arrays.Any(a => a.Rows != rows || a.Columns != cols)) return CalcValue.Error(CellError.Value);
        double sum = 0;
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
            {
                var product = 1.0;
                foreach (var a in arrays)
                {
                    var v = a[r, c];
                    if (v.Kind == CellValueKind.Error) return v;
                    product *= v.Kind == CellValueKind.Number ? v.NumberValue : 0;
                }

                sum += product;
            }

        return Fn.Number(sum);
    }

    private static CalcValue Pairs(FunctionContext ctx, IReadOnlyList<CalcArg> args, Func<double, double, double> f)
    {
        var (x, y) = (Fn.Array(ctx, args[0]), Fn.Array(ctx, args[1]));
        if (x.Rows * x.Columns != y.Rows * y.Columns) return CalcValue.Error(CellError.NA);
        double sum = 0;
        for (var i = 0; i < x.Rows * x.Columns; i++)
        {
            var (a, b) = (x[i / x.Columns, i % x.Columns], y[i / y.Columns, i % y.Columns]);
            if (a.Kind == CellValueKind.Error) return a;
            if (b.Kind == CellValueKind.Error) return b;
            if (a.Kind == CellValueKind.Number && b.Kind == CellValueKind.Number) sum += f(a.NumberValue, b.NumberValue);
        }

        return Fn.Number(sum);
    }

    private static List<CellValue> Collect(FunctionContext ctx, IReadOnlyList<CalcArg> args, int from, bool skipFiltered, bool skipHidden, bool skipErrors,
        bool skipNested)
    {
        var list = new List<CellValue>();
        void Add(CellValue v)
        {
            if (v.Kind == CellValueKind.Empty) return;
            if (v.Kind == CellValueKind.Error && !skipErrors) throw Fn.Error(v.ErrorValue);
            if (v.Kind != CellValueKind.Error) list.Add(v);
        }

        for (var i = from; i < args.Count; i++)
        {
            var value = Fn.Value(args[i]);
            if (value.Kind != CalcValueKind.Reference)
            {
                foreach (var v in ctx.Values(value)) Add(v);
                continue;
            }

            foreach (var (sheet, range) in value.Areas)
            {
                var cells = sheet.ExistingIn(range);
                cells.Sort((x, y) => x.Row != y.Row ? x.Row.CompareTo(y.Row) : x.Column.CompareTo(y.Column));
                foreach (var c in cells)
                {
                    var cell = sheet[c];
                    if (skipHidden ? sheet.IsRowHidden(c.Row) : skipFiltered && sheet.IsRowFiltered(c.Row)) continue;
                    if (skipNested && cell.Formula is { } f && (f.Contains("SUBTOTAL(", StringComparison.OrdinalIgnoreCase)
                                                                || f.Contains("AGGREGATE(", StringComparison.OrdinalIgnoreCase))) continue;
                    Add(cell.Value);
                }
            }
        }

        return list;
    }

    private static CalcValue Aggregate(int fn, List<CellValue> values, double k)
    {
        var n = values.Where(v => v.Kind == CellValueKind.Number).Select(v => v.NumberValue).ToList();
        return Fn.Number(fn switch
        {
            1 => StatisticalFunctions.Average(n),
            2 => n.Count,
            3 => values.Count,
            4 => n.Count == 0 ? 0 : n.Max(),
            5 => n.Count == 0 ? 0 : n.Min(),
            6 => n.Count == 0 ? 0 : n.Aggregate(1.0, (x, y) => x * y),
            7 => Math.Sqrt(StatisticalFunctions.Variance(n, true)),
            8 => Math.Sqrt(StatisticalFunctions.Variance(n, false)),
            9 => n.Sum(),
            10 => StatisticalFunctions.Variance(n, true),
            11 => StatisticalFunctions.Variance(n, false),
            12 => StatisticalFunctions.Median(n),
            13 => StatisticalFunctions.Mode(n),
            14 => StatisticalFunctions.Kth(n, k, true),
            15 => StatisticalFunctions.Kth(n, k, false),
            16 => StatisticalFunctions.Percentile(n, k, true),
            17 => StatisticalFunctions.Percentile(n, Math.Truncate(k) / 4, true),
            18 => StatisticalFunctions.Percentile(n, k, false),
            _ => StatisticalFunctions.Percentile(n, Math.Truncate(k) / 4, false)
        });
    }

    private static double[,] Matrix(FunctionContext ctx, CalcArg arg)
    {
        var a = Fn.Array(ctx, arg);
        var m = new double[a.Rows, a.Columns];
        for (var i = 0; i < a.Rows; i++)
            for (var j = 0; j < a.Columns; j++)
                m[i, j] = a[i, j].Kind == CellValueKind.Number ? a[i, j].NumberValue : throw Fn.Error(CellError.Value);
        return m;
    }

    private static double[,] Square(FunctionContext ctx, CalcArg arg)
    {
        var m = Matrix(ctx, arg);
        return m.GetLength(0) == m.GetLength(1) ? m : throw Fn.Error(CellError.Value);
    }

    private static double Invert(double[,] source, double[,] inverse)
    {
        var n = source.GetLength(0);
        var m = (double[,])source.Clone();
        if (inverse != null)
            for (var i = 0; i < n; i++)
                inverse[i, i] = 1;
        var det = 1.0;
        for (var col = 0; col < n; col++)
        {
            var pivot = col;
            for (var row = col + 1; row < n; row++)
                if (Math.Abs(m[row, col]) > Math.Abs(m[pivot, col])) pivot = row;
            if (Math.Abs(m[pivot, col]) < 1e-15) return 0;
            if (pivot != col)
            {
                det = -det;
                for (var j = 0; j < n; j++)
                {
                    (m[col, j], m[pivot, j]) = (m[pivot, j], m[col, j]);
                    if (inverse != null) (inverse[col, j], inverse[pivot, j]) = (inverse[pivot, j], inverse[col, j]);
                }
            }

            var p = m[col, col];
            det *= p;
            for (var j = 0; j < n; j++)
            {
                m[col, j] /= p;
                if (inverse != null) inverse[col, j] /= p;
            }

            for (var row = 0; row < n; row++)
            {
                if (row == col || m[row, col] == 0) continue;
                var factor = m[row, col];
                for (var j = 0; j < n; j++)
                {
                    m[row, j] -= factor * m[col, j];
                    if (inverse != null) inverse[row, j] -= factor * inverse[col, j];
                }
            }
        }

        return det;
    }
}
