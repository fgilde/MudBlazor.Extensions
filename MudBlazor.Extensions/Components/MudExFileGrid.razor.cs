using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Core.FileManager;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The file area of an explorer: one directory's entries, with multi selection, rubber band, inline rename,
/// keyboard handling and drag and drop.
/// </summary>
/// <remarks>
/// Flat on purpose. A tree ties the shown level to the selected node, which stops working the moment more
/// than one entry is selected - three marked files are not a place. This shows the entries it is given and
/// reports what the user wants done; navigating and changing the structure stay with the consumer.
/// </remarks>
public partial class MudExFileGrid : MudExBaseComponent<MudExFileGrid>
{
    private readonly HashSet<MudExFileStructureNode> _selected = new();
    private MudExFileStructureNode _anchor;
    private MudExFileStructureNode _renaming;
    private string _renameValue;
    private bool _rubberBandActive;

    /// <summary>The entries to show.</summary>
    [Parameter, SafeCategory("Data")]
    public IReadOnlyCollection<MudExFileStructureNode> Items { get; set; } = Array.Empty<MudExFileStructureNode>();

    /// <summary>How the entries are presented.</summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExFileGridView View { get; set; } = MudExFileGridView.Tiles;

    /// <summary>Raised when the user switches the view.</summary>
    [Parameter] public EventCallback<MudExFileGridView> ViewChanged { get; set; }

    /// <summary>The column the entries are sorted by.</summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExFileGridSort SortBy { get; set; } = MudExFileGridSort.Name;

    /// <summary>Raised when the user sorts by another column.</summary>
    [Parameter] public EventCallback<MudExFileGridSort> SortByChanged { get; set; }

    /// <summary>Sorts descending.</summary>
    [Parameter, SafeCategory("Appearance")]
    public bool SortDescending { get; set; }

    /// <summary>Raised when the sort direction changes.</summary>
    [Parameter] public EventCallback<bool> SortDescendingChanged { get; set; }

    /// <summary>Directories are listed before files, whatever the sort is.</summary>
    [Parameter, SafeCategory("Appearance")]
    public bool DirectoriesFirst { get; set; } = true;

    /// <summary>The selected entries.</summary>
    [Parameter, SafeCategory("Data")]
    public IReadOnlyCollection<MudExFileStructureNode> SelectedItems
    {
        get => _selected;
        set
        {
            if (SameSelection(value))
                return;

            _selected.Clear();
            foreach (var item in value ?? Array.Empty<MudExFileStructureNode>())
                _selected.Add(item);
            _anchor = _selected.LastOrDefault();
        }
    }

    /// <summary>Raised whenever the selection changes.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<MudExFileStructureNode>> SelectedItemsChanged { get; set; }

    /// <summary>Lets the user select more than one entry, with ctrl, shift and the rubber band.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowMultiSelect { get; set; } = true;

    /// <summary>Lets the user drag a rectangle over the free space to select what it touches.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowRubberBand { get; set; } = true;

    /// <summary>Lets the user rename an entry in place, with F2 or a second click on the name.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowRename { get; set; } = true;

    /// <summary>Lets the user drag entries out, and drop entries and files from outside on a directory.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowDragDrop { get; set; } = true;

    /// <summary>Shows the context menu on right click.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowContextMenu { get; set; } = true;

    /// <summary>Virtualizes the entries. Worth it from a few hundred on.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool Virtualize { get; set; }

        /// <summary>
    /// Rendered instead of the icon of a tile, for a real preview of the entry rather than a symbol.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public RenderFragment<MudExFileStructureNode> PreviewTemplate { get; set; }

/// <summary>Renders an entry. The default renders an icon, the name and, in details view, its columns.</summary>
    [Parameter] public RenderFragment<MudExFileStructureNode> ItemTemplate { get; set; }

    /// <summary>Additional entries for the context menu, rendered below the built-in ones.</summary>
    [Parameter] public RenderFragment<IReadOnlyCollection<MudExFileStructureNode>> ContextMenuContent { get; set; }

    /// <summary>Shown when there is nothing to list.</summary>
    [Parameter] public RenderFragment EmptyContent { get; set; }

    /// <summary>Raised when an entry is opened: a double click, or enter.</summary>
    [Parameter] public EventCallback<MudExFileStructureNode> OnOpened { get; set; }

    /// <summary>Raised when the user finished an inline rename. Renaming is the consumer's job.</summary>
    [Parameter] public EventCallback<MudExFileGridRenameRequest> OnRenamed { get; set; }

    /// <summary>Raised when the user asks for the selection to be deleted, with delete or the menu.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<MudExFileStructureNode>> OnDeleteRequested { get; set; }

    /// <summary>Raised when the user dropped a selection on a directory of this grid.</summary>
    [Parameter] public EventCallback<MudExFileGridMoveRequest> OnMoveRequested { get; set; }

