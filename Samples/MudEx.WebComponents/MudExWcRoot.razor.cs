using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace MudEx.WebComponents;

/// <summary>
/// Renders the MudBlazor providers once per page. Every custom element on the page shares this host,
/// so dialogs, popovers and snackbars work from any of them.
/// </summary>
public partial class MudExWcRoot : ComponentBase
{
    private static MudExWcRoot _instance;
    private MudThemeProvider _themeProvider;
    private bool _isDarkMode;
    private bool _darkModeSetExplicitly;

    protected override void OnInitialized() => _instance = this;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _darkModeSetExplicitly)
            return;
        _isDarkMode = await _themeProvider.GetSystemDarkModeAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Called from javascript: window.MudEx.setDarkMode(true)
    /// </summary>
    [JSInvokable]
    public static Task SetDarkMode(bool isDarkMode)
    {
        if (_instance == null)
            return Task.CompletedTask;
        _instance._darkModeSetExplicitly = true;
        _instance._isDarkMode = isDarkMode;
        _instance.StateHasChanged();
        return Task.CompletedTask;
    }
}
