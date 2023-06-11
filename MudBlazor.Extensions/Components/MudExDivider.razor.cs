using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A divider to separate content
/// </summary>
public partial class MudExDivider : IMudExComponent
{
    private string _existingStyle = string.Empty;

    private RenderFragment Inherited() => builder =>
    {
        base.BuildRenderTree(builder);
    };

    [Inject] protected IServiceProvider ServiceProvider { get; set; }
    public IJSRuntime JsRuntime => ServiceProvider?.GetService<IJSRuntime>();

    [Parameter] public string Label { get; set; }
    
    [Parameter] public MudExColor Color { get; set; } = MudBlazor.Color.Default;

    [Parameter] public MudExSize<double> Size { get; set; } = 1;
    
    /// <summary>
    /// If this option is true border size is used instead of element size
    /// </summary>
    [Parameter] public bool UseBorder { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(_existingStyle) && !string.IsNullOrEmpty(Style))
            _existingStyle = Style;
        Class = GetClass();
        Style = GetStyle();
        UserAttributes.AddOrUpdate("data-label", Label);
        await base.OnParametersSetAsync();
    }

    private string GetClass()
    {
        return MudExCssBuilder.Default
            .AddClass("mud-ex-divider-vertical", Vertical)
            .AddClass("mud-ex-divider-horizontal", !Vertical)
            .AddClass("mud-ex-labeled-hr", !string.IsNullOrEmpty(Label))
            .Build();
    }

    protected virtual string GetStyle()
    {
        return MudExStyleBuilder.FromObject(new
        {
            BorderWidth = UseBorder ? Size : 0,
            BackgroundColor = Color.Is(MudBlazor.Color.Default) ? "var(--mud-palette-divider)" : Color.ToCssStringValue(),
        }, _existingStyle, Size.SizeUnit)
            .WithWidth(Size, Vertical && !UseBorder)
            .WithMaxWidth(Size, Vertical && !UseBorder)
            .WithHeight(Size, !Vertical && !UseBorder)
            .WithMaxHeight(Size, !Vertical && !UseBorder)
            .Build();
    }

}