```razor
@* Price / Budget Slider *@
@using MudBlazor.Extensions.Helper
@using Nextended.Core.Contracts
@using Nextended.Core.Types
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Budget: {0:C0} – {1:C0}", _range.Start, _range.End]
</MudText>

<MudExRangeSlider T="double"
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
            border-radius:6px;
            overflow:hidden;
            border:1px solid var(--mud-palette-divider);">

            @* Segmente für Budget / Standard / Premium / High-End *@
            @{
                void Segment(double left, double width, string label)
                {
                    <div style="
                                position:absolute;
                                left:@left%;
                                width:@width%;
                                top:0;
                                bottom:0;
                                background:color-mix(in srgb,var(--mud-palette-secondary) 12%,transparent);">
                    </div>
                    ;

                    var mid = left + width / 2;
                    <div style="
                                position:absolute;
                                left:@mid%;
                                top:4px;
                                transform:translateX(-50%);
                                font-size:9px;
                                color:var(--mud-palette-text-secondary);
                                pointer-events:none;">
                        @label
                    </div>
                    ;
                }

                Segment(0, 20, "Budget");
                Segment(20, 40, "Standard");
                Segment(60, 25, "Premium");
                Segment(85, 15, "High-End");
            }
        </div>
    </TrackTemplate>

    <SelectionTemplate Context="ctx">
        <div style="
            height:100%;
            width:100%;
            background:linear-gradient(90deg,var(--mud-palette-secondary),var(--mud-palette-primary));
            display:flex;
            align-items:center;
            justify-content:center;
            border-radius:4px;
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
            <span style="
                color:var(--mud-palette-primary-text);
                font-weight:600;
                font-size:11px;
                white-space:nowrap;
                padding:0 10px;">
                @($"{ctx.Value.Start:C0} – {ctx.Value.End:C0}")
            </span>
        </div>
    </SelectionTemplate>

    <ThumbStartTemplate Context="ctx">
        <div style="
            width:14px;
            height:14px;
            background:var(--mud-palette-surface);
            border-radius:50%;
            border:2px solid var(--mud-palette-secondary);
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
        </div>
    </ThumbStartTemplate>

    <ThumbEndTemplate Context="ctx">
        <div style="
            width:14px;
            height:14px;
            background:var(--mud-palette-surface);
            border-radius:50%;
            border:2px solid var(--mud-palette-primary);
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
        </div>
    </ThumbEndTemplate>

</MudExRangeSlider>

@code {
    private IRange<double> _range = new MudExRange<double>(50, 250);
    private IRange<double> _fullRange = new MudExRange<double>(0, 1000);
}

```
