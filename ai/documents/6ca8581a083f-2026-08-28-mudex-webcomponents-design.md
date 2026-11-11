# MudEx WebComponents — Design

**Datum:** 2026-08-28
**Branch:** offen (noch nichts committet/gepusht — explizite Vorgabe)
**Status:** Umgesetzt (siehe Abschnitt „Umsetzung“ am Ende)

## Ziel

MudEx-Komponenten als Standard Custom Elements ausliefern, so dass eine beliebige HTML-Seite mit **einem** Script-Tag

```html
<script src="https://www.mudex.org/wc/mudex.js"></script>
<mudex-file-display url="https://example.org/demo.pdf"></mudex-file-display>
```

MudEx-Komponenten nutzen kann — ohne Blazor-Projekt, ohne .NET beim Konsumenten.

1. Wichtigste Komponente: `MudExFileDisplay`. Muss nachweislich funktionieren (PDF, Bild, ZIP, Markdown).
2. So viele weitere Komponenten wie technisch möglich, ohne Pflegeliste.
3. Auslieferung über `mudex.org/wc/` **und** npm/jsDelivr.
4. Native HTML-Demoseite (kein Blazor) als Beweis und Beispiel.
5. Doku-Seite in der Demo-App mit Code-Snippets und Link auf die HTML-Seite.
6. Automatisierter Test, der das absichert (Playwright).

## Entschiedene Grundsatzfragen

| Frage | Entscheidung |
|---|---|
| Auslieferung | Eigenes schlankes WASM-Projekt `Samples/MudEx.WebComponents`. Publish-Output nach `mudex.org/wc/` **und** als npm-Paket `mudex-webcomponents` (jsDelivr/unpkg). Kein Mitschleppen des Demo-Bundles (Monaco, Localization, Beispieldaten). |
| Komponenten-Umfang | Auto-Registrierung per Reflection über die `MudBlazor.Extensions`-Assembly. Keine Whitelist, keine Pflege bei neuen Komponenten. |
| Bridge-Technik | Natives Blazor-Feature `Microsoft.AspNetCore.Components.CustomElements` (10.0.11 auf nuget.org verfügbar). Keine Eigenbau-Bridge. |
| Provider | Ein einziger Provider-Root (`#mudex-wc-root`), vom Loader in die Seite gehängt. Alle Custom-Element-Instanzen teilen sich einen WASM-Host, einen Renderer und die DI-Registrierungen. |
| Tag-Schema | PascalCase → kebab-case, Präfix `mudex-` erzwungen: `MudExFileDisplay` → `mudex-file-display`, `MoveContent` → `mudex-move-content`. |
| Tests | Playwright gegen die native HTML-Seite (echter Browser, echter Render) + billiger Reflection-Smoke-Test auf die Registrierungsliste. |
| MudBlazor-Basis-Komponenten | Nicht registriert. Nur MudEx-Assembly. Kann später ergänzt werden, ist aber nicht Teil dieses Specs. |

## Architektur

### 1. Projekt `Samples/MudEx.WebComponents`

`Microsoft.NET.Sdk.BlazorWebAssembly`, `net10.0`, Referenz auf `MudBlazor.Extensions` (ProjectReference in Debug, PackageReference in Release — gleiches Muster wie `MainSample.WebAssembly`).

Neue Zeile in `Directory.Packages.props`:

```xml
<PackageVersion Include="Microsoft.AspNetCore.Components.CustomElements" Version="10.0.11" />
```

Das Projekt wird in `MudBlazor.Extensions.slnx` unter `/Samples/` aufgenommen, damit ein Solution-Build es mitbaut und Breaking Changes an der Library sofort auffallen.

### 2. Registrierung (Program.cs)

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddMudServicesWithExtensions();

builder.RootComponents.Add<MudExWcRoot>("#mudex-wc-root");

foreach (var type in RegistrableComponents(typeof(MudExFileDisplay).Assembly))
    RegisterCustomElementGeneric(builder.RootComponents, type, TagName(type));

