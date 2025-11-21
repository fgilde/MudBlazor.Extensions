```razor
@* Transit Time Slider (days) *@
@using MudBlazor.Extensions.Helper
@using Nextended.Core.Contracts
@using Nextended.Core.Types
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Transit time: {0} – {1} days", _range.Start, _range.End]
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
            overflow:hidden;">

            @* Express 0–3 *@
            <div style="
                position:absolute;
                left:0%;
                width:@(3.0 / 60 * 100)%;
                top:0;
                bottom:0;
                background:color-mix(in srgb,var(--mud-palette-success) 22%,transparent);">
            </div>

            @* Standard 4–10 *@
            <div style="
                position:absolute;
                left:@(4.0 / 60 * 100)%;
                width:@(7.0 / 60 * 100)%;
                top:0;
                bottom:0;
                background:color-mix(in srgb,var(--mud-palette-primary) 18%,transparent);">
            </div>

            @* Economy / Sea 11–60 *@
            <div style="
                position:absolute;
                left:@(11.0 / 60 * 100)%;
                width:@((60.0 - 11) / 60 * 100)%;
                top:0;
                bottom:0;
                background:color-mix(in srgb,var(--mud-palette-secondary) 18%,transparent);">
            </div>

            <div style="position:absolute;left:5%;top:4px;font-size:9px;color:var(--mud-palette-text-secondary);">Express</div>
            <div style="position:absolute;left:25%;top:4px;font-size:9px;color:var(--mud-palette-text-secondary);">Standard</div>
            <div style="position:absolute;left:70%;top:4px;font-size:9px;color:var(--mud-palette-text-secondary);">Economy/Sea</div>
        </div>
    </TrackTemplate>

    <SelectionTemplate Context="ctx">
        <div style="
            height:100%;
            width:100%;
            background:var(--mud-palette-primary);
            display:flex;
            align-items:center;
            justify-content:center;
            border-radius:999px;">
            <span style="
                color:var(--mud-palette-primary-text);
                font-weight:600;
                font-size:11px;
                white-space:nowrap;
                padding:0 10px;">
                @($"{ctx.Value.Start} – {ctx.Value.End} Tage")
            </span>
        </div>
    </SelectionTemplate>

    <ThumbStartTemplate Context="ctx">
        <div style="
            width:12px;
            height:12px;
            background:var(--mud-palette-surface);
            border-radius:50%;
            border:2px solid var(--mud-palette-primary);
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
        </div>
    </ThumbStartTemplate>

    <ThumbEndTemplate Context="ctx">
        <div style="
            width:12px;
            height:12px;
            background:var(--mud-palette-surface);
            border-radius:50%;
            border:2px solid var(--mud-palette-secondary);
            box-shadow:0 2px 4px rgba(0,0,0,0.25);">
        </div>
    </ThumbEndTemplate>

</MudExRangeSlider>

@code {
    private IRange<int> _range = new MudExRange<int>(4, 9);
    private IRange<int> _fullRange = new MudExRange<int>(0, 60);
}

```
