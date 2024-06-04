using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple virtualize component that can be disabled with support for fixed items and ItemsProvider.
/// </summary>
/// <typeparam name="TItem"></typeparam>
public partial class MudExVirtualize<TItem>
{
    private ICollection<TItem> _allItems;

    /// <summary>
    /// Items per row. Defaults to 1.
    /// </summary>
    [Parameter]
    public int ItemsPerRow { get; set; } = 1;
    
    /// <summary>
    /// Set false to turn off virtualization
    /// </summary>
    [Parameter]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the item template for the list.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem> ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the item template for the list.
    /// </summary>
    [Parameter]
    public RenderFragment<TItem> ItemContent { get; set; }

    /// <summary>
    /// Gets or sets the template for items that have not yet been loaded in memory.
    /// </summary>
    [Parameter]
    public RenderFragment<PlaceholderContext> Placeholder { get; set; }

    /// <summary>
    /// Gets the size of each item in pixels. Defaults to 50px.
    /// </summary>
    [Parameter]
    public float ItemSize { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the function providing items to the list.
    /// </summary>
    [Parameter]
    public ItemsProviderDelegate<TItem> ItemsProvider { get; set; }

    /// <summary>
    /// Gets or sets the fixed item source.
    /// </summary>
    [Parameter]
    public ICollection<TItem> Items { get; set; }

    /// <summary>
    /// Gets or sets a value that determines how many additional items will be rendered
    /// before and after the visible region. This help to reduce the frequency of rendering
    /// during scrolling. However, higher values mean that more elements will be present
    /// in the page.
    /// </summary>
    [Parameter]
    public int OverscanCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the tag name of the HTML element that will be used as the virtualization spacer.
    /// One such element will be rendered before the visible items, and one more after them, using
    /// an explicit "height" style to control the scroll range.
    ///
    /// The default value is "div". If you are placing the <see cref="Virtualize{TItem}"/> instance inside
    /// an element that requires a specific child tag name, consider setting that here. For example when
    /// rendering inside a "tbody", consider setting <see cref="SpacerElement"/> to the value "tr".
    /// </summary>
    [Parameter]
    public string SpacerElement { get; set; } = "div";

    private List<TItem[]> GetVirtualizedRows()
        => (Items ?? _allItems).Chunk(Math.Max(ItemsPerRow, 1)).ToList();

    private List<TItem[]> VirtualizedRows => GetVirtualizedRows();


    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (_allItems == null && (!IsEnabled || ItemsPerRow > 1) && ItemsProvider != null)
            _allItems = (await AllItemsFromProviderAsync()).ToList();
    }


    private async Task<IEnumerable<TItem>> AllItemsFromProviderAsync()
    {
        if (ItemsProvider is null)
            return null;
        var request = new ItemsProviderRequest(0, int.MaxValue, default);
        var result = await ItemsProvider(request);
        return result.Items;
    }

    private Task OnScroll(EventArgs arg)
    {        
        return Task.CompletedTask;
    }
}