using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A Component to display text with a gradient foreground or background
/// </summary>
public partial class MudExGradientText : IMudExComponent
{
    /// <summary>
    /// Gets or sets the service provider to use when retrieving services.
    /// </summary>
    [Inject]
    public IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// Gets the JavaScript interop instance.
    /// </summary>
    public IJSRuntime JsRuntime => ServiceProvider.GetService<IJSRuntime>();

    /// <summary>
    /// Gets or sets the radius of the gradient.
    /// </summary>
    [Parameter]
    public int Radius { get; set; } = 90;

    /// <summary>
    /// Gets or sets a value indicating whether the gradient should be applied to the text foreground.
    /// </summary>
    [Parameter]
    public bool GradientForeground { get; set; } = true;

    /// <summary>
    /// Gets or sets the fill color of the text.
    /// </summary>
    [Parameter]
    public MudExColor TextFillColor { get; set; } = Color.Transparent;

    /// <summary>
    /// Gets or sets a value indicating whether to animate the gradient.
    /// </summary>
    [Parameter]
    public bool Animate { get; set; } = true;

    /// <summary>
    /// Gets or sets the duration of the animation.
    /// </summary>
    [Parameter]
    public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Gets or sets the collection of colors to use for the gradient.
    /// </summary>
    [Parameter]
    [Category("Behavior")]
    public IEnumerable<MudExColor> Palette { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Palette ??= new List<MudExColor>
        {
            Color.Primary,
            Color.Secondary,
            Color.Tertiary,
            Color.Info,
            Color.Success,
            Color.Warning,
        }; ;
    }

    private string GetStyle()
    {
        if (Palette == null || !Palette.Any())
            return string.Empty;
        var fillColorStr = TextFillColor.ToCssStringValue();

        return MudExStyleBuilder.FromObject(new {
                Background = $"linear-gradient({Radius}deg, {string.Join(',', Palette.Select(c => c.ToCssStringValue()))})",
                BackgroundSize = "200% auto"
            })
            .WithStyle($"-webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; text-fill-color: {fillColorStr};", GradientForeground)
            .WithColor(fillColorStr, !GradientForeground && !TextFillColor.Is(Color.Transparent))
            .WithStyle("animation: reverse textclip " + AnimationDuration.TotalMilliseconds + "ms linear infinite;@keyframes textclip { to { background-position: 200% center; }}", Animate)
            .Build();
    }
    
    private RenderFragment Inherited() => builder =>
    {
        base.BuildRenderTree(builder);
    };
}