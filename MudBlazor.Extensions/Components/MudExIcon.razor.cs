using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExIcon is a simple MudIcon with extended possibilities to set all colors.
/// </summary>
public partial class MudExIcon
{
    protected string Classname =>
        new MudExCssBuilder("mud-icon-root")
            .AddClass("mud-icon-default", Color.Is(MudBlazor.Color.Inherit) || Color.Is(MudBlazor.Color.Default))
            .AddClass("mud-svg-icon", !string.IsNullOrEmpty(Icon) && Icon.Trim().StartsWith("<"))
            .AddClass(Color.IsColor ? $"mud-{Color.AsColor.GetDescription()}-text":"", Color.IsColor && Color.AsColor != MudBlazor.Color.Default && Color.AsColor != MudBlazor.Color.Inherit)
            .AddClass($"mud-icon-size-{Size.GetDescription()}")
            .AddClass(Class)
            .Build();

    protected string Stylename =>
        new MudExStyleBuilder()
            .AddRaw(Style)            
            //.WithColor(Color, !Color.IsColor)
            .WithFill(Color, !Color.Is(MudBlazor.Color.Inherit) && !Color.Is(MudBlazor.Color.Default))
            .Build();

    /// <summary>
    /// Icon to be used can either be svg paths for font icons.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.Icon.Behavior)]
    public string? Icon { get; set; }

    /// <summary>
    /// Title of the icon used for accessibility.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.Icon.Behavior)]
    public string? Title { get; set; }

    /// <summary>
    /// The Size of the icon.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.Icon.Appearance)]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>
    /// The color of the component. It supports the theme colors.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.Icon.Appearance)]
    public MudExColor Color { get; set; } = MudBlazor.Color.Inherit;

    /// <summary>
    /// The viewbox size of an svg element.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.Icon.Behavior)]
    public string ViewBox { get; set; } = "0 0 24 24";

    /// <summary>
    /// Child content of component.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.Icon.Behavior)]
    public RenderFragment? ChildContent { get; set; }
}