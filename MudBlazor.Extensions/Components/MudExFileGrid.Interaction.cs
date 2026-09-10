using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

// Pointer, drag and keyboard plumbing: the parts that talk to the browser.
public partial class MudExFileGrid
{
    private ElementReference _container;
    private MudMenu _contextMenu;
    private MudExFileStructureNode _dropTarget;
    private IJSObjectReference _gridJs;
    private DotNetObjectReference<MudExFileGrid> _gridRef;
    private IJSObjectReference _externalDropJs;
    private DotNetObjectReference<MudExFileGrid> _externalDropRef;
    private InputFile _externalDropInput;
    private string _externalDropTargetKey;

    // One drag at a time in a browser, and dragging between two grids is exactly the scope this covers.
    private static IReadOnlyCollection<MudExFileStructureNode> _dragged;

    /// <summary>Stable key per entry, so the rubber band can report what it touched.</summary>
    private static string Key(MudExFileStructureNode node) => node?.FullPath;

    private string Draggable(MudExFileStructureNode node) => AllowDragDrop ? "true" : "false";

    private static string ReadableSize(long size)
        => Nextended.Blazor.Extensions.BrowserFileExtensions.GetReadableFileSize(size);

    private string ItemClass(MudExFileStructureNode node, string baseClass) => MudExCssBuilder
        .From(baseClass)
        .AddClass("mud-ex-file-grid-item")
        .AddClass("mud-ex-file-grid-selected", IsSelected(node))
        .AddClass("mud-ex-file-grid-drop", ReferenceEquals(_dropTarget, node))
        .ToString();

    private async Task ContextMenuAsync(MudExFileStructureNode node, MouseEventArgs args)
    {
        if (!AllowContextMenu)
            return;

        // Right clicking outside the selection selects that entry first - acting on something the user cannot
        // see as selected is how a file manager deletes the wrong thing.
        if (!IsSelected(node))
            await ItemClickAsync(node, new MouseEventArgs());

        if (_contextMenu != null)
            await _contextMenu.OpenMenuAsync(args);
    }

    private async Task DragStartAsync(MudExFileStructureNode node)
    {
        if (!AllowDragDrop)
            return;

        // Dragging an entry outside the selection makes it the selection, so what moves is what is highlighted.
        if (!IsSelected(node))
            await ItemClickAsync(node, new MouseEventArgs());

        _dragged = _selected.ToList();
    }

    private void DragEnd()
    {
        _dragged = null;
        _dropTarget = null;
        StateHasChanged();
    }

