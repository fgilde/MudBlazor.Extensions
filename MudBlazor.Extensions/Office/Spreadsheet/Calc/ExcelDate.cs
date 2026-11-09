namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Converts between Excel date serials (1900 date system) and dates. Serial 1 is 1 January 1900 and the fraction is the
/// time of day. Like Excel, the calendar contains the fictitious 29 February 1900 (serial 60), so serials from 61 on match
/// the real calendar shifted by one day.
/// </summary>
public static class ExcelDate
{
    /// <summary>The largest valid serial, 31 December 9999.</summary>
    public const double MaxSerial = 2958465;

    private static readonly DateTime Epoch = new(1899, 12, 31);

    /// <summary>Converts a date and time to its serial.</summary>
    /// <param name="value">The date and time.</param>
    /// <returns>The serial, for example 45292.5 for 1 January 2024 12:00.</returns>
    public static double ToSerial(DateTime value)
    {
        var days = (value.Date - Epoch).Days;
        if (days >= 60) days++;
        return days + value.TimeOfDay.TotalDays;
    }

    /// <summary>Converts a serial to a date and time. Serial 60, the fictitious 29 February 1900, gives 28 February 1900.</summary>
    /// <param name="serial">The serial (0 to <see cref="MaxSerial"/>).</param>
    /// <returns>The date and time.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The serial is negative or beyond <see cref="MaxSerial"/>.</exception>
    public static DateTime ToDateTime(double serial)
    {
        if (!(serial >= 0 && serial < MaxSerial + 1)) throw new ArgumentOutOfRangeException(nameof(serial));
        var days = Math.Floor(serial);
        var time = TimeSpan.FromSeconds(Math.Round((serial - days) * 86400));
        if (days >= 61) days--;
        else if (days == 60) days = 59;
        return Epoch.AddDays(days) + time;
    }

    /// <summary>Splits the day part of a serial into year, month and day, including 29 February 1900 for serial 60.</summary>
    /// <param name="serial">The serial (0 to <see cref="MaxSerial"/>); serial 0 gives 1900-01-00 as in Excel.</param>
    /// <returns>Year, month and day.</returns>
    public static (int Year, int Month, int Day) ToDate(double serial)
    {
        var days = (int)Math.Floor(serial);
        if (days == 0) return (1900, 1, 0);
        if (days == 60) return (1900, 2, 29);
        var d = Epoch.AddDays(days > 60 ? days - 1 : days);
        return (d.Year, d.Month, d.Day);
    }

    /// <summary>
    /// Computes the serial of a date like Excel's DATE function: months and days outside their range roll over into the
    /// neighbouring months and years, and February 1900 has 29 days.
    /// </summary>
    /// <param name="year">The year (1900 to 9999).</param>
    /// <param name="month">The month; values outside 1 to 12 roll over.</param>
    /// <param name="day">The day; values outside the month roll over.</param>
    /// <returns>The serial; may be negative or beyond <see cref="MaxSerial"/> for out-of-range input.</returns>
    public static double FromDate(int year, int month, int day)
    {
        var y = year + (long)Math.Floor((month - 1) / 12.0);
        var m = ((month - 1) % 12 + 12) % 12 + 1;
        if (y < 1900) return -1;
        if (y > 9999) return MaxSerial + 1;
        double first = (new DateTime((int)y, m, 1) - Epoch).Days;
        if (y > 1900 || m > 2) first++;
        return first + day - 1;
    }
}
