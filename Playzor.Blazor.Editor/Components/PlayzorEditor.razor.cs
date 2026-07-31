using System.Text;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using Newtonsoft.Json;
using Nextended.Blazor.Models;
using Nextended.Core.Extensions;
using Playzor.Blazor.Editor.Services;
using Playzor.Core;
using Playzor.Core.Api;

namespace Playzor.Blazor.Editor.Components;

/// <summary>
/// The Playzor playground: monaco editors for any number of files, roslyn compilation in the
/// browser, dockable panels for file tree, preview, errors and console, and a tool bar whose
/// built in buttons can be picked and extended.
/// <para>
/// Compiling produces an assembly that a second webassembly instance runs, so the host has to
/// serve a page that renders the compiled component and point <see cref="PreviewUrl"/> at it.
/// </para>
/// </summary>
public partial class PlayzorEditor : MudExBaseComponent<PlayzorEditor>
{
    private const string MainComponentCodePrefix = "@page \"/__main\"\n";
    private const int MaxPackagesInStatusBar = 4;

    [Inject] private IJSInProcessRuntime JsRuntime { get; set; }
    [Inject] private ILocalStorageService Storage { get; set; }
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; }
    [Inject] private CompilationService CompilationService { get; set; }
    [Inject] private NuGetPackageSearcher PackageSearch { get; set; }
    [Inject] private MudExFileService FileService { get; set; }
    [Inject] private PlayzorLocalizer PlayzorLocalizer { get; set; }
    [Inject] private IServiceProvider Services { get; set; }

    #region Parameters

    /// <summary>
    /// Dom id prefix of this editor. Has to be unique when several editors live on one page,
    /// because the monaco instances, the dock layout and the preview frame derive their ids from it.
    /// </summary>
    [Parameter]
    public string Id { get; set; } = "playzor-editor";

    /// <summary>
    /// The files to edit. When null the editor starts with a single main file containing
    /// <see cref="DefaultSnippet"/>. Assigning a new collection replaces the whole session.
    /// </summary>
    [Parameter]
    public IEnumerable<CodeFile> Files { get; set; }

    /// <summary>
    /// Raised whenever files were added, removed, renamed or compiled, with the current content
    /// of every editor pulled in.
    /// </summary>
    [Parameter]
    public EventCallback<IEnumerable<CodeFile>> FilesChanged { get; set; }

    /// <summary>Content of the main file for a fresh session.</summary>
    [Parameter]
    public string DefaultSnippet { get; set; } = CoreConstants.MainComponentDefaultFileContent;

    /// <summary>Packages a fresh session starts with.</summary>
    [Parameter]
    public IEnumerable<INugetPackageReference> DefaultPackages { get; set; } = CoreConstants.DefaultPackages;

    /// <summary>Which built in tool bar buttons to render.</summary>
    [Parameter]
    public PlayzorToolButtons ToolButtons { get; set; } = PlayzorToolButtons.All;

    /// <summary>Which dock panels the editor brings along.</summary>
    [Parameter]
    public PlayzorPanels Panels { get; set; } = PlayzorPanels.All;

    /// <summary>Shows the status bar with error counts, packages and the preview reload.</summary>
    [Parameter]
    public bool ShowStatusBar { get; set; } = true;

    /// <summary>Shows generated and internal files (the package reference file) in the file tree.</summary>
    [Parameter]
    public bool ShowHiddenFiles { get; set; }

    /// <summary>
    /// Page shown in the preview panel before anything was compiled. Served by the host, usually a
    /// start screen explaining that the code has to run first.
    /// </summary>
    [Parameter]
    public string PreviewUrl { get; set; } = "/user-page";

    /// <summary>
    /// Route of the compiled component. The editor loads it into the preview iframe after every
    /// successful compilation and appends the packages plus the theme as query parameters.
    /// </summary>
    [Parameter]
    public string CompiledPreviewUrl { get; set; } = "/__main";

    /// <summary>
    /// Host page for popped out panels. Ships with the package; dockview clones the stylesheets of
    /// the opener into it and moves the panel's dom node over.
    /// </summary>
    [Parameter]
    public string PopoutUrl { get; set; } = "_content/Playzor.Blazor.Editor/popout.html";

    /// <summary>
    /// Opens the nuget management in a dialog instead of the dock panel. Also the fallback when
    /// <see cref="PlayzorPanels.Packages"/> is not among the <see cref="Panels"/>.
    /// </summary>
    [Parameter]
    public bool PackagesInDialog { get; set; }

    /// <summary>Compiles once as soon as the editor is up.</summary>
    [Parameter]
    public bool AutoRun { get; set; }

    /// <summary>Keeps files, open tabs and the dock layout in local storage across reloads.</summary>
    [Parameter]
    public bool PersistState { get; set; } = true;

    /// <summary>
    /// Prefix of the local storage keys. Give two editors in one app different prefixes so they do
    /// not share their session.
    /// </summary>
    [Parameter]
    public string StateKey { get; set; }

    /// <summary>
    /// Dock layout to start with. Overrides a stored layout, and when it is null the editor uses
    /// its own default (files and editor side by side, errors and console below, preview right).
    /// </summary>
    [Parameter]
    public string InitialLayoutJson { get; set; }

    /// <summary>
    /// Names offered by the samples button. Left empty the editor asks a registered
    /// <see cref="IPlayzorSnippetStore"/>, and without one the button is not rendered.
    /// </summary>
    [Parameter]
    public IEnumerable<string> Samples { get; set; }

    /// <summary>
    /// Raised with the picked sample name. A registered store loads it by itself; this is how a
    /// host without one answers, for example by navigating to its own sample route.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSampleSelected { get; set; }

    /// <summary>
    /// Backend for saving and loading snippets. Overrides a registered
    /// <see cref="IPlayzorSnippetStore"/> for this editor.
    /// </summary>
    [Parameter]
    public IPlayzorSnippetStore SnippetStore { get; set; }

    /// <summary>Id of the snippet currently loaded, set by the store after saving.</summary>
    [Parameter]
    public string SnippetId { get; set; }

    /// <summary>Raised when saving produced a new snippet id.</summary>
    [Parameter]
    public EventCallback<string> SnippetIdChanged { get; set; }

    /// <summary>Dark or light monaco theme. The surrounding app owns the actual theme.</summary>
    [Parameter]
    public bool DarkMode { get; set; } = true;

    /// <summary>Raised by the theme button. Without a handler the button is not rendered.</summary>
    [Parameter]
    public EventCallback<bool> DarkModeChanged { get; set; }

    /// <summary>Two letter culture of the editor ui. Ignored when <see cref="MudExBaseComponent{T}.Localizer"/> is set.</summary>
    [Parameter]
    public string Culture { get; set; }

    /// <summary>Height of the editor, 100% by default so it fills its container.</summary>
    [Parameter]
    public MudExSize<double> Height { get; set; } = new(100, CssUnit.Percentage);

    /// <summary>Height of the tool bar.</summary>
    [Parameter]
    public MudExSize<double> ToolBarHeight { get; set; } = new(48, CssUnit.Pixels);

    /// <summary>Height of the status bar.</summary>
    [Parameter]
    public MudExSize<double> StatusBarHeight { get; set; } = new(26, CssUnit.Pixels);

    /// <summary>Content on the right hand side of the tool bar, after the built in menus.</summary>
    [Parameter]
    public RenderFragment HeaderContent { get; set; }

    /// <summary>Content between the built in buttons and the spacer.</summary>
    [Parameter]
    public RenderFragment ToolBarContent { get; set; }

    /// <summary>Content before the first built in button.</summary>
    [Parameter]
    public RenderFragment ToolBarStartContent { get; set; }

    /// <summary>Content on the right hand side of the status bar, before the package list.</summary>
    [Parameter]
    public RenderFragment StatusBarContent { get; set; }

    /// <summary>
    /// Additional <see cref="MudExDockItem"/> panels. They join the dock layout like the built in
    /// ones, so the user can move, stack and pop them out.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>Raised after every compilation, successful or not.</summary>
    [Parameter]
    public EventCallback<CompileToAssemblyResult> OnCompiled { get; set; }

    /// <summary>
    /// Raised by the save button with the current files. Saving or sharing is up to the host,
    /// and without a handler the button is not rendered.
    /// </summary>
    [Parameter]
    public EventCallback<IEnumerable<CodeFile>> OnSaveRequested { get; set; }

    /// <summary>
    /// Raised by the embed button with the current files. Without a handler the button is not rendered.
    /// </summary>
    [Parameter]
    public EventCallback<IEnumerable<CodeFile>> OnEmbedRequested { get; set; }

    #endregion

    #region Public api

    /// <summary>All files of the current session, with the editor content pulled in.</summary>
    public IEnumerable<CodeFile> GetFiles()
    {
        CollectAllEditorContent();
        return CodeFiles.Values.ToList();
    }

    /// <summary>Replaces the whole session with the given files.</summary>
    public async Task SetFilesAsync(IEnumerable<CodeFile> files)
    {
        await ResetOpenEditorsAsync();
        CodeFiles = files?.ToDictionary(f => f.Path, f => f) ?? new Dictionary<string, CodeFile>();
        EnsureMainComponent();
        _activeCodeFile = GetFile(CoreConstants.MainComponentFilePath) ?? CodeFiles.Values.FirstOrDefault();
        CodeFileNames = GetCodeFileNames();
        _installedPackages = await GetInstalledAsync();
        StateHasChanged();
    }

    /// <summary>Compiles the current files and reloads the preview. Bound to the run button and ctrl+s.</summary>
    [JSInvokable]
    public async Task TriggerCompileAsync()
    {
        await CompileAsync();
        StateHasChanged();
    }

    /// <summary>Diagnostics of the last compilation.</summary>
    public IReadOnlyCollection<CompilationDiagnostic> Diagnostics { get; private set; } = Array.Empty<CompilationDiagnostic>();

    #endregion

    private IStringLocalizer L => Localizer ?? PlayzorLocalizer;

    private IDictionary<string, CodeFile> CodeFiles { get; set; } = new Dictionary<string, CodeFile>();
    private IList<string> CodeFileNames { get; set; } = new List<string>();

    private MudExDockLayout _dock;
    private DotNetObjectReference<PlayzorEditor> _dotNetRef;
    private CodeFile _activeCodeFile;
    private IEnumerable<CodeFile> _lastFilesParameter;
    private NugetPackage[] _installedPackages = Array.Empty<NugetPackage>();
    private string _initialLayoutJson;
    private PlayzorStorageKeys _keys = PlayzorStorageKeys.Default;
    private bool _autoRunDone;
    private bool _compiledOnce;
    private bool _ready;
    private IPlayzorSnippetStore _resolvedStore;
    private IReadOnlyList<string> _storeSamples = Array.Empty<string>();
    private string _previewError;

    // additionally opened files (never the main file). Entries are append-only; closed files
    // become null tombstones so positional blazor diffing never re-maps a dockview-adopted node
    // to a different file.
    private readonly List<string> _openFiles = new();
    private readonly Dictionary<string, string> _editorDomIds = new();
    private int _editorDomIdCounter;

    private bool Loading { get; set; }
    private string LoaderText { get; set; }

    private string DockId => $"{Id}-dock";
    private string PreviewFrameId => $"{Id}-preview";
    private string MobileEditorId => $"{Id}-mobile";
    private string EditorTheme => DarkMode ? "vs-dark" : "default";
    private static string MainEditorPanelId => EditorPanelId(CoreConstants.MainComponentFilePath);
    private int ErrorsCount => Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
    private int WarningsCount => Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

    private bool HasButton(PlayzorToolButtons button) => ToolButtons.HasFlag(button);
    private bool HasPanel(PlayzorPanels panel) => Panels.HasFlag(panel);

    private static string EditorPanelId(string path) => "ed:" + path;
    private static string FileTitle(string path) => path.Contains('/') ? path[(path.LastIndexOf('/') + 1)..] : path;
    private CodeFile GetFile(string path) => path != null && CodeFiles.TryGetValue(path, out var f) ? f : null;

    private string EditorDomId(string path)
    {
        if (!_editorDomIds.TryGetValue(path, out var id))
        {
            id = $"{Id}-ed-{++_editorDomIdCounter}";
            _editorDomIds[path] = id;
        }

        return id;
    }

    #region Styles and classes

    private string ClassStr() => MudExCssBuilder.From("playzor-editor").AddClass(Class).ToString();

    private string StyleStr() => MudExStyleBuilder.Default
        .WithHeight(Height)
        .WithStyle(Style)
        .Style;

    private string ToolBarClassStr() => MudExCssBuilder.From("playzor-editor-toolbar").ToString();

    private string ToolBarStyleStr() => MudExStyleBuilder.Default
        .WithHeight(ToolBarHeight)
        .Style;

    private string StatusBarClassStr() => MudExCssBuilder.From("playzor-editor-statusbar")
        .AddClass("has-errors", ErrorsCount > 0)
        .AddClass("has-warnings", ErrorsCount == 0 && WarningsCount > 0)
        .ToString();

    private string StatusBarStyleStr() => MudExStyleBuilder.Default
        .WithHeight(StatusBarHeight)
        .WithFontSize(12, CssUnit.Pixels)
        .Style;

    private string DockContainerStyleStr() => MudExStyleBuilder.Default
        .WithHeight(100, CssUnit.Percentage)
        .WithWidth(100, CssUnit.Percentage)
        .Style;

    #endregion

    #region Lifecycle

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _keys = new PlayzorStorageKeys(StateKey ?? string.Empty);
        if (Culture != null)
            PlayzorLocalizer.Culture = Culture;

        _resolvedStore = Services.GetService(typeof(IPlayzorSnippetStore)) as IPlayzorSnippetStore;
        if (Store != null && Samples?.Any() != true)
        {
            try
            {
                _storeSamples = await Store.GetSampleNamesAsync();
            }
            catch
            {
                _storeSamples = Array.Empty<string>(); // a store without samples is fine
            }
        }

        _lastFilesParameter = Files;
        var restored = await LoadStateAsync();

        if (!CodeFiles.Any())
            EnsureMainComponent();

        if (!restored)
        {
            // the default layout knows the fixed panels only, so restored editor tabs would end up
            // without a dock panel — they belong to a stored layout
            _openFiles.Clear();
            _initialLayoutJson = InitialLayoutJson ?? BuildDefaultLayoutJson();
        }

        _activeCodeFile ??= GetFile(CoreConstants.MainComponentFilePath) ?? CodeFiles.Values.FirstOrDefault();
        CodeFileNames = GetCodeFileNames();
        _installedPackages = await GetInstalledAsync();

        // the dock must not mount before this point: it reads the initial layout once, and blazor
        // renders a component before its async initialization finished
        _ready = true;

        await base.OnInitializedAsync();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (!ReferenceEquals(_lastFilesParameter, Files) && Files != null)
        {
            _lastFilesParameter = Files;
            await SetFilesAsync(Files);
        }

        if (Culture != null && PlayzorLocalizer.Culture != Culture)
            PlayzorLocalizer.Culture = Culture;

        await base.OnParametersSetAsync();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            JsRuntime.InvokeVoid(PlayzorJs.Initialize, _dotNetRef);
        }

        // the first render happens before OnInitializedAsync finished, so auto run waits for content
        if (AutoRun && !_autoRunDone && CodeFiles.Any())
        {
            _autoRunDone = true;
            await Task.Delay(500); // give monaco a moment to hand its content over
            await CompileAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        try
        {
            JsRuntime.InvokeVoid(PlayzorJs.Dispose);
        }
        catch
        {
            // the circuit may already be gone
        }

        await base.DisposeAsync();
    }

    #endregion

    #region State

    private async Task<bool> LoadStateAsync()
    {
        if (Files != null)
        {
            CodeFiles = Files.ToDictionary(f => f.Path, f => f);
            return false;
        }

        if (!PersistState)
            return false;

        try
        {
            var stored = await Storage.GetItemAsync<Dictionary<string, CodeFile>>(_keys.Code);
            if (stored?.Any() == true)
                CodeFiles = stored;

            var openFiles = await Storage.GetItemAsync<List<string>>(_keys.OpenFiles);
            if (openFiles != null)
                _openFiles.AddRange(openFiles.Where(f => f != null && f != CoreConstants.MainComponentFilePath && CodeFiles.ContainsKey(f)).Distinct());

            _initialLayoutJson = InitialLayoutJson ?? await Storage.GetItemAsStringAsync(_keys.Layout);
            if (string.IsNullOrWhiteSpace(_initialLayoutJson) || _initialLayoutJson == "{}")
                _initialLayoutJson = null;

            return _initialLayoutJson != null;
        }
        catch
        {
            _initialLayoutJson = null;
            return false;
        }
    }

    private async ValueTask SaveStateAsync(bool notifyHost = true)
    {
        if (PersistState)
        {
            await Storage.SetItemAsync(_keys.Code, CodeFiles);
            await Storage.SetItemAsync(_keys.OpenFiles, _openFiles.Where(f => f != null).ToList());
        }

        if (notifyHost && FilesChanged.HasDelegate)
            await FilesChanged.InvokeAsync(CodeFiles.Values.ToList());
    }

    private void EnsureMainComponent()
    {
        if (!CodeFiles.ContainsKey(CoreConstants.MainComponentFilePath))
        {
            CodeFiles[CoreConstants.MainComponentFilePath] = new CodeFile
            {
                Path = CoreConstants.MainComponentFilePath,
                Content = DefaultSnippet ?? CoreConstants.MainComponentDefaultFileContent
            };
        }
    }

    private List<string> GetCodeFileNames() => !ShowHiddenFiles
        ? CodeFiles.Where(c => c.Value.Type != CodeFileType.Hidden).Select(c => c.Key).ToList()
        : CodeFiles.Keys.ToList();

    /// <summary>Pulls the current text of every live monaco instance back into the code files.</summary>
    private void CollectAllEditorContent()
    {
        Dictionary<string, string> values;
        try
        {
            values = JsRuntime.Invoke<Dictionary<string, string>>(PlayzorJs.Editor.GetValues);
        }
        catch
        {
            return;
        }

        if (values == null) return;

        foreach (var (domId, content) in values)
        {
            if (domId == MobileEditorId)
            {
                if (_activeCodeFile != null) _activeCodeFile.Content = content;
                continue;
            }

            var path = _editorDomIds.FirstOrDefault(kv => kv.Value == domId).Key;
            if (path != null && CodeFiles.TryGetValue(path, out var file))
                file.Content = content;
        }
    }

    #endregion

    #region Compile and preview

    private async Task CompileAsync()
    {
        CollectAllEditorContent();
        await SaveStateAsync();
        Loading = true;
        LoaderText = L["Processing"];

        await Task.Delay(10); // ensure rendering has time to be called

        CompileToAssemblyResult compilationResult = null;
        CodeFile mainComponent = null;
        string originalMainComponentContent = null;
        try
        {
            // add the route prefix the preview page needs and revert it right after compilation
            if (CodeFiles.TryGetValue(CoreConstants.MainComponentFilePath, out mainComponent))
            {
                originalMainComponentContent = mainComponent.Content;
                mainComponent.Content = MainComponentCodePrefix +
                                        originalMainComponentContent.Replace(MainComponentCodePrefix, "");
            }

            compilationResult = await CompilationService.CompileToAssemblyAsync(
                CodeFiles.Values,
                _installedPackages,
                UpdateLoaderTextAsync);

            Diagnostics = compilationResult.Diagnostics.OrderByDescending(x => x.Severity).ThenBy(x => x.Code).ToList();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Snackbar.Add(L["Error while compiling the code."], Severity.Error);
        }
        finally
        {
            if (mainComponent != null)
                mainComponent.Content = originalMainComponentContent;

            Loading = false;
            StateHasChanged();
        }

        PublishDiagnosticMarkers();

        if (compilationResult?.AssemblyBytes?.Length > 0)
        {
            // make sure the dll is stored before the preview reloads
            await JsRuntime.InvokeVoidAsync(PlayzorJs.CodeExecution.UpdateUserComponentsDll, compilationResult.AssemblyBytes);
            _compiledOnce = true;
            ReloadPreview();
            _ = VerifyPreviewAsync();
        }

        if (OnCompiled.HasDelegate)
            await OnCompiled.InvokeAsync(compilationResult);

        if (ErrorsCount > 0)
            await ShowErrorsPanelAsync();
    }

    private Task UpdateLoaderTextAsync(string loaderText)
    {
        LoaderText = loaderText;
        StateHasChanged();
        return Task.Delay(10); // ensure rendering has time to be called
    }

    /// <summary>Pushes the compiler diagnostics into the monaco editors as inline squiggles.</summary>
    private void PublishDiagnosticMarkers()
    {
        var byFile = Diagnostics
            .Where(d => !string.IsNullOrEmpty(d.File))
            .GroupBy(d => d.File)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (path, domId) in _editorDomIds)
        {
            var markers = byFile.TryGetValue(path, out var list)
                ? list.Select(d => new
                {
                    line = d.Line ?? 1,
                    column = 0,
                    endLine = d.Line ?? 1,
                    endColumn = 0,
                    message = $"{d.Code}: {d.Description}",
                    severity = d.Severity == DiagnosticSeverity.Error ? "error"
                        : d.Severity == DiagnosticSeverity.Warning ? "warning" : "info",
                }).ToArray()
                : Array.Empty<object>();

            JsRuntime.InvokeVoid(PlayzorJs.Editor.SetMarkers, domId, markers);
        }
    }

    /// <summary>Reloads the preview iframe with the current packages and theme.</summary>
    public void ReloadPreview()
    {
        if (!HasPanel(PlayzorPanels.Preview)) return;

        var packageParam = JsonConvert.SerializeObject(_installedPackages, CoreConstants.PackageSerializerSettings);
        var target = _compiledOnce ? CompiledPreviewUrl : PreviewUrl;
        var separator = target.Contains('?') ? "&" : "?";
        // the preview page reads dark/light from its url, so it follows the editor theme on load
        var url = $"{target}{separator}packages={packageParam}&{(DarkMode ? "dark" : "light")}=true";
        JsRuntime.InvokeVoid(PlayzorJs.ReloadIframe, PreviewFrameId, url);
    }

    /// <summary>
    /// The compiled component runs in a page the host has to serve, and a missing route just shows
    /// an empty frame. The preview script announces itself on load, so when nothing announces after
    /// a run something about that page is wrong and the editor says which part to check.
    /// </summary>
    private async Task VerifyPreviewAsync()
    {
        var startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await Task.Delay(4000);

        bool loaded;
        try
        {
            loaded = await JsRuntime.InvokeAsync<bool>(PlayzorJs.Preview.LoadedSince, startedAt);
        }
        catch
        {
            return; // the editor is gone, nothing to report to
        }

        _previewError = loaded
            ? null
            : string.Format(L["The preview at {0} did not load. The host has to serve that route with the compiled component, reference Playzor.UserComponents and include playzor-preview.js on the page."].Value,
                CompiledPreviewUrl);

        StateHasChanged();
    }

    private async Task ToggleDarkModeAsync()
    {
        DarkMode = !DarkMode;
        await DarkModeChanged.InvokeAsync(DarkMode);
        // the preview is a separate wasm instance and only reads the theme from its url on load
        JsRuntime.InvokeVoid(PlayzorJs.Preview.PushTheme, DarkMode);
    }

    #endregion

    #region Files

    /// <summary>Opens a file as an editor tab and activates it.</summary>
    public async Task OpenFileAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !CodeFiles.TryGetValue(path, out var file)) return;

        _activeCodeFile = file;

        if (path != CoreConstants.MainComponentFilePath && !_openFiles.Contains(path))
        {
            _openFiles.Add(path);
            StateHasChanged();
            await Task.Delay(60); // let blazor render + observer pick up the panel
        }

        if (_dock != null)
            await _dock.ActivatePanelAsync(EditorPanelId(path), highlight: true);

        await SaveStateAsync(false);
        await PersistLayoutAsync();
    }

    private void OpenFileMobile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !CodeFiles.TryGetValue(path, out var file)) return;
        CollectAllEditorContent();
        _activeCodeFile = file;
    }

    private async Task HandleCreateFileAsync(string path)
    {
        AddCodeFile(CodeFile.Create(path));
        CodeFileNames = GetCodeFileNames();
        await OpenFileAsync(path);
    }

    private async Task HandleCreateFromTemplateAsync(CodeFile file)
    {
        AddCodeFile(file.Content == null ? CodeFile.Create(file.Path) : file);
        CodeFileNames = GetCodeFileNames();
        await OpenFileAsync(file.Path);
    }

    private async Task HandleRenameFileAsync((string OldPath, string NewPath) rename)
    {
        if (rename.OldPath == CoreConstants.MainComponentFilePath) return;
        if (!CodeFiles.TryGetValue(rename.OldPath, out var oldFile)) return;

        CollectAllEditorContent();
        CodeFiles.Remove(rename.OldPath);
        CodeFiles[rename.NewPath] = new CodeFile { Path = rename.NewPath, Content = oldFile.Content };

        // close the old panel (tombstone) and open the renamed file
        var idx = _openFiles.IndexOf(rename.OldPath);
        if (idx >= 0) _openFiles[idx] = null;
        if (_activeCodeFile?.Path == rename.OldPath) _activeCodeFile = CodeFiles[rename.NewPath];

        CodeFileNames = GetCodeFileNames();
        StateHasChanged();
        await Task.Delay(60);
        await OpenFileAsync(rename.NewPath);
    }

    private async Task HandleDeleteFileAsync(string path)
    {
        if (path == CoreConstants.MainComponentFilePath) return;

        CodeFiles.Remove(path);
        var idx = _openFiles.IndexOf(path);
        if (idx >= 0) _openFiles[idx] = null;
        if (_activeCodeFile?.Path == path) _activeCodeFile = GetFile(CoreConstants.MainComponentFilePath);

        CodeFileNames = GetCodeFileNames();
        await SaveStateAsync();
    }

    private CodeFile AddCodeFile(CodeFile codeFile)
    {
        CodeFiles.TryAdd(codeFile.Path, codeFile);
        CodeFileNames = GetCodeFileNames();
        _ = SaveStateAsync();
        return codeFile;
    }

    /// <summary>Disposes all dynamic editor panels and clears tombstones (before a full content swap).</summary>
    private async Task ResetOpenEditorsAsync()
    {
        if (_openFiles.Count == 0) return;
        _openFiles.Clear();
        _editorDomIds.Clear();
        _editorDomIdCounter = 0;
        StateHasChanged();
        await Task.Delay(60); // let dispose-signals reach the dock before new content renders
    }

    private async Task OpenDiagnosticAsync(CompilationDiagnostic diagnostic)
    {
        if (string.IsNullOrEmpty(diagnostic?.File)) return;

        await OpenFileAsync(diagnostic.File);
        await Task.Delay(100);
        if (diagnostic.Line.HasValue)
            await JsRuntime.InvokeVoidAsync(PlayzorJs.Editor.SetSelection, EditorDomId(diagnostic.File), diagnostic.Line.Value);
    }

    #endregion

    #region Tool bar actions

    private async Task RequestSaveAsync()
    {
        CollectAllEditorContent();
        var files = CodeFiles.Values.ToList();

        // a registered store saves on its own, otherwise the host answers through the event
        if (Store != null && !OnSaveRequested.HasDelegate)
        {
            try
            {
                SnippetId = await Store.SaveAsync(files);
                await SnippetIdChanged.InvokeAsync(SnippetId);
                Snackbar.Add($"{L["Snippet saved"]}: {SnippetId}", Severity.Success);
            }
            catch (Exception e)
            {
                Snackbar.Add(L["Could not save the snippet."], Severity.Error);
                Console.WriteLine(e.Message);
            }

            return;
        }

        await OnSaveRequested.InvokeAsync(files);
    }

    private async Task RequestEmbedAsync()
    {
        CollectAllEditorContent();
        await OnEmbedRequested.InvokeAsync(CodeFiles.Values.ToList());
    }

    private async Task DownloadAsync()
    {
        CollectAllEditorContent();
        var fileName = await DialogService.PromptAsync(L["Download"], L["Enter file name"], $"playzor_{Guid.NewGuid().ToFormattedId()}.zip",
            icon: Icons.Material.Filled.Archive, canConfirm: s => !string.IsNullOrEmpty(s));
        if (string.IsNullOrEmpty(fileName)) return;

        var stream = CodeFiles.Values.ToZipArchive();
        await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
        {
            Url = await FileService.CreateDataUrlAsync(stream.ToArray(), "application/zip", true),
            FileName = fileName,
            MimeType = "application/zip"
        });
    }

    private async Task UploadAsync()
    {
        var allowedExtensions = new List<string> { "zip", "rar" }
            .Concat(CodeFilesHelper.ValidCodeFileExtensions.Select(e => e.Split('.').Last()))
            .ToArray();

        var parameters = new DialogParameters
        {
            { nameof(MudExMessageDialog.Buttons), MudExDialogResultAction.OkCancel(L["Upload"]) },
            { nameof(MudExMessageDialog.Icon), Icons.Material.Filled.FileUpload }
        };

        var res = await DialogService.ShowComponentInDialogAsync<MudExUploadEdit<UploadableFile>>(L["Upload"],
            L["Upload content files as zip or separate"],
            uploadEdit =>
            {
                uploadEdit.MinHeight = 250;
                uploadEdit.MaxHeight = 250;
                uploadEdit.ExternalProviderRendering = ExternalProviderRendering.ActionButtonsNewLine;
                uploadEdit.ItemIsVisibleFunc = f => ShowHiddenFiles || new CodeFile { Path = f.FileName }.Type != CodeFileType.Hidden;
                uploadEdit.Style = MudExStyleBuilder.Default.WithMarginBottom(20, CssUnit.Pixels).WithHeight(400, CssUnit.Pixels).WithOverflow("auto").Style;
                uploadEdit.AutoExtractArchive = true;
                uploadEdit.Extensions = allowedExtensions;
            }, parameters, options =>
            {
                options.Resizeable = true;
                options.FullWidth = true;
                options.MaxWidth = MaxWidth.Medium;
            });

        if (res.DialogResult.Canceled) return;

        await SetFilesAsync(res.Component.UploadRequests.Select(f => new CodeFile
        {
            Path = f.FileName.Replace('\\', '/'),
            Content = Encoding.UTF8.GetString(f.Data)
        }));
        await SaveStateAsync();
    }

    private async Task ShowSamplesAsync()
    {
        var buttons = MudExDialogResultAction.OkCancel(L["Open sample"]);
        buttons.Last().Color = Color.Primary;

        var res = await DialogService.ShowComponentInDialogAsync<MudExList<string>>(L["Samples"], L["Select sample to open"],
            list =>
            {
                list.Style = MudExStyleBuilder.Default.WithMaxHeight(85, CssUnit.ViewportHeight).WithOverflow("auto").Style;
                list.MultiSelection = false;
                list.ItemCollection = AvailableSamples.Select(s => s.Replace("_", " ")).ToArray();
                list.Clickable = true;
                list.SearchBox = true;
                list.SearchBoxVariant = Variant.Outlined;
                list.OnDoubleClick = EventCallback.Factory.Create<ListItemClickEventArgs<string>>(this, args => SelectSampleAsync(args.ItemValue));
                list.SearchBoxBackgroundColor = "var(--mud-palette-surface)";
            }, dlg =>
            {
                dlg.Icon = Icons.Material.Filled.Folder;
                dlg.Buttons = buttons;
            }, SamplesDialogOptions());

        var value = res.Component.SelectedValue;
        if (!res.DialogResult.Canceled && !string.IsNullOrEmpty(value))
            await SelectSampleAsync(value);
    }

    /// <summary>The store to use: the parameter wins over a registered one.</summary>
    private IPlayzorSnippetStore Store => SnippetStore ?? _resolvedStore;

    /// <summary>Samples offered by the button: the parameter wins, otherwise the store is asked.</summary>
    private IEnumerable<string> AvailableSamples => Samples?.Any() == true ? Samples : _storeSamples;

    private async Task SelectSampleAsync(string sample)
    {
        var name = sample?.Replace(" ", "_");
        if (string.IsNullOrEmpty(name)) return;

        // a registered store loads on its own, otherwise the host answers through the event
        if (Store != null && Samples?.Any() != true)
        {
            try
            {
                await SetFilesAsync(await Store.LoadSampleAsync(name));
                await CompileAsync();
            }
            catch (Exception e)
            {
                Snackbar.Add(L["Could not load the sample."], Severity.Error);
                Console.WriteLine(e.Message);
            }
        }

        await OnSampleSelected.InvokeAsync(name);
    }

    private static DialogOptionsEx SamplesDialogOptions() => new()
    {
        CloseButton = true,
        BackdropClick = true,
        DragMode = MudDialogDragMode.Simple,
        Position = DialogPosition.CenterLeft,
        Animations = new[] { AnimationType.FadeIn, AnimationType.SlideIn },
        AnimationDuration = TimeSpan.FromMilliseconds(500),
        DisablePositionMargin = true,
        MaxWidth = MaxWidth.Small,
        FullHeight = true,
        Resizeable = true
    };

    #endregion

    #region Packages

    /// <summary>Both nuget buttons end up here — see <see cref="PackagesInDialog"/>.</summary>
    private Task EditPackagesAsync(bool fromBottom = false)
        => PackagesInDialog || !HasPanel(PlayzorPanels.Packages)
            ? EditPackageReferencesAsync(fromBottom)
            : ShowPackagesPanelAsync();

    private async Task EditPackageReferencesAsync(bool fromBottom)
    {
        _installedPackages = await GetInstalledAsync();
        var dialog = await DialogService.ShowComponentInDialogAsync<PackageReferences>(L["Manage Nuget packages"], "",
            cmp => cmp.InstalledPackages = _installedPackages,
            new DialogParameters { { nameof(MudExMessageDialog.Icon), MudExIcons.Custom.Brands.ColorFull.Nuget } },
            (fromBottom ? DialogOptionsEx.SlideInFromBottom : DialogOptionsEx.SlideInFromTop).SetProperties(o =>
            {
                o.Resizeable = true;
                o.FullHeight = true;
                o.FullWidth = true;
                o.MaxWidth = MaxWidth.ExtraLarge;
                o.MaxHeight = MaxHeight.Medium;
            }));

        EnsureReferenceFile().Content = JsonConvert.SerializeObject(
            _installedPackages = dialog.Component.SelectedPackages, CoreConstants.PackageSerializerSettings);
    }

    /// <summary>
    /// The packages panel has no ok button, so every install and uninstall is written through right
    /// away. The compiler picks the file up on the next run.
    /// </summary>
    private async Task HandlePackagesChangedAsync(NugetPackage[] packages)
    {
        _installedPackages = packages ?? Array.Empty<NugetPackage>();
        EnsureReferenceFile().Content = JsonConvert.SerializeObject(_installedPackages, CoreConstants.PackageSerializerSettings);
        await SaveStateAsync(false);
        StateHasChanged();
    }

    private CodeFile EnsureReferenceFile()
        => CodeFiles.Values.FirstOrDefault(c => c.Path == CoreConstants.PackageRef)
           ?? AddCodeFile(new CodeFile
           {
               Path = CoreConstants.PackageRef,
               Content = JsonConvert.SerializeObject(DefaultPackages ?? CoreConstants.DefaultPackages, CoreConstants.PackageSerializerSettings)
           });

    private async Task<NugetPackage[]> GetInstalledAsync()
    {
        var refFile = EnsureReferenceFile();
        var tasks = JsonConvert.DeserializeObject<List<NugetPackage>>(refFile.Content)
            .Select(x => PackageSearch.SearchForPackagesAsync(x.Id, 1));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r.Data).ToArray();
    }

    private static string TrimmedPackageText(NugetPackage package, int maxChars = 35)
    {
        var text = $"{package.Id} {package.Version}";
        return text.Length > maxChars ? text.Substring(0, maxChars - 3) + "..." : text;
    }

    #endregion

    #region Dock panels

    private static readonly Dictionary<string, (string Title, string Direction, PlayzorPanels Panel)> StaticPanels = new()
    {
        ["files"] = ("Files", "left", PlayzorPanels.Files),
        ["preview"] = ("Preview", "right", PlayzorPanels.Preview),
        ["errors"] = ("Errors", "below", PlayzorPanels.Errors),
        ["console"] = ("Console", "below", PlayzorPanels.Console),
        ["packages"] = ("Packages", "below", PlayzorPanels.Packages),
    };

    /// <summary>Panels that share the bottom stack with the errors panel.</summary>
    private static bool StacksWithErrors(string id) => id is "console" or "packages";

    private IEnumerable<KeyValuePair<string, (string Title, string Direction, PlayzorPanels Panel)>> AvailablePanels
        => StaticPanels.Where(p => HasPanel(p.Value.Panel));

    /// <summary>Panel ids dockview currently holds — refreshed when the panels menu opens.</summary>
    private HashSet<string> _openPanels = new();

    private bool IsPanelOpen(string id) => _openPanels.Contains(id);

    private async Task RefreshOpenPanelsAsync()
    {
        if (_dock == null) return;
        try
        {
            var ids = await _dock.GetPanelIdsAsync();
            _openPanels = ids?.ToHashSet() ?? new HashSet<string>();
            StateHasChanged();
        }
        catch
        {
            // dock not ready yet
        }
    }

    private async Task TogglePanelAsync(string id)
    {
        if (_dock == null || !StaticPanels.TryGetValue(id, out var meta)) return;

        if (_openPanels.Contains(id))
        {
            await _dock.RemovePanelAsync(id);
        }
        else
        {
            await _dock.AddPanelAsync(JsonConvert.SerializeObject(new
            {
                id,
                title = L[meta.Title].Value,
                direction = meta.Direction,
                stackWith = StacksWithErrors(id) && HasPanel(PlayzorPanels.Errors) ? "errors" : null,
                canClose = id != "preview",
                canPopout = true,
            }));
            await _dock.ActivatePanelAsync(id, highlight: true);
        }

        await RefreshOpenPanelsAsync();
        await PersistLayoutAsync();
    }

    private async Task PopoutPanelAsync(string id)
    {
        if (_dock == null) return;
        if (!_openPanels.Contains(id)) await TogglePanelAsync(id);

        var ok = await _dock.PopoutPanelAsync(id, PopoutUrl);
        if (!ok)
            Snackbar.Add(L["Could not open a window — check your popup blocker."], Severity.Warning);
    }

    private Task ShowErrorsPanelAsync() => ShowPanelAsync("errors", PlayzorPanels.Errors);

    private Task ShowPackagesPanelAsync() => ShowPanelAsync("packages", PlayzorPanels.Packages);

    /// <summary>Brings a panel to the front, opening it when the user closed it before.</summary>
    private async Task ShowPanelAsync(string id, PlayzorPanels panel)
    {
        if (_dock == null || !HasPanel(panel)) return;
        await RefreshOpenPanelsAsync();
        if (!_openPanels.Contains(id)) await TogglePanelAsync(id);
        else await _dock.ActivatePanelAsync(id, highlight: true);
    }

    private void HandlePanelRemoved(string panelId)
    {
        // a layout restore tears every panel down before rebuilding — those removals are
        // not the user closing a file, so they must not tombstone the open files
        if (_restoringLayout) return;
        if (panelId?.StartsWith("ed:") != true) return;

        var path = panelId[3..];
        var idx = _openFiles.IndexOf(path);
        if (idx >= 0)
        {
            _openFiles[idx] = null; // tombstone — component dispose signals removePanelById (idempotent here)
            CollectAllEditorContent();
            _ = SaveStateAsync(false);
            _ = PersistLayoutAsync();
            StateHasChanged();
        }
    }

    private Task HandlePanelMovedAsync(DockviewMovePanelEvent _) => PersistLayoutAsync();

    private void HandleActivePanelChanged(string panelId)
    {
        if (panelId?.StartsWith("ed:") == true && CodeFiles.TryGetValue(panelId[3..], out var file))
            _activeCodeFile = file;
    }

    #endregion

    #region Layouts

    private Dictionary<string, string> _namedLayouts = new();
    private bool _restoringLayout;

    private async Task PersistLayoutAsync()
    {
        if (_dock == null || !PersistState) return;
        try
        {
            var json = await _dock.SaveLayoutAsync();
            if (!string.IsNullOrWhiteSpace(json) && json != "{}")
                await Storage.SetItemAsStringAsync(_keys.Layout, json);
        }
        catch
        {
            // layout persistence is best effort
        }
    }

    private async Task LoadNamedLayoutsAsync()
    {
        try
        {
            _namedLayouts = await Storage.GetItemAsync<Dictionary<string, string>>(_keys.NamedLayouts) ?? new Dictionary<string, string>();
        }
        catch
        {
            _namedLayouts = new Dictionary<string, string>();
        }
    }

    private async Task SaveNamedLayoutAsync()
    {
        if (_dock == null) return;

        var name = await DialogService.PromptAsync(L["Save layout"], L["Name for this layout"], string.Empty,
            icon: Icons.Material.Outlined.Bookmark, canConfirm: s => !string.IsNullOrWhiteSpace(s));
        if (string.IsNullOrWhiteSpace(name)) return;

        var json = await _dock.SaveLayoutAsync();
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return;

        _namedLayouts[name.Trim()] = json;
        await Storage.SetItemAsync(_keys.NamedLayouts, _namedLayouts);
        Snackbar.Add($"{L["Layout saved"]}: {name}", Severity.Success);
    }

    private Task ApplyNamedLayoutAsync(string name)
        => _namedLayouts.TryGetValue(name, out var json) ? ApplyLayoutAsync(json) : Task.CompletedTask;

    private async Task ApplyLayoutAsync(string json, bool persist = true)
    {
        if (_dock == null || string.IsNullOrWhiteSpace(json)) return;

        _restoringLayout = true;
        try
        {
            await _dock.RestoreLayoutAsync(json);
            await Task.Delay(100); // let the teardown events pass while the guard is up
        }
        finally
        {
            _restoringLayout = false;
        }

        if (persist && PersistState) await Storage.SetItemAsStringAsync(_keys.Layout, json);
        await RefreshOpenPanelsAsync();
    }

    private async Task DeleteNamedLayoutAsync(string name)
    {
        if (!_namedLayouts.Remove(name)) return;
        await Storage.SetItemAsync(_keys.NamedLayouts, _namedLayouts);
        StateHasChanged();
    }

    /// <summary>Puts the panels back to the layout the editor starts with, without reloading the page.</summary>
    public async Task ResetLayoutAsync()
    {
        if (PersistState)
        {
            await Storage.RemoveItemAsync(_keys.Layout);
            await Storage.RemoveItemAsync(_keys.OpenFiles);
        }

        // extra editor tabs have to go before the restore: the default layout has no panel for them
        await ResetOpenEditorsAsync();
        await ApplyLayoutAsync(InitialLayoutJson ?? BuildDefaultLayoutJson(), persist: false);
    }

    /// <summary>
    /// Files and editor side by side, errors/console under them, preview full height on the right.
    /// Written out as a dockview layout instead of relying on the declarative panel order, because
    /// that order can only place panels relative to the whole grid — never below a single panel.
    /// dockview flips the orientation per level (root horizontal, its branches vertical, …) and
    /// scales the sizes to the container, so they are ratios rather than pixels.
    /// </summary>
    private string BuildDefaultLayoutJson()
    {
        object Leaf(string groupId, int size, params string[] views) => new
        {
            type = "leaf",
            size,
            data = new { id = groupId, views, activeView = views[0] }
        };

        object Branch(int size, params object[] children) => new { type = "branch", size, data = children };

        object Panel(string id, string title) => new { id, title, renderer = "always" };

        var panels = new Dictionary<string, object>
        {
            [MainEditorPanelId] = Panel(MainEditorPanelId, CoreConstants.MainComponentFilePath)
        };

        var bottomViews = new List<string>();
        if (HasPanel(PlayzorPanels.Errors)) bottomViews.Add("errors");
        if (HasPanel(PlayzorPanels.Console)) bottomViews.Add("console");

        var topRow = new List<object>();
        if (HasPanel(PlayzorPanels.Files))
        {
            topRow.Add(Leaf("2", 250, "files"));
            panels["files"] = Panel("files", L["Files"].Value);
        }
        topRow.Add(Leaf("1", 570, MainEditorPanelId));

        var leftColumn = new List<object> { Branch(500, topRow.ToArray()) };
        if (bottomViews.Any())
        {
            leftColumn.Add(Leaf("3", 300, bottomViews.ToArray()));
            foreach (var view in bottomViews)
                panels[view] = Panel(view, L[view == "errors" ? "Errors" : "Console"].Value);
        }

        var root = new List<object> { Branch(820, leftColumn.ToArray()) };
        if (HasPanel(PlayzorPanels.Preview))
        {
            root.Add(Leaf("4", 580, "preview"));
            panels["preview"] = Panel("preview", L["Preview"].Value);
        }

        return JsonConvert.SerializeObject(new
        {
            grid = new { width = 1400, height = 800, orientation = "HORIZONTAL", root = Branch(800, root.ToArray()) },
            panels,
            activeGroup = "1",
        });
    }

    #endregion
}