await builder.Build().RunAsync();
```

`RegistrableComponents`: public, class, nicht abstrakt, `IComponent` zuweisbar, **kein** offener Generic (`IsGenericTypeDefinition`), öffentlicher parameterloser Konstruktor, nicht `[Obsolete]`. Sortiert und dedupliziert nach Tag-Name (bei Kollision gewinnt der erste, die weiteren werden in `tags.json` als `skipped` vermerkt).

`RegisterCustomElementGeneric` ruft die Extension `RegisterCustomElement<T>` per `MakeGenericMethod` auf — die generische Signatur ist der einzige Grund für Reflection.

Registrierung ist rein deklarativ (Tag → Typ). Eine Komponente, die zur Laufzeit ein fehlendes Cascading-Value braucht, scheitert erst bei Benutzung ihres Tags, nicht beim Start. Damit reißt eine problematische Komponente nicht das gesamte Bundle mit.

### 3. Provider-Root `MudExWcRoot.razor`

```razor
<MudThemeProvider Theme="@_theme" IsDarkMode="@_dark" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

Dark-Mode-Default aus `prefers-color-scheme`, per `window.MudEx.setDarkMode(bool)` umschaltbar. Theme später über `window.MudEx.setTheme({...})` überschreibbar — **nicht** Teil dieses Specs, nur die Dark-Mode-Umschaltung.

### 4. Loader `wwwroot/mudex.js`

Handgeschrieben, ca. 50 Zeilen, keine Build-Kette:

1. Basis-URL aus `document.currentScript.src` ableiten (`new URL('.', src)`).
2. `window.MudEx = { base, ready: <Promise>, assetBase: base }` setzen — **vor** dem Blazor-Start, damit die Library `assetBase` sieht (siehe Cross-Origin unten).
3. Stylesheets injizieren (absolute URLs auf die Basis):
   - `https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap`
   - `_content/MudBlazor/MudBlazor.min.css`
   - `_content/MudBlazor.Extensions/mudBlazorExtensions.min.css`
   - `_content/MudBlazor.Markdown/MudBlazor.Markdown.min.css`
4. Scripts injizieren: `_content/MudBlazor/MudBlazor.min.js`, `_content/MudBlazor.Markdown/MudBlazor.Markdown.min.js`.
5. `<div id="mudex-wc-root">` an `document.body` hängen (versteckt, dient nur den Providern).
6. `_framework/blazor.webassembly.js` mit `autostart="false"` laden, danach

```js
Blazor.start({ loadBootResource: (type, name, defaultUri) => new URL('_framework/' + name, base).href });
```

7. `window.MudEx.ready` auflösen, wenn `Blazor.start()` durch ist.

Mehrfaches Einbinden ist idempotent (Guard über `window.MudEx`).

### 5. Cross-Origin-Assets (der eigentliche Knackpunkt)

Die Library lädt ihre JS-Module über dokumentrelative Pfade:

- `MudBlazor.Extensions/Helper/JsImportHelper.cs:85`
- `MudBlazor.Extensions/MudBlazorExtensionJsInterop.cs:20`
- `MudBlazor.Extensions/Components/MudExExternalFilePickerBase.razor.cs:239,241`

Auf einer fremden Seite zeigt `./_content/MudBlazor.Extensions/...` auf den fremden Origin → 404, die Komponente rendert nicht.

**Lösung:** ein zentraler Helper in der Library, der einen Asset-Base voranstellt:

```csharp
// MudBlazor.Extensions/Helper/MudExAssets.cs
public static string Resolve(string relativePath); // "./_content/..." oder "<base>_content/..."
```

Der Base kommt aus `window.MudEx.assetBase` (einmalig beim ersten Zugriff über JS-Interop gelesen, gecacht). Ist er nicht gesetzt — also in jeder normalen Blazor-App — bleibt exakt das heutige Verhalten. Alle vier Fundstellen gehen über den Helper. CSS-interne `url(...)`-Referenzen brauchen nichts, die lösen relativ zur CSS-Datei auf und die liegt bereits auf der CDN-Basis.

