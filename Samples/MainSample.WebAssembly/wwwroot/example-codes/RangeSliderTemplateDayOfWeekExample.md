```razor
@* Day-of-Week Slider (0=Mon .. 6=Sun) *@
@using System.Globalization
@using MudBlazor.Extensions.Helper
@using Nextended.Core.Contracts
@using Nextended.Core.Types
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Days: {0} – {1}", GetDayName(_range.Start), GetDayName(_range.End)]
</MudText>

<MudExRangeSlider T="int"
                  @bind-Value="_range"
                  SizeRange="@_fullRange"
                  ShowInputs="true"
                  AllowWholeRangeDrag="true">

    <TrackTemplate Context="ctx">
        <div style="
            height:100%;
            position:relative;
            width:100%;
            background:var(--mud-palette-surface);
            border-radius:4px;
            overflow:hidden;
            border:1px solid var(--mud-palette-divider);">

            @* 7 Blöcke für Mo–So *@
            @{
                for (var i = 0; i < 7; i++)
                {
                    var left = i * (100.0 / 7);
                    var width = 100.0 / 7;
                    var isWeekend = i >= 5;

                    var bg = isWeekend
                    ? "color-mix(in srgb,var(--mud-palette-secondary) 18%,transparent)"
                    : "color-mix(in srgb,var(--mud-palette-primary) 8%,transparent)";

                    <div style="
                                position:absolute;
                                left:@left%;
                                width:@width%;
                                top:0;
                                bottom:0;
                                background:@bg;">
                    </div>
                    ;

                    var mid = left + width / 2;
                    <div style="
                                position:absolute;
                                left:@mid%;
                                top:4px;
                                transform:translateX(-50%);
                                font-size:10px;
                                color:var(--mud-palette-text-primary);
                                font-weight:600;
                                pointer-events:none;">
                        @GetDayShort(i)
                    </div>
                    ;
                }
            }
        </div>
    </TrackTemplate>

    <SelectionTemplate Context="ctx">
        <div style="
            height:100%;
            width:100%;
            background:linear-gradient(90deg,var(--mud-palette-primary),var(--mud-palette-secondary));
            display:flex;
            align-items:center;
            justify-content:center;
            border-radius:4px;">
            <span style="
                color:var(--mud-palette-primary-text);
                font-weight:600;
                font-size:11px;
                white-space:nowrap;
                padding:0 10px;">
                @($"{GetDayName(ctx.Value.Start)} – {GetDayName(ctx.Value.End)}")
            </span>
        </div>
    </SelectionTemplate>

    <ThumbStartTemplate Context="ctx">
        <div style="
            width:10px;
            height:20px;
            background:var(--mud-palette-surface);
            border-radius:999px;
            border:2px solid var(--mud-palette-primary);
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
        </div>
    </ThumbStartTemplate>

    <ThumbEndTemplate Context="ctx">
        <div style="
            width:10px;
            height:20px;
            background:var(--mud-palette-surface);
            border-radius:999px;
            border:2px solid var(--mud-palette-secondary);
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
        </div>
    </ThumbEndTemplate>

</MudExRangeSlider>

@code {
    private IRange<int> _range = new MudExRange<int>(0, 4); // Mo–Fr
    private IRange<int> _fullRange = new MudExRange<int>(0, 6);

    private static string GetDayShort(int i)
        => CultureInfo.GetCultureInfo("de-DE")
            .DateTimeFormat.AbbreviatedDayNames[(i + 1) % 7];

    private static string GetDayName(int i)
        => CultureInfo.GetCultureInfo("de-DE")
            .DateTimeFormat.DayNames[(i + 1) % 7];
}

```
