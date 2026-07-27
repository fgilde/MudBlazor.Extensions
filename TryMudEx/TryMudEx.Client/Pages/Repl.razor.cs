using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using Newtonsoft.Json;
using Nextended.Blazor.Models;
using Nextended.Core.Encode;
using Nextended.Core.Extensions;
using Try.Core;
using TryMudEx.Client.Components;
using TryMudEx.Client.Models;
using TryMudEx.Client.Services;

namespace TryMudEx.Client.Pages;

public partial class Repl : IDisposable
{
    [Inject] private LayoutService LayoutService { get; set; }

    private const string MainComponentCodePrefix = "@page \"/__main\"\n";
    private const string MainUserPagePath = "/__main";
    private const string LayoutStorageKey = ReplStorageKeys.Layout;
    private const string OpenFilesStorageKey = ReplStorageKeys.OpenFiles;
    private const string MobileEditorId = "mobile-editor";

    private DotNetObjectReference<Repl> dotNetInstance;
    private string errorMessage;
    private CodeFile activeCodeFile;
    private string[] _samples;
    private NugetPackage[] _installedPackages = Array.Empty<NugetPackage>();

    private MudExDockLayout _dock;
    private string _initialLayoutJson;

    // additionally opened files (never __Main.razor). Entries are append-only; closed
    // files become null tombstones so positional blazor diffing never re-maps a
    // dockview-adopted node to a different file.
    private readonly List<string> _openFiles = new();
    private readonly Dictionary<string, string> _editorDomIds = new();
    private int _editorDomIdCounter;

    [Inject] public NuGetPackageSearcher PackageSearch { get; set; }
    [Inject] public ISnackbar Snackbar { get; set; }
    [Inject] public ILocalStorageService Storage { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] public SnippetsService SnippetsService { get; set; }
    [Inject] public CompilationService CompilationService { get; set; }
    [Inject] public MudExFileService FileService { get; set; }
    [Inject] public IJSInProcessRuntime JsRuntime { get; set; }
    [Inject] public IDialogService DialogService { get; set; }

    [Parameter] public bool ShowHiddenFiles { get; set; }
    [Parameter] public string SnippetId { get; set; }
    [Parameter] public string Sample { get; set; }
    [Parameter] public string SnippetFileUrl { get; set; }

    public IDictionary<string, CodeFile> CodeFiles { get; set; } = new Dictionary<string, CodeFile>();

    private IList<string> CodeFileNames { get; set; } = new List<string>();

    private string EditorTheme => LayoutService.IsDarkMode ? "vs-dark" : "default";

    private static string MainEditorPanelId => EditorPanelId(CoreConstants.MainComponentFilePath);

    private bool SaveSnippetPopupVisible { get; set; }

    private IReadOnlyCollection<CompilationDiagnostic> Diagnostics { get; set; } = Array.Empty<CompilationDiagnostic>();

    private int ErrorsCount => Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

    private int WarningsCount => Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

    private string LoaderText { get; set; }

    private bool Loading { get; set; }

    private static string EditorPanelId(string path) => "ed:" + path;

    private CodeFile GetFile(string path) => path != null && CodeFiles.TryGetValue(path, out var f) ? f : null;

    private static string FileTitle(string path) => path.Contains('/') ? path[(path.LastIndexOf('/') + 1)..] : path;

    private string EditorDomId(string path)
    {
        if (!_editorDomIds.TryGetValue(path, out var id))
        {
            id = $"edd-{++_editorDomIdCounter}";
            _editorDomIds[path] = id;
        }
        return id;
    }

    [JSInvokable]
    public async Task TriggerCompileAsync()
    {
        await CompileAsync();

        StateHasChanged();
    }

