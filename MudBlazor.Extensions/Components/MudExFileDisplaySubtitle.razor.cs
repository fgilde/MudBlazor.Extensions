using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using System.Text;
using System.Text.RegularExpressions;
using AuralizeBlazor.Types;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Viewer for subtitle and lyric files (.srt, .vtt, .lrc, .ttml, .dfxp, .itt, .ass, .ssa). Shows every cue with its
/// timecode and offers a full text search. SubRip, LRC and TTML are parsed by <see cref="LyricData"/> from
/// AuralizeBlazor (the same model the audio player uses for its lyrics overlay); WebVTT and SubStation Alpha are
/// converted into that model here because they have no index line respectively a completely different layout.
/// </summary>
public partial class MudExFileDisplaySubtitle : IMudExFileDisplay
{
    // WebVTT / SubRip timecode: "00:01:02.500" or "01:02,500" - the hour part is optional in WebVTT.
    private static readonly Regex VttCueRegex = new(
        @"^(?:(?<h>\d+):)?(?<m>\d{1,2}):(?<s>\d{1,2})[.,](?<ms>\d{1,3})\s*-->",
        RegexOptions.Compiled);

    // SubStation Alpha event: "Dialogue: 0,0:00:01.00,0:00:04.00,Default,,0,0,0,,Text with, commas"
    private static readonly Regex AssDialogueRegex = new(
        @"^Dialogue:\s*[^,]*,(?<start>[^,]+),(?<end>[^,]+)(?:,[^,]*){6},(?<text>.*)$",
        RegexOptions.Compiled);

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplaySubtitle);

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    private object _loadedSource;
    private static readonly string[] SupportedExtensions = { ".srt", ".vtt", ".lrc", ".ttml", ".dfxp", ".itt", ".ass", ".ssa" };

    private LyricData _lyrics;
    private List<LyricLine> _visibleLines = new();
    private string _errorMessage;
    private string _search = string.Empty;

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var fileName = fileDisplayInfos?.FileName ?? string.Empty;
        return Task.FromResult(SupportedExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Cues", _lyrics?.Lines.Count ?? 0 },
            { "Duration", _lyrics?.Lines.LastOrDefault()?.TimeStamp.ToString(@"hh\:mm\:ss") }
        });
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // The same viewer instance serves every file of its kind, and FileDisplayInfos is always
        // the same object - so only the values tell one file from the next.
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var sourceChanged = infos.SourceChanged(ref _loadedSource);

        await base.SetParametersAsync(parameters);

        if (sourceChanged)
        {
            _lyrics = null;
            _errorMessage = null;
            await LoadSubtitlesAsync();
            // A viewer knows its own values only now, and its renders do not reach the host.
            await FileDisplayInfos.NotifyMetaChangedAsync();
        }
    }

    private async Task LoadSubtitlesAsync()
    {
        try
        {
            var content = await FileService.ReadAsStringFromFileDisplayInfosAsync(FileDisplayInfos);
            if (content == null)
                return;

            _lyrics = Parse(FileDisplayInfos?.FileName ?? string.Empty, content);
            ApplyFilter();
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        StateHasChanged();
    }

    private static LyricData Parse(string fileName, string content)
    {
        if (fileName.EndsWith(".lrc", StringComparison.OrdinalIgnoreCase))
            return LyricData.FromLrc(content);
        if (fileName.EndsWith(".ttml", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".dfxp", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".itt", StringComparison.OrdinalIgnoreCase))
            return LyricData.FromTtml(Encoding.UTF8.GetBytes(content)); // no string overload for TTML
        if (fileName.EndsWith(".ass", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".ssa", StringComparison.OrdinalIgnoreCase))
            return ParseSubStationAlpha(content);
        if (fileName.EndsWith(".vtt", StringComparison.OrdinalIgnoreCase))
            return ParseWebVtt(content);

        return LyricData.FromSrt(content);
    }

    /// <summary>
    /// WebVTT looks like SubRip but has no index line, uses a dot before the milliseconds and may carry cue
    /// settings behind the end timecode plus NOTE/STYLE/REGION blocks, so it gets its own small parser.
    /// </summary>
    private static LyricData ParseWebVtt(string content)
    {
        var result = new LyricData();
        var lines = content.SplitLines();
        var skipBlock = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.Length == 0)
            {
                skipBlock = false;
                continue;
            }

            if (line.StartsWith("NOTE", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("STYLE", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("REGION", StringComparison.OrdinalIgnoreCase))
                skipBlock = true;

            if (skipBlock || line.StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase))
                continue;

            var match = VttCueRegex.Match(line);
            if (!match.Success)
                continue;

            var text = new List<string>();
            while (++i < lines.Length && lines[i].Trim().Length > 0)
                text.Add(StripTags(lines[i].Trim()));

            result.Add(new LyricLine(ToTimeSpan(match), string.Join(Environment.NewLine, text)));
        }

        return result;
    }

    private static LyricData ParseSubStationAlpha(string content)
    {
        var result = new LyricData();

        foreach (var line in content.SplitLines())
        {
            var match = AssDialogueRegex.Match(line.Trim());
            if (!match.Success || !TimeSpan.TryParse(match.Groups["start"].Value.Trim(), out var start))
                continue;

            // "\N" is a hard line break in SubStation Alpha, "{...}" are inline override tags.
            var text = Regex.Replace(match.Groups["text"].Value, @"\{[^}]*\}", string.Empty)
                .Replace("\\N", Environment.NewLine)
                .Replace("\\n", Environment.NewLine);

            result.Add(new LyricLine(start, text.Trim()));
        }

        return result;
    }

    private static TimeSpan ToTimeSpan(Match match) => new(0,
        match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value) : 0,
        int.Parse(match.Groups["m"].Value),
        int.Parse(match.Groups["s"].Value),
        int.Parse(match.Groups["ms"].Value.PadRight(3, '0')));

    private static string StripTags(string value) => Regex.Replace(value, "<[^>]+>", string.Empty);

    private void ApplyFilter() => _visibleLines = _lyrics?.Lines
        .Where(l => string.IsNullOrEmpty(_search) || l.Text?.Contains(_search, StringComparison.OrdinalIgnoreCase) == true)
        .ToList() ?? new List<LyricLine>();
}
