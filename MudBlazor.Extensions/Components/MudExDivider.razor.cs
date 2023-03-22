using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

public partial class MudExDivider : IMudExComponent
{
    private RenderFragment Inherited() => builder =>
    {
        base.BuildRenderTree(builder);
    };

    [Inject] protected IServiceProvider ServiceProvider { get; set; }
    public IJSRuntime JsRuntime => ServiceProvider?.GetService<IJSRuntime>();
    
    [Parameter] public Color Color { get; set; } = Color.Default;
    [Parameter] public int Size { get; set; } = 1;
    [Parameter] public CssUnit SizeUnit { get; set; } = CssUnit.Pixels;

    protected override async Task OnParametersSetAsync()
    {
        Class = GetClass();
        Style = GetStyle();
        await base.OnParametersSetAsync();
    }

    private string GetClass()
    {
        return $"{(Vertical ? "mud-ex-divider-vertical" : "mud-ex-divider-horizontal")}";
    }

    protected virtual string GetStyle()
    {
        return MudExCss.GenerateCssString(new
        {
            BorderWidth = 0,
            BackgroundColor = Color == Color.Default ? "var(--mud-palette-divider)" : Color.CssVarDeclaration(),
            Width = Vertical ? Size.ToString() : "unset",
            MaxWidth = Vertical ? Size.ToString() : "unset",
            Height = !Vertical ? Size.ToString() : "unset",
            MaxHeight = !Vertical ? Size.ToString() : "unset",
        }, SizeUnit);
    }

}