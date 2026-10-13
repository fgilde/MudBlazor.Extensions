using System.Globalization;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// The frequencies <see cref="Components.MudExCronEdit"/> can build an expression for.
/// </summary>
public enum MudExCronFrequency
{
    /// <summary>Every n minutes</summary>
    Minutes,
    /// <summary>Every hour at a given minute</summary>
    Hourly,
    /// <summary>Every day at a given time</summary>
    Daily,
    /// <summary>On selected weekdays at a given time</summary>
    Weekly,
    /// <summary>On a day of the month at a given time</summary>
    Monthly,
    /// <summary>Anything else; the expression stays as written</summary>
    Custom,
}

/// <summary>
/// The common cron patterns as fields; anything else is <see cref="MudExCronFrequency.Custom"/> and stays as written.
/// </summary>
public sealed partial record MudExCronSchedule(MudExCronFrequency Frequency, int Interval = 15, int Minute = 0, int Hour = 6, IReadOnlyList<DayOfWeek> Days = null, int DayOfMonth = 1)
{
    private static readonly (int Min, int Max)[] Ranges = [(0, 59), (0, 23), (1, 31), (1, 12), (0, 7)];

    /// <summary>
    /// The selected weekdays; Monday if none are set.
    /// </summary>
    public IReadOnlyList<DayOfWeek> Weekdays => Days ?? [DayOfWeek.Monday];

    /// <summary>
    /// Reads an expression; everything the fields can't express, or that is not valid, is <see cref="MudExCronFrequency.Custom"/>.
    /// </summary>
    public static MudExCronSchedule Parse(string expression)
    {
        var f = Fields(expression);
        if (f is not { Length: 5 } || !IsValid(expression))
            return new(MudExCronFrequency.Custom);

        if (f[0].StartsWith("*/", StringComparison.Ordinal) && Number(f[0][2..]) is { } interval && interval > 0 && f[1..].All(x => x == "*"))
            return new(MudExCronFrequency.Minutes, Interval: interval);

        if (Number(f[0]) is not { } minute || f[3] != "*")
            return new(MudExCronFrequency.Custom);

        return (f[1], f[2], f[4]) switch
        {
            ("*", "*", "*") => new(MudExCronFrequency.Hourly, Minute: minute),
            (var h, "*", "*") when Number(h) is { } hour => new(MudExCronFrequency.Daily, Minute: minute, Hour: hour),
            (var h, "*", var d) when Number(h) is { } hour && WeekdayList(d) is { } days => new(MudExCronFrequency.Weekly, Minute: minute, Hour: hour, Days: days),
            (var h, var d, "*") when Number(h) is { } hour && Number(d) is { } day => new(MudExCronFrequency.Monthly, Minute: minute, Hour: hour, DayOfMonth: day),
            _ => new(MudExCronFrequency.Custom),
        };
    }

    /// <summary>
    /// Five fields (minute hour day month weekday) or six with seconds first; names, ranges, lists and steps allowed.
    /// </summary>
    public static bool IsValid(string expression)
    {
        var f = Fields(expression);
        if (f is not { Length: 5 or 6 })
            return false;

        var ranges = f.Length == 6 ? [(0, 59), .. Ranges] : Ranges;
        return f.Select((field, i) => FieldPattern().IsMatch(field) && Values().Matches(field).All(m => int.Parse(m.Value, CultureInfo.InvariantCulture) is var n && n >= ranges[i].Min && n <= ranges[i].Max)).All(ok => ok);
    }

    /// <summary>
    /// The expression of the fields; null for <see cref="MudExCronFrequency.Custom"/>, which keeps what was typed.
    /// </summary>
    public string ToExpression() => Frequency switch
    {
        MudExCronFrequency.Minutes => $"*/{Interval} * * * *",
        MudExCronFrequency.Hourly => $"{Minute} * * * *",
        MudExCronFrequency.Daily => $"{Minute} {Hour} * * *",
        MudExCronFrequency.Weekly => $"{Minute} {Hour} * * {string.Join(',', Weekdays.Distinct().Order().Select(d => (int)d))}",
        MudExCronFrequency.Monthly => $"{Minute} {Hour} {DayOfMonth} * *",
        _ => null,
    };

    /// <summary>
    /// The schedule in words through <paramref name="text"/> (the localizer); times are UTC like most job servers.
    /// </summary>
    public string Describe(Func<string, object[], string> text, CultureInfo culture)
    {
        var time = $"{Hour:00}:{Minute:00}";
        return Frequency switch
        {
            MudExCronFrequency.Minutes => text("Every {0} minutes", [Interval]),
            MudExCronFrequency.Hourly => text("Every hour at minute {0}", [Minute]),
            MudExCronFrequency.Daily => text("Every day at {0} UTC", [time]),
            MudExCronFrequency.Weekly => text("Every {0} at {1} UTC", [string.Join(", ", Weekdays.Distinct().Order().Select(d => culture.DateTimeFormat.GetDayName(d))), time]),
            MudExCronFrequency.Monthly => text("Every month on day {0} at {1} UTC", [DayOfMonth, time]),
            _ => text("Custom schedule", []),
        };
    }

    private static string[] Fields(string expression) => expression?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static int? Number(string field) => int.TryParse(field, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : null;

    // Sunday is 0 or 7 in cron; Monday-Friday as 1-5 or 1,2,3,4,5
    private static IReadOnlyList<DayOfWeek> WeekdayList(string field)
    {
        var days = new List<DayOfWeek>();
        foreach (var part in field.Split(','))
        {
            var bounds = part.Split('-');
            if (bounds.Length > 2 || bounds.Select(Number).ToList() is not [{ } from, ..] numbers || numbers[^1] is not { } to || from > to)
                return null;

            days.AddRange(Enumerable.Range(from, to - from + 1).Select(n => (DayOfWeek)(n % 7)));
        }

        return days.Distinct().Order().ToList();
    }

    [GeneratedRegex(@"^[0-9A-Za-z*?/,#\-]+$")]
    private static partial Regex FieldPattern();

    // the plain values, not steps (*/90) or the n-th weekday (5#3)
    [GeneratedRegex(@"(?<![/#\d])\d+")]
    private static partial Regex Values();
}
