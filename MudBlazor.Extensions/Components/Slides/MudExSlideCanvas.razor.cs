using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The editing area of the slide editor: shows a slide at a zoom level with a selection layer above it.
/// Shapes are selected, moved, resized and rotated with the pointer, with smart guides while moving; the gestures run in the browser
/// and only their result reaches .NET as one undo step. The keyboard nudges, deletes, duplicates and cycles through the shapes; Tab past the
/// last shape leaves the canvas. The canvas does not listen to <see cref="Office.Presentation.Presentation.Changed"/>: a host that changes the
/// presentation itself renders the canvas again.
/// </summary>
public partial class MudExSlideCanvas
{
    private const long Nudge = 914400 / 8;
    private const long FineNudge = 914400 / 80;
    private static readonly string[] Handles = ["nw", "n", "ne", "e", "se", "s", "sw", "w"];

    private readonly EventCallback<KeyboardEventArgs> _keyDown;
    private readonly EventCallback<IReadOnlyList<SlideTextParagraphDto>> _textChanged;
    private readonly EventCallback<IReadOnlyList<SlideTextParagraphDto>> _textCommitted;
    private readonly EventCallback<(SlideTextParagraphDto[] Paragraphs, bool Back)> _textTabbed;
    private List<uint> _selection = [];
    private Slide _slide;
    private int _version;
    private bool _raiseSelection;
    private ElementReference _root;
    private IJSObjectReference _module;
    private IJSObjectReference _js;
    private DotNetObjectReference<MudExSlideCanvas> _dotnet;
    private bool _disposed;

    /// <summary>Creates the canvas.</summary>
    public MudExSlideCanvas()
    {
        _keyDown = new EventCallback<KeyboardEventArgs>(WithoutRender.Instance, (Func<KeyboardEventArgs, Task>)OnKeyDownAsync);
        _textChanged = new EventCallback<IReadOnlyList<SlideTextParagraphDto>>(WithoutRender.Instance, (Func<IReadOnlyList<SlideTextParagraphDto>, Task>)OnTextChangedAsync);
        _textCommitted = new EventCallback<IReadOnlyList<SlideTextParagraphDto>>(WithoutRender.Instance, (Func<IReadOnlyList<SlideTextParagraphDto>, Task>)OnTextCommittedAsync);
        _textTabbed = new EventCallback<(SlideTextParagraphDto[], bool)>(WithoutRender.Instance, (Func<(SlideTextParagraphDto[], bool), Task>)OnTextTabbedAsync);
    }

    /// <summary>The presentation the slide belongs to. Edits are executed on it.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The slide to edit. Changing it clears the selection.</summary>
    [Parameter] public Slide Slide { get; set; }

    /// <summary>The image URLs of the presentation.</summary>
    [Parameter] public SlideImageUrls Images { get; set; }

    /// <summary>The zoom factor, 1 is the actual size at 96 dpi. Zero or less (the default) fits the slide into the canvas.</summary>
    [Parameter] public double Zoom { get; set; }

    /// <summary>If true shapes can neither be selected nor changed.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Raised whenever the selected shapes or table cells change, also when the slide changes or selected shapes disappear.</summary>
    [Parameter] public EventCallback<IReadOnlyList<uint>> SelectionChanged { get; set; }

    /// <summary>Raised for keys the canvas does not handle itself, for example to switch slides.</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

    /// <summary>Raised when the user opens the context menu on the slide; the browser menu is suppressed then. The shape under the pointer is selected first.</summary>
    [Parameter] public EventCallback<MouseEventArgs> ContextMenu { get; set; }

    /// <summary>The ids of the selected shapes. A group and one of its descendants are never selected together.</summary>
    public IReadOnlyList<uint> Selection => _selection;

    private IEnumerable<SlideShape> SelectedShapes => _selection.Select(Find).Where(s => s != null);

