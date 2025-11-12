// File: StepResolvers.cs
// Drop this into your MudBlazor.Extensions.Helper (or a shared helpers) project.
// It provides unified StepResolvers delegates for DateTime, DateOnly and TimeOnly:
// Monthly, Quarterly, Yearly, Weekly, Daily (+ Hour/Minute/Second where applicable).
//
// Usage in your component:
//    [Parameter] public Func<T, IRange<T>, RangeLength<T>, int, SnapPolicy, T>? StepResolver { get; set; }
// Then:
//    StepResolver="@(StepResolvers.DateTime.Monthly())" // snap + add based on StepLength
//
// All resolvers treat steps==0 as "Snap" and steps!=0 as "AddSteps".
// StepLength is *always* consulted via the provided (or default) converter.

using System;
using Nextended.Core.Contracts; // IRange<T>
using Nextended.Core.Types;     // RangeLength<T>

namespace MudBlazor.Extensions.Helper;



public static class StepResolvers
{
    // ----------------------------
    // DateTime resolvers
    // ----------------------------
    public static class DateTime
    {
        /// <summary>Default mapping: ~30 Tage ≈ 1 Monat.</summary>
        public static Func<RangeLength<System.DateTime>, int> DefaultStepToMonths
            = step => Math.Max(1, (int)Math.Round(step.Delta / TimeSpan.FromDays(30).Ticks));

        /// <summary>Default mapping: ~365 Tage ≈ 1 Jahr.</summary>
        public static Func<RangeLength<System.DateTime>, int> DefaultStepToYears
            = step => Math.Max(1, (int)Math.Round(step.Delta / TimeSpan.FromDays(365).Ticks));

        /// <summary>Default mapping: 7 Tage = 1 Woche.</summary>
        public static Func<RangeLength<System.DateTime>, int> DefaultStepToWeeks
            = step => Math.Max(1, (int)Math.Round(step.Delta / TimeSpan.FromDays(7).Ticks));

        /// <summary>Default mapping: 1 Tag = 1 Einheit.</summary>
        public static Func<RangeLength<System.DateTime>, int> DefaultStepToDays
            = step => Math.Max(1, (int)Math.Round(step.Delta / TimeSpan.FromDays(1).Ticks));

        /// <summary>Default mapping: 3 Monate = 1 Quartal (aus Months-Konverter abgeleitet).</summary>
        public static Func<RangeLength<System.DateTime>, int> DefaultStepToQuarters
            = step => Math.Max(1, DefaultStepToMonths(step) / 3 == 0 ? 1 : DefaultStepToMonths(step) / 3);

        public static Func<RangeLength<System.DateTime>, int> DefaultStepToHours
            = step => Math.Max(1, (int)Math.Round(step.Delta / TimeSpan.FromHours(1).Ticks));

        public static Func<RangeLength<System.DateTime>, int> DefaultStepToMinutes
            = step => Math.Max(1, (int)Math.Round(step.Delta / TimeSpan.FromMinutes(1).Ticks));

        public static Func<RangeLength<System.DateTime>, int> DefaultStepToSeconds
            = step => Math.Max(1, (int)Math.Round(step.Delta / TimeSpan.FromSeconds(1).Ticks));

        public static Func<System.DateTime, IRange<System.DateTime>, RangeLength<System.DateTime>, int, SnapPolicy, System.DateTime>
            Monthly(Func<RangeLength<System.DateTime>, int>? toMonths = null, bool keepTimeOfDay = true)
            => CalendarMonthBased(toMonths ?? DefaultStepToMonths, monthsPerStep: 1, keepTimeOfDay);

        public static Func<System.DateTime, IRange<System.DateTime>, RangeLength<System.DateTime>, int, SnapPolicy, System.DateTime>
            Quarterly(Func<RangeLength<System.DateTime>, int>? toQuarters = null, bool keepTimeOfDay = true)
            => CalendarMonthBased(
                step => (toQuarters ?? DefaultStepToQuarters)(step) * 3 /* 1 Quarter = 3 Months */,
                monthsPerStep: 1,
                keepTimeOfDay: keepTimeOfDay);

        public static Func<System.DateTime, IRange<System.DateTime>, RangeLength<System.DateTime>, int, SnapPolicy, System.DateTime>
            Yearly(Func<RangeLength<System.DateTime>, int>? toYears = null, bool keepTimeOfDay = true)
            => CalendarYearBased(toYears ?? DefaultStepToYears, keepTimeOfDay);

        public static Func<System.DateTime, IRange<System.DateTime>, RangeLength<System.DateTime>, int, SnapPolicy, System.DateTime>
            Weekly(Func<RangeLength<System.DateTime>, int>? toWeeks = null, bool keepTimeOfDay = true)
            => ConstantUnit(TimeSpan.FromDays(7), toWeeks ?? DefaultStepToWeeks, keepTimeOfDay);

