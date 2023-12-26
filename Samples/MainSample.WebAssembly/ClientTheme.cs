﻿using MudBlazor;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MainSample.WebAssembly;

public class ClientTheme : MudTheme
{
    public bool ShowLogoInDrawer { get; set; } = true;

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
        DrawerWidthLeft = "300px"
    };

    #endregion

    public static ClientTheme DefaultTheme = new ClientTheme()
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
        Typography = DefaultTypography,
        LayoutProperties = DefaultLayoutProperties
    };

    public static ClientTheme Another = new ClientTheme()
    {
        ShowLogoInDrawer = false,
        Palette = new Palette
        {
            Primary = Colors.Pink.Default,
            AppbarBackground = "#ff0000",
            Background = Colors.Grey.Lighten5,
            DrawerBackground = "#FFF",
            DrawerText = "rgba(0,0,0, 0.7)",
            Success = "#19635d"
        },
    }.SetProperties(t => t.PaletteDark = t.Palette.ToPaletteDark());
    
    public static ClientTheme CurrentTheme = DefaultTheme;

    public static ICollection<ThemePreset<ClientTheme>> All => ThemePreset.Create(() => DefaultTheme, () => Another);

    public static async Task<ICollection<ThemePreset<ClientTheme>>> GetAllThemes(LocalStorageService storageService)
    {
        var result = All;
        try
        {
            var fromStorage = await storageService.GetAllThemeItemsAsync<string>();
            foreach (var item in fromStorage.Where(i => !string.IsNullOrWhiteSpace(i.Value)))
            {
                var theme = MudExThemeHelper.FromJson<ClientTheme>(item.Value);
                result.Add(new ThemePreset<ClientTheme>(item.Key.Split("-").Last(), theme));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);            
        }
        return result;
    }

    #endregion
}