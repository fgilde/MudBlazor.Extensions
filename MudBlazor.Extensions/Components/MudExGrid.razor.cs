using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using System.Collections.Concurrent;
using TagLib.Id3v2;

namespace MudBlazor.Extensions.Components;


/// <summary>
/// A simple grid component that supports Columns, and Rows and ColSpan and RowSpan for containing sections.
/// </summary>
public partial class MudExGrid
{
    private ConcurrentBag<MudExGridSection> _childSections = new();
    internal ElementReference GridElement;

    [Inject] private IJSRuntime JS { get; set; }
    private IJSObjectReference? _module;

    private string GetClass() =>
      new MudExCssBuilder("mud-ex-grid")
          .AddClass($"mud-ex-grid-column-{Column}")
          .AddClass($"mud-ex-grid-row-{Row}")
          .AddClass(Class)
      .Build();

    private string GetStyle() =>
        new MudExStyleBuilder()
            .WithHeight($"{Height}", Height is not null)
            .AddRaw(Style)
        .Build();

    /// <summary>
    /// Gets or sets the child content of the component.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the column size for the component. Default is 4.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int Column { get; set; } = 4;

    /// <summary>
    /// Gets or sets the row size for the component. Default is 4.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public int Row { get; set; } = 4;

    /// <summary>
    /// Gets or sets the height of the component.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Appearance)]
    public MudExSize<double>? Height { get; set; }

    /// <summary>
    /// Event callback for the click event.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the click event should stop propagation. Default is false.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public bool OnClickStopPropagation { get; set; }

