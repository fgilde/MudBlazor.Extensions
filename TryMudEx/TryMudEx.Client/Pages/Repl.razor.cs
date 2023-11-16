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
using Try.Core.Json;
using TryMudEx.Client.Components;
using TryMudEx.Client.Enums;
using TryMudEx.Client.Models;
using TryMudEx.Client.Services;

namespace TryMudEx.Client.Pages;

public partial class Repl : IDisposable
{
    [Inject] private LayoutService LayoutService { get; set; }

    private const string MainComponentCodePrefix = "@page \"/__main\"\n";
    private const string MainUserPagePath = "/__main";

    private int _activeTabIndex;
    private DotNetObjectReference<Repl> dotNetInstance;
    private string errorMessage;
    private CodeFile activeCodeFile;
    private bool _compiledSuccessfully;
    private bool _wasCompiledSuccessfullyAlready;
    private CodeViewMode _mode;
    private string[] _samples;
    private NugetPackage[] _installedPackages = Array.Empty<NugetPackage>();

    [Inject] public NuGetPackageSearcher PackageSearch { get; set; }
    [Inject] public ISnackbar Snackbar { get; set; }
    [Inject] public ILocalStorageService Storage { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }

    [Inject] public SnippetsService SnippetsService { get; set; }

    [Inject] public CompilationService CompilationService { get; set; }

    [Inject] public MudExFileService FileService { get; set; }

    [Inject] public IJSInProcessRuntime JsRuntime { get; set; }


    [Inject] public IDialogService DialogService { get; set; }

    [Inject] public IJSUnmarshalledRuntime UnmarshalledJsRuntime { get; set; }


    [Parameter] public bool ShowHiddenFiles { get; set; }

    [Parameter] public string MinWidthLeft { get; set; } = "350px";
    [Parameter] public string MinWidthRight { get; set; } = "350px;";
    [Parameter] public string MinHeightLeft { get; set; } = "150px";
    [Parameter] public string MinHeightRight { get; set; } = "150px;";

