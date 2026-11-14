using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The editing surface of the notebook editor: shows a page with a selection layer above it. In the text tool a click on the free page starts a new
/// text block there and a click into a text block edits it; elements are selected, moved and resized with the pointer. The gestures run in the
/// browser and only their result reaches .NET as one undo step. The keyboard nudges, deletes, duplicates and cycles through the elements; Tab past
/// the last element leaves the surface. The canvas does not listen to <see cref="Office.Notebook.Notebook.Changed"/>: a host that changes the
/// notebook itself renders the canvas again.
/// </summary>
public partial class MudExNotebookCanvas
{
    internal const double NewTextWidth = 400;
    private const double DuplicateOffset = 10;
    private static readonly string[] BoxHandles = ["nw", "n", "ne", "e", "se", "s", "sw", "w"];
    private static readonly string[] TextHandles = ["move", "e"];

    private readonly JsModule<MudExNotebookCanvas> _js = new();
    private readonly EventCallback<KeyboardEventArgs> _keyDown;
    private readonly Dictionary<string, double> _heights = [];
    private List<string> _selection = [];
    private NotebookPage _page;
    private int _version;
    private NotebookTool _tool;
    private bool _raiseSelection;
    private ElementReference _root;

    /// <summary>Creates the canvas.</summary>
    public MudExNotebookCanvas() => _keyDown = new EventCallback<KeyboardEventArgs>(WithoutRender.Instance, (Func<KeyboardEventArgs, Task>)OnKeyDownAsync);

    /// <summary>The notebook the page belongs to. Edits are executed on it.</summary>
    [Parameter] public Notebook Notebook { get; set; }

    /// <summary>The page to edit. Changing it clears the selection.</summary>
    [Parameter] public NotebookPage Page { get; set; }

    /// <summary>The zoom factor; 1 is 100 %.</summary>
    [Parameter] public double Zoom { get; set; } = 1;

    /// <summary>The image URLs of the notebook; if null, the page uses data URIs.</summary>
    [Parameter] public NotebookImageUrls Images { get; set; }

    /// <summary>The width of the visible area in CSS pixels; the page is at least this wide.</summary>
    [Parameter] public double ViewportWidth { get; set; }

    /// <summary>The height of the visible area in CSS pixels; the page is at least this high.</summary>
    [Parameter] public double ViewportHeight { get; set; }

    /// <summary>If true elements can neither be selected nor changed, and links open with a plain click.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>What a pointer gesture on the page does. The host holds the tool and switches it.</summary>
    [Parameter] public NotebookTool Tool { get; set; }

    /// <summary>Raised whenever the selected elements change, also when the page changes or selected elements disappear.</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> SelectionChanged { get; set; }

    /// <summary>Raised when an edit made in the browser failed, with the exception to show. Without a handler the exception is passed to the browser.</summary>
    [Parameter] public EventCallback<Exception> CommandFailed { get; set; }

    /// <summary>The ids of the selected elements.</summary>
    public IReadOnlyList<string> Selection => _selection;

    internal IReadOnlyDictionary<string, double> MeasuredHeights => _heights;

    private bool Editable => !ReadOnly && Notebook is { ReadOnlyFormat: false } && Page is { LoadError: null };

    private double EffectiveZoom => MudExNotebookPageView.ZoomOf(Zoom);

    private IEnumerable<NoteElement> SelectedElements
    {
        get
        {
            var selected = _selection.ToHashSet();
            return Page?.Elements.Where(e => selected.Contains(e.Id)) ?? [];
        }
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (_text != null && Editable && parameters.TryGetValue<bool>(nameof(ReadOnly), out var readOnly) && readOnly)
            await RunAsync(CommitTextAsync);
        await base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ReferenceEquals(Page, _page) || !Editable)
        {
            if (!ReferenceEquals(Page, _page))
                _heights.Clear();
            _page = Page;
            _version = Page?.Version ?? 0;
            _text = null;
            Change([]);
        }
        else if (Page.Version != _version)
        {
            _version = Page.Version;
            if (_text is { } session && !IsAlive(session))
                _text = null;
            Change(_selection);
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (Tool != _tool)
        {
            _tool = Tool;
            await RunAsync(CommitTextAsync);
            if (Tool is NotebookTool.Pen or NotebookTool.Highlighter or NotebookTool.Eraser)
                Change([]);
        }

        await RaiseSelectionAsync();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            await _js.StartAsync(JsRuntime, this, null, dotnet => [_root, dotnet]);
    }

