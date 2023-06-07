using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A divider to separate content
/// </summary>
public partial class MudExDivider : IMudExComponent
{
    private RenderFragment Inherited() => builder =>
    {
        base.BuildRenderTree(builder);
    };

    [Inject] protected IServiceProvider ServiceProvider { get; set; }
    public IJSRuntime JsRuntime => ServiceProvider?.GetService<IJSRuntime>();
    
    [Parameter] public Color Color { get; set; } = Color.Default;
    [Parameter] public MudExSize<double> Size { get; set; } = 1;
    
    /// <summary>
    /// If this option is true border size is used instead of element size
    /// </summary>
    [Parameter] public bool UseBorder { get; set; } 
    
    protected override async Task OnParametersSetAsync()
    {
        Class = GetClass();
        Style = GetStyle();
        await base.OnParametersSetAsync();
    }

    private string GetClass() => $"{(Vertical ? "mud-ex-divider-vertical" : "mud-ex-divider-horizontal")}";

    protected virtual string GetStyle()
    {
       return MudExStyleBuilder.FromObject(new
        {
            BorderWidth = UseBorder ? Size : 0,
            BackgroundColor = Color == Color.Default ? "var(--mud-palette-divider)" : Color.CssVarDeclaration(),
        }, Size.SizeUnit)
            .WithWidth(Size, Vertical && !UseBorder)
            .WithMaxWidth(Size, Vertical && !UseBorder)
            .WithHeight(Size, !Vertical && !UseBorder)
            .WithMaxHeight(Size, !Vertical && !UseBorder)
            .Build();
    }

}