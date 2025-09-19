using MainSample.WebAssembly.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace MainSample.WebAssembly.Pages;

public class BasePage : ComponentBase
{

    [Inject] protected IStringLocalizer<BasePage> L { get; set; }

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