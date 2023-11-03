namespace Samples.Shared
{
    public class AppIds
    {
        public static string DropBox => GetVar("DROPBOX_APP_ID");
        public static string Google => GetVar("GOOGLE_CLIENT_ID");
        public static string OneDrive => GetVar("ONEDRIVE_APP_ID");

        private static string GetVar(string name)
        {
            return Environment.GetEnvironmentVariable(name)
                   ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine)
                   ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
                   ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}