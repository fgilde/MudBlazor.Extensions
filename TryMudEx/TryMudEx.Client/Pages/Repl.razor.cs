using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;
using Nextended.Blazor.Models;
using Nextended.Core.Encode;
using Nextended.Core.Extensions;
using Playzor.Blazor.Components;
using Playzor.Blazor.Core;
using Try.Core;
using TryMudEx.Client.Components;
using TryMudEx.Client.Services;
using Playzor.Blazor.Services;

namespace TryMudEx.Client.Pages;

/// <summary>
/// The playground page. Everything the editor itself does lives in
/// <see cref="PlayzorEditor"/>; this page only brings the parts that belong to this site:
/// brand, snippet storage, samples and the app theme.
/// </summary>
public partial class Repl
{
    [Inject] private LayoutService LayoutService { get; set; }
    [Inject] private BrandingService Branding { get; set; }
    [Inject] private SnippetsService SnippetsService { get; set; }
    [Inject] private ILocalStorageService Storage { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; }
    [Inject] private PlayzorLocalizer L { get; set; }

    [Parameter] public string SnippetId { get; set; }
    [Parameter] public string Sample { get; set; }
    [Parameter] public string SnippetFileUrl { get; set; }

    private PlayzorEditor _editor;
    private IEnumerable<CodeFile> _files;
    private IEnumerable<CodeFile> _snippetFiles = Array.Empty<CodeFile>();
    private string[] _samples = Array.Empty<string>();

    private bool SaveSnippetPopupVisible { get; set; }

    // the preview is its own app instance and resolves its brand from its own url. On a real
    // domain the host answers that; while developing with ?brand= it has to be passed on.
    private string BrandQuery(string separator)
        => string.IsNullOrEmpty(Branding.DevBrandOverride) ? string.Empty : $"{separator}brand={Branding.DevBrandOverride}";

    private string UserPageUrl => "/user-page" + BrandQuery("?");

    private string CompiledPageUrl => "/__main" + BrandQuery("?");

    protected override async Task OnInitializedAsync()
    {
        Snackbar.Clear();

        _ = SnippetsService.GetSamplesAsync().ContinueWith(t =>
        {
            _samples = t.Result;
            StateHasChanged();
        });

        await LoadFilesAsync();
        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Loads what the route asks for. Only a plain /snippet url keeps the last session, everything
    /// else replaces it, so opening a sample never mixes with what was there before.
    /// </summary>
    private async Task LoadFilesAsync()
    {
        var isSnippet = !string.IsNullOrWhiteSpace(SnippetId) && string.IsNullOrWhiteSpace(Sample);
        var isSample = !isSnippet && !string.IsNullOrWhiteSpace(Sample);
        var isFromUrl = !isSnippet && !isSample && !string.IsNullOrWhiteSpace(SnippetFileUrl);

        if (!isSnippet && !isSample && !isFromUrl)
            return; // no route content: the editor restores its own session

        try
        {
            if (isFromUrl)
            {
                var url = SnippetFileUrl.StartsWith("http") || SnippetFileUrl.StartsWith("blob") || DataUrl.IsDataUrl(SnippetFileUrl)
                    ? SnippetFileUrl
                    : SnippetFileUrl.EncodeDecode().Base64.Decode();
                _files = (await SnippetsService.GetSnippetContentFromUrlAsync(url)).ToList();
            }
            else
            {
                _files = isSnippet
                    ? (await SnippetsService.GetSnippetContentAsync(SnippetId)).ToList()
                    : (await SnippetsService.LoadSampleAsync(Sample)).ToList();
            }

            if (!_files.Any())
                Snackbar.Add("No files in snippet or sample.", Severity.Error);
        }
        catch (ArgumentException)
        {
            Snackbar.Add("Invalid Snippet ID.", Severity.Error);
        }
        catch (Exception e)
        {
            Snackbar.Add("Unable to get snippet content. Please try again later.", Severity.Error);
            Console.WriteLine(e.Message);
        }
    }

    private async Task OpenSampleAsync(string sample)
    {
        if (string.IsNullOrWhiteSpace(sample)) return;

        // the editor keeps its session in local storage — drop it so the sample wins on a reload too
        var keys = new PlayzorStorageKeys("playzor");
        await Storage.RemoveItemAsync(keys.Code);
        await Storage.RemoveItemAsync(keys.OpenFiles);

        Sample = sample;
        SnippetId = null;
        SnippetFileUrl = null;
        NavigationManager.NavigateTo($"/snippet/samples/{sample}", false);

        await LoadFilesAsync();
        StateHasChanged();

        if (_editor != null)
            await _editor.TriggerCompileAsync();
    }

    private async Task UpdateThemeAsync(bool dark)
    {
        await LayoutService.ToggleDarkMode(dark);
    }

    private void ShowSaveSnippetPopup(IEnumerable<CodeFile> files)
    {
        _snippetFiles = files.ToList();
        SaveSnippetPopupVisible = true;
    }

    private async Task ShowEmbedDialogAsync(IEnumerable<CodeFile> files)
    {
        await DialogService.ShowComponentInDialogAsync<EmbedDialog>(L["Embed this snippet"],
            L["Paste the snippet into any page — the code travels inside the url, nothing needs to be saved."],
            cmp =>
            {
                cmp.Files = files.ToList();
                cmp.SnippetId = SnippetId;
            },
            new DialogParameters { { nameof(MudExMessageDialog.Icon), Icons.Material.Outlined.Code } },
            options =>
            {
                options.Resizeable = true;
                options.FullWidth = true;
                options.MaxWidth = MaxWidth.Large;
                options.CloseButton = true;
                options.DragMode = MudDialogDragMode.Simple;
            });
    }

    /// <summary>Packages a fresh snippet starts with — MudEx ships its own, other brands may not.</summary>
    private List<INugetPackageReference> BrandDefaultPackages()
        => CoreConstants.DefaultPackages
            .Where(p => Branding.Current.DefaultPackages.Contains(p.Id))
            .ToList();
}