    private string FrameStyle
    {
        get
        {
            var ratio = (Presentation.SlideWidth / (double)Presentation.SlideHeight).ToString(CultureInfo.InvariantCulture);
            return Zoom > 0
                ? $"width:{(Presentation.SlideWidth / 9525d * Zoom).ToString("0.##", CultureInfo.InvariantCulture)}px"
                : $"width:min(100cqw, 100cqh * {ratio})";
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ReferenceEquals(Slide, _slide) || ReadOnly)
        {
            CommitText();
            _insert = null;
            _slide = Slide;
            _version = Slide?.Version ?? 0;
            Change(_pendingSelection is { } pending && ReferenceEquals(pending.Slide, Slide) ? pending.Ids : []);
            if (Slide != null)
                _pendingSelection = null;
        }
        else if (Slide != null && Slide.Version != _version)
        {
            _version = Slide.Version;
            if (_text != null && !SlideShape.Flatten(Slide.Shapes).Contains(_text.Owner))
                _text = null;
            Change(_selection);
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await RaiseSelectionAsync();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!firstRender)
            return;
        try
        {
            await JsRuntime.InitializeMudBlazorExtensionsCoreAsync();
            var dotnet = DotNetObjectReference.Create(this);
            var (module, js) = await JsRuntime.ImportModuleAndCreateJsAsync<MudExSlideCanvas>(_root, dotnet);
            (_module, _js, _dotnet) = (module, js, dotnet);
            if (_disposed)
                await ReleaseJsAsync();
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    /// <summary>Selects shapes. Unknown ids and descendants of other selected shapes are ignored.</summary>
    /// <param name="ids">The shape ids.</param>
    /// <returns>A task that completes when the selection is shown.</returns>
    public async Task SelectAsync(IEnumerable<uint> ids)
    {
        if (ReadOnly)
            return;
        Change(ids);
        StateHasChanged();
        await RaiseSelectionAsync();
    }

    /// <summary>Called by the browser with the shapes the user selected by a marquee or by clicking the empty slide.</summary>
    /// <param name="ids">The shape ids.</param>
    /// <returns>A task that completes when the selection is shown.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnSelectionAsync(uint[] ids) => InvokeAsync(() => SelectAsync(ids ?? []));

    /// <summary>
    /// Called by the browser when the user clicked a shape. A click selects the outermost group of the shape, a click inside a selected group
    /// selects the next level below it. With <paramref name="toggle"/> the clicked shape is added to or removed from the selection.
    /// </summary>
    /// <param name="id">The id of the shape that was hit.</param>
    /// <param name="toggle">True if Shift or Ctrl was pressed.</param>
    /// <returns>The new selection.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<uint[]> OnShapeClickAsync(uint id, bool toggle)
    {
        await InvokeAsync(() => ReadOnly || Find(id) is not { } shape ? Task.CompletedTask : SelectAsync(Click(Path(shape), toggle)));
        return _selection.ToArray();
    }

    private IEnumerable<uint> Click(List<SlideShape> path, bool toggle)
    {
        var selected = path.FindLastIndex(s => _selection.Contains(s.Id));
        return (toggle, selected) switch
        {
            (true, >= 0) => _selection.Where(i => i != path[selected].Id).ToList(),
            (true, _) => _selection.Append(path[0].Id).ToList(),
            (false, < 0) => [path[0].Id],
            _ => [path[Math.Min(selected + 1, path.Count - 1)].Id]
        };
    }

    /// <summary>Called by the browser when the user moved, resized or rotated shapes. All changes are one undo step.</summary>
    /// <param name="transforms">The new transforms in slide coordinates (EMU) by shape id.</param>
    /// <returns>A task that completes when the change is executed.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnTransformsAsync(Dictionary<uint, ShapeTransformDto> transforms) => InvokeAsync(() =>
    {
        if (ReadOnly || Slide == null || transforms == null)
            return;
        var changes = new Dictionary<uint, ShapeTransform>();
        foreach (var (id, dto) in transforms)
        {
            if (dto == null || Find(id) is not { } shape)
                continue;
            var current = shape.ResolvedTransform;
            var target = current with
            {
                X = (long)Math.Round(dto.X),
                Y = (long)Math.Round(dto.Y),
                Width = Math.Max(1, (long)Math.Round(dto.Width)),
                Height = Math.Max(1, (long)Math.Round(dto.Height)),
                Rotation = shape is OpaqueShape or TableShape || Math.Abs(Angle(dto.Rotation - current.Rotation + 180) - 180) < 0.001 ? current.Rotation : Angle(dto.Rotation)
            };
            if (target != current)
                changes[id] = target;
        }

        if (changes.Count == 0)
            return;
        Presentation.Execute(new SetTransformsCommand(Slide, changes));
        StateHasChanged();
    });

    private SlideShape Find(uint id) => Slide == null ? null : SlideShape.Flatten(Slide.Shapes).FirstOrDefault(s => s.Id == id);

    private static List<SlideShape> Path(SlideShape shape)
    {
        var path = new List<SlideShape>();
        for (var s = shape; s != null; s = s.Parent)
            path.Insert(0, s);
        return path;
    }

    private static double Angle(double degrees) => (degrees % 360 + 360) % 360;

    private void Change(IEnumerable<uint> ids)
    {
        var shapes = ids.Distinct().Select(Find).Where(s => s != null).ToList();
        var next = shapes.Where(s => !Path(s).SkipLast(1).Any(shapes.Contains)).Select(s => s.Id).ToList();
        if (_cells is { } cells && !(next is [var only] && only == cells.Table && Find(only) is TableShape))
        {
            _cells = null;
            _raiseSelection = true;
        }

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
        if (!await HandleKeyAsync(e))
            await KeyDown.InvokeAsync(e);
    }

    private async Task<bool> HandleKeyAsync(KeyboardEventArgs e)
    {
        if (ReadOnly || Slide == null)
            return false;
        if (_text != null)
            return true;
        if (e.AltKey)
            return false;
        var ctrl = e.CtrlKey || e.MetaKey;
        if (await HandleCellKeyAsync(e, ctrl))
            return true;
        var any = _selection.Count > 0;
        switch (e.Key)
        {
            case "ArrowLeft" or "ArrowRight" or "ArrowUp" or "ArrowDown" when any:
                var step = ctrl ? FineNudge : Nudge;
                var (dx, dy) = e.Key switch { "ArrowLeft" => (-step, 0L), "ArrowRight" => (step, 0L), "ArrowUp" => (0L, -step), _ => (0L, step) };
                Presentation.Execute(new SetTransformsCommand(Slide, SelectedShapes.ToDictionary(s => s.Id, s => s.ResolvedTransform with
                {
                    X = s.ResolvedTransform.X + dx,
                    Y = s.ResolvedTransform.Y + dy
                })));
                StateHasChanged();
                return true;
            case "Delete" or "Backspace" when any:
                Presentation.Execute(new DeleteShapesCommand(Slide, _selection.ToList()));
                await SelectAsync([]);
                return true;
            case "d" or "D" when ctrl && any:
                var duplicate = new DuplicateShapesCommand(Slide, _selection.ToList(), Nudge);
                Presentation.Execute(duplicate);
                await SelectAsync(duplicate.Copies.Select(c => c.Id));
                return true;
            case "a" or "A" when ctrl:
                await SelectAsync(Slide.Shapes.Select(s => s.Id));
                return true;
            case "Tab" when !ctrl:
                await CycleAsync(e.ShiftKey ? -1 : 1);
                return true;
            case "Escape" when any:
                await SelectAsync([]);
                return true;
            default:
                return await HandleTextKeyAsync(e.Key, ctrl);
        }
    }

    private Task CycleAsync(int direction)
    {
        var last = _selection.Count == 0 ? null : Find(_selection[^1]);
        var siblings = last?.Parent?.Children ?? Slide.Shapes;
        var index = last == null ? (direction > 0 ? 0 : -1) : siblings.ToList().IndexOf(last) + direction;
        return SelectAsync(index >= 0 && index < siblings.Count ? [siblings[index].Id] : []);
    }

    private async Task ReleaseJsAsync()
    {
        var (js, module, dotnet) = (_js, _module, _dotnet);
        (_js, _module, _dotnet) = (null, null, null);
        if (js != null)
        {
            try { await js.InvokeVoidAsync("dispose"); } catch { }
            try { await js.DisposeAsync(); } catch { }
        }

        if (module != null)
            try { await module.DisposeAsync(); } catch { }
        dotnet?.Dispose();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        _disposed = true;
        CommitText();
        await ReleaseJsAsync();
        await base.DisposeAsync();
    }
}

/// <summary>A shape transform reported by the browser after a gesture on the <see cref="MudExSlideCanvas"/>, in slide coordinates (EMU).</summary>
public sealed class ShapeTransformDto
{
    /// <summary>Left edge of the unrotated box.</summary>
    public double X { get; set; }

    /// <summary>Top edge of the unrotated box.</summary>
    public double Y { get; set; }

    /// <summary>Width of the box.</summary>
    public double Width { get; set; }

    /// <summary>Height of the box.</summary>
    public double Height { get; set; }

    /// <summary>Clockwise rotation in degrees.</summary>
    public double Rotation { get; set; }
}
