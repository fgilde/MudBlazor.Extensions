# Playzor.Server

The server half of a self hosted [Playzor](https://playzor.net) playground.

```csharp
// Program.cs
builder.Services.AddPlayzorServer();
…
app.MapPlayzorApi();
```

That is all a playground needs from its host. Without it the editor cannot install nuget
packages — nuget.org answers without CORS headers, so the browser cannot fetch a `.nupkg` itself
and needs a server to forward it.

## Endpoints

| Route | Purpose |
|---|---|
| `GET api/playzor/nuget/package/{id}/{version}` | Forwards the package and lets the browser cache it (id + version is immutable) |
| `POST api/playzor/snippets` | Stores a snippet archive, answers with its id |
| `GET api/playzor/snippets/{id}` | Reads a stored snippet |
| `GET api/playzor/snippets/samples` | Names of the ready made samples |
| `GET api/playzor/snippets/samples/{name}` | Reads a sample |

The snippet endpoints need an `IPlayzorSnippetStorage`; without one they answer `501`. A snippet
is a zip archive with one entry per file — the shape the editor sends and expects.

```csharp
builder.Services.AddSingleton<IPlayzorSnippetStorage>(
    new FilePlayzorSnippetStorage(snippetDirectory: "App_Data/snippets",
                                  sampleDirectory: "wwwroot/data"));
```

`FilePlayzorSnippetStorage` writes to the local disk. Implement the interface yourself for blob
storage, a database or anything else.

## Options

```csharp
builder.Services.AddPlayzorServer(o =>
{
    o.RoutePrefix = "api/playzor";                 // prefix of every route
    o.PackageCacheDuration = TimeSpan.FromDays(365);
    o.AllowedOrigins.Add("https://my-docs.example");  // let another site use this proxy
    o.EnableSnippets = true;
});
```

`AllowedOrigins` is empty by default, so the proxy answers same origin requests only. Add `*` to
open it to everyone — worth thinking about, because every package download then runs through your
bandwidth.

## The other half

Pair this with [Playzor.Blazor.Editor](https://www.nuget.org/packages/Playzor.Blazor.Editor) in
your Blazor app, and point it at these routes:

```csharp
builder.Services.AddPlayzor();          // default routes, same origin
builder.Services.AddPlayzorHttpSnippetStore();
```
