using MainSample.WebAssembly.Shared;
using MainSample.WebAssembly.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Nextended.Core.Extensions;

namespace MainSample.WebAssembly.Pages;

public class BasePage : ComponentBase
{
    private static readonly HashSet<NavigationEntry> _demoEntries = DemoAttribute.AllEntries(true);

    internal readonly HashSet<NavigationEntry> Demos = _demoEntries;

    [Inject] protected IStringLocalizer<BasePage> L { get; set; }
    [Inject] protected NavigationManager Navigation { get; set; }

    internal NavigationEntry? FindCurrentDemoEntry()
    {
        var uri = Navigation.Uri;

        return Demos.FirstOrDefault(d => uri.EndsWith($"/{d.Href}", StringComparison.OrdinalIgnoreCase));
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (MainLayout.Instance != null)
        {
            //MainLayout.Instance.ThemeChanged += (_, _) => StateHasChanged();
            MainLayout.Instance.LanguageChanged += (_, _) => InvokeAsync(StateHasChanged);
        }
    }



}