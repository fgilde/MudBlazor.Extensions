using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

public partial class MudExGroupBox
{
    /// <summary>
    /// Child content of the group box
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Text to display as group Header
    /// </summary>
    [Parameter] public string Text { get; set; }

    /// <summary>
    /// If true, the group box will have a square border
    /// </summary>
    [Parameter] public bool Square { get; set; }

    /// <summary>
    /// The color of the border
    /// </summary>
    [Parameter] public MudExColor BorderColor { get; set; } = "var(--mud-palette-lines-default);";

    private string FieldSetStyle() => MudExStyleBuilder.Default
            .WithMargin(8)
            .WithPadding(8)
            .WithBorderRadius("var(--mud-default-borderradius)", !Square)
            .WithBorder(1, BorderStyle.Solid, BorderColor, BorderColor.IsSet())
            .AddRaw(Style)
            .Style;

    private string LegendStyle() => MudExStyleBuilder.Default
            .WithPadding(2)
            .Style;
}