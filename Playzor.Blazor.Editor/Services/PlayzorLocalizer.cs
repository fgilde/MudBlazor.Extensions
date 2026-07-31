using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;

namespace Playzor.Blazor.Editor.Services;

/// <summary>
/// Ui strings of the editor. Deliberately a plain dictionary: a handful of strings does not justify
/// resx satellite assemblies in a wasm download, and the lookup falls back to english (the keys are
/// the english text) so a missing translation is never fatal.
/// <para>
/// Being an <see cref="IStringLocalizer"/> it can be handed to any MudEx component, and a host that
/// wants different wording registers its own implementation or passes one to the editor.
/// </para>
/// </summary>
public class PlayzorLocalizer : IStringLocalizer
{
    private readonly NavigationManager _navigation;
    private string _culture;

    /// <summary>Creates the localizer. The culture is resolved from <c>?lang=</c> or the current ui culture.</summary>
    public PlayzorLocalizer(NavigationManager navigation = null)
    {
        _navigation = navigation;
    }

    /// <summary>
    /// Two letter culture of the returned strings. Assigning an unknown culture falls back to english.
    /// </summary>
    public string Culture
    {
        get => _culture ??= ResolveCulture();
        set => _culture = value != null && Translations.ContainsKey(value.ToLowerInvariant())
            ? value.ToLowerInvariant()
            : "en";
    }

    /// <summary>Cultures this localizer has translations for.</summary>
    public static IEnumerable<string> SupportedCultures => Translations.Keys;

    /// <inheritdoc />
    public LocalizedString this[string name] => Localize(name);

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var localized = Localize(name);
            return arguments?.Length > 0
                ? new LocalizedString(name, string.Format(localized.Value, arguments), localized.ResourceNotFound)
                : localized;
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => Translations.TryGetValue(Culture, out var table)
            ? table.Select(pair => new LocalizedString(pair.Key, pair.Value, false))
            : Enumerable.Empty<LocalizedString>();

    private LocalizedString Localize(string key)
    {
        if (key != null && Translations.TryGetValue(Culture, out var table) && table.TryGetValue(key, out var value))
            return new LocalizedString(key, value, false);
        return new LocalizedString(key, key ?? string.Empty, true);
    }

