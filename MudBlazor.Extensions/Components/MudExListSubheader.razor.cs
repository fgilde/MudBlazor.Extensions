using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

public partial class MudExListSubheader<T>
{
    protected string Classname =>
        new MudExCssBuilder("mud-ex-list-subheader")
            .AddClass("mud-ex-list-subheader-gutters", !DisableGutters)
            .AddClass("mud-ex-list-subheader-inset", Inset)
            .AddClass("mud-ex-list-subheader-secondary-background", SecondaryBackground)
            .AddClass("mud-ex-list-subheader-sticky", Sticky)
            .AddClass("mud-ex-list-subheader-sticky-dense", Sticky && (MudListExtended != null && MudListExtended.DisablePadding))
            .AddClass(Class)
            .Build();

    protected string Stylename =>
        new MudExStyleBuilder()
            .AddRaw(Style)
            .WithTop(StickyTop, Sticky)            
            .Build();

    [CascadingParameter] protected MudExList<T> MudListExtended { get; set; }

    /// <summary>
    /// The child render fragment.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Disables the left and right spaces.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool DisableGutters { get; set; }

    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool Inset { get; set; }

    /// <summary>
    /// If true, subheader behaves sticky and remains on top until other subheader comes to top.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool Sticky { get; set; }

    /// <summary>
    /// The top position of sticky header
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public MudExSize<double> StickyTop { get; set; } = -8;

    /// <summary>
    /// If true, subheader has darken background.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool SecondaryBackground { get; set; }
}