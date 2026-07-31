using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Playzor.Core;
using System.Linq;
using MudBlazor;
using MudBlazor.Extensions.Helper;
using Microsoft.JSInterop;


namespace Playzor.Blazor.Editor.Components;

public partial class PackageReferences
{
    // Selection lives in a private field: a [Parameter] property gets reset by SetParametersAsync
    // on every external re-render (e.g. when the dialog closes), silently dropping installs.
    private List<NugetPackage> _installed;
    private NugetPackage[] _lastGiven;

    [Parameter]
    public NugetPackage[] InstalledPackages { get; set; }

    /// <summary>
    /// Raised on every install and uninstall. A host without an ok button (the dock panel) uses this
    /// to persist right away; the dialog reads <see cref="SelectedPackages"/> when it closes instead.
    /// </summary>
    [Parameter]
    public EventCallback<NugetPackage[]> SelectedPackagesChanged { get; set; }

    /// <summary>Height limit of the result grid. "none" lets it fill a dock panel.</summary>
    [Parameter]
    public string MaxHeight { get; set; } = "600px";

    public NugetPackage[] SelectedPackages => _installed?.ToArray() ?? InstalledPackages ?? Array.Empty<NugetPackage>();

    protected override void OnParametersSet()
    {
        // only adopt a list the host actually replaced (loading a sample), never the array we
        // handed back ourselves — that would undo the install the user just made
        if (_installed != null && ReferenceEquals(_lastGiven, InstalledPackages)) return;
        _lastGiven = InstalledPackages;
        _installed = (InstalledPackages ?? Array.Empty<NugetPackage>()).ToList();
    }

    [Parameter]
    public bool InfiniteScroll { get; set; } = true;

    [Inject] private NuGetPackageSearcher _nuget { get; set; }
    [Inject] private IScrollManager _scroll { get; set; }
    [Inject] private IJSRuntime _js { get; set; }
    
    public bool IsVirtualized => _packages?.Length > 40;

    private string _search;
    private NugetPackage[] _packages;
    private bool _loading;
    bool _canLoadMore;
    bool _isLoadingMore;
    private async Task<NugetPackage[]> SearchPackage(bool loadAdditional = false)
    {
        if (!string.IsNullOrEmpty(_search))
        {
            _isLoadingMore = loadAdditional;
            SetLoading(true);
            var response = await _nuget.SearchForPackagesAsync(_search, 20, loadAdditional ? _packages?.Length ?? 0 : 0);
            _packages = !loadAdditional ? response.Data : (_packages ?? Array.Empty<NugetPackage>()).Concat(response.Data).ToArray();
            _canLoadMore = response.TotalHits > _packages.Length;
            SetLoading(false);
        }
        return _packages;
    }

    private bool IsInstalled(NugetPackage package)
    {
        return _installed.Any(x => x.Id == package.Id);
    }

    private void SetLoading(bool loading)
    {
        _loading = loading;
        InvokeAsync(StateHasChanged);
    }

    private async Task SearchKeyDown(KeyboardEventArgs obj)
    {
        if (obj.Key == "Enter")
        {
            _packages = null;
            await SearchPackage();
        }
    }

    private string GetInstalledVersion(NugetPackage package)
    {
        var installedPackage = _installed.FirstOrDefault(x => x.Id == package.Id);
        if (string.IsNullOrEmpty(installedPackage?.Version) || (installedPackage.Version.Contains("*") && installedPackage.Version != "*"))
            return package.Versions.FirstOrDefault()?.Version ?? package.Version;
        return installedPackage.Version == "*" ? package.Version : installedPackage.Version;
    }

    private async Task Install(NugetPackage package, PackageVersion version)
    {
        _installed.RemoveAll(x => x.Id == package.Id);
        package.Version = version.Version;
        package.Id = version.Id ?? package.Id;
        _installed.Add(package);

        await NotifyChangedAsync();
    }

    private string PackageVersionStyle(NugetPackage package, PackageVersion version)
    {
        version.Id ??= package.Id;
        var when = _installed.Any(p => p.Id == version.Id && p.Version == version.Version);
        return MudExStyleBuilder.Default.WithFontWeight(MudBlazor.Extensions.Core.Css.FontWeight.Bold, when)
                .WithCursor(MudBlazor.Extensions.Core.Css.Cursor.Default, when)
                .Build();
    }

    private async Task UnInstall(NugetPackage package)
    {
        _installed.RemoveAll(x => x.Id == package.Id);

        await NotifyChangedAsync();
    }

    private async Task NotifyChangedAsync()
    {
        StateHasChanged();
        if (!SelectedPackagesChanged.HasDelegate) return;
        // the host stores what we send, so remember it as ours — OnParametersSet must not adopt it back
        _lastGiven = SelectedPackages;
        await SelectedPackagesChanged.InvokeAsync(_lastGiven);
    }

    private bool CanChange(NugetPackage package)
    {
        return CoreConstants.DefaultPackages.All(p => p.Id != package.Id);
    }

    private async Task OnGridScroll(EventArgs arg)
    {
        if (InfiniteScroll)
        {
            var isAtBottom = await _js.InvokeAsync<bool>("isScrollAtBottom", ".package-result-grid");
            if (isAtBottom && _canLoadMore)
            {
                await LoadMore();
            }
        }
    }

    private async Task LoadMore()
    {
        await SearchPackage(true);
        if (!InfiniteScroll)
        {
            await _scroll.ScrollToBottomAsync("package-result-grid", ScrollBehavior.Smooth);
        }
    }

}