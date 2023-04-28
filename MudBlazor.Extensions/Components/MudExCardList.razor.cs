using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
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

    [Inject] public IJSRuntime JsRuntime { get; set; }
    public IJSObjectReference JsReference { get; set; }
    public IJSObjectReference ModuleReference { get; set; }
    public ElementReference ElementReference { get; set; }
    private IJsMudExComponent<MudExCardList<TData>> AsJsComponent => this;
    
    [Parameter] public Color BackgroundColor { get; set; } = Color.Default;
    [Parameter] public MudColor BackgroundColorCustom { get; set; } = null;
    [Parameter] public Color HoverColor { get; set; } = Color.Primary;
    [Parameter] public MudColor HoverColorCustom { get; set; } = null;
    [Parameter] public bool ZoomOnHover { get; set; } = true;
    [Parameter] public MudExCardHoverMode HoverMode { get; set; } = MudExCardHoverMode.LightBulb;
    [Parameter] public Justify Justify { get; set; } = Justify.Center;
    [Parameter] public int Spacing { get; set; } = 15;
    [Parameter] public int LightBulbSize { get; set; } = 30;
    [Parameter] public CssUnit LightBulbSizeUnit { get; set; } = CssUnit.Percentage;

    private string GetCss() => CssBuilder.Default("mud-ex-card-list")
        .AddClass($"mud-ex-card-list-{_id}")
        .AddClass($"mud-ex-card-list-{HoverMode.ToString().ToLower()}")
        .AddClass($"mud-ex-card-list-zoom", ZoomOnHover)
        .Build();

    public string GetStyle()
    {
        return new StyleBuilder()
            .AddStyle($"--mud-ex-card-bulb-size", $"{GetBulbSize()}{LightBulbSizeUnit.ToDescriptionString()}")
            .AddStyle($"--mud-ex-card-list-bg-color", $"{BackgroundColorCustom?.ToString(MudColorOutputFormats.HexA) ?? BackgroundColor.CssVarDeclaration()}")
            .AddStyle($"--mud-ex-card-list-hover-color", $"{HoverColorCustom?.ToString(MudColorOutputFormats.HexA) ?? HoverColor.CssVarDeclaration()}")
            .AddStyle($"justify-content", Justify.ToDescriptionString())
            .Build();
    }

    private int GetBulbSize() => LightBulbSizeUnit == CssUnit.Percentage ? Math.Min(Math.Max(LightBulbSize, 0), 100) : LightBulbSize;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (AsJsComponent.JsReference != null)
            await AsJsComponent.JsReference.InvokeVoidAsync("initialize", Options());
    }

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
            Id = _id
        };
    }

    public ValueTask DisposeAsync()
    {
        return AsJsComponent.DisposeModulesAsync();
    }
}