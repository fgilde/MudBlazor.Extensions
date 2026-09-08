using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using Nextended.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A viewer for Jupyter Notebook files (.ipynb). Renders markdown cells, code cells (with syntax highlighting for the
/// notebook's kernel language) and their outputs (text/stream, images, html and errors) sequentially, just like
/// Jupyter itself does. Works fully client-side (including Blazor WebAssembly) since .ipynb is just a JSON document.
/// </summary>
public partial class MudExFileDisplayNotebook : IMudExFileDisplay
{
    private static readonly Regex AnsiEscapeRegex = new(@"\x1B\[[0-9;]*[a-zA-Z]", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> KernelLanguageToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        { "python", "py" },
        { "python3", "py" },
        { "csharp", "cs" },
        { "c#", "cs" },
        { "javascript", "js" },
        { "typescript", "ts" },
        { "r", "r" },
        { "bash", "sh" },
        { "sql", "sql" },
        { "go", "go" },
        { "rust", "rs" },
        { "ruby", "rb" },
        { "scala", "scala" },
        { "java", "java" },
        { "php", "php" },
        { "kotlin", "kt" }
    };

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayNotebook);

    /// <summary>
    /// The theme used to render code cells
    /// </summary>
    [Parameter]
    public CodeBlockTheme Theme { get; set; }

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    private List<NotebookCell> _cells;
    private string _errorMessage;
    private MudExCodeLanguage _language = MudExCodeLanguage.Python;
    private string _kernelDisplayName;

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var isExtensionMatch = fileDisplayInfos?.FileName?.EndsWith(".ipynb", StringComparison.OrdinalIgnoreCase) == true;
        var isContentTypeMatch = MimeType.Matches(fileDisplayInfos?.ContentType, "application/x-ipynb+json");
        return Task.FromResult(isExtensionMatch || isContentTypeMatch);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Kernel", _kernelDisplayName },
            { "Cells", _cells?.Count ?? 0 },
            { "Code cells", _cells?.Count(c => c.CellType == "code") ?? 0 },
            { "Markdown cells", _cells?.Count(c => c.CellType == "markdown") ?? 0 }
        });
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // See MudExFileDisplayDataBase for why we detect a usable source (Url/ContentStream) instead of just
        // comparing the FileDisplayInfos reference: when nested inside e.g. MudExFileDisplayZip the host object is
        // reused and only its Url/ContentStream get populated a moment later, so reference equality never changes.
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var hasSource = infos != null && (!string.IsNullOrEmpty(infos.Url) || infos.ContentStream is { Length: > 0 });
        var notYetLoaded = _cells == null && _errorMessage == null;

        await base.SetParametersAsync(parameters);

        if (hasSource && notYetLoaded)
            await LoadNotebookAsync();
    }

    private async Task LoadNotebookAsync()
    {
        try
        {
            var json = await FileService.ReadAsStringFromFileDisplayInfosAsync(FileDisplayInfos);
            if (string.IsNullOrEmpty(json))
                return;

            (_cells, _kernelDisplayName, _language) = ParseNotebook(json);
        }
        catch (Exception e)
        {
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
        }
        StateHasChanged();
    }

    private static (List<NotebookCell> Cells, string KernelDisplayName, MudExCodeLanguage Language) ParseNotebook(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var kernelLanguage = "python";
        string kernelDisplayName = null;
        if (root.TryGetProperty("metadata", out var meta))
        {
            if (meta.TryGetProperty("kernelspec", out var kernelSpec))
            {
                if (kernelSpec.TryGetProperty("language", out var langEl) && langEl.ValueKind == JsonValueKind.String)
                    kernelLanguage = langEl.GetString();
                if (kernelSpec.TryGetProperty("display_name", out var displayNameEl) && displayNameEl.ValueKind == JsonValueKind.String)
                    kernelDisplayName = displayNameEl.GetString();
            }
            else if (meta.TryGetProperty("language_info", out var langInfo) && langInfo.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
            {
                kernelLanguage = nameEl.GetString();
            }
        }

        var language = KernelLanguageToExtension.TryGetValue(kernelLanguage ?? string.Empty, out var extension)
            ? MudExCodeLanguageExtensionsMapping.GetCodeLanguageForFile(extension)
            : MudExCodeLanguageExtensionsMapping.GetCodeLanguageForFile(kernelLanguage);

        var cells = new List<NotebookCell>();
        if (root.TryGetProperty("cells", out var cellsEl) && cellsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var cellEl in cellsEl.EnumerateArray())
                cells.Add(ParseCell(cellEl));
        }

        return (cells, kernelDisplayName ?? kernelLanguage, language);
    }

    private static NotebookCell ParseCell(JsonElement cellEl)
    {
        var cellType = cellEl.TryGetProperty("cell_type", out var ctEl) && ctEl.ValueKind == JsonValueKind.String ? ctEl.GetString() : "code";
        var source = cellEl.TryGetProperty("source", out var srcEl) ? JoinTextField(srcEl) : string.Empty;
        int? executionCount = cellEl.TryGetProperty("execution_count", out var ecEl) && ecEl.ValueKind == JsonValueKind.Number ? ecEl.GetInt32() : null;

        var outputs = new List<NotebookOutput>();
        if (cellEl.TryGetProperty("outputs", out var outputsEl) && outputsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var outEl in outputsEl.EnumerateArray())
                outputs.Add(ParseOutput(outEl));
        }

        return new NotebookCell(cellType, source, executionCount, outputs);
    }

    private static NotebookOutput ParseOutput(JsonElement outEl)
    {
        var outputType = outEl.TryGetProperty("output_type", out var otEl) && otEl.ValueKind == JsonValueKind.String ? otEl.GetString() : string.Empty;

        if (outputType == "stream")
        {
            var text = outEl.TryGetProperty("text", out var textEl) ? JoinTextField(textEl) : string.Empty;
            return NotebookOutput.AsText(text);
        }

        if (outputType == "error")
        {
            var name = outEl.TryGetProperty("ename", out var enameEl) ? enameEl.GetString() : string.Empty;
            var value = outEl.TryGetProperty("evalue", out var evalueEl) ? evalueEl.GetString() : string.Empty;
            var traceback = outEl.TryGetProperty("traceback", out var tbEl) && tbEl.ValueKind == JsonValueKind.Array
                ? string.Join("\n", tbEl.EnumerateArray().Select(e => e.GetString()))
                : string.Empty;
            return NotebookOutput.AsError(name, value, StripAnsiCodes(traceback));
        }

        if ((outputType is "execute_result" or "display_data") && outEl.TryGetProperty("data", out var dataEl))
        {
            if (dataEl.TryGetProperty("image/png", out var pngEl))
                return NotebookOutput.AsImage("image/png", JoinTextField(pngEl));
            if (dataEl.TryGetProperty("image/jpeg", out var jpgEl))
                return NotebookOutput.AsImage("image/jpeg", JoinTextField(jpgEl));
            if (dataEl.TryGetProperty("text/html", out var htmlEl))
                return NotebookOutput.AsHtml(JoinTextField(htmlEl));
            if (dataEl.TryGetProperty("text/plain", out var plainEl))
                return NotebookOutput.AsText(JoinTextField(plainEl));
        }

        return NotebookOutput.Empty;
    }

    private static string JoinTextField(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString() ?? string.Empty;
        if (element.ValueKind == JsonValueKind.Array)
            return string.Concat(element.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() : string.Empty));
        return string.Empty;
    }

    private static string StripAnsiCodes(string input) => string.IsNullOrEmpty(input) ? input : AnsiEscapeRegex.Replace(input, string.Empty);

    private sealed record NotebookCell(string CellType, string Source, int? ExecutionCount, List<NotebookOutput> Outputs);

    private sealed record NotebookOutput(string Kind, string Text, string MimeType, string Base64Data, string ErrorName, string ErrorValue, string Traceback)
    {
        public static readonly NotebookOutput Empty = new(null, null, null, null, null, null, null);
        public static NotebookOutput AsText(string text) => new("text", text, null, null, null, null, null);
        public static NotebookOutput AsHtml(string html) => new("html", html, null, null, null, null, null);
        public static NotebookOutput AsImage(string mimeType, string base64) => new("image", null, mimeType, base64, null, null, null);
        public static NotebookOutput AsError(string name, string value, string traceback) => new("error", null, null, null, name, value, traceback);
    }
}
