using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

public partial class MudExCardList<TData> : MudBaseBindableItemsControl<MudItem, TData>, IMudExComponent
{
    private string _id = Guid.NewGuid().ToFormattedId();
    
    [Inject] protected IJSRuntime JsRuntime { get; set; }

    [Parameter] public Color BackgroundColor { get; set; } = Color.Default;
    [Parameter] public MudColor BackgroundColorCustom { get; set; } = null;
    [Parameter] public Color HoverColor { get; set; } = Color.Primary;
    [Parameter] public MudColor HoverColorCustom { get; set; } = null;
    [Parameter] public bool ZoomOnHover { get; set; } = true;
    [Parameter] public MudExCardHoverMode HoverMode { get; set; } = MudExCardHoverMode.LightBulb;

    [Parameter] public Justify Justify { get; set; } = Justify.Center;
    [Parameter] public int Spacing { get; set; } = 15;

    private string GetCss() => CssBuilder.Default("mud-ex-card-list")
        .AddClass($"mud-ex-card-list-{_id}")
        .AddClass($"mud-ex-card-list-{HoverMode.ToString().ToLower()}")
        .AddClass($"mud-ex-card-list-zoom", ZoomOnHover)
        .Build();

    public string GetStyle()
    {
        return new StyleBuilder()
            .AddStyle($"--mud-ex-card-list-bg-color", $"{BackgroundColorCustom?.ToString(MudColorOutputFormats.HexA) ?? BackgroundColor.CssVarDeclaration()}")
            .AddStyle($"--mud-ex-card-list-hover-color", $"{HoverColorCustom?.ToString(MudColorOutputFormats.HexA) ?? HoverColor.CssVarDeclaration()}")
            .AddStyle($"justify-content", Justify.ToDescriptionString())
            .Build();
    }

    protected override async Task OnParametersSetAsync()
    {
        await SetCssColorVars();
        await base.OnParametersSetAsync();
    }

    private async Task SetCssColorVars()
    {
        await MudExCss.SetCssVariableValueAsync("--mud-ex-card-list-bg-color", BackgroundColorCustom, BackgroundColor);
        await MudExCss.SetCssVariableValueAsync("--mud-ex-card-list-hover-color", HoverColorCustom, HoverColor);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeVoidAsync("eval", @"
                document.querySelector("".mud-ex-cards"").onmousemove = e => {
                  const cardsParent = document.querySelector("".mud-ex-cards"");
                  //const cards = Array.from(cardsParent.children).filter(child => child.classList.contains(""mud-ex-card""));
                  const cards = Array.from(cardsParent.children);                  
                  for(const card of cards) {
                    if (!card.classList.contains(""mud-ex-card"")) {
                        card.classList.add(""mud-ex-card"");
                        card.classList.add(""mud-ex-animate-all-properties"");
                    }
                    const rect = card.getBoundingClientRect(),
                          x = e.clientX - rect.left,
                          y = e.clientY - rect.top;

                    card.style.setProperty(""--mouse-x"", `${x}px`);
                    card.style.setProperty(""--mouse-y"", `${y}px`);
                  };
                };
            ");
        }
        await base.OnAfterRenderAsync(firstRender);
    }
}