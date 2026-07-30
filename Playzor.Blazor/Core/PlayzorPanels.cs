namespace Playzor.Blazor.Core;

/// <summary>
/// Dock panels the editor brings along. The code editors themselves are always there, everything
/// else can be left out - and own panels can be added as <see cref="Components.PlayzorEditor.ChildContent"/>.
/// </summary>
[Flags]
public enum PlayzorPanels
{
    /// <summary>Only the code editors.</summary>
    None = 0,

    /// <summary>File tree with create, rename, delete and templates.</summary>
    Files = 1 << 0,

    /// <summary>The iframe that runs the compiled component.</summary>
    Preview = 1 << 1,

    /// <summary>Compiler errors and warnings, clickable.</summary>
    Errors = 1 << 2,

    /// <summary>Console output of the running component.</summary>
    Console = 1 << 3,

    /// <summary>Everything (the default).</summary>
    All = Files | Preview | Errors | Console
}