Die Änderung ist der einzige Eingriff in die Library selbst und muss vor Phase 3 (Fremd-Origin) stehen.

## Hosting & Deployment

### mudex.org/wc/

Schritt im Job `deploy_application_to_azure` (`.github/workflows/nuget_preview_publish_and_app_deploy.yml`), **vor** dem Azure-Deploy:

```yaml
- name: Publish WebComponents
  run: dotnet publish Samples/MudEx.WebComponents -c Release -o wcpublish
- name: Copy WebComponents into app
  run: cp -r wcpublish/wwwroot/. "${{ env.AZURE_WEBAPP_PACKAGE_PATH }}/wwwroot/wc/"
```

Lokal: MSBuild-Target im WC-Projekt, das nach `Publish` denselben Kopiervorgang nach `Samples/MainSample.WebAssembly/wwwroot/wc` macht. Dieses Verzeichnis kommt in `.gitignore` — es ist Build-Output, kein Quellcode.

### CORS + MIME

`wwwroot/web.config` im WC-Projekt, landet damit unter `/wc/web.config` (IIS erbt nach unten):

- `Access-Control-Allow-Origin: *` (öffentliche, statische, nicht-authentifizierte Assets — kein Credential-Risiko)
- MIME-Typen für `.wasm`, `.dat`, `.blat`, `.dll`, `.br`
- `Cache-Control` für `_framework/*` lang, für `mudex.js` kurz

### npm / jsDelivr

`package.json` (Name `mudex-webcomponents`, Version = MudEx-`PackageVersion`) wird beim Publish in den Output geschrieben, Inhalt = `wwwroot`. Publish-Step im Release-Workflow (`nuget_release_publish.yml`), nur auf Tag/Release.

Ergebnis: `https://cdn.jsdelivr.net/npm/mudex-webcomponents@9/mudex.js`.

**Blocker für dich:** Secret `NPM_TOKEN` in den Repo-Settings. Ohne das läuft Phase 3b nicht — der Rest ist davon unabhängig. Der Paketname ist unskopiert, damit keine npm-Org nötig ist.

## Demo & Doku

### Native HTML-Seite

`Samples/MainSample.WebAssembly/wwwroot/webcomponents.html` — reines HTML, kein Blazor, kein Framework:

- ein `<script src="/wc/mudex.js">`
- `<mudex-file-display>` mit Umschalter (PDF / Bild / ZIP / Markdown / Excel) per plain `el.setAttribute('url', ...)`
- zwei bis drei weitere Komponenten (Kandidaten: `mudex-gravatar`, `mudex-audio-player`, `mudex-color-edit`) als Beleg für die Breite
- ein Bereich, der komplexe Parameter per JS-Property setzt
- ein Bereich, der zeigt wie man ein Event abgreift

Die Seite liegt im Demo-Projekt, wird also mitdeployed und ist unter `https://www.mudex.org/webcomponents.html` erreichbar. Sie ist gleichzeitig das Testziel für Playwright, deshalb bekommen alle interaktiven Elemente stabile `id`s.

### Doku-Seite

`Samples/MainSample.WebAssembly/Pages/Page_WebComponents.razor` im Muster der übrigen Demo-Pages (`MudExDemoPage`, `MudExCodeView` für Snippets), Eintrag in `Shared/NavMenu.razor`:

- Script-Tag-Snippet für mudex.org und für jsDelivr
- Tag-Namensschema und ein vollständiges `MudExFileDisplay`-Beispiel
- Attribute vs. JS-Properties vs. Events, jeweils mit Snippet
- **generierte** Tabelle aller registrierten Tags — die Seite ruft dieselbe Reflection-Logik (`MudExWebComponents.GetRegistrableComponents`) direkt auf, keine Handpflege, kann nicht veralten
- prominenter Link auf `webcomponents.html`
- Abschnitt „Grenzen" (siehe unten)

