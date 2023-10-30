namespace MainSample.WebAssembly;

public static class AppConstants
{
    public static class Urls
    {
        public const string TryOnline = "https://trymudex.azurewebsites.net";

        public static string? GetTryOnline(bool isDark) => TryOnline + (isDark ? "?dark" : "?light");
    }
}