using System.Globalization;

namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class DateTimeFunctions
{
    private const string Cat = "Date & Time";

    internal static void Register(FunctionRegistry r)
    {
        r.Add(Cat, "DATE(year, month, day)", "Returns the serial number of a date; months and days outside their range roll over.", Fn.Lift(a =>
        {
            var (y, m, d) = (Fn.Int(a[0]), Fn.Int(a[1]), Fn.Int(a[2]));
            if (y is < 0 or > 9999) throw Fn.Error(CellError.Num);
            if (y < 1900) y += 1900;
            return Fn.Serial(ExcelDate.FromDate(y, m, d));
        }));
        r.Add(Cat, "TIME(hour, minute, second)", "Returns the decimal number of a time of day.", Fn.Lift(a =>
        {
            var total = (long)Fn.Int(a[0]) * 3600 + (long)Fn.Int(a[1]) * 60 + Fn.Int(a[2]);
            return total < 0 ? throw Fn.Error(CellError.Num) : CellValue.Number(total % 86400 / 86400.0);
        }));
        r.Add(Cat, "TODAY()", "Returns the serial number of today's date.", (_, _) => CalcValue.Number(ExcelDate.ToSerial(DateTime.Today)), true);
        r.Add(Cat, "NOW()", "Returns the serial number of the current date and time.", (_, _) => CalcValue.Number(ExcelDate.ToSerial(DateTime.Now)), true);
        r.Add(Cat, "YEAR(serial_number)", "Returns the year of a date.", Fn.Lift((ctx, a) => CellValue.Number(ExcelDate.ToDate(Fn.SerialArg(ctx, a[0])).Year)));
        r.Add(Cat, "MONTH(serial_number)", "Returns the month of a date (1 to 12).",
            Fn.Lift((ctx, a) => CellValue.Number(ExcelDate.ToDate(Fn.SerialArg(ctx, a[0])).Month)));
        r.Add(Cat, "DAY(serial_number)", "Returns the day of the month of a date (1 to 31).",
            Fn.Lift((ctx, a) => CellValue.Number(ExcelDate.ToDate(Fn.SerialArg(ctx, a[0])).Day)));
        r.Add(Cat, "HOUR(serial_number)", "Returns the hour of a time (0 to 23).", Fn.Lift((ctx, a) => CellValue.Number(Seconds(ctx, a[0]) / 3600)));
        r.Add(Cat, "MINUTE(serial_number)", "Returns the minute of a time (0 to 59).", Fn.Lift((ctx, a) => CellValue.Number(Seconds(ctx, a[0]) / 60 % 60)));
        r.Add(Cat, "SECOND(serial_number)", "Returns the second of a time (0 to 59).", Fn.Lift((ctx, a) => CellValue.Number(Seconds(ctx, a[0]) % 60)));
        r.Add(Cat, "WEEKDAY(serial_number, [return_type])", "Returns the day of the week (by default 1 = Sunday to 7 = Saturday).", Fn.Lift((ctx, a) =>
        {
            var sunday0 = (Fn.Day(ctx, a[0]) + 6) % 7;
            var type = Fn.Int(a, 1, 1);
            return CellValue.Number(type switch
            {
                1 => sunday0 + 1,
                2 => (sunday0 + 6) % 7 + 1,
                3 => (sunday0 + 6) % 7,
                >= 11 and <= 17 => (sunday0 - (type - 10) % 7 + 7) % 7 + 1,
                _ => throw Fn.Error(CellError.Num)
            });
        }));
        r.Add(Cat, "WEEKNUM(serial_number, [return_type])", "Returns the week number of a date in the year.", Fn.Lift((ctx, a) =>
        {
            var day = Fn.Day(ctx, a[0]);
            var type = Fn.Int(a, 1, 1);
            if (type == 21) return CellValue.Number(IsoWeek(day));
            var start = type switch
            {
                1 or 17 => 0,
                2 or 11 => 1,
                >= 12 and <= 16 => type - 10,
                _ => throw Fn.Error(CellError.Num)
            };
            var jan1 = (int)ExcelDate.FromDate(ExcelDate.ToDate(day).Year, 1, 1);
            var offset = ((jan1 + 6) % 7 - start + 7) % 7;
            return CellValue.Number((day - jan1 + offset) / 7 + 1);
        }));
        r.Add(Cat, "ISOWEEKNUM(date)", "Returns the ISO 8601 week number of a date.", Fn.Lift((ctx, a) => CellValue.Number(IsoWeek(Fn.Day(ctx, a[0])))));
        r.Add(Cat, "EDATE(start_date, months)", "Returns the date a number of months before or after a date.",
            Fn.Lift((ctx, a) => Fn.Serial(AddMonths(Fn.Day(ctx, a[0]), Fn.Int(a[1]), false))));
        r.Add(Cat, "EOMONTH(start_date, months)", "Returns the last day of the month a number of months before or after a date.",
            Fn.Lift((ctx, a) => Fn.Serial(AddMonths(Fn.Day(ctx, a[0]), Fn.Int(a[1]), true))));
        r.Add(Cat, "DATEDIF(start_date, end_date, unit)", "Returns the difference between two dates in years (Y), months (M), days (D), or MD, YM, YD.",
            Fn.Lift((ctx, a) =>
            {
                var (s, e) = (Fn.Day(ctx, a[0]), Fn.Day(ctx, a[1]));
                if (s > e) throw Fn.Error(CellError.Num);
                var (ys, ms, ds) = ExcelDate.ToDate(s);
                var (ye, me, de) = ExcelDate.ToDate(e);
                var months = (ye - ys) * 12 + me - ms - (de < ds ? 1 : 0);
                return CellValue.Number(Fn.Str(a[2]).ToUpperInvariant() switch
                {
                    "Y" => months / 12,
                    "M" => months,
                    "D" => e - s,
                    "YM" => months % 12,
                    "MD" => de >= ds ? de - ds : e - ExcelDate.FromDate(ye, me - 1, ds),
                    "YD" => e - (ExcelDate.FromDate(ye, ms, ds) is var anniversary && anniversary > e ? ExcelDate.FromDate(ye - 1, ms, ds) : anniversary),
                    _ => throw Fn.Error(CellError.Num)
                });
            }));
        r.Add(Cat, "DAYS(end_date, start_date)", "Returns the number of days between two dates.",
            Fn.Lift((ctx, a) => CellValue.Number(Fn.Day(ctx, a[0]) - Fn.Day(ctx, a[1]))));
        r.Add(Cat, "DAYS360(start_date, end_date, [method])", "Returns the number of days between two dates based on a 360-day year.",
            Fn.Lift((ctx, a) => CellValue.Number(Days360(Fn.Day(ctx, a[0]), Fn.Day(ctx, a[1]), Fn.Bool(a, 2, false)))));
        r.Add(Cat, "NETWORKDAYS(start_date, end_date, [holidays])", "Returns the number of whole working days between two dates.",
            (ctx, a) => NetworkDays(ctx, a[0], a[1], null, a.Count > 2 ? a[2] : null));
        r.Add(Cat, "NETWORKDAYS.INTL(start_date, end_date, [weekend], [holidays])", "Returns the number of working days with custom weekend days.",
            (ctx, a) => NetworkDays(ctx, a[0], a[1], a.Count > 2 ? a[2] : null, a.Count > 3 ? a[3] : null));
        r.Add(Cat, "WORKDAY(start_date, days, [holidays])", "Returns the date a number of working days before or after a date.",
            (ctx, a) => WorkDay(ctx, a[0], a[1], null, a.Count > 2 ? a[2] : null));
        r.Add(Cat, "WORKDAY.INTL(start_date, days, [weekend], [holidays])", "Returns the date a number of working days away, with custom weekend days.",
            (ctx, a) => WorkDay(ctx, a[0], a[1], a.Count > 2 ? a[2] : null, a.Count > 3 ? a[3] : null));
        r.Add(Cat, "DATEVALUE(date_text)", "Converts a date in text form to a serial number.", Fn.Lift((ctx, a) =>
        {
            if (a[0].Kind != CellValueKind.Text) throw Fn.Error(CellError.Value);
            return Fn.TryParseDate(a[0].TextValue, ctx.Culture, out var serial)
                ? CellValue.Number(Math.Floor(serial))
                : throw Fn.Error(CellError.Value);
        }));
        r.Add(Cat, "TIMEVALUE(time_text)", "Converts a time in text form to a decimal number.", Fn.Lift((ctx, a) =>
        {
            if (a[0].Kind != CellValueKind.Text) throw Fn.Error(CellError.Value);
            return Fn.TryParseDate(a[0].TextValue, ctx.Culture, out var serial)
                ? CellValue.Number(serial - Math.Floor(serial))
                : throw Fn.Error(CellError.Value);
        }));
        r.Add(Cat, "YEARFRAC(start_date, end_date, [basis])", "Returns the fraction of the year between two dates.",
            Fn.Lift((ctx, a) => Fn.Number(YearFrac(Fn.Day(ctx, a[0]), Fn.Day(ctx, a[1]), Fn.Int(a, 2, 0)))));
    }

    private static int Seconds(FunctionContext ctx, CellValue v)
    {
        var serial = Fn.SerialArg(ctx, v);
        return (int)Math.Round((serial - Math.Floor(serial)) * 86400) % 86400;
    }

    private static int IsoWeek(int day) => ISOWeek.GetWeekOfYear(ExcelDate.ToDateTime(day));

    private static double AddMonths(int day, int months, bool endOfMonth)
    {
        var (y, m, d) = ExcelDate.ToDate(day);
        var target = ExcelDate.FromDate(y, m + months, 1);
        if (target < 0 || target > ExcelDate.MaxSerial) throw Fn.Error(CellError.Num);
        var (ty, tm, _) = ExcelDate.ToDate(target);
        var last = ExcelDate.DaysInMonth(ty, tm);
        return target + (endOfMonth ? last : Math.Min(d, last)) - 1;
    }

    private static bool IsLastOfMonth(int y, int m, int d) => d == ExcelDate.DaysInMonth(y, m);

    private static int Days360(int start, int end, bool european)
    {
        var (y1, m1, d1) = ExcelDate.ToDate(start);
        var (y2, m2, d2) = ExcelDate.ToDate(end);
        if (european)
        {
            d1 = Math.Min(d1, 30);
            d2 = Math.Min(d2, 30);
        }
        else
        {
            if (IsLastOfMonth(y1, m1, d1)) d1 = 30;
            if (d2 == 31 && d1 < 30)
            {
                d2 = 1;
                m2++;
            }
            else if (d2 == 31) d2 = 30;
        }

        return (y2 - y1) * 360 + (m2 - m1) * 30 + d2 - d1;
    }

    private static double YearFrac(int start, int end, int basis)
    {
        if (start > end) (start, end) = (end, start);
        var (y1, m1, d1) = ExcelDate.ToDate(start);
        var (y2, m2, d2) = ExcelDate.ToDate(end);
        switch (basis)
        {
            case 0:
                if (IsLastOfMonth(y1, m1, d1) && m1 == 2)
                {
                    if (IsLastOfMonth(y2, m2, d2) && m2 == 2) d2 = 30;
                    d1 = 30;
                }

                if (d2 == 31 && d1 >= 30) d2 = 30;
                if (d1 == 31) d1 = 30;
                return ((y2 - y1) * 360 + (m2 - m1) * 30 + d2 - d1) / 360.0;
            case 1:
                if (y1 == y2) return (end - start) / (double)(DateTime.IsLeapYear(y1) ? 366 : 365);
                if (end <= ExcelDate.FromDate(y1 + 1, m1, d1))
                {
                    var leap = (DateTime.IsLeapYear(y1) && start <= ExcelDate.FromDate(y1, 2, 29))
                               || (DateTime.IsLeapYear(y2) && end >= ExcelDate.FromDate(y2, 2, 29));
                    return (end - start) / (leap ? 366.0 : 365.0);
                }

                var days = ExcelDate.FromDate(y2 + 1, 1, 1) - ExcelDate.FromDate(y1, 1, 1);
                return (end - start) / (days / (y2 - y1 + 1));
            case 2:
                return (end - start) / 360.0;
            case 3:
                return (end - start) / 365.0;
            case 4:
                return ((y2 - y1) * 360 + (m2 - m1) * 30 + Math.Min(d2, 30) - Math.Min(d1, 30)) / 360.0;
            default:
                throw Fn.Error(CellError.Num);
        }
    }

    private static bool[] Weekend(FunctionContext ctx, CalcArg arg)
    {
        var weekend = new bool[7];
        var v = arg == null || arg.IsMissing ? CellValue.Number(1) : Fn.Scalar(ctx, arg);
        if (v.Kind == CellValueKind.Text)
        {
            if (v.TextValue.Length != 7 || v.TextValue.Any(c => c is not ('0' or '1')) || v.TextValue == "1111111") throw Fn.Error(CellError.Value);
            for (var i = 0; i < 7; i++) weekend[i] = v.TextValue[i] == '1';
            return weekend;
        }

        var code = Fn.Int(v);
        switch (code)
        {
            case >= 1 and <= 7:
                weekend[(code + 4) % 7] = weekend[(code + 5) % 7] = true;
                break;
            case >= 11 and <= 17:
                weekend[(code - 5) % 7] = true;
                break;
            default:
                throw Fn.Error(CellError.Num);
        }

        return weekend;
    }

    private static HashSet<int> Holidays(FunctionContext ctx, CalcArg arg) =>
        arg == null || arg.IsMissing ? [] : Fn.Numbers(ctx, arg).Select(d => (int)Math.Floor(d)).ToHashSet();

    private static bool IsWorkday(int day, bool[] weekend, HashSet<int> holidays) => !weekend[(day + 5) % 7] && !holidays.Contains(day);

    private static CalcValue NetworkDays(FunctionContext ctx, CalcArg startArg, CalcArg endArg, CalcArg weekendArg, CalcArg holidayArg)
    {
        var (start, end) = (Fn.Day(ctx, Fn.Scalar(ctx, startArg)), Fn.Day(ctx, Fn.Scalar(ctx, endArg)));
        var (weekend, holidays) = (Weekend(ctx, weekendArg), Holidays(ctx, holidayArg));
        var sign = start <= end ? 1 : -1;
        var count = 0;
        for (var d = Math.Min(start, end); d <= Math.Max(start, end); d++)
            if (IsWorkday(d, weekend, holidays))
                count++;
        return CalcValue.Number(sign * count);
    }

    private static CalcValue WorkDay(FunctionContext ctx, CalcArg startArg, CalcArg daysArg, CalcArg weekendArg, CalcArg holidayArg)
    {
        var day = Fn.Day(ctx, Fn.Scalar(ctx, startArg));
        var days = Fn.Int(Fn.Scalar(ctx, daysArg));
        var (weekend, holidays) = (Weekend(ctx, weekendArg), Holidays(ctx, holidayArg));
        var step = Math.Sign(days);
        for (var left = Math.Abs(days); left > 0;)
        {
            day += step;
            if (day < 1 || day > ExcelDate.MaxSerial) return CalcValue.Error(CellError.Num);
            if (IsWorkday(day, weekend, holidays)) left--;
        }

        return CalcValue.Number(day);
    }
}
