namespace Playzor.Blazor.Editor.Core;

/// <summary>
/// Names of the javascript functions the editor talks to. The script itself ships with the package
/// as <c>_content/Playzor.Blazor.Editor/js/playzor-editor.js</c>.
/// </summary>
public static class PlayzorJs
{
    /// <summary>Registers the dotnet reference the script calls back into (ctrl+s, run message).</summary>
    public const string Initialize = "Playzor.initialize";

    /// <summary>Replaces the browser url without navigating.</summary>
    public const string ChangeDisplayUrl = "Playzor.changeDisplayUrl";

    /// <summary>Reloads the preview iframe with a cache busted url.</summary>
    public const string ReloadIframe = "Playzor.reloadIframe";

    /// <summary>True when the browser prefers a dark color scheme.</summary>
    public const string PrefersDark = "Playzor.prefersDark";

    /// <summary>Releases the dotnet reference and the window handlers.</summary>
    public const string Dispose = "Playzor.dispose";

    /// <summary>Monaco instances, one per dock panel.</summary>
    public static class Editor
    {
        /// <summary>Creates a monaco instance for a dom id.</summary>
        public const string Create = "Playzor.Editor.create";

        /// <summary>Current text of one instance.</summary>
        public const string GetValue = "Playzor.Editor.getValue";

        /// <summary>Current text of every live instance, keyed by dom id.</summary>
        public const string GetValues = "Playzor.Editor.getValues";

        /// <summary>Sets the text of one instance.</summary>
        public const string SetValue = "Playzor.Editor.setValue";

        /// <summary>Toggles read only.</summary>
        public const string SetReadOnly = "Playzor.Editor.setReadOnly";

        /// <summary>Switches the monaco language.</summary>
        public const string SetLanguage = "Playzor.Editor.setLanguage";

        /// <summary>Focuses an instance.</summary>
        public const string Focus = "Playzor.Editor.focus";

        /// <summary>Switches the monaco theme.</summary>
        public const string SetTheme = "Playzor.Editor.setTheme";

        /// <summary>Disposes an instance and its model.</summary>
        public const string Dispose = "Playzor.Editor.dispose";

        /// <summary>Selects and reveals a line.</summary>
        public const string SetSelection = "Playzor.Editor.setSelection";

        /// <summary>Moves the caret.</summary>
        public const string SetPosition = "Playzor.Editor.setPosition";

        /// <summary>Publishes compiler diagnostics as inline squiggles.</summary>
        public const string SetMarkers = "Playzor.Editor.setMarkers";
    }

    /// <summary>Bridge to the preview iframe, which is its own webassembly instance.</summary>
    public static class Preview
    {
        /// <summary>Pushes a dark/light change into the preview.</summary>
        public const string PushTheme = "Playzor.Preview.pushTheme";

        /// <summary>Asks the hosting editor to compile (run button of the empty preview).</summary>
        public const string RequestRun = "Playzor.Preview.requestRun";

        /// <summary>Called inside the preview to listen for theme messages.</summary>
        public const string Listen = "Playzor.Preview.listen";

        /// <summary>True when the preview page announced itself after the given timestamp.</summary>
        public const string LoadedSince = "Playzor.Preview.loadedSince";
    }

    /// <summary>Console output collected from the preview iframe.</summary>
    public static class Console
    {
        /// <summary>Starts forwarding to the given dotnet reference and returns the entries so far.</summary>
        public const string Init = "Playzor.Console.init";

        /// <summary>All collected entries.</summary>
        public const string GetAll = "Playzor.Console.getAll";

        /// <summary>Drops the collected entries.</summary>
        public const string Clear = "Playzor.Console.clear";

        /// <summary>Stops forwarding.</summary>
        public const string Dispose = "Playzor.Console.dispose";

        /// <summary>True when the output container is scrolled to its end.</summary>
        public const string IsScrollAtBottom = "Playzor.Console.isScrollAtBottom";

        /// <summary>Scrolls the output container to its end.</summary>
        public const string ScrollToBottom = "Playzor.Console.scrollToBottom";
    }

    /// <summary>Swapping the compiled user assembly into the preview instance.</summary>
    public static class CodeExecution
    {
        /// <summary>Stores the freshly compiled assembly for the next preview load.</summary>
        public const string UpdateUserComponentsDll = "Playzor.CodeExecution.updateUserComponentsDll";
    }

    /// <summary>Helpers for an embedded playground.</summary>
    public static class Embed
    {
        /// <summary>Reports the content height to the hosting page.</summary>
        public const string InitAutoHeight = "Playzor.Embed.initAutoHeight";
    }
}
