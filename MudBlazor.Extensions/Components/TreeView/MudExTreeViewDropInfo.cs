using Microsoft.AspNetCore.Components.Forms;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Describes a node that was dragged onto another node inside a <see cref="MudExTreeView{T}"/>.
/// </summary>
public class MudExTreeViewDropInfo<TItem> where TItem : IHierarchical<TItem>
{
    /// <summary>The node the user dragged.</summary>
    public TItem DraggedNode { get; init; }

    /// <summary>
    /// The node it was dropped on. <c>null</c> means the drop happened on the tree itself, so the item is
    /// meant to move to the root.
    /// </summary>
    public TItem TargetNode { get; init; }

    /// <summary>The tree view the drag started in.</summary>
    public MudExTreeViewBase<TItem> SourceTreeView { get; init; }

    /// <summary>The tree view the drop happened in. Can be the same instance as <see cref="SourceTreeView"/>.</summary>
    public MudExTreeViewBase<TItem> TargetTreeView { get; init; }

    /// <summary>The <see cref="MudExTreeViewBase{TItem}.DragScope"/> both tree views agreed on.</summary>
    public string DragScope { get; init; }

    /// <summary>True when the drag started and ended in the same tree view instance.</summary>
    public bool IsSameTreeView => ReferenceEquals(SourceTreeView, TargetTreeView);
}

/// <summary>
/// Describes files dragged in from outside the browser and dropped on a node of a <see cref="MudExTreeView{T}"/>.
/// </summary>
public class MudExTreeViewExternalDropInfo<TItem> where TItem : IHierarchical<TItem>
{
    /// <summary>
    /// The node the files were dropped on. <c>null</c> means they were dropped on the tree itself, so they
    /// belong to the root.
    /// </summary>
    public TItem TargetNode { get; init; }

    /// <summary>The dropped files.</summary>
    public IReadOnlyList<IBrowserFile> Files { get; init; }

    /// <summary>The tree view that received the drop.</summary>
    public MudExTreeViewBase<TItem> TreeView { get; init; }
}

/// <summary>
/// The drag that is currently in progress. A browser only ever has one, so one slot per node type is enough -
/// which is also what makes dragging between two tree view instances work without any shared state to set up.
/// </summary>
internal sealed class MudExTreeViewDragState<TItem> where TItem : IHierarchical<TItem>
{
    public TItem Node { get; init; }
    public MudExTreeViewBase<TItem> Source { get; init; }
    public string Scope { get; init; }
}
