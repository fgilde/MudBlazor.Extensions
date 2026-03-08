using BlazorJS;
using Gotho.BlazorPdf;
using Gotho.BlazorPdf.Config;
using Gotho.BlazorPdf.MudBlazor;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// PDF file viewer using Gotho.BlazorPdf with MudBlazor integration
/// </summary>
public partial class MudExFileDisplayPdf : IMudExFileDisplay
{
    private MudPdfViewer _pdfViewer;
    private BlazorPdfColors _colors = new();
    private int _totalPages;
    private int _currentPage;
    private bool _cssLoaded;

    [Inject] private MudExFileService FileService { get; set; }

    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => nameof(MudExFileDisplayPdf);

    /// <summary>
    /// Do not wrap in the default file display div to avoid layout conflicts
    /// </summary>
    public bool WrapInMudExFileDisplayDiv => true;

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
        var canHandle = fileDisplayInfos?.FileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true
                        || MimeType.Matches(fileDisplayInfos?.ContentType, "application/pdf");
        return Task.FromResult(canHandle);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Pages", _totalPages },
            { "Current Page", _currentPage }
        });
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var fileInfosUpdated = parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && FileDisplayInfos != fileDisplayInfos;
        await base.SetParametersAsync(parameters);

        if (fileInfosUpdated)
        {
            try
            {
                await LoadPdfAsync(fileDisplayInfos);
            }
            catch (Exception e)
            {
                MudExFileDisplay?.ShowError(e.Message);
                Console.WriteLine(e);
            }
        }
    }

    private async Task LoadPdfAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        if (_pdfViewer == null || fileDisplayInfos == null)
            return;

        var fileName = fileDisplayInfos.FileName ?? "document.pdf";

        if (fileDisplayInfos.ContentStream != null)
        {
            var bytes = fileDisplayInfos.ContentStream.ToByteArray();
            await _pdfViewer.LoadPdfAsync(bytes, fileName);
        }
        else if (!string.IsNullOrEmpty(fileDisplayInfos.Url))
        {
            await _pdfViewer.LoadPdfAsync(fileDisplayInfos.Url, fileName);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await EnsureCssLoadedAsync();
            await ApplyThemeBackgroundAsync();
            if (FileDisplayInfos != null && _pdfViewer != null)
            {
                try
                {
                    await LoadPdfAsync(FileDisplayInfos);
                }
                catch (Exception e)
                {
                    MudExFileDisplay?.ShowError(e.Message);
                    Console.WriteLine(e);
                }
            }
        }
    }

    private async Task EnsureCssLoadedAsync()
    {
        if (_cssLoaded) return;
        try
        {
            await JsRuntime.LoadFilesAsync(
                "_content/Gotho.BlazorPdf/blazorpdf.min.css",
                "_content/Gotho.BlazorPdf.MudBlazor/blazorpdf_mudblazor.min.css"
            );
            _cssLoaded = true;
        }
        catch
        {
            // CSS loading is best-effort
        }
    }

    private async Task ApplyThemeBackgroundAsync()
    {
        try
        {
            // MudPdfViewer handles Toolbar, Icons, and Loader natively via MudBlazor components.
            // Only the PDF canvas background needs explicit mapping.
            var background = await GetCssVariableAsync("--mud-palette-background");
            if (!string.IsNullOrEmpty(background))
            {
                _colors = new BlazorPdfColors { Background = background };
                StateHasChanged();
            }
        }
        catch
        {
            // Theme sync is best-effort
        }
    }

    private async Task<string> GetCssVariableAsync(string variableName)
    {
        return await JsRuntime.InvokeAsync<string>("eval",
            new object[] { $"getComputedStyle(document.documentElement).getPropertyValue('{variableName}').trim()" });
    }

    private void OnDocumentLoaded(PdfViewerEventArgs args)
    {
        _totalPages = args.TotalPages;
        _currentPage = args.CurrentPage;
    }

    private void OnPageChanged(PdfViewerEventArgs args)
    {
        _currentPage = args.CurrentPage;
        _totalPages = args.TotalPages;
    }
}
