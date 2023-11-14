using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Try.Core;
using System.Diagnostics.Contracts;
using System.Linq;
using Nextended.Core.Extensions;
using MudBlazor.Extensions.Helper;

namespace TryMudEx.Client.Components;

public partial class PackageReferences
{
    [Parameter]
    public NugetPackage[] InstalledPackages { get; set; }

    [Inject] private NuGetPackageSearcher _nuget { get; set; }
    private string _search;
    private NugetPackage[] _packages;
    private bool _loading;

    private async Task SearchPackage()
    {
        if (!string.IsNullOrEmpty(_search))
        {
            SetLoading(true);
            var response = await _nuget.SearchForPackagesAsync(_search);
            _packages = response.Data;
            SetLoading(false);
        }
    }

    private bool IsInstalled(NugetPackage package)
    {
        return InstalledPackages.Any(x => x.Id == package.Id);
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
        var installedPackage = InstalledPackages.FirstOrDefault(x => x.Id == package.Id);
        if (string.IsNullOrEmpty(installedPackage?.Version) || (installedPackage.Version.Contains("*") && installedPackage.Version != "*"))
            return package.Versions.FirstOrDefault()?.Version ?? package.Version;
        return installedPackage.Version == "*" ? package.Version : installedPackage.Version;
    }

    private void Install(NugetPackage package, PackageVersion version)
    {
        UnInstall(package);
        var installedPackages = InstalledPackages.ToList();
        package.Version = version.Version;
        package.Id = version.Id ?? package.Id;
        installedPackages.Add(package);
        InstalledPackages = installedPackages.ToArray();

        StateHasChanged();
    }

    private string PackageVersionStyle(NugetPackage package, PackageVersion version)
    {
        version.Id ??= package.Id;
        var when = InstalledPackages.Any(p => p.Id == version.Id && p.Version == version.Version);
        return MudExStyleBuilder.Default.WithFontWeight(MudBlazor.Extensions.Core.Css.FontWeight.Bold, when)
                .WithCursor(MudBlazor.Extensions.Core.Css.Cursor.Default, when)
                .Build();
    }

    private void UnInstall(NugetPackage package)
    {
        var installedPackages = InstalledPackages.ToList();
        if (IsInstalled(package))
        {
            var installed = InstalledPackages.FirstOrDefault(x => x.Id == package.Id);
            installedPackages.Remove(installed);
        }
        InstalledPackages = installedPackages.ToArray();

        StateHasChanged();
    }

    private bool CanChange(NugetPackage package)
    {
        return CoreConstants.DefaultPackages.All(p => p.Id != package.Id);
    }
}