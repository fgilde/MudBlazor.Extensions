using System.Text.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Viewer for HTTP Archive files (.har) as exported by the network tab of every browser. Lists all requests with
/// method, status, type, size and duration, draws a waterfall relative to the first request and shows headers,
/// query string, post data and timing breakdown of the selected entry. Read with
/// <see cref="JsonDocument"/> instead of a serializer so it stays trimming friendly in Blazor WebAssembly.
/// </summary>
public partial class MudExFileDisplayHar : IMudExFileDisplay
{
    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayHar);

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    private List<HarEntry> _entries;
    private List<HarEntry> _visibleEntries = new();
    private HarEntry _selected;
    private string _creator;
    private string _errorMessage;
    private string _search = string.Empty;
    private double _totalMilliseconds;

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
        => Task.FromResult(fileDisplayInfos?.FileName?.EndsWith(".har", StringComparison.OrdinalIgnoreCase) == true);

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Creator", _creator },
            { "Requests", _entries?.Count ?? 0 },
            { "Failed", _entries?.Count(e => e.Status >= 400 || e.Status == 0) ?? 0 },
            { "Transferred", Nextended.Blazor.Extensions.BrowserFileExtensions.GetReadableFileSize(_entries?.Sum(e => e.Size) ?? 0) },
            { "Total time", $"{_totalMilliseconds:F0} ms" }
        });
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // See MudExFileDisplayDataBase for the rationale of checking for a usable source instead of reference equality.
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var hasSource = infos != null && (!string.IsNullOrEmpty(infos.Url) || infos.ContentStream is { Length: > 0 });
        var notYetLoaded = _entries == null && _errorMessage == null;

        await base.SetParametersAsync(parameters);

        if (hasSource && notYetLoaded)
            await LoadHarAsync();
    }

    private async Task LoadHarAsync()
    {
        try
        {
            var json = await FileService.ReadAsStringFromFileDisplayInfosAsync(FileDisplayInfos);
            if (string.IsNullOrEmpty(json))
                return;

            (_entries, _creator) = Parse(json);
            var start = _entries.Count > 0 ? _entries.Min(e => e.Started) : DateTimeOffset.MinValue;
            foreach (var entry in _entries)
                entry.OffsetMilliseconds = (entry.Started - start).TotalMilliseconds;

            _totalMilliseconds = _entries.Count > 0 ? _entries.Max(e => e.OffsetMilliseconds + e.Duration) : 0;
            ApplyFilter();
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        StateHasChanged();
    }

    private static (List<HarEntry> Entries, string Creator) Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("log", out var log))
            throw new FormatException("The file does not contain a HAR log object.");

        var creator = log.TryGetProperty("creator", out var creatorEl) && creatorEl.TryGetProperty("name", out var creatorName)
            ? $"{creatorName.GetString()} {(creatorEl.TryGetProperty("version", out var v) ? v.GetString() : null)}".Trim()
            : null;

        var entries = new List<HarEntry>();
        if (log.TryGetProperty("entries", out var entriesEl) && entriesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var entryEl in entriesEl.EnumerateArray())
                entries.Add(ParseEntry(entryEl));
        }

        return (entries, creator);
    }

    private static HarEntry ParseEntry(JsonElement entryEl)
    {
        var request = entryEl.TryGetProperty("request", out var requestEl) ? requestEl : default;
        var response = entryEl.TryGetProperty("response", out var responseEl) ? responseEl : default;
        var content = response.ValueKind == JsonValueKind.Object && response.TryGetProperty("content", out var contentEl) ? contentEl : default;

        var url = GetString(request, "url") ?? string.Empty;
        var size = GetLong(response, "_transferSize");
        if (size <= 0)
            size = GetLong(content, "size");

        return new HarEntry
        {
            Started = DateTimeOffset.TryParse(GetString(entryEl, "startedDateTime"), out var started) ? started : DateTimeOffset.MinValue,
            Duration = GetDouble(entryEl, "time"),
            Method = GetString(request, "method") ?? "GET",
            Url = url,
            ShortUrl = ShortenUrl(url),
            HttpVersion = GetString(request, "httpVersion"),
            Status = (int)GetLong(response, "status"),
            StatusText = GetString(response, "statusText"),
            MimeType = GetString(content, "mimeType"),
            ResourceType = GetString(entryEl, "_resourceType"),
            ServerAddress = GetString(entryEl, "serverIPAddress"),
            Size = size,
            RequestHeaders = ReadNameValues(request, "headers"),
            ResponseHeaders = ReadNameValues(response, "headers"),
            QueryString = ReadNameValues(request, "queryString"),
            PostData = request.ValueKind == JsonValueKind.Object && request.TryGetProperty("postData", out var postEl)
                ? GetString(postEl, "text")
                : null,
            Timings = ReadTimings(entryEl)
        };
    }

    private static List<(string Name, string Value)> ReadNameValues(JsonElement parent, string propertyName)
    {
        var result = new List<(string, string)>();
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(propertyName, out var arrayEl) || arrayEl.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var item in arrayEl.EnumerateArray())
            result.Add((GetString(item, "name"), GetString(item, "value")));
        return result;
    }

    private static List<(string Name, double Value)> ReadTimings(JsonElement entryEl)
    {
        var result = new List<(string, double)>();
        if (!entryEl.TryGetProperty("timings", out var timingsEl) || timingsEl.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var timing in timingsEl.EnumerateObject())
        {
            // The spec uses -1 for phases that do not apply to a request.
            if (timing.Value.ValueKind == JsonValueKind.Number && timing.Value.GetDouble() >= 0)
                result.Add((timing.Name, timing.Value.GetDouble()));
        }
        return result;
    }

    private static string GetString(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static long GetLong(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number
            ? (long)el.GetDouble()
            : 0;

    private static double GetDouble(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number
            ? el.GetDouble()
            : 0;

    private static string ShortenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;
        var path = uri.AbsolutePath == "/" ? "/" : uri.AbsolutePath.TrimEnd('/');
        return $"{path}{uri.Query}";
    }

    private void ApplyFilter() => _visibleEntries = _entries?
        .Where(e => string.IsNullOrEmpty(_search)
                    || e.Url.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || e.Method.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || e.Status.ToString().Contains(_search, StringComparison.OrdinalIgnoreCase))
        .ToList() ?? new List<HarEntry>();

    private static Color ColorForStatus(int status) => status switch
    {
        0 => Color.Default,
        >= 500 => Color.Error,
        >= 400 => Color.Error,
        >= 300 => Color.Warning,
        >= 200 => Color.Success,
        _ => Color.Info
    };

    /// <summary>Left offset of the waterfall bar in percent of the whole capture.</summary>
    private double WaterfallOffset(HarEntry entry) => _totalMilliseconds <= 0 ? 0 : entry.OffsetMilliseconds / _totalMilliseconds * 100;

    /// <summary>Width of the waterfall bar in percent, never below a hairline so short requests stay visible.</summary>
    private double WaterfallWidth(HarEntry entry) => _totalMilliseconds <= 0 ? 0 : Math.Max(entry.Duration / _totalMilliseconds * 100, 0.5);

    /// <summary>One request/response pair of the archive.</summary>
    public sealed class HarEntry
    {
        /// <summary>Wall clock time the request started.</summary>
        public DateTimeOffset Started { get; set; }
        /// <summary>Milliseconds between the first request of the capture and this one.</summary>
        public double OffsetMilliseconds { get; set; }
        /// <summary>Total duration in milliseconds.</summary>
        public double Duration { get; set; }
        /// <summary>HTTP method.</summary>
        public string Method { get; set; }
        /// <summary>Full request url.</summary>
        public string Url { get; set; }
        /// <summary>Path and query only, used for the table column.</summary>
        public string ShortUrl { get; set; }
        /// <summary>HTTP version of the request.</summary>
        public string HttpVersion { get; set; }
        /// <summary>Response status code, 0 when the request never completed.</summary>
        public int Status { get; set; }
        /// <summary>Response status text.</summary>
        public string StatusText { get; set; }
        /// <summary>Mime type of the response body.</summary>
        public string MimeType { get; set; }
        /// <summary>Resource type the browser assigned (document, script, xhr, ...).</summary>
        public string ResourceType { get; set; }
        /// <summary>Server ip address if the capture recorded one.</summary>
        public string ServerAddress { get; set; }
        /// <summary>Transferred size in bytes.</summary>
        public long Size { get; set; }
        /// <summary>Request headers.</summary>
        public List<(string Name, string Value)> RequestHeaders { get; set; } = new();
        /// <summary>Response headers.</summary>
        public List<(string Name, string Value)> ResponseHeaders { get; set; } = new();
        /// <summary>Parsed query string parameters.</summary>
        public List<(string Name, string Value)> QueryString { get; set; } = new();
        /// <summary>Raw request body, if any.</summary>
        public string PostData { get; set; }
        /// <summary>Timing phases that apply to this request.</summary>
        public List<(string Name, double Value)> Timings { get; set; } = new();
    }
}
