# TryMudEx → CodePen-Style Playground ("Playzor") — Design

**Datum:** 2026-07-27
**Branch:** `feat/try-adapt`
**Status:** Approved (Design), Spec in Review

## Ziel

TryMudEx wird ein vollwertiger CodePen-artiger Blazor-Playground:

1. Dockbare Panel-UI (DockLayout) statt fixem 2-Panel-Split
2. Multi-File-Projekte mit Ordnerstruktur, File-Tree, ein Editor-Panel pro Datei
3. Bessere Logs/Outputs (Console-Panel mit iframe-Log-Capture, verbessertes Errors-Panel)
4. NuGet-Support repariert (transitive Dependencies, TFM-Filter, Caching)
5. Embed-Mode wie CodePen (`/embed/…`)
6. Neues NuGet-Paket `Playzor.Blazor` mit `<PlayzorPlayground>`-Komponente
7. Domainbasiertes Branding: `try.mudex.org` (MudEx-Showcase) vs. `playzor.net`/`playzor.de` (generischer Blazor-Playground, `.de` deutsch)

Funktionale Parität mit heute bleibt erhalten (Samples, Snippets, Upload/Download, Theme-Toggle, Auto-Compile).

## Entschiedene Grundsatzfragen

| Frage | Entscheidung |
|---|---|
| Dock-Komponente | Bestehendes `MudExDockLayout` (MudBlazor.Extensions/Components/MudExDockLayout.razor.cs) verwenden; Lücken (dynamische Panels zur Laufzeit, ggf. Pop-out) in der Library nachrüsten. Nur bei fundamentalem Blocker: neue Komponente. |
| Snippet-Storage | Bleibt Proxy auf `try.mudblazor.com/api/snippets` (TryMudEx.Server/Controllers/SnippetsController.cs). Embed + Playzor-Komponente funktionieren primär über Inline-Code-URLs (base64url+deflate, existiert schon: `SnippetsService.cs:97-119`) — storage-los. |
| Playzor-Komponente | Eigenes NuGet-Paket `Playzor.Blazor`, keine MudBlazor-Dependency. |
| Reihenfolge | Phase 1 NuGet/Perf → Phase 2 UI → Phase 3 Embed+Paket → Phase 4 Branding. Jede Phase einzeln testbar. |
| Playzor-Positionierung | Generischer Blazor-Playground: neutrale Default-Templates ohne MudEx-Zwang, eigene Samples-Liste, pro Brand konfigurierbare DefaultPackages. `playzor.de` zusätzlich deutsche UI. |

## Ist-Zustand (Kurzreferenz)

- Blazor WASM (`TryMudEx.Client`), Roslyn kompiliert im Browser (`Try.Core/CompilationService.cs`), Ergebnis läuft als zweite WASM-Instanz im iframe `/user-page` (index.html hookt `loadBootResource`, ersetzt `Try.UserComponents.dll` durch kompilierte Bytes).
- Ein Monaco-Singleton für alle Tabs (`wwwroot/editor/main.js:123`), Tabwechsel = `setValue` — kein Undo/Scroll pro Datei.
- `InitCompileAsync` lädt bei jedem Compile ~40 DLLs per HTTP neu (`CompilationService.cs:60,237`) — Kommentar behauptet Caching, existiert aber nicht.
- NuGet: keine transitiven Dependencies, kein TFM-Filter (`NugetReferenceService.cs:94-105` nimmt alle `.dll` im Archiv), Cache nur RAM pro WASM-Instanz.
- Kein Embed-Modus, kein Branding, Query-Flags via `Uri.Contains` (`MainLayout.razor.cs:39-42`, `Repl.razor.cs:166-170`).
- CI: `.github/workflows/TryMudEx.yml` → Azure WebApp `TryMudEx` bei jedem Push (jeder Branch!).

---

## Phase 1 — Basis reparieren

