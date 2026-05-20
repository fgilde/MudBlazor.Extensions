using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core.Css;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Picker wrapper around <see cref="MudExStyleEditor"/> that fits the MudBlazor picker pattern
/// (MudTextField input + popover/dialog). Use this when you want to embed the style editor in a
/// property grid, an object edit, or anywhere a single-line picker is expected.
/// </summary>
public partial class MudExStyleEditorPicker : MudExPickerBase<string>
{
    /// <summary> Title </summary>
    [Parameter] public string Title { get; set; }
    
    /// <summary>The view mode used by the inner editor.</summary>
    [Parameter, SafeCategory("Behavior")] public MudExStyleEditViewMode ViewMode { get; set; } = MudExStyleEditViewMode.DevTools;

    /// <summary>Raised when the editor's view mode changes.</summary>
    [Parameter] public EventCallback<MudExStyleEditViewMode> ViewModeChanged { get; set; }

    /// <summary>Shows or hides the editor's toolbar (view mode buttons, search).</summary>
    [Parameter, SafeCategory("Behavior")] public bool EditorShowToolbar { get; set; } = true;

    /// <summary>Allows the user to switch view modes through the editor's toolbar.</summary>
    [Parameter, SafeCategory("Behavior")] public bool AllowViewModeSwitch { get; set; } = true;

    /// <summary>Shows the property filter / search box in the editor.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowFilter { get; set; } = true;

    /// <summary>Compact rendering (smaller paddings, dense controls).</summary>
    [Parameter, SafeCategory("Appearance")] public bool Dense { get; set; }

    /// <summary>Maximum height of the editor's scroll area.</summary>
    [Parameter, SafeCategory("Appearance")] public string MaxHeight { get; set; } = "420px";

    /// <summary>Whether the editor renders its integrated live preview area below the editor body.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowPreview { get; set; } = true;

    /// <summary>Optional custom preview content for the editor. Receives the current style string.</summary>
    [Parameter, SafeCategory("Appearance")] public RenderFragment<string>? PreviewContent { get; set; }

    /// <summary>Preview heading label inside the editor. Null to hide.</summary>
    [Parameter, SafeCategory("Appearance")] public string? PreviewLabel { get; set; } = "Preview";

    /// <summary>Restricts the editable property catalog of the editor.</summary>
    [Parameter, SafeCategory("Behavior")] public IEnumerable<string>? AllowedProperties { get; set; }

    /// <summary>Whether to show catalog suggestions in the categorized view.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ShowCatalogSuggestions { get; set; } = true;

    /// <summary>Whether the user can disable / enable single declarations via checkbox.</summary>
    [Parameter, SafeCategory("Behavior")] public bool AllowDisableDeclarations { get; set; } = true;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!IsOverwritten(nameof(AdornmentIcon)))
            AdornmentIcon = Icons.Material.Outlined.Style;
        if (!IsOverwritten(nameof(IconSize)))
            IconSize = Size.Medium;
        if (!IsOverwritten(nameof(Placeholder)))
            Placeholder = "style: value; ...";
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
