# Playzor.Blazor.Editor

The [Playzor](https://playzor.net) playground as a Blazor component: monaco editors for any number
of files, roslyn compiling in the browser, dockable panels for file tree, preview, errors and
console, and a tool bar whose built in buttons can be picked and extended.

Looking for the small iframe embed of the hosted playground instead?
That is [Playzor.Blazor](https://www.nuget.org/packages/Playzor.Blazor).

```razor
@using Playzor.Blazor.Editor.Components

<PlayzorEditor Height="100%" DefaultSnippet="@("<h3>Hi</h3>")" />
```

## Setup

```csharp
// Program.cs
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddPlayzor();   // pass a MudEx configuration to let it add MudBlazor too
```

```html
<!-- index.html -->
<link href="_content/Playzor.Blazor.Editor/css/playzor-editor.css" rel="stylesheet" />
<script src="_content/Playzor.Blazor.Editor/lib/monaco-editor/min/vs/loader.js"></script>
<script src="_content/Playzor.Blazor.Editor/js/playzor-editor.js"></script>
```

Two more things are needed, and the editor tells you when one is missing:

* **A package proxy.** nuget.org answers without CORS headers, so a `.nupkg` cannot be fetched from
  the browser. Add [Playzor.Server](https://www.nuget.org/packages/Playzor.Server) and
  `app.MapPlayzorApi()`, or point the editor at a public playground:
  `AddPlayzor(o => o.BaseAddress = PlayzorApiOptions.PlayzorNet)`.
* **A preview page.** The compiled component runs in a second WebAssembly instance. Reference
  [Playzor.UserComponents](https://www.nuget.org/packages/Playzor.UserComponents), follow its
  readme, then point `CompiledPreviewUrl` at that route.

## Content

| Parameter | Default | Description |
|---|---|---|
| `Files` | – | Files to edit. A new collection replaces the whole session |
| `FilesChanged` | – | Raised after edits, with the editor content pulled in |
| `DefaultSnippet` | MudEx sample | Content of the main file for a fresh session |
| `DefaultPackages` | MudBlazor + MudEx | Packages a fresh session starts with |
| `PersistState` | `true` | Keeps files, tabs and layout in local storage |
| `StateKey` | – | Storage prefix, so two editors do not share a session |
| `SnippetStore` | from DI | `IPlayzorSnippetStore` used by save and samples |
| `SnippetId` / `SnippetIdChanged` | – | Id of the loaded snippet, set after saving |

## Tool bar and panels

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

`Save`, `Embed`, `Samples` and `Theme` need someone to answer: their button appears once the
matching event (`OnSaveRequested`, `OnEmbedRequested`, `OnSampleSelected` + `Samples`,
`DarkModeChanged`) is wired — or, for save and samples, once an `IPlayzorSnippetStore` is
registered. `ToolButtons="PlayzorToolButtons.Standalone"` gives you everything that works alone.

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

## Preview, theme and layout

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