    /// <summary>Raised when files from outside the browser were dropped.</summary>
    [Parameter] public EventCallback<MudExFileGridExternalDropRequest> OnExternalFilesDropped { get; set; }

    /// <summary>The entries in the order they are rendered.</summary>
    public List<MudExFileStructureNode> OrderedItems => Order(Items).ToList();

    /// <summary>True while an inline rename is open.</summary>
    public bool IsRenaming => _renaming != null;

    /// <summary>Selects every entry.</summary>
    public Task SelectAllAsync() => SetSelectionAsync(OrderedItems);

    /// <summary>Clears the selection.</summary>
    public Task ClearSelectionAsync() => SetSelectionAsync(Array.Empty<MudExFileStructureNode>());

    /// <summary>Sets the selection to exactly the given entries.</summary>
    public Task SelectAsync(IEnumerable<MudExFileStructureNode> nodes) => SetSelectionAsync(nodes);

    /// <summary>Starts an inline rename of the given entry.</summary>
    public void StartRename(MudExFileStructureNode node)
    {
        if (!AllowRename || node == null)
            return;

        _renaming = node;
        _renameValue = node.Name;
        StateHasChanged();
    }

    private bool IsSelected(MudExFileStructureNode node) => _selected.Contains(node);

    private bool SameSelection(IReadOnlyCollection<MudExFileStructureNode> other)
    {
        other ??= Array.Empty<MudExFileStructureNode>();
        return other.Count == _selected.Count && other.All(_selected.Contains);
    }

    private IEnumerable<MudExFileStructureNode> Order(IEnumerable<MudExFileStructureNode> items)
    {
        items ??= Array.Empty<MudExFileStructureNode>();

        // Folders first is a grouping, not a direction: it applies before the column and is not inverted.
        var ordered = DirectoriesFirst
            ? items.OrderByDescending(n => n.IsDirectory)
            : items.OrderBy(_ => 0);

        return (SortBy, SortDescending) switch
        {
            (MudExFileGridSort.Size, false) => ordered.ThenBy(n => n.Size),
            (MudExFileGridSort.Size, true) => ordered.ThenByDescending(n => n.Size),
            (MudExFileGridSort.Type, false) => ordered.ThenBy(n => n.ContentType ?? string.Empty, StringComparer.OrdinalIgnoreCase),
            (MudExFileGridSort.Type, true) => ordered.ThenByDescending(n => n.ContentType ?? string.Empty, StringComparer.OrdinalIgnoreCase),
            (MudExFileGridSort.Modified, false) => ordered.ThenBy(n => n.LastModified ?? DateTimeOffset.MinValue),
            (MudExFileGridSort.Modified, true) => ordered.ThenByDescending(n => n.LastModified ?? DateTimeOffset.MinValue),
            (_, true) => ordered.ThenByDescending(n => n.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase),
            _ => ordered.ThenBy(n => n.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
        };
    }

    private async Task SetSelectionAsync(IEnumerable<MudExFileStructureNode> items)
    {
        // Materialized first: a caller may pass a query over the current selection, and clearing it would
        // then empty the query too.
        var next = (items ?? Array.Empty<MudExFileStructureNode>()).ToList();

        _selected.Clear();
        foreach (var item in next)
            _selected.Add(item);

        await SelectedItemsChanged.InvokeAsync(_selected.ToList());
        StateHasChanged();
    }

    /// <summary>
    /// A click on an entry. Plain replaces the selection, ctrl toggles one, shift takes the range from the
    /// last anchor - the three gestures every file manager has.
    /// </summary>
    private async Task ItemClickAsync(MudExFileStructureNode node, MouseEventArgs args)
    {
        if (_renaming != null && !ReferenceEquals(_renaming, node))
            await CommitRenameAsync();

        if (!AllowMultiSelect || (!args.CtrlKey && !args.ShiftKey))
        {
            _anchor = node;
            await SetSelectionAsync(new[] { node });
            return;
        }

        if (args.ShiftKey && _anchor != null)
        {
            var ordered = OrderedItems;
            var from = ordered.IndexOf(_anchor);
            var to = ordered.IndexOf(node);
            if (from >= 0 && to >= 0)
            {
                var (start, count) = from <= to ? (from, to - from + 1) : (to, from - to + 1);
                await SetSelectionAsync(ordered.Skip(start).Take(count));
                return;
            }
        }

        if (!_selected.Add(node))
            _selected.Remove(node);

        _anchor = node;
        await SelectedItemsChanged.InvokeAsync(_selected.ToList());
        StateHasChanged();
    }

    private async Task ItemDoubleClickAsync(MudExFileStructureNode node)
    {
        if (_renaming != null)
            await CommitRenameAsync();

        await OnOpened.InvokeAsync(node);
    }

    /// <summary>
    /// Keyboard handling on the grid itself: arrows move, enter opens, F2 renames, delete asks for a delete,
    /// ctrl+a takes everything and escape drops the selection.
    /// </summary>
    private async Task KeyDownAsync(KeyboardEventArgs args)
    {
        if (_renaming != null)
            return;

        var ordered = OrderedItems;
        if (ordered.Count == 0)
            return;

        switch (args.Key)
        {
            case "ArrowDown" or "ArrowRight":
                await MoveSelectionAsync(ordered, 1, args.ShiftKey);
                break;
            case "ArrowUp" or "ArrowLeft":
                await MoveSelectionAsync(ordered, -1, args.ShiftKey);
                break;
            case "Home":
                await SetSelectionAsync(new[] { ordered[0] });
                _anchor = ordered[0];
                break;
            case "End":
                await SetSelectionAsync(new[] { ordered[^1] });
                _anchor = ordered[^1];
                break;
            case "Enter" when _selected.Count > 0:
                await OnOpened.InvokeAsync(_selected.First());
                break;
            case "F2" when _selected.Count == 1:
                StartRename(_selected.First());
                break;
            case "Delete" when _selected.Count > 0:
                await OnDeleteRequested.InvokeAsync(_selected.ToList());
                break;
            case "Escape":
                await ClearSelectionAsync();
                break;
            case "a" or "A" when args.CtrlKey && AllowMultiSelect:
                await SelectAllAsync();
                break;
        }
    }

    private async Task MoveSelectionAsync(List<MudExFileStructureNode> ordered, int offset, bool extend)
    {
        var current = _anchor != null ? ordered.IndexOf(_anchor) : -1;
        var next = Math.Clamp(current + offset, 0, ordered.Count - 1);
        if (current < 0)
            next = offset > 0 ? 0 : ordered.Count - 1;

        var target = ordered[next];
        if (extend && AllowMultiSelect)
        {
            _selected.Add(target);
            await SelectedItemsChanged.InvokeAsync(_selected.ToList());
            StateHasChanged();
        }
        else
        {
            await SetSelectionAsync(new[] { target });
        }

        _anchor = target;
    }

    private async Task CommitRenameAsync()
    {
        var node = _renaming;
        var name = _renameValue;
        _renaming = null;
        _renameValue = null;

        if (node == null || string.IsNullOrWhiteSpace(name) || name == node.Name)
        {
            StateHasChanged();
            return;
        }

        await OnRenamed.InvokeAsync(new MudExFileGridRenameRequest { Node = node, NewName = name.Trim() });
        StateHasChanged();
    }

    private void CancelRename()
    {
        _renaming = null;
        _renameValue = null;
        StateHasChanged();
    }

    private async Task RenameKeyDownAsync(KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "Enter":
                await CommitRenameAsync();
                break;
            case "Escape":
                CancelRename();
                break;
        }
    }

