using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Helper;
using MudEx.WebComponents;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServicesWithExtensions();

// components load file content themselves. Relative urls in the markup belong to the page that
// embeds the components, and HostEnvironment.BaseAddress is exactly that page.
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// hosts the MudBlazor providers (theme, popover, dialog, snackbar) for all custom elements on the page
builder.RootComponents.Add<MudExWcRoot>("#mudex-wc-root");
WebComponentRegistrar.RegisterAll(builder.RootComponents);

var host = builder.Build();

// mudex.js knows where it was loaded from. Without this, lazily imported component scripts
// would be resolved against the hosting page instead of the CDN.
try
{
    if (host.Services.GetService(typeof(IJSRuntime)) is IJSInProcessRuntime js)
    {
        MudExWebComponents.SetAssetBase(js.Invoke<string>("MudEx.getAssetBase"));
        js.InvokeVoid("MudEx.__setTags", WebComponentRegistrar.RegisteredTags);
    }
}
catch (Exception e)
{
    Console.WriteLine($"[MudEx] could not read asset base: {e.Message}");
}

await host.RunAsync();
