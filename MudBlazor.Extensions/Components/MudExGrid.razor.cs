using BlazorJS.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading.Channels;
using static MudBlazor.CategoryTypes;

namespace MudBlazor.Extensions.Components;


/// <summary>
/// A simple grid component that supports Columns, and Rows and ColSpan and RowSpan for containing sections.
/// </summary>
public partial class MudExGrid
{
    int _sectionIdCounter = 0;
    private bool _layoutSuspend;
    private string _cssClassName = "mud-ex-grid";
    private ConcurrentBag<MudExGridSection> _childSections = new();
    internal ElementReference GridElement;

    [Inject] private IJSRuntime JS { get; set; }
    private IJSObjectReference? _module;

    private string GetClass() =>
      new MudExCssBuilder(_cssClassName)
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


    /// <summary>
    ///  Allow items to float up when space frees
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public bool FloatMode { get; set; }

    /// <summary>
    /// Set to true to run compaction after drop
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public bool CompactOnDrop { get; set; } 

    /// <summary>
    ///  Set to true to push items when overlapping
    /// </summary>
    [ForJs, Parameter, SafeCategory("Moving")]
    public bool ResolveCollisions { get; set; } = true;

    /// <summary>
    /// Id of the grid.
    /// </summary>
    [Parameter] 
    public string? Id { get; set; }


    [JSInvokable("MudEx_CommitLayout")]
    public async Task CommitLayout(List<SectionLayoutDto> changes)
    {
        if (_layoutSuspend)
            return;
        _layoutSuspend = true;
        foreach (var ch in changes)
        {
            var s = _childSections.FirstOrDefault(x => x.Id == ch.Id);
            if (s is null) continue;
            s.SetLayout(ch);
        }
        if (CompactOnDrop) Compact();
                
        await InvokeAsync(StateHasChanged);
        await Task.Delay(200);
        _layoutSuspend = false;

    }
    
    protected override void OnInitialized()
    {
        Id ??= $"{_cssClassName}";
        base.OnInitialized();
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


    private bool IsOccupied(MudExGridSection ignore, int r, int c)
    => _childSections.Any(s => s != ignore && r >= s.Row && r < s.Row + s.RowSpan && c >= s.Column && c < s.Column + s.ColSpan);

    /// <summary>
    /// Returns all containing sections.
    /// </summary>
    public MudExGridSection[] GetAllChildSections() => _childSections.ToArray();

    public (int Row, int Column)? GetNextFreeSpace()
    {
        for (int i = 1; i <= Row; i++)
        {
            for (int j = 1; j <= Column; j++)
            {
                if (!IsOccupied(null, i, j))
                {
                    return (i, j);
                }
            }
        }

        return null;
    }

    public void Compact()
    {
        foreach (var s in _childSections.OrderBy(x => x.Row).ThenBy(x => x.Column))
        {
            int r = s.Row;
            while (r > 1 && CanPlace(s, r - 1, s.Column, s.RowSpan, s.ColSpan)) r--;
            s.Row = r;
        }
    }

    public Task<string> SaveLayoutAsync()
    {
        return Task.FromResult(JsonConvert.SerializeObject(GetLayout()));
    }

    private GridLayoutDto GetLayout()
    {
        return new GridLayoutDto(this.Column, this.Row, _childSections.Select(s => s.GetLayout()).ToList());
    }
    
    public void SetLayout(GridLayoutDto layout)
    {
        Column = layout.Columns;
        Row = layout.Rows;
        foreach (var ch in layout.Sections)
        {
            var s = _childSections.FirstOrDefault(x => x.Id == ch.Id);
            if (s is null) continue;
            s.SetLayout(ch);
        }
    }

    public Task RestoreLayoutAsync(string json)
    {
        var layout = JsonConvert.DeserializeObject<GridLayoutDto>(json);
        if (layout is null) return Task.CompletedTask;

        SetLayout(layout);
        return Task.CompletedTask;
    }

    private bool CanPlace(MudExGridSection ignore, int r, int c, int rs, int cs)
    {
        if (r < 1 || c < 1 || r + rs - 1 > Row || c + cs - 1 > Column) return false;
        for (int i = r; i < r + rs; i++)
            for (int j = c; j < c + cs; j++)
                if (IsOccupied(ignore, i, j)) return false;
        return true;
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
    /// The on click handler
    /// </summary>    
    protected virtual Task OnClickHandler(MouseEventArgs ev) => OnClick.InvokeAsync(ev);

    internal string GetNewSectionId()
    {
        return $"{Id}-section-{++_sectionIdCounter}";
    }
}