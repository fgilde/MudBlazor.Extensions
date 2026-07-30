namespace Playzor.Blazor.Core;

/// <summary>
/// Built in buttons of the editor tool bar. Combine the ones you want, or drop them all and put
/// your own into <see cref="Components.PlayzorEditor.ToolBarContent"/>.
/// </summary>
[Flags]
public enum PlayzorToolButtons
{
    /// <summary>No built in button at all.</summary>
    None = 0,

    /// <summary>Compiles the current files and reloads the preview (also bound to ctrl+s).</summary>
    Run = 1 << 0,

    /// <summary>Hands the current files to the host. Only rendered when <see cref="Components.PlayzorEditor.OnSaveRequested"/> is set.</summary>
    Save = 1 << 1,

    /// <summary>Asks the host for an embed link. Only rendered when <see cref="Components.PlayzorEditor.OnEmbedRequested"/> is set.</summary>
    Embed = 1 << 2,

    /// <summary>Downloads all files as a zip archive.</summary>
    Download = 1 << 3,

    /// <summary>Uploads files from a zip archive and replaces the current ones.</summary>
    Upload = 1 << 4,

    /// <summary>Opens the sample picker. Only rendered when <see cref="Components.PlayzorEditor.Samples"/> is not empty.</summary>
    Samples = 1 << 5,

    /// <summary>Opens the nuget package manager.</summary>
    Packages = 1 << 6,

    /// <summary>Menu to show, hide and pop out the dock panels.</summary>
    Panels = 1 << 7,

    /// <summary>Menu to save, apply and reset dock layouts.</summary>
    Layout = 1 << 8,

    /// <summary>Dark and light toggle. Only rendered when <see cref="Components.PlayzorEditor.DarkModeChanged"/> is set.</summary>
    Theme = 1 << 9,

    /// <summary>Everything the editor can do on its own, without a host wiring events.</summary>
    Standalone = Run | Download | Upload | Packages | Panels | Layout,

    /// <summary>Every built in button (the default).</summary>
    All = Standalone | Save | Embed | Samples | Theme
}
