using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

public partial class MudExChipSelect<T>
{
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public Expression<Func<T>>? For { get; set; }
    [Parameter] public bool DisableUnderLine { get; set; }
    [Parameter] public virtual Color ChipColor { get; set; } = Color.Primary;
    [Parameter] public virtual Variant ChipVariant { get; set; } = Variant.Filled;
    [Parameter] public virtual ViewMode ViewMode { get; set; } = ViewMode.ChipsOnly;
    [Parameter] public virtual string Label { get; set; }
    [Parameter] public virtual string HelperText { get; set; }
    [Parameter] public virtual bool FilterEnabled { get; set; } = true;
    [Parameter] public virtual bool MultiSelect { get; set; } = true;
    [Parameter] public virtual bool UseCustomItemRenderInSelectionPopover { get; set; } = false;
    [Parameter] public RenderFragment<T>? ItemTemplate { get; set; }

    [Parameter] public IList<T> AvailableItems { get; set; }
    [Parameter] public Func<CancellationToken, Task<IList<T>>> AvailableItemsLoadFunc { get; set; }
    [Parameter] public Func<T, string> ItemToStringFunc { get; set; } = (item => item?.ToString() ?? string.Empty);  
    [Parameter] public string AdornmentIcon { get; set; } = Icons.Material.Filled.ArrowDropDown;
    [Parameter] public T Value { get; set; }

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
                RaiseChanged();
            }
        }
    }

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

    private IEnumerable<T> _available;
    private IEnumerable<T> _selected;
    private string _filter;
    private string _cssName => $"chip-select-{Enum.GetName(ViewMode)?.ToLower() ?? "none"}";


    protected override async Task OnParametersSetAsync()
    {
        _available = AvailableItems ?? await GetAvailableItemsAsync();
        await base.OnParametersSetAsync();
    }

    private void Remove(MudChip chip, T item)
    {
        ((HashSet<T>)_selected).Remove(item);
        RaiseChanged();
    }

    public virtual string ItemNameRender(T item) => ItemToStringFunc(item);
    
    protected virtual string MultiSelectionTextFunc(List<string> arg) 
        => string.Join(", ", Selected.Where(a => a != null).Select(r => ItemNameRender(r)?.ToUpper(true)));
    
    protected virtual Task<IList<T>> GetAvailableItemsAsync(CancellationToken cancellation = default) 
        => AvailableItemsLoadFunc != null ? AvailableItemsLoadFunc(cancellation) : Task.FromResult(new List<T>(0) as IList<T>);
}

public enum ViewMode
{
    ChipsOnly,
    ChipsAdditionalAbove,
    ChipsAdditionalBelow,
    NoChips
}