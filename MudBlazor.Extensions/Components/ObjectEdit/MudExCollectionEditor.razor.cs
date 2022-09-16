using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;


namespace MudBlazor.Extensions.Components.ObjectEdit;

public partial class MudExCollectionEditor<T>
{
    [Inject] private IServiceProvider _serviceProvider { get; set; }
    [Parameter] public IStringLocalizer Localizer { get; set; }
    [Parameter] public string LocalizerPattern { get; set; } = "{0}";
    private IStringLocalizer<MudExCollectionEditor<T>> _fallbackLocalizer => _serviceProvider.GetService<IStringLocalizer<MudExCollectionEditor<T>>>();
    private IDialogService _mudDialogService => _serviceProvider.GetService<IDialogService>();
    protected IStringLocalizer LocalizerToUse => Localizer ?? _fallbackLocalizer;

    [Parameter] public ICollection<T> Items { get; set; }
    [Parameter] public EventCallback<ICollection<T>> ItemsChanged { get; set; }
    [Parameter] public Func<T, string> ItemToStringFunc { get; set; } = (item => item?.ToString() ?? string.Empty);
    [Parameter] public string Label { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public string HelperText { get; set; }
    [Parameter] public string ItemIcon { get; set; }
    [Parameter] public string RemoveIcon { get; set; } = Icons.Filled.Delete;
    [Parameter] public string RemoveAllIcon { get; set; } = Icons.Material.Filled.DeleteForever;
    [Parameter] public string AddIcon { get; set; } = Icons.Filled.Add;
    [Parameter] public string EditIcon { get; set; } = Icons.Material.Filled.Edit;
    [Parameter] public string ViewIcon { get; set; } = Icons.Material.Filled.Pageview;
    [Parameter] public Variant Variant { get; set; }
    [Parameter] public bool ShowClearButton { get; set; } = true;
    [Parameter] public bool AllowAdd { get; set; } = true;
    [Parameter] public bool AllowEditOrPreview { get; set; } = true;
    [Parameter] public bool AllowRemove { get; set; } = true;
    [Parameter] public DialogOptionsEx DialogOptions { get; set; }
    protected bool Primitive => MudExObjectEdit<T>.IsPrimitive();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Items ??= new List<T>();
    }

    public void Add(T item)
    {
        Items?.Add(item);
        RaiseChanged();
    }
    
    public void Remove(T item)
    {
        Items?.Remove(item);
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        ItemsChanged.InvokeAsync(Items);
        //ItemsChanged.InvokeAsync(new HashSet<T>(Items));
    }

    private void RemoveAll()
    {
        Items?.Clear();
        RaiseChanged();
    }

    public async Task Edit(T item)
    {
        DialogParameters parameters = new DialogParameters
        {
            { nameof(MudExObjectEditDialog<T>.DialogIcon), EditIcon },
            { nameof(MudExObjectEditDialog<T>.Localizer), Localizer }
        };
        var res = await _mudDialogService.EditObject<T>(item, LocalizerToUse.TryLocalize("Edit {0}", ItemNameRender(item)) , DialogOptions ?? DefaultOptions(), null, parameters);
        if (!res.Cancelled)
        {
            SetValue(item, res.Result);
        }
    }

    private void SetValue(T item, T newValue)
    {
        if (Primitive)
        {
            var array = Items.ToArray();
            array[Array.IndexOf(array, item)] = newValue;
            Items = array.ToList();
        }

        RaiseChanged();
    }

    public async Task View(T item)
    {
        DialogParameters parameters = new DialogParameters
        {
            { nameof(MudExObjectEditDialog<T>.Localizer), Localizer },
            {nameof(MudExObjectEditDialog<T>.DialogIcon), ViewIcon},
            {nameof(MudExObjectEditDialog<T>.ShowSaveButton), false},
            {nameof(MudExObjectEditDialog<T>.DefaultPropertyResetSettings), new PropertyResetSettings {AllowReset = false}},
            {nameof(MudExObjectEditDialog<T>.GlobalResetSettings), new GlobalResetSettings {AllowReset = false}},
            {nameof(MudExObjectEditDialog<T>.CancelButtonText), "Close"}
        };
        await _mudDialogService.ShowObject<T>(item, ItemNameRender(item), DialogOptions ?? DefaultOptions(), null, parameters);
    }

    public virtual string ItemNameRender(T item)
    {
        return ItemToStringFunc(item);
    }

    public async Task Add()
    {
        var item = typeof(T) == typeof(string) ? (T) (object) string.Empty : Primitive ? default : Activator.CreateInstance<T>();
        DialogParameters parameters = new DialogParameters
        {
            { nameof(MudExObjectEditDialog<T>.DialogIcon), AddIcon },
            { nameof(MudExObjectEditDialog<T>.Localizer), Localizer }
        };
        var res = await _mudDialogService.EditObject<T>(item, LocalizerToUse.TryLocalize("Add"), DialogOptions ?? DefaultOptions(), null, parameters);
        if (!res.Cancelled)
        {
            Add(res.Result);
        }
    }

    private DialogOptionsEx DefaultOptions()
    {
        return new DialogOptionsEx
        {
            CloseButton = true,
            Resizeable = true,
            DragMode = MudDialogDragMode.Simple
        };
    }
}