### 1.1 NuGet: transitive Dependencies + TFM-Filter

**Problem:** Humanizer ist Meta-Paket (leeres `lib/`, alles in Dependencies) → 0 DLLs geladen → Demo tot. Andere Pakete liefern `net472/`/`ref/`/`analyzers/`-DLLs → Compile-Crash.

**Änderungen in `Try.Core/NugetReferenceService.cs`:**

- Nach nupkg-Download die `.nuspec` aus dem Archiv lesen (kein Extra-Request), `<dependencies>`-Group per TFM wählen, Dependencies rekursiv laden.
- Skip-Liste: bereits installierte Pakete, `DefaultPackages` (MudBlazor, MudBlazor.Extensions), Framework-Pakete (`Microsoft.NETCore.*`, `System.*`, `NETStandard.Library`, `Microsoft.AspNetCore.*` — sind in der WASM-Runtime schon da).
- Zyklen-Schutz: besuchte `id/version`-Menge.
- TFM-Auswahl statt „alle DLLs": bestes `lib/<tfm>/` in Präferenzreihenfolge `net10.0 → net9.0 → … → net6.0 → netstandard2.1 → netstandard2.0`. Kein kompatibles TFM → Fehler an UI („Paket X unterstützt kein .NET 10/netstandard2.0").
- Versionskonflikt bei transitiven Paketen: höchste angeforderte Version gewinnt (naive Resolution reicht hier).

### 1.2 NuGet-Caching über HTTP-Cache

`TryMudEx.Server/Controllers/NugetController.cs` (Proxy `api/nuget/package/{id}/{version}`): Response-Header `Cache-Control: public, max-age=31536000, immutable` (id+version sind unveränderlich). Browser-Cache bedient dann Editor- und iframe-Instanz. RAM-Cache (`_packageCache`) bleibt als L1.

### 1.3 Compile-Referenzen einmalig laden

`CompilationService`: `InitCompileAsync` idempotent machen — `_baseCompilation`/Referenzliste nach erstem Aufruf wiederverwenden (Task-Cache, damit parallele Compiles nicht doppelt laden). NuGet-MetadataReferences pro installierter Paketmenge gecacht.

**Akzeptanz Phase 1:** Humanizer-Sample kompiliert und läuft. Zweiter Compile deutlich schneller (keine `_framework`-Downloads im Netzwerk-Tab). Bestehende Samples laufen unverändert.

---

## Phase 2 — UI-Umbau (DockLayout, Multi-File, Logs)

### 2.1 Panel-Layout

`Pages/Repl.razor` wird DockLayout-Host. Panels (ids stabil, für Layout-Persistenz):

| Panel | Inhalt | Default |
|---|---|---|
| `files` | File-Tree (MudTreeView): Dateien/Ordner, Kontextmenü (neu, umbenennen, löschen, Ordner anlegen), Doppelklick öffnet Editor-Panel | links, schmal |
| `editor:<pfad>` | Ein Monaco pro geöffneter Datei | Mitte, als Tabs in einer Group |
| `preview` | bestehendes iframe `/user-page` | rechts |
| `errors` | Diagnostics-Tabelle (heute `Components/ErrorList.razor`), Klick öffnet Datei-Panel + springt zur Zeile | unten |
| `console` | NEU: Log-Ausgaben aus dem iframe | unten, Tab neben errors |
| `nuget` | Paket-Suche/Install/Uninstall (heute Dialog `PackageReferences.razor`) | links, Tab neben files, default zu |

