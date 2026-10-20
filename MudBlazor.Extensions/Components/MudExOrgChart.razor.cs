using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.Base;
using static System.FormattableString;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// An org chart: a pannable, zoomable tree of cards with search, collapse, minimap and drag and drop re-parenting.
/// The tree is plain nested lists drawn by CSS; pan and zoom are a CSS transform of the stage.
/// </summary>
public partial class MudExOrgChart<TItem>
{
    private const double Padding = 16;
    private readonly string _uid = $"mud-ex-org-{Guid.NewGuid():N}";
    private readonly OrgChartIndex<TItem> _index = new();
    private readonly HashSet<int> _collapsed = [];
    private ElementReference _stage;
    private List<int> _matches = [];
    private HashSet<int> _matchSet = [];
    private int _match;
    private int? _dragged;
    private int? _pendingCenter;
    private bool _layoutDirty = true;
    private bool _fitted;
    private bool _panning;
    private bool _minimapOpen = true;
    private double _panX;
    private double _panY;
    private double _zoom = 1;
    private double _x = Padding;
    private double _y = Padding;
    private Measured _layout = Measured.Empty;
    private string _searchText;

    /// <summary>The items. With <see cref="ParentIdFunc"/> all items, with <see cref="ChildrenFunc"/> the roots.</summary>
    [Parameter, SafeCategory("Data")] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>The id of an item, the item itself by default.</summary>
    [Parameter, SafeCategory("Data")] public Func<TItem, object> IdFunc { get; set; } = item => item;

    /// <summary>The id of the parent of an item; an item whose parent is null or missing is a root.</summary>
    [Parameter, SafeCategory("Data")] public Func<TItem, object> ParentIdFunc { get; set; }

    /// <summary>Alternative to <see cref="ParentIdFunc"/>: the children of an item, <see cref="Items"/> are then the roots.</summary>
    [Parameter, SafeCategory("Data")] public Func<TItem, IEnumerable<TItem>> ChildrenFunc { get; set; }

    /// <summary>The text of an item for the default card and the default search.</summary>
    [Parameter, SafeCategory("Data")] public Func<TItem, string> TextFunc { get; set; } = item => item?.ToString();

    /// <summary>The number on the collapse button of a card; a null result falls back to <see cref="CountDescendants"/>.</summary>
    [Parameter, SafeCategory("Data")] public Func<TItem, int?> CountFunc { get; set; }

    /// <summary>The collapse button counts everyone below a card instead of only its children.</summary>
    [Parameter, SafeCategory("Data")] public bool CountDescendants { get; set; }

    /// <summary>The content of a card. A card with <see cref="TextFunc"/> by default.</summary>
    [Parameter, SafeCategory("Appearance")] public RenderFragment<TItem> NodeTemplate { get; set; }

    /// <summary>Additional content at the end of the toolbar.</summary>
    [Parameter, SafeCategory("Appearance")] public RenderFragment ToolbarContent { get; set; }

    /// <summary>Top down or left to right.</summary>
    [Parameter, SafeCategory("Appearance")] public OrgChartOrientation Orientation { get; set; } = OrgChartOrientation.TopDown;

    /// <summary>The height of the chart area.</summary>
    [Parameter, SafeCategory("Appearance")] public string Height { get; set; } = "600px";

    /// <summary>Shows the toolbar with search, expand, collapse and zoom.</summary>
    [Parameter, SafeCategory("Appearance")] public bool ShowToolbar { get; set; } = true;

    /// <summary>Shows the search field in the toolbar.</summary>
    [Parameter, SafeCategory("Appearance")] public bool ShowSearch { get; set; } = true;

    /// <summary>Shows the minimap; it can still be hidden with its toggle button.</summary>
    [Parameter, SafeCategory("Minimap")] public bool ShowMinimap { get; set; } = true;

    /// <summary>The corner of the minimap. Only the corners are supported, center origins fall back to bottom or right.</summary>
    [Parameter, SafeCategory("Minimap")] public Origin MinimapPosition { get; set; } = Origin.BottomRight;

    /// <summary>The width of the minimap box in pixels; the tree is fitted in with its aspect ratio kept.</summary>
    [Parameter, SafeCategory("Minimap")] public double MinimapWidth { get; set; } = 220;