## Grenzen — dokumentiert, nicht wegdesignt

- **Kein `ChildContent`/`RenderFragment`.** HTML zwischen den Tags wird ignoriert. Komponenten, die ohne ChildContent sinnlos sind, sind als Custom Element sinnlos — sie werden trotzdem registriert, kosten nichts.
- **Komplexe Parameter nur als JS-Property**, nicht als Attribut: `el.parametersForSubControls = {...}`. Attribute können nur Strings, Zahlen, Bools, Enums.
- **Offene Generics** (`MudExSelect<T>` usw.) sind nicht registrierbar. Landen in `tags.json` unter `skipped`.
- **Bundle-Größe:** erster Load mehrere MB (Brotli), danach Browser-Cache. Das ist der Preis für „echtes MudEx im DOM"; wird auf der Doku-Seite offen genannt.
- **Ein WASM-Host pro Seite.** Zwei verschiedene mudex.js-Versionen auf derselben Seite werden nicht unterstützt.

## Tests

`MudBlazor.Extensions.Tests/UITests/Tests/WebComponentsTests.cs`, auf der vorhandenen `PlaywrightFixture`:

1. Publish-Output des WC-Projekts + `webcomponents.html` werden statisch bereitgestellt (bevorzugt über die bestehende `WebTestingHostFactory`; falls das mit dem WASM-Standalone-Output nicht trägt, ein minimaler `WebApplication` mit `UseStaticFiles` im Test-Setup).
2. Seite laden, auf `window.MudEx.ready` warten.
3. `mudex-file-display` rendert echten Inhalt (Assertion auf ein Kindelement im Shadow-freien DOM, nicht nur auf die Existenz des Tags).
4. `url`-Attribut umschalten → Inhalt wechselt.
5. Keine Console-Errors während des Ablaufs.

Dazu ein billiger xUnit-Test ohne Browser: die Registrierungsliste enthält `mudex-file-display`, alle Tags enthalten einen Bindestrich, keine Duplikate, kein Wurf beim Aufbau der Liste.

## Risiken & Fallbacks

| Risiko | Fallback |
|---|---|
| `loadBootResource` löst `dotnet.js` als ES-Modul relativ zum Dokument auf statt zur CDN-Basis | Konsument bindet zwei Zeilen statt einer ein (`<script src=".../blazor.webassembly.js" autostart="false">` + `mudex.js`). Wird in Phase 2 empirisch geklärt, bevor irgendwas anderes darauf aufbaut. |
| Library-JS-Module trotz `assetBase` nicht auffindbar | Import-Map im Loader als zweite Verteidigungslinie. |
| Einzelne Komponenten werfen als Custom Element | Kein Blocker: pro Tag isoliert. In `tags.json` als `unsupported` markieren, sobald bekannt. |
| Azure/IIS liefert `.wasm` oder Brotli falsch aus | `web.config` mit expliziten MIME-Typen; wird beim ersten Deploy verifiziert. |

## Phasen

1. **Projekt + Registrierung.** WC-Projekt, Reflection-Registrierung, Provider-Root, lokale `index.html`. Beweis: `mudex-file-display` rendert lokal ein PDF.
2. **Loader + HTML-Demo, same-origin.** `mudex.js`, `webcomponents.html`, `tags.json`. Beweis: die HTML-Seite funktioniert gegen `/wc/`. Hier wird Risiko 1 geklärt.
3. **Cross-Origin.** `MudExAssets`-Helper in der Library, `web.config` mit CORS, Deploy-Step. (3b: npm/jsDelivr, blockiert auf `NPM_TOKEN`.)
4. **Doku-Seite** + Nav-Eintrag.
5. **Tests.** Playwright + Reflection-Smoke.

Jede Phase ist einzeln lauffähig und einzeln überprüfbar.

---

## Umsetzung

Umgesetzt am 2026-08-29. Was im Bauen anders lief als im Entwurf gedacht:

