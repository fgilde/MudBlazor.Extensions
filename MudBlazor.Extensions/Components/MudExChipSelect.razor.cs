using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A component that allows the user to select a value from a list of possible values and display it as Chips or default Combobox.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class MudExChipSelect<T>
{
    [Parameter] public string LocalizerPattern { get; set; } = "{0}";
    [Parameter] public string Class { get; set; }
    [Parameter] public Variant Variant { get; set; }
    [Parameter] public bool AutoFocusFilter { get; set; }
    [Parameter] public Adornment Adornment { get; set; } = Adornment.End;
    [Parameter] public bool ReadOnly { get; set; }
    //[Parameter] public Expression<Func<T>>? For { get; set; }
    [Parameter] public bool RenderValidationComponent { get; set; } = true;
    [Parameter] public Expression<Func<IEnumerable<T>>>? For { get; set; }
    [Parameter] public bool DisableUnderLine { get; set; }
    [Parameter] public bool DisableUnderLineForValidationComponent { get; set; } = true;
    [Parameter] public string StyleForValidationComponent { get; set; } = "margin-top: -38px; pointer-events: none;";
    [Parameter] public virtual Color ChipColor { get; set; } = Color.Primary;
    [Parameter] public virtual Variant ChipVariant { get; set; } = Variant.Filled;
    [Parameter] public virtual ViewMode ViewMode { get; set; } = ViewMode.ChipsOnly;
    [Parameter] public virtual string Label { get; set; }
    [Parameter] public virtual string HelperText { get; set; }
    [Parameter] public virtual bool FilterEnabled { get; set; } = true;
    [Parameter] public virtual bool Clearable { get; set; } = true;
    [Parameter] public virtual bool MultiSelect { get; set; } = true;
    [Parameter] public virtual bool UseCustomItemRenderInSelectionPopover { get; set; } = false;
    [Parameter] public RenderFragment<T>? ItemTemplate { get; set; }

    [Parameter] public IList<T> AvailableItems { get; set; }
    [Parameter] public Func<CancellationToken, Task<IList<T>>> AvailableItemsLoadFunc { get; set; }
    [Parameter] public Func<T, string> ItemToStringFunc { get; set; } = (item => item?.ToString() ?? string.Empty);  
    [Parameter] public string AdornmentIcon { get; set; } = Icons.Material.Filled.ArrowDropDown;
    [Parameter] public T Value { get; set; }
    [Parameter] public bool UpdateItemsOnStateChange { get; set; }

    [Parameter] public EventCallback<IEnumerable<T>> SelectedChanged { get; set; }
    [Parameter] public EventCallback<T> ValueChanged { get; set; }

    [Parameter]
    public IEnumerable<T> Selected
    {
        get => _selected ??= new HashSet<T>();
        set
        {
            var set = (value ?? new HashSet<T>()).ToList();
            if (Selected.Count() != set.Count || !_selected.All(x => set.Contains(x)))
            {
                _selected = new HashSet<T>(set);
                OnBeforeSelectedChanged(_selected);
                RaiseChanged();
            }
        }
    }

    protected virtual void OnBeforeSelectedChanged(IEnumerable<T> selected)
    {}

    private void RaiseChanged()
    {
        SelectedChanged.InvokeAsync(new HashSet<T>(Selected));
        ValueChanged.InvokeAsync(Value);
    }

    [Parameter]
    public string Filter
    {
        get => _filter;
        set
        {
            if (_filter != value)
            {
                _filter = value;
                StateHasChanged();
            }
        }
    }

    private IEnumerable<T> _selected;
    private string _filter;
    private string _cssName => $"chip-select-{Enum.GetName(ViewMode)?.ToLower() ?? "none"} {(Selected?.Any() == true ? "with-items" : "empty")}";


    protected override async Task OnParametersSetAsync()
    {
        if (AvailableItems == null || !AvailableItems.Any() || UpdateItemsOnStateChange)
            AvailableItems = await GetAvailableItemsAsync();
        await base.OnParametersSetAsync();
    }

    private void Remove(MudChip chip, T item)
    {
        ((HashSet<T>)_selected).Remove(item);
        RaiseChanged();
    }

    public virtual string ItemNameRender(T item)
    {
        var res = ItemToStringFunc(item);
        if (!string.IsNullOrWhiteSpace(res) && LocalizerToUse != null && !string.IsNullOrWhiteSpace(LocalizerPattern))
        {
            return LocalizerToUse[string.Format(LocalizerPattern, item)];
        }

        return res;
    }

    protected virtual string MultiSelectionTextFunc(List<string> arg) 
        => string.Join(", ", Selected.Where(a => a != null).Select(r => ItemNameRender(r)?.ToUpper(true)));
    
    protected virtual Task<IList<T>> GetAvailableItemsAsync(CancellationToken cancellation = default) 
        => AvailableItemsLoadFunc != null ? AvailableItemsLoadFunc(cancellation) : Task.FromResult(new List<T>(0) as IList<T>);

    private void OnClose()
    {
        Filter = string.Empty;
    }
}

public enum ViewMode
{
    ChipsOnly,
    ChipsAdditionalAbove,
    ChipsAdditionalBelow,
    NoChips
}