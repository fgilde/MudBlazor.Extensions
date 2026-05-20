using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Helper;
using System.Text;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Cool style-string editor. Bindable Style string, three view modes (DevTools, Categorized, Raw),
/// smart per-property value pickers and an optional live preview area.
/// </summary>
public partial class MudExStyleEditor : MudExBaseComponent<MudExStyleEditor>
{
    private static readonly Regex DeclarationRegex = new(@"([\-\w]+)\s*:\s*([^;]+);?", RegexOptions.Compiled);
    private static readonly Regex DisabledRegex = new(@"^\s*/\*\s*([\-\w]+)\s*:\s*([^;]+);?\s*\*/\s*$", RegexOptions.Compiled);

    private List<CssPropertyEntry> _entries = new();
    private string _filter = string.Empty;
    private string _rawDraft = string.Empty;
    private bool _suspendBuild;
    private string _internalValue = string.Empty;
    private Guid? _openPropertyDropdownFor;
    private Guid? _openValueDropdownFor;
    private Guid? _positionedPropertyDropdown;
    private Guid? _positionedValueDropdown;
    private int _highlightedPropertyIndex = -1;
    private int _highlightedValueIndex = -1;

    /// <summary>
    /// The style string this editor binds to. Two-way bindable via <c>@bind-Value</c>.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string Value
    {
        get => _internalValue;
        set
        {
            if (string.Equals(_internalValue, value, StringComparison.Ordinal))
                return;
            _internalValue = value ?? string.Empty;
            ParseFromStyle(_internalValue);
            _rawDraft = _internalValue;
        }
    }

    /// <summary>
    /// Raised whenever <see cref="Value"/> changes.
    /// </summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// Active view mode.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public MudExStyleEditViewMode ViewMode { get; set; } = MudExStyleEditViewMode.DevTools;

    /// <summary>Raised when the view mode changes via the toolbar.</summary>
    [Parameter] public EventCallback<MudExStyleEditViewMode> ViewModeChanged { get; set; }

    /// <summary>Shows or hides the toolbar (view mode buttons, search, dense toggle).</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowToolbar { get; set; } = true;

    /// <summary>Allows the user to switch view modes through the toolbar.</summary>
    [Parameter, SafeCategory("Behavior")] public bool AllowViewModeSwitch { get; set; } = true;

    /// <summary>Shows the property filter / search box.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowFilter { get; set; } = true;

    /// <summary>Compact rendering (smaller paddings, dense controls).</summary>
    [Parameter, SafeCategory("Appearance")] public bool Dense { get; set; }

    /// <summary>Maximum height of the scroll area. Use any css size string.</summary>
    [Parameter, SafeCategory("Appearance")] public string MaxHeight { get; set; } = "420px";

    /// <summary>
    /// Whether to render the integrated live preview area below the editor.
    /// </summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowPreview { get; set; } = true;

    /// <summary>
    /// Optional custom preview content. Receives the current style string.
    /// If not set the editor renders a default preview block.
    /// </summary>
    [Parameter, SafeCategory("Appearance")] public RenderFragment<string>? PreviewContent { get; set; }

    /// <summary>
    /// Heading shown above the preview block. Set to null to hide the heading.
    /// </summary>
    [Parameter, SafeCategory("Appearance")] public string? PreviewLabel { get; set; } = "Preview";

    /// <summary>
    /// Restricts the editable property catalog. When provided, only these properties show up
    /// in the autocomplete and the categorized view.
    /// </summary>
    [Parameter, SafeCategory("Behavior")] public IEnumerable<string>? AllowedProperties { get; set; }

    /// <summary>
    /// When true, properties from the catalog that are not yet set are shown as suggestions
    /// in the categorized view (with empty value).
    /// </summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowCatalogSuggestions { get; set; } = true;

    /// <summary>
    /// Whether the user can disable / enable single declarations (Chrome-style checkbox).
    /// </summary>
    [Parameter, SafeCategory("Behavior")] public bool AllowDisableDeclarations { get; set; } = true;

    /// <summary>Returns all entries (read-only snapshot).</summary>
    public IReadOnlyList<CssPropertyEntry> Entries => _entries;

