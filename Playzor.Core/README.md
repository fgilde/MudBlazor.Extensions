# Playzor.Core

Compiles Blazor components in the browser. This is the engine under
[Playzor.Blazor.Editor](https://www.nuget.org/packages/Playzor.Blazor.Editor) — use it directly
when you want the compiler without the editor ui.

```csharp
var result = await compilationService.CompileToAssemblyAsync(codeFiles, packages, updateStatus);
if (result.Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error))
    LoadIntoPreview(result.AssemblyBytes);
```

## What is in it

| Type | Purpose |
|---|---|
| `CompilationService` | Razor pipeline plus roslyn, running on WebAssembly. Produces an assembly from a set of code files |
| `CodeFile`, `CodeFileType` | A file of the snippet — razor, c# or the package reference list |
| `CompilationDiagnostic` | One error or warning, mapped back to the line the user wrote |
| `NugetReferenceService` | Resolves packages including their dependencies and picks the right target framework |
| `InlineCode` | Encodes a snippet into an url (deflate + base64url) — the format shared links use |
| `Brand` | Title, description, colors and default snippet per domain |

## The server seam

`IPlayzorApi` is everything the browser cannot do on its own. In practice that is one thing:
downloading a `.nupkg`, because nuget.org answers without CORS headers.

```csharp
// own server, see the Playzor.Server package and its MapPlayzorApi()
new PlayzorApi(httpClient)

// no own server: borrow the public playground
new PlayzorApi(httpClient, PlayzorApiOptions.PlayzorNet)

// or your own index entirely
class MyApi : IPlayzorApi { … }
```

`IPlayzorSnippetStore` is the optional counterpart for saving, loading and listing snippets.
`PlayzorHttpSnippetStore` talks to the endpoints of the Playzor.Server package.

## Note on dependencies

The compiler hands MudBlazor and MudBlazor.Extensions to the user code as ambient references and
usings, so a snippet can render mud components without declaring anything. That is why this
package depends on them.
