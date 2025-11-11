using Nextended.Core.Contracts;
using System.Globalization;
using System.Numerics;

namespace MudBlazor.Extensions.Helper;


internal static class ValueMath<T> where T : IComparable<T>
{
    static bool IsDateTime => typeof(T) == typeof(DateTime);

    public static double ToDouble(T v)
    {
        if (IsDateTime) return ((DateTime)(object)v).Ticks;
        try { return Convert.ToDouble(v, CultureInfo.InvariantCulture); } catch { return 0d; }
    }

    public static T FromDouble(double d)
    {
        if (IsDateTime) return (T)(object)new DateTime(Convert.ToInt64(d));
        return (T)Convert.ChangeType(d, typeof(T), CultureInfo.InvariantCulture)!;
    }

    public static double Delta(IRange<T> r) => ToDouble(r.End) - ToDouble(r.Start);

    public static T AddDelta(T v, double delta)
    {
        if (IsDateTime)
        {
            var dt = (DateTime)(object)v;
            return (T)(object)new DateTime((long)(dt.Ticks + delta));
        }
        return FromDouble(ToDouble(v) + delta);
    }

    public static T Clamp(T v, IRange<T> bounds)
    {
        var d = ToDouble(v);
        var mi = ToDouble(bounds.Start);
        var ma = ToDouble(bounds.End);
        if (d < mi) return bounds.Start;
        if (d > ma) return bounds.End;
        return v;
    }

    public static double Percent(T v, IRange<T> size)
    {
        var a = ToDouble(v);
        var mi = ToDouble(size.Start);
        var ma = ToDouble(size.End);
        var span = Math.Max(1e-12, ma - mi);
        return (a - mi) / span;
    }

    public static T Lerp(IRange<T> size, double pct)
    {
        pct = Math.Clamp(pct, 0, 1);
        var mi = ToDouble(size.Start);
        var ma = ToDouble(size.End);
        return FromDouble(mi + (ma - mi) * pct);
    }

    public static T SnapToStep(T v, IRange<T> size, IRange<T> step)
    {
        var stepLen = Math.Abs(Delta(step));
        if (stepLen <= 0) return v;
        var dv = ToDouble(v) - ToDouble(size.Start);
        var snapped = Math.Round(dv / stepLen) * stepLen + ToDouble(size.Start);
        var res = FromDouble(snapped);
        return Clamp(res, size);
    }

    public static T AddSteps(T v, IRange<T> step, int steps)
    {
        var stepLen = Delta(step);
        return AddDelta(v, stepLen * steps);
    }
}