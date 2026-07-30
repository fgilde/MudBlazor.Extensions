# Playzor.Blazor

Two components:

* **`<PlayzorEditor>`** — the whole playground inside your own app: monaco editors for any number
  of files, roslyn compiling in the browser, dockable panels and a live preview.
* **`<PlayzorPlayground>`** — a lightweight iframe embed of the hosted playground
  (playzor.net / try.mudex.org), for a blog or docs page.

---

## PlayzorEditor

```razor
@using Playzor.Blazor.Components

<PlayzorEditor Height="100%" DefaultSnippet="@("<h3>Hi</h3>")" />
```

### Setup

```csharp
// Program.cs — pass a MudEx configuration to let AddPlayzor register MudBlazor too,
// or leave it out when you call AddMudServicesWithExtensions yourself
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddPlayzor(c => c.WithoutAutomaticCssLoading());
```

```html
<!-- index.html -->
<link href="_content/Playzor.Blazor/css/playzor-editor.css" rel="stylesheet" />
<script src="_content/Playzor.Blazor/lib/monaco-editor/min/vs/loader.js"></script>
<script src="_content/Playzor.Blazor/js/playzor-editor.js"></script>
```

The compiled component runs in a **second webassembly instance**, so the host has to serve a page
for it and hand its route to the editor. See `TryMudEx` in the repository for a complete host: it
needs a `/__main` route rendering `Try.UserComponents.__Main`, the boot hook that swaps the compiled
assembly, and `_content/Playzor.Blazor/js/playzor-preview.js` on that page so its console output
reaches the console panel.

### Content

| Parameter | Default | Description |
|---|---|---|
| `Files` | – | Files to edit. A new collection replaces the whole session |
| `FilesChanged` | – | Raised after edits, with the editor content pulled in |
| `DefaultSnippet` | MudEx sample | Content of the main file for a fresh session |
| `DefaultPackages` | MudBlazor + MudEx | Packages a fresh session starts with |
| `PersistState` | `true` | Keeps files, tabs and layout in local storage |
| `StateKey` | – | Storage prefix, so two editors do not share a session |

### Tool bar and panels

| Parameter | Default | Description |
|---|---|---|
| `ToolButtons` | `All` | Flags: `Run`, `Save`, `Embed`, `Download`, `Upload`, `Samples`, `Packages`, `Panels`, `Layout`, `Theme`, plus `Standalone` and `None` |
| `Panels` | `All` | Flags: `Files`, `Preview`, `Errors`, `Console` |
| `ShowStatusBar` | `true` | Error counts, packages, preview reload |
| `ToolBarStartContent` | – | Before the first built in button |
| `ToolBarContent` | – | Between the built in buttons and the spacer |
| `HeaderContent` | – | Right hand side of the tool bar, after the menus |
| `StatusBarContent` | – | Right hand side of the status bar |
| `ChildContent` | – | Additional `MudExDockItem` panels, dockable like the built in ones |

`Save`, `Embed`, `Samples` and `Theme` need the host: their button only appears once the matching
event (`OnSaveRequested`, `OnEmbedRequested`, `OnSampleSelected` + `Samples`, `DarkModeChanged`)
is wired. `ToolButtons="PlayzorToolButtons.Standalone"` gives you everything that works alone.

```razor
<PlayzorEditor ToolButtons="PlayzorToolButtons.Standalone | PlayzorToolButtons.Theme"
               DarkMode="@_dark" DarkModeChanged="@(d => _dark = d)">
    <ToolBarContent>
        <MudIconButton Icon="@Icons.Material.Outlined.Share" OnClick="@ShareAsync" />
    </ToolBarContent>
    <HeaderContent>
        <MudText Typo="Typo.h6" Class="ml-2">My playground</MudText>
    </HeaderContent>
    <ChildContent>
        <MudExDockItem Id="docs" Title="Docs" Direction="DockDirection.Right">
            <MyDocsPanel />
        </MudExDockItem>
    </ChildContent>
</PlayzorEditor>
```

### Preview, theme and layout

| Parameter | Default | Description |
|---|---|---|
| `PreviewUrl` | `/user-page` | Page shown before anything was compiled |
| `CompiledPreviewUrl` | `/__main` | Route of the compiled component |
| `PopoutUrl` | package page | Host page for popped out panels |
| `AutoRun` | `false` | Compiles once as soon as the editor is up |
| `DarkMode` / `DarkModeChanged` | `true` | Monaco theme; the app owns its own theme |
| `Culture` | ui culture | Two letter culture of the editor ui (`en`, `de`) |
| `InitialLayoutJson` | built in | dockview layout to start with |
| `Height` / `ToolBarHeight` / `StatusBarHeight` | `100%` / `48px` / `26px` | Sizes |
| `OnCompiled` | – | Raised after every compilation with the result |

Methods: `TriggerCompileAsync()`, `GetFiles()`, `SetFilesAsync(files)`, `OpenFileAsync(path)`,
`ResetLayoutAsync()`, `ReloadPreview()`.

---

## PlayzorPlayground

```razor
@using Playzor.Blazor

<PlayzorPlayground Code="@("<MudText>Hello from an embed</MudText>")" Height="420px" />
```

| Parameter | Default | Description |
|---|---|---|
| `Code` | – | Content of the main razor file |
| `Files` | – | Multi file snippet (`path` → `content`), paths may contain folders |
| `SnippetId` | – | Id of a saved snippet — keeps the url short |
| `Host` | `https://playzor.net` | Playground host, e.g. `https://try.mudex.org` |
| `View` | `Split` | `Split`, `Code` or `Preview` |
| `Theme` | `Auto` | `Auto`, `Light`, `Dark` |
| `File` | – | File shown first |
| `ReadOnly` | `false` | Disables editing |
| `AutoRun` | `true` | Compiles and runs on load |
| `HideHeader` | `false` | Hides tab bar and buttons |
| `Height` | `500px` | CSS height (ignored with `AutoHeight`) |
| `AutoHeight` | `false` | Iframe follows the embed's content height |

`Code` and `Files` travel inside the url — nothing is uploaded or stored. Use `SnippetId`
for long snippets so the url stays short.

### Multiple files

```razor
<PlayzorPlayground Files="@_files" View="PlayzorView.Split" File="Components/Card.razor" />

@code {
    private readonly Dictionary<string, string> _files = new()
    {
        ["__Main.razor"] = "<Card />",
        ["Components/Card.razor"] = "<MudText Typo=\"Typo.h5\">A card</MudText>",
    };
}
```

Files in a folder land in a sub namespace (`Try.UserComponents.Components`), exactly like a
real Blazor project — the playground adds the matching `@using` automatically.
