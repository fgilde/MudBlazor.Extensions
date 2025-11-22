```razor
@using System.Globalization
@using AuralizeBlazor
@using AuralizeBlazor.Options
@using MudBlazor.Extensions.Helper
@using Nextended.Core.Contracts
@using Nextended.Core.Extensions
@using Nextended.Core.Types
@inject IJSRuntime JS
@inherits ExampleBase

<MudText Typo="Typo.caption" Class="mt-2 mb-2">
    @L["Select an audio range on top of the visualized spectrum."]
</MudText>

<MudText Typo="Typo.body2" Class="mb-2">
    @L["The Auralizer control is used as the visual background of the track, while the range slider renders the selection on top of it."]
</MudText>

<MudText Typo="Typo.body2" Class="mb-2">
    @L["Use the button below to play back only the currently selected audio range."]
</MudText>

<MudText Typo="Typo.body2" Class="mb-4">
    @L["Start, end, and length of the selected audio range are displayed as formatted time values."]
</MudText>

<MudText Typo="Typo.caption" Class="mb-1">
    @L["Selected range: {0} – {1} (Length: {2})",
        FormatTime(_selection.Start),
        FormatTime(_selection.End),
        FormatTime(_selection.End - _selection.Start)]
</MudText>

<MudButton Variant="Variant.Filled"
           Color="Color.Primary"
           OnClick="PlaySelectionAsync"
           Class="mb-4">
    @L["Play selected range"]
</MudButton>

<MudExRangeSlider T="double"
                  @bind-Value="_selection"
                  Style="height: 230px;"
                  TrackStyle="height: 100%;"
                  SizeRange="@_fullRange"
                  AllowWholeRangeDrag="true"
                  ShowInputs="false"
                  StepLength="@(new RangeLength<double>(1))">

    @* Auralizer as track background *@
    <TrackTemplate Context="ctx">
        <div style="z-index: 99;">

            <Auralizer InitialRender="InitialRender.WithFullSpectrumAudioDataPrefilledWithRandomData"
                       KeepState="false"
                       Style="z-index: 99;"
                       ShowTrackInfosOnPlay="false"
                       ApplyBackgroundImageFromTrack="false"
                       Height="100%"
                       Presets="@AuralizerPreset.All"
                       ContextMenuAction="VisualizerAction.None"
                       ClickAction="VisualizerAction.None"
                       Overlay="true">
                <center>
                    <audio @ref="_audioRef"
                           class="audio-main mud-ex-animate-all-properties"
                           preload="metadata"
                           loading="lazy"
                           controls="true"
                           src="@mp3Url">
                    </audio>
                </center>
            </Auralizer>

        </div>
    </TrackTemplate>

    @* selection overlay: full height, frosted glass *@
    <SelectionTemplate Context="ctx">
        @{
            var start = ctx.Value.Start;
            var end = ctx.Value.End;
            var length = end - start;
        }
        <div style="
            height:100%;
            width:100%;
            background:rgba(255,255,255,0.18);
            backdrop-filter:blur(6px);
            -webkit-backdrop-filter:blur(6px);
            border-radius:8px;
            border:1px solid rgba(255,255,255,0.45);
            box-shadow:0 0 0 1px rgba(0,0,0,0.25);
            display:flex;
            align-items:flex-end;
            justify-content:center;
            padding:6px 10px;
            box-sizing:border-box;">
            <span style="
                color:white;
                font-weight:600;
                font-size:11px;
                white-space:nowrap;
                text-shadow:0 1px 2px rgba(0,0,0,0.9);">
                @($"{FormatTime(start)} – {FormatTime(end)} ({FormatTime(length)})")
            </span>
        </div>
    </SelectionTemplate>

    @* slim handles *@
    <ThumbStartTemplate Context="ctx">
        <div style="
            width:8px;
            height:28px;
            background:rgba(255,255,255,0.95);
            border-radius:999px;
            border:2px solid var(--mud-palette-primary);
            box-shadow:0 2px 6px rgba(0,0,0,0.8);">
        </div>
    </ThumbStartTemplate>

    <ThumbEndTemplate Context="ctx">
        <div style="
            width:8px;
            height:28px;
            background:rgba(255,255,255,0.95);
            border-radius:999px;
            border:2px solid var(--mud-palette-secondary);
            box-shadow:0 2px 6px rgba(0,0,0,0.8);">
        </div>
    </ThumbEndTemplate>

</MudExRangeSlider>

@code {
    private string mp3Url = string.Empty;

    // Fallback duration until wir die echte Länge per JS haben
    private double _durationSeconds = 180;

    private IRange<double> _selection = new MudExRange<double>(10, 30);
    private IRange<double> _fullRange;

    private ElementReference _audioRef;
    private bool _durationInitialized;

    protected override Task OnInitializedAsync()
    {
        mp3Url = "https://www.mudex.org/sample-data/Unified-Voice.mp3";
        _fullRange = new MudExRange<double>(0, _durationSeconds);
        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _audioRef.Id != null)
        {
            await InitDurationAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task InitDurationAsync()
    {
        if (_durationInitialized)
            return;

        _durationInitialized = true;

        try
        {
            var duration = await JS.InvokeAsync<double>("audioRangePlayer.getAudioDuration", _audioRef);
            if (duration > 0)
            {
                _durationSeconds = duration;
                _fullRange = new MudExRange<double>(0, _durationSeconds);

                if (_selection.End > _durationSeconds)
                    _selection = new MudExRange<double>(Math.Min(_selection.Start, _durationSeconds), _durationSeconds);

                StateHasChanged();
            }
        }
        catch
        {
            // Im Demo-Fall einfach ignorieren
        }
    }

    private async Task PlaySelectionAsync()
    {
        try
        {
            await JS.InvokeAsync<object>("audioRangePlayer.playSelection", _audioRef, _selection.Start, _selection.End);
        }
        catch
        {
            // Im Demo-Fall ignorieren
        }
    }

    private static string FormatTime(double seconds)
        => FormatTime(TimeSpan.FromSeconds(seconds));

    private static string FormatTime(TimeSpan timeSpan)
        => timeSpan.ToString(timeSpan.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
}

<script>
    window.audioRangePlayer = (function () {

        async function getAudioDuration(audio) {
            if (!audio) return 0;

            if (isNaN(audio.duration) || !isFinite(audio.duration)) {
                await new Promise(resolve => {
                    const cb = () => {
                        audio.removeEventListener('loadedmetadata', cb);
                        resolve();
                    };
                    audio.addEventListener('loadedmetadata', cb);
                });
            }

            return audio.duration || 0;
        }

        function playSelection(audio, start, end) {
            if (!audio || isNaN(start) || isNaN(end) || end <= start) {
                return;
            }

            try {
                audio.pause();
                audio.currentTime = start;

                const handler = () => {
                    if (audio.currentTime >= end) {
                        audio.pause();
                        audio.removeEventListener('timeupdate', handler);
                    }
                };

                audio.addEventListener('timeupdate', handler);
                audio.play();
            } catch (e) {
                console.warn("playSelection failed", e);
            }
        }

        return {
            getAudioDuration,
            playSelection
        };
    })();
</script>

```
