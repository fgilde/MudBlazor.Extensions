using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Playzor.Blazor;
using Try.Core;
using TryMudEx.Client.Services;
using Playzor.Blazor.Services;

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
