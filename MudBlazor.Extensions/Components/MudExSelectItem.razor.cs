using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExSelectItem is a select item for MudExSelect.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class MudExSelectItem<T> : IDisposable, IMudExComponent
{
    // Fields
    private IMudExSelect _parent;
    private IMudExShadowSelect _shadowParent;

    // Constants
    internal readonly string ItemId = "selectItem_" + Guid.NewGuid().ToString()[..8];

    [CascadingParameter]
    internal IMudExSelect IMudExSelect
    {
        get => _parent;
        set => SetParent(value);
    }

    [CascadingParameter]
    internal IMudExShadowSelect MudExShadowSelect
    {
        get => _shadowParent;
        set => SetShadowParent(value);
    }

    [CascadingParameter(Name = "HideContent")]
    internal bool HideContent { get; set; }

    // Other properties
    internal MudExSelect<T> MudExSelect => _parent as MudExSelect<T>;
    
    /// <summary>
    /// List item.
    /// </summary>
    public MudExListItem<T> ListItem { get; set; }

    /// <summary>
    /// True when item is only functional.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public bool IsFunctional { get; set; }

    /// <summary>
    /// Text for the item.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.List.Behavior)]
    public string Text { get; set; }

    /// <summary>
    /// Item value.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public T Value { get; set; }

    /// <summary>
    /// Is multi selection enabled.
    /// </summary>
    protected bool MultiSelection => MudExSelect?.MultiSelection ?? false;

    internal bool IsSelected { get; set; }

    /// <summary>
    /// Display string.
    /// </summary>
    protected string DisplayString
    {
        get
        {
            var converter = MudExSelect?.Converter;
            if (converter == null)
                return string.IsNullOrEmpty(Text) ? Value?.ToString() : Text;
            return !string.IsNullOrEmpty(Text) ? Text : converter.Set(Value);
        }
    }

    // Methods
    private void SetParent(IMudExSelect value)
    {
        _parent = value;
        if (_parent == null) return;

        _parent.CheckGenericTypeMatch(this);

        var isSelected = MudExSelect?.Add(this) ?? false;

        if (_parent.MultiSelection && MudExSelect is not null)
        {
            MudExSelect.SelectionChangedFromOutside += OnUpdateSelectionStateFromOutside;
            InvokeAsync(() => OnUpdateSelectionStateFromOutside(MudExSelect.SelectedValues));
        }
        else
        {
            IsSelected = isSelected;
        }
    }

    private void SetShadowParent(IMudExShadowSelect value)
    {
        _shadowParent = value;
        (_shadowParent as MudExSelect<T>)?.RegisterShadowItem(this);
    }

    private void OnUpdateSelectionStateFromOutside(IEnumerable<T> selection)
    {
        if (selection == null) return;

        var oldIsSelected = IsSelected;
        IsSelected = selection.Contains(Value);
        if (oldIsSelected != IsSelected)
            InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    protected async void HandleOnClick()
    {        
        await MudExSelect?.SelectOption(Value);

        await InvokeAsync(StateHasChanged);

        if (MudExSelect == null) return;

        if (!MultiSelection)
            await MudExSelect.CloseMenu();
        else
            await MudExSelect.FocusAsync();
    }

    /// <summary>
    /// Returns the disabled status.
    /// </summary>
    protected bool GetDisabledStatus() => MudExSelect?.ItemDisabledFunc?.Invoke(Value) ?? Disabled;

    /// <summary>
    /// Disposes the component.
    /// </summary>
    public void Dispose()
    {
        try
        {
            MudExSelect?.Remove(this);
            (_shadowParent as MudExSelect<T>)?.UnregisterShadowItem(this);
        }
        catch
        {
            /* Gracefully handle exceptions during disposal */
        }
    }
}