    /// <summary>The height of the minimap box in pixels.</summary>
    [Parameter, SafeCategory("Minimap")] public double MinimapHeight { get; set; } = 140;

    /// <summary>Nodes at this depth and deeper start collapsed (0 = only the roots are visible); null expands everything.</summary>
    [Parameter, SafeCategory("Behavior")] public int? InitiallyExpandedDepth { get; set; }

    /// <summary>Fits the whole tree into view when it first shows.</summary>
    [Parameter, SafeCategory("Behavior")] public bool FitOnOpen { get; set; } = true;

    /// <summary>The smallest zoom of a fit, so a wide tree stays readable; <see cref="MinZoom"/> by default.</summary>
    [Parameter, SafeCategory("Behavior")] public double? FitMinZoom { get; set; }

    /// <summary>The smallest zoom.</summary>
    [Parameter, SafeCategory("Behavior")] public double MinZoom { get; set; } = 0.2;

    /// <summary>The largest zoom.</summary>
    [Parameter, SafeCategory("Behavior")] public double MaxZoom { get; set; } = 2.5;

    /// <summary>The factor of one zoom step.</summary>
    [Parameter, SafeCategory("Behavior")] public double ZoomStep { get; set; } = 1.2;

    /// <summary>The mouse wheel zooms around the pointer and pans with Ctrl, instead of the other way round.</summary>
    [Parameter, SafeCategory("Behavior")] public bool ZoomOnWheel { get; set; }

    /// <summary>The search text; matches are highlighted and the current one is centered.</summary>
    [Parameter, SafeCategory("Search")] public string SearchText { get; set; }

    /// <summary>Raised when the search text changes in the toolbar.</summary>
    [Parameter, SafeCategory("Search")] public EventCallback<string> SearchTextChanged { get; set; }

    /// <summary>Whether an item matches a search text; by default <see cref="TextFunc"/> contains the text.</summary>
    [Parameter, SafeCategory("Search")] public Func<TItem, string, bool> SearchFunc { get; set; }

    /// <summary>The placeholder of the search field, "Search" by default.</summary>
    [Parameter, SafeCategory("Search")] public string SearchPlaceholder { get; set; }

    /// <summary>When set, the search field is an autocomplete that lists the matches with this template; picking one centers it.</summary>
    [Parameter, SafeCategory("Search")] public RenderFragment<TItem> SearchItemTemplate { get; set; }

    /// <summary>Lets the user drag a node onto another to change its parent.</summary>
    [Parameter, SafeCategory("Reparent")] public bool AllowReparent { get; set; }

    /// <summary>Lets the user drop a node onto the empty canvas to make it a root.</summary>
    [Parameter, SafeCategory("Reparent")] public bool AllowDropToRoot { get; set; }

    /// <summary>Whether an item may get a new parent (default for a root). Cycles are always refused.</summary>
    [Parameter, SafeCategory("Reparent")] public Func<TItem, TItem, bool> CanReparent { get; set; }

    /// <summary>Raised when a node is dropped onto a valid new parent; the consumer changes the data.</summary>
    [Parameter, SafeCategory("Reparent")] public EventCallback<OrgChartReparentArgs<TItem>> OnReparent { get; set; }

    /// <summary>Raised when a node is dropped onto a parent below it or one <see cref="CanReparent"/> refuses; <see cref="OrgChartReparentArgs{TItem}.Refusal"/> says why.</summary>
    [Parameter, SafeCategory("Reparent")] public EventCallback<OrgChartReparentArgs<TItem>> OnReparentRefused { get; set; }

    /// <summary>With <see cref="AllowDropToRoot"/>: the toolbar shows this content as a drop zone that makes the dropped node a root. It lights up while a node is dragged.</summary>
    [Parameter, SafeCategory("Reparent")] public RenderFragment RootDropZone { get; set; }

    /// <summary>The selected item.</summary>
    [Parameter, SafeCategory("Selection")] public TItem SelectedItem { get; set; }

    /// <summary>Raised when a click selects an item.</summary>
    [Parameter, SafeCategory("Selection")] public EventCallback<TItem> SelectedItemChanged { get; set; }

