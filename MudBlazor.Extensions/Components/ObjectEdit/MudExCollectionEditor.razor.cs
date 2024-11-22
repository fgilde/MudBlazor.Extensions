using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
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
    /// <summary>
    /// Gets or Sets the Localizer Pattern.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string LocalizerPattern { get; set; } = "{0}";

    /// <summary>
    /// Gets or sets the height of the collection editor.
    /// </summary>
    [Parameter]
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the maximum height of the collection editor.
    /// </summary>
    [Parameter]
    public int? MaxHeight { get; set; }

    /// <summary>
    /// Gets or sets the size unit for the collection editor.
    /// </summary>
    [Parameter]
    public CssUnit SizeUnit { get; set; } = CssUnit.Pixels;

    /// <summary>
    /// Gets or sets the collection of items to be edited.
    /// </summary>
    [Parameter]
    public ICollection<T> Items { get; set; }

    /// <summary>
    /// Gets or sets the event callback for when items are changed.
    /// </summary>
    [Parameter]
    public EventCallback<ICollection<T>> ItemsChanged { get; set; }

    /// <summary>
    /// Gets or sets the function to convert an item to a string.
    /// </summary>
    [Parameter]
    public Func<T, string> ItemToStringFunc { get; set; } = (item => item?.ToString() ?? string.Empty);


    /// <summary>
    /// Gets or sets the text for the add button.
    /// </summary>
    [Parameter]
    public string TextAdd { get; set; } = "Add";

    /// <summary>
    /// Gets or sets the text for the remove all button.
    /// </summary>
    [Parameter]
    public string TextRemoveAll { get; set; } = "Remove All";

    /// <summary>
    /// Gets or sets the text for the edit button.
    /// </summary>
    [Parameter]
    public string TextEdit { get; set; } = "Edit {0}";

    /// <summary>
    /// Gets or sets the label of the collection editor.
    /// </summary>
    [Parameter]
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the collection should be virtualized.
    /// </summary>
    [Parameter]
    public bool Virtualize { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the collection is read-only.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the helper text for the collection editor.
    /// </summary>
    [Parameter]
    public string HelperText { get; set; }

    /// <summary>
    /// Gets or sets the icon for each item in the collection.
    /// </summary>
    [Parameter]
    public string ItemIcon { get; set; }

    /// <summary>
    /// Gets or sets the icon for the remove button.
    /// </summary>
    [Parameter]
    public string RemoveIcon { get; set; } = Icons.Material.Filled.Delete;

    /// <summary>
    /// Gets or sets the icon for the remove all button.
    /// </summary>
    [Parameter]
    public string RemoveAllIcon { get; set; } = Icons.Material.Filled.DeleteForever;

    /// <summary>
    /// Gets or sets the icon for the add button.
    /// </summary>
    [Parameter]
    public string AddIcon { get; set; } = Icons.Material.Filled.Add;

    /// <summary>
    /// Gets or sets the icon for the edit and preview button.
    /// </summary>
    [Parameter]
    public string EditIcon { get; set; } = Icons.Material.Filled.Edit;

    /// <summary>
    /// Gets or sets the icon for the view button.
    /// </summary>
    [Parameter]
    public string ViewIcon { get; set; } = Icons.Material.Filled.Pageview;

    /// <summary>
    /// Gets or sets the variant of the collection editor.
    /// </summary>
    [Parameter]
    public Variant Variant { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the clear button.
    /// </summary>
    [Parameter]
    public bool ShowClearButton { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to allow adding items.
    /// </summary>
    [Parameter]
    public bool AllowAdd { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to allow editing or previewing items.
    /// </summary>
    [Parameter]
    public bool AllowEditOrPreview { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to allow removing items.
    /// </summary>
    [Parameter]
    public bool AllowRemove { get; set; } = true;

    /// <summary>
    /// Gets or sets the options for the dialog.
    /// </summary>
    [Parameter]
    public DialogOptionsEx DialogOptions { get; set; }

    /// <summary>
    /// Gets or sets the color of the toolbar.
    /// </summary>
    [Parameter]
    public MudExColor ToolbarColor { get; set; } = Color.Surface;

    /// <summary>
    /// Gets or sets the position of the toolbar.
    /// </summary>
    [Parameter]
    public Position ToolbarPosition { get; set; } = Position.Bottom;

    /// <summary>
    /// Gets or sets a value indicating whether the toolbar sticks.
    /// </summary>
    [Parameter]
    public bool StickToolbar { get; set; } = true;

    /// <summary>
    /// Gets or sets the style of the toolbar.
    /// </summary>
    [Parameter]
    public string StyleToolbar { get; set; }

    /// <summary>
    /// Gets or sets the class of the toolbar.
    /// </summary>
    [Parameter]
    public string ClassToolbar { get; set; }

    /// <summary>
    /// Gets or sets the color of the toolbar buttons.
    /// </summary>
    [Parameter]
    public Color ToolbarButtonColor { get; set; } = Color.Inherit;

    /// <summary>
    /// Gets or sets the filter mode.
    /// </summary>
    [Parameter]
    public PropertyFilterMode FilterMode { get; set; } = PropertyFilterMode.Toggleable;

    /// <summary>
    /// Gets or sets the filter string.
    /// </summary>
    [Parameter]
    public string Filter { get; set; }

    /// <summary>
    /// Gets or sets the icon for the search button.
    /// </summary>
    [Parameter]
    public string SearchIcon { get; set; } = Icons.Material.Outlined.Search;

    private bool Primitive => MudExObjectEdit<T>.IsPrimitive();

    private bool IsInFilter(T item)
        => string.IsNullOrWhiteSpace(Filter)
           || ItemNameRender(item).ToLower().Contains(Filter.ToLower())
           || item?.GetProperties().Any(p => p.GetValue(item)?.ToString()?.ToLower().Contains(Filter.ToLower()) ?? false) == true;

    /// <summary>
    /// Generates the style string for the MudGrid component.
    /// </summary>
    private string GetMudGridStyle()
    {
        // Display block is required to have virtualization working
        return !Virtualize ? string.Empty : MudExStyleBuilder.GenerateStyleString(new
        {
            Display = "block"
        });
    }

    /// <summary>
    /// Generates the style string for the toolbar.
    /// </summary>
    private string GetToolbarStyle()
    {
        return MudExStyleBuilder.GenerateStyleString(new
        {
            ZIndex = 1,
            BackgroundColor = ToolbarColor.ToCssStringValue(),
            Position = StickToolbar ? "sticky" : null,
            Bottom = ToolbarPosition == Position.Bottom ? 0 as int? : null,
            Top = ToolbarPosition == Position.Top ? 0 as int? : null
        }, StyleToolbar);
    }

    /// <summary>
    /// Generates the style string for the collection editor.
    /// </summary>
    private string GetStyle()
    {
        return MudExStyleBuilder.GenerateStyleString(new
        {
            Height,
            MaxHeight,
        }, SizeUnit, Style);
    }

    /// <summary>
    /// Called when the parameters are set.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (ToolbarPosition != Position.Bottom && ToolbarPosition != Position.Top)
            throw new ArgumentException("ToolbarPosition must be either 'Top' or 'Bottom'.");
        base.OnParametersSet();
        Items ??= new List<T>();
        Items = new List<T>(Items);
    }

    /// <summary>
    /// Adds an item to the collection.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        Items?.Add(item);
        RaiseChanged();
    }

    /// <summary>
    /// Removes an item from the collection.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    public void Remove(T item)
    {
        Items?.Remove(item);
        RaiseChanged();
    }

    /// <summary>
    /// Raises the ItemsChanged event.
    /// </summary>
    private void RaiseChanged()
    {
        ItemsChanged.InvokeAsync(Items);
    }

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    private void RemoveAll()
    {
        Items?.Clear();
        RaiseChanged();
    }

    /// <summary>
    /// Edits an item in the collection.
    /// </summary>
    /// <param name="item">The item to edit.</param>
    public async Task Edit(T item)
    {
        DialogParameters parameters = new DialogParameters
        {
            { nameof(MudExObjectEditDialog<T>.DialogIcon), EditIcon },
            { nameof(MudExObjectEditDialog<T>.Localizer), Localizer }
        };
        var res = await DialogService.EditObject(item, TryLocalize(TextEdit, ItemNameRender(item)), DialogOptions ?? DefaultOptions(), null, parameters);
        if (!res.Cancelled)
        {
            SetValue(item, res.Result);
        }
    }

    /// <summary>
    /// Sets the value of an item in the collection.
    /// </summary>
    /// <param name="item">The item to set the value on.</param>
    /// <param name="newValue">The new value for the item.</param>
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
    
    /// <summary>
    /// Views an item in the collection.
    /// </summary>
    /// <param name="item">The item to view.</param>
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
        await DialogService.ShowObject(item, ItemNameRender(item), DialogOptions ?? DefaultOptions(), null, parameters);
    }

    /// <summary>
    /// Renders the item name
    /// </summary>
    public virtual string ItemNameRender(T item)
    {
        var res = ItemToStringFunc(item);
        if (!string.IsNullOrWhiteSpace(res) && LocalizerToUse != null && !string.IsNullOrWhiteSpace(LocalizerPattern))
        {
            return LocalizerToUse[string.Format(LocalizerPattern, item)];
        }

        return res;
    }

    /// <summary>
    /// Opens the dialog to add an item to the collection.
    /// </summary>
    /// <returns></returns>
    public async Task Add()
    {
        var item = typeof(T) == typeof(string) ? (T)(object)string.Empty : Primitive ? default : Activator.CreateInstance<T>();
        DialogParameters parameters = new DialogParameters
        {
            { nameof(MudExObjectEditDialog<T>.DialogIcon), AddIcon },
            { nameof(MudExObjectEditDialog<T>.Localizer), Localizer }
        };
        var res = await DialogService.EditObject(item, TryLocalize(TextAdd), DialogOptions ?? DefaultOptions(), null, parameters);
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