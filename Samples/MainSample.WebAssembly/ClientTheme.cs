using MainSample.WebAssembly.Shared;
using MudBlazor;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;
using SixLabors.ImageSharp.Processing;

namespace MainSample.WebAssembly;

public class ClientTheme : MudTheme
{
    public bool ShowFilterInDrawer { get; set; } = true;
    public bool ShowLogoInDrawer { get; set; }

    public ExpandMode NavigationExpandMode { get; set; }

    public DrawerClipMode DrawerClipMode { get; set; }
    public DrawerVariant DrawerVariant { get; set; } = DrawerVariant.Responsive;

    #region Statics

    #region Default Typography and Layout

    private static Typography DefaultTypography => new()
    {
        Default = new Default()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = ".875rem",
            FontWeight = 400,
            LineHeight = 1.43,
            LetterSpacing = ".01071em"
        },
        H1 = new H1()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "6rem",
            FontWeight = 300,
            LineHeight = 1.167,
            LetterSpacing = "-.01562em"
        },
        H2 = new H2()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "3.75rem",
            FontWeight = 300,
            LineHeight = 1.2,
            LetterSpacing = "-.00833em"
        },
        H3 = new H3()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "3rem",
            FontWeight = 400,
            LineHeight = 1.167,
            LetterSpacing = "0"
        },
        H4 = new H4()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "2.125rem",
            FontWeight = 400,
            LineHeight = 1.235,
            LetterSpacing = ".00735em"
        },
        H5 = new H5()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "1.5rem",
            FontWeight = 400,
            LineHeight = 1.334,
            LetterSpacing = "0"
        },
        H6 = new H6()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "1.25rem",
            FontWeight = 400,
            LineHeight = 1.6,
            LetterSpacing = ".0075em"
        },
        Button = new Button()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = ".875rem",
            FontWeight = 500,
            LineHeight = 1.75,
            LetterSpacing = ".02857em"
        },
        Body1 = new Body1()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "1rem",
            FontWeight = 400,
            LineHeight = 1.5,
            LetterSpacing = ".00938em"
        },
        Body2 = new Body2()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = ".875rem",
            FontWeight = 400,
            LineHeight = 1.43,
            LetterSpacing = ".01071em"
        },
        Caption = new Caption()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = ".75rem",
            FontWeight = 400,
            LineHeight = 1.66,
            LetterSpacing = ".03333em"
        },
        Subtitle2 = new Subtitle2()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = ".875rem",
            FontWeight = 500,
            LineHeight = 1.57,
            LetterSpacing = ".00714em"
        }
    };

    private static LayoutProperties DefaultLayoutProperties => new()
    {
        DefaultBorderRadius = "3px",
        DrawerWidthLeft = "300px",
        AppbarHeight = "55px"
    };

    #endregion

    public static ClientTheme DefaultTheme = new ClientTheme()
    {
        Palette = new Palette
        {
            Primary = "#199b90",
            AppbarBackground = "#1f2226",
            Background = Colors.Grey.Lighten5,
            DrawerBackground = "#FFF",
            DrawerText = "rgba(0,0,0, 0.7)",
            Success = "#19635d"
        },
        DrawerClipMode = DrawerClipMode.Docked,
        DrawerVariant = DrawerVariant.Responsive,
        Typography = DefaultTypography,
        LayoutProperties = DefaultLayoutProperties
    }.SetProperties(t => t.PaletteDark = t.Palette.ToPaletteDark().SetProperties(dark =>
    {
        dark.AppbarBackground = "#1f2226";
        dark.DrawerBackground = "#1f2226";
        dark.Background = "#121518";
        dark.Surface = "#222529";
    }));

    public static ClientTheme Last = new ClientTheme()
    {
        Palette = new Palette
        {
            Primary = "#199b90",
            AppbarBackground = "#199b90",
            Background = Colors.Grey.Lighten5,
            DrawerBackground = "#FFF",
            DrawerText = "rgba(0,0,0, 0.7)",
            Success = "#19635d"
        },
        ShowLogoInDrawer = true,
        Typography = DefaultTypography,
        LayoutProperties = DefaultLayoutProperties
    };

    public static ClientTheme SimpleBlue = new ClientTheme()
    {
        ShowFilterInDrawer = false,
        Palette = new Palette
        {
            Primary = "#00cffc",
            AppbarBackground = "#0f2334",
            Background = Colors.Grey.Lighten5,
            DrawerBackground = "#FFF",
            DrawerText = "rgba(0,0,0, 0.7)",
            Success = "#19635d",
        },
        DrawerClipMode = DrawerClipMode.Docked,
        DrawerVariant = DrawerVariant.Responsive,
        LayoutProperties = DefaultLayoutProperties.SetProperties(p =>
        {
            p.DefaultBorderRadius = "10px";
            p.DrawerWidthLeft = "250px";
            p.AppbarHeight = "40px";
        }),
    }.SetProperties(t => t.PaletteDark = t.Palette.ToPaletteDark());
    
    public static ClientTheme CurrentTheme = DefaultTheme;

    public static ICollection<ThemePreset<ClientTheme>> All => ThemePreset.Create(() => DefaultTheme, () => Last, () => SimpleBlue);

    public static async Task<ICollection<ThemePreset<ClientTheme>>> GetAllThemes(LocalStorageService storageService)
    {
        var result = All;
        var fromStorage = await storageService.GetAllThemeItemsAsync<string>();
        foreach (var item in fromStorage)
        {
            var theme = MudExThemeHelper.FromJson<ClientTheme>(item.Value);
            result.Add(new ThemePreset<ClientTheme>(item.Key.Split("-").Last(), theme));
        }
        return result;
    }

    #endregion
}

public class StoredThemeItem<T>
{
    public string Key { get; set; }
    public T Value { get; set; }
}