using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.FileManager;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

// Everything that changes the structure, funnelled through RunWriteAsync so a refusal shows as a message.
public partial class MudExFileManager
{
    private string _actionError;
    private bool _busy;

    /// <summary>
    /// Lets the user move entries by dragging, and add files dragged in from outside the browser.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowDragDrop { get; set; } = true;

    /// <summary>
    /// Shows the write actions in the toolbar. Each is still gated on the provider's capabilities.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public bool ShowActions { get; set; } = true;

    /// <summary>Raised after any operation that changed the structure.</summary>
    [Parameter]
    public EventCallback OnStructureChanged { get; set; }

    /// <summary>The provider's writer half, or null when it cannot change anything.</summary>
    private IMudExFileStructureWriter Writer => Manager as IMudExFileStructureWriter;

    private bool CanDo(MudExFileManagerCapabilities capability)
        => Writer != null && Capabilities.HasFlag(capability);

    private bool CanCreateDirectory => CanDo(MudExFileManagerCapabilities.CreateDirectory);
    private bool CanRename => CanDo(MudExFileManagerCapabilities.Rename) && _selectedNodes.Count == 1;
    private bool CanDelete => CanDo(MudExFileManagerCapabilities.Delete) && _selectedNodes.Count > 0;
    private bool CanUpload => CanDo(MudExFileManagerCapabilities.Upload);
    private bool CanMove => CanDo(MudExFileManagerCapabilities.Move);

    // One file: a download is a file, and a batch download would be a zip this component does not build.
    private bool CanDownload => Capabilities.HasFlag(MudExFileManagerCapabilities.Download)
                                && _selectedNodes is [{ IsDirectory: false }];

    private bool DragEnabled => AllowDragDrop && CanMove;
    private bool DropEnabled => AllowDragDrop && (CanMove || CanUpload);

    /// <summary>Creates a directory in the current directory, asking the user for its name.</summary>
    public async Task CreateDirectoryAsync()
    {
        if (!CanCreateDirectory || DialogService == null)
            return;

        var name = await DialogService.PromptAsync(
            TryLocalize("New folder"),
            TryLocalize("Name of the new folder"),
            initialValue: string.Empty,
            buttonOkText: TryLocalize("Create"),
            buttonCancelText: TryLocalize("Cancel"),
            icon: Icons.Material.Filled.CreateNewFolder,
            canConfirm: s => !string.IsNullOrWhiteSpace(s));

        if (string.IsNullOrWhiteSpace(name))
            return;

        await RunWriteAsync(() => Writer.CreateDirectoryAsync(CurrentDirectory, name));
    }

    /// <summary>Renames the selected entry, asking the user for the new name.</summary>
    public async Task RenameSelectedAsync()
    {
        if (!CanRename || DialogService == null)
            return;

        var node = SelectedNode;
        var name = await DialogService.PromptAsync(
            TryLocalize("Rename"),
            TryLocalize("New name for '{0}'", node.Name),
            initialValue: node.Name,
            buttonOkText: TryLocalize("Rename"),
            buttonCancelText: TryLocalize("Cancel"),
            icon: Icons.Material.Filled.DriveFileRenameOutline,
            canConfirm: s => !string.IsNullOrWhiteSpace(s));

        if (string.IsNullOrWhiteSpace(name) || name == node.Name)
            return;

        await RenameAsync(node, name);
    }

    /// <summary>Renames one entry. Used by the toolbar and by the file area's inline rename.</summary>
    public Task RenameAsync(MudExFileStructureNode node, string newName)
        => !CanDo(MudExFileManagerCapabilities.Rename) || node == null || string.IsNullOrWhiteSpace(newName)
            ? Task.CompletedTask
            : RunWriteAsync(() => Writer.RenameAsync(node, newName));

    /// <summary>Deletes the selection after asking for confirmation.</summary>
    public Task DeleteSelectedAsync() => DeleteAsync(_selectedNodes.ToList());

    /// <summary>Deletes the given entries after asking for confirmation.</summary>
    public async Task DeleteAsync(IReadOnlyCollection<MudExFileStructureNode> nodes)
    {
        if (!CanDo(MudExFileManagerCapabilities.Delete) || nodes is not { Count: > 0 } || DialogService == null)
            return;

        var confirmed = await DialogService.ShowConfirmationDialogAsync(new MessageBoxOptions
        {
            Title = TryLocalize("Delete"),
            Message = Message(),
            YesText = TryLocalize("Delete"),
            NoText = TryLocalize("Cancel")
        });

        if (!confirmed)
            return;

        await RunWriteAsync(() => Writer.DeleteAsync(nodes));
        return;

        string Message() => nodes.Count > 1
            ? TryLocalize("Delete these {0} entries and everything in them?", nodes.Count)
            : nodes.First().IsDirectory
                ? TryLocalize("Delete the folder '{0}' and everything in it?", nodes.First().Name)
                : TryLocalize("Delete the file '{0}'?", nodes.First().Name);
    }

