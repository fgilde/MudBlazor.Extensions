using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Dropdown list component to select one ore more fonts
/// </summary>
public partial class MudExFontSelect
{
    /// <summary>
    /// Render base component
    /// </summary>
    protected RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    /// <summary>
    /// FontFamily
    /// </summary>
    [Parameter]
    public string FontFamily {
        get => Selected?.Any() == true ? string.Join(",", Selected) : string.Empty;
        set => Selected = value?.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    /// <summary>
    /// Set to true to allow selection of google latin fonts
    /// </summary>
    [Parameter] public bool WithGoogleLatinFonts { get; set; }

    protected override Task<IList<string>> GetAvailableItemsAsync(CancellationToken cancellation = default)
        => Task.FromResult(GetAvailable());

    private IList<string> GetAvailable() 
        => MudExFonts.WebSafeFonts.Concat(WithGoogleLatinFonts ? MudExFonts.GoogleLatinFonts : Array.Empty<string>()).ToList();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        ItemTemplate = GetItemTemplate();
        UseCustomItemRenderInSelectionPopover = true;
        ViewMode = ViewMode.NoChips;
        base.OnInitialized();
    }
}