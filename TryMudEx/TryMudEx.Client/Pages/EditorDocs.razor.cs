using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Playzor.Blazor.Editor.Services;
using Playzor.Core;
using TryMudEx.Client.Services;

namespace TryMudEx.Client.Pages;

/// <summary>
/// How to put the playground into your own app. The counterpart of <see cref="EmbedDocs"/>, which
/// is about showing a snippet on a page instead of hosting the editor.
/// </summary>
public partial class EditorDocs
{
    [Inject] private NavigationManager Navigation { get; set; }
    [Inject] private IJSRuntime Js { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; }
    [Inject] private BrandingService Branding { get; set; }
    [Inject] private PlayzorLocalizer L { get; set; }

    private Brand Brand => Branding.Current;

    private string Host => Navigation.BaseUri.TrimEnd('/');

    private const string InstallSnippet = "dotnet add package Playzor.Blazor.Editor";

    private const string ProgramSnippet = """
                                          // Program.cs
                                          builder.Services.AddScoped(_ => new HttpClient
                                          {
                                              BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
                                          });
                                          builder.Services.AddPlayzor();
                                          """;

    private const string ComponentSnippet = """
                                            @page "/playground"
                                            @using Playzor.Blazor.Editor.Components

                                            <PlayzorEditor Height="100%" AutoRun="true" />
                                            """;

    private const string IndexHtmlSnippet = """
                                            <!-- index.html -->
                                            <link href="_content/Playzor.Blazor.Editor/css/playzor-editor.css" rel="stylesheet" />
                                            <script src="_content/Playzor.Blazor.Editor/lib/monaco-editor/min/vs/loader.js"></script>
                                            <script src="_content/Playzor.Blazor.Editor/js/playzor-editor.js"></script>

                                            <!-- only for panels in their own window, and before blazor starts -->
                                            <script src="_content/MudBlazor.Extensions/js/mudExPopoutEvents.js"></script>
                                            """;

    private const string ServerSnippet = """
                                         // Program.cs of your server, from Playzor.Server
                                         builder.Services.AddPlayzorServer();
                                         …
                                         app.MapPlayzorApi();
                                         """;

    private string RemoteApiSnippet =>
        $"builder.Services.AddPlayzor(o => o.BaseAddress = \"{Host}\");";

    private const string PreviewSnippet = """
                                          <!-- index.html of the app that renders the preview -->
                                          <script src="_content/Playzor.Blazor.Editor/js/playzor-preview.js"></script>
                                          <script src="_framework/blazor.webassembly.js" autostart="false"></script>
                                          <script>
                                            Blazor.start({
                                              loadBootResource: function (type, name, defaultUri) {
                                                if (type === 'assembly' && name === 'Try.UserComponents.dll') {
                                                  const bytes = window.Playzor?.CodeExecution?.getUserComponentsDllBytes?.();
                                                  if (bytes && bytes.length) {
                                                    const url = URL.createObjectURL(new Blob([bytes]));
                                                    return fetch(url).finally(() => URL.revokeObjectURL(url));
                                                  }
                                                }
                                                return defaultUri;
                                              }
                                            });
                                          </script>
                                          """;

    private const string TailorSnippet = """
                                         <PlayzorEditor ToolButtons="PlayzorToolButtons.Standalone | PlayzorToolButtons.Theme"
                                                        Panels="PlayzorPanels.Files | PlayzorPanels.Preview | PlayzorPanels.Errors"
                                                        DarkMode="@_dark" DarkModeChanged="@(d => _dark = d)">
                                             <ToolBarContent>
                                                 <MudIconButton Icon="@Icons.Material.Outlined.Share" OnClick="@ShareAsync" />
                                             </ToolBarContent>
                                             <HeaderContent>
                                                 <MudText Typo="Typo.h6" Class="ml-2">My playground</MudText>
                                             </HeaderContent>
                                             <ChildContent>
                                                 <MudExDockItem Id="docs" Title="Docs" Direction="DockDirection.Right" CanPopout="true">
                                                     <MyDocsPanel />
                                                 </MudExDockItem>
                                             </ChildContent>
                                         </PlayzorEditor>
                                         """;

    private static readonly (string Id, string Text)[] Packages =
    {
        ("Playzor.Blazor.Editor", "The playground as a component, with monaco, panels and tool bar."),
        ("Playzor.Core", "The compiler underneath, without any ui."),
        ("Playzor.Server", "MapPlayzorApi(): the package proxy and optional snippet endpoints."),
        ("Playzor.UserComponents", "The stub assembly a compiled snippet replaces."),
        ("Playzor.Blazor", "The small embed for a foreign page — iframe component and web component."),
    };

    private async Task CopyAsync(string text)
    {
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", text);
        Snackbar.Add(L["Copied to clipboard"], Severity.Success, o => o.VisibleStateDuration = 1200);
    }
}
