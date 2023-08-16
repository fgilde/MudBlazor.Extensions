using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

public partial class MudExSelectItem<T> : IDisposable
{
    // Fields
    private IMudExSelect _parent;
    private IMudShadowSelectExtended _shadowParent;

    // Constants
    internal readonly string ItemId = "selectItem_" + Guid.NewGuid().ToString().Substring(0, 8);

    // Properties with CascadingParameter
    [CascadingParameter]
    internal IMudExSelect IMudExSelect
    {
        get => _parent;
        set => SetParent(value);
    }

    [CascadingParameter]
    internal IMudShadowSelectExtended IMudShadowSelectExtended
    {
        get => _shadowParent;
        set => SetShadowParent(value);
    }

    [CascadingParameter(Name = "HideContent")]
    internal bool HideContent { get; set; }

    // Other properties
    internal MudExSelect<T> MudExSelect => _parent as MudExSelect<T>;
    public MudExListItem<T> ListItem { get; set; }

    [Parameter]
    [Category(CategoryTypes.List.Behavior)]
    public bool IsFunctional { get; set; }

    [Parameter]
    [Category(CategoryTypes.List.Behavior)]
    public string Text { get; set; }

    [Parameter]
    [Category(CategoryTypes.FormComponent.Behavior)]
    public T Value { get; set; }

    protected bool MultiSelection => MudExSelect?.MultiSelection ?? false;

    internal bool IsSelected { get; set; }

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

    private void SetShadowParent(IMudShadowSelectExtended value)
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

    protected bool GetDisabledStatus() => MudExSelect?.ItemDisabledFunc?.Invoke(Value) ?? Disabled;

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