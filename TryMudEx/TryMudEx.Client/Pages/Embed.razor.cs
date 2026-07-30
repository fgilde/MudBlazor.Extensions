using Playzor.Blazor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Nextended.Blazor.Models;
using Nextended.Core.Encode;
using Try.Core;
using TryMudEx.Client.Models;
using TryMudEx.Client.Services;
using Playzor.Blazor.Services;

namespace TryMudEx.Client.Pages;

/// <summary>
/// Minimal embeddable playground (codepen style iframe target). Shares the compile
/// pipeline and editor with the full repl, but has no dock, no file tree and no toolbar.
/// </summary>
public partial class Embed : IDisposable
{
    private const string EditorDomId = "embed-editor";
    private const string MainComponentCodePrefix = "@page \"/__main\"\n";
    private const string MainUserPagePath = "/__main";

    [Inject] public SnippetsService SnippetsService { get; set; }
    [Inject] public CompilationService CompilationService { get; set; }
    [Inject] public NuGetPackageSearcher PackageSearch { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] public IJSInProcessRuntime JsRuntime { get; set; }
    [Inject] private BrandingService Branding { get; set; }
    [Inject] private PlayzorLocalizer L { get; set; }

    [Parameter] public string SnippetId { get; set; }
    [Parameter] public string Sample { get; set; }
    [Parameter] public string SnippetFileUrl { get; set; }

    private EmbedOptions _options = new();
    private Dictionary<string, CodeFile> _codeFiles = new();
    private CodeFile _activeFile;
    private NugetPackage[] _installedPackages = Array.Empty<NugetPackage>();
    private DotNetObjectReference<Embed> _dotNetRef;
    private string _error;
    private bool _loading;
    private bool _compiledOnce;

    private IEnumerable<CodeFile> VisibleFiles => _codeFiles.Values.Where(f => f.Type != CodeFileType.Hidden);

    private string MonacoTheme => IsDark ? "vs-dark" : "default";

    private bool _prefersDark;

    private bool IsDark => _options.Theme switch
    {
        "dark" => true,
        "light" => false,
        _ => _prefersDark, // "auto" follows the visitor's browser, not the playground's own setting
    };

    private string BrandName => Branding.Current.Name;

    /// <summary>Full playground on the same host the embed is served from, carrying the current code.</summary>
    private string EditUrl
    {
        get
        {
            var baseUri = NavigationManager.BaseUri.TrimEnd('/');
            if (!string.IsNullOrEmpty(SnippetId) && SnippetId.Length == 16)
                return $"{baseUri}/snippet/{SnippetId}";
            if (!string.IsNullOrEmpty(Sample))
                return $"{baseUri}/snippet/samples/{Sample}";

            var code = _codeFiles.Any() ? InlineCode.Encode(_codeFiles.Values) : null;
            return code != null ? $"{baseUri}/snippet/{code}" : $"{baseUri}/snippet";
        }
    }

    private string ViewToggleLabel => _options.View switch
    {
        EmbedView.Code => L["Preview"],
        EmbedView.Preview => L["Code"],
        _ => L["Split"],
    };

    private string ViewToggleTitle => L["Switch view"];

    private static string FileName(string path) => path?.Contains('/') == true ? path[(path.LastIndexOf('/') + 1)..] : path;

    protected override async Task OnInitializedAsync()
    {
        _options = EmbedOptions.Parse(NavigationManager.Uri);

        if (_options.Theme == "auto")
        {
            try { _prefersDark = JsRuntime.Invoke<bool>("Try.prefersDark"); } catch { /* keep light */ }
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(SnippetFileUrl))
            {
                var url = SnippetFileUrl.StartsWith("http") || SnippetFileUrl.StartsWith("blob") || DataUrl.IsDataUrl(SnippetFileUrl)
                    ? SnippetFileUrl
                    : SnippetFileUrl.EncodeDecode().Base64.Decode();
                _codeFiles = (await SnippetsService.GetSnippetContentFromUrlAsync(url)).ToDictionary(f => f.Path, f => f);
            }
            else if (!string.IsNullOrWhiteSpace(Sample))
            {
                _codeFiles = (await SnippetsService.LoadSampleAsync(Sample)).ToDictionary(f => f.Path, f => f);
            }
            else if (!string.IsNullOrWhiteSpace(SnippetId))
            {
                _codeFiles = (await SnippetsService.GetSnippetContentAsync(SnippetId)).ToDictionary(f => f.Path, f => f);
            }
            else
            {
                _error = L["Nothing to show — this embed has no snippet."];
            }
        }
        catch (Exception e)
        {
            _error = L["Could not load this snippet."];
            Console.WriteLine(e.Message);
        }