    /// <summary>Raised when a node is clicked.</summary>
    [Parameter, SafeCategory("Selection")] public EventCallback<TItem> OnNodeClick { get; set; }

    /// <summary>The current zoom factor.</summary>
    public double Zoom => _zoom;

    /// <summary>Whether a node is being dragged.</summary>
    public bool IsDragging => _dragged is not null;

    /// <summary>The item being dragged, default while none is.</summary>
    public TItem DraggedItem => _dragged is { } key ? _index.Item(key) : default;

    internal int Version { get; private set; }

    internal bool CanDrag => AllowReparent;

    internal IReadOnlyList<TItem> Roots => _index.Roots;

    internal IReadOnlyList<TItem> ChildrenOf(TItem item) => _index.Children(_index.KeyOf(item));

    internal int KeyOf(TItem item) => _index.KeyOf(item);

    internal string DomId(int key) => $"{_uid}-{key}";

    internal bool IsCollapsed(int key) => _collapsed.Contains(key);

    internal bool IsSelected(int key) => SelectedItem is not null && _index.TryKeyOf(SelectedItem, out var selected) && selected == key;

    internal bool IsMatch(int key) => _matchSet.Contains(key);

    internal bool IsCurrentMatch(int key) => _matches.Count > 0 && _matches[_match] == key;

    internal int CountOf(TItem item, int key) => CountFunc?.Invoke(item) ?? (CountDescendants ? _index.Descendants(key) : _index.Children(key).Count);

    private string StageStyle => Invariant($"transform:translate({_x:0.#}px,{_y:0.#}px) scale({_zoom:0.###})");

    private string ViewportClass => "mud-ex-org-chart-viewport" + (_panning ? " mud-ex-org-chart-panning" : string.Empty);

    private OrgChartBox View => new(-1, -_x / _zoom, -_y / _zoom, _layout.ViewportWidth / _zoom, _layout.ViewportHeight / _zoom);

    private HashSet<int> Marked()
    {
        var marked = new HashSet<int>(_matchSet);
        if (SelectedItem is not null && _index.TryKeyOf(SelectedItem, out var selected))
        {
            marked.Add(selected);
        }

        return marked;
    }

    /// <inheritdoc />
    public override object[] GetJsArguments() => [ElementReference, CreateDotNetObjectReference()];

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        foreach (var (key, depth) in _index.Build(Items ?? [], IdFunc, ParentIdFunc, ChildrenFunc))
        {
            if (depth >= InitiallyExpandedDepth && _index.Children(key).Count > 0)
            {
                _collapsed.Add(key);
            }
        }

        if (SearchText != _searchText)
        {
            _searchText = SearchText;
            Search();
        }