    /// <summary>All property names from the catalog filtered by <see cref="AllowedProperties"/>.</summary>
    private IEnumerable<string> AllowedCatalogNames
    {
        get
        {
            if (AllowedProperties != null)
                return AllowedProperties.Where(p => !string.IsNullOrWhiteSpace(p));
            return CssPropertyCatalog.All.Keys;
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (_entries.Count == 0 && !string.IsNullOrWhiteSpace(_internalValue))
            ParseFromStyle(_internalValue);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_openPropertyDropdownFor != _positionedPropertyDropdown)
        {
            _positionedPropertyDropdown = _openPropertyDropdownFor;
            if (_openPropertyDropdownFor is { } pId)
                await TryPositionDropdownAsync($"mes-prop-input-{pId:N}", $"mes-prop-dd-{pId:N}");
        }
        if (_openValueDropdownFor != _positionedValueDropdown)
        {
            _positionedValueDropdown = _openValueDropdownFor;
            if (_openValueDropdownFor is { } vId)
                await TryPositionDropdownAsync($"mes-val-input-{vId:N}", $"mes-val-dd-{vId:N}");
        }
    }

    private async Task TryPositionDropdownAsync(string anchorId, string popupId)
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("MudExCssHelper.floatPopupAtAnchor", anchorId, popupId);
        }
        catch
        {
            // ignore - JS not yet ready or element not yet in DOM
        }
    }

    private void ParseFromStyle(string style) => _entries = ParseStyleString(style);

    internal static List<CssPropertyEntry> ParseStyleString(string style)
    {
        var list = new List<CssPropertyEntry>();
        if (string.IsNullOrWhiteSpace(style)) return list;

        var rawSegments = SplitTopLevelDeclarations(style);
        foreach (var raw in rawSegments)
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0) continue;
            var disabled = DisabledRegex.Match(trimmed);
            if (disabled.Success)
            {
                list.Add(new CssPropertyEntry
                {
                    Property = disabled.Groups[1].Value.Trim(),
                    Value = disabled.Groups[2].Value.Trim(),
                    Enabled = false
                });
                continue;
            }

            var m = DeclarationRegex.Match(trimmed);
            if (!m.Success) continue;
            list.Add(new CssPropertyEntry
            {
                Property = m.Groups[1].Value.Trim(),
                Value = m.Groups[2].Value.Trim(),
                Enabled = true
            });
        }
        return list;
    }

    private static IEnumerable<string> SplitTopLevelDeclarations(string style)
    {
        // Top-level split on ';' respecting parentheses (rgba(...), calc(...), url(...) ...)
        var result = new List<string>();
        if (style == null) return result;
        var depth = 0;
        var sb = new StringBuilder();
        foreach (var ch in style)
        {
            if (ch == '(') depth++;
            else if (ch == ')') depth = Math.Max(0, depth - 1);
            if (ch == ';' && depth == 0)
            {
                if (sb.Length > 0) result.Add(sb.ToString());
                sb.Clear();
                continue;
            }
            sb.Append(ch);
        }
        if (sb.Length > 0) result.Add(sb.ToString());
        return result;
    }

    internal static string BuildStyleString(IEnumerable<CssPropertyEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.Property)) continue;
            var value = (e.Value ?? string.Empty).Trim();
            if (value.Length == 0) continue;
            if (e.Enabled)
                sb.Append(e.Property.Trim()).Append(": ").Append(value).Append("; ");
            else
                sb.Append("/* ").Append(e.Property.Trim()).Append(": ").Append(value).Append("; */ ");
        }
        return sb.ToString().TrimEnd();
    }

    private async Task RaiseChangedAsync()
    {
        var newValue = BuildStyleString(_entries);
        StateHasChanged();
        if (string.Equals(newValue, _internalValue, StringComparison.Ordinal)) return;
        _suspendBuild = true;
        _internalValue = newValue;
        _rawDraft = newValue;
        _suspendBuild = false;
        await ValueChanged.InvokeAsync(_internalValue);
    }

    private async Task OnRawChanged(string newValue)
    {
        if (_suspendBuild) return;
        _rawDraft = newValue ?? string.Empty;
        _entries = ParseStyleString(_rawDraft);
        _internalValue = BuildStyleString(_entries);
        await ValueChanged.InvokeAsync(_internalValue);
    }

    private async Task AddEntryAsync(string property = "", string value = "")
    {
        _entries.Add(new CssPropertyEntry { Property = property, Value = value, Enabled = true });
        await RaiseChangedAsync();
    }

    private async Task RemoveEntryAsync(CssPropertyEntry entry)
    {
        _entries.Remove(entry);
        await RaiseChangedAsync();
    }

    private async Task SetPropertyAsync(CssPropertyEntry entry, string newProperty)
    {
        entry.Property = (newProperty ?? string.Empty).Trim();
        await RaiseChangedAsync();
    }

    private async Task SetValueAsync(CssPropertyEntry entry, string newValue)
    {
        entry.Value = (newValue ?? string.Empty).Trim();
        await RaiseChangedAsync();
    }

    private async Task ToggleEnabledAsync(CssPropertyEntry entry, bool enabled)
    {
        entry.Enabled = enabled;
        await RaiseChangedAsync();
    }

    private async Task OnViewModeButtonAsync(MudExStyleEditViewMode mode)
    {
        if (mode == ViewMode) return;
        ViewMode = mode;
        await ViewModeChanged.InvokeAsync(mode);
        StateHasChanged();
    }

    private bool MatchesFilter(CssPropertyEntry entry)
    {
        if (string.IsNullOrWhiteSpace(_filter)) return true;
        var f = _filter.Trim().ToLowerInvariant();
        return (entry.Property?.ToLowerInvariant().Contains(f) ?? false)
               || (entry.Value?.ToLowerInvariant().Contains(f) ?? false);
    }

    private bool MatchesFilter(string property)
    {
        if (string.IsNullOrWhiteSpace(_filter)) return true;
        return property.ToLowerInvariant().Contains(_filter.Trim().ToLowerInvariant());
    }

    private IEnumerable<IGrouping<CssPropertyCategory, CssPropertyEntry>> GroupedByCategory()
    {
        // Existing entries grouped by their property's catalog category.
        var byCategory = _entries
            .Where(MatchesFilter)
            .GroupBy(e => CssPropertyCatalog.Get(e.Property).Category);
        return byCategory.OrderBy(g => (int)g.Key);
    }

    private IEnumerable<string> SuggestedPropertiesFor(CssPropertyCategory cat)
    {
        if (!ShowCatalogSuggestions) yield break;
        var existing = new HashSet<string>(_entries.Select(e => e.Property), StringComparer.OrdinalIgnoreCase);
        foreach (var name in AllowedCatalogNames)
        {
            var desc = CssPropertyCatalog.Get(name);
            if (desc.Category != cat) continue;
            if (existing.Contains(name)) continue;
            if (!MatchesFilter(name)) continue;
            yield return name;
        }
    }

    private Task<IEnumerable<string>> SearchPropertiesAsync(string value, CancellationToken token)
    {
        var pool = AllowedCatalogNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult<IEnumerable<string>>(pool.Take(40).ToArray());
        var v = value.Trim().ToLowerInvariant();
        return Task.FromResult<IEnumerable<string>>(
            pool.Where(n => n.ToLowerInvariant().Contains(v)).Take(40).ToArray());
    }

    private Task<IEnumerable<string>> SearchValuesAsync(CssPropertyEntry entry, string value, CancellationToken token)
    {
        var desc = CssPropertyCatalog.Get(entry.Property);
        IEnumerable<string> pool = desc.EffectiveSuggestions();
        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult(pool.Take(40));
        var v = value.Trim().ToLowerInvariant();
        return Task.FromResult(pool.Where(n => n.ToLowerInvariant().Contains(v)).Take(40));
    }

    private void OpenPropertyDropdown(CssPropertyEntry entry)
    {
        _openPropertyDropdownFor = entry.Id;
        _highlightedPropertyIndex = -1;
        StateHasChanged();
    }

    private async Task SchedulePropertyCloseAsync(Guid id)
    {
        await Task.Delay(160);
        if (_openPropertyDropdownFor == id)
        {
            _openPropertyDropdownFor = null;
            StateHasChanged();
        }
    }

    private void OnPropertyTyping(CssPropertyEntry entry, string value)
    {
        entry.Property = value;
        _highlightedPropertyIndex = -1;
        StateHasChanged();
    }

    private async Task OnPropertyCommitAsync(CssPropertyEntry entry, string value)
    {
        entry.Property = (value ?? string.Empty).Trim();
        await RaiseChangedAsync();
    }

    private async Task SelectPropertySuggestionAsync(CssPropertyEntry entry, string suggestion)
    {
        entry.Property = suggestion;
        _openPropertyDropdownFor = null;
        await RaiseChangedAsync();
    }

    private async Task OnPropertyKeyDownAsync(KeyboardEventArgs e, CssPropertyEntry entry)
    {
        var suggestions = FilteredPropertySuggestions(entry).ToList();

        if (e.Key == "Escape")
        {
            _openPropertyDropdownFor = null;
            _highlightedPropertyIndex = -1;
            StateHasChanged();
        }
        else if (e.Key == "ArrowDown" && suggestions.Count > 0)
        {
            if (_openPropertyDropdownFor != entry.Id) OpenPropertyDropdown(entry);
            _highlightedPropertyIndex = _highlightedPropertyIndex < 0
                ? 0
                : Math.Min(suggestions.Count - 1, _highlightedPropertyIndex + 1);
            StateHasChanged();
        }
        else if (e.Key == "ArrowUp" && suggestions.Count > 0)
        {
            _highlightedPropertyIndex = _highlightedPropertyIndex <= 0
                ? suggestions.Count - 1
                : _highlightedPropertyIndex - 1;
            StateHasChanged();
        }
        else if (e.Key == "Enter")
        {
            string? pick = null;
            if (_highlightedPropertyIndex >= 0 && _highlightedPropertyIndex < suggestions.Count)
                pick = suggestions[_highlightedPropertyIndex];
            else if (suggestions.Count == 1)
                pick = suggestions[0];

            if (!string.IsNullOrEmpty(pick) && !string.Equals(pick, entry.Property, StringComparison.OrdinalIgnoreCase))
                await SelectPropertySuggestionAsync(entry, pick);
            else
                _openPropertyDropdownFor = null;
            _highlightedPropertyIndex = -1;
            StateHasChanged();
        }
    }

    private IEnumerable<string> FilteredPropertySuggestions(CssPropertyEntry entry)
    {
        var query = (entry.Property ?? string.Empty).Trim().ToLowerInvariant();
        var pool = AllowedCatalogNames.OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
            return pool.Take(80);
        return pool.Where(n => n.ToLowerInvariant().Contains(query)).Take(80);
    }

    private void OpenValueDropdown(CssPropertyEntry entry)
    {
        var desc = CssPropertyCatalog.Get(entry.Property);
        if (desc.EffectiveSuggestions().Length == 0) return;
        _openValueDropdownFor = entry.Id;
        _highlightedValueIndex = -1;
        StateHasChanged();
    }

    private async Task ScheduleValueCloseAsync(Guid id)
    {
        await Task.Delay(160);
        if (_openValueDropdownFor == id)
        {
            _openValueDropdownFor = null;
            StateHasChanged();
        }
    }

    private void OnValueTyping(CssPropertyEntry entry, string value)
    {
        entry.Value = value;
        _highlightedValueIndex = -1;
        StateHasChanged();
    }

    private async Task OnValueCommitAsync(CssPropertyEntry entry, string value)
    {
        entry.Value = (value ?? string.Empty).Trim();
        await RaiseChangedAsync();
    }

    private async Task SelectValueSuggestionAsync(CssPropertyEntry entry, string suggestion)
    {
        entry.Value = suggestion;
        _openValueDropdownFor = null;
        await RaiseChangedAsync();
    }

    private async Task OnValueKeyDownAsync(KeyboardEventArgs e, CssPropertyEntry entry)
    {
        var suggestions = FilteredValueSuggestions(entry).ToList();

        if (e.Key == "Escape")
        {
            _openValueDropdownFor = null;
            _highlightedValueIndex = -1;
            StateHasChanged();
        }
        else if (e.Key == "ArrowDown" && suggestions.Count > 0)
        {
            if (_openValueDropdownFor != entry.Id) OpenValueDropdown(entry);
            _highlightedValueIndex = _highlightedValueIndex < 0
                ? 0
                : Math.Min(suggestions.Count - 1, _highlightedValueIndex + 1);
            StateHasChanged();
        }
        else if (e.Key == "ArrowUp" && suggestions.Count > 0)
        {
            _highlightedValueIndex = _highlightedValueIndex <= 0
                ? suggestions.Count - 1
                : _highlightedValueIndex - 1;
            StateHasChanged();
        }
        else if (e.Key == "Enter" && _highlightedValueIndex >= 0 && _highlightedValueIndex < suggestions.Count)
        {
            await SelectValueSuggestionAsync(entry, suggestions[_highlightedValueIndex]);
            _highlightedValueIndex = -1;
        }
    }

    private IEnumerable<string> FilteredValueSuggestions(CssPropertyEntry entry)
    {
        var desc = CssPropertyCatalog.Get(entry.Property);
        var pool = desc.EffectiveSuggestions();
        if (pool.Length == 0) return Array.Empty<string>();
        var query = (entry.Value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(query))
            return pool;
        // Show all if user input exactly matches one suggestion (so they can see siblings)
        if (pool.Any(p => string.Equals(p, entry.Value, StringComparison.OrdinalIgnoreCase)))
            return pool;
        return pool.Where(n => n.ToLowerInvariant().Contains(query));
    }
}
