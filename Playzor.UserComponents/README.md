[![Playzor](https://raw.githubusercontent.com/fgilde/MudBlazor.Extensions/refs/heads/main/docs/playzor_logo.png)](https://playzor.net)

# Playzor.UserComponents

The placeholder assembly a [Playzor](https://playzor.net) preview runs.

A playground compiles the user's snippet into an assembly named `Try.UserComponents` and swaps it
in at startup. This package holds the empty stand-in that ships with your app, so the preview has
something to load before the first run — and so the compiled snippet has a matching identity to
replace.

You only need it in the app that renders the **preview**, next to
[Playzor.Blazor.Editor](https://www.nuget.org/packages/Playzor.Blazor.Editor).

```html
<!-- index.html of the preview app, before blazor starts -->
<script src="_content/Playzor.Blazor.Editor/js/playzor-preview.js"></script>
<script src="_framework/blazor.webassembly.js" autostart="false"></script>
<script>
  Blazor.start({
    loadBootResource: function (type, name, defaultUri) {
      if (type === 'assembly' && name === 'Try.UserComponents.dll') {
        const bytes = window.Playzor?.CodeExecution?.getUserComponentsDllBytes?.();
        if (bytes && bytes.length) {
          const url = URL.createObjectURL(new Blob([bytes], { type: 'application/octet-stream' }));
          return fetch(url).finally(() => URL.revokeObjectURL(url));
        }
      }
      return defaultUri;
    }
  });
</script>
```

The snippet declares `@page "/__main"`, so the compiled component simply becomes a route of that
app — nothing else has to be registered. Point the editor's `CompiledPreviewUrl` at it.

## Why the name does not match the package

The assembly stays `Try.UserComponents`: every snippet saved so far declares
`namespace Try.UserComponents` in its `.cs` files, and the boot hook above swaps by exactly that
name. Renaming it would break shared links.
