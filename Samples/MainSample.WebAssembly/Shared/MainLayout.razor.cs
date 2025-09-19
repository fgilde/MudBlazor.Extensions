using System.Globalization;
using AKSoftware.Localization.MultiLanguages;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core.Types;

namespace MainSample.WebAssembly.Shared;

public partial class MainLayout
{
    public static MainLayout? Instance { get; private set; }
    bool _drawerOpen = true;
    bool _commentsAvailable = true;
    public bool IsOverlayVisible { get; set; }
    public bool IsDark { get; set; } = true;
    public event EventHandler<ThemeChangedEventArgs> ThemeChanged;
    public event EventHandler<EventArgs<CultureInfo>> LanguageChanged;
    [Inject] private IMudMarkdownThemeService MudMarkdownThemeService { get; set; }
    [Inject] private ILanguageContainerService LanguageContainerService { get; set; }

    
    private async void ShowAbout()
    {
        var op = new DialogOptionsEx
        {
            DragMode = MudDialogDragMode.Simple,
            CloseButton = true,
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            FullHeight = true,
            DisablePositionMargin = true,
            DisableSizeMarginY = true,
            Animation = AnimationType.SlideIn,
            Position = DialogPosition.CenterRight,
            DialogAppearance = MudExAppearance.FromCss(MudExCss.Classes.Dialog.ColorfullGlass),
            DialogBackgroundAppearance = MudExAppearance.FromCss(MudExCss.Classes.Backgrounds.MovingDots)
        };


        await DialogService.ShowExAsync<InfoDialog>("Info", op);
    }

    protected override async Task OnInitializedAsync()
    {
        Instance = this;
        await base.OnInitializedAsync();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var codeBlockTheme = GetCodeBlockTheme();
            MudMarkdownThemeService.SetCodeBlockTheme(codeBlockTheme);
        }
        return base.OnAfterRenderAsync(firstRender);
    }


    void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    internal static CodeBlockTheme GetCodeBlockTheme() => Instance is { IsDark: true } ? CodeBlockTheme.AtomOneDark : CodeBlockTheme.AtomOneLight;

    private void HandleThemeChange(bool arg1, ClientTheme arg2)
    {
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs { IsDark = arg1, Theme = arg2 });
        var codeBlockTheme = GetCodeBlockTheme();
        MudMarkdownThemeService.SetCodeBlockTheme(codeBlockTheme);
    }

    public class ThemeChangedEventArgs
    {
        public bool IsDark { get; set; }
        public ClientTheme Theme { get; set; }
    }

    public void SetTheme(ClientTheme theme)
    {
        ClientTheme.CurrentTheme = theme;
        StateHasChanged();
        HandleThemeChange(IsDark, theme);
    }


    public void SetLanguage(CultureInfo info)
    {
        CultureInfo.CurrentUICulture = info;
        LanguageContainerService.SetLanguage(info);
        StateHasChanged();
        LanguageChanged?.Invoke(this, new EventArgs<CultureInfo>(info));
    }
}