using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Excel file viewer using Univer spreadsheet library
/// </summary>
public partial class MudExFileDisplayExcelUniver : IMudExFileDisplay
{
    private readonly string _containerId = $"univer-{Guid.NewGuid().ToFormattedId()}";
    private bool _isLoading;
    private string _errorMessage;
    private byte[] _pendingBytes;

    [Inject] private MudExFileService FileService { get; set; }

    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => "Excel (Univer)";

    /// <summary>
    /// Not the default viewer for Excel files
    /// </summary>
    public bool StartsActive => true;

    /// <summary>
    /// The file display infos
    /// </summary>
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        return Task.FromResult(MimeType.Matches(fileDisplayInfos?.ContentType,
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml*",
            "application/vnd.ms-excel*"));
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(null);
    }

    /// <inheritdoc />
    public override object[] GetJsArguments() => new object[] { ElementReference, CreateDotNetObjectReference(), _containerId };

    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        // Temporarily disable AMD define to prevent UMD scripts from registering as AMD modules
        // instead of assigning to window globals. Blazor's loader.js provides an AMD define that
        // conflicts with React/Univer/LuckyExcel UMD bundles.
        await JsRuntime.InvokeVoidAsync("eval", new object[] { "window.__mudex_saved_define = window.define; window.define = undefined;" });

        try
        {
            // Step 1: Load React first (must be available before ReactDOM)
            await JsRuntime.LoadFilesAsync(
                "https://unpkg.com/react@18.3.1/umd/react.production.min.js"
            );
            await JsRuntime.WaitForNamespaceAsync("React", TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(200));

            // Step 2: Load ReactDOM (depends on React being initialized)
            await JsRuntime.LoadFilesAsync(
                "https://unpkg.com/react-dom@18.3.1/umd/react-dom.production.min.js"
            );
            await JsRuntime.WaitForNamespaceAsync("ReactDOM", TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(200));

            // Step 3: Load RxJS (independent, but sequential to avoid contention)
            await JsRuntime.LoadFilesAsync(
                "https://unpkg.com/rxjs@7.8.2/dist/bundles/rxjs.umd.min.js"
            );

            // Step 4: Load Univer presets (registers UniverCore, UniverPresets)
            await JsRuntime.LoadFilesAsync(
                "https://unpkg.com/@univerjs/presets@0.16.1/lib/umd/index.js"
            );
            await JsRuntime.WaitForNamespaceAsync("UniverPresets", TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(200));

            // Step 5: Load preset-sheets-core (depends on UniverCore from step 4)
            await JsRuntime.LoadFilesAsync(
                "https://unpkg.com/@univerjs/preset-sheets-core@0.16.1/lib/umd/index.js",
                "https://unpkg.com/@univerjs/preset-sheets-core@0.16.1/lib/index.css"
            );
            await JsRuntime.WaitForNamespaceAsync("UniverPresetSheetsCore", TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(200));

            // Step 6: Load locale (depends on UniverCore)
            await JsRuntime.LoadFilesAsync(
                "https://unpkg.com/@univerjs/preset-sheets-core@0.16.1/lib/umd/locales/en-US.js"
            );

            // Step 5: Load LuckyExcel for XLSX parsing
            await JsRuntime.LoadFilesAsync(
                "https://cdn.jsdelivr.net/npm/@zwight/luckyexcel/dist/luckyexcel.umd.min.js"
            );
        }
        finally
        {
            // Restore AMD define
            await JsRuntime.InvokeVoidAsync("eval", new object[] { "window.define = window.__mudex_saved_define; delete window.__mudex_saved_define;" });
        }

        await base.ImportModuleAndCreateJsAsync();

        if (_pendingBytes != null)
        {
            await LoadWorkbookInternalAsync(_pendingBytes);
            _pendingBytes = null;
        }
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var fileInfosUpdated = parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && FileDisplayInfos != fileDisplayInfos;
        await base.SetParametersAsync(parameters);

        if (fileInfosUpdated && fileDisplayInfos != null)
        {
            try
            {
                _errorMessage = null;
                _isLoading = true;
                StateHasChanged();

                byte[] bytes;
                if (fileDisplayInfos.ContentStream != null)
                {
                    bytes = fileDisplayInfos.ContentStream.ToByteArray();
                }
                else if (!string.IsNullOrEmpty(fileDisplayInfos.Url))
                {
                    var stream = await FileService.ReadStreamAsync(fileDisplayInfos.Url);
                    bytes = stream.ToByteArray();
                }
                else
                {
                    throw new ArgumentException("No stream and no url available");
                }

                if (JsReference != null)
                {
                    await LoadWorkbookInternalAsync(bytes);
                }
                else
                {
                    _pendingBytes = bytes;
                }
            }
            catch (Exception e)
            {
                _isLoading = false;
                _errorMessage = e.Message;
                MudExFileDisplay?.ShowError(e.Message);
                Console.WriteLine(e);
                StateHasChanged();
            }
        }
    }

    private async Task LoadWorkbookInternalAsync(byte[] bytes)
    {
        try
        {
            await JsReference.InvokeVoidAsync("loadWorkbook", bytes);
        }
        catch (Exception e)
        {
            _isLoading = false;
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
            Console.WriteLine(e);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Called from JS when the workbook is loaded
    /// </summary>
    [JSInvokable]
    public void OnWorkbookLoaded()
    {
        _isLoading = false;
        _errorMessage = null;
        CallStateHasChanged();
    }

    /// <summary>
    /// Called from JS when an error occurs
    /// </summary>
    [JSInvokable]
    public void OnError(string message)
    {
        _isLoading = false;
        _errorMessage = message;
        MudExFileDisplay?.ShowError(message);
        CallStateHasChanged();
    }
}