### Der eigentliche Cross-Origin-Blocker war ein anderer

Nicht `loadBootResource` (Risiko 1) war das Problem — das ließ sich sauber umbiegen —, sondern **Blazor JS Initializers**. Die Runtime lädt `_content/**/*.lib.module.js` mit `new URL(name, document.baseURI)`, hart verdrahtet, ohne Hook. Auf einer fremden Seite zeigt das auf den falschen Origin.

Lösung: der Loader injiziert eine **Import Map**, die `<seite>/_content/` auf `<bundle>/_content/` umschreibt. Deklariert die Hostseite bereits eine eigene Import Map, überspringt der Loader das und schreibt eine Konsolenwarnung mit dem exakt fehlenden Mapping.

Zusätzlich musste `locateFile` über `configureRuntime`/`withModuleConfig` umgebogen werden, und Satellite-Assemblies (`_framework/de/*.wasm`) verlangen, dass `loadBootResource` die `defaultUri` umschreibt statt den Namen neu zusammenzusetzen.

### Asset-Base: vorhandener Mechanismus statt neuem Helper

Der geplante `MudExAssets.Resolve` war überflüssig — `MudExConfiguration.WithJsBasePath` und `JsImportHelper.BasePath` gab es bereits. Vier Stellen umgingen den Mechanismus mit fest verdrahteten `./_content/...`-Pfaden; die gehen jetzt über `JsImportHelper.JsPath`. Neu ist nur `MudExWebComponents.SetAssetBase(string)` als öffentlicher Einstieg, den `Program.cs` mit `window.MudEx.getAssetBase()` füttert.

### HttpClient

MudExFileDisplay lädt Dateiinhalte selbst. Ohne registrierten `HttpClient` scheitern relative URLs mit `net_http_client_invalid_requesturi` — Markdown, Zip und Excel blieben leer. Der Host registriert jetzt einen `HttpClient` mit `HostEnvironment.BaseAddress`, also der Adresse der **einbettenden Seite**. Relative URLs im Markup verhalten sich damit so, wie ein Seitenautor es erwartet.

`content-type` sollte man mitgeben. Ohne den Wert fällt die Komponente auf den nativen Browser-Viewer zurück statt den passenden MudEx-Viewer zu wählen; die Demo-Seite und die Doku sagen das jetzt explizit.

### Keine vorkomprimierten Dateien im Kopiervorgang

`.gz`/`.br` werden **nicht** mitkopiert. IIS/Azure komprimiert dynamisch (`httpCompression` in der web.config), und die Kopien haben die Static-Web-Assets-Pipeline der Host-App durcheinandergebracht (`Content-Encoding: gzip` auf unkomprimiertem Inhalt, `ERR_CONTENT_DECODING_FAILED`).

### Trimming aus

`PublishTrimmed=false`. Die Registrierung läuft per Reflection über die ganze Assembly; der Trimmer würde genau die Komponenten entfernen, die niemand statisch referenziert. Kostet Bundle-Größe, ist aber die Bedingung dafür, dass „alle Komponenten" auch wirklich alle sind.

### Ergebnis

- **73 Komponenten** registriert, 35 übersprungen (Generics, fehlender parameterloser Konstruktor) — beide Listen stehen auf der Doku-Seite.
- Tests: `MudExWebComponentsTests` (5 Unit-Tests) und `WebComponentsTests` (Playwright: gleiche Origin **und** fremder Origin). Alle grün.
- Die Playwright-Tests brauchen ein vorher publiziertes Bundle: `dotnet publish Samples/MudEx.WebComponents -c Debug`.

### Offen

- npm/jsDelivr: Workflow-Schritt existiert, läuft aber nur mit dem Secret `NPM_TOKEN`. Paketname ist unskopiert (`mudex-webcomponents`), damit keine npm-Org nötig ist.
- Ob Azure/IIS die MIME-Typen und CORS-Header aus `/wc/web.config` wie erwartet ausliefert, zeigt erst das erste Deployment.
