using MudBlazor.Extensions.Helper;
using Nextended.Core.Contracts;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

internal enum DragMode { None, StartThumb, EndThumb, WholeRange }

internal enum Thumb { Start, End }

internal record struct DomRect(double Left, double Top, double Width, double Height);

public static class RangeSnapPresets
{
    /// <summary>
    /// Mappt StepLength (Ticks) -> Monate pro Step. Du kannst deine eigene Funktion übergeben.
    /// Default: 1 Monat je ~30 Tage.
    /// </summary>
    public static Func<RangeLength<DateTime>, int> DefaultStepToMonths =
        step => Math.Max(1, (int)Math.Round(step.Delta / TimeSpan.FromDays(30).Ticks));

    /// <summary>
    /// Snap-Override für DateTime, der echte Monate snapt. Die Step-Länge kommt aus StepLength.
    /// </summary>
    public static Func<DateTime, IRange<DateTime>, RangeLength<DateTime>, SnapPolicy, DateTime>
        DateTimeMonthsSnap(Func<RangeLength<DateTime>, int>? stepToMonths = null, bool keepTimeOfDay = true)
    {
        var toMonths = stepToMonths ?? DefaultStepToMonths;
        return (value, size, stepLength, policy) =>
        {
            int months = toMonths(stepLength);
            var anchor = size.Start;

            int monthsBetween = (value.Year - anchor.Year) * 12 + (value.Month - anchor.Month);
            double n = (double)monthsBetween / months;

            double k = policy switch
            {
                SnapPolicy.Floor => Math.Floor(n),
                SnapPolicy.Ceiling => Math.Ceiling(n),
                _ => Math.Round(n, MidpointRounding.AwayFromZero)
            };

            var snapped = anchor.AddMonths((int)k * months);

            if (keepTimeOfDay)
            {
                snapped = new DateTime(
                    snapped.Year, snapped.Month, snapped.Day,
                    value.Hour, value.Minute, value.Second, value.Millisecond, value.Kind
                ).AddTicks(value.Ticks % TimeSpan.TicksPerMillisecond);
            }

            if (snapped < size.Start) return size.Start;
            if (snapped > size.End) return size.End;
            return snapped;
        };
    }

    /// <summary>
    /// AddSteps-Override für DateTime, der echte Monate addiert. Die Step-Länge kommt aus StepLength.
    /// </summary>
    public static Func<DateTime, RangeLength<DateTime>, int, DateTime>
        DateTimeMonthsAdd(Func<RangeLength<DateTime>, int>? stepToMonths = null)
    {
        var toMonths = stepToMonths ?? DefaultStepToMonths;
        return (value, stepLength, steps) =>
        {
            int months = toMonths(stepLength);
            return value.AddMonths(months * steps);
        };
    }

    // --- Entsprechende Varianten für DateOnly ---

    public static Func<RangeLength<DateOnly>, int> DefaultStepToMonthsDateOnly =
        step => Math.Max(1, (int)Math.Round(step.Delta / 30.0)); // DayNumber-Einheiten

    public static Func<DateOnly, IRange<DateOnly>, RangeLength<DateOnly>, SnapPolicy, DateOnly>
        DateOnlyMonthsSnap(Func<RangeLength<DateOnly>, int>? stepToMonths = null)
    {
        var toMonths = stepToMonths ?? DefaultStepToMonthsDateOnly;
        return (value, size, stepLength, policy) =>
        {
            int months = toMonths(stepLength);
            var anchor = size.Start;
            int monthsBetween = (value.Year - anchor.Year) * 12 + (value.Month - anchor.Month);
            double n = (double)monthsBetween / months;

            double k = policy switch
            {
                SnapPolicy.Floor => Math.Floor(n),
                SnapPolicy.Ceiling => Math.Ceiling(n),
                _ => Math.Round(n, MidpointRounding.AwayFromZero)
            };

            var snapped = anchor.AddMonths((int)k * months);
            if (snapped.CompareTo(size.Start) < 0) return size.Start;
            if (snapped.CompareTo(size.End) > 0) return size.End;
            return snapped;
        };
    }

    public static Func<DateOnly, RangeLength<DateOnly>, int, DateOnly>
        DateOnlyMonthsAdd(Func<RangeLength<DateOnly>, int>? stepToMonths = null)
    {
        var toMonths = stepToMonths ?? DefaultStepToMonthsDateOnly;
        return (value, stepLength, steps) =>
        {
            int months = toMonths(stepLength);
            return value.AddMonths(months * steps);
        };
    }
}
