using MudBlazor;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core.Enums;
using MudBlazor.Extensions.Helper;
using Nextended.Core.DeepClone;
using Nextended.Core.Extensions;


namespace MainSample.WebAssembly;

public class ClientTheme : MudTheme
{
    public string LogoStyle { get; set; } = "height: 25px; margin-right: 10px; margin-bottom: -5px";
    public bool FloatingCommentsButton { get; set; }
    public bool ShowFilterInDrawer { get; set; } = true;
    public bool ShowLogoInDrawer { get; set; }
    public MaxWidth ContentMaxWidth { get; set; } = MaxWidth.ExtraExtraLarge;
    public TreeViewExpandBehaviour NavigationExpandMode { get; set; }
    public TreeViewMode NavigationViewMode { get; set; } = TreeViewMode.Default;

    public DrawerClipMode DrawerClipMode { get; set; }
    public DrawerVariant DrawerVariant { get; set; } = DrawerVariant.Responsive;

    #region Statics

    #region Default Typography and Layout

    private static Typography DefaultTypography => new()
    {
        Default = new DefaultTypography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = ".875rem",
            FontWeight = "400",
            LineHeight = "1.43",
            LetterSpacing = ".01071em"
        },
        H1 = new H1Typography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "6rem",
            FontWeight = "300",
            LineHeight = "1.167",
            LetterSpacing = "-.01562em"
        },
        H2 = new H2Typography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "3.75rem",
            FontWeight = "300",
            LineHeight = "1.2",
            LetterSpacing = "-.00833em"
        },
        H3 = new H3Typography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "3rem",
            FontWeight = "400",
            LineHeight = "1.167",
            LetterSpacing = "0"
        },
        H4 = new H4Typography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "2.125rem",
            FontWeight = "400",
            LineHeight = "1.235",
            LetterSpacing = ".00735em"
        },
        H5 = new H5Typography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "1.5rem",
            FontWeight = "400",
            LineHeight = "1.334",
            LetterSpacing = "0"
        },
        H6 = new H6Typography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "1.25rem",
            FontWeight = "400",
            LineHeight = "1.6",
            LetterSpacing = ".0075em"
        },
        Button = new ButtonTypography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = ".875rem",
            FontWeight = "500",
            LineHeight = "1.75",
            LetterSpacing = ".02857em"
        },
        Body1 = new Body1Typography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = "1rem",
            FontWeight = "400",
            LineHeight = "1.5",
            LetterSpacing = ".00938em"
        },
        Body2 = new Body2Typography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = ".875rem",
            FontWeight = "400",
            LineHeight = "1.43",
            LetterSpacing = ".01071em"
        },
        Caption = new CaptionTypography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = ".75rem",
            FontWeight = "400",
            LineHeight = "1.66",
            LetterSpacing = ".03333em"
        },
        Subtitle2 = new Subtitle2Typography()
        {
            FontFamily = new[] { "Montserrat", "Helvetica", "Arial", "sans-serif" },
            FontSize = ".875rem",
            FontWeight = "500",
            LineHeight = "1.57",
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

    public static ClientTheme LeptonTheme = MudLeptonTheme.Create();
    public static ClientTheme DefaultTheme = new ClientTheme()
    {
        LogoStyle = "height: 50px; margin-right: 10px; margin-top: 5px",
        NavigationExpandMode = TreeViewExpandBehaviour.SingleExpand,
        FloatingCommentsButton = true,
        ContentMaxWidth = MaxWidth.False,
        PaletteLight = new PaletteLight()
        {
            Primary = "#594AE2",
            Secondary = "#FF4081",
            Tertiary = "#1EC8A5",
            AppbarBackground = "#1f2226",
            Background = Colors.Gray.Lighten5,
            DrawerBackground = "#FFF",
            DrawerText = "rgba(0,0,0, 0.7)",
            Success = "#19635d"
        },
        DrawerClipMode = DrawerClipMode.Docked,
        DrawerVariant = DrawerVariant.Responsive,
        //   Typography = DefaultTypography, // TODO: MudBlazor 8
        LayoutProperties = DefaultLayoutProperties.SetProperties(l =>
        {
            l.DrawerWidthLeft = l.DrawerWidthRight = "400px";
            l.DefaultBorderRadius = "8px";
        })
    }.SetProperties(t => t.PaletteDark = t.PaletteLight.ToPaletteDark().SetProperties(dark =>
    {
        dark.AppbarBackground = "#121518";
        dark.DrawerBackground = "#121518";
        dark.Background = "#121518";
        dark.Surface = "#222529";
    }));


    public static ClientTheme Version2 = new ClientTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#199b90",
            AppbarBackground = "#1f2226",
            Background = Colors.Gray.Lighten5,
            DrawerBackground = "#FFF",
            DrawerText = "rgba(0,0,0, 0.7)",
            Success = "#19635d"
        },
        DrawerClipMode = DrawerClipMode.Docked,
        DrawerVariant = DrawerVariant.Responsive,
        //   Typography = DefaultTypography, // TODO: MudBlazor 8
        LayoutProperties = DefaultLayoutProperties
    }.SetProperties(t => t.PaletteDark = t.PaletteLight.ToPaletteDark().SetProperties(dark =>
    {
        dark.AppbarBackground = "#1f2226";
        dark.DrawerBackground = "#1f2226";
        dark.Background = "#121518";
        dark.Surface = "#222529";
    }));
    

    public static ClientTheme InitialTheme = new ClientTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#199b90",
            AppbarBackground = "#199b90",
            Background = Colors.Gray.Lighten5,
            DrawerBackground = "#FFF",
            DrawerText = "rgba(0,0,0, 0.7)",
            Success = "#19635d"
        },
        ShowLogoInDrawer = true,
     //   Typography = DefaultTypography, // TODO: MudBlazor 8
        LayoutProperties = DefaultLayoutProperties
    };

    public static ClientTheme SimpleBlue = new ClientTheme()
    {
        ShowFilterInDrawer = false,
        PaletteLight = new PaletteLight
        {
            Primary = "#00cffc",
            AppbarBackground = "#0f2334",
            Background = Colors.Gray.Lighten5,
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
    }.SetProperties(t => t.PaletteDark = t.PaletteLight.ToPaletteDark());
    
    public static ClientTheme CurrentTheme = DefaultTheme;

    public static ICollection<ThemePreset<ClientTheme>> All =>
    [
        new("Default", DefaultTheme),
        new("Lepton", LeptonTheme),
        new("MudEx 2 (last)", Version2),
        new("MudEx 1 (first)", InitialTheme),
        new("Simple Blue", SimpleBlue)
    ];


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


public static class MudLeptonTheme
{
    // ── LeptonX semantic colors (shared across all themes) ──
    private const string Primary = "#355dff";
    private const string Secondary = "#6c5dd3";
    private const string Success = "#4fbf67";
    private const string Info = "#438aa7";
    private const string Warning = "#ff9f38";
    private const string Danger = "#c00d49";

    public static ClientTheme Create() => new()
    {
        PaletteLight = BuildLightPalette(),
        PaletteDark = BuildDarkPalette(),
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "0.5rem",    // --lpx-radius
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "sans-serif"],  // --bs-body-font-family
                FontSize = "0.875rem",                 // --bs-body-font-size
            },
        },
    };

    private static PaletteLight BuildLightPalette() => new()
    {
        // ── Semantic ──
        Primary = Primary,
        Secondary = Secondary,
        Success = Success,
        Info = Info,
        Warning = Warning,
        Error = Danger,

        // ── Backgrounds & Surfaces ──
        Background = "#f0f4f7",       // --lpx-content-bg (light)
        BackgroundGray = "#e8eef3",   // --lpx-border-color (light)
        Surface = "#fff",             // --lpx-card-bg (light)
        AppbarBackground = "#fff",    // --lpx-navbar-color (light)
        AppbarText = "#445f72",       // --lpx-navbar-text-color (light)
        DrawerBackground = "#fff",
        DrawerText = "#445f72",       // --lpx-navbar-text-color (light)

        // ── Text ──
        TextPrimary = "#325168",      // --lpx-content-text (light)
        TextSecondary = "#445f72",    // --lpx-dark (light)
        TextDisabled = "#c6d2dc",     // --lpx-light (light)

        // ── Lines & Dividers ──
        Divider = "#e8eef3",          // --lpx-border-color (light)
        DividerLight = "#f0f4f7",     // --lpx-content-bg (light)
        LinesDefault = "#e8eef3",     // --lpx-border-color (light)
        LinesInputs = "#c6d2dc",      // --lpx-light (light)

        // ── Table ──
        TableHover = "rgba(53,93,255,0.04)",
        TableStriped = "rgba(0,0,0,0.02)",

        // ── Actions ──
        ActionDefault = "#445f72",    // --lpx-dark (light)
        ActionDisabled = "rgba(50,81,104,0.3)",
        ActionDisabledBackground = "rgba(50,81,104,0.12)",
    };

    private static PaletteDark BuildDarkPalette() => new()
    {
        // ── Semantic ──
        Primary = Primary,
        Secondary = Secondary,
        Success = Success,
        Info = Info,
        Warning = Warning,
        Error = Danger,

        // ── Backgrounds & Surfaces ──
        // Works for both "dark" (bg #121212, card #1b1b1b) and "dim" (navbar #161616)
        Background = "#121212",       // --lpx-content-bg (dark)
        BackgroundGray = "#222",      // --lpx-border-color (dark)
        Surface = "#1b1b1b",          // --lpx-card-bg (dark)
        AppbarBackground = "#161616", // --lpx-navbar-color (dark)
        AppbarText = "#fff",          // --lpx-navbar-active-text-color (dark)
        DrawerBackground = "#161616",
        DrawerText = "#777d87",       // --lpx-navbar-text-color (dark)

        // ── Text ──
        TextPrimary = "#eee",         // --lpx-card-title-text-color (dark)
        TextSecondary = "#9ca5b4",    // --lpx-content-text (dark)
        TextDisabled = "#444",        // --lpx-light (dark)

        // ── Lines & Dividers ──
        Divider = "#222",             // --lpx-border-color (dark)
        DividerLight = "#1b1b1b",     // --lpx-card-bg (dark)
        LinesDefault = "#222",        // --lpx-border-color (dark)
        LinesInputs = "#444",         // --lpx-light (dark)

        // ── Table ──
        TableHover = "rgba(53,93,255,0.08)",
        TableStriped = "rgba(255,255,255,0.02)",

        // ── Actions ──
        ActionDefault = "#bbb",       // --lpx-dark (dark)
        ActionDisabled = "rgba(156,165,180,0.3)",
        ActionDisabledBackground = "rgba(156,165,180,0.12)",
    };
}
