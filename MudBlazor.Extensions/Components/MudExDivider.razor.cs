using BlazorParameterCastingMagic;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
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

    /// <summary>
    /// Render base component
    /// </summary>
    private RenderFragment Inherited() => builder =>
    {
        base.BuildRenderTree(builder);
    };

    /// <summary>
    /// Injected service provider
    /// </summary>
    [Inject] protected IServiceProvider ServiceProvider { get; set; }
    
    /// <summary>
    /// JsRuntime
    /// </summary>
    public IJSRuntime JsRuntime => ServiceProvider?.GetService<IJSRuntime>();

    /// <summary>
    /// Label
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string Label { get; set; }

    /// <summary>
    /// Set to true to add border to label
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public bool BorderLabel { get; set; } = true;

    /// <summary>
    /// Color
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExColor Color { get; set; } = MudBlazor.Color.Default;

    /// <summary>
    /// Size
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    [AllowMagicCasting]
    public MudExSize<double> Size { get; set; } = 1;

    /// <summary>
    /// If this option is true border size is used instead of element size
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool UseBorder { get; set; }


    private string Id = Guid.NewGuid().ToFormattedId();

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(_existingStyle) && !string.IsNullOrEmpty(Style))
            _existingStyle = Style;
        Class = GetClass();
        Style = GetStyle();
        UserAttributes.AddOrUpdate("data-label", Label);
        await base.OnParametersSetAsync();
    }

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) 
        => base.SetParametersAsync(parameters.ApplyMagicCasting(this));

    private string GetClass()
    {
        return MudExCssBuilder.Default
            .AddClass($"mud-ex-divider-{Id}", !string.IsNullOrEmpty(Label))
            .AddClass("mud-ex-divider-vertical", Vertical)
            .AddClass("mud-ex-divider-horizontal", !Vertical)
            .AddClass("mud-ex-labeled-hr", !string.IsNullOrEmpty(Label))
            .Build();
    }

    /// <summary>
    /// Style for divider
    /// </summary>
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