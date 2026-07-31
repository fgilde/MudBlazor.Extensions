[![Playzor](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/refs/heads/main/docs/playzor_logo.png)](https://playzor.net)

# Playzor.Blazor

Embed a live, editable Blazor playground into any page. Readers change the code and run it without
leaving your site — nothing is uploaded, the snippet travels inside the url.

Want the whole editor inside your own app instead of an iframe? That is
[Playzor.Blazor.Editor](https://www.nuget.org/packages/Playzor.Blazor.Editor).

## In a Blazor app

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

## In plain html

The same embed is a web component, so it works on a blog, in docs or in any CMS page:

```html
<script type="module" src="https://playzor.net/_content/Playzor.Blazor/playzor-embed.js"></script>

<playzor-playground height="420px" view="split">
  <h3>Hello Playzor</h3>
</playzor-playground>
```

The code can come from the element's own content (as above), from `code`, from `files` as json, or
from `snippet-id`. Every parameter above has an attribute: `snippet-id`, `host`, `view`, `theme`,
`file`, `readonly`, `autorun`, `hide-header`, `height`, `auto-height`.

```html
<playzor-playground view="split" file="Components/Card.razor"
  files='{"__Main.razor":"<Card />","Components/Card.razor":"<h4>A card</h4>"}'></playzor-playground>
```

The module also exports `buildEmbedUrl(options)` and `encodeFiles(files)` if you would rather
build the link yourself.

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

Files in a folder land in a sub namespace (`Try.UserComponents.Components`), exactly like a real
Blazor project — the playground adds the matching `@using` automatically.
