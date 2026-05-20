using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core.Css;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Picker / browser for the CSS classes available on the current page.
/// Bindable class string with live scan, search, multi-select, per-class style preview and
/// three view modes (List, Compact, Grid) for embedding in dialogs or popovers.
/// </summary>
public partial class MudExClassEditor : MudExBaseComponent<MudExClassEditor>
{
    private readonly HashSet<string> _selected = new(StringComparer.Ordinal);
    private CssClassInfoGroup[] _groups = Array.Empty<CssClassInfoGroup>();
    private string _internalValue = string.Empty;
    private string _filter = string.Empty;
    private string? _expandedGroup;
    private bool _isScanning;

    /// <summary>The class string this editor binds to. Two-way bindable via <c>@bind-Value</c>.</summary>
    [Parameter, SafeCategory("Data")]
    public string Value
    {
        get => _internalValue;
        set
        {
            if (string.Equals(_internalValue, value, StringComparison.Ordinal)) return;
            _internalValue = value ?? string.Empty;
            SyncSelectedFromClassString();
        }
    }

    /// <summary>Raised whenever <see cref="Value"/> changes.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Active view mode.</summary>
    [Parameter, SafeCategory("Behavior")] public MudExClassEditViewMode ViewMode { get; set; } = MudExClassEditViewMode.List;

    /// <summary>Raised when the user toggles the view mode through the toolbar.</summary>
    [Parameter] public EventCallback<MudExClassEditViewMode> ViewModeChanged { get; set; }

    /// <summary>Shows or hides the toolbar (view mode, search, refresh).</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowToolbar { get; set; } = true;

    /// <summary>Allow switching the view mode in the toolbar.</summary>
    [Parameter, SafeCategory("Behavior")] public bool AllowViewModeSwitch { get; set; } = true;

    /// <summary>Show the filter / search field.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowFilter { get; set; } = true;

    /// <summary>Show the row of currently selected classes above the list.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowSelectedBar { get; set; } = true;

    /// <summary>Show inline style snippet for every class (next to / under the name).</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowStyles { get; set; } = true;

    /// <summary>Group the list view by source stylesheet.</summary>
    [Parameter, SafeCategory("Behavior")] public bool GroupBySource { get; set; } = true;

    /// <summary>Maximum height of the scroll area (any CSS size).</summary>
    [Parameter, SafeCategory("Appearance")] public string MaxHeight { get; set; } = "420px";

    /// <summary>
    /// Only include classes whose name starts with this prefix.
    /// </summary>
    [Parameter, SafeCategory("Filtering")] public string? Prefix { get; set; }

    /// <summary>
    /// Only include classes from stylesheets whose source label contains this substring.
    /// </summary>
    [Parameter, SafeCategory("Filtering")] public string? SourceFilter { get; set; }

    /// <summary>
    /// Restrict the result to an explicit allow-list. When set, only classes whose name appears
    /// in this list are shown — useful for limiting the picker to a curated set.
    /// </summary>
    [Parameter, SafeCategory("Filtering")] public IEnumerable<string>? AvailableClasses { get; set; }

    /// <summary>
    /// When true the editor will start a stylesheet scan on first render.
    /// </summary>
    [Parameter, SafeCategory("Behavior")] public bool AutoScan { get; set; } = true;

    /// <summary>
    /// Render an optional live preview area below the editor.
    /// </summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowPreview { get; set; } = true;

    /// <summary>Optional custom preview. Receives the current class string.</summary>
    [Parameter, SafeCategory("Appearance")] public RenderFragment<string>? PreviewContent { get; set; }

    /// <summary>Heading shown above the preview block. Set to null to hide.</summary>
    [Parameter, SafeCategory("Appearance")] public string? PreviewLabel { get; set; } = "Preview";

