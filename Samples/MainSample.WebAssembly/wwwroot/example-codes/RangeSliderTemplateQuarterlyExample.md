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
                  StepResolver="@(StepResolvers.DateOnly.Quarterly())"
                  ShowInputs="true"
                  AllowWholeRangeDrag="true">

    <TrackTemplate Context="ctx">
        <div style="
            height: 100%;
            position: relative;
            width: 100%;
            background: var(--mud-palette-surface);
            border-radius: 6px;
            overflow: hidden;
            border: 1px solid var(--mud-palette-divider);">

            <div style="
                position:absolute;
                inset:0;
                background: radial-gradient(circle at 50% 0,
                    rgba(0,0,0,0.06),
                    transparent 60%);
                pointer-events:none;">
            </div>


            @{
                for (var year = _startDate.Year; year <= _endDate.Year; year++)
                {
                    var yearStart = new DateOnly(year, 1, 1);
                    if (yearStart < _startDate) yearStart = _startDate;

                    var yearEnd = new DateOnly(year, 12, 31);
                    if (yearEnd > _endDate) yearEnd = _endDate;

                    var pctStart = ctx.Math.Percent(yearStart, ctx.SizeRange) * 100;
                    var pctEnd = ctx.Math.Percent(yearEnd, ctx.SizeRange) * 100;
                    var widthPct = pctEnd - pctStart;

                    var isEvenYear = year % 2 == 0;
                    var yearBg = isEvenYear
                    ? "var(--mud-palette-background-grey)"
                    : "transparent";

                    <div style="
                                position:absolute;
                                left:@pctStart.ToString("F2", CultureInfo.InvariantCulture)%;
                                width:@widthPct.ToString("F2", CultureInfo.InvariantCulture)%;
                                top:35%;
                                height:65%;
                                background:@yearBg;
                                opacity:0.5;">
                    </div>

         
                    var yearMid = yearStart.AddDays((yearEnd.DayNumber - yearStart.DayNumber) / 2);
                    var pctMid = ctx.Math.Percent(yearMid, ctx.SizeRange) * 100;

                    <div style="
                                position:absolute;
                                left:@pctMid.ToString("F2", CultureInfo.InvariantCulture)%;
                                top:4px;
                                transform: translateX(-50%);
                                font-size:11px;
                                font-weight:600;
                                color:var(--mud-palette-text-primary);
                                text-shadow:0 1px 2px rgba(0,0,0,0.15);
                                pointer-events:none;
                                white-space:nowrap;
                                z-index:2;">
                        @year
                    </div>
                }
            }

            @{
                var current = _startDate;
                while (current <= _endDate)
                {
                    var q = GetQuarter(current);
                    var qStart = GetQuarterStart(current.Year, q);
                    if (qStart < _startDate) qStart = _startDate;

                    var qEnd = GetQuarterEnd(current.Year, q);
                    if (qEnd > _endDate) qEnd = _endDate;

                    var pctStart = ctx.Math.Percent(qStart, ctx.SizeRange) * 100;
                    var pctEnd = ctx.Math.Percent(qEnd, ctx.SizeRange) * 100;
                    var widthPct = pctEnd - pctStart;

  
                    var secondaryAlpha = q % 2 == 0 ? 0.18 : 0.10;
                    var qBg = $"color-mix(in srgb, var(--mud-palette-primary) {secondaryAlpha * 100:F0}%, transparent)";

                    <div style="
                                position:absolute;
                                left:@pctStart.ToString("F2", CultureInfo.InvariantCulture)%;
                                width:@widthPct.ToString("F2", CultureInfo.InvariantCulture)%;
                                top:40%;
                                height:45%;
                                background:@qBg;
                                border-left:1px solid var(--mud-palette-divider);
                                border-right:1px solid var(--mud-palette-divider);
                                box-sizing:border-box;">
                    </div>

      
                    var qMid = qStart.AddDays((qEnd.DayNumber - qStart.DayNumber) / 2);
                    var pctMid = ctx.Math.Percent(qMid, ctx.SizeRange) * 100;

                    <div style="
                                position:absolute;
                                left:@pctMid.ToString("F2", CultureInfo.InvariantCulture)%;
                                top:55%;
                                transform: translate(-50%, -50%);
                                font-size:10px;
                                font-weight:600;
                                color:var(--mud-palette-primary-text);
                                padding:1px 4px;
                                border-radius:3px;
                                background:color-mix(in srgb, var(--mud-palette-primary) 40%, transparent);
                                box-shadow:0 1px 2px rgba(0,0,0,0.25);
                                pointer-events:none;
                                white-space:nowrap;
                                z-index:3;">
                        @($"Q{q}")
                    </div>

                    current = qEnd.AddDays(1);
                }
            }

 
            @{
                var centerDate = _startDate.AddDays((_endDate.DayNumber - _startDate.DayNumber) / 2);
                var pctCenter = ctx.Math.Percent(centerDate, ctx.SizeRange) * 100;
            }
            <div style="
                position:absolute;
                left:@pctCenter.ToString("F2", CultureInfo.InvariantCulture)%;
                top:35%;
                bottom:0;
                width:1px;
                background:var(--mud-palette-lines-default);
                opacity:0.7;">
            </div>

        </div>
    </TrackTemplate>

    <SelectionTemplate Context="ctx">
        <div style="
            height: 100%;
            width: 100%;
            background: linear-gradient(
                90deg,
                var(--mud-palette-primary),
                var(--mud-palette-secondary)
            );
            display:flex;
            align-items:center;
            justify-content:center;
            position:relative;
            border-radius:999px;
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
            <span style="
                color:var(--mud-palette-primary-text);
                font-weight:600;
                font-size:11px;
                white-space:nowrap;
                padding:0 12px;
                text-shadow:0 1px 2px rgba(0,0,0,0.3);">
                @GetQuarterRangeLabel(ctx.Value.Start, ctx.Value.End)
            </span>
        </div>
    </SelectionTemplate>


    <ThumbStartTemplate Context="ctx">
        <div style="
            width:14px;
            height:14px;
            background:var(--mud-palette-surface);
            border:3px solid var(--mud-palette-primary);
            transform:rotate(45deg);
            border-radius:3px;
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
        </div>
    </ThumbStartTemplate>

    <ThumbEndTemplate Context="ctx">
        <div style="
            width:14px;
            height:14px;
            background:var(--mud-palette-surface);
            border:3px solid var(--mud-palette-secondary);
            transform:rotate(45deg);
            border-radius:3px;
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
        </div>
    </ThumbEndTemplate>

