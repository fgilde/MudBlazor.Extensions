using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Try.Core;
using TryMudEx.Client.Services;
using Playzor.Blazor.Services;

namespace TryMudEx.Client.Pages.Index;

/// <summary>Landing page for the playzor brand — a product page, not a MudBlazor showcase.</summary>
public partial class PlayzorHome
{
    [Inject] private NavigationManager Navigation { get; set; }
    [Inject] private BrandingService Branding { get; set; }
    [Inject] private PlayzorLocalizer L { get; set; }

    private Brand Brand => Branding.Current;

    private string Host => Navigation.BaseUri.TrimEnd('/');

    private const string HeroSnippet = """
                                       <h3>Counter</h3>
                                       <p>Clicked @_count times.</p>
                                       <button @onclick="() => _count++">Click me</button>

                                       @code {
                                           private int _count;
                                       }
                                       """;

    private const string EmbedTeaserSnippet = """
                                              <iframe src="https://playzor.net/embed/<code>"
                                                      style="width:100%;height:420px;border:0"></iframe>
                                              """;

    private record Feature(string Icon, string Title, string Text);

    private IEnumerable<Feature> Features => new[]
    {
        new Feature("⚡", L["Compiles in the browser"],
            L["Roslyn runs on WebAssembly — your code never leaves the tab unless you share it."]),
        new Feature("📁", L["Real projects"],
            L["Several files, folders and sub namespaces, just like a project on your machine."]),
        new Feature("📦", L["NuGet packages"],
            L["Search, install and use packages including their dependencies."]),
        new Feature("🧩", L["Dockable panels"],
            L["Arrange editor, preview, console and errors the way you like — or pop a panel into its own window."]),
        new Feature("🔗", L["Shareable links"],
            L["Every snippet is a url. Short links for saved snippets, self contained links for everything else."]),
        new Feature("🖥️", L["Embeddable"],
            L["Drop a live, editable playground into any page with one iframe."]),
    };
}