        public static Func<System.DateTime, IRange<System.DateTime>, RangeLength<System.DateTime>, int, SnapPolicy, System.DateTime>
            Daily(Func<RangeLength<System.DateTime>, int>? toDays = null, bool keepTimeOfDay = true)
            => ConstantUnit(TimeSpan.FromDays(1), toDays ?? DefaultStepToDays, keepTimeOfDay);

        public static Func<System.DateTime, IRange<System.DateTime>, RangeLength<System.DateTime>, int, SnapPolicy, System.DateTime>
            Hourly(Func<RangeLength<System.DateTime>, int>? toHours = null)
            => ConstantUnit(TimeSpan.FromHours(1), toHours ?? DefaultStepToHours, keepTime: true);

        public static Func<System.DateTime, IRange<System.DateTime>, RangeLength<System.DateTime>, int, SnapPolicy, System.DateTime>
            Minutely(Func<RangeLength<System.DateTime>, int>? toMinutes = null)
            => ConstantUnit(TimeSpan.FromMinutes(1), toMinutes ?? DefaultStepToMinutes, keepTime: true);

        public static Func<System.DateTime, IRange<System.DateTime>, RangeLength<System.DateTime>, int, SnapPolicy, System.DateTime>
            Secondly(Func<RangeLength<System.DateTime>, int>? toSeconds = null)
            => ConstantUnit(TimeSpan.FromSeconds(1), toSeconds ?? DefaultStepToSeconds, keepTime: true);

        // ----- helpers -----

        // Month-based (true calendar months)
        private static Func<System.DateTime, IRange<System.DateTime>, RangeLength<System.DateTime>, int, SnapPolicy, System.DateTime>
            CalendarMonthBased(Func<RangeLength<System.DateTime>, int> toMonths, int monthsPerStep, bool keepTimeOfDay)
            => (value, size, stepLength, steps, policy) =>
            {
                int months = Math.Max(1, toMonths(stepLength)) * monthsPerStep;
                var anchor = size.Start;

                if (steps != 0)
                    return value.AddMonths(months * steps);

                // snap
                var between = (value.Year - anchor.Year) * 12 + (value.Month - anchor.Month);
                double n = (double)between / months;
                double k = policy switch
                {
                    SnapPolicy.Floor => Math.Floor(n),
                    SnapPolicy.Ceiling => Math.Ceiling(n),
                    _ => Math.Round(n, MidpointRounding.AwayFromZero)
                };
                var snapped = anchor.AddMonths((int)k * months);

                if (keepTimeOfDay)
                {
                    snapped = new System.DateTime(
                            snapped.Year, snapped.Month, snapped.Day,
                            value.Hour, value.Minute, value.Second, value.Millisecond, value.Kind)
                        .AddTicks(value.Ticks % TimeSpan.TicksPerMillisecond);
                }

                if (snapped < size.Start) return size.Start;
                if (snapped > size.End) return size.End;
                return snapped;
            };

        // Year-based (true calendar years)
        private static Func<System.DateTime, IRange<System.DateTime>, RangeLength<System.DateTime>, int, SnapPolicy, System.DateTime>
            CalendarYearBased(Func<RangeLength<System.DateTime>, int> toYears, bool keepTimeOfDay)
            => (value, size, stepLength, steps, policy) =>
            {
                int years = Math.Max(1, toYears(stepLength));
                var anchor = size.Start;

                if (steps != 0)
                    return value.AddYears(years * steps);

                // snap
                var between = value.Year - anchor.Year;
                double n = (double)between / years;
                double k = policy switch
                {
                    SnapPolicy.Floor => Math.Floor(n),
                    SnapPolicy.Ceiling => Math.Ceiling(n),
                    _ => Math.Round(n, MidpointRounding.AwayFromZero)
                };
                var snapped = anchor.AddYears((int)k * years);

                if (keepTimeOfDay)
                {
                    snapped = new System.DateTime(
                            snapped.Year, snapped.Month, snapped.Day,
                            value.Hour, value.Minute, value.Second, value.Millisecond, value.Kind)
                        .AddTicks(value.Ticks % TimeSpan.TicksPerMillisecond);
                }

                if (snapped < size.Start) return size.Start;
                if (snapped > size.End) return size.End;
                return snapped;
            };

