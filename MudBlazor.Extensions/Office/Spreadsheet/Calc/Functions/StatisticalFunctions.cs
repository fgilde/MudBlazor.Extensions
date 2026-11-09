namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class StatisticalFunctions
{
    private const string Cat = "Statistical";

    internal static void Register(FunctionRegistry r)
    {
        r.Add(Cat, "AVERAGE(number1, [number2], ...)", "Returns the arithmetic mean of the arguments.",
            (ctx, a) => Fn.Number(Average(Fn.Numbers(ctx, a))));
        r.Add(Cat, "AVERAGEA(value1, [value2], ...)", "Returns the mean, counting text as 0 and booleans as 1 or 0.",
            (ctx, a) => Fn.Number(Average(Fn.Numbers(ctx, a, all: true))));
        r.Add(Cat, "MEDIAN(number1, [number2], ...)", "Returns the median of the numbers.", (ctx, a) => Fn.Number(Median(Fn.Numbers(ctx, a))));
        foreach (var name in new[] { "MODE.SNGL", "MODE" })
            r.Add(Cat, name + "(number1, [number2], ...)", "Returns the most frequently occurring value.", (ctx, a) => Fn.Number(Mode(Fn.Numbers(ctx, a))));
        r.Add(Cat, "MODE.MULT(number1, [number2], ...)", "Returns a vertical array of the most frequently occurring values.", (ctx, a) =>
        {
            var n = Fn.Numbers(ctx, a);
            var counts = n.GroupBy(x => x).Select(g => (g.Key, Count: g.Count())).ToList();
            var max = counts.Count == 0 ? 0 : counts.Max(c => c.Count);
            if (max < 2) return CalcValue.Error(CellError.NA);
            return Column(counts.Where(c => c.Count == max).Select(c => c.Key).ToList());
        });
        r.Add(Cat, "MIN(number1, [number2], ...)", "Returns the smallest number (0 if there is none).", (ctx, a) => MinMax(Fn.Numbers(ctx, a), false));
        r.Add(Cat, "MAX(number1, [number2], ...)", "Returns the largest number (0 if there is none).", (ctx, a) => MinMax(Fn.Numbers(ctx, a), true));
        r.Add(Cat, "MINA(value1, [value2], ...)", "Returns the smallest value, counting text as 0 and booleans as 1 or 0.",
            (ctx, a) => MinMax(Fn.Numbers(ctx, a, all: true), false));
        r.Add(Cat, "MAXA(value1, [value2], ...)", "Returns the largest value, counting text as 0 and booleans as 1 or 0.",
            (ctx, a) => MinMax(Fn.Numbers(ctx, a, all: true), true));
        r.Add(Cat, "COUNT(value1, [value2], ...)", "Counts the numbers in the arguments.", Count);
        r.Add(Cat, "COUNTA(value1, [value2], ...)", "Counts the non-empty values in the arguments.", (ctx, a) =>
        {
            var count = 0;
            foreach (var arg in a)
                count += arg.IsMissing ? 1 : arg.Value.Kind == CalcValueKind.Scalar ? 1 : ctx.Values(arg.Value).Count(v => v.Kind != CellValueKind.Empty);
            return CalcValue.Number(count);
        });
        r.Add(Cat, "COUNTBLANK(range)", "Counts empty cells and cells with empty text in a range.", (ctx, a) =>
        {
            var value = a[0].Value;
            if (value.Kind != CalcValueKind.Reference)
                return CalcValue.Number(ctx.Values(value).Count(IsBlank));
            long blank = 0;
            foreach (var (sheet, range) in value.Areas)
                blank += range.Area - sheet.ExistingIn(range).Count(c => !IsBlank(sheet[c].Value));
            return CalcValue.Number(blank);
        });
        r.Add(Cat, "LARGE(array, k)", "Returns the k-th largest value.",
            (ctx, a) => Fn.Number(Kth(Fn.Numbers(ctx, a[0]), Fn.Num(Fn.Scalar(ctx, a[1])), true)));
        r.Add(Cat, "SMALL(array, k)", "Returns the k-th smallest value.",
            (ctx, a) => Fn.Number(Kth(Fn.Numbers(ctx, a[0]), Fn.Num(Fn.Scalar(ctx, a[1])), false)));
        foreach (var name in new[] { "RANK.EQ", "RANK" })
            r.Add(Cat, name + "(number, ref, [order])", "Returns the rank of a number in a list; ties get the top rank.", (ctx, a) => Rank(ctx, a, false));
        r.Add(Cat, "RANK.AVG(number, ref, [order])", "Returns the rank of a number in a list; ties get the average rank.", (ctx, a) => Rank(ctx, a, true));

        foreach (var (name, sample, all, root) in new[]
                 {
                     ("STDEV.S", true, false, true), ("STDEV", true, false, true), ("STDEV.P", false, false, true), ("STDEVP", false, false, true),
                     ("STDEVA", true, true, true), ("STDEVPA", false, true, true), ("VAR.S", true, false, false), ("VAR", true, false, false),
                     ("VAR.P", false, false, false), ("VARP", false, false, false), ("VARA", true, true, false), ("VARPA", false, true, false)
                 })
        {
            var what = (root ? "standard deviation" : "variance") + (sample ? " of a sample" : " of an entire population");
            r.Add(Cat, name + (all ? "(value1, [value2], ...)" : "(number1, [number2], ...)"),
                "Returns the " + what + (all ? ", counting text as 0 and booleans as 1 or 0." : "."), (ctx, a) =>
                {
                    var v = Variance(Fn.Numbers(ctx, a, all: all), sample);
                    return Fn.Number(root ? Math.Sqrt(v) : v);
                });
        }

        foreach (var name in new[] { "PERCENTILE.INC", "PERCENTILE" })
            r.Add(Cat, name + "(array, k)", "Returns the k-th percentile (0 to 1 inclusive).",
                (ctx, a) => Fn.Number(Percentile(Fn.Numbers(ctx, a[0]), Fn.Num(Fn.Scalar(ctx, a[1])), true)));
        r.Add(Cat, "PERCENTILE.EXC(array, k)", "Returns the k-th percentile (0 to 1 exclusive).",
            (ctx, a) => Fn.Number(Percentile(Fn.Numbers(ctx, a[0]), Fn.Num(Fn.Scalar(ctx, a[1])), false)));
        foreach (var name in new[] { "QUARTILE.INC", "QUARTILE" })
            r.Add(Cat, name + "(array, quart)", "Returns a quartile (0 to 4) of a data set.", (ctx, a) =>
            {
                var q = Math.Truncate(Fn.Num(Fn.Scalar(ctx, a[1])));
                return q is < 0 or > 4 ? CalcValue.Error(CellError.Num) : Fn.Number(Percentile(Fn.Numbers(ctx, a[0]), q / 4, true));
            });
        r.Add(Cat, "QUARTILE.EXC(array, quart)", "Returns a quartile (1 to 3) of a data set, exclusive.", (ctx, a) =>
        {
            var q = Math.Truncate(Fn.Num(Fn.Scalar(ctx, a[1])));
            return q is < 1 or > 3 ? CalcValue.Error(CellError.Num) : Fn.Number(Percentile(Fn.Numbers(ctx, a[0]), q / 4, false));
        });
        foreach (var name in new[] { "PERCENTRANK.INC", "PERCENTRANK" })
            r.Add(Cat, name + "(array, x, [significance])", "Returns the rank of a value as a percentage (0 to 1 inclusive) of the data set.", (ctx, a) =>
            {
                var n = Fn.Numbers(ctx, a[0]);
                n.Sort();
                var x = Fn.Num(Fn.Scalar(ctx, a[1]));
                var digits = a.Count > 2 ? Fn.Int(Fn.Scalar(ctx, a[2])) : 3;
                if (n.Count == 0 || digits < 1) return CalcValue.Error(CellError.Num);
                if (x < n[0] || x > n[^1]) return CalcValue.Error(CellError.NA);
                if (n.Count == 1) return CalcValue.Number(1);
                var less = n.Count(v => v < x);
                var rank = n.Contains(x) ? less : less - 1 + (x - n[less - 1]) / (n[less] - n[less - 1]);
                var factor = Math.Pow(10, digits);
                return Fn.Number(Math.Floor(rank / (n.Count - 1) * factor + 1e-9) / factor);
            });

        foreach (var name in new[] { "CORREL", "PEARSON" })
            r.Add(Cat, name + "(array1, array2)", "Returns the correlation coefficient of two data sets.",
                (ctx, a) => Paired(ctx, a, (x, y) => Covariance(x, y) / Math.Sqrt(Covariance(x, x) * Covariance(y, y))));
        r.Add(Cat, "RSQ(known_y's, known_x's)", "Returns the square of the Pearson correlation coefficient.",
            (ctx, a) => Paired(ctx, a, (y, x) => Math.Pow(Covariance(x, y), 2) / (Covariance(x, x) * Covariance(y, y))));
        foreach (var name in new[] { "COVARIANCE.P", "COVAR" })
            r.Add(Cat, name + "(array1, array2)", "Returns the population covariance of two data sets.",
                (ctx, a) => Paired(ctx, a, (x, y) => Covariance(x, y) / x.Count));
        r.Add(Cat, "COVARIANCE.S(array1, array2)", "Returns the sample covariance of two data sets.",
            (ctx, a) => Paired(ctx, a, (x, y) => Fn.Div(Covariance(x, y), x.Count - 1)));
        r.Add(Cat, "SLOPE(known_y's, known_x's)", "Returns the slope of the linear regression line.",
            (ctx, a) => Paired(ctx, a, (y, x) => Fn.Div(Covariance(x, y), Covariance(x, x))));
        r.Add(Cat, "INTERCEPT(known_y's, known_x's)", "Returns the intercept of the linear regression line.",
            (ctx, a) => Paired(ctx, a, (y, x) => y.Average() - Fn.Div(Covariance(x, y), Covariance(x, x)) * x.Average()));
        foreach (var name in new[] { "FORECAST.LINEAR", "FORECAST" })
            r.Add(Cat, name + "(x, known_y's, known_x's)", "Predicts a value along a linear trend.", (ctx, a) =>
            {
                var x0 = Fn.Num(Fn.Scalar(ctx, a[0]));
                return Paired(ctx, [a[1], a[2]], (y, x) =>
                {
                    var slope = Fn.Div(Covariance(x, y), Covariance(x, x));
                    return y.Average() + slope * (x0 - x.Average());
                });
            });
        r.Add(Cat, "STEYX(known_y's, known_x's)", "Returns the standard error of the predicted y-values of the regression.",
            (ctx, a) => Paired(ctx, a, (y, x) =>
            {
                if (x.Count < 3) throw Fn.Error(CellError.Div0);
                var sxy = Covariance(x, y);
                return Math.Sqrt((Covariance(y, y) - Fn.Div(sxy * sxy, Covariance(x, x))) / (x.Count - 2));
            }));

        r.Add(Cat, "GEOMEAN(number1, [number2], ...)", "Returns the geometric mean.", (ctx, a) =>
        {
            var n = Fn.Numbers(ctx, a);
            if (n.Count == 0 || n.Any(x => x <= 0)) return CalcValue.Error(CellError.Num);
            return Fn.Number(Math.Exp(n.Average(Math.Log)));
        });
        r.Add(Cat, "HARMEAN(number1, [number2], ...)", "Returns the harmonic mean.", (ctx, a) =>
        {
            var n = Fn.Numbers(ctx, a);
            if (n.Count == 0 || n.Any(x => x <= 0)) return CalcValue.Error(CellError.Num);
            return Fn.Number(n.Count / n.Sum(x => 1 / x));
        });
        r.Add(Cat, "AVEDEV(number1, [number2], ...)", "Returns the average of the absolute deviations from the mean.", (ctx, a) =>
        {
            var n = Fn.Numbers(ctx, a);
            if (n.Count == 0) return CalcValue.Error(CellError.Num);
            var mean = n.Average();
            return Fn.Number(n.Average(x => Math.Abs(x - mean)));
        });
        r.Add(Cat, "DEVSQ(number1, [number2], ...)", "Returns the sum of squares of deviations from the mean.", (ctx, a) =>
        {
            var n = Fn.Numbers(ctx, a);
            return n.Count == 0 ? CalcValue.Error(CellError.Num) : Fn.Number(Covariance(n, n));
        });
        r.Add(Cat, "SKEW(number1, [number2], ...)", "Returns the skewness of a distribution.", (ctx, a) =>
        {
            var n = Fn.Numbers(ctx, a);
            if (n.Count < 3) return CalcValue.Error(CellError.Div0);
            var (mean, sd, c) = (n.Average(), Math.Sqrt(Variance(n, true)), n.Count);
            if (sd == 0) return CalcValue.Error(CellError.Div0);
            return Fn.Number(c / ((c - 1.0) * (c - 2)) * n.Sum(x => Math.Pow((x - mean) / sd, 3)));
        });
        r.Add(Cat, "KURT(number1, [number2], ...)", "Returns the kurtosis of a data set.", (ctx, a) =>
        {
            var n = Fn.Numbers(ctx, a);
            if (n.Count < 4) return CalcValue.Error(CellError.Div0);
            var (mean, sd, c) = (n.Average(), Math.Sqrt(Variance(n, true)), (double)n.Count);
            if (sd == 0) return CalcValue.Error(CellError.Div0);
            var sum = n.Sum(x => Math.Pow((x - mean) / sd, 4));
            return Fn.Number(c * (c + 1) / ((c - 1) * (c - 2) * (c - 3)) * sum - 3 * (c - 1) * (c - 1) / ((c - 2) * (c - 3)));
        });
        r.Add(Cat, "STANDARDIZE(x, mean, standard_dev)", "Returns a normalized value.", Fn.Lift(a =>
        {
            var sd = Fn.Num(a[2]);
            return sd <= 0 ? throw Fn.Error(CellError.Num) : Fn.Number((Fn.Num(a[0]) - Fn.Num(a[1])) / sd);
        }));
        r.Add(Cat, "TRIMMEAN(array, percent)", "Returns the mean of the interior of a data set.", (ctx, a) =>
        {
            var n = Fn.Numbers(ctx, a[0]);
            var p = Fn.Num(Fn.Scalar(ctx, a[1]));
            if (p is < 0 or >= 1 || n.Count == 0) return CalcValue.Error(CellError.Num);
            var cut = (int)Math.Floor(n.Count * p / 2);
            n.Sort();
            return Fn.Number(n.Skip(cut).Take(n.Count - 2 * cut).Average());
        });
        r.Add(Cat, "FREQUENCY(data_array, bins_array)", "Returns a vertical array with the counts of values per bin.", (ctx, a) =>
        {
            var data = Fn.Numbers(ctx, a[0]);
            var bins = Fn.Numbers(ctx, a[1]);
            var sorted = bins.OrderBy(b => b).ToList();
            var counts = new double[bins.Count + 1];
            foreach (var x in data)
            {
                var slot = sorted.FindIndex(b => x <= b);
                counts[slot < 0 ? bins.Count : bins.IndexOf(sorted[slot])]++;
            }

            return Column(counts.ToList());
        });

        r.Add(Cat, "NORM.S.DIST(z, cumulative)", "Returns the standard normal distribution.",
            Fn.Lift(a => Fn.Number(Normal(Fn.Num(a[0]), Fn.Bool(a[1])))));
        r.Add(Cat, "NORM.DIST(x, mean, standard_dev, cumulative)", "Returns the normal distribution.", Fn.Lift(a =>
        {
            var sd = Fn.Num(a[2]);
            if (sd <= 0) throw Fn.Error(CellError.Num);
            var z = (Fn.Num(a[0]) - Fn.Num(a[1])) / sd;
            return Fn.Number(Fn.Bool(a[3]) ? Normal(z, true) : Normal(z, false) / sd);
        }));
        r.Add(Cat, "NORM.S.INV(probability)", "Returns the inverse of the standard normal cumulative distribution.",
            Fn.Lift(a => Fn.Number(NormalInverse(Fn.Num(a[0])))));
        r.Add(Cat, "NORM.INV(probability, mean, standard_dev)", "Returns the inverse of the normal cumulative distribution.", Fn.Lift(a =>
        {
            var sd = Fn.Num(a[2]);
            return sd <= 0 ? throw Fn.Error(CellError.Num) : Fn.Number(Fn.Num(a[1]) + sd * NormalInverse(Fn.Num(a[0])));
        }));
        r.Add(Cat, "EXPON.DIST(x, lambda, cumulative)", "Returns the exponential distribution.", Fn.Lift(a =>
        {
            var (x, l) = (Fn.Num(a[0]), Fn.Num(a[1]));
            if (x < 0 || l <= 0) throw Fn.Error(CellError.Num);
            return Fn.Number(Fn.Bool(a[2]) ? 1 - Math.Exp(-l * x) : l * Math.Exp(-l * x));
        }));
        r.Add(Cat, "POISSON.DIST(x, mean, cumulative)", "Returns the Poisson distribution.", Fn.Lift(a =>
        {
            var (k, mean) = (Math.Truncate(Fn.Num(a[0])), Fn.Num(a[1]));
            if (k < 0 || mean < 0) throw Fn.Error(CellError.Num);
            double Pmf(double i) => Math.Exp(i * Math.Log(mean) - mean - LogFactorial(i));
            if (!Fn.Bool(a[2])) return Fn.Number(mean == 0 ? (k == 0 ? 1 : 0) : Pmf(k));
            double sum = 0;
            for (var i = 0; i <= k; i++) sum += mean == 0 ? (i == 0 ? 1 : 0) : Pmf(i);
            return Fn.Number(Math.Min(1, sum));
        }));
        r.Add(Cat, "BINOM.DIST(number_s, trials, probability_s, cumulative)", "Returns the binomial distribution probability.", Fn.Lift(a =>
        {
            var (k, n, p) = (Math.Truncate(Fn.Num(a[0])), Math.Truncate(Fn.Num(a[1])), Fn.Num(a[2]));
            if (k < 0 || k > n || p is < 0 or > 1) throw Fn.Error(CellError.Num);
            double Pmf(double i) => Math.Exp(LogFactorial(n) - LogFactorial(i) - LogFactorial(n - i)) * Math.Pow(p, i) * Math.Pow(1 - p, n - i);
            if (!Fn.Bool(a[3])) return Fn.Number(Pmf(k));
            double sum = 0;
            for (var i = 0; i <= k; i++) sum += Pmf(i);
            return Fn.Number(Math.Min(1, sum));
        }));
    }

    internal static double Average(List<double> n) => Fn.Div(n.Sum(), n.Count);

    internal static double Variance(List<double> n, bool sample)
    {
        var count = n.Count - (sample ? 1 : 0);
        if (count <= 0) throw Fn.Error(CellError.Div0);
        return Covariance(n, n) / count;
    }

    private static double Covariance(List<double> x, List<double> y)
    {
        var (mx, my) = (x.Average(), y.Average());
        double s = 0;
        for (var i = 0; i < x.Count; i++) s += (x[i] - mx) * (y[i] - my);
        return s;
    }

    internal static double Median(List<double> n)
    {
        if (n.Count == 0) throw Fn.Error(CellError.Num);
        var s = n.OrderBy(x => x).ToList();
        return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2;
    }

    internal static double Mode(List<double> n)
    {
        var counts = new Dictionary<double, int>();
        double best = 0;
        var max = 1;
        foreach (var x in n)
        {
            var c = counts[x] = counts.GetValueOrDefault(x) + 1;
            if (c > max || (c == max && c > 1 && n.IndexOf(x) < n.IndexOf(best)))
            {
                max = c;
                best = x;
            }
        }

        return max < 2 ? throw Fn.Error(CellError.NA) : best;
    }

    internal static double Kth(List<double> n, double k, bool largest)
    {
        var i = (int)Math.Ceiling(k);
        if (i < 1 || i > n.Count) throw Fn.Error(CellError.Num);
        var s = largest ? n.OrderByDescending(x => x).ToList() : n.OrderBy(x => x).ToList();
        return s[i - 1];
    }

    internal static double Percentile(List<double> n, double k, bool inclusive)
    {
        if (n.Count == 0 || k < 0 || k > 1 || (!inclusive && (k <= 0 || k >= 1))) throw Fn.Error(CellError.Num);
        var s = n.OrderBy(x => x).ToList();
        var rank = inclusive ? k * (s.Count - 1) : k * (s.Count + 1) - 1;
        if (rank < 0 || rank > s.Count - 1) throw Fn.Error(CellError.Num);
        var lo = (int)Math.Floor(rank);
        return lo + 1 < s.Count ? s[lo] + (rank - lo) * (s[lo + 1] - s[lo]) : s[lo];
    }

    private static CalcValue MinMax(List<double> n, bool max) => CalcValue.Number(n.Count == 0 ? 0 : max ? n.Max() : n.Min());

    private static bool IsBlank(CellValue v) => v.Kind == CellValueKind.Empty || v is { Kind: CellValueKind.Text, TextValue: "" };

    private static CalcValue Column(List<double> values)
    {
        var result = new CalcArray(values.Count, 1);
        for (var i = 0; i < values.Count; i++) result[i, 0] = CellValue.Number(values[i]);
        return CalcValue.FromArray(result);
    }

    private static CalcValue Count(FunctionContext ctx, IReadOnlyList<CalcArg> args)
    {
        var count = 0;
        foreach (var arg in args)
        {
            if (arg.IsMissing) continue;
            var value = arg.Value;
            if (value.Kind == CalcValueKind.Scalar)
            {
                if (value.Scalar.Kind is CellValueKind.Number or CellValueKind.Boolean
                    || (value.Scalar.Kind == CellValueKind.Text && Coercion.TryToNumber(value.Scalar, out _, out _))) count++;
                continue;
            }

            count += ctx.Values(value).Count(v => v.Kind == CellValueKind.Number);
        }

        return CalcValue.Number(count);
    }

    private static CalcValue Rank(FunctionContext ctx, IReadOnlyList<CalcArg> args, bool average)
    {
        var x = Fn.Num(Fn.Scalar(ctx, args[0]));
        var n = Fn.Numbers(ctx, args[1]);
        var ascending = args.Count > 2 && Fn.Num(Fn.Scalar(ctx, args[2])) != 0;
        var ties = n.Count(v => v == x);
        if (ties == 0) return CalcValue.Error(CellError.NA);
        var better = n.Count(v => ascending ? v < x : v > x);
        return CalcValue.Number(better + 1 + (average ? (ties - 1) / 2.0 : 0));
    }

    private static CalcValue Paired(FunctionContext ctx, IReadOnlyList<CalcArg> args, Func<List<double>, List<double>, double> f)
    {
        var (a, b) = (Fn.Array(ctx, args[0]), Fn.Array(ctx, args[1]));
        if (a.Rows * a.Columns != b.Rows * b.Columns) return CalcValue.Error(CellError.NA);
        var (x, y) = (new List<double>(), new List<double>());
        for (var i = 0; i < a.Rows * a.Columns; i++)
        {
            var (u, v) = (a[i / a.Columns, i % a.Columns], b[i / b.Columns, i % b.Columns]);
            if (u.Kind == CellValueKind.Error) return u;
            if (v.Kind == CellValueKind.Error) return v;
            if (u.Kind != CellValueKind.Number || v.Kind != CellValueKind.Number) continue;
            x.Add(u.NumberValue);
            y.Add(v.NumberValue);
        }

        if (x.Count < 2) return CalcValue.Error(CellError.Div0);
        var result = f(x, y);
        return double.IsNaN(result) ? CalcValue.Error(CellError.Div0) : Fn.Number(result);
    }

    private static double LogFactorial(double n)
    {
        double s = 0;
        for (var i = 2; i <= n; i++) s += Math.Log(i);
        return s;
    }

    private static double Normal(double z, bool cumulative) =>
        cumulative ? 0.5 * Erfc(-z / Math.Sqrt(2)) : Math.Exp(-z * z / 2) / Math.Sqrt(2 * Math.PI);

    private static double Erfc(double x)
    {
        if (x < 0) return 2 - Erfc(-x);
        if (x >= 3)
        {
            var k = x;
            for (var n = 60; n >= 1; n--) k = x + n / 2.0 / k;
            return Math.Exp(-x * x) / (Math.Sqrt(Math.PI) * k);
        }

        double sum = 0, term = x;
        for (var n = 0; n < 200 && Math.Abs(term) > 1e-17; n++)
        {
            sum += term / (2 * n + 1);
            term *= -x * x / (n + 1);
        }

        return 1 - 2 / Math.Sqrt(Math.PI) * sum;
    }

    private static double NormalInverse(double p)
    {
        if (p is <= 0 or >= 1) throw Fn.Error(CellError.Num);
        double[] a = [-39.69683028665376, 220.9460984245205, -275.9285104469687, 138.3577518672690, -30.66479806614716, 2.506628277459239];
        double[] b = [-54.47609879822406, 161.5858368580409, -155.6989798598866, 66.80131188771972, -13.28068155288572];
        double[] c = [-0.007784894002430293, -0.3223964580411365, -2.400758277161838, -2.549732539343734, 4.374664141464968, 2.938163982698783];
        double[] d = [0.007784695709041462, 0.3224671290700398, 2.445134137142996, 3.754408661907416];
        double x;
        if (p < 0.02425 || p > 1 - 0.02425)
        {
            var q = Math.Sqrt(-2 * Math.Log(p < 0.5 ? p : 1 - p));
            x = (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
            if (p > 0.5) x = -x;
        }
        else
        {
            var q = p - 0.5;
            var r2 = q * q;
            x = (((((a[0] * r2 + a[1]) * r2 + a[2]) * r2 + a[3]) * r2 + a[4]) * r2 + a[5]) * q
                / (((((b[0] * r2 + b[1]) * r2 + b[2]) * r2 + b[3]) * r2 + b[4]) * r2 + 1);
        }

        for (var i = 0; i < 2; i++)
        {
            var e = Normal(x, true) - p;
            x -= e / Normal(x, false);
        }

        return x;
    }
}
