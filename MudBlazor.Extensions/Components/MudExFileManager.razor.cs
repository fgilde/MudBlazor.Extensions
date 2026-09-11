using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Core.FileManager;
using Nextended.Core.Extensions;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// An explorer style file manager: a structure tree beside a file area, with breadcrumb navigation and
/// optional preview and metadata panels, laid out with <see cref="MudExDockLayout"/> so every panel can be
/// resized, stacked, floated or popped out.
/// </summary>
/// <remarks>
/// The structure comes from an <see cref="IMudExFileStructureManager"/>, so the same component browses an
/// in-memory tree, a folder on the user's machine or a server directory.
/// <para>
/// <see cref="SelectedNode"/> is the only navigation state. The structure tree, the file area and the
/// breadcrumb are three <see cref="MudExTreeView{T}"/> instances over the same root items and the same
/// selection, which is what keeps them in sync, and each of them lists its own level.
/// </para>
/// </remarks>
public partial class MudExFileManager
{
    private bool _ready;
    private string _errorMessage;
    private HashSet<MudExFileStructureNode> _root;
    private MudExFileStructureNode _currentDirectory;
    private readonly List<MudExFileStructureNode> _selectedNodes = new();
    private IDictionary<string, object> _selectedMeta;
    private Stream _previewStream;
    private readonly Dictionary<string, MudExFileManagerThumbnail> _contentPreviews = new(StringComparer.Ordinal);
    private long? _previewLength;
    private MudExDockLayout _dock;
    private MudExFileGrid _grid;

    /// <summary>
    /// Provides the structure to browse. Required.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public IMudExFileStructureManager Manager { get; set; }

    /// <summary>
    /// Which panels exist. Decided once, not at runtime - see <see cref="MudExFileManagerPanels"/>.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExFileManagerPanels Panels { get; set; } = MudExFileManagerPanels.All;

    /// <summary>
    /// How the file area presents its entries.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExFileGridView FileGridView { get; set; } = MudExFileGridView.Tiles;

    /// <summary>Raised when the user switches the file area between tiles and details.</summary>
    [Parameter] public EventCallback<MudExFileGridView> FileGridViewChanged { get; set; }

    /// <summary>Shows the button that switches the file area between tiles and details.</summary>
    [Parameter, SafeCategory("Appearance")]
    public bool ShowFileViewSwitcher { get; set; } = true;

    /// <summary>Lets the user select more than one entry in the file area.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowMultiSelect { get; set; } = true;

    /// <summary>Virtualizes the file area. Worth it for directories with hundreds of entries.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool VirtualizeFileArea { get; set; }

    /// <summary>
    /// Replaces the whole file area, and is handed the entries of the current directory. The default is a
    /// <see cref="MudExFileGrid"/>.
    /// </summary>
    [Parameter] public RenderFragment<IReadOnlyCollection<MudExFileStructureNode>> FileAreaTemplate { get; set; }

    /// <summary>Renders the name of one entry in the file area.</summary>
    [Parameter] public RenderFragment<MudExFileStructureNode> FileItemTemplate { get; set; }

    /// <summary>
    /// How the structure panel renders. Any tree view mode works, so the left panel can be a list or a card
    /// grid just as well as a tree.
    /// </summary>
    /// <remarks>Named apart from the <c>TreeViewMode</c> type so markup does not have to qualify it.</remarks>
    [Parameter, SafeCategory("Appearance")]
    public TreeViewMode TreePanelViewMode { get; set; } = TreeViewMode.Default;

    /// <summary>Raised when the user switches the structure panel's view mode.</summary>
    [Parameter] public EventCallback<TreeViewMode> TreePanelViewModeChanged { get; set; }

    /// <summary>
    /// The view modes the user may switch the structure panel between. One or none hides the switcher.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public TreeViewMode[] ToggleableTreeViewModes { get; set; } =
    {
        TreeViewMode.Default, TreeViewMode.List, TreeViewMode.FlatList
    };

    /// <summary>
    /// Shows the address bar above the file area: the structure as a breadcrumb, with a dropdown per level,
    /// the way an explorer does it. Always above the content, whatever the file area is showing.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool ShowBreadcrumb { get; set; } = true;

    /// <summary>
    /// Whether the file area shows an icon per entry or renders the real content. Content is expensive.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExFileManagerPreviewContent FilePreviewContent { get; set; } = MudExFileManagerPreviewContent.Icon;

    /// <summary>
    /// Upper bound on how many entries render real content when <see cref="FilePreviewContent"/> is
    /// <see cref="MudExFileManagerPreviewContent.Content"/>. Beyond it, entries fall back to an icon.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public int MaxContentPreviews { get; set; } = 24;

