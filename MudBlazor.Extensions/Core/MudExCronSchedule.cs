using System.Globalization;

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
public sealed record MudExCronSchedule(MudExCronFrequency Frequency, int Interval = 15, int Minute = 0, int Hour = 6, IReadOnlyList<DayOfWeek> Days = null, int DayOfMonth = 1)
{
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
    /// Five fields (minute hour day month weekday) or six with seconds first. Every field is a comma separated list of
    /// <c>*</c>, a value, or a range <c>a-b</c> (a &lt;= b), each optionally followed by <c>/step</c> (1 up to the field's maximum).
    /// Values are numbers within the field's range, or names: JAN-DEC for the month, SUN-SAT for the weekday (Sunday is 0 or 7).
    /// Day and weekday also allow <c>?</c>. Quartz/Jenkins style extensions are accepted where cron allows them:
    /// <c>L</c> and <c>nW</c> for the day of the month, <c>nL</c> and <c>n#k</c> (k = 1-5) for the weekday. Nothing else, so typos like <c>FUNDAY</c> or <c>1-</c> are invalid.
    /// </summary>
    public static bool IsValid(string expression)
    {
        var f = Fields(expression);
        if (f is not { Length: 5 or 6 })
            return false;

        var specs = f.Length == 6 ? [Seconds, .. Specs] : Specs;
        return f.Select((field, i) => field.Split(',').All(part => ValidPart(part, specs[i]))).All(ok => ok);
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
    /// The schedule in words through <paramref name="text"/> (the localizer). With <paramref name="utc"/> the times say UTC, like most job servers.
    /// </summary>
    public string Describe(Func<string, object[], string> text, CultureInfo culture, bool utc = true)
    {
        var time = $"{Hour:00}:{Minute:00}";
        return Frequency switch
        {
            MudExCronFrequency.Minutes => text("Every {0} minutes", [Interval]),
            MudExCronFrequency.Hourly => text("Every hour at minute {0}", [Minute]),
            MudExCronFrequency.Daily => text(utc ? "Every day at {0} UTC" : "Every day at {0}", [time]),
            MudExCronFrequency.Weekly => text(utc ? "Every {0} at {1} UTC" : "Every {0} at {1}", [string.Join(", ", Weekdays.Distinct().Order().Select(d => culture.DateTimeFormat.GetDayName(d))), time]),
            MudExCronFrequency.Monthly => text(utc ? "Every month on day {0} at {1} UTC" : "Every month on day {0} at {1}", [DayOfMonth, time]),
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

    private static bool ValidPart(string part, FieldSpec spec)
    {
        if (spec.Kind == FieldKind.DayOfMonth && (part == "L" || part.EndsWith('W') && Value(part[..^1], spec) is >= 1))
            return true;

        if (spec.Kind != FieldKind.Plain && part == "?")
            return true;

        if (spec.Kind == FieldKind.Weekday)
        {
            if (part.EndsWith('L') && Value(part[..^1], spec) is not null)
                return true;

            var hash = part.Split('#');
            if (hash.Length == 2)
                return Value(hash[0], spec) is not null && Number(hash[1]) is >= 1 and <= 5;
        }

        var step = part.Split('/');
        if (step.Length > 2 || step.Length == 2 && !(Number(step[1]) is { } size && size >= 1 && size <= spec.Max))
            return false;

        if (step[0] == "*")
            return true;

        var bounds = step[0].Split('-');
        return bounds.Length switch
        {
            1 => Value(bounds[0], spec) is not null,
            2 => Value(bounds[0], spec) is { } from && Value(bounds[1], spec) is { } to && from <= to,
            _ => false,
        };
    }

    private static int? Value(string token, FieldSpec spec)
    {
        var n = Number(token);
        if (n is null && spec.Names is not null)
        {
            var index = Array.FindIndex(spec.Names, name => name.Equals(token, StringComparison.OrdinalIgnoreCase));
            n = index >= 0 ? spec.NamesFrom + index : null;
        }

        return n >= spec.Min && n <= spec.Max ? n : null;
    }
    private enum FieldKind { Plain, DayOfMonth, Weekday }

    private sealed record FieldSpec(int Min, int Max, FieldKind Kind = FieldKind.Plain, string[] Names = null, int NamesFrom = 0);

    private static readonly FieldSpec Seconds = new(0, 59);

    private static readonly FieldSpec[] Specs =
    [
        new(0, 59),
        new(0, 23),
        new(1, 31, FieldKind.DayOfMonth),
        new(1, 12, Names: ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"], NamesFrom: 1),
        new(0, 7, FieldKind.Weekday, ["SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"]),
    ];
}