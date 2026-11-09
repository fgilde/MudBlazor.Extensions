using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// An in-ribbon gallery of previews, for example text styles. The strip shows <see cref="VisibleRows"/> rows of
/// <see cref="Columns"/> items, steps a row per arrow and drops all items as a grid from its "more" arrow.
/// </summary>
/// <typeparam name="TItem">Type of the gallery items.</typeparam>
public partial class MudExRibbonGallery<TItem>
{
    private int _firstRow;

    /// <summary>
    /// Items of the gallery.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; }

    /// <summary>
    /// The highlighted item.
    /// </summary>
    [Parameter] public TItem SelectedItem { get; set; }

    /// <summary>
    /// Raised when the user selects an item.
    /// </summary>
    [Parameter] public EventCallback<TItem> SelectedItemChanged { get; set; }

    /// <summary>
    /// Preview of an item. Defaults to the text of <see cref="ItemText"/>.
    /// </summary>
    [Parameter] public RenderFragment<TItem> ItemTemplate { get; set; }

    /// <summary>
    /// Name of an item for previews, tooltips and the command list. Defaults to <see cref="object.ToString"/>.
    /// </summary>
    [Parameter] public Func<TItem, string> ItemText { get; set; }

    /// <summary>
    /// Number of item rows visible in the ribbon.
    /// </summary>
    [Parameter] public int VisibleRows { get; set; } = 1;

    /// <summary>
    /// Number of items per row, in the ribbon and in the dropped grid.
    /// </summary>
    [Parameter] public int Columns { get; set; } = 4;

    /// <inheritdoc />
    public override bool IsCommand => false;

    /// <inheritdoc />
    protected override object StateKey
        => (base.StateKey, Items, SelectedItem, VisibleRows, Columns, FirstRow);

    private int ColumnCount => Math.Max(1, Columns);

    private int RowCount => ((Items?.Count() ?? 0) + ColumnCount - 1) / ColumnCount;

    private int MaxFirstRow => Math.Max(0, RowCount - Math.Max(1, VisibleRows));

    private int FirstRow => Math.Min(_firstRow, MaxFirstRow);

    private string ColumnsStyle => $"--mud-ex-ribbon-gallery-columns:{ColumnCount}";

    private IEnumerable<TItem> StripItems => (Items ?? []).Skip(FirstRow * ColumnCount).Take(Math.Max(1, VisibleRows) * ColumnCount);

    private string TextOf(TItem item) => ItemText?.Invoke(item) ?? item?.ToString();

    private bool IsSelected(TItem item) => EqualityComparer<TItem>.Default.Equals(item, SelectedItem);

    private string ItemClass(TItem item) => IsSelected(item) ? "mud-ex-ribbon-gallery-item mud-ex-ribbon-gallery-item-selected" : "mud-ex-ribbon-gallery-item";

    private void Scroll(int rows)
    {
        _firstRow = Math.Clamp(FirstRow + rows, 0, MaxFirstRow);
        NotifyStateChanged();
    }

    private async Task SelectAsync(TItem item)
    {
        if (Disabled)
            return;
        SelectedItem = item;
        var row = (Items ?? []).TakeWhile(i => !IsSelected(i)).Count() / ColumnCount;
        if (row < FirstRow || row >= FirstRow + Math.Max(1, VisibleRows))
            _firstRow = Math.Min(row, MaxFirstRow);
        NotifyStateChanged();
        await SelectedItemChanged.InvokeAsync(item);
    }
}
