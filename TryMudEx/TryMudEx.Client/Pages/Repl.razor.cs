using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using Nextended.Blazor.Models;
using Nextended.Core.Encode;
using Nextended.Core.Extensions;
using Try.Core;
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

    private DotNetObjectReference<Repl> dotNetInstance;
    private string errorMessage;
    private CodeFile activeCodeFile;
    private bool _compiledSuccessfully;
    private bool _wasCompiledSuccessfullyAlready;
    private CodeViewMode _mode;
    
    [Inject] public ISnackbar Snackbar { get; set; }
    [Inject] public ILocalStorageService Storage { get; set; }

    [Inject] public SnippetsService SnippetsService { get; set; }

    [Inject] public CompilationService CompilationService { get; set; }

    [Inject] public MudExFileService FileService { get; set; }

    [Inject] public IJSInProcessRuntime JsRuntime { get; set; }


    [Inject] public IDialogService DialogService { get; set; }

    [Inject] public IJSUnmarshalledRuntime UnmarshalledJsRuntime { get; set; }


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
                if(_wasCompiledSuccessfullyAlready)
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

    private string MudExVersion
    {
        get
        {
            var v = typeof(MudExIcon).Assembly.GetName().Version;
            return $"v{v.Major}.{v.Minor}.{v.Build}";
        }
    }


    private string MudVersion
    {
        get
        {
            var v = typeof(MudText).Assembly.GetName().Version;
            return $"v{v.Major}.{v.Minor}.{v.Build}";
        }
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

    private async ValueTask SaveState()
    {
         await Storage.SetItemAsync("__temp_code", CodeFiles);
         Snackbar.Add("Save code state for reload.", Severity.Info, options =>
         {
             options.HideTransitionDuration = 100;
             options.ShowTransitionDuration = 100;
             options.VisibleStateDuration = 1000;
         });
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
            if(CodeFiles.Any())  
                activeCodeFile = CodeFiles.First().Value;
        }

        return LoadedSample.None;
    }

    protected override async Task OnInitializedAsync()
    {
        Snackbar.Clear();
        
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

        CodeFileNames = CodeFiles.Keys.ToList();

        await base.OnInitializedAsync();
    }


    private async Task CompileAsync()
    {
        await SaveState();
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

    private void ShowSaveSnippetPopup()
    {
        SaveSnippetPopupVisible = true;
    }

    private void HandleTabActivate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        UpdateActiveCodeFileContent();

        if (CodeFiles.TryGetValue(name, out var codeFile))
        {
            activeCodeFile = codeFile;

            CodeEditorComponent.Focus();
        }
    }

    private void HandleTabClose(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        CodeFiles.Remove(name);
        SaveState();
    }

    private void HandleTabCreate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(name);

        var newCodeFile = new CodeFile { Path = name };

        newCodeFile.Content = newCodeFile.Type == CodeFileType.CSharp
            ? string.Format(CoreConstants.DefaultCSharpFileContentFormat, nameWithoutExtension)
            : string.Format(CoreConstants.DefaultRazorFileContentFormat, nameWithoutExtension);

        CodeFiles.TryAdd(name, newCodeFile);

        JsRuntime.InvokeVoid(Models.Try.Editor.SetLangugage,
            newCodeFile.Type == CodeFileType.CSharp ? "csharp" : "razor");
        SaveState();
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
        var allowedExtensions = new List<string> { "zip", "rar", "razor", "cs" };
        var parameters = new DialogParameters
        {
            { nameof(MudExMessageDialog.Buttons), MudExDialogResultAction.OkCancel("Upload") },
            { nameof(MudExMessageDialog.Icon), Icons.Material.Filled.FileUpload }
        };
        var res = await DialogService.ShowComponentInDialogAsync<MudExUploadEdit<UploadableFile>>("Upload content",
            "Upload content files as zip or separate",
            uploadEdit =>
            {
                uploadEdit.MinHeight = 400;
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
            CodeFileNames = CodeFiles.Keys.ToList();
            HandleTabActivate(CodeFileNames.FirstOrDefault());
            StateHasChanged();
        }
    }

    private async Task Download()
    {
        var stream = SnippetsService.DownloadZipAsync(CodeFiles.Values);
        var id = SnippetId ?? Guid.NewGuid().ToFormattedId();
        await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
        {
            Url = await FileService.CreateDataUrlAsync(stream.ToArray(), "application/zip", true),
            FileName = $"{Path.ChangeExtension($"TryMudEx_{id}", "zip")}",
            MimeType = "application/zip"
        });
    }

    private void ReloadIframe()
    {
        JsRuntime.InvokeVoid(Models.Try.ReloadIframe, "user-page-window", MainUserPagePath);
    }
}