    public void Dispose()
    {
        dotNetInstance?.Dispose();
        JsRuntime.InvokeVoid(Models.Try.Dispose);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            dotNetInstance = DotNetObjectReference.Create(this);
            JsRuntime.InvokeVoid(Models.Try.Initialize, dotNetInstance);
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            Snackbar.Add(errorMessage, Severity.Error);
            errorMessage = null;
        }

        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && NavigationManager.Uri.Contains("compile"))
        {
            await Task.Delay(1000);
            await TriggerCompileAsync();
        }
    }

    private async ValueTask SaveState(bool showNotification)
    {
        await Storage.SetItemAsync("__temp_code", CodeFiles);
        await Storage.SetItemAsync(OpenFilesStorageKey, _openFiles.Where(f => f != null).ToList());
        if (showNotification)
        {
            Snackbar.Add("Save code state for reload.", Severity.Info, options =>
            {
                options.HideTransitionDuration = 100;
                options.ShowTransitionDuration = 100;
                options.VisibleStateDuration = 1000;
            });
        }
    }

    private async Task<LoadedSample> LoadDataAsync()
    {
        var isSnippet = !string.IsNullOrWhiteSpace(SnippetId) && string.IsNullOrWhiteSpace(Sample);
        var isSample = !isSnippet && !string.IsNullOrWhiteSpace(Sample);
        var isFromUrl = !isSnippet && !isSample && !string.IsNullOrWhiteSpace(SnippetFileUrl);

        if (isSnippet || isSample || isFromUrl)
        {
            try
            {
                if (isFromUrl)
                {
                    SnippetFileUrl = SnippetFileUrl.StartsWith("http") || SnippetFileUrl.StartsWith("blob") || DataUrl.IsDataUrl(SnippetFileUrl) ? SnippetFileUrl : SnippetFileUrl.EncodeDecode().Base64.Decode();
                    CodeFiles = (await SnippetsService.GetSnippetContentFromUrlAsync(SnippetFileUrl)).ToDictionary(f => f.Path, f => f);
                }
                else
                {

                    CodeFiles = isSnippet
                        ? (await SnippetsService.GetSnippetContentAsync(SnippetId)).ToDictionary(f => f.Path, f => f)
                        : (await SnippetsService.LoadSampleAsync(Sample)).ToDictionary(f => f.Path, f => f);
                }

                if (!CodeFiles.Any())
                    errorMessage = "No files in snippet or sample.";
                else
                    activeCodeFile = CodeFiles.First().Value;
            }
            catch (ArgumentException)
            {
                errorMessage = "Invalid Snippet ID.";
            }
            catch (Exception e)
            {
                errorMessage = "Unable to get snippet content. Please try again later.";
                Console.WriteLine(e.Message);
            }

            return isSnippet ? LoadedSample.Snippet : LoadedSample.Sample;
        }

        if (await Storage.ContainKeyAsync("__temp_code"))
        {
            CodeFiles = await Storage.GetItemAsync<IDictionary<string, CodeFile>>("__temp_code");
            if (CodeFiles.Any())
                activeCodeFile = CodeFiles.First().Value;
        }

        return LoadedSample.None;
    }

    protected override async Task OnInitializedAsync()
    {
        Snackbar.Clear();
        _ = SnippetsService.GetSamplesAsync().ContinueWith(t =>
        {
            _samples = t.Result;
            StateHasChanged();
        });

        var loaded = await LoadDataAsync();

        if (!CodeFiles.Any())
        {
            activeCodeFile = new CodeFile
            {
                Path = CoreConstants.MainComponentFilePath,
                Content = CoreConstants.MainComponentDefaultFileContent
            };
            CodeFiles.Add(CoreConstants.MainComponentFilePath, activeCodeFile);
        }
        else if (!CodeFiles.ContainsKey(CoreConstants.MainComponentFilePath))
        {
            // dock layout requires the main editor panel — ensure the file exists
            CodeFiles.Add(CoreConstants.MainComponentFilePath, new CodeFile
            {
                Path = CoreConstants.MainComponentFilePath,
                Content = CoreConstants.MainComponentDefaultFileContent
            });
        }

        // restore open editor panels + dock layout only for plain reloads (samples/snippets start fresh)
        if (loaded == LoadedSample.None)
        {
            try
            {
                var openFiles = await Storage.GetItemAsync<List<string>>(OpenFilesStorageKey);
                if (openFiles != null)
                    _openFiles.AddRange(openFiles.Where(f => f != null && f != CoreConstants.MainComponentFilePath && CodeFiles.ContainsKey(f)).Distinct());

                _initialLayoutJson = await Storage.GetItemAsStringAsync(LayoutStorageKey);
                if (string.IsNullOrWhiteSpace(_initialLayoutJson) || _initialLayoutJson == "{}")
                    _initialLayoutJson = null;
            }
            catch
            {
                _initialLayoutJson = null;
            }
        }

        CodeFileNames = GetCodeFileNames();

        _installedPackages = await GetInstalledAsync();
        await base.OnInitializedAsync();
    }

    /// <summary>Pulls the current text of every live monaco instance back into CodeFiles.</summary>
    private void CollectAllEditorContent()
    {
        Dictionary<string, string> values;
        try
        {
            values = JsRuntime.Invoke<Dictionary<string, string>>(Models.Try.Editor.GetValues);
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
                if (activeCodeFile != null) activeCodeFile.Content = content;
                continue;
            }

            var path = _editorDomIds.FirstOrDefault(kv => kv.Value == domId).Key;
            if (path != null && CodeFiles.TryGetValue(path, out var file))
                file.Content = content;
        }
    }

    private async Task CompileAsync()
    {
        CollectAllEditorContent();
        await SaveState(true);
        Loading = true;
        LoaderText = "Processing";

        await Task.Delay(10); // Ensure rendering has time to be called

        CompileToAssemblyResult compilationResult = null;
        CodeFile mainComponent = null;
        string originalMainComponentContent = null;
        try
        {
            // Add the necessary main component code prefix and store the original content so we can revert right after compilation.
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
            Snackbar.Add("Error while compiling the code.", Severity.Error);
        }
        finally
        {
            if (mainComponent != null)
            {
                mainComponent.Content = originalMainComponentContent;
            }

            Loading = false;
            StateHasChanged();
        }

        if (compilationResult?.AssemblyBytes?.Length > 0)
        {
            // Make sure the DLL is updated before reloading the user page
            await JsRuntime.InvokeVoidAsync(Models.Try.CodeExecution.UpdateUserComponentsDLL,
                compilationResult.AssemblyBytes);

            ReloadIframe();
        }

        if (ErrorsCount > 0)
            await ShowErrorsPanel();
    }

    private DialogOptionsEx GetSamplesDialogOptions()
    {
        return new DialogOptionsEx
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
    }

    private void ShowSaveSnippetPopup()
    {
        SaveSnippetPopupVisible = true;
    }

    // ---------- dock / editor panel handling ----------

    private async Task OpenFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !CodeFiles.TryGetValue(path, out var file)) return;

        activeCodeFile = file;

        if (path != CoreConstants.MainComponentFilePath && !_openFiles.Contains(path))
        {
            _openFiles.Add(path);
            StateHasChanged();
            await Task.Delay(60); // let blazor render + observer pick up the panel
        }

        if (_dock != null)
            await _dock.ActivatePanelAsync(EditorPanelId(path));
        _ = SaveState(false);
        _ = PersistLayoutAsync();
    }

    private void OpenFileMobile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !CodeFiles.TryGetValue(path, out var file)) return;
        CollectAllEditorContent();
        activeCodeFile = file;
    }

    private async Task HandleCreateFile(string path)
    {
        AddCodeFile(CodeFile.Create(path));
        CodeFileNames = GetCodeFileNames();
        await OpenFile(path);
    }

    private async Task HandleCreateFromTemplate(CodeFile file)
    {
        if (file.Content == null)
            AddCodeFile(CodeFile.Create(file.Path));
        else
            AddCodeFile(file);
        CodeFileNames = GetCodeFileNames();
        await OpenFile(file.Path);
    }

    private async Task HandleRenameFile((string OldPath, string NewPath) rename)
    {
        if (!CodeFiles.TryGetValue(rename.OldPath, out var oldFile)) return;

        CollectAllEditorContent();
        CodeFiles.Remove(rename.OldPath);
        CodeFiles[rename.NewPath] = new CodeFile { Path = rename.NewPath, Content = oldFile.Content };

        // close the old panel (tombstone) and open the renamed file
        var idx = _openFiles.IndexOf(rename.OldPath);
        if (idx >= 0) _openFiles[idx] = null;
        if (activeCodeFile?.Path == rename.OldPath) activeCodeFile = CodeFiles[rename.NewPath];

        CodeFileNames = GetCodeFileNames();
        StateHasChanged();
        await Task.Delay(60);
        await OpenFile(rename.NewPath);
    }

    private async Task HandleDeleteFile(string path)
    {
        if (path == CoreConstants.MainComponentFilePath) return;

        CodeFiles.Remove(path);
        var idx = _openFiles.IndexOf(path);
        if (idx >= 0) _openFiles[idx] = null;
        if (activeCodeFile?.Path == path) activeCodeFile = GetFile(CoreConstants.MainComponentFilePath);

        CodeFileNames = GetCodeFileNames();
        await SaveState(false);
    }

    private void HandlePanelRemoved(string panelId)
    {
        if (panelId?.StartsWith("ed:") != true) return;

        var path = panelId[3..];
        var idx = _openFiles.IndexOf(path);
        if (idx >= 0)
        {
            _openFiles[idx] = null; // tombstone — component dispose signals removePanelById (idempotent here)
            CollectAllEditorContent();
            _ = SaveState(false);
            _ = PersistLayoutAsync();
            StateHasChanged();
        }
    }

    private async Task HandlePanelMoved(DockviewMovePanelEvent _)
    {
        await PersistLayoutAsync();
    }

    private void HandleActivePanelChanged(string panelId)
    {
        if (panelId?.StartsWith("ed:") == true && CodeFiles.TryGetValue(panelId[3..], out var file))
            activeCodeFile = file;
    }

    private async Task PersistLayoutAsync()
    {
        if (_dock == null) return;
        try
        {
            var json = await _dock.SaveLayoutAsync();
            if (!string.IsNullOrWhiteSpace(json) && json != "{}")
                await Storage.SetItemAsStringAsync(LayoutStorageKey, json);
        }
        catch { /* layout persistence is best effort */ }
    }

    private async Task ResetLayout()
    {
        await Storage.RemoveItemAsync(LayoutStorageKey);
        await Storage.RemoveItemAsync(OpenFilesStorageKey);
        NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
    }

    private async Task TogglePanel(string id)
    {
        if (_dock == null) return;
        // re-add is a no-op when the panel is already there; otherwise restore it
        await _dock.AddPanelAsync(JsonConvert.SerializeObject(new
        {
            id,
            title = id == "files" ? "Files" : "Errors",
            direction = id == "files" ? "left" : "down",
        }));
        await _dock.ActivatePanelAsync(id);
    }

    private async Task ShowErrorsPanel()
    {
        await TogglePanel("errors");
    }

    private CodeFile AddCodeFile(CodeFile codefile)
    {
        CodeFiles.TryAdd(codefile.Path, codefile);
        CodeFileNames = GetCodeFileNames();
        SaveState(false);
        return codefile;
    }

    private Task UpdateLoaderTextAsync(string loaderText)
    {
        LoaderText = loaderText;

        StateHasChanged();

        return Task.Delay(10); // Ensure rendering has time to be called
    }

    private async void UpdateTheme()
    {
        await LayoutService.ToggleDarkMode();
    }

    private async Task Upload()
    {
        var allowedExtensions = new List<string> { "zip", "rar" }.Concat(CodeFilesHelper.ValidCodeFileExtensions.Select(e => e.Split('.').Last())).ToList();
        var parameters = new DialogParameters
        {
            { nameof(MudExMessageDialog.Buttons), MudExDialogResultAction.OkCancel("Upload") },
            { nameof(MudExMessageDialog.Icon), Icons.Material.Filled.FileUpload }
        };
        var res = await DialogService.ShowComponentInDialogAsync<MudExUploadEdit<UploadableFile>>("Upload content",
            "Upload content files as zip or separate",
            uploadEdit =>
            {
                uploadEdit.MinHeight = 250;
                uploadEdit.MaxHeight = 250;
                uploadEdit.ExternalProviderRendering = ExternalProviderRendering.ActionButtonsNewLine;
                uploadEdit.ItemIsVisibleFunc = f => ShowHiddenFiles || new CodeFile() { Path = f.FileName }.Type != CodeFileType.Hidden;
                uploadEdit.Style = "margin-bottom: 20px; height: 400px; overflow-y:auto; overflow-x: hidden";
                uploadEdit.AutoExtractArchive = true;
                uploadEdit.Extensions = allowedExtensions.ToArray();
            }, parameters, options =>
            {
                options.Resizeable = true;
                options.FullWidth = true;
                options.MaxWidth = MaxWidth.Medium;
            });
        if (!res.DialogResult.Canceled)
        {
            var files = res.Component.UploadRequests.Select(f => new KeyValuePair<string, CodeFile>(f.FileName.Replace('\\', '/'),
                new CodeFile
                {
                    Path = f.FileName.Replace('\\', '/'),
                    Content = Encoding.UTF8.GetString(f.Data)
                })).ToDictionary(pair => pair.Key, pair => pair.Value);
            await ResetOpenEditorsAsync();
            CodeFiles = files;
            EnsureMainComponent();
            CodeFileNames = GetCodeFileNames();
            activeCodeFile = GetFile(CoreConstants.MainComponentFilePath) ?? CodeFiles.Values.FirstOrDefault();
            _installedPackages = await GetInstalledAsync();
            StateHasChanged();
        }
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

    private void EnsureMainComponent()
    {
        if (!CodeFiles.ContainsKey(CoreConstants.MainComponentFilePath))
        {
            CodeFiles[CoreConstants.MainComponentFilePath] = new CodeFile
            {
                Path = CoreConstants.MainComponentFilePath,
                Content = CoreConstants.MainComponentDefaultFileContent
            };
        }
    }

    private async Task Download()
    {
        CollectAllEditorContent();
        var id = SnippetId ?? Guid.NewGuid().ToFormattedId();
        var fileName = Path.ChangeExtension($"TryMudEx_{id}", "zip");
        fileName = await DialogService.PromptAsync("Filename", "Enter file name", fileName, icon: Icons.Material.Filled.Archive, canConfirm: s => !string.IsNullOrEmpty(s));
        if (!string.IsNullOrEmpty(fileName))
        {
            var stream = SnippetsService.DownloadZipAsync(CodeFiles.Values);
            await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
            {
                Url = await FileService.CreateDataUrlAsync(stream.ToArray(), "application/zip", true),
                FileName = $"{fileName}",
                MimeType = "application/zip"
            });
        }
    }

    private void ReloadIframe()
    {
        var packageParam = JsonConvert.SerializeObject(_installedPackages, CoreConstants.PackageSerializerSettings);
        var url = $"{MainUserPagePath}?packages={packageParam}";
        JsRuntime.InvokeVoid(Models.Try.ReloadIframe, "user-page-window", url);
    }

    private async Task ShowSamples()
    {
        var buttons = MudExDialogResultAction.OkCancel("Open sample");
        buttons.Last().Color = Color.Primary;
        var res = await DialogService.ShowComponentInDialogAsync<MudExList<string>>("Select sample", "Select sample to open",
            list =>
            {
                list.Style = MudExStyleBuilder.Default.WithMaxHeight(85, CssUnit.ViewportHeight).WithOverflow("auto").ToString();
                list.MultiSelection = false;
                list.ItemCollection = _samples.Select(s => s.Replace("_", " ")).ToArray();
                list.Clickable = true;
                list.SearchBox = true;
                list.SearchBoxVariant = Variant.Outlined;
                list.OnDoubleClick = EventCallback.Factory.Create<ListItemClickEventArgs<string>>(this, HandleItemDblClick);
                list.SearchBoxBackgroundColor = "var(--mud-palette-surface)";
            }, dlg =>
            {
                dlg.Icon = Icons.Material.Filled.Folder;
                dlg.Buttons = buttons;

            }, GetSamplesDialogOptions());
        var value = res.Component.SelectedValue;
        if (!res.DialogResult.Canceled && !string.IsNullOrEmpty(value))
        {
            await OpenAndCompileSampleAsync(value);
        }
    }

    private async Task HandleItemDblClick(ListItemClickEventArgs<string> arg)
    {
        await OpenAndCompileSampleAsync(arg.ItemValue);
    }

    private async Task OpenAndCompileSampleAsync(string value)
    {
        value = value.Replace(" ", "_");
        await Storage.RemoveItemAsync("__temp_code");
        await Storage.RemoveItemAsync(OpenFilesStorageKey);
        NavigationManager.NavigateTo($"/snippet/samples/{value}", false);
        Sample = value;
        await ResetOpenEditorsAsync();
        await LoadDataAsync();
        EnsureMainComponent();
        CodeFileNames = GetCodeFileNames();
        _installedPackages = await GetInstalledAsync();
        StateHasChanged();
        await CompileAsync();
    }

    private async Task OpenDiagnostic(CompilationDiagnostic obj)
    {
        if (string.IsNullOrEmpty(obj?.File)) return;

        await OpenFile(obj.File);
        await Task.Delay(100);
        if (obj.Line.HasValue)
            await JsRuntime.InvokeVoidAsync(Models.Try.Editor.SetSelection, EditorDomId(obj.File), obj.Line.Value);
    }

    private List<string> GetCodeFileNames() => !ShowHiddenFiles ? CodeFiles.Where(c => c.Value.Type != CodeFileType.Hidden).Select(c => c.Key).ToList() : CodeFiles.Keys.ToList();


    private async Task EditPackageReferences(bool fromBottom)
    {
        _installedPackages = await GetInstalledAsync();
        var dialog = await DialogService.ShowComponentInDialogAsync<PackageReferences>("Packages", "",
            cmp =>
            {
                cmp.InstalledPackages = _installedPackages;
            },
            new DialogParameters() { { nameof(MudExMessageDialog.Icon), MudExIcons.Custom.Brands.ColorFull.Nuget } },
            (fromBottom ? DialogOptionsEx.SlideInFromBottom : DialogOptionsEx.SlideInFromTop).SetProperties(o =>
            {
                o.Resizeable = true;
                o.FullHeight = true;
                o.FullWidth = true;
                o.MaxWidth = MaxWidth.ExtraLarge;
                o.MaxHeight = MaxHeight.Medium;
            }));


        EnsureReferenceFile().Content = JsonConvert.SerializeObject(_installedPackages = dialog.Component.SelectedPackages, CoreConstants.PackageSerializerSettings);
    }

    private CodeFile EnsureReferenceFile()
        => CodeFiles.Values.FirstOrDefault(c => c.Path == CoreConstants.PackageRef)
        ?? AddCodeFile(new CodeFile() { Path = CoreConstants.PackageRef, Content = JsonConvert.SerializeObject(CoreConstants.DefaultPackages, CoreConstants.PackageSerializerSettings) });


    private async Task<NugetPackage[]> GetInstalledAsync()
    {
        var refFile = EnsureReferenceFile();
        var tasks = JsonConvert.DeserializeObject<List<NugetPackage>>(refFile.Content).Select(x => PackageSearch.SearchForPackagesAsync(x.Id, 1));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r.Data).ToArray();
    }

}
