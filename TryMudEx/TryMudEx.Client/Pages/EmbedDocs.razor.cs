using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Playzor.Blazor;
using Playzor.Blazor.Editor;
using Playzor.Blazor.Editor.Core;
using Playzor.Core;
using TryMudEx.Client.Services;
using Playzor.Blazor.Editor.Services;

namespace TryMudEx.Client.Pages;

public partial class EmbedDocs
{
    [Inject] private NavigationManager Navigation { get; set; }
    [Inject] private IJSRuntime Js { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; }
    [Inject] private BrandingService Branding { get; set; }
    [Inject] private PlayzorLocalizer L { get; set; }

    private PlayzorView _view = PlayzorView.Split;
    private bool _readOnly;

    private Brand Brand => Branding.Current;

    private string Host => Navigation.BaseUri.TrimEnd('/');

    private const string DemoCode = """
                                    <h3>Hello from an embed</h3>
                                    <p>Edit me, then press Run.</p>
                                    """;

    private string IframeSnippet =>
        $"""
         <iframe src="{Host}/embed/{InlineCode.Encode(new[] { new CodeFile { Path = CoreConstants.MainComponentFilePath, Content = DemoCode } })}"
                 style="width:100%;height:420px;border:0" title="{Brand.Name} playground" loading="lazy"></iframe>
         """;

    private string ComponentSnippet =>
        "@using Playzor.Blazor\n\n" +
        $"<PlayzorPlayground Code=\"@_code\" Height=\"420px\" Host=\"{Host}\" />\n\n" +
        "@code {\n" +
        "    private const string _code = \"<h3>Hello</h3>\";\n" +
        "}";

    private const string MultiFileSnippet = """
                                            <PlayzorPlayground Files="@_files" File="Components/Card.razor" />

                                            @code {
                                                private readonly Dictionary<string, string> _files = new()
                                                {
                                                    ["__Main.razor"] = "<Card />",
                                                    ["Components/Card.razor"] = "<h4>A card</h4>",
                                                };
                                            }
                                            """;

    private string WebComponentScriptSnippet =>
        $"<script type=\"module\" src=\"{Host}/_content/Playzor.Blazor/playzor-embed.js\"></script>";

    // a script child per file: the html parser leaves its content alone, so razor markup,
    // generics and @code blocks survive where plain element content would lose them
    private string WebComponentSnippet =>
        $"""
         <playzor-playground height="420px" host="{Host}">
           <script type="text/plain" data-playzor-file="__Main.razor">
             {DemoCode}
           </script>
         </playzor-playground>
         """;

    private string WebComponentFilesSnippet =>
        $$"""
          <playzor-playground host="{{Host}}" view="split" theme="dark" file="Components/Card.razor">
            <script type="text/plain" data-playzor-file="__Main.razor">
              <Card />
            </script>
            <script type="text/plain" data-playzor-file="Components/Card.razor">
              <MudText Typo="Typo.h5">A card</MudText>
            </script>
          </playzor-playground>
          """;

    /// <summary>A complete page around a snippet, so a reader can try it out right away.</summary>
    private async Task OpenPreviewAsync(string snippet)
    {
        // the element needs its module, so a snippet that does not carry the script tag gets one
        var body = snippet.Contains("playzor-embed.js") ? snippet : WebComponentScriptSnippet + "\n\n" + snippet;
        var page = $$"""
                     <!doctype html>
                     <html lang="en">
                     <head>
                       <meta charset="utf-8" />
                       <meta name="viewport" content="width=device-width, initial-scale=1" />
                       <title>{{Brand.Name}} embed</title>
                       <style>
                         body { font-family: system-ui, sans-serif; margin: 0; padding: 24px; background: #14151a; color: #e8e8ea; }
                         h1 { font-size: 15px; font-weight: 500; margin: 0 0 16px; opacity: .7; }
                       </style>
                     </head>
                     <body>
                       <h1>{{Brand.Name}} embed preview</h1>
                     {{body}}
                     </body>
                     </html>
                     """;

        if (!await Js.InvokeAsync<bool>(PlayzorJs.OpenHtmlInNewTab, page))
            Snackbar.Add(L["Could not open a window — check your popup blocker."], Severity.Warning);
    }

    private record EmbedOption(string Query, string Parameter, string Default, string Meaning);

    private IEnumerable<EmbedOption> Options => new[]
    {
        new EmbedOption("view=split|code|preview", "View", "split", L["Which side of the playground is visible."]),
        new EmbedOption("theme=auto|light|dark", "Theme", "auto", L["Auto follows the visitor's browser setting."]),
        new EmbedOption("file=<path>", "File", "__Main.razor", L["File shown first."]),
        new EmbedOption("readonly", "ReadOnly", "false", L["Shows the code but prevents edits."]),
        new EmbedOption("autorun=false", "AutoRun", "true", L["Compile and run as soon as the embed loads."]),
        new EmbedOption("hideheader", "HideHeader", "false", L["Hides the tab bar and buttons."]),
        new EmbedOption("—", "Height", "500px", L["Css height of the iframe."]),
        new EmbedOption("—", "AutoHeight", "false", L["Let the iframe follow the embed's content height."]),
        new EmbedOption("—", "SnippetId", "—", L["Embed a saved snippet by id instead of inline code."]),
        new EmbedOption("—", "Host", "https://playzor.net", L["Playground the embed is loaded from."]),
    };

    private async Task CopyAsync(string text)
    {
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", text);
        Snackbar.Add(L["Copied to clipboard"], Severity.Success, o => o.VisibleStateDuration = 1200);
    }
}
