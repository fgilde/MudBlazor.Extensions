using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

// The handlers sit on the wrapper RenderItemContent puts around each node's content, so every view mode
// gets drag and drop without opting in.
public abstract partial class MudExTreeViewBase<TItem>
{
    // One browser, one drag. Static per closed generic - the scope where dragging between two instances
    // makes sense.
    private static MudExTreeViewDragState<TItem> _activeDrag;

    private readonly Dictionary<string, TItem> _dndNodesByKey = new();
    private readonly Dictionary<TItem, string> _dndKeysByNode = new();
    private TItem _dragOverNode;
    private string _externalDropTargetKey;
    private InputFile _externalDropInput;
    private IJSObjectReference _externalDropJs;
    private DotNetObjectReference<MudExTreeViewBase<TItem>> _externalDropRef;

    /// <summary>
    /// Lets the user drag nodes out of this tree view.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowDrag { get; set; }

    /// <summary>
    /// Decides per node whether it may be dragged. Only consulted when <see cref="AllowDrag"/> is set.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public Func<TItem, bool> AllowDragFunc { get; set; }

    /// <summary>
    /// Lets the user drop nodes, and files from outside the browser, onto this tree view.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowDrop { get; set; }

    /// <summary>
    /// Decides whether a specific drop is allowed. Runs after the built-in checks, which already refuse a drop
    /// on the dragged node itself and a drop into the dragged node's own subtree.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public Func<MudExTreeViewDropInfo<TItem>, bool> CanDropFunc { get; set; }

    /// <summary>
    /// Two tree views accept each other's nodes only when their scope matches. Change it to keep unrelated
    /// trees on the same page from exchanging nodes.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public string DragScope { get; set; } = "mud-ex-tree-view";

    /// <summary>
    /// Raised when a node was dropped on this tree view and every check allowed it. Moving the node is the
    /// consumer's job: the tree view does not touch the structure.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public EventCallback<MudExTreeViewDropInfo<TItem>> OnNodeDropped { get; set; }

    /// <summary>
    /// Raised when files from outside the browser were dropped on a node.
    /// </summary>
    /// <remarks>Files only - a dropped folder arrives as nothing. Use <see cref="MudExUploadEdit"/> for those.</remarks>
    [Parameter, SafeCategory("Behavior")]
    public EventCallback<MudExTreeViewExternalDropInfo<TItem>> OnExternalFilesDropped { get; set; }

    /// <summary>
    /// Css class put on the wrapper of the node the pointer currently hovers during a drag.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string DragOverClass { get; set; } = "mud-ex-tree-view-drag-over";

    /// <summary>The node the pointer currently hovers during a drag, if any.</summary>
    public TItem DragOverNode => _dragOverNode;

    /// <summary>True while a drag started from any tree view of this node type is in progress.</summary>
    public bool IsDragging => _activeDrag != null;

    /// <summary>Returns true when the given node may be dragged.</summary>
    public bool CanDrag(TItem node) =>
        AllowDrag && node != null && (AllowDragFunc?.Invoke(node) ?? true);

    /// <summary>
    /// Returns true when the drag in progress may be dropped on the given node. <c>null</c> asks for the tree
    /// itself, which means the root.
    /// </summary>
    public bool CanDrop(TItem node)
    {
        if (!AllowDrop || _activeDrag == null)
            return false;
        if (!string.Equals(_activeDrag.Scope, DragScope, StringComparison.Ordinal))
            return false;

        var info = BuildDropInfo(node);

        // Not on itself and not into its own subtree. Path() is the node plus its ancestors, so one check does both.
        if (node != null && node.Path().Contains(info.DraggedNode))
            return false;

        return CanDropFunc?.Invoke(info) ?? true;
    }

    private MudExTreeViewDropInfo<TItem> BuildDropInfo(TItem target) => new()
    {
        DraggedNode = _activeDrag.Node,
        TargetNode = target,
        SourceTreeView = _activeDrag.Source,
        TargetTreeView = this,
        DragScope = _activeDrag.Scope
    };

