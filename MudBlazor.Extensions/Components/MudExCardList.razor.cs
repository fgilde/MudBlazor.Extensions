using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple Card List with a hover effect.
/// </summary>
/// <typeparam name="TData"></typeparam>
public partial class MudExCardList<TData> : MudBaseBindableItemsControl<MudItem, TData>, IJsMudExComponent<MudExCardList<TData>>
{
    private string _id = Guid.NewGuid().ToFormattedId();
    private MudExCardHoverMode? _hoverMode = MudExCardHoverMode.LightBulb | MudExCardHoverMode.Zoom;
    /// <summary>
    /// Dependency Injection for IJSRuntime Service.
    /// </summary>
    [Inject]
    public IJSRuntime JsRuntime { get; set; }

    /// <summary>
    /// Gets or Sets IJSObjectReference JsReference Property.
    /// </summary>
    public IJSObjectReference JsReference { get; set; }

    /// <summary>
    /// Gets or Sets IJSObjectReference ModuleReference Property.
    /// </summary>
    public IJSObjectReference ModuleReference { get; set; }

    /// <summary>
    /// Gets or Sets ElementReference Property.
    /// </summary>
    public ElementReference ElementReference { get; set; }

    private IJsMudExComponent<MudExCardList<TData>> AsJsComponent => this;

    /// <summary>
    /// Gets or Sets MudExColor BackgroundColor Property.
    /// </summary>
    [Parameter]
    public MudExColor BackgroundColor { get; set; } = Color.Default;

    /// <summary>
    /// Gets or Sets MudExColor HoverColor Property.
    /// </summary>
    [Parameter]
    public MudExColor HoverColor { get; set; } = Color.Primary;

    /// <summary>
    /// Gets or Sets bool ZoomOnHover Property.
    /// </summary>
    [Obsolete("Use HoverMode instead")]
    [Parameter]
    public bool ZoomOnHover
    {
        get => HoverModeMatches(MudExCardHoverMode.Zoom);
        set
        {
            if (value && HoverMode.HasValue)
                HoverMode |= MudExCardHoverMode.Zoom;
            else if (!value && HoverMode.HasValue) HoverMode &= ~MudExCardHoverMode.Zoom;
        }
    }

    /// <summary>
    /// Gets or Sets MudExCardHoverMode HoverMode Property.
    /// </summary>
    /// <value>
    /// MudExCardHoverMode
    /// </value>
    [Parameter]
    public MudExCardHoverMode? HoverMode
    {
        get => _hoverMode;
        set
        {
            _hoverMode = value;
            _ = UpdateJs();
        }
    }

    /// <summary>
    /// Gets or Sets Justify Property.
    /// </summary>
    [Parameter]
    public Justify Justify { get; set; } = Justify.Center;

    /// <summary>
    /// Gets or Sets Spacing Property.
    /// </summary>
    [Parameter]
    public int Spacing { get; set; } = 15;

    /// <summary>
    /// Gets or Sets Light Bulb Size Property.
    /// </summary>
    [Parameter]
    public int LightBulbSize { get; set; } = 30;

    /// <summary>
    /// Gets or Sets Light Bulb Size Unit Property.
    /// </summary>
    [Parameter]
    public CssUnit LightBulbSizeUnit { get; set; } = CssUnit.Percentage;

    private bool HoverModeMatches(MudExCardHoverMode mode) => HoverMode.HasValue && HoverMode.Value.HasFlag(mode);

    /// <summary>
    /// Methods returns List of MudExCardHoverMode, where hover modes are applied.
    /// </summary>
    public List<MudExCardHoverMode> AllAppliedHoverModes => Enum.GetValues(typeof(MudExCardHoverMode)).Cast<MudExCardHoverMode>().Where(HoverModeMatches).ToList();

    private string GetCss()
    {
        var res = CssBuilder.Default("mud-ex-card-list")
            .AddClass($"mud-ex-card-list-{_id}");

        foreach (var mode in AllAppliedHoverModes)
            res.AddClass($"mud-ex-card-list-{mode.ToString().ToLower()}");

        return res.Build();
    }

    /// <summary>
    /// Style for outer element.
    /// </summary>
    /// <returns></returns>
    public string GetStyle()
    {
        return new StyleBuilder()
            .AddStyle($"--mud-ex-card-bulb-size", $"{GetBulbSize()}{LightBulbSizeUnit.ToDescriptionString()}")
            .AddStyle($"--mud-ex-card-list-bg-color", $"{BackgroundColor.ToCssStringValue()}")
            .AddStyle($"--mud-ex-card-list-hover-color", $"{HoverColor.ToCssStringValue()}")
            .AddStyle($"justify-content", Justify.ToDescriptionString())
            .Build();
    }

    private int GetBulbSize() => LightBulbSizeUnit == CssUnit.Percentage ? Math.Min(Math.Max(LightBulbSize, 0), 100) : LightBulbSize;

    /// <summary>
    /// Method gets called OnParametersSetAsync.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await UpdateJs();
    }

    /// <summary>
    /// Method gets called UpdateJs for fetching JSRuntime.
    /// </summary>
    private async Task UpdateJs()
    {
        if (AsJsComponent.JsReference != null)
            await AsJsComponent.JsReference.InvokeVoidAsync("initialize", Options());
    }

    /// <summary>
    /// Method gets called OnAfterRenderAsync for rendering and Initializing Modules.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await AsJsComponent.ImportModuleAndCreateJsAsync();
        await base.OnAfterRenderAsync(firstRender);
    }

    private object Options()
    {
        return new
        {
            Id = _id,
            Use3dEffect = HoverModeMatches(MudExCardHoverMode.CardEffect3d),
            UseZoomEffect = HoverModeMatches(MudExCardHoverMode.Zoom)
        };
    }

    /// <summary>
    /// Method to dispose the module.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        return AsJsComponent.DisposeModulesAsync();
    }
}