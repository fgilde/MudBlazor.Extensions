namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class FinancialFunctions
{
    private const string Cat = "Financial";
    private const int MaxIterations = 20;
    private const double Tolerance = 1e-7;

    internal static void Register(FunctionRegistry r)
    {
        r.Add(Cat, "PMT(rate, nper, pv, [fv], [type])", "Returns the periodic payment of a loan with constant payments and rate.",
            Fn.Lift(a => Fn.Number(Pmt(Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]), Fn.Num(a, 3, 0), Type(a, 4)))));
        r.Add(Cat, "PV(rate, nper, pmt, [fv], [type])", "Returns the present value of an investment.", Fn.Lift(a =>
        {
            var (rate, n, pmt, fv, t) = (Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]), Fn.Num(a, 3, 0), Type(a, 4));
            if (rate == 0) return Fn.Number(-(fv + pmt * n));
            var g = Math.Pow(1 + rate, n);
            return Fn.Number(-(fv + pmt * (1 + rate * t) * (g - 1) / rate) / g);
        }));
        r.Add(Cat, "FV(rate, nper, pmt, [pv], [type])", "Returns the future value of an investment.",
            Fn.Lift(a => Fn.Number(Fv(Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]), Fn.Num(a, 3, 0), Type(a, 4)))));
        r.Add(Cat, "NPER(rate, pmt, pv, [fv], [type])", "Returns the number of periods of an investment.", Fn.Lift(a =>
        {
            var (rate, pmt, pv, fv, t) = (Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]), Fn.Num(a, 3, 0), Type(a, 4));
            if (rate == 0) return pmt == 0 ? throw Fn.Error(CellError.Num) : Fn.Number(-(pv + fv) / pmt);
            var ratio = (pmt * (1 + rate * t) - fv * rate) / (pv * rate + pmt * (1 + rate * t));
            return ratio <= 0 ? throw Fn.Error(CellError.Num) : Fn.Number(Math.Log(ratio) / Math.Log(1 + rate));
        }));
        r.Add(Cat, "IPMT(rate, per, nper, pv, [fv], [type])", "Returns the interest part of a payment for a period.",
            Fn.Lift(a => Fn.Number(Ipmt(Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]), Fn.Num(a[3]), Fn.Num(a, 4, 0), Type(a, 5)))));
        r.Add(Cat, "PPMT(rate, per, nper, pv, [fv], [type])", "Returns the principal part of a payment for a period.", Fn.Lift(a =>
        {
            var (rate, per, n, pv, fv, t) = (Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]), Fn.Num(a[3]), Fn.Num(a, 4, 0), Type(a, 5));
            return Fn.Number(Pmt(rate, n, pv, fv, t) - Ipmt(rate, per, n, pv, fv, t));
        }));
        r.Add(Cat, "ISPMT(rate, per, nper, pv)", "Returns the interest paid in a period of a loan with even principal payments.",
            Fn.Lift(a => Fn.Number(Fn.Num(a[3]) * Fn.Num(a[0]) * (Fn.Div(Fn.Num(a[1]), Fn.Num(a[2])) - 1))));
        r.Add(Cat, "CUMIPMT(rate, nper, pv, start_period, end_period, type)", "Returns the cumulative interest paid between two periods.",
            Fn.Lift(a => Cumulative(a, true)));
        r.Add(Cat, "CUMPRINC(rate, nper, pv, start_period, end_period, type)", "Returns the cumulative principal paid between two periods.",
            Fn.Lift(a => Cumulative(a, false)));
        r.Add(Cat, "NPV(rate, value1, [value2], ...)", "Returns the net present value of periodic cash flows at a discount rate.", (ctx, a) =>
        {
            var rate = Fn.Num(Fn.Scalar(ctx, a[0]));
            if (rate == -1) return CalcValue.Error(CellError.Div0);
            var values = Fn.Numbers(ctx, a, 1);
            return Fn.Number(values.Select((v, i) => v / Math.Pow(1 + rate, i + 1)).Sum());
        });
        r.Add(Cat, "IRR(values, [guess])", "Returns the internal rate of return of periodic cash flows.", (ctx, a) =>
        {
            var values = Fn.Numbers(ctx, a[0]);
            if (!values.Any(v => v > 0) || !values.Any(v => v < 0)) return CalcValue.Error(CellError.Num);
            var guess = a.Count > 1 && !a[1].IsMissing ? Fn.Num(Fn.Scalar(ctx, a[1])) : 0.1;
            return Newton(guess, MaxIterations, rate =>
            {
                double f = 0, df = 0;
                for (var i = 0; i < values.Count; i++)
                {
                    f += values[i] / Math.Pow(1 + rate, i);
                    df -= i * values[i] / Math.Pow(1 + rate, i + 1);
                }

                return (f, df);
            });
        });
        r.Add(Cat, "RATE(nper, pmt, pv, [fv], [type], [guess])", "Returns the interest rate per period of an annuity.", Fn.Lift(a =>
        {
            var (n, pmt, pv, fv, t) = (Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]), Fn.Num(a, 3, 0), Type(a, 4));
            if (n <= 0) throw Fn.Error(CellError.Num);
            return Newton(Fn.Num(a, 5, 0.1), MaxIterations, rate =>
            {
                if (Math.Abs(rate) < 1e-12) return (pv + pmt * n + fv, pv * n + pmt * (n * (n - 1) / 2 + t * n));
                var g = Math.Pow(1 + rate, n);
                var dg = n * Math.Pow(1 + rate, n - 1);
                var f = pv * g + pmt * (1 + rate * t) * (g - 1) / rate + fv;
                var df = pv * dg + pmt * t * (g - 1) / rate + pmt * (1 + rate * t) * (dg * rate - (g - 1)) / (rate * rate);
                return (f, df);
            });
        }));
        r.Add(Cat, "XNPV(rate, values, dates)", "Returns the net present value of cash flows at irregular dates.", (ctx, a) =>
        {
            var rate = Fn.Num(Fn.Scalar(ctx, a[0]));
            var (values, dates) = Flows(ctx, a[1], a[2]);
            if (rate <= -1) return CalcValue.Error(CellError.Num);
            return Fn.Number(values.Select((v, i) => v / Math.Pow(1 + rate, (dates[i] - dates[0]) / 365)).Sum());
        });
        r.Add(Cat, "XIRR(values, dates, [guess])", "Returns the internal rate of return of cash flows at irregular dates.", (ctx, a) =>
        {
            var (values, dates) = Flows(ctx, a[0], a[1]);
            if (!values.Any(v => v > 0) || !values.Any(v => v < 0)) return CalcValue.Error(CellError.Num);
            var guess = a.Count > 2 && !a[2].IsMissing ? Fn.Num(Fn.Scalar(ctx, a[2])) : 0.1;
            return Newton(guess, 100, rate =>
            {
                double f = 0, df = 0;
                for (var i = 0; i < values.Count; i++)
                {
                    var e = (dates[i] - dates[0]) / 365;
                    f += values[i] / Math.Pow(1 + rate, e);
                    df -= e * values[i] / Math.Pow(1 + rate, e + 1);
                }

                return (f, df);
            });
        });
        r.Add(Cat, "MIRR(values, finance_rate, reinvest_rate)", "Returns the modified internal rate of return.", (ctx, a) =>
        {
            var values = Fn.Numbers(ctx, a[0]);
            var (finance, reinvest) = (Fn.Num(Fn.Scalar(ctx, a[1])), Fn.Num(Fn.Scalar(ctx, a[2])));
            var n = values.Count;
            var positive = values.Select((v, i) => v > 0 ? v / Math.Pow(1 + reinvest, i + 1) : 0).Sum();
            var negative = values.Select((v, i) => v < 0 ? v / Math.Pow(1 + finance, i + 1) : 0).Sum();
            if (positive == 0 || negative == 0 || n < 2) return CalcValue.Error(CellError.Div0);
            return Fn.Number(Math.Pow(-positive * Math.Pow(1 + reinvest, n) / (negative * (1 + finance)), 1.0 / (n - 1)) - 1);
        });
        r.Add(Cat, "SLN(cost, salvage, life)", "Returns the straight-line depreciation for one period.",
            Fn.Lift(a => Fn.Number(Fn.Div(Fn.Num(a[0]) - Fn.Num(a[1]), Fn.Num(a[2])))));
        r.Add(Cat, "SYD(cost, salvage, life, per)", "Returns the sum-of-years' digits depreciation for a period.", Fn.Lift(a =>
        {
            var (cost, salvage, life, per) = (Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]), Fn.Num(a[3]));
            if (life <= 0 || per <= 0 || per > life) throw Fn.Error(CellError.Num);
            return Fn.Number((cost - salvage) * (life - per + 1) * 2 / (life * (life + 1)));
        }));
        r.Add(Cat, "DDB(cost, salvage, life, period, [factor])", "Returns the double-declining balance depreciation for a period.", Fn.Lift(a =>
        {
            var (cost, salvage, life, period, factor) = (Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]), Fn.Num(a[3]), Fn.Num(a, 4, 2));
            if (cost < 0 || salvage < 0 || life <= 0 || period <= 0 || period > life || factor <= 0) throw Fn.Error(CellError.Num);
            double book = cost, depreciation = 0;
            for (var p = 1; p <= Math.Ceiling(period); p++)
            {
                depreciation = Math.Max(0, Math.Min(book * factor / life, book - salvage));
                book -= depreciation;
            }

            return Fn.Number(depreciation);
        }));
        r.Add(Cat, "DB(cost, salvage, life, period, [month])", "Returns the fixed-declining balance depreciation for a period.", Fn.Lift(a =>
        {
            var (cost, salvage, life, period, month) = (Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]), Math.Truncate(Fn.Num(a[3])), Math.Truncate(Fn.Num(a, 4, 12)));
            if (cost < 0 || salvage < 0 || life <= 0 || period <= 0 || month is < 1 or > 12 || period > life + 1) throw Fn.Error(CellError.Num);
            var rate = cost == 0 ? 0 : Math.Round(1 - Math.Pow(salvage / cost, 1 / life), 3);
            double total = 0, depreciation = 0;
            for (var p = 1; p <= period; p++)
            {
                depreciation = p == 1 ? cost * rate * month / 12
                    : p == life + 1 ? (cost - total) * rate * (12 - month) / 12
                    : (cost - total) * rate;
                total += depreciation;
            }

            return Fn.Number(depreciation);
        }));
        r.Add(Cat, "EFFECT(nominal_rate, npery)", "Returns the effective annual interest rate.", Fn.Lift(a =>
        {
            var (rate, n) = (Fn.Num(a[0]), Math.Truncate(Fn.Num(a[1])));
            if (rate <= 0 || n < 1) throw Fn.Error(CellError.Num);
            return Fn.Number(Math.Pow(1 + rate / n, n) - 1);
        }));
        r.Add(Cat, "NOMINAL(effect_rate, npery)", "Returns the nominal annual interest rate.", Fn.Lift(a =>
        {
            var (rate, n) = (Fn.Num(a[0]), Math.Truncate(Fn.Num(a[1])));
            if (rate <= 0 || n < 1) throw Fn.Error(CellError.Num);
            return Fn.Number(n * (Math.Pow(1 + rate, 1 / n) - 1));
        }));
        r.Add(Cat, "FVSCHEDULE(principal, schedule)", "Returns the future value of a principal after a series of interest rates.",
            (ctx, a) => Fn.Number(Fn.Numbers(ctx, a[1]).Aggregate(Fn.Num(Fn.Scalar(ctx, a[0])), (p, rate) => p * (1 + rate))));
        r.Add(Cat, "PDURATION(rate, pv, fv)", "Returns the number of periods for an investment to reach a value.", Fn.Lift(a =>
        {
            var (rate, pv, fv) = (Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]));
            if (rate <= 0 || pv <= 0 || fv <= 0) throw Fn.Error(CellError.Num);
            return Fn.Number((Math.Log(fv) - Math.Log(pv)) / Math.Log(1 + rate));
        }));
        r.Add(Cat, "RRI(nper, pv, fv)", "Returns the equivalent interest rate for the growth of an investment.", Fn.Lift(a =>
        {
            var (n, pv, fv) = (Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]));
            if (n <= 0 || pv == 0) throw Fn.Error(CellError.Num);
            return Fn.Number(Math.Pow(fv / pv, 1 / n) - 1);
        }));
    }

    private static int Type(CellValue[] a, int i)
    {
        var t = Fn.Num(a, i, 0);
        return t == 0 ? 0 : 1;
    }

    private static double Pmt(double rate, double n, double pv, double fv, int t)
    {
        if (n == 0) throw Fn.Error(CellError.Num);
        if (rate == 0) return -(pv + fv) / n;
        var g = Math.Pow(1 + rate, n);
        return -(rate * (fv + pv * g)) / ((1 + rate * t) * (g - 1));
    }

    private static double Fv(double rate, double n, double pmt, double pv, int t)
    {
        if (rate == 0) return -(pv + pmt * n);
        var g = Math.Pow(1 + rate, n);
        return -(pv * g + pmt * (1 + rate * t) * (g - 1) / rate);
    }

    private static double Ipmt(double rate, double per, double n, double pv, double fv, int t)
    {
        if (per < 1 || per > n) throw Fn.Error(CellError.Num);
        if (t == 1 && per == 1) return 0;
        var pmt = Pmt(rate, n, pv, fv, t);
        var interest = Fv(rate, per - 1, pmt, pv, t) * rate;
        return t == 1 ? interest / (1 + rate) : interest;
    }

    private static CellValue Cumulative(CellValue[] a, bool interest)
    {
        var (rate, n, pv, start, end, t) = (Fn.Num(a[0]), Fn.Num(a[1]), Fn.Num(a[2]), Fn.Int(a[3]), Fn.Int(a[4]), Fn.Int(a[5]));
        if (rate <= 0 || n <= 0 || pv <= 0 || start < 1 || end < start || end > n || t is not (0 or 1)) throw Fn.Error(CellError.Num);
        double sum = 0;
        for (var p = start; p <= end; p++)
        {
            var i = Ipmt(rate, p, n, pv, 0, t);
            sum += interest ? i : Pmt(rate, n, pv, 0, t) - i;
        }

        return Fn.Number(sum);
    }

    private static (List<double> Values, List<double> Dates) Flows(FunctionContext ctx, CalcArg values, CalcArg dates)
    {
        var v = Fn.Numbers(ctx, values);
        var d = Fn.Numbers(ctx, dates).Select(Math.Floor).ToList();
        if (v.Count != d.Count || v.Count == 0) throw Fn.Error(CellError.Num);
        if (d.Any(x => x < d[0])) throw Fn.Error(CellError.Num);
        return (v, d);
    }

    private static CellValue Newton(double guess, int iterations, Func<double, (double F, double D)> f)
    {
        var rate = guess;
        for (var i = 0; i < iterations; i++)
        {
            var (value, derivative) = f(rate);
            if (!double.IsFinite(value) || !double.IsFinite(derivative) || derivative == 0) break;
            var next = rate - value / derivative;
            if (Math.Abs(next - rate) < Tolerance) return Fn.Number(next);
            rate = next;
        }

        return CellValue.Error(CellError.Num);
    }
}
