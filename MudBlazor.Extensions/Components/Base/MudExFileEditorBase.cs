using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components.Base;

/// <summary>
/// Base class for file editors. Provides dirty tracking and the save pipeline.
/// </summary>
/// <typeparam name="T">The editor type</typeparam>
public abstract class MudExFileEditorBase<T> : MudExBaseComponent<T>, IMudExFileEditor where T : MudExBaseComponent<T>
{
    private string _handleId;
    private int _editVersion;
    private bool _saving;

    [Inject] private MudExFileService FileService { get; set; }

    /// <summary>
    /// Infos of the file to edit
    /// </summary>
    [Parameter] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// The surrounding file display, if the editor is rendered inside one
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <summary>
    /// If true the content cannot be changed
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// If true the editor may save to a local file when the host does not handle saving
    /// </summary>
    [Parameter] public bool AllowLocalSave { get; set; } = true;

    /// <summary>
    /// Raised before saving. The host can handle or cancel the save.
    /// </summary>
    [Parameter] public EventCallback<MudExFileSaveRequest> SaveRequested { get; set; }

    /// <summary>
    /// Raised after the content was saved
    /// </summary>
    [Parameter] public EventCallback<MudExFileSavedEventArgs> Saved { get; set; }

    /// <summary>
    /// Raised when <see cref="IsDirty"/> changes
    /// </summary>
    [Parameter] public EventCallback<bool> IsDirtyChanged { get; set; }

    /// <summary>
    /// Raised on every content change
    /// </summary>
    [Parameter] public EventCallback ContentChanged { get; set; }

    /// <inheritdoc />
    public bool IsDirty { get; private set; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<MudExFileFormat> SaveFormats { get; }

    /// <inheritdoc />
    public abstract Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService);

    /// <inheritdoc />
    public virtual Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
        => Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>());

    /// <inheritdoc />
    public abstract Task<MudExFileContent> ExportAsync(MudExFileFormat format = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the file into the editor
    /// </summary>
    /// <param name="fileDisplayInfos">Infos of the file</param>
    protected abstract Task LoadAsync(IMudExFileDisplayInfos fileDisplayInfos);

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var previous = FileDisplayInfos;
        await base.SetParametersAsync(parameters);
        if (ReferenceEquals(previous, FileDisplayInfos))
            return;

        try
        {
            await ReleaseHandleAsync();
            await SetDirtyAsync(false);
            if (FileDisplayInfos != null)
                await LoadAsync(FileDisplayInfos);
        }
        catch (Exception e) when (MudExFileDisplay != null)
        {
            MudExFileDisplay.ShowError(e.Message);
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public async Task<bool> SaveAsync(bool saveAs = false)
    {
        if (_saving || IsDisposed)
            return false;

        _saving = true;
        try
        {
            var infos = FileDisplayInfos;
            var version = _editVersion;
            await using var content = await ExportAsync();
            var request = new MudExFileSaveRequest { Content = content, SaveAs = saveAs };
            await SaveRequested.InvokeAsync(request);
            if (request.Cancel)
                return false;

            string fileName;
            MudExFileSaveTarget target;
            string oldHandle = null;
            if (request.Handled)
            {
                fileName = content.FileName;
                target = MudExFileSaveTarget.Host;
            }
            else if (AllowLocalSave)
            {
                var result = await FileService.SaveLocalFileAsync(content, saveAs ? null : _handleId);
                if (result == null)
                    return false;
                if (IsDisposed || !ReferenceEquals(infos, FileDisplayInfos))
                {
                    await TryReleaseAsync(result.HandleId);
                    return false;
                }

                if (_handleId != null && result.HandleId != _handleId)
                    oldHandle = _handleId;
                _handleId = result.HandleId;
                fileName = result.FileName;
                target = result.Target;
            }
            else
                return false;

            if (!ReferenceEquals(infos, FileDisplayInfos))
                return false;

            if (version == _editVersion)
                await MarkCleanAsync();
            await Saved.InvokeAsync(new MudExFileSavedEventArgs(fileName, target));
            if (oldHandle != null)
                await TryReleaseAsync(oldHandle);
            return true;
        }
        catch (Exception e) when (MudExFileDisplay != null)
        {
            MudExFileDisplay.ShowError(e.Message);
            return false;
        }
        finally
        {
            _saving = false;
        }
    }

    /// <summary>
    /// Marks the content as changed
    /// </summary>
    protected async Task MarkDirtyAsync()
    {
        _editVersion++;
        await SetDirtyAsync(true);
        await ContentChanged.InvokeAsync();
    }

    /// <summary>
    /// Marks the content as saved
    /// </summary>
    protected Task MarkCleanAsync() => SetDirtyAsync(false);

    private async Task SetDirtyAsync(bool dirty)
    {
        if (IsDirty == dirty)
            return;
        IsDirty = dirty;
        await IsDirtyChanged.InvokeAsync(dirty);
    }

    private async Task ReleaseHandleAsync()
    {
        var handle = _handleId;
        _handleId = null;
        if (handle != null)
            await TryReleaseAsync(handle);
    }

    private async Task TryReleaseAsync(string handle)
    {
        try { await FileService.ReleaseLocalFileAsync(handle); }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        catch (ObjectDisposedException) { }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        try { await ReleaseHandleAsync(); }
        finally { await base.DisposeAsync(); }
    }
}
