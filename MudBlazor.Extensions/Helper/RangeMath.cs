using Nextended.Core.Contracts;
using Nextended.Core.Types;
using MudBlazor.Extensions.Components;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper;


internal static class RangeMathExtensions
{
   
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