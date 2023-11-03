using MudBlazor.Extensions.Options;
using Samples.Shared;

namespace MainSample.WebAssembly;

public static class AppConstants
{
    public static Action<MudExConfiguration> MudExConfiguration = c =>
        c.WithoutAutomaticCssLoading()
            .EnableDropBoxIntegration(AppIds.DropBox)
            .EnableGoogleDriveIntegration(AppIds.Google)
            .EnableOneDriveIntegration(AppIds.OneDrive);

    public static class Urls
    {
        public const string TryOnline = "https://trymudex.azurewebsites.net";

        public static string? GetTryOnline(bool isDark) => TryOnline + (isDark ? "?dark" : "?light");
    }
}