    /// <summary>Selects elements; unknown ids are ignored. A running text session on another element ends first.</summary>
    /// <param name="ids">The element ids.</param>
    /// <returns>A task that completes when the selection is shown.</returns>
    public async Task SelectAsync(IEnumerable<string> ids)
    {
        if (!Editable)
            return;
        var list = ids.ToList();
        if (_text != null && !(list is [var only] && only == _text.ElementId))
            await CommitTextAsync();
        Change(list);
        StateHasChanged();
        await RaiseSelectionAsync();
    }

    /// <summary>Called by the browser with the elements the user selected; a list of more than 100,000 ids is ignored.</summary>
    /// <param name="ids">The element ids.</param>
    /// <returns>A task that completes when the selection is shown.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnSelectionAsync(string[] ids) => Guard(() => ids is { Length: > JsPayload.MaxItems } ? Task.CompletedTask : SelectAsync(ids ?? []));

    /// <summary>
    /// Called by the browser for a click on an element: selects only it unless it is selected already, or with <paramref name="toggle"/> adds it to or
    /// removes it from the selection.
    /// </summary>
    /// <param name="id">The element id.</param>
    /// <param name="toggle">True if Shift or Ctrl was held.</param>
    /// <returns>A task that completes when the selection is shown.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnElementClickAsync(string id, bool toggle) => Guard(() =>
    {
        var selected = _selection.Contains(id);
        return SelectAsync(toggle ? selected ? _selection.Where(i => i != id).ToList() : [.. _selection, id] : selected ? _selection.ToList() : [id]);
    });

    /// <summary>Called by the browser once a selection frame was drawn: selects the elements lying completely inside.</summary>
    /// <param name="x">The left edge in page coordinates.</param>
    /// <param name="y">The top edge in page coordinates.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="toggle">True to add the elements to the selection.</param>
    /// <returns>A task that completes when the selection is shown.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnMarqueeAsync(double x, double y, double width, double height, bool toggle) => Guard(() =>
    {
        if (!Editable || !new[] { x, y, width, height }.All(double.IsFinite))
            return Task.CompletedTask;
        var inside = Page.Elements.Where(e => e.X >= x && e.Y >= y && e.X + e.Width <= x + width && e.Y + HeightOf(e) <= y + height).Select(e => e.Id);
        return SelectAsync(toggle ? _selection.Concat(inside).ToList() : inside.ToList());
    });

    /// <summary>Scrolls the page so that an element is visible.</summary>
    /// <param name="id">The element id.</param>
    /// <returns>A task that completes when the page scrolled.</returns>
    public async Task RevealAsync(string id)
    {
        if (_js.Js is not { } js || id == null)
            return;
        try { await js.InvokeVoidAsync("reveal", id); }
        catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException) { }
    }

    /// <summary>
    /// Called by the browser for a click on the free page or into a text block. The point becomes <see cref="LastClick"/>; in the text tool the text
    /// block under the point is edited or a new one starts there.
    /// </summary>
    /// <param name="x">The horizontal position in page coordinates.</param>
    /// <param name="y">The vertical position in page coordinates.</param>
    /// <returns>A task that completes when the text session started.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnPageClickAsync(double x, double y) => Guard(async () =>
    {
        if (!Editable || !double.IsFinite(x) || !double.IsFinite(y))
            return;
        LastClick = (Math.Max(0, x), Math.Max(0, y));
        if (Tool != NotebookTool.Text)
            return;
        await CommitTextAsync();
        var hit = Page.Elements.OfType<TextElement>().LastOrDefault(t => x >= t.X && x <= t.X + t.Width && y >= t.Y && y <= t.Y + HeightOf(t));
        await SelectAsync(hit == null ? [] : [hit.Id]);
        await BeginTextAsync(hit == null ? new NotebookTextEditRequest(NoteValues.NewId(), Math.Max(0, x), Math.Max(0, y), true) : Request(hit));
    });

    /// <summary>Called by the browser when the user moved elements. All moves are one undo step; positions are kept at 0 or more.</summary>
    /// <param name="dx">The horizontal offset in page coordinates.</param>
    /// <param name="dy">The vertical offset in page coordinates.</param>
    /// <param name="ids">The moved elements, or null for the selected ones.</param>
    /// <returns>A task that completes when the change is executed.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnMoveAsync(double dx, double dy, string[] ids) => Guard(() =>
    {
        if (!Editable || !double.IsFinite(dx) || !double.IsFinite(dy) || ids is { Length: > JsPayload.MaxItems })
            return;
        var moved = (ids ?? []).ToHashSet();
        Move((ids == null ? SelectedElements : Page.Elements.Where(e => moved.Contains(e.Id))).ToDictionary(e => e.Id, e => (e.X + dx, e.Y + dy)));
    });

    /// <summary>
    /// Called by the browser when the user resized elements. Text blocks and tables change only their width, pictures keep their aspect ratio
    /// unless <see cref="BoundsDto.Free"/> is set, and elements of an unknown type keep their size. All changes are one undo step.
    /// </summary>
    /// <param name="bounds">The new rectangles in page coordinates by element id.</param>
    /// <returns>A task that completes when the change is executed.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnResizeAsync(Dictionary<string, BoundsDto> bounds) => Guard(() =>
    {
        if (!Editable || bounds == null || bounds.Count > JsPayload.MaxItems)
            return;
        var changes = new Dictionary<string, NoteBounds>();
        foreach (var (id, dto) in bounds)
        {
            if (dto == null || !double.IsFinite(dto.X) || !double.IsFinite(dto.Y) || !double.IsFinite(dto.Width) || !double.IsFinite(dto.Height)
                || Find(id) is not { } element || element is OpaqueElement)
                continue;
            var target = new NoteBounds(Math.Max(0, dto.X), Math.Max(0, dto.Y), Math.Max(0, dto.Width), Math.Max(0, dto.Height));
            target = element switch
            {
                TextElement or TableElement => target with { Width = Math.Max(1, target.Width), Height = element.Height },
                ImageElement when !dto.Free && element.Width > 0 && element.Height > 0 => target with { Width = Math.Max(1, target.Width), Height = Math.Max(1, target.Width) * element.Height / element.Width },
                _ => target
            };
            if (target != element.Bounds)
                changes[id] = target;
        }

        if (changes.Count > 0)
            Execute(new ResizeElementsCommand(Page, changes));
    });

    /// <summary>Called by the browser with the rendered heights of text blocks and tables, in CSS pixels at 100 % zoom.</summary>
    /// <param name="heights">The heights by element id; only changed values are sent.</param>
    /// <returns>A task that completes when the page was laid out again.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnHeightsMeasuredAsync(Dictionary<string, double> heights) => Guard(() =>
    {
        if (heights is { Count: > JsPayload.MaxItems })
            return;
        var changed = false;
        foreach (var (id, height) in heights ?? [])
        {
            if (!double.IsFinite(height) || height < 0 || (_heights.TryGetValue(id, out var old) && old == height))
                continue;
            _heights[id] = height;
            changed = true;
        }

        if (changed)
            StateHasChanged();
    });

    private double HeightOf(NoteElement element) => MudExNotebookPageView.HeightOf(element, _heights);

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e) when (CommandFailed.HasDelegate)
        {
            await CommandFailed.InvokeAsync(e);
        }
    }

    private Task Guard(Func<Task> action) => InvokeAsync(() => RunAsync(action));

    private Task Guard(Action action) => Guard(() =>
    {
        action();
        return Task.CompletedTask;
    });

    private NoteElement Find(string id) => Page?.Elements.FirstOrDefault(e => e.Id == id);

    private void Execute(IDocumentCommand<Notebook> command)
    {
        Notebook.Execute(command);
        StateHasChanged();
    }

    private void Move(Dictionary<string, (double X, double Y)> positions)
    {
        var changes = new Dictionary<string, (double X, double Y)>();
        foreach (var (id, (x, y)) in positions)
            if (Find(id) is { } element && (Math.Max(0, x), Math.Max(0, y)) is var target && target != (element.X, element.Y))
                changes[id] = target;
        if (changes.Count > 0)
            Execute(new MoveElementsCommand(Page, changes));
    }

    private void Change(IEnumerable<string> ids)
    {
        var known = Page?.Elements.Select(e => e.Id).ToHashSet() ?? [];
        var next = ids.Distinct().Where(known.Contains).ToList();
        if (next.SequenceEqual(_selection))
            return;
        _selection = next;
        _raiseSelection = true;
    }

    private async Task RaiseSelectionAsync()
    {
        if (!_raiseSelection)
            return;
        _raiseSelection = false;
        await SelectionChanged.InvokeAsync(_selection);
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (!Editable || e.AltKey)
            return;
        if (_text != null)
        {
            if (e.Key == "Escape")
                await CommitTextAsync();
            return;
        }

        var ctrl = e.CtrlKey || e.MetaKey;
        var any = _selection.Count > 0;
        switch (e.Key)
        {
            case "ArrowLeft" or "ArrowRight" or "ArrowUp" or "ArrowDown" when any && !ctrl:
                var step = e.ShiftKey ? 10d : 1d;
                var (dx, dy) = e.Key switch { "ArrowLeft" => (-step, 0d), "ArrowRight" => (step, 0d), "ArrowUp" => (0d, -step), _ => (0d, step) };
                Move(SelectedElements.ToDictionary(el => el.Id, el => (el.X + dx, el.Y + dy)));
                break;
            case "Delete" or "Backspace" when any && !ctrl:
                Execute(new DeleteElementsCommand(Page, _selection.ToList()));
                await SelectAsync([]);
                break;
            case "d" or "D" when ctrl && any:
                var duplicate = new DuplicateElementsCommand(Page, _selection.ToList(), DuplicateOffset);
                Execute(duplicate);
                await SelectAsync(duplicate.Copies.Select(c => c.Id));
                break;
            case "a" or "A" when ctrl:
                await SelectAsync(Page.Elements.Select(el => el.Id));
                break;
            case "b" or "B" or "i" or "I" or "u" or "U" when ctrl && any:
                await FormatTextAsync(e.Key.ToLowerInvariant(), null);
                break;
            case "Tab" when !ctrl:
                await CycleAsync(e.ShiftKey ? -1 : 1);
                break;
            case "Escape" when any:
                await SelectAsync([]);
                break;
            case "Enter" or "F2" or { Length: 1 } when !ctrl && _selection is [var only] && Find(only) is TextElement text:
                await BeginTextAsync(Request(text));
                break;
            case "Enter" or "F2" when !ctrl && _selection is [var only] && Find(only) is TableElement:
                await EditCellAsync(only, 0, 0);
                break;
        }
    }

    private Task CycleAsync(int direction)
    {
        var elements = Page.Elements;
        var last = _selection.Count == 0 ? -1 : elements.ToList().FindIndex(e => e.Id == _selection[^1]);
        var index = last < 0 ? (direction > 0 ? 0 : -1) : last + direction;
        return SelectAsync(index >= 0 && index < elements.Count ? [elements[index].Id] : []);
    }

    private string FrameStyle(NoteElement element)
    {
        var zoom = EffectiveZoom;
        return $"left:{NotebookStyles.Num(element.X * zoom)}px;top:{NotebookStyles.Num(element.Y * zoom)}px;" +
               $"width:{NotebookStyles.Num(element.Width * zoom)}px;height:{NotebookStyles.Num(HeightOf(element) * zoom)}px";
    }

    private static string KindOf(NoteElement element) => element switch
    {
        TextElement or TableElement => "text",
        ImageElement => "image",
        OpaqueElement => "opaque",
        _ => "box"
    };

    private static string[] HandlesOf(NoteElement element) => element switch
    {
        TextElement or TableElement => TextHandles,
        OpaqueElement => [],
        _ => BoxHandles
    };

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await RunAsync(CommitTextAsync);
        await _js.DisposeAsync();
        await base.DisposeAsync();
    }
}

