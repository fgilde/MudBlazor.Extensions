# TryMudEx → Playzor — Umsetzungsstand

Ergänzt [2026-07-27-trymudex-codepen-redesign-design.md](2026-07-27-trymudex-codepen-redesign-design.md).
Branch `feat/try-adapt`, **nicht gepusht**.

## Alle vier Phasen umgesetzt

### Phase 1 — NuGet + Compile
- Transitive Dependencies aus der eingebetteten nuspec, Framework-Pakete werden übersprungen, Zyklenschutz.
- TFM-Auswahl statt „alle DLLs": bestes `lib/<tfm>` (net10 → … → netstandard2.0).
- Proxy-Antworten mit `Cache-Control: immutable`, NuGet-Fehlerstatus wird durchgereicht.
- Framework-Referenzen (~40 DLLs) laden einmal pro App-Lifetime.
- Bugfix: Paket-Installationen gingen beim Dialog-Schließen verloren (Parameter-Mutation in `PackageReferences`).

### Phase 2 — Dock-UI
- `MudExDockLayout` kann jetzt Panels zur Laufzeit: Hinzufügen über den (nicht mehr abgeschalteten) MutationObserver, Entfernen über `MudExDockItem.Dispose` → `removePanelById`.
  **Warum nicht per DOM-Diff entfernen:** dockview hängt inaktive Panels mit `onlyWhenVisible` selbst aus dem DOM — ununterscheidbar von einer Blazor-Entfernung.
- Neue public API: `AddPanelAsync`, `RemovePanelAsync`, `ActivatePanelAsync`, `FloatPanelAsync`, `MaximizePanelAsync`, `ExitMaximizedAsync`; neuer `MudExDockItem.StackWith`.
- Monaco: Instanz + Model pro Datei (Undo/Scroll pro Datei), echtes Dispose.
- FileTree mit Ordnern, Anlegen/Umbenennen/Löschen, Templates.
- Console-Panel: iframe hookt `console.*` + `onerror` + `unhandledrejection` → `postMessage` → Ring-Buffer 2000, Filter, Follow, Copy.
- Compiler-Diagnostics als Monaco-Marker (Wellenlinien) mit korrigierter Zeilenzuordnung.
- Layout + offene Dateien in localStorage (versionierte Keys).
- Ordner erzeugen Sub-Namespaces — die Pipeline ergänzt `@using <Root>.<Ordner>` automatisch. Nebenbei behoben: User-`_Imports.razor` wirkte nur im ersten Compile-Pass.

### Phase 3 — Embed + Paket
- `/embed/{id}`, `/embed/samples/{name}`, `/embed/from/{url}`; Optionen `view`, `file`, `readonly`, `autorun`, `theme`, `hideheader`.
- Inline-Code-Encoder in `Playzor.Core.InlineCode` (Encode war vorher nicht vorhanden).
- Neues Paket `Playzor.Blazor` (net8/9/10, nur `Microsoft.AspNetCore.Components.Web`): `<PlayzorPlayground>` mit `Code`/`Files`/`SnippetId`, `View`, `Theme`, `ReadOnly`, `AutoRun`, `Height`, `AutoHeight`, `Host`.
- Kompatibilität der beiden Encoder ist per Test festgenagelt (`PlayzorCodeCompatibilityTests`).
- Doku-/Testseite `/embed-docs` nutzt das Paket selbst.

### Phase 4 — Branding
- `Playzor.Core.Brand`: MudEx / Playzor / Playzor-DE, Auflösung per Host (`Brand.FromHost`), `?brand=` als Dev-Override.
- `IndexHtmlService` rendert index.html pro Request: Brand-Tokens (Titel, Description, Canonical, OG/Twitter, Loader) plus Asset-Version als Cache-Buster.
- Playzor: neutrales Blazor-Template ohne MudBlazor-Markup, eigene Beschreibung, eigener Akzent, `.de` mit deutscher UI.
- `PlaygroundLocalizer`: Dictionary statt resx (keine Satellite-Assemblies im WASM-Download), Fallback auf Englisch, `?lang=` übersteuert.

## Zusätzlich behoben (nicht geplant)
- `.dv-hide-close { background: yellow !important }` — Debug-Rest im Library-CSS; jetzt versteckt korrekt den Close-Button des Tabs.
- Editor-Wert ging verloren, wenn er gesetzt wurde während Monaco noch lud (betraf jedes spät geladene Snippet/Sample).
- `#{CACHE_TOKEN}#`-Platzhalter (nie ersetzt) durch echten Asset-Versions-Token abgelöst.
- Repl-Shell auf Flex-Layout; `.try-editor{height:100%}` und `.try-errorlist{position:absolute}` aus der generierten CSS werden gezielt überschrieben.
- Development liefert `no-cache` für alle Antworten — sonst laufen Tests gegen alte DLLs.
- Landingpage räumt jetzt auch Layout- und Open-Files-Keys, nicht nur `__temp_code`.

## Bewusst offen
- **CI**: `.github/workflows/TryMudEx.yml` deployt bei Push auf **jedem** Branch nach Produktion. Vor dem ersten Push auf `main` einschränken.
- **Custom Domains** playzor.net / playzor.de + Zertifikate müssen im Azure-Portal eingerichtet werden.
- Marketing-Copy der Landingpage („Smooth Coding" etc.) ist weiter englisch und MudBlazor-lastig; nur Headline, Untertitel, Play-Button und Navigation sind brand-/sprachabhängig.
- Snippet-Speicherung bleibt der Proxy auf try.mudblazor.com (so entschieden).
- NuGet-Verwaltung ist weiter ein Dialog, kein Dock-Panel.
- Kein Panel-Pop-out, keine benannten Layouts, kein `SaveAsSnippet` in der Komponente.
- SCSS wird nicht beim Build kompiliert (VS-Plugin) — Änderungen an `_mudexdockview.scss` müssen in `mudBlazorExtensions.css` und `.min.css` gespiegelt werden.

## Prüfstand
- 75 Unit-Tests grün (`dotnet test TryMudEx/Try.Tests --filter "FullyQualifiedName!~SnippetsService"`; der ausgenommene Test braucht Azure).
- E2E im Browser geprüft: Humanizer-Sample läuft, Multi-File mit Ordner kompiliert, Panel öffnen/schließen/wiederherstellen, Layout-Restore, Console-Ausgabe, Fehlerklick springt zur richtigen Zeile, Light/Dark, Embed mit AutoRun, `<PlayzorPlayground>` im laufenden Playground, Branding für alle drei Domains.