    /// <summary>
    /// Event callback for the context menu event.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public EventCallback OnContextMenu { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the context menu event should prevent its default action. Default is false.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public bool OnContextMenuPreventDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the context menu event should stop propagation. Default is false.
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.Item.Behavior)]
    public bool OnContextMenuStopPropagation { get; set; }


    // Global behavior
    [Parameter] public bool FloatMode { get; set; } = true; // allow items to float up when space frees
    [Parameter] public bool CompactOnDrop { get; set; } = true; // run compaction after drop
    [Parameter] public bool ResolveCollisions { get; set; } = true; // push items when overlapping


    [JSInvokable("MudEx_CommitLayout")]
    public async Task CommitLayout(List<SectionLayoutDto> changes)
    {
        foreach (var ch in changes)
        {
            var s = _childSections.FirstOrDefault(x => x.Id == ch.Id);
            if (s is null) continue;
            s.SetLayout(ch);
        }
        if (CompactOnDrop) Compact();
                
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        string js = JsImportHelper.ComponentJs<MudExGrid>();
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", js);
            await _module.InvokeVoidAsync("mudexGrid.bind", GridElement, DotNetObjectReference.Create(this), Row, Column);
        }

        await base.OnAfterRenderAsync(firstRender);
    }


    // Utility
    private bool IsOccupied(MudExGridSection ignore, int r, int c)
    => _childSections.Any(s => s != ignore && r >= s.Row && r < s.Row + s.RowSpan && c >= s.Column && c < s.Column + s.ColSpan);

    public bool TryMove(MudExGridSection s, int newRow, int newCol)
    {
        newRow = Math.Max(1, Math.Min(Row, newRow));
        newCol = Math.Max(1, Math.Min(Column, newCol));


        if (ResolveCollisions)
            PushOthers(s, newRow, newCol, s.RowSpan, s.ColSpan);


        if (CanPlace(s, newRow, newCol, s.RowSpan, s.ColSpan))
        {
            s.Row = newRow; s.Column = newCol;
            if (CompactOnDrop) Compact();
            return true;
        }
        return false;
    }


    public bool TryResize(MudExGridSection s, int newRowSpan, int newColSpan)
    {
        newRowSpan = Math.Clamp(newRowSpan, s.MinRowSpan, Math.Min(s.MaxRowSpan, Row - s.Row + 1));
        newColSpan = Math.Clamp(newColSpan, s.MinColSpan, Math.Min(s.MaxColSpan, Column - s.Column + 1));


        if (ResolveCollisions)
            PushOthers(s, s.Row, s.Column, newRowSpan, newColSpan);


        if (CanPlace(s, s.Row, s.Column, newRowSpan, newColSpan))
        {
            s.RowSpan = newRowSpan; s.ColSpan = newColSpan;
            if (CompactOnDrop) Compact();
            return true;
        }
        return false;
    }


    private bool CanPlace(MudExGridSection ignore, int r, int c, int rs, int cs)
    {
        if (r < 1 || c < 1 || r + rs - 1 > Row || c + cs - 1 > Column) return false;
        for (int i = r; i < r + rs; i++)
            for (int j = c; j < c + cs; j++)
                if (IsOccupied(ignore, i, j)) return false;
        return true;
    }


    private void PushOthers(MudExGridSection mover, int r, int c, int rs, int cs)
    {
        // Simple push-down then right strategy (gridstack-like). Can be improved to BFS.
        foreach (var s in _childSections.Where(x => x != mover))
        {
            bool overlap = !(s.Row + s.RowSpan - 1 < r || r + rs - 1 < s.Row || s.Column + s.ColSpan - 1 < c || c + cs - 1 < s.Column);
            if (overlap)
            {
                // Prefer pushing down if there is room, else push right
                int newRow = s.Row;
                int newCol = s.Column;
                if (s.Row + s.RowSpan <= Row) newRow = r + rs; else newCol = c + cs;


                newRow = Math.Max(1, Math.Min(Row, newRow));
                newCol = Math.Max(1, Math.Min(Column, newCol));


                // Find nearest free spot scanning row-major
                (int Row, int Col)? free = FindNearestFreeSpot(s, newRow, newCol);
                if (free.HasValue)
                {
                    s.Row = free.Value.Row; s.Column = free.Value.Col;
                }
            }
        }
    }

    private (int Row, int Col)? FindNearestFreeSpot(MudExGridSection s, int startR, int startC)
    {
        for (int i = startR; i <= Row; i++)
        for (int j = (i == startR ? startC : 1); j <= Column; j++)
            if (CanPlace(s, i, j, s.RowSpan, s.ColSpan)) return (i, j);
        // fallback: scan from 1,1
        for (int i = 1; i <= Row; i++)
        for (int j = 1; j <= Column; j++)
            if (CanPlace(s, i, j, s.RowSpan, s.ColSpan)) return (i, j);
        return null;
    }


    public void Compact()
    {
        if (!FloatMode) return;
        // Pull each widget up as far as possible
        foreach (var s in _childSections.OrderBy(x => x.Row).ThenBy(x => x.Column))
        {
            int r = s.Row;
            while (r > 1 && CanPlace(s, r - 1, s.Column, s.RowSpan, s.ColSpan)) r--;
            s.Row = r;
        }
    }


    /// <summary>
    /// Registers a section with the grid.
    /// </summary>
    /// <param name="section"></param>
    internal void RegisterSection(MudExGridSection section)
    {
        if (!_childSections.Contains(section))
        {
            _childSections.Add(section);
        }
    }

    /// <summary>
    /// Unregisters a section with the grid.
    /// </summary>
    /// <param name="section"></param>
    internal void UnregisterSection(MudExGridSection section)
    {
        if (_childSections.Contains(section))
        {
            var newSections = new ConcurrentBag<MudExGridSection>(_childSections.Where(s => s != section));
            Interlocked.Exchange(ref _childSections, newSections);
        }
    }

    /// <summary>
    /// Returns all containing sections.
    /// </summary>
    public MudExGridSection[] GetAllChildSections() => _childSections.ToArray();

    public (int Row, int Column)? GetNextFreeSpace()
    {
        bool isOccupied(int row, int col) => _childSections.Any(section => row >= section.Row && row < section.Row + section.RowSpan && col >= section.Column && col < section.Column + section.ColSpan);

        for (int i = 1; i <= Row; i++)
        {
            for (int j = 1; j <= Column; j++)
            {
                if (!isOccupied(i, j))
                {
                    return (i, j); 
                }
            }
        }

        return null; 
    }

    /// <summary>
    /// The on click handler
    /// </summary>    
    protected virtual Task OnClickHandler(MouseEventArgs ev) => OnClick.InvokeAsync(ev);
}