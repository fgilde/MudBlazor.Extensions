```razor
@using MudBlazor.Extensions.Core.Enums
@inherits ExampleBase

<MudText Typo="Typo.body2" Class="mb-3">
    @L["Drag a node onto a folder to move it - inside one tree, or from one tree into the other. Both trees share the same DragScope, which is what makes the second direction work. Files dragged in from your desktop are reported too."]
</MudText>

<MudToolBar Dense="true">
    <MudSwitch T="bool" @bind-Value="_foldersOnly" Color="Color.Primary" Label="@L["Only folders accept a drop"]" />
    <MudSpacer />
    <MudExEnumSelect Variant="Variant.Outlined" Margin="Margin.Dense" TEnum="TreeViewMode" @bind-Value="@_treeViewMode" Label="TreeViewMode" />
</MudToolBar>

<MudGrid>
    <MudItem xs="12" md="6">
        <MudText Typo="Typo.subtitle2" Class="mb-1">@L["Tree A"]</MudText>
        <MudPaper Style="width: 100%; min-height: 320px; max-height: 450px; overflow: auto;" Outlined="true">
            <MudExTreeView @ref="ComponentRef"
                           T="SampleTreeItem"
                           Items="@_left"
                           ViewMode="@_treeViewMode"
                           TextFunc="@(item => item.Name)"
                           AllowDrag="true"
                           AllowDrop="true"
                           DragScope="tree-demo"
                           CanDropFunc="@CanDrop"
                           OnNodeDropped="@(info => Move(info, _left))"
                           OnExternalFilesDropped="@(info => Dropped(info))" />
        </MudPaper>
    </MudItem>

    <MudItem xs="12" md="6">
        <MudText Typo="Typo.subtitle2" Class="mb-1">@L["Tree B"]</MudText>
        <MudPaper Style="width: 100%; min-height: 320px; max-height: 450px; overflow: auto;" Outlined="true">
            <MudExTreeView T="SampleTreeItem"
                           Items="@_right"
                           ViewMode="@_treeViewMode"
                           TextFunc="@(item => item.Name)"
                           AllowDrag="true"
                           AllowDrop="true"
                           DragScope="tree-demo"
                           CanDropFunc="@CanDrop"
                           OnNodeDropped="@(info => Move(info, _right))"
                           OnExternalFilesDropped="@(info => Dropped(info))" />
        </MudPaper>
    </MudItem>
</MudGrid>

@if (_log.Count > 0)
{
    <MudPaper Class="pa-2 mt-3" Outlined="true">
        @foreach (var line in _log)
        {
            <MudText Typo="Typo.caption" Class="d-block">@line</MudText>
        }
    </MudPaper>
}

@code {
    private TreeViewMode _treeViewMode = TreeViewMode.Default;
    private bool _foldersOnly = true;
    private readonly List<string> _log = new();

    private HashSet<SampleTreeItem> _left;
    private HashSet<SampleTreeItem> _right;

    protected override void OnInitialized()
    {
        _left = new HashSet<SampleTreeItem>
        {
            new SampleTreeItem("Inbox")
            {
                Children = new HashSet<SampleTreeItem>
                {
                    new SampleTreeItem("invoice.pdf"),
                    new SampleTreeItem("photo.png")
                }
            },
            new SampleTreeItem("Archive")
            {
                Children = new HashSet<SampleTreeItem> { new SampleTreeItem("2025") }
            },
            new SampleTreeItem("loose-note.txt")
        };

        _right = new HashSet<SampleTreeItem>
        {
            new SampleTreeItem("Projects")
            {
                Children = new HashSet<SampleTreeItem>
                {
                    new SampleTreeItem("mudex"),
                    new SampleTreeItem("playzor")
                }
            },
            new SampleTreeItem("Trash")
        };
    }

    // A node may never land on itself or inside its own subtree - the tree view checks that before asking, so
    // this only adds what the demo itself wants.
    private bool CanDrop(MudExTreeViewDropInfo<SampleTreeItem> info)
        => !_foldersOnly || info.TargetNode == null || info.TargetNode.HasChildren;

    private void Move(MudExTreeViewDropInfo<SampleTreeItem> info, HashSet<SampleTreeItem> root)
    {
        var node = info.DraggedNode;

        // Detach from wherever it is now. It may come from the other tree, so both roots are candidates.
        if (node.Parent != null)
            node.Parent.Children?.Remove(node);
        else if (!_left.Remove(node))
            _right.Remove(node);

        if (info.TargetNode != null)
        {
            info.TargetNode.Children ??= new HashSet<SampleTreeItem>();
            info.TargetNode.Children.Add(node);
            node.Parent = info.TargetNode;
        }
        else
        {
            // Dropped on the tree itself: the node becomes a root of the tree that received it.
            root.Add(node);
            node.Parent = null;
        }

        _log.Insert(0, $"{node.Name} -> {info.TargetNode?.Name ?? "root"}" +
                       (info.IsSameTreeView ? string.Empty : " (across trees)"));
        StateHasChanged();
    }

    private void Dropped(MudExTreeViewExternalDropInfo<SampleTreeItem> info)
    {
        var names = string.Join(", ", info.Files.Select(f => f.Name));
        _log.Insert(0, $"from the desktop onto {info.TargetNode?.Name ?? "root"}: {names}");
        StateHasChanged();
    }
}

```