- Toolbar (`MudAppBar` oben, ersetzt Mini-Drawer): Run, Save/Share, Download/Upload, Samples-Menü, Theme-Toggle, Panels-Menü (Checkmark = offen, Klick togglet), Layout speichern/zurücksetzen. Statusleiste unten bleibt (Fehler-/Warn-Counter, installierte Pakete).
- Layout-Persistenz: `SaveLayoutAsync` → localStorage, versionierter Key (`trymudex.layout.v1`); Restore mit Fallback auf Default bei kaputtem/altem JSON (Pattern AspireUI `DockLayout.tsx:249-265`).
- Alte ViewModes (`CodeViewMode`, `MudExSplitPanel`-Markup, `Window`-Dialog-Modus) entfallen ersatzlos — Dock ersetzt sie. Responsive: unter `md` erzwungenes einfaches Layout (Editor/Preview gestapelt, Dock deaktiviert).

### 2.2 MudExDockLayout-Erweiterungen (Library)

Benötigt, Stand heute nicht vorhanden:

- **Dynamische Panels zur Laufzeit:** deklarative `MudExDockItem`-Kinder kommen/gehen per `@foreach` über offene Dateien. JS-Seite muss Panel-Add/Remove nach Erst-Init diffen. Falls das JS-Modul (`wwwroot/js/components/…dockLayout…`) das nicht kann: nachrüsten (`addPanel`/`removePanel` auf der JS-Referenz).
- `FloatPanelAsync(id)` / `MaximizePanelAsync(id)` public API (dockview-core kann beides).
- Optional (nur wenn trivial): Panel-Flash beim programmatischen Öffnen.

Diese Erweiterungen sind eigenständig testbar über die bestehende Sample-Page `Samples/MainSample.WebAssembly/Pages/Page_DockLayout.razor`.

### 2.3 Multi-File & Editor-Instanzen

- `Try.Core`-Modell: `CodeFile.Path` erlaubt relative Pfade mit `/` (`Components/Header.razor`). Validierung in `Services/CodeFilesHelper.cs` erweitert (Segmente einzeln validieren, keine `..`, keine führenden `/`). ZIP-Format trägt Pfade bereits — Snippets bleiben kompatibel (flache Alt-Snippets = Root-Dateien).
- `__Main.razor` bleibt Einstiegspunkt (Root, nicht löschbar/verschiebbar). `__Packages.ref` bleibt versteckte Datei.
- Namespace bleibt für alle Dateien `Try.UserComponents` (Razor-Kompilat unabhängig vom Ordner) — Ordner sind reine Organisation. Dokumentiert im UI-Tooltip.
- `wwwroot/editor/main.js`: Umbau von Singleton auf `Map<panelId, {editor, model}>`. API wird instanzbasiert (`create(panelId, el, …)`, `getValue(panelId)`, …). Monaco-Model pro Datei → Undo-Stack, Scrollposition, Marker pro Datei. Sprache aus Dateiendung. Ctrl+S weiterhin global → Compile.
- `Components/CodeEditor.razor(.cs)` wird instanzfähig (Parameter `PanelId`, `Path`); `TabManager` entfällt zugunsten Dock-Groups + File-Tree.
- Diagnostics-Mapping: Compile liefert Fehler pro Datei (heute schon, `CompilationService` nutzt Dateinamen) — Errors-Panel gruppiert nach Datei.

### 2.4 Console-Panel

- `/user-page` (iframe): kleines JS hookt `console.log/info/warn/error` + `window.onerror` + `unhandledrejection`, sendet `postMessage({type:'try-log', level, text, ts})` an Parent. Same-origin, Origin-Check auf beiden Seiten.
- Parent: JS sammelt in Ring-Buffer (2000 Zeilen), Blazor-Panel rendert virtualisiert oder chunked; Features: Follow-Toggle (Smart-Autoscroll: nur wenn <40px vom Boden), Level-Filter, Textfilter, Clear, Copy. Error-Zeilen rot. (Patterns aus AspireUI `ResourceLogDrawer.tsx`.)
- .NET-seitige Logs des User-Codes (`ILogger`/`Console.WriteLine`) landen in WASM sowieso in der Browser-Console → werden vom Hook miterfasst. Kein eigener Log-Provider nötig.

