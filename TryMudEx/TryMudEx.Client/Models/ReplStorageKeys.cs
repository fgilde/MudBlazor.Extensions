namespace TryMudEx.Client.Models;

/// <summary>Local storage keys of the repl editor session. Versioned: bump on layout schema changes.</summary>
public static class ReplStorageKeys
{
    public const string TempCode = "__temp_code";
    public const string Layout = "trymudex.layout.v1";
    public const string OpenFiles = "trymudex.openfiles.v1";
}
