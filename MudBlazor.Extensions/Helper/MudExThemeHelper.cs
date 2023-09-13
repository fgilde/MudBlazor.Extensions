using Newtonsoft.Json;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Static util MudExThemeHelper
/// </summary>
public static class MudExThemeHelper
{
    /// <summary>
    /// Convert PaletteLight to PaletteDark
    /// </summary>
    public static PaletteDark ToPaletteDark(this PaletteLight other) => ((Palette)other).ToPaletteDark();
    
    /// <summary>
    /// Convert PaletteLight to PaletteDark
    /// </summary>    
    public static PaletteDark ToPaletteDark(this Palette other)
    {
        return new PaletteDark
        {
            AppbarBackground = other.AppbarBackground,
            Primary = other.Primary,
            Success = other.Success,
            Secondary = other.Secondary,
            Info = other.Info,
            Warning = other.Warning
        };
    }

    /// <summary>
    /// Convert PaletteDark to PaletteLight
    /// </summary>
    public static PaletteLight ToPaletteLight(this PaletteDark other)
    {
        return new PaletteLight
        {
            AppbarBackground = other.AppbarBackground,
            Primary = other.Primary,
            Success = other.Success,
            Secondary = other.Secondary,
            Info = other.Info,
            Warning = other.Warning
        };
    }

    /// <summary>
    /// Clones the theme
    /// </summary>
    /// <typeparam name="TTheme"></typeparam>
    /// <param name="theme"></param>
    /// <returns>Cloned theme with same options</returns>
    public static TTheme CloneTheme<TTheme>(this TTheme theme) where TTheme : MudTheme 
        => FromJson<TTheme>(JsonConvert.SerializeObject(theme));

    /// <summary>
    /// Creates a new theme from json
    /// </summary>
    public static TTheme FromJson<TTheme>(string json) where TTheme : MudTheme 
        => JsonConvert.DeserializeObject<TTheme>(MudExJsonHelper.SimplifyMudColorInJson(json));

    /// <summary>
    /// Converts Theme to json
    /// </summary>
    public static string AsJson<TTheme>(this TTheme theme) where TTheme : MudTheme
        => MudExJsonHelper.SimplifyMudColorInJson(JsonConvert.SerializeObject(theme));

}