using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A <see cref="MudExFileManager"/> on a folder of the user's own machine.
/// </summary>
/// <remarks>
/// Feature detection picks the acquisition mode: with the File System Access API the folder is enumerated
/// lazily and can be written to, without it an <c>input webkitdirectory</c> selection is read once and stays
/// read-only. Every parameter is a primitive, so
/// <c>&lt;mudex-file-manager-client&gt;</c> works as a web component with no Blazor project.
/// </remarks>
public partial class MudExFileManagerClient
{
    private MudExClientFolderStructureManager _manager;
    private MudExFileManager _fileManager;
    private InputFile _folderInput;
    private bool _supportsFileSystemAccess;
    private bool _picked;
    private bool _busy;
    private string _errorMessage;

    /// <summary>Shows the toolbar that lets the user pick the folder. Off when the host does it instead.</summary>
    [Parameter, SafeCategory("Appearance")]
    public bool ShowPicker { get; set; } = true;

    /// <summary>
    /// Asks the browser for write access when picking. Only the File System Access API can grant it; the
    /// fallback stays read-only no matter what this says.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowWrite { get; set; } = true;

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

    /// <summary>
    /// The page a popped out panel opens. Hosts that do not serve the library's static assets point this at
    /// their own copy of it.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public string PopoutUrl { get; set; } = "_content/MudBlazor.Extensions/popout.html";

    /// <summary>Height of the whole component.</summary>
    [Parameter, SafeCategory("Appearance")]
    public MudExSize<double> Height { get; set; } = "70vh";

    /// <summary>Raised after a folder was picked, with the folder's name.</summary>
    [Parameter]
    public EventCallback<string> OnFolderPicked { get; set; }

    /// <summary>The provider this component built, for a consumer that wants to call it directly.</summary>
    public IMudExFileStructureManager Manager => _manager;

    /// <summary>True when the browser has the File System Access API.</summary>
    public bool SupportsFileSystemAccess => _supportsFileSystemAccess;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!firstRender)
            return;

        _manager = new MudExClientFolderStructureManager(JsRuntime);
        try
        {
            _supportsFileSystemAccess = await _manager.IsFileSystemAccessSupportedAsync();
        }
        catch (Exception)
        {
            // A browser that cannot answer is a browser without the api.
            _supportsFileSystemAccess = false;
        }

        StateHasChanged();
    }

    /// <summary>Opens the folder picker. Only works where <see cref="SupportsFileSystemAccess"/> is true.</summary>
    public async Task PickFolderAsync()
    {
        if (_manager == null)
            return;

        _errorMessage = null;
        _busy = true;
        StateHasChanged();

        try
        {
            // Dropping the old view before the new structure arrives: it belongs to the previous pick, and the
            // provider refuses its nodes from here on anyway.
            _picked = false;
            if (await _manager.PickFolderAsync(AllowWrite))
            {
                _picked = true;
                await OnFolderPicked.InvokeAsync(_manager.RootName);
            }
        }
        catch (Exception e)
        {
            _errorMessage = $"{e.GetType().Name}: {e.Message}";
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    private async Task FolderSelectedAsync(InputFileChangeEventArgs args)
    {
        if (_manager == null || args.FileCount == 0)
            return;

        _errorMessage = null;
        _busy = true;
        _picked = false;
        StateHasChanged();

        try
        {
            // IBrowserFile does not carry webkitRelativePath, so the paths come from the input element itself.
            // Both lists are in input.files order, which is what pairs them.
            var paths = await JsRuntime.InvokeAsync<FlatFile[]>(
                "MudExFileSystemAccess.relativePaths", _folderInput.Element);
            var files = args.GetMultipleFiles(args.FileCount);

            var entries = new List<MudExClientFolderStructureManager.FlatEntry>();
            for (var i = 0; i < files.Count; i++)
            {
                var path = i < (paths?.Length ?? 0) ? paths[i] : null;
                entries.Add(new MudExClientFolderStructureManager.FlatEntry
                {
                    RelativePath = path?.RelativePath ?? files[i].Name,
                    Size = files[i].Size,
                    LastModified = files[i].LastModified,
                    ContentType = files[i].ContentType,
                    File = files[i]
                });
            }

            await _manager.UseFlatFileListAsync(entries);
            _picked = true;
            await OnFolderPicked.InvokeAsync(_manager.RootName);
        }
        catch (Exception e)
        {
            _errorMessage = $"{e.GetType().Name}: {e.Message}";
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    private sealed class FlatFile
    {
        public string RelativePath { get; set; }
        public long Size { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public string ContentType { get; set; }
    }
}
