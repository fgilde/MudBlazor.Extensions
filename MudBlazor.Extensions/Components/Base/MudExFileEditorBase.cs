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
    private int _document;
    private bool _saving;
    private bool _replacing;
    private bool _foreign;

    /// <summary>
    /// Service to read, open and save files
    /// </summary>
    [Inject] protected MudExFileService FileService { get; set; }

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
    /// If true the user may open a local file into the editor
    /// </summary>
    [Parameter] public bool AllowOpen { get; set; } = true;

    /// <summary>
    /// If true the user may replace the content with a new empty document
    /// </summary>
    [Parameter] public bool AllowNew { get; set; } = true;

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
    public string FileName { get; private set; }

    /// <inheritdoc />
    public string ContentType { get; private set; }

    /// <inheritdoc />
    public event Action DocumentChanged;

    /// <inheritdoc />
    public long? FileSize { get; private set; }

    /// <summary>
    /// True if loading the current document failed
    /// </summary>
    protected bool LoadFailed { get; private set; }

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
    /// Formats offered when opening a local file. Empty accepts any file.
    /// </summary>
    protected virtual IReadOnlyList<MudExFileFormat> OpenFormats => SaveFormats;

    /// <summary>
    /// Creates the content of a new empty document
    /// </summary>
    /// <returns>The new content, or null if the editor does not support new documents</returns>
    protected virtual MudExFileContent CreateNew() => null;

    /// <summary>
    /// Loads a document into the editor
    /// </summary>
    /// <param name="content">The document. Its stream is disposed after this call.</param>
    protected abstract Task LoadContentAsync(MudExFileContent content);

    /// <summary>
    /// Loads the file of <see cref="FileDisplayInfos"/> into the editor
    /// </summary>
    /// <param name="fileDisplayInfos">Infos of the file</param>
    protected virtual async Task LoadAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        await using var content = new MudExFileContent
        {
            FileName = fileDisplayInfos.FileName,
            ContentType = fileDisplayInfos.ContentType,
            Stream = await FileService.ReadStreamAsync(fileDisplayInfos) ?? Stream.Null
        };
        await LoadDocumentAsync(content);
    }

    private Task LoadDocumentAsync(MudExFileContent content)
    {
        FileName = content.FileName;
        ContentType = content.ContentType;
        FileSize = content.Stream is { CanSeek: true } stream ? stream.Length : null;
        return LoadContentAsync(content);
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var previous = FileDisplayInfos;
        await base.SetParametersAsync(parameters);
        if (ReferenceEquals(previous, FileDisplayInfos))
            return;

        try
        {
            await ReplaceDocumentAsync(null, false, () => FileDisplayInfos != null ? LoadAsync(FileDisplayInfos) : Task.CompletedTask);
        }
        catch (Exception e) when (CanShowError)
        {
            await ShowErrorAsync(e.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> OpenAsync()
    {
        if (_replacing || !AllowOpen || IsDisposed)
            return false;

        _replacing = true;
        string handle = null;
        try
        {
            var file = await FileService.OpenLocalFileAsync(OpenFormats);
            if (file == null)
                return false;

            handle = file.HandleId;
            await using var content = new MudExFileContent { FileName = file.FileName, ContentType = file.ContentType, Stream = file.Content };
            if (IsDisposed || !await ConfirmReplaceAsync() || IsDisposed)
                return false;
            handle = null;
            await ReplaceDocumentAsync(file.HandleId, true, () => LoadDocumentAsync(content));
            return true;
        }
        catch (Exception e) when (CanShowError)
        {
            await ShowErrorAsync(e.Message);
            return false;
        }
        finally
        {
            _replacing = false;
            if (handle != null)
                await TryReleaseAsync(handle);
        }
    }

    /// <inheritdoc />
    public async Task<bool> NewAsync()
    {
        if (_replacing || !AllowNew || ReadOnly || IsDisposed)
            return false;

        _replacing = true;
        try
        {
            await using var content = CreateNew();
            if (content == null || !await ConfirmReplaceAsync() || IsDisposed)
                return false;
            await ReplaceDocumentAsync(null, true, () => LoadDocumentAsync(content));
            return true;
        }
        catch (Exception e) when (CanShowError)
        {
            await ShowErrorAsync(e.Message);
            return false;
        }
        finally
        {
            _replacing = false;
        }
    }

    private async Task<bool> ConfirmReplaceAsync()
    {
        if (!IsDirty)
            return true;
        if (MudExFileDisplay != null)
            return await MudExFileDisplay.ConfirmDiscardChangesAsync();
        return await Components.MudExFileDisplay.AskSaveChangesAsync(DialogService, s => TryLocalize(s)) switch
        {
            true => await SaveAsync(),
            false => true,
            null => false
        };
    }

    private async Task ReplaceDocumentAsync(string handleId, bool foreign, Func<Task> load)
    {
        _document++;
        _foreign = foreign;
        await ReleaseHandleAsync();
        _handleId = handleId;
        await SetDirtyAsync(false);
        LoadFailed = true;
        try
        {
            await load();
            LoadFailed = false;
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
            NotifyDocumentChanged(foreign);
        }
    }

    private void NotifyDocumentChanged(bool notifyDisplay)
    {
        DocumentChanged?.Invoke();
        if (notifyDisplay)
            MudExFileDisplay?.EditorDocumentChanged();
    }

    private bool CanShowError => MudExFileDisplay != null || DialogService != null;

    /// <summary>
    /// Shows an error in the surrounding <see cref="MudExFileDisplay"/>, or in a dialog when the editor is used on its own
    /// </summary>
    /// <param name="message">The message</param>
    protected async Task ShowErrorAsync(string message)
    {
        if (MudExFileDisplay != null)
            MudExFileDisplay.ShowError(message);
        else
            await DialogService.ShowInformationAsync(TryLocalize("Error"), message, Icons.Material.Filled.Error);
    }

    /// <inheritdoc />
    public Task<bool> SaveAsync(bool saveAs = false) => SaveCoreAsync(null, saveAs, false);

    /// <inheritdoc />
    public Task<bool> SaveCopyAsync(MudExFileFormat format) => SaveCoreAsync(format, true, true);

    private async Task<bool> SaveCoreAsync(MudExFileFormat format, bool saveAs, bool copy)
    {
        if (_saving || IsDisposed)
            return false;

        _saving = true;
        try
        {
            if (LoadFailed)
                throw new InvalidOperationException(TryLocalize("The file could not be loaded and cannot be saved."));

            var document = _document;
            var handle = _handleId;
            var version = _editVersion;
            var hostSaveAs = saveAs || _foreign;
            await using var content = await ExportAsync(format);
            if (document != _document || IsDisposed)
                return false;
            var request = new MudExFileSaveRequest { Content = content, SaveAs = hostSaveAs, IsCopy = copy };
            await SaveRequested.InvokeAsync(request);
            if (request.Cancel || document != _document || IsDisposed)
                return false;

            string fileName;
            MudExFileSaveTarget target;
            string oldHandle = null;
            if (request.Handled)
            {
                if (copy)
                    return true;
                fileName = content.FileName;
                target = MudExFileSaveTarget.Host;
            }
            else if (AllowLocalSave)
            {
                var result = await FileService.SaveLocalFileAsync(content, saveAs ? null : handle);
                if (result == null)
                    return false;
                if (copy || IsDisposed || document != _document)
                {
                    if (result.HandleId != _handleId)
                        await TryReleaseAsync(result.HandleId);
                    return copy;
                }

                if (handle != null && result.HandleId != handle)
                    oldHandle = handle;
                _handleId = result.HandleId;
                fileName = result.FileName;
                target = result.Target;
            }
            else
                return false;

            FileName = fileName;
            if (content.Stream is { CanSeek: true } stream)
                FileSize = stream.Length;
            if (version == _editVersion)
                await MarkCleanAsync();
            NotifyDocumentChanged(true);
            await Saved.InvokeAsync(new MudExFileSavedEventArgs(fileName, target));
            if (oldHandle != null)
                await TryReleaseAsync(oldHandle);
            return true;
        }
        catch (Exception e) when (CanShowError)
        {
            await ShowErrorAsync(e.Message);
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