/// <summary>A rectangle reported by the browser after resizing an element, in page coordinates (CSS pixels at 100 % zoom).</summary>
public sealed class BoundsDto
{
    /// <summary>The left edge.</summary>
    public double X { get; set; }

    /// <summary>The top edge.</summary>
    public double Y { get; set; }

    /// <summary>The width.</summary>
    public double Width { get; set; }

    /// <summary>The height.</summary>
    public double Height { get; set; }

    /// <summary>True if the user held Shift, so a picture is resized without keeping its aspect ratio.</summary>
    public bool Free { get; set; }
}

/// <summary>Asks the host to show the text editor for a block of a <see cref="MudExNotebookCanvas"/>.</summary>
/// <param name="ElementId">The id of the text block or table; for a new block the id it gets when its text is committed.</param>
/// <param name="X">The left edge of the block in page coordinates.</param>
/// <param name="Y">The top edge of the block in page coordinates.</param>
/// <param name="IsNew">True if the block does not exist yet; it is added only if text is written.</param>
public sealed record NotebookTextEditRequest(string ElementId, double X, double Y, bool IsNew)
{
    /// <summary>The row of the edited cell if <see cref="ElementId"/> is a table, otherwise null.</summary>
    public int? Row { get; init; }

    /// <summary>The column of the edited cell if <see cref="ElementId"/> is a table, otherwise null.</summary>
    public int? Column { get; init; }
}