        if (_codeFiles.Any())
        {
            _activeFile = (!string.IsNullOrEmpty(_options.File) && _codeFiles.TryGetValue(_options.File, out var wanted))
                ? wanted
                : _codeFiles.Values.FirstOrDefault(f => f.Path == CoreConstants.MainComponentFilePath)
                  ?? VisibleFiles.FirstOrDefault();

            _installedPackages = await GetInstalledAsync();
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // js wiring is best effort — it must never keep autorun from happening
            try
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                JsRuntime.InvokeVoid(PlayzorJs.Initialize, _dotNetRef);
                await JsRuntime.InvokeVoidAsync("Try.Embed.initAutoHeight");
            }
            catch (Exception e)
            {
                Console.WriteLine("embed js init failed: " + e.Message);
            }
        }

        // blazor renders once before OnInitializedAsync finished, so the snippet is
        // usually not loaded on the first render — autorun on the first render that has files
        if (_options.AutoRun && !_compiledOnce && _codeFiles.Any())
        {
            _compiledOnce = true;
            // monaco may still be loading, so the snippet — not the editor — is the source of truth here
            await CompileAsync(collectFromEditor: false);
        }
    }

    [JSInvokable]
    public async Task TriggerCompileAsync()
    {
        await CompileAsync();
        StateHasChanged();
    }

    private void SelectFile(CodeFile file)
    {
        CollectEditorContent();
        _activeFile = file;
    }

    private void CycleView()
    {
        _options = _options with
        {
            View = _options.View switch
            {
                EmbedView.Split => EmbedView.Code,
                EmbedView.Code => EmbedView.Preview,
                _ => EmbedView.Split,
            }
        };
    }

    private void CollectEditorContent()
    {
        if (_activeFile == null || _options.ReadOnly || _options.View == EmbedView.Preview) return;
        try
        {
            _activeFile.Content = JsRuntime.Invoke<string>(PlayzorJs.Editor.GetValue, EditorDomId);
        }
        catch { /* editor not created yet */ }
    }

    private async Task CompileAsync(bool collectFromEditor = true)
    {
        if (!_codeFiles.Any() || _loading) return;

        if (collectFromEditor) CollectEditorContent();
        _loading = true;
        StateHasChanged();

        CodeFile mainComponent = null;
        string originalContent = null;
        CompileToAssemblyResult result = null;
        try
        {
            if (_codeFiles.TryGetValue(CoreConstants.MainComponentFilePath, out mainComponent))
            {
                originalContent = mainComponent.Content;
                mainComponent.Content = MainComponentCodePrefix + originalContent.Replace(MainComponentCodePrefix, "");
            }

            result = await CompilationService.CompileToAssemblyAsync(_codeFiles.Values, _installedPackages, _ => Task.CompletedTask);

            var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            _error = errors.Any() ? $"{errors.Count} compile error(s): {errors[0].Description}" : null;
        }
        catch (Exception e)
        {
            _error = L["Compilation failed."];
            Console.WriteLine(e.Message);
        }
        finally
        {
            if (mainComponent != null) mainComponent.Content = originalContent;
            _loading = false;
            StateHasChanged();
        }

        if (result?.AssemblyBytes?.Length > 0)
        {
            await JsRuntime.InvokeVoidAsync(PlayzorJs.CodeExecution.UpdateUserComponentsDll, result.AssemblyBytes);
            var packageParam = JsonConvert.SerializeObject(_installedPackages, CoreConstants.PackageSerializerSettings);
            var url = $"{MainUserPagePath}?packages={packageParam}&{(IsDark ? "dark" : "light")}=true";
            JsRuntime.InvokeVoid(PlayzorJs.ReloadIframe, "user-page-window", url);
        }
    }

    private async Task<NugetPackage[]> GetInstalledAsync()
    {
        var refFile = _codeFiles.Values.FirstOrDefault(c => c.Path == CoreConstants.PackageRef);
        var content = refFile?.Content ?? JsonConvert.SerializeObject(CoreConstants.DefaultPackages, CoreConstants.PackageSerializerSettings);
        var tasks = JsonConvert.DeserializeObject<List<NugetPackage>>(content).Select(x => PackageSearch.SearchForPackagesAsync(x.Id, 1));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r.Data).ToArray();
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
        JsRuntime.InvokeVoid(PlayzorJs.Dispose);
    }
}
