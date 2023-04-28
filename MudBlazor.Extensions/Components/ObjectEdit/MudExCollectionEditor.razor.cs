using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core.Extensions;


namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// Powerful component to edit a set of items and their properties.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class MudExCollectionEditor<T>
{
    [Parameter] public int? Height { get; set; }
    [Parameter] public int? MaxHeight { get; set; }
    [Parameter] public CssUnit SizeUnit { get; set; } = CssUnit.Pixels;
    [Parameter] public string Style { get; set; }

    [Parameter] public ICollection<T> Items { get; set; }
    [Parameter] public EventCallback<ICollection<T>> ItemsChanged { get; set; }
    [Parameter] public Func<T, string> ItemToStringFunc { get; set; } = (item => item?.ToString() ?? string.Empty);
    [Parameter] public string TextAdd { get; set; } = "Add";
    [Parameter] public string TextRemoveAll { get; set; } = "Remove All";
    [Parameter] public string TextEdit { get; set; } = "Edit {0}";

    [Parameter] public string Label { get; set; }
    [Parameter] public bool Virtualize { get; set; } = true;
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
    [Parameter] public Color ToolbarColor { get; set; } = Color.Surface;
    [Parameter] public Position ToolbarPosition { get; set; } = Position.Bottom;
    [Parameter] public bool StickToolbar { get; set; } = true;
    [Parameter] public string StyleToolbar { get; set; }
    [Parameter] public string ClassToolbar { get; set; }
    [Parameter] public string Class { get; set; }
    [Parameter] public Color ToolbarButtonColor { get; set; } = Color.Inherit;
    [Parameter] public PropertyFilterMode FilterMode { get; set; } = PropertyFilterMode.Toggleable;
    [Parameter] public string Filter { get; set; }
    [Parameter] public string SearchIcon { get; set; } = Icons.Material.Outlined.Search;
    protected bool Primitive => MudExObjectEdit<T>.IsPrimitive();

    private bool IsInFilter(T item)
        => string.IsNullOrWhiteSpace(Filter)
           || ItemNameRender(item).ToLower().Contains(Filter.ToLower())
           || item?.GetProperties().Any(p => p.GetValue(item)?.ToString()?.ToLower().Contains(Filter.ToLower()) ?? false) == true;


    private string GetMudGridStyle()
    {
        // Display block is required to have virtualization working
        return !Virtualize ? string.Empty : MudExCss.GenerateCssString(new
        {
            Display = "block"
        });
    }

    private string GetToolbarStyle()
    {
        return MudExCss.GenerateCssString(new
        {
            ZIndex = 1,
            BackgroundColor = ToolbarColor,
            Position = StickToolbar ? "sticky" : null,
            Bottom = ToolbarPosition == Position.Bottom ? 0 as int? : null,
            Top = ToolbarPosition == Position.Top ? 0 as int? : null
        }, StyleToolbar);
    }

    private string GetStyle()
    {
        return MudExCss.GenerateCssString(new
        {
            Height,
            MaxHeight,
        }, SizeUnit, Style);
    }

    protected override void OnParametersSet()
    {
        if (ToolbarPosition != Position.Bottom && ToolbarPosition != Position.Top)
            throw new ArgumentException("ToolbarPosition must be either 'Top' or 'Bottom'.");
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
        var res = await DialogService.EditObject<T>(item, TryLocalize(TextEdit, ItemNameRender(item)), DialogOptions ?? DefaultOptions(), null, parameters);
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
        await DialogService.ShowObject<T>(item, ItemNameRender(item), DialogOptions ?? DefaultOptions(), null, parameters);
    }

    public virtual string ItemNameRender(T item)
    {
        return ItemToStringFunc(item);
    }

    public async Task Add()
    {
        var item = typeof(T) == typeof(string) ? (T)(object)string.Empty : Primitive ? default : Activator.CreateInstance<T>();
        DialogParameters parameters = new DialogParameters
        {
            { nameof(MudExObjectEditDialog<T>.DialogIcon), AddIcon },
            { nameof(MudExObjectEditDialog<T>.Localizer), Localizer }
        };
        var res = await DialogService.EditObject<T>(item, TryLocalize(TextAdd), DialogOptions ?? DefaultOptions(), null, parameters);
        if (!res.Cancelled)
        {
            Add(res.Result);
        }
    }

    private DialogOptionsEx DefaultOptions()
    {
        return DialogOptionsEx.DefaultDialogOptions ?? new DialogOptionsEx
        {
            CloseButton = true,
            Resizeable = true,
            DragMode = MudDialogDragMode.Simple
        };
    }
}