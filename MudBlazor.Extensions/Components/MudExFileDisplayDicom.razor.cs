using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;
using Nextended.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>Viewer for uncompressed little-endian DICOM images with common patient and study metadata.</summary>
public partial class MudExFileDisplayDicom : IMudExFileDisplay
{
    private object _loadedSource;
    private DicomFile _dicom;
    private MemoryStream _previewStream;
    private IMudExFileDisplayInfos _preview;
    private bool _loading;
    private string _errorMessage;
    [Inject] private MudExFileService FileService { get; set; }
    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayDicom);
    /// <inheritdoc />
    public int RenderPriority => 110;
    /// <inheritdoc />
    [Parameter] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }
    /// <summary>Reference to the parent file display.</summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos infos, IMudExFileService service)
        => Task.FromResult(Path.GetExtension(infos?.FileName ?? "").Equals(".dcm", StringComparison.OrdinalIgnoreCase)
                           || MimeType.Matches(infos?.ContentType, "application/dicom", "application/dicom+json"));

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos infos)
        => Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            ["Patient"] = _dicom?.PatientName, ["Modality"] = _dicom?.Modality, ["Study"] = _dicom?.StudyDescription,
            ["Dimensions"] = _dicom == null ? null : $"{_dicom.Columns} × {_dicom.Rows}", ["Bits"] = _dicom?.BitsAllocated,
            ["Photometric interpretation"] = _dicom?.PhotometricInterpretation, ["Transfer syntax"] = _dicom?.TransferSyntax
        });

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var changed = infos.SourceChanged(ref _loadedSource); await base.SetParametersAsync(parameters); if (!changed) return;
        Clear(); _loading = true; _errorMessage = null;
        try
        {
            await using var stream = await FileService.ReadStreamAsync(FileDisplayInfos);
            using var copy = new MemoryStream(); await stream.CopyToAsync(copy);
            _dicom = DicomFile.Read(copy.ToArray());
            _previewStream = new MemoryStream(_dicom.RenderPng(), writable:false);
            _preview = new DicomPreview(Path.ChangeExtension(FileDisplayInfos.FileName, ".png"), _previewStream);
        }
        catch (Exception e) { _errorMessage = e.Message; MudExFileDisplay?.ShowError(e.Message); }
        finally { _loading = false; StateHasChanged(); }
        await FileDisplayInfos.NotifyMetaChangedAsync();
    }

    private void Clear() { _preview = null; _previewStream?.Dispose(); _previewStream = null; _dicom = null; }
    /// <inheritdoc />
    public override async ValueTask DisposeAsync() { Clear(); await base.DisposeAsync(); }
    private sealed record DicomPreview(string FileName, Stream ContentStream) : IMudExFileDisplayInfos { public string Url => null; public string ContentType => "image/png"; }
}