**Akzeptanz Phase 2:** Alle heutigen Funktionen erreichbar (Run, Save, Samples, Upload/Download, NuGet, Theme). Datei in Ordner anlegen, in zwei Editor-Panels nebeneinander arbeiten, Layout überlebt Reload, `console.log` aus User-Code erscheint im Console-Panel, Fehlerklick springt in richtige Datei/Zeile.

---

## Phase 3 — Embed-Mode + Playzor.Blazor

### 3.1 Embed-Routen

Neue Seite `Pages/Embed.razor` (EmptyLayout, kein Drawer/AppBar):

- `/embed/{snippetId}` — gespeichertes Snippet (16-stellige ID) oder Inline-Code (andere Länge, wie Repl)
- `/embed/samples/{sample}` und `/embed/from/{urlBase64}` — gleiche Lade-Semantik wie `Repl.razor:1-3` (gemeinsamer Loader-Service, kein Copy-Paste)

Layout: oben schmale Tab-Leiste (Dateien) + View-Switcher (Code/Preview/Split), Inhalt, unten Footer: Brand-Logo + „Edit on {BrandName}"-Link. Kein Dock — Embed ist bewusst einfach (fixe Splits).

### 3.2 Embed-Query-Optionen

Echtes Query-Parsing (ersetzt `Uri.Contains`-Hacks, gemeinsamer `QueryOptions`-Helper auch für Repl):

| Param | Werte | Default |
|---|---|---|
| `view` | `preview` \| `code` \| `split` | `split` |
| `file` | Startdatei-Pfad | `__Main.razor` |
| `readonly` | flag | aus (editierbar) |
| `autorun` | `true`/`false` | `true` |
| `theme` | `dark` \| `light` \| `auto` | `auto` (prefers-color-scheme) |
| `hideheader` | flag | aus |

- Editierbar heißt: User tippt, Ctrl+S/Run-Button kompiliert im Embed. „Edit on {Brand}"-Link nimmt den *aktuellen* (ggf. geänderten) Code als Inline-URL mit.
- „Edit on"-Ziel = eigener Origin des Embeds (relative URL `/snippet/…`) → Domain-Branding kommt automatisch: Embed von playzor.de verlinkt auf playzor.de.
- Auto-Height: Embed sendet `postMessage({type:'playzor-resize', height})` bei Größenänderung; `Playzor.Blazor`-Komponente und dokumentiertes Script-Snippet konsumieren das. Reine Option — ohne Script fixe Höhe.

### 3.3 Server-Anpassungen

- `frame-ancestors`: für `/embed*` explizit erlauben (`Content-Security-Policy: frame-ancestors *`), übrige Seiten unangetastet (heute kein X-Frame-Options gesetzt — bleibt so, nur Embed bekommt explizite Freigabe, damit künftige Hardening-Änderungen Embed nicht brechen).
- iframe `/user-page` bleibt same-origin, unverändert.

### 3.4 Playzor.Blazor (neues Projekt)

`Playzor.Blazor/Playzor.Blazor.csproj` — Razor-Class-Library, net10.0, Dependencies: nur `Microsoft.AspNetCore.Components.Web`. Im Repo-Root neben `MudBlazor.Extensions`, Teil der Haupt-Solution.

```razor
<PlayzorPlayground Code="@myCode" />
<PlayzorPlayground Files="@myFilesDict" View="PlaygroundView.Split"
                   Height="600px" Theme="PlaygroundTheme.Auto"
                   ReadOnly AutoRun="false" Host="https://playzor.net" />
```

