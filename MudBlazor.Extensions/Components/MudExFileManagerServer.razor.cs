using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A <see cref="MudExFileManager"/> on a server directory, configured with nothing but a base url.
/// </summary>
/// <remarks>
/// This is the web component friendly half of the file manager: every parameter is a string, a number, a bool
/// or an enum, so <c>&lt;mudex-file-manager-server base-url="/api/files"&gt;</c> is a working file browser
/// without a Blazor project. <see cref="MudExFileManager"/> itself takes an
/// <see cref="IMudExFileStructureManager"/> instance, which html cannot pass.
/// </remarks>
public partial class MudExFileManagerServer
{
    private MudExHttpFileStructureManager _manager;
    private string _configuredFor;

    /// <summary>Base url every route is resolved against, for example <c>/api/files</c>. Required.</summary>
    [Parameter, SafeCategory("Data")]
    public string BaseUrl { get; set; }

    /// <summary>Route of the directory listing, appended to <see cref="BaseUrl"/>. Receives <c>?path=</c>.</summary>
    [Parameter, SafeCategory("Data")]
    public string ListRoute { get; set; } = string.Empty;

    /// <summary>Route of the file content, appended to <see cref="BaseUrl"/>. Receives <c>?path=</c>.</summary>
    [Parameter, SafeCategory("Data")]
    public string ContentRoute { get; set; } = "/content";

    /// <summary>Route that creates a directory.</summary>
    [Parameter, SafeCategory("Data")]
    public string CreateFolderRoute { get; set; } = "/folder";

    /// <summary>Route that renames an entry.</summary>
    [Parameter, SafeCategory("Data")]
    public string RenameRoute { get; set; } = "/rename";

    /// <summary>Route that moves entries.</summary>
    [Parameter, SafeCategory("Data")]
    public string MoveRoute { get; set; } = "/move";

    /// <summary>Route that deletes entries.</summary>
    [Parameter, SafeCategory("Data")]
    public string DeleteRoute { get; set; } = string.Empty;

    /// <summary>Route that accepts a new file.</summary>
    [Parameter, SafeCategory("Data")]
    public string UploadRoute { get; set; } = "/upload";

    /// <summary>
    /// Whether the endpoints can change the structure. False keeps the manager read-only, which is the safe
    /// default for a browser that was handed a base url and nothing else.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowWrite { get; set; }

    /// <summary>Which panels the file manager shows.</summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExFileManagerPanels Panels { get; set; } = MudExFileManagerPanels.All;

    /// <summary>How the file area presents its entries.</summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExFileGridView FileGridView { get; set; } = MudExFileGridView.Tiles;

    /// <summary>Lets the user select more than one entry in the file area.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowMultiSelect { get; set; } = true;

    /// <summary>How the structure panel renders.</summary>
    [Parameter, SafeCategory("Appearance")]
    public TreeViewMode TreePanelViewMode { get; set; } = TreeViewMode.Default;

    /// <summary>The structure panel also lists files, not only directories.</summary>
    [Parameter, SafeCategory("Appearance")]
    public bool TreeShowsFiles { get; set; }

    /// <summary>Lets the user close a panel.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowClosePanels { get; set; }

    /// <summary>Whether the file area shows icons or the real content.</summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExFileManagerPreviewContent FilePreviewContent { get; set; } = MudExFileManagerPreviewContent.Icon;

    /// <summary>Shows the breadcrumb bar above the file area.</summary>
    [Parameter, SafeCategory("Appearance")]
    public bool ShowBreadcrumb { get; set; } = true;

    /// <summary>Shows the toolbar buttons that change the structure.</summary>
    [Parameter, SafeCategory("Appearance")]
    public bool ShowActions { get; set; } = true;

    /// <summary>Allows moving entries by dragging them.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowDragDrop { get; set; } = true;

    /// <summary>Allows the user to pop a panel out into its own window.</summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowPopout { get; set; } = true;

    /// <summary>Height of the whole component.</summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExSize<double> Height { get; set; } = "70vh";

    /// <summary>The provider this component built, for a consumer that wants to call it directly.</summary>
    public IMudExFileStructureManager Manager => _manager;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Rebuilding the provider resets the whole structure, so it only happens when the configuration that
        // shapes it actually changed - not on every parameter set.
        var signature = string.Join("|", BaseUrl, ListRoute, ContentRoute, CreateFolderRoute, RenameRoute,
            MoveRoute, DeleteRoute, UploadRoute, AllowWrite);
        if (_manager != null && signature == _configuredFor)
            return;

        _configuredFor = signature;
        _manager = new MudExHttpFileStructureManager(Get<HttpClient>() ?? new HttpClient())
        {
            BaseUrl = BaseUrl,
            ListRoute = ListRoute,
            ContentRoute = ContentRoute,
            CreateFolderRoute = CreateFolderRoute,
            RenameRoute = RenameRoute,
            MoveRoute = MoveRoute,
            DeleteRoute = DeleteRoute,
            UploadRoute = UploadRoute,
            Capabilities = AllowWrite
                ? MudExFileManagerCapabilities.All
                : MudExFileManagerCapabilities.Read | MudExFileManagerCapabilities.Download
        };
    }
}
