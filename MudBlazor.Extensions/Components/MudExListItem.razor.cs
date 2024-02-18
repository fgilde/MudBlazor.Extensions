using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;
using System.Windows.Input;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// List item for MudExList.
/// </summary>
public partial class MudExListItem<T>
{

    #region Parameters, Fields, Injected Services

    /// <summary>
    /// UriHelper for navigation.
    /// </summary>
    [Inject] protected NavigationManager UriHelper { get; set; }

    /// <summary>
    /// Parent list if any.
    /// </summary>
    [CascadingParameter] protected MudExList<T> MudExList { get; set; }
    
    /// <summary>
    /// Parent list item if any.
    /// </summary>
    [CascadingParameter] protected internal MudExListItem<T> ParentListItem { get; set; }

    /// <summary>
    /// Classname for the component.
    /// </summary>
    protected string Classname => GetCls();

    private string GetCls()
    {
        var builder = new MudExCssBuilder("mud-ex-list-item")
            .AddClass("mud-ex-list-item-dense", Dense == true || MudExList?.Dense == true)
            .AddClass("mud-ex-list-item-gutters", !DisableGutters && !(MudExList?.DisableGutters == true))
            .AddClass("mud-ex-list-item-clickable", MudExList?.Clickable == true)
            .AddClass("mud-ripple", MudExList?.Clickable == true && !DisableRipple && !Disabled && !IsFunctional);
        if (MudExList?.Color.IsColor == true) 
        {             
            builder.AddClass($"mud-selected-item mud-{MudExList?.Color.AsColor.GetDescription()}-text mud-{MudExList?.Color.AsColor.GetDescription()}-hover", MudExList != null && _selected && !Disabled && NestedList == null && !MudExList.DisableSelectedItemStyle);
        }
        builder.AddClass("mud-ex-list-item-hilight", _active && !Disabled && NestedList == null && !IsFunctional)
        .AddClass("mud-ex-list-item-disabled", Disabled)
        .AddClass("mud-ex-list-item-nested-background", MudExList != null && MudExList.SecondaryBackgroundForNestedItemHeader && NestedList != null)
        .AddClass("mud-list-item-functional", IsFunctional)
        .AddClass(Class);
        
        return builder.Build();
    }

    /// <summary>
    /// Classname for the multiselect component.
    /// </summary>
    protected string MultiSelectClassName =>
    new CssBuilder()
        .AddClass("mud-ex-list-item-multiselect")
        .AddClass("mud-ex-list-item-multiselect-checkbox", MudExList?.MultiSelectionComponent == MultiSelectionComponent.CheckBox || OverrideMultiSelectionComponent == MultiSelectionComponent.CheckBox)
        .Build();

    /// <summary>
    /// Style for the icon.
    /// </summary>
    protected string IconStyleName =>
        new MudExStyleBuilder()
            .WithColor(IconColor, !IconColor.IsColor)
            .Build();


    /// <summary>
    /// Id of the list item.
    /// </summary>
    protected internal string ItemId { get; } = "listitem_" + Guid.NewGuid().ToString().Substring(0, 8);

    /// <summary>
    /// If true the item template will be ignored.
    /// </summary>
    [Parameter] 
    public bool IgnoreItemTemplate { get; set; }

    /// <summary>
    /// Functional items does not hold values. If a value set on Functional item, it ignores by the MudList. They can not count on Items list (they count on AllItems), cannot be subject of keyboard navigation and selection.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public bool IsFunctional { get; set; }

    /// <summary>
    /// The text to display
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public string Text { get; set; }

    /// <summary>
    /// The secondary text to display
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public string SecondaryText { get; set; }

    /// <summary>
    /// Value of the list item.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Selecting)]
    public T Value { get; set; }

    /// <summary>
    /// Avatar to use if set.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public string Avatar { get; set; }

    /// <summary>
    /// Avatar CSS Class to apply if Avatar is set.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public string AvatarClass { get; set; }

    /// <summary>
    /// Link to a URL when clicked.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.ClickAction)]
    public string Href { get; set; }

    /// <summary>
    /// If true, force browser to redirect outside component router-space.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.ClickAction)]
    public bool ForceLoad { get; set; }

    private bool _disabled;
    /// <summary>
    /// If true, will disable the list item if it has onclick.
    /// The value can be overridden by the parent list.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public bool Disabled
    {
        get => _disabled || (MudExList?.Disabled ?? false);
        set => _disabled = value;
    }

    /// <summary>
    /// If true, the left and right padding is removed.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool DisableGutters { get; set; }

    /// <summary>
    /// If true, disables ripple effect.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool DisableRipple { get; set; }

    /// <summary>
    /// Overriden component for multi selection. Keep it null to have default one that MudList has.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.ClickAction)]
    public MultiSelectionComponent? OverrideMultiSelectionComponent { get; set; } = null;

    /// <summary>
    /// Icon to use if set.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public string Icon { get; set; }

    /// <summary>
    /// The color of the icon.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public MudExColor IconColor { get; set; } = Color.Inherit;

    /// <summary>
    /// Sets the Icon Size.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public Size IconSize { get; set; } = Size.Medium;

