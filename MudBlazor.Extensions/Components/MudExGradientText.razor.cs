using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Utilities;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A Component to display text with a gradient foreground or background
/// </summary>
public partial class MudExGradientText: IMudExComponent
{
    [Inject] protected IServiceProvider ServiceProvider { get; set; }
    public IJSRuntime JsRuntime => ServiceProvider.GetService<IJSRuntime>();

    [Parameter]
    public int Radius { get; set; } = 90;

    [Parameter]
    public bool GradientForeground { get; set; } = true;

    [Parameter]
    public MudColor TextFillColor { get; set; } = null;

    [Parameter]
    public bool Animate { get; set; } = true;

    [Parameter]
    public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromSeconds(3);

    [Parameter]
    [Category("Behavior")]
    public IEnumerable<MudColor> Palette { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Palette ??= await DefaultPaletteByTheme();
    }

    private string GetStyle()
    {
        if (Palette == null || !Palette.Any())
            return string.Empty;
        var fillColorStr = TextFillColor == null ? "transparent" : TextFillColor.ToString(MudColorOutputFormats.HexA);
        var res = MudExCss.GenerateCssString(new
        {
            Background = $"linear-gradient({Radius}deg, {string.Join(',', Palette.Select(c => c.ToString(MudColorOutputFormats.HexA)))})",
            BackgroundSize = "200% auto",
        });
        if (GradientForeground)
            res = res.EnsureEndsWith(";") + $"-webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; text-fill-color: {fillColorStr};";
        else if (TextFillColor != null)
            res = res.EnsureEndsWith(";") + $"color: {fillColorStr};";
        if (Animate)
            res = res.EnsureEndsWith(";") + "animation: reverse textclip "+ AnimationDuration.TotalMilliseconds + "ms linear infinite;@keyframes textclip { to { background-position: 200% center; }}";
        
        return res;
    }

    private async Task<IEnumerable<MudColor>> DefaultPaletteByTheme()
    {
        await JsRuntime.InitializeMudBlazorExtensionsAsync();
        var themeColors = await MudExCss.GetCssColorVariablesAsync();
        return themeColors
            .Where(c => !c.Key.Contains("background", StringComparison.InvariantCultureIgnoreCase) && !c.Key.Contains("surface", StringComparison.InvariantCultureIgnoreCase) && !c.Value.IsBlack() && !c.Value.IsWhite() && c.Value.APercentage >= 1.0)
            .Select(x => x.Value)
            .Distinct()
            .Take(10);
    }

    private RenderFragment Inherited() => builder =>
    {
        base.BuildRenderTree(builder);
    };
}