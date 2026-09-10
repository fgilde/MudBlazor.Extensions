using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Viewer for plain text log files (.log, .out, .err). Splits the file into lines, detects the log level of each
/// line (ERROR/WARN/INFO/DEBUG/TRACE/FATAL), colorizes it accordingly and offers a search box plus level filter
/// chips. Fully client-side, no native dependencies.
/// </summary>
public partial class MudExFileDisplayLog : IMudExFileDisplay
{
    private object _loadedSource;
    private static readonly Regex LevelRegex = new(@"\b(FATAL|CRITICAL|ERROR|WARN(?:ING)?|INFO|DEBUG|TRACE)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayLog);

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    private List<LogLine> _lines;
    private string _errorMessage;
    private string _search = string.Empty;
    private readonly HashSet<string> _hiddenLevels = new(StringComparer.OrdinalIgnoreCase);

    // Materialized instead of a lazy IEnumerable: the markup virtualizes over it and also needs
    // its Count, so re-filtering per render would walk a huge log twice on every keystroke.
    private List<LogLine> _visibleLines = new();

    private void ApplyFilter() => _visibleLines = _lines?.Where(l =>
        (string.IsNullOrEmpty(_search) || l.Text.Contains(_search, StringComparison.OrdinalIgnoreCase)) &&
        !_hiddenLevels.Contains(l.Level ?? string.Empty)).ToList() ?? new List<LogLine>();

    // Counted once after parsing and ordered by severity: the toolbar shows a count per level, and deriving
    // that from _lines on every render would walk the whole file once per chip.
    private List<LevelInfo> _levels = new();

    private bool IsLevelVisible(string level) => !_hiddenLevels.Contains(level);

    private bool IsFiltered => !string.IsNullOrEmpty(_search) || _hiddenLevels.Count > 0;

    private void ResetFilter()
    {
        _search = string.Empty;
        _hiddenLevels.Clear();
        ApplyFilter();
    }

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var fileName = fileDisplayInfos?.FileName ?? string.Empty;
        var isMatch = fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
                      || fileName.EndsWith(".out", StringComparison.OrdinalIgnoreCase)
                      || fileName.EndsWith(".err", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(isMatch);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Lines", _lines?.Count ?? 0 },
            { "Errors", _lines?.Count(l => string.Equals(l.Level, "ERROR", StringComparison.OrdinalIgnoreCase) || string.Equals(l.Level, "FATAL", StringComparison.OrdinalIgnoreCase)) ?? 0 },
            { "Warnings", _lines?.Count(l => l.Level?.StartsWith("WARN", StringComparison.OrdinalIgnoreCase) == true) ?? 0 }
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
            _lines = null;
            _errorMessage = null;
            await LoadLogAsync();
            // A viewer knows its own values only now, and its renders do not reach the host.
            await FileDisplayInfos.NotifyMetaChangedAsync();
        }
    }

    private async Task LoadLogAsync()
    {
        try
        {
            var content = await FileService.ReadAsStringFromFileDisplayInfosAsync(FileDisplayInfos);
            if (content == null)
                return;

            _lines = content.SplitLines().Select(ParseLine).ToList();
            _levels = _lines.Where(l => l.Level != null)
                .GroupBy(l => l.Level, StringComparer.OrdinalIgnoreCase)
                .Select(g => new LevelInfo(g.Key, g.Count()))
                .OrderBy(l => SeverityRank(l.Level))
                .ToList();
            ApplyFilter();
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        StateHasChanged();
    }

    private static LogLine ParseLine(string text)
    {
        var match = LevelRegex.Match(text);
        var level = match.Success ? NormalizeLevel(match.Value) : null;
        return new LogLine(text, level);
    }

    private static string NormalizeLevel(string raw) => raw.ToUpperInvariant() switch
    {
        "WARNING" => "WARN",
        "CRITICAL" => "FATAL",
        var other => other
    };

    private static string CssForLevel(string level) => level switch
    {
        "FATAL" => "mud-ex-log-fatal",
        "ERROR" => "mud-ex-log-error",
        "WARN" => "mud-ex-log-warn",
        "INFO" => "mud-ex-log-info",
        "DEBUG" => "mud-ex-log-debug",
        "TRACE" => "mud-ex-log-trace",
        _ => string.Empty
    };

    /// <summary>
    /// A theme color for the filter chips. The css classes above only set a text color, which is invisible on a
    /// filled chip - the chip needs the palette color itself to make its on/off state readable.
    /// </summary>
    private static Color ChipColorForLevel(string level) => level switch
    {
        "FATAL" or "ERROR" => Color.Error,
        "WARN" => Color.Warning,
        "INFO" => Color.Info,
        _ => Color.Default
    };

    private static int SeverityRank(string level) => level switch
    {
        "FATAL" => 0,
        "ERROR" => 1,
        "WARN" => 2,
        "INFO" => 3,
        "DEBUG" => 4,
        "TRACE" => 5,
        _ => 6
    };

    private void ToggleLevel(string level)
    {
        if (!_hiddenLevels.Add(level))
            _hiddenLevels.Remove(level);
        ApplyFilter();
    }

    private sealed record LogLine(string Text, string Level);

    private sealed record LevelInfo(string Level, int Count);
}