    /// <summary>
    /// The structure panel also lists files, not only directories.
    /// </summary>
    /// <remarks>
    /// Off by default: the left side navigates, the file area shows the contents. Turn it on for a manager
    /// without a file area, for example <see cref="Panels"/> of <c>Tree | Preview</c>.
    /// </remarks>
    [Parameter, SafeCategory("Appearance")]
    public bool TreeShowsFiles { get; set; }

    /// <summary>Allows the user to pop a panel out into its own window.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowPopout { get; set; } = true;

    /// <summary>Lets the user close a panel. Off by default - there is no way to bring one back.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowClosePanels { get; set; }

    /// <summary>Width of the tree panel in pixels.</summary>
    [Parameter, SafeCategory("Appearance")]
    public int TreePanelWidth { get; set; } = 260;

    /// <summary>Width of the preview and metadata panels in pixels.</summary>
    [Parameter, SafeCategory("Appearance")]
    public int SidePanelWidth { get; set; } = 380;

    /// <summary>Height of the whole component.</summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExSize<double> Height { get; set; } = "70vh";

    /// <summary>A previously saved dock layout to restore.</summary>
    [Parameter, SafeCategory("Behavior")]
    public string InitialLayoutJson { get; set; }

    /// <summary>
    /// Additional dock panels. They are rendered last, which is what keeps the dock's append-only rule intact.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// The selected entry, or the first of them. Selecting is not navigating - see
    /// <see cref="CurrentDirectory"/>.
    /// </summary>
    [Parameter]
    public MudExFileStructureNode SelectedNode
    {
        get => _selectedNodes.FirstOrDefault();
        set => _ = NavigateToAsync(value);
    }

    /// <summary>Raised when the selection changes.</summary>
    [Parameter] public EventCallback<MudExFileStructureNode> SelectedNodeChanged { get; set; }

    /// <summary>Every selected entry. The file area allows more than one.</summary>
    public IReadOnlyList<MudExFileStructureNode> SelectedNodes => _selectedNodes;

    /// <summary>Raised when the selection changes, with all of it.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<MudExFileStructureNode>> SelectedNodesChanged { get; set; }

    /// <summary>Raised when the current directory changes.</summary>
    [Parameter] public EventCallback<MudExFileStructureNode> CurrentDirectoryChanged { get; set; }

    /// <summary>Raised when a file is activated, by double click or by pressing enter.</summary>
    [Parameter] public EventCallback<MudExFileStructureNode> OnFileOpened { get; set; }

    /// <summary>
    /// The directory whose contents the file area shows. <c>null</c> is the root.
    /// </summary>
    /// <remarks>
    /// Its own state, not derived from the selection: with more than one entry selected there would be
    /// nothing to derive it from - three marked files are not a place.
    /// </remarks>
    public MudExFileStructureNode CurrentDirectory => _currentDirectory;

    /// <summary>
    /// The metadata the preview's viewer reported - the same set the info dialog shows.
    /// </summary>
    public IDictionary<string, object> SelectedFileMetaInformation => _selectedMeta;

    /// <summary>The dock layout, for saving and restoring the arrangement.</summary>
    public MudExDockLayout Dock => _dock;

    /// <summary>Saves the current panel arrangement.</summary>
    public Task<string> SaveLayoutAsync() => _dock != null ? _dock.SaveLayoutAsync() : Task.FromResult<string>(null);

    /// <summary>Restores a previously saved panel arrangement.</summary>
    public Task RestoreLayoutAsync(string json) => _dock != null ? _dock.RestoreLayoutAsync(json) : Task.CompletedTask;

    /// <summary>Reloads the structure from the manager, discarding every loaded node.</summary>
    public async Task RefreshAsync()
    {
        _currentDirectory = null;
        _selectedNodes.Clear();
        _selectedMeta = null;
        DisposePreviewStream();
        await LoadRootAsync();
        await EnsureContentPreviewsAsync();
        StateHasChanged();
    }

    private bool HasPanel(MudExFileManagerPanels panel) => Panels.HasFlag(panel);

    private bool ShowViewModeSwitcher => ShowFileViewSwitcher;

    /// <summary>The file area, for a consumer that wants its selection or wants to start a rename.</summary>
    public MudExFileGrid FileGrid => _grid;

    private void SetTreePanelViewMode(TreeViewMode mode)
    {
        TreePanelViewMode = mode;
        _ = TreePanelViewModeChanged.InvokeAsync(mode);
        StateHasChanged();
    }

