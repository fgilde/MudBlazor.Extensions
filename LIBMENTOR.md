# AI documentation

LibMentor generates versioned documentation from this repository. Restore the pinned tool with `dotnet tool restore`, then run `dotnet tool run libmentor -- generate`.

CI resolves the library version from its existing metadata and adds a source revision suffix. This describes the source snapshot, rather than claiming that a package with the same version has already been released. Files are generated under `.libmentor/`; repository contributor instructions are preserved.

The consumer entry point after deployment is [llms.txt](https://fgilde.github.io/MudBlazor.Extensions/ai/llms.txt). The same output is available as the `libmentor-documentation` workflow artifact. The first LibMentor 0.1.0 NuGet release must exist before tool restore can succeed.

The published skill is under `ai/skills/`. Copy the complete library folder, including its references, to your client's skills directory.

Generation validates local Markdown links and explicit API-symbol links. C# source declarations are parsed without a project build; see extraction evidence in `knowledge.json`. Compiled Razor APIs require compiler XML input. Markdown code fences are not compiled automatically.
