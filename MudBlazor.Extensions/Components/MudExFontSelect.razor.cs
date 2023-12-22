using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Dropdown list component to select one ore more fonts
/// </summary>
public partial class MudExFontSelect
{
    private bool _initialized;
    private string[] _preInitParameters;

    /// <summary>
    /// Render base component
    /// </summary>
    protected RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    /// <summary>
    /// FontFamily
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string FontFamily {
        get => SelectedValues?.Any() == true ? string.Join(",", SelectedValues) : string.Empty;
        set => SelectedValues = value?.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    /// <summary>
    /// Set to true to allow selection of google latin fonts
    /// </summary>
    [Parameter, SafeCategory("Behavior")] 
    public bool WithGoogleLatinFonts { get; set; }

    /// <inheritdoc />
    protected override Task<IList<string>> GetAvailableItemsAsync(CancellationToken cancellation = default)
        => Task.FromResult(GetAvailable());

    private IList<string> GetAvailable() 
        => MudExFonts.WebSafeFonts.Concat(WithGoogleLatinFonts ? MudExFonts.GoogleLatinFonts : Array.Empty<string>()).ToList();

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if(!_initialized)
            _preInitParameters = parameters.ToDictionary().Select(x => x.Key).ToArray();
        return base.SetParametersAsync(parameters);
    }

    private bool IsOverwritten(string paramName) => _preInitParameters?.Contains(paramName) == true;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();        
        if(!IsOverwritten(nameof(ItemTemplate)))
            ItemTemplate = GetItemTemplate();
        if (!IsOverwritten(nameof(Virtualize)))
            Virtualize = true;
        if (!IsOverwritten(nameof(MultiSelection)))
            MultiSelection = true;
        if (!IsOverwritten(nameof(ValuePresenter)))
            ValuePresenter = Options.ValuePresenter.Chip;
        _initialized = true;
    }

    protected override bool NeedsValueUpdateForNonMultiSelection()
    {
        return false;
    }
}