using Microsoft.AspNetCore.Components;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Components;

public partial class MudExSelectItemGroup<T>
{

    //private IMudSelect _parent;
    internal string ItemId { get; } = "_" + Guid.NewGuid().ToString().Substring(0, 8);

    /// <summary>
    /// A user-defined option that can be selected
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.FormComponent.Behavior)]
    public T Value { get; set; }

    /// <summary>
    /// A user-defined option that can be selected
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.FormComponent.Behavior)]
    public string Text { get; set; }

    /// <summary>
    /// A user-defined option that can be selected
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.FormComponent.Behavior)]
    public bool Nested { get; set; }

    /// <summary>
    /// Sets the group's expanded state on popover opening. Works only if nested is true.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.FormComponent.Behavior)]
    public bool InitiallyExpanded { get; set; }

    /// <summary>
    /// Sticky header for item group. Works only with nested is false.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.FormComponent.Behavior)]
    public bool Sticky { get; set; }

    [CascadingParameter]
    internal MudExList<T> MudExList { get; set; }

    /// <summary>
    /// Select items with HideContent==true are only there to register their RenderFragment with the select but
    /// wont render and have no other purpose!
    /// </summary>
    [CascadingParameter(Name = "HideContent")]
    internal bool HideContent { get; set; }

    private void HandleExpandedChanged(bool isExpanded)
    {
        if (isExpanded)
        {
            MudExList?.UpdateSelectedStyles();
        }
    }

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