        Changed();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!_layoutDirty || JsReference is null)
        {
            return;
        }

        _layoutDirty = false;
        _layout = await JsReference.InvokeAsync<Measured>("measure", _stage) ?? Measured.Empty;
        if (!_fitted && FitOnOpen && _layout.StageWidth > 0)
        {
            _fitted = true;
            Fit(FitMinZoom ?? MinZoom);
        }

        if (_pendingCenter is { } key && _layout.Box(key) is { } box)
        {
            _pendingCenter = null;
            CenterOnPoint(box.X + (box.Width / 2), box.Y + (box.Height / 2));
        }

        StateHasChanged();
    }

    /// <summary>Expands every node.</summary>
    public void ExpandAll()
    {
        _collapsed.Clear();
        Changed();
    }

    /// <summary>Collapses every node below the roots.</summary>
    public void CollapseAll()
    {
        _collapsed.UnionWith(_index.Keys.Where(k => _index.Children(k).Count > 0));
        _collapsed.ExceptWith(_index.Roots.Select(KeyOf));
        Changed();
    }

    /// <summary>Collapses or expands a node.</summary>
    public void Toggle(TItem item)
    {
        var key = KeyOf(item);
        if (!_collapsed.Remove(key))
        {
            _collapsed.Add(key);
        }

        Changed();
    }

    /// <summary>Zooms in one step around the middle of the view.</summary>
    public void ZoomIn() => ZoomTo(_zoom * ZoomStep);

    /// <summary>Zooms out one step around the middle of the view.</summary>
    public void ZoomOut() => ZoomTo(_zoom / ZoomStep);

    /// <summary>Back to 100 %.</summary>
    public void ResetZoom() => ZoomTo(1);

    /// <summary>Fits the whole tree into view.</summary>
    public void FitToView()
    {
        Fit(MinZoom);
        StateHasChanged();
    }

    /// <summary>Expands the way to an item and moves it into the middle of the view.</summary>
    public void CenterOn(TItem item)
    {
        if (!_index.TryKeyOf(item, out var key))
        {
            return;
        }

        _collapsed.ExceptWith(_index.Ancestors(key));
        _pendingCenter = key;
        Changed();
    }

    internal async Task ClickAsync(TItem item)
    {
        await OnNodeClick.InvokeAsync(item);
        if (!Equals(SelectedItem, item))
        {
            SelectedItem = item;
            await SelectedItemChanged.InvokeAsync(item);
            Changed();
        }
    }

    internal void DragStart(TItem item)
    {
        _dragged = KeyOf(item);
        StateHasChanged();
    }

    // also after a drop outside the chart or Escape
    internal void DragEnd()
    {
        if (_dragged is not null)
        {
            _dragged = null;
            StateHasChanged();
        }
    }

    internal bool CanDropOn(int? target) => _dragged is { } dragged && IsTarget(dragged, target) && RefusalOf(dragged, target) is null;

    internal async Task DropAsync(int? target)
    {
        var dragged = _dragged;
        DragEnd();
        if (dragged is not { } key || !IsTarget(key, target))
        {
            return;
        }

        var parent = _index.ParentKey(key);
        var args = new OrgChartReparentArgs<TItem>(_index.Item(key), parent is { } p ? _index.Item(p) : default, target is { } t ? _index.Item(t) : default);
        if (RefusalOf(key, target) is { } refusal)
        {
            await OnReparentRefused.InvokeAsync(args with { Refusal = refusal });
            return;
        }

        await OnReparent.InvokeAsync(args);
    }

    // no target at all, nothing to refuse: the node itself, its current parent, the canvas without AllowDropToRoot
    private bool IsTarget(int dragged, int? target) => dragged != target && _index.ParentKey(dragged) != target && (target is not null || AllowDropToRoot);

    private OrgChartReparentRefusal? RefusalOf(int dragged, int? target) =>
        target is { } t && _index.Ancestors(t).Contains(dragged) ? OrgChartReparentRefusal.Cycle
        : CanReparent?.Invoke(_index.Item(dragged), target is { } p ? _index.Item(p) : default) == false ? OrgChartReparentRefusal.NotAllowed
        : null;

    private void Changed()
    {
        Version++;
        _layoutDirty = true;
        StateHasChanged();
    }

    // the autocomplete calls this debounced with the typed text: the matches are highlighted and listed
    private async Task<IEnumerable<TItem>> FindAsync(string text, CancellationToken cancellationToken)
    {
        if (text != _searchText)
        {
            await SetSearchAsync(text);
        }

        return _matches.Select(_index.Item);
    }

    // a picked match becomes the current one
    private void Pick(TItem item)
    {
        if (item is null || !_index.TryKeyOf(item, out var key))
        {
            return;
        }

        if (_matches.IndexOf(key) is var match and >= 0)
        {
            _match = match;
            FocusMatch();
            Changed();
            return;
        }

        CenterOn(item);
    }

    private async Task SetSearchAsync(string text)
    {
        _searchText = SearchText = text;
        await SearchTextChanged.InvokeAsync(text);
        Search();
        Changed();
    }

    private void Search()
    {
        var match = SearchFunc ?? ((item, text) => TextFunc(item)?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
        _matches = string.IsNullOrWhiteSpace(_searchText) ? [] : _index.Keys.Where(k => match(_index.Item(k), _searchText.Trim())).ToList();
        _matchSet = [.. _matches];
        _match = 0;
        FocusMatch();
    }

    private void NextMatch(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && _matches.Count > 0)
        {
            _match = (_match + 1) % _matches.Count;
            FocusMatch();
            Changed();
        }
    }

    private void FocusMatch()
    {
        if (_matches.Count > 0)
        {
            _collapsed.ExceptWith(_index.Ancestors(_matches[_match]));
            _pendingCenter = _matches[_match];
        }
    }

    private void ZoomTo(double zoom, double? x = null, double? y = null)
    {
        var next = Math.Clamp(zoom, MinZoom, MaxZoom);
        var cx = x ?? _layout.ViewportWidth / 2;
        var cy = y ?? _layout.ViewportHeight / 2;
        _x = cx - ((cx - _x) * next / _zoom);
        _y = cy - ((cy - _y) * next / _zoom);
        _zoom = next;
        StateHasChanged();
    }

    private void Fit(double minZoom)
    {
        if (_layout.StageWidth <= 0)
        {
            return;
        }

        _zoom = Math.Clamp(Math.Min((_layout.ViewportWidth - (2 * Padding)) / _layout.StageWidth, (_layout.ViewportHeight - (2 * Padding)) / _layout.StageHeight), Math.Min(minZoom, 1), 1);
        var width = _layout.StageWidth * _zoom;
        _x = Math.Max(Padding, (_layout.ViewportWidth - width) / 2);
        _y = Math.Max(Padding, (_layout.ViewportHeight - (_layout.StageHeight * _zoom)) / 2);
        // wider than the view even so: the biggest tree's root in the middle instead of the first root at the left
        if (width > _layout.ViewportWidth && Orientation == OrgChartOrientation.TopDown && _index.Roots.MaxBy(r => _index.Descendants(KeyOf(r))) is { } head
            && _layout.Box(KeyOf(head)) is { } box)
        {
            _x = (_layout.ViewportWidth / 2) - ((box.X + (box.Width / 2)) * _zoom);
        }
    }

    private void CenterOnPoint(double stageX, double stageY)
    {
        _x = (_layout.ViewportWidth / 2) - (stageX * _zoom);
        _y = (_layout.ViewportHeight / 2) - (stageY * _zoom);
        StateHasChanged();
    }

    /// <summary>Called by the JS module for the mouse wheel: Ctrl zooms around the pointer, otherwise it pans (the other way round with <see cref="ZoomOnWheel"/>).</summary>
    [JSInvokable]
    public void OnWheel(double x, double y, double deltaX, double deltaY, bool zoom)
    {
        if (zoom != ZoomOnWheel)
        {
            ZoomTo(_zoom * (deltaY < 0 ? 1.1 : 1 / 1.1), x, y);
            return;
        }

        _x -= deltaX;
        _y -= deltaY;
        StateHasChanged();
    }

    private void StartPan(PointerEventArgs e)
    {
        _panning = e.Button == 0;
        _panX = e.ClientX - _x;
        _panY = e.ClientY - _y;
    }

    // ponytail: each pointer move renders the chart shell and the minimap (the tree itself is skipped); a JS pan if that gets slow
    private void Pan(PointerEventArgs e)
    {
        if (_panning)
        {
            _x = e.ClientX - _panX;
            _y = e.ClientY - _panY;
        }
    }

    private void EndPan() => _panning = false;

    private IEnumerable<OrgChartLine> Lines() =>
        _layout.Boxes.Select(child => (child, parent: _index.ParentKey(child.Key) is { } p ? _layout.Box(p) : null))
            .Where(l => l.parent is not null)
            .Select(l => Orientation == OrgChartOrientation.TopDown
                ? new OrgChartLine(l.parent.X + (l.parent.Width / 2), l.parent.Y + l.parent.Height, l.child.X + (l.child.Width / 2), l.child.Y)
                : new OrgChartLine(l.parent.X + l.parent.Width, l.parent.Y + (l.parent.Height / 2), l.child.X, l.child.Y + (l.child.Height / 2)));

    private sealed record Measured(double ViewportWidth, double ViewportHeight, double StageWidth, double StageHeight, OrgChartBox[] Boxes)
    {
        public static readonly Measured Empty = new(0, 0, 0, 0, []);

        private Dictionary<int, OrgChartBox> _byKey;

        public OrgChartBox Box(int key) => (_byKey ??= Boxes.ToDictionary(b => b.Key)).GetValueOrDefault(key);
    }
}