        // Constant-duration (weeks/days/hours/minutes/seconds) – precise with TimeSpan
        private static Func<System.DateTime, IRange<System.DateTime>, RangeLength<System.DateTime>, int, SnapPolicy, System.DateTime>
            ConstantUnit(TimeSpan unit, Func<RangeLength<System.DateTime>, int> toUnitsPerStep, bool keepTime = true)
            => (value, size, stepLength, steps, policy) =>
            {
                int units = Math.Max(1, toUnitsPerStep(stepLength));
                long ticksPerStep = unit.Ticks * units;
                var anchor = size.Start;

                if (steps != 0)
                    return value.AddTicks(ticksPerStep * steps);

                // snap
                long dvTicks = value.Ticks - anchor.Ticks;
                double n = (double)dvTicks / ticksPerStep;
                double k = policy switch
                {
                    SnapPolicy.Floor => Math.Floor(n),
                    SnapPolicy.Ceiling => Math.Ceiling(n),
                    _ => Math.Round(n, MidpointRounding.AwayFromZero)
                };
                var snapped = anchor.AddTicks((long)(k * ticksPerStep));

                if (!keepTime)
                    snapped = new System.DateTime(snapped.Year, snapped.Month, snapped.Day, 0, 0, 0, snapped.Kind);

                if (snapped < size.Start) return size.Start;
                if (snapped > size.End) return size.End;
                return snapped;
            };
    }

    // ----------------------------
    // DateOnly resolvers
    // ----------------------------
    public static class DateOnly
    {
        public static Func<RangeLength<System.DateOnly>, int> DefaultStepToMonths
            = step => Math.Max(1, (int)Math.Round(step.Delta / 30.0));

        public static Func<RangeLength<System.DateOnly>, int> DefaultStepToYears
            = step => Math.Max(1, (int)Math.Round(step.Delta / 365.0));

        public static Func<RangeLength<System.DateOnly>, int> DefaultStepToWeeks
            = step => Math.Max(1, (int)Math.Round(step.Delta / 7.0));

        public static Func<RangeLength<System.DateOnly>, int> DefaultStepToDays
            = step => Math.Max(1, (int)Math.Round(step.Delta / 1.0));

        public static Func<RangeLength<System.DateOnly>, int> DefaultStepToQuarters
            = step =>
            {
                var m = DefaultStepToMonths(step);
                return Math.Max(1, m / 3 == 0 ? 1 : m / 3);
            };

        public static Func<System.DateOnly, IRange<System.DateOnly>, RangeLength<System.DateOnly>, int, SnapPolicy, System.DateOnly>
            Monthly(Func<RangeLength<System.DateOnly>, int>? toMonths = null)
            => CalendarMonthBased(toMonths ?? DefaultStepToMonths, monthsPerStep: 1);

        public static Func<System.DateOnly, IRange<System.DateOnly>, RangeLength<System.DateOnly>, int, SnapPolicy, System.DateOnly>
            Quarterly(Func<RangeLength<System.DateOnly>, int>? toQuarters = null)
            => CalendarMonthBased(step => (toQuarters ?? DefaultStepToQuarters)(step) * 3, monthsPerStep: 1);

        public static Func<System.DateOnly, IRange<System.DateOnly>, RangeLength<System.DateOnly>, int, SnapPolicy, System.DateOnly>
            Yearly(Func<RangeLength<System.DateOnly>, int>? toYears = null)
            => CalendarYearBased(toYears ?? DefaultStepToYears);

        public static Func<System.DateOnly, IRange<System.DateOnly>, RangeLength<System.DateOnly>, int, SnapPolicy, System.DateOnly>
            Weekly(Func<RangeLength<System.DateOnly>, int>? toWeeks = null)
            => ConstantDays(toWeeks ?? DefaultStepToWeeks, daysPerUnit: 7);

        public static Func<System.DateOnly, IRange<System.DateOnly>, RangeLength<System.DateOnly>, int, SnapPolicy, System.DateOnly>
            Daily(Func<RangeLength<System.DateOnly>, int>? toDays = null)
            => ConstantDays(toDays ?? DefaultStepToDays, daysPerUnit: 1);

        // ----- helpers -----

