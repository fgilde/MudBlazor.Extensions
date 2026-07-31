# Playzor als Pakete

Nachfolger von [2026-07-27-trymudex-codepen-redesign-status.md](2026-07-27-trymudex-codepen-redesign-status.md).
Branch `feat/extract-source-edit-cmp`, **nicht gepusht**.

## Schnitt

| Paket | TFM | Inhalt | Abhängig von |
|---|---|---|---|
| `Playzor.Blazor` | net8/9/10 | `<PlayzorPlayground>` (iframe-Embed) und die Web-Component `<playzor-playground>` | nur Blazor |
| `Playzor.Core` | net10 | Compiler, CodeFiles, NuGet-Auflösung, `IPlayzorApi`, `IPlayzorSnippetStore` | MudBlazor.Extensions |
| `Playzor.Blazor.Editor` | net10 | `<PlayzorEditor>` samt Monaco, Panels, Assets | MudBlazor.Extensions, Playzor.Core |
| `Playzor.UserComponents` | net10 | Stub-Assembly der Vorschau | – |
| `Playzor.Server` | net10 | `MapPlayzorApi()`: NuGet-Proxy, optionale Snippet-Endpunkte | ASP.NET Core |

`build/Playzor.props` hält Version, Icon, Lizenz und Links zentral; `-p:PlayzorVersion=` übersteuert
die Version, das nutzt der Workflow.

## Namensregeln, die nicht verhandelbar sind

- **Assembly `Try.UserComponents`** bleibt so, obwohl das Paket `Playzor.UserComponents` heißt:
  jedes gespeicherte Snippet deklariert `namespace Try.UserComponents` in seinen `.cs`-Dateien, und
  der Boot-Hook tauscht genau unter diesem Namen. Umbenennen bräche geteilte Links.
  Das Paket hängt bewusst an **keinem** anderen Playzor-Paket: `Playzor.Core` kennt den Namen nur
  als Zeichenkette, gebraucht wird das Stub allein von der App, die die Vorschau ausliefert.
- **Das URL-Format** (Unit-Separator, Deflate, Base64Url) hat jetzt drei Implementierungen:
  `Playzor.Core.InlineCode`, `Playzor.Blazor.PlayzorCode` und `playzor-embed.js`.
  `PlayzorCodeCompatibilityTests` nagelt alle drei gegeneinander fest.

## Server-Naht

`IPlayzorApi` ist alles, was der Browser nicht allein kann — praktisch nur der NuGet-Download,
weil nuget.org keine CORS-Header schickt. Drei Wege:

```csharp
services.AddPlayzor();                                              // eigener Server
services.AddPlayzor(o => o.BaseAddress = PlayzorApiOptions.PlayzorNet);  // fremder Server
services.AddScoped<IPlayzorApi, MyApi>();                           // eigene Implementierung
```

`IPlayzorSnippetStore` ist optional: registriert, speichern und laden Save- und Samples-Button von
selbst; sonst antwortet der Host über `OnSaveRequested` / `OnSampleSelected`. In TryMudEx
implementiert `SnippetsService` das Interface, die Seite verdrahtet die Events trotzdem selbst,
weil sie zusätzlich die URL umschreibt und den Share-Dialog zeigt.

playzor.net erlaubt fremde Origins (`AllowedOrigins = ["*"]`), damit eine App ohne eigenen Server
darauf zeigen kann. Das kostet Traffic — die Zeile in `TryMudEx.Server/Startup.cs` ist der Schalter.

## Vorschau

Der kompilierte Code läuft in einer zweiten WASM-Instanz. Der Host muss die Route ausliefern und
`playzor-preview.js` einbinden; das Script meldet sich beim Laden, und bleibt die Meldung nach
einem Run aus, zeigt der Editor eine Warnung im Vorschau-Panel mit der erwarteten Route.
Das erkennt eine fehlende oder falsche Route, **nicht** ein Snippet, das absichtlich nichts rendert.

## Bewusst offen

- `Samples/MainSample.WebAssembly` referenziert MudBlazor.Extensions in Release als **Paket**
  (`Version="*-*"`). Seit 9.7.0 stabil auf nuget.org liegt, gewinnt die veröffentlichte Version
  gegen die neuere Quelle und der Release-Build der Sample-App bricht. Gleiche Ursache wie bei
  TryMudEx, dort ist die ProjectReference inzwischen bedingungslos.
- Der Compiler reicht MudBlazor und MudEx als Ambient-Referenzen an den Nutzercode — deshalb hängt
  `Playzor.Core` daran. Für einen wirklich generischen Playground müsste das konfigurierbar werden.
- Publish läuft über `.github/workflows/playzor_publish.yml`, per Tag `playzor-v1.2.3` oder manuell
  mit Dry-Run. Auf nuget.org braucht jedes der fünf Pakete eine Trusted-Publishing-Policy mit
  Workflow-Datei `playzor_publish.yml`.
