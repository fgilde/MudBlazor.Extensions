using System.Numerics;
using Nextended.Core.Contracts;

namespace MudBlazor.Extensions.Helper;

internal static class RangeMath
{
    // Convert arbitrary numeric T to double for layout math
    public static double ToDouble<T>(T value) where T : INumber<T>
        => double.CreateChecked(value);


    public static T FromDouble<T>(double value) where T : INumber<T>
        => T.CreateChecked(value);


    public static T Clamp<T>(T value, T min, T max) where T : INumber<T>
        => T.Min(T.Max(value, min), max);


    public static T RoundToStep<T>(T value, T step, T min) where T : INumber<T>
    {
        if (step == T.Zero) return value;
        // Snap to steps from min
        var dv = ToDouble(value) - ToDouble(min);
        var ds = ToDouble(step);
        var snapped = Math.Round(dv / ds) * ds + ToDouble(min);
        return FromDouble<T>(snapped);
    }


    public static double Percent<T>(T value, T min, T max) where T : INumber<T>
    {
        var a = ToDouble(value);
        var mi = ToDouble(min);
        var ma = ToDouble(max);
        if (ma <= mi) return 0;
        return (a - mi) / (ma - mi);
    }


    public static T Lerp<T>(T min, T max, double percent) where T : INumber<T>
    {
        percent = Math.Clamp(percent, 0, 1);
        var mi = ToDouble(min);
        var ma = ToDouble(max);
        var val = mi + (ma - mi) * percent;
        return FromDouble<T>(val);
    }
}






public class Range<T>: MudExRange<T> where T : struct, IComparable<T>
{
    public Range(T start, T end) : base(start, end)
    {
    }
}

public class MudExRange<T> : IRange<T> where T : struct, IComparable<T>
{
    public T Start { get; private set; }
    public T End { get; private set; }


    public MudExRange(T start, T end)
    {
        if (start.CompareTo(end) <= 0)
        {
            Start = start; End = end;
        }
        else
        {
            Start = end; End = start;
        }
    }


    public bool Contains(T value) => value.CompareTo(Start) >= 0 && value.CompareTo(End) <= 0;
    public bool IsInRange(T value) => Contains(value);


    public bool Intersects(IRange<T> other)
        => other.End.CompareTo(Start) >= 0 && other.Start.CompareTo(End) <= 0;


    public IRange<T>? Intersection(IRange<T> other)
    {
        if (!Intersects(other)) return null;
        var s = Max(Start, other.Start);
        var e = Min(End, other.End);
        return new MudExRange<T>(s, e);
    }


    public IRange<T> Union(IRange<T> other)
    {
        // Require adjacency or overlap
        if (other.Start.CompareTo(End) > 0 && !Equals(End, other.Start))
            throw new InvalidOperationException("Ranges are separate – cannot union.");
        if (Start.CompareTo(other.End) > 0 && !Equals(Start, other.End))
            throw new InvalidOperationException("Ranges are separate – cannot union.");


        var s = Min(Start, other.Start);
        var e = Max(End, other.End);
        return new MudExRange<T>(s, e);
    }


    private static T Min(T a, T b) => a.CompareTo(b) <= 0 ? a : b;
    private static T Max(T a, T b) => a.CompareTo(b) >= 0 ? a : b;


    public override string ToString() => $"[{Start}..{End}]";
}