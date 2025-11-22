```razor
@using System.Globalization
@using MudBlazor.Extensions.Helper
@using Nextended.Core.Contracts
@using Nextended.Core.Extensions
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Current period: {0} – {1}", _timeRange.Start.ToLongTimeString(), _timeRange.End.ToLongTimeString()]
</MudText>

<MudExRangeSlider T="TimeOnly"
                  @bind-Value="_timeRange"
                  @ref="ComponentRef"
                  SizeRange="@FullTimeRange"
                  StepResolver="@(StepResolvers.TimeOnly.Minutely())"
                  ShowInputs="true"
                  AllowWholeRangeDrag="true">

    <TrackTemplate Context="ctx">
        <div style="
            height: 100%;
            position: relative;
            width: 100%;
            background: var(--mud-palette-surface);
            border-radius: 4px;
            overflow: hidden;">

            <div style="
                position:absolute;
                left:0;
                right:0;
                top:40%;
                height:20%;
                background: linear-gradient(
                    to right,
                    var(--mud-palette-background-grey),
                    var(--mud-palette-surface)
                );
                pointer-events:none;">
            </div>

            @* Hour markers *@
            @for (var hour = 0; hour <= 23; hour++)
            {
                var time = new TimeOnly(hour, 0);
                var pct = ctx.Math.Percent(time, ctx.SizeRange) * 100;

                var isMajor = hour % 3 == 0; // every 3 hours as major tick

                var top = isMajor ? "10%" : "25%";
                var bottom = isMajor ? "10%" : "25%";
                var width = isMajor ? "2px" : "1px";

                var color = isMajor
                ? "var(--mud-palette-lines-default)"
                : "var(--mud-palette-divider)";

                <div style="
                                position:absolute;
                                left:@pct.ToString("F2", CultureInfo.InvariantCulture)%;
                                top:@top;
                                bottom:@bottom;
                                width:@width;
                                background:@color;">
                </div>

                @* Labels für Major-Ticks *@
                @if (isMajor)
                {
                    <div style="
                                            position:absolute;
                                            left:@pct.ToString("F2", CultureInfo.InvariantCulture)%;
                                            bottom:4px;
                                            transform: translateX(-50%);
                                            font-size:10px;
                                            color:var(--mud-palette-text-secondary);
                                            font-weight:500;
                                            pointer-events:none;
                                            white-space:nowrap;
                                            z-index:1;">
                        @time.ToString("HH\\:mm", CultureInfo.CurrentCulture)
                    </div>
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
                var(--mud-palette-primary-darken)
            );
            display:flex;
            align-items:center;
            justify-content:center;
            position:relative;
            border-radius:4px;
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
            <span style="
                color:var(--mud-palette-primary-text);
                font-weight:600;
                font-size:11px;
                white-space:nowrap;
                padding:0 8px;
                text-shadow:0 1px 2px rgba(0,0,0,0.3);">
                @FormatTime(ctx.Value.Start) – @FormatTime(ctx.Value.End)
            </span>
        </div>
    </SelectionTemplate>

    <ThumbStartTemplate Context="ctx">
        <div style="
            width:14px;
            height:14px;
            background:var(--mud-palette-surface);
            border:3px solid var(--mud-palette-primary);
            border-radius:50%;
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
        </div>
    </ThumbStartTemplate>

    <ThumbEndTemplate Context="ctx">
        <div style="
            width:14px;
            height:14px;
            background:var(--mud-palette-surface);
            border:3px solid var(--mud-palette-primary);
            border-radius:50%;
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
        </div>
    </ThumbEndTemplate>

</MudExRangeSlider>

@code {

    private IRange<TimeOnly> _timeRange = new MudExRange<TimeOnly>(
        new TimeOnly(8, 0),
        new TimeOnly(17, 0)
    );

    private IRange<TimeOnly> FullTimeRange => new MudExRange<TimeOnly>(
        new TimeOnly(0, 0),
        new TimeOnly(23, 59)
    );

    private string FormatTime(TimeOnly time)
    {
        return time.ToString("HH\\:mm", CultureInfo.CurrentCulture);
    }
}

```