    private Task DragOverAsync(MudExFileStructureNode node)
    {
        if (!AllowDragDrop)
            return Task.CompletedTask;

        var target = DropTargetFor(node);
        if (ReferenceEquals(_dropTarget, target))
            return Task.CompletedTask;

        _dropTarget = target;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void DragLeave(MudExFileStructureNode node)
    {
        if (!ReferenceEquals(_dropTarget, node))
            return;

        _dropTarget = null;
        StateHasChanged();
    }

    /// <summary>
    /// Only a directory takes a drop; dropping on a file, or on the free space, means this level.
    /// </summary>
    private static MudExFileStructureNode DropTargetFor(MudExFileStructureNode node)
        => node is { IsDirectory: true } ? node : null;

    private async Task DropAsync(MudExFileStructureNode node)
    {
        var target = DropTargetFor(node);
        _dropTarget = null;

        var dragged = _dragged;
        _dragged = null;

        if (!AllowDragDrop)
            return;

        // Files from outside the browser raise this too, without a drag of ours - the hidden input handles those.
        if (dragged is not { Count: > 0 })
        {
            StateHasChanged();
            return;
        }

        // Nothing to do when the entries are already there, and a directory cannot move into itself.
        var moving = dragged.Where(n => !ReferenceEquals(n, target)).ToList();
        if (moving.Count == 0 || (target != null && target.Path.Any(dragged.Contains)))
        {
            StateHasChanged();
            return;
        }

        await OnMoveRequested.InvokeAsync(new MudExFileGridMoveRequest { Nodes = moving, Target = target });
        StateHasChanged();
    }

    /// <summary>
    /// Called from JavaScript when a rubber band drag ended, with the keys it touched.
    /// </summary>
    [JSInvokable]
    public async Task RubberBandSelected(string[] keys, bool additive)
    {
        if (!AllowMultiSelect || !AllowRubberBand)
            return;

        var touched = new HashSet<string>(keys ?? Array.Empty<string>(), StringComparer.Ordinal);
        var hit = Items.Where(n => touched.Contains(Key(n)));

        await SetSelectionAsync(additive ? _selected.Concat(hit).Distinct() : hit);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        await EnsureExternalDropListenerAsync();

        // Also attached without a rubber band: the same script auto scrolls a long list while dragging.
        var rubberBand = AllowRubberBand && AllowMultiSelect;
        if (!firstRender || _gridJs != null || (!rubberBand && !AllowDragDrop))
            return;

        _gridRef = DotNetObjectReference.Create(this);
        try
        {
            _gridJs = await JsRuntime.InvokeAsync<IJSObjectReference>(
                "MudExFileGridSelection.attach", _container, _gridRef, rubberBand);
        }
        catch (Exception)
        {
            // Without the script there is no rubber band and no auto scrolling; clicking, ctrl, shift and
            // dragging itself are pure Blazor and stay.
            _gridRef.Dispose();
            _gridRef = null;
        }
    }

    private async Task EnsureExternalDropListenerAsync()
    {
        if (!ListensForExternalFiles || _externalDropJs != null || _externalDropInput == null)
            return;

        _externalDropRef = DotNetObjectReference.Create(this);
        try
        {
            _externalDropJs = await JsRuntime.InvokeAsync<IJSObjectReference>(
                "MudExExternalFileDrop.attach", ElementId, _externalDropInput.Element, _externalDropRef);
        }
        catch (Exception)
        {
            // No script means no drop from the desktop; dragging inside the grid is pure Blazor and stays.
            _externalDropRef.Dispose();
            _externalDropRef = null;
        }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (_gridJs != null)
        {
            try
            {
                await _gridJs.InvokeVoidAsync("dispose");
                await _gridJs.DisposeAsync();
            }
            catch (Exception)
            {
                // The page may be gone already, and then there is nothing left to detach from.
            }

            _gridJs = null;
        }

        _gridRef?.Dispose();
        _gridRef = null;

        if (_externalDropJs != null)
        {
            try
            {
                await _externalDropJs.InvokeVoidAsync("dispose");
                await _externalDropJs.DisposeAsync();
            }
            catch (Exception)
            {
                // ignore
            }

            _externalDropJs = null;
        }

        _externalDropRef?.Dispose();
        _externalDropRef = null;

        await base.DisposeAsync();
    }

    private bool ListensForExternalFiles => AllowDragDrop && OnExternalFilesDropped.HasDelegate;

    /// <summary>
    /// Called from JavaScript right before the dropped files reach the hidden input, so the change event that
    /// follows knows which entry they landed on.
    /// </summary>
    [JSInvokable]
    public void SetExternalDropTargetKey(string key) => _externalDropTargetKey = key;

    private async Task ExternalFilesDroppedAsync(InputFileChangeEventArgs args)
    {
        var key = _externalDropTargetKey;
        _externalDropTargetKey = null;

        var files = args.FileCount == 0 ? Array.Empty<IBrowserFile>() : args.GetMultipleFiles(args.FileCount);
        if (files.Count == 0)
            return;

        // Only a directory takes files; anything else means this level.
        var target = Items.FirstOrDefault(n => n.IsDirectory && Key(n) == key);

        await OnExternalFilesDropped.InvokeAsync(new MudExFileGridExternalDropRequest
        {
            Files = files,
            Target = target
        });
    }
}
