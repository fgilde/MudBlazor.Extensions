using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Utilities;
namespace MudBlazor.Extensions.Components;

public partial class MudExSelectItem<T>: IDisposable
{
    private String GetCssClasses() => new CssBuilder()
       .AddClass(Class)
       .Build();

    private IMudExSelect _parent;
    internal MudExSelect<T> MudExSelect => (MudExSelect<T>)IMudExSelect;
    public MudExListItem<T> ListItem { get; set; }
    internal string ItemId { get; } = "selectItem_" + Guid.NewGuid().ToString().Substring(0, 8);

    /// <summary>
    /// The parent select component
    /// </summary>
    [CascadingParameter]
    internal IMudExSelect IMudExSelect
    {
        get => _parent;
        set
        {
            _parent = value;
            if (_parent == null)
                return;
            _parent.CheckGenericTypeMatch(this);
            if (MudExSelect == null)
                return;
            bool isSelected = MudExSelect.Add(this);
            if (_parent.MultiSelection)
            {
                MudExSelect.SelectionChangedFromOutside += OnUpdateSelectionStateFromOutside;
                InvokeAsync(() => OnUpdateSelectionStateFromOutside(MudExSelect.SelectedValues));
            }
            else
            {
                IsSelected = isSelected;
            }
        }
    }

    /// <summary>
    /// Functional items does not hold values. If a value set on Functional item, it ignores by the MudSelect. They cannot be subject of keyboard navigation and selection.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.List.Behavior)]
    public bool IsFunctional { get; set; }

    /// <summary>
    /// The text to display
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.List.Behavior)]
    public string Text { get; set; }

    private IMudShadowSelectExtended _shadowParent;
    [CascadingParameter]
    internal IMudShadowSelectExtended IMudShadowSelectExtended
    {
        get => _shadowParent;
        set
        {
            _shadowParent = value;
            ((MudExSelect<T>)_shadowParent)?.RegisterShadowItem(this);
        }
    }

    /// <summary>
    /// Select items with HideContent==true are only there to register their RenderFragment with the select but
    /// wont render and have no other purpose!
    /// </summary>
    [CascadingParameter(Name = "HideContent")]
    internal bool HideContent { get; set; }

    private void OnUpdateSelectionStateFromOutside(IEnumerable<T> selection)
    {
        if (selection == null)
            return;
        var old_is_selected = IsSelected;
        IsSelected = selection.Contains(Value);
        if (old_is_selected != IsSelected)
            InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// A user-defined option that can be selected
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.FormComponent.Behavior)]
    public T Value { get; set; }

    /// <summary>
    /// Mirrors the MultiSelection status of the parent select
    /// </summary>
    protected bool MultiSelection
    {
        get
        {
            if (MudExSelect == null)
                return false;
            return MudExSelect.MultiSelection;
        }
    }

    private bool _isSelected;
    internal bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
        }
    }

    protected string DisplayString
    {
        get
        {
            var converter = MudExSelect?.Converter;
            if (converter == null)
                return $"{(string.IsNullOrEmpty(Text) ? Value : Text)}";
            return !string.IsNullOrEmpty(Text) ? Text : converter.Set(Value);
        }
    }

    protected async void HandleOnClick()
    {
        // Selection works on list. We arrange only popover state and some minor arrangements on click.
        await MudExSelect?.SelectOption(Value);
        
        await InvokeAsync(StateHasChanged);
        if (MudExSelect != null)
        {
            if (!MultiSelection)
            {
                await MudExSelect.CloseMenu();
            }
            else
            {
                await MudExSelect.FocusAsync();
            }
        }
        await OnClick.InvokeAsync();
    }

    protected bool GetDisabledStatus()
    {
        if (MudExSelect?.ItemDisabledFunc != null)
        {
            return MudExSelect.ItemDisabledFunc(Value);
        }
        return Disabled;
    }

    public void Dispose()
    {
        try
        {
            MudExSelect?.Remove(this);
            ((MudExSelect<T>)_shadowParent)?.UnregisterShadowItem(this);
        }
        catch (Exception) { }
    }

}