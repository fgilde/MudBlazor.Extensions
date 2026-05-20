using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core.Css;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Picker wrapper around <see cref="MudExClassEditor"/> that fits the MudBlazor picker pattern
/// (MudTextField input + popover/dialog). Use this in property grids or wherever a single-line
/// picker is the expected control for selecting CSS class names.
/// </summary>
public partial class MudExClassEditorPicker : MudExPickerBase<string>
{
    /// <summary> Title </summary>
    [Parameter] public string Title { get; set; }
    
    /// <summary>The view mode used by the inner editor.</summary>
    [Parameter, SafeCategory("Behavior")] public MudExClassEditViewMode ViewMode { get; set; } = MudExClassEditViewMode.List;

    /// <summary>Raised when the editor's view mode changes.</summary>
    [Parameter] public EventCallback<MudExClassEditViewMode> ViewModeChanged { get; set; }

    /// <summary>Shows or hides the editor's toolbar.</summary>
    [Parameter, SafeCategory("Behavior")] public bool EditorShowToolbar { get; set; } = true;

    /// <summary>Allows the user to switch view modes through the editor's toolbar.</summary>
    [Parameter, SafeCategory("Behavior")] public bool AllowViewModeSwitch { get; set; } = true;

    /// <summary>Shows the editor's filter / search box.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowFilter { get; set; } = true;

    /// <summary>Shows the row of currently selected classes above the list.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowSelectedBar { get; set; } = true;

    /// <summary>Shows the inline style snippet under every class name.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowStyles { get; set; } = true;

    /// <summary>Group the list view by source stylesheet.</summary>
    [Parameter, SafeCategory("Behavior")] public bool GroupBySource { get; set; } = true;

    /// <summary>Maximum height of the editor's scroll area.</summary>
    [Parameter, SafeCategory("Appearance")] public string MaxHeight { get; set; } = "420px";

    /// <summary>Only include classes whose name starts with this prefix.</summary>
    [Parameter, SafeCategory("Filtering")] public string? Prefix { get; set; }

    /// <summary>Only include classes from stylesheets whose source label contains this substring.</summary>
    [Parameter, SafeCategory("Filtering")] public string? SourceFilter { get; set; }

    /// <summary>Restrict the result to an explicit allow-list.</summary>
    [Parameter, SafeCategory("Filtering")] public IEnumerable<string>? AvailableClasses { get; set; }

    /// <summary>When true the editor will start a stylesheet scan on first render.</summary>
    [Parameter, SafeCategory("Behavior")] public bool AutoScan { get; set; } = true;

    /// <summary>Render the integrated live preview area below the editor.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowPreview { get; set; } = true;

    /// <summary>Optional custom preview. Receives the current class string.</summary>
    [Parameter, SafeCategory("Appearance")] public RenderFragment<string>? PreviewContent { get; set; }

    /// <summary>Preview heading label inside the editor. Null to hide.</summary>
    [Parameter, SafeCategory("Appearance")] public string? PreviewLabel { get; set; } = "Preview";

    /// <summary>Maximum characters of the inline style snippet shown next to a class.</summary>
    [Parameter, SafeCategory("Appearance")] public int StylePreviewMaxLength { get; set; } = 120;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!IsOverwritten(nameof(AdornmentIcon)))
            AdornmentIcon = Icons.Material.Outlined.FormatPaint;
        if (!IsOverwritten(nameof(IconSize)))
            IconSize = Size.Medium;
        if (!IsOverwritten(nameof(Placeholder)))
            Placeholder = "class-1 class-2 ...";
        if (!IsOverwritten(nameof(Editable)))
            Editable = true;
        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override Task OnPickerClosedAsync()
    {
        Text = Value ?? string.Empty;
        return base.OnPickerClosedAsync();
    }

    /// <inheritdoc />
    protected override async Task StringValueChangedAsync(string value)
    {
        if (!Rendered) return;
        Touched = true;
        var v = value ?? string.Empty;
        if (string.Equals(Value, v, StringComparison.Ordinal)) return;
        Value = v;
        await ValueChanged.InvokeAsync(Value);
    }
}
