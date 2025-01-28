using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Group of items in a select list
/// </summary>
public partial class MudExSelectItemGroup<T>
{

    //private IMudSelect _parent;
    internal string ItemId { get; } = "_" + Guid.NewGuid().ToString().Substring(0, 8);

    /// <summary>
    /// Style for the component.
    /// </summary>
    protected string Stylename =>
        new MudExStyleBuilder()
            .AddRaw(Style)
            .WithPosition(Core.Css.Position.Sticky, Sticky)
            .WithTop(StickyTop, Sticky)
            .WithZIndex(1, Sticky)
            .WithBackgroundColor("var(--mud-palette-background)")
            .Build();


    /// <summary>
    /// A user-defined option that can be selected
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public string Text { get; set; }

    /// <summary>
    /// Set to true to use a expansion panel to nest items
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public bool Nested { get; set; }

    /// <summary>
    /// Sets the group's expanded state on popover opening. Works only if nested is true.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public bool InitiallyExpanded { get; set; }

    /// <summary>
    /// Sticky header for item group. Works only with nested is false.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public bool Sticky { get; set; }

    /// <summary>
    /// The top position of sticky header
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public MudExSize<double> StickyTop { get; set; } = -8;

    [CascadingParameter]
    internal MudExList<T> MudExList { get; set; }

    private bool ShouldHideContent() => this.ExposeField<bool>("HideContent");


    private void HandleExpandedChanged(bool isExpanded)
    {
        if (isExpanded)
        {
            MudExList?.UpdateSelectedStyles();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            //This line is for nested and initially expanded items. Still doesn't work for multiselection
            MudExList?.UpdateSelectedStyles(false);
        }
    }

}