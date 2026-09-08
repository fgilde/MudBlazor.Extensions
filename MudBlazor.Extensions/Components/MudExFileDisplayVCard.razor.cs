using MudBlazor.Extensions.Helper.Internal;
using System.Text;
using AuralizeBlazor.Types;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Viewer for vCard (.vcf) contact files and iCalendar (.ics) calendar files. Both formats share the same simple
/// "KEY:VALUE" line based structure (RFC 6350 / RFC 5545), so a single lightweight parser handles both. Renders
/// contacts as cards and calendar events as a chronological list. Fully client-side, no external dependency.
/// </summary>
public partial class MudExFileDisplayVCard : IMudExFileDisplay
{
    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayVCard);

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    private List<VCardContact> _contacts;
    private List<CalendarEvent> _events;
    private string _errorMessage;
    private bool _isCalendar;

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var fileName = fileDisplayInfos?.FileName ?? string.Empty;
        var isMatch = fileName.EndsWith(".vcf", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".ics", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(isMatch);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Contacts", _contacts?.Count ?? 0 },
            { "Events", _events?.Count ?? 0 }
        });
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // See MudExFileDisplayDataBase for the rationale of checking for a usable source instead of reference equality.
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var hasSource = infos != null && (!string.IsNullOrEmpty(infos.Url) || infos.ContentStream is { Length: > 0 });
        var notYetLoaded = _contacts == null && _events == null && _errorMessage == null;

        await base.SetParametersAsync(parameters);

        if (hasSource && notYetLoaded)
            await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var content = await FileService.ReadAsStringFromFileDisplayInfosAsync(FileDisplayInfos);
            if (content == null)
                return;

            var fileName = FileDisplayInfos?.FileName ?? string.Empty;
            _isCalendar = fileName.EndsWith(".ics", StringComparison.OrdinalIgnoreCase);

            if (_isCalendar)
                _events = ParseCalendar(content);
            else
                _contacts = ParseVCards(content);
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        StateHasChanged();
    }

    private static List<VCardContact> ParseVCards(string content)
    {
        var result = new List<VCardContact>();
        foreach (var block in SplitComponents(content, "VCARD"))
        {
            var props = ParseProperties(block);
            result.Add(new VCardContact
            {
                FullName = GetValue(props, "FN"),
                Organization = GetValue(props, "ORG"),
                Title = GetValue(props, "TITLE"),
                Emails = GetAllValues(props, "EMAIL"),
                Phones = GetAllValues(props, "TEL"),
                Addresses = GetRawValues(props, "ADR").Select(FormatAddress).Where(a => !string.IsNullOrWhiteSpace(a)).ToList(),
                Note = GetValue(props, "NOTE")
            });
        }
        return result;
    }

    private static List<CalendarEvent> ParseCalendar(string content)
    {
        var result = new List<CalendarEvent>();
        foreach (var block in SplitComponents(content, "VEVENT"))
        {
            var props = ParseProperties(block);
            result.Add(new CalendarEvent
            {
                Summary = GetValue(props, "SUMMARY"),
                Location = GetValue(props, "LOCATION"),
                Description = GetValue(props, "DESCRIPTION"),
                Start = ParseIcsDate(GetValue(props, "DTSTART")),
                End = ParseIcsDate(GetValue(props, "DTEND")),
                Organizer = GetValue(props, "ORGANIZER")
            });
        }
        return result.OrderBy(e => e.Start).ToList();
    }

    /// <summary>
    /// Extracts all "BEGIN:&lt;name&gt; ... END:&lt;name&gt;" blocks (vCard/vEvent components), unfolding
    /// RFC-required line continuations (a line starting with a space/tab continues the previous line) first.
    /// </summary>
    private static IEnumerable<string> SplitComponents(string content, string componentName)
    {
        var unfolded = content.NormalizeLineEndings().Replace("\n ", "").Replace("\n\t", "");
        var lines = unfolded.Split('\n');
        var current = new List<string>();
        var inBlock = false;

        foreach (var line in lines)
        {
            if (line.Equals($"BEGIN:{componentName}", StringComparison.OrdinalIgnoreCase))
            {
                inBlock = true;
                current = new List<string>();
                continue;
            }
            if (line.Equals($"END:{componentName}", StringComparison.OrdinalIgnoreCase))
            {
                inBlock = false;
                yield return string.Join('\n', current);
                continue;
            }
            if (inBlock)
                current.Add(line);
        }
    }

    private static Dictionary<string, List<string>> ParseProperties(string block)
    {
        var props = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in block.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex < 0)
                continue;

            var key = line[..separatorIndex].Split(';')[0].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (!props.TryGetValue(key, out var list))
                props[key] = list = new List<string>();
            list.Add(value);
        }
        return props;
    }

    private static string GetValue(Dictionary<string, List<string>> props, string key) =>
        props.TryGetValue(key, out var list) ? Unescape(list.FirstOrDefault()) : null;

    private static List<string> GetAllValues(Dictionary<string, List<string>> props, string key) =>
        GetRawValues(props, key).Select(Unescape).ToList();

    /// <summary>Values as they appear in the file - needed where the escapes still carry meaning, e.g. ADR.</summary>
    private static List<string> GetRawValues(Dictionary<string, List<string>> props, string key) =>
        props.TryGetValue(key, out var list) ? list : new List<string>();

    /// <summary>
    /// Un-escapes a property value as defined by RFC 5545 / RFC 6350: "\n" and "\N" are line breaks, every other
    /// backslash escape yields the escaped character itself. Scanned character by character instead of chained
    /// Replace calls so an escaped backslash followed by an "n" is not mistaken for a line break.
    /// </summary>
    private static string Unescape(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains('\\'))
            return value;

        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\' || i + 1 == value.Length)
            {
                sb.Append(value[i]);
                continue;
            }

            var escaped = value[++i];
            sb.Append(escaped is 'n' or 'N' ? '\n' : escaped);
        }
        return sb.ToString();
    }

    /// <summary>
    /// ADR values are seven ";" separated components (post office box, extended address, street, locality, region,
    /// postal code, country). Joins the non empty ones so a raw ";;Street;City;;12345;Country" becomes readable.
    /// </summary>
    private static string FormatAddress(string raw) =>
        string.Join(", ", SplitUnescaped(raw, ';').Select(Unescape).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));

    private static IEnumerable<string> SplitUnescaped(string value, char separator)
    {
        var start = 0;
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\')
            {
                i++;
                continue;
            }
            if (value[i] != separator)
                continue;

            yield return value[start..i];
            start = i + 1;
        }
        yield return value[start..];
    }

    private static DateTime? ParseIcsDate(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var format = value.Length switch
        {
            8 => "yyyyMMdd",
            var l when l >= 15 && value.EndsWith("Z") => "yyyyMMdd'T'HHmmss'Z'",
            var l when l >= 15 => "yyyyMMdd'T'HHmmss",
            _ => null
        };

        if (format != null && DateTime.TryParseExact(value, format, null, System.Globalization.DateTimeStyles.None, out var parsed))
            return parsed;

        return DateTime.TryParse(value, out var fallback) ? fallback : null;
    }

    /// <summary>A single parsed vCard contact.</summary>
    public class VCardContact
    {
        /// <summary>Formatted full name (FN).</summary>
        public string FullName { get; set; }
        /// <summary>Organization (ORG).</summary>
        public string Organization { get; set; }
        /// <summary>Job title (TITLE).</summary>
        public string Title { get; set; }
        /// <summary>All email addresses (EMAIL).</summary>
        public List<string> Emails { get; set; } = new();
        /// <summary>All phone numbers (TEL).</summary>
        public List<string> Phones { get; set; } = new();
        /// <summary>All addresses (ADR).</summary>
        public List<string> Addresses { get; set; } = new();
        /// <summary>Free text note (NOTE).</summary>
        public string Note { get; set; }
    }

    /// <summary>A single parsed calendar event (VEVENT).</summary>
    public class CalendarEvent
    {
        /// <summary>Event title (SUMMARY).</summary>
        public string Summary { get; set; }
        /// <summary>Event location (LOCATION).</summary>
        public string Location { get; set; }
        /// <summary>Event description (DESCRIPTION).</summary>
        public string Description { get; set; }
        /// <summary>Start date/time (DTSTART).</summary>
        public DateTime? Start { get; set; }
        /// <summary>End date/time (DTEND).</summary>
        public DateTime? End { get; set; }
        /// <summary>Organizer (ORGANIZER).</summary>
        public string Organizer { get; set; }
    }
}