</MudExRangeSlider>

@code {
    private readonly DateOnly _startDate = new(2021, 1, 1);
    private readonly DateOnly _endDate = new(2024, 12, 31);

    private IRange<DateOnly> _dateRange = new MudExRange<DateOnly>(
        new DateOnly(2022, 4, 1), 
        new DateOnly(2023, 9, 30) 
    );

    private IRange<DateOnly> FullDateRange => new MudExRange<DateOnly>(
        _startDate,
        _endDate
    );

    private int GetQuarter(DateOnly date)
        => ((date.Month - 1) / 3) + 1;

    private DateOnly GetQuarterStart(int year, int quarter)
    {
        var month = (quarter - 1) * 3 + 1;
        return new DateOnly(year, month, 1);
    }

    private DateOnly GetQuarterEnd(int year, int quarter)
    {
        var month = (quarter - 1) * 3 + 3;
        var lastDay = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, lastDay);
    }

    private string GetQuarterRangeLabel(DateOnly start, DateOnly end)
    {
        var qs = GetQuarter(start);
        var qe = GetQuarter(end);
        var ys = start.Year;
        var ye = end.Year;

        // same Quarter & same Year → Qx YYYY
        if (ys == ye && qs == qe)
            return $"Q{qs} {ys}";

        // same Year different Quarter → Qx - Qy YYYY
        if (ys == ye)
            return $"Q{qs} - Q{qe}";

        // diverse Quarter & Jahre → Qx YYYY - Qy YYYY
        return $"Q{qs} {ys} - Q{qe} {ye}";
    }
}

```