    /// <summary>Starts a drag for the given node.</summary>
    protected virtual void OnNodeDragStart(TItem node)
    {
        if (!CanDrag(node))
            return;
        _activeDrag = new MudExTreeViewDragState<TItem> { Node = node, Source = this, Scope = DragScope };
    }

    /// <summary>Ends the current drag, whether it was dropped or cancelled.</summary>
    protected virtual void OnNodeDragEnd()
    {
        _activeDrag = null;
        _dragOverNode = default;
        Update();
    }

    /// <summary>Marks the given node as the current drop candidate.</summary>
    protected virtual void OnNodeDragOver(TItem node)
    {
        if (!CanDrop(node) || ReferenceEquals(_dragOverNode, node))
            return;
        _dragOverNode = node;
        Update();
    }

    /// <summary>Clears the drop candidate when the pointer leaves the node.</summary>
    protected virtual void OnNodeDragLeave(TItem node)
    {
        if (!ReferenceEquals(_dragOverNode, node))
            return;
        _dragOverNode = default;
        Update();
    }

    /// <summary>Completes a node drop.</summary>
    protected virtual async Task OnNodeDrop(TItem node)
    {
        _dragOverNode = default;

        // External file drops raise this too and have no active drag - the hidden file input handles those.
        if (!CanDrop(node))
        {
            _activeDrag = null;
            Update();
            return;
        }

        var info = BuildDropInfo(node);
        _activeDrag = null;
        await OnNodeDropped.InvokeAsync(info);
        Update();
    }

    /// <summary>
    /// Stable per-node key for a data attribute, so a drop from JavaScript maps back to its node.
    /// </summary>
    protected string DragDropKey(TItem node)
    {
        if (node == null)
            return null;
        if (_dndKeysByNode.TryGetValue(node, out var existing))
            return existing;

        var key = $"n{_dndKeysByNode.Count + 1}";
        _dndKeysByNode[node] = key;
        _dndNodesByKey[key] = node;
        return key;
    }

    /// <summary>Whether the drag and drop wrapper is needed at all.</summary>
    protected bool HasDragDrop => AllowDrag || AllowDrop;

    /// <summary>Css class of a node's drag and drop wrapper.</summary>
    protected virtual string DragDropClassStr(TreeViewItemContext<TItem> context) => MudExCssBuilder
        .From("mud-ex-tree-view-dnd")
        .AddClass(DragOverClass, ReferenceEquals(_dragOverNode, context.Value))
        .ToString();

    /// <summary>
    /// Inline style of the wrapper. Inline so it works without a stylesheet; override with
    /// <see cref="DragOverClass"/>.
    /// </summary>
    protected virtual string DragDropStyleStr(TreeViewItemContext<TItem> context) =>
        ReferenceEquals(_dragOverNode, context.Value)
            ? "outline: 2px dashed var(--mud-palette-primary); outline-offset: -2px; border-radius: 4px;"
            : null;

    private bool ListensForExternalFiles => AllowDrop && OnExternalFilesDropped.HasDelegate;

    /// <summary>
    /// Called from JavaScript right before the dropped files reach the hidden file input, so the change event
    /// that follows knows which node they belong to.
    /// </summary>
    [JSInvokable]
    public void SetExternalDropTargetKey(string key) => _externalDropTargetKey = key;

    private async Task OnExternalFilesSelected(InputFileChangeEventArgs args)
    {
        var key = _externalDropTargetKey;
        _externalDropTargetKey = null;

        var target = key != null && _dndNodesByKey.TryGetValue(key, out var node) ? node : default;
        var files = args.FileCount == 0 ? Array.Empty<IBrowserFile>() : args.GetMultipleFiles(args.FileCount);
        if (files.Count == 0)
            return;

        await OnExternalFilesDropped.InvokeAsync(new MudExTreeViewExternalDropInfo<TItem>
        {
            TargetNode = target,
            Files = files,
            TreeView = this
        });
        Update();
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
            // No script means no external file drop; node dragging is pure Blazor and keeps working.
            _externalDropRef.Dispose();
            _externalDropRef = null;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        await EnsureExternalDropListenerAsync();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
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
}
