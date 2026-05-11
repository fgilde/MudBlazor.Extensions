using System.Globalization;
using MudBlazor.Extensions.Components;
using Nextended.Core.Contracts;
using Nextended.Core.Extensions;
using Nextended.Core.Types;
using Nextended.Core.Types.Ranges.Math;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Adaptive tick generation for the typed range sliders (<see cref="MudExDateRangeSlider"/>,
/// <see cref="MudExTimeRangeSlider"/>, <see cref="MudExDateTimeRangeSlider"/>).
/// Granularity is chosen based on the visible span so the track stays readable at any zoom level.
/// All percent calculations are delegated to <see cref="IRangeMath{T}"/>; this class only owns
/// calendar alignment, major/minor heuristics and labels.
/// </summary>
public static class RangeSliderTicks
{
    private const int MaxTicks = 200; // hard cap so we never DoS the DOM at extreme spans

    private static readonly IRangeMath<TimeOnly> TimeMath = RangeMathFactory.For<TimeOnly>();
    private static readonly IRangeMath<DateOnly> DateMath = RangeMathFactory.For<DateOnly>();
    private static readonly IRangeMath<DateTime> DateTimeMath = RangeMathFactory.For<DateTime>();

    // ------------------------------------------------------------
    // TimeOnly
    // ------------------------------------------------------------
    /// <summary>
    /// Generates ticks for a <see cref="TimeOnly"/> range slider given the currently visible span.
    /// </summary>
    public static IEnumerable<RangeTick<TimeOnly>> ForTime(IRange<TimeOnly> visible, TimeRangeSliderMode mode)
    {
        var startMin = (int)visible.Start.ToTimeSpan().TotalMinutes;
        var endMin = (int)visible.End.ToTimeSpan().TotalMinutes;
        var spanMin = Math.Max(1, endMin - startMin);

        var (minorMin, majorMin) = PickTimeGranularity(spanMin, mode);

        var firstTick = ((startMin + minorMin - 1) / minorMin) * minorMin;
        for (var totalMin = firstTick; totalMin <= endMin; totalMin += minorMin)
        {
            var time = new TimeOnly(totalMin / 60, totalMin % 60);
            var pct = TimeMath.Percent(time, visible) * 100.0;
            var isMajor = totalMin % majorMin == 0;
            var label = isMajor ? time.ToString("HH\\:mm", CultureInfo.CurrentCulture) : string.Empty;
            yield return new RangeTick<TimeOnly>(time, pct, isMajor, label);
        }
    }

    private static (int Minor, int Major) PickTimeGranularity(int spanMin, TimeRangeSliderMode mode)
    {
        int minor, major;
        if (spanMin > 720)      { minor = 60; major = 180; }
        else if (spanMin > 240) { minor = 30; major = 60; }
        else if (spanMin > 60)  { minor = 15; major = 60; }
        else if (spanMin > 15)  { minor = 5;  major = 15; }
        else                    { minor = 1;  major = 5; }

        // Mode acts as a floor – never render finer than the snap resolution.
        var floor = mode switch
        {
            TimeRangeSliderMode.Hour        => 60,
            TimeRangeSliderMode.HalfHour    => 30,
            TimeRangeSliderMode.QuarterHour => 15,
            TimeRangeSliderMode.FiveMinute  => 5,
            _                               => 1
        };
        if (minor < floor) minor = floor;
        if (major < minor) major = minor;
        return (minor, major);
    }

    // ------------------------------------------------------------
    // DateOnly
    // ------------------------------------------------------------
    /// <summary>
    /// Generates ticks for a <see cref="DateOnly"/> range slider given the currently visible span.
    /// </summary>
    public static IEnumerable<RangeTick<DateOnly>> ForDate(IRange<DateOnly> visible, DateRangeSliderMode mode)
    {
        var totalDays = Math.Max(1, visible.End.DayNumber - visible.Start.DayNumber);
        var granularity = PickDateGranularity(totalDays, mode);

        return granularity switch
        {
            DateGranularity.Day     => DailyTicks(visible),
            DateGranularity.Week    => WeeklyTicks(visible),
            DateGranularity.Month   => MonthlyTicks(visible),
            DateGranularity.Quarter => QuarterlyTicks(visible),
            _                       => YearlyTicks(visible)
        };
    }

    private enum DateGranularity { Day, Week, Month, Quarter, Year }

    private static DateGranularity PickDateGranularity(int totalDays, DateRangeSliderMode mode)
    {
        var byZoom = totalDays switch
        {
            <= 14   => DateGranularity.Day,
            <= 90   => DateGranularity.Week,
            <= 730  => DateGranularity.Month,
            <= 3650 => DateGranularity.Quarter,
            _       => DateGranularity.Year
        };
        var byMode = mode switch
        {
            DateRangeSliderMode.Year    => DateGranularity.Year,
            DateRangeSliderMode.Quarter => DateGranularity.Quarter,
            DateRangeSliderMode.Month   => DateGranularity.Month,
            DateRangeSliderMode.Week    => DateGranularity.Week,
            _                           => DateGranularity.Day
        };
        return byZoom < byMode ? byMode : byZoom;
    }

    private static IEnumerable<RangeTick<DateOnly>> DailyTicks(IRange<DateOnly> visible)
    {
        var tickCount = visible.End.DayNumber - visible.Start.DayNumber + 1;
        var step = Math.Max(1, tickCount / MaxTicks);
        for (var d = visible.Start; d <= visible.End; d = d.AddDays(step))
        {
            var pct = DateMath.Percent(d, visible) * 100.0;
            var isMajor = d.DayOfWeek == DayOfWeek.Monday;
            var label = isMajor ? d.ToString("dd.MM", CultureInfo.CurrentCulture) : string.Empty;
            yield return new RangeTick<DateOnly>(d, pct, isMajor, label);
        }
    }

    private static IEnumerable<RangeTick<DateOnly>> WeeklyTicks(IRange<DateOnly> visible)
    {
        var first = visible.Start;
        while (first.DayOfWeek != DayOfWeek.Monday && first <= visible.End) first = first.AddDays(1);
        for (var d = first; d <= visible.End; d = d.AddDays(7))
        {
            var pct = DateMath.Percent(d, visible) * 100.0;
            var isFirstOfMonth = d.Day <= 7;
            var label = isFirstOfMonth
                ? d.ToString("MMM", CultureInfo.CurrentCulture)
                : $"KW{ISOWeek.GetWeekOfYear(d.ToDateTime(TimeOnly.MinValue))}";
            yield return new RangeTick<DateOnly>(d, pct, isFirstOfMonth, label);
        }
    }

    private static IEnumerable<RangeTick<DateOnly>> MonthlyTicks(IRange<DateOnly> visible)
    {
        var first = new DateOnly(visible.Start.Year, visible.Start.Month, 1);
        if (first < visible.Start) first = first.AddMonths(1);
        for (var d = first; d <= visible.End; d = d.AddMonths(1))
        {
            var pct = DateMath.Percent(d, visible) * 100.0;
            var isMajor = d.Month == 1;
            var label = isMajor
                ? d.ToString("yyyy", CultureInfo.CurrentCulture)
                : d.ToString("MMM", CultureInfo.CurrentCulture);
            yield return new RangeTick<DateOnly>(d, pct, isMajor, label);
        }
    }

    private static IEnumerable<RangeTick<DateOnly>> QuarterlyTicks(IRange<DateOnly> visible)
    {
        var first = FirstOfQuarter(visible.Start);
        if (first < visible.Start) first = first.AddMonths(3);
        for (var d = first; d <= visible.End; d = d.AddMonths(3))
        {
            var pct = DateMath.Percent(d, visible) * 100.0;
            var isMajor = d.Month == 1;
            var quarter = (d.Month - 1) / 3 + 1;
            var label = isMajor ? d.ToString("yyyy", CultureInfo.CurrentCulture) : $"Q{quarter}";
            yield return new RangeTick<DateOnly>(d, pct, isMajor, label);
        }
    }

    private static IEnumerable<RangeTick<DateOnly>> YearlyTicks(IRange<DateOnly> visible)
    {
        var first = new DateOnly(visible.Start.Year, 1, 1);
        if (first < visible.Start) first = first.AddYears(1);
        for (var d = first; d <= visible.End; d = d.AddYears(1))
        {
            var pct = DateMath.Percent(d, visible) * 100.0;
            var isMajor = d.Year % 5 == 0;
            yield return new RangeTick<DateOnly>(d, pct, isMajor, d.Year.ToString(CultureInfo.CurrentCulture));
        }
    }

    private static DateOnly FirstOfQuarter(DateOnly d)
    {
        var qStartMonth = ((d.Month - 1) / 3) * 3 + 1;
        return new DateOnly(d.Year, qStartMonth, 1);
    }

    // ------------------------------------------------------------
    // DateTime
    // ------------------------------------------------------------
    /// <summary>
    /// Generates ticks for a <see cref="DateTime"/> range slider given the currently visible span.
    /// </summary>
    public static IEnumerable<RangeTick<DateTime>> ForDateTime(IRange<DateTime> visible, DateTimeRangeSliderMode mode)
    {
        var totalHours = (visible.End - visible.Start).TotalHours;
        if (totalHours <= 24)
            return SubDayTicks(visible);

        // Delegate longer spans to the date-only engine and lift the result back to DateTime.
        var dateMode = mode switch
        {
            DateTimeRangeSliderMode.Month => DateRangeSliderMode.Month,
            DateTimeRangeSliderMode.Week  => DateRangeSliderMode.Week,
            _                             => DateRangeSliderMode.Day
        };
        var dateRange = new MudExRange<DateOnly>(DateOnly.FromDateTime(visible.Start), DateOnly.FromDateTime(visible.End));
        return LiftDateTicks(ForDate(dateRange, dateMode), visible);
    }

    private static IEnumerable<RangeTick<DateTime>> SubDayTicks(IRange<DateTime> visible)
    {
        var spanMin = Math.Max(1, (int)(visible.End - visible.Start).TotalMinutes);
        int minorMin, majorMin;
        if (spanMin > 720)      { minorMin = 60; majorMin = 180; }
        else if (spanMin > 240) { minorMin = 30; majorMin = 60; }
        else if (spanMin > 60)  { minorMin = 15; majorMin = 60; }
        else if (spanMin > 15)  { minorMin = 5;  majorMin = 15; }
        else                    { minorMin = 1;  majorMin = 5; }

        var first = visible.Start.AddTicks(-(visible.Start.Ticks % TimeSpan.TicksPerMinute));
        var firstMinOffset = (long)(first - visible.Start).TotalMinutes;
        var startTotal = (long)Math.Ceiling(firstMinOffset / (double)minorMin) * minorMin;
        for (var minOffset = startTotal; ; minOffset += minorMin)
        {
            var t = first.AddMinutes(minOffset);
            if (t < visible.Start) continue;
            if (t > visible.End) yield break;
            var pct = DateTimeMath.Percent(t, visible) * 100.0;
            var isMajor = ((long)(t - DateTime.MinValue).TotalMinutes) % majorMin == 0;
            var label = isMajor ? t.ToString("HH\\:mm", CultureInfo.CurrentCulture) : string.Empty;
            yield return new RangeTick<DateTime>(t, pct, isMajor, label);
        }
    }

    private static IEnumerable<RangeTick<DateTime>> LiftDateTicks(IEnumerable<RangeTick<DateOnly>> dateTicks, IRange<DateTime> visible)
    {
        foreach (var t in dateTicks)
        {
            var dt = t.Value.ToDateTime(TimeOnly.MinValue);
            var pct = DateTimeMath.Percent(dt, visible) * 100.0;
            yield return new RangeTick<DateTime>(dt, pct, t.IsMajor, t.Label);
        }
    }
}