    private async Task ToggleFileGridView()
    {
        FileGridView = FileGridView == MudExFileGridView.Tiles
            ? MudExFileGridView.Details
            : MudExFileGridView.Tiles;

        await FileGridViewChanged.InvokeAsync(FileGridView);
        StateHasChanged();
    }

    private bool ShowTreeViewModeSwitcher => ToggleableTreeViewModes is { Length: > 1 };

    private MudExFileManagerCapabilities Capabilities => Manager?.Capabilities ?? MudExFileManagerCapabilities.None;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadRootAsync();
        await EnsureContentPreviewsAsync();
    }

    private async Task LoadRootAsync()
    {
        _errorMessage = null;
        if (Manager == null)
        {
            _errorMessage = TryLocalize("No file structure manager was assigned.");
            _ready = true;
            return;
        }

        try
        {
            _root = await Manager.GetRootAsync();
        }
        catch (Exception e)
        {
            _errorMessage = $"{e.GetType().Name}: {e.Message}";
        }

        // The dock reads its layout once and adopts the rendered nodes, so not before the structure is there.
        _layoutJson = InitialLayoutJson ?? BuildDefaultLayoutJson();
        _ready = true;
    }

    /// <summary>The root items every panel works on.</summary>
    public IReadOnlyCollection<MudExFileStructureNode> RootItems => TreeItems;

    private IReadOnlyCollection<MudExFileStructureNode> TreeItems =>
        (IReadOnlyCollection<MudExFileStructureNode>)_root ?? Array.Empty<MudExFileStructureNode>();

    /// <summary>
    /// The entries of the current directory, for the entry count and the content preview budget. The file
    /// area gets the root items and lists the level itself.
    /// </summary>
    private IReadOnlyCollection<MudExFileStructureNode> CurrentEntries
    {
        get
        {
            var directory = CurrentDirectory;
            if (directory == null)
                return TreeItems;

            // GetLoadedChildren, not Children: reading Children starts a load, and counting must not.
            return directory.GetLoadedChildren()?.ToList() ?? (IReadOnlyCollection<MudExFileStructureNode>)Array.Empty<MudExFileStructureNode>();
        }
    }

    /// <summary>Hides files in the structure panel when <see cref="TreeShowsFiles"/> is off.</summary>
    private Func<MudExFileStructureNode, bool> TreeItemFilter
        => TreeShowsFiles ? null : IsDirectoryNode;

    /// <summary>The address bar shows where you are, so it lists directories only.</summary>
    private static bool IsDirectoryNode(MudExFileStructureNode node) => node.IsDirectory;

    private Func<MudExFileStructureNode, bool> IsDirectoryFilter => IsDirectoryNode;

    /// <summary>
    /// The one place navigation happens: every panel reports here and reads the result back.
    /// </summary>
    /// <remarks>
    /// A directory is entered, which clears the selection because the old one belonged to the level you just
    /// left. A file is selected, and the directory it lives in stays open.
    /// </remarks>
    public async Task NavigateToAsync(MudExFileStructureNode node)
    {
        if (node is { IsDirectory: false })
        {
            await SelectAsync(new[] { node });
            return;
        }

        if (ReferenceEquals(_currentDirectory, node))
            return;

        _currentDirectory = node;
        _selectedNodes.Clear();
        _selectedMeta = null;
        DisposePreviewStream();

        // Wait for the contents, or the file area renders an empty level and nothing brings it back.
        if (node != null)
            await EnsureChildrenLoadedAsync(node);

        await EnsureContentPreviewsAsync();
        await CurrentDirectoryChanged.InvokeAsync(node);
        await RaiseSelectionChangedAsync();
        StateHasChanged();
    }

    /// <summary>Sets the selection without leaving the current directory.</summary>
    public async Task SelectAsync(IReadOnlyCollection<MudExFileStructureNode> nodes)
    {
        nodes ??= Array.Empty<MudExFileStructureNode>();
        if (nodes.Count == _selectedNodes.Count && nodes.All(_selectedNodes.Contains))
            return;

        _selectedNodes.Clear();
        _selectedNodes.AddRange(nodes);
        _selectedMeta = null;
        DisposePreviewStream();

        // Only a single file has a preview - and it is a preview panel, not a batch operation.
        if (_selectedNodes is [{ IsDirectory: false } file])
            await LoadPreviewAsync(file);

        await RaiseSelectionChangedAsync();
        StateHasChanged();
    }

    private async Task RaiseSelectionChangedAsync()
    {
        await SelectedNodeChanged.InvokeAsync(SelectedNode);
        await SelectedNodesChanged.InvokeAsync(_selectedNodes.ToList());
    }

    /// <summary>
    /// Makes sure a directory's children are there before the panels render it. A load this starts is
    /// awaited; one the tree view already started while expanding is one-shot, so it can only be watched.
    /// </summary>
    private async Task EnsureChildrenLoadedAsync(MudExFileStructureNode directory)
    {
        if (directory.NeedsLoadChildren())
        {
            await directory.LoadChildren();
            return;
        }

        if (directory is not IAsyncHierarchical<MudExFileStructureNode> { IsLoading: true } loading)
            return;

        // ponytail: polling, because a load started elsewhere exposes no completion signal. Drop this the day
        // IAsyncHierarchical reports one.
        for (var i = 0; i < 200 && loading.IsLoading; i++)
            await Task.Delay(25);
    }

    private async Task LoadPreviewAsync(MudExFileStructureNode node)
    {
        _selectedMeta = new Dictionary<string, object>();

        if (!HasPanel(MudExFileManagerPanels.Preview) || Manager == null)
            return;

        try
        {
            _previewStream = await Manager.OpenReadAsync(node);
            _previewLength = _previewStream is { CanSeek: true } ? _previewStream.Length : null;
        }
        catch (Exception e)
        {
            // A provider is allowed to refuse; the preview panel says so rather than the whole component.
            _selectedMeta[TryLocalize("Preview")] = $"{e.GetType().Name}: {e.Message}";
        }
    }

    /// <summary>Goes up one level.</summary>
    public Task NavigateUpAsync() => NavigateToAsync(_currentDirectory?.Parent);

    /// <summary>
    /// Opening an entry - the double click. A directory is entered, a file raises <see cref="OnFileOpened"/>.
    /// </summary>
    public async Task OpenAsync(MudExFileStructureNode node)
    {
        if (node == null)
            return;

        await NavigateToAsync(node);
        if (!node.IsDirectory)
            await OnFileOpened.InvokeAsync(node);
    }

    /// <summary>What the structure panel highlights: the current directory, or a file when it lists files.</summary>
    private MudExFileStructureNode TreeSelectedNode
        => TreeShowsFiles && SelectedNode is { IsDirectory: false } ? SelectedNode : _currentDirectory;

    /// <summary>
    /// Shows what the preview's <see cref="MudExFileDisplay"/> reports - a handover, not a second collection.
    /// </summary>
    private void OnPreviewMetaChanged(IDictionary<string, object> meta)
    {
        _selectedMeta = meta;
        StateHasChanged();
    }

    /// <summary>Whether the file area renders real content instead of icons for the level it shows.</summary>
    private bool ShowsContentPreviews
        => FilePreviewContent == MudExFileManagerPreviewContent.Content
           && CurrentEntries.Count <= MaxContentPreviews;

    /// <summary>What a tile renders for an entry, or null when there is nothing to show but an icon.</summary>
    private MudExFileManagerThumbnail ContentPreviewOf(MudExFileStructureNode node)
        => node is { IsDirectory: false } && _contentPreviews.TryGetValue(node.FullPath ?? string.Empty, out var preview)
            ? preview
            : null;

    /// <summary>
    /// Reads the head of every file of the current level, so the tiles can show the file itself: the picture
    /// for an image, the first lines for anything textual, an icon for the rest.
    /// </summary>
    /// <remarks>
    /// Only the head is read and nothing is kept open - a thumbnail is a glance, not a viewer, and a level of
    /// entries must not cost a stream each. <see cref="MaxContentPreviews"/> caps how many are read at all.
    /// </remarks>
    private async Task EnsureContentPreviewsAsync()
    {
        DisposeContentPreviews();

        if (!ShowsContentPreviews || Manager == null)
            return;

        foreach (var node in CurrentEntries.Where(n => n is { IsDirectory: false, Size: > 0 }))
        {
            try
            {
                await using var stream = await Manager.OpenReadAsync(node);
                if (stream == null)
                    continue;

                var thumbnail = await MudExFileManagerThumbnail.ReadAsync(stream, node.ContentType, node.Name);
                if (thumbnail != null)
                    _contentPreviews[node.FullPath ?? string.Empty] = thumbnail;
            }
            catch (Exception)
            {
                // An entry a provider refuses simply keeps its icon.
            }
        }
    }

    private void DisposeContentPreviews() => _contentPreviews.Clear();

    private void DisposePreviewStream()
    {
        _previewStream?.Dispose();
        _previewStream = null;
        _previewLength = null;
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        DisposeContentPreviews();
        DisposePreviewStream();
        return base.DisposeAsync();
    }
}
