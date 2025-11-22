```razor
@using System
@using System.Globalization
@using MudBlazor.Extensions.Helper
@using Nextended.Core.Contracts
@using Nextended.Core.Extensions
@using Nextended.Core.Types
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Current period: {0} – {1}", _dateRange.Start.ToShortDateString(), _dateRange.End.ToShortDateString()]
</MudText>

<MudExRangeSlider T="DateOnly"
                  @bind-Value="_dateRange"
                  @ref="ComponentRef"
                  SizeRange="@FullDateRange"
                  StepResolver="@(StepResolvers.DateOnly.Yearly())"
                  ShowInputs="true"
                  AllowWholeRangeDrag="true">

    <TrackTemplate Context="ctx">
        <div style="
            height: 100%;
            position: relative;
            width: 100%;
            background: var(--mud-palette-surface);
            border-radius: 8px;
            overflow: hidden;
            border: 1px solid var(--mud-palette-divider);">

            <div style="
                position:absolute;
                inset:0;
                background-image: repeating-linear-gradient(
                    135deg,
                    color-mix(in srgb, var(--mud-palette-background-grey) 10%, transparent),
                    color-mix(in srgb, var(--mud-palette-background-grey) 10%, transparent) 6px,
                    transparent 6px,
                    transparent 12px
                );
                opacity:0.4;
                pointer-events:none;">
            </div>

            @* Fiscal-Year-Block (start on 1. April) *@
            @{
                for (var fy = _firstFiscalYear; fy <= _lastFiscalYear; fy++)
                {
                    var fyStart = GetFiscalYearStart(fy);
                    var fyEnd = GetFiscalYearEnd(fy);

                    if (fyEnd < _startDate || fyStart > _endDate)
                        continue;

                    if (fyStart < _startDate) fyStart = _startDate;
                    if (fyEnd > _endDate) fyEnd = _endDate;

                    var pctStart = ctx.Math.Percent(fyStart, ctx.SizeRange) * 100;
                    var pctEnd = ctx.Math.Percent(fyEnd, ctx.SizeRange) * 100;
                    var widthPct = pctEnd - pctStart;

                    var isEvenFy = fy % 2 == 0;

                    var fyBg = isEvenFy
                    ? "color-mix(in srgb, var(--mud-palette-secondary) 12%, transparent)"
                    : "color-mix(in srgb, var(--mud-palette-primary) 10%, transparent)";

                    <div style="
                                position:absolute;
                                left:@pctStart.ToString("F2", CultureInfo.InvariantCulture)%;
                                width:@widthPct.ToString("F2", CultureInfo.InvariantCulture)%;
                                top:38%;
                                height:50%;
                                background:@fyBg;
                                box-sizing:border-box;
                                border-left:1px solid var(--mud-palette-divider);
                                border-right:1px solid var(--mud-palette-divider);">
                    </div>

                    
                    var fyMid = fyStart.AddDays((fyEnd.DayNumber - fyStart.DayNumber) / 2);
                    var pctMid = ctx.Math.Percent(fyMid, ctx.SizeRange) * 100;

                    <div style="
                                position:absolute;
                                left:@pctMid.ToString("F2", CultureInfo.InvariantCulture)%;
                                top:6px;
                                transform: translateX(-50%);
                                font-size:11px;
                                font-weight:700;
                                color:var(--mud-palette-text-primary);
                                background:color-mix(in srgb, var(--mud-palette-surface) 80%, transparent);
                                padding:2px 8px;
                                border-radius:999px;
                                box-shadow:0 1px 3px rgba(0,0,0,0.25);
                                pointer-events:none;
                                white-space:nowrap;
                                z-index:3;">
                        @($"FY {fy}")
                    </div>
                }
            }

            
            @{
                for (var fy = _firstFiscalYear; fy <= _lastFiscalYear; fy++)
                {
                    var fyStart = GetFiscalYearStart(fy);
                    var fyEnd = GetFiscalYearEnd(fy);

                    if (fyEnd < _startDate || fyStart > _endDate)
                        continue;

                    for (var q = 1; q <= 4; q++)
                    {
                        var qStart = fyStart.AddMonths((q - 1) * 3);
                        var qEnd = qStart.AddMonths(3).AddDays(-1);

                        if (qEnd < _startDate || qStart > _endDate)
                            continue;

                        if (qStart < _startDate) qStart = _startDate;
                        if (qEnd > _endDate) qEnd = _endDate;

                        var pctStart = ctx.Math.Percent(qStart, ctx.SizeRange) * 100;
                        var pctEnd = ctx.Math.Percent(qEnd, ctx.SizeRange) * 100;
                        var mid = qStart.AddDays((qEnd.DayNumber - qStart.DayNumber) / 2);
                        var pctMid = ctx.Math.Percent(mid, ctx.SizeRange) * 100;

                        var isMajor = (q == 1 || q == 3);

                        var top = isMajor ? "42%" : "50%";
                        var bottom = "88%";
                        var width = isMajor ? "2px" : "1px";
                        var color = isMajor
                        ? "var(--mud-palette-lines-default)"
                        : "var(--mud-palette-divider)";

                        <div style="
                                                position:absolute;
                                                left:@pctStart.ToString("F2", CultureInfo.InvariantCulture)%;
                                                top:@top;
                                                bottom:@bottom;
                                                width:@width;
                                                background:@color;">
                        </div>

                        @if (isMajor)
                        {
                            <div style="
                                                        position:absolute;
                                                        left:@pctMid.ToString("F2", CultureInfo.InvariantCulture)%;
                                                        bottom:4px;
                                                        transform: translateX(-50%);
                                                        font-size:9px;
                                                        color:var(--mud-palette-text-secondary);
                                                        pointer-events:none;
                                                        white-space:nowrap;
                                                        z-index:3;">
                                @($"Q{q}")
                            </div>
                        }
                    }
                }
            }
        </div>
    </TrackTemplate>

    <SelectionTemplate Context="ctx">
        <div style="
            height: 100%;
            width: 100%;
            background: linear-gradient(
                90deg,
                var(--mud-palette-primary),
                var(--mud-palette-secondary-darken)
            );
            display:flex;
            align-items:center;
            justify-content:center;
            position:relative;
            border-radius:10px;
            box-shadow:0 2px 5px rgba(0,0,0,0.3);">
            <span style="
                color:var(--mud-palette-primary-text);
                font-weight:600;
                font-size:11px;
                white-space:nowrap;
                padding:0 12px;
                text-shadow:0 1px 2px rgba(0,0,0,0.3);">
                @GetFiscalLabel(ctx.Value.Start, ctx.Value.End)
            </span>
        </div>
    </SelectionTemplate>

    @* andere Thumb-Optik: schmale, hohe „Pills“ *@
    <ThumbStartTemplate Context="ctx">
        <div style="
            width:10px;
            height:22px;
            background:var(--mud-palette-surface);
            border:3px solid var(--mud-palette-primary);
            border-radius:999px;
            box-shadow:0 2px 4px rgba(0,0,0,0.3);">
        </div>
    </ThumbStartTemplate>

    <ThumbEndTemplate Context="ctx">
        <div style="
            width:10px;
            height:22px;
            background:var(--mud-palette-surface);
            border:3px solid var(--mud-palette-secondary);
            border-radius:999px;
            box-shadow:0 2px 4px rgba(0,0,0,0.3);">
        </div>
    </ThumbEndTemplate>

</MudExRangeSlider>

@code {
    // Geschäftsjahr beginnt am 1. April
    private const int FiscalStartMonth = 4;
    private const int FiscalStartDay = 1;

    // Gesamtbereich über mehrere Fiscal Years
    private readonly DateOnly _startDate = new(2020, 4, 1);  // FY2020 start
    private readonly DateOnly _endDate = new(2026, 3, 31); // FY2025 end

    private readonly int _firstFiscalYear = 2020;
    private readonly int _lastFiscalYear = 2025;

    private IRange<DateOnly> _dateRange = new MudExRange<DateOnly>(
        new DateOnly(2022, 6, 1),  // irgendwo in FY2022 Q2
        new DateOnly(2024, 1, 15)  // irgendwo in FY2023 Q4
    );

    private IRange<DateOnly> FullDateRange => new MudExRange<DateOnly>(
        _startDate,
        _endDate
    );

    private static int GetFiscalYear(DateOnly date)
    {
        if (date.Month > FiscalStartMonth ||
           (date.Month == FiscalStartMonth && date.Day >= FiscalStartDay))
            return date.Year;

        return date.Year - 1;
    }

    private static DateOnly GetFiscalYearStart(int fiscalYear)
        => new(fiscalYear, FiscalStartMonth, FiscalStartDay);

    private static DateOnly GetFiscalYearEnd(int fiscalYear)
        => GetFiscalYearStart(fiscalYear + 1).AddDays(-1);

    private static int GetFiscalQuarter(DateOnly date)
    {
        var fy = GetFiscalYear(date);
        var fyStart = GetFiscalYearStart(fy);

        // Monats-Offset relativ zum Fiscal-Start
        var monthOffset = (date.Year - fy) * 12 + (date.Month - FiscalStartMonth);
        if (monthOffset < 0) monthOffset = 0;
        if (monthOffset > 11) monthOffset = 11;

        return (monthOffset / 3) + 1; // 1..4
    }

    private static string GetFiscalLabel(DateOnly start, DateOnly end)
    {
        var fyStart = GetFiscalYear(start);
        var fyEnd = GetFiscalYear(end);

        var qStart = GetFiscalQuarter(start);
        var qEnd = GetFiscalQuarter(end);

        if (fyStart == fyEnd && qStart == 1 && qEnd == 4)
            return $"FY {fyStart}";

        if (fyStart == fyEnd)
        {
            if (qStart == qEnd)
                return $"FY {fyStart} Q{qStart}";

            return $"FY {fyStart} Q{qStart} – Q{qEnd}";
        }

        if (qStart == 1 && qEnd == 4)
            return $"FY {fyStart} – FY {fyEnd}";

        return $"FY {fyStart} Q{qStart} – FY {fyEnd} Q{qEnd}";
    }
}

```
