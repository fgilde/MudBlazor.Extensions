using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExListSubheader is a sub header for MudExList.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class MudExListSubheader<T>
{
    /// <summary>
    /// Classname for the component.
    /// </summary>
    protected string Classname =>
        new MudExCssBuilder("mud-ex-list-subheader")
            .AddClass("mud-ex-list-subheader-gutters", !DisableGutters)
            .AddClass("mud-ex-list-subheader-inset", Inset)
            .AddClass("mud-ex-list-subheader-secondary-background", SecondaryBackground)
            .AddClass("mud-ex-list-subheader-sticky", Sticky)
            .AddClass("mud-ex-list-subheader-sticky-dense", Sticky && MudExList is { DisablePadding: true })
            .AddClass(Class)
            .Build();

    /// <summary>
    /// Style for the component.
    /// </summary>
    protected string Stylename =>
        new MudExStyleBuilder()
            .AddRaw(Style)
            .WithTop(StickyTop, Sticky)            
            .Build();

    /// <summary>
    /// MudExList instance.
    /// </summary>
    [CascadingParameter] protected MudExList<T> MudExList { get; set; }

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

    /// <summary>
    /// If true, the List Sub header will be indented.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool Inset { get; set; }

    /// <summary>
    /// If true, sub header behaves sticky and remains on top until other sub header comes to top.
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
    /// If true, sub header has darker background.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool SecondaryBackground { get; set; }
}