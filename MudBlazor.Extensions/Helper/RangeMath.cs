using Nextended.Core.Contracts;
using Nextended.Core.Types;
using MudBlazor.Extensions.Components;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper;


internal static class RangeMathExtensions
{
    private static MudExRange<T> OrderedRange<T>(T a, T b) where T : IComparable<T>
    {
        if (a.CompareTo(b) <= 0) return new MudExRange<T>(a, b);
        return new MudExRange<T>(b, a);
    }

    internal static IRange<T> EnforceMinMaxLengthSymmetric<T>(this IRangeMath<T> M,
        IRange<T> r, IRange<T> size,
        RangeLength<T>? min, RangeLength<T>? max) where T : IComparable<T>
    {
        var rr = r.Normalize();
        var len = M.Span(rr);

        var minLen = min.HasValue ? Math.Abs(min.Value.Delta) : double.NaN;
        var maxLen = max.HasValue ? Math.Abs(max.Value.Delta) : double.NaN;

        // expand to min (symmetrisch)
        if (min.HasValue && len < minLen)
        {
            var need = (minLen - len) / 2.0;
            var s = M.Add(rr.Start, -need);
            var e = M.Add(rr.End, +need);
            s = M.Clamp(s, size);
            e = M.Clamp(e, size);

            // falls Clamping ungleichmäßig war, erneut ordnen
            rr = OrderedRange(s, e);
            len = M.Span(rr);
        }

        // shrink to max (symmetrisch)
        if (max.HasValue && len > maxLen)
        {
            var over = (len - maxLen) / 2.0;
            var s = M.Add(rr.Start, +over);
            var e = M.Add(rr.End, -over);
            s = M.Clamp(s, size);
            e = M.Clamp(e, size);

            rr = OrderedRange(s, e);
        }

        return rr;
    }

    internal static IRange<T> EnforceMinMaxLength<T>(this IRangeMath<T> math, IRange<T> r, IRange<T> size, RangeLength<T>? min, RangeLength<T>? max, Thumb bias) where T : IComparable<T>
    {
        T s = r.Start;
        T e = r.End;
        if (s.CompareTo(e) > 0) { var tmp = s; s = e; e = tmp; }

        var len = Math.Abs(math.Difference(s, e));
        if (min.HasValue && len < Math.Abs(min.Value.Delta))
        {
            var need = Math.Abs(min.Value.Delta) - len;

            if (bias == Thumb.Start)
            {
                var newStart = math.Add(e, -need);
                s = math.Clamp(newStart, size);
            }
            else
            {
                var newEnd = math.Add(s, need);
                e = math.Clamp(newEnd, size);
            }
            if (s.CompareTo(e) > 0) { var tmp = s; s = e; e = tmp; }
            len = Math.Abs(math.Difference(s, e));
        }

        if (max.HasValue && len > Math.Abs(max.Value.Delta))
        {
            var over = len - Math.Abs(max.Value.Delta);

            if (bias == Thumb.Start)
            {
                var newEnd = math.Add(e, -over);
                e = math.Clamp(newEnd, size);
            }
            else
            {
                var newStart = math.Add(s, over);
                s = math.Clamp(newStart, size);
            }

            if (s.CompareTo(e) > 0) { var tmp = s; s = e; e = tmp; }
        }

        s = math.Clamp(s, size);
        e = math.Clamp(e, size);
        if (s.CompareTo(e) > 0) { var tmp = s; s = e; e = tmp; }

        return new MudExRange<T>(s, e);
    }


}