using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace TryMudEx.Client.Services;

/// <summary>
/// Ui strings of the playground shell in the brand's culture. Deliberately a plain dictionary:
/// a handful of strings does not justify resx satellite assemblies in a wasm download, and the
/// lookup falls back to english (and then to the key) so a missing translation is never fatal.
/// </summary>
public class PlaygroundLocalizer
{
    private readonly BrandingService _branding;
    private readonly NavigationManager _navigation;
    private string _culture;

    public PlaygroundLocalizer(BrandingService branding, NavigationManager navigation)
    {
        _branding = branding;
        _navigation = navigation;
    }

    public string Culture => _culture ??= ResolveCulture();

    public string this[string key] => Translate(key);

    private string ResolveCulture()
    {
        var uri = new System.Uri(_navigation.Uri);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        if (query.TryGetValue("lang", out var lang))
        {
            var requested = lang.ToString().ToLowerInvariant();
            if (Translations.ContainsKey(requested)) return requested;
        }

        return Translations.ContainsKey(_branding.Current.Culture) ? _branding.Current.Culture : "en";
    }

    private string Translate(string key)
    {
        if (Translations.TryGetValue(Culture, out var table) && table.TryGetValue(key, out var value))
            return value;
        return key; // english strings are used as keys
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
        },
    };
}
