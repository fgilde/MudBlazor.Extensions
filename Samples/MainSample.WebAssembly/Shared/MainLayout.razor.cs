using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;

namespace MainSample.WebAssembly.Shared;

public partial class MainLayout
{
    private string filter;
    private string hoveredFilter;
    private List<string> filters = new();
    public static MainLayout? Instance { get; private set; }
    bool _drawerOpen = true;
    bool _commentsAvailable = true;
    public bool IsOverlayVisible { get; set; }
    public bool IsDark { get; set; } = true;
    public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

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


        await DialogService.ShowEx<InfoDialog>("Info", op);
    }

    protected override async Task OnInitializedAsync()
    {
        Instance = this;
        await base.OnInitializedAsync();
    }



    void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    internal static CodeBlockTheme GetCodeBlockTheme() => Instance is { IsDark: true } ? CodeBlockTheme.AtomOneDark : CodeBlockTheme.AtomOneLight;

    private async void HandleThemeChange(bool arg1, ClientTheme arg2)
    {
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs { IsDark = arg1, Theme = arg2 });
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

    private void SearchKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            if (string.IsNullOrWhiteSpace(filter))
                filters.Clear();
            filter = string.Empty;
        }
        else if (args.Key == "Delete" && string.IsNullOrWhiteSpace(filter))
        {
            filter.Remove(filters.Count - 1);
        }
    }

    private string GetToHighlight() => !string.IsNullOrWhiteSpace(hoveredFilter) && filters.Contains(hoveredFilter) ? hoveredFilter : filter;

    private void OnTagOver(ChipMouseEventArgs<string> arg)
    {
        hoveredFilter = arg.Value;
    }

    private void OnTagOut(ChipMouseEventArgs<string> arg)
    {
        hoveredFilter = string.Empty;
    }
}