    /// <summary>Downloads the selected file through the browser.</summary>
    public async Task DownloadSelectedAsync()
    {
        if (!CanDownload)
            return;

        var node = SelectedNode;
        _actionError = null;
        try
        {
            await using var stream = await Manager.OpenReadAsync(node);
            var fileService = Get<MudExFileService>();
            var url = fileService != null
                ? await fileService.ReadDataUrlForStreamAsync(stream, node.ContentType, useBlob: true)
                : null;

            if (string.IsNullOrEmpty(url))
            {
                _actionError = TryLocalize("'{0}' could not be prepared for download.", node.Name);
                return;
            }

            await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
            {
                Url = url,
                FileName = node.Name,
                MimeType = node.ContentType
            });
        }
        catch (Exception e)
        {
            _actionError = $"{e.GetType().Name}: {e.Message}";
        }
        finally
        {
            StateHasChanged();
        }
    }

    /// <summary>
    /// Applied to the <see cref="MudExUploadEdit{T}"/> of the upload dialog, so every one of its options -
    /// restrictions, archive content inspection, folder upload, the external providers - is yours to set.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public Action<MudExUploadEdit<UploadableFile>> UploadEditConfigure { get; set; }

    /// <summary>Options for the upload dialog itself.</summary>
    [Parameter, SafeCategory("Behavior")]
    public DialogOptionsEx UploadDialogOptions { get; set; }

    /// <summary>Opens the upload dialog for the current directory.</summary>
    public async Task ShowUploadDialogAsync()
    {
        if (!CanUpload || DialogService == null)
            return;

        var target = CurrentDirectory;
        var parameters = new DialogParameters
        {
            { nameof(MudExFileManagerUploadDialog.Restrictions), Restrictions },
            { nameof(MudExFileManagerUploadDialog.Configure), UploadEditConfigure },
            { nameof(MudExFileManagerUploadDialog.TargetName), target?.Name }
        };

        var dialog = await DialogService.ShowExAsync<MudExFileManagerUploadDialog>(
            TryLocalize("Upload"), parameters, UploadDialogOptions ?? DialogOptionsEx.DefaultDialogOptions);

        var result = await dialog.Result;
        if (result is { Canceled: false, Data: IReadOnlyList<IBrowserFile> files })
            await UploadFilesAsync(target, files);
    }

    /// <summary>
    /// What an incoming file has to satisfy. Applied on every path that adds one - the upload dialog and a
    /// file dropped straight onto a folder alike - and handed to the dialog's upload edit as well.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public MudExFileRestrictions Restrictions { get; set; }

    /// <summary>
    /// Adds the given files to the target directory, skipping any that <see cref="Restrictions"/> refuses.
    /// </summary>
    public async Task UploadFilesAsync(MudExFileStructureNode target, IReadOnlyCollection<IBrowserFile> files)
    {
        if (!CanUpload || files == null || files.Count == 0)
            return;

        var accepted = new List<IBrowserFile>();
        var refused = new List<string>();

        foreach (var file in files)
        {
            var reason = Restrictions?.Validate(file.Name, file.ContentType, file.Size);
            if (reason == null)
                accepted.Add(file);
            else
                refused.Add($"{file.Name}: {reason}");
        }

        // One bad file in a drop of twenty should not cost the other nineteen, so the rest still goes through
        // and the refused ones are named afterwards - RunWriteAsync clears the message on its way in.
        if (accepted.Count > 0)
            await RunWriteAsync(async () =>
            {
                foreach (var file in accepted)
                    await Writer.UploadAsync(target, file);
            });

        if (refused.Count > 0)
        {
            _actionError = string.Join(Environment.NewLine, refused);
            StateHasChanged();
        }
    }

    private Task HandleNodeDroppedAsync(MudExTreeViewDropInfo<MudExFileStructureNode> info)
    {
        // Dropping on a file means "next to this one".
        var target = info?.TargetNode is { IsDirectory: false } ? info.TargetNode.Parent : info?.TargetNode;
        return info?.DraggedNode == null ? Task.CompletedTask : MoveAsync(new[] { info.DraggedNode }, target);
    }

    /// <summary>Moves the given entries into a directory. <c>null</c> is the root.</summary>
    public Task MoveAsync(IReadOnlyCollection<MudExFileStructureNode> nodes, MudExFileStructureNode target)
    {
        if (!CanMove || nodes is not { Count: > 0 })
            return Task.CompletedTask;

        // Entries already in the target have nothing to do, and a directory cannot move into itself.
        var moving = nodes
            .Where(n => !ReferenceEquals(n.Parent, target) && !ReferenceEquals(n, target))
            .Where(n => target == null || !target.Path.Contains(n))
            .ToList();

        return moving.Count == 0 ? Task.CompletedTask : RunWriteAsync(() => Writer.MoveAsync(moving, target));
    }

    private Task HandleExternalFilesDroppedAsync(MudExTreeViewExternalDropInfo<MudExFileStructureNode> info)
    {
        if (info?.Files == null)
            return Task.CompletedTask;

        var target = info.TargetNode is { IsDirectory: false } ? info.TargetNode.Parent : info.TargetNode;
        return UploadFilesAsync(target, info.Files);
    }

    /// <summary>
    /// Runs a write and reloads from the root - a write invalidates its nodes, and a lazy node cannot be
    /// re-loaded in place.
    /// </summary>
    private async Task RunWriteAsync(Func<Task> action)
    {
        _actionError = null;
        _busy = true;
        StateHasChanged();

        try
        {
            await action();
            await RefreshAsync();
            await OnStructureChanged.InvokeAsync();
        }
        catch (Exception e)
        {
            _actionError = $"{e.GetType().Name}: {e.Message}";
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }
}
