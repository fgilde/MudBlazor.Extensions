# Phase 2: Dock-UI, Multi-File, Console Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** TryMudEx-Repl wird Dock-Layout (MudExDockLayout) mit File-Tree, einem Monaco-Editor-Panel pro Datei, Errors- und Console-Panel; MudExDockLayout lernt dynamische Panels.

**Architecture:** Erst Library-Fähigkeiten (JS-Observer inkrementell + explizite Panel-API), dann TryMudEx-Editor-Schicht instanzfähig (Monaco-Map statt Singleton), dann Repl-Shell-Tausch (DockLayout + FileTree + Console statt TabManager/SplitPanel). Jeder Task lässt die App lauffähig.

**Tech Stack:** Blazor WASM, dockview-core (via MudExDockLayout), Monaco 0.44 (AMD), MudBlazor 9.7, NUnit.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-27-trymudex-codepen-redesign-design.md` (Phase 2)
- Committen ja, **niemals pushen**. Keine Attribution-Zeilen in Commits. Commit-Stil: kurz, lowercase.
- Tests: `dotnet test TryMudEx/Try.Tests/Try.Tests.csproj --filter FullyQualifiedName~<Fixture>`
- App-Start für Browser-Checks: preview_start `trymudex` (`dotnet run --project TryMudEx/TryMudEx.Server/... -- --environment Development`)
- `MudExDockLayout.min.js` muss nach JS-Änderungen aktualisiert werden (zur Not 1:1-Kopie der unminifizierten Datei — `JsImportHelper` lädt je nach Modus die min-Variante).
- Panel-Ids in TryMudEx immer explizit (nie `BuildPathId`-Auto-Ids — Index-basiert, rutschen bei Remove).
- Erhaltungs-Checkliste Editor (aus Bestandsanalyse): Completion-Provider einmalig global (`window.Try.__providerRegistered`), `razorDefaults.setModeConfiguration` einmalig, Theme global via `monaco.editor.setTheme`, Ctrl+S-Throttle 1000ms am `window`, `automaticLayout:true`, `@code {`-Normalisierung, `_overrideValue`-Pattern pro Instanz, Sprachableitung aus Extension.

## Bewusste Vereinfachungen (v1)

- NuGet bleibt Dialog (frisch gefixt) statt Dock-Panel — reine Verpackung, später verschiebbar.
- Responsive unter `md`: gestapeltes Einfach-Layout (Datei-Select + Editor + Preview), kein Dock.
- Kein Panel-Pop-out, keine benannten Layouts (Spec: Nicht-Ziele).
- Rename nur für Dateien, nicht für Ordner (Ordner-Rename = Dateien einzeln umbenennen).

---

### Task 1: MudExDockLayout JS — inkrementeller Observer, elementStore-Cleanup, initialLayoutJson-Fix

**Files:**
- Modify: `MudBlazor.Extensions/wwwroot/js/components/MudExDockLayout.js` (Pfad verifizieren — Datei heißt `MudExDockLayout.js`, liegt unter wwwroot/js; exakten Pfad per Glob suchen)
- Modify: zugehörige `.min.js` daneben
- Modify: `Samples/MainSample.WebAssembly/Pages/Page_DockLayout.razor` (Testbuttons)

**Interfaces:**
- Produces (JS-Instanzmethoden, zusätzlich):
  - `addPanel(optionsJson)` — options wie `data-options`-Format `{id,title,direction,...}`; Element wird aus DOM/`elementStore` gesucht
  - `removePanel(id)`, `activatePanel(id)`, `floatPanel(id)`, `maximizePanel(id)`, `exitMaximized()`
- Verhalten: MutationObserver bleibt nach Bootstrap aktiv — neue direkte `.dv-node`-Leaves ⇒ indexieren + `api.addPanel`; aus DOM verschwundene Ids ⇒ `api.removePanel` + `elementStore.delete(id)`. `init()` mit `initialLayoutJson` ruft zusätzlich `wireEvents()` + Observer + `indexDomLeaves()` (Panels aus JSON finden ihre Elemente).

- [ ] **Step 1: JS-Datei lokalisieren und komplett lesen**

Run: Glob `MudBlazor.Extensions/wwwroot/js/**/MudExDockLayout*.js`, dann Read. Die Analyse-Referenzzeilen: `init:18-38`, `_createComponent:78-122`, `indexDomLeaves:149-173`, `observeAndBootstrap:175-188`, `setOptions:190-192`, `bootstrapFromDom:198-242`, `toJSON/fromJSON:246-252`, `reinitialize:260-337`, `dispose:341`.

- [ ] **Step 2: Observer inkrementell umbauen**

In `observeAndBootstrap()`: One-Shot-Logik (`if (this.bootstrapped) return;` + `observer.disconnect()`) ersetzen durch:

```js
observeAndBootstrap() {
    this.tryBootstrap();
    if (this.observer) return;
    this.observer = new MutationObserver(() => {
        if (!this.bootstrapped) { this.tryBootstrap(); return; }
        this.syncDomPanels();
    });
    this.observer.observe(this.containerRef, { childList: true, subtree: true });
}

tryBootstrap() {
    // bisheriger Bootstrap-Pfad (indexDomLeaves + bootstrapFromDom), setzt this.bootstrapped = true bei Erfolg
}

syncDomPanels() {
    if (!this.api) return;
    // 1) neue Leaves: direkte .dv-node-Kinder des Containers, die nicht im elementStore sind
    const rootNodes = Array.from(this.containerRef.querySelectorAll(':scope > .dv-node'));
    for (const node of rootNodes) {
        const opts = this._parseOptions(node); // data-options JSON.parse, wie in indexDomLeaves
        if (!opts?.id || this.elementStore.has(opts.id)) continue;
        node.style.display = 'none';
        this.elementStore.set(opts.id, node);
        const panel = { id: opts.id, component: 'default', title: opts.title || opts.id, params: opts };
        const active = this.api.activePanel;
        if (opts.float) this.api.addFloatingGroup(this.api.addPanel(panel));
        else this.api.addPanel(active && !opts.direction
            ? { ...panel, position: { referencePanel: active.id, direction: 'within' } }
            : { ...panel, position: opts.direction ? { direction: opts.direction } : undefined });
    }
    // 2) entfernte Elemente: elementStore-Einträge, deren Node nicht mehr im Dokument hängt
    for (const [id, el] of Array.from(this.elementStore.entries())) {
        if (document.contains(el)) continue;
        const panel = this.api.getPanel(id);
        if (panel) this.api.removePanel(panel);
        this.elementStore.delete(id);
    }
}
```

(`_parseOptions` = die bestehende data-options-Parse-Logik aus `indexDomLeaves` extrahieren, damit beide Pfade identisch parsen. Exakte Feldnamen — `direction`-Werte, `float` — beim Lesen der Datei aus `bootstrapFromDom`/`_planFromNode` übernehmen, nicht raten.)

- [ ] **Step 3: `init()`-Fix für initialLayoutJson**

Der Early-Return-Pfad bei gesetztem `initialLayoutJson` muss vorher `indexDomLeaves()`, `wireEvents()` und `observeAndBootstrap()` ausführen (Reihenfolge: erst indexieren, dann `fromJSON`, dann Events+Observer; `bootstrapped = true` setzen, damit der Observer in den Sync-Modus geht).

- [ ] **Step 4: Explizite API-Methoden ergänzen**

```js
addPanelByOptions(optionsJson) {
    const opts = typeof optionsJson === 'string' ? JSON.parse(optionsJson) : optionsJson;
    /* wie syncDomPanels-Add-Zweig, Element via _createComponent-Suchpfad */
}
removePanelById(id) { const p = this.api?.getPanel(id); if (p) { this.api.removePanel(p); this.elementStore.delete(id); } }
activatePanel(id) { this.api?.getPanel(id)?.api.setActive(); }
floatPanel(id) { const p = this.api?.getPanel(id); if (p) this.api.addFloatingGroup(p); }
maximizePanel(id) { const p = this.api?.getPanel(id); if (p) { p.api.setActive(); p.group.api.maximize(); } }
exitMaximized() { this.api?.exitMaximizedGroup?.(); }
```

(dockview-core-API-Namen beim Implementieren gegen `dockview-core.esm.js` verifizieren: `addFloatingGroup`, `maximizeGroup`/`group.api.maximize`, `exitMaximizedGroup`.)

- [ ] **Step 5: min.js aktualisieren**

Prüfen, ob ein Bundler-Setup existiert (`bundleconfig.json`); sonst `.min.js` als Kopie der Quelldatei schreiben.

- [ ] **Step 6: Sample-Page als Testbett**

`Page_DockLayout.razor`: `@foreach (var p in _dynamicPanels) { <MudExDockItem Id="@p" Title="@p">...</MudExDockItem> }` + zwei Buttons „Add panel" / „Remove last". Buttons mutieren die Liste.

- [ ] **Step 7: Browser-Verifikation**

MainSample starten (launch.json-Eintrag ergänzen oder `dotnet run --project Samples/MainSample.WebAssembly` — WASM-Projekt braucht Host: `dotnet run` startet DevServer), Page_DockLayout öffnen: Panels erscheinen/verschwinden zur Laufzeit, kein Zombie, Layout-Drag funktioniert weiter.

- [ ] **Step 8: Commit**

```bash
git add MudBlazor.Extensions/wwwroot/js/... Samples/MainSample.WebAssembly/Pages/Page_DockLayout.razor
git commit -m "docklayout dynamic panels and explicit panel api"
```

---

### Task 2: MudExDockLayout C# — öffentliche Panel-API

**Files:**
- Modify: `MudBlazor.Extensions/Components/MudExDockLayout.razor.cs`

**Interfaces:**
- Produces (public, auf `MudExDockLayout`):
  - `Task AddPanelAsync(string optionsJson)` → JS `addPanelByOptions`
  - `Task RemovePanelAsync(string id)` → JS `removePanelById`
  - `Task ActivatePanelAsync(string id)` → JS `activatePanel`
  - `Task FloatPanelAsync(string id)` / `Task MaximizePanelAsync(string id)` / `Task ExitMaximizedAsync()`

- [ ] **Step 1: Methoden ergänzen** (Muster wie `SaveLayoutAsync`, `JsReference!.InvokeVoidAsync("name", args)`)
- [ ] **Step 2: Build** `dotnet build MudBlazor.Extensions/MudBlazor.Extensions.csproj` → 0 Fehler
- [ ] **Step 3: Commit** `git commit -m "docklayout public panel api"`

---

### Task 3: main.js → Monaco-Instanz-Map + CodeEditor instanzfähig

**Files:**
- Modify: `TryMudEx/TryMudEx.Client/wwwroot/editor/main.js` (Editor-IIFE)
- Modify: `TryMudEx/TryMudEx.Client/Components/CodeEditor.razor` + `.razor.cs`
- Modify: `TryMudEx/TryMudEx.Client/Pages/Repl.razor.cs` (AddCodeFile-Direktaufruf `Try.Editor.setLangugage` entfernen)
- Modify: `TryMudEx/TryMudEx.Client/wwwroot/css/site.css` (`#user-code-editor`-Selektor → `.code-editor`)

**Interfaces:**
- Produces (JS `window.Try.Editor`, alle mit `id` als erstem Parameter):
  - `create(id, value, language, readOnly, theme)` — erzeugt `monaco.editor.createModel(value, language)` + `monaco.editor.create(el, {model, ...})`, speichert in `Map<id,{editor,model,pendingValue}>`; disposed eine evtl. vorhandene alte Instanz gleicher id sauber (`editor.dispose()` + `model.dispose()`)
  - `getValue(id)`, `setValue(id, v)` (Instanz fehlt ⇒ pendingValue je id), `setLanguage(id, lang)`, `setReadOnly(id, ro)`, `focus(id)`, `setSelection(id, sl, sc?, el?, ec?)`, `dispose(id)` (echtes dispose + Map-Delete)
  - `getValues()` — `{[id]: value}` aller lebenden Instanzen (für Einsammeln vor Compile/Save/Download)
  - `setTheme(theme)` — bleibt global (Monaco-Theme ist global)
  - Global bleiben: Provider-Registrierung einmalig (`__providerRegistered`-Guard beim ersten `create`), `razorDefaults.setModeConfiguration` einmalig, `initialize`/Ctrl+S/`reloadIframe`/`CodeExecution` unverändert
- Produces (C# `CodeEditor`):
  - Parameter neu: `Id` (string, required — Repl vergibt eindeutige Ids), `Path` (string, für Sprachableitung), bestehende `Code`/`ReadOnly`/`Theme` bleiben
  - `internal string GetCode()` / `Focus()` / `SelectLineAsync(int?)` — rufen JS mit `Id`
  - echtes `Dispose()` → `Try.Editor.dispose(Id)`
- Consumes: nichts Neues. Repl nutzt weiterhin genau EINEN CodeEditor (Tab-UI unverändert) — dieser Task ist reiner API-Umbau, App verhält sich identisch.

- [ ] **Step 1: main.js Editor-IIFE umbauen** — `let _editor` → `const _instances = new Map(); const _pending = new Map();`; alle Funktionen auf id-Zugriff; `create` mit Model-Erzeugung; Provider-/ModeConfig-Registrierung bleibt im ersten `create` hinter Guard.
- [ ] **Step 2: CodeEditor.razor.cs** — `const EditorId` raus, `[Parameter] public string Id { get; set; }`, div-id `@Id`, alle `JsRuntime`-Calls mit `Id`-Argument; `GetLanguage()` aus `Path`-Extension statt `CodeFileType`-Parameter (Parameter `CodeFileType` bleibt für Abwärtskompatibilität eine Renderquelle, aber Sprache = Extension).
- [ ] **Step 3: Repl.razor** — `<CodeEditor Id="main-editor" ...>` setzen; `Repl.razor.cs:417-418` (`setLangugage`-Direktaufruf) entfernen — Sprachwechsel läuft über CodeEditor-Parameter beim Tab-Wechsel.
- [ ] **Step 4: site.css** — `#user-code-editor`-Regeln auf `.code-editor` umstellen.
- [ ] **Step 5: Build + Browser-Smoke** — App starten, Tabs wechseln (Sprache+Inhalt korrekt), Ctrl+S kompiliert, Fehlerklick springt, Theme-Toggle färbt Editor.
- [ ] **Step 6: Commit** `git commit -m "monaco instance map, editor component takes id"`

---

### Task 4: Ordner-Pfade in CodeFilesHelper (TDD)

**Files:**
- Modify: `TryMudEx/TryMudEx.Client/Services/CodeFilesHelper.cs`
- Test: `TryMudEx/Try.Tests/CodeFilesHelperTests.cs` (neu)

**Interfaces:**
- Produces: `NormalizeCodeFilePath(string path, out string error)` akzeptiert jetzt relative Pfade mit `/` (`Components/Header.razor`):
  - Backslashes → `/`; führende/`..`/leere Segmente ⇒ Fehler
  - Ordnersegmente: `SyntaxFacts.IsValidIdentifier`-Prüfung pro Segment (gleiche Regel wie Dateiname)
  - Dateisegment: bestehende Regeln unverändert (Extension-Whitelist, Identifier, `.razor` großgeschrieben)
  - Rückgabe: normalisierter Gesamtpfad
- `ValidateCodeFilesForSnippetCreation` bleibt semantisch (nutzt intern die neue Normalize-Logik).

- [ ] **Step 1: Failing Tests**

```csharp
using NUnit.Framework;
using TryMudEx.Client.Services;

namespace Try.Tests;

[TestFixture]
public class CodeFilesHelperTests
{
    [TestCase("__Main.razor", "__Main.razor")]
    [TestCase("Components/Header.razor", "Components/Header.razor")]
    [TestCase(@"Components\Header.razor", "Components/Header.razor")]
    [TestCase("Services/My/Deep/Service.cs", "Services/My/Deep/Service.cs")]
    public void NormalizeCodeFilePath_ValidPaths(string input, string expected)
    {
        var result = CodeFilesHelper.NormalizeCodeFilePath(input, out var error);
        Assert.That(error, Is.Null);
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("../evil.razor")]
    [TestCase("/rooted.razor")]
    [TestCase("a//b.razor")]
    [TestCase("bad-folder/File.razor")]
    [TestCase("Folder/lowercase.razor")]
    public void NormalizeCodeFilePath_InvalidPaths(string input)
    {
        CodeFilesHelper.NormalizeCodeFilePath(input, out var error);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }
}
```

- [ ] **Step 2: Run — FAIL** (Pfade mit `/` werden heute verworfen bzw. Verzeichnis gestrippt)
- [ ] **Step 3: Implementieren** — Segment-Split, Ordner-Validierung, Datei-Validierung wie bisher, Join mit `/`.
- [ ] **Step 4: Run — PASS**, zusätzlich `--filter FullyQualifiedName~NugetPackageHelper` weiter grün
- [ ] **Step 5: Commit** `git commit -m "code file paths with folders"`

---

### Task 5: Repl-Umbau auf DockLayout + FileTree

**Files:**
- Create: `TryMudEx/TryMudEx.Client/Components/FileTree.razor` + `.razor.cs`
- Modify: `TryMudEx/TryMudEx.Client/Pages/Repl.razor` (komplette Shell) + `.razor.cs`
- Delete: `TryMudEx/TryMudEx.Client/Components/TabManager.razor` + `.razor.cs`, `TryMudEx/TryMudEx.Client/Enums/CodeViewMode.cs`
- Modify: `TryMudEx/TryMudEx.Client/wwwroot/css/site.css` (Dock-Höhen, `.try-*`-Anpassungen)

**Interfaces:**
- FileTree Parameter: `Files` (`IEnumerable<CodeFile>`), `ActivePath` (string), Events: `OnOpen(string path)`, `OnCreate(string path)`, `OnRename((string oldPath, string newPath))`, `OnDelete(string path)`. Baut `MudTreeView` aus Pfaden (Ordnerknoten aus Segmenten abgeleitet, keine eigene Ordner-Entität — leerer Ordner existiert nicht). Kontextmenü: New file, New folder (= Prompt, erzeugt `folder/NewFile.razor`), Rename, Delete. `__Main.razor` ohne Rename/Delete. Hidden-Files ausgeblendet.
- Repl-State neu:
  - `List<string> _openFiles` (Pfade, Reihenfolge = Öffnungsreihenfolge, `__Main.razor` immer enthalten)
  - `Dictionary<string, CodeEditor> _editorRefs` (`@ref`-Capture pro Panel; Key = Pfad)
  - Editor-Panel-Id-Konvention: `"ed:" + pfad`; DOM-Editor-Id: `"edd" + Hash/Index` — eindeutig, DOM-safe
  - `CollectAllEditorContentAsync()`: für jeden offenen Pfad `GetCode()` → `CodeFiles[path].Content` — ersetzt `UpdateActiveCodeFileContent` an ALLEN Aufrufstellen (Compile, Save-Popup-Action, Download — Download-Bug damit gefixt) 
  - Rename: neue `CodeFile { Path = neu, Content = alt }`, Dictionary-Rekey, `_openFiles`-Ersetzung; Compile-Diagnostics matchen den neuen Pfad
- Dock-Struktur (Repl.razor, deklarativ):

```razor
<MudExDockLayout @ref="_dock" Id="try-dock" ContainerStyle="height:100%;width:100%"
                 OnPanelRemoved="HandlePanelRemoved" OnActiveChanged="HandleActivePanel">
    <MudExDockItem Id="files" Title="Files" Direction="DockDirection.Left" InitialWidth="220" CanClose="false">
        <FileTree Files="@CodeFiles.Values" ActivePath="@activeCodeFile?.Path"
                  OnOpen="OpenFile" OnCreate="HandleCreateFile" OnRename="HandleRenameFile" OnDelete="HandleDeleteFile" />
    </MudExDockItem>
    @foreach (var path in _openFiles)
    {
        <MudExDockItem @key="path" Id="@EditorPanelId(path)" Title="@System.IO.Path.GetFileName(path)"
                       CanClose="@(path != CoreConstants.MainComponentFilePath)">
            <CodeEditor @ref="..." Id="@EditorDomId(path)" Path="@path" Code="@CodeFiles[path].Content" Theme="@EditorTheme" />
        </MudExDockItem>
    }
    <MudExDockItem Id="preview" Title="Preview" Direction="DockDirection.Right" CanClose="false">
        <iframe id="user-page-window" data-base-src="/user-page" src="/user-page" ...></iframe>
    </MudExDockItem>
    <MudExDockItem Id="errors" Title="Errors" Direction="DockDirection.Down" InitialHeight="180">
        <ErrorList Diagnostics="@Diagnostics" OnDiagnosticClick="OpenDiagnostic" />
    </MudExDockItem>
</MudExDockLayout>
```

  - Toolbar (`MudAppBar` oben, ersetzt Mini-Drawer): Run, Save/Share, Download, Upload, Samples-Menü, Theme-Toggle, Panels-Menü (togglet errors/console/files via Add/RemovePanelAsync), Layout-Reset (localStorage-Key löschen + Reinitialize). Statusleiste unten bleibt unverändert.
  - Layout-Persistenz: `OnPanelMoved`/Resize → debounced `SaveLayoutAsync` → localStorage `trymudex.layout.v1`; `_openFiles` separat unter `trymudex.openfiles.v1`. Beim Init: erst `_openFiles` laden (Schnitt mit existierenden CodeFiles, `__Main.razor` erzwingen) → deklarativ rendern → `InitialLayoutJson` aus localStorage setzen. Restore-Fehler ⇒ Key löschen, Default-Layout.
  - `HandlePanelRemoved(id)`: `ed:`-Panels ⇒ Pfad aus `_openFiles` entfernen (Guard: nicht während Restore/Reinit — Flag `_syncingLayout`).
  - `OpenDiagnostic(diag)`: Pfad nicht offen ⇒ `_openFiles.Add` + StateHasChanged (Observer addiert Panel) ⇒ `ActivatePanelAsync("ed:"+pfad)` ⇒ `SelectLineAsync`.
  - Responsive: `MudHidden`-Split — unter `md` statt DockLayout: `MudSelect` (Datei) + ein CodeEditor + iframe gestapelt (bestehende Komponenten wiederverwendet).
  - Entfallen ersatzlos: `CodeViewMode`, `MudExSplitPanel`-Markup, `Window`-Dialog-Modus, TabManager (Template-Dropdown wandert als Menüpunkte ins FileTree-Kontextmenü: `CodeFileTemplates.All()`).

- [ ] **Step 1: FileTree-Komponente bauen** (MudTreeView<string>, Items aus Pfaden gruppiert; Kontextmenü via MudMenu; Validierung über `CodeFilesHelper.NormalizeCodeFilePath` + Duplikat-Check gegen `Files`; Snackbar bei Fehler — Logik aus TabManager.CreateTabAsync übernehmen)
- [ ] **Step 2: Repl.razor Shell tauschen** (Markup oben; Drawer-Aktionen in AppBar überführen)
- [ ] **Step 3: Repl.razor.cs State-Umbau** (`_openFiles`, `_editorRefs`, `CollectAllEditorContentAsync`, Open/Create/Rename/Delete-Handler, Panel-Events, Layout-Persistenz, `_activeTabIndex`-Logik ersatzlos raus)
- [ ] **Step 4: TabManager + CodeViewMode löschen**, Referenzen bereinigen
- [ ] **Step 5: Build + Browser-Durchlauf** — Kernszenarien: Datei in Ordner anlegen (`Components/Card.razor`), zwei Editor-Panels nebeneinander ziehen, beide editieren, Run (beide Inhalte im Compile), Fehlerklick öffnet+springt, Panel schließen entfernt aus `_openFiles`, Reload stellt Layout+offene Dateien wieder her, Rename, Delete, Samples laden, Upload/Download, Snippet-Save.
- [ ] **Step 6: Commit** `git commit -m "repl dock layout with file tree, remove tab manager"`

---

### Task 6: Console-Panel

**Files:**
- Modify: `TryMudEx/TryMudEx.Client/wwwroot/index.html` (iframe-seitiger Hook, winzig, vor blazor-Start)
- Modify: `TryMudEx/TryMudEx.Client/wwwroot/editor/main.js` (Parent-Collector `window.Try.Console`)
- Create: `TryMudEx/TryMudEx.Client/Components/ConsolePanel.razor` + `.razor.cs`
- Modify: `TryMudEx/TryMudEx.Client/Pages/Repl.razor` (+ MudExDockItem `console` neben `errors`)

**Interfaces:**
- iframe-Hook (in index.html, läuft nur wenn `window.self !== window.top`): wrappt `console.log/info/warn/error/debug` + `window.onerror` + `unhandledrejection` → `parent.postMessage({__try: 'log', level, text: args.map(String).join(' '), ts: Date.now()}, location.origin)`.
- `window.Try.Console` (main.js): `init(dotNetRef)` — message-Listener (Origin-Check `event.origin === location.origin`), Ring-Buffer 2000, batcht neue Einträge und ruft max. alle 250ms `dotNetRef.invokeMethodAsync('OnConsoleBatch', entries)`; `clear()`; `getAll()`.
- `ConsolePanel` (Blazor): hält `List<ConsoleEntry>` (Ring 2000), `[JSInvokable] OnConsoleBatch(ConsoleEntry[])`; UI: Follow-Toggle (Auto-Scroll nur wenn <40px vom Boden — via kleinem JS-Helper `isScrollAtBottom`, existiert schon in main.js:7 und ist derzeit tot → reaktivieren), Level-Filter (MudChipSet), Textfilter, Clear, Copy; Zeilen monospace, `error` rot, `warn` orange. `record ConsoleEntry(string Level, string Text, long Ts)`.

- [ ] **Step 1: iframe-Hook in index.html** (nur `self !== top`-Zweig; VOR `Blazor.start`, damit frühe Fehler ankommen)
- [ ] **Step 2: `Try.Console`-Collector in main.js**
- [ ] **Step 3: ConsolePanel-Komponente** + Dock-Item `console` (Direction Down, Tab neben errors — zweites Item mit gleicher Ziel-Group: `Direction="DockDirection.Down"`; dockview legt es als Tab in die bestehende Down-Group, verifizieren)
- [ ] **Step 4: Browser-Check** — Snippet mit `Console.WriteLine("hi")` + `@code { protected override void OnInitialized() => Console.WriteLine("init!"); }` und einem JS-Fehler; Einträge erscheinen, Follow/Filter/Clear funktionieren, Origin-fremde Messages ignoriert.
- [ ] **Step 5: Commit** `git commit -m "console panel with iframe log capture"`

---

### Task 7: E2E-Gesamtverifikation (kein Commit)

- [ ] Voller Durchlauf der Phase-2-Akzeptanz aus der Spec: alle heutigen Funktionen erreichbar (Run, Save, Samples, Upload/Download, NuGet, Theme); Datei in Ordner; zwei Editor-Panels parallel; Layout überlebt Reload; `console.log` im Console-Panel; Fehlerklick springt in richtige Datei/Zeile; Humanizer-Sample läuft (Phase-1-Regression).
- [ ] Unit-Tests: beide Fixtures grün.

## Self-Review (erledigt)

- Spec-Coverage 2.1 (Task 5), 2.2 (Tasks 1-2), 2.3 (Tasks 3-5), 2.4 (Task 6), Akzeptanz (Task 7). NuGet-Panel bewusst Dialog (dokumentiert oben). ✓
- Namespace-Risiko Ordner-Razor-Dateien: `CreateRazorProjectItem` erhält Pfade mit `/` — Kommentar in CompilationService sagt Format `/a/b/c.razor` ist vorgesehen; generierter Namespace könnte trotzdem `Try.UserComponents.Components` werden → in Task 5 Step 5 explizit testen (Komponente in Ordner per `<Components.Card/>` oder `@using` referenzieren); falls Namespace ordnerabhängig: `_Imports.razor`-Hinweis im FileTree-Tooltip ergänzen statt Pipeline umbiegen. ✓
- Typkonsistenz: Panel-Id-Konvention `ed:`+Pfad einheitlich (EditorPanelId), JS-API-Namen Task 1 = Task 2 Wrapper. ✓
