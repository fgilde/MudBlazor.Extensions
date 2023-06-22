using Newtonsoft.Json;

namespace MudBlazor.Extensions.Helper;

public static class MudExThemeHelper
{
    public static PaletteDark ToPaletteDark(this PaletteLight other) => ((Palette)other).ToPaletteDark();
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

    public static TTheme CloneTheme<TTheme>(this TTheme theme) where TTheme : MudTheme 
        => FromJson<TTheme>(JsonConvert.SerializeObject(theme));

    public static TTheme FromJson<TTheme>(string json) where TTheme : MudTheme 
        => JsonConvert.DeserializeObject<TTheme>(JsonHelper.SimplifyMudColorInJson(json));

    public static string AsJson<TTheme>(this TTheme theme) where TTheme : MudTheme
        => JsonHelper.SimplifyMudColorInJson(JsonConvert.SerializeObject(theme));

}