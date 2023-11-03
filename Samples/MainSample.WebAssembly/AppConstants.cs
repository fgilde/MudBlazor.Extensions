using MudBlazor.Extensions.Options;

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

public class AppIds
{
    public const string DropBox = "2ak2m6cfpdeb9f1";
    public const string Google = "787005879852-vkv0cduhl70u087pq4a8s2jtkdgv1n6s.apps.googleusercontent.com";
    public const string OneDrive = "55d00a29-1bb6-40bf-90a6-ecd689a52a51";
    //public static string DropBox => GetVar("DROPBOX_APP_ID");
    //public static string Google => GetVar("GOOGLE_CLIENT_ID");
    //public static string OneDrive => GetVar("ONEDRIVE_APP_ID");

    //private static string GetVar(string name)
    //{
    //    return Environment.GetEnvironmentVariable(name)
    //           ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine)
    //           ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
    //           ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    //}
}