    private string ResolveCulture()
    {
        if (_navigation != null)
        {
            try
            {
                var query = QueryHelpers.ParseQuery(new Uri(_navigation.Uri).Query);
                if (query.TryGetValue("lang", out var lang))
                {
                    var requested = lang.ToString().ToLowerInvariant();
                    if (Translations.ContainsKey(requested)) return requested;
                }
            }
            catch
            {
                // a relative or not yet initialized uri simply means no override
            }
        }

        var current = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return Translations.ContainsKey(current) ? current : "en";
    }

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["en"] = new Dictionary<string, string>(),
        ["de"] = new Dictionary<string, string>
        {
            // toolbar
            ["Run (Ctrl + S)"] = "Ausführen (Strg + S)",
            ["Save or Share"] = "Speichern oder teilen",
            ["Download"] = "Herunterladen",
            ["Upload"] = "Hochladen",
            ["Samples"] = "Beispiele",
            ["Manage Nuget packages"] = "NuGet-Pakete verwalten",
            ["Panels"] = "Bereiche",
            ["Reset layout"] = "Layout zurücksetzen",
            ["Layout"] = "Layout",
            ["Save layout"] = "Layout speichern",
            ["Layout saved"] = "Layout gespeichert",
            ["Name for this layout"] = "Name für dieses Layout",
            ["Open in new window"] = "In neuem Fenster öffnen",
            ["Could not open a window — check your popup blocker."] = "Fenster konnte nicht geöffnet werden — Popup-Blocker prüfen.",
            ["Switch to Light Theme"] = "Zu hellem Design wechseln",
            ["Switch to Dark Theme"] = "Zu dunklem Design wechseln",

            // panels
            ["Files"] = "Dateien",
            ["Preview"] = "Vorschau",
            ["Errors"] = "Fehler",
            ["Console"] = "Konsole",
            ["Warnings"] = "Warnungen",
            ["No errors or warnings."] = "Keine Fehler oder Warnungen.",
            ["Nothing matches the current filter."] = "Nichts entspricht dem aktuellen Filter.",
            ["Copy all"] = "Alles kopieren",
            ["Copy"] = "Kopieren",
            ["Clear"] = "Leeren",
            ["Filter..."] = "Filtern...",
            ["Follow output"] = "Ausgabe verfolgen",
            ["No output yet. Run your code — Console.WriteLine and JS console output show up here."] =
                "Noch keine Ausgabe. Führe deinen Code aus — Console.WriteLine und JS-Konsolenausgaben erscheinen hier.",

            // empty preview
            ["Your preview shows up here."] = "Hier erscheint deine Vorschau.",
            ["Write your component on the left and run it — everything compiles in this browser tab."] =
                "Schreib deine Komponente links und führe sie aus — kompiliert wird direkt in diesem Browser-Tab.",
            ["Run code"] = "Code ausführen",

            // file tree
            ["New file"] = "Neue Datei",
            ["New folder (creates folder/File.razor)"] = "Neuer Ordner (erzeugt Ordner/Datei.razor)",
            ["Add from template"] = "Aus Vorlage hinzufügen",
            ["Rename"] = "Umbenennen",
            ["Delete"] = "Löschen",
            ["Delete file"] = "Datei löschen",
            ["Enter file name (folders with '/', e.g. Components/Card.razor)"] =
                "Dateiname eingeben (Ordner mit '/', z. B. Components/Card.razor)",

            // status bar
            ["Reload"] = "Neu laden",
            ["Show errors"] = "Fehler anzeigen",
            ["Reload preview"] = "Vorschau neu laden",

            // embed
            ["Run"] = "Ausführen",
            ["Running…"] = "Läuft…",
            ["Split"] = "Geteilt",
            ["Code"] = "Code",
            ["Switch view"] = "Ansicht wechseln",
            ["Edit on"] = "Bearbeiten auf",
            ["Open in"] = "Öffnen in",
            ["Nothing to show — this embed has no snippet."] = "Nichts anzuzeigen — dieses Embed hat kein Snippet.",
            ["Could not load this snippet."] = "Dieses Snippet konnte nicht geladen werden.",
            ["Compilation failed."] = "Kompilierung fehlgeschlagen.",

            // embed dialog
            ["Web component"] = "Web-Component",
            ["Open in new tab"] = "In neuem Tab öffnen",
            ["One script tag, then a custom element — the code stays readable in your html instead of being encoded into an url."] =
                "Ein Script-Tag, dann ein eigenes Element — der Code bleibt im HTML lesbar statt in einer URL kodiert.",
            ["Every option is an attribute; several files go in as json:"] =
                "Jede Option ist ein Attribut, mehrere Dateien kommen als JSON rein:",

            // editor
            ["Snippet saved"] = "Snippet gespeichert",
            ["Could not save the snippet."] = "Snippet konnte nicht gespeichert werden.",
            ["Could not load the sample."] = "Beispiel konnte nicht geladen werden.",
            ["Processing"] = "Verarbeite",
            ["Error while compiling the code."] = "Fehler beim Kompilieren.",
            ["Enter file name"] = "Dateiname eingeben",
            ["Upload content files as zip or separate"] = "Dateien als ZIP oder einzeln hochladen",
            ["Open sample"] = "Beispiel öffnen",
            ["Select sample to open"] = "Beispiel zum Öffnen wählen",
            ["The preview at {0} did not load. The host has to serve that route with the compiled component, reference Playzor.UserComponents and include playzor-preview.js on the page."] =
                "Die Vorschau unter {0} hat nicht geladen. Die Host-App muss diese Route mit der kompilierten Komponente ausliefern, Playzor.UserComponents referenzieren und playzor-preview.js auf der Seite einbinden.",

            ["Embed this snippet"] = "Dieses Snippet einbetten",
            ["Paste the snippet into any page — the code travels inside the url, nothing needs to be saved."] =
                "Snippet in eine beliebige Seite einfügen — der Code steckt in der URL, es muss nichts gespeichert werden.",
            ["Options"] = "Optionen",
            ["View"] = "Ansicht",
            ["Theme"] = "Design",
            ["Auto"] = "Automatisch",
            ["Light"] = "Hell",
            ["Dark"] = "Dunkel",
            ["Start file"] = "Startdatei",
            ["Height"] = "Höhe",
            ["Editable"] = "Bearbeitbar",
            ["Run on load"] = "Beim Laden ausführen",
            ["Show toolbar"] = "Werkzeugleiste zeigen",
            ["Live preview"] = "Live-Vorschau",
            ["Link"] = "Link",
            ["Copied to clipboard"] = "In die Zwischenablage kopiert",

            // product pages
            ["Embedding"] = "Einbetten",
            ["Open the editor"] = "Editor öffnen",
            ["Blazor, straight in your browser."] = "Blazor, direkt im Browser.",
            ["Write a component, hit run, share the link. No SDK, no project file, no build server — the compiler runs in your browser tab."] =
                "Komponente schreiben, ausführen, Link teilen. Kein SDK, keine Projektdatei, kein Build-Server — der Compiler läuft in deinem Browser-Tab.",
            ["Start coding"] = "Loslegen",
            ["Embed it anywhere"] = "Überall einbetten",
            ["Runs offline in the browser"] = "Läuft offline im Browser",
            ["Multiple files and folders"] = "Mehrere Dateien und Ordner",
            ["NuGet packages"] = "NuGet-Pakete",
            ["Compiles in the browser"] = "Kompiliert im Browser",
            ["Roslyn runs on WebAssembly — your code never leaves the tab unless you share it."] =
                "Roslyn läuft auf WebAssembly — dein Code verlässt den Tab nur, wenn du ihn teilst.",
            ["Real projects"] = "Echte Projekte",
            ["Several files, folders and sub namespaces, just like a project on your machine."] =
                "Mehrere Dateien, Ordner und Unter-Namespaces, wie in einem Projekt auf deinem Rechner.",
            ["Search, install and use packages including their dependencies."] =
                "Pakete suchen, installieren und samt Abhängigkeiten nutzen.",
            ["Dockable panels"] = "Andockbare Bereiche",
            ["Arrange editor, preview, console and errors the way you like — or pop a panel into its own window."] =
                "Editor, Vorschau, Konsole und Fehler nach Belieben anordnen — oder einen Bereich in ein eigenes Fenster lösen.",
            ["Shareable links"] = "Teilbare Links",
            ["Every snippet is a url. Short links for saved snippets, self contained links for everything else."] =
                "Jedes Snippet ist eine URL. Kurze Links für gespeicherte Snippets, selbsttragende Links für alles andere.",
            ["Embeddable"] = "Einbettbar",
            ["Drop a live, editable playground into any page with one iframe."] =
                "Mit einem iframe einen lebenden, bearbeitbaren Playground in jede Seite setzen.",
            ["Put a live playground on your own page"] = "Einen lebenden Playground auf deine Seite setzen",
            ["One iframe, or one component in a Blazor app. Readers edit the code and run it without leaving your site."] =
                "Ein iframe oder eine Komponente in einer Blazor-App. Leser bearbeiten den Code und führen ihn aus, ohne deine Seite zu verlassen.",
            ["Read the embedding guide"] = "Zur Einbettungs-Anleitung",
            ["Playzor is built with MudBlazor.Extensions"] = "Playzor ist mit MudBlazor.Extensions gebaut",

            // embedding guide
            ["Embed a playground"] = "Einen Playground einbetten",
            ["Live example"] = "Live-Beispiel",
            ["This is a real embed. Change the code and press Run."] = "Das ist ein echtes Embed. Ändere den Code und drücke Ausführen.",
            ["Read only"] = "Nur lesen",
            ["Plain iframe"] = "Einfaches iframe",
            ["Works on any website — a blog, docs, a CMS page."] = "Funktioniert auf jeder Website — Blog, Doku, CMS-Seite.",
            ["Blazor component"] = "Blazor-Komponente",
            ["In a Blazor app install the package and pass the code as a parameter."] =
                "In einer Blazor-App das Paket installieren und den Code als Parameter übergeben.",
            ["Several files work too — folders become sub namespaces, exactly like in a real project:"] =
                "Mehrere Dateien gehen auch — Ordner werden zu Unter-Namespaces, genau wie in einem echten Projekt:",
            ["Append them to the embed url as query parameters, or set them as parameters on the component."] =
                "An die Embed-URL als Query-Parameter anhängen oder als Parameter an der Komponente setzen.",
            ["Url"] = "URL",
            ["Component"] = "Komponente",
            ["Default"] = "Standard",
            ["Meaning"] = "Bedeutung",
            ["Which side of the playground is visible."] = "Welche Seite des Playgrounds sichtbar ist.",
            ["File shown first."] = "Zuerst angezeigte Datei.",
            ["Shows the code but prevents edits."] = "Zeigt den Code, verhindert aber Änderungen.",
            ["Compile and run as soon as the embed loads."] = "Kompiliert und führt aus, sobald das Embed lädt.",
            ["Hides the tab bar and buttons."] = "Versteckt Tab-Leiste und Schaltflächen.",
            ["Css height of the iframe."] = "CSS-Höhe des iframes.",
            ["Embed a saved snippet by id instead of inline code."] = "Ein gespeichertes Snippet per ID einbetten statt Inline-Code.",
            ["Playground the embed is loaded from."] = "Playground, von dem das Embed geladen wird.",
            ["Sizing"] = "Größe",
            ["Where does the code live?"] = "Wo liegt der Code?",
        },
    };
}