    /// <summary>Snapshot of all scanned classes.</summary>
    public IReadOnlyList<CssClassInfoGroup> AvailableGroups => _groups;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && AutoScan)
        {
            await ScanAsync();
        }
    }

    /// <summary>
    /// Re-scans the document's stylesheets for available classes.
    /// </summary>
    public async Task ScanAsync()
    {
        try
        {
            _isScanning = true;
            StateHasChanged();
            var options = new { includeStyles = ShowStyles, prefix = Prefix ?? string.Empty, sourceFilter = SourceFilter ?? string.Empty };
            var result = await JsRuntime.InvokeAsync<CssClassInfoGroup[]>("MudExCssHelper.getAllCssClasses", options);
            _groups = ApplyAllowList(NormalizeStyles(result ?? Array.Empty<CssClassInfoGroup>()));
            _expandedGroup ??= _groups.FirstOrDefault()?.Source;
        }
        catch
        {
            _groups = Array.Empty<CssClassInfoGroup>();
        }
        finally
        {
            _isScanning = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Maximum characters of the inline style snippet shown next to a class.
    /// Long values (e.g. base64 data: URLs) are truncated with an ellipsis.
    /// </summary>
    [Parameter, SafeCategory("Appearance")] public int StylePreviewMaxLength { get; set; } = 120;

    private CssClassInfoGroup[] NormalizeStyles(CssClassInfoGroup[] input)
    {
        var limit = Math.Max(20, StylePreviewMaxLength);
        foreach (var g in input)
        {
            foreach (var c in g.Classes)
            {
                if (string.IsNullOrEmpty(c.Styles)) continue;
                var s = c.Styles.Replace('\n', ' ').Replace('\r', ' ');
                if (s.Length > limit)
                    s = s.Substring(0, limit) + "...";
                c.Styles = s;
            }
        }
        return input;
    }

    private CssClassInfoGroup[] ApplyAllowList(CssClassInfoGroup[] input)
    {
        if (AvailableClasses == null) return input;
        var allow = new HashSet<string>(AvailableClasses, StringComparer.OrdinalIgnoreCase);
        return input
            .Select(g => new CssClassInfoGroup
            {
                Source = g.Source,
                Classes = g.Classes.Where(c => allow.Contains(c.Name)).ToArray()
            })
            .Where(g => g.Classes.Length > 0)
            .ToArray();
    }

    private void SyncSelectedFromClassString()
    {
        _selected.Clear();
        if (string.IsNullOrWhiteSpace(_internalValue)) return;
        foreach (var token in _internalValue.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            _selected.Add(token);
    }

    private async Task ToggleClassAsync(string className)
    {
        if (!_selected.Add(className))
            _selected.Remove(className);
        await RaiseChangedAsync();
    }

    private async Task RemoveClassAsync(string className)
    {
        if (_selected.Remove(className))
            await RaiseChangedAsync();
    }

    private async Task ClearAllAsync()
    {
        if (_selected.Count == 0) return;
        _selected.Clear();
        await RaiseChangedAsync();
    }

    private async Task RaiseChangedAsync()
    {
        _internalValue = string.Join(' ', _selected);
        await ValueChanged.InvokeAsync(_internalValue);
        StateHasChanged();
    }

    private async Task OnViewModeButtonAsync(MudExClassEditViewMode mode)
    {
        if (mode == ViewMode) return;
        ViewMode = mode;
        await ViewModeChanged.InvokeAsync(mode);
        StateHasChanged();
    }

    private bool IsSelected(string className) => _selected.Contains(className);

    private bool MatchesFilter(CssClassInfo info)
    {
        if (string.IsNullOrWhiteSpace(_filter)) return true;
        var f = _filter.Trim().ToLowerInvariant();
        return info.Name.ToLowerInvariant().Contains(f)
               || (info.Styles?.ToLowerInvariant().Contains(f) ?? false);
    }

    private IEnumerable<CssClassInfoGroup> FilteredGroups()
    {
        foreach (var g in _groups)
        {
            var classes = g.Classes.Where(MatchesFilter).ToArray();
            if (classes.Length == 0) continue;
            yield return new CssClassInfoGroup { Source = g.Source, Classes = classes };
        }
    }

    private IEnumerable<CssClassInfo> FlatFiltered() =>
        _groups.SelectMany(g => g.Classes).Where(MatchesFilter).OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase);
}
