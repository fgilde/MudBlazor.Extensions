using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>Client-side tabular viewer for Apache Parquet, Arrow IPC and Feather files.</summary>
public partial class MudExFileDisplayColumnarData : IMudExFileDisplay
{
    private static readonly string[] Extensions = { ".parquet", ".arrow", ".feather", ".ipc" };
    private readonly string _containerId = $"columnar-{Guid.NewGuid().ToFormattedId()}";
    private object _loadedSource;
    private byte[] _pendingBytes;
    private string _pendingFormat;
    private bool _loading = true;
    private string _errorMessage;
    private int _rows;
    private int _columns;
    [Inject] private MudExFileService FileService { get; set; }
    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayColumnarData);
    /// <inheritdoc />
    public int RenderPriority => 110;
    /// <inheritdoc />
    [Parameter] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }
    /// <summary>Maximum number of rows rendered into the browser DOM.</summary>
    [Parameter] public int MaxRows { get; set; } = 5000;
    /// <summary>Reference to the parent file display.</summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos infos, IMudExFileService service)
        => Task.FromResult(Extensions.Contains(Path.GetExtension(infos?.FileName ?? ""), StringComparer.OrdinalIgnoreCase)
            || MimeType.Matches(infos?.ContentType, "application/vnd.apache.parquet", "application/vnd.apache.arrow.file", "application/vnd.apache.arrow.stream", "application/x-feather"));

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos infos)
        => Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        { ["Format"] = Path.GetExtension(infos?.FileName ?? "").TrimStart('.').ToUpperInvariant(), ["Rows"] = _rows, ["Columns"] = _columns, ["Rendered rows"] = Math.Min(_rows, MaxRows) });

    /// <inheritdoc />
    public override object[] GetJsArguments() => new object[] { ElementReference, CreateDotNetObjectReference(), _containerId };
    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        await base.ImportModuleAndCreateJsAsync();
        if (_pendingBytes != null) { await RenderAsync(_pendingBytes, _pendingFormat); _pendingBytes = null; _pendingFormat = null; }
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var changed = infos.SourceChanged(ref _loadedSource); await base.SetParametersAsync(parameters); if (!changed) return;
        _loading = true; _errorMessage = null; _rows = _columns = 0;
        try
        {
            await using var stream = await FileService.ReadStreamAsync(FileDisplayInfos);
            using var copy = new MemoryStream(); await stream.CopyToAsync(copy);
            var bytes = copy.ToArray(); var format = Path.GetExtension(FileDisplayInfos.FileName ?? "").TrimStart('.').ToLowerInvariant();
            if (JsReference == null) { _pendingBytes = bytes; _pendingFormat = format; } else await RenderAsync(bytes, format);
        }
        catch (Exception e) { OnError(e.Message); }
        StateHasChanged();
    }

    private Task RenderAsync(byte[] bytes, string format) => JsReference.InvokeVoidAsync("render", bytes, format, MaxRows).AsTask();
    /// <summary>Called when data has been parsed and rendered.</summary>
    [JSInvokable] public async Task OnRendered(int rows, int columns) { _rows = rows; _columns = columns; _loading = false; await FileDisplayInfos.NotifyMetaChangedAsync(); StateHasChanged(); }
    /// <summary>Called when parsing fails.</summary>
    [JSInvokable] public void OnError(string message) { _loading = false; _errorMessage = message; MudExFileDisplay?.ShowError(message); StateHasChanged(); }
}