    private async Task SortAsync(MudExFileGridSort column)
    {
        if (SortBy == column)
        {
            SortDescending = !SortDescending;
            await SortDescendingChanged.InvokeAsync(SortDescending);
        }
        else
        {
            SortBy = column;
            SortDescending = false;
            await SortByChanged.InvokeAsync(column);
            await SortDescendingChanged.InvokeAsync(false);
        }

        StateHasChanged();
    }

    private string SortIcon(MudExFileGridSort column) => SortBy != column
        ? null
        : SortDescending
            ? Icons.Material.Filled.ArrowDownward
            : Icons.Material.Filled.ArrowUpward;
}

/// <summary>An inline rename the user finished in a <see cref="MudExFileGrid"/>.</summary>
public class MudExFileGridRenameRequest
{
    /// <summary>The entry to rename.</summary>
    public MudExFileStructureNode Node { get; init; }

    /// <summary>The name the user typed, trimmed.</summary>
    public string NewName { get; init; }
}

/// <summary>Entries the user dropped on a directory of a <see cref="MudExFileGrid"/>.</summary>
public class MudExFileGridMoveRequest
{
    /// <summary>The entries that were dragged.</summary>
    public IReadOnlyCollection<MudExFileStructureNode> Nodes { get; init; }

    /// <summary>The directory they were dropped on. <c>null</c> means the grid's own level.</summary>
    public MudExFileStructureNode Target { get; init; }
}

/// <summary>Files from outside the browser, dropped on a <see cref="MudExFileGrid"/>.</summary>
public class MudExFileGridExternalDropRequest
{
    /// <summary>The dropped files.</summary>
    public IReadOnlyList<Microsoft.AspNetCore.Components.Forms.IBrowserFile> Files { get; init; }

    /// <summary>The directory they were dropped on. <c>null</c> means the grid's own level.</summary>
    public MudExFileStructureNode Target { get; init; }
}