        private static Func<System.DateOnly, IRange<System.DateOnly>, RangeLength<System.DateOnly>, int, SnapPolicy, System.DateOnly>
            CalendarMonthBased(Func<RangeLength<System.DateOnly>, int> toMonths, int monthsPerStep)
            => (value, size, stepLength, steps, policy) =>
            {
                int months = Math.Max(1, toMonths(stepLength)) * monthsPerStep;
                var anchor = size.Start;

                if (steps != 0)
                    return value.AddMonths(months * steps);

                // snap
                int between = (value.Year - anchor.Year) * 12 + (value.Month - anchor.Month);
                double n = (double)between / months;
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

        private static Func<System.DateOnly, IRange<System.DateOnly>, RangeLength<System.DateOnly>, int, SnapPolicy, System.DateOnly>
            CalendarYearBased(Func<RangeLength<System.DateOnly>, int> toYears)
            => (value, size, stepLength, steps, policy) =>
            {
                int years = Math.Max(1, toYears(stepLength));
                var anchor = size.Start;

                if (steps != 0)
                    return value.AddYears(years * steps);

                // snap
                int between = value.Year - anchor.Year;
                double n = (double)between / years;
                double k = policy switch
                {
                    SnapPolicy.Floor => Math.Floor(n),
                    SnapPolicy.Ceiling => Math.Ceiling(n),
                    _ => Math.Round(n, MidpointRounding.AwayFromZero)
                };
                var snapped = anchor.AddYears((int)k * years);

                if (snapped.CompareTo(size.Start) < 0) return size.Start;
                if (snapped.CompareTo(size.End) > 0) return size.End;
                return snapped;
            };

        private static Func<System.DateOnly, IRange<System.DateOnly>, RangeLength<System.DateOnly>, int, SnapPolicy, System.DateOnly>
            ConstantDays(Func<RangeLength<System.DateOnly>, int> toUnitsPerStep, int daysPerUnit)
            => (value, size, stepLength, steps, policy) =>
            {
                int units = Math.Max(1, toUnitsPerStep(stepLength));
                int daysPerStep = units * daysPerUnit;
                var anchor = size.Start;

                if (steps != 0)
                    return value.AddDays(daysPerStep * steps);

                // snap
                int dv = value.DayNumber - anchor.DayNumber;
                double n = (double)dv / daysPerStep;
                double k = policy switch
                {
                    SnapPolicy.Floor => Math.Floor(n),
                    SnapPolicy.Ceiling => Math.Ceiling(n),
                    _ => Math.Round(n, MidpointRounding.AwayFromZero)
                };
                var snapped = anchor.AddDays((int)k * daysPerStep);

                if (snapped.CompareTo(size.Start) < 0) return size.Start;
                if (snapped.CompareTo(size.End) > 0) return size.End;
                return snapped;
            };
    }

    // ----------------------------
    // TimeOnly resolvers (Hour/Minute/Second)
    // ----------------------------
    public static class TimeOnly
    {
        public static Func<RangeLength<System.TimeOnly>, int> DefaultStepToHours
            = step => Math.Max(1, (int)Math.Round(step.Delta / TimeSpan.TicksPerHour));

        public static Func<RangeLength<System.TimeOnly>, int> DefaultStepToMinutes
            = step => Math.Max(1, (int)Math.Round(step.Delta / TimeSpan.TicksPerMinute));

        public static Func<RangeLength<System.TimeOnly>, int> DefaultStepToSeconds
            = step => Math.Max(1, (int)Math.Round(step.Delta / TimeSpan.TicksPerSecond));

        public static Func<System.TimeOnly, IRange<System.TimeOnly>, RangeLength<System.TimeOnly>, int, SnapPolicy, System.TimeOnly>
            Hourly(Func<RangeLength<System.TimeOnly>, int>? toHours = null)
            => ConstantUnit(System.TimeSpan.FromHours(1), toHours ?? DefaultStepToHours);

        public static Func<System.TimeOnly, IRange<System.TimeOnly>, RangeLength<System.TimeOnly>, int, SnapPolicy, System.TimeOnly>
            Minutely(Func<RangeLength<System.TimeOnly>, int>? toMinutes = null)
            => ConstantUnit(System.TimeSpan.FromMinutes(1), toMinutes ?? DefaultStepToMinutes);

        public static Func<System.TimeOnly, IRange<System.TimeOnly>, RangeLength<System.TimeOnly>, int, SnapPolicy, System.TimeOnly>
            Secondly(Func<RangeLength<System.TimeOnly>, int>? toSeconds = null)
            => ConstantUnit(System.TimeSpan.FromSeconds(1), toSeconds ?? DefaultStepToSeconds);

        // ----- helpers -----
        private static Func<System.TimeOnly, IRange<System.TimeOnly>, RangeLength<System.TimeOnly>, int, SnapPolicy, System.TimeOnly>
            ConstantUnit(System.TimeSpan unit, Func<RangeLength<System.TimeOnly>, int> toUnitsPerStep)
            => (value, size, stepLength, steps, policy) =>
            {
                int units = Math.Max(1, toUnitsPerStep(stepLength));
                long ticksPerStep = unit.Ticks * units;
                var anchor = size.Start;

                if (steps != 0)
                    return value.Add(TimeSpan.FromTicks(ticksPerStep * steps));

                // snap
                long dvTicks = value.Ticks - anchor.Ticks;
                double n = (double)dvTicks / ticksPerStep;
                double k = policy switch
                {
                    SnapPolicy.Floor => Math.Floor(n),
                    SnapPolicy.Ceiling => Math.Ceiling(n),
                    _ => Math.Round(n, MidpointRounding.AwayFromZero)
                };
                var snapped = anchor.Add(TimeSpan.FromTicks((long)(k * ticksPerStep)));

                if (snapped < size.Start) return size.Start;
                if (snapped > size.End) return size.End;
                return snapped;
            };
    }
}