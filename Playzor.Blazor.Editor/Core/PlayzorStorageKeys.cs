namespace Playzor.Blazor.Editor.Core;

/// <summary>
/// Local storage keys of an editor session. Every key is prefixed so several editors on different
/// pages of the same app can keep their own state, and versioned so a layout schema change starts over.
/// </summary>
public sealed class PlayzorStorageKeys
{
    /// <summary>Keys of the default (unprefixed) editor.</summary>
    public static readonly PlayzorStorageKeys Default = new(string.Empty);

    private readonly string _prefix;

    /// <summary>Creates the key set for an editor instance.</summary>
    /// <param name="prefix">Distinguishes editors within the same app, may be empty.</param>
    public PlayzorStorageKeys(string prefix)
    {
        _prefix = string.IsNullOrWhiteSpace(prefix) ? string.Empty : prefix.Trim() + ".";
    }

    /// <summary>Content of the current session, restored on reload.</summary>
    public string Code => $"playzor.{_prefix}code.v1";

    /// <summary>Dockview layout of the current session.</summary>
    public string Layout => $"playzor.{_prefix}layout.v1";

    /// <summary>Files the user has open as editor tabs.</summary>
    public string OpenFiles => $"playzor.{_prefix}openfiles.v1";

    /// <summary>User saved layouts: name to dockview json.</summary>
    public string NamedLayouts => $"playzor.{_prefix}layouts.v1";
}
