using MudBlazor.Extensions.Options;

namespace MainSample.WebAssembly;

public static class AppConstants
{
    public static Action<MudExConfiguration> MudExConfiguration = c =>
        c.WithoutAutomaticCssLoading()
            .EnableDropBoxIntegration("2ak2m6cfpdeb9f1")
            .EnableGoogleDriveIntegration("787005879852-vkv0cduhl70u087pq4a8s2jtkdgv1n6s.apps.googleusercontent.com")
            .EnableOneDriveIntegration("55d00a29-1bb6-40bf-90a6-ecd689a52a51");
    public static class Urls
    {
        public const string TryOnline = "https://trymudex.azurewebsites.net";

        public static string? GetTryOnline(bool isDark) => TryOnline + (isDark ? "?dark" : "?light");
    }
}