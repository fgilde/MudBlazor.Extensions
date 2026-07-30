# Playzor.Blazor

Embed a live, editable Blazor playground into your own Blazor app with one component.

```razor
@using Playzor.Blazor

<PlayzorPlayground Code="@("<MudText>Hello from an embed</MudText>")" Height="420px" />
```

## Parameters

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

## Multiple files

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