    [Parameter]
    public CodeViewMode Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
                if (_wasCompiledSuccessfullyAlready && value == CodeViewMode.Window)
                    Task.Delay(500).ContinueWith(_ => ReloadIframe());
            }
        }
    }

    [Parameter] public string SnippetId { get; set; }

    [Parameter] public string Sample { get; set; }
    [Parameter] public string SnippetFileUrl { get; set; }

    public CodeEditor CodeEditorComponent { get; set; }

    public IDictionary<string, CodeFile> CodeFiles { get; set; } = new Dictionary<string, CodeFile>();

    private IList<string> CodeFileNames { get; set; } = new List<string>();

    private string CodeEditorContent => activeCodeFile?.Content;

    private CodeFileType CodeFileType => activeCodeFile?.Type ?? CodeFileType.Razor;

    private bool SaveSnippetPopupVisible { get; set; }

    private IReadOnlyCollection<CompilationDiagnostic> Diagnostics { get; set; } = Array.Empty<CompilationDiagnostic>();

    private int ErrorsCount => Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

    private int WarningsCount => Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

    private bool AreDiagnosticsShown { get; set; }

    private string LoaderText { get; set; }

    private bool Loading { get; set; }

    private bool ShowDiagnostics { get; set; }

    private string CodeContainerStyle => MudExStyleBuilder.Default
        .WithWidth(100, CssUnit.Percentage, true || Mode == CodeViewMode.Window)
        .WithHeight(100, CssUnit.Percentage)
        .Build();

    private void ToggleDiagnostics()
    {
        ShowDiagnostics = !ShowDiagnostics;
        AreDiagnosticsShown = ShowDiagnostics;
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

        await LoadDataAsync();

        if (!CodeFiles.Any())
        {
            activeCodeFile = new CodeFile
            {
                Path = CoreConstants.MainComponentFilePath,
                Content = CoreConstants.MainComponentDefaultFileContent
            };
            CodeFiles.Add(CoreConstants.MainComponentFilePath, activeCodeFile);
        }

        CodeFileNames = GetCodeFileNames();

        _installedPackages = await GetInstalledAsync();
        await base.OnInitializedAsync();
    }


    private async Task CompileAsync()
    {
        await SaveState(true);
        Loading = true;
        LoaderText = "Processing";

        await Task.Delay(10); // Ensure rendering has time to be called

        CompileToAssemblyResult compilationResult = null;
        CodeFile mainComponent = null;
        string originalMainComponentContent = null;
        try
        {
            UpdateActiveCodeFileContent();

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
            AreDiagnosticsShown = true;
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
                StateHasChanged();
            }

            Loading = false;
            _compiledSuccessfully = ErrorsCount == 0;
            if (!_wasCompiledSuccessfullyAlready && _compiledSuccessfully)
                _wasCompiledSuccessfullyAlready = true;
            StateHasChanged();
        }

        if (compilationResult?.AssemblyBytes?.Length > 0)
        {
            // Make sure the DLL is updated before reloading the user page
            await JsRuntime.InvokeVoidAsync(Models.Try.CodeExecution.UpdateUserComponentsDLL,
                compilationResult.AssemblyBytes);


            // TODO: Add error page in iframe
            ReloadIframe();
        }
    }

    private DialogOptionsEx GetResultDialogOptions()
    {
        return new DialogOptionsEx
        {
            CloseButton = true,
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true,
            DisableBackdropClick = false,
            MaximizeButton = true,
            DragMode = MudDialogDragMode.Simple,
            Position = DialogPosition.BottomCenter,
            Animations = new[] { AnimationType.FadeIn, AnimationType.SlideIn },
            AnimationDuration = TimeSpan.FromSeconds(1),
            DisablePositionMargin = true,
            DisableSizeMarginX = false,
            DisableSizeMarginY = false,
            FullHeight = true,
            Resizeable = true
        };
    }

    private DialogOptionsEx GetSamplesDialogOptions()
    {
        return GetResultDialogOptions().SetProperties(o =>
        {
            o.Position = DialogPosition.CenterLeft;
            o.DisableSizeMarginX = true;
            o.DisableSizeMarginY = true;
            o.MaxWidth = MaxWidth.Small;
            o.AnimationDuration = TimeSpan.FromMilliseconds(500);
        });
    }

    private void ShowSaveSnippetPopup()
    {
        SaveSnippetPopupVisible = true;
    }

    private void HandleTabActivate(string name)
    {
        ActivateTab(name);
    }

    private void ActivateTab(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        UpdateActiveCodeFileContent();

        if (CodeFiles.TryGetValue(name, out var codeFile))
        {
            activeCodeFile = codeFile;
            var idx = CodeFiles.Values.ToArray().IndexOf(codeFile);
            if (idx >= 0 && idx != _activeTabIndex)
                _activeTabIndex = idx;

            CodeEditorComponent.Focus();
        }
    }

    private void HandleTabClose(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        CodeFiles.Remove(name);
        SaveState(false);
    }

    private void HandleCreateFromTemplate(CodeFile file)
    {
        if (file.Content == null)
            HandleTabCreate(file.Path);
        else
            AddCodeFile(file);
        ActivateTab(file.Path);
    }

    private void HandleTabCreate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        AddCodeFile(CodeFile.Create(name));
    }

    private CodeFile AddCodeFile(CodeFile codefile)
    {
        CodeFiles.TryAdd(codefile.Path, codefile);
        CodeFileNames = GetCodeFileNames();
        JsRuntime.InvokeVoid(Models.Try.Editor.SetLangugage,
            codefile.Type == CodeFileType.CSharp ? "csharp" : "razor");
        SaveState(false);
        return codefile;
    }

    private void UpdateActiveCodeFileContent()
    {
        if (activeCodeFile == null)
        {
            Snackbar.Add("No active file to update.", Severity.Error);
            return;
        }

        activeCodeFile.Content = CodeEditorComponent.GetCode();
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
        //string theme = LayoutService.IsDarkMode ? "vs-dark" : "default";
        //this.JsRuntime.InvokeVoid(Try.Editor.SetTheme, theme);
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
                //uploadEdit.MimeTypes = MimeType.ArchiveTypes.Concat(new[] { "text/plain", ""}).ToArray();
            }, parameters, options =>
            {
                options.Resizeable = true;
                options.FullWidth = true;
                options.MaxWidth = MaxWidth.Medium;
                //options.FullHeight = true;
            });
        if (!res.DialogResult.Canceled)
        {
            CodeFiles = res.Component.UploadRequests.Select(f => new KeyValuePair<string, CodeFile>(f.FileName,
                new CodeFile
                {
                    Path = f.FileName,
                    Content = Encoding.UTF8.GetString(f.Data)
                })).ToDictionary(pair => pair.Key, pair => pair.Value);
            CodeFileNames = GetCodeFileNames();
            HandleTabActivate(CodeFileNames.FirstOrDefault());
            _installedPackages = await GetInstalledAsync();
            StateHasChanged();
        }
    }

    private async Task Download()
    {
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
        NavigationManager.NavigateTo($"/snippet/samples/{value}", false);
        Sample = value;
        await LoadDataAsync();
        CodeFileNames = GetCodeFileNames();
        _installedPackages = await GetInstalledAsync();
        StateHasChanged();
        await CompileAsync();
    }

    private async Task OpenDiagnostic(CompilationDiagnostic obj)
    {
        ActivateTab(obj.File);
        await Task.Delay(100);
        await CodeEditorComponent.SelectLineAsync(obj.Line);
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


        EnsureReferenceFile().Content = JsonConvert.SerializeObject(_installedPackages = dialog.Component.InstalledPackages, CoreConstants.PackageSerializerSettings);
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