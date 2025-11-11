
using System;
using System.Globalization;

namespace MudBlazor.Extensions.Core
{
    /// <summary>
    /// Adapter API to map generic T to double for layout and back, plus stepping and clamping semantics.
    /// </summary>
    public interface IRangeAdapter<T>
    {
        double ToDouble(T v);
        T FromDouble(double d);

        double Percent(T v, T min, T max);
        T Lerp(T min, T max, double pct);

        T Clamp(T v, T min, T max);
        T Snap(T v, T min, T max);
        T AddSteps(T v, int steps);

        /// <summary>Converts a delta in percent of [min..max] to a delta-value (double-space). Used for whole-range dragging.</summary>
        double Scale(T max, T min, double pctDelta);
    }

    public static class RangeAdapters
    {
        /// <summary>
        /// Factory: returns a DateTimeAdapter when typeof(T) == DateTime; otherwise a numeric adapter that uses Convert.ToDouble/ChangeType.
        /// </summary>
        public static IRangeAdapter<T> For<T>(T? step, TimeSpan? stepSpan)
        {
            if (typeof(T) == typeof(DateTime))
                return (IRangeAdapter<T>)(object)new DateTimeAdapter(stepSpan ?? TimeSpan.FromDays(1));

            // Default numeric adapter: use Convert for common numeric types (int, long, double, decimal ...)
            double stepDouble = 1d;
            if (step is not null)
            {
                try { stepDouble = Convert.ToDouble(step, CultureInfo.InvariantCulture); }
                catch { stepDouble = 1d; }
            }
            return new NumericAdapter<T>(stepDouble);
        }
    }

    /// <summary>
    /// Numeric adapter using Convert.ToDouble/ChangeType. Works for common numeric Ts (int, long, float, double, decimal, etc.).
    /// </summary>
    public sealed class NumericAdapter<T> : IRangeAdapter<T>
    {
        private readonly double _step;
        public NumericAdapter(double step) => _step = Math.Abs(step) < 1e-12 ? 1d : step;

        public double ToDouble(T v) => Convert.ToDouble(v, CultureInfo.InvariantCulture);
        public T FromDouble(double d) => (T)Convert.ChangeType(d, typeof(T), CultureInfo.InvariantCulture)!;

        public double Percent(T v, T min, T max)
        {
            var dv = ToDouble(v) - ToDouble(min);
            var range = Math.Max(1e-12, ToDouble(max) - ToDouble(min));
            return dv / range;
        }

        public T Lerp(T min, T max, double pct)
        {
            pct = Math.Clamp(pct, 0, 1);
            var mi = ToDouble(min);
            var ma = ToDouble(max);
            return FromDouble(mi + (ma - mi) * pct);
        }

        public T Clamp(T v, T min, T max)
        {
            var d = ToDouble(v);
            var mi = ToDouble(min);
            var ma = ToDouble(max);
            return FromDouble(Math.Min(Math.Max(d, mi), ma));
        }

        public T Snap(T v, T min, T max)
        {
            // snap to steps from min
            var dv = ToDouble(v) - ToDouble(min);
            var snapped = Math.Round(dv / _step) * _step + ToDouble(min);
            var res = FromDouble(snapped);
            return Clamp(res, min, max);
        }

        public T AddSteps(T v, int steps) => FromDouble(ToDouble(v) + _step * steps);

        public double Scale(T max, T min, double pctDelta) => (ToDouble(max) - ToDouble(min)) * pctDelta;
    }

    /// <summary>
    /// DateTime adapter with given TimeSpan step. Uses ticks internally.
    /// </summary>
    public sealed class DateTimeAdapter : IRangeAdapter<DateTime>
    {
        private readonly TimeSpan _step;
        public DateTimeAdapter(TimeSpan step) => _step = step <= TimeSpan.Zero ? TimeSpan.FromDays(1) : step;

        public double ToDouble(DateTime v) => v.Ticks;
        public DateTime FromDouble(double d) => new DateTime(Convert.ToInt64(d));

        public double Percent(DateTime v, DateTime min, DateTime max)
        {
            var dv = (v - min).Ticks;
            var range = Math.Max(1.0, (max - min).Ticks);
            return dv / range;
        }

        public DateTime Lerp(DateTime min, DateTime max, double pct)
        {
            pct = Math.Clamp(pct, 0, 1);
            var ticks = (long)Math.Round((max - min).Ticks * pct);
            return min + TimeSpan.FromTicks(ticks);
        }

        public DateTime Clamp(DateTime v, DateTime min, DateTime max)
            => v < min ? min : (v > max ? max : v);

        public DateTime Snap(DateTime v, DateTime min, DateTime max)
        {
            var dv = (v - min).Ticks;
            var ds = _step.Ticks;
            if (ds <= 0) return v;
            var snapped = (long)(Math.Round(dv / (double)ds) * ds) + min.Ticks;
            var res = new DateTime(snapped, v.Kind);
            return Clamp(res, min, max);
        }

        public DateTime AddSteps(DateTime v, int steps) => v + TimeSpan.FromTicks(_step.Ticks * steps);

        public double Scale(DateTime max, DateTime min, double pctDelta) => (max - min).Ticks * pctDelta;
    }
}
