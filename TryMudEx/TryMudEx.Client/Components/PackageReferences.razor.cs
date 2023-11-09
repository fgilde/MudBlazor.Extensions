using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Try.Core;

namespace TryMudEx.Client.Components;

public partial class PackageReferences
{
    [Inject] private HttpClient _httpClient { get; set; }
    private NuGetPackageSearcher _nuget;
    private string _search;
    private NugetPackage[] _packages;
    private bool _loading;

    private async Task SearchPackage()
    {
        if (!string.IsNullOrEmpty(_search))
        {
            SetLoading(true);
            _nuget ??= new NuGetPackageSearcher(_httpClient);
            var response = await _nuget.SearchForPackagesAsync(_search);
            _packages = response.Data;
            SetLoading(false);
        }
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
}