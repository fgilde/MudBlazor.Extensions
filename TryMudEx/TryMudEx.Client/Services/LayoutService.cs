using MudBlazor;

namespace TryMudEx.Client.Services;

using System;
using System.Threading.Tasks;
using UserPreferences;

public class LayoutService
{
    private readonly IUserPreferencesService _userPreferencesService;
    private UserPreferences.UserPreferences _userPreferences;
    
    public bool IsDarkMode { get; private set; } = true;

    public MudTheme Theme { get; private set; } = new()
    {
        Palette = new PaletteLight
        {
            Primary = "#199b90",
            AppbarBackground = "#199b90",
            Background = Colors.Grey.Lighten5,
            DrawerBackground = "#FFF",
            DrawerText = "rgba(0,0,0, 0.7)",
            Success = "#19635d"
        }
    };

    public LayoutService(IUserPreferencesService userPreferencesService)
    {
        _userPreferencesService = userPreferencesService;
    }
    
    public void SetDarkMode(bool value)
    {
        IsDarkMode = value;
    }
    
    public async Task ApplyUserPreferences(bool isDarkModeDefaultTheme)
    {
        _userPreferences = await _userPreferencesService.LoadUserPreferences();
        if (_userPreferences != null)
        {
            IsDarkMode = _userPreferences.DarkTheme;
        }
        else
        {
            IsDarkMode = isDarkModeDefaultTheme;
            _userPreferences = new UserPreferences.UserPreferences {DarkTheme = IsDarkMode};
            await _userPreferencesService.SaveUserPreferences(_userPreferences);
        }
    }
    
    public event EventHandler MajorUpdateOccured;

    private  void OnMajorUpdateOccured() => MajorUpdateOccured?.Invoke(this,EventArgs.Empty);
    
    public async Task ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        _userPreferences.DarkTheme = IsDarkMode;
        await _userPreferencesService.SaveUserPreferences(_userPreferences);
        OnMajorUpdateOccured();
    }
}