- Parameter: `Code` (string, eine Datei) XOR `Files` (`IDictionary<string,string>` Pfad→Inhalt), `View`, `Height` (CSS-Wert, default `500px`), `Theme`, `ReadOnly`, `AutoRun`, `File` (Start-Tab), `HideHeader`, `Host` (default `https://playzor.net`), `SnippetId` (statt Code — bettet gespeichertes Snippet ein).
- Rendert `<iframe src="{Host}/embed/{inlineCode}?{options}" …>` + optional Auto-Height-Listener (kleines eingebettetes JS via `IJSRuntime`, collocated `PlayzorPlayground.razor.js`).
- URL-Encoding: gleiche base64url+deflate-Logik wie `SnippetsService` — kleiner interner Encoder (Code-Duplikation bewusst: kein Paket-Dependency auf Try.Core; Format ist stabil und dokumentiert; Roundtrip-Test sichert Kompatibilität).
- URL-Längen-Schutz: encodierte URL > ~8 KB → Warnung im Debug-Log, Render trotzdem (Browser-Limits liegen höher).
- `SaveAsSnippet`: **nicht** in v1 (bräuchte API-Call beim Rendern, Fehlerpfade, Rate-Limits — YAGNI; SnippetId-Parameter deckt den Fall „kurze URL" ab: einmal manuell speichern).

**Akzeptanz Phase 3:** Testseite (lokales HTML + Sample im MainSample) bindet Embed via iframe und via `<PlayzorPlayground>` ein; alle View-Modi funktionieren; „Edit on"-Link öffnet vollen Playground mit identischem Code; readonly verhindert Edits; Auto-Height funktioniert mit Listener-Script.

---

## Phase 4 — Domain-Branding

### 4.1 Brand-Modell (Client)

`TryMudEx.Client/Services/BrandingService.cs`:

```csharp
record Brand(string Key, string Name, string LogoUrl, string PrimaryColor,
             string CanonicalHost, string DefaultCulture,
             string[] DefaultPackages, string SamplesSet,
             MudTheme Theme /* Light+Dark Paletten */);
```

- Auflösung: `window.location.hostname` → Mapping (`*.mudex.org` → MudEx-Brand, `playzor.net`/`www.playzor.net` → Playzor en, `playzor.de` → Playzor de, unbekannt/localhost → via `?brand=`-Query übersteuerbar, sonst MudEx).
- Wirkung: Landingpage-Inhalte, Logo/Name in Toolbar+Embed-Footer, MudTheme-Paletten, Default-Template für „New" (MudEx: heutiges Template; Playzor: neutrales Blazor-Template ohne MudBlazor-Provider-Zwang), Samples-Liste (`api/Snippets/samples?set=<brand>` — Server filtert nach Unterordner in `wwwroot/data/`), DefaultPackages (`CoreConstants.DefaultPackages` wird brand-abhängig).

**Achtung Compile-Pipeline:** `CompilationService.cs:37-40` injiziert MudBlazor-Provider fix in Datei 0. Wird brand-/paketabhängig: Provider nur einfügen, wenn MudBlazor in den Paketen ist (Playzor-Neutral-Template läuft sonst ohne).

### 4.2 index.html / Meta-Tags (Server)

Middleware in `TryMudEx.Server`: liefert `index.html` mit Token-Ersetzung pro Request (`{{BRAND_TITLE}}`, `{{BRAND_DESCRIPTION}}`, `{{BRAND_LOGO}}`, `{{BRAND_OG_*}}`, Loader-`AppName`/`AccentColor`) anhand Host-Header. Gecacht pro Brand (3 Varianten im RAM). Ersetzt tote `#{CACHE_TOKEN}#`-Platzhalter.

### 4.3 Lokalisierung

- `Microsoft.Extensions.Localization` + resx (`Resources/`) für Repl-/Embed-UI-Strings (Toolbar, Panels, Dialoge, Fehlermeldungen der UI — nicht Compiler-Output). Sprachen: `en` (default), `de`.
- Culture-Wahl: Brand-`DefaultCulture`, übersteuerbar per `?lang=` + localStorage. `playzor.de` → `de`.
- `BlazorWebAssemblyLoadAllGlobalizationData` nicht nötig (nur UI-Strings, keine Kultur-Formatierung).

### 4.4 Domains/Deployment

- Azure WebApp: Custom Domains `playzor.net`, `www.playzor.net`, `playzor.de` + Zertifikate — manuell im Portal (nicht Teil des Codes; dokumentiert in README).
- Hinweis: CI (`.github/workflows/TryMudEx.yml`) triggert auf `branches: '**'` und deployed nach Prod. Wir pushen diesen Branch nicht — kein Handlungsbedarf jetzt; vor dem ersten Push den Trigger auf `main` einschränken.

**Akzeptanz Phase 4:** `localhost?brand=playzor` zeigt Playzor-Branding (Name, Logo, Theme, neutrales Template, eigene Samples), `?brand=playzor&lang=de` deutsch; mudex.org-Verhalten unverändert; OG-Tags pro Host korrekt (curl-Check).

---

## Tests

- **Try.Tests (NUnit), neue Unit-Tests ohne Netz:** `CodeFilesHelper`-Pfadvalidierung (gültig/ungültig/`..`), `NugetReferenceService`-TFM-Auswahl + transitive Resolution + Zyklen (in-memory-nupkg-Streams), Inline-Code-Encoder-Roundtrip Try.Core ↔ Playzor.Blazor (Kompatibilitätsgarantie), `QueryOptions`-Parsing, Brand-Auflösung (Host→Brand).
- Bestehender `SnippetsServiceTests` (braucht Azure) bleibt unangetastet.
- E2E manuell pro Phase gemäß Akzeptanzkriterien.

## Fehlerbehandlung

- NuGet: Paket nicht gefunden / kein kompatibles TFM / Dependency-Fehler → Snackbar + Eintrag im Console-Panel mit Paketname und Grund. Compile läuft mit geladenen Referenzen weiter (Fehler zeigt dann fehlende Typen).
- Compile-Fehler: wie heute Errors-Panel; zusätzlich Datei-Zuordnung für Sprung.
- Embed: Snippet nicht ladbar → Fehlerkarte im Embed mit Link zur Vollseite.
- Layout-Restore-Fehler: still auf Default zurückfallen, kaputten Key löschen.

## Nicht-Ziele (v1)

- Kein Account-System/eigene Snippet-Verwaltung, kein Fork/Like/Kommentar (CodePen-Social-Features).
- Kein `SaveAsSnippet` in `PlayzorPlayground` (SnippetId-Parameter deckt Kurz-URL ab).
- Kein serverseitiges Compile, keine IntelliSense-Server-API (Roslyn-LSP wie AspireUI wäre Kandidat für v2 — Browser-Roslyn bleibt).
- Keine Layout-Galerie/benannte Layouts (nur ein persistiertes Layout + Reset).
- Kein Pop-out von Panels in eigene Fenster (v2-Kandidat, dockview kann es).

## Risiken

| Risiko | Mitigation |
|---|---|
| MudExDockLayout kann dynamische Panels nicht sauber (JS-Diff) | Früh in Phase 2 mit Spike verifizieren; Fallback: `addPanel`/`removePanel`-API in JS-Modul nachrüsten; letzter Ausweg lt. User: neue Komponente |
| Monaco-Multi-Instanz-Umbau bricht bestehende Editor-Features (Completion, Ctrl+S, Theme) | Feature-Liste aus `main.js` vor Umbau extrahieren, nach Umbau einzeln durchtesten |
| Alte Snippets (flach, ohne Pfade) müssen laden | Pfadlose Namen bleiben gültig (Root); Roundtrip-Test mit bestehendem Sample-ZIP |
| try.mudblazor.com-Proxy fällt aus | Bewusst akzeptiert (User-Entscheidung); Inline-URLs funktionieren weiter |
| CI deployed jeden gepushten Branch nach Prod | Branch wird nicht gepusht; vor erstem Push Trigger auf `main` einschränken |