    /// <summary>
    /// The color of the adornment if used. It supports the theme colors.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Expanding)]
    public Color AdornmentColor { get; set; } = Color.Default;

    /// <summary>
    /// Custom expand less icon.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Expanding)]
    public string ExpandLessIcon { get; set; } = Icons.Material.Filled.ExpandLess;

    /// <summary>
    /// Custom expand more icon.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Expanding)]
    public string ExpandMoreIcon { get; set; } = Icons.Material.Filled.ExpandMore;

    /// <summary>
    /// If true, the List Sub header will be indented.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool Inset { get; set; }

    /// <summary>
    /// If true, stop propagation on click. Default is true.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool OnClickStopPropagation { get; set; } = true;

    private bool? _dense;
    /// <summary>
    /// If true, compact vertical padding will be used.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Appearance)]
    public bool? Dense
    {
        get => _dense;
        set
        {
            if (_dense == value)
            {
                return;
            }
            _dense = value;
            OnListParametersChanged();
        }
    }

    /// <summary>
    /// Command parameter.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.ClickAction)]
    public object CommandParameter { get; set; }

    /// <summary>
    /// Command executed when the user clicks on an element.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.ClickAction)]
    public ICommand Command { get; set; }

    /// <summary>
    /// Prevent default behavior when click on MudSelectItem. Default behavior is selecting the item and style adjustments.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public bool OnClickHandlerPreventDefault { get; set; }

    /// <summary>
    /// Display content of this list item. If set, overrides Text.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Add child list items here to create a nested list.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public RenderFragment NestedList { get; set; }

    /// <summary>
    /// List click event.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// If true, expands the nested list on first display.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Expanding)]
    public bool InitiallyExpanded { get; set; }

    private bool _expanded;
    /// <summary>
    /// Expand or collapse nested list. Two-way bind able. Note: if you directly set this to
    /// true or false (instead of using two-way binding) it will force the nested list's expansion state.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Expanding)]
    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value)
                return;
            _expanded = value;
            _ = ExpandedChanged.InvokeAsync(value);
        }
    }

    /// <summary>
    /// Called when expanded state change.
    /// </summary>
    [Parameter]
    public EventCallback<bool> ExpandedChanged { get; set; }

    #endregion


    #region Lifecycle Methods (& Dispose)

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _expanded = InitiallyExpanded;
        if (MudExList != null)
        {
            MudExList.Register(this);
            OnListParametersChanged();
            MudExList.ParametersChanged += OnListParametersChanged;
        }
    }

    /// <summary>
    /// Dispose the component.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (MudExList == null)
                return;
            MudExList.ParametersChanged -= OnListParametersChanged;
            MudExList.Unregister(this);
        }
        catch (Exception) { /*ignore*/ }
    }

    #endregion


    #region Select & Active

    private bool _selected;
    private bool _active;

    /// <summary>
    /// Selected state of the option. Readonly. Use SetSelected for selecting.
    /// </summary>
    public bool IsSelected => _selected;

    internal bool IsActive => _active;

    /// <summary>
    /// Set the selected state of the option.
    /// </summary>
    public void SetSelected(bool selected, bool forceRender = true, bool returnIfDisabled = false)
    {
        if (returnIfDisabled && Disabled)
        {
            return;
        }
        if (_selected == selected)
            return;
        _selected = selected;
        if (forceRender)
        {
            StateHasChanged();
        }
    }

    internal void SetActive(bool active, bool forceRender = true)
    {
        if (Disabled)
            return;
        if (_active == active)
            return;
        _active = active;
        if (forceRender)
        {
            ForceRender();
        }
    }

    #endregion


    #region Other (ClickHandler etc.)

    /// <summary>
    /// Force render the component.
    /// </summary>
    public void ForceRender()
    {
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handler for click event.
    /// </summary>
    protected void OnClickHandler(MouseEventArgs ev)
    {
        if (Disabled)
            return;

        if (OnClickHandlerPreventDefault)
        {
            OnClick.InvokeAsync(ev).AndForget();
            return;
        }

        if (NestedList != null)
        {
            Expanded = !Expanded;
        }
        else if (Href != null)
        {
            MudExList?.SetSelectedValue(this);
            OnClick.InvokeAsync(ev).AndForget();
            UriHelper.NavigateTo(Href, ForceLoad);
        }
        else if (MudExList?.Clickable == true || MudExList?.MultiSelection == true)
        {
            MudExList?.SetSelectedValue(this);
            OnClick.InvokeAsync(ev).AndForget();
        }
    }

    /// <summary>
    /// Only click event.
    /// </summary>
    protected void OnlyOnClick(MouseEventArgs ev)
    {
        OnClick.InvokeAsync(ev).AndForget();
    }

    private Typo _textTypo;
    
    /// <summary>
    /// Called when list parameters changed.
    /// </summary>
    protected void OnListParametersChanged()
    {
        if ((Dense ?? MudExList?.Dense) ?? false)
        {
            _textTypo = Typo.body2;
        }
        else
        {
            _textTypo = Typo.body1;
        }
        StateHasChanged();
    }

    #endregion

}